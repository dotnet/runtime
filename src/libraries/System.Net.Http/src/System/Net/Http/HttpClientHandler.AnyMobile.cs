// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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

        private readonly HttpMessageHandler? _underlyingHandler;

        private volatile bool _disposed;

        public HttpClientHandler()
        {
            HttpMessageHandler handler;

            if (IsSocketHandler)
            {
                _socketHandler = new SocketsHttpHandler();
                handler = _socketHandler;
            }
            else
            {
                _underlyingHandler = CreateNativeHandler();
                handler = _underlyingHandler;
            }

            if (DiagnosticsHandler.IsGloballyEnabled)
            {
                _diagnosticsHandler = new DiagnosticsHandler(handler);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;

                if (IsSocketHandler)
                {
                    _socketHandler!.Dispose();
                }
                else
                {
                    _underlyingHandler!.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        public virtual bool SupportsProxy => false;

        [UnsupportedOSPlatform("browser")]
        public bool UseCookies
        {
            get
            {
                if (IsSocketHandler)
                {
                    return _socketHandler!.UseCookies;
                }
                else
                {
                    return GetUseCookies();
                }
            }
            set
            {
                if (IsSocketHandler)
                {
                    _socketHandler!.UseCookies = value;
                }
                else
                {
                    SetUseCookies(value);
                }
            }
        }

        [UnsupportedOSPlatform("browser")]
        public CookieContainer CookieContainer
        {
            get
            {
                if (IsSocketHandler)
                {
                    return _socketHandler!.CookieContainer;
                }
                else
                {
                    return GetCookieContainer();
                }
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                if (IsSocketHandler)
                {
                    _socketHandler!.CookieContainer = value;
                }
                else
                {
                    SetCookieContainer(value);
                }
            }
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        //[UnsupportedOSPlatform("ios")]
        //[UnsupportedOSPlatform("tvos")]
        //[UnsupportedOSPlatform("maccatalyst")]
        public IWebProxy? Proxy
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        //[UnsupportedOSPlatform("ios")]
        //[UnsupportedOSPlatform("tvos")]
        //[UnsupportedOSPlatform("maccatalyst")]
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
                if (IsSocketHandler)
                {
                    creds = _socketHandler!.Credentials;
                }
                else
                {
                    creds = GetCredentials();
                }

                return creds == CredentialCache.DefaultCredentials;
            }
            set
            {
                if (value)
                {
                    if (IsSocketHandler)
                    {
                        _socketHandler!.Credentials = CredentialCache.DefaultCredentials;
                    }
                    else
                    {
                        SetCredentials(CredentialCache.DefaultCredentials);
                    }
                }
                else
                {
                    if (IsSocketHandler)
                    {
                        if (_socketHandler!.Credentials == CredentialCache.DefaultCredentials)
                        {
                            _socketHandler!.Credentials = null;
                        }
                    }
                    else
                    {
                        ICredentials? creds = GetCredentials();

                        if (creds == CredentialCache.DefaultCredentials)
                        {
                            SetCredentials(null!);
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
                if (IsSocketHandler)
                {
                    return _socketHandler!.Credentials;
                }
                else
                {
                    return GetCredentials();
                }

            }
            set
            {
                if (IsSocketHandler)
                {
                    _socketHandler!.Credentials = value;
                }
                else
                {
                    SetCredentials(value!);
                }
            }
        }

        public bool AllowAutoRedirect
        {
            get
            {
                if (IsSocketHandler)
                {
                    return _socketHandler!.AllowAutoRedirect;
                }
                else
                {
                    return GetAllowAutoRedirect();
                }
            }
            set
            {
                if (IsSocketHandler)
                {
                    _socketHandler!.AllowAutoRedirect = value;
                }
                else
                {
                    SetAllowAutoRedirect(value);
                }
            }
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        //[UnsupportedOSPlatform("ios")]
        //[UnsupportedOSPlatform("tvos")]
        //[UnsupportedOSPlatform("maccatalyst")]
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
        //[UnsupportedOSPlatform("ios")]
        //[UnsupportedOSPlatform("tvos")]
        //[UnsupportedOSPlatform("maccatalyst")]
        public int MaxResponseHeadersLength
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("android")]
        //[UnsupportedOSPlatform("ios")]
        //[UnsupportedOSPlatform("tvos")]
        //[UnsupportedOSPlatform("maccatalyst")]
        public ClientCertificateOption ClientCertificateOptions
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        //[UnsupportedOSPlatform("ios")]
        //[UnsupportedOSPlatform("tvos")]
        //[UnsupportedOSPlatform("maccatalyst")]
        public X509CertificateCollection ClientCertificates
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        //[UnsupportedOSPlatform("ios")]
        //[UnsupportedOSPlatform("tvos")]
        //[UnsupportedOSPlatform("maccatalyst")]
        public Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool>? ServerCertificateCustomValidationCallback
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        //[UnsupportedOSPlatform("ios")]
        //[UnsupportedOSPlatform("tvos")]
        //[UnsupportedOSPlatform("maccatalyst")]
        public bool CheckCertificateRevocationList
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        //[UnsupportedOSPlatform("ios")]
        //[UnsupportedOSPlatform("tvos")]
        //[UnsupportedOSPlatform("maccatalyst")]
        public SslProtocols SslProtocols
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("android")]
        //[UnsupportedOSPlatform("ios")]
        //[UnsupportedOSPlatform("tvos")]
        //[UnsupportedOSPlatform("maccatalyst")]
        public IDictionary<string, object?> Properties => throw new PlatformNotSupportedException();

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
            if (DiagnosticsHandler.IsGloballyEnabled && _diagnosticsHandler != null)
            {
                return _diagnosticsHandler!.SendAsync(request, cancellationToken);
            }

            if (IsSocketHandler)
            {
                return _socketHandler!.SendAsync(request, cancellationToken);
            }
            else
            {
                return _underlyingHandler!.SendAsync(request, cancellationToken);
            }
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        //[UnsupportedOSPlatform("ios")]
        //[UnsupportedOSPlatform("tvos")]
        //[UnsupportedOSPlatform("maccatalyst")]
        public static Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool> DangerousAcceptAnyServerCertificateValidator =>
            throw new PlatformNotSupportedException();

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().ToString());
            }
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern",
            Justification = "Xamarin dependencies are not available during libraries build")]
        private object InvokeNativeHandlerMethod(string name, params object?[] parameters)
        {
            return _underlyingHandler!.GetType()!.GetMethod(name)!.Invoke(_underlyingHandler, parameters)!;
        }

        private static bool IsSocketHandler => IsSocketHandlerEnabled();

        private static bool IsSocketHandlerEnabled()
        {
            if (!AppContext.TryGetSwitch("System.Net.Http.UseNativeHttpHandler", out bool isNativeHandlerEnabled))
            {
                return true;
            }

            return !isNativeHandlerEnabled;
        }
    }
}