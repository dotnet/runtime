// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Text
{
    /*
     * Contains naive unoptimized (non-SIMD) implementations of ASCII transcoding
     * operations. Vectorized methods can be substituted here as a drop-in replacement.
     */

    internal unsafe static class ASCIIUtility
    {
        [MethodImpl(MethodImplOptions.NoInlining)] // the actual implementation won't be inlined, so this shouldn't be either, lest it throw off benchmarks
        public static uint GetIndexOfFirstNonAsciiByte(byte* pBytes, uint byteCount)
        {
            uint idx = 0;
            for (; idx < byteCount; idx++)
            {
                if ((sbyte)pBytes[idx] < 0)
                {
                    break;
                }
            }
            return idx;
        }

        [MethodImpl(MethodImplOptions.NoInlining)] // the actual implementation won't be inlined, so this shouldn't be either, lest it throw off benchmarks
        public static uint GetIndexOfFirstNonAsciiChar(char* pChars, uint charCount)
        {
            uint idx = 0;
            for (; idx < charCount; idx++)
            {
                if (pChars[idx] > 0x7Fu)
                {
                    break;
                }
            }
            return idx;
        }

        [MethodImpl(MethodImplOptions.NoInlining)] // the actual implementation won't be inlined, so this shouldn't be either, lest it throw off benchmarks
        public static uint NarrowUtf16ToAscii(char* pChars, byte* pBytes, uint elementCount)
        {
            uint idx = 0;
            for (; idx < elementCount; idx++)
            {
                uint ch = pChars[idx];
                if (ch > 0x7Fu)
                {
                    break;
                }
                pBytes[idx] = (byte)ch;
            }
            return idx;
        }

        [MethodImpl(MethodImplOptions.NoInlining)] // the actual implementation won't be inlined, so this shouldn't be either, lest it throw off benchmarks
        public static uint WidenAsciiToUtf16(byte* pBytes, char* pChars, uint elementCount)
        {
            uint idx = 0;
            for (; idx < elementCount; idx++)
            {
                byte b = pBytes[idx];
                if (b > 0x7F)
                {
                    break;
                }
                pChars[idx] = (char)b;
            }
            return idx;
        }
    }
}
