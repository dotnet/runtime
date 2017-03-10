// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Text;

namespace System.Net.Http
{
    internal static class HttpRuleParser
    {
        private static readonly bool[] s_tokenChars = CreateTokenChars();
        private const int maxNestedCount = 5;
        private static readonly string[] s_dateFormats = new string[] {
            // "r", // RFC 1123, required output format but too strict for input
            "ddd, d MMM yyyy H:m:s 'GMT'", // RFC 1123 (r, except it allows both 1 and 01 for date and time)
            "ddd, d MMM yyyy H:m:s", // RFC 1123, no zone - assume GMT
            "d MMM yyyy H:m:s 'GMT'", // RFC 1123, no day-of-week
            "d MMM yyyy H:m:s", // RFC 1123, no day-of-week, no zone
            "ddd, d MMM yy H:m:s 'GMT'", // RFC 1123, short year
            "ddd, d MMM yy H:m:s", // RFC 1123, short year, no zone
            "d MMM yy H:m:s 'GMT'", // RFC 1123, no day-of-week, short year
            "d MMM yy H:m:s", // RFC 1123, no day-of-week, short year, no zone

            "dddd, d'-'MMM'-'yy H:m:s 'GMT'", // RFC 850
            "dddd, d'-'MMM'-'yy H:m:s", // RFC 850 no zone
            "ddd MMM d H:m:s yyyy", // ANSI C's asctime() format

            "ddd, d MMM yyyy H:m:s zzz", // RFC 5322
            "ddd, d MMM yyyy H:m:s", // RFC 5322 no zone
            "d MMM yyyy H:m:s zzz", // RFC 5322 no day-of-week
            "d MMM yyyy H:m:s", // RFC 5322 no day-of-week, no zone
        };

        internal const char CR = (char)13;
        internal const char LF = (char)10;
        internal const int MaxInt64Digits = 19;
        internal const int MaxInt32Digits = 10;

        // iso-8859-1, Western European (ISO)
#if uap
        internal static readonly Encoding DefaultHttpEncoding = Encoding.GetEncoding("iso-8859-1");
#else
        internal static readonly Encoding DefaultHttpEncoding = Encoding.GetEncoding(28591);
#endif

        private static bool[] CreateTokenChars()
        {
            // token = 1*<any CHAR except CTLs or separators>
            // CTL = <any US-ASCII control character (octets 0 - 31) and DEL (127)>

            var tokenChars = new bool[128]; // All elements default to "false".

            for (int i = 33; i < 127; i++) // Skip Space (32) & DEL (127).
            {
                tokenChars[i] = true;
            }

            // Remove separators: these are not valid token characters.
            tokenChars[(byte)'('] = false;
            tokenChars[(byte)')'] = false;
            tokenChars[(byte)'<'] = false;
            tokenChars[(byte)'>'] = false;
            tokenChars[(byte)'@'] = false;
            tokenChars[(byte)','] = false;
            tokenChars[(byte)';'] = false;
            tokenChars[(byte)':'] = false;
            tokenChars[(byte)'\\'] = false;
            tokenChars[(byte)'"'] = false;
            tokenChars[(byte)'/'] = false;
            tokenChars[(byte)'['] = false;
            tokenChars[(byte)']'] = false;
            tokenChars[(byte)'?'] = false;
            tokenChars[(byte)'='] = false;
            tokenChars[(byte)'{'] = false;
            tokenChars[(byte)'}'] = false;

            return tokenChars;
        }

        internal static bool IsTokenChar(char character)
        {
            // Must be between 'space' (32) and 'DEL' (127).
            if (character > 127)
            {
                return false;
            }

            return s_tokenChars[character];
        }

        [Pure]
        internal static int GetTokenLength(string input, int startIndex)
        {
            Debug.Assert(input != null);
            Contract.Ensures((Contract.Result<int>() >= 0) && (Contract.Result<int>() <= (input.Length - startIndex)));

            if (startIndex >= input.Length)
            {
                return 0;
            }

            int current = startIndex;

            while (current < input.Length)
            {
                if (!IsTokenChar(input[current]))
                {
                    return current - startIndex;
                }
                current++;
            }
            return input.Length - startIndex;
        }

