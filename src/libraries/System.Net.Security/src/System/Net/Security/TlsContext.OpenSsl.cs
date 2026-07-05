// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

namespace System.Net.Security
{
    public sealed partial class TlsContext
    {
        // Long-lived OpenSSL SSL_CTX shared by every TlsSession created from this
        // TlsContext. Allocated lazily on the first session's handshake (via
        // AttachSharedNativeContext) so we can defer until cipher/protocol/cert
        // settings on the options bag are finalized. Disposed with the TlsContext.
        //
        // Cert/key/ALPN/SNI that are intrinsically per-session continue to live on
        // the SSL* handle (SafeSslHandle); the SSL_CTX carries only the bits that
        // are stable across every session: protocol mask, cipher list, encryption
        // policy, the cert-verify callback wiring, and the session-resume cache.
        private SafeSslContextHandle? _sslContext;
        private readonly object _sslContextLock = new object();

        partial void AttachSharedNativeContext(SslAuthenticationOptions sessionOptions)
        {
            // Wedge mode reuses SslStream's existing per-handshake SSL_CTX caching path;
            // don't allocate a TlsContext-owned SSL_CTX that would conflict with it.
            if (!_ownsOptions)
            {
                return;
            }

            // AllocateSslContext attaches the server cert directly to SSL_CTX. When the
            // server cert isn't known up front (ServerCertificateSelectionCallback or
            // deferred SetServerContext), each session resolves its own cert after the
            // ClientHello, so a shared SSL_CTX would carry no cert. Fall back to the
            // per-session allocation path in AllocateSslHandle.
            if (_options.IsServer && _options.CertificateContext is null)
            {
                return;
            }

            SafeSslContextHandle? ctx = _sslContext;
            if (ctx is null)
            {
                lock (_sslContextLock)
                {
                    ctx = _sslContext;
                    if (ctx is null)
                    {
                        // allowCached:false bypasses the global SslContextCacheKey lookup;
                        // the handle returned is exclusively owned by this TlsContext.
                        // enableResume honors the AllowTlsResume option on the bag so
                        // server-side session resume works against this owned SSL_CTX.
                        bool enableResume = sessionOptions.AllowTlsResume
                            && !LocalAppContextSwitches.DisableTlsResume
                            && sessionOptions.EncryptionPolicy == EncryptionPolicy.RequireEncryption
                            && sessionOptions.CipherSuitesPolicy == null;
                        ctx = Interop.OpenSsl.GetOrCreateSslContextHandle(sessionOptions, allowCached: false, enableResume: enableResume);
                        _sslContext = ctx;
                    }
                }
            }

            sessionOptions.PreallocatedSslContext = ctx;
        }

        partial void DisposeNativeContext()
        {
            SafeSslContextHandle? ctx = _sslContext;
            _sslContext = null;
            ctx?.Dispose();
        }
    }
}
