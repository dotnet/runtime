// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Net;
using System.Web;

namespace WebServer
{
    public static class RequestHelper
    {
        public static void AddResponseCookies(HttpContext context)
        {
            // Turn all 'X-SetCookie' request headers into 'Set-Cookie' response headers.
            string headerName;
            string headerValue;
            for (int i = 0; i < context.Request.Headers.Count; i++)
            {
                headerName = context.Request.Headers.Keys[i];
                headerValue = context.Request.Headers[i];

                if (string.Equals(headerName, "X-SetCookie", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.Headers.Add("Set-Cookie", headerValue);
                }
            }
        }

        public static CookieCollection GetRequestCookies(HttpRequest request)
        {
            var cookieCollection = new CookieCollection();
            HttpCookieCollection cookies = request.Cookies;

            for (int i = 0; i < cookies.Count; i++)
            {
                var cookie = new Cookie(cookies[i].Name, cookies[i].Value);
                cookieCollection.Add(cookie);
            }

            return cookieCollection;
        }
    }
}
