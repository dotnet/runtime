// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization.Converters;

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// todo
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class JsonObjectInfo<T> : JsonTypeInfo<T>
    {
        internal JsonObjectInfo(Type type, JsonSerializerOptions options) :
            base(type, options, ClassType.Object)
        { }

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="createObjectFunc"></param>
        /// <param name="serializeFunc"></param>
        /// <param name="deserializeFunc"></param>
        /// <param name="options"></param>
        public JsonObjectInfo(
            ConstructorDelegate createObjectFunc,
            SerializeDelegate serializeFunc,
            DeserializeDelegate deserializeFunc,
            JsonSerializerOptions? options) : base(typeof(T), options, ClassType.Object)
        {
            if (createObjectFunc == null)
            {
                throw new ArgumentNullException(nameof(createObjectFunc));
            }

            CreateObject = createObjectFunc;

            if (serializeFunc == null)
            {
                throw new ArgumentNullException(nameof(serializeFunc));
            }
            Serialize = serializeFunc;

            if (deserializeFunc == null)
            {
                throw new ArgumentNullException(nameof(deserializeFunc));
            }
            Deserialize = deserializeFunc;

            JsonConverter converter = new ObjectCodeGenConverter<T>();
            ConverterBase = converter;
            PropertyInfoForClassInfo = CreatePropertyInfoForClassInfo(Type, Type, converter, Options);
        }

        /// <summary>
        /// todo
        /// </summary>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="propertyName"></param>
        /// <param name="getter"></param>
        /// <param name="setter"></param>
        /// <param name="classInfo"></param>
        public JsonPropertyInfo<TProperty> AddProperty<TProperty>(
            string propertyName,
            Func<object, TProperty>? getter,
            Action<object, TProperty>? setter,
            JsonTypeInfo<TProperty> classInfo)
        {
            var jsonPropertyInfo = new JsonPropertyInfo<TProperty>();
            if (getter != null)
            {
                jsonPropertyInfo.Get = getter;
                jsonPropertyInfo.ShouldSerialize = true;
            }

            if (setter != null)
            {
                jsonPropertyInfo.Set = setter;
                jsonPropertyInfo.ShouldDeserialize = true;
            }

            jsonPropertyInfo.Converter = (JsonConverter<TProperty>)classInfo.ConverterBase;
            jsonPropertyInfo.ClassType = jsonPropertyInfo.Converter!.ClassType;

            jsonPropertyInfo.NameAsString = propertyName;
            jsonPropertyInfo.NameAsUtf8Bytes = Encoding.UTF8.GetBytes(propertyName);
            jsonPropertyInfo.EscapedNameSection = JsonHelpers.GetEscapedPropertyNameSection(jsonPropertyInfo.NameAsUtf8Bytes, Options.Encoder);

            jsonPropertyInfo.DeclaredPropertyType = typeof(TProperty);
            jsonPropertyInfo.Options = Options;
            jsonPropertyInfo.RuntimeClassInfo = classInfo;
            jsonPropertyInfo.RuntimePropertyType = typeof(TProperty);

            PropertyCache!.Add(jsonPropertyInfo.NameAsString, jsonPropertyInfo);

            return jsonPropertyInfo;
        }

        /// <summary>
        /// todo
        /// </summary>
        public void CompleteInitialization()
        {
            CompleteObjectInitialization();

            //todo: should we not add?
            Options.AddJsonClassInfo(this);
        }
    }
}
