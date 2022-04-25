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
        internal static DefaultJsonTypeInfoResolver? s_reflectionTypeInfoResolver;

        [RequiresUnreferencedCode(JsonSerializer.SerializationUnreferencedCodeMessage)]
        internal JsonTypeInfo<T> CreateReflectionJsonTypeInfo<T>()
        {
            return new ReflectionJsonTypeInfo<T>(this);
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
        public JsonConverter GetConverter(Type typeToConvert)
        {
            if (typeToConvert is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(typeToConvert));
            }

            EnsureInitializedForReflectionSerializer();
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
            converter ??= DefaultJsonTypeInfoResolver.GetBuiltinConverter(typeToConvert);

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
            if (_serializerContext != null)
            {
                // For consistency do not return any default converters for
                // options instances linked to a JsonSerializerContext,
                // even if the default converters might have been rooted.
                converter = null;
                return false;
            }

            converter = DefaultJsonTypeInfoResolver.GetSimpleBuiltinConverter(typeToConvert);
            return converter != null;
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
