// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Net.Http
{
    internal static class CurlResponseParseUtils
    {
        internal const string HttpPrefix = "HTTP/";

        private static int ReadInt(string responseHeader, ref int index)
        {
            int value = 0;
            for ( ; index < responseHeader.Length; index++)
            {
                char c = responseHeader[index];
                if (c < '0' || c > '9')
                {
                    break;
                }

                value = (value * 10) + (c - '0');
            }

            return value;
        }

        private static bool SkipSpace(string responseHeader, ref int index)
        {
            bool foundSpace = false;
            for ( ; index < responseHeader.Length; index++)
            {
                if (responseHeader[index] == ' ' || responseHeader[index] == '\t')
                {
                    foundSpace = true;
                }
                else
                {
                    break;
                }
            }
            return foundSpace;
        }

        private static bool ValidHeaderNameChar(char c)
        {
            const string invalidChars = "()<>@,;:\\\"/[]?={}";
            return c > ' ' && invalidChars.IndexOf(c) < 0 ;
        }

        private static void CheckResponseMsgFormat(bool condition)
        {
            if (!condition)
            {
                throw new HttpRequestException(SR.net_http_unix_invalid_response);
            }
        }

        internal static void ReadStatusLine(HttpResponseMessage response, string responseHeader)
        {
            Debug.Assert(responseHeader.StartsWith(HttpPrefix, StringComparison.OrdinalIgnoreCase), "Status Line starts with http prefix");

            int index =  HttpPrefix.Length;
            int majorVersion = ReadInt(responseHeader, ref index);
            CheckResponseMsgFormat(majorVersion != 0);

            CheckResponseMsgFormat(index < responseHeader.Length && responseHeader[index] == '.');
            index++;

            // Need minor version
            CheckResponseMsgFormat(index < responseHeader.Length && responseHeader[index] >= '0' && responseHeader[index] <= '9');
            int minorVersion = ReadInt(responseHeader, ref index);

            CheckResponseMsgFormat(SkipSpace(responseHeader, ref index));

            //  parse status code
            int statusCode = ReadInt(responseHeader, ref index);
            CheckResponseMsgFormat(statusCode >= 100 && statusCode < 600);

            bool foundSpace = SkipSpace(responseHeader, ref index);
            CheckResponseMsgFormat(index <= responseHeader.Length);
            CheckResponseMsgFormat(foundSpace || index == responseHeader.Length);

            // Set the response HttpVersion
            response.Version =
                (majorVersion == 1 && minorVersion == 1) ? HttpVersion.Version11 :
                (majorVersion == 1 && minorVersion == 0) ? HttpVersion.Version10 :
                (majorVersion == 2 && minorVersion == 0) ? HttpVersion.Version20 :
                HttpVersion.Unknown;

            response.StatusCode = (HttpStatusCode)statusCode;
            response.ReasonPhrase = responseHeader.Substring(index);
        }

        internal static string ReadHeaderName(string responseHeader, out int index)
        {
            index = 0;
            while (index < responseHeader.Length && ValidHeaderNameChar(responseHeader[index]))
            {
                index++;
            }

            if (index > 0)
            {
                CheckResponseMsgFormat(index < responseHeader.Length);
                CheckResponseMsgFormat(responseHeader[index] == ':');
                string headerName = responseHeader.Substring(0, index);
                CheckResponseMsgFormat(headerName.Length > 0);
                index++;
                return headerName;
            }

            return null;
        }
    }
}
