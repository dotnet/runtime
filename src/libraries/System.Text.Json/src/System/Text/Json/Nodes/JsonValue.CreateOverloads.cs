// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Nodes
{
    public partial class JsonValue
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue Create(bool value, JsonNodeOptions? options = null) => new JsonValueTrimmable<bool>(value, JsonMetadataServices.BooleanConverter);

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue? Create(bool? value, JsonNodeOptions? options = null) => value.HasValue ? new JsonValueTrimmable<bool>(value.Value, JsonMetadataServices.BooleanConverter) : null;

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue Create(byte value, JsonNodeOptions? options = null) => new JsonValueTrimmable<byte>(value, JsonMetadataServices.ByteConverter);

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue? Create(byte? value, JsonNodeOptions? options = null) => value.HasValue ? new JsonValueTrimmable<byte>(value.Value, JsonMetadataServices.ByteConverter) : null;

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue Create(char value, JsonNodeOptions? options = null) => new JsonValueTrimmable<char>(value, JsonMetadataServices.CharConverter);

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue? Create(char? value, JsonNodeOptions? options = null) => value.HasValue ? new JsonValueTrimmable<char>(value.Value, JsonMetadataServices.CharConverter) : null;

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue Create(DateTime value, JsonNodeOptions? options = null) => new JsonValueTrimmable<DateTime>(value, JsonMetadataServices.DateTimeConverter);

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue? Create(DateTime? value, JsonNodeOptions? options = null) => value.HasValue ? new JsonValueTrimmable<DateTime>(value.Value, JsonMetadataServices.DateTimeConverter) : null;

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue Create(DateTimeOffset value, JsonNodeOptions? options = null) => new JsonValueTrimmable<DateTimeOffset>(value, JsonMetadataServices.DateTimeOffsetConverter);

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue? Create(DateTimeOffset? value, JsonNodeOptions? options = null) => value.HasValue ? new JsonValueTrimmable<DateTimeOffset>(value.Value, JsonMetadataServices.DateTimeOffsetConverter) : null;

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue Create(decimal value, JsonNodeOptions? options = null) => new JsonValueTrimmable<decimal>(value, JsonMetadataServices.DecimalConverter);

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue? Create(decimal? value, JsonNodeOptions? options = null) => value.HasValue ? new JsonValueTrimmable<decimal>(value.Value, JsonMetadataServices.DecimalConverter) : null;

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue Create(double value, JsonNodeOptions? options = null) => new JsonValueTrimmable<double>(value, JsonMetadataServices.DoubleConverter);

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue? Create(double? value, JsonNodeOptions? options = null) => value.HasValue ? new JsonValueTrimmable<double>(value.Value, JsonMetadataServices.DoubleConverter) : null;

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue Create(Guid value, JsonNodeOptions? options = null) => new JsonValueTrimmable<Guid>(value, JsonMetadataServices.GuidConverter);

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue? Create(Guid? value, JsonNodeOptions? options = null) => value.HasValue ? new JsonValueTrimmable<Guid>(value.Value, JsonMetadataServices.GuidConverter) : null;

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue Create(short value, JsonNodeOptions? options = null) => new JsonValueTrimmable<short>(value, JsonMetadataServices.Int16Converter);

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue? Create(short? value, JsonNodeOptions? options = null) => value.HasValue ? new JsonValueTrimmable<short>(value.Value, JsonMetadataServices.Int16Converter) : null;

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue Create(int value, JsonNodeOptions? options = null) => new JsonValueTrimmable<int>(value, JsonMetadataServices.Int32Converter);

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue? Create(int? value, JsonNodeOptions? options = null) => value.HasValue ? new JsonValueTrimmable<int>(value.Value, JsonMetadataServices.Int32Converter) : null;

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue Create(long value, JsonNodeOptions? options = null) => new JsonValueTrimmable<long>(value, JsonMetadataServices.Int64Converter);

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue? Create(long? value, JsonNodeOptions? options = null) => value.HasValue ? new JsonValueTrimmable<long>(value.Value, JsonMetadataServices.Int64Converter) : null;

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        [CLSCompliantAttribute(false)]
        public static JsonValue Create(sbyte value, JsonNodeOptions? options = null) => new JsonValueTrimmable<sbyte>(value, JsonMetadataServices.SByteConverter);

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        [CLSCompliantAttribute(false)]
        public static JsonValue? Create(sbyte? value, JsonNodeOptions? options = null) => value.HasValue ? new JsonValueTrimmable<sbyte>(value.Value, JsonMetadataServices.SByteConverter) : null;

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue Create(float value, JsonNodeOptions? options = null) => new JsonValueTrimmable<float>(value, JsonMetadataServices.SingleConverter);

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue? Create(float? value, JsonNodeOptions? options = null) => value.HasValue ? new JsonValueTrimmable<float>(value.Value, JsonMetadataServices.SingleConverter) : null;

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue? Create(string? value, JsonNodeOptions? options = null) => value != null ? new JsonValueTrimmable<string>(value, JsonMetadataServices.StringConverter) : null;

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        [CLSCompliantAttribute(false)]
        public static JsonValue Create(ushort value, JsonNodeOptions? options = null) => new JsonValueTrimmable<ushort>(value, JsonMetadataServices.UInt16Converter);

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        [CLSCompliantAttribute(false)]
        public static JsonValue? Create(ushort? value, JsonNodeOptions? options = null) => value.HasValue ? new JsonValueTrimmable<ushort>(value.Value, JsonMetadataServices.UInt16Converter) : null;

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        [CLSCompliantAttribute(false)]
        public static JsonValue Create(uint value, JsonNodeOptions? options = null) => new JsonValueTrimmable<uint>(value, JsonMetadataServices.UInt32Converter);

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        [CLSCompliantAttribute(false)]
        public static JsonValue? Create(uint? value, JsonNodeOptions? options = null) => value.HasValue ? new JsonValueTrimmable<uint>(value.Value, JsonMetadataServices.UInt32Converter) : null;

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        [CLSCompliantAttribute(false)]
        public static JsonValue Create(ulong value, JsonNodeOptions? options = null) => new JsonValueTrimmable<ulong>(value, JsonMetadataServices.UInt64Converter);

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        [CLSCompliantAttribute(false)]
        public static JsonValue? Create(ulong? value, JsonNodeOptions? options = null) => value.HasValue ? new JsonValueTrimmable<ulong>(value.Value, JsonMetadataServices.UInt64Converter) : null;

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue? Create(JsonElement value, JsonNodeOptions? options = null)
        {
            if (value.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            VerifyJsonElementIsNotArrayOrObject(ref value);

            return new JsonValueTrimmable<JsonElement>(value, JsonMetadataServices.JsonElementConverter);
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue? Create(JsonElement? value, JsonNodeOptions? options = null)
        {
            if (value == null)
            {
                return null;
            }

            JsonElement element = value.Value;
            if (element.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            VerifyJsonElementIsNotArrayOrObject(ref element);

            return new JsonValueTrimmable<JsonElement>(element, JsonMetadataServices.JsonElementConverter);
        }
    }
}