        internal static int GetWhitespaceLength(string input, int startIndex)
        {
            Debug.Assert(input != null);
            Contract.Ensures((Contract.Result<int>() >= 0) && (Contract.Result<int>() <= (input.Length - startIndex)));

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

                if (c == '\r')
                {
                    // If we have a #13 char, it must be followed by #10 and then at least one SP or HT.
                    if ((current + 2 < input.Length) && (input[current + 1] == '\n'))
                    {
                        char spaceOrTab = input[current + 2];
                        if ((spaceOrTab == ' ') || (spaceOrTab == '\t'))
                        {
                            current += 3;
                            continue;
                        }
                    }
                }

                return current - startIndex;
            }

            // All characters between startIndex and the end of the string are LWS characters.
            return input.Length - startIndex;
        }

        internal static bool ContainsInvalidNewLine(string value)
        {
            return ContainsInvalidNewLine(value, 0);
        }

        internal static bool ContainsInvalidNewLine(string value, int startIndex)
        {
            // Search for newlines followed by non-whitespace: This is not allowed in any header (be it a known or 
            // custom header). E.g. "value\r\nbadformat: header" is invalid. However "value\r\n goodformat: header"
            // is valid: newlines followed by whitespace are allowed in header values.
            int current = startIndex;
            while (current < value.Length)
            {
                if (value[current] == '\r')
                {
                    int char10Index = current + 1;
                    if ((char10Index < value.Length) && (value[char10Index] == '\n'))
                    {
                        current = char10Index + 1;

                        if (current == value.Length)
                        {
                            return true; // We have a string terminating with \r\n. This is invalid.
                        }

                        char c = value[current];
                        if ((c != ' ') && (c != '\t'))
                        {
                            return true;
                        }
                    }
                }
                current++;
            }

            return false;
        }

