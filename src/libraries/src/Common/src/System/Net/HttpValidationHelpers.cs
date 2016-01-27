// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net
{
    internal static class HttpValidationHelpers
    {
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
