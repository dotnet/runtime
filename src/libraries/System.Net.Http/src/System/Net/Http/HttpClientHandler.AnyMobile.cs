// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Security;
using System.Reflection;
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
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

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

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("maccatalyst")]
        public ICredentials? DefaultProxyCredentials
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
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

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("maccatalyst")]
        public int MaxConnectionsPerServer
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
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
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                if (value > HttpContent.MaxBufferSize)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value,
                        SR.Format(CultureInfo.InvariantCulture, SR.net_http_content_buffersize_limit,
                        HttpContent.MaxBufferSize));
                }

                CheckDisposed();

                // No-op on property setter.
            }
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("maccatalyst")]
        public int MaxResponseHeadersLength
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("maccatalyst")]
        public ClientCertificateOption ClientCertificateOptions
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("maccatalyst")]
        public X509CertificateCollection ClientCertificates
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("maccatalyst")]
        public Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool>? ServerCertificateCustomValidationCallback
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("maccatalyst")]
        public bool CheckCertificateRevocationList
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("maccatalyst")]
        public SslProtocols SslProtocols
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("maccatalyst")]
        public IDictionary<string, object?> Properties => throw new PlatformNotSupportedException();

        //
        // Attributes are commented out due to https://github.com/dotnet/arcade/issues/7585
        // API compat will fail until this is fixed
        //
        //[UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        //[UnsupportedOSPlatform("ios")]
        //[UnsupportedOSPlatform("tvos")]
        //[UnsupportedOSPlatform("maccatalyst")]
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

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("maccatalyst")]
        public static Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool> DangerousAcceptAnyServerCertificateValidator =>
            throw new PlatformNotSupportedException();

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().ToString());
            }
        }

        private object InvokeNativeHandlerMethod(string name, params object?[] parameters)
        {
            MethodInfo? method;

            if (!s_cachedMethods.TryGetValue(name, out method))
            {
                method = _nativeHandler!.GetType()!.GetMethod(name);
                s_cachedMethods[name] = method;
            }

            return method!.Invoke(_nativeHandler, parameters)!;
        }

        private static bool IsNativeHandlerEnabled => RuntimeSettingParser.QueryRuntimeSettingSwitch(
                "System.Net.Http.UseNativeHttpHandler",
                false);
    }
}