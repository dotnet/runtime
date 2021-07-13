// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Versioning;

namespace System.Net.Http
{
    public partial class HttpClientHandler : HttpMessageHandler
    {
        private static MethodInfo? _nativeHandlerMethod;

        private const string NativeHandlerType = "Xamarin.Android.Net.AndroidMessageHandler";
        private const string AssemblyName = "Mono.Android";

        public virtual bool SupportsAutomaticDecompression => true;
        public virtual bool SupportsProxy => true;
        public virtual bool SupportsRedirectConfiguration => true;

        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("maccatalyst")]
        public DecompressionMethods AutomaticDecompression
        {
            get
            {
                if (IsNativeHandlerEnabled)
                {
                    return GetAutomaticDecompression();
                }
                else
                {
                    return _socketHandler!.AutomaticDecompression;
                }
            }
            set
            {
                if (IsNativeHandlerEnabled)
                {
                    SetAutomaticDecompression(value);
                }
                else
                {
                    _socketHandler!.AutomaticDecompression = value;
                }
            }
        }

        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("maccatalyst")]
        public bool UseProxy
        {
            get
            {
                if (IsNativeHandlerEnabled)
                {
                    return GetUseProxy();
                }
                else
                {
                    return _socketHandler!.UseProxy;
                }
            }
            set
            {
                if (IsNativeHandlerEnabled)
                {
                    SetUseProxy(value);
                }
                else
                {
                    _socketHandler!.UseProxy = value;
                }
            }
        }

        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("maccatalyst")]
        public IWebProxy? Proxy
        {
            get
            {
                if (IsNativeHandlerEnabled)
                {
                    return GetProxy();
                }
                else
                {
                    return _socketHandler!.Proxy;
                }
            }
            set
            {
                if (IsNativeHandlerEnabled)
                {
                    SetProxy(value!);
                }
                else
                {
                    _socketHandler!.Proxy = value;
                }
            }
        }

        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("maccatalyst")]
        public bool PreAuthenticate
        {
            get
            {
                if (IsNativeHandlerEnabled)
                {
                    return GetPreAuthenticate();
                }
                else
                {
                    return _socketHandler!.PreAuthenticate;
                }
            }
            set
            {
                if (IsNativeHandlerEnabled)
                {
                    SetPreAuthenticate(value);
                }
                else
                {
                    _socketHandler!.PreAuthenticate = value;
                }
            }
        }

        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("maccatalyst")]
        public int MaxAutomaticRedirections
        {
            get
            {
                if (IsNativeHandlerEnabled)
                {
                    return GetMaxAutomaticRedirections();
                }
                else
                {
                    return _socketHandler!.MaxAutomaticRedirections;
                }
            }
            set
            {
                if (IsNativeHandlerEnabled)
                {
                    SetMaxAutomaticRedirections(value);
                }
                else
                {
                    _socketHandler!.MaxAutomaticRedirections = value;
                }
            }
        }

        [DynamicDependency("get_MaxAutomaticRedirections", NativeHandlerType, AssemblyName)]
        private int GetMaxAutomaticRedirections() => (int)InvokeNativeHandlerMethod("get_MaxAutomaticRedirections");

        [DynamicDependency("set_MaxAutomaticRedirections", NativeHandlerType, AssemblyName)]
        private void SetMaxAutomaticRedirections(int value) => InvokeNativeHandlerMethod("set_MaxAutomaticRedirections", value);

        [DynamicDependency("get_PreAuthenticate", NativeHandlerType, AssemblyName)]
        private bool GetPreAuthenticate() => (bool)InvokeNativeHandlerMethod("get_PreAuthenticate");

        [DynamicDependency("set_PreAuthenticate", NativeHandlerType, AssemblyName)]
        private void SetPreAuthenticate(bool value) => InvokeNativeHandlerMethod("set_PreAuthenticate", value);

        [DynamicDependency("get_UseProxy", NativeHandlerType, AssemblyName)]
        private bool GetUseProxy() => (bool)InvokeNativeHandlerMethod("get_UseProxy");

        [DynamicDependency("set_UseProxy", NativeHandlerType, AssemblyName)]
        private void SetUseProxy(bool value) => InvokeNativeHandlerMethod("set_UseProxy", value);

        [DynamicDependency("get_Proxy", NativeHandlerType, AssemblyName)]
        private IWebProxy GetProxy() => (IWebProxy)InvokeNativeHandlerMethod("get_Proxy");

        [DynamicDependency("set_Proxy", NativeHandlerType, AssemblyName)]
        private void SetProxy(IWebProxy value) => InvokeNativeHandlerMethod("set_Proxy", value);

        [DynamicDependency("get_AutomaticDecompression", NativeHandlerType, AssemblyName)]
        private DecompressionMethods GetAutomaticDecompression() => (DecompressionMethods)InvokeNativeHandlerMethod("get_AutomaticDecompression");

        [DynamicDependency("set_AutomaticDecompression", NativeHandlerType, AssemblyName)]
        private void SetAutomaticDecompression(DecompressionMethods value) => InvokeNativeHandlerMethod("set_AutomaticDecompression", value);

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
                Type? androidEnv = Type.GetType("Android.Runtime.AndroidEnvironment, Mono.Android");
                _nativeHandlerMethod = androidEnv!.GetMethod("GetHttpMessageHandler", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            }

            return (HttpMessageHandler)_nativeHandlerMethod!.Invoke(null, null)!;
        }
    }
}