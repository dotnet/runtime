// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;

namespace System.Numerics
{
    [Serializable]
    public struct BigInteger : IFormattable, IComparable, IComparable<BigInteger>, IEquatable<BigInteger>
    {
        private const int knMaskHighBit = int.MinValue;
        private const uint kuMaskHighBit = unchecked((uint)int.MinValue);
        private const int kcbitUint = 32;
        private const int kcbitUlong = 64;
        private const int DecimalScaleFactorMask = 0x00FF0000;
        private const int DecimalSignMask = unchecked((int)0x80000000);

        // For values int.MinValue < n <= int.MaxValue, the value is stored in sign
        // and _bits is null. For all other values, sign is +1 or -1 and the bits are in _bits
        internal readonly int _sign;
        internal readonly uint[] _bits;

        // We have to make a choice of how to represent int.MinValue. This is the one
        // value that fits in an int, but whose negation does not fit in an int.
        // We choose to use a large representation, so we're symmetric with respect to negation.
        private static readonly BigInteger s_bnMinInt = new BigInteger(-1, new uint[] { kuMaskHighBit });
        private static readonly BigInteger s_bnOneInt = new BigInteger(1);
        private static readonly BigInteger s_bnZeroInt = new BigInteger(0);
        private static readonly BigInteger s_bnMinusOneInt = new BigInteger(-1);

        public BigInteger(int value)
        {
            if (value == int.MinValue)
                this = s_bnMinInt;
            else
            {
                _sign = value;
                _bits = null;
            }
            AssertValid();
        }

        [CLSCompliant(false)]
        public BigInteger(uint value)
        {
            if (value <= int.MaxValue)
            {
                _sign = (int)value;
                _bits = null;
            }
            else
            {
                _sign = +1;
                _bits = new uint[1];
                _bits[0] = value;
            }
            AssertValid();
        }

        public BigInteger(long value)
        {
            if (int.MinValue < value && value <= int.MaxValue)
            {
                _sign = (int)value;
                _bits = null;
            }
            else if (value == int.MinValue)
            {
                this = s_bnMinInt;
            }
            else
            {
                ulong x = 0;
                if (value < 0)
                {
                    x = (ulong)-value;
                    _sign = -1;
                }
                else
                {
                    x = (ulong)value;
                    _sign = +1;
                }

                if (x <= uint.MaxValue)
                {
                    _bits = new uint[1];
                    _bits[0] = (uint)x;
                }
                else
                {
                    _bits = new uint[2];
                    _bits[0] = (uint)x;
                    _bits[1] = (uint)(x >> kcbitUint);
                }
            }

            AssertValid();
        }

        [CLSCompliant(false)]
        public BigInteger(ulong value)
        {
            if (value <= int.MaxValue)
            {
                _sign = (int)value;
                _bits = null;
            }
            else if (value <= uint.MaxValue)
            {
                _sign = +1;
                _bits = new uint[1];
                _bits[0] = (uint)value;
            }
            else
            {
                _sign = +1;
                _bits = new uint[2];
                _bits[0] = (uint)value;
                _bits[1] = (uint)(value >> kcbitUint);
            }

            AssertValid();
        }

        public BigInteger(float value) : this((double)value)
        {
        }

        public BigInteger(double value)
        {
            if (double.IsInfinity(value))
                throw new OverflowException(SR.Overflow_BigIntInfinity);
            if (double.IsNaN(value))
                throw new OverflowException(SR.Overflow_NotANumber);
            Contract.EndContractBlock();

            _sign = 0;
            _bits = null;

            int sign, exp;
            ulong man;
            bool fFinite;
            NumericsHelpers.GetDoubleParts(value, out sign, out exp, out man, out fFinite);
            Debug.Assert(sign == +1 || sign == -1);

            if (man == 0)
            {
                this = Zero;
                return;
            }

            Debug.Assert(man < (1UL << 53));
            Debug.Assert(exp <= 0 || man >= (1UL << 52));

            if (exp <= 0)
            {
                if (exp <= -kcbitUlong)
                {
                    this = Zero;
                    return;
                }
                this = man >> -exp;
                if (sign < 0)
                    _sign = -_sign;
            }
            else if (exp <= 11)
            {
                this = man << exp;
                if (sign < 0)
                    _sign = -_sign;
            }
            else
            {
                // Overflow into at least 3 uints.
                // Move the leading 1 to the high bit.
                man <<= 11;
                exp -= 11;

                // Compute cu and cbit so that exp == 32 * cu - cbit and 0 <= cbit < 32.
                int cu = (exp - 1) / kcbitUint + 1;
                int cbit = cu * kcbitUint - exp;
                Debug.Assert(0 <= cbit && cbit < kcbitUint);
                Debug.Assert(cu >= 1);

                // Populate the uints.
                _bits = new uint[cu + 2];
                _bits[cu + 1] = (uint)(man >> (cbit + kcbitUint));
                _bits[cu] = (uint)(man >> cbit);
                if (cbit > 0)
                    _bits[cu - 1] = (uint)man << (kcbitUint - cbit);
                _sign = sign;
            }

            AssertValid();
        }

        public BigInteger(decimal value)
        {
            // First truncate to get scale to 0 and extract bits
            int[] bits = decimal.GetBits(decimal.Truncate(value));

            Debug.Assert(bits.Length == 4 && (bits[3] & DecimalScaleFactorMask) == 0);

            int size = 3;
            while (size > 0 && bits[size - 1] == 0)
                size--;
            if (size == 0)
            {
                this = s_bnZeroInt;
            }
            else if (size == 1 && bits[0] > 0)
            {
                // bits[0] is the absolute value of this decimal
                // if bits[0] < 0 then it is too large to be packed into _sign
                _sign = bits[0];
                _sign *= ((bits[3] & DecimalSignMask) != 0) ? -1 : +1;
                _bits = null;
            }
            else
            {
                _bits = new uint[size];
                _bits[0] = (uint)bits[0];
                if (size > 1)
                    _bits[1] = (uint)bits[1];
                if (size > 2)
                    _bits[2] = (uint)bits[2];
                _sign = ((bits[3] & DecimalSignMask) != 0) ? -1 : +1;
            }
            AssertValid();
        }

