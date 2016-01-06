// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;

using SafeWinHttpHandle = Interop.WinHttp.SafeWinHttpHandle;

namespace System.Net.Http
{
    internal static class WinHttpResponseParser
    {
        private const string EncodingNameDeflate = "DEFLATE";
        private const string EncodingNameGzip = "GZIP";
        private const string HeaderNameContentEncoding = "Content-Encoding";
        private const string HeaderNameContentLength = "Content-Length";
        private const string HeaderNameSetCookie = "Set-Cookie";
        private static readonly string[] s_HttpHeadersSeparator = { "\r\n" };

        public static HttpResponseMessage CreateResponseMessage(
            WinHttpRequestState state,
            bool doManualDecompressionCheck)
        {
            HttpRequestMessage request = state.RequestMessage;
            SafeWinHttpHandle requestHandle = state.RequestHandle;
            CookieUsePolicy cookieUsePolicy = state.Handler.CookieUsePolicy;
            CookieContainer cookieContainer = state.Handler.CookieContainer;
            var response = new HttpResponseMessage();
            bool stripEncodingHeaders = false;

            // Get HTTP version, status code, reason phrase from the response headers.
            string version = GetResponseHeaderStringInfo(requestHandle, Interop.WinHttp.WINHTTP_QUERY_VERSION);
            if (string.Compare("HTTP/1.1", version, StringComparison.OrdinalIgnoreCase) == 0)
            {
                response.Version = HttpVersion.Version11;
            }
            else if (string.Compare("HTTP/1.0", version, StringComparison.OrdinalIgnoreCase) == 0)
            {
                response.Version = HttpVersion.Version10;
            }
            else
            {
                response.Version = HttpVersion.Unknown;
            }

            response.StatusCode = (HttpStatusCode)GetResponseHeaderNumberInfo(
                requestHandle,
                Interop.WinHttp.WINHTTP_QUERY_STATUS_CODE);
            response.ReasonPhrase = GetResponseHeaderStringInfo(
                requestHandle,
                Interop.WinHttp.WINHTTP_QUERY_STATUS_TEXT);

            // Create response stream and wrap it in a StreamContent object.
            var responseStream = new WinHttpResponseStream(state);
            Stream decompressedStream = responseStream;

            if (doManualDecompressionCheck)
            {
                string contentEncoding = GetResponseHeaderStringInfo(
                    requestHandle,
                    Interop.WinHttp.WINHTTP_QUERY_CONTENT_ENCODING);
                if (!string.IsNullOrEmpty(contentEncoding))
                {
                    if (contentEncoding.IndexOf(EncodingNameDeflate, StringComparison.OrdinalIgnoreCase) > -1)
                    {
                        decompressedStream = new DeflateStream(responseStream, CompressionMode.Decompress);
                        stripEncodingHeaders = true;
                    }
                    else if (contentEncoding.IndexOf(EncodingNameGzip, StringComparison.OrdinalIgnoreCase) > -1)
                    {
                        decompressedStream = new GZipStream(responseStream, CompressionMode.Decompress);
                        stripEncodingHeaders = true;
                    }
                }
            }

            var content = new StreamContent(decompressedStream);

            response.Content = content;
            response.RequestMessage = request;

            // Parse raw response headers and place them into response message.
            ParseResponseHeaders(requestHandle, response, stripEncodingHeaders);

            return response;
        }

        // Returns the first header or throws if header isn't found.
        public static uint GetResponseHeaderNumberInfo(SafeWinHttpHandle requestHandle, uint infoLevel)
        {
            uint result = 0;
            uint resultSize = sizeof(uint);

            if (!Interop.WinHttp.WinHttpQueryHeaders(
                requestHandle,
                infoLevel | Interop.WinHttp.WINHTTP_QUERY_FLAG_NUMBER,
                Interop.WinHttp.WINHTTP_HEADER_NAME_BY_INDEX,
                ref result,
                ref resultSize,
                IntPtr.Zero))
            {
                WinHttpException.ThrowExceptionUsingLastError();
            }

            return result;
        }

