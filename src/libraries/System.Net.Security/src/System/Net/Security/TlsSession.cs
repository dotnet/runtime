// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Security
{
    /// <summary>
    /// Non-blocking TLS state machine wrapper around the existing
    /// <see cref="SslStreamPal"/>. PoC scope: detached mode only (caller owns I/O),
    /// Linux-only (OpenSSL). Provides <see cref="ProcessHandshake"/>,
    /// <see cref="Encrypt"/>, <see cref="Decrypt"/>, and a pending-output queue.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The session never performs any I/O. The caller drives ciphertext in/out
    /// via byte spans. Any ciphertext the TLS layer needs to send (handshake
    /// records, alerts, encrypted application data) is staged in an internal
    /// pending-output buffer and drained via <see cref="DrainPendingOutput"/>.
    /// </para>
    /// <para>
    /// Contract: any operation may return <see cref="TlsOperationStatus.WantWrite"/>
    /// to indicate the caller must drain pending output before further progress
    /// is possible. The session does not consume new input while pending output
    /// is non-empty.
    /// </para>
    /// </remarks>
    public sealed class TlsSession : IDisposable
    {
        // Matches StreamSizes.Default on Unix; conservative upper bound for a
        // single TLS record's plaintext payload.
        internal const int MaxRecordPlaintext = 16354;

        private readonly TlsContext _context;
        private SafeFreeCredentials? _credentialsHandle;
        private SafeDeleteSslContext? _securityContext;

        private byte[]? _pending;
        private int _pendingOffset;
        private int _pendingLength;

        private byte[]? _decryptScratch;

        private bool _isHandshakeComplete;
        private bool _disposed;
        private SslConnectionInfo _connectionInfo;
        private X509Certificate2? _remoteCertificate;

        private TlsSession(TlsContext context)
        {
            _context = context;
        }

        public static TlsSession Create(TlsContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (!OperatingSystem.IsLinux() && !OperatingSystem.IsFreeBSD())
            {
                throw new PlatformNotSupportedException(
                    "TlsSession is currently implemented only on Linux/FreeBSD (OpenSSL).");
            }

            TlsSession session = new TlsSession(context);

            // Provide a default cert validation hook so OpenSSL's CertVerifyCallback
            // can drive the user RemoteCertificateValidationCallback even for a
            // standalone TlsSession. If SslStream wraps this session (wedge mode),
            // it sets its own validator first and we leave it untouched.
            context.Options.RemoteCertificateValidator ??= session.VerifyRemoteCertificate;

            return session;
        }

        // ── State ─────────────────────────────────────────────────────────

        public bool IsServer => _context.IsServer;

        public bool IsHandshakeComplete => _isHandshakeComplete;

        public bool HasPendingOutput => _pendingLength > 0;

        public string? TargetHostName
        {
            get => _context.Options.TargetHost;
            set => _context.Options.TargetHost = value ?? string.Empty;
        }

        public SslProtocols NegotiatedProtocol =>
            _isHandshakeComplete ? (SslProtocols)_connectionInfo.Protocol : SslProtocols.None;

        [System.CLSCompliant(false)]
        public TlsCipherSuite NegotiatedCipherSuite =>
            _isHandshakeComplete ? (TlsCipherSuite)_connectionInfo.TlsCipherSuite : default;

        public SslApplicationProtocol NegotiatedApplicationProtocol
        {
            get
            {
                if (!_isHandshakeComplete || _connectionInfo.ApplicationProtocol == null)
                {
                    return default;
                }
                return new SslApplicationProtocol(_connectionInfo.ApplicationProtocol);
            }
        }

        public X509Certificate2? GetRemoteCertificate()
        {
            if (_securityContext == null || _securityContext.IsInvalid)
            {
                return null;
            }
            return CertificateValidationPal.GetRemoteCertificate(_securityContext);
        }

        /// <summary>
        /// Returns the local certificate sent to the peer, or <c>null</c> if no
        /// local certificate was negotiated. For a server session this is the
        /// server certificate; for a client session this is the client
        /// certificate selected during handshake (which may be <c>null</c> if
        /// the server did not request a client certificate or the client did
        /// not supply one).
        /// </summary>
        public X509Certificate2? LocalCertificate
        {
            get
            {
                ThrowIfDisposed();
                if (_context.IsServer)
                {
                    return _context.Options.CertificateContext?.TargetCertificate;
                }

                if (_securityContext == null || _securityContext.IsInvalid)
                {
                    return null;
                }

                if (!CertificateValidationPal.IsLocalCertificateUsed(_credentialsHandle, _securityContext))
                {
                    return null;
                }

                return _context.Options.CertificateContext?.TargetCertificate;
            }
        }

        /// <summary>
        /// Returns a <see cref="ChannelBinding"/> for the requested
        /// <paramref name="kind"/> derived from the current TLS session, or
        /// <c>null</c> if the binding is unavailable (e.g. handshake not yet
        /// complete, or unsupported binding kind).
        /// </summary>
        public ChannelBinding? GetChannelBinding(ChannelBindingKind kind)
        {
            ThrowIfDisposed();
            if (_securityContext == null || _securityContext.IsInvalid)
            {
                return null;
            }
            return SslStreamPal.QueryContextChannelBinding(_securityContext, kind);
        }

        // ── Handshake ─────────────────────────────────────────────────────

        public TlsOperationStatus ProcessHandshake(
            ReadOnlySpan<byte> input,
            Span<byte> output,
            out int consumed,
            out int produced)
        {
            ThrowIfDisposed();
            consumed = 0;
            produced = 0;

            if (_isHandshakeComplete)
            {
                throw new InvalidOperationException("Handshake has already completed.");
            }

            // Drain pending first; do not consume new input while output is owed.
            if (_pendingLength > 0)
            {
                produced = DrainTo(output);
                return _pendingLength > 0 ? TlsOperationStatus.WantWrite : TlsOperationStatus.Complete;
            }

            ProtocolToken token = default;
            token.RentBuffer = true;
            try
            {
                if (_context.IsServer)
                {
                    // On the very first server-side call, inspect the incoming
                    // ClientHello to surface SNI (TargetHost) and, if the caller
                    // supplied a ServerCertificateSelectionCallback, resolve the
                    // server certificate from it before AllocateSslHandle runs.
                    if (_securityContext is null)
                    {
                        bool needsCertResolution =
                            _context.Options.CertificateContext is null &&
                            _context.Options.ServerCertSelectionDelegate is not null;

                        if (input.Length == 0)
                        {
                            // Defer first PAL call until we have data: empty input would
                            // otherwise reach AllocateSslHandle and assert when cert
                            // resolution still owes a CertificateContext.
                            return TlsOperationStatus.WantRead;
                        }

                        if (needsCertResolution && !ResolveServerCertificateFromClientHello(input))
                        {
                            // Need more bytes to parse the ClientHello (and run the
                            // ServerCertificateSelectionCallback).
                            return TlsOperationStatus.WantRead;
                        }
                    }

                    token = SslStreamPal.AcceptSecurityContext(
                        ref _credentialsHandle,
                        ref _securityContext,
                        input,
                        out consumed,
                        _context.Options);
                }
                else
                {
                    string hostName = TargetHostNameHelper.NormalizeHostName(_context.Options.TargetHost);
                    token = SslStreamPal.InitializeSecurityContext(
                        ref _credentialsHandle,
                        ref _securityContext,
                        hostName,
                        input,
                        out consumed,
                        _context.Options);
                }

                // Stage any handshake bytes the PAL produced.
                if (token.Size > 0)
                {
                    Debug.Assert(token.Payload != null);
                    AppendPending(new ReadOnlySpan<byte>(token.Payload, 0, token.Size));
                }

                if (token.Failed)
                {
                    throw new AuthenticationException(SR.net_auth_SSPI, token.GetException());
                }

                bool done = token.Status.ErrorCode == SecurityStatusPalErrorCode.OK;
                bool needsCredentials = token.Status.ErrorCode == SecurityStatusPalErrorCode.CredentialsNeeded;

                if (done)
                {
                    _isHandshakeComplete = true;
                    SslStreamPal.QueryContextConnectionInfo(_securityContext!, ref _connectionInfo);
                }

                if (_pendingLength > 0)
                {
                    produced = DrainTo(output);
                    if (_pendingLength > 0)
                    {
                        return TlsOperationStatus.WantWrite;
                    }
                }

                if (done)
                {
                    return TlsOperationStatus.Complete;
                }

                if (needsCredentials)
                {
                    return TlsOperationStatus.WantCredentials;
                }

                // We made progress but still need more peer data.
                return TlsOperationStatus.WantRead;
            }
            finally
            {
                token.ReleasePayload();
            }
        }

        // ── Encrypt ───────────────────────────────────────────────────────

        public TlsOperationStatus Encrypt(
            ReadOnlySpan<byte> plaintext,
            Span<byte> ciphertext,
            out int consumed,
            out int produced)
        {
            ThrowIfDisposed();
            consumed = 0;
            produced = 0;

            if (!_isHandshakeComplete)
            {
                throw new InvalidOperationException("Handshake has not yet completed.");
            }

            if (_pendingLength > 0)
            {
                produced = DrainTo(ciphertext);
                return _pendingLength > 0 ? TlsOperationStatus.WantWrite : TlsOperationStatus.Complete;
            }

            if (plaintext.IsEmpty)
            {
                return TlsOperationStatus.Complete;
            }

            int chunk = Math.Min(plaintext.Length, MaxRecordPlaintext);
            byte[] rented = ArrayPool<byte>.Shared.Rent(chunk);
            try
            {
                plaintext.Slice(0, chunk).CopyTo(rented);

                ProtocolToken token = SslStreamPal.EncryptMessage(
                    _securityContext!,
                    new ReadOnlyMemory<byte>(rented, 0, chunk),
                    0,
                    0);

                try
                {
                    if (token.Status.ErrorCode != SecurityStatusPalErrorCode.OK)
                    {
                        throw new IOException(SR.net_io_encrypt, SslStreamPal.GetException(token.Status));
                    }

                    consumed = chunk;

                    if (token.Size > 0)
                    {
                        Debug.Assert(token.Payload != null);
                        AppendPending(new ReadOnlySpan<byte>(token.Payload, 0, token.Size));
                    }
                }
                finally
                {
                    token.ReleasePayload();
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented);
            }

            produced = DrainTo(ciphertext);
            return _pendingLength > 0 ? TlsOperationStatus.WantWrite : TlsOperationStatus.Complete;
        }

        // ── Decrypt ───────────────────────────────────────────────────────

        public TlsOperationStatus Decrypt(
            ReadOnlySpan<byte> ciphertext,
            Span<byte> plaintext,
            out int consumed,
            out int produced)
        {
            ThrowIfDisposed();
            consumed = 0;
            produced = 0;

            if (!_isHandshakeComplete)
            {
                throw new InvalidOperationException("Handshake has not yet completed.");
            }

            if (_pendingLength > 0)
            {
                // Caller must drain before we accept new input.
                return TlsOperationStatus.WantWrite;
            }

            // Need at least a frame header.
            if (ciphertext.Length < TlsFrameHelper.HeaderSize)
            {
                return TlsOperationStatus.WantRead;
            }

            TlsFrameHeader header = default;
            if (!TlsFrameHelper.TryGetFrameHeader(ciphertext, ref header))
            {
                throw new IOException(SR.net_io_decrypt);
            }

            int frameSize = header.Length;
            if (ciphertext.Length < frameSize)
            {
                return TlsOperationStatus.WantRead;
            }

            // PAL decrypts in place; copy into a writable scratch buffer.
            EnsureDecryptScratch(frameSize);
            ciphertext.Slice(0, frameSize).CopyTo(_decryptScratch);

            SecurityStatusPal status = SslStreamPal.DecryptMessage(
                _securityContext!,
                _decryptScratch.AsSpan(0, frameSize),
                out int outOffset,
                out int outCount);

            switch (status.ErrorCode)
            {
                case SecurityStatusPalErrorCode.OK:
                    consumed = frameSize;
                    if (outCount > plaintext.Length)
                    {
                        throw new InvalidOperationException(
                            $"Plaintext buffer too small: needed {outCount}, got {plaintext.Length}.");
                    }
                    _decryptScratch.AsSpan(outOffset, outCount).CopyTo(plaintext);
                    produced = outCount;
                    return TlsOperationStatus.Complete;

                case SecurityStatusPalErrorCode.ContextExpired:
                    consumed = frameSize;
                    return TlsOperationStatus.Closed;

                case SecurityStatusPalErrorCode.Renegotiate:
                    // OpenSSL handles renegotiation transparently inside SSL_read/SSL_write.
                    // The PAL has already consumed the frame; surface no plaintext and ask
                    // the caller for more input. Any handshake bytes OpenSSL needs to send
                    // out will surface on the next Encrypt/Decrypt call.
                    consumed = frameSize;
                    return TlsOperationStatus.WantRead;

                default:
                    throw new IOException(SR.net_io_decrypt, SslStreamPal.GetException(status));
            }
        }

        // ── Renegotiation / Post-handshake auth ──────────────────────────

        /// <summary>
        /// Server-side: initiates a TLS renegotiation. On TLS 1.2 this issues
        /// a HelloRequest; on TLS 1.3 this issues a post-handshake
        /// CertificateRequest (same primitive as <see cref="RequestClientCertificate"/>,
        /// because OpenSSL exposes only the combined operation).
        /// </summary>
        /// <remarks>
        /// The generated handshake bytes are staged into the pending-output
        /// buffer (drained into <paramref name="ciphertext"/>). The caller must
        /// then continue normal <see cref="Decrypt"/> / <see cref="Encrypt"/>
        /// operations; OpenSSL processes the peer's response transparently
        /// inside subsequent <c>SSL_read</c> calls.
        /// </remarks>
        public TlsOperationStatus RequestRenegotiation(Span<byte> ciphertext, out int produced)
            => RequestClientCertificate(ciphertext, out produced);

        /// <summary>
        /// Server-side: requests a client certificate from the peer after the
        /// initial handshake has completed. On TLS 1.3 this issues a
        /// post-handshake authentication CertificateRequest; on TLS 1.2 it
        /// initiates a renegotiation.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The generated handshake bytes are staged into the pending-output
        /// buffer (drained into <paramref name="ciphertext"/>). The caller
        /// must then continue normal <see cref="Decrypt"/> / <see cref="Encrypt"/>
        /// operations; OpenSSL processes the peer's response transparently
        /// inside subsequent <c>SSL_read</c> calls. Once the peer's
        /// certificate has been received, it becomes observable via
        /// <see cref="GetRemoteCertificate"/>.
        /// </para>
        /// </remarks>
        public TlsOperationStatus RequestClientCertificate(Span<byte> ciphertext, out int produced)
        {
            ThrowIfDisposed();
            produced = 0;

            if (!_context.IsServer)
            {
                throw new InvalidOperationException("RequestClientCertificate can only be invoked on a server session.");
            }

            if (!_isHandshakeComplete || _securityContext == null || _securityContext.IsInvalid)
            {
                throw new InvalidOperationException("Handshake has not yet completed.");
            }

            if (_pendingLength == 0)
            {
                ProtocolToken token = SslStreamPal.Renegotiate(
                    ref _credentialsHandle,
                    ref _securityContext!,
                    _context.Options);
                try
                {
                    if (token.Failed)
                    {
                        throw new AuthenticationException(SR.net_auth_SSPI, token.GetException());
                    }

                    if (token.Size > 0)
                    {
                        Debug.Assert(token.Payload != null);
                        AppendPending(new ReadOnlySpan<byte>(token.Payload, 0, token.Size));
                    }
                }
                finally
                {
                    token.ReleasePayload();
                }
            }

            produced = DrainTo(ciphertext);
            return _pendingLength > 0 ? TlsOperationStatus.WantWrite : TlsOperationStatus.Complete;
        }

        // ── Shutdown ──────────────────────────────────────────────────────

        private bool _shutdownSent;

        /// <summary>
        /// Initiates a TLS close_notify shutdown and stages the resulting alert
        /// record into the pending-output buffer (drained into <paramref name="ciphertext"/>).
        /// Subsequent calls drain any remaining shutdown output.
        /// </summary>
        /// <remarks>
        /// Returns <see cref="TlsOperationStatus.WantWrite"/> if the caller must
        /// drain more output before the shutdown record is fully written;
        /// otherwise <see cref="TlsOperationStatus.Closed"/> once all bytes have
        /// been handed to the caller.
        /// </remarks>
        public TlsOperationStatus Shutdown(Span<byte> ciphertext, out int produced)
        {
            ThrowIfDisposed();
            produced = 0;

            if (_securityContext == null || _securityContext.IsInvalid)
            {
                return TlsOperationStatus.Closed;
            }

            if (!_shutdownSent)
            {
                _shutdownSent = true;

                SecurityStatusPal status = SslStreamPal.ApplyShutdownToken(_securityContext);
                if (status.ErrorCode != SecurityStatusPalErrorCode.OK)
                {
                    throw new IOException(SR.net_io_encrypt, SslStreamPal.GetException(status));
                }

                // Drive one step to extract the close_notify bytes the PAL queued
                // into the underlying BIO. Input is empty; we only care about
                // any output the PAL produces.
                ProtocolToken token = default;
                token.RentBuffer = true;
                try
                {
                    if (_context.IsServer)
                    {
                        token = SslStreamPal.AcceptSecurityContext(
                            ref _credentialsHandle,
                            ref _securityContext,
                            ReadOnlySpan<byte>.Empty,
                            out _,
                            _context.Options);
                    }
                    else
                    {
                        string hostName = TargetHostNameHelper.NormalizeHostName(_context.Options.TargetHost);
                        token = SslStreamPal.InitializeSecurityContext(
                            ref _credentialsHandle,
                            ref _securityContext,
                            hostName,
                            ReadOnlySpan<byte>.Empty,
                            out _,
                            _context.Options);
                    }

                    if (token.Size > 0)
                    {
                        Debug.Assert(token.Payload != null);
                        AppendPending(new ReadOnlySpan<byte>(token.Payload, 0, token.Size));
                    }
                }
                finally
                {
                    token.ReleasePayload();
                }
            }

            produced = DrainTo(ciphertext);
            return _pendingLength > 0 ? TlsOperationStatus.WantWrite : TlsOperationStatus.Closed;
        }

        // ── Pending output ────────────────────────────────────────────────

        public TlsOperationStatus DrainPendingOutput(Span<byte> ciphertext, out int produced)
        {
            ThrowIfDisposed();
            produced = DrainTo(ciphertext);
            return _pendingLength > 0 ? TlsOperationStatus.WantWrite : TlsOperationStatus.Complete;
        }

        // ── Internals ─────────────────────────────────────────────────────

        private void AppendPending(ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty)
            {
                return;
            }

            // Compact if anything was already drained.
            if (_pending != null && _pendingOffset > 0)
            {
                if (_pendingLength > 0)
                {
                    Buffer.BlockCopy(_pending, _pendingOffset, _pending, 0, _pendingLength);
                }
                _pendingOffset = 0;
            }

            int needed = _pendingLength + data.Length;
            if (_pending == null || _pending.Length < needed)
            {
                byte[] bigger = ArrayPool<byte>.Shared.Rent(Math.Max(needed, 4096));
                if (_pending is byte[] old)
                {
                    if (_pendingLength > 0)
                    {
                        Buffer.BlockCopy(old, 0, bigger, 0, _pendingLength);
                    }
                    ArrayPool<byte>.Shared.Return(old);
                }
                _pending = bigger;
            }

            data.CopyTo(_pending.AsSpan(_pendingLength));
            _pendingLength += data.Length;
        }

        private int DrainTo(Span<byte> output)
        {
            if (_pendingLength == 0)
            {
                return 0;
            }

            int n = Math.Min(output.Length, _pendingLength);
            _pending!.AsSpan(_pendingOffset, n).CopyTo(output);
            _pendingOffset += n;
            _pendingLength -= n;

            if (_pendingLength == 0)
            {
                ArrayPool<byte>.Shared.Return(_pending!);
                _pending = null;
                _pendingOffset = 0;
            }

            return n;
        }

        private void EnsureDecryptScratch(int size)
        {
            if (_decryptScratch == null || _decryptScratch.Length < size)
            {
                if (_decryptScratch != null)
                {
                    ArrayPool<byte>.Shared.Return(_decryptScratch);
                }
                _decryptScratch = ArrayPool<byte>.Shared.Rent(size);
            }
        }

        private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

        // Server-side SNI + certificate selection. Parses the ClientHello to
        // extract the server_name extension (SNI) and, if a
        // ServerCertificateSelectionCallback was supplied and no static
        // CertificateContext has been resolved yet, invokes the callback to
        // pick the cert. Mirrors the path SslStream takes in
        // ReceiveBlobAsync/AcquireServerCredentials.
        private bool ResolveServerCertificateFromClientHello(ReadOnlySpan<byte> input)
        {
            TlsFrameHelper.TlsFrameInfo frameInfo = default;
            if (!TlsFrameHelper.TryGetFrameInfo(input, ref frameInfo))
            {
                return false;
            }

            if (frameInfo.HandshakeType != TlsHandshakeType.ClientHello)
            {
                return true;
            }

            if (!string.IsNullOrEmpty(frameInfo.TargetName))
            {
                _context.Options.TargetHost = frameInfo.TargetName;
            }

            ServerCertificateSelectionCallback? selector = _context.Options.ServerCertSelectionDelegate;
            if (selector is null || _context.Options.CertificateContext is not null)
            {
                return true;
            }

            X509Certificate? selected = selector(this, _context.Options.TargetHost);
            if (selected is null)
            {
                throw new AuthenticationException(SR.net_ssl_io_no_server_cert);
            }

            X509Certificate2? withKey = SslStream.FindCertificateWithPrivateKey(this, isServer: true, selected);
            if (withKey is null)
            {
                throw new AuthenticationException(SR.net_ssl_io_no_server_cert);
            }

            _context.Options.SetCertificateContextFromCert(withKey);
            return true;
        }

        // ── Internal surface for the SslStream wedge (Linux/FreeBSD only) ─

        // Direct accessors used by SslStream to mirror state into its own fields after
        // each handshake step. Both handles are owned by this TlsSession; SslStream
        // observes them via the mirror but does not dispose them.
        internal SafeDeleteSslContext? SecurityContext => _securityContext;
        internal SafeFreeCredentials? CredentialsHandle => _credentialsHandle;

        // SslStream's GenerateToken replacement. Drives one ASC/ISC step via PAL and
        // updates internal handshake-complete state. Returns the raw PAL token so the
        // caller can preserve existing ProtocolToken-based plumbing (alerts, error
        // mapping, NetEventSource).
        internal ProtocolToken HandshakeStepForSslStream(ReadOnlySpan<byte> input, out int consumed)
        {
            ThrowIfDisposed();

            ProtocolToken token;
            if (_context.IsServer)
            {
                token = SslStreamPal.AcceptSecurityContext(
                    ref _credentialsHandle,
                    ref _securityContext,
                    input,
                    out consumed,
                    _context.Options);
            }
            else
            {
                string hostName = TargetHostNameHelper.NormalizeHostName(_context.Options.TargetHost);
                token = SslStreamPal.InitializeSecurityContext(
                    ref _credentialsHandle,
                    ref _securityContext,
                    hostName,
                    input,
                    out consumed,
                    _context.Options);
            }

            if (token.Status.ErrorCode == SecurityStatusPalErrorCode.OK)
            {
                _isHandshakeComplete = true;
                SslStreamPal.QueryContextConnectionInfo(_securityContext!, ref _connectionInfo);
            }

            return token;
        }

        // Invoked by OpenSSL's CertVerifyCallback (via SslAuthenticationOptions.RemoteCertificateValidator)
        // when this TlsSession owns the validation flow.
        internal bool VerifyRemoteCertificate(
            X509Certificate2? certificate,
            X509Chain? chain,
            SslCertificateTrust? trust,
            ref ProtocolToken alertToken,
            out SslPolicyErrors sslPolicyErrors,
            out X509ChainStatusFlags chainStatus)
        {
            return SslStream.VerifyRemoteCertificateCore(
                this,
                _context.Options,
                _securityContext,
                ref _remoteCertificate,
                ref _connectionInfo,
                certificate,
                chain,
                trust,
                ref alertToken,
                out sslPolicyErrors,
                out chainStatus);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;

            _securityContext?.Dispose();
            _securityContext = null;
            _credentialsHandle?.Dispose();
            _credentialsHandle = null;

            if (_pending != null)
            {
                ArrayPool<byte>.Shared.Return(_pending);
                _pending = null;
            }
            if (_decryptScratch != null)
            {
                ArrayPool<byte>.Shared.Return(_decryptScratch);
                _decryptScratch = null;
            }
        }
    }
}
