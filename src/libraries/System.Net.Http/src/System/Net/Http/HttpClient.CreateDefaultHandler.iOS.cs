// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Http
{
    public partial class HttpClient
    {
        private const string HandlerSwitchName = "System.Net.Http.UseNativeHttpHandler";

        private static HttpMessageHandler CreateDefaultHandler()
        {
            // Default is to use the Android native handler
            if (!AppContext.TryGetSwitch(HandlerSwitchName, out bool isEnabled))
            {
                return new HttpClientHandler();
            }

            Type? handler = Type.GetType("Foundation.NSUrlSessionHandler, Xamarin.iOS");
            return (HttpMessageHandler)Activator.CreateInstance(handler!)!;
        }
    }
}