        // Returns the first header or null if header isn't found.
        public static string GetResponseHeaderStringInfo(SafeWinHttpHandle requestHandle, uint infoLevel)
        {
            uint index = 0;
            
            return GetResponseHeaderStringHelper(requestHandle, infoLevel, ref index);
        }

        // Returns all headers or an empty list if header isn't found.
        public static List<string> GetResponseHeaders(SafeWinHttpHandle requestHandle, uint infoLevel)
        {
            uint index = 0;
            var headerList = new List<string>();
            string header;
            
            while (true)
            {
                header = GetResponseHeaderStringHelper(requestHandle, infoLevel, ref index);
                if (header == null)
                {
                    break;
                }
                
                headerList.Add(header);
            }
            
            return headerList;
        }

        private static string GetResponseHeaderStringHelper(
            SafeWinHttpHandle requestHandle,
            uint infoLevel,
            ref uint index)
        {
            uint bytesNeeded = 0;
            bool results = false;

            // Call WinHttpQueryHeaders once to obtain the size of the buffer needed.  The size is returned in
            // bytes but the API actually returns Unicode characters.
            if (!Interop.WinHttp.WinHttpQueryHeaders(
                requestHandle,
                infoLevel,
                Interop.WinHttp.WINHTTP_HEADER_NAME_BY_INDEX,
                null,
                ref bytesNeeded,
                ref index))
            {
                int lastError = Marshal.GetLastWin32Error();
                if (lastError == Interop.WinHttp.ERROR_WINHTTP_HEADER_NOT_FOUND)
                {
                    return null;
                }

                if (lastError != Interop.WinHttp.ERROR_INSUFFICIENT_BUFFER)
                {
                    throw WinHttpException.CreateExceptionUsingError(lastError);
                }
            }

            // Allocate space for the buffer.
            int charsNeeded = (int)bytesNeeded / sizeof(char);
            var buffer = new StringBuilder(charsNeeded, charsNeeded);

            results = Interop.WinHttp.WinHttpQueryHeaders(
                requestHandle,
                infoLevel,
                Interop.WinHttp.WINHTTP_HEADER_NAME_BY_INDEX,
                buffer,
                ref bytesNeeded,
                ref index);
            if (!results)
            {
                WinHttpException.ThrowExceptionUsingLastError();
            }

            return buffer.ToString();
        }

        private static void ParseResponseHeaders(
            SafeWinHttpHandle requestHandle,
            HttpResponseMessage response,
            bool stripEncodingHeaders)
        {
            string rawResponseHeaders = GetResponseHeaderStringInfo(
                requestHandle,
                Interop.WinHttp.WINHTTP_QUERY_RAW_HEADERS_CRLF);
            string[] responseHeaderArray = rawResponseHeaders.Split(
                s_HttpHeadersSeparator,
                StringSplitOptions.RemoveEmptyEntries);

            // Parse the array of headers and split them between Content headers and Response headers.
            // Skip the first line which contains status code, etc. information that we already parsed.
            for (int i = 1; i < responseHeaderArray.Length; i++)
            {
                int colonIndex = responseHeaderArray[i].IndexOf(':');

                // Skip malformed header lines that are missing the colon character.
                if (colonIndex > 0)
                {
                    string headerName = responseHeaderArray[i].Substring(0, colonIndex);
                    string headerValue = responseHeaderArray[i].SubstringTrim(colonIndex + 1); // Normalize header value by trimming white space.

                    if (!response.Headers.TryAddWithoutValidation(headerName, headerValue))
                    {
                        if (stripEncodingHeaders)
                        {
                            // Remove Content-Length and Content-Encoding headers if we are
                            // decompressing the response stream in the handler (due to 
                            // WINHTTP not supporting it in a particular downlevel platform). 
                            // This matches the behavior of WINHTTP when it does decompression iself.
                            if (string.Equals(
                                HeaderNameContentLength,
                                headerName,
                                StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            if (string.Equals(
                                HeaderNameContentEncoding,
                                headerName,
                                StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }
                        }

                        // TODO: Issue #2165. Should we log if there is an error here?
                        response.Content.Headers.TryAddWithoutValidation(headerName, headerValue);
                    }
                }
            }
        }
    }
}
