// Copyright © 2010 Xamasoft

using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Xamasoft.JsonClassGenerator
{
    public class FieldInfo
    {

        public FieldInfo(IJsonClassGeneratorConfig generator, string jsonMemberName, JsonType type, bool usePascalCase, IList<object> Examples)
        {
            this._generator = generator;
            this.JsonMemberName = jsonMemberName;
            this.MemberName = jsonMemberName;
            if (usePascalCase) MemberName = JsonTypeGenerator.JsonCSharpClassGeneratorLib.JsonClassGenerator.ToTitleCase(MemberName);
            this.Type = type;
            this.Examples = Examples;
        }
        private IJsonClassGeneratorConfig _generator;
        public string MemberName { get; private set; }
        public string JsonMemberName { get; private set; }
        public JsonType Type { get; private set; }
        public IList<object> Examples { get; private set; }

        public string GetGenerationCode(string jobject)
        {
            var field = this;
            return field.Type.Type switch
            {
                JsonTypeEnum.Array => string.Format(
                    "({1})JsonClassHelper.ReadArray<{4}>(JsonClassHelper.GetJToken<JArray>({0}, \"{2}\"), JsonClassHelper.{3}, typeof({5}))",
                    jobject, field.Type.GetTypeName(), field.JsonMemberName,
                    field.Type.GetInnermostType().GetReaderName(), field.Type.GetInnermostType().GetTypeName(),
                    field.Type.GetTypeName()),
                JsonTypeEnum.Dictionary => string.Format(
                    "({1})JsonClassHelper.ReadDictionary<{2}>(JsonClassHelper.GetJToken<JObject>({0}, \"{3}\"))",
                    jobject, field.Type.GetTypeName(), field.Type.InternalType.GetTypeName(), field.JsonMemberName),
                _ => string.Format("JsonClassHelper.{1}(JsonClassHelper.GetJToken<{2}>({0}, \"{3}\"))", jobject,
                    field.Type.GetReaderName(), field.Type.GetJTokenType(), field.JsonMemberName)
            };
        }

        public string GetExamplesText()
        {
            return string.Join(", ", Examples.Take(5).Select(JsonConvert.SerializeObject).ToArray());
        }

    }
}
