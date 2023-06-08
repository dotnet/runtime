// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Text;

namespace System.Net.Http
{
    internal static class HttpRuleParser
    {
        // token = 1*<any CHAR except CTLs or separators>
        // CTL = <any US-ASCII control character (octets 0 - 31) and DEL (127)>
        private static readonly SearchValues<char> s_tokenChars =
            SearchValues.Create("!#$%&'*+-.0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ^_`abcdefghijklmnopqrstuvwxyz|~");

        private static readonly SearchValues<byte> s_tokenBytes =
            SearchValues.Create("!#$%&'*+-.0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ^_`abcdefghijklmnopqrstuvwxyz|~"u8);

        private static readonly SearchValues<char> s_hostDelimiterChars =
            SearchValues.Create("/ \t\r,");

        private const int MaxNestedCount = 5;

        internal const char CR = (char)13;
        internal const char LF = (char)10;
        internal const int MaxInt64Digits = 19;
        internal const int MaxInt32Digits = 10;

        internal static Encoding DefaultHttpEncoding => Encoding.Latin1;

        internal static int GetTokenLength(string input, int startIndex)
        {
            Debug.Assert(input is not null);

            ReadOnlySpan<char> slice = input.AsSpan(startIndex);

            int index = slice.IndexOfAnyExcept(s_tokenChars);

            return index < 0 ? slice.Length : index;
        }

        internal static bool IsToken(ReadOnlySpan<char> input) =>
            input.IndexOfAnyExcept(s_tokenChars) < 0;

        internal static bool IsToken(ReadOnlySpan<byte> input) =>
            input.IndexOfAnyExcept(s_tokenBytes) < 0;

        internal static string GetTokenString(ReadOnlySpan<byte> input)
        {
            Debug.Assert(IsToken(input));

            return Encoding.ASCII.GetString(input);
        }

        internal static int GetWhitespaceLength(string input, int startIndex)
        {
            Debug.Assert(input != null);

            if (startIndex >= input.Length)
            {
                return 0;
            }

            int current = startIndex;

            char c;
            while (current < input.Length)
            {
                c = input[current];

                if ((c == ' ') || (c == '\t'))
                {
                    current++;
                    continue;
                }

                return current - startIndex;
            }

            // All characters between startIndex and the end of the string are LWS characters.
            return input.Length - startIndex;
        }

        internal static bool ContainsNewLine(string value, int startIndex = 0) =>
            value.AsSpan(startIndex).IndexOfAny('\r', '\n') >= 0;

        internal static int GetNumberLength(string input, int startIndex, bool allowDecimal)
        {
            Debug.Assert(input != null);
            Debug.Assert((startIndex >= 0) && (startIndex < input.Length));

            int current = startIndex;
            char c;

            // If decimal values are not allowed, we pretend to have read the '.' character already. I.e. if a dot is
            // found in the string, parsing will be aborted.
            bool haveDot = !allowDecimal;

            // The RFC doesn't allow decimal values starting with dot. I.e. value ".123" is invalid. It must be in the
            // form "0.123". Also, there are no negative values defined in the RFC. So we'll just parse non-negative
            // values.
            // The RFC only allows decimal dots not ',' characters as decimal separators. Therefore value "1,23" is
            // considered invalid and must be represented as "1.23".
            if (input[current] == '.')
            {
                return 0;
            }

            while (current < input.Length)
            {
                c = input[current];
                if (char.IsAsciiDigit(c))
                {
                    current++;
                }
                else if (!haveDot && (c == '.'))
                {
                    // Note that value "1." is valid.
                    haveDot = true;
                    current++;
                }
                else
                {
                    break;
                }
            }

            return current - startIndex;
        }

        internal static int GetHostLength(string input, int startIndex, bool allowToken)
        {
            Debug.Assert(input != null);
            Debug.Assert(startIndex >= 0);

            if (startIndex >= input.Length)
            {
                return 0;
            }

            ReadOnlySpan<char> slice = input.AsSpan(startIndex);

            // A 'host' is either a token (if 'allowToken' == true) or a valid host name as defined by the URI RFC.
            // So we first iterate through the string and search for path delimiters and whitespace. When found, stop
            // and try to use the substring as token or URI host name. If it works, we have a host name, otherwise not.
            int index = slice.IndexOfAny(s_hostDelimiterChars);
            if (index >= 0)
            {
                if (index == 0)
                {
                    return 0;
                }

                if (slice[index] == '/')
                {
                    return 0; // Host header must not contain paths.
                }

                slice = slice.Slice(0, index);
            }

            if ((allowToken && IsToken(slice)) || IsValidHostName(slice))
            {
                return slice.Length;
            }

            return 0;
        }

        internal static HttpParseResult GetCommentLength(string input, int startIndex, out int length)
        {
            return GetExpressionLength(input, startIndex, '(', ')', true, 1, out length);
        }

