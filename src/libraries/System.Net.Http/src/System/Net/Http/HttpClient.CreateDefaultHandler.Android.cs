// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Http
{
    public partial class HttpClient
    {
        private const string CustomHandlerTypeEnvVar = "USE_NATIVE_HTTP_HANDLER";

        private static Type? s_customHandlerType;

        private static HttpMessageHandler CreateDefaultHandler()
        {
            if (s_customHandlerType != null)
            {
                return (HttpMessageHandler)Activator.CreateInstance(s_customHandlerType)!;
            }
            if (bool.TryParse(Environment.GetEnvironmentVariable(CustomHandlerTypeEnvVar), out bool useNativeHandler) && useNativeHandler)
            {
                s_customHandlerType = Type.GetType("Xamarin.Android.Net.AndroidClientHandler, Mono.Android");
                return (HttpMessageHandler)Activator.CreateInstance(s_customHandlerType!)!;
            }
            return new HttpClientHandler();
        }
    }
}
