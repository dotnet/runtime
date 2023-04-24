// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Text.Unicode
{
    internal static partial class Utf8Utility
    {
        /// <summary>
        /// The maximum number of bytes that can result from UTF-8 transcoding
        /// any Unicode scalar value.
        /// </summary>
        internal const int MaxBytesPerScalar = 4;

        /// <summary>
        /// Returns the byte index in <paramref name="utf8Data"/> where the first invalid UTF-8 sequence begins,
        /// or -1 if the buffer contains no invalid sequences. Also outs the <paramref name="isAscii"/> parameter
        /// stating whether all data observed (up to the first invalid sequence or the end of the buffer, whichever
        /// comes first) is ASCII.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int GetIndexOfFirstInvalidUtf8Sequence(ReadOnlySpan<byte> utf8Data, out bool isAscii)
        {
            fixed (byte* pUtf8Data = &MemoryMarshal.GetReference(utf8Data))
            {
                byte* pFirstInvalidByte = GetPointerToFirstInvalidByte(pUtf8Data, utf8Data.Length, out int utf16CodeUnitCountAdjustment, out _);
                int index = (int)(void*)Unsafe.ByteOffset(ref *pUtf8Data, ref *pFirstInvalidByte);

                isAscii = (utf16CodeUnitCountAdjustment == 0); // If UTF-16 char count == UTF-8 byte count, it's ASCII.
                return (index < utf8Data.Length) ? index : -1;
            }
        }

        /// <summary>
        /// Returns true iff the UInt32 represents four ASCII UTF-8 characters in machine endianness.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool AllBytesInUInt32AreAscii(uint value) => (value & ~0x7F7F_7F7Fu) == 0;

        /// <summary>
        /// Returns true iff the UInt64 represents eighty ASCII UTF-8 characters in machine endianness.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool AllBytesInUInt64AreAscii(ulong value) => (value & ~0x7F7F_7F7F_7F7F_7F7Ful) == 0;

        /// <summary>
        /// Given a UInt32 that represents four ASCII UTF-8 characters, returns the invariant
        /// lowercase representation of those characters. Requires the input value to contain
        /// four ASCII UTF-8 characters in machine endianness.
        /// </summary>
        /// <remarks>
        /// This is a branchless implementation.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint ConvertAllAsciiBytesInUInt32ToLowercase(uint value)
        {
            // ASSUMPTION: Caller has validated that input value is ASCII.
            Debug.Assert(AllBytesInUInt32AreAscii(value));

            // the 0x80 bit of each byte of 'lowerIndicator' will be set iff the word has value >= 'A'
            uint lowerIndicator = value + 0x8080_8080u - 0x4141_4141u;

            // the 0x80 bit of each byte of 'upperIndicator' will be set iff the word has value > 'Z'
            uint upperIndicator = value + 0x8080_8080u - 0x5B5B_5B5Bu;

            // the 0x80 bit of each byte of 'combinedIndicator' will be set iff the word has value >= 'A' and <= 'Z'
            uint combinedIndicator = (lowerIndicator ^ upperIndicator);

            // the 0x20 bit of each byte of 'mask' will be set iff the word has value >= 'A' and <= 'Z'
            uint mask = (combinedIndicator & 0x8080_8080u) >> 2;

            return value ^ mask; // bit flip uppercase letters [A-Z] => [a-z]
        }

        /// <summary>
        /// Given a UInt32 that represents four ASCII UTF-8 characters, returns the invariant
        /// uppercase representation of those characters. Requires the input value to contain
        /// four ASCII UTF-8 characters in machine endianness.
        /// </summary>
        /// <remarks>
        /// This is a branchless implementation.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint ConvertAllAsciiBytesInUInt32ToUppercase(uint value)
        {
            // Intrinsified in mono interpreter
            // ASSUMPTION: Caller has validated that input value is ASCII.
            Debug.Assert(AllBytesInUInt32AreAscii(value));

            // the 0x80 bit of each byte of 'lowerIndicator' will be set iff the word has value >= 'a'
            uint lowerIndicator = value + 0x8080_8080u - 0x6161_6161u;

            // the 0x80 bit of each byte of 'upperIndicator' will be set iff the word has value > 'z'
            uint upperIndicator = value + 0x8080_8080u - 0x7B7B_7B7Bu;

            // the 0x80 bit of each byte of 'combinedIndicator' will be set iff the word has value >= 'a' and <= 'z'
            uint combinedIndicator = (lowerIndicator ^ upperIndicator);

            // the 0x20 bit of each byte of 'mask' will be set iff the word has value >= 'a' and <= 'z'
            uint mask = (combinedIndicator & 0x8080_8080u) >> 2;

            return value ^ mask; // bit flip lowercase letters [a-z] => [A-Z]
        }

        /// <summary>
        /// Given a UInt64 that represents eight ASCII UTF-8 characters, returns the invariant
        /// uppercase representation of those characters. Requires the input value to contain
        /// eight ASCII UTF-8 characters in machine endianness.
        /// </summary>
        /// <remarks>
        /// This is a branchless implementation.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong ConvertAllAsciiBytesInUInt64ToUppercase(ulong value)
        {
            // ASSUMPTION: Caller has validated that input value is ASCII.
            Debug.Assert(AllBytesInUInt64AreAscii(value));

            // the 0x80 bit of each byte of 'lowerIndicator' will be set iff the word has value >= 'a'
            ulong lowerIndicator = value + 0x8080_8080_8080_8080ul - 0x6161_6161_6161_6161ul;

            // the 0x80 bit of each byte of 'upperIndicator' will be set iff the word has value > 'z'
            ulong upperIndicator = value + 0x8080_8080_8080_8080ul - 0x7B7B_7B7B_7B7B_7B7Bul;

            // the 0x80 bit of each byte of 'combinedIndicator' will be set iff the word has value >= 'a' and <= 'z'
            ulong combinedIndicator = (lowerIndicator ^ upperIndicator);

            // the 0x20 bit of each byte of 'mask' will be set iff the word has value >= 'a' and <= 'z'
            ulong mask = (combinedIndicator & 0x8080_8080_8080_8080ul) >> 2;

            return value ^ mask; // bit flip lowercase letters [a-z] => [A-Z]
        }

        /// <summary>
        /// Given a UInt64 that represents eight ASCII UTF-8 characters, returns the invariant
        /// uppercase representation of those characters. Requires the input value to contain
        /// eight ASCII UTF-8 characters in machine endianness.
        /// </summary>
        /// <remarks>
        /// This is a branchless implementation.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ulong ConvertAllAsciiBytesInUInt64ToLowercase(ulong value)
        {
            // ASSUMPTION: Caller has validated that input value is ASCII.
            Debug.Assert(AllBytesInUInt64AreAscii(value));

            // the 0x80 bit of each byte of 'lowerIndicator' will be set iff the word has value >= 'A'
            ulong lowerIndicator = value + 0x8080_8080_8080_8080ul - 0x4141_4141_4141_4141ul;

            // the 0x80 bit of each byte of 'upperIndicator' will be set iff the word has value > 'Z'
            ulong upperIndicator = value + 0x8080_8080_8080_8080ul - 0x5B5B_5B5B_5B5B_5B5Bul;

            // the 0x80 bit of each byte of 'combinedIndicator' will be set iff the word has value >= 'a' and <= 'z'
            ulong combinedIndicator = (lowerIndicator ^ upperIndicator);

            // the 0x20 bit of each byte of 'mask' will be set iff the word has value >= 'a' and <= 'z'
            ulong mask = (combinedIndicator & 0x8080_8080_8080_8080ul) >> 2;

            return value ^ mask; // bit flip uppercase letters [A-Z] => [a-z]
        }
    }
}
