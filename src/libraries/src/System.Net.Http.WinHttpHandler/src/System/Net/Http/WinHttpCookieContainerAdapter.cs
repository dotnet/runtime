// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

using SafeWinHttpHandle = Interop.WinHttp.SafeWinHttpHandle;

namespace System.Net.Http
{
    internal static class WinHttpCookieContainerAdapter
    {
        private const string CookieHeaderNameWithColon = "Cookie" + ":";

        public static void AddResponseCookiesToContainer(WinHttpRequestState state)
        {
            HttpRequestMessage request = state.RequestMessage;
            SafeWinHttpHandle requestHandle = state.RequestHandle;
            CookieContainer cookieContainer = state.Handler.CookieContainer;

            Debug.Assert(state.Handler.CookieUsePolicy == CookieUsePolicy.UseSpecifiedCookieContainer);
            Debug.Assert(cookieContainer != null);

            // Get 'Set-Cookie' headers from response.
            char[] buffer = null;
            uint index = 0;
            string cookieHeader;
            WinHttpTraceHelper.Trace("WINHTTP_QUERY_SET_COOKIE");
            while (WinHttpResponseParser.GetResponseHeader(
                requestHandle, Interop.WinHttp.WINHTTP_QUERY_SET_COOKIE, ref buffer, ref index, out cookieHeader))
            {
                WinHttpTraceHelper.Trace(cookieHeader);
                try
                {
                    cookieContainer.SetCookies(request.RequestUri, cookieHeader);
                    WinHttpTraceHelper.Trace(cookieHeader);
                }
                catch (CookieException)
                {
                    // We ignore malformed cookies in the response.
                    WinHttpTraceHelper.Trace("Ignoring invalid cookie: {0}", cookieHeader);
                }
            }
        }

        public static void ResetCookieRequestHeaders(WinHttpRequestState state, Uri redirectUri)
        {
            SafeWinHttpHandle requestHandle = state.RequestHandle;
            
            Debug.Assert(state.Handler.CookieUsePolicy == CookieUsePolicy.UseSpecifiedCookieContainer);
            
            // Clear cookies.
            if (!Interop.WinHttp.WinHttpAddRequestHeaders(
                requestHandle,
                CookieHeaderNameWithColon,
                (uint)CookieHeaderNameWithColon.Length,
                Interop.WinHttp.WINHTTP_ADDREQ_FLAG_REPLACE))
            {
                int lastError = Marshal.GetLastWin32Error();
                if (lastError != Interop.WinHttp.ERROR_WINHTTP_HEADER_NOT_FOUND)
                {
                    throw WinHttpException.CreateExceptionUsingError(lastError);
                }
            }

            // Re-add cookies. The GetCookieHeader() method will return the correct set of
            // cookies based on the redirectUri.
            string cookieHeader = GetCookieHeader(redirectUri, state.Handler.CookieContainer);
            if (!string.IsNullOrEmpty(cookieHeader))
            {
                if (!Interop.WinHttp.WinHttpAddRequestHeaders(
                    requestHandle,
                    cookieHeader,
                    (uint)cookieHeader.Length,
                    Interop.WinHttp.WINHTTP_ADDREQ_FLAG_ADD))
                {
                    WinHttpException.ThrowExceptionUsingLastError();
                }
            }
        }
        
        public static string GetCookieHeader(Uri uri, CookieContainer cookies)
        {
            string cookieHeader = null;

            Debug.Assert(cookies != null);

            string cookieValues = cookies.GetCookieHeader(uri);
            if (!string.IsNullOrEmpty(cookieValues))
            {
                cookieHeader = CookieHeaderNameWithColon + " " + cookieValues;                
            }

            return cookieHeader;
        }        
    }
}
