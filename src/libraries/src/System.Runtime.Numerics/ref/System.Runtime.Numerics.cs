// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Numerics
{
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct BigInteger : System.IComparable, System.IComparable<System.Numerics.BigInteger>, System.IEquatable<System.Numerics.BigInteger>, System.IFormattable
    {
        [System.CLSCompliantAttribute(false)]
        public BigInteger(byte[] value) { throw new System.NotImplementedException(); }
        public BigInteger(decimal value) { throw new System.NotImplementedException(); }
        public BigInteger(double value) { throw new System.NotImplementedException(); }
        public BigInteger(int value) { throw new System.NotImplementedException(); }
        public BigInteger(long value) { throw new System.NotImplementedException(); }
        public BigInteger(float value) { throw new System.NotImplementedException(); }
        [System.CLSCompliantAttribute(false)]
        public BigInteger(uint value) { throw new System.NotImplementedException(); }
        [System.CLSCompliantAttribute(false)]
        public BigInteger(ulong value) { throw new System.NotImplementedException(); }
        public bool IsEven { get { return default(bool); } }
        public bool IsOne { get { return default(bool); } }
        public bool IsPowerOfTwo { get { return default(bool); } }
        public bool IsZero { get { return default(bool); } }
        public static System.Numerics.BigInteger MinusOne { get { return default(System.Numerics.BigInteger); } }
        public static System.Numerics.BigInteger One { get { return default(System.Numerics.BigInteger); } }
        public int Sign { get { return default(int); } }
        public static System.Numerics.BigInteger Zero { get { return default(System.Numerics.BigInteger); } }
        public static System.Numerics.BigInteger Abs(System.Numerics.BigInteger value) { return default(System.Numerics.BigInteger); }
        public static System.Numerics.BigInteger Add(System.Numerics.BigInteger left, System.Numerics.BigInteger right) { return default(System.Numerics.BigInteger); }
        public static int Compare(System.Numerics.BigInteger left, System.Numerics.BigInteger right) { return default(int); }
        public int CompareTo(long other) { return default(int); }
        public int CompareTo(System.Numerics.BigInteger other) { return default(int); }
        [System.CLSCompliantAttribute(false)]
        public int CompareTo(ulong other) { return default(int); }
        public static System.Numerics.BigInteger Divide(System.Numerics.BigInteger dividend, System.Numerics.BigInteger divisor) { return default(System.Numerics.BigInteger); }
        public static System.Numerics.BigInteger DivRem(System.Numerics.BigInteger dividend, System.Numerics.BigInteger divisor, out System.Numerics.BigInteger remainder) { remainder = default(System.Numerics.BigInteger); return default(System.Numerics.BigInteger); }
        public bool Equals(long other) { return default(bool); }
        public bool Equals(System.Numerics.BigInteger other) { return default(bool); }
        public override bool Equals(object obj) { return default(bool); }
        [System.CLSCompliantAttribute(false)]
        public bool Equals(ulong other) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static System.Numerics.BigInteger GreatestCommonDivisor(System.Numerics.BigInteger left, System.Numerics.BigInteger right) { return default(System.Numerics.BigInteger); }
        public static double Log(System.Numerics.BigInteger value) { return default(double); }
        public static double Log(System.Numerics.BigInteger value, double baseValue) { return default(double); }
        public static double Log10(System.Numerics.BigInteger value) { return default(double); }
        public static System.Numerics.BigInteger Max(System.Numerics.BigInteger left, System.Numerics.BigInteger right) { return default(System.Numerics.BigInteger); }
        public static System.Numerics.BigInteger Min(System.Numerics.BigInteger left, System.Numerics.BigInteger right) { return default(System.Numerics.BigInteger); }
        public static System.Numerics.BigInteger ModPow(System.Numerics.BigInteger value, System.Numerics.BigInteger exponent, System.Numerics.BigInteger modulus) { return default(System.Numerics.BigInteger); }
        public static System.Numerics.BigInteger Multiply(System.Numerics.BigInteger left, System.Numerics.BigInteger right) { return default(System.Numerics.BigInteger); }
        public static System.Numerics.BigInteger Negate(System.Numerics.BigInteger value) { return default(System.Numerics.BigInteger); }
        public static System.Numerics.BigInteger operator +(System.Numerics.BigInteger left, System.Numerics.BigInteger right) { return default(System.Numerics.BigInteger); }
        public static System.Numerics.BigInteger operator &(System.Numerics.BigInteger left, System.Numerics.BigInteger right) { return default(System.Numerics.BigInteger); }
        public static System.Numerics.BigInteger operator |(System.Numerics.BigInteger left, System.Numerics.BigInteger right) { return default(System.Numerics.BigInteger); }
        public static System.Numerics.BigInteger operator --(System.Numerics.BigInteger value) { return default(System.Numerics.BigInteger); }
        public static System.Numerics.BigInteger operator /(System.Numerics.BigInteger dividend, System.Numerics.BigInteger divisor) { return default(System.Numerics.BigInteger); }
        public static bool operator ==(long left, System.Numerics.BigInteger right) { return default(bool); }
        public static bool operator ==(System.Numerics.BigInteger left, long right) { return default(bool); }
        public static bool operator ==(System.Numerics.BigInteger left, System.Numerics.BigInteger right) { return default(bool); }
        [System.CLSCompliantAttribute(false)]
        public static bool operator ==(System.Numerics.BigInteger left, ulong right) { return default(bool); }
        [System.CLSCompliantAttribute(false)]
        public static bool operator ==(ulong left, System.Numerics.BigInteger right) { return default(bool); }
        public static System.Numerics.BigInteger operator ^(System.Numerics.BigInteger left, System.Numerics.BigInteger right) { return default(System.Numerics.BigInteger); }
        public static explicit operator System.Numerics.BigInteger(decimal value) { return default(System.Numerics.BigInteger); }
        public static explicit operator System.Numerics.BigInteger(double value) { return default(System.Numerics.BigInteger); }
        public static explicit operator byte (System.Numerics.BigInteger value) { return default(byte); }
        public static explicit operator decimal (System.Numerics.BigInteger value) { return default(decimal); }
        public static explicit operator double (System.Numerics.BigInteger value) { return default(double); }
        public static explicit operator short (System.Numerics.BigInteger value) { return default(short); }
        public static explicit operator int (System.Numerics.BigInteger value) { return default(int); }
        public static explicit operator long (System.Numerics.BigInteger value) { return default(long); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator sbyte (System.Numerics.BigInteger value) { return default(sbyte); }
        public static explicit operator float (System.Numerics.BigInteger value) { return default(float); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator ushort (System.Numerics.BigInteger value) { return default(ushort); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator uint (System.Numerics.BigInteger value) { return default(uint); }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator ulong (System.Numerics.BigInteger value) { return default(ulong); }
        public static explicit operator System.Numerics.BigInteger(float value) { return default(System.Numerics.BigInteger); }
        public static bool operator >(long left, System.Numerics.BigInteger right) { return default(bool); }
        public static bool operator >(System.Numerics.BigInteger left, long right) { return default(bool); }
        public static bool operator >(System.Numerics.BigInteger left, System.Numerics.BigInteger right) { return default(bool); }
        [System.CLSCompliantAttribute(false)]
        public static bool operator >(System.Numerics.BigInteger left, ulong right) { return default(bool); }
        [System.CLSCompliantAttribute(false)]
        public static bool operator >(ulong left, System.Numerics.BigInteger right) { return default(bool); }
        public static bool operator >=(long left, System.Numerics.BigInteger right) { return default(bool); }
        public static bool operator >=(System.Numerics.BigInteger left, long right) { return default(bool); }
        public static bool operator >=(System.Numerics.BigInteger left, System.Numerics.BigInteger right) { return default(bool); }
        [System.CLSCompliantAttribute(false)]
        public static bool operator >=(System.Numerics.BigInteger left, ulong right) { return default(bool); }
        [System.CLSCompliantAttribute(false)]
        public static bool operator >=(ulong left, System.Numerics.BigInteger right) { return default(bool); }
        public static implicit operator System.Numerics.BigInteger(byte value) { return default(System.Numerics.BigInteger); }
        public static implicit operator System.Numerics.BigInteger(short value) { return default(System.Numerics.BigInteger); }
        public static implicit operator System.Numerics.BigInteger(int value) { return default(System.Numerics.BigInteger); }
        public static implicit operator System.Numerics.BigInteger(long value) { return default(System.Numerics.BigInteger); }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Numerics.BigInteger(sbyte value) { return default(System.Numerics.BigInteger); }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Numerics.BigInteger(ushort value) { return default(System.Numerics.BigInteger); }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Numerics.BigInteger(uint value) { return default(System.Numerics.BigInteger); }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Numerics.BigInteger(ulong value) { return default(System.Numerics.BigInteger); }
        public static System.Numerics.BigInteger operator ++(System.Numerics.BigInteger value) { return default(System.Numerics.BigInteger); }
        public static bool operator !=(long left, System.Numerics.BigInteger right) { return default(bool); }
        public static bool operator !=(System.Numerics.BigInteger left, long right) { return default(bool); }
        public static bool operator !=(System.Numerics.BigInteger left, System.Numerics.BigInteger right) { return default(bool); }
        [System.CLSCompliantAttribute(false)]
        public static bool operator !=(System.Numerics.BigInteger left, ulong right) { return default(bool); }
        [System.CLSCompliantAttribute(false)]
        public static bool operator !=(ulong left, System.Numerics.BigInteger right) { return default(bool); }
        public static System.Numerics.BigInteger operator <<(System.Numerics.BigInteger value, int shift) { return default(System.Numerics.BigInteger); }
        public static bool operator <(long left, System.Numerics.BigInteger right) { return default(bool); }
        public static bool operator <(System.Numerics.BigInteger left, long right) { return default(bool); }
        public static bool operator <(System.Numerics.BigInteger left, System.Numerics.BigInteger right) { return default(bool); }
        [System.CLSCompliantAttribute(false)]
        public static bool operator <(System.Numerics.BigInteger left, ulong right) { return default(bool); }
        [System.CLSCompliantAttribute(false)]
        public static bool operator <(ulong left, System.Numerics.BigInteger right) { return default(bool); }
        public static bool operator <=(long left, System.Numerics.BigInteger right) { return default(bool); }
        public static bool operator <=(System.Numerics.BigInteger left, long right) { return default(bool); }
        public static bool operator <=(System.Numerics.BigInteger left, System.Numerics.BigInteger right) { return default(bool); }
        [System.CLSCompliantAttribute(false)]
        public static bool operator <=(System.Numerics.BigInteger left, ulong right) { return default(bool); }
        [System.CLSCompliantAttribute(false)]
        public static bool operator <=(ulong left, System.Numerics.BigInteger right) { return default(bool); }
        public static System.Numerics.BigInteger operator %(System.Numerics.BigInteger dividend, System.Numerics.BigInteger divisor) { return default(System.Numerics.BigInteger); }
        public static System.Numerics.BigInteger operator *(System.Numerics.BigInteger left, System.Numerics.BigInteger right) { return default(System.Numerics.BigInteger); }
        public static System.Numerics.BigInteger operator ~(System.Numerics.BigInteger value) { return default(System.Numerics.BigInteger); }
        public static System.Numerics.BigInteger operator >>(System.Numerics.BigInteger value, int shift) { return default(System.Numerics.BigInteger); }
        public static System.Numerics.BigInteger operator -(System.Numerics.BigInteger left, System.Numerics.BigInteger right) { return default(System.Numerics.BigInteger); }
        public static System.Numerics.BigInteger operator -(System.Numerics.BigInteger value) { return default(System.Numerics.BigInteger); }
        public static System.Numerics.BigInteger operator +(System.Numerics.BigInteger value) { return default(System.Numerics.BigInteger); }
        public static System.Numerics.BigInteger Parse(string value) { return default(System.Numerics.BigInteger); }
        public static System.Numerics.BigInteger Parse(string value, System.Globalization.NumberStyles style) { return default(System.Numerics.BigInteger); }
        public static System.Numerics.BigInteger Parse(string value, System.Globalization.NumberStyles style, System.IFormatProvider provider) { return default(System.Numerics.BigInteger); }
        public static System.Numerics.BigInteger Parse(string value, System.IFormatProvider provider) { return default(System.Numerics.BigInteger); }
        public static System.Numerics.BigInteger Pow(System.Numerics.BigInteger value, int exponent) { return default(System.Numerics.BigInteger); }
        public static System.Numerics.BigInteger Remainder(System.Numerics.BigInteger dividend, System.Numerics.BigInteger divisor) { return default(System.Numerics.BigInteger); }
        public static System.Numerics.BigInteger Subtract(System.Numerics.BigInteger left, System.Numerics.BigInteger right) { return default(System.Numerics.BigInteger); }
        int System.IComparable.CompareTo(object obj) { return default(int); }
        public byte[] ToByteArray() { return default(byte[]); }
        public override string ToString() { return default(string); }
        public string ToString(System.IFormatProvider provider) { return default(string); }
        public string ToString(string format) { return default(string); }
        public string ToString(string format, System.IFormatProvider provider) { return default(string); }
        public static bool TryParse(string value, System.Globalization.NumberStyles style, System.IFormatProvider provider, out System.Numerics.BigInteger result) { result = default(System.Numerics.BigInteger); return default(bool); }
        public static bool TryParse(string value, out System.Numerics.BigInteger result) { result = default(System.Numerics.BigInteger); return default(bool); }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct Complex : System.IEquatable<System.Numerics.Complex>, System.IFormattable
    {
        public static readonly System.Numerics.Complex ImaginaryOne;
        public static readonly System.Numerics.Complex One;
        public static readonly System.Numerics.Complex Zero;
        public Complex(double real, double imaginary) { throw new System.NotImplementedException(); }
        public double Imaginary { get { return default(double); } }
        public double Magnitude { get { return default(double); } }
        public double Phase { get { return default(double); } }
        public double Real { get { return default(double); } }
        public static double Abs(System.Numerics.Complex value) { return default(double); }
        public static System.Numerics.Complex Acos(System.Numerics.Complex value) { return default(System.Numerics.Complex); }
        public static System.Numerics.Complex Add(System.Numerics.Complex left, System.Numerics.Complex right) { return default(System.Numerics.Complex); }
        public static System.Numerics.Complex Asin(System.Numerics.Complex value) { return default(System.Numerics.Complex); }
        public static System.Numerics.Complex Atan(System.Numerics.Complex value) { return default(System.Numerics.Complex); }
        public static System.Numerics.Complex Conjugate(System.Numerics.Complex value) { return default(System.Numerics.Complex); }
        public static System.Numerics.Complex Cos(System.Numerics.Complex value) { return default(System.Numerics.Complex); }
        public static System.Numerics.Complex Cosh(System.Numerics.Complex value) { return default(System.Numerics.Complex); }
        public static System.Numerics.Complex Divide(System.Numerics.Complex dividend, System.Numerics.Complex divisor) { return default(System.Numerics.Complex); }
        public bool Equals(System.Numerics.Complex value) { return default(bool); }
        public override bool Equals(object obj) { return default(bool); }
        public static System.Numerics.Complex Exp(System.Numerics.Complex value) { return default(System.Numerics.Complex); }
        public static System.Numerics.Complex FromPolarCoordinates(double magnitude, double phase) { return default(System.Numerics.Complex); }
        public override int GetHashCode() { return default(int); }
        public static System.Numerics.Complex Log(System.Numerics.Complex value) { return default(System.Numerics.Complex); }
        public static System.Numerics.Complex Log(System.Numerics.Complex value, double baseValue) { return default(System.Numerics.Complex); }
        public static System.Numerics.Complex Log10(System.Numerics.Complex value) { return default(System.Numerics.Complex); }
        public static System.Numerics.Complex Multiply(System.Numerics.Complex left, System.Numerics.Complex right) { return default(System.Numerics.Complex); }
        public static System.Numerics.Complex Negate(System.Numerics.Complex value) { return default(System.Numerics.Complex); }
        public static System.Numerics.Complex operator +(System.Numerics.Complex left, System.Numerics.Complex right) { return default(System.Numerics.Complex); }
        public static System.Numerics.Complex operator /(System.Numerics.Complex left, System.Numerics.Complex right) { return default(System.Numerics.Complex); }
        public static bool operator ==(System.Numerics.Complex left, System.Numerics.Complex right) { return default(bool); }
        public static explicit operator System.Numerics.Complex(decimal value) { return default(System.Numerics.Complex); }
        public static explicit operator System.Numerics.Complex(System.Numerics.BigInteger value) { return default(System.Numerics.Complex); }
        public static implicit operator System.Numerics.Complex(byte value) { return default(System.Numerics.Complex); }
        public static implicit operator System.Numerics.Complex(double value) { return default(System.Numerics.Complex); }
        public static implicit operator System.Numerics.Complex(short value) { return default(System.Numerics.Complex); }
        public static implicit operator System.Numerics.Complex(int value) { return default(System.Numerics.Complex); }
        public static implicit operator System.Numerics.Complex(long value) { return default(System.Numerics.Complex); }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Numerics.Complex(sbyte value) { return default(System.Numerics.Complex); }
        public static implicit operator System.Numerics.Complex(float value) { return default(System.Numerics.Complex); }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Numerics.Complex(ushort value) { return default(System.Numerics.Complex); }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Numerics.Complex(uint value) { return default(System.Numerics.Complex); }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator System.Numerics.Complex(ulong value) { return default(System.Numerics.Complex); }
        public static bool operator !=(System.Numerics.Complex left, System.Numerics.Complex right) { return default(bool); }
        public static System.Numerics.Complex operator *(System.Numerics.Complex left, System.Numerics.Complex right) { return default(System.Numerics.Complex); }
        public static System.Numerics.Complex operator -(System.Numerics.Complex left, System.Numerics.Complex right) { return default(System.Numerics.Complex); }
        public static System.Numerics.Complex operator -(System.Numerics.Complex value) { return default(System.Numerics.Complex); }
        public static System.Numerics.Complex Pow(System.Numerics.Complex value, double power) { return default(System.Numerics.Complex); }
        public static System.Numerics.Complex Pow(System.Numerics.Complex value, System.Numerics.Complex power) { return default(System.Numerics.Complex); }
        public static System.Numerics.Complex Reciprocal(System.Numerics.Complex value) { return default(System.Numerics.Complex); }
        public static System.Numerics.Complex Sin(System.Numerics.Complex value) { return default(System.Numerics.Complex); }
        public static System.Numerics.Complex Sinh(System.Numerics.Complex value) { return default(System.Numerics.Complex); }
        public static System.Numerics.Complex Sqrt(System.Numerics.Complex value) { return default(System.Numerics.Complex); }
        public static System.Numerics.Complex Subtract(System.Numerics.Complex left, System.Numerics.Complex right) { return default(System.Numerics.Complex); }
        public static System.Numerics.Complex Tan(System.Numerics.Complex value) { return default(System.Numerics.Complex); }
        public static System.Numerics.Complex Tanh(System.Numerics.Complex value) { return default(System.Numerics.Complex); }
        public override string ToString() { return default(string); }
        public string ToString(System.IFormatProvider provider) { return default(string); }
        public string ToString(string format) { return default(string); }
        public string ToString(string format, System.IFormatProvider provider) { return default(string); }
    }
}
