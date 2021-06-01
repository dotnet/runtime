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
        /// <returns>A <see cref="JsonPropertyInfo"/> instance intialized with the provided metadata.</returns>
        public static JsonPropertyInfo CreatePropertyInfo<T>(
            JsonSerializerOptions options,
            bool isProperty,
            Type declaringType,
            JsonTypeInfo propertyTypeInfo,
            JsonConverter<T>? converter,
            Func<object, T>? getter,
            Action<object, T>? setter,
            JsonIgnoreCondition ignoreCondition,
            JsonNumberHandling numberHandling,
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
                throw new ArgumentNullException(propertyName);
            }

            if (converter == null)
            {
                converter = propertyTypeInfo.PropertyInfoForTypeInfo.ConverterBase as JsonConverter<T>;
                if (converter == null)
                {
                    throw new InvalidOperationException(SR.Format(SR.ConverterForPropertyMustBeValid, declaringType, propertyName, typeof(T)));
                }
            }

            JsonPropertyInfo<T> jsonPropertyInfo = new JsonPropertyInfo<T>();
            jsonPropertyInfo.InitializeForSourceGen(
                options,
                isProperty,
                declaringType,
                propertyTypeInfo,
                converter,
                getter,
                setter,
                ignoreCondition,
                numberHandling,
                propertyName,
                jsonPropertyName);

            return jsonPropertyInfo;
        }

        /// <summary>
        /// Creates metadata for a complex class or struct.
        /// </summary>
        /// <typeparam name="T">The type of the class or struct.</typeparam>
        /// <returns>A <see cref="JsonTypeInfo{T}"/> instance representing the class or struct.</returns>
        public static JsonTypeInfo<T> CreateObjectInfo<T>() where T : notnull => new JsonTypeInfoInternal<T>();

        /// <summary>
        /// Initializes metadata for a class or struct.
        /// </summary>
        /// <typeparam name="T">The type of the class or struct</typeparam>
        /// <param name="info"></param>
        /// <param name="options"></param>
        /// <param name="createObjectFunc"></param>
        /// <param name="propInitFunc"></param>
        /// <param name="numberHandling"></param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/>, <paramref name="info"/>, or <paramref name="propInitFunc"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="info"/>, does not represent a complex class or struct type.</exception>
        public static void InitializeObjectInfo<T>(
            JsonTypeInfo<T> info,
            JsonSerializerOptions options,
            Func<T>? createObjectFunc,
            Func<JsonSerializerContext, JsonPropertyInfo[]> propInitFunc,
            JsonNumberHandling numberHandling)
            where T : notnull
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            if (info.PropertyInfoForTypeInfo != null)
            {
                // ConverterStrategy.Object is the only info type we won't have set PropertyInfoForTypeInfo for at this point.
                throw new ArgumentException(SR.InitializeTypeInfoAsObjectInvalid, nameof(info));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (propInitFunc == null)
            {
                throw new ArgumentNullException(nameof(propInitFunc));
            }

            ((JsonTypeInfoInternal<T>)info).InitializeAsObject(options, createObjectFunc, propInitFunc, numberHandling);
            Debug.Assert(info.PropertyInfoForTypeInfo!.ConverterStrategy == ConverterStrategy.Object);
        }

        /// <summary>
        /// Creates metadata for a primitive or a type with a custom converter.
        /// </summary>
        /// <typeparam name="T">The generic type definition.</typeparam>
        /// <returns>A <see cref="JsonTypeInfo{T}"/> instance representing the type.</returns>
        public static JsonTypeInfo<T> CreateValueInfo<T>(JsonSerializerOptions options, JsonConverter converter)
        {
            JsonTypeInfo<T> info = new JsonTypeInfoInternal<T>(options);
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
