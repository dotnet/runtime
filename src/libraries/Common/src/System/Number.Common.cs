// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    internal static partial class Number
    {
        private const int CharStackBufferSize = 32;
        internal enum ParsingStatus
        {
            OK,
            Failed,
            Overflow
        }

        private static bool IsWhite(uint ch) => (ch == 0x20) || ((ch - 0x09) <= (0x0D - 0x09));

        private static bool IsDigit(uint ch) => (ch - '0') <= 9;

        private static bool IsSpaceReplacingChar(uint c) => (c == '\u00a0') || (c == '\u202f');
    }
}