        /// <summary>
        /// Creates a BigInteger from a little-endian twos-complement byte array.
        /// </summary>
        /// <param name="value"></param>
        [CLSCompliant(false)]
        public BigInteger(byte[] value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            Contract.EndContractBlock();

            int byteCount = value.Length;
            bool isNegative = byteCount > 0 && ((value[byteCount - 1] & 0x80) == 0x80);

            // Try to conserve space as much as possible by checking for wasted leading byte[] entries 
            while (byteCount > 0 && value[byteCount - 1] == 0) byteCount--;

            if (byteCount == 0)
            {
                // BigInteger.Zero
                _sign = 0;
                _bits = null;
                AssertValid();
                return;
            }

            if (byteCount <= 4)
            {
                if (isNegative)
                    _sign = unchecked((int)0xffffffff);
                else
                    _sign = 0;
                for (int i = byteCount - 1; i >= 0; i--)
                {
                    _sign <<= 8;
                    _sign |= value[i];
                }
                _bits = null;

                if (_sign < 0 && !isNegative)
                {
                    // Int32 overflow
                    // Example: Int64 value 2362232011 (0xCB, 0xCC, 0xCC, 0x8C, 0x0)
                    // can be naively packed into 4 bytes (due to the leading 0x0)
                    // it overflows into the int32 sign bit
                    _bits = new uint[1];
                    _bits[0] = (uint)_sign;
                    _sign = +1;
                }
                if (_sign == int.MinValue)
                    this = s_bnMinInt;
            }
            else
            {
                int unalignedBytes = byteCount % 4;
                int dwordCount = byteCount / 4 + (unalignedBytes == 0 ? 0 : 1);
                bool isZero = true;
                uint[] val = new uint[dwordCount];

                // Copy all dwords, except but don't do the last one if it's not a full four bytes
                int curDword, curByte, byteInDword;
                curByte = 3;
                for (curDword = 0; curDword < dwordCount - (unalignedBytes == 0 ? 0 : 1); curDword++)
                {
                    byteInDword = 0;
                    while (byteInDword < 4)
                    {
                        if (value[curByte] != 0x00) isZero = false;
                        val[curDword] <<= 8;
                        val[curDword] |= value[curByte];
                        curByte--;
                        byteInDword++;
                    }
                    curByte += 8;
                }

                // Copy the last dword specially if it's not aligned
                if (unalignedBytes != 0)
                {
                    if (isNegative) val[dwordCount - 1] = 0xffffffff;
                    for (curByte = byteCount - 1; curByte >= byteCount - unalignedBytes; curByte--)
                    {
                        if (value[curByte] != 0x00) isZero = false;
                        val[curDword] <<= 8;
                        val[curDword] |= value[curByte];
                    }
                }

                if (isZero)
                {
                    this = s_bnZeroInt;
                }
                else if (isNegative)
                {
                    NumericsHelpers.DangerousMakeTwosComplement(val); // Mutates val

                    // Pack _bits to remove any wasted space after the twos complement
                    int len = val.Length;
                    while (len > 0 && val[len - 1] == 0)
                        len--;
                    if (len == 1 && ((int)(val[0])) > 0)
                    {
                        if (val[0] == 1 /* abs(-1) */)
                        {
                            this = s_bnMinusOneInt;
                        }
                        else if (val[0] == kuMaskHighBit /* abs(Int32.MinValue) */)
                        {
                            this = s_bnMinInt;
                        }
                        else
                        {
                            _sign = (-1) * ((int)val[0]);
                            _bits = null;
                        }
                    }
                    else if (len != val.Length)
                    {
                        _sign = -1;
                        _bits = new uint[len];
                        Array.Copy(val, 0, _bits, 0, len);
                    }
                    else
                    {
                        _sign = -1;
                        _bits = val;
                    }
                }
                else
                {
                    _sign = +1;
                    _bits = val;
                }
            }
            AssertValid();
        }

        internal BigInteger(int n, uint[] rgu)
        {
            _sign = n;
            _bits = rgu;
            AssertValid();
        }
        
        /// <summary>
        /// Constructor used during bit manipulation and arithmetic.
        /// When possible the uint[] will be packed into  _sign to conserve space.
        /// </summary>
        /// <param name="value">The absolute value of the number</param>
        /// <param name="negative">The bool indicating the sign of the value.</param>
        internal BigInteger(uint[] value, bool negative)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            Contract.EndContractBlock();

            int len;

            // Try to conserve space as much as possible by checking for wasted leading uint[] entries 
            // sometimes the uint[] has leading zeros from bit manipulation operations & and ^
            for (len = value.Length; len > 0 && value[len - 1] == 0; len--) ;

            if (len == 0)
                this = s_bnZeroInt;
            // Values like (Int32.MaxValue+1) are stored as "0x80000000" and as such cannot be packed into _sign
            else if (len == 1 && value[0] < kuMaskHighBit)
            {
                _sign = (negative ? -(int)value[0] : (int)value[0]);
                _bits = null;
                // Although Int32.MinValue fits in _sign, we represent this case differently for negate
                if (_sign == int.MinValue)
                    this = s_bnMinInt;
            }
            else
            {
                _sign = negative ? -1 : +1;
                _bits = new uint[len];
                Array.Copy(value, 0, _bits, 0, len);
            }
            AssertValid();
        }

        /// <summary>
        /// Create a BigInteger from a little-endian twos-complement UInt32 array.
        /// When possible, value is assigned directly to this._bits without an array copy
        /// so use this ctor with care.
        /// </summary>
        /// <param name="value"></param>
        private BigInteger(uint[] value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            int dwordCount = value.Length;
            bool isNegative = dwordCount > 0 && ((value[dwordCount - 1] & 0x80000000) == 0x80000000);

            // Try to conserve space as much as possible by checking for wasted leading uint[] entries 
            while (dwordCount > 0 && value[dwordCount - 1] == 0) dwordCount--;

            if (dwordCount == 0)
            {
                // BigInteger.Zero
                this = s_bnZeroInt;
                AssertValid();
                return;
            }
            if (dwordCount == 1)
            {
                if ((int)value[0] < 0 && !isNegative)
                {
                    _bits = new uint[1];
                    _bits[0] = value[0];
                    _sign = +1;
                }
                // Handle the special cases where the BigInteger likely fits into _sign
                else if (int.MinValue == (int)value[0])
                {
                    this = s_bnMinInt;
                }
                else
                {
                    _sign = (int)value[0];
                    _bits = null;
                }
                AssertValid();
                return;
            }

            if (!isNegative)
            {
                // Handle the simple positive value cases where the input is already in sign magnitude
                if (dwordCount != value.Length)
                {
                    _sign = +1;
                    _bits = new uint[dwordCount];
                    Array.Copy(value, 0, _bits, 0, dwordCount);
                }
                // No trimming is possible.  Assign value directly to _bits.  
                else
                {
                    _sign = +1;
                    _bits = value;
                }
                AssertValid();
                return;
            }

            // Finally handle the more complex cases where we must transform the input into sign magnitude
            NumericsHelpers.DangerousMakeTwosComplement(value); // mutates val

            // Pack _bits to remove any wasted space after the twos complement
            int len = value.Length;
            while (len > 0 && value[len - 1] == 0) len--;

            // The number is represented by a single dword
            if (len == 1 && ((int)(value[0])) > 0)
            {
                if (value[0] == 1 /* abs(-1) */)
                {
                    this = s_bnMinusOneInt;
                }
                else if (value[0] == kuMaskHighBit /* abs(Int32.MinValue) */)
                {
                    this = s_bnMinInt;
                }
                else
                {
                    _sign = (-1) * ((int)value[0]);
                    _bits = null;
                }
            }
            // The number is represented by multiple dwords.
            // Trim off any wasted uint values when possible.
            else if (len != value.Length)
            {
                _sign = -1;
                _bits = new uint[len];
                Array.Copy(value, 0, _bits, 0, len);
            }
            // No trimming is possible.  Assign value directly to _bits.  
            else
            {
                _sign = -1;
                _bits = value;
            }
            AssertValid();
            return;
        }
        
        public static BigInteger Zero { get { return s_bnZeroInt; } }

        public static BigInteger One { get { return s_bnOneInt; } }

        public static BigInteger MinusOne { get { return s_bnMinusOneInt; } }

