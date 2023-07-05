// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Numerics
{
    [Serializable]
    [TypeForwardedFrom("System.Numerics, Version=4.0.0.0, PublicKeyToken=b77a5c561934e089")]
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public readonly struct BigInteger
        : ISpanFormattable,
          IComparable,
          IComparable<BigInteger>,
          IEquatable<BigInteger>,
          IBinaryInteger<BigInteger>,
          ISignedNumber<BigInteger>
    {
        private const uint kuMaskHighBit = unchecked((uint)int.MinValue);
        private const int kcbitUint = 32;
        private const int kcbitUlong = 64;
        private const int DecimalScaleFactorMask = 0x00FF0000;

        // For values int.MinValue < n <= int.MaxValue, the value is stored in sign
        // and _bits is null. For all other values, sign is +1 or -1 and the bits are in _bits
        internal readonly int _sign; // Do not rename (binary serialization)
        internal readonly uint[]? _bits; // Do not rename (binary serialization)

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
                ulong x;
                if (value < 0)
                {
                    x = unchecked((ulong)-value);
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
                    _bits[0] = unchecked((uint)x);
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
                _bits[0] = unchecked((uint)value);
                _bits[1] = (uint)(value >> kcbitUint);
            }

            AssertValid();
        }

        public BigInteger(float value) : this((double)value)
        {
        }

        public BigInteger(double value)
        {
            if (!double.IsFinite(value))
            {
                if (double.IsInfinity(value))
                {
                    throw new OverflowException(SR.Overflow_BigIntInfinity);
                }
                else // NaN
                {
                    throw new OverflowException(SR.Overflow_NotANumber);
                }
            }

            _sign = 0;
            _bits = null;

            int sign, exp;
            ulong man;
            NumericsHelpers.GetDoubleParts(value, out sign, out exp, out man, out _);
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
                _bits[cu] = unchecked((uint)(man >> cbit));
                if (cbit > 0)
                    _bits[cu - 1] = unchecked((uint)man) << (kcbitUint - cbit);
                _sign = sign;
            }

            AssertValid();
        }

        public BigInteger(decimal value)
        {
            // First truncate to get scale to 0 and extract bits
            Span<int> bits = stackalloc int[4];
            decimal.GetBits(decimal.Truncate(value), bits);

            Debug.Assert(bits.Length == 4 && (bits[3] & DecimalScaleFactorMask) == 0);

            const int signMask = unchecked((int)kuMaskHighBit);
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
                _sign *= ((bits[3] & signMask) != 0) ? -1 : +1;
                _bits = null;
            }
            else
            {
                _bits = new uint[size];

                unchecked
                {
                    _bits[0] = (uint)bits[0];
                    if (size > 1)
                        _bits[1] = (uint)bits[1];
                    if (size > 2)
                        _bits[2] = (uint)bits[2];
                }

                _sign = ((bits[3] & signMask) != 0) ? -1 : +1;
            }
            AssertValid();
        }

        /// <summary>
        /// Creates a BigInteger from a little-endian twos-complement byte array.
        /// </summary>
        /// <param name="value"></param>
        [CLSCompliant(false)]
        public BigInteger(byte[] value) :
            this(new ReadOnlySpan<byte>(value ?? throw new ArgumentNullException(nameof(value))))
        {
        }

        public BigInteger(ReadOnlySpan<byte> value, bool isUnsigned = false, bool isBigEndian = false)
        {
            int byteCount = value.Length;

            bool isNegative;
            if (byteCount > 0)
            {
                byte mostSignificantByte = isBigEndian ? value[0] : value[byteCount - 1];
                isNegative = (mostSignificantByte & 0x80) != 0 && !isUnsigned;

                if (mostSignificantByte == 0)
                {
                    // Try to conserve space as much as possible by checking for wasted leading byte[] entries
                    if (isBigEndian)
                    {
                        int offset = 1;

                        while (offset < byteCount && value[offset] == 0)
                        {
                            offset++;
                        }

                        value = value.Slice(offset);
                        byteCount = value.Length;
                    }
                    else
                    {
                        byteCount -= 2;

                        while (byteCount >= 0 && value[byteCount] == 0)
                        {
                            byteCount--;
                        }

                        byteCount++;
                    }
                }
            }
            else
            {
                isNegative = false;
            }

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
                _sign = isNegative ? unchecked((int)0xffffffff) : 0;

                if (isBigEndian)
                {
                    for (int i = 0; i < byteCount; i++)
                    {
                        _sign = (_sign << 8) | value[i];
                    }
                }
                else
                {
                    for (int i = byteCount - 1; i >= 0; i--)
                    {
                        _sign = (_sign << 8) | value[i];
                    }
                }

                _bits = null;
                if (_sign < 0 && !isNegative)
                {
                    // Int32 overflow
                    // Example: Int64 value 2362232011 (0xCB, 0xCC, 0xCC, 0x8C, 0x0)
                    // can be naively packed into 4 bytes (due to the leading 0x0)
                    // it overflows into the int32 sign bit
                    _bits = new uint[1] { unchecked((uint)_sign) };
                    _sign = +1;
                }
                if (_sign == int.MinValue)
                {
                    this = s_bnMinInt;
                }
            }
            else
            {
                int unalignedBytes = byteCount % 4;
                int dwordCount = byteCount / 4 + (unalignedBytes == 0 ? 0 : 1);
                uint[] val = new uint[dwordCount];
                int byteCountMinus1 = byteCount - 1;

                // Copy all dwords, except don't do the last one if it's not a full four bytes
                int curDword, curByte;

                if (isBigEndian)
                {
                    curByte = byteCount - sizeof(int);
                    for (curDword = 0; curDword < dwordCount - (unalignedBytes == 0 ? 0 : 1); curDword++)
                    {
                        for (int byteInDword = 0; byteInDword < 4; byteInDword++)
                        {
                            byte curByteValue = value[curByte];
                            val[curDword] = (val[curDword] << 8) | curByteValue;
                            curByte++;
                        }

                        curByte -= 8;
                    }
                }
                else
                {
                    curByte = sizeof(int) - 1;
                    for (curDword = 0; curDword < dwordCount - (unalignedBytes == 0 ? 0 : 1); curDword++)
                    {
                        for (int byteInDword = 0; byteInDword < 4; byteInDword++)
                        {
                            byte curByteValue = value[curByte];
                            val[curDword] = (val[curDword] << 8) | curByteValue;
                            curByte--;
                        }

                        curByte += 8;
                    }
                }

                // Copy the last dword specially if it's not aligned
                if (unalignedBytes != 0)
                {
                    if (isNegative)
                    {
                        val[dwordCount - 1] = 0xffffffff;
                    }

                    if (isBigEndian)
                    {
                        for (curByte = 0; curByte < unalignedBytes; curByte++)
                        {
                            byte curByteValue = value[curByte];
                            val[curDword] = (val[curDword] << 8) | curByteValue;
                        }
                    }
                    else
                    {
                        for (curByte = byteCountMinus1; curByte >= byteCount - unalignedBytes; curByte--)
                        {
                            byte curByteValue = value[curByte];
                            val[curDword] = (val[curDword] << 8) | curByteValue;
                        }
                    }
                }

                if (isNegative)
                {
                    NumericsHelpers.DangerousMakeTwosComplement(val); // Mutates val

                    // Pack _bits to remove any wasted space after the twos complement
                    int len = val.Length - 1;
                    while (len >= 0 && val[len] == 0) len--;
                    len++;

                    if (len == 1)
                    {
                        switch (val[0])
                        {
                            case 1: // abs(-1)
                                this = s_bnMinusOneInt;
                                return;

                            case kuMaskHighBit: // abs(Int32.MinValue)
                                this = s_bnMinInt;
                                return;

                            default:
                                if (unchecked((int)val[0]) > 0)
                                {
                                    _sign = (-1) * ((int)val[0]);
                                    _bits = null;
                                    AssertValid();
                                    return;
                                }

                                break;
                        }
                    }

                    if (len != val.Length)
                    {
                        _sign = -1;
                        _bits = new uint[len];
                        Array.Copy(val, _bits, len);
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

        internal BigInteger(int n, uint[]? rgu)
        {
            if ((rgu is not null) && (rgu.Length > MaxLength))
            {
                ThrowHelper.ThrowOverflowException();
            }

            _sign = n;
            _bits = rgu;

            AssertValid();
        }

        /// <summary>
        /// Constructor used during bit manipulation and arithmetic.
        /// When possible the value will be packed into  _sign to conserve space.
        /// </summary>
        /// <param name="value">The absolute value of the number</param>
        /// <param name="negative">The bool indicating the sign of the value.</param>
        private BigInteger(ReadOnlySpan<uint> value, bool negative)
        {
            if (value.Length > MaxLength)
            {
                ThrowHelper.ThrowOverflowException();
            }

            int len;

            // Try to conserve space as much as possible by checking for wasted leading span entries
            // sometimes the span has leading zeros from bit manipulation operations & and ^
            for (len = value.Length; len > 0 && value[len - 1] == 0; len--);

            if (len == 0)
            {
                this = s_bnZeroInt;
            }
            else if (len == 1 && value[0] < kuMaskHighBit)
            {
                // Values like (Int32.MaxValue+1) are stored as "0x80000000" and as such cannot be packed into _sign
                _sign = negative ? -(int)value[0] : (int)value[0];
                _bits = null;
                if (_sign == int.MinValue)
                {
                    // Although Int32.MinValue fits in _sign, we represent this case differently for negate
                    this = s_bnMinInt;
                }
            }
            else
            {
                _sign = negative ? -1 : +1;
                _bits = value.Slice(0, len).ToArray();
            }
            AssertValid();
        }

        /// <summary>
        /// Create a BigInteger from a little-endian twos-complement UInt32 span.
        /// </summary>
        /// <param name="value"></param>
        private BigInteger(Span<uint> value)
        {
            if (value.Length > MaxLength)
            {
                ThrowHelper.ThrowOverflowException();
            }

            int dwordCount = value.Length;
            bool isNegative = dwordCount > 0 && ((value[dwordCount - 1] & kuMaskHighBit) == kuMaskHighBit);

            // Try to conserve space as much as possible by checking for wasted leading span entries
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
                if (unchecked((int)value[0]) < 0 && !isNegative)
                {
                    _bits = new uint[1];
                    _bits[0] = value[0];
                    _sign = +1;
                }
                // Handle the special cases where the BigInteger likely fits into _sign
                else if (int.MinValue == unchecked((int)value[0]))
                {
                    this = s_bnMinInt;
                }
                else
                {
                    _sign = unchecked((int)value[0]);
                    _bits = null;
                }
                AssertValid();
                return;
            }

            if (!isNegative)
            {
                // Handle the simple positive value cases where the input is already in sign magnitude
                _sign = +1;
                value = value.Slice(0, dwordCount);
                _bits = value.ToArray();
                AssertValid();
                return;
            }

            // Finally handle the more complex cases where we must transform the input into sign magnitude
            NumericsHelpers.DangerousMakeTwosComplement(value); // mutates val

            // Pack _bits to remove any wasted space after the twos complement
            int len = value.Length;
            while (len > 0 && value[len - 1] == 0) len--;

            // The number is represented by a single dword
            if (len == 1 && unchecked((int)(value[0])) > 0)
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
            else
            {
                _sign = -1;
                _bits = value.Slice(0, len).ToArray();
            }
            AssertValid();
            return;
        }

        public static BigInteger Zero { get { return s_bnZeroInt; } }

        public static BigInteger One { get { return s_bnOneInt; } }

        public static BigInteger MinusOne { get { return s_bnMinusOneInt; } }

        internal static int MaxLength => Array.MaxLength / sizeof(uint);

        public bool IsPowerOfTwo
        {
            get
            {
                AssertValid();

                if (_bits == null)
                    return BitOperations.IsPow2(_sign);

                if (_sign != 1)
                    return false;
                int iu = _bits.Length - 1;
                if (!BitOperations.IsPow2(_bits[iu]))
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

        public static BigInteger Parse(string value, IFormatProvider? provider)
        {
            return Parse(value, NumberStyles.Integer, NumberFormatInfo.GetInstance(provider));
        }

        public static BigInteger Parse(string value, NumberStyles style, IFormatProvider? provider)
        {
            return BigNumber.ParseBigInteger(value, style, NumberFormatInfo.GetInstance(provider));
        }

        public static bool TryParse([NotNullWhen(true)] string? value, out BigInteger result)
        {
            return TryParse(value, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
        }

        public static bool TryParse([NotNullWhen(true)] string? value, NumberStyles style, IFormatProvider? provider, out BigInteger result)
        {
            return BigNumber.TryParseBigInteger(value, style, NumberFormatInfo.GetInstance(provider), out result) == BigNumber.ParsingStatus.OK;
        }

        public static BigInteger Parse(ReadOnlySpan<char> value, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
        {
            return BigNumber.ParseBigInteger(value, style, NumberFormatInfo.GetInstance(provider));
        }

        public static bool TryParse(ReadOnlySpan<char> value, out BigInteger result)
        {
            return TryParse(value, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
        }

        public static bool TryParse(ReadOnlySpan<char> value, NumberStyles style, IFormatProvider? provider, out BigInteger result)
        {
            return BigNumber.TryParseBigInteger(value, style, NumberFormatInfo.GetInstance(provider), out result) == BigNumber.ParsingStatus.OK;
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
                BigInteger quotient;
                (quotient, remainder) = Math.DivRem(dividend._sign, divisor._sign);
                return quotient;
            }

            if (trivialDividend)
            {
                // The divisor is non-trivial
                // and therefore the bigger one
                remainder = dividend;
                return s_bnZeroInt;
            }

            Debug.Assert(dividend._bits != null);

            if (trivialDivisor)
            {
                uint rest;

                uint[]? bitsFromPool = null;
                int size = dividend._bits.Length;
                Span<uint> quotient = ((uint)size <= BigIntegerCalculator.StackAllocThreshold
                                    ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                                    : bitsFromPool = ArrayPool<uint>.Shared.Rent(size)).Slice(0, size);

                try
                {
                    // may throw DivideByZeroException
                    BigIntegerCalculator.Divide(dividend._bits, NumericsHelpers.Abs(divisor._sign), quotient, out rest);

                    remainder = dividend._sign < 0 ? -1 * rest : rest;
                    return new BigInteger(quotient, (dividend._sign < 0) ^ (divisor._sign < 0));
                }
                finally
                {
                    if (bitsFromPool != null)
                        ArrayPool<uint>.Shared.Return(bitsFromPool);
                }
            }

            Debug.Assert(divisor._bits != null);

            if (dividend._bits.Length < divisor._bits.Length)
            {
                remainder = dividend;
                return s_bnZeroInt;
            }
            else
            {
                uint[]? remainderFromPool = null;
                int size = dividend._bits.Length;
                Span<uint> rest = ((uint)size <= BigIntegerCalculator.StackAllocThreshold
                                ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                                : remainderFromPool = ArrayPool<uint>.Shared.Rent(size)).Slice(0, size);

                uint[]? quotientFromPool = null;
                size = dividend._bits.Length - divisor._bits.Length + 1;
                Span<uint> quotient = ((uint)size <= BigIntegerCalculator.StackAllocThreshold
                                    ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                                    : quotientFromPool = ArrayPool<uint>.Shared.Rent(size)).Slice(0, size);

                BigIntegerCalculator.Divide(dividend._bits, divisor._bits, quotient, rest);

                remainder = new BigInteger(rest, dividend._sign < 0);
                var result = new BigInteger(quotient, (dividend._sign < 0) ^ (divisor._sign < 0));

                if (remainderFromPool != null)
                    ArrayPool<uint>.Shared.Return(remainderFromPool);

                if (quotientFromPool != null)
                    ArrayPool<uint>.Shared.Return(quotientFromPool);

                return result;
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
            int c = BitOperations.LeadingZeroCount((uint)h);
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
                Debug.Assert(right._bits != null);
                return left._sign != 0
                    ? BigIntegerCalculator.Gcd(right._bits, NumericsHelpers.Abs(left._sign))
                    : new BigInteger(right._bits, negative: false);
            }

            if (trivialRight)
            {
                Debug.Assert(left._bits != null);
                return right._sign != 0
                    ? BigIntegerCalculator.Gcd(left._bits, NumericsHelpers.Abs(right._sign))
                    : new BigInteger(left._bits, negative: false);
            }

            Debug.Assert(left._bits != null && right._bits != null);

            if (BigIntegerCalculator.Compare(left._bits, right._bits) < 0)
            {
                return GreatestCommonDivisor(right._bits, left._bits);
            }
            else
            {
                return GreatestCommonDivisor(left._bits, right._bits);
            }
        }

        private static BigInteger GreatestCommonDivisor(ReadOnlySpan<uint> leftBits, ReadOnlySpan<uint> rightBits)
        {
            Debug.Assert(BigIntegerCalculator.Compare(leftBits, rightBits) >= 0);

            uint[]? bitsFromPool = null;
            BigInteger result;

            // Short circuits to spare some allocations...
            if (rightBits.Length == 1)
            {
                uint temp = BigIntegerCalculator.Remainder(leftBits, rightBits[0]);
                result = BigIntegerCalculator.Gcd(rightBits[0], temp);
            }
            else if (rightBits.Length == 2)
            {
                Span<uint> bits = (leftBits.Length <= BigIntegerCalculator.StackAllocThreshold
                                ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                                : bitsFromPool = ArrayPool<uint>.Shared.Rent(leftBits.Length)).Slice(0, leftBits.Length);

                BigIntegerCalculator.Remainder(leftBits, rightBits, bits);

                ulong left = ((ulong)rightBits[1] << 32) | rightBits[0];
                ulong right = ((ulong)bits[1] << 32) | bits[0];

                result = BigIntegerCalculator.Gcd(left, right);
            }
            else
            {
                Span<uint> bits = (leftBits.Length <= BigIntegerCalculator.StackAllocThreshold
                                ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                                : bitsFromPool = ArrayPool<uint>.Shared.Rent(leftBits.Length)).Slice(0, leftBits.Length);

                BigIntegerCalculator.Gcd(leftBits, rightBits, bits);
                result = new BigInteger(bits, negative: false);
            }

            if (bitsFromPool != null)
                ArrayPool<uint>.Shared.Return(bitsFromPool);

            return result;
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
            ArgumentOutOfRangeException.ThrowIfNegative(exponent.Sign, nameof(exponent));

            value.AssertValid();
            exponent.AssertValid();
            modulus.AssertValid();

            bool trivialValue = value._bits == null;
            bool trivialExponent = exponent._bits == null;
            bool trivialModulus = modulus._bits == null;

            BigInteger result;

            if (trivialModulus)
            {
                uint bits = trivialValue && trivialExponent ? BigIntegerCalculator.Pow(NumericsHelpers.Abs(value._sign), NumericsHelpers.Abs(exponent._sign), NumericsHelpers.Abs(modulus._sign)) :
                            trivialValue ? BigIntegerCalculator.Pow(NumericsHelpers.Abs(value._sign), exponent._bits!, NumericsHelpers.Abs(modulus._sign)) :
                            trivialExponent ? BigIntegerCalculator.Pow(value._bits!, NumericsHelpers.Abs(exponent._sign), NumericsHelpers.Abs(modulus._sign)) :
                            BigIntegerCalculator.Pow(value._bits!, exponent._bits!, NumericsHelpers.Abs(modulus._sign));

                result = value._sign < 0 && !exponent.IsEven ? -1 * bits : bits;
            }
            else
            {
                int size = (modulus._bits?.Length ?? 1) << 1;
                uint[]? bitsFromPool = null;
                Span<uint> bits = ((uint)size <= BigIntegerCalculator.StackAllocThreshold
                                ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                                : bitsFromPool = ArrayPool<uint>.Shared.Rent(size)).Slice(0, size);
                bits.Clear();
                if (trivialValue)
                {
                    if (trivialExponent)
                    {
                        BigIntegerCalculator.Pow(NumericsHelpers.Abs(value._sign), NumericsHelpers.Abs(exponent._sign), modulus._bits!, bits);
                    }
                    else
                    {
                        BigIntegerCalculator.Pow(NumericsHelpers.Abs(value._sign), exponent._bits!, modulus._bits!, bits);
                    }
                }
                else if (trivialExponent)
                {
                    BigIntegerCalculator.Pow(value._bits!, NumericsHelpers.Abs(exponent._sign), modulus._bits!, bits);
                }
                else
                {
                    BigIntegerCalculator.Pow(value._bits!, exponent._bits!, modulus._bits!, bits);
                }

                result = new BigInteger(bits, value._sign < 0 && !exponent.IsEven);

                if (bitsFromPool != null)
                    ArrayPool<uint>.Shared.Return(bitsFromPool);
            }

            return result;
        }

        public static BigInteger Pow(BigInteger value, int exponent)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(exponent);

            value.AssertValid();

            if (exponent == 0)
                return s_bnOneInt;
            if (exponent == 1)
                return value;

            bool trivialValue = value._bits == null;

            uint power = NumericsHelpers.Abs(exponent);
            uint[]? bitsFromPool = null;
            BigInteger result;

            if (trivialValue)
            {
                if (value._sign == 1)
                    return value;
                if (value._sign == -1)
                    return (exponent & 1) != 0 ? value : s_bnOneInt;
                if (value._sign == 0)
                    return value;

                int size = BigIntegerCalculator.PowBound(power, 1);
                Span<uint> bits = ((uint)size <= BigIntegerCalculator.StackAllocThreshold
                                ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                                : bitsFromPool = ArrayPool<uint>.Shared.Rent(size)).Slice(0, size);
                bits.Clear();

                BigIntegerCalculator.Pow(NumericsHelpers.Abs(value._sign), power, bits);
                result = new BigInteger(bits, value._sign < 0 && (exponent & 1) != 0);
            }
            else
            {
                int size = BigIntegerCalculator.PowBound(power, value._bits!.Length);
                Span<uint> bits = ((uint)size <= BigIntegerCalculator.StackAllocThreshold
                                ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                                : bitsFromPool = ArrayPool<uint>.Shared.Rent(size)).Slice(0, size);
                bits.Clear();

                BigIntegerCalculator.Pow(value._bits, power, bits);
                result = new BigInteger(bits, value._sign < 0 && (exponent & 1) != 0);
            }

            if (bitsFromPool != null)
                ArrayPool<uint>.Shared.Return(bitsFromPool);

            return result;
        }

        public override int GetHashCode()
        {
            AssertValid();

            if (_bits is null)
                return _sign;

            HashCode hash = default;
            hash.AddBytes(MemoryMarshal.AsBytes(_bits.AsSpan()));
            hash.Add(_sign);
            return hash.ToHashCode();
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            AssertValid();

            return obj is BigInteger other && Equals(other);
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

            return NumericsHelpers.MakeUInt64(_bits[1], _bits[0]) == uu;
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
            return NumericsHelpers.MakeUInt64(_bits[1], _bits[0]) == other;
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
            ulong uuTmp = cu == 2 ? NumericsHelpers.MakeUInt64(_bits[1], _bits[0]) : _bits[0];
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
            ulong uuTmp = cu == 2 ? NumericsHelpers.MakeUInt64(_bits[1], _bits[0]) : _bits[0];
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

        public int CompareTo(object? obj)
        {
            if (obj == null)
                return 1;
            if (obj is not BigInteger bigInt)
                throw new ArgumentException(SR.Argument_MustBeBigInt, nameof(obj));
            return CompareTo(bigInt);
        }

        /// <summary>
        /// Returns the value of this BigInteger as a little-endian twos-complement
        /// byte array, using the fewest number of bytes possible. If the value is zero,
        /// return an array of one byte whose element is 0x00.
        /// </summary>
        /// <returns></returns>
        public byte[] ToByteArray() => ToByteArray(isUnsigned: false, isBigEndian: false);

        /// <summary>
        /// Returns the value of this BigInteger as a byte array using the fewest number of bytes possible.
        /// If the value is zero, returns an array of one byte whose element is 0x00.
        /// </summary>
        /// <param name="isUnsigned">Whether or not an unsigned encoding is to be used</param>
        /// <param name="isBigEndian">Whether or not to write the bytes in a big-endian byte order</param>
        /// <returns></returns>
        /// <exception cref="OverflowException">
        ///   If <paramref name="isUnsigned"/> is <c>true</c> and <see cref="Sign"/> is negative.
        /// </exception>
        /// <remarks>
        /// The integer value <c>33022</c> can be exported as four different arrays.
        ///
        /// <list type="bullet">
        ///   <item>
        ///     <description>
        ///       <c>(isUnsigned: false, isBigEndian: false)</c> => <c>new byte[] { 0xFE, 0x80, 0x00 }</c>
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <description>
        ///       <c>(isUnsigned: false, isBigEndian: true)</c> => <c>new byte[] { 0x00, 0x80, 0xFE }</c>
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <description>
        ///       <c>(isUnsigned: true, isBigEndian: false)</c> => <c>new byte[] { 0xFE, 0x80 }</c>
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <description>
        ///       <c>(isUnsigned: true, isBigEndian: true)</c> => <c>new byte[] { 0x80, 0xFE }</c>
        ///     </description>
        ///   </item>
        /// </list>
        /// </remarks>
        public byte[] ToByteArray(bool isUnsigned = false, bool isBigEndian = false)
        {
            int ignored = 0;
            return TryGetBytes(GetBytesMode.AllocateArray, default, isUnsigned, isBigEndian, ref ignored)!;
        }

        /// <summary>
        /// Copies the value of this BigInteger as little-endian twos-complement
        /// bytes, using the fewest number of bytes possible. If the value is zero,
        /// outputs one byte whose element is 0x00.
        /// </summary>
        /// <param name="destination">The destination span to which the resulting bytes should be written.</param>
        /// <param name="bytesWritten">The number of bytes written to <paramref name="destination"/>.</param>
        /// <param name="isUnsigned">Whether or not an unsigned encoding is to be used</param>
        /// <param name="isBigEndian">Whether or not to write the bytes in a big-endian byte order</param>
        /// <returns>true if the bytes fit in <paramref name="destination"/>; false if not all bytes could be written due to lack of space.</returns>
        /// <exception cref="OverflowException">If <paramref name="isUnsigned"/> is <c>true</c> and <see cref="Sign"/> is negative.</exception>
        public bool TryWriteBytes(Span<byte> destination, out int bytesWritten, bool isUnsigned = false, bool isBigEndian = false)
        {
            bytesWritten = 0;
            if (TryGetBytes(GetBytesMode.Span, destination, isUnsigned, isBigEndian, ref bytesWritten) == null)
            {
                bytesWritten = 0;
                return false;
            }
            return true;
        }

        internal bool TryWriteOrCountBytes(Span<byte> destination, out int bytesWritten, bool isUnsigned = false, bool isBigEndian = false)
        {
            bytesWritten = 0;
            return TryGetBytes(GetBytesMode.Span, destination, isUnsigned, isBigEndian, ref bytesWritten) != null;
        }

        /// <summary>Gets the number of bytes that will be output by <see cref="ToByteArray(bool, bool)"/> and <see cref="TryWriteBytes(Span{byte}, out int, bool, bool)"/>.</summary>
        /// <returns>The number of bytes.</returns>
        public int GetByteCount(bool isUnsigned = false)
        {
            int count = 0;
            // Big or Little Endian doesn't matter for the byte count.
            const bool IsBigEndian = false;
            TryGetBytes(GetBytesMode.Count, default(Span<byte>), isUnsigned, IsBigEndian, ref count);
            return count;
        }

        /// <summary>Mode used to enable sharing <see cref="TryGetBytes(GetBytesMode, Span{byte}, bool, bool, ref int)"/> for multiple purposes.</summary>
        private enum GetBytesMode { AllocateArray, Count, Span }

        /// <summary>Shared logic for <see cref="ToByteArray(bool, bool)"/>, <see cref="TryWriteBytes(Span{byte}, out int, bool, bool)"/>, and <see cref="GetByteCount"/>.</summary>
        /// <param name="mode">Which entry point is being used.</param>
        /// <param name="destination">The destination span, if mode is <see cref="GetBytesMode.Span"/>.</param>
        /// <param name="isUnsigned">True to never write a padding byte, false to write it if the high bit is set.</param>
        /// <param name="isBigEndian">True for big endian byte ordering, false for little endian byte ordering.</param>
        /// <param name="bytesWritten">
        /// If <paramref name="mode"/>==<see cref="GetBytesMode.AllocateArray"/>, ignored.
        /// If <paramref name="mode"/>==<see cref="GetBytesMode.Count"/>, the number of bytes that would be written.
        /// If <paramref name="mode"/>==<see cref="GetBytesMode.Span"/>, the number of bytes written to the span or that would be written if it were long enough.
        /// </param>
        /// <returns>
        /// If <paramref name="mode"/>==<see cref="GetBytesMode.AllocateArray"/>, the result array.
        /// If <paramref name="mode"/>==<see cref="GetBytesMode.Count"/>, null.
        /// If <paramref name="mode"/>==<see cref="GetBytesMode.Span"/>, non-null if the span was long enough, null if there wasn't enough room.
        /// </returns>
        /// <exception cref="OverflowException">If <paramref name="isUnsigned"/> is <c>true</c> and <see cref="Sign"/> is negative.</exception>
        private byte[]? TryGetBytes(GetBytesMode mode, Span<byte> destination, bool isUnsigned, bool isBigEndian, ref int bytesWritten)
        {
            Debug.Assert(mode == GetBytesMode.AllocateArray || mode == GetBytesMode.Count || mode == GetBytesMode.Span, $"Unexpected mode {mode}.");
            Debug.Assert(mode == GetBytesMode.Span || destination.IsEmpty, $"If we're not in span mode, we shouldn't have been passed a destination.");

            int sign = _sign;
            if (sign == 0)
            {
                switch (mode)
                {
                    case GetBytesMode.AllocateArray:
                        return new byte[] { 0 };
                    case GetBytesMode.Count:
                        bytesWritten = 1;
                        return null;
                    default: // case GetBytesMode.Span:
                        bytesWritten = 1;
                        if (destination.Length != 0)
                        {
                            destination[0] = 0;
                            return Array.Empty<byte>();
                        }
                        return null;
                }
            }

            if (isUnsigned && sign < 0)
            {
                throw new OverflowException(SR.Overflow_Negative_Unsigned);
            }

            byte highByte;
            int nonZeroDwordIndex = 0;
            uint highDword;
            uint[]? bits = _bits;
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
            bool needExtraByte = (msb & 0x80) != (highByte & 0x80) && !isUnsigned;
            int length = msbIndex + 1 + (needExtraByte ? 1 : 0);
            if (bits != null)
            {
                length = checked(4 * (bits.Length - 1) + length);
            }

            byte[] array;
            switch (mode)
            {
                case GetBytesMode.AllocateArray:
                    destination = array = new byte[length];
                    break;
                case GetBytesMode.Count:
                    bytesWritten = length;
                    return null;
                default: // case GetBytesMode.Span:
                    bytesWritten = length;
                    if (destination.Length < length)
                    {
                        return null;
                    }
                    array = Array.Empty<byte>();
                    break;
            }

            int curByte = isBigEndian ? length - 1 : 0;
            int increment = isBigEndian ? -1 : 1;

            if (bits != null)
            {
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

                    destination[curByte] = unchecked((byte)dword);
                    curByte += increment;
                    destination[curByte] = unchecked((byte)(dword >> 8));
                    curByte += increment;
                    destination[curByte] = unchecked((byte)(dword >> 16));
                    curByte += increment;
                    destination[curByte] = unchecked((byte)(dword >> 24));
                    curByte += increment;
                }
            }

            Debug.Assert(msbIndex >= 0 && msbIndex <= 3);
            destination[curByte] = unchecked((byte)highDword);
            if (msbIndex != 0)
            {
                curByte += increment;
                destination[curByte] = unchecked((byte)(highDword >> 8));
                if (msbIndex != 1)
                {
                    curByte += increment;
                    destination[curByte] = unchecked((byte)(highDword >> 16));
                    if (msbIndex != 2)
                    {
                        curByte += increment;
                        destination[curByte] = unchecked((byte)(highDword >> 24));
                    }
                }
            }

            // Assert we're big endian, or little endian consistency holds.
            Debug.Assert(isBigEndian || (!needExtraByte && curByte == length - 1) || (needExtraByte && curByte == length - 2));
            // Assert we're little endian, or big endian consistency holds.
            Debug.Assert(!isBigEndian || (!needExtraByte && curByte == 0) || (needExtraByte && curByte == 1));

            if (needExtraByte)
            {
                curByte += increment;
                destination[curByte] = highByte;
            }

            return array;
        }

        /// <summary>
        /// Converts the value of this BigInteger to a little-endian twos-complement
        /// uint span allocated by the caller using the fewest number of uints possible.
        /// </summary>
        /// <param name="buffer">Pre-allocated buffer by the caller.</param>
        /// <returns>The actual number of copied elements.</returns>
        private int WriteTo(Span<uint> buffer)
        {
            Debug.Assert(_bits is null || _sign == 0 ? buffer.Length == 2 : buffer.Length >= _bits.Length + 1);

            uint highDWord;

            if (_bits is null)
            {
                buffer[0] = unchecked((uint)_sign);
                highDWord = (_sign < 0) ? uint.MaxValue : 0;
            }
            else
            {
                _bits.CopyTo(buffer);
                buffer = buffer.Slice(0, _bits.Length + 1);
                if (_sign == -1)
                {
                    NumericsHelpers.DangerousMakeTwosComplement(buffer.Slice(0, buffer.Length - 1));  // Mutates dwords
                    highDWord = uint.MaxValue;
                }
                else
                    highDWord = 0;
            }

            // Find highest significant byte and ensure high bit is 0 if positive, 1 if negative
            int msb = buffer.Length - 2;
            while (msb > 0 && buffer[msb] == highDWord)
            {
                msb--;
            }

            // Ensure high bit is 0 if positive, 1 if negative
            bool needExtraByte = (buffer[msb] & 0x80000000) != (highDWord & 0x80000000);
            int count;

            if (needExtraByte)
            {
                count = msb + 2;
                buffer = buffer.Slice(0, count);
                buffer[buffer.Length - 1] = highDWord;
            }
            else
            {
                count = msb + 1;
            }

            return count;
        }

        public override string ToString()
        {
            return BigNumber.FormatBigInteger(this, null, NumberFormatInfo.CurrentInfo);
        }

        public string ToString(IFormatProvider? provider)
        {
            return BigNumber.FormatBigInteger(this, null, NumberFormatInfo.GetInstance(provider));
        }

        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format)
        {
            return BigNumber.FormatBigInteger(this, format, NumberFormatInfo.CurrentInfo);
        }

        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider)
        {
            return BigNumber.FormatBigInteger(this, format, NumberFormatInfo.GetInstance(provider));
        }

        private string DebuggerDisplay
        {
            get
            {
                // For very big numbers, ToString can be too long or even timeout for Visual Studio to display
                // Display a fast estimated value instead

                // Use ToString for small values

                if ((_bits is null) || (_bits.Length <= 4))
                {
                    return ToString();
                }

                // Estimate the value x as `L * 2^n`, while L is the value of high bits, and n is the length of low bits
                // Represent L as `k * 10^i`, then `x = L * 2^n = k * 10^(i + (n * log10(2)))`
                // Let `m = n * log10(2)`, the final result would be `x = (k * 10^(m - [m])) * 10^(i+[m])`

                const double log10Of2 = 0.3010299956639812; // Log10(2)
                ulong highBits = ((ulong)_bits[^1] << kcbitUint) + _bits[^2];
                double lowBitsCount32 = _bits.Length - 2; // if Length > int.MaxValue/32, counting in bits can cause overflow
                double exponentLow = lowBitsCount32 * kcbitUint * log10Of2;

                // Max possible length of _bits is int.MaxValue of bytes,
                // thus max possible value of BigInteger is 2^(8*Array.MaxLength)-1 which is larger than 10^(2^33)
                // Use long to avoid potential overflow
                long exponent = (long)exponentLow;
                double significand = (double)highBits * Math.Pow(10, exponentLow - exponent);

                // scale significand to [1, 10)
                double log10 = Math.Log10(significand);
                if (log10 >= 1)
                {
                    exponent += (long)log10;
                    significand /= Math.Pow(10, Math.Floor(log10));
                }

                // The digits can be incorrect because of floating point errors and estimation in Log and Exp
                // Keep some digits in the significand. 8 is arbitrarily chosen, about half of the precision of double
                significand = Math.Round(significand, 8);

                if (significand >= 10.0)
                {
                    // 9.9999999999999 can be rounded to 10, make the display to be more natural
                    significand /= 10.0;
                    exponent++;
                }

                string signStr = _sign < 0 ? NumberFormatInfo.CurrentInfo.NegativeSign : "";

                // Use about a half of the precision of double
                return $"{signStr}{significand:F8}e+{exponent}";
            }
        }

        public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        {
            return BigNumber.TryFormatBigInteger(this, format, NumberFormatInfo.GetInstance(provider), destination, out charsWritten);
        }

        private static BigInteger Add(ReadOnlySpan<uint> leftBits, int leftSign, ReadOnlySpan<uint> rightBits, int rightSign)
        {
            bool trivialLeft = leftBits.IsEmpty;
            bool trivialRight = rightBits.IsEmpty;

            if (trivialLeft && trivialRight)
            {
                return (long)leftSign + rightSign;
            }

            BigInteger result;
            uint[]? bitsFromPool = null;

            if (trivialLeft)
            {
                Debug.Assert(!rightBits.IsEmpty);

                int size = rightBits.Length + 1;
                Span<uint> bits = ((uint)size <= BigIntegerCalculator.StackAllocThreshold
                                ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                                : bitsFromPool = ArrayPool<uint>.Shared.Rent(size)).Slice(0, size);

                BigIntegerCalculator.Add(rightBits, NumericsHelpers.Abs(leftSign), bits);
                result = new BigInteger(bits, leftSign < 0);
            }
            else if (trivialRight)
            {
                Debug.Assert(!leftBits.IsEmpty);

                int size = leftBits.Length + 1;
                Span<uint> bits = ((uint)size <= BigIntegerCalculator.StackAllocThreshold
                                ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                                : bitsFromPool = ArrayPool<uint>.Shared.Rent(size)).Slice(0, size);

                BigIntegerCalculator.Add(leftBits, NumericsHelpers.Abs(rightSign), bits);
                result = new BigInteger(bits, leftSign < 0);
            }
            else if (leftBits.Length < rightBits.Length)
            {
                Debug.Assert(!leftBits.IsEmpty && !rightBits.IsEmpty);

                int size = rightBits.Length + 1;
                Span<uint> bits = ((uint)size <= BigIntegerCalculator.StackAllocThreshold
                                ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                                : bitsFromPool = ArrayPool<uint>.Shared.Rent(size)).Slice(0, size);

                BigIntegerCalculator.Add(rightBits, leftBits, bits);
                result = new BigInteger(bits, leftSign < 0);
            }
            else
            {
                Debug.Assert(!leftBits.IsEmpty && !rightBits.IsEmpty);

                int size = leftBits.Length + 1;
                Span<uint> bits = ((uint)size <= BigIntegerCalculator.StackAllocThreshold
                                ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                                : bitsFromPool = ArrayPool<uint>.Shared.Rent(size)).Slice(0, size);

                BigIntegerCalculator.Add(leftBits, rightBits, bits);
                result = new BigInteger(bits, leftSign < 0);
            }

            if (bitsFromPool != null)
                    ArrayPool<uint>.Shared.Return(bitsFromPool);

            return result;
        }

        public static BigInteger operator -(BigInteger left, BigInteger right)
        {
            left.AssertValid();
            right.AssertValid();

            if (left._sign < 0 != right._sign < 0)
                return Add(left._bits, left._sign, right._bits, -1 * right._sign);
            return Subtract(left._bits, left._sign, right._bits, right._sign);
        }

        private static BigInteger Subtract(ReadOnlySpan<uint> leftBits, int leftSign, ReadOnlySpan<uint> rightBits, int rightSign)
        {
            bool trivialLeft = leftBits.IsEmpty;
            bool trivialRight = rightBits.IsEmpty;

            if (trivialLeft && trivialRight)
            {
                return (long)leftSign - rightSign;
            }

            BigInteger result;
            uint[]? bitsFromPool = null;

            if (trivialLeft)
            {
                Debug.Assert(!rightBits.IsEmpty);

                int size = rightBits.Length;
                Span<uint> bits = (size <= BigIntegerCalculator.StackAllocThreshold
                                ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                                : bitsFromPool = ArrayPool<uint>.Shared.Rent(size)).Slice(0, size);

                BigIntegerCalculator.Subtract(rightBits, NumericsHelpers.Abs(leftSign), bits);
                result = new BigInteger(bits, leftSign >= 0);
            }
            else if (trivialRight)
            {
                Debug.Assert(!leftBits.IsEmpty);

                int size = leftBits.Length;
                Span<uint> bits = (size <= BigIntegerCalculator.StackAllocThreshold
                                ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                                : bitsFromPool = ArrayPool<uint>.Shared.Rent(size)).Slice(0, size);

                BigIntegerCalculator.Subtract(leftBits, NumericsHelpers.Abs(rightSign), bits);
                result = new BigInteger(bits, leftSign < 0);
            }
            else if (BigIntegerCalculator.Compare(leftBits, rightBits) < 0)
            {
                int size = rightBits.Length;
                Span<uint> bits = (size <= BigIntegerCalculator.StackAllocThreshold
                                ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                                : bitsFromPool = ArrayPool<uint>.Shared.Rent(size)).Slice(0, size);

                BigIntegerCalculator.Subtract(rightBits, leftBits, bits);
                result = new BigInteger(bits, leftSign >= 0);
            }
            else
            {
                Debug.Assert(!leftBits.IsEmpty && !rightBits.IsEmpty);

                int size = leftBits.Length;
                Span<uint> bits = (size <= BigIntegerCalculator.StackAllocThreshold
                                ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                                : bitsFromPool = ArrayPool<uint>.Shared.Rent(size)).Slice(0, size);

                BigIntegerCalculator.Subtract(leftBits, rightBits, bits);
                result = new BigInteger(bits, leftSign < 0);
            }

            if (bitsFromPool != null)
                ArrayPool<uint>.Shared.Return(bitsFromPool);

            return result;
        }

        //
        // Explicit Conversions From BigInteger
        //

        public static explicit operator byte(BigInteger value)
        {
            return checked((byte)((int)value));
        }

        /// <summary>Explicitly converts a big integer to a <see cref="char" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to <see cref="char" /> value.</returns>
        public static explicit operator char(BigInteger value)
        {
            return checked((char)((int)value));
        }

        public static explicit operator decimal(BigInteger value)
        {
            value.AssertValid();
            if (value._bits == null)
                return value._sign;

            int length = value._bits.Length;
            if (length > 3) throw new OverflowException(SR.Overflow_Decimal);

            int lo = 0, mi = 0, hi = 0;

            unchecked
            {
                if (length > 2) hi = (int)value._bits[2];
                if (length > 1) mi = (int)value._bits[1];
                if (length > 0) lo = (int)value._bits[0];
            }

            return new decimal(lo, mi, hi, value._sign < 0, 0);
        }

        public static explicit operator double(BigInteger value)
        {
            value.AssertValid();

            int sign = value._sign;
            uint[]? bits = value._bits;

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

            int z = BitOperations.LeadingZeroCount((uint)h);

            int exp = (length - 2) * 32 - z;
            ulong man = (h << 32 + z) | (m << z) | (l >> 32 - z);

            return NumericsHelpers.GetDoubleFromParts(sign, exp, man);
        }

        /// <summary>Explicitly converts a big integer to a <see cref="Half" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to <see cref="Half" /> value.</returns>
        public static explicit operator Half(BigInteger value)
        {
            return (Half)(double)value;
        }

        public static explicit operator short(BigInteger value)
        {
            return checked((short)((int)value));
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
                uu = NumericsHelpers.MakeUInt64(value._bits[1], value._bits[0]);
            }
            else
            {
                uu = value._bits[0];
            }

            long ll = value._sign > 0 ? unchecked((long)uu) : unchecked(-(long)uu);
            if ((ll > 0 && value._sign > 0) || (ll < 0 && value._sign < 0))
            {
                // Signs match, no overflow
                return ll;
            }
            throw new OverflowException(SR.Overflow_Int64);
        }

        /// <summary>Explicitly converts a big integer to a <see cref="Int128" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to <see cref="Int128" /> value.</returns>
        public static explicit operator Int128(BigInteger value)
        {
            value.AssertValid();

            if (value._bits is null)
            {
                return value._sign;
            }

            int len = value._bits.Length;

            if (len > 4)
            {
                throw new OverflowException(SR.Overflow_Int128);
            }

            UInt128 uu;

            if (len > 2)
            {
                uu = new UInt128(
                    NumericsHelpers.MakeUInt64((len > 3) ? value._bits[3] : 0, value._bits[2]),
                    NumericsHelpers.MakeUInt64(value._bits[1], value._bits[0])
                );
            }
            else if (len > 1)
            {
                uu = NumericsHelpers.MakeUInt64(value._bits[1], value._bits[0]);
            }
            else
            {
                uu = value._bits[0];
            }

            Int128 ll = (value._sign > 0) ? unchecked((Int128)uu) : unchecked(-(Int128)uu);

            if (((ll > 0) && (value._sign > 0)) || ((ll < 0) && (value._sign < 0)))
            {
                // Signs match, no overflow
                return ll;
            }
            throw new OverflowException(SR.Overflow_Int128);
        }

        /// <summary>Explicitly converts a big integer to a <see cref="IntPtr" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to <see cref="IntPtr" /> value.</returns>
        public static explicit operator nint(BigInteger value)
        {
            if (Environment.Is64BitProcess)
            {
                return (nint)(long)value;
            }
            else
            {
                return (int)value;
            }
        }

        [CLSCompliant(false)]
        public static explicit operator sbyte(BigInteger value)
        {
            return checked((sbyte)((int)value));
        }

        public static explicit operator float(BigInteger value)
        {
            return (float)((double)value);
        }

        [CLSCompliant(false)]
        public static explicit operator ushort(BigInteger value)
        {
            return checked((ushort)((int)value));
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
                return NumericsHelpers.MakeUInt64(value._bits[1], value._bits[0]);
            }
            return value._bits[0];
        }

        /// <summary>Explicitly converts a big integer to a <see cref="UInt128" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to <see cref="UInt128" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator UInt128(BigInteger value)
        {
            value.AssertValid();

            if (value._bits is null)
            {
                return checked((UInt128)value._sign);
            }

            int len = value._bits.Length;

            if ((len > 4) || (value._sign < 0))
            {
                throw new OverflowException(SR.Overflow_UInt128);
            }

            if (len > 2)
            {
                return new UInt128(
                    NumericsHelpers.MakeUInt64((len > 3) ? value._bits[3] : 0, value._bits[2]),
                    NumericsHelpers.MakeUInt64(value._bits[1], value._bits[0])
                );
            }
            else if (len > 1)
            {
                return NumericsHelpers.MakeUInt64(value._bits[1], value._bits[0]);
            }
            return value._bits[0];
        }

        /// <summary>Explicitly converts a big integer to a <see cref="UIntPtr" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to <see cref="UIntPtr" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator nuint(BigInteger value)
        {
            if (Environment.Is64BitProcess)
            {
                return (nuint)(ulong)value;
            }
            else
            {
                return (uint)value;
            }
        }

        //
        // Explicit Conversions To BigInteger
        //

        public static explicit operator BigInteger(decimal value)
        {
            return new BigInteger(value);
        }

        public static explicit operator BigInteger(double value)
        {
            return new BigInteger(value);
        }

        /// <summary>Explicitly converts a <see cref="Half" /> value to a big integer.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a big integer.</returns>
        public static explicit operator BigInteger(Half value)
        {
            return new BigInteger((float)value);
        }

        /// <summary>Explicitly converts a <see cref="Complex" /> value to a big integer.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a big integer.</returns>
        public static explicit operator BigInteger(Complex value)
        {
            if (value.Imaginary != 0)
            {
                ThrowHelper.ThrowOverflowException();
            }
            return (BigInteger)value.Real;
        }

        public static explicit operator BigInteger(float value)
        {
            return new BigInteger(value);
        }

        //
        // Implicit Conversions To BigInteger
        //

        public static implicit operator BigInteger(byte value)
        {
            return new BigInteger(value);
        }

        /// <summary>Implicitly converts a <see cref="char" /> value to a big integer.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a big integer.</returns>
        public static implicit operator BigInteger(char value)
        {
            return new BigInteger(value);
        }

        public static implicit operator BigInteger(short value)
        {
            return new BigInteger(value);
        }

        public static implicit operator BigInteger(int value)
        {
            return new BigInteger(value);
        }

        public static implicit operator BigInteger(long value)
        {
            return new BigInteger(value);
        }

        /// <summary>Implicitly converts a <see cref="Int128" /> value to a big integer.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a big integer.</returns>
        public static implicit operator BigInteger(Int128 value)
        {
            int sign;
            uint[]? bits;

            if ((int.MinValue < value) && (value <= int.MaxValue))
            {
                sign = (int)value;
                bits = null;
            }
            else if (value == int.MinValue)
            {
                return s_bnMinInt;
            }
            else
            {
                UInt128 x;
                if (value < 0)
                {
                    x = unchecked((UInt128)(-value));
                    sign = -1;
                }
                else
                {
                    x = (UInt128)value;
                    sign = +1;
                }

                if (x <= uint.MaxValue)
                {
                    bits = new uint[1];
                    bits[0] = (uint)(x >> (kcbitUint * 0));
                }
                else if (x <= ulong.MaxValue)
                {
                    bits = new uint[2];
                    bits[0] = (uint)(x >> (kcbitUint * 0));
                    bits[1] = (uint)(x >> (kcbitUint * 1));
                }
                else if (x <= new UInt128(0x0000_0000_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF))
                {
                    bits = new uint[3];
                    bits[0] = (uint)(x >> (kcbitUint * 0));
                    bits[1] = (uint)(x >> (kcbitUint * 1));
                    bits[2] = (uint)(x >> (kcbitUint * 2));
                }
                else
                {
                    bits = new uint[4];
                    bits[0] = (uint)(x >> (kcbitUint * 0));
                    bits[1] = (uint)(x >> (kcbitUint * 1));
                    bits[2] = (uint)(x >> (kcbitUint * 2));
                    bits[3] = (uint)(x >> (kcbitUint * 3));
                }
            }

            return new BigInteger(sign, bits);
        }

        /// <summary>Implicitly converts a <see cref="IntPtr" /> value to a big integer.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a big integer.</returns>
        public static implicit operator BigInteger(nint value)
        {
            if (Environment.Is64BitProcess)
            {
                return new BigInteger(value);
            }
            else
            {
                return new BigInteger((int)value);
            }
        }

        [CLSCompliant(false)]
        public static implicit operator BigInteger(sbyte value)
        {
            return new BigInteger(value);
        }

        [CLSCompliant(false)]
        public static implicit operator BigInteger(ushort value)
        {
            return new BigInteger(value);
        }

        [CLSCompliant(false)]
        public static implicit operator BigInteger(uint value)
        {
            return new BigInteger(value);
        }

        [CLSCompliant(false)]
        public static implicit operator BigInteger(ulong value)
        {
            return new BigInteger(value);
        }

        /// <summary>Implicitly converts a <see cref="UInt128" /> value to a big integer.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a big integer.</returns>
        [CLSCompliant(false)]
        public static implicit operator BigInteger(UInt128 value)
        {
            int sign = +1;
            uint[]? bits;

            if (value <= (uint)int.MaxValue)
            {
                sign = (int)value;
                bits = null;
            }
            else if (value <= uint.MaxValue)
            {
                bits = new uint[1];
                bits[0] = (uint)(value >> (kcbitUint * 0));
            }
            else if (value <= ulong.MaxValue)
            {
                bits = new uint[2];
                bits[0] = (uint)(value >> (kcbitUint * 0));
                bits[1] = (uint)(value >> (kcbitUint * 1));
            }
            else if (value <= new UInt128(0x0000_0000_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF))
            {
                bits = new uint[3];
                bits[0] = (uint)(value >> (kcbitUint * 0));
                bits[1] = (uint)(value >> (kcbitUint * 1));
                bits[2] = (uint)(value >> (kcbitUint * 2));
            }
            else
            {
                bits = new uint[4];
                bits[0] = (uint)(value >> (kcbitUint * 0));
                bits[1] = (uint)(value >> (kcbitUint * 1));
                bits[2] = (uint)(value >> (kcbitUint * 2));
                bits[3] = (uint)(value >> (kcbitUint * 3));
            }

            return new BigInteger(sign, bits);
        }

        /// <summary>Implicitly converts a <see cref="UIntPtr" /> value to a big integer.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a big integer.</returns>
        [CLSCompliant(false)]
        public static implicit operator BigInteger(nuint value)
        {
            if (Environment.Is64BitProcess)
            {
                return new BigInteger(value);
            }
            else
            {
                return new BigInteger((uint)value);
            }
        }

        public static BigInteger operator &(BigInteger left, BigInteger right)
        {
            if (left.IsZero || right.IsZero)
            {
                return Zero;
            }

            if (left._bits is null && right._bits is null)
            {
                return left._sign & right._sign;
            }

            uint xExtend = (left._sign < 0) ? uint.MaxValue : 0;
            uint yExtend = (right._sign < 0) ? uint.MaxValue : 0;

            uint[]? leftBufferFromPool = null;
            int size = (left._bits?.Length ?? 1) + 1;
            Span<uint> x = ((uint)size <= BigIntegerCalculator.StackAllocThreshold
                         ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                         : leftBufferFromPool = ArrayPool<uint>.Shared.Rent(size)).Slice(0, size);
            x = x.Slice(0, left.WriteTo(x));

            uint[]? rightBufferFromPool = null;
            size = (right._bits?.Length ?? 1) + 1;
            Span<uint> y = ((uint)size <= BigIntegerCalculator.StackAllocThreshold
                         ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                         : rightBufferFromPool = ArrayPool<uint>.Shared.Rent(size)).Slice(0, size);
            y = y.Slice(0, right.WriteTo(y));

            uint[]? resultBufferFromPool = null;
            size = Math.Max(x.Length, y.Length);
            Span<uint> z = (size <= BigIntegerCalculator.StackAllocThreshold
                         ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                         : resultBufferFromPool = ArrayPool<uint>.Shared.Rent(size)).Slice(0, size);

            for (int i = 0; i < z.Length; i++)
            {
                uint xu = ((uint)i < (uint)x.Length) ? x[i] : xExtend;
                uint yu = ((uint)i < (uint)y.Length) ? y[i] : yExtend;
                z[i] = xu & yu;
            }

            if (leftBufferFromPool != null)
                ArrayPool<uint>.Shared.Return(leftBufferFromPool);

            if (rightBufferFromPool != null)
                ArrayPool<uint>.Shared.Return(rightBufferFromPool);

            var result = new BigInteger(z);

            if (resultBufferFromPool != null)
                ArrayPool<uint>.Shared.Return(resultBufferFromPool);

            return result;
        }

        public static BigInteger operator |(BigInteger left, BigInteger right)
        {
            if (left.IsZero)
                return right;
            if (right.IsZero)
                return left;

            if (left._bits is null && right._bits is null)
            {
                return left._sign | right._sign;
            }

            uint xExtend = (left._sign < 0) ? uint.MaxValue : 0;
            uint yExtend = (right._sign < 0) ? uint.MaxValue : 0;

            uint[]? leftBufferFromPool = null;
            int size = (left._bits?.Length ?? 1) + 1;
            Span<uint> x = ((uint)size <= BigIntegerCalculator.StackAllocThreshold
                         ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                         : leftBufferFromPool = ArrayPool<uint>.Shared.Rent(size)).Slice(0, size);
            x = x.Slice(0, left.WriteTo(x));

            uint[]? rightBufferFromPool = null;
            size = (right._bits?.Length ?? 1) + 1;
            Span<uint> y = ((uint)size <= BigIntegerCalculator.StackAllocThreshold
                         ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                         : rightBufferFromPool = ArrayPool<uint>.Shared.Rent(size)).Slice(0, size);
            y = y.Slice(0, right.WriteTo(y));

            uint[]? resultBufferFromPool = null;
            size = Math.Max(x.Length, y.Length);
            Span<uint> z = (size <= BigIntegerCalculator.StackAllocThreshold
                         ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                         : resultBufferFromPool = ArrayPool<uint>.Shared.Rent(size)).Slice(0, size);

            for (int i = 0; i < z.Length; i++)
            {
                uint xu = ((uint)i < (uint)x.Length) ? x[i] : xExtend;
                uint yu = ((uint)i < (uint)y.Length) ? y[i] : yExtend;
                z[i] = xu | yu;
            }

            if (leftBufferFromPool != null)
                ArrayPool<uint>.Shared.Return(leftBufferFromPool);

            if (rightBufferFromPool != null)
                ArrayPool<uint>.Shared.Return(rightBufferFromPool);

            var result = new BigInteger(z);

            if (resultBufferFromPool != null)
                ArrayPool<uint>.Shared.Return(resultBufferFromPool);

            return result;
        }

        public static BigInteger operator ^(BigInteger left, BigInteger right)
        {
            if (left._bits is null && right._bits is null)
            {
                return left._sign ^ right._sign;
            }

            uint xExtend = (left._sign < 0) ? uint.MaxValue : 0;
            uint yExtend = (right._sign < 0) ? uint.MaxValue : 0;

            uint[]? leftBufferFromPool = null;
            int size = (left._bits?.Length ?? 1) + 1;
            Span<uint> x = ((uint)size <= BigIntegerCalculator.StackAllocThreshold
                         ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                         : leftBufferFromPool = ArrayPool<uint>.Shared.Rent(size)).Slice(0, size);
            x = x.Slice(0, left.WriteTo(x));

            uint[]? rightBufferFromPool = null;
            size = (right._bits?.Length ?? 1) + 1;
            Span<uint> y = ((uint)size <= BigIntegerCalculator.StackAllocThreshold
                         ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                         : rightBufferFromPool = ArrayPool<uint>.Shared.Rent(size)).Slice(0, size);
            y = y.Slice(0, right.WriteTo(y));

            uint[]? resultBufferFromPool = null;
            size = Math.Max(x.Length, y.Length);
            Span<uint> z = (size <= BigIntegerCalculator.StackAllocThreshold
                         ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                         : resultBufferFromPool = ArrayPool<uint>.Shared.Rent(size)).Slice(0, size);

            for (int i = 0; i < z.Length; i++)
            {
                uint xu = ((uint)i < (uint)x.Length) ? x[i] : xExtend;
                uint yu = ((uint)i < (uint)y.Length) ? y[i] : yExtend;
                z[i] = xu ^ yu;
            }

            if (leftBufferFromPool != null)
                ArrayPool<uint>.Shared.Return(leftBufferFromPool);

            if (rightBufferFromPool != null)
                ArrayPool<uint>.Shared.Return(rightBufferFromPool);

            var result = new BigInteger(z);

            if (resultBufferFromPool != null)
                ArrayPool<uint>.Shared.Return(resultBufferFromPool);

            return result;
        }

        public static BigInteger operator <<(BigInteger value, int shift)
        {
            if (shift == 0)
                return value;

            if (shift == int.MinValue)
                return ((value >> int.MaxValue) >> 1);

            if (shift < 0)
                return value >> -shift;

            (int digitShift, int smallShift) = Math.DivRem(shift, kcbitUint);

            uint[]? xdFromPool = null;
            int xl = value._bits?.Length ?? 1;
            Span<uint> xd = (xl <= BigIntegerCalculator.StackAllocThreshold
                          ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                          : xdFromPool = ArrayPool<uint>.Shared.Rent(xl)).Slice(0, xl);
            bool negx = value.GetPartsForBitManipulation(xd);

            int zl = xl + digitShift + 1;
            uint[]? zdFromPool = null;
            Span<uint> zd = ((uint)zl <= BigIntegerCalculator.StackAllocThreshold
                          ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                          : zdFromPool = ArrayPool<uint>.Shared.Rent(zl)).Slice(0, zl);
            zd.Clear();

            uint carry = 0;
            if (smallShift == 0)
            {
                for (int i = 0; i < xd.Length; i++)
                {
                    zd[i + digitShift] = xd[i];
                }
            }
            else
            {
                int carryShift = kcbitUint - smallShift;
                int i;
                for (i = 0; i < xd.Length; i++)
                {
                    uint rot = xd[i];
                    zd[i + digitShift] = rot << smallShift | carry;
                    carry = rot >> carryShift;
                }
            }

            zd[zd.Length - 1] = carry;

            var result = new BigInteger(zd, negx);

            if (xdFromPool != null)
                ArrayPool<uint>.Shared.Return(xdFromPool);
            if (zdFromPool != null)
                ArrayPool<uint>.Shared.Return(zdFromPool);

            return result;
        }

        public static BigInteger operator >>(BigInteger value, int shift)
        {
            if (shift == 0)
                return value;

            if (shift == int.MinValue)
                return ((value << int.MaxValue) << 1);

            if (shift < 0)
                return value << -shift;

            (int digitShift, int smallShift) = Math.DivRem(shift, kcbitUint);

            BigInteger result;

            uint[]? xdFromPool = null;
            int xl = value._bits?.Length ?? 1;
            Span<uint> xd = (xl <= BigIntegerCalculator.StackAllocThreshold
                          ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                          : xdFromPool = ArrayPool<uint>.Shared.Rent(xl)).Slice(0, xl);

            bool negx = value.GetPartsForBitManipulation(xd);
            bool trackSignBit = false;

            if (negx)
            {
                if (shift >= ((long)kcbitUint * xd.Length))
                {
                    result = MinusOne;
                    goto exit;
                }

                NumericsHelpers.DangerousMakeTwosComplement(xd); // Mutates xd

                // For a shift of N x 32 bit,
                // We check for a special case where its sign bit could be outside the uint array after 2's complement conversion.
                // For example given [0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF], its 2's complement is [0x01, 0x00, 0x00]
                // After a 32 bit right shift, it becomes [0x00, 0x00] which is [0x00, 0x00] when converted back.
                // The expected result is [0x00, 0x00, 0xFFFFFFFF] (2's complement) or [0x00, 0x00, 0x01] when converted back
                // If the 2's component's last element is a 0, we will track the sign externally
                trackSignBit = smallShift == 0 && xd[xd.Length - 1] == 0;
            }

            uint[]? zdFromPool = null;
            int zl = Math.Max(xl - digitShift, 0) + (trackSignBit ? 1 : 0);
            Span<uint> zd = ((uint)zl <= BigIntegerCalculator.StackAllocThreshold
                          ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                          : zdFromPool = ArrayPool<uint>.Shared.Rent(zl)).Slice(0, zl);
            zd.Clear();

            if (smallShift == 0)
            {
                for (int i = xd.Length - 1; i >= digitShift; i--)
                {
                    zd[i - digitShift] = xd[i];
                }
            }
            else
            {
                int carryShift = kcbitUint - smallShift;
                uint carry = 0;
                for (int i = xd.Length - 1; i >= digitShift; i--)
                {
                    uint rot = xd[i];
                    if (negx && i == xd.Length - 1)
                        // Sign-extend the first shift for negative ints then let the carry propagate
                        zd[i - digitShift] = (rot >> smallShift) | (0xFFFFFFFF << carryShift);
                    else
                        zd[i - digitShift] = (rot >> smallShift) | carry;
                    carry = rot << carryShift;
                }
            }

            if (negx)
            {
                // Set the tracked sign to the last element
                if (trackSignBit)
                    zd[zd.Length - 1] = 0xFFFFFFFF;

                NumericsHelpers.DangerousMakeTwosComplement(zd); // Mutates zd
            }

            result = new BigInteger(zd, negx);

            if (zdFromPool != null)
                ArrayPool<uint>.Shared.Return(zdFromPool);
        exit:
            if (xdFromPool != null)
                ArrayPool<uint>.Shared.Return(xdFromPool);

            return result;
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

            return Multiply(left._bits, left._sign, right._bits, right._sign);
        }

        private static BigInteger Multiply(ReadOnlySpan<uint> left, int leftSign, ReadOnlySpan<uint> right, int rightSign)
        {
            bool trivialLeft = left.IsEmpty;
            bool trivialRight = right.IsEmpty;

            if (trivialLeft && trivialRight)
            {
                return (long)leftSign * rightSign;
            }

            BigInteger result;
            uint[]? bitsFromPool = null;

            if (trivialLeft)
            {
                Debug.Assert(!right.IsEmpty);

                int size = right.Length + 1;
                Span<uint> bits = ((uint)size <= BigIntegerCalculator.StackAllocThreshold
                                ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                                : bitsFromPool = ArrayPool<uint>.Shared.Rent(size)).Slice(0, size);

                BigIntegerCalculator.Multiply(right, NumericsHelpers.Abs(leftSign), bits);
                result = new BigInteger(bits, (leftSign < 0) ^ (rightSign < 0));
            }
            else if (trivialRight)
            {
                Debug.Assert(!left.IsEmpty);

                int size = left.Length + 1;
                Span<uint> bits = ((uint)size <= BigIntegerCalculator.StackAllocThreshold
                                ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                                : bitsFromPool = ArrayPool<uint>.Shared.Rent(size)).Slice(0, size);

                BigIntegerCalculator.Multiply(left, NumericsHelpers.Abs(rightSign), bits);
                result = new BigInteger(bits, (leftSign < 0) ^ (rightSign < 0));
            }
            else if (left == right)
            {
                int size = left.Length + right.Length;
                Span<uint> bits = ((uint)size <= BigIntegerCalculator.StackAllocThreshold
                                ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                                : bitsFromPool = ArrayPool<uint>.Shared.Rent(size)).Slice(0, size);

                BigIntegerCalculator.Square(left, bits);
                result = new BigInteger(bits, (leftSign < 0) ^ (rightSign < 0));
            }
            else if (left.Length < right.Length)
            {
                Debug.Assert(!left.IsEmpty && !right.IsEmpty);

                int size = left.Length + right.Length;
                Span<uint> bits = ((uint)size <= BigIntegerCalculator.StackAllocThreshold
                                ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                                : bitsFromPool = ArrayPool<uint>.Shared.Rent(size)).Slice(0, size);
                bits.Clear();

                BigIntegerCalculator.Multiply(right, left, bits);
                result = new BigInteger(bits, (leftSign < 0) ^ (rightSign < 0));
            }
            else
            {
                Debug.Assert(!left.IsEmpty && !right.IsEmpty);

                int size = left.Length + right.Length;
                Span<uint> bits = ((uint)size <= BigIntegerCalculator.StackAllocThreshold
                                ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                                : bitsFromPool = ArrayPool<uint>.Shared.Rent(size)).Slice(0, size);
                bits.Clear();

                BigIntegerCalculator.Multiply(left, right, bits);
                result = new BigInteger(bits, (leftSign < 0) ^ (rightSign < 0));
            }

            if (bitsFromPool != null)
                ArrayPool<uint>.Shared.Return(bitsFromPool);

            return result;
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

            uint[]? quotientFromPool = null;

            if (trivialDivisor)
            {
                Debug.Assert(dividend._bits != null);

                int size = dividend._bits.Length;
                Span<uint> quotient = ((uint)size <= BigIntegerCalculator.StackAllocThreshold
                                    ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                                    : quotientFromPool = ArrayPool<uint>.Shared.Rent(size)).Slice(0, size);

                try
                {
                    //may throw DivideByZeroException
                    BigIntegerCalculator.Divide(dividend._bits, NumericsHelpers.Abs(divisor._sign), quotient);
                    return new BigInteger(quotient, (dividend._sign < 0) ^ (divisor._sign < 0));
                }
                finally
                {
                    if (quotientFromPool != null)
                        ArrayPool<uint>.Shared.Return(quotientFromPool);
                }
            }

            Debug.Assert(dividend._bits != null && divisor._bits != null);

            if (dividend._bits.Length < divisor._bits.Length)
            {
                return s_bnZeroInt;
            }
            else
            {
                int size = dividend._bits.Length - divisor._bits.Length + 1;
                Span<uint> quotient = ((uint)size < BigIntegerCalculator.StackAllocThreshold
                                    ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                                    : quotientFromPool = ArrayPool<uint>.Shared.Rent(size)).Slice(0, size);

                BigIntegerCalculator.Divide(dividend._bits, divisor._bits, quotient);
                var result = new BigInteger(quotient, (dividend._sign < 0) ^ (divisor._sign < 0));

                if (quotientFromPool != null)
                    ArrayPool<uint>.Shared.Return(quotientFromPool);

                return result;
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
                Debug.Assert(dividend._bits != null);
                uint remainder = BigIntegerCalculator.Remainder(dividend._bits, NumericsHelpers.Abs(divisor._sign));
                return dividend._sign < 0 ? -1 * remainder : remainder;
            }

            Debug.Assert(dividend._bits != null && divisor._bits != null);

            if (dividend._bits.Length < divisor._bits.Length)
            {
                return dividend;
            }

            uint[]? bitsFromPool = null;
            int size = dividend._bits.Length;
            Span<uint> bits = (size <= BigIntegerCalculator.StackAllocThreshold
                            ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                            : bitsFromPool = ArrayPool<uint>.Shared.Rent(size)).Slice(0, size);

            BigIntegerCalculator.Remainder(dividend._bits, divisor._bits, bits);
            var result = new BigInteger(bits, dividend._sign < 0);

            if (bitsFromPool != null)
                ArrayPool<uint>.Shared.Return(bitsFromPool);

            return result;
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
        /// Gets the number of bits required for shortest two's complement representation of the current instance without the sign bit.
        /// </summary>
        /// <returns>The minimum non-negative number of bits in two's complement notation without the sign bit.</returns>
        /// <remarks>This method returns 0 iff the value of current object is equal to <see cref="Zero"/> or <see cref="MinusOne"/>. For positive integers the return value is equal to the ordinary binary representation string length.</remarks>
        public long GetBitLength()
        {
            AssertValid();

            uint highValue;
            int bitsArrayLength;
            int sign = _sign;
            uint[]? bits = _bits;

            if (bits == null)
            {
                bitsArrayLength = 1;
                highValue = (uint)(sign < 0 ? -sign : sign);
            }
            else
            {
                bitsArrayLength = bits.Length;
                highValue = bits[bitsArrayLength - 1];
            }

            long bitLength = bitsArrayLength * 32L - BitOperations.LeadingZeroCount(highValue);

            if (sign >= 0)
                return bitLength;

            // When negative and IsPowerOfTwo, the answer is (bitLength - 1)

            // Check highValue
            if ((highValue & (highValue - 1)) != 0)
                return bitLength;

            // Check the rest of the bits (if present)
            for (int i = bitsArrayLength - 2; i >= 0; i--)
            {
                // bits array is always non-null when bitsArrayLength >= 2
                if (bits![i] == 0)
                    continue;

                return bitLength;
            }

            return bitLength - 1;
        }

        /// <summary>
        /// Encapsulate the logic of normalizing the "small" and "large" forms of BigInteger
        /// into the "large" form so that Bit Manipulation algorithms can be simplified.
        /// </summary>
        /// <param name="xd">
        /// The UInt32 array containing the entire big integer in "large" (denormalized) form.
        /// E.g., the number one (1) and negative one (-1) are both stored as 0x00000001
        /// BigInteger values Int32.MinValue &lt; x &lt;= Int32.MaxValue are converted to this
        /// format for convenience.
        /// </param>
        /// <returns>True for negative numbers.</returns>
        private bool GetPartsForBitManipulation(Span<uint> xd)
        {
            Debug.Assert(_bits is null ? xd.Length == 1 : xd.Length == _bits.Length);

            if (_bits is null)
            {
                xd[0] = (uint)(_sign < 0 ? -_sign : _sign);
            }
            else
            {
                _bits.CopyTo(xd);
            }
            return _sign < 0;
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
                //// Wasted space: leading zeros could have been truncated // TODO: https://github.com/dotnet/runtime/issues/84991
                //Debug.Assert(_bits[_bits.Length - 1] != 0);
                // Arrays larger than this can't fit into a Span<byte>
                Debug.Assert(_bits.Length <= MaxLength);
            }
            else
            {
                // Int32.MinValue should not be stored in the _sign field
                Debug.Assert(_sign > int.MinValue);
            }
        }

        //
        // IAdditiveIdentity
        //

        /// <inheritdoc cref="IAdditiveIdentity{TSelf, TResult}.AdditiveIdentity" />
        static BigInteger IAdditiveIdentity<BigInteger, BigInteger>.AdditiveIdentity => Zero;

        //
        // IBinaryInteger
        //

        /// <inheritdoc cref="IBinaryInteger{TSelf}.DivRem(TSelf, TSelf)" />
        public static (BigInteger Quotient, BigInteger Remainder) DivRem(BigInteger left, BigInteger right)
        {
            BigInteger quotient = DivRem(left, right, out BigInteger remainder);
            return (quotient, remainder);
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.LeadingZeroCount(TSelf)" />
        public static BigInteger LeadingZeroCount(BigInteger value)
        {
            value.AssertValid();

            if (value._bits is null)
            {
                return int.LeadingZeroCount(value._sign);
            }

            // When the value is positive, we just need to get the lzcnt of the most significant bits
            // Otherwise, we're negative and the most significant bit is always set.

            return (value._sign >= 0) ? uint.LeadingZeroCount(value._bits[^1]) : 0;
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.PopCount(TSelf)" />
        public static BigInteger PopCount(BigInteger value)
        {
            value.AssertValid();

            if (value._bits is null)
            {
                return int.PopCount(value._sign);
            }

            ulong result = 0;

            if (value._sign >= 0)
            {
                // When the value is positive, we simply need to do a popcount for all bits

                for (int i = 0; i < value._bits.Length; i++)
                {
                    uint part = value._bits[i];
                    result += uint.PopCount(part);
                }
            }
            else
            {
                // When the value is negative, we need to popcount the two's complement representation
                // We'll do this "inline" to avoid needing to unnecessarily allocate.

                int i = 0;
                uint part;

                do
                {
                    // Simply process bits, adding the carry while the previous value is zero

                    part = ~value._bits[i] + 1;
                    result += uint.PopCount(part);

                    i++;
                }
                while ((part == 0) && (i < value._bits.Length));

                while (i < value._bits.Length)
                {
                    // Then process the remaining bits only utilizing the one's complement

                    part = ~value._bits[i];
                    result += uint.PopCount(part);

                    i++;
                }
            }

            return result;
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.RotateLeft(TSelf, int)" />
        public static BigInteger RotateLeft(BigInteger value, int rotateAmount)
        {
            value.AssertValid();
            int byteCount = (value._bits is null) ? sizeof(int) : (value._bits.Length * 4);

            // Normalize the rotate amount to drop full rotations
            rotateAmount = (int)(rotateAmount % (byteCount * 8L));

            if (rotateAmount == 0)
                return value;

            if (rotateAmount == int.MinValue)
                return RotateRight(RotateRight(value, int.MaxValue), 1);

            if (rotateAmount < 0)
                return RotateRight(value, -rotateAmount);

            (int digitShift, int smallShift) = Math.DivRem(rotateAmount, kcbitUint);

            uint[]? xdFromPool = null;
            int xl = value._bits?.Length ?? 1;

            Span<uint> xd = (xl <= BigIntegerCalculator.StackAllocThreshold)
                          ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                          : xdFromPool = ArrayPool<uint>.Shared.Rent(xl);
            xd = xd.Slice(0, xl);

            bool negx = value.GetPartsForBitManipulation(xd);

            int zl = xl;
            uint[]? zdFromPool = null;

            Span<uint> zd = (zl <= BigIntegerCalculator.StackAllocThreshold)
                          ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                          : zdFromPool = ArrayPool<uint>.Shared.Rent(zl);
            zd = zd.Slice(0, zl);

            zd.Clear();

            if (negx)
            {
                NumericsHelpers.DangerousMakeTwosComplement(xd);
            }

            if (smallShift == 0)
            {
                int dstIndex = 0;
                int srcIndex = xd.Length - digitShift;

                do
                {
                    // Copy last digitShift elements from xd to the start of zd
                    zd[dstIndex] = xd[srcIndex];

                    dstIndex++;
                    srcIndex++;
                }
                while (srcIndex < xd.Length);

                srcIndex = 0;

                while (dstIndex < zd.Length)
                {
                    // Copy remaining elements from start of xd to end of zd
                    zd[dstIndex] = xd[srcIndex];

                    dstIndex++;
                    srcIndex++;
                }
            }
            else
            {
                int carryShift = kcbitUint - smallShift;

                int dstIndex = 0;
                int srcIndex = 0;

                uint carry = 0;

                if (digitShift == 0)
                {
                    carry = xd[^1] >> carryShift;
                }
                else
                {
                    srcIndex = xd.Length - digitShift;
                    carry = xd[srcIndex - 1] >> carryShift;
                }

                do
                {
                    uint part = xd[srcIndex];

                    zd[dstIndex] = (part << smallShift) | carry;
                    carry = part >> carryShift;

                    dstIndex++;
                    srcIndex++;
                }
                while (srcIndex < xd.Length);

                srcIndex = 0;

                while (dstIndex < zd.Length)
                {
                    uint part = xd[srcIndex];

                    zd[dstIndex] = (part << smallShift) | carry;
                    carry = part >> carryShift;

                    dstIndex++;
                    srcIndex++;
                }
            }

            if (negx && (int)zd[^1] < 0)
            {
                NumericsHelpers.DangerousMakeTwosComplement(zd);
            }
            else
            {
                negx = false;
            }

            var result = new BigInteger(zd, negx);

            if (xdFromPool != null)
                ArrayPool<uint>.Shared.Return(xdFromPool);
            if (zdFromPool != null)
                ArrayPool<uint>.Shared.Return(zdFromPool);

            return result;
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.RotateRight(TSelf, int)" />
        public static BigInteger RotateRight(BigInteger value, int rotateAmount)
        {
            value.AssertValid();
            int byteCount = (value._bits is null) ? sizeof(int) : (value._bits.Length * 4);

            // Normalize the rotate amount to drop full rotations
            rotateAmount = (int)(rotateAmount % (byteCount * 8L));

            if (rotateAmount == 0)
                return value;

            if (rotateAmount == int.MinValue)
                return RotateLeft(RotateLeft(value, int.MaxValue), 1);

            if (rotateAmount < 0)
                return RotateLeft(value, -rotateAmount);

            (int digitShift, int smallShift) = Math.DivRem(rotateAmount, kcbitUint);

            uint[]? xdFromPool = null;
            int xl = value._bits?.Length ?? 1;

            Span<uint> xd = (xl <= BigIntegerCalculator.StackAllocThreshold)
                          ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                          : xdFromPool = ArrayPool<uint>.Shared.Rent(xl);
            xd = xd.Slice(0, xl);

            bool negx = value.GetPartsForBitManipulation(xd);

            int zl = xl;
            uint[]? zdFromPool = null;

            Span<uint> zd = (zl <= BigIntegerCalculator.StackAllocThreshold)
                          ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                          : zdFromPool = ArrayPool<uint>.Shared.Rent(zl);
            zd = zd.Slice(0, zl);

            zd.Clear();

            if (negx)
            {
                NumericsHelpers.DangerousMakeTwosComplement(xd);
            }

            if (smallShift == 0)
            {
                int dstIndex = 0;
                int srcIndex = digitShift;

                do
                {
                    // Copy first digitShift elements from xd to the end of zd
                    zd[dstIndex] = xd[srcIndex];

                    dstIndex++;
                    srcIndex++;
                }
                while (srcIndex < xd.Length);

                srcIndex = 0;

                while (dstIndex < zd.Length)
                {
                    // Copy remaining elements from end of xd to start of zd
                    zd[dstIndex] = xd[srcIndex];

                    dstIndex++;
                    srcIndex++;
                }
            }
            else
            {
                int carryShift = kcbitUint - smallShift;

                int dstIndex = 0;
                int srcIndex = digitShift;

                uint carry = 0;

                if (digitShift == 0)
                {
                    carry = xd[^1] << carryShift;
                }
                else
                {
                    carry = xd[srcIndex - 1] << carryShift;
                }

                do
                {
                    uint part = xd[srcIndex];

                    zd[dstIndex] = (part >> smallShift) | carry;
                    carry = part << carryShift;

                    dstIndex++;
                    srcIndex++;
                }
                while (srcIndex < xd.Length);

                srcIndex = 0;

                while (dstIndex < zd.Length)
                {
                    uint part = xd[srcIndex];

                    zd[dstIndex] = (part >> smallShift) | carry;
                    carry = part << carryShift;

                    dstIndex++;
                    srcIndex++;
                }
            }

            if (negx && (int)zd[^1] < 0)
            {
                NumericsHelpers.DangerousMakeTwosComplement(zd);
            }
            else
            {
                negx = false;
            }

            var result = new BigInteger(zd, negx);

            if (xdFromPool != null)
                ArrayPool<uint>.Shared.Return(xdFromPool);
            if (zdFromPool != null)
                ArrayPool<uint>.Shared.Return(zdFromPool);

            return result;
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TrailingZeroCount(TSelf)" />
        public static BigInteger TrailingZeroCount(BigInteger value)
        {
            value.AssertValid();

            if (value._bits is null)
            {
                return int.TrailingZeroCount(value._sign);
            }

            ulong result = 0;

            // Both positive values and their two's-complement negative representation will share the same TrailingZeroCount,
            // so the sign of value does not matter and both cases can be handled in the same way

            uint part = value._bits[0];

            for (int i = 1; (part == 0) && (i < value._bits.Length); i++)
            {
                part = value._bits[i];
                result += (sizeof(uint) * 8);
            }

            result += uint.TrailingZeroCount(part);

            return result;
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TryReadBigEndian(ReadOnlySpan{byte}, bool, out TSelf)" />
        static bool IBinaryInteger<BigInteger>.TryReadBigEndian(ReadOnlySpan<byte> source, bool isUnsigned, out BigInteger value)
        {
            value = new BigInteger(source, isUnsigned, isBigEndian: true);
            return true;
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TryReadLittleEndian(ReadOnlySpan{byte}, bool, out TSelf)" />
        static bool IBinaryInteger<BigInteger>.TryReadLittleEndian(ReadOnlySpan<byte> source, bool isUnsigned, out BigInteger value)
        {
            value = new BigInteger(source, isUnsigned, isBigEndian: false);
            return true;
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.GetShortestBitLength()" />
        int IBinaryInteger<BigInteger>.GetShortestBitLength()
        {
            AssertValid();
            uint[]? bits = _bits;

            if (bits is null)
            {
                int value = _sign;

                if (value >= 0)
                {
                    return (sizeof(int) * 8) - BitOperations.LeadingZeroCount((uint)(value));
                }
                else
                {
                    return (sizeof(int) * 8) + 1 - BitOperations.LeadingZeroCount((uint)(~value));
                }
            }

            int result = (bits.Length - 1) * 32;

            if (_sign >= 0)
            {
                result += (sizeof(uint) * 8) - BitOperations.LeadingZeroCount(bits[^1]);
            }
            else
            {
                uint part = ~bits[^1] + 1;

                // We need to remove the "carry" (the +1) if any of the initial
                // bytes are not zero. This ensures we get the correct two's complement
                // part for the computation.

                for (int index = 0; index < bits.Length - 1; index++)
                {
                    if (bits[index] != 0)
                    {
                        part -= 1;
                        break;
                    }
                }

                result += (sizeof(uint) * 8) + 1 - BitOperations.LeadingZeroCount(~part);
            }

            return result;
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.GetByteCount()" />
        int IBinaryInteger<BigInteger>.GetByteCount() => GetGenericMathByteCount();

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TryWriteBigEndian(Span{byte}, out int)" />
        bool IBinaryInteger<BigInteger>.TryWriteBigEndian(Span<byte> destination, out int bytesWritten)
        {
            AssertValid();
            uint[]? bits = _bits;

            int byteCount = GetGenericMathByteCount();

            if (destination.Length >= byteCount)
            {
                if (bits is null)
                {
                    int value = BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(_sign) : _sign;
                    Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
                }
                else if (_sign >= 0)
                {
                    // When the value is positive, we simply need to copy all bits as big endian

                    ref byte startAddress = ref MemoryMarshal.GetReference(destination);
                    ref byte address = ref Unsafe.Add(ref startAddress, (bits.Length - 1) * sizeof(uint));

                    for (int i = 0; i < bits.Length; i++)
                    {
                        uint part = bits[i];

                        if (BitConverter.IsLittleEndian)
                        {
                            part = BinaryPrimitives.ReverseEndianness(part);
                        }

                        Unsafe.WriteUnaligned(ref address, part);
                        address = ref Unsafe.Subtract(ref address, sizeof(uint));
                    }
                }
                else
                {
                    // When the value is negative, we need to copy the two's complement representation
                    // We'll do this "inline" to avoid needing to unnecessarily allocate.

                    ref byte startAddress = ref MemoryMarshal.GetReference(destination);
                    ref byte address = ref Unsafe.Add(ref startAddress, byteCount - sizeof(uint));

                    int i = 0;
                    uint part;

                    do
                    {
                        // first do complement and +1 as long as carry is needed
                        part = ~bits[i] + 1;

                        if (BitConverter.IsLittleEndian)
                        {
                            part = BinaryPrimitives.ReverseEndianness(part);
                        }

                        Unsafe.WriteUnaligned(ref address, part);
                        address = ref Unsafe.Subtract(ref address, sizeof(uint));

                        i++;
                    }
                    while ((part == 0) && (i < bits.Length));

                    while (i < bits.Length)
                    {
                        // now ones complement is sufficient
                        part = ~bits[i];

                        if (BitConverter.IsLittleEndian)
                        {
                            part = BinaryPrimitives.ReverseEndianness(part);
                        }

                        Unsafe.WriteUnaligned(ref address, part);
                        address = ref Unsafe.Subtract(ref address, sizeof(uint));

                        i++;
                    }

                    if (Unsafe.AreSame(ref address, ref startAddress))
                    {
                        // We need one extra part to represent the sign as the most
                        // significant bit of the two's complement value was 0.
                        Unsafe.WriteUnaligned(ref address, uint.MaxValue);
                    }
                    else
                    {
                        // Otherwise we should have been precisely one part behind address
                        Debug.Assert(Unsafe.AreSame(ref startAddress, ref Unsafe.Add(ref address, sizeof(uint))));
                    }
                }

                bytesWritten = byteCount;
                return true;
            }
            else
            {
                bytesWritten = 0;
                return false;
            }
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TryWriteLittleEndian(Span{byte}, out int)" />
        bool IBinaryInteger<BigInteger>.TryWriteLittleEndian(Span<byte> destination, out int bytesWritten)
        {
            AssertValid();
            uint[]? bits = _bits;

            int byteCount = GetGenericMathByteCount();

            if (destination.Length >= byteCount)
            {
                if (bits is null)
                {
                    int value = BitConverter.IsLittleEndian ? _sign : BinaryPrimitives.ReverseEndianness(_sign);
                    Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
                }
                else if (_sign >= 0)
                {
                    // When the value is positive, we simply need to copy all bits as little endian

                    ref byte address = ref MemoryMarshal.GetReference(destination);

                    for (int i = 0; i < bits.Length; i++)
                    {
                        uint part = bits[i];

                        if (!BitConverter.IsLittleEndian)
                        {
                            part = BinaryPrimitives.ReverseEndianness(part);
                        }

                        Unsafe.WriteUnaligned(ref address, part);
                        address = ref Unsafe.Add(ref address, sizeof(uint));
                    }
                }
                else
                {
                    // When the value is negative, we need to copy the two's complement representation
                    // We'll do this "inline" to avoid needing to unnecessarily allocate.

                    ref byte address = ref MemoryMarshal.GetReference(destination);
                    ref byte lastAddress = ref Unsafe.Add(ref address, byteCount - sizeof(uint));

                    int i = 0;
                    uint part;

                    do
                    {
                        // first do complement and +1 as long as carry is needed
                        part = ~bits[i] + 1;

                        if (!BitConverter.IsLittleEndian)
                        {
                            part = BinaryPrimitives.ReverseEndianness(part);
                        }

                        Unsafe.WriteUnaligned(ref address, part);
                        address = ref Unsafe.Add(ref address, sizeof(uint));

                        i++;
                    }
                    while ((part == 0) && (i < bits.Length));

                    while (i < bits.Length)
                    {
                        // now ones complement is sufficient
                        part = ~bits[i];

                        if (!BitConverter.IsLittleEndian)
                        {
                            part = BinaryPrimitives.ReverseEndianness(part);
                        }

                        Unsafe.WriteUnaligned(ref address, part);
                        address = ref Unsafe.Add(ref address, sizeof(uint));

                        i++;
                    }

                    if (Unsafe.AreSame(ref address, ref lastAddress))
                    {
                        // We need one extra part to represent the sign as the most
                        // significant bit of the two's complement value was 0.
                        Unsafe.WriteUnaligned(ref address, uint.MaxValue);
                    }
                    else
                    {
                        // Otherwise we should have been precisely one part ahead address
                        Debug.Assert(Unsafe.AreSame(ref lastAddress, ref Unsafe.Subtract(ref address, sizeof(uint))));
                    }
                }

                bytesWritten = byteCount;
                return true;
            }
            else
            {
                bytesWritten = 0;
                return false;
            }
        }

        private int GetGenericMathByteCount()
        {
            AssertValid();
            uint[]? bits = _bits;

            if (bits is null)
            {
                return sizeof(int);
            }

            int result = bits.Length * 4;

            if (_sign < 0)
            {
                uint part = ~bits[^1] + 1;

                // We need to remove the "carry" (the +1) if any of the initial
                // bytes are not zero. This ensures we get the correct two's complement
                // part for the computation.

                for (int index = 0; index < bits.Length - 1; index++)
                {
                    if (bits[index] != 0)
                    {
                        part -= 1;
                        break;
                    }
                }

                if ((int)part >= 0)
                {
                    // When the most significant bit of the part is zero
                    // we need another part to represent the value.
                    result += sizeof(uint);
                }
            }

            return result;
        }

        //
        // IBinaryNumber
        //

        /// <inheritdoc cref="IBinaryNumber{TSelf}.AllBitsSet" />
        static BigInteger IBinaryNumber<BigInteger>.AllBitsSet => MinusOne;

        /// <inheritdoc cref="IBinaryNumber{TSelf}.IsPow2(TSelf)" />
        public static bool IsPow2(BigInteger value) => value.IsPowerOfTwo;

        /// <inheritdoc cref="IBinaryNumber{TSelf}.Log2(TSelf)" />
        public static BigInteger Log2(BigInteger value)
        {
            value.AssertValid();

            if (IsNegative(value))
            {
                ThrowHelper.ThrowValueArgumentOutOfRange_NeedNonNegNumException();
            }

            if (value._bits is null)
            {
                return 31 ^ uint.LeadingZeroCount((uint)(value._sign | 1));
            }

            return ((value._bits.Length * 32) - 1) ^ uint.LeadingZeroCount(value._bits[^1]);
        }

        //
        // IMultiplicativeIdentity
        //

        /// <inheritdoc cref="IMultiplicativeIdentity{TSelf, TResult}.MultiplicativeIdentity" />
        static BigInteger IMultiplicativeIdentity<BigInteger, BigInteger>.MultiplicativeIdentity => One;

        //
        // INumber
        //

        /// <inheritdoc cref="INumber{TSelf}.Clamp(TSelf, TSelf, TSelf)" />
        public static BigInteger Clamp(BigInteger value, BigInteger min, BigInteger max)
        {
            value.AssertValid();

            min.AssertValid();
            max.AssertValid();

            if (min > max)
            {
                ThrowMinMaxException(min, max);
            }

            if (value < min)
            {
                return min;
            }
            else if (value > max)
            {
                return max;
            }

            return value;

            [DoesNotReturn]
            static void ThrowMinMaxException<T>(T min, T max)
            {
                throw new ArgumentException(SR.Format(SR.Argument_MinMaxValue, min, max));
            }
        }

        /// <inheritdoc cref="INumber{TSelf}.CopySign(TSelf, TSelf)" />
        public static BigInteger CopySign(BigInteger value, BigInteger sign)
        {
            value.AssertValid();
            sign.AssertValid();

            int currentSign = value._sign;

            if (value._bits is null)
            {
                currentSign = (currentSign >= 0) ? 1 : -1;
            }

            int targetSign = sign._sign;

            if (sign._bits is null)
            {
                targetSign = (targetSign >= 0) ? 1 : -1;
            }

            return (currentSign == targetSign) ? value : -value;
        }

        /// <inheritdoc cref="INumber{TSelf}.MaxNumber(TSelf, TSelf)" />
        static BigInteger INumber<BigInteger>.MaxNumber(BigInteger x, BigInteger y) => Max(x, y);

        /// <inheritdoc cref="INumber{TSelf}.MinNumber(TSelf, TSelf)" />
        static BigInteger INumber<BigInteger>.MinNumber(BigInteger x, BigInteger y) => Min(x, y);

        /// <inheritdoc cref="INumber{TSelf}.Sign(TSelf)" />
        static int INumber<BigInteger>.Sign(BigInteger value)
        {
            value.AssertValid();

            if (value._bits is null)
            {
                return int.Sign(value._sign);
            }

            return value._sign;
        }

        //
        // INumberBase
        //

        /// <inheritdoc cref="INumberBase{TSelf}.Radix" />
        static int INumberBase<BigInteger>.Radix => 2;

        /// <inheritdoc cref="INumberBase{TSelf}.CreateChecked{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BigInteger CreateChecked<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            BigInteger result;

            if (typeof(TOther) == typeof(BigInteger))
            {
                result = (BigInteger)(object)value;
            }
            else if (!TryConvertFromChecked(value, out result) && !TOther.TryConvertToChecked(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.CreateSaturating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BigInteger CreateSaturating<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            BigInteger result;

            if (typeof(TOther) == typeof(BigInteger))
            {
                result = (BigInteger)(object)value;
            }
            else if (!TryConvertFromSaturating(value, out result) && !TOther.TryConvertToSaturating(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.CreateTruncating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BigInteger CreateTruncating<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            BigInteger result;

            if (typeof(TOther) == typeof(BigInteger))
            {
                result = (BigInteger)(object)value;
            }
            else if (!TryConvertFromTruncating(value, out result) && !TOther.TryConvertToTruncating(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsCanonical(TSelf)" />
        static bool INumberBase<BigInteger>.IsCanonical(BigInteger value) => true;

        /// <inheritdoc cref="INumberBase{TSelf}.IsComplexNumber(TSelf)" />
        static bool INumberBase<BigInteger>.IsComplexNumber(BigInteger value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsEvenInteger(TSelf)" />
        public static bool IsEvenInteger(BigInteger value)
        {
            value.AssertValid();

            if (value._bits is null)
            {
                return (value._sign & 1) == 0;
            }
            return (value._bits[0] & 1) == 0;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsFinite(TSelf)" />
        static bool INumberBase<BigInteger>.IsFinite(BigInteger value) => true;

        /// <inheritdoc cref="INumberBase{TSelf}.IsImaginaryNumber(TSelf)" />
        static bool INumberBase<BigInteger>.IsImaginaryNumber(BigInteger value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsInfinity(TSelf)" />
        static bool INumberBase<BigInteger>.IsInfinity(BigInteger value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsInteger(TSelf)" />
        static bool INumberBase<BigInteger>.IsInteger(BigInteger value) => true;

        /// <inheritdoc cref="INumberBase{TSelf}.IsNaN(TSelf)" />
        static bool INumberBase<BigInteger>.IsNaN(BigInteger value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsNegative(TSelf)" />
        public static bool IsNegative(BigInteger value)
        {
            value.AssertValid();
            return value._sign < 0;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsNegativeInfinity(TSelf)" />
        static bool INumberBase<BigInteger>.IsNegativeInfinity(BigInteger value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsNormal(TSelf)" />
        static bool INumberBase<BigInteger>.IsNormal(BigInteger value) => (value != 0);

        /// <inheritdoc cref="INumberBase{TSelf}.IsOddInteger(TSelf)" />
        public static bool IsOddInteger(BigInteger value)
        {
            value.AssertValid();

            if (value._bits is null)
            {
                return (value._sign & 1) != 0;
            }
            return (value._bits[0] & 1) != 0;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsPositive(TSelf)" />
        public static bool IsPositive(BigInteger value)
        {
            value.AssertValid();
            return value._sign >= 0;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsPositiveInfinity(TSelf)" />
        static bool INumberBase<BigInteger>.IsPositiveInfinity(BigInteger value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsRealNumber(TSelf)" />
        static bool INumberBase<BigInteger>.IsRealNumber(BigInteger value) => true;

        /// <inheritdoc cref="INumberBase{TSelf}.IsSubnormal(TSelf)" />
        static bool INumberBase<BigInteger>.IsSubnormal(BigInteger value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsZero(TSelf)" />
        static bool INumberBase<BigInteger>.IsZero(BigInteger value)
        {
            value.AssertValid();
            return value._sign == 0;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.MaxMagnitude(TSelf, TSelf)" />
        public static BigInteger MaxMagnitude(BigInteger x, BigInteger y)
        {
            x.AssertValid();
            y.AssertValid();

            BigInteger ax = Abs(x);
            BigInteger ay = Abs(y);

            if (ax > ay)
            {
                return x;
            }

            if (ax == ay)
            {
                return IsNegative(x) ? y : x;
            }

            return y;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.MaxMagnitudeNumber(TSelf, TSelf)" />
        static BigInteger INumberBase<BigInteger>.MaxMagnitudeNumber(BigInteger x, BigInteger y) => MaxMagnitude(x, y);

        /// <inheritdoc cref="INumberBase{TSelf}.MinMagnitude(TSelf, TSelf)" />
        public static BigInteger MinMagnitude(BigInteger x, BigInteger y)
        {
            x.AssertValid();
            y.AssertValid();

            BigInteger ax = Abs(x);
            BigInteger ay = Abs(y);

            if (ax < ay)
            {
                return x;
            }

            if (ax == ay)
            {
                return IsNegative(x) ? x : y;
            }

            return y;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.MinMagnitudeNumber(TSelf, TSelf)" />
        static BigInteger INumberBase<BigInteger>.MinMagnitudeNumber(BigInteger x, BigInteger y) => MinMagnitude(x, y);

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromChecked{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<BigInteger>.TryConvertFromChecked<TOther>(TOther value, out BigInteger result) => TryConvertFromChecked(value, out result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryConvertFromChecked<TOther>(TOther value, out BigInteger result)
            where TOther : INumberBase<TOther>
        {
            if (typeof(TOther) == typeof(byte))
            {
                byte actualValue = (byte)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(char))
            {
                char actualValue = (char)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                decimal actualValue = (decimal)(object)value;
                result = (BigInteger)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(double))
            {
                double actualValue = (double)(object)value;
                result = checked((BigInteger)actualValue);
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                Half actualValue = (Half)(object)value;
                result = checked((BigInteger)actualValue);
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                short actualValue = (short)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                int actualValue = (int)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                long actualValue = (long)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                Int128 actualValue = (Int128)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                nint actualValue = (nint)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                sbyte actualValue = (sbyte)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                float actualValue = (float)(object)value;
                result = checked((BigInteger)actualValue);
                return true;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                ushort actualValue = (ushort)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                uint actualValue = (uint)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                ulong actualValue = (ulong)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(UInt128))
            {
                UInt128 actualValue = (UInt128)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                nuint actualValue = (nuint)(object)value;
                result = actualValue;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromSaturating{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<BigInteger>.TryConvertFromSaturating<TOther>(TOther value, out BigInteger result) => TryConvertFromSaturating(value, out result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryConvertFromSaturating<TOther>(TOther value, out BigInteger result)
            where TOther : INumberBase<TOther>
        {
            if (typeof(TOther) == typeof(byte))
            {
                byte actualValue = (byte)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(char))
            {
                char actualValue = (char)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                decimal actualValue = (decimal)(object)value;
                result = (BigInteger)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(double))
            {
                double actualValue = (double)(object)value;
                result = double.IsNaN(actualValue) ? Zero : (BigInteger)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                Half actualValue = (Half)(object)value;
                result = Half.IsNaN(actualValue) ? Zero : (BigInteger)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                short actualValue = (short)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                int actualValue = (int)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                long actualValue = (long)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                Int128 actualValue = (Int128)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                nint actualValue = (nint)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                sbyte actualValue = (sbyte)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                float actualValue = (float)(object)value;
                result = float.IsNaN(actualValue) ? Zero : (BigInteger)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                ushort actualValue = (ushort)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                uint actualValue = (uint)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                ulong actualValue = (ulong)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(UInt128))
            {
                UInt128 actualValue = (UInt128)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                nuint actualValue = (nuint)(object)value;
                result = actualValue;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromTruncating{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<BigInteger>.TryConvertFromTruncating<TOther>(TOther value, out BigInteger result) => TryConvertFromTruncating(value, out result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryConvertFromTruncating<TOther>(TOther value, out BigInteger result)
            where TOther : INumberBase<TOther>
        {
            if (typeof(TOther) == typeof(byte))
            {
                byte actualValue = (byte)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(char))
            {
                char actualValue = (char)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                decimal actualValue = (decimal)(object)value;
                result = (BigInteger)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(double))
            {
                double actualValue = (double)(object)value;
                result = double.IsNaN(actualValue) ? Zero : (BigInteger)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                Half actualValue = (Half)(object)value;
                result = Half.IsNaN(actualValue) ? Zero : (BigInteger)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                short actualValue = (short)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                int actualValue = (int)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                long actualValue = (long)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                Int128 actualValue = (Int128)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                nint actualValue = (nint)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                sbyte actualValue = (sbyte)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                float actualValue = (float)(object)value;
                result = float.IsNaN(actualValue) ? Zero : (BigInteger)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                ushort actualValue = (ushort)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                uint actualValue = (uint)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                ulong actualValue = (ulong)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(UInt128))
            {
                UInt128 actualValue = (UInt128)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                nuint actualValue = (nuint)(object)value;
                result = actualValue;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertToChecked{TOther}(TSelf, out TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<BigInteger>.TryConvertToChecked<TOther>(BigInteger value, [MaybeNullWhen(false)] out TOther result)
        {
            if (typeof(TOther) == typeof(byte))
            {
                byte actualResult = checked((byte)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(char))
            {
                char actualResult = checked((char)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                decimal actualResult = checked((decimal)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(double))
            {
                double actualResult = (double)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                Half actualResult = (Half)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                short actualResult = checked((short)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                int actualResult = checked((int)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                long actualResult = checked((long)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                Int128 actualResult = checked((Int128)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                nint actualResult = checked((nint)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Complex))
            {
                Complex actualResult = (Complex)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                sbyte actualResult = checked((sbyte)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                float actualResult = checked((float)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                ushort actualResult = checked((ushort)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                uint actualResult = checked((uint)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                ulong actualResult = checked((ulong)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(UInt128))
            {
                UInt128 actualResult = checked((UInt128)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                nuint actualResult = checked((nuint)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertToSaturating{TOther}(TSelf, out TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<BigInteger>.TryConvertToSaturating<TOther>(BigInteger value, [MaybeNullWhen(false)] out TOther result)
        {
            if (typeof(TOther) == typeof(byte))
            {
                byte actualResult;

                if (value._bits is not null)
                {
                    actualResult = IsNegative(value) ? byte.MinValue : byte.MaxValue;
                }
                else
                {
                    actualResult = (value._sign >= byte.MaxValue) ? byte.MaxValue :
                                   (value._sign <= byte.MinValue) ? byte.MinValue : (byte)value._sign;
                }

                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(char))
            {
                char actualResult;

                if (value._bits is not null)
                {
                    actualResult = IsNegative(value) ? char.MinValue : char.MaxValue;
                }
                else
                {
                    actualResult = (value._sign >= char.MaxValue) ? char.MaxValue :
                                   (value._sign <= char.MinValue) ? char.MinValue : (char)value._sign;
                }

                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                decimal actualResult = (value >= new Int128(0x0000_0000_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)) ? decimal.MaxValue :
                                       (value <= new Int128(0xFFFF_FFFF_0000_0000, 0x0000_0000_0000_0001)) ? decimal.MinValue : (decimal)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(double))
            {
                double actualResult = (double)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                Half actualResult = (Half)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                short actualResult;

                if (value._bits is not null)
                {
                    actualResult = IsNegative(value) ? short.MinValue : short.MaxValue;
                }
                else
                {
                    actualResult = (value._sign >= short.MaxValue) ? short.MaxValue :
                                   (value._sign <= short.MinValue) ? short.MinValue : (short)value._sign;
                }

                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                int actualResult;

                if (value._bits is not null)
                {
                    actualResult = IsNegative(value) ? int.MinValue : int.MaxValue;
                }
                else
                {
                    actualResult = (value._sign >= int.MaxValue) ? int.MaxValue :
                                   (value._sign <= int.MinValue) ? int.MinValue : (int)value._sign;
                }

                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                long actualResult = (value >= long.MaxValue) ? long.MaxValue :
                                    (value <= long.MinValue) ? long.MinValue : (long)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                Int128 actualResult = (value >= Int128.MaxValue) ? Int128.MaxValue :
                                      (value <= Int128.MinValue) ? Int128.MinValue : (Int128)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                nint actualResult = (value >= nint.MaxValue) ? nint.MaxValue :
                                    (value <= nint.MinValue) ? nint.MinValue : (nint)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Complex))
            {
                Complex actualResult = (Complex)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                sbyte actualResult;

                if (value._bits is not null)
                {
                    actualResult = IsNegative(value) ? sbyte.MinValue : sbyte.MaxValue;
                }
                else
                {
                    actualResult = (value._sign >= sbyte.MaxValue) ? sbyte.MaxValue :
                                   (value._sign <= sbyte.MinValue) ? sbyte.MinValue : (sbyte)value._sign;
                }

                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                float actualResult = (float)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                ushort actualResult;

                if (value._bits is not null)
                {
                    actualResult = IsNegative(value) ? ushort.MinValue : ushort.MaxValue;
                }
                else
                {
                    actualResult = (value._sign >= ushort.MaxValue) ? ushort.MaxValue :
                                   (value._sign <= ushort.MinValue) ? ushort.MinValue : (ushort)value._sign;
                }

                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                uint actualResult = (value >= uint.MaxValue) ? uint.MaxValue :
                                    IsNegative(value) ? uint.MinValue : (uint)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                ulong actualResult = (value >= ulong.MaxValue) ? ulong.MaxValue :
                                     IsNegative(value) ? ulong.MinValue : (ulong)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(UInt128))
            {
                UInt128 actualResult = (value >= UInt128.MaxValue) ? UInt128.MaxValue :
                                       IsNegative(value) ? UInt128.MinValue : (UInt128)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                nuint actualResult = (value >= nuint.MaxValue) ? nuint.MaxValue :
                                     IsNegative(value) ? nuint.MinValue : (nuint)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertToTruncating{TOther}(TSelf, out TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<BigInteger>.TryConvertToTruncating<TOther>(BigInteger value, [MaybeNullWhen(false)] out TOther result)
        {
            if (typeof(TOther) == typeof(byte))
            {
                byte actualResult;

                if (value._bits is not null)
                {
                    uint bits = value._bits[0];

                    if (IsNegative(value))
                    {
                        bits = ~bits + 1;
                    }

                    actualResult = (byte)bits;
                }
                else
                {
                    actualResult = (byte)value._sign;
                }

                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(char))
            {
                char actualResult;

                if (value._bits is not null)
                {
                    uint bits = value._bits[0];

                    if (IsNegative(value))
                    {
                        bits = ~bits + 1;
                    }

                    actualResult = (char)bits;
                }
                else
                {
                    actualResult = (char)value._sign;
                }

                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                decimal actualResult = (value >= new Int128(0x0000_0000_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF)) ? decimal.MaxValue :
                                       (value <= new Int128(0xFFFF_FFFF_0000_0000, 0x0000_0000_0000_0001)) ? decimal.MinValue : (decimal)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(double))
            {
                double actualResult = (double)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                Half actualResult = (Half)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                short actualResult;

                if (value._bits is not null)
                {
                    actualResult = IsNegative(value) ? (short)(~value._bits[0] + 1) : (short)value._bits[0];
                }
                else
                {
                    actualResult = (short)value._sign;
                }

                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                int actualResult;

                if (value._bits is not null)
                {
                    actualResult = IsNegative(value) ? (int)(~value._bits[0] + 1) : (int)value._bits[0];
                }
                else
                {
                    actualResult = value._sign;
                }

                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                long actualResult;

                if (value._bits is not null)
                {
                    ulong bits = 0;

                    if (value._bits.Length >= 2)
                    {
                        bits = value._bits[1];
                        bits <<= 32;
                    }

                    bits |= value._bits[0];

                    if (IsNegative(value))
                    {
                        bits = ~bits + 1;
                    }

                    actualResult = (long)bits;
                }
                else
                {
                    actualResult = value._sign;
                }

                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                Int128 actualResult;

                if (value._bits is not null)
                {
                    ulong lowerBits = 0;
                    ulong upperBits = 0;

                    if (value._bits.Length >= 4)
                    {
                        upperBits = value._bits[3];
                        upperBits <<= 32;
                    }

                    if (value._bits.Length >= 3)
                    {
                        upperBits = value._bits[2];
                    }

                    if (value._bits.Length >= 2)
                    {
                        lowerBits = value._bits[1];
                        lowerBits <<= 32;
                    }

                    lowerBits |= value._bits[0];

                    UInt128 bits = new UInt128(upperBits, lowerBits);

                    if (IsNegative(value))
                    {
                        bits = ~bits + 1;
                    }

                    actualResult = (Int128)bits;
                }
                else
                {
                    actualResult = value._sign;
                }

                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                nint actualResult;

                if (value._bits is not null)
                {
                    nuint bits = 0;

                    if (Environment.Is64BitProcess && (value._bits.Length >= 2))
                    {
                        bits = value._bits[1];
                        bits <<= 32;
                    }

                    bits |= value._bits[0];

                    if (IsNegative(value))
                    {
                        bits = ~bits + 1;
                    }

                    actualResult = (nint)bits;
                }
                else
                {
                    actualResult = value._sign;
                }

                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Complex))
            {
                Complex actualResult = (Complex)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                sbyte actualResult;

                if (value._bits is not null)
                {
                    actualResult = IsNegative(value) ? (sbyte)(~value._bits[0] + 1) : (sbyte)value._bits[0];
                }
                else
                {
                    actualResult = (sbyte)value._sign;
                }

                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                float actualResult = (float)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                ushort actualResult;

                if (value._bits is not null)
                {
                    uint bits = value._bits[0];

                    if (IsNegative(value))
                    {
                        bits = ~bits + 1;
                    }

                    actualResult = (ushort)bits;
                }
                else
                {
                    actualResult = (ushort)value._sign;
                }

                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                uint actualResult;

                if (value._bits is not null)
                {
                    uint bits = value._bits[0];

                    if (IsNegative(value))
                    {
                        bits = ~bits + 1;
                    }

                    actualResult = bits;
                }
                else
                {
                    actualResult = (uint)value._sign;
                }

                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                ulong actualResult;

                if (value._bits is not null)
                {
                    ulong bits = 0;

                    if (value._bits.Length >= 2)
                    {
                        bits = value._bits[1];
                        bits <<= 32;
                    }

                    bits |= value._bits[0];

                    if (IsNegative(value))
                    {
                        bits = ~bits + 1;
                    }

                    actualResult = bits;
                }
                else
                {
                    actualResult = (ulong)value._sign;
                }

                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(UInt128))
            {
                UInt128 actualResult;

                if (value._bits is not null)
                {
                    ulong lowerBits = 0;
                    ulong upperBits = 0;

                    if (value._bits.Length >= 4)
                    {
                        upperBits = value._bits[3];
                        upperBits <<= 32;
                    }

                    if (value._bits.Length >= 3)
                    {
                        upperBits |= value._bits[2];
                    }

                    if (value._bits.Length >= 2)
                    {
                        lowerBits = value._bits[1];
                        lowerBits <<= 32;
                    }

                    lowerBits |= value._bits[0];

                    UInt128 bits = new UInt128(upperBits, lowerBits);

                    if (IsNegative(value))
                    {
                        bits = ~bits + 1;
                    }

                    actualResult = bits;
                }
                else
                {
                    actualResult = (UInt128)value._sign;
                }

                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                nuint actualResult;

                if (value._bits is not null)
                {
                    nuint bits = 0;

                    if (Environment.Is64BitProcess && (value._bits.Length >= 2))
                    {
                        bits = value._bits[1];
                        bits <<= 32;
                    }

                    bits |= value._bits[0];

                    if (IsNegative(value))
                    {
                        bits = ~bits + 1;
                    }

                    actualResult = bits;
                }
                else
                {
                    actualResult = (nuint)value._sign;
                }

                result = (TOther)(object)actualResult;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        //
        // IParsable
        //

        /// <inheritdoc cref="IParsable{TSelf}.TryParse(string?, IFormatProvider?, out TSelf)" />
        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out BigInteger result) => TryParse(s, NumberStyles.Integer, provider, out result);

        //
        // IShiftOperators
        //

        /// <inheritdoc cref="IShiftOperators{TSelf, TOther, TResult}.op_UnsignedRightShift(TSelf, TOther)" />
        public static BigInteger operator >>>(BigInteger value, int shiftAmount)
        {
            value.AssertValid();

            if (shiftAmount == 0)
                return value;

            if (shiftAmount == int.MinValue)
                return ((value << int.MaxValue) << 1);

            if (shiftAmount < 0)
                return value << -shiftAmount;

            (int digitShift, int smallShift) = Math.DivRem(shiftAmount, kcbitUint);

            BigInteger result;

            uint[]? xdFromPool = null;
            int xl = value._bits?.Length ?? 1;
            Span<uint> xd = (xl <= BigIntegerCalculator.StackAllocThreshold
                          ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                          : xdFromPool = ArrayPool<uint>.Shared.Rent(xl)).Slice(0, xl);

            bool negx = value.GetPartsForBitManipulation(xd);

            if (negx)
            {
                if (shiftAmount >= ((long)kcbitUint * xd.Length))
                {
                    result = MinusOne;
                    goto exit;
                }

                NumericsHelpers.DangerousMakeTwosComplement(xd); // Mutates xd
            }

            uint[]? zdFromPool = null;
            int zl = Math.Max(xl - digitShift, 0);
            Span<uint> zd = ((uint)zl <= BigIntegerCalculator.StackAllocThreshold
                          ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                          : zdFromPool = ArrayPool<uint>.Shared.Rent(zl)).Slice(0, zl);
            zd.Clear();

            if (smallShift == 0)
            {
                for (int i = xd.Length - 1; i >= digitShift; i--)
                {
                    zd[i - digitShift] = xd[i];
                }
            }
            else
            {
                int carryShift = kcbitUint - smallShift;
                uint carry = 0;
                for (int i = xd.Length - 1; i >= digitShift; i--)
                {
                    uint rot = xd[i];
                    zd[i - digitShift] = (rot >>> smallShift) | carry;
                    carry = rot << carryShift;
                }
            }

            if (negx && (int)zd[^1] < 0)
            {
                NumericsHelpers.DangerousMakeTwosComplement(zd);
            }
            else
            {
                negx = false;
            }

            result = new BigInteger(zd, negx);

            if (zdFromPool != null)
                ArrayPool<uint>.Shared.Return(zdFromPool);
            exit:
            if (xdFromPool != null)
                ArrayPool<uint>.Shared.Return(xdFromPool);

            return result;
        }

        //
        // ISignedNumber
        //

        /// <inheritdoc cref="ISignedNumber{TSelf}.NegativeOne" />
        static BigInteger ISignedNumber<BigInteger>.NegativeOne => MinusOne;

        //
        // ISpanParsable
        //

        /// <inheritdoc cref="ISpanParsable{TSelf}.Parse(ReadOnlySpan{char}, IFormatProvider?)" />
        public static BigInteger Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s, NumberStyles.Integer, provider);

        /// <inheritdoc cref="ISpanParsable{TSelf}.TryParse(ReadOnlySpan{char}, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out BigInteger result) => TryParse(s, NumberStyles.Integer, provider, out result);
    }
}
