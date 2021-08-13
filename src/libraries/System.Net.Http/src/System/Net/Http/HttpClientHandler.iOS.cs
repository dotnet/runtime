// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Net.Http
{
    public partial class HttpClientHandler : HttpMessageHandler
    {
        private static MethodInfo? _nativeHandlerMethod;

        private const string NativeHandlerType = "System.Net.Http.NSUrlSessionHandler";
        private const string AssemblyName = "Xamarin.iOS";

        [DynamicDependency("get_UseCookies", NativeHandlerType, AssemblyName)]
        private bool GetUseCookies() => (bool)InvokeNativeHandlerMethod("get_UseCookies");

        [DynamicDependency("set_UseCookies", NativeHandlerType, AssemblyName)]
        private void SetUseCookies(bool value) => InvokeNativeHandlerMethod("set_UseCookies", value);

        [DynamicDependency("get_CookieContainer", NativeHandlerType, AssemblyName)]
        private CookieContainer GetCookieContainer() => (CookieContainer)InvokeNativeHandlerMethod("get_CookieContainer");

        [DynamicDependency("set_CookieContainer", NativeHandlerType, AssemblyName)]
        private void SetCookieContainer(CookieContainer value) => InvokeNativeHandlerMethod("set_CookieContainer", value);

        [DynamicDependency("get_AllowAutoRedirect", NativeHandlerType, AssemblyName)]
        private bool GetAllowAutoRedirect() => (bool)InvokeNativeHandlerMethod("get_AllowAutoRedirect");

        [DynamicDependency("set_AllowAutoRedirect", NativeHandlerType, AssemblyName)]
        private void SetAllowAutoRedirect(bool value) => InvokeNativeHandlerMethod("set_AllowAutoRedirect", value);

        [DynamicDependency("get_Credentials", NativeHandlerType, AssemblyName)]
        private ICredentials GetCredentials() => (ICredentials)InvokeNativeHandlerMethod("get_Credentials");

        [DynamicDependency("set_Credentials", NativeHandlerType, AssemblyName)]
        private void SetCredentials(ICredentials? value) => InvokeNativeHandlerMethod("set_Credentials", value);

        private HttpMessageHandler CreateNativeHandler()
        {
            if (_nativeHandlerMethod == null)
            {
                Type? runtimeOptions = Type.GetType("ObjCRuntime.RuntimeOptions, Xamarin.iOS");
                _nativeHandlerMethod = runtimeOptions!.GetMethod("GetHttpMessageHandler", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            }

            return (HttpMessageHandler)_nativeHandlerMethod!.Invoke(null, null)!;
        }
    }
}