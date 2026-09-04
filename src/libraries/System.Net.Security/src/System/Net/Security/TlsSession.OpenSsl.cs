// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using Microsoft.Win32.SafeHandles;

namespace System.Net.Security
{
    public partial class TlsSession
    {
        // When true, socket-bound I/O delegates ciphertext directly to OpenSSL via
        // SSL_set_fd / SSL_do_handshake / SSL_read / SSL_write, bypassing the
        // managed ProcessHandshake/Encrypt/Decrypt loop and its scratch buffers.
        private bool _useFdMode;

        // Socket that will be bound to OpenSSL once server options are resolved.
        // Non-null only in the deferred-server socket-bound flow: the session
        // returns nativeBindingEnabled=false so the managed pre-fetch loop can
        // parse the ClientHello and surface NeedsServerOptions. OnServerContextSet
        // then activates fd-mode with the peeked bytes replayed via the socket BIO.
        private SafeSocketHandle? _pendingFdSocket;

        // Socket-replay BIO populated by TryPeekClientHello via BioPeekTlsFrame. Holds
        // the ClientHello record buffered off the fd; the same BIO becomes the SSL's
        // read BIO once OnServerContextSet transfers ownership to _options.PreallocatedReadBio.
        // Freed by OnDispose if the session is disposed before that handoff runs.
        private SafeBioHandle? _peekBio;

        // Post-handshake client-certificate exchange state for fd-mode sessions. Set by
        // TryFastRequestClientCertificate and cleared once the peer's answer has been
        // processed; _fdPostHandshakeRequestSent tracks whether the CertificateRequest /
        // HelloRequest flight has already been written to the socket.
        private bool _fdPostHandshakeAuthPending;
        private bool _fdPostHandshakeRequestSent;
        // Set when SSL_do_handshake stalls on SSL_ERROR_WANT_WRITE during the post-handshake
        // exchange (the socket send buffer filled mid-flight). It forces the next Handshake()
        // to re-enter OpenSSL to finish the write even when the peer is idle, so a write-blocked
        // flight cannot deadlock behind the PeerHasPendingData() readable-data gate.
        private bool _fdPostHandshakeWriteBlocked;

        // Bind the socket directly to the SSL object so OpenSSL drives ciphertext
        // I/O itself. AllocateSslHandle inspects options.SocketHandle and skips
        // the ManagedSpanBio installation when set. With fd-mode active, no
        // managed Socket wrapper is needed - OpenSSL calls recv/send on the fd.
        //
        // Server sessions created without options up front (SNI-driven callback)
        // cannot go fd-mode immediately: SSL_set_fd would let OpenSSL consume the
        // ClientHello before managed code sees SNI. Defer binding until
        // OnServerContextSet runs; until then the session uses the managed loop.
        partial void EnableNativeSocketBinding(SafeSocketHandle socket, ref bool nativeBindingEnabled)
        {
            // Defer binding to fd-mode whenever we need to see the ClientHello managed-side
            // first: either because options aren't set yet (SNI-driven callback flow) or
            // because ClientHello capture is on (the default). Callers can disable capture
            // via the System.Net.Security.CaptureClientHello AppContext switch to skip the
            // peek and take the SSL_set_fd fast path when options are already supplied.
            if (_context!.IsServer && (!_hasServerOptions || LocalAppContextSwitches.CaptureClientHello))
            {
                _pendingFdSocket = socket;
                nativeBindingEnabled = false;
                return;
            }

            _options.SocketHandle = socket;
            _useFdMode = true;
            nativeBindingEnabled = true;
        }

