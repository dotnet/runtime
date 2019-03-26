// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Internal.Runtime.CompilerServices;

#if BIT64
using nint = System.Int64;
using nuint = System.UInt64;
#else // BIT64
using nint = System.Int32;
using nuint = System.UInt32;
#endif // BIT64

namespace System.Text
{
    internal static partial class ASCIIUtility
    {
        /// <summary>
        /// Returns <see langword="true"/> iff all bytes in <paramref name="value"/> are ASCII.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool AllBytesInUInt32AreAscii(uint value)
        {
            return (value & 0x80808080u) == 0;
        }


        /// <summary>
        /// Given a 24-bit integer which represents a three-byte buffer read in machine endianness,
        /// counts the number of consecutive ASCII bytes starting from the beginning of the buffer.
        /// Returns a value 0 - 3, inclusive.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint CountNumberOfLeadingAsciiBytesFrom24BitInteger(uint value)
        {
            // This implementation seems to have better performance than tzcnt.

            // The 'allBytesUpToNowAreAscii' DWORD uses bit twiddling to hold a 1 or a 0 depending
            // on whether all processed bytes were ASCII. Then we accumulate all of the
            // results to calculate how many consecutive ASCII bytes are present.

            value = ~value;

            if (BitConverter.IsLittleEndian)
            {
                // Read first byte
                uint allBytesUpToNowAreAscii = (value >>= 7) & 1;
                uint numAsciiBytes = allBytesUpToNowAreAscii;

                // Read second byte
                allBytesUpToNowAreAscii &= (value >>= 8);
                numAsciiBytes += allBytesUpToNowAreAscii;

                // Read third byte
                allBytesUpToNowAreAscii &= (value >>= 8);
                numAsciiBytes += allBytesUpToNowAreAscii;

                return numAsciiBytes;
            }
            else
            {
                // Read first byte
                uint allBytesUpToNowAreAscii = (value = ROL32(value, 1)) & 1;
                uint numAsciiBytes = allBytesUpToNowAreAscii;

                // Read second byte
                allBytesUpToNowAreAscii &= (value = ROL32(value, 8));
                numAsciiBytes += allBytesUpToNowAreAscii;

                // Read third byte
                allBytesUpToNowAreAscii &= (value = ROL32(value, 8));
                numAsciiBytes += allBytesUpToNowAreAscii;

                return numAsciiBytes;
            }
        }
    }
}
