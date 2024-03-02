// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using X86 = System.Runtime.Intrinsics.X86;

namespace System
{
    public partial struct Decimal
    {
        // Low level accessors used by a DecCalc and formatting
        internal uint High => _hi32;
        internal uint Low => (uint)_lo64;
        internal uint Mid => (uint)(_lo64 >> 32);

        internal ulong Low64 => _lo64;

        private static ref DecCalc AsMutable(ref decimal d) => ref Unsafe.As<decimal, DecCalc>(ref d);

        #region APIs need by number formatting.

        internal static uint DecDivMod1E9(ref decimal value)
        {
            return DecCalc.DecDivMod1E9(ref AsMutable(ref value));
        }

        #endregion

        /// <summary>
        /// Class that contains all the mathematical calculations for decimal. Most of which have been ported from oleaut32.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        private struct DecCalc
        {
            // NOTE: Do not change the offsets of these fields. This structure must have the same layout as Decimal.
            [FieldOffset(0)]
            private uint uflags;
            [FieldOffset(4)]
            private uint uhi;
#if BIGENDIAN
            [FieldOffset(8)]
            private uint umid;
            [FieldOffset(12)]
            private uint ulo;
#else
            [FieldOffset(8)]
            private uint ulo;
            [FieldOffset(12)]
            private uint umid;
#endif

            /// <summary>
            /// The low and mid fields combined
            /// </summary>
            [FieldOffset(8)]
            private ulong ulomid;

            private uint High
            {
                get => uhi;
                set => uhi = value;
            }

            private uint Low
            {
                get => ulo;
                set => ulo = value;
            }

            private uint Mid
            {
                get => umid;
                set => umid = value;
            }

            private bool IsNegative => (int)uflags < 0;

            private int Scale => (byte)(uflags >> ScaleShift);

            private ulong Low64
            {
                get => ulomid;
                set => ulomid = value;
            }

            private const uint SignMask = 0x80000000;
            private const uint ScaleMask = 0x00FF0000;

            private const int DEC_SCALE_MAX = 28;

            private const uint TenToPowerNine = 1000000000;
            private const ulong TenToPowerEighteen = 1000000000000000000;

            // The maximum power of 10 that a 32 bit integer can store
            private const int MaxInt32Scale = 9;
            // The maximum power of 10 that a 64 bit integer can store
            private const int MaxInt64Scale = 19;

            // Fast access for 10^n where n is 0-9
            private static ReadOnlySpan<uint> UInt32Powers10 =>
            [
                1,
                10,
                100,
                1000,
                10000,
                100000,
                1000000,
                10000000,
                100000000,
                1000000000
            ];

            // Fast access for 10^n where n is 1-19
            private static ReadOnlySpan<ulong> UInt64Powers10 =>
            [
                10,
                100,
                1000,
                10000,
                100000,
                1000000,
                10000000,
                100000000,
                1000000000,
                10000000000,
                100000000000,
                1000000000000,
                10000000000000,
                100000000000000,
                1000000000000000,
                10000000000000000,
                100000000000000000,
                1000000000000000000,
                10000000000000000000,
            ];

            private static ReadOnlySpan<double> DoublePowers10 =>
            [
                1, 1e1, 1e2, 1e3, 1e4, 1e5, 1e6, 1e7, 1e8, 1e9,
                1e10, 1e11, 1e12, 1e13, 1e14, 1e15, 1e16, 1e17, 1e18, 1e19,
                1e20, 1e21, 1e22, 1e23, 1e24, 1e25, 1e26, 1e27, 1e28, 1e29,
                1e30, 1e31, 1e32, 1e33, 1e34, 1e35, 1e36, 1e37, 1e38, 1e39,
                1e40, 1e41, 1e42, 1e43, 1e44, 1e45, 1e46, 1e47, 1e48, 1e49,
                1e50, 1e51, 1e52, 1e53, 1e54, 1e55, 1e56, 1e57, 1e58, 1e59,
                1e60, 1e61, 1e62, 1e63, 1e64, 1e65, 1e66, 1e67, 1e68, 1e69,
                1e70, 1e71, 1e72, 1e73, 1e74, 1e75, 1e76, 1e77, 1e78, 1e79,
                1e80
            ];

#region Decimal Math Helpers

            private static unsafe uint GetExponent(float f)
            {
                // Based on pulling out the exp from this single struct layout
                // typedef struct {
                //    ULONG mant:23;
                //    ULONG exp:8;
                //    ULONG sign:1;
                // } SNGSTRUCT;

                return (byte)(BitConverter.SingleToUInt32Bits(f) >> 23);
            }

            private static unsafe uint GetExponent(double d)
            {
                // Based on pulling out the exp from this double struct layout
                // typedef struct {
                //   DWORDLONG mant:52;
                //   DWORDLONG signexp:12;
                // } DBLSTRUCT;

                return (uint)(BitConverter.DoubleToUInt64Bits(d) >> 52) & 0x7FFu;
            }

            private static void UInt64x64To128(ulong a, ulong b, ref DecCalc result)
            {
                ulong high = Math.BigMul(a, b, out ulong low);
                if (high > uint.MaxValue)
                    Number.ThrowOverflowException(SR.Overflow_Decimal);
                result.Low64 = low;
                result.High = (uint)high;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static (ulong Quotient, uint Remainder) DivRem64By32(ulong left, uint right)
            {
                ulong quotient = left / right;
                return (quotient, (uint)quotient * right - (uint)left);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static (uint Quotient, uint Remainder) DivRem64By32Quo32(ulong left, uint right)
            {
                uint quotient = (uint)(left / right);
                return (quotient, quotient * right - (uint)left);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static (uint Quotient, uint Remainder) DivRem32Carry32By32(uint leftBase, uint leftCarry, uint right)
            {
                Debug.Assert(leftCarry < right);
                return DivRem64By32Quo32(((ulong)leftCarry << 32) | leftBase, right);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static (uint Quotient, uint Remainder) DivRem16Carry16By16(uint leftBase, uint leftCarry, uint right)
            {
                Debug.Assert(leftBase <= ushort.MaxValue);
                Debug.Assert(right <= ushort.MaxValue);
                Debug.Assert(leftCarry < right);
                return Math.DivRem((leftCarry << 16) | leftBase, right);
            }

            /// <summary>
            /// Do full divide, yielding 96-bit result and 32-bit remainder.
            /// </summary>
            /// <param name="bufNum">96-bit dividend as array of uints, least-sig first</param>
            /// <param name="den">32-bit divisor</param>
            /// <returns>Returns remainder. Quotient overwrites dividend.</returns>
            private static uint Div96By32(ref Buf12 bufNum, uint den)
            {
                uint rem;
                if (bufNum.U2 != 0)
                {
                    (bufNum.High64, rem) = DivRem64By32(bufNum.High64, den);
                    (bufNum.U0, rem) = DivRem32Carry32By32(bufNum.U0, rem, den);
                    return rem;
                }
                (bufNum.Low64, rem) = DivRem64By32(bufNum.Low64, den);
                return rem;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static bool Div96ByConst(ref ulong high64, ref uint low, uint pow)
            {
#if TARGET_64BIT
                (ulong div64, uint rem) = DivRem64By32(high64, pow);
                (uint div, rem) = DivRem32Carry32By32(low, rem, pow);
                if (rem == 0)
                {
                    high64 = div64;
                    low = div;
                    return true;
                }
#else
                // 32-bit RyuJIT doesn't convert 64-bit division by constant into multiplication by reciprocal. Do half-width divisions instead.
                Debug.Assert(pow <= ushort.MaxValue);
                uint mid32, low16, div, rem;
                if (high64 <= uint.MaxValue)
                {
                    (mid32, rem) = Math.DivRem((uint)high64, pow);
                    (low16, rem) = DivRem16Carry16By16(low >> 16, rem, pow);
                    (div, rem) = DivRem16Carry16By16((ushort)low, rem, pow);
                    if (rem == 0)
                    {
                        high64 = mid32;
                        low = (low16 << 16) | div;
                        return true;
                    }
                }
                else
                {
                    (uint high32, rem) = Math.DivRem((uint)(high64 >> 32), pow);
                    (mid32, rem) = DivRem16Carry16By16((uint)high64 >> 16, rem, pow);
                    (div, rem) = DivRem16Carry16By16((ushort)high64, rem, pow);
                    mid32 = div | (mid32 << 16);
                    (low16, rem) = DivRem16Carry16By16(low >> 16, rem, pow);
                    (div, rem) = DivRem16Carry16By16((ushort)low, rem, pow);
                    if (rem == 0)
                    {
                        high64 = ((ulong)high32 << 32) | mid32;
                        low = (low16 << 16) | div;
                        return true;
                    }
                }
#endif
                return false;
            }

            /// <summary>
            /// Normalize (unscale) the number by trying to divide out 10^8, 10^4, 10^2, and 10^1.
            /// If a division by one of these powers returns a zero remainder, then we keep the quotient.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void Unscale(ref uint low, ref ulong high64, ref int scale)
            {
                // Since 10 = 2 * 5, there must be a factor of 2 for every power of 10 we can extract.
                // We use this as a quick test on whether to try a given power.

#if TARGET_64BIT
                while ((byte)low == 0 && scale >= 8 && Div96ByConst(ref high64, ref low, 100000000))
                    scale -= 8;

                if ((low & 0xF) == 0 && scale >= 4 && Div96ByConst(ref high64, ref low, 10000))
                    scale -= 4;
#else
                while ((low & 0xF) == 0 && scale >= 4 && Div96ByConst(ref high64, ref low, 10000))
                    scale -= 4;
#endif

                if ((low & 3) == 0 && scale >= 2 && Div96ByConst(ref high64, ref low, 100))
                    scale -= 2;

                if ((low & 1) == 0 && scale >= 1 && Div96ByConst(ref high64, ref low, 10))
                    scale--;
            }

            /// <summary>
            /// Do partial divide, yielding 32-bit result and 64-bit remainder.
            /// Divisor must be larger than upper 64 bits of dividend.
            /// </summary>
            /// <param name="bufNum">96-bit dividend as array of uints, least-sig first</param>
            /// <param name="den">64-bit divisor</param>
            /// <returns>Returns quotient. Remainder overwrites lower 64-bits of dividend.</returns>
            private static uint Div96By64(ref Buf12 bufNum, ulong den)
            {
                Debug.Assert(den > bufNum.High64);
                ulong num;
                uint num2 = bufNum.U2;
                if (num2 == 0)
                {
                    num = bufNum.Low64;
                    if (num < den)
                        // Result is zero.  Entire dividend is remainder.
                        return 0;

                    (ulong quo64, bufNum.Low64) = Math.DivRem(num, den);
                    return (uint)quo64;
                }

                uint quo, rem;
                uint denHigh32 = (uint)(den >> 32);
                if (num2 >= denHigh32)
                {
                    // Divide would overflow.  Assume a quotient of 2^32, and set
                    // up remainder accordingly.
                    //
                    num = bufNum.Low64;
                    num -= den << 32;
                    quo = 0;

                    // Remainder went negative.  Add divisor back in until it's positive,
                    // a max of 2 times.
                    //
                    do
                    {
                        quo--;
                        num += den;
                    } while (num >= den);

                    bufNum.Low64 = num;
                    return quo;
                }

                // Hardware divide won't overflow
                //
                ulong num64 = bufNum.High64;
                if (num64 < denHigh32)
                    // Result is zero.  Entire dividend is remainder.
                    //
                    return 0;

                (quo, rem) = DivRem64By32Quo32(num64, denHigh32);
                num = bufNum.U0 | ((ulong)rem << 32); // remainder

                // Compute full remainder, rem = dividend - (quo * divisor).
                //
                ulong prod = Math.BigMul(quo, (uint)den); // quo * lo divisor
                num -= prod;

                if (num > ~prod)
                {
                    // Remainder went negative.  Add divisor back in until it's positive,
                    // a max of 2 times.
                    //
                    do
                    {
                        quo--;
                        num += den;
                    } while (num >= den);
                }

                bufNum.Low64 = num;
                return quo;
            }

            /// <summary>
            /// Do partial divide, yielding 32-bit result and 96-bit remainder.
            /// Top divisor uint must be larger than top dividend uint. This is
            /// assured in the initial call because the divisor is normalized
            /// and the dividend can't be. In subsequent calls, the remainder
            /// is multiplied by 10^9 (max), so it can be no more than 1/4 of
            /// the divisor which is effectively multiplied by 2^32 (4 * 10^9).
            /// </summary>
            /// <param name="bufNum">128-bit dividend as array of uints, least-sig first</param>
            /// <param name="bufDen">96-bit divisor</param>
            /// <returns>Returns quotient. Remainder overwrites lower 96-bits of dividend.</returns>
            private static uint Div128By96(ref Buf16 bufNum, ref Buf12 bufDen)
            {
                Debug.Assert(bufDen.U2 > bufNum.U3);
                ulong dividend = bufNum.High64;
                uint den = bufDen.U2;
                if (dividend < den)
                    // Result is zero.  Entire dividend is remainder.
                    //
                    return 0;

                (uint quo, uint remainder) = DivRem64By32Quo32(dividend, den);

                // Compute full remainder, rem = dividend - (quo * divisor).
                //
                ulong prod1 = Math.BigMul(quo, bufDen.U0); // quo * lo divisor
                ulong prod2 = Math.BigMul(quo, bufDen.U1); // quo * mid divisor
                prod2 += prod1 >> 32;
                prod1 = (uint)prod1 | (prod2 << 32);
                prod2 >>= 32;

                ulong num = bufNum.Low64;
                num -= prod1;
                remainder -= (uint)prod2;

                // Propagate carries
                //
                if (num > ~prod1)
                {
                    remainder--;
                    if (remainder < ~(uint)prod2)
                        goto PosRem;
                }
                else if (remainder <= ~(uint)prod2)
                    goto PosRem;
                {
                    // Remainder went negative.  Add divisor back in until it's positive,
                    // a max of 2 times.
                    //
                    prod1 = bufDen.Low64;

                    while (true)
                    {
                        quo--;
                        num += prod1;
                        remainder += den;

                        if (num < prod1)
                        {
                            // Detected carry. Check for carry out of top
                            // before adding it in.
                            //
                            if (remainder++ < den)
                                break;
                        }
                        if (remainder < den)
                            break; // detected carry
                    }
                }
PosRem:

                bufNum.Low64 = num;
                bufNum.U2 = remainder;
                return quo;
            }

            /// <summary>
            /// Multiply the two numbers. The low 96 bits of the result overwrite
            /// the input. The last 32 bits of the product are the return value.
            /// </summary>
            /// <param name="bufNum">96-bit number as array of uints, least-sig first</param>
            /// <param name="power">Scale factor to multiply by</param>
            /// <returns>Returns highest 32 bits of product</returns>
            private static uint IncreaseScale(ref Buf12 bufNum, uint power)
            {
                ulong tmp = Math.BigMul(bufNum.U0, power);
                bufNum.U0 = (uint)tmp;
                tmp >>= 32;
                tmp += Math.BigMul(bufNum.U1, power);
                bufNum.U1 = (uint)tmp;
                tmp >>= 32;
                tmp += Math.BigMul(bufNum.U2, power);
                bufNum.U2 = (uint)tmp;
                return (uint)(tmp >> 32);
            }

            private static void IncreaseScale64(ref Buf12 bufNum, uint power)
            {
                ulong tmp = Math.BigMul(bufNum.U0, power);
                bufNum.U0 = (uint)tmp;
                tmp >>= 32;
                tmp += Math.BigMul(bufNum.U1, power);
                bufNum.High64 = tmp;
            }

            /// <summary>
            /// See if we need to scale the result to fit it in 96 bits.
            /// Perform needed scaling. Adjust scale factor accordingly.
            /// </summary>
            /// <param name="bufRes">Array of uints with value, least-significant first</param>
            /// <param name="hiRes">Index of last non-zero value in bufRes</param>
            /// <param name="scale">Scale factor for this value, range 0 - 2 * DEC_SCALE_MAX</param>
            /// <returns>Returns new scale factor. bufRes updated in place, always 3 uints.</returns>
            private static unsafe int ScaleResult(Buf24* bufRes, uint hiRes, int scale)
            {
                Debug.Assert(hiRes < Buf24.Length);
                uint* result = (uint*)bufRes;

                // See if we need to scale the result.  The combined scale must
                // be <= DEC_SCALE_MAX and the upper 96 bits must be zero.
                //
                // Start by figuring a lower bound on the scaling needed to make
                // the upper 96 bits zero.  hiRes is the index into result[]
                // of the highest non-zero uint.
                //
                int newScale = 0;
                if (hiRes > 2)
                {
                    newScale = (int)hiRes * 32 - 64 - 1;
                    newScale -= BitOperations.LeadingZeroCount(result[hiRes]);

                    // Multiply bit position by log10(2) to figure it's power of 10.
                    // We scale the log by 256.  log(2) = .30103, * 256 = 77.  Doing this
                    // with a multiply saves a 96-byte lookup table.  The power returned
                    // is <= the power of the number, so we must add one power of 10
                    // to make it's integer part zero after dividing by 256.
                    //
                    // Note: the result of this multiplication by an approximation of
                    // log10(2) have been exhaustively checked to verify it gives the
                    // correct result.  (There were only 95 to check...)
                    //
                    newScale = ((newScale * 77) >> 8) + 1;

                    // newScale = min scale factor to make high 96 bits zero, 0 - 29.
                    // This reduces the scale factor of the result.  If it exceeds the
                    // current scale of the result, we'll overflow.
                    //
                    if (newScale > scale)
                        goto ThrowOverflow;
                }

                // Make sure we scale by enough to bring the current scale factor
                // into valid range.
                //
                if (newScale < scale - DEC_SCALE_MAX)
                    newScale = scale - DEC_SCALE_MAX;

                if (newScale != 0)
                {
                    // Scale by the power of 10 given by newScale.  Note that this is
                    // NOT guaranteed to bring the number within 96 bits -- it could
                    // be 1 power of 10 short.
                    //
                    scale -= newScale;
                    uint sticky = 0;
                    uint quotient, remainder = 0;

                    while (true)
                    {
                        sticky |= remainder; // record remainder as sticky bit

                        uint power;
                        // Scaling loop specialized for each power of 10 because division by constant is an order of magnitude faster (especially for 64-bit division that's actually done by 128bit DIV on x64)
                        switch (newScale)
                        {
                            case 1:
                                power = DivByConst(result, hiRes, out quotient, out remainder, 10);
                                break;
                            case 2:
                                power = DivByConst(result, hiRes, out quotient, out remainder, 100);
                                break;
                            case 3:
                                power = DivByConst(result, hiRes, out quotient, out remainder, 1000);
                                break;
                            case 4:
                                power = DivByConst(result, hiRes, out quotient, out remainder, 10000);
                                break;
#if TARGET_64BIT
                            case 5:
                                power = DivByConst(result, hiRes, out quotient, out remainder, 100000);
                                break;
                            case 6:
                                power = DivByConst(result, hiRes, out quotient, out remainder, 1000000);
                                break;
                            case 7:
                                power = DivByConst(result, hiRes, out quotient, out remainder, 10000000);
                                break;
                            case 8:
                                power = DivByConst(result, hiRes, out quotient, out remainder, 100000000);
                                break;
                            default:
                                power = DivByConst(result, hiRes, out quotient, out remainder, TenToPowerNine);
                                break;
#else
                            default:
                                goto case 4;
#endif
                        }
                        result[hiRes] = quotient;
                        // If first quotient was 0, update hiRes.
                        //
                        if (quotient == 0 && hiRes != 0)
                            hiRes--;

#if TARGET_64BIT
                        newScale -= MaxInt32Scale;
#else
                        newScale -= 4;
#endif
                        if (newScale > 0)
                            continue; // scale some more

                        // If we scaled enough, hiRes would be 2 or less.  If not,
                        // divide by 10 more.
                        //
                        if (hiRes > 2)
                        {
                            if (scale == 0)
                                goto ThrowOverflow;
                            newScale = 1;
                            scale--;
                            continue; // scale by 10
                        }

                        // Round final result.  See if remainder >= 1/2 of divisor.
                        // If remainder == 1/2 divisor, round up if odd or sticky bit set.
                        //
                        power >>= 1;  // power of 10 always even
                        if (power <= remainder && (power < remainder || ((result[0] & 1) | sticky) != 0) && ++result[0] == 0)
                        {
                            uint cur = 0;
                            do
                            {
                                Debug.Assert(cur + 1 < Buf24.Length);
                            }
                            while (++result[++cur] == 0);

                            if (cur > 2)
                            {
                                // The rounding caused us to carry beyond 96 bits.
                                // Scale by 10 more.
                                //
                                if (scale == 0)
                                    goto ThrowOverflow;
                                hiRes = cur;
                                sticky = 0;    // no sticky bit
                                remainder = 0; // or remainder
                                newScale = 1;
                                scale--;
                                continue; // scale by 10
                            }
                        }

                        break;
                    } // while (true)
                }
                return scale;

ThrowOverflow:
                Number.ThrowOverflowException(SR.Overflow_Decimal);
                return 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static unsafe uint DivByConst(uint* result, uint hiRes, out uint quotient, out uint remainder, uint power)
            {
                uint high = result[hiRes];
                (quotient, remainder) = Math.DivRem(high, power);
                for (uint i = hiRes - 1; (int)i >= 0; i--)
                {
#if TARGET_64BIT
                    (result[i], remainder) = DivRem32Carry32By32(result[i], remainder, power);
#else
                    // 32-bit RyuJIT doesn't convert 64-bit division by constant into multiplication by reciprocal. Do half-width divisions instead.
                    Debug.Assert(power <= ushort.MaxValue);
#if BIGENDIAN
                    const int low16 = 2, high16 = 0;
#else
                    const int low16 = 0, high16 = 2;
#endif
                    // byte* is used here because Roslyn doesn't do constant propagation for pointer arithmetic
                    (uint div, remainder) = DivRem16Carry16By16(*(ushort*)((byte*)result + i * 4 + high16), remainder, power);
                    *(ushort*)((byte*)result + i * 4 + high16) = (ushort)div;

                    (div, remainder) = DivRem16Carry16By16(*(ushort*)((byte*)result + i * 4 + low16), remainder, power);
                    *(ushort*)((byte*)result + i * 4 + low16) = (ushort)div;
#endif
                }
                return power;
            }

            /// <summary>
            /// Adjust the quotient to deal with an overflow.
            /// We need to divide by 10, feed in the high bit to undo the overflow and then round as required.
            /// </summary>
            private static int OverflowUnscale(ref Buf12 bufQuo, int scale, bool sticky)
            {
                if (--scale < 0)
                    Number.ThrowOverflowException(SR.Overflow_Decimal);

                Debug.Assert(bufQuo.U2 == 0);

                // We have overflown, so load the high bit with a one.
                const ulong highbit = 1UL << 32;
                bufQuo.U2 = (uint)(highbit / 10);
                (bufQuo.U1, uint remainder) = DivRem32Carry32By32(bufQuo.U1, (uint)(highbit % 10), 10);
                (bufQuo.U0, remainder) = DivRem32Carry32By32(bufQuo.U0, remainder, 10);
                // The remainder is the last digit that does not fit, so we can use it to work out if we need to round up
                if (remainder > 5 || remainder == 5 && (sticky || (bufQuo.U0 & 1) != 0))
                    Add32To96(ref bufQuo, 1);
                return scale;
            }

            /// <summary>
            /// Determine the max power of 10, &lt;= 9, that the quotient can be scaled
            /// up by and still fit in 96 bits.
            /// </summary>
            /// <param name="bufQuo">96-bit quotient</param>
            /// <param name="scale ">Scale factor of quotient, range -DEC_SCALE_MAX to DEC_SCALE_MAX-1</param>
            /// <returns>power of 10 to scale by</returns>
            private static int SearchScale(ref Buf12 bufQuo, int scale)
            {
                const uint OVFL_MAX_9_HI = 4;
                const uint OVFL_MAX_8_HI = 42;
                const uint OVFL_MAX_7_HI = 429;
                const uint OVFL_MAX_6_HI = 4294;
                const uint OVFL_MAX_5_HI = 42949;
                const uint OVFL_MAX_4_HI = 429496;
                const uint OVFL_MAX_3_HI = 4294967;
                const uint OVFL_MAX_2_HI = 42949672;
                const uint OVFL_MAX_1_HI = 429496729;
                const ulong OVFL_MAX_9_MIDLO = 5441186219426131129;

                uint resHi = bufQuo.U2;
                ulong resMidLo = bufQuo.Low64;
                int curScale = 0;

                // Quick check to stop us from trying to scale any more.
                //
                if (resHi > OVFL_MAX_1_HI)
                {
                    goto HaveScale;
                }

                PowerOvfl[] powerOvfl = PowerOvflValues;
                if (scale > DEC_SCALE_MAX - 9)
                {
                    // We can't scale by 10^9 without exceeding the max scale factor.
                    // See if we can scale to the max.  If not, we'll fall into
                    // standard search for scale factor.
                    //
                    curScale = DEC_SCALE_MAX - scale;
                    if (resHi < powerOvfl[curScale - 1].Hi)
                        goto HaveScale;
                }
                else if (resHi < OVFL_MAX_9_HI || resHi == OVFL_MAX_9_HI && resMidLo <= OVFL_MAX_9_MIDLO)
                    return 9;

                // Search for a power to scale by < 9.  Do a binary search.
                //
                if (resHi > OVFL_MAX_5_HI)
                {
                    if (resHi > OVFL_MAX_3_HI)
                    {
                        curScale = 2;
                        if (resHi > OVFL_MAX_2_HI)
                            curScale--;
                    }
                    else
                    {
                        curScale = 4;
                        if (resHi > OVFL_MAX_4_HI)
                            curScale--;
                    }
                }
                else
                {
                    if (resHi > OVFL_MAX_7_HI)
                    {
                        curScale = 6;
                        if (resHi > OVFL_MAX_6_HI)
                            curScale--;
                    }
                    else
                    {
                        curScale = 8;
                        if (resHi > OVFL_MAX_8_HI)
                            curScale--;
                    }
                }

                // In all cases, we already found we could not use the power one larger.
                // So if we can use this power, it is the biggest, and we're done.  If
                // we can't use this power, the one below it is correct for all cases
                // unless it's 10^1 -- we might have to go to 10^0 (no scaling).
                //
                if (resHi == powerOvfl[curScale - 1].Hi && resMidLo > powerOvfl[curScale - 1].MidLo)
                    curScale--;

                HaveScale:
                // curScale = largest power of 10 we can scale by without overflow,
                // curScale < 9.  See if this is enough to make scale factor
                // positive if it isn't already.
                //
                if (curScale + scale < 0)
                    Number.ThrowOverflowException(SR.Overflow_Decimal);

                return curScale;
            }

            /// <summary>
            /// Add a 32-bit uint to an array of 3 uints representing a 96-bit integer.
            /// </summary>
            /// <returns>Returns false if there is an overflow</returns>
            private static bool Add32To96(ref Buf12 bufNum, uint value)
            {
                if ((bufNum.Low64 += value) < value)
                {
                    if (++bufNum.U2 == 0)
                        return false;
                }
                return true;
            }

            /// <summary>
            /// Adds or subtracts two decimal values.
            /// On return, d1 contains the result of the operation and d2 is trashed.
            /// </summary>
            /// <param name="d1">First decimal to add or subtract.</param>
            /// <param name="d2">Second decimal to add or subtract.</param>
            /// <param name="sign">True means subtract and false means add.</param>
            internal static unsafe void DecAddSub(ref DecCalc d1, ref DecCalc d2, bool sign)
            {
                ulong low64 = d1.Low64;
                uint high = d1.High, flags = d1.uflags, d2flags = d2.uflags;

                uint xorflags = d2flags ^ flags;
                sign ^= (xorflags & SignMask) != 0;

                if ((xorflags & ScaleMask) == 0)
                {
                    // Scale factors are equal, no alignment necessary.
                    //
                    goto AlignedAdd;
                }
                else
                {
                    // Scale factors are not equal.  Assume that a larger scale
                    // factor (more decimal places) is likely to mean that number
                    // is smaller.  Start by guessing that the right operand has
                    // the larger scale factor.  The result will have the larger
                    // scale factor.
                    //
                    uint d1flags = flags;
                    flags = d2flags & ScaleMask | flags & SignMask; // scale factor of "smaller",  but sign of "larger"
                    int scale = (int)(flags - d1flags) >> ScaleShift;

                    if (scale < 0)
                    {
                        // Guessed scale factor wrong. Swap operands.
                        //
                        scale = -scale;
                        flags = d1flags;
                        if (sign)
                            flags ^= SignMask;
                        low64 = d2.Low64;
                        high = d2.High;
                        d2 = d1;
                    }

                    uint power;
                    ulong tmp64, tmpLow;

                    // d1 will need to be multiplied by 10^scale so
                    // it will have the same scale as d2.  We could be
                    // extending it to up to 192 bits of precision.

                    // Scan for zeros in the upper words.
                    //
                    if (high == 0)
                    {
                        if (low64 <= uint.MaxValue)
                        {
                            if ((uint)low64 == 0)
                            {
                                // Left arg is zero, return right.
                                //
                                uint signFlags = flags & SignMask;
                                if (sign)
                                    signFlags ^= SignMask;
                                d1 = d2;
                                d1.uflags = d2.uflags & ScaleMask | signFlags;
                                return;
                            }

                            do
                            {
                                if (scale <= MaxInt32Scale)
                                {
                                    low64 = Math.BigMul((uint)low64, UInt32Powers10[scale]);
                                    goto AlignedAdd;
                                }
                                scale -= MaxInt32Scale;
                                low64 = Math.BigMul((uint)low64, TenToPowerNine);
                            } while (low64 <= uint.MaxValue);
                        }

                        do
                        {
                            power = TenToPowerNine;
                            if (scale < MaxInt32Scale)
                                power = UInt32Powers10[scale];
                            tmpLow = Math.BigMul((uint)low64, power);
                            tmp64 = Math.BigMul((uint)(low64 >> 32), power) + (tmpLow >> 32);
                            low64 = (uint)tmpLow | (tmp64 << 32);
                            high = (uint)(tmp64 >> 32);
                            if ((scale -= MaxInt32Scale) <= 0)
                                goto AlignedAdd;
                        } while (high == 0);
                    }

                    while (true)
                    {
                        // Scaling won't make it larger than 4 uints
                        //
                        power = TenToPowerNine;
                        if (scale < MaxInt32Scale)
                            power = UInt32Powers10[scale];
                        tmpLow = Math.BigMul((uint)low64, power);
                        tmp64 = Math.BigMul((uint)(low64 >> 32), power) + (tmpLow >> 32);
                        low64 = (uint)tmpLow | (tmp64 << 32);
                        tmp64 >>= 32;
                        tmp64 += Math.BigMul(high, power);

                        scale -= MaxInt32Scale;
                        if (tmp64 > uint.MaxValue)
                            break;

                        high = (uint)tmp64;
                        // Result fits in 96 bits.  Use standard aligned add.
                        if (scale <= 0)
                            goto AlignedAdd;
                    }

                    // Have to scale by a bunch. Move the number to a buffer where it has room to grow as it's scaled.
                    //
                    Unsafe.SkipInit(out Buf24 bufNum);

                    bufNum.Low64 = low64;
                    bufNum.Mid64 = tmp64;
                    uint hiProd = 3;

                    // Scaling loop, up to 10^9 at a time. hiProd stays updated with index of highest non-zero uint.
                    //
                    for (; scale > 0; scale -= MaxInt32Scale)
                    {
                        power = TenToPowerNine;
                        if (scale < MaxInt32Scale)
                            power = UInt32Powers10[scale];
                        tmp64 = 0;
                        uint* rgulNum = (uint*)&bufNum;
                        for (uint cur = 0; ;)
                        {
                            Debug.Assert(cur < Buf24.Length);
                            tmp64 += Math.BigMul(rgulNum[cur], power);
                            rgulNum[cur] = (uint)tmp64;
                            cur++;
                            tmp64 >>= 32;
                            if (cur > hiProd)
                                break;
                        }

                        if ((uint)tmp64 != 0)
                        {
                            // We're extending the result by another uint.
                            Debug.Assert(hiProd + 1 < Buf24.Length);
                            rgulNum[++hiProd] = (uint)tmp64;
                        }
                    }

                    // Scaling complete, do the add.  Could be subtract if signs differ.
                    //
                    tmp64 = bufNum.Low64;
                    low64 = d2.Low64;
                    uint tmpHigh = bufNum.U2;
                    high = d2.High;

                    if (sign)
                    {
                        // Signs differ, subtract.
                        //
                        low64 = tmp64 - low64;
                        high = tmpHigh - high;

                        // Propagate carry
                        //
                        if (low64 > tmp64)
                        {
                            high--;
                            if (high < tmpHigh)
                                goto NoCarry;
                        }
                        else if (high <= tmpHigh)
                            goto NoCarry;

                        // Carry the subtraction into the higher bits.
                        //
                        uint* number = (uint*)&bufNum;
                        uint cur = 3;
                        do
                        {
                            Debug.Assert(cur < Buf24.Length);
                        } while (number[cur++]-- == 0);
                        Debug.Assert(hiProd < Buf24.Length);
                        if (number[hiProd] == 0 && --hiProd <= 2)
                            goto ReturnResult;
                    }
                    else
                    {
                        // Signs the same, add.
                        //
                        low64 += tmp64;
                        high += tmpHigh;

                        // Propagate carry
                        //
                        if (low64 < tmp64)
                        {
                            high++;
                            if (high > tmpHigh)
                                goto NoCarry;
                        }
                        else if (high >= tmpHigh)
                            goto NoCarry;

                        uint* number = (uint*)&bufNum;
                        for (uint cur = 3; ++number[cur++] == 0;)
                        {
                            Debug.Assert(cur < Buf24.Length);
                            if (hiProd < cur)
                            {
                                number[cur] = 1;
                                hiProd = cur;
                                break;
                            }
                        }
                    }
NoCarry:

                    bufNum.Low64 = low64;
                    bufNum.U2 = high;
                    scale = ScaleResult(&bufNum, hiProd, (byte)(flags >> ScaleShift));
                    flags = (flags & ~ScaleMask) | ((uint)scale << ScaleShift);
                    low64 = bufNum.Low64;
                    high = bufNum.U2;
                    goto ReturnResult;
                }

SignFlip:
                {
                    // Got negative result.  Flip its sign.
                    flags ^= SignMask;
                    high = ~high;
                    low64 = (ulong)-(long)low64;
                    if (low64 == 0)
                        high++;
                    goto ReturnResult;
                }

AlignedScale:
                {
                    // The addition carried above 96 bits.
                    // Divide the value by 10, dropping the scale factor.
                    //
                    if ((flags & ScaleMask) == 0)
                        Number.ThrowOverflowException(SR.Overflow_Decimal);
                    flags -= 1 << ScaleShift;

                    (high, uint rem) = DivRem32Carry32By32(high, 1, 10);
                    (uint div, rem) = DivRem32Carry32By32((uint)(low64 >> 32), rem, 10);
                    low64 = div;
                    low64 <<= 32;
                    (div, rem) = DivRem32Carry32By32((uint)low64, rem, 10);
                    low64 |= div;

                    // See if we need to round up.
                    //
                    if (rem >= 5 && (rem > 5 || (low64 & 1) != 0))
                    {
                        if (++low64 == 0)
                            high++;
                    }
                    goto ReturnResult;
                }

AlignedAdd:
                {
                    ulong d1Low64 = low64;
                    uint d1High = high;
                    if (sign)
                    {
                        // Signs differ - subtract
                        //
                        low64 = d1Low64 - d2.Low64;
                        high = d1High - d2.High;

                        // Propagate carry
                        //
                        if (low64 > d1Low64)
                        {
                            high--;
                            if (high >= d1High)
                                goto SignFlip;
                        }
                        else if (high > d1High)
                            goto SignFlip;
                    }
                    else
                    {
                        // Signs are the same - add
                        //
                        low64 = d1Low64 + d2.Low64;
                        high = d1High + d2.High;

                        // Propagate carry
                        //
                        if (low64 < d1Low64)
                        {
                            high++;
                            if (high <= d1High)
                                goto AlignedScale;
                        }
                        else if (high < d1High)
                            goto AlignedScale;
                    }
                    goto ReturnResult;
                }

ReturnResult:
                d1.uflags = flags;
                d1.High = high;
                d1.Low64 = low64;
                return;
            }

#endregion

            /// <summary>
            /// Convert Decimal to Currency (similar to OleAut32 api.)
            /// </summary>
            internal static long VarCyFromDec(ref DecCalc pdecIn)
            {
                long value;

                int scale = pdecIn.Scale - 4;
                // Need to scale to get 4 decimal places.  -4 <= scale <= 24.
                //
                if (scale < 0)
                {
                    if (pdecIn.High != 0)
                        goto ThrowOverflow;
                    uint pwr = UInt32Powers10[-scale];
                    ulong high = Math.BigMul(pwr, pdecIn.Mid);
                    if (high > uint.MaxValue)
                        goto ThrowOverflow;
                    ulong low = Math.BigMul(pwr, pdecIn.Low);
                    low += high <<= 32;
                    if (low < high)
                        goto ThrowOverflow;
                    value = (long)low;
                }
                else
                {
                    if (scale != 0)
                        InternalRound(ref pdecIn, (uint)scale, MidpointRounding.ToEven);
                    if (pdecIn.High != 0)
                        goto ThrowOverflow;
                    value = (long)pdecIn.Low64;
                }

                if (value < 0 && (value != long.MinValue || !pdecIn.IsNegative))
                    goto ThrowOverflow;

                if (pdecIn.IsNegative)
                    value = -value;

                return value;

ThrowOverflow:
                throw new OverflowException(SR.Overflow_Currency);
            }

            /// <summary>
            /// Decimal Compare updated to return values similar to ICompareTo
            /// </summary>
            internal static int VarDecCmp(in decimal d1, in decimal d2)
            {
                if ((d2.Low64 | d2.High) == 0)
                {
                    if ((d1.Low64 | d1.High) == 0)
                        return 0;
                    return (d1._flags >> 31) | 1;
                }
                if ((d1.Low64 | d1.High) == 0)
                    return -((d2._flags >> 31) | 1);

                int sign = (d1._flags >> 31) - (d2._flags >> 31);
                if (sign != 0)
                    return sign;
                return VarDecCmpSub(in d1, in d2);
            }

            private static int VarDecCmpSub(in decimal d1, in decimal d2)
            {
                int flags = d2._flags;
                int sign = (flags >> 31) | 1;
                int scale = flags - d1._flags;

                ulong low64 = d1.Low64;
                uint high = d1.High;

                ulong d2Low64 = d2.Low64;
                uint d2High = d2.High;

                if (scale != 0)
                {
                    scale >>= ScaleShift;

                    // Scale factors are not equal. Assume that a larger scale factor (more decimal places) is likely to mean that number is smaller.
                    // Start by guessing that the right operand has the larger scale factor.
                    if (scale < 0)
                    {
                        // Guessed scale factor wrong. Swap operands.
                        scale = -scale;
                        sign = -sign;

                        ulong tmp64 = low64;
                        low64 = d2Low64;
                        d2Low64 = tmp64;

                        uint tmp = high;
                        high = d2High;
                        d2High = tmp;
                    }

                    // d1 will need to be multiplied by 10^scale so it will have the same scale as d2.
                    // Scaling loop, up to 10^9 at a time.
                    do
                    {
                        uint power = scale >= MaxInt32Scale ? TenToPowerNine : UInt32Powers10[scale];
                        ulong tmpLow = Math.BigMul((uint)low64, power);
                        ulong tmp = Math.BigMul((uint)(low64 >> 32), power) + (tmpLow >> 32);
                        low64 = (uint)tmpLow | (tmp << 32);
                        tmp >>= 32;
                        tmp += Math.BigMul(high, power);
                        // If the scaled value has more than 96 significant bits then it's greater than d2
                        if (tmp > uint.MaxValue)
                            return sign;
                        high = (uint)tmp;
                    } while ((scale -= MaxInt32Scale) > 0);
                }

                uint cmpHigh = high - d2High;
                if (cmpHigh != 0)
                {
                    // check for overflow
                    if (cmpHigh > high)
                        sign = -sign;
                    return sign;
                }

                ulong cmpLow64 = low64 - d2Low64;
                if (cmpLow64 == 0)
                    sign = 0;
                // check for overflow
                else if (cmpLow64 > low64)
                    sign = -sign;
                return sign;
            }

            /// <summary>
            /// Decimal Multiply
            /// </summary>
            internal static unsafe void VarDecMul(ref DecCalc d1, ref DecCalc d2)
            {
                int scale = (byte)(d1.uflags + d2.uflags >> ScaleShift);

                ulong tmp;
                uint hiProd;
                Unsafe.SkipInit(out Buf24 bufProd);

                if ((d1.High | d1.Mid) == 0)
                {
                    if ((d2.High | d2.Mid) == 0)
                    {
                        // Upper 64 bits are zero.
                        //
                        ulong low64 = Math.BigMul(d1.Low, d2.Low);
                        if (scale > DEC_SCALE_MAX)
                        {
                            // Result scale is too big.  Divide result by power of 10 to reduce it.
                            // If the amount to divide by is > 19 the result is guaranteed
                            // less than 1/2.  [max value in 64 bits = 1.84E19]
                            //
                            if (scale > DEC_SCALE_MAX + MaxInt64Scale)
                                goto ReturnZero;

                            scale -= DEC_SCALE_MAX + 1;
                            ulong power = UInt64Powers10[scale];

                            (low64, ulong remainder) = Math.DivRem(low64, power);

                            // Round result.  See if remainder >= 1/2 of divisor.
                            // Divisor is a power of 10, so it is always even.
                            //
                            power >>= 1;
                            if (remainder >= power && (remainder > power || ((uint)low64 & 1) > 0))
                                low64++;

                            scale = DEC_SCALE_MAX;
                        }
                        d1.Low64 = low64;
                        d1.uflags = ((d2.uflags ^ d1.uflags) & SignMask) | ((uint)scale << ScaleShift);
                        return;
                    }
                    else
                    {
                        // Left value is 32-bit, result fits in 4 uints
                        tmp = Math.BigMul(d1.Low, d2.Low);
                        bufProd.U0 = (uint)tmp;

                        tmp = Math.BigMul(d1.Low, d2.Mid) + (tmp >> 32);
                        bufProd.U1 = (uint)tmp;
                        tmp >>= 32;

                        if (d2.High != 0)
                        {
                            tmp += Math.BigMul(d1.Low, d2.High);
                            if (tmp > uint.MaxValue)
                            {
                                bufProd.Mid64 = tmp;
                                hiProd = 3;
                                goto SkipScan;
                            }
                        }
                        bufProd.U2 = (uint)tmp;
                        hiProd = 2;
                    }
                }
                else if ((d2.High | d2.Mid) == 0)
                {
                    // Right value is 32-bit, result fits in 4 uints
                    tmp = Math.BigMul(d2.Low, d1.Low);
                    bufProd.U0 = (uint)tmp;

                    tmp = Math.BigMul(d2.Low, d1.Mid) + (tmp >> 32);
                    bufProd.U1 = (uint)tmp;
                    tmp >>= 32;

                    if (d1.High != 0)
                    {
                        tmp += Math.BigMul(d2.Low, d1.High);
                        if (tmp > uint.MaxValue)
                        {
                            bufProd.Mid64 = tmp;
                            hiProd = 3;
                            goto SkipScan;
                        }
                    }
                    bufProd.U2 = (uint)tmp;
                    hiProd = 2;
                }
                else
                {
                    // Both operands have bits set in the upper 64 bits.
                    //
                    // Compute and accumulate the 9 partial products into a
                    // 192-bit (24-byte) result.
                    //
                    //        [l-h][l-m][l-l]      left high, middle, low
                    //         x    [r-h][r-m][r-l]      right high, middle, low
                    // ------------------------------
                    //
                    //             [0-h][0-l]      l-l * r-l
                    //        [1ah][1al]      l-l * r-m
                    //        [1bh][1bl]      l-m * r-l
                    //       [2ah][2al]          l-m * r-m
                    //       [2bh][2bl]          l-l * r-h
                    //       [2ch][2cl]          l-h * r-l
                    //      [3ah][3al]          l-m * r-h
                    //      [3bh][3bl]          l-h * r-m
                    // [4-h][4-l]              l-h * r-h
                    // ------------------------------
                    // [p-5][p-4][p-3][p-2][p-1][p-0]      prod[] array
                    //

                    tmp = Math.BigMul(d1.Low, d2.Low);
                    bufProd.U0 = (uint)tmp;

                    ulong tmp2 = Math.BigMul(d1.Low, d2.Mid) + (tmp >> 32);

                    tmp = Math.BigMul(d1.Mid, d2.Low);
                    tmp += tmp2; // this could generate carry
                    bufProd.U1 = (uint)tmp;
                    if (tmp < tmp2) // detect carry
                        tmp2 = (tmp >> 32) | (1UL << 32);
                    else
                        tmp2 = tmp >> 32;

                    tmp = Math.BigMul(d1.Mid, d2.Mid) + tmp2;

                    if ((d1.High | d2.High) > 0)
                    {
                        // Highest 32 bits is non-zero.     Calculate 5 more partial products.
                        //
                        tmp2 = Math.BigMul(d1.Low, d2.High);
                        tmp += tmp2; // this could generate carry
                        uint tmp3 = 0;
                        if (tmp < tmp2) // detect carry
                            tmp3 = 1;

                        tmp2 = Math.BigMul(d1.High, d2.Low);
                        tmp += tmp2; // this could generate carry
                        bufProd.U2 = (uint)tmp;
                        if (tmp < tmp2) // detect carry
                            tmp3++;
                        tmp2 = ((ulong)tmp3 << 32) | (tmp >> 32);

                        tmp = Math.BigMul(d1.Mid, d2.High);
                        tmp += tmp2; // this could generate carry
                        tmp3 = 0;
                        if (tmp < tmp2) // detect carry
                            tmp3 = 1;

                        tmp2 = Math.BigMul(d1.High, d2.Mid);
                        tmp += tmp2; // this could generate carry
                        bufProd.U3 = (uint)tmp;
                        if (tmp < tmp2) // detect carry
                            tmp3++;
                        tmp = ((ulong)tmp3 << 32) | (tmp >> 32);

                        bufProd.High64 = Math.BigMul(d1.High, d2.High) + tmp;

                        hiProd = 5;
                    }
                    else
                    {
                        bufProd.Mid64 = tmp;
                        hiProd = 3;
                    }
                }

                // Check for leading zero uints on the product
                //
                uint* product = (uint*)&bufProd;
                while (product[(int)hiProd] == 0)
                {
                    if (hiProd == 0)
                        goto ReturnZero;
                    hiProd--;
                }

SkipScan:
                if (hiProd > 2 || scale > DEC_SCALE_MAX)
                {
                    scale = ScaleResult(&bufProd, hiProd, scale);
                }

                d1.Low64 = bufProd.Low64;
                d1.High = bufProd.U2;
                d1.uflags = ((d2.uflags ^ d1.uflags) & SignMask) | ((uint)scale << ScaleShift);
                return;

ReturnZero:
                d1 = default;
            }

            /// <summary>
            /// Convert float to Decimal
            /// </summary>
            internal static void VarDecFromR4(float input, out DecCalc result)
            {
                result = default;

                // The most we can scale by is 10^28, which is just slightly more
                // than 2^93.  So a float with an exponent of -94 could just
                // barely reach 0.5, but smaller exponents will always round to zero.
                //
                const uint SNGBIAS = 126;
                int exp = (int)(GetExponent(input) - SNGBIAS);
                if (exp < -94)
                    return; // result should be zeroed out

                if (exp > 96)
                    Number.ThrowOverflowException(SR.Overflow_Decimal);

                uint flags = 0;
                if (input < 0)
                {
                    input = -input;
                    flags = SignMask;
                }

                // Round the input to a 7-digit integer.  The R4 format has
                // only 7 digits of precision, and we want to keep garbage digits
                // out of the Decimal were making.
                //
                // Calculate max power of 10 input value could have by multiplying
                // the exponent by log10(2).  Using scaled integer multiplcation,
                // log10(2) * 2 ^ 16 = .30103 * 65536 = 19728.3.
                //
                double dbl = input;
                int power = 6 - ((exp * 19728) >> 16);
                // power is between -22 and 35

                if (power >= 0)
                {
                    // We have less than 7 digits, scale input up.
                    //
                    if (power > DEC_SCALE_MAX)
                        power = DEC_SCALE_MAX;

                    dbl *= DoublePowers10[power];
                }
                else
                {
                    if (power != -1 || dbl >= 1E7)
                        dbl /= DoublePowers10[-power];
                    else
                        power = 0; // didn't scale it
                }

                Debug.Assert(dbl < 1E7);
                if (dbl < 1E6 && power < DEC_SCALE_MAX)
                {
                    dbl *= 10;
                    power++;
                    Debug.Assert(dbl >= 1E6);
                }

                // Round to integer
                //
                uint mant;
                // with SSE4.1 support ROUNDSD can be used
                if (X86.Sse41.IsSupported)
                    mant = (uint)(int)Math.Round(dbl);
                else
                {
                    mant = (uint)(int)dbl;
                    dbl -= (int)mant;  // difference between input & integer
                    if (dbl > 0.5 || dbl == 0.5 && (mant & 1) != 0)
                        mant++;
                }

                if (mant == 0)
                    return;  // result should be zeroed out

                if (power < 0)
                {
                    // Add -power factors of 10, -power <= (29 - 7) = 22.
                    //
                    power = -power;
                    if (power < 10)
                    {
                        result.Low64 = Math.BigMul(mant, UInt32Powers10[power]);
                    }
                    else
                    {
                        // Have a big power of 10.
                        //
                        if (power > 18)
                        {
                            ulong low64 = Math.BigMul(mant, UInt32Powers10[power - 18]);
                            UInt64x64To128(low64, TenToPowerEighteen, ref result);
                        }
                        else
                        {
                            ulong low64 = Math.BigMul(mant, UInt32Powers10[power - 9]);
                            ulong hi64 = Math.BigMul(TenToPowerNine, (uint)(low64 >> 32));
                            low64 = Math.BigMul(TenToPowerNine, (uint)low64);
                            result.Low = (uint)low64;
                            hi64 += low64 >> 32;
                            result.Mid = (uint)hi64;
                            hi64 >>= 32;
                            result.High = (uint)hi64;
                        }
                    }
                }
                else
                {
                    // Factor out powers of 10 to reduce the scale, if possible.
                    // The maximum number we could factor out would be 6.  This
                    // comes from the fact we have a 7-digit number, and the
                    // MSD must be non-zero -- but the lower 6 digits could be
                    // zero.  Note also the scale factor is never negative, so
                    // we can't scale by any more than the power we used to
                    // get the integer.
                    //
                    int lmax = power;
                    if (lmax > 6)
                        lmax = 6;

                    if ((mant & 0xF) == 0 && lmax >= 4)
                    {
                        (uint div, uint rem) = Math.DivRem(mant, 10000);
                        if (rem == 0)
                        {
                            mant = div;
                            power -= 4;
                            lmax -= 4;
                        }
                    }

                    if ((mant & 3) == 0 && lmax >= 2)
                    {
                        (uint div, uint rem) = Math.DivRem(mant, 100);
                        if (rem == 0)
                        {
                            mant = div;
                            power -= 2;
                            lmax -= 2;
                        }
                    }

                    if ((mant & 1) == 0 && lmax >= 1)
                    {
                        (uint div, uint rem) = Math.DivRem(mant, 10);
                        if (rem == 0)
                        {
                            mant = div;
                            power--;
                        }
                    }

                    flags |= (uint)power << ScaleShift;
                    result.Low = mant;
                }

                result.uflags = flags;
            }

            /// <summary>
            /// Convert double to Decimal
            /// </summary>
            internal static void VarDecFromR8(double input, out DecCalc result)
            {
                result = default;

                // The most we can scale by is 10^28, which is just slightly more
                // than 2^93.  So a float with an exponent of -94 could just
                // barely reach 0.5, but smaller exponents will always round to zero.
                //
                const uint DBLBIAS = 1022;
                int exp = (int)(GetExponent(input) - DBLBIAS);
                if (exp < -94)
                    return; // result should be zeroed out

                if (exp > 96)
                    Number.ThrowOverflowException(SR.Overflow_Decimal);

                uint flags = 0;
                if (input < 0)
                {
                    input = -input;
                    flags = SignMask;
                }

                // Round the input to a 15-digit integer.  The R8 format has
                // only 15 digits of precision, and we want to keep garbage digits
                // out of the Decimal were making.
                //
                // Calculate max power of 10 input value could have by multiplying
                // the exponent by log10(2).  Using scaled integer multiplcation,
                // log10(2) * 2 ^ 16 = .30103 * 65536 = 19728.3.
                //
                double dbl = input;
                int power = 14 - ((exp * 19728) >> 16);
                // power is between -14 and 43

                if (power >= 0)
                {
                    // We have less than 15 digits, scale input up.
                    //
                    if (power > DEC_SCALE_MAX)
                        power = DEC_SCALE_MAX;

                    dbl *= DoublePowers10[power];
                }
                else
                {
                    if (power != -1 || dbl >= 1E15)
                        dbl /= DoublePowers10[-power];
                    else
                        power = 0; // didn't scale it
                }

                Debug.Assert(dbl < 1E15);
                if (dbl < 1E14 && power < DEC_SCALE_MAX)
                {
                    dbl *= 10;
                    power++;
                    Debug.Assert(dbl >= 1E14);
                }

                // Round to int64
                //
                ulong mant;
                // with SSE4.1 support ROUNDSD can be used
                if (X86.Sse41.IsSupported)
                    mant = (ulong)(long)Math.Round(dbl);
                else
                {
                    mant = (ulong)(long)dbl;
                    dbl -= (long)mant;  // difference between input & integer
                    if (dbl > 0.5 || dbl == 0.5 && (mant & 1) != 0)
                        mant++;
                }

                if (mant == 0)
                    return;  // result should be zeroed out

                if (power < 0)
                {
                    // Add -power factors of 10, -power <= (29 - 15) = 14.
                    //
                    power = -power;
                    if (power < 10)
                    {
                        uint pow10 = UInt32Powers10[power];
                        ulong low64 = Math.BigMul((uint)mant, pow10);
                        ulong hi64 = Math.BigMul((uint)(mant >> 32), pow10);
                        result.Low = (uint)low64;
                        hi64 += low64 >> 32;
                        result.Mid = (uint)hi64;
                        hi64 >>= 32;
                        result.High = (uint)hi64;
                    }
                    else
                    {
                        // Have a big power of 10.
                        //
                        Debug.Assert(power <= 14);
                        UInt64x64To128(mant, UInt64Powers10[power - 1], ref result);
                    }
                }
                else
                {
                    // Factor out powers of 10 to reduce the scale, if possible.
                    // The maximum number we could factor out would be 14.  This
                    // comes from the fact we have a 15-digit number, and the
                    // MSD must be non-zero -- but the lower 14 digits could be
                    // zero.  Note also the scale factor is never negative, so
                    // we can't scale by any more than the power we used to
                    // get the integer.
                    //
                    int lmax = power;
                    if (lmax > 14)
                        lmax = 14;

                    if ((byte)mant == 0 && lmax >= 8)
                    {
                        (ulong div, uint rem) = DivRem64By32(mant, 100000000);
                        if (rem == 0)
                        {
                            mant = div;
                            power -= 8;
                            lmax -= 8;
                        }
                    }

                    if (((uint)mant & 0xF) == 0 && lmax >= 4)
                    {
                        (ulong div, uint rem) = DivRem64By32(mant, 10000);
                        if (rem == 0)
                        {
                            mant = div;
                            power -= 4;
                            lmax -= 4;
                        }
                    }

                    if (((uint)mant & 3) == 0 && lmax >= 2)
                    {
                        (ulong div, uint rem) = DivRem64By32(mant, 100);
                        if (rem == 0)
                        {
                            mant = div;
                            power -= 2;
                            lmax -= 2;
                        }
                    }

                    if (((uint)mant & 1) == 0 && lmax >= 1)
                    {
                        (ulong div, uint rem) = DivRem64By32(mant, 10);
                        if (rem == 0)
                        {
                            mant = div;
                            power--;
                        }
                    }

                    flags |= (uint)power << ScaleShift;
                    result.Low64 = mant;
                }

                result.uflags = flags;
            }

            /// <summary>
            /// Convert Decimal to float
            /// </summary>
            internal static float VarR4FromDec(in decimal value)
            {
                return (float)VarR8FromDec(in value);
            }

            /// <summary>
            /// Convert Decimal to double
            /// </summary>
            internal static double VarR8FromDec(in decimal value)
            {
                // Value taken via reverse engineering the double that corresponds to 2^64. (oleaut32 has ds2to64 = DEFDS(0, 0, DBLBIAS + 65, 0))
                const double ds2to64 = 1.8446744073709552e+019;

                double dbl = ((double)value.Low64 +
                    (double)value.High * ds2to64) / DoublePowers10[value.Scale];

                if (decimal.IsNegative(value))
                    dbl = -dbl;

                return dbl;
            }

            internal static int GetHashCode(in decimal d)
            {
                if ((d.Low64 | d.High) == 0)
                    return 0;

                uint flags = (uint)d._flags;
                if ((flags & ScaleMask) == 0 || (d.Low & 1) != 0)
                    return (int)(flags ^ d.High ^ d.Mid ^ d.Low);

                int scale = (byte)(flags >> ScaleShift);
                uint low = d.Low;
                ulong high64 = ((ulong)d.High << 32) | d.Mid;

                Unscale(ref low, ref high64, ref scale);

                flags = (flags & ~ScaleMask) | (uint)scale << ScaleShift;
                return (int)(flags ^ (uint)(high64 >> 32) ^ (uint)high64 ^ low);
            }

            /// <summary>
            /// Divides two decimal values.
            /// On return, d1 contains the result of the operation.
            /// </summary>
            internal static unsafe void VarDecDiv(ref DecCalc d1, ref DecCalc d2)
            {
                Unsafe.SkipInit(out Buf12 bufQuo);

                uint power;
                int curScale;

                int scale = (sbyte)(d1.uflags - d2.uflags >> ScaleShift);
                bool unscale = false;
                uint tmp;

                if ((d2.High | d2.Mid) == 0)
                {
                    // Divisor is only 32 bits.  Easy divide.
                    //
                    uint den = d2.Low;
                    if (den == 0)
                        throw new DivideByZeroException();

                    bufQuo.Low64 = d1.Low64;
                    bufQuo.U2 = d1.High;
                    uint remainder = Div96By32(ref bufQuo, den);

                    while (true)
                    {
                        if (remainder == 0)
                        {
                            if (scale < 0)
                            {
                                curScale = Math.Min(9, -scale);
                                goto HaveScale;
                            }
                            break;
                        }

                        // We need to unscale if and only if we have a non-zero remainder
                        unscale = true;

                        // We have computed a quotient based on the natural scale
                        // ( <dividend scale> - <divisor scale> ).  We have a non-zero
                        // remainder, so now we should increase the scale if possible to
                        // include more quotient bits.
                        //
                        // If it doesn't cause overflow, we'll loop scaling by 10^9 and
                        // computing more quotient bits as long as the remainder stays
                        // non-zero.  If scaling by that much would cause overflow, we'll
                        // drop out of the loop and scale by as much as we can.
                        //
                        // Scaling by 10^9 will overflow if bufQuo[2].bufQuo[1] >= 2^32 / 10^9
                        // = 4.294 967 296.  So the upper limit is bufQuo[2] == 4 and
                        // bufQuo[1] == 0.294 967 296 * 2^32 = 1,266,874,889.7+.  Since
                        // quotient bits in bufQuo[0] could be all 1's, then 1,266,874,888
                        // is the largest value in bufQuo[1] (when bufQuo[2] == 4) that is
                        // assured not to overflow.
                        //
                        if (scale == DEC_SCALE_MAX || (curScale = SearchScale(ref bufQuo, scale)) == 0)
                        {
                            // No more scaling to be done, but remainder is non-zero.
                            // Round quotient.
                            //
                            tmp = remainder << 1;
                            if (tmp < remainder || tmp >= den && (tmp > den || (bufQuo.U0 & 1) != 0))
                                goto RoundUp;
                            break;
                        }

                        HaveScale:
                        power = UInt32Powers10[curScale];
                        scale += curScale;

                        if (IncreaseScale(ref bufQuo, power) != 0)
                            goto ThrowOverflow;

                        ulong num = Math.BigMul(remainder, power);
                        (uint div, remainder) = DivRem64By32Quo32(num, den);

                        if (!Add32To96(ref bufQuo, div))
                        {
                            scale = OverflowUnscale(ref bufQuo, scale, remainder != 0);
                            break;
                        }
                    } // while (true)
                }
                else
                {
                    // Divisor has bits set in the upper 64 bits.
                    //
                    // Divisor must be fully normalized (shifted so bit 31 of the most
                    // significant uint is 1).  Locate the MSB so we know how much to
                    // normalize by.  The dividend will be shifted by the same amount so
                    // the quotient is not changed.
                    //
                    tmp = d2.High;
                    if (tmp == 0)
                        tmp = d2.Mid;

                    curScale = BitOperations.LeadingZeroCount(tmp);

                    // Shift both dividend and divisor left by curScale.
                    //
                    Unsafe.SkipInit(out Buf16 bufRem);

                    bufRem.Low64 = d1.Low64 << curScale;
                    bufRem.High64 = (d1.Mid | ((ulong)d1.High << 32)) >> (32 - curScale);

                    ulong divisor = d2.Low64 << curScale;

                    if (d2.High == 0)
                    {
                        // Have a 64-bit divisor in sdlDivisor.  The remainder
                        // (currently 96 bits spread over 4 uints) will be < divisor.
                        //
                        bufQuo.U2 = 0;
                        bufQuo.U1 = Div96By64(ref *(Buf12*)&bufRem.U1, divisor);
                        bufQuo.U0 = Div96By64(ref *(Buf12*)&bufRem, divisor);

                        while (true)
                        {
                            if (bufRem.Low64 == 0)
                            {
                                if (scale < 0)
                                {
                                    curScale = Math.Min(9, -scale);
                                    goto HaveScale64;
                                }
                                break;
                            }

                            // We need to unscale if and only if we have a non-zero remainder
                            unscale = true;

                            // Remainder is non-zero.  Scale up quotient and remainder by
                            // powers of 10 so we can compute more significant bits.
                            //
                            if (scale == DEC_SCALE_MAX || (curScale = SearchScale(ref bufQuo, scale)) == 0)
                            {
                                // No more scaling to be done, but remainder is non-zero.
                                // Round quotient.
                                //
                                ulong tmp64 = bufRem.Low64;
                                if ((long)tmp64 < 0 || (tmp64 <<= 1) > divisor ||
                                  (tmp64 == divisor && (bufQuo.U0 & 1) != 0))
                                    goto RoundUp;
                                break;
                            }

                            HaveScale64:
                            power = UInt32Powers10[curScale];
                            scale += curScale;

                            if (IncreaseScale(ref bufQuo, power) != 0)
                                goto ThrowOverflow;

                            IncreaseScale64(ref *(Buf12*)&bufRem, power);
                            tmp = Div96By64(ref *(Buf12*)&bufRem, divisor);
                            if (!Add32To96(ref bufQuo, tmp))
                            {
                                scale = OverflowUnscale(ref bufQuo, scale, bufRem.Low64 != 0);
                                break;
                            }
                        } // while (true)
                    }
                    else
                    {
                        // Have a 96-bit divisor in bufDivisor.
                        //
                        // Start by finishing the shift left by curScale.
                        //
                        Unsafe.SkipInit(out Buf12 bufDivisor);

                        bufDivisor.Low64 = divisor;
                        bufDivisor.U2 = (uint)((d2.Mid | ((ulong)d2.High << 32)) >> (32 - curScale));

                        // The remainder (currently 96 bits spread over 4 uints) will be < divisor.
                        //
                        bufQuo.Low64 = Div128By96(ref bufRem, ref bufDivisor);
                        bufQuo.U2 = 0;

                        while (true)
                        {
                            if ((bufRem.Low64 | bufRem.U2) == 0)
                            {
                                if (scale < 0)
                                {
                                    curScale = Math.Min(9, -scale);
                                    goto HaveScale96;
                                }
                                break;
                            }

                            // We need to unscale if and only if we have a non-zero remainder
                            unscale = true;

                            // Remainder is non-zero.  Scale up quotient and remainder by
                            // powers of 10 so we can compute more significant bits.
                            //
                            if (scale == DEC_SCALE_MAX || (curScale = SearchScale(ref bufQuo, scale)) == 0)
                            {
                                // No more scaling to be done, but remainder is non-zero.
                                // Round quotient.
                                //
                                if ((int)bufRem.U2 < 0)
                                {
                                    goto RoundUp;
                                }

                                tmp = bufRem.U1 >> 31;
                                bufRem.Low64 <<= 1;
                                bufRem.U2 = (bufRem.U2 << 1) + tmp;

                                if (bufRem.U2 > bufDivisor.U2 || bufRem.U2 == bufDivisor.U2 &&
                                  (bufRem.Low64 > bufDivisor.Low64 || bufRem.Low64 == bufDivisor.Low64 &&
                                  (bufQuo.U0 & 1) != 0))
                                    goto RoundUp;
                                break;
                            }

                            HaveScale96:
                            power = UInt32Powers10[curScale];
                            scale += curScale;

                            if (IncreaseScale(ref bufQuo, power) != 0)
                                goto ThrowOverflow;

                            bufRem.U3 = IncreaseScale(ref *(Buf12*)&bufRem, power);
                            tmp = Div128By96(ref bufRem, ref bufDivisor);
                            if (!Add32To96(ref bufQuo, tmp))
                            {
                                scale = OverflowUnscale(ref bufQuo, scale, (bufRem.Low64 | bufRem.High64) != 0);
                                break;
                            }
                        } // while (true)
                    }
                }

Unscale:
                if (unscale)
                {
                    uint low = bufQuo.U0;
                    ulong high64 = bufQuo.High64;
                    Unscale(ref low, ref high64, ref scale);
                    d1.Low = low;
                    d1.Mid = (uint)high64;
                    d1.High = (uint)(high64 >> 32);
                }
                else
                {
                    d1.Low64 = bufQuo.Low64;
                    d1.High = bufQuo.U2;
                }

                d1.uflags = ((d1.uflags ^ d2.uflags) & SignMask) | ((uint)scale << ScaleShift);
                return;

RoundUp:
                {
                    if (++bufQuo.Low64 == 0 && ++bufQuo.U2 == 0)
                    {
                        scale = OverflowUnscale(ref bufQuo, scale, true);
                    }
                    goto Unscale;
                }

ThrowOverflow:
                Number.ThrowOverflowException(SR.Overflow_Decimal);
            }

            /// <summary>
            /// Computes the remainder between two decimals.
            /// On return, d1 contains the result of the operation and d2 is trashed.
            /// </summary>
            internal static void VarDecMod(ref DecCalc d1, ref DecCalc d2)
            {
                if ((d2.ulo | d2.umid | d2.uhi) == 0)
                    throw new DivideByZeroException();

                if ((d1.ulo | d1.umid | d1.uhi) == 0)
                    return;

                // In the operation x % y the sign of y does not matter. Result will have the sign of x.
                d2.uflags = (d2.uflags & ~SignMask) | (d1.uflags & SignMask);

                int cmp = VarDecCmpSub(in Unsafe.As<DecCalc, decimal>(ref d1), in Unsafe.As<DecCalc, decimal>(ref d2));
                if (cmp == 0)
                {
                    d1.ulo = 0;
                    d1.umid = 0;
                    d1.uhi = 0;
                    if (d2.uflags > d1.uflags)
                        d1.uflags = d2.uflags;
                    return;
                }
                if ((cmp ^ (int)(d1.uflags & SignMask)) < 0)
                    return;

                // The divisor is smaller than the dividend and both are non-zero. Calculate the integer remainder using the larger scaling factor.

                int scale = (sbyte)(d1.uflags - d2.uflags >> ScaleShift);
                if (scale > 0)
                {
                    // Divisor scale can always be increased to dividend scale for remainder calculation.
                    do
                    {
                        uint power = scale >= MaxInt32Scale ? TenToPowerNine : UInt32Powers10[scale];
                        ulong tmp = Math.BigMul(d2.Low, power);
                        d2.Low = (uint)tmp;
                        tmp >>= 32;
                        tmp += (d2.Mid | ((ulong)d2.High << 32)) * power;
                        d2.Mid = (uint)tmp;
                        d2.High = (uint)(tmp >> 32);
                    } while ((scale -= MaxInt32Scale) > 0);
                    scale = 0;
                }

                do
                {
                    if (scale < 0)
                    {
                        d1.uflags = d2.uflags;
                        // Try to scale up dividend to match divisor.
                        Unsafe.SkipInit(out Buf12 bufQuo);

                        bufQuo.Low64 = d1.Low64;
                        bufQuo.U2 = d1.High;
                        do
                        {
                            int iCurScale = SearchScale(ref bufQuo, DEC_SCALE_MAX + scale);
                            if (iCurScale == 0)
                                break;
                            uint power = iCurScale >= MaxInt32Scale ? TenToPowerNine : UInt32Powers10[iCurScale];
                            scale += iCurScale;
                            ulong tmp = Math.BigMul(bufQuo.U0, power);
                            bufQuo.U0 = (uint)tmp;
                            tmp >>= 32;
                            bufQuo.High64 = tmp + bufQuo.High64 * power;
                            if (power != TenToPowerNine)
                                break;
                        }
                        while (scale < 0);
                        d1.Low64 = bufQuo.Low64;
                        d1.High = bufQuo.U2;
                    }

                    if (d1.High == 0)
                    {
                        Debug.Assert(d2.High == 0);
                        Debug.Assert(scale == 0);
                        d1.Low64 %= d2.Low64;
                        return;
                    }
                    else if ((d2.High | d2.Mid) == 0)
                    {
                        uint den = d2.Low;
                        ulong tmp = ((ulong)d1.High << 32) | d1.Mid;
                        tmp = ((tmp % den) << 32) | d1.Low;
                        d1.Low64 = tmp % den;
                        d1.High = 0;
                    }
                    else
                    {
                        VarDecModFull(ref d1, ref d2, scale);
                        return;
                    }
                } while (scale < 0);
            }

            private static unsafe void VarDecModFull(ref DecCalc d1, ref DecCalc d2, int scale)
            {
                // Divisor has bits set in the upper 64 bits.
                //
                // Divisor must be fully normalized (shifted so bit 31 of the most significant uint is 1).
                // Locate the MSB so we know how much to normalize by.
                // The dividend will be shifted by the same amount so the quotient is not changed.
                //
                uint tmp = d2.High;
                if (tmp == 0)
                    tmp = d2.Mid;
                int shift = BitOperations.LeadingZeroCount(tmp);

                Unsafe.SkipInit(out Buf28 b);

                b.Buf24.Low64 = d1.Low64 << shift;
                b.Buf24.Mid64 = (d1.Mid + ((ulong)d1.High << 32)) >> (32 - shift);

                // The dividend might need to be scaled up to 221 significant bits.
                // Maximum scaling is required when the divisor is 2^64 with scale 28 and is left shifted 31 bits
                // and the dividend is decimal.MaxValue: (2^96 - 1) * 10^28 << 31 = 221 bits.
                uint high = 3;
                while (scale < 0)
                {
                    uint power = scale <= -MaxInt32Scale ? TenToPowerNine : UInt32Powers10[-scale];
                    uint* buf = (uint*)&b;
                    ulong tmp64 = Math.BigMul(b.Buf24.U0, power);
                    b.Buf24.U0 = (uint)tmp64;
                    for (int i = 1; i <= high; i++)
                    {
                        tmp64 >>= 32;
                        tmp64 += Math.BigMul(buf[i], power);
                        buf[i] = (uint)tmp64;
                    }
                    // The high bit of the dividend must not be set.
                    if (tmp64 > int.MaxValue)
                    {
                        Debug.Assert(high + 1 < Buf28.Length);
                        buf[++high] = (uint)(tmp64 >> 32);
                    }

                    scale += MaxInt32Scale;
                }

                if (d2.High == 0)
                {
                    ulong divisor = d2.Low64 << shift;
                    switch (high)
                    {
                        case 6:
                            Div96By64(ref *(Buf12*)&b.Buf24.U4, divisor);
                            goto case 5;
                        case 5:
                            Div96By64(ref *(Buf12*)&b.Buf24.U3, divisor);
                            goto case 4;
                        case 4:
                            Div96By64(ref *(Buf12*)&b.Buf24.U2, divisor);
                            break;
                    }
                    Div96By64(ref *(Buf12*)&b.Buf24.U1, divisor);
                    Div96By64(ref *(Buf12*)&b, divisor);

                    d1.Low64 = b.Buf24.Low64 >> shift;
                    d1.High = 0;
                }
                else
                {
                    Unsafe.SkipInit(out Buf12 bufDivisor);

                    bufDivisor.Low64 = d2.Low64 << shift;
                    bufDivisor.U2 = (uint)((d2.Mid + ((ulong)d2.High << 32)) >> (32 - shift));

                    switch (high)
                    {
                        case 6:
                            Div128By96(ref *(Buf16*)&b.Buf24.U3, ref bufDivisor);
                            goto case 5;
                        case 5:
                            Div128By96(ref *(Buf16*)&b.Buf24.U2, ref bufDivisor);
                            goto case 4;
                        case 4:
                            Div128By96(ref *(Buf16*)&b.Buf24.U1, ref bufDivisor);
                            break;
                    }
                    Div128By96(ref *(Buf16*)&b, ref bufDivisor);

                    d1.Low64 = (b.Buf24.Low64 >> shift) + ((ulong)b.Buf24.U2 << (32 - shift) << 32);
                    d1.High = b.Buf24.U2 >> shift;
                }
            }

            // Does an in-place round by the specified scale
            internal static void InternalRound(ref DecCalc d, uint scale, MidpointRounding mode)
            {
                // the scale becomes the desired decimal count
                d.uflags -= scale << ScaleShift;

                uint remainder, sticky = 0, power;
                // First divide the value by constant 10^9 up to three times
                while (scale >= MaxInt32Scale)
                {
                    scale -= MaxInt32Scale;

                    const uint divisor = TenToPowerNine;
                    uint n = d.uhi;
                    if (n == 0)
                    {
                        (d.Low64, remainder) = DivRem64By32(d.Low64, divisor);
                    }
                    else
                    {
                        (d.uhi, remainder) = Math.DivRem(n, divisor);
                        n = d.umid;
                        if ((n | remainder) != 0)
                            (d.umid, remainder) = DivRem32Carry32By32(n, remainder, divisor);
                        n = d.ulo;
                        if ((n | remainder) != 0)
                            (d.ulo, remainder) = DivRem32Carry32By32(n, remainder, divisor);
                    }
                    power = divisor;
                    if (scale == 0)
                        goto checkRemainder;
                    sticky |= remainder;
                }

                {
                    power = UInt32Powers10[(int)scale];
                    uint n = d.uhi;
                    if (n == 0)
                    {
                        ulong tmp = d.Low64;
                        if (tmp == 0)
                        {
                            if (mode <= MidpointRounding.ToZero)
                                goto done;
                            remainder = 0;
                            goto checkRemainder;
                        }
                        (d.Low64, remainder) = DivRem64By32(tmp, power);
                    }
                    else
                    {
                        (d.uhi, remainder) = Math.DivRem(n, power);
                        n = d.umid;
                        if ((n | remainder) != 0)
                            (d.umid, remainder) = DivRem32Carry32By32(n, remainder, power);
                        n = d.ulo;
                        if ((n | remainder) != 0)
                            (d.ulo, remainder) = DivRem32Carry32By32(n, remainder, power);
                    }
                }

checkRemainder:
                if (mode == MidpointRounding.ToZero)
                    goto done;
                else if (mode == MidpointRounding.ToEven)
                {
                    // To do IEEE rounding, we add LSB of result to sticky bits so either causes round up if remainder * 2 == last divisor.
                    remainder <<= 1;
                    if ((sticky | d.ulo & 1) != 0)
                        remainder++;
                    if (power >= remainder)
                        goto done;
                }
                else if (mode == MidpointRounding.AwayFromZero)
                {
                    // Round away from zero at the mid point.
                    remainder <<= 1;
                    if (power > remainder)
                        goto done;
                }
                else if (mode == MidpointRounding.ToNegativeInfinity)
                {
                    // Round toward -infinity if we have chopped off a non-zero amount from a negative value.
                    if ((remainder | sticky) == 0 || !d.IsNegative)
                        goto done;
                }
                else
                {
                    Debug.Assert(mode == MidpointRounding.ToPositiveInfinity);
                    // Round toward infinity if we have chopped off a non-zero amount from a positive value.
                    if ((remainder | sticky) == 0 || d.IsNegative)
                        goto done;
                }
                if (++d.Low64 == 0)
                    d.uhi++;
done:
                return;
            }

            internal static uint DecDivMod1E9(ref DecCalc value)
            {
                ulong high64 = ((ulong)value.uhi << 32) | value.umid;
                (ulong div64, uint rem) = DivRem64By32(high64, TenToPowerNine);
                value.uhi = (uint)(div64 >> 32);
                value.umid = (uint)div64;
                (value.ulo, rem) = DivRem32Carry32By32(value.ulo, rem, TenToPowerNine);
                return rem;
            }

            private readonly struct PowerOvfl
            {
                public readonly uint Hi;
                public readonly ulong MidLo;

                public PowerOvfl(uint hi, uint mid, uint lo)
                {
                    Hi = hi;
                    MidLo = ((ulong)mid << 32) + lo;
                }
            }

            private static readonly PowerOvfl[] PowerOvflValues = new[]
            {
                // This is a table of the largest values that can be in the upper two
                // uints of a 96-bit number that will not overflow when multiplied
                // by a given power.  For the upper word, this is a table of
                // 2^32 / 10^n for 1 <= n <= 8.  For the lower word, this is the
                // remaining fraction part * 2^32.  2^32 = 4294967296.
                //
                new PowerOvfl(429496729, 2576980377, 2576980377),  // 10^1 remainder 0.6
                new PowerOvfl(42949672,  4123168604, 687194767),   // 10^2 remainder 0.16
                new PowerOvfl(4294967,   1271310319, 2645699854),  // 10^3 remainder 0.616
                new PowerOvfl(429496,    3133608139, 694066715),   // 10^4 remainder 0.1616
                new PowerOvfl(42949,     2890341191, 2216890319),  // 10^5 remainder 0.51616
                new PowerOvfl(4294,      4154504685, 2369172679),  // 10^6 remainder 0.551616
                new PowerOvfl(429,       2133437386, 4102387834),  // 10^7 remainder 0.9551616
                new PowerOvfl(42,        4078814305, 410238783),   // 10^8 remainder 0.09991616
            };

            [StructLayout(LayoutKind.Explicit)]
            private struct Buf12
            {
                [FieldOffset(0 * 4)]
                public uint U0;
                [FieldOffset(1 * 4)]
                public uint U1;
                [FieldOffset(2 * 4)]
                public uint U2;

                [FieldOffset(0)]
                private ulong ulo64LE;
                [FieldOffset(4)]
                private ulong uhigh64LE;

                public ulong Low64
                {
#if BIGENDIAN
                    get => ((ulong)U1 << 32) | U0;
                    set { U1 = (uint)(value >> 32); U0 = (uint)value; }
#else
                    get => ulo64LE;
                    set => ulo64LE = value;
#endif
                }

                /// <summary>
                /// U1-U2 combined (overlaps with Low64)
                /// </summary>
                public ulong High64
                {
#if BIGENDIAN
                    get => ((ulong)U2 << 32) | U1;
                    set { U2 = (uint)(value >> 32); U1 = (uint)value; }
#else
                    get => uhigh64LE;
                    set => uhigh64LE = value;
#endif
                }
            }

            [StructLayout(LayoutKind.Explicit)]
            private struct Buf16
            {
                [FieldOffset(0 * 4)]
                public uint U0;
                [FieldOffset(1 * 4)]
                public uint U1;
                [FieldOffset(2 * 4)]
                public uint U2;
                [FieldOffset(3 * 4)]
                public uint U3;

                [FieldOffset(0 * 8)]
                private ulong ulo64LE;
                [FieldOffset(1 * 8)]
                private ulong uhigh64LE;

                public ulong Low64
                {
#if BIGENDIAN
                    get => ((ulong)U1 << 32) | U0;
                    set { U1 = (uint)(value >> 32); U0 = (uint)value; }
#else
                    get => ulo64LE;
                    set => ulo64LE = value;
#endif
                }

                public ulong High64
                {
#if BIGENDIAN
                    get => ((ulong)U3 << 32) | U2;
                    set { U3 = (uint)(value >> 32); U2 = (uint)value; }
#else
                    get => uhigh64LE;
                    set => uhigh64LE = value;
#endif
                }
            }

            [StructLayout(LayoutKind.Explicit)]
            private struct Buf24
            {
                [FieldOffset(0 * 4)]
                public uint U0;
                [FieldOffset(1 * 4)]
                public uint U1;
                [FieldOffset(2 * 4)]
                public uint U2;
                [FieldOffset(3 * 4)]
                public uint U3;
                [FieldOffset(4 * 4)]
                public uint U4;
                [FieldOffset(5 * 4)]
                public uint U5;

                [FieldOffset(0 * 8)]
                private ulong ulo64LE;
                [FieldOffset(1 * 8)]
                private ulong umid64LE;
                [FieldOffset(2 * 8)]
                private ulong uhigh64LE;

                public ulong Low64
                {
#if BIGENDIAN
                    get => ((ulong)U1 << 32) | U0;
                    set { U1 = (uint)(value >> 32); U0 = (uint)value; }
#else
                    get => ulo64LE;
                    set => ulo64LE = value;
#endif
                }

                public ulong Mid64
                {
#if BIGENDIAN
                    get => ((ulong)U3 << 32) | U2;
                    set { U3 = (uint)(value >> 32); U2 = (uint)value; }
#else
                    get => umid64LE;
                    set => umid64LE = value;
#endif
                }

                public ulong High64
                {
#if BIGENDIAN
                    get => ((ulong)U5 << 32) | U4;
                    set { U5 = (uint)(value >> 32); U4 = (uint)value; }
#else
                    get => uhigh64LE;
                    set => uhigh64LE = value;
#endif
                }

                public const int Length = 6;
            }

            private struct Buf28
            {
                public Buf24 Buf24;
                public uint U6;

                public const int Length = 7;
            }
        }
    }
}
