// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net
{
    internal static class HttpValidationHelpers
    {
        internal static string CheckBadHeaderNameChars(string name)
        {
            // First, check for absence of separators and spaces.
            if (IsInvalidMethodOrHeaderString(name))
            {
                throw new ArgumentException(SR.net_WebHeaderInvalidHeaderChars, nameof(name));
            }

            // Second, check for non CTL ASCII-7 characters (32-126).
            if (ContainsNonAsciiChars(name))
            {
                throw new ArgumentException(SR.net_WebHeaderInvalidHeaderChars, nameof(name));
            }
            return name;
        }

        internal static bool ContainsNonAsciiChars(string token)
        {
            for (int i = 0; i < token.Length; ++i)
            {
                if ((token[i] < 0x20) || (token[i] > 0x7e))
                {
                    return true;
                }
            }
            return false;
        }

        internal static bool IsValidToken(string token)
        {
            return (token.Length > 0)
                && !IsInvalidMethodOrHeaderString(token)
                && !ContainsNonAsciiChars(token);
        }

        private static readonly char[] s_httpTrimCharacters = new char[] { (char)0x09, (char)0xA, (char)0xB, (char)0xC, (char)0xD, (char)0x20 };

        /// <summary>
        /// Throws on invalid header value chars.
        /// </summary>
        public static string CheckBadHeaderValueChars(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                // empty value is OK.
                return string.Empty;
            }

            // Trim spaces from both ends.
            value = value.Trim(s_httpTrimCharacters);

            // First, check for correctly formed multi-line value.
            // Second, check for absence of CTL characters.
            int crlf = 0;
            for (int i = 0; i < value.Length; ++i)
            {
                char c = (char)(0x000000ff & (uint)value[i]);
                switch (crlf)
                {
                    case 0:
                        if (c == '\r')
                        {
                            crlf = 1;
                        }
                        else if (c == '\n')
                        {
                            // Technically this is bad HTTP, but we want to be permissive in what we accept.
                            // It is important to note that it would be a breaking change to reject this.
                            crlf = 2;
                        }
                        else if (c == 127 || (c < ' ' && c != '\t'))
                        {
                            throw new ArgumentException(SR.net_WebHeaderInvalidControlChars, nameof(value));
                        }
                        break;

                    case 1:
                        if (c == '\n')
                        {
                            crlf = 2;
                            break;
                        }
                        throw new ArgumentException(SR.net_WebHeaderInvalidCRLFChars, nameof(value));

                    case 2:
                        if (c == ' ' || c == '\t')
                        {
                            crlf = 0;
                            break;
                        }
                        throw new ArgumentException(SR.net_WebHeaderInvalidControlChars, nameof(value));
                }
            }

            if (crlf != 0)
            {
                throw new ArgumentException(SR.net_WebHeaderInvalidCRLFChars, nameof(value));
            }

            return value;
        }


        // Returns true if stringValue contains characters that cannot appear
        // in a valid method-verb or HTTP header.
        public static bool IsInvalidMethodOrHeaderString(string stringValue)
        {
            for (int i = 0; i < stringValue.Length; i++)
            {
                switch (stringValue[i])
                {
                    case '(':
                    case ')':
                    case '<':
                    case '>':
                    case '@':
                    case ',':
                    case ';':
                    case ':':
                    case '\\':
                    case '"':
                    case '\'':
                    case '/':
                    case '[':
                    case ']':
                    case '?':
                    case '=':
                    case '{':
                    case '}':
                    case ' ':
                    case '\t':
                    case '\r':
                    case '\n':
                        return true;

                    default:
                        break;
                }
            }

            return false;
        }
    }
}
