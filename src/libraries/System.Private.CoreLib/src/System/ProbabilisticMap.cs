// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    /// <summary>Data structure used to optimize checks for whether a char is in a set of chars.</summary>
    /// <remarks>
    /// Like a Bloom filter, the idea is to create a bit map of the characters we are
    /// searching for and use this map as a "cheap" check to decide if the current
    /// character in the string exists in the array of input characters. There are
    /// 256 bits in the map, with each character mapped to 2 bits. Every character is
    /// divided into 2 bytes, and then every byte is mapped to 1 bit. The character map
    /// is an array of 8 integers acting as map blocks. The 3 lsb in each byte in the
    /// character is used to index into this map to get the right block, the value of
    /// the remaining 5 msb are used as the bit position inside this block.
    /// </remarks>
    [StructLayout(LayoutKind.Explicit, Size = Size * sizeof(uint))]
    internal struct ProbabilisticMap
    {
        private const int Size = 0x8;
        private const int IndexMask = 0x7;
        private const int IndexShift = 0x3;

        /// <summary>Initializes the map based on the specified values.</summary>
        /// <param name="charMap">A pointer to the beginning of a <see cref="ProbabilisticMap"/>.</param>
        /// <param name="values">The values to set in the map.</param>
        public static unsafe void Initialize(uint* charMap, ReadOnlySpan<char> values)
        {
#if DEBUG
            for (int i = 0; i < Size; i++)
            {
                Debug.Assert(charMap[i] == 0, "Expected charMap to be zero-initialized.");
            }
#endif
            bool hasAscii = false;
            uint* charMapLocal = charMap; // https://github.com/dotnet/runtime/issues/9040

            for (int i = 0; i < values.Length; ++i)
            {
                int c = values[i];

                // Map low bit
                SetCharBit(charMapLocal, (byte)c);

                // Map high bit
                c >>= 8;

                if (c == 0)
                {
                    hasAscii = true;
                }
                else
                {
                    SetCharBit(charMapLocal, (byte)c);
                }
            }

            if (hasAscii)
            {
                // Common to search for ASCII symbols. Just set the high value once.
                charMapLocal[0] |= 1u;
            }
        }

        public static unsafe bool IsCharBitSet(uint* charMap, byte value) =>
            (charMap[(uint)value & IndexMask] & (1u << (value >> IndexShift))) != 0;

        private static unsafe void SetCharBit(uint* charMap, byte value) =>
            charMap[(uint)value & IndexMask] |= 1u << (value >> IndexShift);

        /// <summary>Determines whether <paramref name="searchChar"/> is in <paramref name="span"/>.</summary>
        /// <remarks>
        /// <see cref="MemoryExtensions.Contains{T}(ReadOnlySpan{T}, T)"/> could be used, but it's optimized
        /// for longer spans, whereas typical usage here expects a relatively small number of items in the span.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SpanContains(ReadOnlySpan<char> span, char searchChar)
        {
            for (int i = 0; i < span.Length; i++)
            {
                if (span[i] == searchChar)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
