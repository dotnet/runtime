// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// Provides helpers to create and initialize metadata for JSON-serializable types.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static partial class JsonMetadataServices
    {
        /// <summary>
        /// Creates metadata for a property or field.
        /// </summary>
        /// <typeparam name="T">The type that the converter for the property returns or accepts when converting JSON data.</typeparam>
        /// <param name="options">The <see cref="JsonSerializerOptions"/> to initialize the metadata with.</param>
        /// <param name="isProperty">Whether the CLR member is a property or field.</param>
        /// <param name="isPublic">Whether the CLR member is public.</param>
        /// <param name="isVirtual">Whether the CLR member is a virtual property.</param>
        /// <param name="declaringType">The declaring type of the property or field.</param>
        /// <param name="propertyTypeInfo">The <see cref="JsonTypeInfo"/> info for the property or field's type.</param>
        /// <param name="converter">A <see cref="JsonConverter"/> for the property or field, specified by <see cref="JsonConverterAttribute"/>.</param>
        /// <param name="getter">Provides a mechanism to get the property or field's value.</param>
        /// <param name="setter">Provides a mechanism to set the property or field's value.</param>
        /// <param name="ignoreCondition">Specifies a condition for the property to be ignored.</param>
        /// <param name="numberHandling">If the property or field is a number, specifies how it should processed when serializing and deserializing.</param>
        /// <param name="hasJsonInclude">Whether the property was annotated with <see cref="JsonIncludeAttribute"/>.</param>
        /// <param name="propertyName">The CLR name of the property or field.</param>
        /// <param name="jsonPropertyName">The name to be used when processing the property or field, specified by <see cref="JsonPropertyNameAttribute"/>.</param>
        /// <returns>A <see cref="JsonPropertyInfo"/> instance intialized with the provided metadata.</returns>
        public static JsonPropertyInfo CreatePropertyInfo<T>(
            JsonSerializerOptions options,
            bool isProperty,
            bool isPublic,
            bool isVirtual,
            Type declaringType,
            JsonTypeInfo propertyTypeInfo,
            JsonConverter<T>? converter,
            Func<object, T>? getter,
            Action<object, T>? setter,
            JsonIgnoreCondition? ignoreCondition,
            bool hasJsonInclude,
            JsonNumberHandling? numberHandling,
            string propertyName,
            string? jsonPropertyName)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (declaringType == null)
            {
                throw new ArgumentNullException(nameof(declaringType));
            }

            if (propertyTypeInfo == null)
            {
                throw new ArgumentNullException(nameof(propertyTypeInfo));
            }

            if (propertyName == null)
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            if (converter == null)
            {
                converter = propertyTypeInfo.PropertyInfoForTypeInfo.ConverterBase as JsonConverter<T>;
                if (converter == null)
                {
                    throw new InvalidOperationException(SR.Format(SR.ConverterForPropertyMustBeValid, declaringType, propertyName, typeof(T)));
                }
            }

            if (!isProperty && isVirtual)
            {
                throw new InvalidOperationException(SR.Format(SR.FieldCannotBeVirtual, nameof(isProperty), nameof(isVirtual)));
            }

            JsonPropertyInfo<T> jsonPropertyInfo = new JsonPropertyInfo<T>();
            jsonPropertyInfo.InitializeForSourceGen(
                options,
                isProperty,
                isPublic,
                declaringType,
                propertyTypeInfo,
                converter,
                getter,
                setter,
                ignoreCondition,
                hasJsonInclude,
                numberHandling,
                propertyName,
                jsonPropertyName);

            return jsonPropertyInfo;
        }

        /// <summary>
        /// Creates metadata for a complex class or struct.
        /// </summary>
        /// <param name="options">The <see cref="JsonSerializerOptions"/> to initialize the metadata with.</param>
        /// <param name="createObjectFunc">Provides a mechanism to create an instance of the class or struct when deserializing.</param>
        /// <param name="propInitFunc">Provides a mechanism to initialize metadata for properties and fields of the class or struct.</param>
        /// <param name="serializeFunc">Provides a serialization implementation for instances of the class or struct which assumes options specified by <see cref="JsonSourceGenerationOptionsAttribute"/>.</param>
        /// <param name="numberHandling">Specifies how number properties and fields should be processed when serializing and deserializing.</param>
        /// <typeparam name="T">The type of the class or struct.</typeparam>
        /// <exception cref="InvalidOperationException">Thrown when <paramref name="options"/> and <paramref name="propInitFunc"/> are both null.</exception>
        /// <returns>A <see cref="JsonTypeInfo{T}"/> instance representing the class or struct.</returns>
        public static JsonTypeInfo<T> CreateObjectInfo<T>(
            JsonSerializerOptions options,
            Func<T>? createObjectFunc,
            Func<JsonSerializerContext, JsonPropertyInfo[]>? propInitFunc,
            JsonNumberHandling numberHandling,
            Action<Utf8JsonWriter, T>? serializeFunc) where T : notnull
            => new JsonTypeInfoInternal<T>(options, createObjectFunc, propInitFunc, numberHandling, serializeFunc);

        /// <summary>
        /// Creates metadata for a primitive or a type with a custom converter.
        /// </summary>
        /// <typeparam name="T">The generic type definition.</typeparam>
        /// <returns>A <see cref="JsonTypeInfo{T}"/> instance representing the type.</returns>
        public static JsonTypeInfo<T> CreateValueInfo<T>(JsonSerializerOptions options, JsonConverter converter)
        {
            JsonTypeInfo<T> info = new JsonTypeInfoInternal<T>(options, ConverterStrategy.Value);
            info.PropertyInfoForTypeInfo = CreateJsonPropertyInfoForClassInfo(typeof(T), info, converter, options);
            return info;
        }

        internal static JsonPropertyInfo CreateJsonPropertyInfoForClassInfo(
            Type type,
            JsonTypeInfo typeInfo,
            JsonConverter converter,
            JsonSerializerOptions options)
        {
            JsonPropertyInfo propertyInfo = converter.CreateJsonPropertyInfo();
            propertyInfo.InitializeForTypeInfo(type, typeInfo, converter, options);
            return propertyInfo;
        }
    }
}
