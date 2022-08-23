// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.RegularExpressions.Symbolic
{
    internal static class CharKind
    {
        /// <summary>Number of kinds of chars.</summary>
        internal const int CharKindCount = 5;

        /// <summary>All characters other than those in the four other kinds.</summary>
        internal const uint General = 0;

        /// <summary>Beginning or End of input (bit 0 is 1)</summary>
        internal const uint BeginningEnd = 1;

        /// <summary>New line character (\n) (bit 1 is 1)</summary>
        internal const uint Newline = 2;

        /// <summary>Last \n or first \n in reverse mode (both Newline and StartStop bits are 1)</summary>
        internal const uint NewLineS = 3;

        /// <summary>Word letter (bit 2 is 1)</summary>
        internal const uint WordLetter = 4;

        /// <summary>Gets the previous character kind from a context</summary>
        internal static uint Prev(uint context) => context & 0x7;

        /// <summary>Gets the next character kind from a context</summary>
        internal static uint Next(uint context) => context >> 3;

        /// <summary>Encodes the pair (prevKind, nextKind) using 6 bits</summary>
        internal static uint Context(uint prevKind, uint nextKind) => (nextKind << 3) | prevKind;

        /// <summary>Exclusive maximum context (limit) is 64 because a context uses bit-shifting where each kind needs 3 bits.</summary>
        internal const int ContextLimit = 64;

        internal static string DescribePrev(uint i) => i switch
        {
            BeginningEnd => @"\A",
            Newline => @"\n",
            NewLineS => @"\A\n",
            WordLetter => @"\w",
            _ => string.Empty,
        };

        /// <summary>Returns whether the given value is in the range of valid character kinds.</summary>
        internal static bool IsValidCharKind(uint charKind) => charKind < CharKindCount;
    }
}
