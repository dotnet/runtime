// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics
{
    public readonly struct BFloat16
        : IComparable,
          IComparable<BFloat16>,
          IEquatable<BFloat16>
    {
        public static BFloat16 Epsilon { get; }
        public static BFloat16 MinValue { get; }
        public static BFloat16 MaxValue { get; }

        internal readonly ushort _value;

        internal BFloat16(ushort value) => _value = value;

        // Casting
        public static explicit operator BFloat16(float value) => new BFloat16((ushort)(BitConverter.SingleToUInt32Bits(value) >> 16));
        public static explicit operator BFloat16(double value) => (BFloat16)(float)value;
        public static explicit operator float(BFloat16 value) => BitConverter.Int32BitsToSingle(value._value << 16);
        public static explicit operator double(BFloat16 value) => (double)(float)value;

        // Comparison
        public int CompareTo(object value);
        public int CompareTo(BFloat16 value);
        public static bool operator ==(BFloat16 left, BFloat16 right);
        public static bool operator !=(BFloat16 left, BFloat16 right);
        public static bool operator <(BFloat16 left, BFloat16 right);
        public static bool operator >(BFloat16 left, BFloat16 right);
        public static bool operator <=(BFloat16 left, BFloat16 right);
        public static bool operator >=(BFloat16 left, BFloat16 right);

        // Equality
        public bool Equals(BFloat16 other) => ((float)this).Equals((float)other);
        public override bool Equals(object? obj) => obj is BFloat16 other && Equals(other);
        public override int GetHashCode() => ((float)this).GetHashCode();

        // ToString override
        public override string ToString() => ((float)this).ToString();
    }
}
