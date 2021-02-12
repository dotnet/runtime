// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace System.Net.Http
{
    public partial class HttpClient
    {
        private const string HandlerSwitchName = "System.Net.Http.UseNativeHttpHandler";

        private static MethodInfo? handlerMethod;

        private static HttpMessageHandler CreateDefaultHandler()
        {
            // Default is to use the Android native handler
            if (!AppContext.TryGetSwitch(HandlerSwitchName, out bool isEnabled))
            {
                return new HttpClientHandler();
            }

            if (handlerMethod == null)
            {
                Type? androidEnv = Type.GetType("Android.Runtime.AndroidEnvironment, Mono.Android");
                handlerMethod = androidEnv!.GetMethod("GetHttpMessageHandler", BindingFlags.Public | BindingFlags.Static)!;
            }

            return (HttpMessageHandler)handlerMethod!.Invoke(null, null)!;
        }
    }
}
