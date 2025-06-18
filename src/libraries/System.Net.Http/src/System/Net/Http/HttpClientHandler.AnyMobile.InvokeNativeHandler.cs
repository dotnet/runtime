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
#pragma warning disable CA1823 // Unused field 'NativeHandlerType'. The field is used only in local functions. Tracked at https://github.com/dotnet/roslyn-analyzers/issues/7666
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
#pragma warning restore CA1823 // Unused field 'NativeHandlerType'

        // UnsafeAccessor declarations for all native handler properties/methods
        private ICredentials? GetDefaultProxyCredentials()
        {
            return CallNative(_nativeUnderlyingHandler!);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_DefaultProxyCredentials")]
            static extern ICredentials? CallNative([UnsafeAccessorType(NativeHandlerType)] object handler);
        }

        private void SetDefaultProxyCredentials(ICredentials? value)
        {
            CallNative(_nativeUnderlyingHandler!, value);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_DefaultProxyCredentials")]
            static extern void CallNative([UnsafeAccessorType(NativeHandlerType)] object handler, ICredentials? value);
        }

        private int GetMaxConnectionsPerServer()
        {
            return CallNative(_nativeUnderlyingHandler!);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_MaxConnectionsPerServer")]
            static extern int CallNative([UnsafeAccessorType(NativeHandlerType)] object handler);
        }

        private void SetMaxConnectionsPerServer(int value)
        {
            CallNative(_nativeUnderlyingHandler!, value);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_MaxConnectionsPerServer")]
            static extern void CallNative([UnsafeAccessorType(NativeHandlerType)] object handler, int value);
        }

        private int GetMaxResponseHeadersLength()
        {
            return CallNative(_nativeUnderlyingHandler!);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_MaxResponseHeadersLength")]
            static extern int CallNative([UnsafeAccessorType(NativeHandlerType)] object handler);
        }

        private void SetMaxResponseHeadersLength(int value)
        {
            CallNative(_nativeUnderlyingHandler!, value);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_MaxResponseHeadersLength")]
            static extern void CallNative([UnsafeAccessorType(NativeHandlerType)] object handler, int value);
        }

        private ClientCertificateOption GetClientCertificateOptions()
        {
            return CallNative(_nativeUnderlyingHandler!);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_ClientCertificateOptions")]
            static extern ClientCertificateOption CallNative([UnsafeAccessorType(NativeHandlerType)] object handler);
        }

        private void SetClientCertificateOptions(ClientCertificateOption value)
        {
            CallNative(_nativeUnderlyingHandler!, value);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_ClientCertificateOptions")]
            static extern void CallNative([UnsafeAccessorType(NativeHandlerType)] object handler, ClientCertificateOption value);
        }

        private X509CertificateCollection GetClientCertificates()
        {
            return CallNative(_nativeUnderlyingHandler!);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_ClientCertificates")]
            static extern X509CertificateCollection CallNative([UnsafeAccessorType(NativeHandlerType)] object handler);
        }

        private Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool> GetServerCertificateCustomValidationCallback()
        {
            return CallNative(_nativeUnderlyingHandler!);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_ServerCertificateCustomValidationCallback")]
            static extern Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool> CallNative([UnsafeAccessorType(NativeHandlerType)] object handler);
        }

        private void SetServerCertificateCustomValidationCallback(Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool>? value)
        {
            CallNative(_nativeUnderlyingHandler!, value);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_ServerCertificateCustomValidationCallback")]
            static extern void CallNative([UnsafeAccessorType(NativeHandlerType)] object handler, Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool>? value);
        }

        private bool GetCheckCertificateRevocationList()
        {
            return CallNative(_nativeUnderlyingHandler!);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_CheckCertificateRevocationList")]
            static extern bool CallNative([UnsafeAccessorType(NativeHandlerType)] object handler);
        }

        private void SetCheckCertificateRevocationList(bool value)
        {
            CallNative(_nativeUnderlyingHandler!, value);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_CheckCertificateRevocationList")]
            static extern void CallNative([UnsafeAccessorType(NativeHandlerType)] object handler, bool value);
        }

        private SslProtocols GetSslProtocols()
        {
            return CallNative(_nativeUnderlyingHandler!);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_SslProtocols")]
            static extern SslProtocols CallNative([UnsafeAccessorType(NativeHandlerType)] object handler);
        }

        private void SetSslProtocols(SslProtocols value)
        {
            CallNative(_nativeUnderlyingHandler!, value);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_SslProtocols")]
            static extern void CallNative([UnsafeAccessorType(NativeHandlerType)] object handler, SslProtocols value);
        }

        private IDictionary<string, object?> GetProperties()
        {
            return CallNative(_nativeUnderlyingHandler!);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_Properties")]
            static extern IDictionary<string, object?> CallNative([UnsafeAccessorType(NativeHandlerType)] object handler);
        }

        private bool GetSupportsAutomaticDecompression()
        {
            return CallNative(_nativeUnderlyingHandler!);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_SupportsAutomaticDecompression")]
            static extern bool CallNative([UnsafeAccessorType(NativeHandlerType)] object handler);
        }

        private bool GetSupportsProxy()
        {
            return CallNative(_nativeUnderlyingHandler!);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_SupportsProxy")]
            static extern bool CallNative([UnsafeAccessorType(NativeHandlerType)] object handler);
        }

        private bool GetSupportsRedirectConfiguration()
        {
            return CallNative(_nativeUnderlyingHandler!);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_SupportsRedirectConfiguration")]
            static extern bool CallNative([UnsafeAccessorType(NativeHandlerType)] object handler);
        }

        private DecompressionMethods GetAutomaticDecompression()
        {
            return CallNative(_nativeUnderlyingHandler!);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_AutomaticDecompression")]
            static extern DecompressionMethods CallNative([UnsafeAccessorType(NativeHandlerType)] object handler);
        }

        private void SetAutomaticDecompression(DecompressionMethods value)
        {
            CallNative(_nativeUnderlyingHandler!, value);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_AutomaticDecompression")]
            static extern void CallNative([UnsafeAccessorType(NativeHandlerType)] object handler, DecompressionMethods value);
        }

        private bool GetUseProxy()
        {
            return CallNative(_nativeUnderlyingHandler!);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_UseProxy")]
            static extern bool CallNative([UnsafeAccessorType(NativeHandlerType)] object handler);
        }

        private void SetUseProxy(bool value)
        {
            CallNative(_nativeUnderlyingHandler!, value);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_UseProxy")]
            static extern void CallNative([UnsafeAccessorType(NativeHandlerType)] object handler, bool value);
        }

        private IWebProxy GetProxy()
        {
            return CallNative(_nativeUnderlyingHandler!);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_Proxy")]
            static extern IWebProxy CallNative([UnsafeAccessorType(NativeHandlerType)] object handler);
        }

        private void SetProxy(IWebProxy value)
        {
            CallNative(_nativeUnderlyingHandler!, value);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_Proxy")]
            static extern void CallNative([UnsafeAccessorType(NativeHandlerType)] object handler, IWebProxy value);
        }

        private bool GetPreAuthenticate()
        {
            return CallNative(_nativeUnderlyingHandler!);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_PreAuthenticate")]
            static extern bool CallNative([UnsafeAccessorType(NativeHandlerType)] object handler);
        }

        private void SetPreAuthenticate(bool value)
        {
            CallNative(_nativeUnderlyingHandler!, value);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_PreAuthenticate")]
            static extern void CallNative([UnsafeAccessorType(NativeHandlerType)] object handler, bool value);
        }

        private int GetMaxAutomaticRedirections()
        {
            return CallNative(_nativeUnderlyingHandler!);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_MaxAutomaticRedirections")]
            static extern int CallNative([UnsafeAccessorType(NativeHandlerType)] object handler);
        }

        private void SetMaxAutomaticRedirections(int value)
        {
            CallNative(_nativeUnderlyingHandler!, value);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_MaxAutomaticRedirections")]
            static extern void CallNative([UnsafeAccessorType(NativeHandlerType)] object handler, int value);
        }

        private bool GetUseCookies()
        {
            return CallNative(_nativeUnderlyingHandler!);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_UseCookies")]
            static extern bool CallNative([UnsafeAccessorType(NativeHandlerType)] object handler);
        }

        private void SetUseCookies(bool value)
        {
            CallNative(_nativeUnderlyingHandler!, value);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_UseCookies")]
            static extern void CallNative([UnsafeAccessorType(NativeHandlerType)] object handler, bool value);
        }

        private CookieContainer GetCookieContainer()
        {
            return CallNative(_nativeUnderlyingHandler!);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_CookieContainer")]
            static extern CookieContainer CallNative([UnsafeAccessorType(NativeHandlerType)] object handler);
        }

        private void SetCookieContainer(CookieContainer value)
        {
            CallNative(_nativeUnderlyingHandler!, value);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_CookieContainer")]
            static extern void CallNative([UnsafeAccessorType(NativeHandlerType)] object handler, CookieContainer value);
        }

        private bool GetAllowAutoRedirect()
        {
            return CallNative(_nativeUnderlyingHandler!);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_AllowAutoRedirect")]
            static extern bool CallNative([UnsafeAccessorType(NativeHandlerType)] object handler);
        }

        private void SetAllowAutoRedirect(bool value)
        {
            CallNative(_nativeUnderlyingHandler!, value);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_AllowAutoRedirect")]
            static extern void CallNative([UnsafeAccessorType(NativeHandlerType)] object handler, bool value);
        }

        private ICredentials GetCredentials()
        {
            return CallNative(_nativeUnderlyingHandler!);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_Credentials")]
            static extern ICredentials CallNative([UnsafeAccessorType(NativeHandlerType)] object handler);
        }

        private void SetCredentials(ICredentials? value)
        {
            CallNative(_nativeUnderlyingHandler!, value);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_Credentials")]
            static extern void CallNative([UnsafeAccessorType(NativeHandlerType)] object handler, ICredentials? value);
        }

        private static HttpMessageHandler CreateNativeHandler()
        {
            return CallNative(null);

            [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "GetHttpMessageHandler")]
            static extern HttpMessageHandler CallNative([UnsafeAccessorType(GetHttpMessageHandlerType)] object? _);
        }
    }
}
