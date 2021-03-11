// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace System.Net.Http
{
    public partial class HttpClient
    {
        private static MethodInfo? handlerMethod;

        private static HttpMessageHandler CreateDefaultHandler()
        {
            // Default is to use the tvOS native handler
            if (!IsNativeHandlerEnabled())
            {
                return new HttpClientHandler();
            }

            if (handlerMethod == null)
            {
                Type? runtimeOptions = Type.GetType("ObjCRuntime.RuntimeOptions, Xamarin.TVOS");
                handlerMethod = runtimeOptions!.GetMethod("GetHttpMessageHandler", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            }

            return (HttpMessageHandler)handlerMethod!.Invoke(null, null)!;
        }
    }
}
