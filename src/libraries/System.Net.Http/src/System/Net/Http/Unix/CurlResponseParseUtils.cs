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

        private static void SkipMandatorySpace(string responseHeader, ref int index)
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

            CheckResponseMsgFormat(foundSpace);
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

            SkipMandatorySpace(responseHeader, ref index);

            //  parse status code
            int statusCode = ReadInt(responseHeader, ref index);
            CheckResponseMsgFormat(statusCode >= 100 && statusCode < 600);

            SkipMandatorySpace(responseHeader, ref index);

            // we need reason phrase
            CheckResponseMsgFormat( index < responseHeader.Length);

            // Check if version is  HTTP/1.1 or HTTP/1.0
            if (majorVersion == 1)
            {
                response.Version =
                    minorVersion == 1 ? HttpVersion.Version11 :
                    minorVersion == 0 ? HttpVersion.Version10 :
                    new Version(0, 0);
            }
            else
            {
                response.Version = new Version(0, 0);
            }

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