        // Activated when the caller supplies server options in response to
        // NeedsServerOptions. In the deferred socket-bound flow, hand the peeked
        // ClientHello bytes to a socket-replay BIO so OpenSSL sees them, then
        // switch subsequent Handshake/Read/Write calls onto the fd-mode fast path.
        partial void OnServerContextSet()
        {
            if (_pendingFdSocket is null)
            {
                return;
            }

            if (_peekBio is not null)
            {
                // Native-peek path (TryPeekClientHello ran): hand the pre-populated
                // BIO to the options bag; SafeSslHandle.Create adopts it as the read
                // BIO. No managed byte[] copy, no ReplayPrefix. We keep _peekBio
                // referenced after transfer so GetClientHelloBytes can span the
                // retained peek buffer via BioGetReplayPrefix; the SafeBioHandle stays
                // valid (SSL* is the real owner, our reference DangerousReleases
                // the parent on session Dispose()).
                _options.PreallocatedReadBio = _peekBio;
            }
            else if (_socketInBuffer.ActiveLength > 0)
            {
                // Legacy managed pre-fetch path: still exercised e.g. by a caller
                // driving ProcessHandshake directly rather than Handshake(). Copy the
                // peeked bytes so SafeSslHandle.Create's BioNewSocketReplay-with-prefix
                // branch can seed the replay BIO.
                _options.ReplayPrefix = _socketInBuffer.ActiveReadOnlySpan.ToArray();
            }

            _options.SocketHandle = _pendingFdSocket;
            _pendingFdSocket = null;

            // Discard any managed pre-fetch bytes; ownership transfers to the native BIO now.
            if (_socketInBuffer.ActiveLength > 0)
            {
                _socketInBuffer.Discard(_socketInBuffer.ActiveLength);
            }

            _useFdMode = true;
        }

        // Native ClientHello peek for the deferred socket-bound flow AND the always-capture
        // path (server session with options up front + CaptureClientHello switch on).
        // Creates a socket-replay BIO on the fd, buffers a full TLS record via
        // BioPeekTlsFrame, parses via TlsFrameHelper, and populates ClientHelloInfo /
        // TargetHostName from the SNI extension. In the deferred flow returns
        // NeedsServerOptions so the caller resolves via SetContext; in the capture
        // flow transfers the peek BIO to the pending SSL* and falls through to fd-mode.
        // Either way the BIO stays alive so GetClientHelloBytes can span its retained
        // prefix until Dispose().
        partial void TryPeekClientHello(ref TlsOperationStatus? result)
        {
            if (_pendingFdSocket is null)
            {
                return;
            }

            // Deferred flow: caller hasn't resolved NeedsServerOptions yet - re-surface
            // without re-reading the fd. Not reached on the capture-only path because we
            // transition to fd-mode on the same TryPeekClientHello call that populates it.
            if (_clientHelloInfo is not null && !_hasServerOptions)
            {
                result = TlsOperationStatus.NeedsTlsContext;
                return;
            }

            if (_peekBio is null)
            {
                _peekBio = Interop.Ssl.BioNewSocketReplay(_pendingFdSocket, ReadOnlySpan<byte>.Empty);
                if (_peekBio.IsInvalid)
                {
                    _peekBio.Dispose();
                    _peekBio = null;
                    throw Interop.OpenSsl.CreateSslException(SR.net_ssl_read_bio_failed_error);
                }
            }

            unsafe
            {
                int rc = Interop.Ssl.BioPeekTlsFrame(_peekBio, out byte* framePtr, out int frameLen);
                if (rc == 0)
                {
                    // Need more bytes off the socket. Caller polls SelectRead and retries.
                    result = TlsOperationStatus.NeedMoreData;
                    return;
                }
                if (rc < 0)
                {
                    throw new IOException(SR.net_ssl_read_bio_failed_error);
                }

                ReadOnlySpan<byte> frame = new ReadOnlySpan<byte>(framePtr, frameLen);
                SslClientHelloInfo? parsed = TryParseClientHello(frame, out _);
                if (parsed is null)
                {
                    // TlsFrameHelper couldn't parse the record as a ClientHello.
                    throw new IOException(SR.net_io_decrypt);
                }

                _clientHelloInfo = parsed;
                if (!string.IsNullOrEmpty(parsed.Value.ServerName))
                {
                    _sessionTargetHost = parsed.Value.ServerName;
                }
            }

            if (!_hasServerOptions)
            {
                // Deferred / SNI-callback flow: caller inspects ClientHelloInfo and
                // resolves via SetContext; OnServerContextSet then transfers the
                // peek BIO to _options.PreallocatedReadBio.
                result = TlsOperationStatus.NeedsTlsContext;
                return;
            }

            // Always-capture flow: options are already supplied. Transfer the peek BIO
            // to the pending SSL* now and drive the fast-path handshake step so the
            // caller sees the same behavior as the pre-capture SSL_set_fd path.
            // See OnServerContextSet: _peekBio stays referenced after transfer.
            _options.PreallocatedReadBio = _peekBio;
            _options.SocketHandle = _pendingFdSocket;
            _pendingFdSocket = null;
            _useFdMode = true;

            TryFastHandshake(ref result);
        }

