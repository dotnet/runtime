// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;

namespace Microsoft.Extensions.Logging.Console
{
    internal static class ConsoleControlCharacterSanitizer
    {
        public static string? Sanitize(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            int firstEscapedCharacterIndex = GetFirstEscapedCharacterIndex(value);
            if (firstEscapedCharacterIndex < 0)
            {
                return value;
            }

            var sanitized = new ValueStringBuilder(stackalloc char[256]);
            sanitized.Append(value.AsSpan(0, firstEscapedCharacterIndex));

            for (int i = firstEscapedCharacterIndex; i < value.Length; i++)
            {
                char current = value[i];
                if (ShouldEscape(current))
                {
                    sanitized.Append('\\');
                    sanitized.Append('u');
                    int codePoint = current;
                    Span<char> hex = sanitized.AppendSpan(4);
                    hex[0] = ToHexChar(codePoint >> 12);
                    hex[1] = ToHexChar((codePoint >> 8) & 0xF);
                    hex[2] = ToHexChar((codePoint >> 4) & 0xF);
                    hex[3] = ToHexChar(codePoint & 0xF);
                }
                else
                {
                    sanitized.Append(current);
                }
            }

            return sanitized.ToString();
        }

        private static char ToHexChar(int value) =>
            (char)(value < 10 ? '0' + value : 'A' + value - 10);

        private static int GetFirstEscapedCharacterIndex(string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                if (ShouldEscape(value[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool ShouldEscape(char c)
        {
            return c switch
            {
                '\u0000' => true, // NUL
                '\u0001' => true, // SOH
                '\u0002' => true, // STX
                '\u0003' => true, // ETX
                '\u0004' => true, // EOT
                '\u0005' => true, // ENQ
                '\u0006' => true, // ACK
                '\u0007' => true, // BEL
                '\u0008' => true, // BS
                // '\u0009' HT (tab) - preserved for log formatting
                // '\u000A' LF (newline) - preserved for log formatting
                '\u000B' => true, // VT
                '\u000C' => true, // FF
                // '\u000D' CR (carriage return) - preserved for log formatting
                '\u000E' => true, // SO
                '\u000F' => true, // SI
                '\u0010' => true, // DLE
                '\u0011' => true, // DC1
                '\u0012' => true, // DC2
                '\u0013' => true, // DC3
                '\u0014' => true, // DC4
                '\u0015' => true, // NAK
                '\u0016' => true, // SYN
                '\u0017' => true, // ETB
                '\u0018' => true, // CAN
                '\u0019' => true, // EM
                '\u001A' => true, // SUB
                '\u001B' => true, // ESC
                '\u001C' => true, // FS
                '\u001D' => true, // GS
                '\u001E' => true, // RS
                '\u001F' => true, // US
                '\u007F' => true, // DEL
                >= '\u0080' and <= '\u009F' => true, // C1 control range
                >= '\u200B' and <= '\u200F' => true, // zero-width and directional marks
                >= '\u202A' and <= '\u202E' => true, // bidi embedding/override
                >= '\u2066' and <= '\u2069' => true, // bidi isolates
                _ => false,
            };
        }
    }
}
