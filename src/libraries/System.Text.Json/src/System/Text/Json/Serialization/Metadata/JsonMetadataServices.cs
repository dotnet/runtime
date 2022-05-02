// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// Provides helpers to create and initialize metadata for JSON-serializable types.
    /// </summary>
    /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static partial class JsonMetadataServices
    {
        /// <summary>
        /// Creates metadata for a property or field.
        /// </summary>
        /// <typeparam name="T">The type that the converter for the property returns or accepts when converting JSON data.</typeparam>
        /// <param name="options">The <see cref="JsonSerializerOptions"/> to initialize the metadata with.</param>
        /// <param name="propertyInfo">Provides serialization metadata about the property or field.</param>
        /// <returns>A <see cref="JsonPropertyInfo"/> instance intialized with the provided metadata.</returns>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonPropertyInfo CreatePropertyInfo<T>(JsonSerializerOptions options, JsonPropertyInfoValues<T> propertyInfo)
        {
            if (options is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(options));
            }
            if (propertyInfo is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(propertyInfo));
            }

            Type? declaringType = propertyInfo.DeclaringType;
            if (declaringType == null)
            {
                throw new ArgumentException(nameof(propertyInfo.DeclaringType));
            }

            JsonTypeInfo? propertyTypeInfo = propertyInfo.PropertyTypeInfo;
            if (propertyTypeInfo == null)
            {
                throw new ArgumentException(nameof(propertyInfo.PropertyTypeInfo));
            }

            string? propertyName = propertyInfo.PropertyName;
            if (propertyName == null)
            {
                throw new ArgumentException(nameof(propertyInfo.PropertyName));
            }

            JsonConverter? converter = propertyInfo.Converter;
            if (converter == null)
            {
                converter = propertyTypeInfo.PropertyInfoForTypeInfo.ConverterBase as JsonConverter<T>;
                if (converter == null)
                {
                    throw new InvalidOperationException(SR.Format(SR.ConverterForPropertyMustBeValid, declaringType, propertyName, typeof(T)));
                }
            }

            if (!propertyInfo.IsProperty && propertyInfo.IsVirtual)
            {
                throw new InvalidOperationException(SR.Format(SR.FieldCannotBeVirtual, nameof(propertyInfo.IsProperty), nameof(propertyInfo.IsVirtual)));
            }

            JsonPropertyInfo<T> jsonPropertyInfo = new JsonPropertyInfo<T>();
            jsonPropertyInfo.InitializeForSourceGen(options, propertyInfo);
            return jsonPropertyInfo;
        }

        /// <summary>
        /// Creates metadata for a complex class or struct.
        /// </summary>
        /// <param name="options">The <see cref="JsonSerializerOptions"/> to initialize the metadata with.</param>
        /// <param name="objectInfo">Provides serialization metadata about an object type with constructors, properties, and fields.</param>
        /// <typeparam name="T">The type of the class or struct.</typeparam>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> or <paramref name="objectInfo"/> is null.</exception>
        /// <returns>A <see cref="JsonTypeInfo{T}"/> instance representing the class or struct.</returns>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonTypeInfo<T> CreateObjectInfo<T>(JsonSerializerOptions options, JsonObjectInfoValues<T> objectInfo) where T : notnull
        {
            if (options is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(options));
            }
            if (objectInfo is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(objectInfo));
            }

            return new SourceGenJsonTypeInfo<T>(options, objectInfo);
        }

        /// <summary>
        /// Creates metadata for a primitive or a type with a custom converter.
        /// </summary>
        /// <typeparam name="T">The generic type definition.</typeparam>
        /// <returns>A <see cref="JsonTypeInfo{T}"/> instance representing the type.</returns>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonTypeInfo<T> CreateValueInfo<T>(JsonSerializerOptions options, JsonConverter converter)
        {
            JsonTypeInfo<T> info = new SourceGenJsonTypeInfo<T>(converter, options);
            return info;
        }
    }
}
