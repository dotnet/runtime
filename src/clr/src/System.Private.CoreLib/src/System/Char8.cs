// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
    /// <summary>
    /// Represents a UTF-8 code unit, the elemental type of <see cref="Utf8String"/>.
    /// </summary>
    public readonly struct Char8 : IComparable<Char8>, IEquatable<Char8>
    {
        private readonly byte _value;

        private Char8(byte value)
        {
            _value = value;
        }

        public static bool operator ==(Char8 left, Char8 right) => left._value == right._value;
        public static bool operator !=(Char8 left, Char8 right) => left._value != right._value;
        public static bool operator <(Char8 left, Char8 right) => left._value < right._value;
        public static bool operator <=(Char8 left, Char8 right) => left._value <= right._value;
        public static bool operator >(Char8 left, Char8 right) => left._value > right._value;
        public static bool operator >=(Char8 left, Char8 right) => left._value >= right._value;

        // Operators from Utf8Char to <other primitives>
        // TODO: Once C# gets support for checked operators, we should add those here.

        public static implicit operator byte(Char8 value) => value._value;
        [CLSCompliant(false)]
        public static explicit operator sbyte(Char8 value) => (sbyte)value._value; // explicit because can integer overflow
        public static explicit operator char(Char8 value) => (char)value._value; // explicit because don't want to encourage char conversion
        public static implicit operator short(Char8 value) => value._value;
        [CLSCompliant(false)]
        public static implicit operator ushort(Char8 value) => value._value;
        public static implicit operator int(Char8 value) => value._value;
        [CLSCompliant(false)]
        public static implicit operator uint(Char8 value) => value._value;
        public static implicit operator long(Char8 value) => value._value;
        [CLSCompliant(false)]
        public static implicit operator ulong(Char8 value) => value._value;

        // Operators from <other primitives> to Char8; most are explicit because narrowing conversions could be lossy
        // TODO: Once C# gets support for checked operators, we should add those here.

        public static implicit operator Char8(byte value) => new Char8(value);
        [CLSCompliant(false)]
        public static explicit operator Char8(sbyte value) => new Char8((byte)value);
        public static explicit operator Char8(char value) => new Char8((byte)value);
        public static explicit operator Char8(short value) => new Char8((byte)value);
        [CLSCompliant(false)]
        public static explicit operator Char8(ushort value) => new Char8((byte)value);
        public static explicit operator Char8(int value) => new Char8((byte)value);
        [CLSCompliant(false)]
        public static explicit operator Char8(uint value) => new Char8((byte)value);
        public static explicit operator Char8(long value) => new Char8((byte)value);
        [CLSCompliant(false)]
        public static explicit operator Char8(ulong value) => new Char8((byte)value);

        public int CompareTo(Char8 other) => this._value.CompareTo(other._value);

        public override bool Equals(object? obj) => (obj is Char8 other) && (this == other);
        public bool Equals(Char8 other) => this == other;

        public override int GetHashCode() => _value;

        public override string ToString() => _value.ToString("X2");
    }
}
