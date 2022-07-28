// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
#if NETCOREAPP
using System.Numerics;
#endif

namespace System.Reflection.Internal
{
    internal static class BitArithmetic
    {
        internal static int CountBits(int v)
        {
            return CountBits(unchecked((uint)v));
        }

        internal static int CountBits(uint v)
        {
#if NETCOREAPP
            return BitOperations.PopCount(v);
#else
            unchecked
            {
                v -= ((v >> 1) & 0x55555555u);
                v = (v & 0x33333333u) + ((v >> 2) & 0x33333333u);
                return (int)((v + (v >> 4) & 0xF0F0F0Fu) * 0x1010101u) >> 24;
            }
#endif
        }

        internal static int CountBits(ulong v)
        {
#if NETCOREAPP
            return BitOperations.PopCount(v);
#else
            const ulong Mask01010101 = 0x5555555555555555UL;
            const ulong Mask00110011 = 0x3333333333333333UL;
            const ulong Mask00001111 = 0x0F0F0F0F0F0F0F0FUL;
            const ulong Mask00000001 = 0x0101010101010101UL;
            v -= ((v >> 1) & Mask01010101);
            v = (v & Mask00110011) + ((v >> 2) & Mask00110011);
            return (int)(unchecked(((v + (v >> 4)) & Mask00001111) * Mask00000001) >> 56);
#endif
        }

        internal static uint Align(uint position, uint alignment)
        {
            Debug.Assert(CountBits(alignment) == 1);

            uint result = position & ~(alignment - 1);
            if (result == position)
            {
                return result;
            }

            return result + alignment;
        }

        internal static int Align(int position, int alignment)
        {
            Debug.Assert(position >= 0 && alignment > 0);
            Debug.Assert(CountBits(alignment) == 1);

            int result = position & ~(alignment - 1);
            if (result == position)
            {
                return result;
            }

            return result + alignment;
        }
    }
}
