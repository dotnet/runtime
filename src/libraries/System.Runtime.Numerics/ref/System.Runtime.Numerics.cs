// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Numerics
{
    public readonly partial struct BigInteger : System.IComparable, System.IComparable<System.Numerics.BigInteger>, System.IEquatable<System.Numerics.BigInteger>, System.IFormattable, System.IParsable<System.Numerics.BigInteger>, System.ISpanFormattable, System.ISpanParsable<System.Numerics.BigInteger>, System.Numerics.IAdditionOperators<System.Numerics.BigInteger, System.Numerics.BigInteger, System.Numerics.BigInteger>, System.Numerics.IAdditiveIdentity<System.Numerics.BigInteger, System.Numerics.BigInteger>, System.Numerics.IBinaryInteger<System.Numerics.BigInteger>, System.Numerics.IBinaryNumber<System.Numerics.BigInteger>, System.Numerics.IBitwiseOperators<System.Numerics.BigInteger, System.Numerics.BigInteger, System.Numerics.BigInteger>, System.Numerics.IComparisonOperators<System.Numerics.BigInteger, System.Numerics.BigInteger>, System.Numerics.IDecrementOperators<System.Numerics.BigInteger>, System.Numerics.IDivisionOperators<System.Numerics.BigInteger, System.Numerics.BigInteger, System.Numerics.BigInteger>, System.Numerics.IEqualityOperators<System.Numerics.BigInteger, System.Numerics.BigInteger>, System.Numerics.IIncrementOperators<System.Numerics.BigInteger>, System.Numerics.IModulusOperators<System.Numerics.BigInteger, System.Numerics.BigInteger, System.Numerics.BigInteger>, System.Numerics.IMultiplicativeIdentity<System.Numerics.BigInteger, System.Numerics.BigInteger>, System.Numerics.IMultiplyOperators<System.Numerics.BigInteger, System.Numerics.BigInteger, System.Numerics.BigInteger>, System.Numerics.INumber<System.Numerics.BigInteger>, System.Numerics.INumberBase<System.Numerics.BigInteger>, System.Numerics.IShiftOperators<System.Numerics.BigInteger, System.Numerics.BigInteger>, System.Numerics.ISignedNumber<System.Numerics.BigInteger>, System.Numerics.ISubtractionOperators<System.Numerics.BigInteger, System.Numerics.BigInteger, System.Numerics.BigInteger>, System.Numerics.IUnaryNegationOperators<System.Numerics.BigInteger, System.Numerics.BigInteger>, System.Numerics.IUnaryPlusOperators<System.Numerics.BigInteger, System.Numerics.BigInteger>
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        [System.CLSCompliantAttribute(false)]
        public BigInteger(byte[] value) { throw null; }
        public BigInteger(decimal value) { throw null; }
        public BigInteger(double value) { throw null; }
        public BigInteger(int value) { throw null; }
        public BigInteger(long value) { throw null; }
        public BigInteger(System.ReadOnlySpan<byte> value, bool isUnsigned = false, bool isBigEndian = false) { throw null; }
        public BigInteger(float value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public BigInteger(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public BigInteger(ulong value) { throw null; }
        public bool IsEven { get { throw null; } }
        public bool IsOne { get { throw null; } }
        public bool IsPowerOfTwo { get { throw null; } }
        public bool IsZero { get { throw null; } }
        public static System.Numerics.BigInteger MinusOne { get { throw null; } }
        public static System.Numerics.BigInteger One { get { throw null; } }
        public int Sign { get { throw null; } }
        static System.Numerics.BigInteger System.Numerics.IAdditiveIdentity<System.Numerics.BigInteger,System.Numerics.BigInteger>.AdditiveIdentity { get { throw null; } }
        static System.Numerics.BigInteger System.Numerics.IMultiplicativeIdentity<System.Numerics.BigInteger,System.Numerics.BigInteger>.MultiplicativeIdentity { get { throw null; } }
        static int System.Numerics.INumberBase<System.Numerics.BigInteger>.Radix { get { throw null; } }
        static System.Numerics.BigInteger System.Numerics.ISignedNumber<System.Numerics.BigInteger>.NegativeOne { get { throw null; } }
        public static System.Numerics.BigInteger Zero { get { throw null; } }
        public static System.Numerics.BigInteger Abs(System.Numerics.BigInteger value) { throw null; }
        public static System.Numerics.BigInteger Add(System.Numerics.BigInteger left, System.Numerics.BigInteger right) { throw null; }
        public static System.Numerics.BigInteger Clamp(System.Numerics.BigInteger value, System.Numerics.BigInteger min, System.Numerics.BigInteger max) { throw null; }
        public static int Compare(System.Numerics.BigInteger left, System.Numerics.BigInteger right) { throw null; }
        public int CompareTo(long other) { throw null; }
        public int CompareTo(System.Numerics.BigInteger other) { throw null; }
        public int CompareTo(object? obj) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public int CompareTo(ulong other) { throw null; }
        public static System.Numerics.BigInteger CopySign(System.Numerics.BigInteger value, System.Numerics.BigInteger sign) { throw null; }
        public static System.Numerics.BigInteger Divide(System.Numerics.BigInteger dividend, System.Numerics.BigInteger divisor) { throw null; }
        public static (System.Numerics.BigInteger Quotient, System.Numerics.BigInteger Remainder) DivRem(System.Numerics.BigInteger left, System.Numerics.BigInteger right) { throw null; }
        public static System.Numerics.BigInteger DivRem(System.Numerics.BigInteger dividend, System.Numerics.BigInteger divisor, out System.Numerics.BigInteger remainder) { throw null; }
        public bool Equals(long other) { throw null; }
        public bool Equals(System.Numerics.BigInteger other) { throw null; }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public bool Equals(ulong other) { throw null; }
        public long GetBitLength() { throw null; }
        public int GetByteCount(bool isUnsigned = false) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Numerics.BigInteger GreatestCommonDivisor(System.Numerics.BigInteger left, System.Numerics.BigInteger right) { throw null; }
        public static bool IsEvenInteger(System.Numerics.BigInteger value) { throw null; }
        public static bool IsNegative(System.Numerics.BigInteger value) { throw null; }
        public static bool IsOddInteger(System.Numerics.BigInteger value) { throw null; }
        public static bool IsPositive(System.Numerics.BigInteger value) { throw null; }
        public static bool IsPow2(System.Numerics.BigInteger value) { throw null; }
        public static System.Numerics.BigInteger LeadingZeroCount(System.Numerics.BigInteger value) { throw null; }
        public static double Log(System.Numerics.BigInteger value) { throw null; }
        public static double Log(System.Numerics.BigInteger value, double baseValue) { throw null; }
        public static double Log10(System.Numerics.BigInteger value) { throw null; }
        public static System.Numerics.BigInteger Log2(System.Numerics.BigInteger value) { throw null; }
        public static System.Numerics.BigInteger Max(System.Numerics.BigInteger left, System.Numerics.BigInteger right) { throw null; }
        public static System.Numerics.BigInteger MaxMagnitude(System.Numerics.BigInteger x, System.Numerics.BigInteger y) { throw null; }
        public static System.Numerics.BigInteger Min(System.Numerics.BigInteger left, System.Numerics.BigInteger right) { throw null; }
        public static System.Numerics.BigInteger MinMagnitude(System.Numerics.BigInteger x, System.Numerics.BigInteger y) { throw null; }
        public static System.Numerics.BigInteger ModPow(System.Numerics.BigInteger value, System.Numerics.BigInteger exponent, System.Numerics.BigInteger modulus) { throw null; }
        public static System.Numerics.BigInteger Multiply(System.Numerics.BigInteger left, System.Numerics.BigInteger right) { throw null; }
        public static System.Numerics.BigInteger Negate(System.Numerics.BigInteger value) { throw null; }
        public static System.Numerics.BigInteger operator +(System.Numerics.BigInteger left, System.Numerics.BigInteger right) { throw null; }
        public static System.Numerics.BigInteger operator &(System.Numerics.BigInteger left, System.Numerics.BigInteger right) { throw null; }
        public static System.Numerics.BigInteger operator |(System.Numerics.BigInteger left, System.Numerics.BigInteger right) { throw null; }
        public static System.Numerics.BigInteger operator --(System.Numerics.BigInteger value) { throw null; }
        public static System.Numerics.BigInteger operator /(System.Numerics.BigInteger dividend, System.Numerics.BigInteger divisor) { throw null; }
        public static bool operator ==(long left, System.Numerics.BigInteger right) { throw null; }
        public static bool operator ==(System.Numerics.BigInteger left, long right) { throw null; }
        public static bool operator ==(System.Numerics.BigInteger left, System.Numerics.BigInteger right) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool operator ==(System.Numerics.BigInteger left, ulong right) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool operator ==(ulong left, System.Numerics.BigInteger right) { throw null; }
        public static System.Numerics.BigInteger operator ^(System.Numerics.BigInteger left, System.Numerics.BigInteger right) { throw null; }
        public static explicit operator System.Numerics.BigInteger (decimal value) { throw null; }
        public static explicit operator System.Numerics.BigInteger (double value) { throw null; }
        public static explicit operator System.Numerics.BigInteger (System.Half value) { throw null; }
        public static explicit operator byte (System.Numerics.BigInteger value) { throw null; }
        public static explicit operator char (System.Numerics.BigInteger value) { throw null; }
        public static explicit operator decimal (System.Numerics.BigInteger value) { throw null; }
        public static explicit operator double (System.Numerics.BigInteger value) { throw null; }
        public static explicit operator System.Half (System.Numerics.BigInteger value) { throw null; }
        public static explicit operator System.Int128 (System.Numerics.BigInteger value) { throw null; }
        public static explicit operator short (System.Numerics.BigInteger value) { throw null; }
        public static explicit operator int (System.Numerics.BigInteger value) { throw null; }
        public static explicit operator long (System.Numerics.BigInteger value) { throw null; }
        public static explicit operator nint (System.Numerics.BigInteger value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator sbyte (System.Numerics.BigInteger value) { throw null; }
        public static explicit operator float (System.Numerics.BigInteger value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.UInt128 (System.Numerics.BigInteger value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator ushort (System.Numerics.BigInteger value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator uint (System.Numerics.BigInteger value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator ulong (System.Numerics.BigInteger value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator nuint (System.Numerics.BigInteger value) { throw null; }
        public static explicit operator System.Numerics.BigInteger (System.Numerics.Complex value) { throw null; }
        public static explicit operator System.Numerics.BigInteger (float value) { throw null; }
        public static bool operator >(long left, System.Numerics.BigInteger right) { throw null; }
        public static bool operator >(System.Numerics.BigInteger left, long right) { throw null; }
        public static bool operator >(System.Numerics.BigInteger left, System.Numerics.BigInteger right) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool operator >(System.Numerics.BigInteger left, ulong right) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool operator >(ulong left, System.Numerics.BigInteger right) { throw null; }
        public static bool operator >=(long left, System.Numerics.BigInteger right) { throw null; }
        public static bool operator >=(System.Numerics.BigInteger left, long right) { throw null; }
        public static bool operator >=(System.Numerics.BigInteger left, System.Numerics.BigInteger right) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool operator >=(System.Numerics.BigInteger left, ulong right) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool operator >=(ulong left, System.Numerics.BigInteger right) { throw null; }
        public static implicit operator System.Numerics.BigInteger (byte value) { throw null; }
        public static implicit operator System.Numerics.BigInteger (char value) { throw null; }
        public static implicit operator System.Numerics.BigInteger (System.Int128 value) { throw null; }
        public static implicit operator System.Numerics.BigInteger (short value) { throw null; }
        public static implicit operator System.Numerics.BigInteger (int value) { throw null; }
        public static implicit operator System.Numerics.BigInteger (long value) { throw null; }
        public static implicit operator System.Numerics.BigInteger (nint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Numerics.BigInteger (sbyte value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Numerics.BigInteger (System.UInt128 value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Numerics.BigInteger (ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Numerics.BigInteger (uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Numerics.BigInteger (ulong value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Numerics.BigInteger (nuint value) { throw null; }
        public static System.Numerics.BigInteger operator ++(System.Numerics.BigInteger value) { throw null; }
        public static bool operator !=(long left, System.Numerics.BigInteger right) { throw null; }
        public static bool operator !=(System.Numerics.BigInteger left, long right) { throw null; }
        public static bool operator !=(System.Numerics.BigInteger left, System.Numerics.BigInteger right) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool operator !=(System.Numerics.BigInteger left, ulong right) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool operator !=(ulong left, System.Numerics.BigInteger right) { throw null; }
        public static System.Numerics.BigInteger operator <<(System.Numerics.BigInteger value, int shift) { throw null; }
        public static bool operator <(long left, System.Numerics.BigInteger right) { throw null; }
        public static bool operator <(System.Numerics.BigInteger left, long right) { throw null; }
        public static bool operator <(System.Numerics.BigInteger left, System.Numerics.BigInteger right) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool operator <(System.Numerics.BigInteger left, ulong right) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool operator <(ulong left, System.Numerics.BigInteger right) { throw null; }
        public static bool operator <=(long left, System.Numerics.BigInteger right) { throw null; }
        public static bool operator <=(System.Numerics.BigInteger left, long right) { throw null; }
        public static bool operator <=(System.Numerics.BigInteger left, System.Numerics.BigInteger right) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool operator <=(System.Numerics.BigInteger left, ulong right) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool operator <=(ulong left, System.Numerics.BigInteger right) { throw null; }
        public static System.Numerics.BigInteger operator %(System.Numerics.BigInteger dividend, System.Numerics.BigInteger divisor) { throw null; }
        public static System.Numerics.BigInteger operator *(System.Numerics.BigInteger left, System.Numerics.BigInteger right) { throw null; }
        public static System.Numerics.BigInteger operator ~(System.Numerics.BigInteger value) { throw null; }
        public static System.Numerics.BigInteger operator >>(System.Numerics.BigInteger value, int shift) { throw null; }
        public static System.Numerics.BigInteger operator -(System.Numerics.BigInteger left, System.Numerics.BigInteger right) { throw null; }
        public static System.Numerics.BigInteger operator -(System.Numerics.BigInteger value) { throw null; }
        public static System.Numerics.BigInteger operator +(System.Numerics.BigInteger value) { throw null; }
        public static System.Numerics.BigInteger operator >>>(System.Numerics.BigInteger value, int shiftAmount) { throw null; }
        public static System.Numerics.BigInteger Parse(System.ReadOnlySpan<char> value, System.Globalization.NumberStyles style = System.Globalization.NumberStyles.Integer, System.IFormatProvider? provider = null) { throw null; }
        public static System.Numerics.BigInteger Parse(System.ReadOnlySpan<char> s, System.IFormatProvider? provider) { throw null; }
        public static System.Numerics.BigInteger Parse(string value) { throw null; }
        public static System.Numerics.BigInteger Parse(string value, System.Globalization.NumberStyles style) { throw null; }
        public static System.Numerics.BigInteger Parse(string value, System.Globalization.NumberStyles style, System.IFormatProvider? provider) { throw null; }
        public static System.Numerics.BigInteger Parse(string value, System.IFormatProvider? provider) { throw null; }
        public static System.Numerics.BigInteger PopCount(System.Numerics.BigInteger value) { throw null; }
        public static System.Numerics.BigInteger Pow(System.Numerics.BigInteger value, int exponent) { throw null; }
        public static System.Numerics.BigInteger Remainder(System.Numerics.BigInteger dividend, System.Numerics.BigInteger divisor) { throw null; }
        public static System.Numerics.BigInteger RotateLeft(System.Numerics.BigInteger value, int rotateAmount) { throw null; }
        public static System.Numerics.BigInteger RotateRight(System.Numerics.BigInteger value, int rotateAmount) { throw null; }
        public static System.Numerics.BigInteger Subtract(System.Numerics.BigInteger left, System.Numerics.BigInteger right) { throw null; }
        int System.Numerics.IBinaryInteger<System.Numerics.BigInteger>.GetByteCount() { throw null; }
        int System.Numerics.IBinaryInteger<System.Numerics.BigInteger>.GetShortestBitLength() { throw null; }
        bool System.Numerics.IBinaryInteger<System.Numerics.BigInteger>.TryWriteBigEndian(System.Span<byte> destination, out int bytesWritten) { throw null; }
        bool System.Numerics.IBinaryInteger<System.Numerics.BigInteger>.TryWriteLittleEndian(System.Span<byte> destination, out int bytesWritten) { throw null; }
        static bool System.Numerics.INumberBase<System.Numerics.BigInteger>.IsCanonical(System.Numerics.BigInteger value) { throw null; }
        static bool System.Numerics.INumberBase<System.Numerics.BigInteger>.IsComplexNumber(System.Numerics.BigInteger value) { throw null; }
        static bool System.Numerics.INumberBase<System.Numerics.BigInteger>.IsFinite(System.Numerics.BigInteger value) { throw null; }
        static bool System.Numerics.INumberBase<System.Numerics.BigInteger>.IsImaginaryNumber(System.Numerics.BigInteger value) { throw null; }
        static bool System.Numerics.INumberBase<System.Numerics.BigInteger>.IsInfinity(System.Numerics.BigInteger value) { throw null; }
        static bool System.Numerics.INumberBase<System.Numerics.BigInteger>.IsInteger(System.Numerics.BigInteger value) { throw null; }
        static bool System.Numerics.INumberBase<System.Numerics.BigInteger>.IsNaN(System.Numerics.BigInteger value) { throw null; }
        static bool System.Numerics.INumberBase<System.Numerics.BigInteger>.IsNegativeInfinity(System.Numerics.BigInteger value) { throw null; }
        static bool System.Numerics.INumberBase<System.Numerics.BigInteger>.IsNormal(System.Numerics.BigInteger value) { throw null; }
        static bool System.Numerics.INumberBase<System.Numerics.BigInteger>.IsPositiveInfinity(System.Numerics.BigInteger value) { throw null; }
        static bool System.Numerics.INumberBase<System.Numerics.BigInteger>.IsRealNumber(System.Numerics.BigInteger value) { throw null; }
        static bool System.Numerics.INumberBase<System.Numerics.BigInteger>.IsSubnormal(System.Numerics.BigInteger value) { throw null; }
        static bool System.Numerics.INumberBase<System.Numerics.BigInteger>.IsZero(System.Numerics.BigInteger value) { throw null; }
        static System.Numerics.BigInteger System.Numerics.INumberBase<System.Numerics.BigInteger>.MaxMagnitudeNumber(System.Numerics.BigInteger x, System.Numerics.BigInteger y) { throw null; }
        static System.Numerics.BigInteger System.Numerics.INumberBase<System.Numerics.BigInteger>.MinMagnitudeNumber(System.Numerics.BigInteger x, System.Numerics.BigInteger y) { throw null; }
        static bool System.Numerics.INumberBase<System.Numerics.BigInteger>.TryConvertFromChecked<TOther>(TOther value, out System.Numerics.BigInteger result) { throw null; }
        static bool System.Numerics.INumberBase<System.Numerics.BigInteger>.TryConvertFromSaturating<TOther>(TOther value, out System.Numerics.BigInteger result) { throw null; }
        static bool System.Numerics.INumberBase<System.Numerics.BigInteger>.TryConvertFromTruncating<TOther>(TOther value, out System.Numerics.BigInteger result) { throw null; }
        static bool System.Numerics.INumberBase<System.Numerics.BigInteger>.TryConvertToChecked<TOther>(System.Numerics.BigInteger value, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out TOther result) { throw null; }
        static bool System.Numerics.INumberBase<System.Numerics.BigInteger>.TryConvertToSaturating<TOther>(System.Numerics.BigInteger value, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out TOther result) { throw null; }
        static bool System.Numerics.INumberBase<System.Numerics.BigInteger>.TryConvertToTruncating<TOther>(System.Numerics.BigInteger value, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out TOther result) { throw null; }
        static System.Numerics.BigInteger System.Numerics.INumber<System.Numerics.BigInteger>.MaxNumber(System.Numerics.BigInteger x, System.Numerics.BigInteger y) { throw null; }
        static System.Numerics.BigInteger System.Numerics.INumber<System.Numerics.BigInteger>.MinNumber(System.Numerics.BigInteger x, System.Numerics.BigInteger y) { throw null; }
        static int System.Numerics.INumber<System.Numerics.BigInteger>.Sign(System.Numerics.BigInteger value) { throw null; }
        public byte[] ToByteArray() { throw null; }
        public byte[] ToByteArray(bool isUnsigned = false, bool isBigEndian = false) { throw null; }
        public override string ToString() { throw null; }
        public string ToString(System.IFormatProvider? provider) { throw null; }
        public string ToString([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("NumericFormat")] string? format) { throw null; }
        public string ToString([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("NumericFormat")] string? format, System.IFormatProvider? provider) { throw null; }
        public static System.Numerics.BigInteger TrailingZeroCount(System.Numerics.BigInteger value) { throw null; }
        public bool TryFormat(System.Span<char> destination, out int charsWritten, [System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("NumericFormat")] System.ReadOnlySpan<char> format = default(System.ReadOnlySpan<char>), System.IFormatProvider? provider = null) { throw null; }
        public static bool TryParse(System.ReadOnlySpan<char> value, System.Globalization.NumberStyles style, System.IFormatProvider? provider, out System.Numerics.BigInteger result) { throw null; }
        public static bool TryParse(System.ReadOnlySpan<char> s, System.IFormatProvider? provider, out System.Numerics.BigInteger result) { throw null; }
        public static bool TryParse(System.ReadOnlySpan<char> value, out System.Numerics.BigInteger result) { throw null; }
        public static bool TryParse([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] string? value, System.Globalization.NumberStyles style, System.IFormatProvider? provider, out System.Numerics.BigInteger result) { throw null; }
        public static bool TryParse([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] string? s, System.IFormatProvider? provider, out System.Numerics.BigInteger result) { throw null; }
        public static bool TryParse([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] string? value, out System.Numerics.BigInteger result) { throw null; }
        public bool TryWriteBytes(System.Span<byte> destination, out int bytesWritten, bool isUnsigned = false, bool isBigEndian = false) { throw null; }
    }
    public readonly partial struct Complex : System.IEquatable<System.Numerics.Complex>, System.IFormattable, System.IParsable<System.Numerics.Complex>, System.ISpanFormattable, System.ISpanParsable<System.Numerics.Complex>, System.Numerics.IAdditionOperators<System.Numerics.Complex, System.Numerics.Complex, System.Numerics.Complex>, System.Numerics.IAdditiveIdentity<System.Numerics.Complex, System.Numerics.Complex>, System.Numerics.IDecrementOperators<System.Numerics.Complex>, System.Numerics.IDivisionOperators<System.Numerics.Complex, System.Numerics.Complex, System.Numerics.Complex>, System.Numerics.IEqualityOperators<System.Numerics.Complex, System.Numerics.Complex>, System.Numerics.IIncrementOperators<System.Numerics.Complex>, System.Numerics.IMultiplicativeIdentity<System.Numerics.Complex, System.Numerics.Complex>, System.Numerics.IMultiplyOperators<System.Numerics.Complex, System.Numerics.Complex, System.Numerics.Complex>, System.Numerics.INumberBase<System.Numerics.Complex>, System.Numerics.ISignedNumber<System.Numerics.Complex>, System.Numerics.ISubtractionOperators<System.Numerics.Complex, System.Numerics.Complex, System.Numerics.Complex>, System.Numerics.IUnaryNegationOperators<System.Numerics.Complex, System.Numerics.Complex>, System.Numerics.IUnaryPlusOperators<System.Numerics.Complex, System.Numerics.Complex>
    {
        private readonly int _dummyPrimitive;
        public static readonly System.Numerics.Complex ImaginaryOne;
        public static readonly System.Numerics.Complex Infinity;
        public static readonly System.Numerics.Complex NaN;
        public static readonly System.Numerics.Complex One;
        public static readonly System.Numerics.Complex Zero;
        public Complex(double real, double imaginary) { throw null; }
        public double Imaginary { get { throw null; } }
        public double Magnitude { get { throw null; } }
        public double Phase { get { throw null; } }
        public double Real { get { throw null; } }
        static System.Numerics.Complex System.Numerics.IAdditiveIdentity<System.Numerics.Complex,System.Numerics.Complex>.AdditiveIdentity { get { throw null; } }
        static System.Numerics.Complex System.Numerics.IMultiplicativeIdentity<System.Numerics.Complex,System.Numerics.Complex>.MultiplicativeIdentity { get { throw null; } }
        static System.Numerics.Complex System.Numerics.INumberBase<System.Numerics.Complex>.One { get { throw null; } }
        static int System.Numerics.INumberBase<System.Numerics.Complex>.Radix { get { throw null; } }
        static System.Numerics.Complex System.Numerics.INumberBase<System.Numerics.Complex>.Zero { get { throw null; } }
        static System.Numerics.Complex System.Numerics.ISignedNumber<System.Numerics.Complex>.NegativeOne { get { throw null; } }
        public static double Abs(System.Numerics.Complex value) { throw null; }
        public static System.Numerics.Complex Acos(System.Numerics.Complex value) { throw null; }
        public static System.Numerics.Complex Add(double left, System.Numerics.Complex right) { throw null; }
        public static System.Numerics.Complex Add(System.Numerics.Complex left, double right) { throw null; }
        public static System.Numerics.Complex Add(System.Numerics.Complex left, System.Numerics.Complex right) { throw null; }
        public static System.Numerics.Complex Asin(System.Numerics.Complex value) { throw null; }
        public static System.Numerics.Complex Atan(System.Numerics.Complex value) { throw null; }
        public static System.Numerics.Complex Conjugate(System.Numerics.Complex value) { throw null; }
        public static System.Numerics.Complex Cos(System.Numerics.Complex value) { throw null; }
        public static System.Numerics.Complex Cosh(System.Numerics.Complex value) { throw null; }
        public static System.Numerics.Complex Divide(double dividend, System.Numerics.Complex divisor) { throw null; }
        public static System.Numerics.Complex Divide(System.Numerics.Complex dividend, double divisor) { throw null; }
        public static System.Numerics.Complex Divide(System.Numerics.Complex dividend, System.Numerics.Complex divisor) { throw null; }
        public bool Equals(System.Numerics.Complex value) { throw null; }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public static System.Numerics.Complex Exp(System.Numerics.Complex value) { throw null; }
        public static System.Numerics.Complex FromPolarCoordinates(double magnitude, double phase) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool IsComplexNumber(System.Numerics.Complex value) { throw null; }
        public static bool IsEvenInteger(System.Numerics.Complex value) { throw null; }
        public static bool IsFinite(System.Numerics.Complex value) { throw null; }
        public static bool IsImaginaryNumber(System.Numerics.Complex value) { throw null; }
        public static bool IsInfinity(System.Numerics.Complex value) { throw null; }
        public static bool IsInteger(System.Numerics.Complex value) { throw null; }
        public static bool IsNaN(System.Numerics.Complex value) { throw null; }
        public static bool IsNegative(System.Numerics.Complex value) { throw null; }
        public static bool IsNegativeInfinity(System.Numerics.Complex value) { throw null; }
        public static bool IsNormal(System.Numerics.Complex value) { throw null; }
        public static bool IsOddInteger(System.Numerics.Complex value) { throw null; }
        public static bool IsPositive(System.Numerics.Complex value) { throw null; }
        public static bool IsPositiveInfinity(System.Numerics.Complex value) { throw null; }
        public static bool IsRealNumber(System.Numerics.Complex value) { throw null; }
        public static bool IsSubnormal(System.Numerics.Complex value) { throw null; }
        public static System.Numerics.Complex Log(System.Numerics.Complex value) { throw null; }
        public static System.Numerics.Complex Log(System.Numerics.Complex value, double baseValue) { throw null; }
        public static System.Numerics.Complex Log10(System.Numerics.Complex value) { throw null; }
        public static System.Numerics.Complex MaxMagnitude(System.Numerics.Complex x, System.Numerics.Complex y) { throw null; }
        public static System.Numerics.Complex MinMagnitude(System.Numerics.Complex x, System.Numerics.Complex y) { throw null; }
        public static System.Numerics.Complex Multiply(double left, System.Numerics.Complex right) { throw null; }
        public static System.Numerics.Complex Multiply(System.Numerics.Complex left, double right) { throw null; }
        public static System.Numerics.Complex Multiply(System.Numerics.Complex left, System.Numerics.Complex right) { throw null; }
        public static System.Numerics.Complex Negate(System.Numerics.Complex value) { throw null; }
        public static System.Numerics.Complex operator +(double left, System.Numerics.Complex right) { throw null; }
        public static System.Numerics.Complex operator +(System.Numerics.Complex left, double right) { throw null; }
        public static System.Numerics.Complex operator +(System.Numerics.Complex left, System.Numerics.Complex right) { throw null; }
        public static System.Numerics.Complex operator --(System.Numerics.Complex value) { throw null; }
        public static System.Numerics.Complex operator /(double left, System.Numerics.Complex right) { throw null; }
        public static System.Numerics.Complex operator /(System.Numerics.Complex left, double right) { throw null; }
        public static System.Numerics.Complex operator /(System.Numerics.Complex left, System.Numerics.Complex right) { throw null; }
        public static bool operator ==(System.Numerics.Complex left, System.Numerics.Complex right) { throw null; }
        public static explicit operator System.Numerics.Complex (decimal value) { throw null; }
        public static explicit operator System.Numerics.Complex (System.Int128 value) { throw null; }
        public static explicit operator System.Numerics.Complex (System.Numerics.BigInteger value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Numerics.Complex (System.UInt128 value) { throw null; }
        public static implicit operator System.Numerics.Complex (byte value) { throw null; }
        public static implicit operator System.Numerics.Complex (char value) { throw null; }
        public static implicit operator System.Numerics.Complex (double value) { throw null; }
        public static implicit operator System.Numerics.Complex (System.Half value) { throw null; }
        public static implicit operator System.Numerics.Complex (short value) { throw null; }
        public static implicit operator System.Numerics.Complex (int value) { throw null; }
        public static implicit operator System.Numerics.Complex (long value) { throw null; }
        public static implicit operator System.Numerics.Complex (nint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Numerics.Complex (sbyte value) { throw null; }
        public static implicit operator System.Numerics.Complex (float value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Numerics.Complex (ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Numerics.Complex (uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Numerics.Complex (ulong value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Numerics.Complex (nuint value) { throw null; }
        public static System.Numerics.Complex operator ++(System.Numerics.Complex value) { throw null; }
        public static bool operator !=(System.Numerics.Complex left, System.Numerics.Complex right) { throw null; }
        public static System.Numerics.Complex operator *(double left, System.Numerics.Complex right) { throw null; }
        public static System.Numerics.Complex operator *(System.Numerics.Complex left, double right) { throw null; }
        public static System.Numerics.Complex operator *(System.Numerics.Complex left, System.Numerics.Complex right) { throw null; }
        public static System.Numerics.Complex operator -(double left, System.Numerics.Complex right) { throw null; }
        public static System.Numerics.Complex operator -(System.Numerics.Complex left, double right) { throw null; }
        public static System.Numerics.Complex operator -(System.Numerics.Complex left, System.Numerics.Complex right) { throw null; }
        public static System.Numerics.Complex operator -(System.Numerics.Complex value) { throw null; }
        public static System.Numerics.Complex operator +(System.Numerics.Complex value) { throw null; }
        public static System.Numerics.Complex Parse(System.ReadOnlySpan<char> s, System.Globalization.NumberStyles style, System.IFormatProvider? provider) { throw null; }
        public static System.Numerics.Complex Parse(System.ReadOnlySpan<char> s, System.IFormatProvider? provider) { throw null; }
        public static System.Numerics.Complex Parse(string s, System.Globalization.NumberStyles style, System.IFormatProvider? provider) { throw null; }
        public static System.Numerics.Complex Parse(string s, System.IFormatProvider? provider) { throw null; }
        public static System.Numerics.Complex Pow(System.Numerics.Complex value, double power) { throw null; }
        public static System.Numerics.Complex Pow(System.Numerics.Complex value, System.Numerics.Complex power) { throw null; }
        public static System.Numerics.Complex Reciprocal(System.Numerics.Complex value) { throw null; }
        public static System.Numerics.Complex Sin(System.Numerics.Complex value) { throw null; }
        public static System.Numerics.Complex Sinh(System.Numerics.Complex value) { throw null; }
        public static System.Numerics.Complex Sqrt(System.Numerics.Complex value) { throw null; }
        public static System.Numerics.Complex Subtract(double left, System.Numerics.Complex right) { throw null; }
        public static System.Numerics.Complex Subtract(System.Numerics.Complex left, double right) { throw null; }
        public static System.Numerics.Complex Subtract(System.Numerics.Complex left, System.Numerics.Complex right) { throw null; }
        static System.Numerics.Complex System.Numerics.INumberBase<System.Numerics.Complex>.Abs(System.Numerics.Complex value) { throw null; }
        static bool System.Numerics.INumberBase<System.Numerics.Complex>.IsCanonical(System.Numerics.Complex value) { throw null; }
        static bool System.Numerics.INumberBase<System.Numerics.Complex>.IsZero(System.Numerics.Complex value) { throw null; }
        static System.Numerics.Complex System.Numerics.INumberBase<System.Numerics.Complex>.MaxMagnitudeNumber(System.Numerics.Complex x, System.Numerics.Complex y) { throw null; }
        static System.Numerics.Complex System.Numerics.INumberBase<System.Numerics.Complex>.MinMagnitudeNumber(System.Numerics.Complex x, System.Numerics.Complex y) { throw null; }
        static bool System.Numerics.INumberBase<System.Numerics.Complex>.TryConvertFromChecked<TOther>(TOther value, out System.Numerics.Complex result) { throw null; }
        static bool System.Numerics.INumberBase<System.Numerics.Complex>.TryConvertFromSaturating<TOther>(TOther value, out System.Numerics.Complex result) { throw null; }
        static bool System.Numerics.INumberBase<System.Numerics.Complex>.TryConvertFromTruncating<TOther>(TOther value, out System.Numerics.Complex result) { throw null; }
        static bool System.Numerics.INumberBase<System.Numerics.Complex>.TryConvertToChecked<TOther>(System.Numerics.Complex value, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out TOther result) { throw null; }
        static bool System.Numerics.INumberBase<System.Numerics.Complex>.TryConvertToSaturating<TOther>(System.Numerics.Complex value, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out TOther result) { throw null; }
        static bool System.Numerics.INumberBase<System.Numerics.Complex>.TryConvertToTruncating<TOther>(System.Numerics.Complex value, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out TOther result) { throw null; }
        public static System.Numerics.Complex Tan(System.Numerics.Complex value) { throw null; }
        public static System.Numerics.Complex Tanh(System.Numerics.Complex value) { throw null; }
        public override string ToString() { throw null; }
        public string ToString(System.IFormatProvider? provider) { throw null; }
        public string ToString([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("NumericFormat")] string? format) { throw null; }
        public string ToString([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("NumericFormat")] string? format, System.IFormatProvider? provider) { throw null; }
        public bool TryFormat(System.Span<char> destination, out int charsWritten, System.ReadOnlySpan<char> format, System.IFormatProvider? provider) { throw null; }
        public static bool TryParse(System.ReadOnlySpan<char> s, System.Globalization.NumberStyles style, System.IFormatProvider? provider, out System.Numerics.Complex result) { throw null; }
        public static bool TryParse(System.ReadOnlySpan<char> s, System.IFormatProvider? provider, out System.Numerics.Complex result) { throw null; }
        public static bool TryParse([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] string? s, System.Globalization.NumberStyles style, System.IFormatProvider? provider, out System.Numerics.Complex result) { throw null; }
        public static bool TryParse([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] string? s, System.IFormatProvider? provider, out System.Numerics.Complex result) { throw null; }
    }
}
