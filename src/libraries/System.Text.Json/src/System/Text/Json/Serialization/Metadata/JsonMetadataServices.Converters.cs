// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Converters;

namespace System.Text.Json.Serialization.Metadata
{
    public static partial class JsonMetadataServices
    {
        // When adding/removing a built-in JsonConverter property below, also update
        // gen/JsonSourceGenerator.Parser.cs::GetSupportedJsonValueTypes so the union ambiguity
        // diagnostic (SYSLIB1227) agrees with JsonTypeInfo.BuildUnionValueTypeMap.

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="bool"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<bool> BooleanConverter => field ??= new BooleanConverter();

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts byte array values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<byte[]?> ByteArrayConverter => field ??= new ByteArrayConverter();

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="byte"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<byte> ByteConverter => field ??= new ByteConverter();

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="char"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<char> CharConverter => field ??= new CharConverter();

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="DateTime"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<DateTime> DateTimeConverter => field ??= new DateTimeConverter();

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="DateTimeOffset"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<DateTimeOffset> DateTimeOffsetConverter => field ??= new DateTimeOffsetConverter();

#if NET
        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="DateOnly"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<DateOnly> DateOnlyConverter => field ??= new DateOnlyConverter();

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="TimeOnly"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<TimeOnly> TimeOnlyConverter => field ??= new TimeOnlyConverter();
#endif

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="decimal"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<decimal> DecimalConverter => field ??= new DecimalConverter();

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="double"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<double> DoubleConverter => field ??= new DoubleConverter();

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="Guid"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<Guid> GuidConverter => field ??= new GuidConverter();

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="short"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<short> Int16Converter => field ??= new Int16Converter();

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="int"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<int> Int32Converter => field ??= new Int32Converter();

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="long"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<long> Int64Converter => field ??= new Int64Converter();

#if NET
        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="Int128"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<Int128> Int128Converter => field ??= new Int128Converter();

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="UInt128"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        [CLSCompliant(false)]
        public static JsonConverter<UInt128> UInt128Converter => field ??= new UInt128Converter();
#endif

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="JsonArray"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<JsonArray?> JsonArrayConverter => field ??= new JsonArrayConverter();

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="JsonElement"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<JsonElement> JsonElementConverter => field ??= new JsonElementConverter();

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="JsonNode"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<JsonNode?> JsonNodeConverter => field ??= new JsonNodeConverter();

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="JsonObject"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<JsonObject?> JsonObjectConverter => field ??= new JsonObjectConverter();

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="JsonArray"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<JsonValue?> JsonValueConverter => field ??= new JsonValueConverter();

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="JsonDocument"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<JsonDocument?> JsonDocumentConverter => field ??= new JsonDocumentConverter();

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="Memory{Byte}"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<Memory<byte>> MemoryByteConverter => field ??= new MemoryByteConverter();

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="ReadOnlyMemory{Byte}"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<ReadOnlyMemory<byte>> ReadOnlyMemoryByteConverter => field ??= new ReadOnlyMemoryByteConverter();

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="object"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<object?> ObjectConverter => field ??= new DefaultObjectConverter();

#if NET
        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="Half"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<Half> HalfConverter => field ??= new HalfConverter();
#endif

#if NET11_0_OR_GREATER
        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="System.Numerics.BFloat16"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<System.Numerics.BFloat16> BFloat16Converter =>
            field ??= new Ieee754FloatingPointConverter<System.Numerics.BFloat16>(NumericType.BFloat16);

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="System.Numerics.Decimal32"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<System.Numerics.Decimal32> Decimal32Converter =>
            field ??= new Ieee754FloatingPointConverter<System.Numerics.Decimal32>(NumericType.Decimal32);

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="System.Numerics.Decimal64"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<System.Numerics.Decimal64> Decimal64Converter =>
            field ??= new Ieee754FloatingPointConverter<System.Numerics.Decimal64>(NumericType.Decimal64);

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="System.Numerics.Decimal128"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<System.Numerics.Decimal128> Decimal128Converter =>
            field ??= new Ieee754FloatingPointConverter<System.Numerics.Decimal128>(NumericType.Decimal128);
#endif

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="float"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<float> SingleConverter => field ??= new SingleConverter();

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="sbyte"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        [CLSCompliant(false)]
        public static JsonConverter<sbyte> SByteConverter => field ??= new SByteConverter();

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="string"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<string?> StringConverter => field ??= new StringConverter();

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="TimeSpan"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<TimeSpan> TimeSpanConverter => field ??= new TimeSpanConverter();

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="ushort"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        [CLSCompliant(false)]
        public static JsonConverter<ushort> UInt16Converter => field ??= new UInt16Converter();

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="uint"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        [CLSCompliant(false)]
        public static JsonConverter<uint> UInt32Converter => field ??= new UInt32Converter();

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="ulong"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        [CLSCompliant(false)]
        public static JsonConverter<ulong> UInt64Converter => field ??= new UInt64Converter();

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="Uri"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<Uri?> UriConverter => field ??= new UriConverter();

        /// <summary>
        /// Returns a <see cref="JsonConverter{T}"/> instance that converts <see cref="Version"/> values.
        /// </summary>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<Version?> VersionConverter => field ??= new VersionConverter();

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
            ArgumentNullException.ThrowIfNull(options);

            return EnumConverterFactory.Helpers.Create<T>(EnumConverterOptions.AllowNumbers, options);
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
            ArgumentNullException.ThrowIfNull(underlyingTypeInfo);

            JsonConverter<T> underlyingConverter = GetTypedConverter<T>(underlyingTypeInfo.Converter);

            return new NullableConverter<T>(underlyingConverter);
        }

        /// <summary>
        /// Creates a <see cref="JsonConverter{T}"/> instance that converts <typeparamref name="T?"/> values.
        /// </summary>
        /// <typeparam name="T">The generic definition for the underlying nullable type.</typeparam>
        /// <param name="options">The <see cref="JsonSerializerOptions"/> to use for serialization and deserialization.</param>
        /// <returns>A <see cref="JsonConverter{T}"/> instance that converts <typeparamref name="T?"/> values</returns>
        /// <remarks>This API is for use by the output of the System.Text.Json source generator and should not be called directly.</remarks>
        public static JsonConverter<T?> GetNullableConverter<T>(JsonSerializerOptions options) where T : struct
        {
            ArgumentNullException.ThrowIfNull(options);

            JsonConverter<T> underlyingConverter = GetTypedConverter<T>(options.GetConverterInternal(typeof(T)));

            return new NullableConverter<T>(underlyingConverter);
        }

        internal static JsonConverter<T> GetTypedConverter<T>(JsonConverter converter)
        {
            JsonConverter<T>? typedConverter = converter as JsonConverter<T>;
            if (typedConverter is null)
            {
                throw new InvalidOperationException(SR.Format(SR.SerializationConverterNotCompatible, typedConverter, typeof(T)));
            }

            return typedConverter;
        }
    }
}