        public bool IsPowerOfTwo
        {
            get
            {
                AssertValid();

                if (_bits == null)
                    return (_sign & (_sign - 1)) == 0 && _sign != 0;

                if (_sign != 1)
                    return false;
                int iu = _bits.Length - 1;
                if ((_bits[iu] & (_bits[iu] - 1)) != 0)
                    return false;
                while (--iu >= 0)
                {
                    if (_bits[iu] != 0)
                        return false;
                }
                return true;
            }
        }

        public bool IsZero { get { AssertValid(); return _sign == 0; } }

        public bool IsOne { get { AssertValid(); return _sign == 1 && _bits == null; } }

        public bool IsEven { get { AssertValid(); return _bits == null ? (_sign & 1) == 0 : (_bits[0] & 1) == 0; } }

        public int Sign
        {
            get { AssertValid(); return (_sign >> (kcbitUint - 1)) - (-_sign >> (kcbitUint - 1)); }
        }

        public static BigInteger Parse(string value)
        {
            return Parse(value, NumberStyles.Integer);
        }

        public static BigInteger Parse(string value, NumberStyles style)
        {
            return Parse(value, style, NumberFormatInfo.CurrentInfo);
        }

        public static BigInteger Parse(string value, IFormatProvider provider)
        {
            return Parse(value, NumberStyles.Integer, NumberFormatInfo.GetInstance(provider));
        }

        public static BigInteger Parse(string value, NumberStyles style, IFormatProvider provider)
        {
            return BigNumber.ParseBigInteger(value, style, NumberFormatInfo.GetInstance(provider));
        }

