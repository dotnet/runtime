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

        private readonly object? _underlyingHandler;
        private static MethodInfo? _underlyingHandlerMethod;

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
                handler = (HttpMessageHandler)_underlyingHandler;
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
                    _socketHandler!.Dispose();
                }
                else
                {
                    ((HttpMessageHandler)_underlyingHandler!)!.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        protected static bool IsSocketHandler => IsNativeHandlerEnabled();

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
                    return (bool)GetNativeHandlerProp("UseCookies");
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
                    SetNativeHandlerProp("UseCookies", value);
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
                    _socketHandler!.CookieContainer = value;
                }
                else
                {
                    SetNativeHandlerProp("CookieContainer", value);
                }
            }
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        public IWebProxy? Proxy
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
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
                        _socketHandler!.Credentials = CredentialCache.DefaultCredentials;
                    }
                    else
                    {
                        SetNativeHandlerProp("Credentials", CredentialCache.DefaultCredentials);
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
                        ICredentials? creds = (ICredentials)GetNativeHandlerProp("Credentials");

                        if (creds == CredentialCache.DefaultCredentials)
                        {
                            SetNativeHandlerProp("Credentials", null);
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
                    return (ICredentials)GetNativeHandlerProp("Credentials");
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
                    return _socketHandler!.AllowAutoRedirect;
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
                    _socketHandler!.AllowAutoRedirect = value;
                }
                else
                {
                    SetNativeHandlerProp("AllowAutoRedirect", value);
                }
            }
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
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

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        public int MaxResponseHeadersLength
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("ios")]
        public ClientCertificateOption ClientCertificateOptions
        {
            get => throw new PlatformNotSupportedException();
            set
            {
                throw new PlatformNotSupportedException();
            }
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
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
        public Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool>? ServerCertificateCustomValidationCallback
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        public bool CheckCertificateRevocationList
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        public SslProtocols SslProtocols
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("ios")]
        public IDictionary<string, object?> Properties => throw new PlatformNotSupportedException();

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
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
                return _socketHandler!.SendAsync(request, cancellationToken);
            }
            else
            {
                return (Task<HttpResponseMessage>)InvokeNativeHandlerMethod("SendAsync", request, cancellationToken);
            }
        }

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
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
            Justification = "Unused fields don't make a difference for hashcode quality")]
        private object GetNativeHandlerProp(string name)
        {
            return _underlyingHandler!.GetType()!.GetProperty(name)!.GetValue(_underlyingHandler, null)!;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern",
            Justification = "Unused fields don't make a difference for hashcode quality")]
        private void SetNativeHandlerProp(string name, object? value)
        {
            _underlyingHandler!.GetType()!.GetProperty(name)!.SetValue(_underlyingHandler, value!);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern",
            Justification = "Unused fields don't make a difference for hashcode quality")]
        private object InvokeNativeHandlerMethod(string name, params object[] parameters)
        {
            return _underlyingHandler!.GetType()!.GetMethod(name)!.Invoke(_underlyingHandler, parameters)!;
        }

        // check to see if this is linker friendly or not.
        private static bool IsNativeHandlerEnabled()
        {
            if (!AppContext.TryGetSwitch("System.Net.Http.UseNativeHttpHandler", out bool isEnabled))
            {
                return false;
            }

            return isEnabled;
        }
    }
}