        internal static int GetNumberLength(string input, int startIndex, bool allowDecimal)
        {
            Debug.Assert(input != null);
            Debug.Assert((startIndex >= 0) && (startIndex < input.Length));
            Contract.Ensures((Contract.Result<int>() >= 0) && (Contract.Result<int>() <= (input.Length - startIndex)));

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
                if ((c >= '0') && (c <= '9'))
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

        internal static int GetHostLength(string input, int startIndex, bool allowToken, out string host)
        {
            Debug.Assert(input != null);
            Debug.Assert(startIndex >= 0);
            Contract.Ensures((Contract.Result<int>() >= 0) && (Contract.Result<int>() <= (input.Length - startIndex)));

            host = null;
            if (startIndex >= input.Length)
            {
                return 0;
            }

            // A 'host' is either a token (if 'allowToken' == true) or a valid host name as defined by the URI RFC. 
            // So we first iterate through the string and search for path delimiters and whitespace. When found, stop 
            // and try to use the substring as token or URI host name. If it works, we have a host name, otherwise not.
            int current = startIndex;
            bool isToken = true;
            while (current < input.Length)
            {
                char c = input[current];
                if (c == '/')
                {
                    return 0; // Host header must not contain paths. 
                }

                if ((c == ' ') || (c == '\t') || (c == '\r') || (c == ','))
                {
                    break; // We hit a delimiter (',' or whitespace). Stop here.
                }

                isToken = isToken && IsTokenChar(c);

                current++;
            }

            int length = current - startIndex;
            if (length == 0)
            {
                return 0;
            }

            string result = input.Substring(startIndex, length);
            if ((!allowToken || !isToken) && !IsValidHostName(result))
            {
                return 0;
            }

            host = result;
            return length;
        }

        internal static HttpParseResult GetCommentLength(string input, int startIndex, out int length)
        {
            int nestedCount = 0;
            return GetExpressionLength(input, startIndex, '(', ')', true, ref nestedCount, out length);
        }

        internal static HttpParseResult GetQuotedStringLength(string input, int startIndex, out int length)
        {
            int nestedCount = 0;
            return GetExpressionLength(input, startIndex, '"', '"', false, ref nestedCount, out length);
        }

        // quoted-pair = "\" CHAR
        // CHAR = <any US-ASCII character (octets 0 - 127)>
        internal static HttpParseResult GetQuotedPairLength(string input, int startIndex, out int length)
        {
            Debug.Assert(input != null);
            Debug.Assert((startIndex >= 0) && (startIndex < input.Length));
            Contract.Ensures((Contract.ValueAtReturn(out length) >= 0) &&
                (Contract.ValueAtReturn(out length) <= (input.Length - startIndex)));

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

        internal static string DateToString(DateTimeOffset dateTime)
        {
            // Format according to RFC1123; 'r' uses invariant info (DateTimeFormatInfo.InvariantInfo).
            return dateTime.ToUniversalTime().ToString("r", CultureInfo.InvariantCulture);
        }

        internal static bool TryStringToDate(string input, out DateTimeOffset result)
        {
            // Try the various date formats in the order listed above. 
            // We should accept a wide variety of common formats, but only output RFC 1123 style dates.
            if (DateTimeOffset.TryParseExact(input, s_dateFormats, DateTimeFormatInfo.InvariantInfo,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal, out result))
            {
                return true;
            }

            return false;
        }

        // TEXT = <any OCTET except CTLs, but including LWS>
        // LWS = [CRLF] 1*( SP | HT )
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
            char closeChar, bool supportsNesting, ref int nestedCount, out int length)
        {
            Debug.Assert(input != null);
            Debug.Assert((startIndex >= 0) && (startIndex < input.Length));
            Contract.Ensures((Contract.Result<HttpParseResult>() != HttpParseResult.Parsed) ||
                (Contract.ValueAtReturn<int>(out length) > 0));

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
                int quotedPairLength = 0;
                if ((current + 2 < input.Length) &&
                    (GetQuotedPairLength(input, current, out quotedPairLength) == HttpParseResult.Parsed))
                {
                    // We ignore invalid quoted-pairs. Invalid quoted-pairs may mean that it looked like a quoted pair,
                    // but we actually have a quoted-string: e.g. "\ü" ('\' followed by a char >127 - quoted-pair only
                    // allows ASCII chars after '\'; qdtext allows both '\' and >127 chars).
                    current = current + quotedPairLength;
                    continue;
                }

                // If we support nested expressions and we find an open-char, then parse the nested expressions.
                if (supportsNesting && (input[current] == openChar))
                {
                    nestedCount++;
                    try
                    {
                        // Check if we exceeded the number of nested calls.
                        if (nestedCount > maxNestedCount)
                        {
                            return HttpParseResult.InvalidFormat;
                        }

                        int nestedLength = 0;
                        HttpParseResult nestedResult = GetExpressionLength(input, current, openChar, closeChar,
                            supportsNesting, ref nestedCount, out nestedLength);

                        switch (nestedResult)
                        {
                            case HttpParseResult.Parsed:
                                current += nestedLength; // Add the length of the nested expression and continue.
                                break;

                            case HttpParseResult.NotParsed:
                                Debug.Assert(false, "'NotParsed' is unexpected: We started nested expression " +
                                    "parsing, because we found the open-char. So either it's a valid nested " +
                                    "expression or it has invalid format.");
                                break;

                            case HttpParseResult.InvalidFormat:
                                // If the nested expression is invalid, we can't continue, so we fail with invalid format.
                                return HttpParseResult.InvalidFormat;

                            default:
                                Debug.Assert(false, "Unknown enum result: " + nestedResult);
                                break;
                        }
                    }
                    finally
                    {
                        nestedCount--;
                    }
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

        private static bool IsValidHostName(string host)
        {
            // Also add user info (u@) to make sure 'host' doesn't include user info.
            Uri hostUri;
            return Uri.TryCreate("http://u@" + host + "/", UriKind.Absolute, out hostUri);
        }
    }
}