        public static bool TryParse(string value, out BigInteger result)
        {
            return TryParse(value, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
        }

        public static bool TryParse(string value, NumberStyles style, IFormatProvider provider, out BigInteger result)
        {
            return BigNumber.TryParseBigInteger(value, style, NumberFormatInfo.GetInstance(provider), out result);
        }

        public static int Compare(BigInteger left, BigInteger right)
        {
            return left.CompareTo(right);
        }

        public static BigInteger Abs(BigInteger value)
        {
            return (value >= Zero) ? value : -value;
        }

        public static BigInteger Add(BigInteger left, BigInteger right)
        {
            return left + right;
        }

        public static BigInteger Subtract(BigInteger left, BigInteger right)
        {
            return left - right;
        }

        public static BigInteger Multiply(BigInteger left, BigInteger right)
        {
            return left * right;
        }

        public static BigInteger Divide(BigInteger dividend, BigInteger divisor)
        {
            return dividend / divisor;
        }

        public static BigInteger Remainder(BigInteger dividend, BigInteger divisor)
        {
            return dividend % divisor;
        }

        public static BigInteger DivRem(BigInteger dividend, BigInteger divisor, out BigInteger remainder)
        {
            dividend.AssertValid();
            divisor.AssertValid();

            bool trivialDividend = dividend._bits == null;
            bool trivialDivisor = divisor._bits == null;

            if (trivialDividend && trivialDivisor)
            {
                remainder = dividend._sign % divisor._sign;
                return dividend._sign / divisor._sign;
            }

            if (trivialDividend)
            {
                // The divisor is non-trivial
                // and therefore the bigger one
                remainder = dividend;
                return s_bnZeroInt;
            }

            if (trivialDivisor)
            {
                uint rest;
                uint[] bits = BigIntegerCalculator.Divide(dividend._bits, NumericsHelpers.Abs(divisor._sign), out rest);

                remainder = dividend._sign < 0 ? -1 * rest : rest;
                return new BigInteger(bits, (dividend._sign < 0) ^ (divisor._sign < 0));
            }

            if (dividend._bits.Length < divisor._bits.Length)
            {
                remainder = dividend;
                return s_bnZeroInt;
            }
            else
            {
                uint[] rest;
                uint[] bits = BigIntegerCalculator.Divide(dividend._bits, divisor._bits, out rest);

                remainder = new BigInteger(rest, dividend._sign < 0);
                return new BigInteger(bits, (dividend._sign < 0) ^ (divisor._sign < 0));
            }
        }

        public static BigInteger Negate(BigInteger value)
        {
            return -value;
        }
        
        public static double Log(BigInteger value)
        {
            return Log(value, Math.E);
        }

        public static double Log(BigInteger value, double baseValue)
        {
            if (value._sign < 0 || baseValue == 1.0D)
                return double.NaN;
            if (baseValue == double.PositiveInfinity)
                return value.IsOne ? 0.0D : double.NaN;
            if (baseValue == 0.0D && !value.IsOne)
                return double.NaN;
            if (value._bits == null)
                return Math.Log(value._sign, baseValue);

            ulong h = value._bits[value._bits.Length - 1];
            ulong m = value._bits.Length > 1 ? value._bits[value._bits.Length - 2] : 0;
            ulong l = value._bits.Length > 2 ? value._bits[value._bits.Length - 3] : 0;

            // Measure the exact bit count
            int c = NumericsHelpers.CbitHighZero((uint)h);
            long b = (long)value._bits.Length * 32 - c;

            // Extract most significant bits
            ulong x = (h << 32 + c) | (m << c) | (l >> 32 - c);

            // Let v = value, b = bit count, x = v/2^b-64
            // log ( v/2^b-64 * 2^b-64 ) = log ( x ) + log ( 2^b-64 )
            return Math.Log(x, baseValue) + (b - 64) / Math.Log(baseValue, 2);
        }

        public static double Log10(BigInteger value)
        {
            return Log(value, 10);
        }

        public static BigInteger GreatestCommonDivisor(BigInteger left, BigInteger right)
        {
            left.AssertValid();
            right.AssertValid();

            bool trivialLeft = left._bits == null;
            bool trivialRight = right._bits == null;

            if (trivialLeft && trivialRight)
            {
                return BigIntegerCalculator.Gcd(NumericsHelpers.Abs(left._sign), NumericsHelpers.Abs(right._sign));
            }

            if (trivialLeft)
            {
                return left._sign != 0
                    ? BigIntegerCalculator.Gcd(right._bits, NumericsHelpers.Abs(left._sign))
                    : new BigInteger(right._bits, false);
            }

            if (trivialRight)
            {
                return right._sign != 0
                    ? BigIntegerCalculator.Gcd(left._bits, NumericsHelpers.Abs(right._sign))
                    : new BigInteger(left._bits, false);
            }

            if (BigIntegerCalculator.Compare(left._bits, right._bits) < 0)
            {
                return GreatestCommonDivisor(right._bits, left._bits);
            }
            else
            {
                return GreatestCommonDivisor(left._bits, right._bits);
            }
        }

        private static BigInteger GreatestCommonDivisor(uint[] leftBits, uint[] rightBits)
        {
            Debug.Assert(BigIntegerCalculator.Compare(leftBits, rightBits) >= 0);

            // Short circuits to spare some allocations...
            if (rightBits.Length == 1)
            {
                uint temp = BigIntegerCalculator.Remainder(leftBits, rightBits[0]);
                return BigIntegerCalculator.Gcd(rightBits[0], temp);
            }

            if (rightBits.Length == 2)
            {
                uint[] tempBits = BigIntegerCalculator.Remainder(leftBits, rightBits);

                ulong left = ((ulong)rightBits[1] << 32) | rightBits[0];
                ulong right = ((ulong)tempBits[1] << 32) | tempBits[0];

                return BigIntegerCalculator.Gcd(left, right);
            }

            uint[] bits = BigIntegerCalculator.Gcd(leftBits, rightBits);
            return new BigInteger(bits, false);
        }

        public static BigInteger Max(BigInteger left, BigInteger right)
        {
            if (left.CompareTo(right) < 0)
                return right;
            return left;
        }

        public static BigInteger Min(BigInteger left, BigInteger right)
        {
            if (left.CompareTo(right) <= 0)
                return left;
            return right;
        }

        public static BigInteger ModPow(BigInteger value, BigInteger exponent, BigInteger modulus)
        {
            if (exponent.Sign < 0)
                throw new ArgumentOutOfRangeException(nameof(exponent), SR.ArgumentOutOfRange_MustBeNonNeg);
            Contract.EndContractBlock();

            value.AssertValid();
            exponent.AssertValid();
            modulus.AssertValid();

            bool trivialValue = value._bits == null;
            bool trivialExponent = exponent._bits == null;
            bool trivialModulus = modulus._bits == null;

            if (trivialModulus)
            {
                uint bits = trivialValue && trivialExponent ? BigIntegerCalculator.Pow(NumericsHelpers.Abs(value._sign), NumericsHelpers.Abs(exponent._sign), NumericsHelpers.Abs(modulus._sign)) :
                            trivialValue ? BigIntegerCalculator.Pow(NumericsHelpers.Abs(value._sign), exponent._bits, NumericsHelpers.Abs(modulus._sign)) :
                            trivialExponent ? BigIntegerCalculator.Pow(value._bits, NumericsHelpers.Abs(exponent._sign), NumericsHelpers.Abs(modulus._sign)) :
                            BigIntegerCalculator.Pow(value._bits, exponent._bits, NumericsHelpers.Abs(modulus._sign));

                return value._sign < 0 && !exponent.IsEven ? -1 * bits : bits;
            }
            else
            {
                uint[] bits = trivialValue && trivialExponent ? BigIntegerCalculator.Pow(NumericsHelpers.Abs(value._sign), NumericsHelpers.Abs(exponent._sign), modulus._bits) :
                              trivialValue ? BigIntegerCalculator.Pow(NumericsHelpers.Abs(value._sign), exponent._bits, modulus._bits) :
                              trivialExponent ? BigIntegerCalculator.Pow(value._bits, NumericsHelpers.Abs(exponent._sign), modulus._bits) :
                              BigIntegerCalculator.Pow(value._bits, exponent._bits, modulus._bits);

                return new BigInteger(bits, value._sign < 0 && !exponent.IsEven);
            }
        }

        public static BigInteger Pow(BigInteger value, int exponent)
        {
            if (exponent < 0)
                throw new ArgumentOutOfRangeException(nameof(exponent), SR.ArgumentOutOfRange_MustBeNonNeg);
            Contract.EndContractBlock();

            value.AssertValid();

            if (exponent == 0)
                return s_bnOneInt;
            if (exponent == 1)
                return value;

            bool trivialValue = value._bits == null;

            if (trivialValue)
            {
                if (value._sign == 1)
                    return value;
                if (value._sign == -1)
                    return (exponent & 1) != 0 ? value : s_bnOneInt;
                if (value._sign == 0)
                    return value;
            }

            uint[] bits = trivialValue
                        ? BigIntegerCalculator.Pow(NumericsHelpers.Abs(value._sign), NumericsHelpers.Abs(exponent))
                        : BigIntegerCalculator.Pow(value._bits, NumericsHelpers.Abs(exponent));

            return new BigInteger(bits, value._sign < 0 && (exponent & 1) != 0);
        }

        public override int GetHashCode()
        {
            AssertValid();

            if (_bits == null)
                return _sign;
            int hash = _sign;
            for (int iv = _bits.Length; --iv >= 0;)
                hash = NumericsHelpers.CombineHash(hash, (int)_bits[iv]);
            return hash;
        }

        public override bool Equals(object obj)
        {
            AssertValid();

            if (!(obj is BigInteger))
                return false;
            return Equals((BigInteger)obj);
        }

        public bool Equals(long other)
        {
            AssertValid();

            if (_bits == null)
                return _sign == other;

            int cu;
            if ((_sign ^ other) < 0 || (cu = _bits.Length) > 2)
                return false;

            ulong uu = other < 0 ? (ulong)-other : (ulong)other;
            if (cu == 1)
                return _bits[0] == uu;

            return NumericsHelpers.MakeUlong(_bits[1], _bits[0]) == uu;
        }

        [CLSCompliant(false)]
        public bool Equals(ulong other)
        {
            AssertValid();

            if (_sign < 0)
                return false;
            if (_bits == null)
                return (ulong)_sign == other;

            int cu = _bits.Length;
            if (cu > 2)
                return false;
            if (cu == 1)
                return _bits[0] == other;
            return NumericsHelpers.MakeUlong(_bits[1], _bits[0]) == other;
        }

        public bool Equals(BigInteger other)
        {
            AssertValid();
            other.AssertValid();

            if (_sign != other._sign)
                return false;
            if (_bits == other._bits)
                // _sign == other._sign && _bits == null && other._bits == null
                return true;

            if (_bits == null || other._bits == null)
                return false;
            int cu = _bits.Length;
            if (cu != other._bits.Length)
                return false;
            int cuDiff = GetDiffLength(_bits, other._bits, cu);
            return cuDiff == 0;
        }

        public int CompareTo(long other)
        {
            AssertValid();

            if (_bits == null)
                return ((long)_sign).CompareTo(other);
            int cu;
            if ((_sign ^ other) < 0 || (cu = _bits.Length) > 2)
                return _sign;
            ulong uu = other < 0 ? (ulong)-other : (ulong)other;
            ulong uuTmp = cu == 2 ? NumericsHelpers.MakeUlong(_bits[1], _bits[0]) : _bits[0];
            return _sign * uuTmp.CompareTo(uu);
        }

        [CLSCompliant(false)]
        public int CompareTo(ulong other)
        {
            AssertValid();

            if (_sign < 0)
                return -1;
            if (_bits == null)
                return ((ulong)_sign).CompareTo(other);
            int cu = _bits.Length;
            if (cu > 2)
                return +1;
            ulong uuTmp = cu == 2 ? NumericsHelpers.MakeUlong(_bits[1], _bits[0]) : _bits[0];
            return uuTmp.CompareTo(other);
        }

        public int CompareTo(BigInteger other)
        {
            AssertValid();
            other.AssertValid();

            if ((_sign ^ other._sign) < 0)
            {
                // Different signs, so the comparison is easy.
                return _sign < 0 ? -1 : +1;
            }

            // Same signs
            if (_bits == null)
            {
                if (other._bits == null)
                    return _sign < other._sign ? -1 : _sign > other._sign ? +1 : 0;
                return -other._sign;
            }
            int cuThis, cuOther;
            if (other._bits == null || (cuThis = _bits.Length) > (cuOther = other._bits.Length))
                return _sign;
            if (cuThis < cuOther)
                return -_sign;

            int cuDiff = GetDiffLength(_bits, other._bits, cuThis);
            if (cuDiff == 0)
                return 0;
            return _bits[cuDiff - 1] < other._bits[cuDiff - 1] ? -_sign : _sign;
        }

        public int CompareTo(object obj)
        {
            if (obj == null)
                return 1;
            if (!(obj is BigInteger))
                throw new ArgumentException(SR.Argument_MustBeBigInt, nameof(obj));
            return CompareTo((BigInteger)obj);
        }

        /// <summary>
        /// Returns the value of this BigInteger as a little-endian twos-complement
        /// byte array, using the fewest number of bytes possible. If the value is zero,
        /// return an array of one byte whose element is 0x00.
        /// </summary>
        /// <returns></returns>
        public byte[] ToByteArray()
        {
            int sign = _sign;
            if (sign == 0)
            {
                return new byte[] { 0 };
            }

            byte highByte;
            int nonZeroDwordIndex = 0;
            uint highDword;
            uint[] bits = _bits;
            if (bits == null)
            {
                highByte = (byte)((sign < 0) ? 0xff : 0x00);
                highDword = unchecked((uint)sign);
            }
            else if (sign == -1)
            {
                highByte = 0xff;

                // If sign is -1, we will need to two's complement bits.
                // Previously this was accomplished via NumericsHelpers.DangerousMakeTwosComplement(),
                // however, we can do the two's complement on the stack so as to avoid
                // creating a temporary copy of bits just to hold the two's complement.
                // One special case in DangerousMakeTwosComplement() is that if the array
                // is all zeros, then it would allocate a new array with the high-order
                // uint set to 1 (for the carry). In our usage, we will not hit this case
                // because a bits array of all zeros would represent 0, and this case
                // would be encoded as _bits = null and _sign = 0.
                Debug.Assert(bits.Length > 0);
                Debug.Assert(bits[bits.Length - 1] != 0);
                while (bits[nonZeroDwordIndex] == 0U)
                {
                    nonZeroDwordIndex++;
                }

                highDword = ~bits[bits.Length - 1];
                if (bits.Length - 1 == nonZeroDwordIndex)
                {
                    // This will not overflow because highDword is less than or equal to uint.MaxValue - 1.
                    Debug.Assert(highDword <= uint.MaxValue - 1);
                    highDword += 1U;
                }
            }
            else
            {
                Debug.Assert(sign == 1);
                highByte = 0x00;
                highDword = bits[bits.Length - 1];
            }

            byte msb;
            int msbIndex;
            if ((msb = unchecked((byte)(highDword >> 24))) != highByte)
            {
                msbIndex = 3;
            }
            else if ((msb = unchecked((byte)(highDword >> 16))) != highByte)
            {
                msbIndex = 2;
            }
            else if ((msb = unchecked((byte)(highDword >> 8))) != highByte)
            {
                msbIndex = 1;
            }
            else
            {
                msb = unchecked((byte)highDword);
                msbIndex = 0;
            }

            // Ensure high bit is 0 if positive, 1 if negative
            bool needExtraByte = (msb & 0x80) != (highByte & 0x80);
            byte[] bytes;
            int curByte = 0;
            if (bits == null)
            {
                bytes = new byte[msbIndex + 1 + (needExtraByte ? 1 : 0)];
                Debug.Assert(bytes.Length <= 4);
            }
            else
            {
                bytes = new byte[checked(4 * (bits.Length - 1) + msbIndex + 1 + (needExtraByte ? 1 : 0))];

                for (int i = 0; i < bits.Length - 1; i++)
                {
                    uint dword = bits[i];
                    if (sign == -1)
                    {
                        dword = ~dword;
                        if (i <= nonZeroDwordIndex)
                        {
                            dword = unchecked(dword + 1U);
                        }
                    }
                    for (int j = 0; j < 4; j++)
                    {
                        bytes[curByte++] = unchecked((byte)dword);
                        dword >>= 8;
                    }
                }
            }
            for (int j = 0; j <= msbIndex; j++)
            {
                bytes[curByte++] = unchecked((byte)highDword);
                highDword >>= 8;
            }
            if (needExtraByte)
            {
                bytes[bytes.Length - 1] = highByte;
            }
            return bytes;
        }

        /// <summary>
        /// Return the value of this BigInteger as a little-endian twos-complement
        /// uint array, using the fewest number of uints possible. If the value is zero,
        /// return an array of one uint whose element is 0.
        /// </summary>
        /// <returns></returns>
        private uint[] ToUInt32Array()
        {
            if (_bits == null && _sign == 0)
                return new uint[] { 0 };

            uint[] dwords;
            uint highDWord;

            if (_bits == null)
            {
                dwords = new uint[] { (uint)_sign };
                highDWord = (_sign < 0) ? uint.MaxValue : 0;
            }
            else if (_sign == -1)
            {
                dwords = (uint[])_bits.Clone();
                NumericsHelpers.DangerousMakeTwosComplement(dwords);  // Mutates dwords
                highDWord = uint.MaxValue;
            }
            else
            {
                dwords = _bits;
                highDWord = 0;
            }

            // Find highest significant byte
            int msb;
            for (msb = dwords.Length - 1; msb > 0; msb--)
            {
                if (dwords[msb] != highDWord) break;
            }
            // Ensure high bit is 0 if positive, 1 if negative
            bool needExtraByte = (dwords[msb] & 0x80000000) != (highDWord & 0x80000000);

            uint[] trimmed = new uint[msb + 1 + (needExtraByte ? 1 : 0)];
            Array.Copy(dwords, 0, trimmed, 0, msb + 1);

            if (needExtraByte) trimmed[trimmed.Length - 1] = highDWord;
            return trimmed;
        }

        public override string ToString()
        {
            return BigNumber.FormatBigInteger(this, null, NumberFormatInfo.CurrentInfo);
        }

        public string ToString(IFormatProvider provider)
        {
            return BigNumber.FormatBigInteger(this, null, NumberFormatInfo.GetInstance(provider));
        }

        public string ToString(string format)
        {
            return BigNumber.FormatBigInteger(this, format, NumberFormatInfo.CurrentInfo);
        }

        public string ToString(string format, IFormatProvider provider)
        {
            return BigNumber.FormatBigInteger(this, format, NumberFormatInfo.GetInstance(provider));
        }

        private static BigInteger Add(uint[] leftBits, int leftSign, uint[] rightBits, int rightSign)
        {
            bool trivialLeft = leftBits == null;
            bool trivialRight = rightBits == null;

            if (trivialLeft && trivialRight)
            {
                return (long)leftSign + rightSign;
            }

            if (trivialLeft)
            {
                uint[] bits = BigIntegerCalculator.Add(rightBits, NumericsHelpers.Abs(leftSign));
                return new BigInteger(bits, leftSign < 0);
            }

            if (trivialRight)
            {
                uint[] bits = BigIntegerCalculator.Add(leftBits, NumericsHelpers.Abs(rightSign));
                return new BigInteger(bits, leftSign < 0);
            }

            if (leftBits.Length < rightBits.Length)
            {
                uint[] bits = BigIntegerCalculator.Add(rightBits, leftBits);
                return new BigInteger(bits, leftSign < 0);
            }
            else
            {
                uint[] bits = BigIntegerCalculator.Add(leftBits, rightBits);
                return new BigInteger(bits, leftSign < 0);
            }
        }

        public static BigInteger operator -(BigInteger left, BigInteger right)
        {
            left.AssertValid();
            right.AssertValid();

            if (left._sign < 0 != right._sign < 0)
                return Add(left._bits, left._sign, right._bits, -1 * right._sign);
            return Subtract(left._bits, left._sign, right._bits, right._sign);
        }

        private static BigInteger Subtract(uint[] leftBits, int leftSign, uint[] rightBits, int rightSign)
        {
            bool trivialLeft = leftBits == null;
            bool trivialRight = rightBits == null;

            if (trivialLeft && trivialRight)
            {
                return (long)leftSign - rightSign;
            }

            if (trivialLeft)
            {
                uint[] bits = BigIntegerCalculator.Subtract(rightBits, NumericsHelpers.Abs(leftSign));
                return new BigInteger(bits, leftSign >= 0);
            }

            if (trivialRight)
            {
                uint[] bits = BigIntegerCalculator.Subtract(leftBits, NumericsHelpers.Abs(rightSign));
                return new BigInteger(bits, leftSign < 0);
            }

            if (BigIntegerCalculator.Compare(leftBits, rightBits) < 0)
            {
                uint[] bits = BigIntegerCalculator.Subtract(rightBits, leftBits);
                return new BigInteger(bits, leftSign >= 0);
            }
            else
            {
                uint[] bits = BigIntegerCalculator.Subtract(leftBits, rightBits);
                return new BigInteger(bits, leftSign < 0);
            }
        }

        public static implicit operator BigInteger(byte value)
        {
            return new BigInteger(value);
        }

        [CLSCompliant(false)]
        public static implicit operator BigInteger(sbyte value)
        {
            return new BigInteger(value);
        }

        public static implicit operator BigInteger(short value)
        {
            return new BigInteger(value);
        }

        [CLSCompliant(false)]
        public static implicit operator BigInteger(ushort value)
        {
            return new BigInteger(value);
        }

        public static implicit operator BigInteger(int value)
        {
            return new BigInteger(value);
        }

        [CLSCompliant(false)]
        public static implicit operator BigInteger(uint value)
        {
            return new BigInteger(value);
        }

        public static implicit operator BigInteger(long value)
        {
            return new BigInteger(value);
        }

        [CLSCompliant(false)]
        public static implicit operator BigInteger(ulong value)
        {
            return new BigInteger(value);
        }

        public static explicit operator BigInteger(float value)
        {
            return new BigInteger(value);
        }

        public static explicit operator BigInteger(double value)
        {
            return new BigInteger(value);
        }

        public static explicit operator BigInteger(decimal value)
        {
            return new BigInteger(value);
        }

        public static explicit operator byte(BigInteger value)
        {
            return checked((byte)((int)value));
        }

        [CLSCompliant(false)]
        public static explicit operator sbyte(BigInteger value)
        {
            return checked((sbyte)((int)value));
        }

        public static explicit operator short(BigInteger value)
        {
            return checked((short)((int)value));
        }

        [CLSCompliant(false)]
        public static explicit operator ushort(BigInteger value)
        {
            return checked((ushort)((int)value));
        }

        public static explicit operator int(BigInteger value)
        {
            value.AssertValid();
            if (value._bits == null)
            {
                return value._sign;  // Value packed into int32 sign
            }
            if (value._bits.Length > 1)
            {
                // More than 32 bits
                throw new OverflowException(SR.Overflow_Int32);
            }
            if (value._sign > 0)
            {
                return checked((int)value._bits[0]);
            }
            if (value._bits[0] > kuMaskHighBit)
            {
                // Value > Int32.MinValue
                throw new OverflowException(SR.Overflow_Int32);
            }
            return unchecked(-(int)value._bits[0]);
        }

        [CLSCompliant(false)]
        public static explicit operator uint(BigInteger value)
        {
            value.AssertValid();
            if (value._bits == null)
            {
                return checked((uint)value._sign);
            }
            else if (value._bits.Length > 1 || value._sign < 0)
            {
                throw new OverflowException(SR.Overflow_UInt32);
            }
            else
            {
                return value._bits[0];
            }
        }

        public static explicit operator long(BigInteger value)
        {
            value.AssertValid();
            if (value._bits == null)
            {
                return value._sign;
            }

            int len = value._bits.Length;
            if (len > 2)
            {
                throw new OverflowException(SR.Overflow_Int64);
            }

            ulong uu;
            if (len > 1)
            {
                uu = NumericsHelpers.MakeUlong(value._bits[1], value._bits[0]);
            }
            else
            {
                uu = value._bits[0];
            }

            long ll = value._sign > 0 ? (long)uu : -(long)uu;
            if ((ll > 0 && value._sign > 0) || (ll < 0 && value._sign < 0))
            {
                // Signs match, no overflow
                return ll;
            }
            throw new OverflowException(SR.Overflow_Int64);
        }

        [CLSCompliant(false)]
        public static explicit operator ulong(BigInteger value)
        {
            value.AssertValid();
            if (value._bits == null)
            {
                return checked((ulong)value._sign);
            }

            int len = value._bits.Length;
            if (len > 2 || value._sign < 0)
            {
                throw new OverflowException(SR.Overflow_UInt64);
            }

            if (len > 1)
            {
                return NumericsHelpers.MakeUlong(value._bits[1], value._bits[0]);
            }
            return value._bits[0];
        }

        public static explicit operator float(BigInteger value)
        {
            return (float)((double)value);
        }

        public static explicit operator double(BigInteger value)
        {
            value.AssertValid();

            int sign = value._sign;
            uint[] bits = value._bits;

            if (bits == null)
                return sign;

            int length = bits.Length;

            // The maximum exponent for doubles is 1023, which corresponds to a uint bit length of 32.
            // All BigIntegers with bits[] longer than 32 evaluate to Double.Infinity (or NegativeInfinity).
            // Cases where the exponent is between 1024 and 1035 are handled in NumericsHelpers.GetDoubleFromParts.
            const int InfinityLength = 1024 / kcbitUint;

            if (length > InfinityLength)
            {
                if (sign == 1)
                    return double.PositiveInfinity;
                else
                    return double.NegativeInfinity;
            }

            ulong h = bits[length - 1];
            ulong m = length > 1 ? bits[length - 2] : 0;
            ulong l = length > 2 ? bits[length - 3] : 0;

            int z = NumericsHelpers.CbitHighZero((uint)h);

            int exp = (length - 2) * 32 - z;
            ulong man = (h << 32 + z) | (m << z) | (l >> 32 - z);

            return NumericsHelpers.GetDoubleFromParts(sign, exp, man);
        }

        public static explicit operator decimal(BigInteger value)
        {
            value.AssertValid();
            if (value._bits == null)
                return value._sign;

            int length = value._bits.Length;
            if (length > 3) throw new OverflowException(SR.Overflow_Decimal);

            int lo = 0, mi = 0, hi = 0;
            if (length > 2) hi = (int)value._bits[2];
            if (length > 1) mi = (int)value._bits[1];
            if (length > 0) lo = (int)value._bits[0];

            return new decimal(lo, mi, hi, value._sign < 0, 0);
        }

        public static BigInteger operator &(BigInteger left, BigInteger right)
        {
            if (left.IsZero || right.IsZero)
            {
                return Zero;
            }

            uint[] x = left.ToUInt32Array();
            uint[] y = right.ToUInt32Array();
            uint[] z = new uint[Math.Max(x.Length, y.Length)];
            uint xExtend = (left._sign < 0) ? uint.MaxValue : 0;
            uint yExtend = (right._sign < 0) ? uint.MaxValue : 0;

            for (int i = 0; i < z.Length; i++)
            {
                uint xu = (i < x.Length) ? x[i] : xExtend;
                uint yu = (i < y.Length) ? y[i] : yExtend;
                z[i] = xu & yu;
            }
            return new BigInteger(z);
        }

        public static BigInteger operator |(BigInteger left, BigInteger right)
        {
            if (left.IsZero)
                return right;
            if (right.IsZero)
                return left;

            uint[] x = left.ToUInt32Array();
            uint[] y = right.ToUInt32Array();
            uint[] z = new uint[Math.Max(x.Length, y.Length)];
            uint xExtend = (left._sign < 0) ? uint.MaxValue : 0;
            uint yExtend = (right._sign < 0) ? uint.MaxValue : 0;

            for (int i = 0; i < z.Length; i++)
            {
                uint xu = (i < x.Length) ? x[i] : xExtend;
                uint yu = (i < y.Length) ? y[i] : yExtend;
                z[i] = xu | yu;
            }
            return new BigInteger(z);
        }

        public static BigInteger operator ^(BigInteger left, BigInteger right)
        {
            uint[] x = left.ToUInt32Array();
            uint[] y = right.ToUInt32Array();
            uint[] z = new uint[Math.Max(x.Length, y.Length)];
            uint xExtend = (left._sign < 0) ? uint.MaxValue : 0;
            uint yExtend = (right._sign < 0) ? uint.MaxValue : 0;

            for (int i = 0; i < z.Length; i++)
            {
                uint xu = (i < x.Length) ? x[i] : xExtend;
                uint yu = (i < y.Length) ? y[i] : yExtend;
                z[i] = xu ^ yu;
            }

            return new BigInteger(z);
        }

        public static BigInteger operator <<(BigInteger value, int shift)
        {
            if (shift == 0) return value;
            else if (shift == int.MinValue) return ((value >> int.MaxValue) >> 1);
            else if (shift < 0) return value >> -shift;

            int digitShift = shift / kcbitUint;
            int smallShift = shift - (digitShift * kcbitUint);

            uint[] xd; int xl; bool negx;
            negx = GetPartsForBitManipulation(ref value, out xd, out xl);

            int zl = xl + digitShift + 1;
            uint[] zd = new uint[zl];

            if (smallShift == 0)
            {
                for (int i = 0; i < xl; i++)
                {
                    zd[i + digitShift] = xd[i];
                }
            }
            else
            {
                int carryShift = kcbitUint - smallShift;
                uint carry = 0;
                int i;
                for (i = 0; i < xl; i++)
                {
                    uint rot = xd[i];
                    zd[i + digitShift] = rot << smallShift | carry;
                    carry = rot >> carryShift;
                }
                zd[i + digitShift] = carry;
            }
            return new BigInteger(zd, negx);
        }

        public static BigInteger operator >>(BigInteger value, int shift)
        {
            if (shift == 0) return value;
            else if (shift == int.MinValue) return ((value << int.MaxValue) << 1);
            else if (shift < 0) return value << -shift;

            int digitShift = shift / kcbitUint;
            int smallShift = shift - (digitShift * kcbitUint);

            uint[] xd; int xl; bool negx;
            negx = GetPartsForBitManipulation(ref value, out xd, out xl);

            if (negx)
            {
                if (shift >= (kcbitUint * xl))
                {
                    return MinusOne;
                }
                uint[] temp = new uint[xl];
                Array.Copy(xd /* sourceArray */, 0 /* sourceIndex */, temp /* destinationArray */, 0 /* destinationIndex */, xl /* length */);  // Make a copy of immutable value._bits
                xd = temp;
                NumericsHelpers.DangerousMakeTwosComplement(xd); // Mutates xd
            }

            int zl = xl - digitShift;
            if (zl < 0) zl = 0;
            uint[] zd = new uint[zl];

            if (smallShift == 0)
            {
                for (int i = xl - 1; i >= digitShift; i--)
                {
                    zd[i - digitShift] = xd[i];
                }
            }
            else
            {
                int carryShift = kcbitUint - smallShift;
                uint carry = 0;
                for (int i = xl - 1; i >= digitShift; i--)
                {
                    uint rot = xd[i];
                    if (negx && i == xl - 1)
                        // Sign-extend the first shift for negative ints then let the carry propagate
                        zd[i - digitShift] = (rot >> smallShift) | (0xFFFFFFFF << carryShift);
                    else
                        zd[i - digitShift] = (rot >> smallShift) | carry;
                    carry = rot << carryShift;
                }
            }
            if (negx)
            {
                NumericsHelpers.DangerousMakeTwosComplement(zd); // Mutates zd
            }
            return new BigInteger(zd, negx);
        }

        public static BigInteger operator ~(BigInteger value)
        {
            return -(value + One);
        }

        public static BigInteger operator -(BigInteger value)
        {
            value.AssertValid();
            return new BigInteger(-value._sign, value._bits);
        }

        public static BigInteger operator +(BigInteger value)
        {
            value.AssertValid();
            return value;
        }

        public static BigInteger operator ++(BigInteger value)
        {
            return value + One;
        }

        public static BigInteger operator --(BigInteger value)
        {
            return value - One;
        }

        public static BigInteger operator +(BigInteger left, BigInteger right)
        {
            left.AssertValid();
            right.AssertValid();

            if (left._sign < 0 != right._sign < 0)
                return Subtract(left._bits, left._sign, right._bits, -1 * right._sign);
            return Add(left._bits, left._sign, right._bits, right._sign);
        }

        public static BigInteger operator *(BigInteger left, BigInteger right)
        {
            left.AssertValid();
            right.AssertValid();

            bool trivialLeft = left._bits == null;
            bool trivialRight = right._bits == null;

            if (trivialLeft && trivialRight)
            {
                return (long)left._sign * right._sign;
            }
            
            if (trivialLeft)
            {
                uint[] bits = BigIntegerCalculator.Multiply(right._bits, NumericsHelpers.Abs(left._sign));
                return new BigInteger(bits, (left._sign < 0) ^ (right._sign < 0));
            }

            if (trivialRight)
            {
                uint[] bits = BigIntegerCalculator.Multiply(left._bits, NumericsHelpers.Abs(right._sign));
                return new BigInteger(bits, (left._sign < 0) ^ (right._sign < 0));
            }

            if (left._bits == right._bits)
            {
                uint[] bits = BigIntegerCalculator.Square(left._bits);
                return new BigInteger(bits, (left._sign < 0) ^ (right._sign < 0));
            }

            if (left._bits.Length < right._bits.Length)
            {
                uint[] bits = BigIntegerCalculator.Multiply(right._bits, left._bits);
                return new BigInteger(bits, (left._sign < 0) ^ (right._sign < 0));
            }
            else
            {
                uint[] bits = BigIntegerCalculator.Multiply(left._bits, right._bits);
                return new BigInteger(bits, (left._sign < 0) ^ (right._sign < 0));
            }
        }

        public static BigInteger operator /(BigInteger dividend, BigInteger divisor)
        {
            dividend.AssertValid();
            divisor.AssertValid();

            bool trivialDividend = dividend._bits == null;
            bool trivialDivisor = divisor._bits == null;

            if (trivialDividend && trivialDivisor)
            {
                return dividend._sign / divisor._sign;
            }

            if (trivialDividend)
            {
                // The divisor is non-trivial
                // and therefore the bigger one
                return s_bnZeroInt;
            }
            
            if (trivialDivisor)
            {
                uint[] bits = BigIntegerCalculator.Divide(dividend._bits, NumericsHelpers.Abs(divisor._sign));
                return new BigInteger(bits, (dividend._sign < 0) ^ (divisor._sign < 0));
            }

            if (dividend._bits.Length < divisor._bits.Length)
            {
                return s_bnZeroInt;
            }
            else
            {
                uint[] bits = BigIntegerCalculator.Divide(dividend._bits, divisor._bits);
                return new BigInteger(bits, (dividend._sign < 0) ^ (divisor._sign < 0));
            }
        }

        public static BigInteger operator %(BigInteger dividend, BigInteger divisor)
        {
            dividend.AssertValid();
            divisor.AssertValid();

            bool trivialDividend = dividend._bits == null;
            bool trivialDivisor = divisor._bits == null;

            if (trivialDividend && trivialDivisor)
            {
                return dividend._sign % divisor._sign;
            }

            if (trivialDividend)
            {
                // The divisor is non-trivial
                // and therefore the bigger one
                return dividend;
            }
            
            if (trivialDivisor)
            {
                uint remainder = BigIntegerCalculator.Remainder(dividend._bits, NumericsHelpers.Abs(divisor._sign));
                return dividend._sign < 0 ? -1 * remainder : remainder;
            }

            if (dividend._bits.Length < divisor._bits.Length)
            {
                return dividend;
            }
            uint[] bits = BigIntegerCalculator.Remainder(dividend._bits, divisor._bits);
            return new BigInteger(bits, dividend._sign < 0);
        }

        public static bool operator <(BigInteger left, BigInteger right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator <=(BigInteger left, BigInteger right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >(BigInteger left, BigInteger right)
        {
            return left.CompareTo(right) > 0;
        }
        public static bool operator >=(BigInteger left, BigInteger right)
        {
            return left.CompareTo(right) >= 0;
        }

        public static bool operator ==(BigInteger left, BigInteger right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BigInteger left, BigInteger right)
        {
            return !left.Equals(right);
        }

        public static bool operator <(BigInteger left, long right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator <=(BigInteger left, long right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >(BigInteger left, long right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator >=(BigInteger left, long right)
        {
            return left.CompareTo(right) >= 0;
        }

        public static bool operator ==(BigInteger left, long right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BigInteger left, long right)
        {
            return !left.Equals(right);
        }

        public static bool operator <(long left, BigInteger right)
        {
            return right.CompareTo(left) > 0;
        }

        public static bool operator <=(long left, BigInteger right)
        {
            return right.CompareTo(left) >= 0;
        }

        public static bool operator >(long left, BigInteger right)
        {
            return right.CompareTo(left) < 0;
        }

        public static bool operator >=(long left, BigInteger right)
        {
            return right.CompareTo(left) <= 0;
        }

        public static bool operator ==(long left, BigInteger right)
        {
            return right.Equals(left);
        }

        public static bool operator !=(long left, BigInteger right)
        {
            return !right.Equals(left);
        }

        [CLSCompliant(false)]
        public static bool operator <(BigInteger left, ulong right)
        {
            return left.CompareTo(right) < 0;
        }

        [CLSCompliant(false)]
        public static bool operator <=(BigInteger left, ulong right)
        {
            return left.CompareTo(right) <= 0;
        }

        [CLSCompliant(false)]
        public static bool operator >(BigInteger left, ulong right)
        {
            return left.CompareTo(right) > 0;
        }

        [CLSCompliant(false)]
        public static bool operator >=(BigInteger left, ulong right)
        {
            return left.CompareTo(right) >= 0;
        }

        [CLSCompliant(false)]
        public static bool operator ==(BigInteger left, ulong right)
        {
            return left.Equals(right);
        }

        [CLSCompliant(false)]
        public static bool operator !=(BigInteger left, ulong right)
        {
            return !left.Equals(right);
        }

        [CLSCompliant(false)]
        public static bool operator <(ulong left, BigInteger right)
        {
            return right.CompareTo(left) > 0;
        }

        [CLSCompliant(false)]
        public static bool operator <=(ulong left, BigInteger right)
        {
            return right.CompareTo(left) >= 0;
        }

        [CLSCompliant(false)]
        public static bool operator >(ulong left, BigInteger right)
        {
            return right.CompareTo(left) < 0;
        }

        [CLSCompliant(false)]
        public static bool operator >=(ulong left, BigInteger right)
        {
            return right.CompareTo(left) <= 0;
        }

        [CLSCompliant(false)]
        public static bool operator ==(ulong left, BigInteger right)
        {
            return right.Equals(left);
        }

        [CLSCompliant(false)]
        public static bool operator !=(ulong left, BigInteger right)
        {
            return !right.Equals(left);
        }

        /// <summary>
        /// Encapsulate the logic of normalizing the "small" and "large" forms of BigInteger
        /// into the "large" form so that Bit Manipulation algorithms can be simplified. 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="xd">
        /// The UInt32 array containing the entire big integer in "large" (denormalized) form.
        /// E.g., the number one (1) and negative one (-1) are both stored as 0x00000001
        //  BigInteger values Int32.MinValue < x <= Int32.MaxValue are converted to this
        //  format for convenience.
        /// </param>
        /// <param name="xl">The length of xd.</param>
        /// <returns>True for negative numbers.</returns>
        private static bool GetPartsForBitManipulation(ref BigInteger x, out uint[] xd, out int xl)
        {
            if (x._bits == null)
            {
                if (x._sign < 0)
                {
                    xd = new uint[] { (uint)-x._sign };
                }
                else
                {
                    xd = new uint[] { (uint)x._sign };
                }
            }
            else
            {
                xd = x._bits;
            }
            xl = (x._bits == null ? 1 : x._bits.Length);
            return x._sign < 0;
        }

        internal static int GetDiffLength(uint[] rgu1, uint[] rgu2, int cu)
        {
            for (int iv = cu; --iv >= 0;)
            {
                if (rgu1[iv] != rgu2[iv])
                    return iv + 1;
            }
            return 0;
        }

        [Conditional("DEBUG")]
        private void AssertValid()
        {
            if (_bits != null)
            {
                // _sign must be +1 or -1 when _bits is non-null
                Debug.Assert(_sign == 1 || _sign == -1);
                // _bits must contain at least 1 element or be null
                Debug.Assert(_bits.Length > 0);
                // Wasted space: _bits[0] could have been packed into _sign
                Debug.Assert(_bits.Length > 1 || _bits[0] >= kuMaskHighBit);
                // Wasted space: leading zeros could have been truncated
                Debug.Assert(_bits[_bits.Length - 1] != 0);
            }
            else
            {
                // Int32.MinValue should not be stored in the _sign field
                Debug.Assert(_sign > int.MinValue);
            }
        }
    }
}
