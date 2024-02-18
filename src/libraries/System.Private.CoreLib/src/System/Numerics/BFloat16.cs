// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics
{
    public readonly struct BFloat16
        : IComparable,
          IComparable<BFloat16>,
          IEquatable<BFloat16>
    {
        private const ushort EpsilonBits = 0x0001;

        private const ushort MinValueBits = 0xFF7F;
        private const ushort MaxValueBits = 0x7F7F;

        public static BFloat16 Epsilon => new BFloat16(EpsilonBits);
        public static BFloat16 MinValue => new BFloat16(MinValueBits);
        public static BFloat16 MaxValue => new BFloat16(MaxValueBits);

        internal readonly ushort _value;

        internal BFloat16(ushort value) => _value = value;

        // Casting
        public static explicit operator BFloat16(float value) => new BFloat16((ushort)(BitConverter.SingleToUInt32Bits(value) >> 16));
        public static explicit operator BFloat16(double value) => (BFloat16)(float)value;
        public static explicit operator float(BFloat16 value) => BitConverter.Int32BitsToSingle(value._value << 16);
        public static explicit operator double(BFloat16 value) => (double)(float)value;

        // BFloat is effectively a truncation of Single, with lower 16 bits of precision truncated.
        // Delegating all operations to Single should be correct and effective.

        // Comparison
        public int CompareTo(object? obj)
        {
            if (obj is not BFloat16 other)
            {
                return (obj is null) ? 1 : throw new ArgumentException(SR.Arg_MustBeBFloat16);
            }
            return CompareTo(other);
        }
        public int CompareTo(BFloat16 other) => ((float)this).CompareTo((float)other);
        public static bool operator ==(BFloat16 left, BFloat16 right) => (float)left == (float)right;
        public static bool operator !=(BFloat16 left, BFloat16 right) => (float)left != (float)right;
        public static bool operator <(BFloat16 left, BFloat16 right) => (float)left < (float)right;
        public static bool operator >(BFloat16 left, BFloat16 right) => (float)left > (float)right;
        public static bool operator <=(BFloat16 left, BFloat16 right) => (float)left <= (float)right;
        public static bool operator >=(BFloat16 left, BFloat16 right) => (float)left >= (float)right;

        // Equality
        public bool Equals(BFloat16 other) => ((float)this).Equals((float)other);
        public override bool Equals(object? obj) => obj is BFloat16 other && Equals(other);
        public override int GetHashCode() => ((float)this).GetHashCode();

        // ToString override
        public override string ToString() => ((float)this).ToString();
    }
}
