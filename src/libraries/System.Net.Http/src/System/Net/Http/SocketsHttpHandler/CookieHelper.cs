// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http.Headers;

namespace System.Net.Http
{
    internal static class CookieHelper
    {
        public static void ProcessReceivedCookies(HttpResponseMessage response, CookieContainer cookieContainer)
        {
            if (response.Headers.TryGetValues(KnownHeaders.SetCookie.Descriptor, out IEnumerable<string>? values))
            {
                // The header values are always a string[]
                var valuesArray = (string[])values;
                Debug.Assert(valuesArray.Length > 0, "No values for header??");
                Debug.Assert(response.RequestMessage != null && response.RequestMessage.RequestUri != null);

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
                        if (NetEventSource.Log.IsEnabled())
                        {
                            NetEventSource.Error(response, $"Invalid Set-Cookie '{valuesArray[i]}' ignored.");
                        }
                    }
                }
            }
        }
    }
}
