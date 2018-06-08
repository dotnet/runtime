// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using Internal.Runtime.CompilerServices;

namespace System
{
    public partial struct Decimal
    {
        internal static uint DecDivMod1E9(ref decimal value)
        {
            return D32DivMod1E9(D32DivMod1E9(D32DivMod1E9(0,
                                                          ref Unsafe.As<int, uint>(ref value.hi)),
                                             ref Unsafe.As<int, uint>(ref value.mid)),
                                ref Unsafe.As<int, uint>(ref value.lo));

            uint D32DivMod1E9(uint hi32, ref uint lo32)
            {
                ulong n = (ulong)hi32 << 32 | lo32;
                lo32 = (uint)(n / 1000000000);
                return (uint)(n % 1000000000);
            }
        }

        private static int GetHashCode(ref decimal d)
        {
            if ((d.Low | d.Mid | d.High) == 0)
                return 0;

            uint flags = (uint)d.flags;
            if ((flags & ScaleMask) == 0 || (d.Low & 1) != 0)
                return (int)(flags ^ d.High ^ d.Mid ^ d.Low);

            int scale = (byte)(flags >> ScaleShift);
            uint low = d.Low;
            ulong high64 = ((ulong)d.High << 32) | d.Mid;

            Unscale(ref low, ref high64, ref scale);

            flags = ((flags) & ~(uint)ScaleMask) | (uint)scale << ScaleShift;
            return (int)(flags ^ (uint)(high64 >> 32) ^ (uint)high64 ^ low);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool Div96ByConst(ref ulong high64, ref uint low, uint pow)
        {
            ulong div64;
#if !BIT64
            if (high64 <= uint.MaxValue)
            {
                div64 = ((high64 << 32) | low) / pow;
                if (low == (uint)div64 * pow)
                {
                    low = (uint)div64;
                    high64 = div64 >> 32;
                    return true;
                }
                return false;
            }
#endif
            div64 = high64 / pow;
            uint div = (uint)((((high64 - (uint)div64 * pow) << 32) | low) / pow);
            if (low == div * pow)
            {
                high64 = div64;
                low = div;
                return true;
            }
            return false;
        }

        // Normalize (unscale) the number by trying to divide out 10^8, 10^4, 10^2, and 10^1.
        // If a division by one of these powers returns a zero remainder, then we keep the quotient.
        // 
        // Since 10 = 2 * 5, there must be a factor of 2 for every power of 10 we can extract. 
        // We use this as a quick test on whether to try a given power.
        // 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Unscale(ref uint low, ref ulong high64, ref int scale)
        {
            while ((byte)low == 0 && scale >= 8 && Div96ByConst(ref high64, ref low, 100000000))
                scale -= 8;

            if ((low & 0xF) == 0 && scale >= 4 && Div96ByConst(ref high64, ref low, 10000))
                scale -= 4;

            if ((low & 3) == 0 && scale >= 2 && Div96ByConst(ref high64, ref low, 100))
                scale -= 2;

            if ((low & 1) == 0 && scale >= 1 && Div96ByConst(ref high64, ref low, 10))
                scale--;
        }
    }
}
