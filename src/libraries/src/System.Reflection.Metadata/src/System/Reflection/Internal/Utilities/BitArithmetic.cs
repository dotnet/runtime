// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Reflection.Internal
{
    internal static class BitArithmetic
    {
        internal static int CountBits(ulong v)
        {
            unchecked
            {
                const ulong MASK_01010101010101010101010101010101 = 0x5555555555555555UL;
                const ulong MASK_00110011001100110011001100110011 = 0x3333333333333333UL;
                const ulong MASK_00001111000011110000111100001111 = 0x0F0F0F0F0F0F0F0FUL;
                const ulong MASK_00000000111111110000000011111111 = 0x00FF00FF00FF00FFUL;
                const ulong MASK_00000000000000001111111111111111 = 0x0000FFFF0000FFFFUL;
                const ulong MASK_11111111111111111111111111111111 = 0x00000000FFFFFFFFUL;
                v = (v & MASK_01010101010101010101010101010101) + ((v >> 1) & MASK_01010101010101010101010101010101);
                v = (v & MASK_00110011001100110011001100110011) + ((v >> 2) & MASK_00110011001100110011001100110011);
                v = (v & MASK_00001111000011110000111100001111) + ((v >> 4) & MASK_00001111000011110000111100001111);
                v = (v & MASK_00000000111111110000000011111111) + ((v >> 8) & MASK_00000000111111110000000011111111);
                v = (v & MASK_00000000000000001111111111111111) + ((v >> 16) & MASK_00000000000000001111111111111111);
                v = (v & MASK_11111111111111111111111111111111) + ((v >> 32) & MASK_11111111111111111111111111111111);
                return (int)v;
            }
        }
    }
}
