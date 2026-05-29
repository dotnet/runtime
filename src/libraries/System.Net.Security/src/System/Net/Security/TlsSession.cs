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
    /// <see cref="SslStreamPal"/>. PoC scope: detached mode only (caller owns I/O).
    /// Supported on Linux/FreeBSD (OpenSSL) and Windows (SChannel). Provides
    /// <see cref="ProcessHandshake"/>, <see cref="Encrypt"/>, <see cref="Decrypt"/>,
    /// and a pending-output queue.
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
        private SafeDeleteSslContext? _securityContext;

        private byte[]? _pending;
        private int _pendingOffset;
        private int _pendingLength;

        private byte[]? _decryptScratch;

        private bool _isHandshakeComplete;
        private bool _suppressInternalCertificateValidation;
        private bool _externalValidationPending;
        private bool _externalValidationResolved;
        private X509Chain? _externalValidationChain;
        private X509Certificate2? _externalPendingCert;
        private Exception? _externalValidationFault;
        private SslClientHelloInfo? _clientHelloInfo;
        private bool _disposed;
        private SslConnectionInfo _connectionInfo;
        private X509Certificate2? _remoteCertificate;
        private int _headerSize;
        private int _trailerSize;
        private int _maxDataSize = MaxRecordPlaintext;

        private TlsSession(TlsContext context)
        {
            _context = context;
        }

        public static TlsSession Create(TlsContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            TlsSession session = new TlsSession(context);

#if !TARGET_WINDOWS && !SYSNETSECURITY_NO_OPENSSL
            // OpenSSL's CertVerifyCallback must answer synchronously, but a TlsSession
            // always defers peer-cert validation to its caller. On OpenSSL 3.0+ the
            // callback uses SSL_set_retry_verify to pause the handshake; on 1.1.x it
            // accepts the cert and validation runs after the handshake completes.
            // The SslStream wedge sets its own validator on the shared options bag
            // before calling Create; the callback gives precedence to a non-null
            // RemoteCertificateValidator, so the wedge path is unaffected.
            context.Options.DeferCertificateValidation = true;
#endif

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

        public SslProtocols NegotiatedProtocol
        {
            get
            {
                if (!_isHandshakeComplete || _connectionInfo.Protocol == 0)
                {
                    return SslProtocols.None;
                }

                // On Windows (SChannel), the reported protocol value carries
                // client/server direction bits (SP_PROT_TLS1_2_CLIENT == 0x800,
                // SP_PROT_TLS1_2_SERVER == 0x400, etc.). Canonicalize to the
                // managed SslProtocols enum values, matching SslStream.
                SslProtocols proto = (SslProtocols)_connectionInfo.Protocol;
                SslProtocols ret = SslProtocols.None;
#pragma warning disable 0618
                if ((proto & SslProtocols.Ssl2) != 0) ret |= SslProtocols.Ssl2;
                if ((proto & SslProtocols.Ssl3) != 0) ret |= SslProtocols.Ssl3;
#pragma warning restore
#pragma warning disable SYSLIB0039
                if ((proto & SslProtocols.Tls) != 0) ret |= SslProtocols.Tls;
                if ((proto & SslProtocols.Tls11) != 0) ret |= SslProtocols.Tls11;
#pragma warning restore SYSLIB0039
                if ((proto & SslProtocols.Tls12) != 0) ret |= SslProtocols.Tls12;
                if ((proto & SslProtocols.Tls13) != 0) ret |= SslProtocols.Tls13;
                return ret;
            }
        }

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
            if (_remoteCertificate is not null)
            {
                return _remoteCertificate;
            }

            if (_externalPendingCert is not null)
            {
                return _externalPendingCert;
            }

            if (_securityContext == null || _securityContext.IsInvalid)
            {
                return null;
            }
            return CertificateValidationPal.GetRemoteCertificate(_securityContext);
        }

        /// <summary>
        /// Returns the <see cref="X509Chain"/> the platform built for the peer
        /// certificate during handshake, or <c>null</c> if no chain was retained.
        /// Only meaningful while the session is awaiting an external validation result
        /// (after <see cref="ProcessHandshake"/> returned
        /// <see cref="TlsOperationStatus.NeedsCertificateValidation"/>). The chain is
        /// owned by the session and disposed when the session is disposed or when the
        /// validation result is recorded.
        /// </summary>
        public X509Chain? GetRemoteCertificateChain()
        {
            ThrowIfDisposed();
            return _externalValidationChain;
        }

        /// <summary>
        /// Runs the same validation <see cref="SslStream"/> performs (default chain
        /// build plus any user-supplied <see cref="SslClientAuthenticationOptions.RemoteCertificateValidationCallback"/>
        /// on the underlying options), records the result on the session, and returns
        /// the effective <see cref="SslPolicyErrors"/>. Intended for callers that want
        /// <see cref="SslStream"/>-compatible semantics without writing their own
        /// validation logic.
        /// </summary>
        /// <remarks>
        /// Must be called only after <see cref="ProcessHandshake"/> returned
        /// <see cref="TlsOperationStatus.NeedsCertificateValidation"/> and before
        /// <see cref="SetRemoteCertificateValidationResult"/> is called.
        /// </remarks>
        public SslPolicyErrors AcceptWithDefaultValidation()
        {
            ThrowIfDisposed();
            if (!_externalValidationPending)
            {
                throw new InvalidOperationException(
                    "AcceptWithDefaultValidation can only be called after ProcessHandshake returned NeedsCertificateValidation.");
            }

            ProtocolToken alertToken = default;
            // Pass _externalPendingCert as the candidate cert and an empty _remoteCertificate slot.
            // VerifyRemoteCertificateCore assigns the slot to the candidate on success; the renegotiation
            // shortcut at the top of that method would otherwise dispose our cert if the slot were already
            // populated with the same instance.
            bool ok = SslStream.VerifyRemoteCertificateCore(
                this,
                _context.Options,
                _securityContext,
                ref _remoteCertificate,
                ref _connectionInfo,
                _externalPendingCert,
                _externalValidationChain,
                trust: null,
                ref alertToken,
                out SslPolicyErrors sslPolicyErrors,
                out _);

            // On success VerifyRemoteCertificateCore set _remoteCertificate = _externalPendingCert, so
            // SetRemoteCertificateValidationResult below leaves it alone. On failure we must dispose the
            // pending cert ourselves because no one adopted it.
            SetRemoteCertificateValidationResult(ok ? SslPolicyErrors.None : sslPolicyErrors);
            return sslPolicyErrors;
        }

        /// <summary>
        /// Records the caller's external certificate-validation result.
        /// <see cref="SslPolicyErrors.None"/> means accept; any other value causes
        /// subsequent calls to <see cref="ProcessHandshake"/>, <see cref="Encrypt"/>,
        /// and <see cref="Decrypt"/> to throw <see cref="AuthenticationException"/>.
        /// Must be called exactly once between
        /// <see cref="TlsOperationStatus.NeedsCertificateValidation"/> and the next
        /// session operation.
        /// </summary>
        public void SetRemoteCertificateValidationResult(SslPolicyErrors errors)
        {
            ThrowIfDisposed();
            if (!_externalValidationPending)
            {
                throw new InvalidOperationException(
                    "SetRemoteCertificateValidationResult can only be called after ProcessHandshake returned NeedsCertificateValidation.");
            }

            _externalValidationPending = false;
            _externalValidationResolved = true;

            if (errors == SslPolicyErrors.None)
            {
                // Caller accepted. Promote the pending cert to the canonical remote-cert slot
                // (unless AcceptWithDefaultValidation already did so).
                if (_remoteCertificate is null)
                {
                    _remoteCertificate = _externalPendingCert;
                    _externalPendingCert = null;
                }
                else
                {
                    // VerifyRemoteCertificateCore adopted the cert into _remoteCertificate. Drop our copy.
                    _externalPendingCert = null;
                }
            }
            else
            {
                _externalValidationFault = new AuthenticationException(SR.net_ssl_io_cert_validation);
                _externalPendingCert?.Dispose();
                _externalPendingCert = null;
            }

            DisposeExternalValidationChain();
        }

        /// <summary>
        /// Server-side only. The parsed ClientHello information, populated when
        /// <see cref="ProcessHandshake"/> returns
        /// <see cref="TlsOperationStatus.NeedsServerOptions"/> after the context was
        /// created with null server options. Returns <see langword="null"/> at all
        /// other times. The value is cleared after <see cref="SetServerOptions"/> is
        /// called.
        /// </summary>
        public SslClientHelloInfo? ClientHelloInfo
        {
            get
            {
                ThrowIfDisposed();
                return _clientHelloInfo;
            }
        }

        /// <summary>
        /// Server-side only. Supplies the resolved server options when the session is
        /// suspended on <see cref="TlsOperationStatus.NeedsServerOptions"/>. The next
        /// <see cref="ProcessHandshake"/> call should be invoked with the same input
        /// buffer (the ClientHello bytes the session returned uncomsumed).
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the session is not currently awaiting server options, or if
        /// server options were already supplied at <see cref="TlsContext"/> creation.
        /// </exception>
        public void SetServerOptions(SslServerAuthenticationOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            ThrowIfDisposed();

            if (!_context.IsServer)
            {
                throw new InvalidOperationException("SetServerOptions can only be called on a server-side session.");
            }
            if (_context.HasServerOptions)
            {
                throw new InvalidOperationException("Server options were already supplied when the TlsContext was created.");
            }
            if (_clientHelloInfo is null)
            {
                throw new InvalidOperationException("SetServerOptions can only be called after ProcessHandshake returned NeedsServerOptions.");
            }

            _context.ApplyServerOptions(options);
#if !TARGET_WINDOWS && !SYSNETSECURITY_NO_OPENSSL
            // Preserve the retry-verify suspension semantics that TlsSession.Create
            // would have configured up front had server options been available then.
            _context.Options.DeferCertificateValidation = true;
#endif
            _clientHelloInfo = null;
        }

        private void ThrowIfPendingExternalValidation()
        {
            if (_externalValidationFault is not null)
            {
                throw _externalValidationFault;
            }
            if (_externalValidationPending)
            {
                throw new InvalidOperationException(
                    "External certificate validation result has not been recorded. Call SetRemoteCertificateValidationResult or AcceptWithDefaultValidation first.");
            }
        }

        private void DisposeExternalValidationChain()
        {
            X509Chain? chain = _externalValidationChain;
            _externalValidationChain = null;
            // Match the inline-validation cleanup in OnHandshakeCompleted: dispose the chain context only,
            // not individual element certs. The leaf is owned by _remoteCertificate or already disposed via
            // _externalPendingCert; intermediate elements are platform-built refs we don't own.
            chain?.Dispose();
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

                if (!CertificateValidationPal.IsLocalCertificateUsed(_context.CredentialsHandle, _securityContext))
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

            if (_externalValidationFault is not null)
            {
                throw _externalValidationFault;
            }

            if (_externalValidationPending)
            {
                return TlsOperationStatus.NeedsCertificateValidation;
            }

            if (_clientHelloInfo is not null)
            {
                // The caller previously saw NeedsServerOptions but hasn't supplied options yet.
                return TlsOperationStatus.NeedsServerOptions;
            }

            if (_isHandshakeComplete)
            {
                // Once the caller has resolved external validation, subsequent
                // ProcessHandshake calls on an already-complete session are a
                // no-op signal that the handshake is done (one-call window).
                if (_externalValidationResolved)
                {
                    return TlsOperationStatus.Complete;
                }

                throw new InvalidOperationException("Handshake has already completed.");
            }

            // Drain pending first; do not consume new input while output is owed.
            if (_pendingLength > 0)
            {
                produced = DrainTo(output);
                return _pendingLength > 0 ? TlsOperationStatus.WantWrite : TlsOperationStatus.Complete;
            }

            // The PAL state machine — SChannel in particular — must only be handed
            // complete TLS records. SChannel's PAL wrapper reports consumed=input.Length
            // when it returns SEC_E_INCOMPLETE_MESSAGE, which would silently swallow
            // bytes it actually still needs. OpenSSL's BIO accepts partial bytes, but
            // pre-checking the frame here costs nothing extra and keeps the state
            // machine identical across platforms.
            //
            // The only call that legitimately runs with empty input is the very first
            // client-side ISC, which produces the ClientHello.
            bool isInitialClientCall = !_context.IsServer && _securityContext is null;
            if (!isInitialClientCall)
            {
                if (input.Length < TlsFrameHelper.HeaderSize)
                {
                    return TlsOperationStatus.WantRead;
                }

                TlsFrameHeader frameHeader = default;
                if (!TlsFrameHelper.TryGetFrameHeader(input, ref frameHeader))
                {
                    throw new IOException(SR.net_io_decrypt);
                }

                if (input.Length < frameHeader.Length)
                {
                    return TlsOperationStatus.WantRead;
                }
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
                        // Deferred options: parse ClientHello and suspend so the caller can
                        // supply server options via SetServerOptions. Leave input unconsumed
                        // (consumed = 0); the caller re-feeds the same bytes after resuming.
                        if (!_context.HasServerOptions)
                        {
                            SslClientHelloInfo? parsed = TryParseClientHello(input);
                            if (parsed is null)
                            {
                                return TlsOperationStatus.WantRead;
                            }

                            _clientHelloInfo = parsed;
                            return TlsOperationStatus.NeedsServerOptions;
                        }

                        bool needsCertResolution =
                            _context.Options.CertificateContext is null &&
                            _context.Options.ServerCertSelectionDelegate is not null;

                        if (needsCertResolution && !ResolveServerCertificateFromClientHello(input))
                        {
                            // Need more bytes to parse the ClientHello (and run the
                            // ServerCertificateSelectionCallback).
                            return TlsOperationStatus.WantRead;
                        }
                    }

                    EnsureCredentialsAcquired();

                    token = SslStreamPal.AcceptSecurityContext(
                        ref _context.CredentialsHandle,
                        ref _securityContext,
                        input,
                        out consumed,
                        _context.Options);
                }
                else
                {
                    EnsureCredentialsAcquired();

                    string hostName = TargetHostNameHelper.NormalizeHostName(_context.Options.TargetHost);
                    token = SslStreamPal.InitializeSecurityContext(
                        ref _context.CredentialsHandle,
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
                bool needsCertValidation = token.Status.ErrorCode == SecurityStatusPalErrorCode.NeedsRemoteCertificateValidation;

                if (done)
                {
                    OnHandshakeCompleted();
                }
                else if (needsCertValidation)
                {
                    // OpenSSL 3.0+ retry-verify: the handshake paused inside
                    // the verify callback. Capture the peer cert + chain so the
                    // caller can validate, then return NeedsCertificateValidation.
                    CaptureRemoteCertificateForExternalValidation();
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
                    return _externalValidationPending
                        ? TlsOperationStatus.NeedsCertificateValidation
                        : TlsOperationStatus.Complete;
                }

                if (needsCertValidation)
                {
                    return TlsOperationStatus.NeedsCertificateValidation;
                }

                if (needsCredentials)
                {
                    return TlsOperationStatus.WantCredentials;
                }

                // SChannel consumes one TLS record per AcceptSecurityContext/
                // InitializeSecurityContext call (OpenSSL typically consumes the
                // whole input via the BIO). When the PAL accepted bytes but the
                // caller still has more buffered, return Complete so the driver
                // re-enters us with the remainder instead of blocking on a network
                // read the peer will never satisfy (e.g. server seeing CKE+CCS+
                // Finished in one TCP read during a TLS 1.2 handshake).
                if (consumed > 0 && consumed < input.Length)
                {
                    return TlsOperationStatus.Complete;
                }

                return TlsOperationStatus.WantRead;
            }
            finally
            {
                token.ReleasePayload();
            }
        }

        public TlsOperationStatus Encrypt(
            ReadOnlySpan<byte> plaintext,
            Span<byte> ciphertext,
            out int consumed,
            out int produced)
        {
            ThrowIfDisposed();
            ThrowIfPendingExternalValidation();
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

            int chunk = Math.Min(plaintext.Length, _maxDataSize);
            byte[] rented = ArrayPool<byte>.Shared.Rent(chunk);
            try
            {
                plaintext.Slice(0, chunk).CopyTo(rented);

                ProtocolToken token = SslStreamPal.EncryptMessage(
                    _securityContext!,
                    new ReadOnlyMemory<byte>(rented, 0, chunk),
                    _headerSize,
                    _trailerSize);

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
            ThrowIfPendingExternalValidation();
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
                case SecurityStatusPalErrorCode.ContextExpiredError:
                    consumed = frameSize;
                    return TlsOperationStatus.Closed;

                case SecurityStatusPalErrorCode.Renegotiate:
                    // SChannel surfaces SEC_I_RENEGOTIATE for two distinct cases:
                    //  - TLS 1.2 peer-initiated renegotiation (HelloRequest).
                    //  - TLS 1.3 post-handshake messages (NewSessionTicket,
                    //    KeyUpdate, post-handshake CertificateRequest).
                    // In either case the decrypted payload is the inner handshake
                    // record that must be fed back into ASC/ISC so SChannel can
                    // update its internal state. If we don't, the next DecryptMessage
                    // returns SEC_E_CONTEXT_EXPIRED because the context is stuck.
                    consumed = frameSize;
                    ProcessPostHandshakeMessage(_decryptScratch.AsSpan(outOffset, outCount));
                    // Return Complete (not WantRead): we consumed input bytes but
                    // produced no plaintext. The caller's loop should re-enter to
                    // process any remaining buffered ciphertext (e.g. application
                    // data that arrived in the same TCP segment as the NST).
                    return TlsOperationStatus.Complete;

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
                    ref _context.CredentialsHandle,
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
                            ref _context.CredentialsHandle,
                            ref _securityContext,
                            ReadOnlySpan<byte>.Empty,
                            out _,
                            _context.Options);
                    }
                    else
                    {
                        string hostName = TargetHostNameHelper.NormalizeHostName(_context.Options.TargetHost);
                        token = SslStreamPal.InitializeSecurityContext(
                            ref _context.CredentialsHandle,
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

        // Server-side: parses the ClientHello and returns a populated
        // SslClientHelloInfo (SNI + supported versions), or null if more bytes
        // are needed or the record is not a ClientHello. Used by the
        // deferred-options path; does not mutate session state.
        private static SslClientHelloInfo? TryParseClientHello(ReadOnlySpan<byte> input)
        {
            TlsFrameHelper.TlsFrameInfo frameInfo = default;
            if (!TlsFrameHelper.TryGetFrameInfo(input, ref frameInfo))
            {
                return null;
            }

            if (frameInfo.HandshakeType != TlsHandshakeType.ClientHello)
            {
                return null;
            }

            return new SslClientHelloInfo(frameInfo.TargetName ?? string.Empty, frameInfo.SupportedVersions);
        }

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
        // Set by the SslStream wedge: SslStream owns the validation flow and will
        // invoke the user callback itself with the SslStream as the sender. Skipping
        // here avoids invoking the callback twice and avoids handing TlsSession to
        // user code that expects SslStream.
        internal bool SuppressInternalCertificateValidation
        {
            get => _suppressInternalCertificateValidation;
            set => _suppressInternalCertificateValidation = value;
        }

        internal SafeDeleteSslContext? SecurityContext => _securityContext;
        internal TlsContext Context => _context;
        internal SafeFreeCredentials? CredentialsHandle
        {
            get => _context.CredentialsHandle;
            set => _context.CredentialsHandle = value;
        }

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
                    ref _context.CredentialsHandle,
                    ref _securityContext,
                    input,
                    out consumed,
                    _context.Options);
            }
            else
            {
                string hostName = TargetHostNameHelper.NormalizeHostName(_context.Options.TargetHost);
                token = SslStreamPal.InitializeSecurityContext(
                    ref _context.CredentialsHandle,
                    ref _securityContext,
                    hostName,
                    input,
                    out consumed,
                    _context.Options);
            }

            if (token.Status.ErrorCode == SecurityStatusPalErrorCode.OK)
            {
                OnHandshakeCompleted();
            }

            return token;
        }

        private void OnHandshakeCompleted()
        {
            _isHandshakeComplete = true;
            SslStreamPal.QueryContextConnectionInfo(_securityContext!, ref _connectionInfo);
            SslStreamPal.QueryContextStreamSizes(_securityContext!, out StreamSizes streamSizes);
            _headerSize = streamSizes.Header;
            _trailerSize = streamSizes.Trailer;
            if (streamSizes.MaximumMessage > 0)
            {
                _maxDataSize = Math.Min(streamSizes.MaximumMessage, MaxRecordPlaintext);
            }

            // Invoke remote-certificate validation callback (mirrors SslStream).
            // Skip when the peer does not present a certificate AND validation isn't
            // mandatory (client always validates the server; server only when
            // ClientCertificateRequired).
            bool needsValidation = !_suppressInternalCertificateValidation &&
                (!_context.IsServer || _context.Options.RemoteCertRequired);
            if (!needsValidation)
            {
                return;
            }

            // If the caller already resolved validation mid-handshake (OpenSSL 3.0+
            // retry-verify path), don't re-suspend here.
            if (_externalValidationResolved)
            {
                return;
            }

            CaptureRemoteCertificateForExternalValidation();
        }

        // Capture the peer certificate and chain so the caller can perform validation
        // out of band. Used both when the handshake completes (1.1.x: callback already
        // accepted) and when the handshake pauses mid-flight via SSL_set_retry_verify
        // (OpenSSL 3.0+). Keeps the cert in _externalPendingCert (not _remoteCertificate)
        // so VerifyRemoteCertificateCore's renegotiation shortcut doesn't dispose it
        // when AcceptWithDefaultValidation runs.
        private void CaptureRemoteCertificateForExternalValidation()
        {
            X509Chain? chain = null;
            _externalPendingCert = CertificateValidationPal.GetRemoteCertificate(
                _securityContext, ref chain, _context.Options.CertificateChainPolicy);
            _externalValidationChain = chain;
            _externalValidationPending = true;
        }

        // Acquire the SafeFreeCredentials the PAL needs for the first ASC/ISC
        // call. OpenSSL handles credential acquisition lazily inside the PAL,
        // but SChannel rejects ASC/ISC with a null credentials handle.
        //
        // PoC scope: minimal acquisition path — server requires a pre-set
        // CertificateContext (or one resolved via ServerCertSelectionDelegate
        // above), and the client connects anonymously. We don't yet integrate
        // with SslSessionsCache, the legacy CertSelectionDelegate, or client
        // certificate selection.
        private void EnsureCredentialsAcquired()
        {
            if (_context.CredentialsHandle is not null)
            {
                return;
            }

            _context.CredentialsHandle = SslStreamPal.AcquireCredentialsHandle(_context.Options, false);
        }

        // Feed a decrypted post-handshake message (e.g. TLS 1.3 NewSessionTicket
        // or KeyUpdate) back through ASC/ISC so SChannel updates its internal
        // state. The PAL may or may not produce a reply token; if it does, stage
        // it for the caller to send on the next drain.
        private void ProcessPostHandshakeMessage(ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty)
            {
                return;
            }

            ProtocolToken token = default;
            token.RentBuffer = true;
            try
            {
                if (_context.IsServer)
                {
                    token = SslStreamPal.AcceptSecurityContext(
                        ref _context.CredentialsHandle,
                        ref _securityContext,
                        data,
                        out _,
                        _context.Options);
                }
                else
                {
                    string hostName = TargetHostNameHelper.NormalizeHostName(_context.Options.TargetHost);
                    token = SslStreamPal.InitializeSecurityContext(
                        ref _context.CredentialsHandle,
                        ref _securityContext,
                        hostName,
                        data,
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

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;

            DisposeExternalValidationChain();
            _externalPendingCert?.Dispose();
            _externalPendingCert = null;

            _securityContext?.Dispose();
            _securityContext = null;

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
