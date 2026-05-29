// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;

namespace Microsoft.Extensions.Logging.Console
{
    internal static class ConsoleControlCharacterSanitizer
    {
        public static string? Sanitize(string? value, bool sanitizeControlCharacters)
        {
            if (!sanitizeControlCharacters || string.IsNullOrEmpty(value))
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
            UnicodeCategory category = char.GetUnicodeCategory(c);
            return category == UnicodeCategory.Control || category == UnicodeCategory.Format;
        }
    }
}
