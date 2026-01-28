// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;

namespace ILAssembler;

/// <summary>
/// Helper methods for parsing IL string literals.
/// </summary>
public static class StringHelpers
{
    /// <summary>
    /// Parses an IL string literal, handling escape sequences.
    /// </summary>
    /// <param name="rawText">The raw token text including surrounding quotes.</param>
    /// <returns>The unescaped string value.</returns>
    public static string ParseQuotedString(string rawText)
    {
        if (rawText.Length < 2)
        {
            return string.Empty;
        }

        // Strip the surrounding quotes
        char quote = rawText[0];
        if (quote != '"' && quote != '\'')
        {
            return rawText;
        }

        ReadOnlySpan<char> content = rawText.AsSpan(1, rawText.Length - 2);

        // Fast path: if no backslashes, return as-is
        if (!content.Contains('\\'))
        {
            return content.ToString();
        }

        StringBuilder result = new(content.Length);
        int i = 0;

        while (i < content.Length)
        {
            char c = content[i];

            if (c == '\\' && i + 1 < content.Length)
            {
                char next = content[i + 1];
                switch (next)
                {
                    case 't':
                        result.Append('\t');
                        i += 2;
                        break;
                    case 'n':
                        result.Append('\n');
                        i += 2;
                        break;
                    case 'r':
                        result.Append('\r');
                        i += 2;
                        break;
                    case 'b':
                        result.Append('\b');
                        i += 2;
                        break;
                    case 'f':
                        result.Append('\f');
                        i += 2;
                        break;
                    case 'v':
                        result.Append('\v');
                        i += 2;
                        break;
                    case 'a':
                        result.Append('\a');
                        i += 2;
                        break;
                    case '?':
                        result.Append('?');
                        i += 2;
                        break;
                    case '\\':
                        result.Append('\\');
                        i += 2;
                        break;
                    case '"':
                        result.Append('"');
                        i += 2;
                        break;
                    case '\'':
                        result.Append('\'');
                        i += 2;
                        break;
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                        // Octal escape sequence (up to 3 digits)
                        if (i + 3 < content.Length &&
                            IsOctalDigit(content[i + 2]) &&
                            IsOctalDigit(content[i + 3]))
                        {
                            int value = (next - '0') * 64 +
                                        (content[i + 2] - '0') * 8 +
                                        (content[i + 3] - '0');
                            result.Append((char)value);
                            i += 4;
                        }
                        else if (next == '0')
                        {
                            // \0 alone is null character
                            result.Append('\0');
                            i += 2;
                        }
                        else
                        {
                            // Not a valid octal sequence, just output the character
                            result.Append(next);
                            i += 2;
                        }
                        break;
                    case '\n':
                        // Line continuation - skip the backslash, newline, and any following whitespace
                        i += 2;
                        while (i < content.Length && char.IsWhiteSpace(content[i]))
                        {
                            i++;
                        }
                        break;
                    default:
                        // Unknown escape sequence - just output the character after backslash
                        result.Append(next);
                        i += 2;
                        break;
                }
            }
            else
            {
                result.Append(c);
                i++;
            }
        }

        return result.ToString();
    }

    private static bool IsOctalDigit(char c) => c >= '0' && c <= '7';
}
