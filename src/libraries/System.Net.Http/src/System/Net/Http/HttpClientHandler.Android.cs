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

        // not sure
        public virtual bool SupportsAutomaticDecompression => true;
        public virtual bool SupportsRedirectConfiguration => true;

        [UnsupportedOSPlatform("browser")]
        //[UnsupportedOSPlatform("ios")]
        //[UnsupportedOSPlatform("tvos")]
        //[UnsupportedOSPlatform("maccatalyst")]
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

                //[DynamicDependency("get_AutomaticDecompression", "Xamarin.Android.Net.AndroidMessageHandler", "Mono.Android")]
                //[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2035:Unresolved External Assemblies",
                //    Justification = "Xamarin dependencies are not available during libraries build")]
                DecompressionMethods GetAutomaticDecompression() => (DecompressionMethods)InvokeNativeHandlerMethod("get_AutomaticDecompression");
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

                //[DynamicDependency("set_AutomaticDecompression", "Xamarin.Android.Net.AndroidMessageHandler", "Mono.Android")]
                void SetAutomaticDecompression(DecompressionMethods value) => InvokeNativeHandlerMethod("set_AutomaticDecompression", value);
            }
        }

        [UnsupportedOSPlatform("browser")]
        //[UnsupportedOSPlatform("ios")]
        //[UnsupportedOSPlatform("tvos")]
        //[UnsupportedOSPlatform("maccatalyst")]
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

                //[DynamicDependency("get_UseProxy", "Xamarin.Android.Net.AndroidMessageHandler", "Mono.Android")]
                bool GetUseProxy() => (bool)InvokeNativeHandlerMethod("get_UseProxy");
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

                //[DynamicDependency("set_UseProxy", "Xamarin.Android.Net.AndroidMessageHandler", "Mono.Android")]
                void SetUseProxy(bool value) => InvokeNativeHandlerMethod("set_UseProxy", value);
            }
        }

        [UnsupportedOSPlatform("browser")]
        //[UnsupportedOSPlatform("ios")]
        //[UnsupportedOSPlatform("tvos")]
        //[UnsupportedOSPlatform("maccatalyst")]
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

                //[DynamicDependency("get_PreAuthenticate", "Xamarin.Android.Net.AndroidMessageHandler", "Mono.Android")]
                //[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2035:Unresolved External Assemblies",
                //    Justification = "Xamarin dependencies are not available during libraries build")]
                bool GetPreAuthenticate() => (bool)InvokeNativeHandlerMethod("get_PreAuthenticate");
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

                //[DynamicDependency("set_PreAuthenticate", "Xamarin.Android.Net.AndroidMessageHandler", "Mono.Android")]
                void SetPreAuthenticate(bool value) => InvokeNativeHandlerMethod("set_PreAuthenticate", value);
            }
        }

        [UnsupportedOSPlatform("browser")]
        //[UnsupportedOSPlatform("ios")]
        //[UnsupportedOSPlatform("tvos")]
        //[UnsupportedOSPlatform("maccatalyst")]
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

                //[DynamicDependency("get_MaxAutomaticRedirections", "Xamarin.Android.Net.AndroidMessageHandler", "Mono.Android")]
                //[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2035:Unresolved External Assemblies",
                //    Justification = "Xamarin dependencies are not available during libraries build")]
                int GetMaxAutomaticRedirections() => (int)InvokeNativeHandlerMethod("get_MaxAutomaticRedirections");
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

                //[DynamicDependency("set_MaxAutomaticRedirections", "Xamarin.Android.Net.AndroidMessageHandler", "Mono.Android")]
                void SetMaxAutomaticRedirections(int value) => InvokeNativeHandlerMethod("set_MaxAutomaticRedirections", value);
            }
        }

        //[DynamicDependency("get_UseCookies", "Xamarin.Android.Net.AndroidMessageHandler", "Mono.Android")]
        private bool GetUseCookies() => (bool)InvokeNativeHandlerMethod("get_UseCookies");

        //[DynamicDependency("set_UseCookies", "Xamarin.Android.Net.AndroidMessageHandler", "Mono.Android")]
        private void SetUseCookies(bool value) => InvokeNativeHandlerMethod("set_UseCookies", value);

        //[DynamicDependency("get_CookieContainer", "Xamarin.Android.Net.AndroidMessageHandler", "Mono.Android")]
        private CookieContainer GetCookieContainer() => (CookieContainer)InvokeNativeHandlerMethod("get_CookieContainer");

        //[DynamicDependency("set_CookieContainer", "Xamarin.Android.Net.AndroidMessageHandler", "Mono.Android")]
        private void SetCookieContainer(CookieContainer value) => InvokeNativeHandlerMethod("set_CookieContainer", value);

        //[DynamicDependency("get_AllowAutoRedirect", "Xamarin.Android.Net.AndroidMessageHandler", "Mono.Android")]
        private bool GetAllowAutoRedirect() => (bool)InvokeNativeHandlerMethod("get_AllowAutoRedirect");

        //[DynamicDependency("set_AllowAutoRedirect", "Xamarin.Android.Net.AndroidMessageHandler", "Mono.Android")]
        private void SetAllowAutoRedirect(bool value) => InvokeNativeHandlerMethod("set_AllowAutoRedirect", value);

        //[DynamicDependency("get_Credentials", "Xamarin.Android.Net.AndroidMessageHandler", "Mono.Android")]
        private ICredentials GetCredentials() => (ICredentials)InvokeNativeHandlerMethod("get_Credentials");

        //[DynamicDependency("set_Credentials", "Xamarin.Android.Net.AndroidMessageHandler", "Mono.Android")]
        private void SetCredentials(ICredentials? value) => InvokeNativeHandlerMethod("set_Credentials", value);

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern",
            Justification = "Xamarin dependencies are not available during libraries build")]
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