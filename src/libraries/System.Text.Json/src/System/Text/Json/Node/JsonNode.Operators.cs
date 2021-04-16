// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Node
{
    public partial class JsonNode
    {
        /// <summary>
        ///   Defines an implicit conversion of a given <see cref="bool"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="bool"/> to implicitly convert.</param>
        public static implicit operator JsonNode(bool value) => new JsonValue<bool>(value);

        /// <summary>
        ///   Defines an implicit conversion of a given <see cref="bool"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="bool"/> to implicitly convert.</param>
        public static implicit operator JsonNode?(bool? value) => value.HasValue ? new JsonValue<bool>(value.Value) : null;

        /// <summary>
        ///   Defines an implicit conversion of a given <see cref="byte"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="byte"/> to implicitly convert.</param>
        public static implicit operator JsonNode(byte value) => new JsonValue<byte>(value);

        /// <summary>
        ///   Defines an implicit conversion of a given <see cref="byte"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="byte"/> to implicitly convert.</param>
        public static implicit operator JsonNode?(byte? value) => value.HasValue ? new JsonValue<byte>(value.Value) : null;

        /// <summary>
        ///   Defines an implicit conversion of a given <see cref="char"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="char"/> to implicitly convert.</param>
        public static implicit operator JsonNode(char value) => new JsonValue<char>(value);

        /// <summary>
        ///   Defines an implicit conversion of a given <see cref="char"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="char"/> to implicitly convert.</param>
        public static implicit operator JsonNode?(char? value) => value.HasValue ? new JsonValue<char>(value.Value) : null;

        /// <summary>
        ///   Defines an implicit conversion of a given <see cref="DateTime"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="DateTime"/> to implicitly convert.</param>
        public static implicit operator JsonNode(DateTime value) => new JsonValue<DateTime>(value);

        /// <summary>
        ///   Defines an implicit conversion of a given <see cref="DateTime"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="DateTime"/> to implicitly convert.</param>
        public static implicit operator JsonNode?(DateTime? value) => value.HasValue ? new JsonValue<DateTime>(value.Value) : null;

        /// <summary>
        ///   Defines an implicit conversion of a given <see cref="DateTimeOffset"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="DateTimeOffset"/> to implicitly convert.</param>
        public static implicit operator JsonNode(DateTimeOffset value) => new JsonValue<DateTimeOffset>(value);

        /// <summary>
        ///   Defines an implicit conversion of a given <see cref="DateTimeOffset"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="DateTimeOffset"/> to implicitly convert.</param>
        public static implicit operator JsonNode?(DateTimeOffset? value) => value.HasValue ? new JsonValue<DateTimeOffset>(value.Value) : null;

        /// <summary>
        ///   Defines an implicit conversion of a given <see cref="decimal"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="decimal"/> to implicitly convert.</param>
        public static implicit operator JsonNode(decimal value) => new JsonValue<decimal>(value);

        /// <summary>
        ///   Defines an implicit conversion of a given <see cref="decimal"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="decimal"/> to implicitly convert.</param>
        public static implicit operator JsonNode?(decimal? value) => value.HasValue ? new JsonValue<decimal>(value.Value) : null;

        /// <summary>
        ///   Defines an implicit conversion of a given <see cref="double"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="double"/> to implicitly convert.</param>
        public static implicit operator JsonNode(double value) => new JsonValue<double>(value);

        /// <summary>
        ///   Defines an implicit conversion of a given <see cref="double"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="double"/> to implicitly convert.</param>
        public static implicit operator JsonNode?(double? value) => value.HasValue ? new JsonValue<double>(value.Value) : null;

        /// <summary>
        ///   Defines an implicit conversion of a given <see cref="Guid"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="Guid"/> to implicitly convert.</param>
        public static implicit operator JsonNode(Guid value) => new JsonValue<Guid>(value);

        /// <summary>
        ///   Defines an implicit conversion of a given <see cref="Guid"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="Guid"/> to implicitly convert.</param>
        public static implicit operator JsonNode?(Guid? value) => value.HasValue ? new JsonValue<Guid>(value.Value) : null;

        /// <summary>
        ///   Defines an implicit conversion of a given <see cref="short"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="short"/> to implicitly convert.</param>
        public static implicit operator JsonNode(short value) => new JsonValue<short>(value);

        /// <summary>
        ///   Defines an implicit conversion of a given <see cref="short"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="short"/> to implicitly convert.</param>
        public static implicit operator JsonNode?(short? value) => value.HasValue ? new JsonValue<short>(value.Value) : null;

        /// <summary>
        ///   Defines an implicit conversion of a given <see cref="int"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="int"/> to implicitly convert.</param>
        public static implicit operator JsonNode(int value) => new JsonValue<int>(value);

        /// <summary>
        ///   Defines an implicit conversion of a given <see cref="int"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="int"/> to implicitly convert.</param>
        public static implicit operator JsonNode?(int? value) => value.HasValue ? new JsonValue<int>(value.Value) : null;

        /// <summary>
        ///   Defines an implicit conversion of a given <see cref="long"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="long"/> to implicitly convert.</param>
        public static implicit operator JsonNode(long value) => new JsonValue<long>(value);

        /// <summary>
        ///   Defines an implicit conversion of a given <see cref="long"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="long"/> to implicitly convert.</param>
        public static implicit operator JsonNode?(long? value) => value.HasValue ? new JsonValue<long>(value.Value) : null;

        /// <summary>
        ///   Defines an implicit conversion of a given <see cref="sbyte"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="sbyte"/> to implicitly convert.</param>
        [System.CLSCompliantAttribute(false)]
        public static implicit operator JsonNode(sbyte value) => new JsonValue<sbyte>(value);

        /// <summary>
        ///   Defines an implicit conversion of a given <see cref="sbyte"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="sbyte"/> to implicitly convert.</param>
        [System.CLSCompliantAttribute(false)]
        public static implicit operator JsonNode?(sbyte? value) => value.HasValue ? new JsonValue<sbyte>(value.Value) : null;

        /// <summary>
        ///   Defines an implicit conversion of a given <see cref="float"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="float"/> to implicitly convert.</param>
        public static implicit operator JsonNode(float value) => new JsonValue<float>(value);

        /// <summary>
        ///   Defines an implicit conversion of a given <see cref="float"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="float"/> to implicitly convert.</param>
        public static implicit operator JsonNode?(float? value) => value.HasValue ? new JsonValue<float>(value.Value) : null;

        /// <summary>
        ///   Defines an implicit conversion of a given <see cref="string"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="string"/> to implicitly convert.</param>
        public static implicit operator JsonNode?(string? value) => (value == null ? null : new JsonValue<string>(value));

        /// <summary>
        ///   Defines an implicit conversion of a given <see cref="ushort"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="ushort"/> to implicitly convert.</param>
        [System.CLSCompliantAttribute(false)]
        public static implicit operator JsonNode(ushort value) => new JsonValue<ushort>(value);

        /// <summary>
        ///   Defines an implicit conversion of a given <see cref="ushort"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="ushort"/> to implicitly convert.</param>
        [System.CLSCompliantAttribute(false)]
        public static implicit operator JsonNode?(ushort? value) => value.HasValue ? new JsonValue<ushort>(value.Value) : null;

        /// <summary>
        ///   Defines an implicit conversion of a given <see cref="uint"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="uint"/> to implicitly convert.</param>
        [System.CLSCompliantAttribute(false)]
        public static implicit operator JsonNode(uint value) => new JsonValue<uint>(value);

        /// <summary>
        ///   Defines an implicit conversion of a given <see cref="uint"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="uint"/> to implicitly convert.</param>
        [System.CLSCompliantAttribute(false)]
        public static implicit operator JsonNode?(uint? value) => value.HasValue ? new JsonValue<uint>(value.Value) : null;

        /// <summary>
        ///   Defines an implicit conversion of a given <see cref="ulong"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="ulong"/> to implicitly convert.</param>
        [System.CLSCompliantAttribute(false)]
        public static implicit operator JsonNode(ulong value) => new JsonValue<ulong>(value);

        /// <summary>
        ///   Defines an implicit conversion of a given <see cref="ulong"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="ulong"/> to implicitly convert.</param>
        [System.CLSCompliantAttribute(false)]
        public static implicit operator JsonNode?(ulong? value) => value.HasValue ? new JsonValue<ulong>(value.Value) : null;

        /// <summary>
        ///   Defines an explicit conversion of a given <see cref="bool"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="bool"/> to implicitly convert.</param>
        public static explicit operator bool(JsonNode value) => value.GetValue<bool>();

        /// <summary>
        ///   Defines an explicit conversion of a given <see cref="bool"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="bool"/> to implicitly convert.</param>
        public static explicit operator bool?(JsonNode? value) => value?.GetValue<bool>();

        /// <summary>
        ///   Defines an explicit conversion of a given <see cref="byte"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="byte"/> to implicitly convert.</param>
        public static explicit operator byte(JsonNode value) => value.GetValue<byte>();

        /// <summary>
        ///   Defines an explicit conversion of a given <see cref="byte"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="byte"/> to implicitly convert.</param>
        public static explicit operator byte?(JsonNode? value) => value?.GetValue<byte>();

        /// <summary>
        ///   Defines an explicit conversion of a given <see cref="char"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="char"/> to implicitly convert.</param>
        public static explicit operator char(JsonNode value) => value.GetValue<char>();

        /// <summary>
        ///   Defines an explicit conversion of a given <see cref="char"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="char"/> to implicitly convert.</param>
        public static explicit operator char?(JsonNode? value) => value?.GetValue<char>();

        /// <summary>
        ///   Defines an explicit conversion of a given <see cref="DateTime"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="DateTime"/> to implicitly convert.</param>
        public static explicit operator DateTime(JsonNode value) => value.GetValue<DateTime>();

        /// <summary>
        ///   Defines an explicit conversion of a given <see cref="DateTime"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="DateTime"/> to implicitly convert.</param>
        public static explicit operator DateTime?(JsonNode? value) => value?.GetValue<DateTime>();

        /// <summary>
        ///   Defines an explicit conversion of a given <see cref="DateTimeOffset"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="DateTimeOffset"/> to implicitly convert.</param>
        public static explicit operator DateTimeOffset(JsonNode value) => value.GetValue<DateTimeOffset>();

        /// <summary>
        ///   Defines an explicit conversion of a given <see cref="DateTimeOffset"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="DateTimeOffset"/> to implicitly convert.</param>
        public static explicit operator DateTimeOffset?(JsonNode? value) => value?.GetValue<DateTimeOffset>();

        /// <summary>
        ///   Defines an explicit conversion of a given <see cref="decimal"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="decimal"/> to implicitly convert.</param>
        public static explicit operator decimal(JsonNode value) => value.GetValue<decimal>();

        /// <summary>
        ///   Defines an explicit conversion of a given <see cref="decimal"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="decimal"/> to implicitly convert.</param>
        public static explicit operator decimal?(JsonNode? value) => value?.GetValue<decimal>();

        /// <summary>
        ///   Defines an explicit conversion of a given <see cref="double"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="double"/> to implicitly convert.</param>
        public static explicit operator double(JsonNode value) => value.GetValue<double>();

        /// <summary>
        ///   Defines an explicit conversion of a given <see cref="double"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="double"/> to implicitly convert.</param>
        public static explicit operator double?(JsonNode? value) => value?.GetValue<double>();

        /// <summary>
        ///   Defines an explicit conversion of a given <see cref="Guid"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="Guid"/> to implicitly convert.</param>
        public static explicit operator Guid(JsonNode value) => value.GetValue<Guid>();

        /// <summary>
        ///   Defines an explicit conversion of a given <see cref="Guid"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="Guid"/> to implicitly convert.</param>
        public static explicit operator Guid?(JsonNode? value) => value?.GetValue<Guid>();

        /// <summary>
        ///   Defines an explicit conversion of a given <see cref="short"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="short"/> to implicitly convert.</param>
        public static explicit operator short(JsonNode value) => value.GetValue<short>();

        /// <summary>
        ///   Defines an explicit conversion of a given <see cref="short"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="short"/> to implicitly convert.</param>
        public static explicit operator short?(JsonNode? value) => value?.GetValue<short>();

        /// <summary>
        ///   Defines an explicit conversion of a given <see cref="int"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="int"/> to implicitly convert.</param>
        public static explicit operator int(JsonNode value) => value.GetValue<int>();

        /// <summary>
        ///   Defines an explicit conversion of a given <see cref="int"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="int"/> to implicitly convert.</param>
        public static explicit operator int?(JsonNode? value) => value?.GetValue<int>();

        /// <summary>
        ///   Defines an explicit conversion of a given <see cref="long"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="long"/> to implicitly convert.</param>
        public static explicit operator long(JsonNode value) => value.GetValue<long>();

        /// <summary>
        ///   Defines an explicit conversion of a given <see cref="long"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="long"/> to implicitly convert.</param>
        public static explicit operator long?(JsonNode? value) => value?.GetValue<long>();

        /// <summary>
        ///   Defines an explicit conversion of a given <see cref="sbyte"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="sbyte"/> to implicitly convert.</param>
        [System.CLSCompliantAttribute(false)]
        public static explicit operator sbyte(JsonNode value) => value.GetValue<sbyte>();

        /// <summary>
        ///   Defines an explicit conversion of a given <see cref="sbyte"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="sbyte"/> to implicitly convert.</param>
        [System.CLSCompliantAttribute(false)]
        public static explicit operator sbyte?(JsonNode? value) => value?.GetValue<sbyte>();

        /// <summary>
        ///   Defines an explicit conversion of a given <see cref="float"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="float"/> to implicitly convert.</param>
        public static explicit operator float(JsonNode value) => value.GetValue<float>();

        /// <summary>
        ///   Defines an explicit conversion of a given <see cref="float"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="float"/> to implicitly convert.</param>
        public static explicit operator float?(JsonNode? value) => value?.GetValue<float>();

        /// <summary>
        ///   Defines an explicit conversion of a given <see cref="string"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="string"/> to implicitly convert.</param>
        public static explicit operator string?(JsonNode? value) => value?.GetValue<string>();

        /// <summary>
        ///   Defines an explicit conversion of a given <see cref="ushort"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="ushort"/> to implicitly convert.</param>
        [System.CLSCompliantAttribute(false)]
        public static explicit operator ushort(JsonNode value) => value.GetValue<ushort>();

        /// <summary>
        ///   Defines an explicit conversion of a given <see cref="ushort"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="ushort"/> to implicitly convert.</param>
        [System.CLSCompliantAttribute(false)]
        public static explicit operator ushort?(JsonNode? value) => value?.GetValue<ushort>();

        /// <summary>
        ///   Defines an explicit conversion of a given <see cref="uint"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="uint"/> to implicitly convert.</param>
        [System.CLSCompliantAttribute(false)]
        public static explicit operator uint(JsonNode value) => value.GetValue<uint>();

        /// <summary>
        ///   Defines an explicit conversion of a given <see cref="uint"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="uint"/> to implicitly convert.</param>
        [System.CLSCompliantAttribute(false)]
        public static explicit operator uint?(JsonNode? value) => value?.GetValue<uint>();

        /// <summary>
        ///   Defines an explicit conversion of a given <see cref="ulong"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="ulong"/> to implicitly convert.</param>
        [System.CLSCompliantAttribute(false)]
        public static explicit operator ulong(JsonNode value) => value.GetValue<ulong>();

        /// <summary>
        ///   Defines an explicit conversion of a given <see cref="ulong"/> to a <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="value">A <see cref="ulong"/> to implicitly convert.</param>
        [System.CLSCompliantAttribute(false)]
        public static explicit operator ulong?(JsonNode? value) => value?.GetValue<ulong>();
    }
}
