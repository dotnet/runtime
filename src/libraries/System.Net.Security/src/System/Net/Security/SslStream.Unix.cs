// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Net.Security
{
    // Routes the SslStream handshake/encrypt/decrypt hot-path through TlsSession on
    // Linux and FreeBSD. The PAL calls underneath are unchanged; this is a wedge
    // that proves TlsSession is expressive enough to host SslStream's TLS engine.
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
            if (_securityContext is null)
            {
                byte[]? thumbPrint = null;
                if (_sslAuthenticationOptions!.IsServer)
                {
                    AcquireServerCredentials(ref thumbPrint);
                }
                else
                {
                    AcquireClientCredentials(ref thumbPrint);
                }
            }

            token = _tlsSession!.HandshakeStepForSslStream(incomingBuffer, out consumed);

            // OpenSSL surfaces CredentialsNeeded when the local cert callback returned
            // null on the first call. Re-run client cert selection then drive the
            // handshake again with no new input, matching the legacy GenerateToken loop.
            if (token.Status.ErrorCode == SecurityStatusPalErrorCode.CredentialsNeeded)
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Info(this, "TlsSession reported 'CredentialsNeeded'; reselecting client credentials.");
                }

                byte[]? thumbPrint = null;
                AcquireClientCredentials(ref thumbPrint, newCredentialsRequested: true);

                token = _tlsSession.HandshakeStepForSslStream(ReadOnlySpan<byte>.Empty, out _);
            }

            // Mirror handles so legacy SslStream paths (cert validation, channel binding,
            // ProcessHandshakeSuccess, renegotiation, Dispose) keep working unchanged.
            _securityContext = _tlsSession.SecurityContext;
            _credentialsHandle = _tlsSession.CredentialsHandle;

            return true;
        }
    }
}