        // Called from TlsSession.Dispose. If the caller disposed the session before
        // OnServerContextSet transferred the peek BIO to _options.PreallocatedReadBio,
        // release it here so the native buffer / fd reference are freed.
        partial void OnDispose()
        {
            _peekBio?.Dispose();
            _peekBio = null;
        }

        // Returns a ReadOnlySpan over the socket-replay BIO's retained peek buffer.
        // Valid as long as the SafeBioHandle is open. Consumers reach here through
        // GetClientHelloBytes, which does ThrowIfDisposed() first.
        partial void TryGetNativeClientHelloBytes(ref ReadOnlySpan<byte> bytes)
        {
            if (_peekBio is null || _peekBio.IsInvalid)
            {
                return;
            }

            unsafe
            {
                if (Interop.Ssl.BioGetReplayPrefix(_peekBio, out byte* ptr, out int len) == 1 && len > 0)
                {
                    bytes = new ReadOnlySpan<byte>(ptr, len);
                }
            }
        }

        partial void TryFastHandshake(ref TlsOperationStatus? result)
        {
            if (!_useFdMode)
            {
                return;
            }

            // Mirror HandshakeBufferedCore's external-validation gates. A rejected peer
            // certificate faults the session, and a suspension the caller hasn't resolved
            // yet re-surfaces without re-entering OpenSSL.
            if (_externalValidationFault is not null && (_isHandshakeComplete || !_externalValidationResolved))
            {
                throw _externalValidationFault;
            }

            if (_externalValidationPending)
            {
                result = TlsOperationStatus.NeedsCertificateValidation;
                return;
            }

            SafeSslHandle ssl = EnsureFdSslHandle();

            if (_fdPostHandshakeAuthPending)
            {
                result = DriveFdPostHandshakeAuth(ssl);
                return;
            }

            int ret = Interop.Ssl.SslDoHandshake(ssl, out Interop.Ssl.SslErrorCode err);
            if (ret == 1)
            {
                OnHandshakeCompleted();

                // OnHandshakeCompleted captures the peer certificate for external validation
                // (unless the caller already resolved it or suppressed the internal callback).
                // Surface the suspension so the caller's validation runs, exactly like the
                // buffered path; swallowing it here would silently skip certificate validation.
                result = _externalValidationPending
                    ? TlsOperationStatus.NeedsCertificateValidation
                    : TlsOperationStatus.Complete;
                return;
            }
            result = MapSslError(err, ret, SR.net_ssl_handshake_failed_error);
        }

