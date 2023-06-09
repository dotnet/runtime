// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Net.Security;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Versioning;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    public partial class HttpClientHandler : HttpMessageHandler
    {
        private readonly SocketsHttpHandler? _socketHandler;
        private readonly DiagnosticsHandler? _diagnosticsHandler;

        private readonly HttpMessageHandler? _nativeHandler;

        private static readonly ConcurrentDictionary<string, MethodInfo?> s_cachedMethods =
            new ConcurrentDictionary<string, MethodInfo?>();

        private ClientCertificateOption _clientCertificateOptions;

        private volatile bool _disposed;

        public HttpClientHandler()
        {
            HttpMessageHandler handler;

            if (IsNativeHandlerEnabled)
            {
                _nativeHandler = CreateNativeHandler();
                handler = _nativeHandler;
            }
            else
            {
                _socketHandler = new SocketsHttpHandler();
                handler = _socketHandler;
                ClientCertificateOptions = ClientCertificateOption.Manual;
            }

            if (DiagnosticsHandler.IsGloballyEnabled())
            {
                _diagnosticsHandler = new DiagnosticsHandler(handler, DistributedContextPropagator.Current);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;

                if (IsNativeHandlerEnabled)
                {
                    _nativeHandler!.Dispose();
                }
                else
                {
                    _socketHandler!.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        [CLSCompliant(false)]
        public Meter Meter
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        [UnsupportedOSPlatform("browser")]
        public bool UseCookies
        {
            get
            {
                if (IsNativeHandlerEnabled)
                {
                    return GetUseCookies();
                }
                else
                {
                    return _socketHandler!.UseCookies;
                }
            }
            set
            {
                if (IsNativeHandlerEnabled)
                {
                    SetUseCookies(value);
                }
                else
                {
                    _socketHandler!.UseCookies = value;
                }
            }
        }

        [UnsupportedOSPlatform("browser")]
        public CookieContainer CookieContainer
        {
            get
            {
                if (IsNativeHandlerEnabled)
                {
                    return GetCookieContainer();
                }
                else
                {
                    return _socketHandler!.CookieContainer;
                }
            }
            set
            {
                ArgumentNullException.ThrowIfNull(value);

                if (IsNativeHandlerEnabled)
                {
                    SetCookieContainer(value);
                }
                else
                {
                    _socketHandler!.CookieContainer = value;
                }
            }
        }

        [UnsupportedOSPlatform("browser")]
        public ICredentials? DefaultProxyCredentials
        {
            get
            {
                if (IsNativeHandlerEnabled)
                {
                    return GetDefaultProxyCredentials();
                }
                else
                {
                    return _socketHandler!.DefaultProxyCredentials;
                }
            }
            set
            {
                if (IsNativeHandlerEnabled)
                {
                    SetDefaultProxyCredentials(value);
                }
                else
                {
                    _socketHandler!.DefaultProxyCredentials = value;
                }
            }
        }

        [UnsupportedOSPlatform("browser")]
        public bool UseDefaultCredentials
        {
            // SocketsHttpHandler doesn't have a separate UseDefaultCredentials property.  There
            // is just a Credentials property.  So, we need to map the behavior.
            // Same with the native handler.
            get
            {
                ICredentials? creds;
                if (IsNativeHandlerEnabled)
                {
                    creds = GetCredentials();
                }
                else
                {
                    creds = _socketHandler!.Credentials;
                }

                return creds == CredentialCache.DefaultCredentials;
            }
            set
            {
                if (value)
                {
                    if (IsNativeHandlerEnabled)
                    {
                        SetCredentials(CredentialCache.DefaultCredentials);
                    }
                    else
                    {
                        _socketHandler!.Credentials = CredentialCache.DefaultCredentials;
                    }
                }
                else
                {
                    if (IsNativeHandlerEnabled)
                    {
                        ICredentials? creds = GetCredentials();

                        if (creds == CredentialCache.DefaultCredentials)
                        {
                            SetCredentials(null!);
                        }
                    }
                    else
                    {
                        if (_socketHandler!.Credentials == CredentialCache.DefaultCredentials)
                        {
                            _socketHandler!.Credentials = null;
                        }
                    }
                }
            }
        }

        [UnsupportedOSPlatform("browser")]
        public ICredentials? Credentials
        {
            get
            {
                if (IsNativeHandlerEnabled)
                {
                    return GetCredentials();
                }
                else
                {
                    return _socketHandler!.Credentials;
                }

            }
            set
            {
                if (IsNativeHandlerEnabled)
                {
                    SetCredentials(value!);
                }
                else
                {
                    _socketHandler!.Credentials = value;
                }
            }
        }

        public bool AllowAutoRedirect
        {
            get
            {
                if (IsNativeHandlerEnabled)
                {
                    return GetAllowAutoRedirect();
                }
                else
                {
                    return _socketHandler!.AllowAutoRedirect;
                }
            }
            set
            {
                if (IsNativeHandlerEnabled)
                {
                    SetAllowAutoRedirect(value);
                }
                else
                {
                    _socketHandler!.AllowAutoRedirect = value;
                }
            }
        }

        [UnsupportedOSPlatform("browser")]
        public int MaxConnectionsPerServer
        {
            get
            {
                if (IsNativeHandlerEnabled)
                {
                    return GetMaxConnectionsPerServer();
                }
                else
                {
                    return _socketHandler!.MaxConnectionsPerServer;
                }
            }
            set
            {
                if (IsNativeHandlerEnabled)
                {
                    SetMaxConnectionsPerServer(value);
                }
                else
                {
                    _socketHandler!.MaxConnectionsPerServer = value;
                }
            }
        }

        public long MaxRequestContentBufferSize
        {
            // This property is not supported. In the .NET Framework it was only used when the handler needed to
            // automatically buffer the request content. That only happened if neither 'Content-Length' nor
            // 'Transfer-Encoding: chunked' request headers were specified. So, the handler thus needed to buffer
            // in the request content to determine its length and then would choose 'Content-Length' semantics when
            // POST'ing. In .NET Core, the handler will resolve the ambiguity by always choosing
            // 'Transfer-Encoding: chunked'. The handler will never automatically buffer in the request content.
            get
            {
                return 0; // Returning zero is appropriate since in .NET Framework it means no limit.
            }

            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value);

                if (value > HttpContent.MaxBufferSize)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value,
                        SR.Format(CultureInfo.InvariantCulture, SR.net_http_content_buffersize_limit,
                        HttpContent.MaxBufferSize));
                }

                ObjectDisposedException.ThrowIf(_disposed, this);

                // No-op on property setter.
            }
        }

        [UnsupportedOSPlatform("browser")]
        public int MaxResponseHeadersLength
        {
            get
            {
                if (IsNativeHandlerEnabled)
                {
                    return GetMaxResponseHeadersLength();
                }
                else
                {
                    return _socketHandler!.MaxResponseHeadersLength;
                }
            }
            set
            {
                if (IsNativeHandlerEnabled)
                {
                    SetMaxResponseHeadersLength(value);
                }
                else
                {
                    _socketHandler!.MaxResponseHeadersLength = value;
                }
            }
        }

        public ClientCertificateOption ClientCertificateOptions
        {
            get
            {
                if (IsNativeHandlerEnabled)
                {
                    return GetClientCertificateOptions();
                }
                else
                {
                    return _clientCertificateOptions;
                }
            }
            set
            {
                if (IsNativeHandlerEnabled)
                {
                    SetClientCertificateOptions(value);
                }
                else
                {
                    switch (value)
                    {
                        case ClientCertificateOption.Manual:
                            ThrowForModifiedManagedSslOptionsIfStarted();
                            _clientCertificateOptions = value;
                            _socketHandler!.SslOptions.LocalCertificateSelectionCallback = (sender, targetHost, localCertificates, remoteCertificate, acceptableIssuers) => CertificateHelper.GetEligibleClientCertificate(ClientCertificates)!;
                            break;

                        case ClientCertificateOption.Automatic:
                            ThrowForModifiedManagedSslOptionsIfStarted();
                            _clientCertificateOptions = value;
                            _socketHandler!.SslOptions.LocalCertificateSelectionCallback = (sender, targetHost, localCertificates, remoteCertificate, acceptableIssuers) => CertificateHelper.GetEligibleClientCertificate()!;
                            break;

                        default:
                            throw new ArgumentOutOfRangeException(nameof(value));
                    }
                }
            }
        }

        [UnsupportedOSPlatform("browser")]
        public X509CertificateCollection ClientCertificates
        {
            get
            {
                if (IsNativeHandlerEnabled)
                {
                    return GetClientCertificates();
                }
                else
                {
                    if (ClientCertificateOptions != ClientCertificateOption.Manual)
                    {
                        throw new InvalidOperationException(SR.Format(SR.net_http_invalid_enable_first, nameof(ClientCertificateOptions), nameof(ClientCertificateOption.Manual)));
                    }

                    return _socketHandler!.SslOptions.ClientCertificates ??
                        (_socketHandler!.SslOptions.ClientCertificates = new X509CertificateCollection());
                }
            }
        }

        [UnsupportedOSPlatform("browser")]
        public Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool>? ServerCertificateCustomValidationCallback
        {
            get
            {
                if (IsNativeHandlerEnabled)
                {
                    return GetServerCertificateCustomValidationCallback();
                }
                else
                {
                    return (_socketHandler!.SslOptions.RemoteCertificateValidationCallback?.Target as ConnectHelper.CertificateCallbackMapper)?.FromHttpClientHandler;
                }
            }
            set
            {
                if (IsNativeHandlerEnabled)
                {
                    SetServerCertificateCustomValidationCallback(value);
                }
                else
                {
                    ThrowForModifiedManagedSslOptionsIfStarted();
                    _socketHandler!.SslOptions.RemoteCertificateValidationCallback = value != null ?
                        new ConnectHelper.CertificateCallbackMapper(value).ForSocketsHttpHandler :
                        null;
                }
            }
        }

        [UnsupportedOSPlatform("browser")]
        public bool CheckCertificateRevocationList
        {
            get
            {
                if (IsNativeHandlerEnabled)
                {
                    return GetCheckCertificateRevocationList();
                }
                else
                {
                    return _socketHandler!.SslOptions.CertificateRevocationCheckMode == X509RevocationMode.Online;
                }
            }
            set
            {
                if (IsNativeHandlerEnabled)
                {
                    SetCheckCertificateRevocationList(value);
                }
                else
                {
                    ThrowForModifiedManagedSslOptionsIfStarted();
                    _socketHandler!.SslOptions.CertificateRevocationCheckMode = value ? X509RevocationMode.Online : X509RevocationMode.NoCheck;
                }
            }
        }

        [UnsupportedOSPlatform("browser")]
        public SslProtocols SslProtocols
        {
            get
            {
                if (IsNativeHandlerEnabled)
                {
                    return GetSslProtocols();
                }
                else
                {
                    return _socketHandler!.SslOptions.EnabledSslProtocols;
                }
            }
            set
            {
                if (IsNativeHandlerEnabled)
                {
                    SetSslProtocols(value);
                }
                else
                {
                    ThrowForModifiedManagedSslOptionsIfStarted();
                    _socketHandler!.SslOptions.EnabledSslProtocols = value;
                }
            }
        }

        public IDictionary<string, object?> Properties
        {
            get
            {
                if (IsNativeHandlerEnabled)
                {
                    return GetProperties();
                }
                else
                {
                    return _socketHandler!.Properties;
                }
            }
        }

        public virtual bool SupportsAutomaticDecompression
        {
            get
            {
                if (IsNativeHandlerEnabled)
                {
                    return GetSupportsAutomaticDecompression();
                }
                else
                {
                    return SocketsHttpHandler.SupportsAutomaticDecompression;
                }
            }
        }

        public virtual bool SupportsProxy
        {
            get
            {
                if (IsNativeHandlerEnabled)
                {
                    return GetSupportsProxy();
                }
                else
                {
                    return SocketsHttpHandler.SupportsProxy;
                }
            }
        }

        public virtual bool SupportsRedirectConfiguration
        {
            get
            {
                if (IsNativeHandlerEnabled)
                {
                    return GetSupportsRedirectConfiguration();
                }
                else
                {
                    return SocketsHttpHandler.SupportsRedirectConfiguration;
                }
            }
        }

        [UnsupportedOSPlatform("browser")]
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

        //
        // Attributes are commented out due to https://github.com/dotnet/arcade/issues/7585
        // API compat will fail until this is fixed
        //
        //[UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        //[UnsupportedOSPlatform("ios")]
        //[UnsupportedOSPlatform("tvos")]
        protected internal override HttpResponseMessage Send(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            throw new PlatformNotSupportedException();
        }

        protected internal override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (DiagnosticsHandler.IsGloballyEnabled() && _diagnosticsHandler != null)
            {
                return _diagnosticsHandler!.SendAsync(request, cancellationToken);
            }

            if (IsNativeHandlerEnabled)
            {
                return _nativeHandler!.SendAsync(request, cancellationToken);
            }
            else
            {
                return _socketHandler!.SendAsync(request, cancellationToken);
            }
        }

        // lazy-load the validator func so it can be trimmed by the ILLinker if it isn't used.
        private static Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool>? s_dangerousAcceptAnyServerCertificateValidator;
        [UnsupportedOSPlatform("browser")]
        public static Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool> DangerousAcceptAnyServerCertificateValidator
        {
            get
            {
                return Volatile.Read(ref s_dangerousAcceptAnyServerCertificateValidator) ??
                Interlocked.CompareExchange(ref s_dangerousAcceptAnyServerCertificateValidator, delegate { return true; }, null) ??
                s_dangerousAcceptAnyServerCertificateValidator;
            }
        }

        private void ThrowForModifiedManagedSslOptionsIfStarted()
        {
            // Hack to trigger an InvalidOperationException if a property that's stored on
            // SslOptions is changed, since SslOptions itself does not do any such checks.
            _socketHandler!.SslOptions = _socketHandler!.SslOptions;
        }

        private object InvokeNativeHandlerMethod(string name, params object?[] parameters)
        {
            MethodInfo? method;

            if (!s_cachedMethods.TryGetValue(name, out method))
            {
                method = _nativeHandler!.GetType()!.GetMethod(name);
                s_cachedMethods[name] = method;
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

        private static bool IsNativeHandlerEnabled => RuntimeSettingParser.QueryRuntimeSettingSwitch(
                "System.Net.Http.UseNativeHttpHandler",
                false);
    }
}
