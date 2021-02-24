// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using SafeWinHttpHandle = Interop.WinHttp.SafeWinHttpHandle;

namespace System.Net.Http
{
    internal static class WinHttpTrailersHelper
    {
        private const string RequestMessagePropertyName = "__ResponseTrailers";

        private static Lazy<bool> s_trailersSupported = new Lazy<bool>(GetTrailersSupported);
        public static bool OsSupportsTrailers => s_trailersSupported.Value;

        public static HttpHeaders GetResponseTrailers(HttpResponseMessage response)
        {
#if NETSTANDARD2_1
            // We are (ab)using a property that became Obsolete on .NET 5
#pragma warning disable CS0618
            return response.TrailingHeaders;
#pragma warning restore CS0618
#else
            HttpResponseTrailers responseTrailers = new HttpResponseTrailers();
            response.RequestMessage.Properties[RequestMessagePropertyName] = responseTrailers;
            return responseTrailers;
#endif
        }

        // There is no way to verify if WINHTTP_QUERY_FLAG_TRAILERS is supported by the OS without creating a request.
        // Instead, the WinHTTP team recommended to check if WINHTTP_OPTION_STREAM_ERROR_CODE is recognized by the OS.
        // Both features were introduced in Manganese and are planned to be backported to older Windows versions together.
        private static bool GetTrailersSupported()
        {
            SafeWinHttpHandle sessionHandle = null;

            try
            {
                sessionHandle = Interop.WinHttp.WinHttpOpen(
                    IntPtr.Zero,
                    Interop.WinHttp.WINHTTP_ACCESS_TYPE_DEFAULT_PROXY,
                    Interop.WinHttp.WINHTTP_NO_PROXY_NAME,
                    Interop.WinHttp.WINHTTP_NO_PROXY_BYPASS,
                    (int)Interop.WinHttp.WINHTTP_FLAG_ASYNC);

                if (sessionHandle.IsInvalid) return false;
                uint buffer = 0;
                uint bufferSize = sizeof(uint);
                if (Interop.WinHttp.WinHttpQueryOption(sessionHandle, Interop.WinHttp.WINHTTP_OPTION_STREAM_ERROR_CODE, ref buffer, ref bufferSize))
                {
                    Debug.Fail("Querying WINHTTP_OPTION_STREAM_ERROR_CODE on a session handle should never succeed.");
                    return false;
                }

                int lastError = Marshal.GetLastWin32Error();

                // New Windows builds are expected to fail with ERROR_WINHTTP_INCORRECT_HANDLE_TYPE,
                // when querying WINHTTP_OPTION_STREAM_ERROR_CODE on a session handle.
                return lastError != Interop.WinHttp.ERROR_INVALID_PARAMETER;
            }
            finally
            {
                sessionHandle.Dispose();
            }
        }

#if !NETSTANDARD2_1
        private class HttpResponseTrailers : HttpHeaders
        {
        }
#endif
    }
}
