// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization.Converters;

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// Creates and initializes serialization metadata for a type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class JsonTypeInfoInternal<T> : JsonTypeInfo<T>
    {
        /// <summary>
        /// Creates serialization metadata given JsonSerializerOptions and a ConverterStrategy.
        /// </summary>
        public JsonTypeInfoInternal(JsonSerializerOptions options, ConverterStrategy converterStrategy)
            : base(typeof(T), options, converterStrategy)
        {
        }

        /// <summary>
        /// Creates serialization metadata for an object.
        /// </summary>
        public JsonTypeInfoInternal(
            JsonSerializerOptions options,
            Func<T>? createObjectFunc,
            Func<JsonSerializerContext, JsonPropertyInfo[]>? propInitFunc,
            JsonNumberHandling numberHandling,
            Action<Utf8JsonWriter, T>? serializeFunc
            ) : base(typeof(T), options, ConverterStrategy.Object)
        {
            if (propInitFunc == null && serializeFunc == null)
            {
                ThrowHelper.ThrowInvalidOperationException_PropInitAndSerializeFuncsNull();
            }

#pragma warning disable CS8714
            // The type cannot be used as type parameter in the generic type or method.
            // Nullability of type argument doesn't match 'notnull' constraint.
            JsonConverter converter = new JsonMetadataServicesConverter<T>(() => new ObjectDefaultConverter<T>(), ConverterStrategy.Object, keyType: null, elementType: null);
#pragma warning restore CS8714

            PropertyInfoForTypeInfo = JsonMetadataServices.CreateJsonPropertyInfoForClassInfo(typeof(T), this, converter, Options);
            NumberHandling = numberHandling;
            PropInitFunc = propInitFunc;
            Serialize = serializeFunc;
            SetCreateObjectFunc(createObjectFunc);
        }

        /// <summary>
        /// Creates serialization metadata for a <see cref="ConverterStrategy.Enumerable"/>.
        /// </summary>
        public JsonTypeInfoInternal(
            JsonSerializerOptions options,
            Func<T>? createObjectFunc,
            Func<JsonConverter<T>> converterCreator,
            JsonTypeInfo? elementInfo,
            JsonNumberHandling numberHandling,
            Action<Utf8JsonWriter, T>? serializeFunc,
            Type elementType) : base(typeof(T), options, ConverterStrategy.Enumerable)
        {
            JsonConverter<T> converter = new JsonMetadataServicesConverter<T>(converterCreator, ConverterStrategy.Enumerable, keyType: null, elementType);

            ElementType = converter.ElementType;
            ElementTypeInfo = elementInfo ?? throw new ArgumentNullException(nameof(elementInfo));
            NumberHandling = numberHandling;
            PropertyInfoForTypeInfo = JsonMetadataServices.CreateJsonPropertyInfoForClassInfo(typeof(T), this, converter, options);
            Serialize = serializeFunc;
            SetCreateObjectFunc(createObjectFunc);
        }

        /// <summary>
        /// Creates serialization metadata for a <see cref="ConverterStrategy.Dictionary"/>.
        /// </summary>
        public JsonTypeInfoInternal(
            JsonSerializerOptions options,
            Func<T>? createObjectFunc,
            Func<JsonConverter<T>> converterCreator,
            JsonTypeInfo? keyInfo,
            JsonTypeInfo? valueInfo,
            JsonNumberHandling numberHandling,
            Action<Utf8JsonWriter, T>? serializeFunc,
            Type keyType,
            Type elementType) : base(typeof(T), options, ConverterStrategy.Dictionary)
        {
            JsonConverter<T> converter = new JsonMetadataServicesConverter<T>(converterCreator, ConverterStrategy.Dictionary, keyType, elementType);

            KeyType = converter.KeyType;
            ElementType = converter.ElementType;
            KeyTypeInfo = keyInfo ?? throw new ArgumentNullException(nameof(keyInfo));
            ElementType = converter.ElementType;
            ElementTypeInfo = valueInfo ?? throw new ArgumentNullException(nameof(valueInfo));
            NumberHandling = numberHandling;
            PropertyInfoForTypeInfo = JsonMetadataServices.CreateJsonPropertyInfoForClassInfo(typeof(T), this, converter, options);
            Serialize = serializeFunc;
            SetCreateObjectFunc(createObjectFunc);
        }

        private void SetCreateObjectFunc(Func<T>? createObjectFunc)
        {
            if (createObjectFunc != null)
            {
                CreateObject = () => createObjectFunc();
            }
        }
    }
}
