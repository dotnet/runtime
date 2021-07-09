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
        private static MethodInfo? _underlyingHandlerMethod;

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
                if (IsSocketHandler)
                {
                    return _socketHandler!.AutomaticDecompression;
                }
                else
                {
                    return GetAutomaticDecompression();
                }
            }
            set
            {
                if (IsSocketHandler)
                {
                    _socketHandler!.AutomaticDecompression = value;
                }
                else
                {
                    SetAutomaticDecompression(value);
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
                if (IsSocketHandler)
                {
                    return _socketHandler!.UseProxy;
                }
                else
                {
                    return GetUseProxy();
                }
            }
            set
            {
                if (IsSocketHandler)
                {
                    _socketHandler!.UseProxy = value;
                }
                else
                {
                    SetUseProxy(value);
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
                if (IsSocketHandler)
                {
                    return _socketHandler!.Proxy;
                }
                else
                {
                    return GetProxy();
                }
            }
            set
            {
                if (IsSocketHandler)
                {
                    _socketHandler!.Proxy = value;
                }
                else
                {
                    SetProxy(value!);
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
                if (IsSocketHandler)
                {
                    return _socketHandler!.PreAuthenticate;
                }
                else
                {
                    return GetPreAuthenticate();
                }
            }
            set
            {
                if (IsSocketHandler)
                {
                    _socketHandler!.PreAuthenticate = value;
                }
                else
                {
                    SetPreAuthenticate(value);
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
                if (IsSocketHandler)
                {
                    return _socketHandler!.MaxAutomaticRedirections;
                }
                else
                {
                    return GetMaxAutomaticRedirections();
                }
            }
            set
            {
                if (IsSocketHandler)
                {
                    _socketHandler!.MaxAutomaticRedirections = value;
                }
                else
                {
                    SetMaxAutomaticRedirections(value);
                }
            }
        }

        [DynamicDependency("get_MaxAutomaticRedirections", "Xamarin.Android.Net.AndroidMessageHandler", "Mono.Android")]
        private int GetMaxAutomaticRedirections() => (int)InvokeNativeHandlerMethod("get_MaxAutomaticRedirections");

        [DynamicDependency("set_MaxAutomaticRedirections", "Xamarin.Android.Net.AndroidMessageHandler", "Mono.Android")]
        private void SetMaxAutomaticRedirections(int value) => InvokeNativeHandlerMethod("set_MaxAutomaticRedirections", value);

        [DynamicDependency("get_PreAuthenticate", "Xamarin.Android.Net.AndroidMessageHandler", "Mono.Android")]
        private bool GetPreAuthenticate() => (bool)InvokeNativeHandlerMethod("get_PreAuthenticate");

        [DynamicDependency("set_PreAuthenticate", "Xamarin.Android.Net.AndroidMessageHandler", "Mono.Android")]
        private void SetPreAuthenticate(bool value) => InvokeNativeHandlerMethod("set_PreAuthenticate", value);

        [DynamicDependency("get_UseProxy", "Xamarin.Android.Net.AndroidMessageHandler", "Mono.Android")]
        private bool GetUseProxy() => (bool)InvokeNativeHandlerMethod("get_UseProxy");

        [DynamicDependency("set_UseProxy", "Xamarin.Android.Net.AndroidMessageHandler", "Mono.Android")]
        private void SetUseProxy(bool value) => InvokeNativeHandlerMethod("set_UseProxy", value);

        [DynamicDependency("get_Proxy", "Xamarin.Android.Net.AndroidMessageHandler", "Mono.Android")]
        private IWebProxy GetProxy() => (IWebProxy)InvokeNativeHandlerMethod("get_Proxy");

        [DynamicDependency("set_Proxy", "Xamarin.Android.Net.AndroidMessageHandler", "Mono.Android")]
        private void SetProxy(IWebProxy value) => InvokeNativeHandlerMethod("set_Proxy", value);

        [DynamicDependency("get_AutomaticDecompression", "Xamarin.Android.Net.AndroidMessageHandler", "Mono.Android")]
        private DecompressionMethods GetAutomaticDecompression() => (DecompressionMethods)InvokeNativeHandlerMethod("get_AutomaticDecompression");

        [DynamicDependency("set_AutomaticDecompression", "Xamarin.Android.Net.AndroidMessageHandler", "Mono.Android")]
        private void SetAutomaticDecompression(DecompressionMethods value) => InvokeNativeHandlerMethod("set_AutomaticDecompression", value);

        [DynamicDependency("get_UseCookies", "Xamarin.Android.Net.AndroidMessageHandler", "Mono.Android")]
        private bool GetUseCookies() => (bool)InvokeNativeHandlerMethod("get_UseCookies");

        [DynamicDependency("set_UseCookies", "Xamarin.Android.Net.AndroidMessageHandler", "Mono.Android")]
        private void SetUseCookies(bool value) => InvokeNativeHandlerMethod("set_UseCookies", value);

        [DynamicDependency("get_CookieContainer", "Xamarin.Android.Net.AndroidMessageHandler", "Mono.Android")]
        private CookieContainer GetCookieContainer() => (CookieContainer)InvokeNativeHandlerMethod("get_CookieContainer");

        [DynamicDependency("set_CookieContainer", "Xamarin.Android.Net.AndroidMessageHandler", "Mono.Android")]
        private void SetCookieContainer(CookieContainer value) => InvokeNativeHandlerMethod("set_CookieContainer", value);

        [DynamicDependency("get_AllowAutoRedirect", "Xamarin.Android.Net.AndroidMessageHandler", "Mono.Android")]
        private bool GetAllowAutoRedirect() => (bool)InvokeNativeHandlerMethod("get_AllowAutoRedirect");

        [DynamicDependency("set_AllowAutoRedirect", "Xamarin.Android.Net.AndroidMessageHandler", "Mono.Android")]
        private void SetAllowAutoRedirect(bool value) => InvokeNativeHandlerMethod("set_AllowAutoRedirect", value);

        [DynamicDependency("get_Credentials", "Xamarin.Android.Net.AndroidMessageHandler", "Mono.Android")]
        private ICredentials GetCredentials() => (ICredentials)InvokeNativeHandlerMethod("get_Credentials");

        [DynamicDependency("set_Credentials", "Xamarin.Android.Net.AndroidMessageHandler", "Mono.Android")]
        private void SetCredentials(ICredentials? value) => InvokeNativeHandlerMethod("set_Credentials", value);

        private HttpMessageHandler CreateNativeHandler()
        {
            if (_underlyingHandlerMethod == null)
            {
                Type? androidEnv = Type.GetType("Android.Runtime.AndroidEnvironment, Mono.Android");
                _underlyingHandlerMethod = androidEnv!.GetMethod("GetHttpMessageHandler", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            }

            return (HttpMessageHandler)_underlyingHandlerMethod!.Invoke(null, null)!;
        }
    }
}