// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*
* Routines used to manipulate IEEE 754 double-precision numbers, taken from JScript codebase.
*
* Define NOPARSE if you do not need FloatingDecimal -> double conversions
*
*/

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Xml.Xsl
{
    /**
    * Converts according to XPath/XSLT rules.
    */
    internal static class XPathConvert
    {
        public static uint DblHi(double dbl)
        {
            return (uint)(BitConverter.DoubleToUInt64Bits(dbl) >> 32);
        }

        public static uint DblLo(double dbl)
        {
            return unchecked((uint)BitConverter.DoubleToUInt64Bits(dbl));
        }

        // Returns true if value is infinite or NaN (exponent bits are all ones)
        public static bool IsSpecial(double dbl)
        {
            return 0 == (~DblHi(dbl) & 0x7FF00000);
        }

#if DEBUG
        // Returns the next representable neighbor of x in the direction toward y
        public static double NextAfter(double x, double y)
        {
            long bits;

            if (double.IsNaN(x))
            {
                return x;
            }
            if (double.IsNaN(y))
            {
                return y;
            }
            if (x == y)
            {
                return y;
            }
            if (x == 0)
            {
                bits = BitConverter.DoubleToInt64Bits(y) & 1L << 63;
                return BitConverter.Int64BitsToDouble(bits | 1);
            }

            // At this point x!=y, and x!=0. x can be treated as a 64bit
            // integer in sign/magnitude representation. To get the next
            // representable neighbor we add or subtract one from this
            // integer.

            bits = BitConverter.DoubleToInt64Bits(x);
            if (0 < x && x < y || 0 > x && x > y)
            {
                bits++;
            }
            else
            {
                bits--;
            }
            return BitConverter.Int64BitsToDouble(bits);
        }

        public static double Succ(double x)
        {
            return NextAfter(x, double.PositiveInfinity);
        }

        public static double Pred(double x)
        {
            return NextAfter(x, double.NegativeInfinity);
        }
#endif

        // Small powers of ten. These are all the powers of ten that have an exact
        // representation in IEEE double precision format.
        public static readonly double[] C10toN = {
            1e00, 1e01, 1e02, 1e03, 1e04, 1e05, 1e06, 1e07, 1e08, 1e09,
            1e10, 1e11, 1e12, 1e13, 1e14, 1e15, 1e16, 1e17, 1e18, 1e19,
            1e20, 1e21, 1e22,
        };

        // Returns 1 if argument is non-zero, and 0 otherwise
        public static uint NotZero(uint u)
        {
            return 0 != u ? 1u : 0u;
        }

        /*  ----------------------------------------------------------------------------
            AddU()

            Add two unsigned ints and return the carry bit.
        */
        public static uint AddU(ref uint u1, uint u2)
        {
            u1 = unchecked(u1 + u2);
            return u1 < u2 ? 1u : 0u;
        }

        /*  ----------------------------------------------------------------------------
            MulU()

            Multiply two unsigned ints. Return the low uint and fill uHi with
            the high uint.
        */
        public static uint MulU(uint u1, uint u2, out uint uHi)
        {
            ulong result = (ulong)u1 * u2;
            uHi = (uint)(result >> 32);
            return unchecked((uint)result);
        }

        /*  ----------------------------------------------------------------------------
            CbitZeroLeft()

            Return a count of the number of leading 0 bits in u.
        */
        public static int CbitZeroLeft(uint u)
        {
            int cbit = 0;

            if (0 == (u & 0xFFFF0000))
            {
                cbit += 16;
                u <<= 16;
            }
            if (0 == (u & 0xFF000000))
            {
                cbit += 8;
                u <<= 8;
            }
            if (0 == (u & 0xF0000000))
            {
                cbit += 4;
                u <<= 4;
            }
            if (0 == (u & 0xC0000000))
            {
                cbit += 2;
                u <<= 2;
            }
            if (0 == (u & 0x80000000))
            {
                cbit += 1;
                u <<= 1;
            }
            Debug.Assert(0 != (u & 0x80000000));

            return cbit;
        }

        /*  ----------------------------------------------------------------------------
            IsInteger()

            If dbl is a whole number in the range of INT_MIN to INT_MAX, return true
            and the integer in value.  Otherwise, return false.
        */
        public static bool IsInteger(double dbl, out int value)
        {
            if (!IsSpecial(dbl))
            {
                int i = (int)dbl;
                double dblRound = (double)i;

                if (dbl == dblRound)
                {
                    value = i;
                    return true;
                }
            }
            value = 0;
            return false;
        }

        /**
        * Implementation of a big floating point number used to ensure adequate
        * precision when performing calculations.
        *
        * Hungarian: num
        *
        */
        private struct BigNumber
        {
            private uint _u0;
            private uint _u1;
            private uint _u2;
            private int _exp;

            // This is a bound on the absolute value of the error. It is based at
            // one bit before the least significant bit of u0.
            private uint _error;

            public uint Error { get { return _error; } }

            public BigNumber(uint u0, uint u1, uint u2, int exp, uint error)
            {
                _u0 = u0;
                _u1 = u1;
                _u2 = u2;
                _exp = exp;
                _error = error;
            }

#if !NOPARSE || DEBUG
            // Set the value from floating decimal
            public BigNumber(FloatingDecimal dec)
            {
                Debug.Assert(dec.MantissaSize > 0);

                int ib, exponent, mantissaSize, wT;
                uint uExtra;
                BigNumber[] TenPowers;

                ib = 0;
                exponent = dec.Exponent;
                mantissaSize = dec.MantissaSize;

                // Record the first digit
                Debug.Assert(dec[ib] > 0 && dec[ib] <= 9);
                _u2 = (uint)(dec[ib]) << 28;
                _u1 = 0;
                _u0 = 0;
                _exp = 4;
                _error = 0;
                exponent--;
                Normalize();

                while (++ib < mantissaSize)
                {
                    Debug.Assert(dec[ib] >= 0 && dec[ib] <= 9);
                    uExtra = MulTenAdd(dec[ib]);
                    exponent--;
                    if (0 != uExtra)
                    {
                        // We've filled up our precision.
                        Round(uExtra);
                        if (ib < mantissaSize - 1)
                        {
                            // There are more digits, so add another error bit just for
                            // safety's sake.
                            _error++;
                        }
                        break;
                    }
                }

                // Now multiply by 10^exp
                if (0 == exponent)
                {
                    return;
                }

                if (exponent < 0)
                {
                    TenPowers = s_tenPowersNeg;
                    exponent = -exponent;
                }
                else
                {
                    TenPowers = s_tenPowersPos;
                }

                Debug.Assert(exponent > 0 && exponent < 512);
                wT = exponent & 0x1F;
                if (wT > 0)
                {
                    Mul(ref TenPowers[wT - 1]);
                }

                wT = (exponent >> 5) & 0x0F;
                if (wT > 0)
                {
                    Mul(ref TenPowers[wT + 30]);
                }
            }

            // Multiply by ten and add a base 10 digit.
            private unsafe uint MulTenAdd(uint digit)
            {
                Debug.Assert(digit <= 9);
                Debug.Assert(0 != (_u2 & 0x80000000));

                // First "multipy" by eight
                _exp += 3;
                Debug.Assert(_exp >= 4);

                // Initialize the carry values based on digit and exp.
                uint* rgu = stackalloc uint[5];
                for (int i = 0; i < 5; i++)
                {
                    rgu[i] = 0;
                }
                if (0 != digit)
                {
                    int idx = 3 - (_exp >> 5);
                    if (idx < 0)
                    {
                        rgu[0] = 1;
                    }
                    else
                    {
                        int ibit = _exp & 0x1F;
                        if (ibit < 4)
                        {
                            rgu[idx + 1] = digit >> ibit;
                            if (ibit > 0)
                            {
                                rgu[idx] = digit << (32 - ibit);
                            }
                        }
                        else
                        {
                            rgu[idx] = digit << (32 - ibit);
                        }
                    }
                }

                // Shift and add to multiply by ten.
                rgu[1] += AddU(ref rgu[0], _u0 << 30);
                rgu[2] += AddU(ref _u0, (_u0 >> 2) + (_u1 << 30));
                if (0 != rgu[1])
                {
                    rgu[2] += AddU(ref _u0, rgu[1]);
                }
                rgu[3] += AddU(ref _u1, (_u1 >> 2) + (_u2 << 30));
                if (0 != rgu[2])
                {
                    rgu[3] += AddU(ref _u1, rgu[2]);
                }
                rgu[4] = AddU(ref _u2, (_u2 >> 2) + rgu[3]);

                // Handle the final carry.
                if (0 != rgu[4])
                {
                    Debug.Assert(1 == rgu[4]);
                    rgu[0] = (rgu[0] >> 1) | (rgu[0] & 1) | (_u0 << 31);
                    _u0 = (_u0 >> 1) | (_u1 << 31);
                    _u1 = (_u1 >> 1) | (_u2 << 31);
                    _u2 = (_u2 >> 1) | 0x80000000;
                    _exp++;
                }

                return rgu[0];
            }

            // Round based on the given extra data using IEEE round to nearest rule.
            private void Round(uint uExtra)
            {
                if (0 == (uExtra & 0x80000000) || 0 == (uExtra & 0x7FFFFFFF) && 0 == (_u0 & 1))
                {
                    if (0 != uExtra)
                    {
                        _error++;
                    }
                    return;
                }
                _error++;

                // Round up.
                if (0 != AddU(ref _u0, 1) && 0 != AddU(ref _u1, 1) && 0 != AddU(ref _u2, 1))
                {
                    Debug.Assert(this.IsZero);
                    _u2 = 0x80000000;
                    _exp++;
                }
            }
#endif

            // Test to see if the num is zero. This works even if we're not normalized.
            private bool IsZero
            {
                get
                {
                    return (0 == _u2) && (0 == _u1) && (0 == _u0);
                }
            }

            /*  ----------------------------------------------------------------------------
                Normalize()

                Normalize the big number - make sure the high bit is 1 or everything is zero
                (including the exponent).
            */
            private void Normalize()
            {
                int w1, w2;

                // Normalize mantissa
                if (0 == _u2)
                {
                    if (0 == _u1)
                    {
                        if (0 == _u0)
                        {
                            _exp = 0;
                            return;
                        }
                        _u2 = _u0;
                        _u0 = 0;
                        _exp -= 64;
                    }
                    else
                    {
                        _u2 = _u1;
                        _u1 = _u0;
                        _u0 = 0;
                        _exp -= 32;
                    }
                }

                if (0 != (w1 = CbitZeroLeft(_u2)))
                {
                    w2 = 32 - w1;
                    _u2 = (_u2 << w1) | (_u1 >> w2);
                    _u1 = (_u1 << w1) | (_u0 >> w2);
                    _u0 = (_u0 << w1);
                    _exp -= w1;
                }
            }

            /*  ----------------------------------------------------------------------------
                Mul()

                Multiply this big number by another big number.
            */
            private void Mul(ref BigNumber numOp)
            {
                Debug.Assert(0 != (_u2 & 0x80000000));
                Debug.Assert(0 != (numOp._u2 & 0x80000000));

                //uint *rgu = stackalloc uint[6];
                uint rgu0 = 0, rgu1 = 0, rgu2 = 0, rgu3 = 0, rgu4 = 0, rgu5 = 0;
                uint uLo, uHi, uT;
                uint wCarry;

                if (0 != (uT = _u0))
                {
                    uLo = MulU(uT, numOp._u0, out uHi);
                    rgu0 = uLo;
                    rgu1 = uHi;

                    uLo = MulU(uT, numOp._u1, out uHi);
                    Debug.Assert(uHi < 0xFFFFFFFF);
                    wCarry = AddU(ref rgu1, uLo);
                    AddU(ref rgu2, uHi + wCarry);

                    uLo = MulU(uT, numOp._u2, out uHi);
                    Debug.Assert(uHi < 0xFFFFFFFF);
                    wCarry = AddU(ref rgu2, uLo);
                    AddU(ref rgu3, uHi + wCarry);
                }

                if (0 != (uT = _u1))
                {
                    uLo = MulU(uT, numOp._u0, out uHi);
                    Debug.Assert(uHi < 0xFFFFFFFF);
                    wCarry = AddU(ref rgu1, uLo);
                    wCarry = AddU(ref rgu2, uHi + wCarry);
                    if (0 != wCarry && 0 != AddU(ref rgu3, 1))
                    {
                        AddU(ref rgu4, 1);
                    }
                    uLo = MulU(uT, numOp._u1, out uHi);
                    Debug.Assert(uHi < 0xFFFFFFFF);
                    wCarry = AddU(ref rgu2, uLo);
                    wCarry = AddU(ref rgu3, uHi + wCarry);
                    if (0 != wCarry)
                    {
                        AddU(ref rgu4, 1);
                    }
                    uLo = MulU(uT, numOp._u2, out uHi);
                    Debug.Assert(uHi < 0xFFFFFFFF);
                    wCarry = AddU(ref rgu3, uLo);
                    AddU(ref rgu4, uHi + wCarry);
                }

                uT = _u2;
                Debug.Assert(0 != uT);
                uLo = MulU(uT, numOp._u0, out uHi);
                Debug.Assert(uHi < 0xFFFFFFFF);
                wCarry = AddU(ref rgu2, uLo);
                wCarry = AddU(ref rgu3, uHi + wCarry);
                if (0 != wCarry && 0 != AddU(ref rgu4, 1))
                {
                    AddU(ref rgu5, 1);
                }
                uLo = MulU(uT, numOp._u1, out uHi);
                Debug.Assert(uHi < 0xFFFFFFFF);
                wCarry = AddU(ref rgu3, uLo);
                wCarry = AddU(ref rgu4, uHi + wCarry);
                if (0 != wCarry)
                {
                    AddU(ref rgu5, 1);
                }
                uLo = MulU(uT, numOp._u2, out uHi);
                Debug.Assert(uHi < 0xFFFFFFFF);
                wCarry = AddU(ref rgu4, uLo);
                AddU(ref rgu5, uHi + wCarry);

                // Compute the new exponent
                _exp += numOp._exp;

                // Accumulate the error. Adding doesn't necessarily give an accurate
                // bound if both of the errors are bigger than 2.
                Debug.Assert(_error <= 2 || numOp._error <= 2);
                _error += numOp._error;

                // Handle rounding and normalize.
                if (0 == (rgu5 & 0x80000000))
                {
                    if (0 != (rgu2 & 0x40000000) &&
                            (0 != (rgu2 & 0xBFFFFFFF) || 0 != rgu1 || 0 != rgu0))
                    {
                        // Round up by 1
                        if (0 != AddU(ref rgu2, 0x40000000)
                            && 0 != AddU(ref rgu3, 1)
                            && 0 != AddU(ref rgu4, 1)
                        )
                        {
                            AddU(ref rgu5, 1);
                            if (0 != (rgu5 & 0x80000000))
                            {
                                goto LNormalized;
                            }
                        }
                    }

                    // have to shift by one
                    Debug.Assert(0 != (rgu5 & 0x40000000));
                    _u2 = (rgu5 << 1) | (rgu4 >> 31);
                    _u1 = (rgu4 << 1) | (rgu3 >> 31);
                    _u0 = (rgu3 << 1) | (rgu2 >> 31);
                    _exp--;
                    _error <<= 1;

                    // Add one for the error.
                    if (0 != (rgu2 & 0x7FFFFFFF) || 0 != rgu1 || 0 != rgu0)
                    {
                        _error++;
                    }
                    return;
                }
                else
                {
                    if (0 != (rgu2 & 0x80000000) &&
                        (0 != (rgu3 & 1) || 0 != (rgu2 & 0x7FFFFFFF) ||
                            0 != rgu1 || 0 != rgu0))
                    {
                        // Round up by 1
                        if (0 != AddU(ref rgu3, 1) && 0 != AddU(ref rgu4, 1) && 0 != AddU(ref rgu5, 1))
                        {
                            Debug.Assert(0 == rgu3);
                            Debug.Assert(0 == rgu4);
                            Debug.Assert(0 == rgu5);
                            rgu5 = 0x80000000;
                            _exp++;
                        }
                    }
                }

            LNormalized:
                _u2 = rgu5;
                _u1 = rgu4;
                _u0 = rgu3;

                // Add one for the error.
                if (0 != rgu2 || 0 != rgu1 || 0 != rgu0)
                {
                    _error++;
                }
            }

            // Get the double value.
            public static explicit operator double(BigNumber bn)
            {
                uint uEx;
                int exp;
                uint dblHi, dblLo;

                Debug.Assert(0 != (bn._u2 & 0x80000000));

                exp = bn._exp + 1022;
                if (exp >= 2047)
                {
                    return double.PositiveInfinity;
                }

                // Round after filling in the bits. In the extra uint, we set the low bit
                // if there are any extra non-zero bits. This is for breaking the tie when
                // deciding whether to round up or down.
                if (exp > 0)
                {
                    // Normalized.
                    dblHi = ((uint)exp << 20) | ((bn._u2 & 0x7FFFFFFF) >> 11);
                    dblLo = bn._u2 << 21 | bn._u1 >> 11;
                    uEx = bn._u1 << 21 | NotZero(bn._u0);
                }
                else if (exp > -20)
                {
                    // Denormal with some high bits.
                    int wT = 12 - exp;
                    Debug.Assert(wT >= 12 && wT < 32);

                    dblHi = bn._u2 >> wT;
                    dblLo = (bn._u2 << (32 - wT)) | (bn._u1 >> wT);
                    uEx = (bn._u1 << (32 - wT)) | NotZero(bn._u0);
                }
                else if (exp == -20)
                {
                    // Denormal with no high bits.
                    dblHi = 0;
                    dblLo = bn._u2;
                    uEx = bn._u1 | (uint)(0 != bn._u0 ? 1 : 0);
                }
                else if (exp > -52)
                {
                    // Denormal with no high bits.
                    int wT = -exp - 20;
                    Debug.Assert(wT > 0 && wT < 32);

                    dblHi = 0;
                    dblLo = bn._u2 >> wT;
                    uEx = bn._u2 << (32 - wT) | NotZero(bn._u1) | NotZero(bn._u0);
                }
                else if (exp == -52)
                {
                    // Zero unless we round up below.
                    dblHi = 0;
                    dblLo = 0;
                    uEx = bn._u2 | NotZero(bn._u1) | NotZero(bn._u0);
                }
                else
                {
                    return 0.0;
                }

                // Handle rounding
                if (0 != (uEx & 0x80000000) && (0 != (uEx & 0x7FFFFFFF) || 0 != (dblLo & 1)))
                {
                    // Round up. Note that this works even when we overflow into the
                    // exponent.
                    if (0 != AddU(ref dblLo, 1))
                    {
                        AddU(ref dblHi, 1);
                    }
                }
                return BitConverter.UInt64BitsToDouble((ulong)dblHi << 32 | dblLo);
            }

            // Lop off the integer part and return it.
            private uint UMod1()
            {
                if (_exp <= 0)
                {
                    return 0;
                }
                Debug.Assert(_exp <= 32);
                uint uT = _u2 >> (32 - _exp);
                _u2 &= (uint)0x7FFFFFFF >> (_exp - 1);
                Normalize();
                return uT;
            }

            // If error is not zero, add it and set error to zero.
            public void MakeUpperBound()
            {
                Debug.Assert(_error < 0xFFFFFFFF);
                uint uT = (_error + 1) >> 1;

                if (0 != uT && 0 != AddU(ref _u0, uT) && 0 != AddU(ref _u1, 1) && 0 != AddU(ref _u2, 1))
                {
                    Debug.Assert(0 == _u2 && 0 == _u1);
                    _u2 = 0x80000000;
                    _u0 = (_u0 >> 1) + (_u0 & 1);
                    _exp++;
                }
                _error = 0;
            }

            // If error is not zero, subtract it and set error to zero.
            public void MakeLowerBound()
            {
                Debug.Assert(_error < 0xFFFFFFFF);
                uint uT = (_error + 1) >> 1;

                if (0 != uT && 0 == AddU(ref _u0, unchecked((uint)-(int)uT)) && 0 == AddU(ref _u1, 0xFFFFFFFF))
                {
                    AddU(ref _u2, 0xFFFFFFFF);
                    if (0 == (0x80000000 & _u2))
                    {
                        Normalize();
                    }
                }
                _error = 0;
            }

            /*  ----------------------------------------------------------------------------
                DblToRgbFast()

                Get mantissa bytes (BCD).
            */
            public static bool DblToRgbFast(double dbl, byte[] mantissa, out int exponent, out int mantissaSize)
            {
                BigNumber numHH, numHL, numLH, numLL;
                BigNumber numBase;
                BigNumber tenPower;
                int ib, iT;
                uint uT, uScale;
                byte bHH, bHL, bLH, bLL;
                uint uHH, uHL, uLH, uLL;
                int wExp2, wExp10 = 0;
                double dblInt;
                uint dblHi, dblLo;

                dblHi = DblHi(dbl);
                dblLo = DblLo(dbl);

                // Caller should take care of 0, negative and non-finite values.
                Debug.Assert(!IsSpecial(dbl));
                Debug.Assert(0 < dbl);

                // Get numHH and numLL such that numLL < dbl < numHH and the
                // difference between adjacent values is half the distance to the next
                // representable value (in a double).
                wExp2 = (int)((dblHi >> 20) & 0x07FF);
                if (wExp2 > 0)
                {
                    // See if dbl is a small integer.
                    if (wExp2 >= 1023 && wExp2 <= 1075 && dbl == Math.Floor(dbl))
                    {
                        goto LSmallInt;
                    }

                    // Normalized
                    numBase._u2 = 0x80000000 | ((dblHi & 0x000FFFFFF) << 11) | (dblLo >> 21);
                    numBase._u1 = dblLo << 11;
                    numBase._u0 = 0;
                    numBase._exp = wExp2 - 1022;
                    numBase._error = 0;

                    // Get the upper bound
                    numHH = numBase;
                    numHH._u1 |= (1 << 10);

                    // Get the lower bound. A power of 2 must be special cased.
                    numLL = numBase;
                    if (0x80000000 == numLL._u2 && 0 == numLL._u1)
                    {
                        // Subtract (0x00000000, 0x00000200, 0x00000000). Same as adding
                        // (0xFFFFFFFF, 0xFFFFFE00, 0x00000000)
                        uT = 0xFFFFFE00;
                    }
                    else
                    {
                        // Subtract (0x00000000, 0x00000400, 0x00000000). Same as adding
                        // (0xFFFFFFFF, 0xFFFFFC00, 0x00000000)
                        uT = 0xFFFFFC00;
                    }
                    if (0 == AddU(ref numLL._u1, uT))
                    {
                        AddU(ref numLL._u2, 0xFFFFFFFF);
                        if (0 == (0x80000000 & numLL._u2))
                        {
                            numLL.Normalize();
                        }
                    }
                }
                else
                {
                    // Denormal
                    numBase._u2 = dblHi & 0x000FFFFF;
                    numBase._u1 = dblLo;
                    numBase._u0 = 0;
                    numBase._exp = -1010;
                    numBase._error = 0;

                    // Get the upper bound
                    numHH = numBase;
                    numHH._u0 = 0x80000000;

                    // Get the lower bound
                    numLL = numHH;
                    if (0 == AddU(ref numLL._u1, 0xFFFFFFFF))
                    {
                        AddU(ref numLL._u2, 0xFFFFFFFF);
                    }

                    numBase.Normalize();
                    numHH.Normalize();
                    numLL.Normalize();
                }

                // Multiply by powers of ten until 0 < numHH.exp < 32.
                if (numHH._exp >= 32)
                {
                    iT = (numHH._exp - 25) * 15 / -s_tenPowersNeg[45]._exp;
                    Debug.Assert(iT >= 0 && iT < 16);
                    if (iT > 0)
                    {
                        tenPower = s_tenPowersNeg[30 + iT];
                        Debug.Assert(numHH._exp + tenPower._exp > 1);
                        numHH.Mul(ref tenPower);
                        numLL.Mul(ref tenPower);
                        wExp10 += iT * 32;
                    }

                    if (numHH._exp >= 32)
                    {
                        iT = (numHH._exp - 25) * 32 / -s_tenPowersNeg[31]._exp;
                        Debug.Assert(iT > 0 && iT <= 32);
                        tenPower = s_tenPowersNeg[iT - 1];
                        Debug.Assert(numHH._exp + tenPower._exp > 1);
                        numHH.Mul(ref tenPower);
                        numLL.Mul(ref tenPower);
                        wExp10 += iT;
                    }
                }
                else if (numHH._exp < 1)
                {
                    iT = (25 - numHH._exp) * 15 / s_tenPowersPos[45]._exp;
                    Debug.Assert(iT >= 0 && iT < 16);
                    if (iT > 0)
                    {
                        tenPower = s_tenPowersPos[30 + iT];
                        Debug.Assert(numHH._exp + tenPower._exp <= 32);
                        numHH.Mul(ref tenPower);
                        numLL.Mul(ref tenPower);
                        wExp10 -= iT * 32;
                    }

                    if (numHH._exp < 1)
                    {
                        iT = (25 - numHH._exp) * 32 / s_tenPowersPos[31]._exp;
                        Debug.Assert(iT > 0 && iT <= 32);
                        tenPower = s_tenPowersPos[iT - 1];
                        Debug.Assert(numHH._exp + tenPower._exp <= 32);
                        numHH.Mul(ref tenPower);
                        numLL.Mul(ref tenPower);
                        wExp10 -= iT;
                    }
                }

                Debug.Assert(numHH._exp > 0 && numHH._exp < 32);

                // Get the upper and lower bounds for these.
                numHL = numHH;
                numHH.MakeUpperBound();
                numHL.MakeLowerBound();
                uHH = numHH.UMod1();
                uHL = numHL.UMod1();
                numLH = numLL;
                numLH.MakeUpperBound();
                numLL.MakeLowerBound();
                uLH = numLH.UMod1();
                uLL = numLL.UMod1();
                Debug.Assert(uLL <= uLH && uLH <= uHL && uHL <= uHH);

                // Find the starting scale
                uScale = 1;
                if (uHH >= 100000000)
                {
                    uScale = 100000000;
                    wExp10 += 8;
                }
                else
                {
                    if (uHH >= 10000)
                    {
                        uScale = 10000;
                        wExp10 += 4;
                    }
                    if (uHH >= 100 * uScale)
                    {
                        uScale *= 100;
                        wExp10 += 2;
                    }
                }
                if (uHH >= 10 * uScale)
                {
                    uScale *= 10;
                    wExp10++;
                }
                wExp10++;
                Debug.Assert(uHH >= uScale && uHH / uScale < 10);

                for (ib = 0; ;)
                {
                    Debug.Assert(uLL <= uHH);
                    bHH = (byte)(uHH / uScale);
                    uHH %= uScale;
                    bLL = (byte)(uLL / uScale);
                    uLL %= uScale;

                    if (bHH != bLL)
                    {
                        break;
                    }

                    Debug.Assert(0 != uHH || !numHH.IsZero);
                    mantissa[ib++] = bHH;

                    if (1 == uScale)
                    {
                        // Multiply by 10^8.
                        uScale = 10000000;

                        numHH.Mul(ref s_tenPowersPos[7]);
                        numHH.MakeUpperBound();
                        uHH = numHH.UMod1();
                        if (uHH >= 100000000)
                        {
                            goto LFail;
                        }
                        numHL.Mul(ref s_tenPowersPos[7]);
                        numHL.MakeLowerBound();
                        uHL = numHL.UMod1();

                        numLH.Mul(ref s_tenPowersPos[7]);
                        numLH.MakeUpperBound();
                        uLH = numLH.UMod1();
                        numLL.Mul(ref s_tenPowersPos[7]);
                        numLL.MakeLowerBound();
                        uLL = numLL.UMod1();
                    }
                    else
                    {
                        uScale /= 10;
                    }
                }

                // LL and HH diverged. Get the digit values for LH and HL.
                Debug.Assert(0 <= bLL && bLL < bHH && bHH <= 9);
                bLH = (byte)((uLH / uScale) % 10);
                uLH %= uScale;
                bHL = (byte)((uHL / uScale) % 10);
                uHL %= uScale;

                if (bLH >= bHL)
                {
                    goto LFail;
                }

                // LH and HL also diverged.

                // We can get by with one fewer digit if: LL == LH and bLH is zero
                // and the current value of LH is zero and the least significant bit of
                // the double is zero. In this case, we have exactly the digit sequence
                // for the original numLL and IEEE and will rounds numLL up to the double.
                if (0 == bLH && 0 == uLH && numLH.IsZero && 0 == (dblLo & 1))
                {
                }
                else if (bHL - bLH > 1)
                {
                    // HL and LH differ by at least two in this digit, so split
                    // the difference.
                    mantissa[ib++] = (byte)((bHL + bLH + 1) / 2);
                }
                else if (0 != uHL || !numHL.IsZero || 0 == (dblLo & 1))
                {
                    // We can just use bHL because this guarantees that we're bigger than
                    // LH and less than HL, so must convert to the double.
                    mantissa[ib++] = bHL;
                }
                else
                {
                    goto LFail;
                }

                exponent = wExp10;
                mantissaSize = ib;
                goto LSucceed;

            LSmallInt:
                // dbl should be an integer from 1 to (2^53 - 1).
                dblInt = dbl;
                Debug.Assert(dblInt == Math.Floor(dblInt) && 1 <= dblInt && dblInt <= 9007199254740991.0d);

                iT = 0;
                if (dblInt >= C10toN[iT + 8])
                {
                    iT += 8;
                }
                if (dblInt >= C10toN[iT + 4])
                {
                    iT += 4;
                }
                if (dblInt >= C10toN[iT + 2])
                {
                    iT += 2;
                }
                if (dblInt >= C10toN[iT + 1])
                {
                    iT += 1;
                }
                Debug.Assert(iT >= 0 && iT <= 15);
                Debug.Assert(dblInt >= C10toN[iT] && dblInt < C10toN[iT + 1]);
                exponent = iT + 1;

                for (ib = 0; 0 != dblInt; iT--)
                {
                    Debug.Assert(iT >= 0);
                    bHH = (byte)(dblInt / C10toN[iT]);
                    dblInt -= bHH * C10toN[iT];
                    Debug.Assert(dblInt == Math.Floor(dblInt) && 0 <= dblInt && dblInt < C10toN[iT]);
                    mantissa[ib++] = bHH;
                }
                mantissaSize = ib;

            LSucceed:

#if DEBUG
                // Verify that precise is working and gives the same answer
                if (mantissaSize > 0)
                {
                    byte[] mantissaPrec = new byte[20];
                    int exponentPrec, mantissaSizePrec, idx;

                    DblToRgbPrecise(dbl, mantissaPrec, out exponentPrec, out mantissaSizePrec);
                    Debug.Assert(exponent == exponentPrec && mantissaSize == mantissaSizePrec);
                    // Assert(!memcmp(mantissa, mantissaPrec, mantissaSizePrec - 1));
                    bool equal = true;
                    for (idx = 0; idx < mantissaSize; idx++)
                    {
                        equal &= (
                            (mantissa[idx] == mantissaPrec[idx]) ||
                            (idx == mantissaSize - 1) && Math.Abs(mantissa[idx] - mantissaPrec[idx]) <= 1
                        );
                    }
                    Debug.Assert(equal, "DblToRgbFast and DblToRgbPrecise should give the same result");
                }
#endif

                return true;

            LFail:
                exponent = mantissaSize = 0;
                return false;
            }

            /*  ----------------------------------------------------------------------------
                DblToRgbPrecise()

                Uses big integer arithmetic to get the sequence of digits.
            */
            public static void DblToRgbPrecise(double dbl, byte[] mantissa, out int exponent, out int mantissaSize)
            {
                BigInteger biNum = new BigInteger();
                BigInteger biDen = new BigInteger();
                BigInteger biHi = new BigInteger();
                BigInteger biLo = new BigInteger();
                BigInteger biT = new BigInteger();
                BigInteger biHiLo;
                byte bT;
                bool fPow2;
                int ib, cu;
                int wExp10, wExp2, w1, w2;
                int c2Num, c2Den, c5Num, c5Den;
                double dblT;
                //uint *rgu = stackalloc uint[2];
                uint rgu0, rgu1;
                uint dblHi, dblLo;

                dblHi = DblHi(dbl);
                dblLo = DblLo(dbl);

                // Caller should take care of 0, negative and non-finite values.
                Debug.Assert(!IsSpecial(dbl));
                Debug.Assert(0 < dbl);

                // Init the Denominator, Hi error and Lo error bigints.
                biDen.InitFromDigits(1, 0, 1);
                biHi.InitFromDigits(1, 0, 1);

                wExp2 = (int)(((dblHi & 0x7FF00000) >> 20) - 1075);
                rgu1 = dblHi & 0x000FFFFF;
                rgu0 = dblLo;
                cu = 2;
                fPow2 = false;
                if (wExp2 == -1075)
                {
                    // dbl is denormalized.
                    Debug.Assert(0 == (dblHi & 0x7FF00000));
                    if (0 == rgu1)
                    {
                        cu = 1;
                    }

                    // Get dblT such that dbl / dblT is a power of 2 and 1 <= dblT < 2.
                    // First multiply by a power of 2 to get a normalized value.
                    dblT = BitConverter.Int64BitsToDouble(0x4FF00000L << 32);
                    dblT *= dbl;
                    Debug.Assert(0 != (DblHi(dblT) & 0x7FF00000));

                    // This is the power of 2.
                    w1 = (int)((DblHi(dblT) & 0x7FF00000) >> 20) - (256 + 1023);

                    dblHi = DblHi(dblT);
                    dblHi &= 0x000FFFFF;
                    dblHi |= 0x3FF00000;
                    dblT = BitConverter.UInt64BitsToDouble((ulong)dblHi << 32 | DblLo(dblT));

                    // Adjust wExp2 because we don't have the implicit bit.
                    wExp2++;
                }
                else
                {
                    // Get dblT such that dbl / dblT is a power of 2 and 1 <= dblT < 2.
                    // First multiply by a power of 2 to get a normalized value.
                    dblHi &= 0x000FFFFF;
                    dblHi |= 0x3FF00000;
                    dblT = BitConverter.UInt64BitsToDouble((ulong)dblHi << 32 | dblLo);

                    // This is the power of 2.
                    w1 = wExp2 + 52;

                    if (0 == rgu0 && 0 == rgu1 && wExp2 > -1074)
                    {
                        // Power of 2 bigger than smallest normal. The next smaller
                        // representable value is closer than the next larger value.
                        rgu1 = 0x00200000;
                        wExp2--;
                        fPow2 = true;
                    }
                    else
                    {
                        // Normalized and not a power of 2 or the smallest normal. The
                        // representable values on either side are the same distance away.
                        rgu1 |= 0x00100000;
                    }
                }

                // Compute an approximation to the base 10 log. This is borrowed from
                // David Gay's paper.
                Debug.Assert(1 <= dblT && dblT < 2);
                dblT = (dblT - 1.5) * 0.289529654602168 + 0.1760912590558 +
                    w1 * 0.301029995663981;
                wExp10 = (int)dblT;
                if (dblT < 0 && dblT != wExp10)
                {
                    wExp10--;
                }

                if (wExp2 >= 0)
                {
                    c2Num = wExp2;
                    c2Den = 0;
                }
                else
                {
                    c2Num = 0;
                    c2Den = -wExp2;
                }

                if (wExp10 >= 0)
                {
                    c5Num = 0;
                    c5Den = wExp10;
                    c2Den += wExp10;
                }
                else
                {
                    c2Num -= wExp10;
                    c5Num = -wExp10;
                    c5Den = 0;
                }

                if (c2Num > 0 && c2Den > 0)
                {
                    w1 = c2Num < c2Den ? c2Num : c2Den;
                    c2Num -= w1;
                    c2Den -= w1;
                }
                // We need a bit for the Hi and Lo values.
                c2Num++;
                c2Den++;

                // Initialize biNum and multiply by powers of 5.
                if (c5Num > 0)
                {
                    Debug.Assert(0 == c5Den);
                    biHi.MulPow5(c5Num);
                    biNum.InitFromBigint(biHi);
                    if (1 == cu)
                    {
                        biNum.MulAdd(rgu0, 0);
                    }
                    else
                    {
                        biNum.MulAdd(rgu1, 0);
                        biNum.ShiftLeft(32);
                        if (0 != rgu0)
                        {
                            biT.InitFromBigint(biHi);
                            biT.MulAdd(rgu0, 0);
                            biNum.Add(biT);
                        }
                    }
                }
                else
                {
                    Debug.Assert(cu <= 2);
                    biNum.InitFromDigits(rgu0, rgu1, cu);
                    if (c5Den > 0)
                    {
                        biDen.MulPow5(c5Den);
                    }
                }

                // BigInteger.DivRem only works if the 4 high bits of the divisor are 0.
                // It works most efficiently if there are exactly 4 zero high bits.
                // Adjust c2Den and c2Num to guarantee this.
                w1 = CbitZeroLeft(biDen[biDen.Length - 1]);
                w1 = (w1 + 28 - c2Den) & 0x1F;
                c2Num += w1;
                c2Den += w1;

                // Multiply by powers of 2.
                Debug.Assert(c2Num > 0 && c2Den > 0);
                biNum.ShiftLeft(c2Num);
                if (c2Num > 1)
                {
                    biHi.ShiftLeft(c2Num - 1);
                }
                biDen.ShiftLeft(c2Den);
                Debug.Assert(0 == (biDen[biDen.Length - 1] & 0xF0000000));
                Debug.Assert(0 != (biDen[biDen.Length - 1] & 0x08000000));

                // Get biHiLo and handle the power of 2 case where biHi needs to be doubled.
                if (fPow2)
                {
                    biHiLo = biLo;
                    biHiLo.InitFromBigint(biHi);
                    biHi.ShiftLeft(1);
                }
                else
                {
                    biHiLo = biHi;
                }

                for (ib = 0; ;)
                {
                    bT = (byte)biNum.DivRem(biDen);
                    if (0 == ib && 0 == bT)
                    {
                        // Our estimate of wExp10 was too big. Oh well.
                        wExp10--;
                        goto LSkip;
                    }

                    // w1 = sign(biNum - biHiLo).
                    w1 = biNum.CompareTo(biHiLo);

                    // w2 = sign(biNum + biHi - biDen).
                    if (biDen.CompareTo(biHi) < 0)
                    {
                        w2 = 1;
                    }
                    else
                    {
                        // REVIEW: is there a faster way to do this?
                        biT.InitFromBigint(biDen);
                        biT.Subtract(biHi);
                        w2 = biNum.CompareTo(biT);
                    }

                    // if (biNum + biHi == biDen && even)
                    if (0 == w2 && 0 == (dblLo & 1))
                    {
                        // Rounding up this digit produces exactly (biNum + biHi) which
                        // StrToDbl will round down to dbl.
                        if (bT == 9)
                        {
                            goto LRoundUp9;
                        }
                        if (w1 > 0)
                        {
                            bT++;
                        }
                        mantissa[ib++] = bT;
                        break;
                    }

                    // if (biNum < biHiLo || biNum == biHiLo && even)
                    if (w1 < 0 || 0 == w1 && 0 == (dblLo & 1))
                    {
                        // if (biNum + biHi > biDen)
                        if (w2 > 0)
                        {
                            // Decide whether to round up.
                            biNum.ShiftLeft(1);
                            w2 = biNum.CompareTo(biDen);
                            if ((w2 > 0 || 0 == w2 && (0 != (bT & 1))) && bT++ == 9)
                            {
                                goto LRoundUp9;
                            }
                        }
                        mantissa[ib++] = bT;
                        break;
                    }

                    // if (biNum + biHi > biDen)
                    if (w2 > 0)
                    {
                        // Round up and be done with it.
                        if (bT != 9)
                        {
                            mantissa[ib++] = (byte)(bT + 1);
                            break;
                        }
                        goto LRoundUp9;
                    }

                    // Save the digit.
                    mantissa[ib++] = bT;

                LSkip:
                    biNum.MulAdd(10, 0);
                    biHi.MulAdd(10, 0);
                    if ((object)biHiLo != (object)biHi)
                    {
                        biHiLo.MulAdd(10, 0);
                    }
                    continue;

                LRoundUp9:
                    while (ib > 0)
                    {
                        if (mantissa[--ib] != 9)
                        {
                            mantissa[ib++]++;
                            goto LReturn;
                        }
                    }
                    wExp10++;
                    mantissa[ib++] = 1;
                    break;
                }

            LReturn:
                exponent = wExp10 + 1;
                mantissaSize = ib;
            }

            #region Powers of ten
            private static readonly BigNumber[] s_tenPowersPos = new BigNumber[46] {
                // Positive powers of 10 to 96 bits precision.
                new BigNumber( 0x00000000, 0x00000000, 0xA0000000,     4, 0 ), // 10^1
                new BigNumber( 0x00000000, 0x00000000, 0xC8000000,     7, 0 ), // 10^2
                new BigNumber( 0x00000000, 0x00000000, 0xFA000000,    10, 0 ), // 10^3
                new BigNumber( 0x00000000, 0x00000000, 0x9C400000,    14, 0 ), // 10^4
                new BigNumber( 0x00000000, 0x00000000, 0xC3500000,    17, 0 ), // 10^5
                new BigNumber( 0x00000000, 0x00000000, 0xF4240000,    20, 0 ), // 10^6
                new BigNumber( 0x00000000, 0x00000000, 0x98968000,    24, 0 ), // 10^7
                new BigNumber( 0x00000000, 0x00000000, 0xBEBC2000,    27, 0 ), // 10^8
                new BigNumber( 0x00000000, 0x00000000, 0xEE6B2800,    30, 0 ), // 10^9
                new BigNumber( 0x00000000, 0x00000000, 0x9502F900,    34, 0 ), // 10^10
                new BigNumber( 0x00000000, 0x00000000, 0xBA43B740,    37, 0 ), // 10^11
                new BigNumber( 0x00000000, 0x00000000, 0xE8D4A510,    40, 0 ), // 10^12
                new BigNumber( 0x00000000, 0x00000000, 0x9184E72A,    44, 0 ), // 10^13
                new BigNumber( 0x00000000, 0x80000000, 0xB5E620F4,    47, 0 ), // 10^14
                new BigNumber( 0x00000000, 0xA0000000, 0xE35FA931,    50, 0 ), // 10^15
                new BigNumber( 0x00000000, 0x04000000, 0x8E1BC9BF,    54, 0 ), // 10^16
                new BigNumber( 0x00000000, 0xC5000000, 0xB1A2BC2E,    57, 0 ), // 10^17
                new BigNumber( 0x00000000, 0x76400000, 0xDE0B6B3A,    60, 0 ), // 10^18
                new BigNumber( 0x00000000, 0x89E80000, 0x8AC72304,    64, 0 ), // 10^19
                new BigNumber( 0x00000000, 0xAC620000, 0xAD78EBC5,    67, 0 ), // 10^20
                new BigNumber( 0x00000000, 0x177A8000, 0xD8D726B7,    70, 0 ), // 10^21
                new BigNumber( 0x00000000, 0x6EAC9000, 0x87867832,    74, 0 ), // 10^22
                new BigNumber( 0x00000000, 0x0A57B400, 0xA968163F,    77, 0 ), // 10^23
                new BigNumber( 0x00000000, 0xCCEDA100, 0xD3C21BCE,    80, 0 ), // 10^24
                new BigNumber( 0x00000000, 0x401484A0, 0x84595161,    84, 0 ), // 10^25
                new BigNumber( 0x00000000, 0x9019A5C8, 0xA56FA5B9,    87, 0 ), // 10^26
                new BigNumber( 0x00000000, 0xF4200F3A, 0xCECB8F27,    90, 0 ), // 10^27
                new BigNumber( 0x40000000, 0xF8940984, 0x813F3978,    94, 0 ), // 10^28
                new BigNumber( 0x50000000, 0x36B90BE5, 0xA18F07D7,    97, 0 ), // 10^29
                new BigNumber( 0xA4000000, 0x04674EDE, 0xC9F2C9CD,   100, 0 ), // 10^30
                new BigNumber( 0x4D000000, 0x45812296, 0xFC6F7C40,   103, 0 ), // 10^31
                new BigNumber( 0xF0200000, 0x2B70B59D, 0x9DC5ADA8,   107, 0 ), // 10^32
                new BigNumber( 0x3CBF6B72, 0xFFCFA6D5, 0xC2781F49,   213, 1 ), // 10^64   (rounded up)
                new BigNumber( 0xC5CFE94F, 0xC59B14A2, 0xEFB3AB16,   319, 1 ), // 10^96   (rounded up)
                new BigNumber( 0xC66F336C, 0x80E98CDF, 0x93BA47C9,   426, 1 ), // 10^128
                new BigNumber( 0x577B986B, 0x7FE617AA, 0xB616A12B,   532, 1 ), // 10^160
                new BigNumber( 0x85BBE254, 0x3927556A, 0xE070F78D,   638, 1 ), // 10^192  (rounded up)
                new BigNumber( 0x82BD6B71, 0xE33CC92F, 0x8A5296FF,   745, 1 ), // 10^224  (rounded up)
                new BigNumber( 0xDDBB901C, 0x9DF9DE8D, 0xAA7EEBFB,   851, 1 ), // 10^256  (rounded up)
                new BigNumber( 0x73832EEC, 0x5C6A2F8C, 0xD226FC19,   957, 1 ), // 10^288
                new BigNumber( 0xE6A11583, 0xF2CCE375, 0x81842F29,  1064, 1 ), // 10^320
                new BigNumber( 0x5EBF18B7, 0xDB900AD2, 0x9FA42700,  1170, 1 ), // 10^352  (rounded up)
                new BigNumber( 0x1027FFF5, 0xAEF8AA17, 0xC4C5E310,  1276, 1 ), // 10^384
                new BigNumber( 0xB5E54F71, 0xE9B09C58, 0xF28A9C07,  1382, 1 ), // 10^416
                new BigNumber( 0xA7EA9C88, 0xEBF7F3D3, 0x957A4AE1,  1489, 1 ), // 10^448
                new BigNumber( 0x7DF40A74, 0x0795A262, 0xB83ED8DC,  1595, 1 ), // 10^480
            };

            private static readonly BigNumber[] s_tenPowersNeg = new BigNumber[46] {
                // Negative powers of 10 to 96 bits precision.
                new BigNumber( 0xCCCCCCCD, 0xCCCCCCCC, 0xCCCCCCCC,    -3, 1 ), // 10^-1   (rounded up)
                new BigNumber( 0x3D70A3D7, 0x70A3D70A, 0xA3D70A3D,    -6, 1 ), // 10^-2
                new BigNumber( 0x645A1CAC, 0x8D4FDF3B, 0x83126E97,    -9, 1 ), // 10^-3
                new BigNumber( 0xD3C36113, 0xE219652B, 0xD1B71758,   -13, 1 ), // 10^-4
                new BigNumber( 0x0FCF80DC, 0x1B478423, 0xA7C5AC47,   -16, 1 ), // 10^-5
                new BigNumber( 0xA63F9A4A, 0xAF6C69B5, 0x8637BD05,   -19, 1 ), // 10^-6   (rounded up)
                new BigNumber( 0x3D329076, 0xE57A42BC, 0xD6BF94D5,   -23, 1 ), // 10^-7
                new BigNumber( 0xFDC20D2B, 0x8461CEFC, 0xABCC7711,   -26, 1 ), // 10^-8
                new BigNumber( 0x31680A89, 0x36B4A597, 0x89705F41,   -29, 1 ), // 10^-9   (rounded up)
                new BigNumber( 0xB573440E, 0xBDEDD5BE, 0xDBE6FECE,   -33, 1 ), // 10^-10
                new BigNumber( 0xF78F69A5, 0xCB24AAFE, 0xAFEBFF0B,   -36, 1 ), // 10^-11
                new BigNumber( 0xF93F87B7, 0x6F5088CB, 0x8CBCCC09,   -39, 1 ), // 10^-12
                new BigNumber( 0x2865A5F2, 0x4BB40E13, 0xE12E1342,   -43, 1 ), // 10^-13
                new BigNumber( 0x538484C2, 0x095CD80F, 0xB424DC35,   -46, 1 ), // 10^-14  (rounded up)
                new BigNumber( 0x0F9D3701, 0x3AB0ACD9, 0x901D7CF7,   -49, 1 ), // 10^-15
                new BigNumber( 0x4C2EBE68, 0xC44DE15B, 0xE69594BE,   -53, 1 ), // 10^-16
                new BigNumber( 0x09BEFEBA, 0x36A4B449, 0xB877AA32,   -56, 1 ), // 10^-17  (rounded up)
                new BigNumber( 0x3AFF322E, 0x921D5D07, 0x9392EE8E,   -59, 1 ), // 10^-18
                new BigNumber( 0x2B31E9E4, 0xB69561A5, 0xEC1E4A7D,   -63, 1 ), // 10^-19  (rounded up)
                new BigNumber( 0x88F4BB1D, 0x92111AEA, 0xBCE50864,   -66, 1 ), // 10^-20  (rounded up)
                new BigNumber( 0xD3F6FC17, 0x74DA7BEE, 0x971DA050,   -69, 1 ), // 10^-21  (rounded up)
                new BigNumber( 0x5324C68B, 0xBAF72CB1, 0xF1C90080,   -73, 1 ), // 10^-22
                new BigNumber( 0x75B7053C, 0x95928A27, 0xC16D9A00,   -76, 1 ), // 10^-23
                new BigNumber( 0xC4926A96, 0x44753B52, 0x9ABE14CD,   -79, 1 ), // 10^-24
                new BigNumber( 0x3A83DDBE, 0xD3EEC551, 0xF79687AE,   -83, 1 ), // 10^-25  (rounded up)
                new BigNumber( 0x95364AFE, 0x76589DDA, 0xC6120625,   -86, 1 ), // 10^-26
                new BigNumber( 0x775EA265, 0x91E07E48, 0x9E74D1B7,   -89, 1 ), // 10^-27  (rounded up)
                new BigNumber( 0x8BCA9D6E, 0x8300CA0D, 0xFD87B5F2,   -93, 1 ), // 10^-28
                new BigNumber( 0x096EE458, 0x359A3B3E, 0xCAD2F7F5,   -96, 1 ), // 10^-29
                new BigNumber( 0xA125837A, 0x5E14FC31, 0xA2425FF7,   -99, 1 ), // 10^-30  (rounded up)
                new BigNumber( 0x80EACF95, 0x4B43FCF4, 0x81CEB32C,  -102, 1 ), // 10^-31  (rounded up)
                new BigNumber( 0x67DE18EE, 0x453994BA, 0xCFB11EAD,  -106, 1 ), // 10^-32  (rounded up)
                new BigNumber( 0x3F2398D7, 0xA539E9A5, 0xA87FEA27,  -212, 1 ), // 10^-64
                new BigNumber( 0x11DBCB02, 0xFD75539B, 0x88B402F7,  -318, 1 ), // 10^-96
                new BigNumber( 0xAC7CB3F7, 0x64BCE4A0, 0xDDD0467C,  -425, 1 ), // 10^-128 (rounded up)
                new BigNumber( 0x59ED2167, 0xDB73A093, 0xB3F4E093,  -531, 1 ), // 10^-160
                new BigNumber( 0x7B6306A3, 0x5423CC06, 0x91FF8377,  -637, 1 ), // 10^-192
                new BigNumber( 0xA4F8BF56, 0x4A314EBD, 0xECE53CEC,  -744, 1 ), // 10^-224
                new BigNumber( 0xFA911156, 0x637A1939, 0xC0314325,  -850, 1 ), // 10^-256 (rounded up)
                new BigNumber( 0x4EE367F9, 0x836AC577, 0x9BECCE62,  -956, 1 ), // 10^-288
                new BigNumber( 0x8920B099, 0x478238D0, 0xFD00B897, -1063, 1 ), // 10^-320 (rounded up)
                new BigNumber( 0x0092757C, 0x46F34F7D, 0xCD42A113, -1169, 1 ), // 10^-352 (rounded up)
                new BigNumber( 0x88DBA000, 0xB11B0857, 0xA686E3E8, -1275, 1 ), // 10^-384 (rounded up)
                new BigNumber( 0x1A4EB007, 0x3FFC68A6, 0x871A4981, -1381, 1 ), // 10^-416 (rounded up)
                new BigNumber( 0x84C663CF, 0xB6074244, 0xDB377599, -1488, 1 ), // 10^-448 (rounded up)
                new BigNumber( 0x61EB52E2, 0x79007736, 0xB1D983B4, -1594, 1 ), // 10^-480
            };
            #endregion

#if false
            /***************************************************************************
                This is JScript code to compute the BigNumber values in the tables above.
            ***************************************************************************/
            var cbits = 100;
            var cbitsExact = 96;
            var arrPos = new Array;
            var arrNeg = new Array;

            function main()
            {
                Compute(0, 31);
                Compute(5, 15);

                print();
                Dump()
            }

            function Dump()
            {
                var i;
                for (i = 0; i < arrPos.length; i++)
                    print(arrPos[i]);
                print();
                for (i = 0; i < arrNeg.length; i++)
                    print(arrNeg[i]);
            }

            function Compute(p, n)
            {
                var t;
                var i;
                var exp = 1;
                var str = '101';

                for (i = 0; i < p; i++)
                    {
                    exp *= 2;
                    str = Mul(str, str);
                    }

                PrintNum(str, exp);
                PrintInv(str, exp);

                t = str;
                for (i = 2; i <= n; i++)
                    {
                    t = Mul(str, t);
                    PrintNum(t, i * exp);
                    PrintInv(t, i * exp);
                    }
            }

            function Mul(a, b)
            {
                var c;
                var len;
                var i;
                var res;
                var add;

                if (a.length > b.length)
                    {
                    c = a;
                    a = b;
                    b = c;
                    }

                res = '';
                add = b;
                len = a.length;
                for (i = 1; i <= len; i++)
                    {
                    if (a.charAt(len - i) == '1')
                        res = Add(res, add);
                    add += '0';
                    }

                return res;
            }

            function Add(a, b)
            {
                var bit;
                var i;
                var c = 0;
                var res = '';
                var lena = a.length;
                var lenb = b.length;
                var lenm = Math.max(lena, lenb);

                for (i = 1; i <= lenm; i++)
                    {
                    bit = (a.charAt(lena - i) == '1') + (b.charAt(lenb - i) == '1') + c;
                    if (bit & 1)
                        res = '1' + res;
                    else
                        res = '0' + res;
                    c = bit >> 1;
                    }
                if (c)
                    res = '1' + res;

                return res;
            }

            function PrintNum(a, n)
            {
                arrPos[arrPos.length] = PrintHex(a, a.length, n);
            }

            function PrintHex(a, e, n)
            {
                var arr;
                var i;
                var dig;
                var fRoundUp = false;
                var res = '';

                dig = 0;
                for (i = 0; i < cbitsExact; )
                    {
                    dig *= 2;
                    if (a.charAt(i) == '1')
                        dig++;
                    if (0 == (++i & 0x1F) && i < cbitsExact)
                        {
                        strT = dig.toString(16).toUpperCase();
                        res += ' 0x' + '00000000'.substring(strT.length) + strT;
                        dig = 0;
                        }
                    }

                if (a.charAt(cbitsExact) == '1')
                    {
                    // Round up. Don't have to worry about overflowing.
                    fRoundUp = true;
                    dig++;
                    }
                strT = dig.toString(16).toUpperCase();
                res += ' 0x' + '00000000'.substring(strT.length) + strT;

                arr = SR.split(' ');
                res  = '\t{ ' + arr[3];
                res += ', ' + arr[2];
                res += ', ' + arr[1];
                strT = '' + (e + n);
                res += ', ' + '     '.substring(strT.length) + strT;
                res += ', ' + (a.length <= cbitsExact ? 0 : 1);
                strT = '' + n;
                res += ' ), // 10^' + strT;
                if (fRoundUp)
                    res +=  '     '.substring(strT.length) + '(rounded up)';
                print(res);

                return res;
            }

            function PrintInv(a, n)
            {
                var exp;
                var cdig;
                var len = a.length;
                var div = '1';
                var res = '';

                for (exp = 0; div.length <= len; exp++)
                    div += '0';

                for (cdig = 0; cdig < cbits; cdig++)
                    {
                    if (div.length > len || div.length == len && div >= a)
                        {
                        res += '1';
                        div = Sub(div, a);
                        }
                    else
                        res += '0';
                    div += '0';
                    }

                arrNeg[arrNeg.length] = PrintHex(res, -exp + 1, -n);
            }

            function Sub(a, b)
            {
                var ad, bd;
                var i;
                var dig;
                var cch;
                var lena = a.length;
                var lenb = b.length;
                var c = false;
                var res = '';

                for (i = 1; i <= lena; i++)
                    {
                    ad = (a.charAt(lena - i) == '1');
                    bd = (b.charAt(lenb - i) == '1');
                    if (ad == bd)
                        dig = c;
                    else
                        {
                        dig = !c;
                        c = bd;
                        }
                    if (dig)
                        {
                        cch = i;
                        res = '1' + res;
                        }
                    else
                        res = '0' + res;
                    }
                return SR.substring(SR.length - cch, SR.length);
            }
#endif
        }

        /**
        * Implementation of very large variable-precision non-negative integers.
        *
        * Hungarian: bi
        *
        */
        private sealed class BigInteger : IComparable
        {
            // Make this big enough that we rarely have to reallocate.
            private const int InitCapacity = 30;

            private int _capacity;
            private int _length;
            private uint[] _digits;

            public BigInteger()
            {
                _capacity = InitCapacity;
                _length = 0;
                _digits = new uint[InitCapacity];
                AssertValid();
            }

            public int Length
            {
                get { return _length; }
            }

            public uint this[int idx]
            {
                get
                {
                    AssertValid();
                    Debug.Assert(0 <= idx && idx < _length);
                    return _digits[idx];
                }
            }

            [Conditional("DEBUG")]
            private void AssertValidNoVal()
            {
                Debug.Assert(_capacity >= InitCapacity);
                Debug.Assert(_length >= 0 && _length <= _capacity);
            }

            [Conditional("DEBUG")]
            private void AssertValid()
            {
                AssertValidNoVal();
                Debug.Assert(0 == _length || 0 != _digits[_length - 1]);
            }

            private void Ensure(int cu)
            {
                AssertValidNoVal();

                if (cu <= _capacity)
                {
                    return;
                }

                cu += cu;
                uint[] newDigits = new uint[cu];
                _digits.CopyTo(newDigits, 0);
                _digits = newDigits;
                _capacity = cu;

                AssertValidNoVal();
            }

            /*  ----------------------------------------------------------------------------
                InitFromRgu()

                Initialize this big integer from an array of uint values.
            */
            public void InitFromRgu(uint[] rgu, int cu)
            {
                AssertValid();
                Debug.Assert(cu >= 0);

                Ensure(cu);
                _length = cu;
                // REVIEW: if (cu > 0) memcpy(digits, prgu, cu * sizeof(uint));
                for (int i = 0; i < cu; i++)
                {
                    _digits[i] = rgu[i];
                }
                AssertValid();
            }

            /*  ----------------------------------------------------------------------------
                InitFromRgu()

                Initialize this big integer from 0, 1, or 2 uint values.
            */
            public void InitFromDigits(uint u0, uint u1, int cu)
            {
                AssertValid();
                Debug.Assert(2 <= _capacity);

                _length = cu;
                _digits[0] = u0;
                _digits[1] = u1;
                AssertValid();
            }

            /*  ----------------------------------------------------------------------------
                InitFromBigint()

                Initialize this big integer from another BigInteger object.
            */
            public void InitFromBigint(BigInteger biSrc)
            {
                AssertValid();
                biSrc.AssertValid();
                Debug.Assert((object)this != (object)biSrc);

                InitFromRgu(biSrc._digits, biSrc._length);
            }

#if !NOPARSE || DEBUG
            /*  ----------------------------------------------------------------------------
                InitFromFloatingDecimal()

                Initialize this big integer from a FloatingDecimal object.
            */
            public void InitFromFloatingDecimal(FloatingDecimal dec)
            {
                AssertValid();
                Debug.Assert(dec.MantissaSize >= 0);

                uint uAdd, uMul;
                int cu = (dec.MantissaSize + 8) / 9;
                int mantissaSize = dec.MantissaSize;

                Ensure(cu);
                _length = 0;

                uAdd = 0;
                uMul = 1;
                for (int ib = 0; ib < mantissaSize; ib++)
                {
                    Debug.Assert(dec[ib] >= 0 && dec[ib] <= 9);
                    if (1000000000 == uMul)
                    {
                        MulAdd(uMul, uAdd);
                        uMul = 1;
                        uAdd = 0;
                    }
                    uMul *= 10;
                    uAdd = uAdd * 10 + dec[ib];
                }
                Debug.Assert(1 < uMul);
                MulAdd(uMul, uAdd);

                AssertValid();
            }
#endif

            public void MulAdd(uint uMul, uint uAdd)
            {
                AssertValid();
                Debug.Assert(0 != uMul);

                for (int i = 0; i < _length; i++)
                {
                    uint d, uT;
                    d = MulU(_digits[i], uMul, out uT);
                    if (0 != uAdd)
                    {
                        uT += AddU(ref d, uAdd);
                    }
                    _digits[i] = d;
                    uAdd = uT;
                }
                if (0 != uAdd)
                {
                    Ensure(_length + 1);
                    _digits[_length++] = uAdd;
                }
                AssertValid();
            }

            public void MulPow5(int c5)
            {
                AssertValid();
                Debug.Assert(c5 >= 0);

                const uint C5to13 = 1220703125;
                int cu = (c5 + 12) / 13;

                if (0 == _length || 0 == c5)
                {
                    return;
                }

                Ensure(_length + cu);

                for (; c5 >= 13; c5 -= 13)
                {
                    MulAdd(C5to13, 0);
                }

                if (c5 > 0)
                {
                    uint uT;
                    for (uT = 5; --c5 > 0;)
                    {
                        uT *= 5;
                    }
                    MulAdd(uT, 0);
                }
                AssertValid();
            }

            public void ShiftLeft(int cbit)
            {
                AssertValid();
                Debug.Assert(cbit >= 0);

                int idx, cu;
                uint uExtra;

                if (0 == cbit || 0 == _length)
                {
                    return;
                }

                cu = cbit >> 5;
                cbit &= 0x001F;

                if (cbit > 0)
                {
                    idx = _length - 1;
                    uExtra = _digits[idx] >> (32 - cbit);

                    for (; ; idx--)
                    {
                        _digits[idx] <<= cbit;
                        if (0 == idx)
                        {
                            break;
                        }
                        _digits[idx] |= _digits[idx - 1] >> (32 - cbit);
                    }
                }
                else
                {
                    uExtra = 0;
                }

                if (cu > 0 || 0 != uExtra)
                {
                    // Make sure there's enough room.
                    idx = _length + (0 != uExtra ? 1 : 0) + cu;
                    Ensure(idx);

                    if (cu > 0)
                    {
                        // Shift the digits.
                        // REVIEW: memmove(digits + cu, digits, length * sizeof(uint));
                        for (int i = _length; 0 != i--;)
                        {
                            _digits[cu + i] = _digits[i];
                        }
                        // REVIEW: memset(digits, 0, cu * sizeof(uint));
                        for (int i = 0; i < cu; i++)
                        {
                            _digits[i] = 0;
                        }
                        _length += cu;
                    }

                    // Throw on the extra one.
                    if (0 != uExtra)
                    {
                        _digits[_length++] = uExtra;
                    }
                }
                AssertValid();
            }

            public void ShiftUsRight(int cu)
            {
                AssertValid();
                Debug.Assert(cu >= 0);

                if (cu >= _length)
                {
                    _length = 0;
                }
                else if (cu > 0)
                {
                    // REVIEW: memmove(digits, digits + cu, (length - cu) * sizeof(uint));
                    for (int i = 0; i < _length - cu; i++)
                    {
                        _digits[i] = _digits[cu + i];
                    }
                    _length -= cu;
                }
                AssertValid();
            }

            public void ShiftRight(int cbit)
            {
                AssertValid();
                Debug.Assert(cbit >= 0);

                int idx;
                int cu = cbit >> 5;
                cbit &= 0x001F;

                if (cu > 0)
                {
                    ShiftUsRight(cu);
                }

                if (0 == cbit || 0 == _length)
                {
                    AssertValid();
                    return;
                }

                for (idx = 0; ;)
                {
                    _digits[idx] >>= cbit;
                    if (++idx >= _length)
                    {
                        // Last one.
                        if (0 == _digits[idx - 1])
                        {
                            _length--;
                        }
                        break;
                    }
                    _digits[idx - 1] |= _digits[idx] << (32 - cbit);
                }
                AssertValid();
            }

            public int CompareTo(object? obj)
            {
                Debug.Assert(obj != null);
                BigInteger bi = (BigInteger)obj;
                AssertValid();
                bi.AssertValid();

                if (_length > bi._length)
                {
                    return 1;
                }
                else if (_length < bi._length)
                {
                    return -1;
                }
                else if (0 == _length)
                {
                    return 0;
                }

                int idx;

                for (idx = _length - 1; _digits[idx] == bi._digits[idx]; idx--)
                {
                    if (0 == idx)
                    {
                        return 0;
                    }
                }
                Debug.Assert(idx >= 0 && idx < _length);
                Debug.Assert(_digits[idx] != bi._digits[idx]);

                return (_digits[idx] > bi._digits[idx]) ? 1 : -1;
            }

            public void Add(BigInteger bi)
            {
                AssertValid();
                bi.AssertValid();
                Debug.Assert((object)this != (object)bi);

                int idx, cuMax, cuMin;
                uint wCarry;

                if ((cuMax = _length) < (cuMin = bi._length))
                {
                    cuMax = bi._length;
                    cuMin = _length;
                    Ensure(cuMax + 1);
                }

                wCarry = 0;
                for (idx = 0; idx < cuMin; idx++)
                {
                    if (0 != wCarry)
                    {
                        wCarry = AddU(ref _digits[idx], wCarry);
                    }
                    wCarry += AddU(ref _digits[idx], bi._digits[idx]);
                }

                if (_length < bi._length)
                {
                    for (; idx < cuMax; idx++)
                    {
                        _digits[idx] = bi._digits[idx];
                        if (0 != wCarry)
                        {
                            wCarry = AddU(ref _digits[idx], wCarry);
                        }
                    }
                    _length = cuMax;
                }
                else
                {
                    for (; 0 != wCarry && idx < cuMax; idx++)
                    {
                        wCarry = AddU(ref _digits[idx], wCarry);
                    }
                }

                if (0 != wCarry)
                {
                    Ensure(_length + 1);
                    _digits[_length++] = wCarry;
                }
                AssertValid();
            }

            public void Subtract(BigInteger bi)
            {
                AssertValid();
                bi.AssertValid();
                Debug.Assert((object)this != (object)bi);

                int idx;
                uint wCarry, uT;

                if (_length < bi._length)
                {
                    goto LNegative;
                }

                wCarry = 1;
                for (idx = 0; idx < bi._length; idx++)
                {
                    Debug.Assert(0 == wCarry || 1 == wCarry);
                    uT = bi._digits[idx];

                    // NOTE: We should really do:
                    //    wCarry = AddU(ref digits[idx], wCarry);
                    //    wCarry += AddU(ref digits[idx], ~uT);
                    // The only case where this is different than
                    //    wCarry = AddU(ref digits[idx], ~uT + wCarry);
                    // is when 0 == uT and 1 == wCarry, in which case we don't
                    // need to add anything and wCarry should still be 1, so we can
                    // just skip the operations.

                    if (0 != uT || 0 == wCarry)
                    {
                        wCarry = AddU(ref _digits[idx], ~uT + wCarry);
                    }
                }
                while (0 == wCarry && idx < _length)
                {
                    wCarry = AddU(ref _digits[idx], 0xFFFFFFFF);
                }

                if (0 != wCarry)
                {
                    if (idx == _length)
                    {
                        // Trim off zeros.
                        while (--idx >= 0 && 0 == _digits[idx])
                        {
                        }
                        _length = idx + 1;
                    }
                    AssertValid();
                    return;
                }

            LNegative:
                // bi was bigger than this.
                Debug.Fail("Who's subtracting to negative?");
                _length = 0;
                AssertValid();
            }

            public uint DivRem(BigInteger bi)
            {
                AssertValid();
                bi.AssertValid();
                Debug.Assert((object)this != (object)bi);

                int idx, cu;
                uint uQuo, wCarry;
                int wT;
                uint uT, uHi, uLo;

                cu = bi._length;
                Debug.Assert(_length <= cu);
                if (_length < cu)
                {
                    return 0;
                }

                // Get a lower bound on the quotient.
                uQuo = (uint)(_digits[cu - 1] / (bi._digits[cu - 1] + 1));
                Debug.Assert(uQuo >= 0 && uQuo <= 9);

                // Handle 0 and 1 as special cases.
                switch (uQuo)
                {
                    case 0:
                        break;
                    case 1:
                        Subtract(bi);
                        break;
                    default:
                        uHi = 0;
                        wCarry = 1;
                        for (idx = 0; idx < cu; idx++)
                        {
                            Debug.Assert(0 == wCarry || 1 == wCarry);

                            // Compute the product.
                            uLo = MulU(uQuo, bi._digits[idx], out uT);
                            uHi = uT + AddU(ref uLo, uHi);

                            // Subtract the product. See note in BigInteger.Subtract.
                            if (0 != uLo || 0 == wCarry)
                            {
                                wCarry = AddU(ref _digits[idx], ~uLo + wCarry);
                            }
                        }
                        Debug.Assert(1 == wCarry);
                        Debug.Assert(idx == cu);

                        // Trim off zeros.
                        while (--idx >= 0 && 0 == _digits[idx])
                        {
                        }
                        _length = idx + 1;
                        break;
                }

                if (uQuo < 9 && (wT = CompareTo(bi)) >= 0)
                {
                    // Quotient was off too small (by one).
                    uQuo++;
                    if (0 == wT)
                    {
                        _length = 0;
                    }
                    else
                    {
                        Subtract(bi);
                    }
                }
                Debug.Assert(CompareTo(bi) < 0);

                return uQuo;
            }

#if NEVER
            public static explicit operator double(BigInteger bi) {
                uint uHi, uLo;
                uint u1, u2, u3;
                int idx;
                int cbit;
                uint dblHi, dblLo;

                switch (bi.length) {
                case 0:
                    return 0;
                case 1:
                    return bi.digits[0];
                case 2:
                    return (ulong)bi.digits[1] << 32 | bi.digits[0];
                }

                Debug.Assert(3 <= bi.length);
                if (bi.length > 32) {
                    // Result is infinite.
                    return BitConverter.Int64BitsToDouble(0x7FF00000L << 32);
                }

                u1 = bi.digits[bi.length - 1];
                u2 = bi.digits[bi.length - 2];
                u3 = bi.digits[bi.length - 3];
                Debug.Assert(0 != u1);
                cbit = 31 - CbitZeroLeft(u1);

                if (0 == cbit) {
                    uHi = u2;
                    uLo = u3;
                } else {
                    uHi = (u1 << (32 - cbit)) | (u2 >> cbit);
                    // Or 1 if there are any remaining nonzero bits in u3, so we take
                    // them into account when rounding.
                    uLo = (u2 << (32 - cbit)) | (u3 >> cbit) | NotZero(u3 << (32 - cbit));
                }

                // Set the mantissa bits.
                dblHi = uHi >> 12;
                dblLo = (uHi << 20) | (uLo >> 12);

                // Set the exponent field.
                dblHi |= (uint)(0x03FF + cbit + (bi.length - 1) * 0x0020) << 20;

                // Do IEEE rounding.
                if (0 != (uLo & 0x0800)) {
                    if (0 != (uLo & 0x07FF) || 0 != (dblLo & 1)) {
                        if (0 == ++dblLo) {
                            ++dblHi;
                        }
                    } else {
                        // If there are any non-zero bits in digits from 0 to length - 4,
                        // round up.
                        for (idx = bi.length - 4; idx >= 0; idx--) {
                            if (0 != bi.digits[idx]) {
                                if (0 == ++dblLo) {
                                    ++dblHi;
                                }
                                break;
                            }
                        }
                    }
                }
                return BitConverter.UInt64BitsToDouble((ulong)dblHi << 32 | dblLo);
            }
#endif
        };

        /**
        * Floating point number represented in base-10.
        */
        private sealed class FloatingDecimal
        {
            public const int MaxDigits = 50;
            private const int MaxExp10 = 310;  // Upper bound on base 10 exponent
            private const int MinExp10 = -325;  // Lower bound on base 10 exponent

            private int _exponent;             // Base-10 scaling factor (0 means decimal point immediately precedes first digit)
            private int _sign;                 // Sign is -1 or 1, depending on sign of number
            private int _mantissaSize;         // Size of mantissa
            private readonly byte[] _mantissa = new byte[MaxDigits];    // Array of base-10 digits

            public int Exponent { get { return _exponent; } set { _exponent = value; } }
            public int Sign { get { return _sign; } set { _sign = value; } }
            public byte[] Mantissa { get { return _mantissa; } }

            public int MantissaSize
            {
                get
                {
                    return _mantissaSize;
                }
                set
                {
                    Debug.Assert(value <= MaxDigits);
                    _mantissaSize = value;
                }
            }

            public byte this[int ib]
            {
                get
                {
                    Debug.Assert(0 <= ib && ib < _mantissaSize);
                    return _mantissa[ib];
                }
            }

            public FloatingDecimal()
            {
                _exponent = 0;
                _sign = 1;
                _mantissaSize = 0;
            }

            public FloatingDecimal(double dbl)
            {
                InitFromDouble(dbl);

#if DEBUG
                if (0 != _mantissaSize)
                {
                    Debug.Assert(dbl == (double)this);

                    FloatingDecimal decAfter = new FloatingDecimal();
                    decAfter.InitFromDouble(Succ(dbl));
                    // Assert(memcmp(this, &decAfter, sizeof(*this) - MaxDigits + mantissaSize));
                    Debug.Assert(!this.Equals(decAfter));

                    FloatingDecimal decBefore = new FloatingDecimal();
                    decBefore.InitFromDouble(Pred(dbl));
                    // Assert(memcmp(this, &decBefore, sizeof(*this) - MaxDigits + mantissaSize));
                    Debug.Assert(!this.Equals(decBefore));
                }
#endif
            }

#if DEBUG
            private bool Equals(FloatingDecimal other)
            {
                if (_exponent != other._exponent || _sign != other._sign || _mantissaSize != other._mantissaSize)
                {
                    return false;
                }
                for (int idx = 0; idx < _mantissaSize; idx++)
                {
                    if (_mantissa[idx] != other._mantissa[idx])
                    {
                        return false;
                    }
                }
                return true;
            }
#endif

#if NEVER
            /*  ----------------------------------------------------------------------------
                RoundTo()

                Rounds off the BCD representation of a number to a specified number of digits.
                This may result in the exponent being incremented (e.g. if digits were 999).
            */
            public void RoundTo(int sizeMantissa) {
                if (sizeMantissa >= mantissaSize) {
                    // No change required
                    return;
                }

                if (sizeMantissa >= 0) {
                    bool fRoundUp = mantissa[sizeMantissa] >= 5;
                    mantissaSize = sizeMantissa;

                    // Round up if necessary and trim trailing zeros
                    for (int idx = mantissaSize - 1; idx >= 0; idx--) {
                        if (fRoundUp) {
                            if (++(mantissa[idx]) <= 9) {
                                // Trailing digit is non-zero, so break
                                fRoundUp = false;
                                break;
                            }
                        } else if (mantissa[idx] > 0) {
                            // Trailing digit is non-zero, so break
                            break;
                        }

                        // Trim trailing zeros
                        mantissaSize--;
                    }

                    if (fRoundUp) {
                        // Number consisted only of 9's
                        Debug.Assert(0 == mantissaSize);
                        mantissa[0] = 1;
                        mantissaSize = 1;
                        exponent++;
                    }
                } else {
                    // Number was rounded past any significant digits (e.g. 0.001 rounded to 1 fractional place), so round to 0.0
                    mantissaSize = 0;
                }

                if (0 == mantissaSize) {
                    // 0.0
                    sign = 1;
                    exponent = 0;
                }
            }
#endif

#if !NOPARSE || DEBUG
            /*  ----------------------------------------------------------------------------
                explicit operator double()

                Returns the double value of this floating decimal.
            */
            public static explicit operator double(FloatingDecimal dec)
            {
                BigNumber num, numHi, numLo;
                uint ul;
                int scale;
                double dbl, dblLowPrec, dblLo;
                int mantissaSize = dec._mantissaSize;

                // Verify that there are no leading or trailing zeros in the mantissa
                Debug.Assert(0 != mantissaSize && 0 != dec[0] && 0 != dec[mantissaSize - 1]);

                // See if we can just use IEEE double arithmetic.
                scale = dec._exponent - mantissaSize;
                if (mantissaSize <= 15 && scale >= -22 && dec._exponent <= 37)
                {
                    // These calculations are all exact since mantissaSize <= 15.
                    if (mantissaSize <= 9)
                    {
                        // Can use the ALU to perform fast integer arithmetic
                        ul = 0;
                        for (int ib = 0; ib < mantissaSize; ib++)
                        {
                            Debug.Assert(dec[ib] >= 0 && dec[ib] <= 9);
                            ul = ul * 10 + dec[ib];
                        }
                        dbl = ul;
                    }
                    else
                    {
                        // Use floating point arithmetic
                        dbl = 0.0;
                        for (int ib = 0; ib < mantissaSize; ib++)
                        {
                            Debug.Assert(dec[ib] >= 0 && dec[ib] <= 9);
                            dbl = dbl * 10.0 + dec[ib];
                        }
                    }

                    // This is the only (potential) rounding operation and we assume
                    // the compiler does the correct IEEE rounding.
                    if (scale > 0)
                    {
                        // Need to scale upwards by powers of 10
                        if (scale > 22)
                        {
                            // This one is exact. We're using the fact that mantissaSize < 15
                            // to handle exponents bigger than 22.
                            dbl *= C10toN[scale - 22];
                            dbl *= C10toN[22];
                        }
                        else
                        {
                            dbl *= C10toN[scale];
                        }
                    }
                    else if (scale < 0)
                    {
                        // Scale number by negative power of 10
                        dbl /= C10toN[-scale];
                    }

#if DEBUG
                    // In the debug version, execute the high precision code also and
                    // verify that the results are the same.
                    dblLowPrec = dbl;
                }
                else
                {
                    dblLowPrec = double.NaN;
                }
#else
                    goto LDone;
                }
#endif

                if (dec._exponent >= MaxExp10)
                {
                    // Overflow to infinity.
                    dbl = double.PositiveInfinity;
                    goto LDone;
                }

                if (dec._exponent <= MinExp10)
                {
                    // Underflow to 0.
                    dbl = 0.0;
                    goto LDone;
                }

                // Convert to a big number.
                num = new BigNumber(dec);

                // If there is no error in the big number, just convert it to a double.
                if (0 == num.Error)
                {
                    dbl = (double)num;
#if DEBUG
                    dblLo = dec.AdjustDbl(dbl);
                    Debug.Assert(dbl == dblLo);
#endif
                    goto LDone;
                }

                // The big number has error in it, so see if the error matters.
                // Get the upper bound and lower bound. If they convert to the same
                // double we're done.
                numHi = num;
                numHi.MakeUpperBound();
                numLo = num;
                numLo.MakeLowerBound();

                dbl = (double)numHi;
                dblLo = (double)numLo;
                if (dbl == dblLo)
                {
#if DEBUG
                    Debug.Assert(dbl == (double)num);
                    dblLo = dec.AdjustDbl(dbl);
                    Debug.Assert(dbl == dblLo || double.IsNaN(dblLo));
#endif
                    goto LDone;
                }

                // Need to use big integer arithmetic. There's too much error in
                // our result and it's close to a boundary value. This is rare,
                // but does happen. Eg,
                // x = 1.2345678901234568347913049445e+200;
                //
                dbl = dec.AdjustDbl((double)num);

            LDone:
                // This assert was removed because it would fire on VERY rare occasions. Not
                // repro on all machines and very hard to repro even on machines that could repro it.
                // The numbers (dblLowPrec and dbl) were different in their two least sig bits only
                // which is _probably_ within expected errror. I did not take the time to fully
                // investigate whether this really does meet the ECMA spec...
                //
                Debug.Assert(double.IsNaN(dblLowPrec) || dblLowPrec == dbl);
                return dec._sign < 0 ? -dbl : dbl;
            }

            /*  ----------------------------------------------------------------------------
                AdjustDbl()

                The double contains a binary value, M * 2^n, which is off by at most 1
                in the least significant bit; this class' members represent a decimal
                value, D * 10^e.

                The general scheme is to find an integer N (the smaller the better) such
                that N * M * 2^n and N * D * 10^e are both integers. We then compare
                N * M * 2^n to N * D * 10^e (at full precision). If the binary value is
                greater, we adjust it to be exactly half way to the next value that can
                come from a double. We then compare again to decided whether to bump the
                double up to the next value. Similary if the binary value is smaller,
                we adjust it to be exactly half way to the previous representable value
                and recompare.
            */
            private double AdjustDbl(double dbl)
            {
                BigInteger biDec = new BigInteger();
                BigInteger biDbl = new BigInteger();
                int c2Dec, c2Dbl;
                int c5Dec, c5Dbl;
                uint wAddHi, uT;
                int wT, iT;
                int lwExp, wExp2;
                //uint *rgu = stackalloc uint[2];
                uint rgu0, rgu1;
                int cu;

                biDec.InitFromFloatingDecimal(this);
                lwExp = _exponent - _mantissaSize;

                // lwExp is a base 10 exponent.
                if (lwExp >= 0)
                {
                    c5Dec = c2Dec = lwExp;
                    c5Dbl = c2Dbl = 0;
                }
                else
                {
                    c5Dec = c2Dec = 0;
                    c5Dbl = c2Dbl = -lwExp;
                }

                rgu1 = DblHi(dbl);
                rgu0 = DblLo(dbl);
                wExp2 = (int)(rgu1 >> 20) & 0x07FF;
                rgu1 &= 0x000FFFFF;
                wAddHi = 1;
                if (0 != wExp2)
                {
                    // Normal, so add implicit bit.
                    if (0 == rgu1 && 0 == rgu0 && 1 != wExp2)
                    {
                        // Power of 2 (and not adjacent to the first denormal), so the
                        // adjacent low value is closer than the high value.
                        wAddHi = 2;
                        rgu1 = 0x00200000;
                        wExp2--;
                    }
                    else
                    {
                        rgu1 |= 0x00100000;
                    }
                    wExp2 -= 1076;
                }
                else
                {
                    wExp2 = -1075;
                }

                // Shift left by 1 bit : the adjustment values need the next lower bit.
                rgu1 = (rgu1 << 1) | (rgu0 >> 31);
                rgu0 <<= 1;

                // We must determine how many words of significant digits this requiSR.
                if (0 == rgu0 && 0 == rgu1)
                {
                    cu = 0;
                }
                else if (0 == rgu1)
                {
                    cu = 1;
                }
                else
                {
                    cu = 2;
                }

                biDbl.InitFromDigits(rgu0, rgu1, cu);

                if (wExp2 >= 0)
                {
                    c2Dbl += wExp2;
                }
                else
                {
                    c2Dec += -wExp2;
                }

                // Eliminate common powers of 2.
                if (c2Dbl > c2Dec)
                {
                    c2Dbl -= c2Dec;
                    c2Dec = 0;

                    // See if biDec has some powers of 2 that we can get rid of.
                    for (iT = 0; c2Dbl >= 32 && 0 == biDec[iT]; iT++)
                    {
                        c2Dbl -= 32;
                    }
                    if (iT > 0)
                    {
                        biDec.ShiftUsRight(iT);
                    }
                    Debug.Assert(c2Dbl < 32 || 0 != biDec[0]);
                    uT = biDec[0];
                    for (iT = 0; iT < c2Dbl && 0 == (uT & (1L << iT)); iT++)
                    {
                    }
                    if (iT > 0)
                    {
                        c2Dbl -= iT;
                        biDec.ShiftRight(iT);
                    }
                }
                else
                {
                    c2Dec -= c2Dbl;
                    c2Dbl = 0;
                }

                // There are no common powers of 2 or common powers of 5.
                Debug.Assert(0 == c2Dbl || 0 == c2Dec);
                Debug.Assert(0 == c5Dbl || 0 == c5Dec);

                // Fold in the powers of 5.
                if (c5Dbl > 0)
                {
                    biDbl.MulPow5(c5Dbl);
                }
                else if (c5Dec > 0)
                {
                    biDec.MulPow5(c5Dec);
                }

                // Fold in the powers of 2.
                if (c2Dbl > 0)
                {
                    biDbl.ShiftLeft(c2Dbl);
                }
                else if (c2Dec > 0)
                {
                    biDec.ShiftLeft(c2Dec);
                }

                // Now determine whether biDbl is above or below biDec.
                wT = biDbl.CompareTo(biDec);

                if (0 == wT)
                {
                    return dbl;
                }
                else if (wT > 0)
                {
                    // biDbl is greater. Recompute with the dbl minus half the distance
                    // to the next smaller double.
                    if (0 == AddU(ref rgu0, 0xFFFFFFFF))
                    {
                        AddU(ref rgu1, 0xFFFFFFFF);
                    }
                    biDbl.InitFromDigits(rgu0, rgu1, 1 + (0 != rgu1 ? 1 : 0));
                    if (c5Dbl > 0)
                    {
                        biDbl.MulPow5(c5Dbl);
                    }
                    if (c2Dbl > 0)
                    {
                        biDbl.ShiftLeft(c2Dbl);
                    }

                    wT = biDbl.CompareTo(biDec);
                    if (wT > 0 || 0 == wT && 0 != (DblLo(dbl) & 1))
                    {
                        // Return the next lower value.
                        dbl = BitConverter.Int64BitsToDouble(BitConverter.DoubleToInt64Bits(dbl) - 1);
                    }
                }
                else
                {
                    // biDbl is smaller. Recompute with the dbl plus half the distance
                    // to the next larger double.
                    if (0 != AddU(ref rgu0, wAddHi))
                    {
                        AddU(ref rgu1, 1);
                    }
                    biDbl.InitFromDigits(rgu0, rgu1, 1 + (0 != rgu1 ? 1 : 0));
                    if (c5Dbl > 0)
                    {
                        biDbl.MulPow5(c5Dbl);
                    }
                    if (c2Dbl > 0)
                    {
                        biDbl.ShiftLeft(c2Dbl);
                    }

                    wT = biDbl.CompareTo(biDec);
                    if (wT < 0 || 0 == wT && 0 != (DblLo(dbl) & 1))
                    {
                        // Return the next higher value.
                        dbl = BitConverter.Int64BitsToDouble(BitConverter.DoubleToInt64Bits(dbl) + 1);
                    }
                }
                return dbl;
            }
#endif

            private void InitFromDouble(double dbl)
            {
                if (0.0 == dbl || IsSpecial(dbl))
                {
                    _exponent = 0;
                    _sign = 1;
                    _mantissaSize = 0;
                }
                else
                {
                    // Handle the sign.
                    if (dbl < 0)
                    {
                        _sign = -1;
                        dbl = -dbl;
                    }
                    else
                    {
                        _sign = 1;
                    }

                    if (!BigNumber.DblToRgbFast(dbl, _mantissa, out _exponent, out _mantissaSize))
                    {
                        BigNumber.DblToRgbPrecise(dbl, _mantissa, out _exponent, out _mantissaSize);
                    }
                }
            }
        };

        /*  ----------------------------------------------------------------------------
            DoubleToString()

            Converts a floating point number to a string according to XPath rules.
        */
        public static string DoubleToString(double dbl)
        {
            Debug.Assert(('0' & 0xF) == 0, "We use (char)(d |'0') to convert digit to char");
            int maxSize, sizeInt, sizeFract, cntDigits, ib;
            int iVal;

            if (IsInteger(dbl, out iVal))
            {
                return iVal.ToString(CultureInfo.InvariantCulture);
            }

            // Handle NaN and infinity
            if (IsSpecial(dbl))
            {
                if (double.IsNaN(dbl))
                {
                    return "NaN";
                }

                Debug.Assert(double.IsInfinity(dbl));
                return dbl < 0 ? "-Infinity" : "Infinity";
            }

            // Get decimal digits
            FloatingDecimal dec = new FloatingDecimal(dbl);
            Debug.Assert(0 != dec.MantissaSize);

            // If exponent is negative, size of fraction increases
            sizeFract = dec.MantissaSize - dec.Exponent;

            if (sizeFract > 0)
            {
                // Decimal consists of a fraction part + possible integer part
                sizeInt = (dec.Exponent > 0) ? dec.Exponent : 0;
            }
            else
            {
                // Decimal consists of just an integer part
                sizeInt = dec.Exponent;
                sizeFract = 0;
            }

            // Sign + integer + fraction + decimal point + leading zero + terminating null
            maxSize = sizeInt + sizeFract + 4;

            unsafe
            {
                // Allocate output memory
                char* pBuf = stackalloc char[maxSize];
                char* pch = pBuf;

                if (dec.Sign < 0)
                {
                    *pch++ = '-';
                }

                cntDigits = dec.MantissaSize;
                ib = 0;

                if (0 != sizeInt)
                {
                    do
                    {
                        if (0 != cntDigits)
                        {
                            // Still mantissa digits left to consume
                            Debug.Assert(dec[ib] >= 0 && dec[ib] <= 9);
                            *pch++ = (char)(dec[ib++] | '0');
                            cntDigits--;
                        }
                        else
                        {
                            // Add trailing zeros
                            *pch++ = '0';
                        }
                    } while (0 != --sizeInt);
                }
                else
                {
                    *pch++ = '0';
                }

                if (0 != sizeFract)
                {
                    Debug.Assert(0 != cntDigits);
                    Debug.Assert(sizeFract == cntDigits || sizeFract > cntDigits && pch[-1] == '0');
                    *pch++ = '.';

                    while (sizeFract > cntDigits)
                    {
                        // Add leading zeros
                        *pch++ = '0';
                        sizeFract--;
                    }

                    Debug.Assert(sizeFract == cntDigits);
                    while (0 != cntDigits)
                    {
                        // Output remaining mantissa digits
                        Debug.Assert(dec[ib] >= 0 && dec[ib] <= 9);
                        *pch++ = (char)(dec[ib++] | '0');
                        cntDigits--;
                    }
                }

                Debug.Assert(0 == sizeInt && 0 == cntDigits);
                return new string(pBuf, 0, (int)(pch - pBuf));
            }
        }

        private static bool IsAsciiDigit(char ch)
        {
            return unchecked((uint)(ch - '0')) <= 9;
        }

        private static bool IsWhitespace(char ch)
        {
            return ch == '\x20' || ch == '\x9' || ch == '\xA' || ch == '\xD';
        }

        private static unsafe char* SkipWhitespace(char* pch)
        {
            while (IsWhitespace(*pch))
            {
                pch++;
            }
            return pch;
        }

        /*  ----------------------------------------------------------------------------
            StringToDouble()

            Converts a string to a floating point number according to XPath rules.
            NaN is returned if the entire string is not a valid number.

            This code was stolen from MSXML6 DecimalFormat::parse(). The implementation
            depends on the fact that the String objects are always zero-terminated.
        */
        public static unsafe double StringToDouble(string s)
        {
            Debug.Assert(('0' & 0xF) == 0, "We use (ch & 0xF) to convert char to digit");
            // For the mantissa digits. After leaving the state machine, pchFirstDig
            // points to the first digit and pch points just past the last digit.
            // numDig is the number of digits. pch - pchFirstDig may be numDig + 1
            // (if there is a decimal point).

            fixed (char* pchStart = s)
            {
                int numDig = 0;
                char* pch = pchStart;
                char* pchFirstDig = null;
                char ch;

                int sign = 1;       // sign of the mantissa
                int expAdj = 0;     // exponent adjustment

            // Enter the state machine

            // Initialization
            LRestart:
                ch = *pch++;
                if (IsAsciiDigit(ch))
                {
                    goto LGetLeft;
                }

                switch (ch)
                {
                    case '-':
                        if (sign < 0)
                        {
                            break;
                        }
                        sign = -1;
                        goto LRestart;
                    case '.':
                        if (IsAsciiDigit(*pch))
                        {
                            goto LGetRight;
                        }
                        break;
                    default:
                        // MSXML has a bug, we should not allow whitespace after a minus sign
                        if (IsWhitespace(ch) && sign > 0)
                        {
                            pch = SkipWhitespace(pch);
                            goto LRestart;
                        }
                        break;
                }

                // Nothing digested - set the result to NaN and exit.
                return double.NaN;

            LGetLeft:
                // Get digits to the left of the decimal point
                if (ch == '0')
                {
                    do
                    {
                        // Trim leading zeros
                        ch = *pch++;
                    } while (ch == '0');

                    if (!IsAsciiDigit(ch))
                    {
                        goto LSkipNonZeroDigits;
                    }
                }

                Debug.Assert(IsAsciiDigit(ch));
                pchFirstDig = pch - 1;
                do
                {
                    ch = *pch++;
                } while (IsAsciiDigit(ch));
                numDig = (int)(pch - pchFirstDig) - 1;

            LSkipNonZeroDigits:
                if (ch != '.')
                {
                    goto LEnd;
                }

            LGetRight:
                Debug.Assert(ch == '.');
                ch = *pch++;
                if (pchFirstDig == null)
                {
                    // Count fractional leading zeros (e.g. six zeros in '0.0000005')
                    while (ch == '0')
                    {
                        expAdj--;
                        ch = *pch++;
                    }
                    pchFirstDig = pch - 1;
                }

                while (IsAsciiDigit(ch))
                {
                    expAdj--;
                    numDig++;
                    ch = *pch++;
                }

            LEnd:
                pch--;
                char* pchEnd = pchStart + s.Length;
                Debug.Assert(*pchEnd == '\0', "String objects must be zero-terminated");

                if (pch < pchEnd && SkipWhitespace(pch) < pchEnd)
                {
                    // If we're not at the end of the string, this is not a valid number
                    return double.NaN;
                }

                if (numDig == 0)
                {
                    return 0.0;
                }

                Debug.Assert(pchFirstDig != null);

                if (expAdj == 0)
                {
                    // Assert(StrRChrW(pchFirstDig, &pchFirstDig[numDig], '.') == null);

                    // Detect special case where number is an integer
                    if (numDig <= 9)
                    {
                        Debug.Assert(pchFirstDig != pch);
                        int iNum = *pchFirstDig & 0xF; // - '0'
                        while (--numDig != 0)
                        {
                            pchFirstDig++;
                            Debug.Assert(IsAsciiDigit(*pchFirstDig));
                            iNum = iNum * 10 + (*pchFirstDig & 0xF); // - '0'
                        }
                        return (double)(sign < 0 ? -iNum : iNum);
                    }
                }
                else
                {
                    // The number has a fractional part
                    Debug.Assert(expAdj < 0);
                    // Assert(StrRChrW(pchStart, pch, '.') != null);
                }

                // Limit to 50 digits (double is only precise up to 17 or so digits)
                if (numDig > FloatingDecimal.MaxDigits)
                {
                    pch -= (numDig - FloatingDecimal.MaxDigits);
                    expAdj += (numDig - FloatingDecimal.MaxDigits);
                    numDig = FloatingDecimal.MaxDigits;
                }

                // Remove trailing zero's from mantissa
                Debug.Assert(IsAsciiDigit(*pchFirstDig) && *pchFirstDig != '0');
                while (true)
                {
                    if (*--pch == '0')
                    {
                        numDig--;
                        expAdj++;
                    }
                    else if (*pch != '.')
                    {
                        Debug.Assert(IsAsciiDigit(*pch) && *pch != '0');
                        pch++;
                        break;
                    }
                }
                Debug.Assert(pch - pchFirstDig == numDig || pch - pchFirstDig == numDig + 1);

                {
                    // Construct a floating decimal from the array of digits
                    Debug.Assert(numDig > 0 && numDig <= FloatingDecimal.MaxDigits);

                    FloatingDecimal dec = new FloatingDecimal();
                    dec.Exponent = expAdj + numDig;
                    dec.Sign = sign;
                    dec.MantissaSize = numDig;

                    fixed (byte* pin = &dec.Mantissa[0])
                    {
                        byte* mantissa = pin;
                        while (pchFirstDig < pch)
                        {
                            if (*pchFirstDig != '.')
                            {
                                *mantissa = (byte)(*pchFirstDig & 0xF); // - '0'
                                mantissa++;
                            }
                            pchFirstDig++;
                        }
                    }

                    return (double)dec;
                }
            }
        }
    }
}
