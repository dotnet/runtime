// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Net.Http
{
    public partial class HttpClientHandler : HttpMessageHandler
    {
        private static MethodInfo? _underlyingHandlerMethod;

        [DynamicDependency("get_UseCookies", "System.Net.Http.NSUrlSessionHandler", "Xamarin.TVOS")]
        private bool GetUseCookies() => (bool)InvokeNativeHandlerMethod("get_UseCookies");

        [DynamicDependency("set_UseCookies", "System.Net.Http.NSUrlSessionHandler", "Xamarin.TVOS")]
        private void SetUseCookies(bool value) => InvokeNativeHandlerMethod("set_UseCookies", value);

        [DynamicDependency("get_CookieContainer", "System.Net.Http.NSUrlSessionHandler", "Xamarin.TVOS")]
        private CookieContainer GetCookieContainer() => (CookieContainer)InvokeNativeHandlerMethod("get_CookieContainer");

        [DynamicDependency("set_CookieContainer", "System.Net.Http.NSUrlSessionHandler", "Xamarin.TVOS")]
        private void SetCookieContainer(CookieContainer value) => InvokeNativeHandlerMethod("set_CookieContainer", value);

        [DynamicDependency("get_AllowAutoRedirect", "System.Net.Http.NSUrlSessionHandler", "Xamarin.TVOS")]
        private bool GetAllowAutoRedirect() => (bool)InvokeNativeHandlerMethod("get_AllowAutoRedirect");

        [DynamicDependency("set_AllowAutoRedirect", "System.Net.Http.NSUrlSessionHandler", "Xamarin.TVOS")]
        void SetAllowAutoRedirect(bool value) => InvokeNativeHandlerMethod("set_AllowAutoRedirect", value);

        [DynamicDependency("get_Credentials", "System.Net.Http.NSUrlSessionHandler", "Xamarin.TVOS")]
        private ICredentials GetCredentials() => (ICredentials)InvokeNativeHandlerMethod("get_Credentials");

        [DynamicDependency("set_Credentials", "System.Net.Http.NSUrlSessionHandler", "Xamarin.TVOS")]
        private void SetCredentials(ICredentials? value) => InvokeNativeHandlerMethod("set_Credentials", value);

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern",
            Justification = "Xamarin dependencies are not available during libraries build")]
        private HttpMessageHandler CreateNativeHandler()
        {
            if (_underlyingHandlerMethod == null)
            {
                Type? runtimeOptions = Type.GetType("ObjCRuntime.RuntimeOptions, Xamarin.TVOS");
                _underlyingHandlerMethod = runtimeOptions!.GetMethod("GetHttpMessageHandler", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            }

            return (HttpMessageHandler)_underlyingHandlerMethod!.Invoke(null, null)!;
        }
    }
}