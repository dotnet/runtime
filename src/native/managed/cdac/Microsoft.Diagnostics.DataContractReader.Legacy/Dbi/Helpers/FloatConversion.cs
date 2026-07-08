// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

internal static class FloatConversion
{
    // Convert an x87 80-bit double-extended value to IEEE-754 binary64 with
    // round-to-nearest-even, matching the hardware FSTP conversion the legacy
    // x86 path used. Layout (Intel SDM Vol 1 sec 8.2.2): bytes 0-7 = 64-bit
    // significand with an explicit integer bit at 63; bytes 8-9 = 15-bit biased
    // exponent (bias 16383) in bits 0-14 and the sign in bit 15.
    public static double X87ExtendedToDouble(ReadOnlySpan<byte> slot10)
    {
        ulong significand = BinaryPrimitives.ReadUInt64LittleEndian(slot10);
        ushort signExp = BinaryPrimitives.ReadUInt16LittleEndian(slot10.Slice(8));

        ulong sign = (ulong)((signExp >> 15) & 1) << 63;
        uint exp80 = (uint)(signExp & 0x7FFF);

        if (exp80 == 0x7FFF)
        {
            ulong frac = significand & ~(1UL << 63);
            if (frac == 0)
                return BitConverter.Int64BitsToDouble(unchecked((long)(sign | ((ulong)0x7FF << 52))));
            // Force a quiet NaN (keeping the high payload bits) so a signaling
            // NaN is never misencoded as an infinity.
            ulong payload = (frac >> 12) & ((1UL << 51) - 1);
            return BitConverter.Int64BitsToDouble(unchecked((long)(sign | ((ulong)0x7FF << 52) | (1UL << 51) | payload)));
        }

        // Zero, extended denormals and unnormals all fall below the binary64
        // subnormal floor (2^-1074) and map to signed zero.
        if (exp80 == 0 || (significand & (1UL << 63)) == 0)
            return BitConverter.Int64BitsToDouble(unchecked((long)sign));

        int expD = (int)exp80 - 16383 + 1023;   // pre-rounding binary64 biased exponent

        if (expD >= 0x7FF)
            return BitConverter.Int64BitsToDouble(unchecked((long)(sign | ((ulong)0x7FF << 52))));   // overflow -> infinity

        if (expD > 0)
        {
            // Normal: keep the top 52 fraction bits and round the low 11 to even.
            ulong frac = significand & ~(1UL << 63);   // 63 fraction bits
            ulong result = frac >> 11;                 // top 52 bits
            ulong discarded = frac & 0x7FF;            // low 11 bits
            if (discarded > 0x400 || (discarded == 0x400 && (result & 1) != 0))
            {
                result++;
                if ((result >> 52) != 0)               // carry out of the fraction
                {
                    result = 0;
                    expD++;
                    if (expD >= 0x7FF)
                        return BitConverter.Int64BitsToDouble(unchecked((long)(sign | ((ulong)0x7FF << 52))));
                }
            }
            return BitConverter.Int64BitsToDouble(unchecked((long)(sign | ((ulong)expD << 52) | (result & ((1UL << 52) - 1)))));
        }

        // Subnormal / underflow (expD <= 0): right-shift the significand by
        // (12 - expD) and round to nearest-even. A carry into bit 52 promotes the
        // value to the smallest normal, which the encoding produces for free.
        int shift = 12 - expD;                         // >= 12
        if (shift > 64)                                // below half the smallest subnormal
            return BitConverter.Int64BitsToDouble(unchecked((long)sign));

        ulong res;
        ulong disc;
        ulong half;
        if (shift == 64)
        {
            res = 0;
            disc = significand;
            half = 1UL << 63;
        }
        else
        {
            res = significand >> shift;
            disc = significand & ((1UL << shift) - 1);
            half = 1UL << (shift - 1);
        }
        if (disc > half || (disc == half && (res & 1) != 0))
            res++;
        return BitConverter.Int64BitsToDouble(unchecked((long)(sign | res)));
    }
}
