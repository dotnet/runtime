// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Http.Headers;
using System.Diagnostics;
using System.Collections.Generic;

namespace System.Net.Http
{
    internal static class CookieHelper
    {
        public static void ProcessReceivedCookies(HttpResponseMessage response, CookieContainer cookieContainer)
        {
            IEnumerable<string> values;
            if (response.Headers.TryGetValues(KnownHeaders.SetCookie.Descriptor, out values))
            {
                // The header values are always a string[]
                var valuesArray = (string[])values;
                Debug.Assert(valuesArray.Length > 0, "No values for header??");

                Uri requestUri = response.RequestMessage.RequestUri;
                for (int i = 0; i < valuesArray.Length; i++)
                {
                    try
                    {
                        cookieContainer.SetCookies(requestUri, valuesArray[i]);
                    }
                    catch (CookieException)
                    {
                        // Ignore invalid Set-Cookie header and continue processing.
                        if (NetEventSource.IsEnabled)
                        {
                            NetEventSource.Error(response, $"Invalid Set-Cookie '{valuesArray[i]}' ignored.");
                        }
                    }
                }
            }
        }
    }
}
