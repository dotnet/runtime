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

        // UnsafeAccessor declarations for all native handler properties/methods
        private ICredentials? GetDefaultProxyCredentials()
        {
            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_DefaultProxyCredentials")]
            static extern ICredentials? CallNative([UnsafeAccessorType(NativeHandlerType)] object handler);
            return CallNative(_nativeUnderlyingHandler!);
        }
        private void SetDefaultProxyCredentials(ICredentials? value)
        {
            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_DefaultProxyCredentials")]
            static extern void CallNative([UnsafeAccessorType(NativeHandlerType)] object handler, ICredentials? value);
            CallNative(_nativeUnderlyingHandler!, value);
        }
        private int GetMaxConnectionsPerServer()
        {
            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_MaxConnectionsPerServer")]
            static extern int CallNative([UnsafeAccessorType(NativeHandlerType)] object handler);
            return CallNative(_nativeUnderlyingHandler!);
        }
        private void SetMaxConnectionsPerServer(int value)
        {
            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_MaxConnectionsPerServer")]
            static extern void CallNative([UnsafeAccessorType(NativeHandlerType)] object handler, int value);
            CallNative(_nativeUnderlyingHandler!, value);
        }
        private int GetMaxResponseHeadersLength()
        {
            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_MaxResponseHeadersLength")]
            static extern int CallNative([UnsafeAccessorType(NativeHandlerType)] object handler);
            return CallNative(_nativeUnderlyingHandler!);
        }
        private void SetMaxResponseHeadersLength(int value)
        {
            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_MaxResponseHeadersLength")]
            static extern void CallNative([UnsafeAccessorType(NativeHandlerType)] object handler, int value);
            CallNative(_nativeUnderlyingHandler!, value);
        }
        private ClientCertificateOption GetClientCertificateOptions()
        {
            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_ClientCertificateOptions")]
            static extern ClientCertificateOption CallNative([UnsafeAccessorType(NativeHandlerType)] object handler);
            return CallNative(_nativeUnderlyingHandler!);
        }
        private void SetClientCertificateOptions(ClientCertificateOption value)
        {
            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_ClientCertificateOptions")]
            static extern void CallNative([UnsafeAccessorType(NativeHandlerType)] object handler, ClientCertificateOption value);
            CallNative(_nativeUnderlyingHandler!, value);
        }
        private X509CertificateCollection GetClientCertificates()
        {
            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_ClientCertificates")]
            static extern X509CertificateCollection CallNative([UnsafeAccessorType(NativeHandlerType)] object handler);
            return CallNative(_nativeUnderlyingHandler!);
        }
        private Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool> GetServerCertificateCustomValidationCallback()
        {
            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_ServerCertificateCustomValidationCallback")]
            static extern Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool> CallNative([UnsafeAccessorType(NativeHandlerType)] object handler);
            return CallNative(_nativeUnderlyingHandler!);
        }
        private void SetServerCertificateCustomValidationCallback(Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool>? value)
        {
            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_ServerCertificateCustomValidationCallback")]
            static extern void CallNative([UnsafeAccessorType(NativeHandlerType)] object handler, Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool>? value);
            CallNative(_nativeUnderlyingHandler!, value);
        }
        private bool GetCheckCertificateRevocationList()
        {
            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_CheckCertificateRevocationList")]
            static extern bool CallNative([UnsafeAccessorType(NativeHandlerType)] object handler);
            return CallNative(_nativeUnderlyingHandler!);
        }
        private void SetCheckCertificateRevocationList(bool value)
        {
            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_CheckCertificateRevocationList")]
            static extern void CallNative([UnsafeAccessorType(NativeHandlerType)] object handler, bool value);
            CallNative(_nativeUnderlyingHandler!, value);
        }
        private SslProtocols GetSslProtocols()
        {
            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_SslProtocols")]
            static extern SslProtocols CallNative([UnsafeAccessorType(NativeHandlerType)] object handler);
            return CallNative(_nativeUnderlyingHandler!);
        }
        private void SetSslProtocols(SslProtocols value)
        {
            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_SslProtocols")]
            static extern void CallNative([UnsafeAccessorType(NativeHandlerType)] object handler, SslProtocols value);
            CallNative(_nativeUnderlyingHandler!, value);
        }
        private IDictionary<string, object?> GetProperties()
        {
            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_Properties")]
            static extern IDictionary<string, object?> CallNative([UnsafeAccessorType(NativeHandlerType)] object handler);
            return CallNative(_nativeUnderlyingHandler!);
        }
        private bool GetSupportsAutomaticDecompression()
        {
            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_SupportsAutomaticDecompression")]
            static extern bool CallNative([UnsafeAccessorType(NativeHandlerType)] object handler);
            return CallNative(_nativeUnderlyingHandler!);
        }
        private bool GetSupportsProxy()
        {
            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_SupportsProxy")]
            static extern bool CallNative([UnsafeAccessorType(NativeHandlerType)] object handler);
            return CallNative(_nativeUnderlyingHandler!);
        }
        private bool GetSupportsRedirectConfiguration()
        {
            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_SupportsRedirectConfiguration")]
            static extern bool CallNative([UnsafeAccessorType(NativeHandlerType)] object handler);
            return CallNative(_nativeUnderlyingHandler!);
        }
        private DecompressionMethods GetAutomaticDecompression()
        {
            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_AutomaticDecompression")]
            static extern DecompressionMethods CallNative([UnsafeAccessorType(NativeHandlerType)] object handler);
            return CallNative(_nativeUnderlyingHandler!);
        }
        private void SetAutomaticDecompression(DecompressionMethods value)
        {
            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_AutomaticDecompression")]
            static extern void CallNative([UnsafeAccessorType(NativeHandlerType)] object handler, DecompressionMethods value);
            CallNative(_nativeUnderlyingHandler!, value);
        }
        private bool GetUseProxy()
        {
            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_UseProxy")]
            static extern bool CallNative([UnsafeAccessorType(NativeHandlerType)] object handler);
            return CallNative(_nativeUnderlyingHandler!);
        }
        private void SetUseProxy(bool value)
        {
            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_UseProxy")]
            static extern void CallNative([UnsafeAccessorType(NativeHandlerType)] object handler, bool value);
            CallNative(_nativeUnderlyingHandler!, value);
        }
        private IWebProxy GetProxy()
        {
            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_Proxy")]
            static extern IWebProxy CallNative([UnsafeAccessorType(NativeHandlerType)] object handler);
            return CallNative(_nativeUnderlyingHandler!);
        }
        private void SetProxy(IWebProxy value)
        {
            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_Proxy")]
            static extern void CallNative([UnsafeAccessorType(NativeHandlerType)] object handler, IWebProxy value);
            CallNative(_nativeUnderlyingHandler!, value);
        }
        private bool GetPreAuthenticate()
        {
            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_PreAuthenticate")]
            static extern bool CallNative([UnsafeAccessorType(NativeHandlerType)] object handler);
            return CallNative(_nativeUnderlyingHandler!);
        }
        private void SetPreAuthenticate(bool value)
        {
            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_PreAuthenticate")]
            static extern void CallNative([UnsafeAccessorType(NativeHandlerType)] object handler, bool value);
            CallNative(_nativeUnderlyingHandler!, value);
        }
        private int GetMaxAutomaticRedirections()
        {
            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_MaxAutomaticRedirections")]
            static extern int CallNative([UnsafeAccessorType(NativeHandlerType)] object handler);
            return CallNative(_nativeUnderlyingHandler!);
        }
        private void SetMaxAutomaticRedirections(int value)
        {
            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_MaxAutomaticRedirections")]
            static extern void CallNative([UnsafeAccessorType(NativeHandlerType)] object handler, int value);
            CallNative(_nativeUnderlyingHandler!, value);
        }
        private bool GetUseCookies()
        {
            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_UseCookies")]
            static extern bool CallNative([UnsafeAccessorType(NativeHandlerType)] object handler);
            return CallNative(_nativeUnderlyingHandler!);
        }
        private void SetUseCookies(bool value)
        {
            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_UseCookies")]
            static extern void CallNative([UnsafeAccessorType(NativeHandlerType)] object handler, bool value);
            CallNative(_nativeUnderlyingHandler!, value);
        }
        private CookieContainer GetCookieContainer()
        {
            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_CookieContainer")]
            static extern CookieContainer CallNative([UnsafeAccessorType(NativeHandlerType)] object handler);
            return CallNative(_nativeUnderlyingHandler!);
        }
        private void SetCookieContainer(CookieContainer value)
        {
            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_CookieContainer")]
            static extern void CallNative([UnsafeAccessorType(NativeHandlerType)] object handler, CookieContainer value);
            CallNative(_nativeUnderlyingHandler!, value);
        }
        private bool GetAllowAutoRedirect()
        {
            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_AllowAutoRedirect")]
            static extern bool CallNative([UnsafeAccessorType(NativeHandlerType)] object handler);
            return CallNative(_nativeUnderlyingHandler!);
        }
        private void SetAllowAutoRedirect(bool value)
        {
            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_AllowAutoRedirect")]
            static extern void CallNative([UnsafeAccessorType(NativeHandlerType)] object handler, bool value);
            CallNative(_nativeUnderlyingHandler!, value);
        }
        private ICredentials GetCredentials()
        {
            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_Credentials")]
            static extern ICredentials CallNative([UnsafeAccessorType(NativeHandlerType)] object handler);
            return CallNative(_nativeUnderlyingHandler!);
        }
        private void SetCredentials(ICredentials? value)
        {
            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_Credentials")]
            static extern void CallNative([UnsafeAccessorType(NativeHandlerType)] object handler, ICredentials? value);
            CallNative(_nativeUnderlyingHandler!, value);
        }
        private static HttpMessageHandler CreateNativeHandler()
        {
            [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "GetHttpMessageHandler")]
            [return: UnsafeAccessorType(NativeHandlerType)]
            static extern object CallNative();
            return (HttpMessageHandler)CallNative();
        }
    }
}
