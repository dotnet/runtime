// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
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

            var sanitized = new StringBuilder(value.Length + 8);
            sanitized.Append(value, 0, firstEscapedCharacterIndex);

            for (int i = firstEscapedCharacterIndex; i < value.Length; i++)
            {
                char current = value[i];
                if (ShouldEscape(current))
                {
                    sanitized.Append(@"\u");
                    sanitized.Append(((int)current).ToString("X4", CultureInfo.InvariantCulture));
                }
                else
                {
                    sanitized.Append(current);
                }
            }

            return sanitized.ToString();
        }

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
                '\u001B' => true, // ESC - ANSI escape sequences
                '\u0007' => true, // BEL - terminal bell
                '\u0008' => true, // BS  - backspace
                '\u007F' => true, // DEL - delete
                '\u009B' => true, // CSI - control sequence introducer (8-bit)
                '\u009C' => true, // ST  - string terminator (8-bit)
                '\u009D' => true, // OSC - operating system command (8-bit)
                '\u200B' => true, // zero-width space
                '\u200C' => true, // zero-width non-joiner
                '\u200D' => true, // zero-width joiner
                '\u200E' => true, // left-to-right mark
                '\u200F' => true, // right-to-left mark
                '\u202A' => true, // left-to-right embedding
                '\u202B' => true, // right-to-left embedding
                '\u202C' => true, // pop directional formatting
                '\u202D' => true, // left-to-right override
                '\u202E' => true, // right-to-left override
                '\u2066' => true, // left-to-right isolate
                '\u2067' => true, // right-to-left isolate
                '\u2068' => true, // first strong isolate
                '\u2069' => true, // pop directional isolate
                _ => false,
            };
        }
    }
}
