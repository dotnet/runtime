// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Converters;

namespace System.Text.Json.Serialization.Metadata
{
    public partial class DefaultJsonTypeInfoResolver
    {
        private static Dictionary<Type, JsonConverter>? s_defaultSimpleConverters;
        private static JsonConverterFactory[]? s_defaultFactoryConverters;

        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
        private static JsonConverterFactory[] GetDefaultFactoryConverters()
        {
            return new JsonConverterFactory[]
            {
                // Check for disallowed types.
                new UnsupportedTypeConverterFactory(),
                // Nullable converter should always be next since it forwards to any nullable type.
                new NullableConverterFactory(),
                new EnumConverterFactory(),
                new JsonNodeConverterFactory(),
                new FSharpTypeConverterFactory(),
                // IAsyncEnumerable takes precedence over IEnumerable.
                new IAsyncEnumerableConverterFactory(),
                // IEnumerable should always be second to last since they can convert any IEnumerable.
                new IEnumerableConverterFactory(),
                // Object should always be last since it converts any type.
                new ObjectConverterFactory()
            };
        }

        private static Dictionary<Type, JsonConverter> GetDefaultSimpleConverters()
        {
            const int NumberOfSimpleConverters = 26;
            var converters = new Dictionary<Type, JsonConverter>(NumberOfSimpleConverters);

            // Use a dictionary for simple converters.
            // When adding to this, update NumberOfSimpleConverters above.
            Add(JsonMetadataServices.BooleanConverter);
            Add(JsonMetadataServices.ByteConverter);
            Add(JsonMetadataServices.ByteArrayConverter);
            Add(JsonMetadataServices.CharConverter);
            Add(JsonMetadataServices.DateTimeConverter);
            Add(JsonMetadataServices.DateTimeOffsetConverter);
#if NETCOREAPP
            Add(JsonMetadataServices.DateOnlyConverter);
            Add(JsonMetadataServices.TimeOnlyConverter);
#endif
            Add(JsonMetadataServices.DoubleConverter);
            Add(JsonMetadataServices.DecimalConverter);
            Add(JsonMetadataServices.GuidConverter);
            Add(JsonMetadataServices.Int16Converter);
            Add(JsonMetadataServices.Int32Converter);
            Add(JsonMetadataServices.Int64Converter);
            Add(JsonMetadataServices.JsonElementConverter);
            Add(JsonMetadataServices.JsonDocumentConverter);
            Add(JsonMetadataServices.ObjectConverter);
            Add(JsonMetadataServices.SByteConverter);
            Add(JsonMetadataServices.SingleConverter);
            Add(JsonMetadataServices.StringConverter);
            Add(JsonMetadataServices.TimeSpanConverter);
            Add(JsonMetadataServices.UInt16Converter);
            Add(JsonMetadataServices.UInt32Converter);
            Add(JsonMetadataServices.UInt64Converter);
            Add(JsonMetadataServices.UriConverter);
            Add(JsonMetadataServices.VersionConverter);

            Debug.Assert(converters.Count <= NumberOfSimpleConverters);

            return converters;

            void Add(JsonConverter converter) =>
                converters.Add(converter.TypeToConvert, converter);
        }

        internal static JsonConverter GetDefaultConverter(Type typeToConvert)
        {
            if (s_defaultSimpleConverters == null || s_defaultFactoryConverters == null)
            {
                // (De)serialization using serializer's options-based methods has not yet occurred, so the built-in converters are not rooted.
                // Even though source-gen code paths do not call this method <i.e. JsonSerializerOptions.GetConverter(Type)>, we do not root all the
                // built-in converters here since we fetch converters for any type included for source generation from the binded context (Priority 1).
                ThrowHelper.ThrowNotSupportedException_BuiltInConvertersNotRooted(typeToConvert);
                return null!;
            }

            JsonConverter? converter;
            if (s_defaultSimpleConverters.TryGetValue(typeToConvert, out converter))
            {
                return converter;
            }
            else
            {
                foreach (JsonConverter item in s_defaultFactoryConverters)
                {
                    if (item.CanConvert(typeToConvert))
                    {
                        converter = item;
                        break;
                    }
                }

                // Since the object and IEnumerable converters cover all types, we should have a converter.
                Debug.Assert(converter != null);
                return converter;
            }
        }

        internal static bool TryGetDefaultSimpleConverter(Type typeToConvert, [NotNullWhen(true)] out JsonConverter? converter)
        {
            if (s_defaultSimpleConverters is null)
            {
                converter = null;
                return false;
            }

            return s_defaultSimpleConverters.TryGetValue(typeToConvert, out converter);
        }
    }
}
