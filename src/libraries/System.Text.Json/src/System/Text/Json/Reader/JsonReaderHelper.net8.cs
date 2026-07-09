// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.CompilerServices;

namespace System.Text.Json
{
    internal static partial class JsonReaderHelper
    {
        /// <summary>'"', '\',  or any control characters (i.e. 0 to 31).</summary>
        /// <remarks>https://tools.ietf.org/html/rfc8259</remarks>
        private static readonly SearchValues<byte> s_controlQuoteBackslash = SearchValues.Create(
            // Any Control, < 32 (' ')
            "\u0000\u0001\u0002\u0003\u0004\u0005\u0006\u0007\u0008\u0009\u000A\u000B\u000C\u000D\u000E\u000F\u0010\u0011\u0012\u0013\u0014\u0015\u0016\u0017\u0018\u0019\u001A\u001B\u001C\u001D\u001E\u001F"u8 +
            // Quote
            "\""u8 +
            // Backslash
            "\\"u8);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOfQuoteOrAnyControlOrBackSlash(this ReadOnlySpan<byte> span) =>
            span.IndexOfAny(s_controlQuoteBackslash);

        /// <summary>JSON insignificant whitespace: space (0x20), tab (0x09), CR (0x0D), and LF (0x0A).</summary>
        /// <remarks>https://tools.ietf.org/html/rfc8259#section-2</remarks>
        private static readonly SearchValues<byte> s_whiteSpace = SearchValues.Create(" \t\r\n"u8);

        /// <summary>
        /// Returns the index of the first byte that is not JSON insignificant whitespace,
        /// or the length of the span if every byte is whitespace.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOfFirstNonWhiteSpace(this ReadOnlySpan<byte> span)
        {
            int index = span.IndexOfAnyExcept(s_whiteSpace);
            return index < 0 ? span.Length : index;
        }
    }
}
