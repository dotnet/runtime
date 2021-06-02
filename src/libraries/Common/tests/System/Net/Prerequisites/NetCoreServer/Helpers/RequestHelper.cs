// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace NetCoreServer
{
    public class RequestHelper
    {
        public static void AddResponseCookies(HttpContext context)
        {
            // Turn all 'X-SetCookie' request headers into 'Set-Cookie' response headers.
            foreach (KeyValuePair<string, StringValues> pair in context.Request.Headers)
            {
                if (string.Equals(pair.Key, "X-SetCookie", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.Headers.Add("Set-Cookie", pair.Value.ToString());
                }
            }
        }

        public static CookieCollection GetRequestCookies(HttpRequest request)
        {
            var cookieCollection = new CookieCollection();
            foreach (KeyValuePair<string, string> pair in request.Cookies)
            {
                var cookie = new Cookie(pair.Key, pair.Value);
                cookieCollection.Add(cookie);
            }

            return cookieCollection;
        }
    }
}
