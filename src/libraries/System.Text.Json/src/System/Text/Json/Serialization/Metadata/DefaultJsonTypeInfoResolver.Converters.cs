// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Reflection;
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

        private static JsonConverter GetBuiltInConverter(Type typeToConvert)
        {
            Debug.Assert(s_defaultSimpleConverters != null);
            Debug.Assert(s_defaultFactoryConverters != null);

            JsonConverter? converter;
            if (s_defaultSimpleConverters.TryGetValue(typeToConvert, out converter))
            {
                return converter;
            }
            else
            {
                foreach (JsonConverterFactory factory in s_defaultFactoryConverters)
                {
                    if (factory.CanConvert(typeToConvert))
                    {
                        converter = factory;
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

        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
        internal static JsonConverter? GetCustomConverterForMember(Type typeToConvert, MemberInfo memberInfo, JsonSerializerOptions options)
        {
            Debug.Assert(memberInfo is FieldInfo or PropertyInfo);
            Debug.Assert(typeToConvert != null);

            JsonConverterAttribute? converterAttribute = memberInfo.GetUniqueCustomAttribute<JsonConverterAttribute>(inherit: false);
            return converterAttribute is null ? null : GetConverterFromAttribute(converterAttribute, typeToConvert, memberInfo, options);
        }

        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
        internal static JsonConverter GetConverterForType(Type typeToConvert, JsonSerializerOptions options, bool resolveJsonConverterAttribute = true)
        {
            RootDefaultInstance(); // Ensure default converters are rooted.

            // Priority 1: Attempt to get custom converter from the Converters list.
            JsonConverter? converter = options.GetConverterFromList(typeToConvert);

            // Priority 2: Attempt to get converter from [JsonConverter] on the type being converted.
            if (resolveJsonConverterAttribute && converter == null)
            {
                JsonConverterAttribute? converterAttribute = typeToConvert.GetUniqueCustomAttribute<JsonConverterAttribute>(inherit: false);
                if (converterAttribute != null)
                {
                    converter = GetConverterFromAttribute(converterAttribute, typeToConvert: typeToConvert, memberInfo: null, options);
                }
            }

            // Priority 3: Query the built-in converters.
            converter ??= GetBuiltInConverter(typeToConvert);

            // Expand if factory converter & validate.
            converter = options.ExpandConverterFactory(converter, typeToConvert);
            if (!converter.TypeToConvert.IsInSubtypeRelationshipWith(typeToConvert))
            {
                ThrowHelper.ThrowInvalidOperationException_SerializationConverterNotCompatible(converter.GetType(), typeToConvert);
            }

            JsonSerializerOptions.CheckConverterNullabilityIsSameAsPropertyType(converter, typeToConvert);
            return converter;
        }

        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(JsonSerializer.SerializationRequiresDynamicCodeMessage)]
        private static JsonConverter GetConverterFromAttribute(JsonConverterAttribute converterAttribute, Type typeToConvert, MemberInfo? memberInfo, JsonSerializerOptions options)
        {
            JsonConverter? converter;

            Type declaringType = memberInfo?.DeclaringType ?? typeToConvert;
            Type? converterType = converterAttribute.ConverterType;
            if (converterType == null)
            {
                // Allow the attribute to create the converter.
                converter = converterAttribute.CreateConverter(typeToConvert);
                if (converter == null)
                {
                    ThrowHelper.ThrowInvalidOperationException_SerializationConverterOnAttributeNotCompatible(declaringType, memberInfo, typeToConvert);
                }
            }
            else
            {
                ConstructorInfo? ctor = converterType.GetConstructor(Type.EmptyTypes);
                if (!typeof(JsonConverter).IsAssignableFrom(converterType) || ctor == null || !ctor.IsPublic)
                {
                    ThrowHelper.ThrowInvalidOperationException_SerializationConverterOnAttributeInvalid(declaringType, memberInfo);
                }

                converter = (JsonConverter)Activator.CreateInstance(converterType)!;
            }

            Debug.Assert(converter != null);
            if (!converter.CanConvert(typeToConvert))
            {
                Type? underlyingType = Nullable.GetUnderlyingType(typeToConvert);
                if (underlyingType != null && converter.CanConvert(underlyingType))
                {
                    if (converter is JsonConverterFactory converterFactory)
                    {
                        converter = converterFactory.GetConverterInternal(underlyingType, options);
                    }

                    // Allow nullable handling to forward to the underlying type's converter.
                    return NullableConverterFactory.CreateValueConverter(underlyingType, converter);
                }

                ThrowHelper.ThrowInvalidOperationException_SerializationConverterOnAttributeNotCompatible(declaringType, memberInfo, typeToConvert);
            }

            return converter;
        }
    }
}
