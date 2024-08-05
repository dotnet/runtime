// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Formats.Cbor
{
    internal static partial class HalfHelpers
    {
        // Half constants
        private const ushort HalfExponentMask = 0x7C00;
        private const ushort HalfExponentShift = 10;
        private const ushort HalfSignShift = 15;
        private const ushort HalfPositiveInfinityBits = 0x7C00;
        private const ushort HalfNegativeInfinityBits = 0xFC00;

        // Float constants
        private const uint FloatExponentMask = 0x7F80_0000;
        private const int FloatExponentShift = 23;
        private const uint FloatSignificandMask = 0x007F_FFFF;
        private const uint FloatSignMask = 0x8000_0000;
        private const int FloatSignShift = 31;

        #region From Half and related helpers.
        // Instead of copying (yet again) the explicit operator,
        // we perform a Half to float to double conversion.
        public static double HalfToDouble(ushort value)
            => (double)HalfToFloat(value);

        public static unsafe float HalfToFloat(ushort value)
        {
            const ushort ExponentMask = 0x7C00;
            const ushort ExponentShift = 10;
            const ushort SignificandMask = 0x03FF;
            const ushort SignificandShift = 0;
            const ushort MaxExponent = 0x1F;

            bool sign = (short)value < 0;
            int exp = (sbyte)((value & ExponentMask) >> ExponentShift);
            uint sig = (ushort)((value & SignificandMask) >> SignificandShift);

            if (exp == MaxExponent)
            {
                if (sig != 0)
                {
                    return CreateSingleNaN(sign, (ulong)sig << 54);
                }
                return sign ? float.NegativeInfinity : float.PositiveInfinity;
            }

            if (exp == 0)
            {
                if (sig == 0)
                {
                    return CborHelpers.UInt32BitsToSingle(sign ? FloatSignMask : 0); // Positive / Negative zero
                }
                (exp, sig) = NormSubnormalF16Sig(sig);
                exp -= 1;
            }

            return CreateSingle(sign, (byte)(exp + 0x70), sig << 13);

            static float CreateSingle(bool sign, byte exp, uint sig)
                => CborHelpers.Int32BitsToSingle((int)(((sign ? 1U : 0U) << FloatSignShift) + ((uint)exp << FloatExponentShift) + sig));
        }

        public static bool HalfIsNaN(ushort value)
        {
            return (value & ~((ushort)1 << HalfSignShift)) > HalfPositiveInfinityBits;
        }

        private static (int Exp, uint Sig) NormSubnormalF16Sig(uint sig)
        {
            int shiftDist = LeadingZeroCount(sig) - 16 - 5;
            return (1 - shiftDist, sig << shiftDist);
        }

        public static int LeadingZeroCount(uint value)
        {
            // Unguarded fallback contract is 0->31, BSR contract is 0->undefined
            if (value == 0)
            {
                return 32;
            }

            return 31 ^ Log2SoftwareFallback(value);
        }

        private static ReadOnlySpan<byte> Log2DeBruijn => // 32
        [
            00, 09, 01, 10, 13, 21, 02, 29,
            11, 14, 16, 18, 22, 25, 03, 30,
            08, 12, 20, 28, 15, 17, 24, 07,
            19, 27, 23, 06, 26, 05, 04, 31
        ];

        private static int Log2SoftwareFallback(uint value)
        {
            // No AggressiveInlining due to large method size
            // Has conventional contract 0->0 (Log(0) is undefined)

            // Fill trailing zeros with ones, eg 00010010 becomes 00011111
            value |= value >> 01;
            value |= value >> 02;
            value |= value >> 04;
            value |= value >> 08;
            value |= value >> 16;

            // uint.MaxValue >> 27 is always in range [0 - 31] so we use Unsafe.AddByteOffset to avoid bounds check
            return Unsafe.AddByteOffset(
                // Using deBruijn sequence, k=2, n=5 (2^5=32) : 0b_0000_0111_1100_0100_1010_1100_1101_1101u
                ref MemoryMarshal.GetReference(Log2DeBruijn),
                // uint|long -> IntPtr cast on 32-bit platforms does expensive overflow checks not needed here
                (IntPtr)(int)((value * 0x07C4ACDDu) >> 27));
        }

        private static float CreateSingleNaN(bool sign, ulong significand)
        {
            const uint NaNBits = FloatExponentMask | 0x400000; // Most significant significand bit

            uint signInt = (sign ? 1U : 0U) << FloatSignShift;
            uint sigInt = (uint)(significand >> 41);

            return CborHelpers.UInt32BitsToSingle(signInt | NaNBits | sigInt);
        }
        #endregion

        #region To Half and related helpers.
        public static ushort FloatToHalf(float value)
        {
            const int SingleMaxExponent = 0xFF;

            uint floatInt = CborHelpers.SingleToUInt32Bits(value);
            bool sign = (floatInt & FloatSignMask) >> FloatSignShift != 0;
            int exp = (int)(floatInt & FloatExponentMask) >> FloatExponentShift;
            uint sig = floatInt & FloatSignificandMask;

            if (exp == SingleMaxExponent)
            {
                if (sig != 0) // NaN
                {
                    return CreateHalfNaN(sign, (ulong)sig << 41); // Shift the significand bits to the left end
                }
                return sign ? HalfNegativeInfinityBits : HalfPositiveInfinityBits;
            }

            uint sigHalf = sig >> 9 | ((sig & 0x1FFU) != 0 ? 1U : 0U); // RightShiftJam

            if ((exp | (int)sigHalf) == 0)
            {
                return HalfCtor(sign, 0, 0);
            }

            return RoundPackToHalf(sign, (short)(exp - 0x71), (ushort)(sigHalf | 0x4000));
        }

        private static ushort CreateHalfNaN(bool sign, ulong significand)
        {
            const uint NaNBits = HalfExponentMask | 0x200; // Most significant significand bit

            uint signInt = (sign ? 1U : 0U) << HalfSignShift;
            uint sigInt = (uint)(significand >> 54);

            return (ushort)(signInt | NaNBits | sigInt);
        }

        private static ushort HalfCtor(bool sign, ushort exp, ushort sig)
            => (ushort)(((sign ? 1 : 0) << HalfSignShift) + (exp << HalfExponentShift) + sig);

        private static ushort RoundPackToHalf(bool sign, short exp, ushort sig)
        {
            const int RoundIncrement = 0x8; // Depends on rounding mode but it's always towards closest / ties to even
            int roundBits = sig & 0xF;

            if ((uint)exp >= 0x1D)
            {
                if (exp < 0)
                {
                    sig = (ushort)ShiftRightJam(sig, -exp);
                    exp = 0;
                    roundBits = sig & 0xF;
                }
                else if (exp > 0x1D || sig + RoundIncrement >= 0x8000) // Overflow
                {
                    return sign ? HalfNegativeInfinityBits : HalfPositiveInfinityBits;
                }
            }

            sig = (ushort)((sig + RoundIncrement) >> 4);
            sig &= (ushort)~(((roundBits ^ 8) != 0 ? 0 : 1) & 1);

            if (sig == 0)
            {
                exp = 0;
            }

            return HalfCtor(sign, (ushort)exp, sig);
        }

        // If any bits are lost by shifting, "jam" them into the LSB.
        // if dist > bit count, Will be 1 or 0 depending on i
        // (unlike bitwise operators that masks the lower 5 bits)
        private static uint ShiftRightJam(uint i, int dist)
            => dist < 31 ? (i >> dist) | (i << (-dist & 31) != 0 ? 1U : 0U) : (i != 0 ? 1U : 0U);
        #endregion
    }
}