        internal static HttpParseResult GetQuotedStringLength(string input, int startIndex, out int length)
        {
            return GetExpressionLength(input, startIndex, '"', '"', false, 1, out length);
        }

        // quoted-pair = "\" CHAR
        // CHAR = <any US-ASCII character (octets 0 - 127)>
        internal static HttpParseResult GetQuotedPairLength(string input, int startIndex, out int length)
        {
            Debug.Assert(input != null);
            Debug.Assert((startIndex >= 0) && (startIndex < input.Length));

            length = 0;

            if (input[startIndex] != '\\')
            {
                return HttpParseResult.NotParsed;
            }

            // Quoted-char has 2 characters. Check whether there are 2 chars left ('\' + char)
            // If so, check whether the character is in the range 0-127. If not, it's an invalid value.
            if ((startIndex + 2 > input.Length) || (input[startIndex + 1] > 127))
            {
                return HttpParseResult.InvalidFormat;
            }

            // It doesn't matter what the char next to '\' is so we can skip along.
            length = 2;
            return HttpParseResult.Parsed;
        }

        // TEXT = <any OCTET except CTLs, but including LWS>
        // LWS = SP | HT
        // CTL = <any US-ASCII control character (octets 0 - 31) and DEL (127)>
        //
        // Since we don't really care about the content of a quoted string or comment, we're more tolerant and
        // allow these characters. We only want to find the delimiters ('"' for quoted string and '(', ')' for comment).
        //
        // 'nestedCount': Comments can be nested. We allow a depth of up to 5 nested comments, i.e. something like
        // "(((((comment)))))". If we wouldn't define a limit an attacker could send a comment with hundreds of nested
        // comments, resulting in a stack overflow exception. In addition having more than 1 nested comment (if any)
        // is unusual.
        private static HttpParseResult GetExpressionLength(string input, int startIndex, char openChar,
            char closeChar, bool supportsNesting, int nestedCount, out int length)
        {
            Debug.Assert(input != null);
            Debug.Assert((startIndex >= 0) && (startIndex < input.Length));

            length = 0;

            if (input[startIndex] != openChar)
            {
                return HttpParseResult.NotParsed;
            }

            int current = startIndex + 1; // Start parsing with the character next to the first open-char.
            while (current < input.Length)
            {
                // Only check whether we have a quoted char, if we have at least 3 characters left to read (i.e.
                // quoted char + closing char). Otherwise the closing char may be considered part of the quoted char.
                int quotedPairLength;
                if ((current + 2 < input.Length) &&
                    (GetQuotedPairLength(input, current, out quotedPairLength) == HttpParseResult.Parsed))
                {
                    // We ignore invalid quoted-pairs. Invalid quoted-pairs may mean that it looked like a quoted pair,
                    // but we actually have a quoted-string: e.g. "\\u00FC" ('\' followed by a char >127 - quoted-pair only
                    // allows ASCII chars after '\'; qdtext allows both '\' and >127 chars).
                    current += quotedPairLength;
                    continue;
                }

                char c = input[current];

                if (c == '\r' || c == '\n')
                {
                    return HttpParseResult.InvalidFormat;
                }

                // If we support nested expressions and we find an open-char, then parse the nested expressions.
                if (supportsNesting && (c == openChar))
                {
                    // Check if we exceeded the number of nested calls.
                    if (nestedCount > MaxNestedCount)
                    {
                        return HttpParseResult.InvalidFormat;
                    }

                    int nestedLength;
                    HttpParseResult nestedResult = GetExpressionLength(input, current, openChar, closeChar,
                        supportsNesting, nestedCount + 1, out nestedLength);

                    switch (nestedResult)
                    {
                        case HttpParseResult.Parsed:
                            current += nestedLength; // Add the length of the nested expression and continue.
                            break;

                        case HttpParseResult.NotParsed:
                            Debug.Fail("'NotParsed' is unexpected: We started nested expression " +
                                "parsing, because we found the open-char. So either it's a valid nested " +
                                "expression or it has invalid format.");
                            break;

                        case HttpParseResult.InvalidFormat:
                            // If the nested expression is invalid, we can't continue, so we fail with invalid format.
                            return HttpParseResult.InvalidFormat;

                        default:
                            Debug.Fail("Unknown enum result: " + nestedResult);
                            break;
                    }

                    // after nested call we continue with parsing
                    continue;
                }

                if (input[current] == closeChar)
                {
                    length = current - startIndex + 1;
                    return HttpParseResult.Parsed;
                }
                current++;
            }

            // We didn't find the final quote, therefore we have an invalid expression string.
            return HttpParseResult.InvalidFormat;
        }

        private static bool IsValidHostName(ReadOnlySpan<char> host)
        {
            // Also add user info (u@) to make sure 'host' doesn't include user info.
            return Uri.TryCreate($"http://u@{host}/", UriKind.Absolute, out _);
        }
    }
}
