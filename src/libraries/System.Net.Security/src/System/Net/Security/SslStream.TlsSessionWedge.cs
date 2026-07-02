// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Security
{
    // Routes the SslStream handshake hot-path through TlsSession. The PAL calls
    // underneath are unchanged; this is a wedge that proves TlsSession is
    // expressive enough to host SslStream's TLS engine. Compiled on Linux,
    // FreeBSD, and Windows.
    //
    // SslStream's _securityContext / _credentialsHandle fields are mirrored from
    // the TlsSession after each step so that the rest of SslStream (cert
    // validation, channel binding, ProcessHandshakeSuccess, renegotiation,
    // dispose) continues to work against the same SafeHandles.
    public partial class SslStream
    {
        private TlsSession? _tlsSession;

        private void EnsureTlsSession()
        {
            if (_tlsSession is null)
            {
                Debug.Assert(_sslAuthenticationOptions != null);
                TlsContext ctx = TlsContext.WrapShared(_sslAuthenticationOptions);
                _tlsSession = TlsSession.Create(ctx);

                // SslStream owns post-handshake certificate validation (see
                // SslStream.IO.cs ProcessHandshakeSuccess). Tell TlsSession not to run
                // its own callback so the user delegate sees the SslStream as sender
                // and isn't invoked twice.
                _tlsSession.SuppressInternalCertificateValidation = true;
            }
        }

        private partial bool TryNextMessageViaTlsSession(ReadOnlySpan<byte> incomingBuffer, out ProtocolToken token, out int consumed)
        {
            EnsureTlsSession();

            // The legacy GenerateToken acquires credentials before the first PAL call.
            // On Unix AcquireCredentialsHandle is a no-op (returns null), but
            // AcquireServerCredentials has a side effect we must preserve: it resolves
            // the cert via ServerCertSelectionDelegate / CertSelectionDelegate /
            // CertificateContext and assigns _sslAuthenticationOptions.CertificateContext,
            // which the OpenSSL handshake asserts on. AcquireClientCredentials similarly
            // bootstraps the client cert context. Run them once per handshake before the
            // first PAL call.
            bool refreshCredentialNeeded = _securityContext is null;
            bool cachedCreds = false;
            bool sendTrustList = false;
            byte[]? thumbPrint = null;
            try
            {
                if (refreshCredentialNeeded)
                {
                    if (_sslAuthenticationOptions!.IsServer)
                    {
                        sendTrustList = _sslAuthenticationOptions.CertificateContext?.Trust?._sendTrustInHandshake ?? false;
                        cachedCreds = AcquireServerCredentials(ref thumbPrint);
                    }
                    else
                    {
                        cachedCreds = AcquireClientCredentials(ref thumbPrint);
                    }

                    // SChannel-style PALs populate SslStream._credentialsHandle from
                    // SslSessionsCache before the first ASC/ISC. Seed TlsSession with it
                    // so its ref parameter starts from the cached handle rather than null.
                    _tlsSession!.CredentialsHandle = _credentialsHandle;
                }

                token = _tlsSession!.HandshakeStepForSslStream(incomingBuffer, out consumed);

                // SChannel server-side ALPN: when the first ASC call returns
                // HandshakeStarted, the wire bytes were consumed but ASC stopped so we
                // can run SelectApplicationProtocol with the parsed ClientHello before
                // generating the ServerHello. Re-enter with no new input afterwards.
                if (token.Status.ErrorCode == SecurityStatusPalErrorCode.HandshakeStarted)
                {
                    token.Status = SslStreamPal.SelectApplicationProtocol(
                        _tlsSession.CredentialsHandle!,
                        _tlsSession.SecurityContext!,
                        _sslAuthenticationOptions!,
                        _lastFrame.RawApplicationProtocols);

                    if (token.Status.ErrorCode == SecurityStatusPalErrorCode.OK)
                    {
                        token = _tlsSession.HandshakeStepForSslStream(ReadOnlySpan<byte>.Empty, out _);
                    }
                }

                // OpenSSL surfaces CredentialsNeeded when the local cert callback returned
                // null on the first call. SChannel surfaces it on a later ISC step after
                // the server's CertificateRequest is parsed. Re-run client cert selection
                // with newCredentialsRequested=true (mirrors legacy GenerateToken), then
                // drive the handshake again with no new input. Set refreshCredentialNeeded
                // so the finally-block caches the new cert-bound credential.
                if (token.Status.ErrorCode == SecurityStatusPalErrorCode.CredentialsNeeded)
                {
                    if (NetEventSource.Log.IsEnabled())
                    {
                        NetEventSource.Info(this, "TlsSession reported 'CredentialsNeeded'; reselecting client credentials.");
                    }

                    refreshCredentialNeeded = true;
                    cachedCreds = AcquireClientCredentials(ref thumbPrint, newCredentialsRequested: true);
                    _tlsSession.CredentialsHandle = _credentialsHandle;

                    token = _tlsSession.HandshakeStepForSslStream(ReadOnlySpan<byte>.Empty, out _);
                }

                // Mirror handles so legacy SslStream paths (cert validation, channel binding,
                // ProcessHandshakeSuccess, renegotiation, Dispose) keep working unchanged.
                _securityContext = _tlsSession.SecurityContext;
                _credentialsHandle = _tlsSession.CredentialsHandle;
            }
            finally
            {
                if (refreshCredentialNeeded)
                {
                    // Mirror legacy GenerateToken bookkeeping: the PAL has bumped the cred
                    // refcount, so drop our reference. Then publish a fresh entry to
                    // SslSessionsCache so subsequent connections to the same host can
                    // resume the TLS session (Windows SChannel session ticket lives on
                    // the cred handle).
                    _credentialsHandle?.Dispose();

                    bool wouldCache = !cachedCreds && _securityContext is not null && !_securityContext.IsInvalid &&
                        _credentialsHandle is not null && !_credentialsHandle.IsInvalid;

                    if (wouldCache)
                    {
                        SslSessionsCache.CacheCredential(
                            _credentialsHandle!,
                            thumbPrint,
                            _sslAuthenticationOptions!.EnabledSslProtocols,
                            _sslAuthenticationOptions.IsServer,
                            _sslAuthenticationOptions.EncryptionPolicy,
                            _sslAuthenticationOptions.CertificateRevocationCheckMode != X509RevocationMode.NoCheck,
                            _sslAuthenticationOptions.AllowTlsResume,
                            sendTrustList,
                            _sslAuthenticationOptions.AllowRsaPssPadding,
                            _sslAuthenticationOptions.AllowRsaPkcs1Padding);
                    }
                }
            }

            return true;
        }
    }
}
