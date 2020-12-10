// Copyright © 2010 Xamasoft

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Xamasoft.JsonClassGenerator
{
    public class JsonType
    {
        private JsonType(IJsonClassGeneratorConfig generator)
        {
            this._generator = generator;
        }

        public JsonType(IJsonClassGeneratorConfig generator, JToken token)
            : this(generator)
        {

            Type = GetFirstTypeEnum(token);

            if (Type == JsonTypeEnum.Array)
            {
                var array = (JArray)token;
                InternalType = GetCommonType(generator, array.ToArray());
            }
        }

        internal static JsonType GetNull(IJsonClassGeneratorConfig generator)
        {
            return new JsonType(generator, JsonTypeEnum.NullableSomething);
        }

        private readonly IJsonClassGeneratorConfig _generator;

        private JsonType(IJsonClassGeneratorConfig generator, JsonTypeEnum type)
            : this(generator)
        {
            this.Type = type;
        }

        private static JsonType GetCommonType(IJsonClassGeneratorConfig generator, JToken[] tokens)
        {
            if (tokens.Length == 0) return new JsonType(generator, JsonTypeEnum.NonConstrained);

            var common = new JsonType(generator, tokens[0]).MaybeMakeNullable(generator);

            for (int i = 1; i < tokens.Length; i++)
            {
                var current = new JsonType(generator, tokens[i]);
                common = common.GetCommonType(current);
            }

            return common;
        }

        internal JsonType MaybeMakeNullable(IJsonClassGeneratorConfig generator)
        {
            if (!generator.AlwaysUseNullableValues) return this;
            return this.GetCommonType(JsonType.GetNull(generator));
        }
        
        public JsonTypeEnum Type { get; }
        public JsonType InternalType { get; private set; }
        public string AssignedName { get; private set; }

        public void AssignName(string name)
        {
            AssignedName = name;
        }
        
        public string GetReaderName()
        {
            return Type switch
            {
                JsonTypeEnum.Anything => "ReadObject",
                JsonTypeEnum.NullableSomething => "ReadObject",
                JsonTypeEnum.NonConstrained => "ReadObject",
                JsonTypeEnum.Object => $"ReadStronglyTypedObject<{AssignedName}>",
                JsonTypeEnum.Array => $"ReadArray<{InternalType.GetTypeName()}>",
                _ => $"Read{Enum.GetName(typeof(JsonTypeEnum), Type)}"
            };
        }

        public JsonType GetInnermostType()
        {
            if (Type != JsonTypeEnum.Array) throw new InvalidOperationException();
            if (InternalType.Type != JsonTypeEnum.Array) return InternalType;
            return InternalType.GetInnermostType();
        }

        public string GetTypeName()
        {
            return _generator.CodeWriter.GetTypeName(this, _generator);
        }

        public string GetJTokenType()
        {
            return Type switch
            {
                JsonTypeEnum.Boolean => "JValue",
                JsonTypeEnum.Integer => "JValue",
                JsonTypeEnum.Long => "JValue",
                JsonTypeEnum.Float => "JValue",
                JsonTypeEnum.Date => "JValue",
                JsonTypeEnum.NullableBoolean => "JValue",
                JsonTypeEnum.NullableInteger => "JValue",
                JsonTypeEnum.NullableLong => "JValue",
                JsonTypeEnum.NullableFloat => "JValue",
                JsonTypeEnum.NullableDate => "JValue",
                JsonTypeEnum.String => "JValue",
                JsonTypeEnum.Array => "JArray",
                JsonTypeEnum.Dictionary => "JObject",
                JsonTypeEnum.Object => "JObject",
                _ => "JToken"
            };
        }

        public JsonType GetCommonType(JsonType type2)
        {
            var commonType = GetCommonTypeEnum(this.Type, type2.Type);

            if (commonType == JsonTypeEnum.Array)
            {
                if (type2.Type == JsonTypeEnum.NullableSomething) return this;
                if (this.Type == JsonTypeEnum.NullableSomething) return type2;
                var commonInternalType = InternalType.GetCommonType(type2.InternalType).MaybeMakeNullable(_generator);
                if (commonInternalType != InternalType) return new JsonType(_generator, JsonTypeEnum.Array) { InternalType = commonInternalType };
            }

            if (this.Type == commonType) return this;
            return new JsonType(_generator, commonType).MaybeMakeNullable(_generator);
        }
        
        private static bool IsNull(JsonTypeEnum type)
        {
            return type == JsonTypeEnum.NullableSomething;
        }
        
        private JsonTypeEnum GetCommonTypeEnum(JsonTypeEnum type1, JsonTypeEnum type2)
        {
            if (type1 == JsonTypeEnum.NonConstrained) return type2;
            if (type2 == JsonTypeEnum.NonConstrained) return type1;

            switch (type1)
            {
                case JsonTypeEnum.Boolean:
                    if (IsNull(type2)) return JsonTypeEnum.NullableBoolean;
                    if (type2 == JsonTypeEnum.Boolean) return type1;
                    break;
                case JsonTypeEnum.NullableBoolean:
                    if (IsNull(type2)) return type1;
                    if (type2 == JsonTypeEnum.Boolean) return type1;
                    break;
                case JsonTypeEnum.Integer:
                    if (IsNull(type2)) return JsonTypeEnum.NullableInteger;
                    if (type2 == JsonTypeEnum.Float) return JsonTypeEnum.Float;
                    if (type2 == JsonTypeEnum.Long) return JsonTypeEnum.Long;
                    if (type2 == JsonTypeEnum.Integer) return type1;
                    break;
                case JsonTypeEnum.NullableInteger:
                    if (IsNull(type2)) return type1;
                    if (type2 == JsonTypeEnum.Float) return JsonTypeEnum.NullableFloat;
                    if (type2 == JsonTypeEnum.Long) return JsonTypeEnum.NullableLong;
                    if (type2 == JsonTypeEnum.Integer) return type1;
                    break;
                case JsonTypeEnum.Float:
                    if (IsNull(type2)) return JsonTypeEnum.NullableFloat;
                    if (type2 == JsonTypeEnum.Float) return type1;
                    if (type2 == JsonTypeEnum.Integer) return type1;
                    if (type2 == JsonTypeEnum.Long) return type1;
                    break;
                case JsonTypeEnum.NullableFloat:
                    if (IsNull(type2)) return type1;
                    if (type2 == JsonTypeEnum.Float) return type1;
                    if (type2 == JsonTypeEnum.Integer) return type1;
                    if (type2 == JsonTypeEnum.Long) return type1;
                    break;
                case JsonTypeEnum.Long:
                    if (IsNull(type2)) return JsonTypeEnum.NullableLong;
                    if (type2 == JsonTypeEnum.Float) return JsonTypeEnum.Float;
                    if (type2 == JsonTypeEnum.Integer) return type1;
                    break;
                case JsonTypeEnum.NullableLong:
                    if (IsNull(type2)) return type1;
                    if (type2 == JsonTypeEnum.Float) return JsonTypeEnum.NullableFloat;
                    if (type2 == JsonTypeEnum.Integer) return type1;
                    if (type2 == JsonTypeEnum.Long) return type1;
                    break;
                case JsonTypeEnum.Date:
                    if (IsNull(type2)) return JsonTypeEnum.NullableDate;
                    if (type2 == JsonTypeEnum.Date) return JsonTypeEnum.Date;
                    break;
                case JsonTypeEnum.NullableDate:
                    if (IsNull(type2)) return type1;
                    if (type2 == JsonTypeEnum.Date) return type1;
                    break;
                case JsonTypeEnum.NullableSomething:
                    if (IsNull(type2)) return type1;
                    if (type2 == JsonTypeEnum.String) return JsonTypeEnum.String;
                    if (type2 == JsonTypeEnum.Integer) return JsonTypeEnum.NullableInteger;
                    if (type2 == JsonTypeEnum.Float) return JsonTypeEnum.NullableFloat;
                    if (type2 == JsonTypeEnum.Long) return JsonTypeEnum.NullableLong;
                    if (type2 == JsonTypeEnum.Boolean) return JsonTypeEnum.NullableBoolean;
                    if (type2 == JsonTypeEnum.Date) return JsonTypeEnum.NullableDate;
                    if (type2 == JsonTypeEnum.Array) return JsonTypeEnum.Array;
                    if (type2 == JsonTypeEnum.Object) return JsonTypeEnum.Object;
                    break;
                case JsonTypeEnum.Object:
                    if (IsNull(type2)) return type1;
                    if (type2 == JsonTypeEnum.Object) return type1;
                    if (type2 == JsonTypeEnum.Dictionary) throw new ArgumentException();
                    break;
                case JsonTypeEnum.Dictionary:
                    throw new ArgumentException();
                case JsonTypeEnum.Array:
                    if (IsNull(type2)) return type1;
                    if (type2 == JsonTypeEnum.Array) return type1;
                    break;
                case JsonTypeEnum.String:
                    if (IsNull(type2)) return type1;
                    if (type2 == JsonTypeEnum.String) return type1;
                    break;
            }

            return JsonTypeEnum.Anything;
        }

        private static JsonTypeEnum GetFirstTypeEnum(JToken token)
        {
            var type = token.Type;
            if (type == JTokenType.Integer)
            {
                return (long)((JValue)token).Value < int.MaxValue ? JsonTypeEnum.Integer : JsonTypeEnum.Long;
            }

            return type switch
            {
                JTokenType.Array => JsonTypeEnum.Array,
                JTokenType.Boolean => JsonTypeEnum.Boolean,
                JTokenType.Float => JsonTypeEnum.Float,
                JTokenType.Null => JsonTypeEnum.NullableSomething,
                JTokenType.Undefined => JsonTypeEnum.NullableSomething,
                JTokenType.String => JsonTypeEnum.String,
                JTokenType.Object => JsonTypeEnum.Object,
                JTokenType.Date => JsonTypeEnum.Date,
                _ => JsonTypeEnum.Anything
            };
        }

        public IList<FieldInfo> Fields { get; internal set; }
        public bool IsRoot { get; internal set; }
    }
}
