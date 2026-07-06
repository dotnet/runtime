// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
#if NET
using System.Buffers;
using System.Collections.Generic;
#endif

namespace Microsoft.Extensions.Logging.Console
{
    internal static class ConsoleControlCharacterSanitizer
    {
#if NET
        // Built from ShouldEscape so the vectorized scan can never drift from the scalar predicate.
        private static readonly SearchValues<char> s_charsToEscape = CreateSearchValues();
#endif

        public static string? Sanitize(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            ReadOnlySpan<char> remaining = value.AsSpan();
            int firstEscapedCharacterIndex = IndexOfFirstCharToEscape(remaining);
            if (firstEscapedCharacterIndex < 0)
            {
                return value;
            }

            var sanitized = new ValueStringBuilder(stackalloc char[256]);
            sanitized.Append(remaining.Slice(0, firstEscapedCharacterIndex));
            remaining = remaining.Slice(firstEscapedCharacterIndex);

            while (true)
            {
                // remaining[0] is always a character that must be escaped.
                AppendEscaped(ref sanitized, remaining[0]);
                remaining = remaining.Slice(1);

                int next = IndexOfFirstCharToEscape(remaining);
                if (next < 0)
                {
                    sanitized.Append(remaining);
                    break;
                }

                sanitized.Append(remaining.Slice(0, next));
                remaining = remaining.Slice(next);
            }

            return sanitized.ToString();
        }

        private static void AppendEscaped(ref ValueStringBuilder builder, char c)
        {
            Span<char> escaped = builder.AppendSpan(6);
            escaped[0] = '\\';
            escaped[1] = 'u';
            escaped[2] = ToHexChar(c >> 12);
            escaped[3] = ToHexChar(c >> 8);
            escaped[4] = ToHexChar(c >> 4);
            escaped[5] = ToHexChar(c);
        }

        private static char ToHexChar(int nibble)
        {
            nibble &= 0xF;
            return (char)(nibble < 10 ? '0' + nibble : 'A' + nibble - 10);
        }

        private static int IndexOfFirstCharToEscape(ReadOnlySpan<char> value)
        {
#if NET
            return value.IndexOfAny(s_charsToEscape);
#else
            for (int i = 0; i < value.Length; i++)
            {
                if (ShouldEscape(value[i]))
                {
                    return i;
                }
            }

            return -1;
#endif
        }

#if NET
        private static SearchValues<char> CreateSearchValues()
        {
            // ShouldEscape only returns true for characters in the C0/DEL/C1 range (<= U+009F),
            // so there is no need to scan the rest of the BMP when building the set.
            var chars = new List<char>();
            for (int c = 0; c <= 0x9F; c++)
            {
                if (ShouldEscape((char)c))
                {
                    chars.Add((char)c);
                }
            }

            return SearchValues.Create(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(chars));
        }
#endif

        private static bool ShouldEscape(char c)
        {
            // Escape the control characters that can drive terminal escape sequences when written to a
            // console: the C0 range (U+0000-U+001F), DEL (U+007F) and the C1 range (U+0080-U+009F). These
            // are the same ranges sanitized by systemd and OpenSSH for terminal output. \t, \n and \r are
            // control characters but are preserved for log formatting.
            if (c is '\t' or '\n' or '\r')
            {
                return false;
            }

            return c <= '\u001F' || (c >= '\u007F' && c <= '\u009F');
        }
    }
}
