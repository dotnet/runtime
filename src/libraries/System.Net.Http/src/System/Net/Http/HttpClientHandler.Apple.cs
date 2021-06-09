// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net.Security;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    public partial class HttpClientHandler : HttpMessageHandler
    {
        private static MethodInfo? _underlyingHandlerMethod;

        private readonly SocketsHttpHandler _socketHandler;
        private readonly object? _appleHandler;

        private readonly DiagnosticsHandler? _diagnosticsHandler;

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
                _appleHandler = CreateNativeHandler();
                handler = (HttpMessageHandler)_appleHandler;
            }

            if (DiagnosticsHandler.IsGloballyEnabled())
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
                    _socketHandler.Dispose();
                }
                else
                {
                    (HttpMessageHandler)_appleHandler.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        // not sure
        public virtual bool SupportsAutomaticDecompression => false;
        public virtual bool SupportsProxy => false;
        public virtual bool SupportsRedirectConfiguration => true;

        public bool UseCookies
        {
            get
            {
                if (IsSocketHandler)
                {
                    return _socketHandler.UseCookies;
                }
                else
                {
                    return (bool)GetNativeHandlerProp("UseCookies");
                }
            }
            set
            {
                if (IsSocketHandler)
                {
                    _socketHandler.UseCookies = value;
                }
                else
                {
                    SetNativeHandlerProp("UseCookies", value);
                }
            }
        }

        public CookieContainer CookieContainer
        {
            get
            {
                if (IsSocketHandler)
                {
                    return _socketHandler.CookieContainer;
                }
                else
                {
                    return (CookieContainer)GetNativeHandlerProp("CookieContainer");
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
                    _socketHandler.CookieContainer = value;
                }
                else
                {
                    SetNativeHandlerProp("CookieContainer", value);
                }
            }
        }

        [UnsupportedOSPlatform("ios")]
        public DecompressionMethods AutomaticDecompression
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("ios")]
        public bool UseProxy
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("ios")]
        public IWebProxy? Proxy
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("ios")]
        public ICredentials? DefaultProxyCredentials
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("ios")]
        public bool PreAuthenticate
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public bool UseDefaultCredentials
        {
            // SocketsHttpHandler doesn't have a separate UseDefaultCredentials property.  There
            // is just a Credentials property.  So, we need to map the behavior.
            // Same with the native handler.
            get
            {
                ICredentials creds;
                if (IsSocketHandler)
                {
                    creds = _socketHandler.Credentials;
                }
                else
                {
                    creds = (ICredentials)GetNativeHandlerProp("Credentials");
                }

                return creds == CredentialCache.DefaultCredentials;
            }
            set
            {
                if (value)
                {
                    if (IsSocketHandler)
                    {
                        _socketHandler.Credentials = CredentialCache.DefaultCredentials;
                    }
                    else
                    {
                        SetNativeHandlerProp("Credentials", CredentialCache.DefaultCredentials);
                    }
                }
                else
                {
                    if (_underlyingHandler.Credentials == CredentialCache.DefaultCredentials)
                    {
                        // Only clear out the Credentials property if it was a DefaultCredentials.
                        if (IsSocketHandler)
                        {
                            _socketHandler.Credentials = null;
                        }
                        else
                        {
                            SetNativeHandlerProp("Credentials", null);
                        }
                    }
                }
            }
        }

        public ICredentials? Credentials
        {
            get
            {
                if (IsSocketHandler)
                {
                    return _socketHandler.Credentials;
                }
                else
                {
                    return (ICredentials)GetNativeHandlerProp("Credentials");
                }
                
            }
            set
            {
                if (IsSocketHandler)
                {
                    _socketHandler.Credentials = value;
                }
                else
                {
                    SetNativeHandlerProp("Credentials", value);
                }
            }
        }

        public bool AllowAutoRedirect
        {
            get
            {
                if (IsSocketHandler)
                {
                    return _socketHandler.AllowAutoRedirect;
                }
                else
                {
                    return (bool)GetNativeHandlerProp("AllowAutoRedirect");
                }
            }
            set
            {
                if (IsSocketHandler)
                {
                    _socketHandler.AllowAutoRedirect = value;
                }
                else
                {
                    SetNativeHandlerProp("AllowAutoRedirect", value);
                }
            }
        }

        [UnsupportedOSPlatform("ios")]
        public int MaxAutomaticRedirections
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("ios")]
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

        [UnsupportedOSPlatform("ios")]
        public int MaxResponseHeadersLength
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public ClientCertificateOption ClientCertificateOptions
        {
            get => throw new PlatformNotSupportedException();
            set
            {
                throw new PlatformNotSupportedException();
            }
        }

        [UnsupportedOSPlatform("ios")]
        public X509CertificateCollection ClientCertificates
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }

        // this may be able to map somehow to NSUrlSessionHandlerTrustOverrideCallback?
        [UnsupportedOSPlatform("ios")]
        public Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool>? ServerCertificateCustomValidationCallback
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("ios")]
        public bool CheckCertificateRevocationList
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("ios")]
        public SslProtocols SslProtocols
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("ios")]
        public IDictionary<string, object?> Properties => throw new PlatformNotSupportedException();

        [UnsupportedOSPlatform("ios")]
        protected internal override HttpResponseMessage Send(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            throw new PlatformNotSupportedException();
        }

        protected internal override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (DiagnosticsHandler.IsEnabled() && _diagnosticsHandler != null)
            {
                return _diagnosticsHandler.SendAsync(request, cancellationToken);
            }

            if (IsSocketHandler)
            {
                return _socketHandler.SendAsync(request, cancellationToken);
            }
            else
            {
                return (Task<HttpResponseMessage>)InvokeNativeHandlerMethod("SendAsync", request, cancellationToken);
            }
        }

        [UnsupportedOSPlatform("ios")]
        public static Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool> DangerousAcceptAnyServerCertificateValidator =>
            throw new PlatformNotSupportedException();

        // move these to a common place
        private object GetNativeHandlerProp(string name)
        {
            return _appleHandler!.GetType().GetProperty(name).GetValue(_appleHandler, null);
        }

        private void SetNativeHandlerProp(string name, object value)
        {
            _appleHandler!.GetType().GetProperty(name).SetValue(_appleHandler, value);
        }

        private object InvokeNativeHandlerMethod(string name, params object[] parameters)
        {
            return _appleHandler!.Invoke(_appleHandler, parameters)!;
        }

        private static bool IsSocketHandler => true/false;

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().ToString());
            }
        }
    }
}
