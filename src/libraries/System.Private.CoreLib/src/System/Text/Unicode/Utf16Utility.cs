// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace System.Text.Unicode
{
    internal static partial class Utf16Utility
    {
        /// <summary>
        /// Returns true iff the UInt32 represents two ASCII UTF-16 characters in machine endianness.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool AllCharsInUInt32AreAscii(uint value)
        {
            return (value & ~0x007F_007Fu) == 0;
        }

        /// <summary>
        /// Returns true iff the UInt64 represents four ASCII UTF-16 characters in machine endianness.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool AllCharsInUInt64AreAscii(ulong value)
        {
            return (value & ~0x007F_007F_007F_007Ful) == 0;
        }

        /// <summary>
        /// Given a UInt32 that represents two ASCII UTF-16 characters, returns the invariant
        /// lowercase representation of those characters. Requires the input value to contain
        /// two ASCII UTF-16 characters in machine endianness.
        /// </summary>
        /// <remarks>
        /// This is a branchless implementation.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint ConvertAllAsciiCharsInUInt32ToLowercase(uint value)
        {
            // ASSUMPTION: Caller has validated that input value is ASCII.
            Debug.Assert(AllCharsInUInt32AreAscii(value));

            // the 0x80 bit of each word of 'lowerIndicator' will be set iff the word has value >= 'A'
            uint lowerIndicator = value + 0x0080_0080u - 0x0041_0041u;

            // the 0x80 bit of each word of 'upperIndicator' will be set iff the word has value > 'Z'
            uint upperIndicator = value + 0x0080_0080u - 0x005B_005Bu;

            // the 0x80 bit of each word of 'combinedIndicator' will be set iff the word has value >= 'A' and <= 'Z'
            uint combinedIndicator = (lowerIndicator ^ upperIndicator);

            // the 0x20 bit of each word of 'mask' will be set iff the word has value >= 'A' and <= 'Z'
            uint mask = (combinedIndicator & 0x0080_0080u) >> 2;

            return value ^ mask; // bit flip uppercase letters [A-Z] => [a-z]
        }

        /// <summary>
        /// Given a UInt32 that represents two ASCII UTF-16 characters, returns the invariant
        /// uppercase representation of those characters. Requires the input value to contain
        /// two ASCII UTF-16 characters in machine endianness.
        /// </summary>
        /// <remarks>
        /// This is a branchless implementation.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint ConvertAllAsciiCharsInUInt32ToUppercase(uint value)
        {
            // Intrinsified in mono interpreter
            // ASSUMPTION: Caller has validated that input value is ASCII.
            Debug.Assert(AllCharsInUInt32AreAscii(value));

            // the 0x80 bit of each word of 'lowerIndicator' will be set iff the word has value >= 'a'
            uint lowerIndicator = value + 0x0080_0080u - 0x0061_0061u;

            // the 0x80 bit of each word of 'upperIndicator' will be set iff the word has value > 'z'
            uint upperIndicator = value + 0x0080_0080u - 0x007B_007Bu;

            // the 0x80 bit of each word of 'combinedIndicator' will be set iff the word has value >= 'a' and <= 'z'
            uint combinedIndicator = (lowerIndicator ^ upperIndicator);

            // the 0x20 bit of each word of 'mask' will be set iff the word has value >= 'a' and <= 'z'
            uint mask = (combinedIndicator & 0x0080_0080u) >> 2;

            return value ^ mask; // bit flip lowercase letters [a-z] => [A-Z]
        }

        /// <summary>
        /// Given a UInt32 that represents two ASCII UTF-16 characters, returns true iff
        /// the input contains one or more lowercase ASCII characters.
        /// </summary>
        /// <remarks>
        /// This is a branchless implementation.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool UInt32ContainsAnyLowercaseAsciiChar(uint value)
        {
            // ASSUMPTION: Caller has validated that input value is ASCII.
            Debug.Assert(AllCharsInUInt32AreAscii(value));

            // the 0x80 bit of each word of 'lowerIndicator' will be set iff the word has value >= 'a'
            uint lowerIndicator = value + 0x0080_0080u - 0x0061_0061u;

            // the 0x80 bit of each word of 'upperIndicator' will be set iff the word has value > 'z'
            uint upperIndicator = value + 0x0080_0080u - 0x007B_007Bu;

            // the 0x80 bit of each word of 'combinedIndicator' will be set iff the word has value >= 'a' and <= 'z'
            uint combinedIndicator = (lowerIndicator ^ upperIndicator);

            return (combinedIndicator & 0x0080_0080u) != 0;
        }

        /// <summary>
        /// Given a UInt32 that represents two ASCII UTF-16 characters, returns true iff
        /// the input contains one or more uppercase ASCII characters.
        /// </summary>
        /// <remarks>
        /// This is a branchless implementation.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool UInt32ContainsAnyUppercaseAsciiChar(uint value)
        {
            // ASSUMPTION: Caller has validated that input value is ASCII.
            Debug.Assert(AllCharsInUInt32AreAscii(value));

            // the 0x80 bit of each word of 'lowerIndicator' will be set iff the word has value >= 'A'
            uint lowerIndicator = value + 0x0080_0080u - 0x0041_0041u;

            // the 0x80 bit of each word of 'upperIndicator' will be set iff the word has value > 'Z'
            uint upperIndicator = value + 0x0080_0080u - 0x005B_005Bu;

            // the 0x80 bit of each word of 'combinedIndicator' will be set iff the word has value >= 'A' and <= 'Z'
            uint combinedIndicator = (lowerIndicator ^ upperIndicator);

            return (combinedIndicator & 0x0080_0080u) != 0;
        }

        /// <summary>
        /// Given two UInt32s that represent two ASCII UTF-16 characters each, returns true iff
        /// the two inputs are equal using an ordinal case-insensitive comparison.
        /// </summary>
        /// <remarks>
        /// This is a branchless implementation.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool UInt32OrdinalIgnoreCaseAscii(uint valueA, uint valueB)
        {
            // Intrinsified in mono interpreter
            // ASSUMPTION: Caller has validated that input values are ASCII.
            Debug.Assert(AllCharsInUInt32AreAscii(valueA));
            Debug.Assert(AllCharsInUInt32AreAscii(valueB));

            // Generate a mask of all bits which are different between A and B. Since [A-Z]
            // and [a-z] differ by the 0x20 bit, we'll left-shift this by 2 now so that
            // this is moved over to the 0x80 bit, which nicely aligns with the calculation
            // we're going to do on the indicator flag later.
            //
            // n.b. All of the logic below assumes we have at least 2 "known zero" bits leading
            // each of the 7-bit ASCII values. This assumption won't hold if this method is
            // ever adapted to deal with packed bytes instead of packed chars.

            uint differentBits = (valueA ^ valueB) << 2;

            // Now, we want to generate a mask where for each word in the input, the mask contains
            // 0xFF7F if the word is [A-Za-z], 0xFFFF if the word is not [A-Za-z]. We know each
            // input word is ASCII (only low 7 bit set), so we can use a combination of addition
            // and logical operators as follows.
            //
            // original input   +05         |A0         +1A
            // ====================================================
            //         00 .. 3F -> 05 .. 44 -> A5 .. E4 -> BF .. FE
            //               40 ->       45 ->       E5 ->       FF
            // ([A-Z]) 41 .. 5A -> 46 .. 5F -> E6 .. FF -> 00 .. 19
            //         5B .. 5F -> 60 .. 64 -> E0 .. E4 -> FA .. FE
            //               60 ->       65 ->       E5 ->       FF
            // ([a-z]) 61 .. 7A -> 66 .. 7F -> E6 .. FF -> 00 .. 19
            //         7B .. 7F -> 80 .. 84 -> A0 .. A4 -> BA .. BE
            //
            // This combination of operations results in the 0x80 bit of each word being set
            // iff the original word value was *not* [A-Za-z].

            uint indicator = valueA + 0x0005_0005u;
            indicator |= 0x00A0_00A0u;
            indicator += 0x001A_001Au;
            indicator |= 0xFF7F_FF7Fu; // normalize each word to 0xFF7F or 0xFFFF

            // At this point, 'indicator' contains the mask of bits which are *not* allowed to
            // differ between the inputs, and 'differentBits' contains the mask of bits which
            // actually differ between the inputs. If these masks have any bits in common, then
            // the two values are *not* equal under an OrdinalIgnoreCase comparer.

            return (differentBits & indicator) == 0;
        }

        /// <summary>
        /// Given two UInt64s that represent four ASCII UTF-16 characters each, returns true iff
        /// the two inputs are equal using an ordinal case-insensitive comparison.
        /// </summary>
        /// <remarks>
        /// This is a branchless implementation.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool UInt64OrdinalIgnoreCaseAscii(ulong valueA, ulong valueB)
        {
            // Intrinsified in mono interpreter
            // ASSUMPTION: Caller has validated that input values are ASCII.
            Debug.Assert(AllCharsInUInt64AreAscii(valueA));
            Debug.Assert(AllCharsInUInt64AreAscii(valueB));

            // Duplicate of logic in UInt32OrdinalIgnoreCaseAscii, but using 64-bit consts.
            // See comments in that method for more info.

            ulong differentBits = (valueA ^ valueB) << 2;
            ulong indicator = valueA + 0x0005_0005_0005_0005ul;
            indicator |= 0x00A0_00A0_00A0_00A0ul;
            indicator += 0x001A_001A_001A_001Aul;
            indicator |= 0xFF7F_FF7F_FF7F_FF7Ful;
            return (differentBits & indicator) == 0;
        }
    }
}
