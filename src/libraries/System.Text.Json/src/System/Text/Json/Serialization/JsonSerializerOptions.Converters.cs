// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
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

        // This may return factory converter
        internal JsonConverter? GetCustomConverterFromMember(Type? parentClassType, Type typeToConvert, MemberInfo? memberInfo)
        {
            JsonConverter? converter = null;

            if (memberInfo != null)
            {
                Debug.Assert(parentClassType != null);

                JsonConverterAttribute? converterAttribute = (JsonConverterAttribute?)
                    GetAttributeThatCanHaveMultiple(parentClassType!, typeof(JsonConverterAttribute), memberInfo);

                if (converterAttribute != null)
                {
                    converter = GetConverterFromAttribute(converterAttribute, typeToConvert, classTypeAttributeIsOn: parentClassType!, memberInfo);
                }
            }

            return converter;
        }

        /// <summary>
        /// Gets converter for type but does not use TypeInfoResolver
        /// </summary>
        internal JsonConverter GetConverterForType(Type typeToConvert)
        {
            JsonConverter converter = GetConverterFromOptionsOrReflectionConverter(typeToConvert);
            Debug.Assert(converter != null);

            converter = ExpandFactoryConverter(converter, typeToConvert);

            CheckConverterNullabilityIsSameAsPropertyType(converter, typeToConvert);

            return converter;
        }

        [return: NotNullIfNotNull("converter")]
        internal JsonConverter? ExpandFactoryConverter(JsonConverter? converter, Type typeToConvert)
        {
            if (converter is JsonConverterFactory factory)
            {
                converter = factory.GetConverterInternal(typeToConvert, this);

                // A factory cannot return null; GetConverterInternal checked for that.
                Debug.Assert(converter != null);
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

            DefaultJsonTypeInfoResolver.RootDefaultInstance();
            return GetConverterFromTypeInfo(typeToConvert);
        }

        /// <summary>
        /// Same as GetConverter but does not root converters
        /// </summary>
        internal JsonConverter GetConverterFromTypeInfo(Type typeToConvert)
        {
            if (_cachingContext == null)
            {
                if (_isLockedInstance)
                {
                    InitializeCachingContext();
                }
                else
                {
                    // We do not want to lock options instance here but we need to return correct answer
                    // which means we need to go through TypeInfoResolver but without caching because that's the
                    // only place which will have correct converter for JsonSerializerContext and reflection
                    // based resolver. It will also work correctly for combined resolvers.
                    return GetTypeInfoInternal(typeToConvert)?.Converter
                        ?? GetConverterFromOptionsOrReflectionConverter(typeToConvert);

                }
            }

            JsonConverter? converter = _cachingContext.GetOrAddJsonTypeInfo(typeToConvert)?.Converter;

            // we can get here if resolver returned null but converter was added for the type
            converter ??= GetConverterFromOptions(typeToConvert);

            if (converter == null)
            {
                ThrowHelper.ThrowNotSupportedException_BuiltInConvertersNotRooted(typeToConvert);
                return null!;
            }

            return converter;
        }

        private JsonConverter? GetConverterFromOptions(Type typeToConvert)
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

        private JsonConverter GetConverterFromOptionsOrReflectionConverter(Type typeToConvert)
        {
            Debug.Assert(typeToConvert != null);

            // Priority 1: Attempt to get custom converter from the Converters list.
            JsonConverter? converter = GetConverterFromOptions(typeToConvert);

            // Priority 2: Attempt to get converter from [JsonConverter] on the type being converted.
            if (converter == null)
            {
                JsonConverterAttribute? converterAttribute = (JsonConverterAttribute?)
                    GetAttributeThatCanHaveMultiple(typeToConvert, typeof(JsonConverterAttribute));

                if (converterAttribute != null)
                {
                    converter = GetConverterFromAttribute(converterAttribute, typeToConvert: typeToConvert, classTypeAttributeIsOn: typeToConvert, memberInfo: null);
                }
            }

            // Priority 3: Attempt to get built-in converter.
            if (converter == null)
            {
                converter = DefaultJsonTypeInfoResolver.GetDefaultConverter(typeToConvert);
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

        // This suppression needs to be removed. https://github.com/dotnet/runtime/issues/68878
        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode", Justification = "The factory constructors are only invoked in the context of reflection serialization code paths " +
            "and are marked RequiresDynamicCode")]
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
