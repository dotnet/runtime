// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Net.Http
{
    internal static class CookieHelper
    {
        public static void ProcessReceivedCookies(HttpResponseMessage response, CookieContainer cookieContainer)
        {
            IEnumerable<string> setCookies;
            if (response.Headers.TryGetValues(HttpKnownHeaderNames.SetCookie, out setCookies))
            {
                Uri requestUri = response.RequestMessage.RequestUri;
                foreach (string setCookie in setCookies)
                {
                    cookieContainer.SetCookies(requestUri, setCookie);
                }
            }
        }
    }
}