        // Drives the post-handshake client-certificate exchange started by
        // TryFastRequestClientCertificate. SSL_do_handshake reports success as soon as our
        // flight has been written to the socket, i.e. before the peer has answered, so
        // completing on that alone would capture a peer certificate that has not arrived yet.
        // HandshakeBufferedCore avoids this by refusing to enter the PAL without caller-supplied
        // peer bytes; the fd path has no caller-supplied bytes, so gate re-entry on the socket
        // actually having something to process (the analog of SslStream.RenegotiateAsync, which
        // always receives a handshake frame before re-testing the status). The gate is bypassed
        // while our own flight still needs writing so a send that backpressures on
        // SSL_ERROR_WANT_WRITE resumes instead of waiting on the (still idle) peer.
        private TlsOperationStatus DriveFdPostHandshakeAuth(SafeSslHandle ssl)
        {
            bool flushingRequest = !_fdPostHandshakeRequestSent;
            if (!flushingRequest && !_fdPostHandshakeWriteBlocked && !PeerHasPendingData())
            {
                return TlsOperationStatus.NeedMoreData;
            }

            int ret = Interop.Ssl.SslDoHandshake(ssl, out Interop.Ssl.SslErrorCode err);
            bool writeBlocked = err == Interop.Ssl.SslErrorCode.SSL_ERROR_WANT_WRITE;
            // Track a write-side stall so the next call re-enters to finish the flush even if
            // the peer sends nothing in the meantime (OpenSSL may still owe handshake bytes
            // after consuming the peer's flight, e.g. the TLS 1.2 renegotiation Finished).
            _fdPostHandshakeWriteBlocked = writeBlocked;

            // The request flight is fully on the wire unless the send backpressured
            // (SSL_ERROR_WANT_WRITE). A partial write keeps flushingRequest sticky so the
            // resumed call finishes writing instead of waiting for a peer answer that cannot
            // come; WANT_READ means the request is out and OpenSSL is now awaiting the peer.
            if (!writeBlocked)
            {
                _fdPostHandshakeRequestSent = true;
            }

            if (ret != 1)
            {
                return MapSslError(err, ret, SR.net_ssl_handshake_failed_error);
            }

            if (flushingRequest ||
                !Interop.Ssl.IsSslStateOK(ssl) ||
                Interop.Ssl.IsSslRenegotiatePending(ssl))
            {
                // The request has just gone out (TLS 1.3 post-handshake auth) or the
                // renegotiation is still in flight (TLS 1.2); wait for the peer.
                return TlsOperationStatus.NeedMoreData;
            }

            _fdPostHandshakeAuthPending = false;
            OnHandshakeCompleted();
            return _externalValidationPending
                ? TlsOperationStatus.NeedsCertificateValidation
                : TlsOperationStatus.Complete;
        }

        // True when the socket has ciphertext waiting (or has been closed / reset). fd-mode
        // sessions leave the socket to OpenSSL, so the managed Socket wrapper is created
        // lazily here; TlsSession.Dispose disposes it (and with it the owned handle).
        private bool PeerHasPendingData()
        {
            _socket ??= new Socket(_socketHandle!);
            return _socket.Poll(0, SelectMode.SelectRead);
        }

        // Socket-bound (fd-mode) analog of RequestClientCertificateBufferedCore. OpenSSL owns
        // the fd, so the CertificateRequest / HelloRequest is written straight to the socket
        // when the caller drives Handshake(); there are no staged ciphertext bytes to drain.
        // All this hook has to do is ask the PAL for the post-handshake exchange and re-arm
        // the handshake state machine so Handshake() re-enters OpenSSL and re-surfaces
        // NeedsCertificateValidation once the peer's certificate arrives.
        partial void TryFastRequestClientCertificate(ref TlsOperationStatus? result)
        {
            if (!_useFdMode)
            {
                return;
            }

            if (!_context!.IsServer)
            {
                throw new InvalidOperationException(SR.net_tlssession_request_client_cert_server_only);
            }

            if (!_isHandshakeComplete || _securityContext is null || _securityContext.IsInvalid)
            {
                throw new InvalidOperationException(SR.net_tlssession_handshake_not_complete);
            }

            // Match SslStream.RenegotiateAsync: promote the client-certificate requirement for
            // the post-handshake exchange even when the initial handshake allowed no client cert.
            _options.RemoteCertRequired = true;

            SecurityStatusPal status = Interop.OpenSsl.SslRenegotiate((SafeSslHandle)_securityContext, out _);
            if (status.ErrorCode == SecurityStatusPalErrorCode.NoRenegotiation)
            {
                // The peer declined the renegotiation. Leave the session in its completed
                // state; there is nothing for the caller to drive.
                result = TlsOperationStatus.Complete;
                return;
            }

            if (status.ErrorCode != SecurityStatusPalErrorCode.OK)
            {
                throw new AuthenticationException(SR.net_auth_SSPI, status.Exception);
            }

            _isHandshakeComplete = false;
            _externalValidationResolved = false;
            _fdPostHandshakeAuthPending = true;
            _fdPostHandshakeRequestSent = false;
            _fdPostHandshakeWriteBlocked = false;
            result = TlsOperationStatus.Complete;
        }

