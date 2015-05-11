// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Text.Encodings.Web
{
    /// <summary>
    /// Contains helpers for dealing with byte-hex char conversions.
    /// </summary>
    internal static class HexUtil
    {
        /// <summary>
        /// Converts a number 0 - 15 to its associated hex character '0' - 'F'.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static char UInt32LsbToHexDigit(uint value)
        {
            Debug.Assert(value < 16);
            return (value < 10) ? (char)('0' + value) : (char)('A' + (value - 10));
        }

        /// <summary>
        /// Converts a number 0 - 15 to its associated hex character '0' - 'F'.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static char Int32LsbToHexDigit(int value)
        {
            Debug.Assert(value < 16);
            return (char)((value < 10) ? ('0' + value) : ('A' + (value - 10)));
        }

        /// <summary>
        /// Gets the uppercase hex-encoded form of a byte.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ByteToHexDigits(byte value, out char firstHexChar, out char secondHexChar)
        {
            firstHexChar = UInt32LsbToHexDigit((uint)value >> 4);
            secondHexChar = UInt32LsbToHexDigit((uint)value & 0xFU);
        }
    }
}
