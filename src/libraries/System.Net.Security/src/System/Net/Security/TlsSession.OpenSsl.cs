// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using Microsoft.Win32.SafeHandles;

namespace System.Net.Security
{
    public sealed partial class TlsSession
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

        // Socket-replay BIO populated by TryPeekClientHello via BioReadTlsFrame. Holds
        // the ClientHello record buffered off the fd; the same BIO becomes the SSL's
        // read BIO once OnServerContextSet transfers ownership to _options.PreallocatedReadBio.
        // Freed by OnDispose if the session is disposed before that handoff runs.
        private SafeBioHandle? _peekBio;

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
            if (_context.IsServer && (!_hasServerOptions || LocalAppContextSwitches.CaptureClientHello))
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
                // retained peek buffer via BioGetPrefix; the SafeBioHandle stays
                // valid (SSL* is the real owner, our reference DangerousReleases
                // the parent on session Dispose()).
                _options.PreallocatedReadBio = _peekBio;
            }
            else if (_socketInUsed > 0)
            {
                // Legacy managed pre-fetch path: still exercised e.g. by a caller
                // driving ProcessHandshake directly rather than Handshake(). Copy the
                // peeked bytes so SafeSslHandle.Create's BioNewSocketReplay-with-prefix
                // branch can seed the replay BIO.
                byte[] prefix = new byte[_socketInUsed];
                Buffer.BlockCopy(_socketInBuf!, 0, prefix, 0, _socketInUsed);
                _options.ReplayPrefix = prefix;
            }

            _options.SocketHandle = _pendingFdSocket;
            _pendingFdSocket = null;

            // Return the managed pre-fetch buffer if we ever rented one.
            if (_socketInBuf is not null)
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(_socketInBuf);
                _socketInBuf = null;
                _socketInUsed = 0;
            }

            _useFdMode = true;
        }

        // Native ClientHello peek for the deferred socket-bound flow AND the always-capture
        // path (server session with options up front + CaptureClientHello switch on).
        // Creates a socket-replay BIO on the fd, buffers a full TLS record via
        // BioReadTlsFrame, parses via TlsFrameHelper, and populates ClientHelloInfo /
        // TargetHostName from the SNI extension. In the deferred flow returns
        // NeedsServerOptions so the caller resolves via SetServerContext; in the capture
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
                result = TlsOperationStatus.NeedsServerOptions;
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
                int rc = Interop.Ssl.BioReadTlsFrame(_peekBio, out byte* framePtr, out int frameLen);
                if (rc == 0)
                {
                    // Need more bytes off the socket. Caller polls SelectRead and retries.
                    result = TlsOperationStatus.WantRead;
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
                    _options.TargetHost = parsed.Value.ServerName;
                }
            }

            if (!_hasServerOptions)
            {
                // Deferred / SNI-callback flow: caller inspects ClientHelloInfo and
                // resolves via SetServerContext; OnServerContextSet then transfers the
                // peek BIO to _options.PreallocatedReadBio.
                result = TlsOperationStatus.NeedsServerOptions;
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
                if (Interop.Ssl.BioGetPrefix(_peekBio, out byte* ptr, out int len) == 1 && len > 0)
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

            SafeSslHandle ssl = EnsureFdSslHandle();
            int ret = Interop.Ssl.SslDoHandshake(ssl, out Interop.Ssl.SslErrorCode err);
            if (ret == 1)
            {
                OnHandshakeCompleted();
                result = TlsOperationStatus.Complete;
                return;
            }
            result = MapSslError(err, "SSL_do_handshake");
        }

        partial void TryFastRead(Span<byte> buffer, ref int bytesRead, ref TlsOperationStatus? result)
        {
            if (!_useFdMode)
            {
                return;
            }

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
            result = MapSslError(err, "SSL_read");
        }

        partial void TryFastWrite(ReadOnlySpan<byte> buffer, ref int bytesWritten, ref TlsOperationStatus? result)
        {
            if (!_useFdMode)
            {
                return;
            }

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
            result = MapSslError(err, "SSL_write");
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

        private static TlsOperationStatus MapSslError(Interop.Ssl.SslErrorCode error, string op)
        {
            return error switch
            {
                Interop.Ssl.SslErrorCode.SSL_ERROR_WANT_READ => TlsOperationStatus.WantRead,
                Interop.Ssl.SslErrorCode.SSL_ERROR_WANT_WRITE => TlsOperationStatus.WantWrite,
                Interop.Ssl.SslErrorCode.SSL_ERROR_ZERO_RETURN => TlsOperationStatus.Closed,
                _ => throw new AuthenticationException($"OpenSSL {op} failed: {error}"),
            };
        }
    }
}
