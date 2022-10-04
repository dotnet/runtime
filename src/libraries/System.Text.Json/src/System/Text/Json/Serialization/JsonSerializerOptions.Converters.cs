// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json
{
    /// <summary>
    /// Provides options to be used with <see cref="JsonSerializer"/>.
    /// </summary>
    public sealed partial class JsonSerializerOptions
    {
        /// <summary>
        /// The list of custom converters.
        /// </summary>
        /// <remarks>
        /// Once serialization or deserialization occurs, the list cannot be modified.
        /// </remarks>
        public IList<JsonConverter> Converters => _converters;

        /// <summary>
        /// Returns the converter for the specified type.
        /// </summary>
        /// <param name="typeToConvert">The type to return a converter for.</param>
        /// <returns>
        /// The converter for the given type.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// The configured <see cref="JsonConverter"/> for <paramref name="typeToConvert"/> returned an invalid converter.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <paramref name="typeToConvert"/> or its serializable members.
        /// </exception>
        [RequiresUnreferencedCode("Getting a converter for a type may require reflection which depends on unreferenced code.")]
        [RequiresDynamicCode("Getting a converter for a type may require reflection which depends on runtime code generation.")]
        public JsonConverter GetConverter(Type typeToConvert)
        {
            if (typeToConvert is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(typeToConvert));
            }

            if (_typeInfoResolver is null)
            {
                // Backward compatibility -- root & query the default reflection converters
                // but do not populate the TypeInfoResolver setting.
                return DefaultJsonTypeInfoResolver.GetConverterForType(typeToConvert, this);
            }

            return GetConverterInternal(typeToConvert);
        }

        /// <summary>
        /// Same as GetConverter but without defaulting to reflection converters.
        /// </summary>
        internal JsonConverter GetConverterInternal(Type typeToConvert)
        {
            JsonTypeInfo jsonTypeInfo = GetTypeInfoInternal(typeToConvert, ensureConfigured: false, resolveIfMutable: true);
            return jsonTypeInfo.Converter;
        }

        internal JsonConverter? GetConverterFromList(Type typeToConvert)
        {
            foreach (JsonConverter item in _converters)
            {
                if (item.CanConvert(typeToConvert))
                {
                    return item;
                }
            }

            return null;
        }

        [return: NotNullIfNotNull(nameof(converter))]
        internal JsonConverter? ExpandConverterFactory(JsonConverter? converter, Type typeToConvert)
        {
            if (converter is JsonConverterFactory factory)
            {
                converter = factory.GetConverterInternal(typeToConvert, this);
            }

            return converter;
        }

        internal static void CheckConverterNullabilityIsSameAsPropertyType(JsonConverter converter, Type propertyType)
        {
            // User has indicated that either:
            //   a) a non-nullable-struct handling converter should handle a nullable struct type or
            //   b) a nullable-struct handling converter should handle a non-nullable struct type.
            // User should implement a custom converter for the underlying struct and remove the unnecessary CanConvert method override.
            // The serializer will automatically wrap the custom converter with NullableConverter<T>.
            //
            // We also throw to avoid passing an invalid argument to setters for nullable struct properties,
            // which would cause an InvalidProgramException when the generated IL is invoked.
            if (propertyType.IsValueType && converter.IsValueType &&
                (propertyType.IsNullableOfT() ^ converter.TypeToConvert.IsNullableOfT()))
            {
                ThrowHelper.ThrowInvalidOperationException_ConverterCanConvertMultipleTypes(propertyType, converter);
            }
        }
    }
}
