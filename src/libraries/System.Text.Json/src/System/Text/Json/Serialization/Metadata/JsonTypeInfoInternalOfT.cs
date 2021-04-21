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
        /// Creates serialization metadata for a <see cref="ConverterStrategy.Object"/>.
        /// </summary>
        public JsonTypeInfoInternal() : base(typeof(T), null!, ConverterStrategy.Object)
        {
        }

        /// <summary>
        /// Creates serialization metadata for a <see cref="ConverterStrategy.Value"/>.
        /// </summary>
        public JsonTypeInfoInternal(JsonSerializerOptions options)
            : base (typeof(T), options, ConverterStrategy.Value)
        {
        }

        /// <summary>
        /// Creates serialization metadata for a <see cref="ConverterStrategy.Enumerable"/>.
        /// </summary>
        public JsonTypeInfoInternal(
            JsonSerializerOptions options,
            Func<T>? createObjectFunc,
            JsonConverter<T> converter,
            JsonTypeInfo elementInfo,
            JsonNumberHandling numberHandling) : base(typeof(T), options, ConverterStrategy.Enumerable)
        {
            ElementType = converter.ElementType;
            ElementTypeInfo = elementInfo ?? throw new ArgumentNullException(nameof(elementInfo));
            NumberHandling = numberHandling;
            PropertyInfoForTypeInfo = JsonMetadataServices.CreateJsonPropertyInfoForClassInfo(typeof(T), this, converter, options);
            SetCreateObjectFunc(createObjectFunc);
        }

        /// <summary>
        /// Creates serialization metadata for a <see cref="ConverterStrategy.Dictionary"/>.
        /// </summary>
        public JsonTypeInfoInternal(
            JsonSerializerOptions options,
            Func<T>? createObjectFunc,
            JsonConverter<T> converter,
            JsonTypeInfo keyInfo,
            JsonTypeInfo valueInfo,
            JsonNumberHandling numberHandling) : base(typeof(T), options, ConverterStrategy.Dictionary)
        {
            KeyType = converter.KeyType;
            KeyTypeInfo = keyInfo ?? throw new ArgumentNullException(nameof(keyInfo)); ;
            ElementType = converter.ElementType;
            ElementTypeInfo = valueInfo ?? throw new ArgumentNullException(nameof(valueInfo));
            NumberHandling = numberHandling;
            PropertyInfoForTypeInfo = JsonMetadataServices.CreateJsonPropertyInfoForClassInfo(typeof(T), this, converter, options);
            SetCreateObjectFunc(createObjectFunc);
        }

        /// <summary>
        /// Initializes serialization metadata for a <see cref="ConverterStrategy.Object"/>.
        /// </summary>
        public void InitializeAsObject(
            JsonSerializerOptions options,
            Func<T>? createObjectFunc,
            Func<JsonSerializerContext, JsonPropertyInfo[]> propInitFunc,
            JsonNumberHandling numberHandling)
        {
            Options = options;

#pragma warning disable CS8714
            // The type cannot be used as type parameter in the generic type or method.
            // Nullability of type argument doesn't match 'notnull' constraint.
            JsonConverter converter = new ObjectSourceGenConverter<T>();
#pragma warning restore CS8714

            PropertyInfoForTypeInfo = JsonMetadataServices.CreateJsonPropertyInfoForClassInfo(typeof(T), this, converter, options);
            NumberHandling = numberHandling;
            PropInitFunc = propInitFunc;
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
