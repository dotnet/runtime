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
        // not sure
        public virtual bool SupportsAutomaticDecompression => true;
        public virtual bool SupportsRedirectConfiguration => true;

        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
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
                    return (DecompressionMethods)GetNativeHandlerProp("AutomaticDecompression");
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
                    InvokeNativeHandlerMethod("set_AutomaticDecompression", value);
                }
            }
        }

        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
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
                    return (bool)GetNativeHandlerProp("UseProxy");
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
                    InvokeNativeHandlerMethod("set_UseProxy", value);
                }
            }
        }

        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
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
                    return (bool)GetNativeHandlerProp("PreAuthenticate");
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
                    InvokeNativeHandlerMethod("set_PreAuthenticate", value);
                }
            }
        }

        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
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
                    return (int)GetNativeHandlerProp("MaxAutomaticRedirections");
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
                    InvokeNativeHandlerMethod("set_MaxAutomaticRedirections", value);
                }
            }
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern",
            Justification = "Unused fields don't make a difference for hashcode quality")]
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