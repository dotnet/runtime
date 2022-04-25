// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.Json.Serialization.Converters;
using System.Threading;

namespace System.Text.Json.Serialization.Metadata
{
    internal sealed class DefaultJsonTypeInfoResolver
    {
        // The global list of built-in simple converters.
        private static Dictionary<Type, JsonConverter>? s_defaultSimpleConverters;

        // The global list of built-in converters that override CanConvert().
        private static JsonConverter[]? s_defaultFactoryConverters;

        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        public DefaultJsonTypeInfoResolver()
        {
            RootReflectionSerializerDependencies();
        }

        internal static JsonConverter GetBuiltinConverter(Type typeToConvert)
        {
            if (s_defaultSimpleConverters == null || s_defaultFactoryConverters == null)
            {
                // (De)serialization using serializer's options-based methods has not yet occurred, so the built-in converters are not rooted.
                // Even though source-gen code paths do not call this method <i.e. JsonSerializerOptions.GetConverter(Type)>, we do not root all the
                // built-in converters here since we fetch converters for any type included for source generation from the binded context (Priority 1).
                Debug.Assert(s_defaultSimpleConverters == null);
                Debug.Assert(s_defaultFactoryConverters == null);
                ThrowHelper.ThrowNotSupportedException_BuiltInConvertersNotRooted(typeToConvert);
                return null!;
            }

            if (s_defaultSimpleConverters.TryGetValue(typeToConvert, out JsonConverter? foundConverter))
            {
                return foundConverter;
            }
            else
            {
                foreach (JsonConverter item in s_defaultFactoryConverters)
                {
                    if (item.CanConvert(typeToConvert))
                    {
                        return item;
                    }
                }

                Debug.Fail("Since the object and IEnumerable converters cover all types, we should have a converter.");
                return null!;
            }
        }

        internal static JsonConverter? GetSimpleBuiltinConverter(Type typeToConvert)
        {
            JsonConverter? converter = null;
            s_defaultSimpleConverters?.TryGetValue(typeToConvert, out converter);
            return converter;
        }

        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        private static void RootReflectionSerializerDependencies()
        {
            // s_defaultFactoryConverters is the last field assigned.
            // Use it as the sentinel to ensure that all dependencies are initialized.
            if (s_defaultFactoryConverters == null)
            {
                s_defaultSimpleConverters = GetDefaultSimpleConverters();
                s_defaultFactoryConverters = GetDefaultFactoryConverters();
            }
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "The ctor is marked with RequiresUnreferencedCode.")]
        internal JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            JsonTypeInfo.ValidateType(type, null, null, options);

            MethodInfo methodInfo = typeof(JsonSerializerOptions).GetMethod(nameof(JsonSerializerOptions.CreateReflectionJsonTypeInfo), BindingFlags.NonPublic | BindingFlags.Instance)!;
#if NETCOREAPP
            return (JsonTypeInfo)methodInfo.MakeGenericMethod(type).Invoke(options, BindingFlags.NonPublic | BindingFlags.DoNotWrapExceptions, null, null, null)!;
#else
            try
            {
                return (JsonTypeInfo)methodInfo.MakeGenericMethod(type).Invoke(options, null)!;
            }
            catch (TargetInvocationException ex)
            {
                // Some of the validation is done during construction (i.e. validity of JsonConverter, inner types etc.)
                // therefore we need to unwrap TargetInvocationException for better user experience
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw null!;
            }
#endif
        }

        private static Dictionary<Type, JsonConverter> GetDefaultSimpleConverters()
        {
            const int NumberOfSimpleConverters = 24;
            var converters = new Dictionary<Type, JsonConverter>(NumberOfSimpleConverters);

            // Use a dictionary for simple converters.
            // When adding to this, update NumberOfSimpleConverters above.
            Add(JsonMetadataServices.BooleanConverter);
            Add(JsonMetadataServices.ByteConverter);
            Add(JsonMetadataServices.ByteArrayConverter);
            Add(JsonMetadataServices.CharConverter);
            Add(JsonMetadataServices.DateTimeConverter);
            Add(JsonMetadataServices.DateTimeOffsetConverter);
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

            Debug.Assert(NumberOfSimpleConverters == converters.Count);

            return converters;

            void Add(JsonConverter converter) =>
                converters.Add(converter.TypeToConvert, converter);
        }

        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        private static JsonConverter[] GetDefaultFactoryConverters()
        {
            return new JsonConverter[]
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
    }
}
