// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.Json.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Converters;
using System.Text.Json.Serialization.Metadata;
using System.Threading;

namespace System.Text.Json
{
    /// <summary>
    /// Provides options to be used with <see cref="JsonSerializer"/>.
    /// </summary>
    public sealed partial class JsonSerializerOptions
    {
        // The global list of built-in simple converters.
        private static Dictionary<Type, JsonConverter>? s_defaultSimpleConverters;

        // The global list of built-in converters that override CanConvert().
        private static JsonConverter[]? s_defaultFactoryConverters;

        // Stores the JsonTypeInfo factory, which requires unreferenced code and must be rooted by the reflection-based serializer.
        private static Func<Type, JsonSerializerOptions, JsonTypeInfo>? s_typeInfoCreationFunc;

        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        private static void RootReflectionSerializerDependencies()
        {
            // s_typeInfoCreationFunc is the last field assigned.
            // Use it as the sentinel to ensure that all dependencies are initialized.
            if (Volatile.Read(ref s_typeInfoCreationFunc) is null)
            {
                s_defaultSimpleConverters = GetDefaultSimpleConverters();
                s_defaultFactoryConverters = GetDefaultFactoryConverters();
                // Explicitly ensure that the previous fields are initialized along with this one.
                Volatile.Write(ref s_typeInfoCreationFunc, CreateJsonTypeInfo);
            }

            [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
            static JsonTypeInfo CreateJsonTypeInfo(Type type, JsonSerializerOptions options)
            {
                JsonTypeInfo.ValidateType(type, null, null, options);

                MethodInfo methodInfo = typeof(JsonSerializerOptions).GetMethod(nameof(CreateReflectionJsonTypeInfo), BindingFlags.NonPublic | BindingFlags.Instance)!;
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
        }

        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        private JsonTypeInfo<T> CreateReflectionJsonTypeInfo<T>()
        {
            return new ReflectionJsonTypeInfo<T>(this);
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

        /// <summary>
        /// The list of custom converters.
        /// </summary>
        /// <remarks>
        /// Once serialization or deserialization occurs, the list cannot be modified.
        /// </remarks>
        public IList<JsonConverter> Converters => _converters;

        /// <summary>
        /// The list of custom polymorphic type configurations.
        /// </summary>
        /// <remarks>
        /// Once serialization or deserialization occurs, the list cannot be modified.
        /// </remarks>
        public IList<JsonPolymorphicTypeConfiguration> PolymorphicTypeConfigurations => _polymorphicTypeConfigurations;

        internal JsonConverter GetConverterFromMember(Type? parentClassType, Type propertyType, MemberInfo? memberInfo)
        {
            JsonConverter converter = null!;

            // Priority 1: attempt to get converter from JsonConverterAttribute on property.
            if (memberInfo != null)
            {
                Debug.Assert(parentClassType != null);

                JsonConverterAttribute? converterAttribute = (JsonConverterAttribute?)
                    GetAttributeThatCanHaveMultiple(parentClassType!, typeof(JsonConverterAttribute), memberInfo);

                if (converterAttribute != null)
                {
                    converter = GetConverterFromAttribute(converterAttribute, typeToConvert: propertyType, classTypeAttributeIsOn: parentClassType!, memberInfo);
                }
            }

            if (converter == null)
            {
                converter = GetConverterInternal(propertyType);
                Debug.Assert(converter != null);
            }

            if (converter is JsonConverterFactory factory)
            {
                converter = factory.GetConverterInternal(propertyType, this);

                // A factory cannot return null; GetConverterInternal checked for that.
                Debug.Assert(converter != null);
            }

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

            return converter;
        }

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
        public JsonConverter GetConverter(Type typeToConvert!!)
        {
            RootReflectionSerializerDependencies();
            return GetConverterInternal(typeToConvert);
        }

        internal JsonConverter GetConverterInternal(Type typeToConvert)
        {
            // Only cache the value once (de)serialization has occurred since new converters can be added that may change the result.
            if (_cachingContext != null)
            {
                return _cachingContext.GetOrAddConverter(typeToConvert);
            }

            return GetConverterFromType(typeToConvert);
        }

        private JsonConverter GetConverterFromType(Type typeToConvert)
        {
            Debug.Assert(typeToConvert != null);

            // Priority 1: If there is a JsonSerializerContext, fetch the converter from there.
            JsonConverter? converter = _serializerContext?.GetTypeInfo(typeToConvert)?.PropertyInfoForTypeInfo?.ConverterBase;

            // Priority 2: Attempt to get custom converter added at runtime.
            // Currently there is not a way at runtime to override the [JsonConverter] when applied to a property.
            foreach (JsonConverter item in _converters)
            {
                if (item.CanConvert(typeToConvert))
                {
                    converter = item;
                    break;
                }
            }

            // Priority 3: Attempt to get converter from [JsonConverter] on the type being converted.
            if (converter == null)
            {
                JsonConverterAttribute? converterAttribute = (JsonConverterAttribute?)
                    GetAttributeThatCanHaveMultiple(typeToConvert, typeof(JsonConverterAttribute));

                if (converterAttribute != null)
                {
                    converter = GetConverterFromAttribute(converterAttribute, typeToConvert: typeToConvert, classTypeAttributeIsOn: typeToConvert, memberInfo: null);
                }
            }

            // Priority 4: Attempt to get built-in converter.
            if (converter == null)
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
                    converter = foundConverter;
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
                }
            }

            // Allow redirection for generic types or the enum converter.
            if (converter is JsonConverterFactory factory)
            {
                converter = factory.GetConverterInternal(typeToConvert, this);

                // A factory cannot return null; GetConverterInternal checked for that.
                Debug.Assert(converter != null);
            }

            Type converterTypeToConvert = converter.TypeToConvert;

            if (!converterTypeToConvert.IsAssignableFromInternal(typeToConvert)
                && !typeToConvert.IsAssignableFromInternal(converterTypeToConvert))
            {
                ThrowHelper.ThrowInvalidOperationException_SerializationConverterNotCompatible(converter.GetType(), typeToConvert);
            }

            return converter;
        }

        private JsonConverter GetConverterFromAttribute(JsonConverterAttribute converterAttribute, Type typeToConvert, Type classTypeAttributeIsOn, MemberInfo? memberInfo)
        {
            JsonConverter? converter;

            Type? type = converterAttribute.ConverterType;
            if (type == null)
            {
                // Allow the attribute to create the converter.
                converter = converterAttribute.CreateConverter(typeToConvert);
                if (converter == null)
                {
                    ThrowHelper.ThrowInvalidOperationException_SerializationConverterOnAttributeNotCompatible(classTypeAttributeIsOn, memberInfo, typeToConvert);
                }
            }
            else
            {
                ConstructorInfo? ctor = type.GetConstructor(Type.EmptyTypes);
                if (!typeof(JsonConverter).IsAssignableFrom(type) || ctor == null || !ctor.IsPublic)
                {
                    ThrowHelper.ThrowInvalidOperationException_SerializationConverterOnAttributeInvalid(classTypeAttributeIsOn, memberInfo);
                }

                converter = (JsonConverter)Activator.CreateInstance(type)!;
            }

            Debug.Assert(converter != null);
            if (!converter.CanConvert(typeToConvert))
            {
                Type? underlyingType = Nullable.GetUnderlyingType(typeToConvert);
                if (underlyingType != null && converter.CanConvert(underlyingType))
                {
                    if (converter is JsonConverterFactory converterFactory)
                    {
                        converter = converterFactory.GetConverterInternal(underlyingType, this);
                    }

                    // Allow nullable handling to forward to the underlying type's converter.
                    return NullableConverterFactory.CreateValueConverter(underlyingType, converter);
                }

                ThrowHelper.ThrowInvalidOperationException_SerializationConverterOnAttributeNotCompatible(classTypeAttributeIsOn, memberInfo, typeToConvert);
            }

            return converter;
        }

        internal bool TryGetDefaultSimpleConverter(Type typeToConvert, [NotNullWhen(true)] out JsonConverter? converter)
        {
            if (_serializerContext == null && // For consistency do not return any default converters for
                                    // options instances linked to a JsonSerializerContext,
                                    // even if the default converters might have been rooted.
                s_defaultSimpleConverters != null &&
                s_defaultSimpleConverters.TryGetValue(typeToConvert, out converter))
            {
                return true;
            }

            converter = null;
            return false;
        }

        private static Attribute? GetAttributeThatCanHaveMultiple(Type classType, Type attributeType, MemberInfo memberInfo)
        {
            object[] attributes = memberInfo.GetCustomAttributes(attributeType, inherit: false);
            return GetAttributeThatCanHaveMultiple(attributeType, classType, memberInfo, attributes);
        }

        internal static Attribute? GetAttributeThatCanHaveMultiple(Type classType, Type attributeType)
        {
            object[] attributes = classType.GetCustomAttributes(attributeType, inherit: false);
            return GetAttributeThatCanHaveMultiple(attributeType, classType, null, attributes);
        }

        private static Attribute? GetAttributeThatCanHaveMultiple(Type attributeType, Type classType, MemberInfo? memberInfo, object[] attributes)
        {
            if (attributes.Length == 0)
            {
                return null;
            }

            if (attributes.Length == 1)
            {
                return (Attribute)attributes[0];
            }

            ThrowHelper.ThrowInvalidOperationException_SerializationDuplicateAttribute(attributeType, classType, memberInfo);
            return default;
        }
    }
}
