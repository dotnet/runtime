// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Net.Http
{
    public partial class HttpClientHandler : HttpMessageHandler
    {
        private object CreateNativeHandler()
        {
            if (_underlyingHandlerMethod == null)
            {
                Type? runtimeOptions = Type.GetType("ObjCRuntime.RuntimeOptions, Xamarin.MacCatalyst");
                _underlyingHandlerMethod = runtimeOptions!.GetMethod("GetHttpMessageHandler", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            }

            return _underlyingHandlerMethod!.Invoke(null, null)!;
        }
    }
}