        partial void TryFastRead(Span<byte> buffer, ref int bytesRead, ref TlsOperationStatus? result)
        {
            if (!_useFdMode)
            {
                return;
            }

            // The buffered Read/Write cores gate on the external-validation state; the
            // fd fast path bypasses them, so apply the same guard here.
            ThrowIfPendingExternalValidation();

            if (buffer.IsEmpty)
            {
                result = TlsOperationStatus.Complete;
                return;
            }

            SafeSslHandle ssl = (SafeSslHandle)_securityContext!;
            int ret = Interop.Ssl.SslRead(ssl, ref MemoryMarshal.GetReference(buffer), buffer.Length, out Interop.Ssl.SslErrorCode err);
            if (ret > 0)
            {
                bytesRead = ret;
                result = TlsOperationStatus.Complete;
                return;
            }
            result = MapSslError(err, ret, SR.net_ssl_decrypt_failed);
        }

        partial void TryFastWrite(ReadOnlySpan<byte> buffer, ref int bytesWritten, ref TlsOperationStatus? result)
        {
            if (!_useFdMode)
            {
                return;
            }

            // The buffered Read/Write cores gate on the external-validation state; the
            // fd fast path bypasses them, so apply the same guard here.
            ThrowIfPendingExternalValidation();

            if (buffer.IsEmpty)
            {
                result = TlsOperationStatus.Complete;
                return;
            }

            SafeSslHandle ssl = (SafeSslHandle)_securityContext!;
            int ret = Interop.Ssl.SslWrite(ssl, ref MemoryMarshal.GetReference(buffer), buffer.Length, out Interop.Ssl.SslErrorCode err);
            if (ret > 0)
            {
                bytesWritten = ret;
                result = TlsOperationStatus.Complete;
                return;
            }
            result = MapSslError(err, ret, SR.net_ssl_encrypt_failed);
        }

        private SafeSslHandle EnsureFdSslHandle()
        {
            if (_securityContext is SafeSslHandle existing && !existing.IsInvalid)
            {
                return existing;
            }
            SafeSslHandle handle = Interop.OpenSsl.AllocateSslHandle(_options);
            _securityContext = handle;
            return handle;
        }

        // Translates a non-progress SslErrorCode into either a status the caller
        // can act on (NeedMoreData / DestinationTooSmall / Closed) or, for a real
        // failure, an AuthenticationException whose inner SslException names the
        // SslErrorCode and carries the most specific diagnostic available as its own
        // inner exception. The template is picked per operation so exceptions surface
        // consistently with SslStream's OpenSSL error handling.
        //
        // 'result' is the raw SSL_* return value; it is required to tell a protocol
        // violating EOF (0) from an I/O error (-1) when the code is SSL_ERROR_SYSCALL.
        private static TlsOperationStatus MapSslError(Interop.Ssl.SslErrorCode error, int result, string sslErrorTemplate)
        {
            return error switch
            {
                Interop.Ssl.SslErrorCode.SSL_ERROR_WANT_READ => TlsOperationStatus.NeedMoreData,
                Interop.Ssl.SslErrorCode.SSL_ERROR_WANT_WRITE => TlsOperationStatus.DestinationTooSmall,
                Interop.Ssl.SslErrorCode.SSL_ERROR_ZERO_RETURN => TlsOperationStatus.Closed,
                _ => throw new AuthenticationException(SR.net_auth_SSPI, CreateDiagnosticSslException(error, result, sslErrorTemplate)),
            };
        }

        private static Interop.OpenSsl.SslException CreateDiagnosticSslException(Interop.Ssl.SslErrorCode error, int result, string sslErrorTemplate)
        {
            Exception? detail = Interop.OpenSsl.GetSslError(result, error);
            Interop.Crypto.ErrClearError();
            return new Interop.OpenSsl.SslException(SR.Format(sslErrorTemplate, error), detail);
        }
    }
}
