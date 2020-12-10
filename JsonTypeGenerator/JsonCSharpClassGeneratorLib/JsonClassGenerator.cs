// Copyright © 2010 Xamasoft

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using JsonTypeGenerator.JsonCSharpClassGeneratorLib.CodeWriters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xamasoft.JsonClassGenerator;

namespace JsonTypeGenerator.JsonCSharpClassGeneratorLib
{
    public class JsonClassGenerator : IJsonClassGeneratorConfig
    {
        public string Example { get; set; }
        public string Namespace { get; set; }
        public string SecondaryNamespace { get; set; }
        public bool UseProperties { get; set; }
        public bool InternalVisibility { get; set; }
        public string MainClass { get; set; }
        public bool UsePascalCase { get; set; }
        public bool UseNestedClasses { get; set; }
        public bool ApplyObfuscationAttributes { get; set; }
        public ICodeWriter CodeWriter { get; set; }
        public TextWriter OutputStream { get; set; }
        public bool AlwaysUseNullableValues { get; set; }
        public bool ExamplesInDocumentation { get; set; }

        public string PropertyAttribute { get; set; }

        private readonly Pluralizer _pluralizationService = new();

        private bool used;
        public bool UseNamespaces => Namespace != null;

        public void GenerateClasses()
        {
            CodeWriter ??= new CSharpCodeWriter();
            
            if (used) throw new InvalidOperationException("This instance of JsonClassGenerator has already been used. Please create a new instance.");
            used = true;

            JObject[] examples;
            var example = Example;
            using (var sr = new StringReader(example))
            using (var reader = new JsonTextReader(sr))
            {
                var json = JToken.ReadFrom(reader);
                examples = json switch
                {
                    JArray array => array.Cast<JObject>().ToArray(),
                    JObject jObject => new[] {jObject},
                    _ => throw new Exception("Sample JSON must be either a JSON array, or a JSON object.")
                };
            }

            Types = new List<JsonType>();
            _names.Add(MainClass);
            var rootType = new JsonType(this, examples[0]) {IsRoot = false};
            rootType.AssignName(MainClass);
            GenerateClass(examples, rootType);

            if (OutputStream != null)
            {
                CodeWriter.WriteFileStart(this, OutputStream);
                WriteClassesToFile(OutputStream, Types);
                CodeWriter.WriteFileEnd(this, OutputStream);
            }
        }

        private void WriteClassesToFile(TextWriter sw, IEnumerable<JsonType> types)
        {
            var inNamespace = false;
            var rootNamespace = false;

            foreach (var type in types)
            {
                if (UseNamespaces && inNamespace && rootNamespace != type.IsRoot && SecondaryNamespace != null) { CodeWriter.WriteNamespaceEnd(this, sw, rootNamespace); inNamespace = false; }
                if (UseNamespaces && !inNamespace) { CodeWriter.WriteNamespaceStart(this, sw, type.IsRoot); inNamespace = true; rootNamespace = type.IsRoot; }

                CodeWriter.WriteClass(this, sw, type);
            }
            if (UseNamespaces && inNamespace) CodeWriter.WriteNamespaceEnd(this, sw, rootNamespace);
        }

        private readonly List<string> _classNames = new();
        private void GenerateClass(JObject[] examples, JsonType type)
        {
            var jsonFields = new Dictionary<string, JsonType>();
            var fieldExamples = new Dictionary<string, IList<object>>();

            var first = true;

            foreach (var obj in examples)
            {
                foreach (var prop in obj.Properties())
                {
                    var currentType = new JsonType(this, prop.Value);
                    var propName = prop.Name;
                    if (jsonFields.TryGetValue(propName, out var fieldType))
                    {

                        var commonType = fieldType.GetCommonType(currentType);

                        jsonFields[propName] = commonType;
                    }
                    else
                    {
                        var commonType = currentType;
                        if (first) commonType = commonType.MaybeMakeNullable(this);
                        else commonType = commonType.GetCommonType(JsonType.GetNull(this));
                        jsonFields.Add(propName, commonType);
                        fieldExamples[propName] = new List<object>();
                    }
                    var fe = fieldExamples[propName];
                    var val = prop.Value;
                    if (val.Type == JTokenType.Null || val.Type == JTokenType.Undefined)
                    {
                        if (!fe.Contains(null))
                        {
                            fe.Insert(0, null);
                        }
                    }
                    else
                    {
                        var v = val.Type == JTokenType.Array || val.Type == JTokenType.Object ? val : val.Value<object>();
                        if (!fe.Any(x => v.Equals(x)))
                            fe.Add(v);
                    }
                }
                first = false;
            }

            if (UseNestedClasses)
            {
                foreach (var field in jsonFields)
                {
                    _names.Add(field.Key.ToLower());
                }
            }

            foreach (var field in jsonFields)
            {
                var fieldType = field.Value;
                if (fieldType.Type == JsonTypeEnum.Object)
                {
                    var subexamples = new List<JObject>(examples.Length);
                    foreach (var obj in examples)
                    {
                        if (obj.TryGetValue(field.Key, out var value))
                        {
                            if (value.Type == JTokenType.Object)
                            {
                                subexamples.Add((JObject)value);
                            }
                        }
                    }

                    fieldType.AssignName(CreateUniqueClassName(field.Key));
                    if (!_classNames.Contains(field.Key))
                    {
                        GenerateClass(subexamples.ToArray(), fieldType);

                        _classNames.Add(field.Key);
                    }
                }

                if (fieldType.InternalType != null && fieldType.InternalType.Type == JsonTypeEnum.Object)
                {
                    var subexamples = new List<JObject>(examples.Length);
                    foreach (var obj in examples)
                    {
                        if (obj.TryGetValue(field.Key, out var value))
                        {
                            switch (value.Type)
                            {
                                case JTokenType.Array:
                                {
                                    foreach (var item in (JArray)value)
                                    {
                                        if (!(item is JObject)) throw new NotSupportedException("Arrays of non-objects are not supported yet.");
                                        subexamples.Add((JObject)item);
                                    }

                                    break;
                                }
                                case JTokenType.Object:
                                {
                                    foreach (var item in (JObject)value)
                                    {
                                        if (!(item.Value is JObject)) throw new NotSupportedException("Arrays of non-objects are not supported yet.");

                                        subexamples.Add((JObject)item.Value);
                                    }

                                    break;
                                }
                            }
                        }
                    }

                    field.Value.InternalType.AssignName(CreateUniqueClassNameFromPlural(field.Key));
                    GenerateClass(subexamples.ToArray(), field.Value.InternalType);
                }
            }

            type.Fields = jsonFields.Select(x => new FieldInfo(this, x.Key, x.Value, UsePascalCase, fieldExamples[x.Key])).ToArray();

            Types.Add(type);

        }

        public IList<JsonType> Types { get; private set; }
        private readonly HashSet<string> _names = new();

        private string CreateUniqueClassName(string name)
        {
            name = ToTitleCase(name);

            return name;
        }

        private string CreateUniqueClassNameFromPlural(string plural)
        {
            plural = ToTitleCase(plural);
            return CreateUniqueClassName(_pluralizationService.Singularize(plural));
        }

        internal static string ToTitleCase(string str)
        {
            var sb = new StringBuilder(str.Length);
            var flag = true;

            foreach (var c in str)
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(flag ? char.ToUpper(c) : c);
                    flag = false;
                }
                else
                {
                    flag = true;
                }
            }

            return sb.ToString();
        }

        public bool HasSecondaryClasses => Types.Count > 1;
    }
}
