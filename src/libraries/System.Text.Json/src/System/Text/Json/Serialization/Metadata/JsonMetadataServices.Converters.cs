// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Converters;

namespace System.Text.Json.Serialization.Metadata
{
    public static partial class JsonMetadataServices
    {
        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="bool"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<bool> BooleanConverter => s_booleanConverter ??= new BooleanConverter();
        private static JsonConverter<bool>? s_booleanConverter;

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts byte array values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<byte[]> ByteArrayConverter => s_byteArrayConverter ??= new ByteArrayConverter();
        private static JsonConverter<byte[]>? s_byteArrayConverter;

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="byte"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<byte> ByteConverter => s_byteConverter ??= new ByteConverter();
        private static JsonConverter<byte>? s_byteConverter;

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="char"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<char> CharConverter => s_charConverter ??= new CharConverter();
        private static JsonConverter<char>? s_charConverter;

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="DateTime"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<DateTime> DateTimeConverter => s_dateTimeConverter ??= new DateTimeConverter();
        private static JsonConverter<DateTime>? s_dateTimeConverter;

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="DateTimeOffset"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<DateTimeOffset> DateTimeOffsetConverter => s_dateTimeOffsetConverter ??= new DateTimeOffsetConverter();
        private static JsonConverter<DateTimeOffset>? s_dateTimeOffsetConverter;

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="decimal"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<decimal> DecimalConverter => s_decimalConverter ??= new DecimalConverter();
        private static JsonConverter<decimal>? s_decimalConverter;

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="double"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<double> DoubleConverter => s_doubleConverter ??= new DoubleConverter();
        private static JsonConverter<double>? s_doubleConverter;

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="Guid"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<Guid> GuidConverter => s_guidConverter ??= new GuidConverter();
        private static JsonConverter<Guid>? s_guidConverter;

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="short"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<short> Int16Converter => s_int16Converter ??= new Int16Converter();
        private static JsonConverter<short>? s_int16Converter;

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="int"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<int> Int32Converter => s_int32Converter ??= new Int32Converter();
        private static JsonConverter<int>? s_int32Converter;

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="long"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<long> Int64Converter => s_int64Converter ??= new Int64Converter();
        private static JsonConverter<long>? s_int64Converter;

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="JsonArray"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<JsonArray> JsonArrayConverter => s_jsonArrayConverter ??= new JsonArrayConverter();
        private static JsonConverter<JsonArray>? s_jsonArrayConverter;

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="JsonElement"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<JsonElement> JsonElementConverter => s_jsonElementConverter ??= new JsonElementConverter();
        private static JsonConverter<JsonElement>? s_jsonElementConverter;

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="JsonNode"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<JsonNode> JsonNodeConverter => s_jsonNodeConverter ??= new JsonNodeConverter();
        private static JsonConverter<JsonNode>? s_jsonNodeConverter;

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="JsonObject"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<JsonObject> JsonObjectConverter => s_jsonObjectConverter ??= new JsonObjectConverter();
        private static JsonConverter<JsonObject>? s_jsonObjectConverter;

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="JsonArray"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<JsonValue> JsonValueConverter => s_jsonValueConverter ??= new JsonValueConverter();
        private static JsonConverter<JsonValue>? s_jsonValueConverter;

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="JsonDocument"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<JsonDocument> JsonDocumentConverter => s_jsonDocumentConverter ??= new JsonDocumentConverter();
        private static JsonConverter<JsonDocument>? s_jsonDocumentConverter;

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="object"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<object?> ObjectConverter => s_objectConverter ??= new ObjectConverter();
        private static JsonConverter<object?>? s_objectConverter;

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="float"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<float> SingleConverter => s_singleConverter ??= new SingleConverter();
        private static JsonConverter<float>? s_singleConverter;

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="sbyte"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        [CLSCompliant(false)]
        public static JsonConverter<sbyte> SByteConverter => s_sbyteConverter ??= new SByteConverter();
        private static JsonConverter<sbyte>? s_sbyteConverter;

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="string"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<string> StringConverter => s_stringConverter ??= new StringConverter();
        private static JsonConverter<string>? s_stringConverter;

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="TimeSpan"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<TimeSpan> TimeSpanConverter => s_timeSpanConverter ??= new TimeSpanConverter();
        private static JsonConverter<TimeSpan>? s_timeSpanConverter;

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="ushort"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        [CLSCompliant(false)]
        public static JsonConverter<ushort> UInt16Converter => s_uint16Converter ??= new UInt16Converter();
        private static JsonConverter<ushort>? s_uint16Converter;

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="uint"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        [CLSCompliant(false)]
        public static JsonConverter<uint> UInt32Converter => s_uint32Converter ??= new UInt32Converter();
        private static JsonConverter<uint>? s_uint32Converter;

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="ulong"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        [CLSCompliant(false)]
        public static JsonConverter<ulong> UInt64Converter => s_uint64Converter ??= new UInt64Converter();
        private static JsonConverter<ulong>? s_uint64Converter;

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="Uri"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<Uri> UriConverter => s_uriConverter ??= new UriConverter();
        private static JsonConverter<Uri>? s_uriConverter;

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="Version"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<Version> VersionConverter => s_versionConverter ??= new VersionConverter();
        private static JsonConverter<Version>? s_versionConverter;

        /// <summary>
        /// Creates a <see cref="JsonConverter{T}"/> instance that throws <see cref="NotSupportedException"/>.
        /// </summary>
        /// <typeparam name="T">The generic definition for the type.</typeparam>
        /// <returns>A <see cref="JsonConverter{T}"/> instance that throws <see cref="NotSupportedException"/></returns>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<T> GetUnsupportedTypeConverter<T>()
            => new UnsupportedTypeConverter<T>();

        /// <summary>
        /// Creates a <see cref="JsonConverter{T}"/> instance that converts <typeparamref name="T"/> values.
        /// </summary>
        /// <typeparam name="T">The generic definition for the enum type.</typeparam>
        /// <param name="options">The <see cref="JsonSerializerOptions"/> to use for serialization and deserialization.</param>
        /// <returns>A <see cref="JsonConverter{T}"/> instance that converts <typeparamref name="T"/> values.</returns>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<T> GetEnumConverter<T>(JsonSerializerOptions options) where T : struct, Enum
        {
            if (options is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(options));
            }

            return new EnumConverter<T>(EnumConverterOptions.AllowNumbers, options);
        }

        /// <summary>
        /// Creates a <see cref="JsonConverter{T}"/> instance that converts <typeparamref name="T?"/> values.
        /// </summary>
        /// <typeparam name="T">The generic definition for the underlying nullable type.</typeparam>
        /// <param name="underlyingTypeInfo">Serialization metadata for the underlying nullable type.</param>
        /// <returns>A <see cref="JsonConverter{T}"/> instance that converts <typeparamref name="T?"/> values</returns>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<T?> GetNullableConverter<T>(JsonTypeInfo<T> underlyingTypeInfo) where T : struct
        {
            if (underlyingTypeInfo is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(underlyingTypeInfo));
            }

            JsonConverter<T>? underlyingConverter = underlyingTypeInfo.PropertyInfoForTypeInfo?.ConverterBase as JsonConverter<T>;
            if (underlyingConverter == null)
            {
                throw new InvalidOperationException(SR.Format(SR.SerializationConverterNotCompatible, underlyingConverter, typeof(T)));
            }

            return new NullableConverter<T>(underlyingConverter);
        }
    }
}
