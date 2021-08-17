// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Security;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Http
{
    public partial class HttpClientHandler : HttpMessageHandler
    {
        private static MethodInfo? _nativeHandlerMethod;

        private const string NativeHandlerType = "System.Net.Http.NSUrlSessionHandler";
        private const string AssemblyName = "Xamarin.TVOS";

        [DynamicDependency("get_DefaultProxyCredentials", NativeHandlerType, AssemblyName)]
        private ICredentials? GetDefaultProxyCredentials() => (ICredentials?)InvokeNativeHandlerMethod("get_DefaultProxyCredentials");

        [DynamicDependency("set_DefaultProxyCredentials", NativeHandlerType, AssemblyName)]
        private void SetDefaultProxyCredentials(ICredentials? value) => InvokeNativeHandlerMethod("set_DefaultProxyCredentials", value);

        [DynamicDependency("get_MaxConnectionsPerServer", NativeHandlerType, AssemblyName)]
        private int GetMaxConnectionsPerServer() => (int)InvokeNativeHandlerMethod("get_MaxConnectionsPerServer");

        [DynamicDependency("set_MaxConnectionsPerServer", NativeHandlerType, AssemblyName)]
        private void SetMaxConnectionsPerServer(int value) => InvokeNativeHandlerMethod("set_MaxConnectionsPerServer", value);

        [DynamicDependency("get_MaxResponseHeadersLength", NativeHandlerType, AssemblyName)]
        private int GetMaxResponseHeadersLength() => (int)InvokeNativeHandlerMethod("get_MaxResponseHeadersLength");

        [DynamicDependency("set_MaxResponseHeadersLength", NativeHandlerType, AssemblyName)]
        private void SetMaxResponseHeadersLength(int value) => InvokeNativeHandlerMethod("set_MaxResponseHeadersLength", value);

        [DynamicDependency("get_ClientCertificateOptions", NativeHandlerType, AssemblyName)]
        private ClientCertificateOption GetClientCertificateOptions() => (ClientCertificateOption)InvokeNativeHandlerMethod("get_ClientCertificateOptions");

        [DynamicDependency("set_ClientCertificateOptions", NativeHandlerType, AssemblyName)]
        private void SetClientCertificateOptions(ClientCertificateOption value) => InvokeNativeHandlerMethod("set_ClientCertificateOptions", value);

        [DynamicDependency("get_ClientCertificates", NativeHandlerType, AssemblyName)]
        private X509CertificateCollection GetClientCertificates() => (X509CertificateCollection)InvokeNativeHandlerMethod("get_ClientCertificates");

        [DynamicDependency("get_CheckCertificateRevocationList", NativeHandlerType, AssemblyName)]
        private bool GetCheckCertificateRevocationList() => (bool)InvokeNativeHandlerMethod("get_CheckCertificateRevocationList");

        [DynamicDependency("set_CheckCertificateRevocationList", NativeHandlerType, AssemblyName)]
        private void SetCheckCertificateRevocationList(bool value) => InvokeNativeHandlerMethod("set_CheckCertificateRevocationList", value);

        [DynamicDependency("get_SslProtocols", NativeHandlerType, AssemblyName)]
        private SslProtocols GetSslProtocols() => (SslProtocols)InvokeNativeHandlerMethod("get_SslProtocols");

        [DynamicDependency("set_SslProtocols", NativeHandlerType, AssemblyName)]
        private void SetSslProtocols(SslProtocols value) => InvokeNativeHandlerMethod("set_SslProtocols", value);

        [DynamicDependency("get_Properties", NativeHandlerType, AssemblyName)]
        private IDictionary<string, object?> GetProperties() => (IDictionary<string, object?>)InvokeNativeHandlerMethod("get_Properties");

        [DynamicDependency("get_SupportsAutomaticDecompression", NativeHandlerType, AssemblyName)]
        private bool GetSupportsAutomaticDecompression() => (bool)InvokeNativeHandlerMethod("get_SupportsAutomaticDecompression");

        [DynamicDependency("get_SupportsProxy", NativeHandlerType, AssemblyName)]
        private bool GetSupportsProxy() => (bool)InvokeNativeHandlerMethod("get_SupportsProxy");

        [DynamicDependency("get_SupportsRedirectConfiguration", NativeHandlerType, AssemblyName)]
        private bool GetSupportsRedirectConfiguration() => (bool)InvokeNativeHandlerMethod("get_SupportsRedirectConfiguration");

        [DynamicDependency("get_AutomaticDecompression", NativeHandlerType, AssemblyName)]
        private DecompressionMethods GetAutomaticDecompression() => (DecompressionMethods)InvokeNativeHandlerMethod("get_AutomaticDecompression");

        [DynamicDependency("set_AutomaticDecompression", NativeHandlerType, AssemblyName)]
        private void SetAutomaticDecompression(DecompressionMethods value) => InvokeNativeHandlerMethod("set_AutomaticDecompression", value);

        [DynamicDependency("get_UseProxy", NativeHandlerType, AssemblyName)]
        private bool GetUseProxy() => (bool)InvokeNativeHandlerMethod("get_UseProxy");

        [DynamicDependency("set_UseProxy", NativeHandlerType, AssemblyName)]
        private void SetUseProxy(bool value) => InvokeNativeHandlerMethod("set_UseProxy", value);

        [DynamicDependency("get_Proxy", NativeHandlerType, AssemblyName)]
        private IWebProxy GetProxy() => (IWebProxy)InvokeNativeHandlerMethod("get_Proxy");

        [DynamicDependency("set_Proxy", NativeHandlerType, AssemblyName)]
        private void SetProxy(IWebProxy value) => InvokeNativeHandlerMethod("set_Proxy", value);

        [DynamicDependency("get_PreAuthenticate", NativeHandlerType, AssemblyName)]
        private bool GetPreAuthenticate() => (bool)InvokeNativeHandlerMethod("get_PreAuthenticate");

        [DynamicDependency("set_PreAuthenticate", NativeHandlerType, AssemblyName)]
        private void SetPreAuthenticate(bool value) => InvokeNativeHandlerMethod("set_PreAuthenticate", value);

        [DynamicDependency("get_MaxAutomaticRedirections", NativeHandlerType, AssemblyName)]
        private int GetMaxAutomaticRedirections() => (int)InvokeNativeHandlerMethod("get_MaxAutomaticRedirections");

        [DynamicDependency("set_MaxAutomaticRedirections", NativeHandlerType, AssemblyName)]
        private void SetMaxAutomaticRedirections(int value) => InvokeNativeHandlerMethod("set_MaxAutomaticRedirections", value);

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
                Type? runtimeOptions = Type.GetType("ObjCRuntime.RuntimeOptions, Xamarin.TVOS");
                _nativeHandlerMethod = runtimeOptions!.GetMethod("GetHttpMessageHandler", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            }

            return (HttpMessageHandler)_nativeHandlerMethod!.Invoke(null, null)!;
        }
    }
}