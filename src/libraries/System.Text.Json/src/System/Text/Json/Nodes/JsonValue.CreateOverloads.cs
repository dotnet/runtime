// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
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
        public static JsonValue Create(bool value, JsonNodeOptions? options = null) => new JsonValuePrimitive<bool>(value, JsonMetadataServices.BooleanConverter, options);

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue? Create(bool? value, JsonNodeOptions? options = null) => value.HasValue ? new JsonValuePrimitive<bool>(value.Value, JsonMetadataServices.BooleanConverter, options) : null;

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue Create(byte value, JsonNodeOptions? options = null) => new JsonValuePrimitive<byte>(value, JsonMetadataServices.ByteConverter, options);

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue? Create(byte? value, JsonNodeOptions? options = null) => value.HasValue ? new JsonValuePrimitive<byte>(value.Value, JsonMetadataServices.ByteConverter, options) : null;

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue Create(char value, JsonNodeOptions? options = null) => new JsonValuePrimitive<char>(value, JsonMetadataServices.CharConverter, options);

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue? Create(char? value, JsonNodeOptions? options = null) => value.HasValue ? new JsonValuePrimitive<char>(value.Value, JsonMetadataServices.CharConverter, options) : null;

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue Create(DateTime value, JsonNodeOptions? options = null) => new JsonValuePrimitive<DateTime>(value, JsonMetadataServices.DateTimeConverter, options);

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue? Create(DateTime? value, JsonNodeOptions? options = null) => value.HasValue ? new JsonValuePrimitive<DateTime>(value.Value, JsonMetadataServices.DateTimeConverter, options) : null;

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue Create(DateTimeOffset value, JsonNodeOptions? options = null) => new JsonValuePrimitive<DateTimeOffset>(value, JsonMetadataServices.DateTimeOffsetConverter, options);

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue? Create(DateTimeOffset? value, JsonNodeOptions? options = null) => value.HasValue ? new JsonValuePrimitive<DateTimeOffset>(value.Value, JsonMetadataServices.DateTimeOffsetConverter, options) : null;

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue Create(decimal value, JsonNodeOptions? options = null) => new JsonValuePrimitive<decimal>(value, JsonMetadataServices.DecimalConverter, options);

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue? Create(decimal? value, JsonNodeOptions? options = null) => value.HasValue ? new JsonValuePrimitive<decimal>(value.Value, JsonMetadataServices.DecimalConverter, options) : null;

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue Create(double value, JsonNodeOptions? options = null) => new JsonValuePrimitive<double>(value, JsonMetadataServices.DoubleConverter, options);

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue? Create(double? value, JsonNodeOptions? options = null) => value.HasValue ? new JsonValuePrimitive<double>(value.Value, JsonMetadataServices.DoubleConverter, options) : null;

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue Create(Guid value, JsonNodeOptions? options = null) => new JsonValuePrimitive<Guid>(value, JsonMetadataServices.GuidConverter, options);

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue? Create(Guid? value, JsonNodeOptions? options = null) => value.HasValue ? new JsonValuePrimitive<Guid>(value.Value, JsonMetadataServices.GuidConverter, options) : null;

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue Create(short value, JsonNodeOptions? options = null) => new JsonValuePrimitive<short>(value, JsonMetadataServices.Int16Converter, options);

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue? Create(short? value, JsonNodeOptions? options = null) => value.HasValue ? new JsonValuePrimitive<short>(value.Value, JsonMetadataServices.Int16Converter, options) : null;

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue Create(int value, JsonNodeOptions? options = null) => new JsonValuePrimitive<int>(value, JsonMetadataServices.Int32Converter, options);

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue? Create(int? value, JsonNodeOptions? options = null) => value.HasValue ? new JsonValuePrimitive<int>(value.Value, JsonMetadataServices.Int32Converter, options) : null;

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue Create(long value, JsonNodeOptions? options = null) => new JsonValuePrimitive<long>(value, JsonMetadataServices.Int64Converter, options);

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue? Create(long? value, JsonNodeOptions? options = null) => value.HasValue ? new JsonValuePrimitive<long>(value.Value, JsonMetadataServices.Int64Converter, options) : null;

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        [CLSCompliantAttribute(false)]
        public static JsonValue Create(sbyte value, JsonNodeOptions? options = null) => new JsonValuePrimitive<sbyte>(value, JsonMetadataServices.SByteConverter, options);

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        [CLSCompliantAttribute(false)]
        public static JsonValue? Create(sbyte? value, JsonNodeOptions? options = null) => value.HasValue ? new JsonValuePrimitive<sbyte>(value.Value, JsonMetadataServices.SByteConverter, options) : null;

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue Create(float value, JsonNodeOptions? options = null) => new JsonValuePrimitive<float>(value, JsonMetadataServices.SingleConverter, options);

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue? Create(float? value, JsonNodeOptions? options = null) => value.HasValue ? new JsonValuePrimitive<float>(value.Value, JsonMetadataServices.SingleConverter, options) : null;

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        [return: NotNullIfNotNull(nameof(value))]
        public static JsonValue? Create(string? value, JsonNodeOptions? options = null) => value != null ? new JsonValuePrimitive<string>(value, JsonMetadataServices.StringConverter!, options) : null;

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        [CLSCompliantAttribute(false)]
        public static JsonValue Create(ushort value, JsonNodeOptions? options = null) => new JsonValuePrimitive<ushort>(value, JsonMetadataServices.UInt16Converter, options);

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        [CLSCompliantAttribute(false)]
        public static JsonValue? Create(ushort? value, JsonNodeOptions? options = null) => value.HasValue ? new JsonValuePrimitive<ushort>(value.Value, JsonMetadataServices.UInt16Converter, options) : null;

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        [CLSCompliantAttribute(false)]
        public static JsonValue Create(uint value, JsonNodeOptions? options = null) => new JsonValuePrimitive<uint>(value, JsonMetadataServices.UInt32Converter, options);

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        [CLSCompliantAttribute(false)]
        public static JsonValue? Create(uint? value, JsonNodeOptions? options = null) => value.HasValue ? new JsonValuePrimitive<uint>(value.Value, JsonMetadataServices.UInt32Converter, options) : null;

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        [CLSCompliantAttribute(false)]
        public static JsonValue Create(ulong value, JsonNodeOptions? options = null) => new JsonValuePrimitive<ulong>(value, JsonMetadataServices.UInt64Converter, options);

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        [CLSCompliantAttribute(false)]
        public static JsonValue? Create(ulong? value, JsonNodeOptions? options = null) => value.HasValue ? new JsonValuePrimitive<ulong>(value.Value, JsonMetadataServices.UInt64Converter, options) : null;

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue? Create(JsonElement value, JsonNodeOptions? options = null) => JsonValue.CreateFromElement(ref value, options);

        /// <summary>
        ///   Initializes a new instance of the <see cref="JsonValue"/> class that contains the specified value.
        /// </summary>
        /// <param name="value">The underlying value of the new <see cref="JsonValue"/> instance.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <returns>The new instance of the <see cref="JsonValue"/> class that contains the specified value.</returns>
        public static JsonValue? Create(JsonElement? value, JsonNodeOptions? options = null) => value is JsonElement element ? JsonValue.CreateFromElement(ref element, options) : null;
    }
}
