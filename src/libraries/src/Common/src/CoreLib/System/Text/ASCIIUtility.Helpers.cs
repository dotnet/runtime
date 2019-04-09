// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace System.Text
{
    internal static partial class ASCIIUtility
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
        /// Given a 24-bit integer which represents a three-byte buffer read in machine endianness,
        /// counts the number of consecutive ASCII bytes starting from the beginning of the buffer.
        /// Returns a value 0 - 3, inclusive. (The caller is responsible for ensuring that an all-
        /// ASCII value does not make its way to this method.)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint CountNumberOfLeadingAsciiBytesFrom24BitInteger(uint value)
        {
            Debug.Assert(!AllBytesInUInt32AreAscii(value), "Caller shouldn't provide an all-ASCII value.");

            if (BitConverter.IsLittleEndian)
            {
                return (uint)BitOperations.TrailingZeroCount(value & UInt32HighBitsOnlyMask) >> 3;
            }
            else
            {
                // The 'allBytesUpToNowAreAscii' DWORD uses bit twiddling to hold a 1 or a 0 depending
                // on whether all processed bytes were ASCII. Then we accumulate all of the
                // results to calculate how many consecutive ASCII bytes are present.

                value = ~value;

                // Read first byte
                value = BitOperations.RotateLeft(value, 1);
                uint allBytesUpToNowAreAscii = value & 1;
                uint numAsciiBytes = allBytesUpToNowAreAscii;

                // Read second byte
                value = BitOperations.RotateLeft(value, 8);
                allBytesUpToNowAreAscii &= value;
                numAsciiBytes += allBytesUpToNowAreAscii;

                // Read third byte
                value = BitOperations.RotateLeft(value, 8);
                allBytesUpToNowAreAscii &= value;
                numAsciiBytes += allBytesUpToNowAreAscii;

                return numAsciiBytes;
            }
        }
    }
}
