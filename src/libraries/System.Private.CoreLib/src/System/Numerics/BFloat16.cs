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

        // Casting
        public static explicit operator BFloat16(float value);
        public static explicit operator BFloat16(double value);
        public static explicit operator float(BFloat16 value);
        public static explicit operator double(BFloat16 value);

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
        public bool Equals(BFloat16 obj);
        public override bool Equals(object? obj);
        public override int GetHashCode();

        // ToString override
        public override string ToString();
    }
}
