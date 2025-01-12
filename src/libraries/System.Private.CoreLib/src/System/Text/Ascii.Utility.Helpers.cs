// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Text
{
#if SYSTEM_PRIVATE_CORELIB
    public
#else
    internal
#endif
        static partial class Ascii
    {
        /// <summary>
        /// A mask which selects only the high bit of each byte of the given <see cref="uint"/>.
        /// </summary>
        private const uint UInt32HighBitsOnlyMask = 0x80808080u;

        /// <summary>
        /// A mask which selects only the high bit of each byte of the given <see cref="ulong"/>.
        /// </summary>
        private const ulong UInt64HighBitsOnlyMask = 0x80808080_80808080ul;

        /// <summary>
        /// Returns <see langword="true"/> iff all bytes in <paramref name="value"/> are ASCII.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool AllBytesInUInt32AreAscii(uint value)
        {
            // If the high bit of any byte is set, that byte is non-ASCII.

            return (value & UInt32HighBitsOnlyMask) == 0;
        }

        /// <summary>
        /// Given a DWORD which represents a four-byte buffer read in machine endianness, and which
        /// the caller has asserted contains a non-ASCII byte *somewhere* in the data, counts the
        /// number of consecutive ASCII bytes starting from the beginning of the buffer. Returns
        /// a value 0 - 3, inclusive. (The caller is responsible for ensuring that the buffer doesn't
        /// contain all-ASCII data.)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint CountNumberOfLeadingAsciiBytesFromUInt32WithSomeNonAsciiData(uint value)
        {
            Debug.Assert(!AllBytesInUInt32AreAscii(value), "Caller shouldn't provide an all-ASCII value.");

            if (BitConverter.IsLittleEndian)
            {
#if !MICROSOFT_BCL_MEMORY
                return (uint)BitOperations.TrailingZeroCount(value & UInt32HighBitsOnlyMask) >> 3;
#else
                return (uint)TrailingZeroCount(value & UInt32HighBitsOnlyMask) >> 3;
#endif
            }
            else
            {
                // Couldn't use tzcnt, use specialized software fallback.
                // The 'allBytesUpToNowAreAscii' DWORD uses bit twiddling to hold a 1 or a 0 depending
                // on whether all processed bytes were ASCII. Then we accumulate all of the
                // results to calculate how many consecutive ASCII bytes are present.

                value = ~value;

                // BinaryPrimitives.ReverseEndianness is only implemented as an intrinsic on
                // little-endian platforms, so using it in this big-endian path would be too
                // expensive. Instead we'll just change how we perform the shifts.

                // Read first byte
#if !MICROSOFT_BCL_MEMORY
                value = BitOperations.RotateLeft(value, 1);
#else
                value = (value << 1) | (value >> (32 - 1));
#endif
                uint allBytesUpToNowAreAscii = value & 1;
                uint numAsciiBytes = allBytesUpToNowAreAscii;

                // Read second byte
#if !MICROSOFT_BCL_MEMORY
                value = BitOperations.RotateLeft(value, 8);
#else
                value = (value << 8) | (value >> (32 - 8));
#endif
                allBytesUpToNowAreAscii &= value;
                numAsciiBytes += allBytesUpToNowAreAscii;

                // Read third byte
#if !MICROSOFT_BCL_MEMORY
                value = BitOperations.RotateLeft(value, 8);
#else
                value = (value << 8) | (value >> (32 - 8));
#endif
                allBytesUpToNowAreAscii &= value;
                numAsciiBytes += allBytesUpToNowAreAscii;

                return numAsciiBytes;
            }
        }

#if MICROSOFT_BCL_MEMORY
        private static ReadOnlySpan<byte> TrailingZeroCountDeBruijn => // 32
        [
            00, 01, 28, 02, 29, 14, 24, 03,
            30, 22, 20, 15, 25, 17, 04, 08,
            31, 27, 13, 23, 21, 19, 16, 07,
            26, 12, 18, 06, 11, 05, 10, 09
        ];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int TrailingZeroCount(uint value)
        {
            // uint.MaxValue >> 27 is always in range [0 - 31] so we use Unsafe.AddByteOffset to avoid bounds check
            return Unsafe.AddByteOffset(
                // Using deBruijn sequence, k=2, n=5 (2^5=32) : 0b_0000_0111_0111_1100_1011_0101_0011_0001u
                ref MemoryMarshal.GetReference(TrailingZeroCountDeBruijn),
                // uint|long -> IntPtr cast on 32-bit platforms does expensive overflow checks not needed here
                (IntPtr)(int)(((value & (uint)-(int)value) * 0x077CB531u) >> 27)); // Multi-cast mitigates redundant conv.u8
        }
#endif
    }
}
