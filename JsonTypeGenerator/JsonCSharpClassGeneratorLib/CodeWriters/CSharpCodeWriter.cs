using System;
using System.IO;
using Xamasoft.JsonClassGenerator;

namespace JsonTypeGenerator.JsonCSharpClassGeneratorLib.CodeWriters
{
    public class CSharpCodeWriter : ICodeWriter
    {
        public string DisplayName => "C#";
        
        private const string NoRenameAttribute = "[Obfuscation(Feature = \"renaming\", Exclude = true)]";
        private const string NoPruneAttribute = "[Obfuscation(Feature = \"trigger\", Exclude = false)]";

        public string GetTypeName(JsonType type, IJsonClassGeneratorConfig config)
        {
            return type.Type switch
            {
                JsonTypeEnum.Anything => "object",
                JsonTypeEnum.Array => "IList<" + GetTypeName(type.InternalType, config) + ">",
                JsonTypeEnum.Dictionary => "Dictionary<string, " + GetTypeName(type.InternalType, config) + ">",
                JsonTypeEnum.Boolean => "bool",
                JsonTypeEnum.Float => "double",
                JsonTypeEnum.Integer => "int",
                JsonTypeEnum.Long => "long",
                JsonTypeEnum.Date => "DateTime",
                JsonTypeEnum.NonConstrained => "object",
                JsonTypeEnum.NullableBoolean => "bool?",
                JsonTypeEnum.NullableFloat => "double?",
                JsonTypeEnum.NullableInteger => "int?",
                JsonTypeEnum.NullableLong => "long?",
                JsonTypeEnum.NullableDate => "DateTime?",
                JsonTypeEnum.NullableSomething => "object",
                JsonTypeEnum.Object => type.AssignedName,
                JsonTypeEnum.String => "string",
                _ => throw new NotSupportedException("Unsupported json type")
            };
        }

        private bool ShouldApplyNoRenamingAttribute(IJsonClassGeneratorConfig config)
        {
            return config.ApplyObfuscationAttributes && !config.UsePascalCase;
        }
        private bool ShouldApplyNoPruneAttribute(IJsonClassGeneratorConfig config)
        {
            return config.ApplyObfuscationAttributes && config.UseProperties;
        }

        public void WriteFileStart(IJsonClassGeneratorConfig config, TextWriter sw)
        {
            if (config.UseNamespaces)
            {
                sw.WriteLine("using System;");
                sw.WriteLine("using System.Collections.Generic;");
                if (ShouldApplyNoPruneAttribute(config) || ShouldApplyNoRenamingAttribute(config))
                    sw.WriteLine("using System.Reflection;");
                if (config.UsePascalCase && config.PropertyAttribute == "JsonProperty")
                    sw.WriteLine("using Newtonsoft.Json;");
                //sw.WriteLine("using Newtonsoft.Json.Linq;");
                if (config.SecondaryNamespace != null && config.HasSecondaryClasses && !config.UseNestedClasses)
                {
                    sw.WriteLine("using {0};", config.SecondaryNamespace);
                }
            }

            if (config.UseNestedClasses)
            {
                sw.WriteLine("    {0} class {1}", config.InternalVisibility ? "internal" : "public", config.MainClass);
                sw.WriteLine("    {");
            }
        }

        public void WriteFileEnd(IJsonClassGeneratorConfig config, TextWriter sw)
        {
            if (config.UseNestedClasses)
            {
                sw.WriteLine("    }");
            }
        }


        public void WriteNamespaceStart(IJsonClassGeneratorConfig config, TextWriter sw, bool root)
        {
            sw.WriteLine();
            sw.WriteLine("namespace {0}", root && !config.UseNestedClasses ? config.Namespace : (config.SecondaryNamespace ?? config.Namespace));
            sw.WriteLine("{");
            sw.WriteLine();
        }

        public void WriteNamespaceEnd(IJsonClassGeneratorConfig config, TextWriter sw, bool root)
        {
            sw.WriteLine("}");
        }

        public void WriteClass(IJsonClassGeneratorConfig config, TextWriter sw, JsonType type)
        {

            var visibility = config.InternalVisibility ? "internal" : "public";

            if (config.UseNestedClasses)
            {
                if (!type.IsRoot)
                {
                    if (config.PropertyAttribute == "DataMember")
                    {
                        sw.WriteLine("        [DataContract]");
                    }

                    if (ShouldApplyNoRenamingAttribute(config)) sw.WriteLine("        " + NoRenameAttribute);
                    if (ShouldApplyNoPruneAttribute(config)) sw.WriteLine("        " + NoPruneAttribute);
                    sw.WriteLine("        {0} class {1}", visibility, type.AssignedName);
                    sw.WriteLine("        {");
                }
            }
            else
            {
                if (config.PropertyAttribute == "DataMember")
                {
                    sw.WriteLine("    [DataContract]");
                }

                if (ShouldApplyNoRenamingAttribute(config)) sw.WriteLine("    " + NoRenameAttribute);
                if (ShouldApplyNoPruneAttribute(config)) sw.WriteLine("    " + NoPruneAttribute);
                sw.WriteLine("    {0} class {1}", visibility, type.AssignedName);
                sw.WriteLine("    {");
            }

            var prefix = config.UseNestedClasses && !type.IsRoot ? "            " : "        ";

            var shouldSuppressWarning = config.InternalVisibility && !config.UseProperties;
            if (shouldSuppressWarning)
            {
                sw.WriteLine("#pragma warning disable 0649");
                if (!config.UsePascalCase) sw.WriteLine();
            }

            WriteClassMembers(config, sw, type, prefix);

            if (shouldSuppressWarning)
            {
                sw.WriteLine();
                sw.WriteLine("#pragma warning restore 0649");
                sw.WriteLine();
            }


            if (config.UseNestedClasses && !type.IsRoot)
                sw.WriteLine("        }");

            if (!config.UseNestedClasses)
                sw.WriteLine("    }");

            sw.WriteLine();


        }
        
        private void WriteClassMembers(IJsonClassGeneratorConfig config, TextWriter sw, JsonType type, string prefix)
        {
            foreach (var field in type.Fields)
            {
                if (config.PropertyAttribute != "None" || config.ExamplesInDocumentation) sw.WriteLine();

                if (config.ExamplesInDocumentation)
                {
                    sw.WriteLine(prefix + "/// <summary>");
                    sw.WriteLine(prefix + "/// Examples: " + field.GetExamplesText());
                    sw.WriteLine(prefix + "/// </summary>");
                }

                if (config.UsePascalCase || config.PropertyAttribute != "None")
                {
                    if (config.PropertyAttribute == "DataMember")
                        sw.WriteLine(prefix + "[" + config.PropertyAttribute + "(Name=\"{0}\")]", field.JsonMemberName);
                    else if (config.PropertyAttribute == "JsonProperty")
                        sw.WriteLine(prefix + "[" + config.PropertyAttribute + "(\"{0}\")]", field.JsonMemberName);
                }

                if (config.UseProperties)
                {
                    sw.WriteLine(prefix + "public {0} {1} {{ get; set; }}", field.Type.GetTypeName(), field.MemberName);
                }
                else
                {
                    sw.WriteLine(prefix + "public {0} {1};", field.Type.GetTypeName(), field.MemberName);
                }
            }
        }
    }
}
