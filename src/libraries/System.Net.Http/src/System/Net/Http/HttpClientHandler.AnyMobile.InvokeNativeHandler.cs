// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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

        private ICredentials? GetDefaultProxyCredentials()
            => (ICredentials?)InvokeNativeHandlerGetter(() => Type.GetType(NativeHandlerType)!.GetMethod("get_DefaultProxyCredentials")!);

        private void SetDefaultProxyCredentials(ICredentials? value)
            => InvokeNativeHandlerSetter(() => Type.GetType(NativeHandlerType)!.GetMethod("set_DefaultProxyCredentials")!, value);

        private int GetMaxConnectionsPerServer()
            => (int)InvokeNativeHandlerGetter(() => Type.GetType(NativeHandlerType)!.GetMethod("get_MaxConnectionsPerServer")!);

        private void SetMaxConnectionsPerServer(int value)
            => InvokeNativeHandlerSetter(() => Type.GetType(NativeHandlerType)!.GetMethod("set_MaxConnectionsPerServer")!, value);

        private int GetMaxResponseHeadersLength()
            => (int)InvokeNativeHandlerGetter(() => Type.GetType(NativeHandlerType)!.GetMethod("get_MaxResponseHeadersLength")!);

        private void SetMaxResponseHeadersLength(int value)
            => InvokeNativeHandlerSetter(() => Type.GetType(NativeHandlerType)!.GetMethod("set_MaxResponseHeadersLength")!, value);

        private ClientCertificateOption GetClientCertificateOptions()
            => (ClientCertificateOption)InvokeNativeHandlerGetter(() => Type.GetType(NativeHandlerType)!.GetMethod("get_ClientCertificateOptions")!);

        private void SetClientCertificateOptions(ClientCertificateOption value)
            => InvokeNativeHandlerSetter(() => Type.GetType(NativeHandlerType)!.GetMethod("set_ClientCertificateOptions")!, value);

        private X509CertificateCollection GetClientCertificates()
            => (X509CertificateCollection)InvokeNativeHandlerGetter(() => Type.GetType(NativeHandlerType)!.GetMethod("get_ClientCertificates")!);

        private Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool> GetServerCertificateCustomValidationCallback()
            => (Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool>)InvokeNativeHandlerGetter(() => Type.GetType(NativeHandlerType)!.GetMethod("get_ServerCertificateCustomValidationCallback")!);

        private void SetServerCertificateCustomValidationCallback(Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool>? value)
            => InvokeNativeHandlerSetter(() => Type.GetType(NativeHandlerType)!.GetMethod("set_ServerCertificateCustomValidationCallback")!, value);

        private bool GetCheckCertificateRevocationList()
            => (bool)InvokeNativeHandlerGetter(() => Type.GetType(NativeHandlerType)!.GetMethod("get_CheckCertificateRevocationList")!);

        private void SetCheckCertificateRevocationList(bool value)
            => InvokeNativeHandlerSetter(() => Type.GetType(NativeHandlerType)!.GetMethod("set_CheckCertificateRevocationList")!, value);

        private SslProtocols GetSslProtocols()
            => (SslProtocols)InvokeNativeHandlerGetter(() => Type.GetType(NativeHandlerType)!.GetMethod("get_SslProtocols")!);

        private void SetSslProtocols(SslProtocols value)
            => InvokeNativeHandlerSetter(() => Type.GetType(NativeHandlerType)!.GetMethod("set_SslProtocols")!, value);

        private IDictionary<string, object?> GetProperties()
            => (IDictionary<string, object?>)InvokeNativeHandlerGetter(() => Type.GetType(NativeHandlerType)!.GetMethod("get_Properties")!);

        private bool GetSupportsAutomaticDecompression()
            => (bool)InvokeNativeHandlerGetter(() => Type.GetType(NativeHandlerType)!.GetMethod("get_SupportsAutomaticDecompression")!);

        private bool GetSupportsProxy()
            => (bool)InvokeNativeHandlerGetter(() => Type.GetType(NativeHandlerType)!.GetMethod("get_SupportsProxy")!);

        private bool GetSupportsRedirectConfiguration()
            => (bool)InvokeNativeHandlerGetter(() => Type.GetType(NativeHandlerType)!.GetMethod("get_SupportsRedirectConfiguration")!);

        private DecompressionMethods GetAutomaticDecompression()
            => (DecompressionMethods)InvokeNativeHandlerGetter(() => Type.GetType(NativeHandlerType)!.GetMethod("get_AutomaticDecompression")!);

        private void SetAutomaticDecompression(DecompressionMethods value)
            => InvokeNativeHandlerSetter(() => Type.GetType(NativeHandlerType)!.GetMethod("set_AutomaticDecompression")!, value);

        private bool GetUseProxy()
            => (bool)InvokeNativeHandlerGetter(() => Type.GetType(NativeHandlerType)!.GetMethod("get_UseProxy")!);

        private void SetUseProxy(bool value)
            => InvokeNativeHandlerSetter(() => Type.GetType(NativeHandlerType)!.GetMethod("set_UseProxy")!, value);

        private IWebProxy GetProxy()
            => (IWebProxy)InvokeNativeHandlerGetter(() => Type.GetType(NativeHandlerType)!.GetMethod("get_Proxy")!);

        private void SetProxy(IWebProxy value)
            => InvokeNativeHandlerSetter(() => Type.GetType(NativeHandlerType)!.GetMethod("set_Proxy")!, value);

        private bool GetPreAuthenticate()
            => (bool)InvokeNativeHandlerGetter(() => Type.GetType(NativeHandlerType)!.GetMethod("get_PreAuthenticate")!);

        private void SetPreAuthenticate(bool value)
            => InvokeNativeHandlerSetter(() => Type.GetType(NativeHandlerType)!.GetMethod("set_PreAuthenticate")!, value);

        private int GetMaxAutomaticRedirections()
            => (int)InvokeNativeHandlerGetter(() => Type.GetType(NativeHandlerType)!.GetMethod("get_MaxAutomaticRedirections")!);

        private void SetMaxAutomaticRedirections(int value)
            => InvokeNativeHandlerSetter(() => Type.GetType(NativeHandlerType)!.GetMethod("set_MaxAutomaticRedirections")!, value);

        private bool GetUseCookies() => (bool)InvokeNativeHandlerGetter(() => Type.GetType(NativeHandlerType)!.GetMethod("get_UseCookies")!);

        private void SetUseCookies(bool value)
            => InvokeNativeHandlerSetter(() => Type.GetType(NativeHandlerType)!.GetMethod("set_UseCookies")!, value);

        private CookieContainer GetCookieContainer()
            => (CookieContainer)InvokeNativeHandlerGetter(() => Type.GetType(NativeHandlerType)!.GetMethod("get_CookieContainer")!);

        private void SetCookieContainer(CookieContainer value)
            => InvokeNativeHandlerSetter(() => Type.GetType(NativeHandlerType)!.GetMethod("set_CookieContainer")!, value);

        private bool GetAllowAutoRedirect()
            => (bool)InvokeNativeHandlerGetter(() => Type.GetType(NativeHandlerType)!.GetMethod("get_AllowAutoRedirect")!);

        private void SetAllowAutoRedirect(bool value)
            => InvokeNativeHandlerSetter(() => Type.GetType(NativeHandlerType)!.GetMethod("set_AllowAutoRedirect")!, value);

        private ICredentials GetCredentials()
            => (ICredentials)InvokeNativeHandlerGetter(() => Type.GetType(NativeHandlerType)!.GetMethod("get_Credentials")!);

        private void SetCredentials(ICredentials? value)
            => InvokeNativeHandlerSetter(() => Type.GetType(NativeHandlerType)!.GetMethod("set_Credentials")!, value);

        private static HttpMessageHandler CreateNativeHandler()
        {
            if (_nativeHandlerMethod == null)
            {
                Type? runtimeOptions = Type.GetType(GetHttpMessageHandlerType);
                _nativeHandlerMethod = runtimeOptions!.GetMethod("GetHttpMessageHandler", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            }

            return (HttpMessageHandler)_nativeHandlerMethod!.Invoke(null, null)!;
        }

        private object InvokeNativeHandlerGetter(Func<MethodInfo> getMethod, [CallerMemberName] string? cachingKey = null)
        {
            return InvokeNativeHandlerMethod(getMethod, parameters: null, cachingKey!);
        }

        private void InvokeNativeHandlerSetter(Func<MethodInfo> getMethod, object? value, [CallerMemberName] string? cachingKey = null)
        {
            InvokeNativeHandlerMethod(getMethod, parameters: new object?[] { value }, cachingKey!);
        }

        private object InvokeNativeHandlerMethod(Func<MethodInfo> getMethod, object?[]? parameters, string cachingKey)
        {
            MethodInfo? method;

            if (!s_cachedMethods.TryGetValue(cachingKey, out method))
            {
                method = getMethod();
                s_cachedMethods[cachingKey] = method;
            }

            try
            {
                return method!.Invoke(_nativeHandler, parameters)!;
            }
            catch (TargetInvocationException e)
            {
                ExceptionDispatchInfo.Capture(e.InnerException!).Throw();
                throw;
            }
        }
    }
}
