// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Security;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Http
{
    public partial class HttpClientHandler : HttpMessageHandler
    {
        private static MethodInfo? _nativeHandlerMethod;

#if TARGET_ANDROID
        private const string NativeHandlerType = "Xamarin.Android.Net.AndroidMessageHandler, Mono.Android";
        private const string GetHttpMessageHandlerType = "Android.Runtime.AndroidEnvironment, Mono.Android";
#elif TARGET_IOS
        private const string NativeHandlerType = "System.Net.Http.NSUrlSessionHandler, Microsoft.iOS";
        private const string GetHttpMessageHandlerType = "ObjCRuntime.RuntimeOptions, Microsoft.iOS";
#elif TARGET_MACCATALYST
        private const string NativeHandlerType = "System.Net.Http.NSUrlSessionHandler, Microsoft.MacCatalyst";
        private const string GetHttpMessageHandlerType = "ObjCRuntime.RuntimeOptions, Microsoft.MacCatalyst";
#elif TARGET_TVOS
        private const string NativeHandlerType = "System.Net.Http.NSUrlSessionHandler, Microsoft.tvOS";
        private const string GetHttpMessageHandlerType = "ObjCRuntime.RuntimeOptions, Microsoft.tvOS";
#else
#error Unknown target
#endif

        private static ICredentials? GetDefaultProxyCredentials(HttpMessageHandler nativeHandler)
            => (ICredentials?)NativeHandlerInvoker.Invoke(nativeHandler, static () => Type.GetType(NativeHandlerType)!.GetMethod("get_DefaultProxyCredentials")!);

        private static void SetDefaultProxyCredentials(HttpMessageHandler nativeHandler, ICredentials? value)
            => NativeHandlerInvoker.Invoke(nativeHandler, static () => Type.GetType(NativeHandlerType)!.GetMethod("set_DefaultProxyCredentials")!, new object?[] { value });

        private static int GetMaxConnectionsPerServer(HttpMessageHandler nativeHandler)
            => (int)NativeHandlerInvoker.Invoke(nativeHandler, static () => Type.GetType(NativeHandlerType)!.GetMethod("get_MaxConnectionsPerServer")!);

        private static void SetMaxConnectionsPerServer(HttpMessageHandler nativeHandler, int value)
            => NativeHandlerInvoker.Invoke(nativeHandler, static () => Type.GetType(NativeHandlerType)!.GetMethod("set_MaxConnectionsPerServer")!, new object?[] { value });

        private static int GetMaxResponseHeadersLength(HttpMessageHandler nativeHandler)
            => (int)NativeHandlerInvoker.Invoke(nativeHandler, static () => Type.GetType(NativeHandlerType)!.GetMethod("get_MaxResponseHeadersLength")!);

        private static void SetMaxResponseHeadersLength(HttpMessageHandler nativeHandler, int value)
            => NativeHandlerInvoker.Invoke(nativeHandler, static () => Type.GetType(NativeHandlerType)!.GetMethod("set_MaxResponseHeadersLength")!, new object?[] { value });

        private static ClientCertificateOption GetClientCertificateOptions(HttpMessageHandler nativeHandler)
            => (ClientCertificateOption)NativeHandlerInvoker.Invoke(nativeHandler, static () => Type.GetType(NativeHandlerType)!.GetMethod("get_ClientCertificateOptions")!);

        private static void SetClientCertificateOptions(HttpMessageHandler nativeHandler, ClientCertificateOption value)
            => NativeHandlerInvoker.Invoke(nativeHandler, static () => Type.GetType(NativeHandlerType)!.GetMethod("set_ClientCertificateOptions")!, new object?[] { value });

        private static X509CertificateCollection GetClientCertificates(HttpMessageHandler nativeHandler)
            => (X509CertificateCollection)NativeHandlerInvoker.Invoke(nativeHandler, static () => Type.GetType(NativeHandlerType)!.GetMethod("get_ClientCertificates")!);

        private static Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool> GetServerCertificateCustomValidationCallback(HttpMessageHandler nativeHandler)
            => (Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool>)NativeHandlerInvoker.Invoke(nativeHandler, static () => Type.GetType(NativeHandlerType)!.GetMethod("get_ServerCertificateCustomValidationCallback")!);

        private static void SetServerCertificateCustomValidationCallback(HttpMessageHandler nativeHandler, Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool>? value)
            => NativeHandlerInvoker.Invoke(nativeHandler, static () => Type.GetType(NativeHandlerType)!.GetMethod("set_ServerCertificateCustomValidationCallback")!, new object?[] { value });

        private static bool GetCheckCertificateRevocationList(HttpMessageHandler nativeHandler)
            => (bool)NativeHandlerInvoker.Invoke(nativeHandler, static () => Type.GetType(NativeHandlerType)!.GetMethod("get_CheckCertificateRevocationList")!);

        private static void SetCheckCertificateRevocationList(HttpMessageHandler nativeHandler, bool value)
            => NativeHandlerInvoker.Invoke(nativeHandler, static () => Type.GetType(NativeHandlerType)!.GetMethod("set_CheckCertificateRevocationList")!, new object?[] { value });

        private static SslProtocols GetSslProtocols(HttpMessageHandler nativeHandler)
            => (SslProtocols)NativeHandlerInvoker.Invoke(nativeHandler, static () => Type.GetType(NativeHandlerType)!.GetMethod("get_SslProtocols")!);

        private static void SetSslProtocols(HttpMessageHandler nativeHandler, SslProtocols value)
            => NativeHandlerInvoker.Invoke(nativeHandler, static () => Type.GetType(NativeHandlerType)!.GetMethod("set_SslProtocols")!, new object?[] { value });

        private static IDictionary<string, object?> GetProperties(HttpMessageHandler nativeHandler)
            => (IDictionary<string, object?>)NativeHandlerInvoker.Invoke(nativeHandler, static () => Type.GetType(NativeHandlerType)!.GetMethod("get_Properties")!);

        private static bool GetSupportsAutomaticDecompression(HttpMessageHandler nativeHandler)
            => (bool)NativeHandlerInvoker.Invoke(nativeHandler, static () => Type.GetType(NativeHandlerType)!.GetMethod("get_SupportsAutomaticDecompression")!);

        private static bool GetSupportsProxy(HttpMessageHandler nativeHandler)
            => (bool)NativeHandlerInvoker.Invoke(nativeHandler, static () => Type.GetType(NativeHandlerType)!.GetMethod("get_SupportsProxy")!);

        private static bool GetSupportsRedirectConfiguration(HttpMessageHandler nativeHandler)
            => (bool)NativeHandlerInvoker.Invoke(nativeHandler, static () => Type.GetType(NativeHandlerType)!.GetMethod("get_SupportsRedirectConfiguration")!);

        private static DecompressionMethods GetAutomaticDecompression(HttpMessageHandler nativeHandler)
            => (DecompressionMethods)NativeHandlerInvoker.Invoke(nativeHandler, static () => Type.GetType(NativeHandlerType)!.GetMethod("get_AutomaticDecompression")!);

        private static void SetAutomaticDecompression(HttpMessageHandler nativeHandler, DecompressionMethods value)
            => NativeHandlerInvoker.Invoke(nativeHandler, static () => Type.GetType(NativeHandlerType)!.GetMethod("set_AutomaticDecompression")!, new object?[] { value });

        private static bool GetUseProxy(HttpMessageHandler nativeHandler)
            => (bool)NativeHandlerInvoker.Invoke(nativeHandler, static () => Type.GetType(NativeHandlerType)!.GetMethod("get_UseProxy")!);

        private static void SetUseProxy(HttpMessageHandler nativeHandler, bool value)
            => NativeHandlerInvoker.Invoke(nativeHandler, static () => Type.GetType(NativeHandlerType)!.GetMethod("set_UseProxy")!, new object?[] { value });

        private static IWebProxy GetProxy(HttpMessageHandler nativeHandler)
            => (IWebProxy)NativeHandlerInvoker.Invoke(nativeHandler, static () => Type.GetType(NativeHandlerType)!.GetMethod("get_Proxy")!);

        private static void SetProxy(HttpMessageHandler nativeHandler, IWebProxy value)
            => NativeHandlerInvoker.Invoke(nativeHandler, static () => Type.GetType(NativeHandlerType)!.GetMethod("set_Proxy")!, new object?[] { value });

        private static bool GetPreAuthenticate(HttpMessageHandler nativeHandler)
            => (bool)NativeHandlerInvoker.Invoke(nativeHandler, static () => Type.GetType(NativeHandlerType)!.GetMethod("get_PreAuthenticate")!);

        private static void SetPreAuthenticate(HttpMessageHandler nativeHandler, bool value)
            => NativeHandlerInvoker.Invoke(nativeHandler, static () => Type.GetType(NativeHandlerType)!.GetMethod("set_PreAuthenticate")!, new object?[] { value });

        private static int GetMaxAutomaticRedirections(HttpMessageHandler nativeHandler)
            => (int)NativeHandlerInvoker.Invoke(nativeHandler, static () => Type.GetType(NativeHandlerType)!.GetMethod("get_MaxAutomaticRedirections")!);

        private static void SetMaxAutomaticRedirections(HttpMessageHandler nativeHandler, int value)
            => NativeHandlerInvoker.Invoke(nativeHandler, static () => Type.GetType(NativeHandlerType)!.GetMethod("set_MaxAutomaticRedirections")!, new object?[] { value });

        private static bool GetUseCookies(HttpMessageHandler nativeHandler)
            => (bool)NativeHandlerInvoker.Invoke(nativeHandler, static () => Type.GetType(NativeHandlerType)!.GetMethod("get_UseCookies")!);

        private static void SetUseCookies(HttpMessageHandler nativeHandler, bool value)
            => NativeHandlerInvoker.Invoke(nativeHandler, static () => Type.GetType(NativeHandlerType)!.GetMethod("set_UseCookies")!, new object?[] { value });

        private static CookieContainer GetCookieContainer(HttpMessageHandler nativeHandler)
            => (CookieContainer)NativeHandlerInvoker.Invoke(nativeHandler, static () => Type.GetType(NativeHandlerType)!.GetMethod("get_CookieContainer")!);

        private static void SetCookieContainer(HttpMessageHandler nativeHandler, CookieContainer value)
            => NativeHandlerInvoker.Invoke(nativeHandler, static () => Type.GetType(NativeHandlerType)!.GetMethod("set_CookieContainer")!, new object?[] { value });

        private static bool GetAllowAutoRedirect(HttpMessageHandler nativeHandler)
            => (bool)NativeHandlerInvoker.Invoke(nativeHandler, static () => Type.GetType(NativeHandlerType)!.GetMethod("get_AllowAutoRedirect")!);

        private static void SetAllowAutoRedirect(HttpMessageHandler nativeHandler, bool value)
            => NativeHandlerInvoker.Invoke(nativeHandler, static () => Type.GetType(NativeHandlerType)!.GetMethod("set_AllowAutoRedirect")!, new object?[] { value });

        private static ICredentials GetCredentials(HttpMessageHandler nativeHandler)
            => (ICredentials)NativeHandlerInvoker.Invoke(nativeHandler, static () => Type.GetType(NativeHandlerType)!.GetMethod("get_Credentials")!);

        private static void SetCredentials(HttpMessageHandler nativeHandler, ICredentials? value)
            => NativeHandlerInvoker.Invoke(nativeHandler, static () => Type.GetType(NativeHandlerType)!.GetMethod("set_Credentials")!, new object?[] { value });

        private static HttpMessageHandler CreateNativeHandler()
        {
            if (_nativeHandlerMethod == null)
            {
                Type? runtimeOptions = Type.GetType(GetHttpMessageHandlerType);
                _nativeHandlerMethod = runtimeOptions!.GetMethod("GetHttpMessageHandler", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            }

            return (HttpMessageHandler)_nativeHandlerMethod!.Invoke(null, null)!;
        }
    }

#pragma warning disable SA1400 // Access modifier should be declared https://github.com/DotNetAnalyzers/StyleCopAnalyzers/issues/3659
    file static class NativeHandlerInvoker
#pragma warning restore SA1400 // Access modifier should be declared
    {
        private static readonly ConcurrentDictionary<int, MethodInfo?> s_cachedMethods = new();
        internal static object Invoke(HttpMessageHandler nativeHandler, Func<MethodInfo> getMethod, object?[]? parameters = null, [CallerLineNumber] int cachingKey = 0)
        {
            MethodInfo? method;

            if (!s_cachedMethods.TryGetValue(cachingKey, out method))
            {
                method = getMethod();
                s_cachedMethods[cachingKey] = method;
            }

            try
            {
                return method!.Invoke(nativeHandler, parameters)!;
            }
            catch (TargetInvocationException e)
            {
                ExceptionDispatchInfo.Capture(e.InnerException!).Throw();
                throw;
            }
        }
    }
}
