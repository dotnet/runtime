// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
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
            Func<JsonConverter<T>> converterCreator,
            JsonTypeInfo? elementInfo,
            JsonNumberHandling numberHandling,
            Action<Utf8JsonWriter, T>? serializeFunc,
            Type elementType) : base(typeof(T), options, ConverterStrategy.Enumerable)
        {
            if (serializeFunc != null)
            {
                Serialize = serializeFunc;
                DetermineIfCanUseSerializeFastPath();
            }

            JsonConverter<T> converter = new SourceGenConverter<T>(converterCreator, ConverterStrategy.Enumerable, keyType: null, elementType);

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
            Func<JsonConverter<T>> converterCreator,
            JsonTypeInfo? keyInfo,
            JsonTypeInfo? valueInfo,
            JsonNumberHandling numberHandling,
            Action<Utf8JsonWriter, T>? serializeFunc,
            Type keyType,
            Type elementType) : base(typeof(T), options, ConverterStrategy.Dictionary)
        {
            if (serializeFunc != null)
            {
                Serialize = serializeFunc;
                DetermineIfCanUseSerializeFastPath();
            }

            JsonConverter<T> converter = new SourceGenConverter<T>(converterCreator, ConverterStrategy.Dictionary, keyType, elementType);

            KeyType = converter.KeyType;
            ElementType = converter.ElementType;
            KeyTypeInfo = keyInfo ?? throw new ArgumentNullException(nameof(keyInfo)); ;
            ElementType = converter.ElementType;
            ElementTypeInfo = valueInfo ?? throw new ArgumentNullException(nameof(valueInfo)); ;
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
            Func<JsonSerializerContext, JsonPropertyInfo[]>? propInitFunc,
            JsonNumberHandling numberHandling,
            Action<Utf8JsonWriter, T>? serializeFunc)
        {
            Options = options;

            if (serializeFunc != null)
            {
                Serialize = serializeFunc;
                DetermineIfCanUseSerializeFastPath();
            }

#pragma warning disable CS8714
            // The type cannot be used as type parameter in the generic type or method.
            // Nullability of type argument doesn't match 'notnull' constraint.
            JsonConverter converter = new SourceGenConverter<T>(() => new ObjectDefaultConverter<T>(), ConverterStrategy.Object, keyType: null, elementType: null);
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

        private void DetermineIfCanUseSerializeFastPath()
        {
            Debug.Assert(Options == Options._context!.Options);
            JsonSerializerOptions? contextDefaultOptions = Options._context!.DefaultOptions;
            if (contextDefaultOptions == null)
            {
                throw new InvalidOperationException($"To specify a fast-path implementation for type {typeof(T)}, context {Options._context.GetType()} must specify default options.");
            }

            UseFastPathOnWrite =
                // Guard against unsupported features
                Options.Encoder == null &&
                Options.NumberHandling == JsonNumberHandling.Strict &&
                Options.ReferenceHandlingStrategy == ReferenceHandlingStrategy.None &&
                // Ensure options values are consistent with expected defaults.
                Options.DefaultIgnoreCondition == contextDefaultOptions.DefaultIgnoreCondition &&
                Options.IgnoreReadOnlyFields == contextDefaultOptions.IgnoreReadOnlyFields &&
                Options.IgnoreReadOnlyProperties == contextDefaultOptions.IgnoreReadOnlyProperties &&
                Options.IncludeFields == contextDefaultOptions.IncludeFields &&
                Options.PropertyNamingPolicy == contextDefaultOptions.PropertyNamingPolicy &&
                Options.WriteIndented == contextDefaultOptions.WriteIndented;
        }
    }
}
