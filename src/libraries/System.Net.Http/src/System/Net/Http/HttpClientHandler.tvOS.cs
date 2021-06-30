// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Net.Http
{
    public partial class HttpClientHandler : HttpMessageHandler
    {
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern",
            Justification = "Unused fields don't make a difference for hashcode quality")]
        private object CreateNativeHandler()
        {
            if (_underlyingHandlerMethod == null)
            {
                Type? runtimeOptions = Type.GetType("ObjCRuntime.RuntimeOptions, Xamarin.TVOS");
                _underlyingHandlerMethod = runtimeOptions!.GetMethod("GetHttpMessageHandler", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            }

            return _underlyingHandlerMethod!.Invoke(null, null)!;
        }
    }
}