// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;
// macOS PAL has two SafeDelete* derivatives (SecureTransport + Network.framework)
// and surfaces the base type in ref parameters. Use the base type for the security-context
// field on macOS so it lines up with the PAL ref signatures; other platforms keep the
// derived SafeDeleteSslContext.
#if TARGET_APPLE
using TlsSecurityContext = System.Net.Security.SafeDeleteContext;
#else
using TlsSecurityContext = System.Net.Security.SafeDeleteSslContext;
#endif

namespace System.Net.Security
{
    /// <summary>
    /// Non-blocking TLS state machine wrapper around the existing
    /// <see cref="SslStreamPal"/>. The caller owns I/O and drives ciphertext
    /// in and out via byte spans. Supported on Linux/FreeBSD (OpenSSL) and
    /// Windows (SChannel). Provides <see cref="ProcessHandshake"/>,
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
    [Experimental(Experimentals.LowLevelTlsDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    public sealed partial class TlsSession : IDisposable
    {
        // Matches StreamSizes.Default on Unix; conservative upper bound for a
        // single TLS record's plaintext payload.
        internal const int MaxRecordPlaintext = 16354;

        private readonly TlsContext _context;
        private readonly SslAuthenticationOptions _options;
        private readonly bool _ownsOptions;
        private bool _hasServerOptions;
        private TlsSecurityContext? _securityContext;

        private byte[]? _pending;
        private int _pendingOffset;
        private int _pendingLength;

        private byte[]? _decryptScratch;

        private bool _isHandshakeComplete;
        private bool _suppressInternalCertificateValidation;
        private bool _externalValidationPending;
        private bool _externalValidationResolved;
        // Set by SetClientCertificateContext after a WantCredentials suspension; consumed by
        // the next ProcessHandshake to allow an empty-input re-entry past the frame guard.
        private bool _resumeAfterCredentials;
        // Intermediate certs the peer sent (chain elements minus the leaf). The platform-built
        // X509Chain itself is never surfaced to TlsSession callers; AcceptWithDefaultValidation
        // rebuilds a fresh chain from this collection at validation time.
        private X509Certificate2Collection? _externalRemoteCertificates;
        private X509Certificate2? _externalPendingCert;
        private Exception? _externalValidationFault;
        private SslClientHelloInfo? _clientHelloInfo;
        private bool _disposed;
        private SslConnectionInfo _connectionInfo;
        private X509Certificate2? _remoteCertificate;
        private int _headerSize;
        private int _trailerSize;
        private int _maxDataSize = MaxRecordPlaintext;

        // Socket-bound mode (optional). When set, the session performs its own
        // non-blocking I/O via Handshake/Read/Write. The session takes ownership
        // of the supplied socket handle and disposes it with the session.
        private SafeSocketHandle? _socketHandle;
        private Socket? _socket;
        private byte[]? _socketInBuf;
        private int _socketInUsed;

        private TlsSession(TlsContext context)
        {
            _context = context;
            _ownsOptions = !context.ShareOptions;
            _options = context.CreateSessionOptions();
            _hasServerOptions = context.TemplateHasServerOptions;
        }


        /// <summary>
        /// Creates a socket-bound session that drives its own ciphertext I/O on the
        /// supplied socket via <see cref="Handshake"/>, <see cref="Read"/>, and
        /// <see cref="Write"/>. The socket must be configured as non-blocking;
        /// behavior with a blocking socket is unspecified.
        /// </summary>
        /// <remarks>
        /// The session takes ownership of <paramref name="socket"/> and disposes
        /// it when the session is disposed.
        /// </remarks>
        public static TlsSession Create(TlsContext context, SafeSocketHandle socket)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(socket);

            TlsSession session = new TlsSession(context);
            session._socketHandle = socket;

            // Platforms with a native fd-binding fast path (OpenSSL) take the
            // socket directly; otherwise wrap it in a managed Socket for the
            // buffered I/O path.
            bool nativeBindingEnabled = false;
            session.EnableNativeSocketBinding(socket, ref nativeBindingEnabled);
            if (!nativeBindingEnabled)
            {
                session._socket = new Socket(socket);
            }
            return session;
        }

        public SafeSocketHandle? Socket => _socketHandle;
        public static TlsSession Create(TlsContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            TlsSession session = new TlsSession(context);

            return session;
        }

        // ── State ─────────────────────────────────────────────────────────

        public bool IsHandshakeComplete => _isHandshakeComplete;

        public bool HasPendingOutput => _pendingLength > 0;

        public string? TargetHostName
        {
            get => _options.TargetHost;
            set => _options.TargetHost = value ?? string.Empty;
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
        /// Returns the intermediate certificates the peer sent alongside its leaf certificate
        /// (the leaf itself is available via <see cref="GetRemoteCertificate"/>), or <c>null</c>
        /// if no intermediates were received. Only meaningful while the session is awaiting an
        /// external validation result (after <see cref="ProcessHandshake"/> returned
        /// <see cref="TlsOperationStatus.NeedsCertificateValidation"/>). The certificates are
        /// owned by the session and disposed when the session is disposed or when the validation
        /// result is recorded; callers that need to retain them must clone the instances.
        /// </summary>
        public X509Certificate2Collection? GetRemoteCertificates()
        {
            ThrowIfDisposed();
            return _externalRemoteCertificates;
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

            // Build a fresh X509Chain locally and seed it with the peer-sent intermediates.
            // The chain instance is never exposed to TlsSession callers; once validation is
            // recorded it is disposed in SetRemoteCertificateValidationResult below.
            X509Chain chain = new X509Chain();
            if (_externalRemoteCertificates is { Count: > 0 } intermediates)
            {
                chain.ChainPolicy.ExtraStore.AddRange(intermediates);
            }

            ProtocolToken alertToken = default;
            SslPolicyErrors sslPolicyErrors;
            bool ok;
            try
            {
                // Pass _externalPendingCert as the candidate cert and an empty _remoteCertificate slot.
                // VerifyRemoteCertificateCore assigns the slot to the candidate on success; the renegotiation
                // shortcut at the top of that method would otherwise dispose our cert if the slot were already
                // populated with the same instance.
                ok = SslStream.VerifyRemoteCertificateCore(
                    this,
                    _options,
                    _securityContext,
                    ref _remoteCertificate,
                    ref _connectionInfo,
                    _externalPendingCert,
                    chain,
                    trust: null,
                    ref alertToken,
                    out sslPolicyErrors,
                    out _);
            }
            finally
            {
                chain.Dispose();
            }

            // A user RemoteCertificateValidationCallback can reject an otherwise-clean chain
            // by returning false with sslPolicyErrors == None. Synthesize a non-None failure
            // so SetRemoteCertificateValidationResult takes the reject branch instead of accepting.
            if (!ok && sslPolicyErrors == SslPolicyErrors.None)
            {
                sslPolicyErrors = SslPolicyErrors.RemoteCertificateChainErrors;
            }

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

#if !TARGET_WINDOWS && !SYSNETSECURITY_NO_OPENSSL
            // OpenSSL 3.0+ retry-verify path: the handshake paused inside the CertVerifyCallback.
            // Push the verdict to the SafeSslHandle so the next SSL_do_handshake call (driven by
            // the caller's next ProcessHandshake) re-invokes the callback and either accepts the
            // peer cert (Finished is emitted) or rejects it (a fatal alert is emitted).
            PushExternalValidationVerdictToPalIfRetryVerify(errors);
#endif

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
                // Post-hoc rejection (handshake already wire-complete on OpenSSL 1.1.x or Schannel):
                // surface the fault immediately so subsequent Encrypt/Decrypt throw. For the
                // retry-verify path the handshake is still incomplete and the fault is set when
                // ProcessHandshake drives SSL_do_handshake to failure (so any pending alert bytes
                // are drained to the caller first).
                if (_isHandshakeComplete)
                {
                    _externalValidationFault = new AuthenticationException(SR.net_ssl_io_cert_validation);
                }

                // VerifyRemoteCertificateCore assigns _remoteCertificate to the candidate before it
                // knows whether the chain validates, so on the reject path the rejected leaf is sitting
                // in the canonical slot. Drop it so GetRemoteCertificate cannot surface a cert the caller
                // explicitly refused. Either _remoteCertificate or _externalPendingCert owns it, not both.
                if (_remoteCertificate is not null && ReferenceEquals(_remoteCertificate, _externalPendingCert))
                {
                    _remoteCertificate = null;
                }
                else
                {
                    _remoteCertificate?.Dispose();
                    _remoteCertificate = null;
                }
                _externalPendingCert?.Dispose();
                _externalPendingCert = null;
            }

            DisposeExternalRemoteCertificates();
        }

#if !TARGET_WINDOWS && !SYSNETSECURITY_NO_OPENSSL
        // Client-side only path. When CertVerifyCallback paused the handshake via
        // SSL_set_retry_verify, RetryVerifyAttempted is set on the SafeSslHandle. Stamp
        // the caller's verdict onto the handle so the next SSL_do_handshake (driven by
        // the caller's next ProcessHandshake) re-enters the callback and either accepts
        // the peer cert or emits a fatal alert. No-op on server sessions and on 1.1.x
        // where CertVerifyCallback took the accept-and-defer branch instead of retrying.
        private void PushExternalValidationVerdictToPalIfRetryVerify(SslPolicyErrors errors)
        {
            if (_securityContext is not Microsoft.Win32.SafeHandles.SafeSslHandle sslHandle ||
                !sslHandle.RetryVerifyAttempted)
            {
                return;
            }

            sslHandle.ExternalValidationAccepted = errors == SslPolicyErrors.None;
        }
#endif

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
            if (_hasServerOptions)
            {
                throw new InvalidOperationException("Server options were already supplied when the TlsContext was created.");
            }
            if (_clientHelloInfo is null)
            {
                throw new InvalidOperationException("SetServerOptions can only be called after ProcessHandshake returned NeedsServerOptions.");
            }

            _options.UpdateOptions(options);
            _hasServerOptions = true;
            _clientHelloInfo = null;

            OnServerOptionsSet();
        }

        /// <summary>
        /// Client-side only. Supplies the certificate context the session should send
        /// in response to the server's CertificateRequest. Intended to resolve a session
        /// suspended on <see cref="TlsOperationStatus.WantCredentials"/>: callers that need
        /// to fetch a certificate from an out-of-process source (e.g. a key vault) do so
        /// outside the session, then resume the handshake with another call to
        /// <see cref="ProcessHandshake"/> (with empty input). May also be called before
        /// the first <see cref="ProcessHandshake"/> to seed the client credential when the
        /// <see cref="TlsContext"/> was created without one.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown on a server-side session.
        /// </exception>
        public void SetClientCertificateContext(SslStreamCertificateContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            ThrowIfDisposed();

            if (_context.IsServer)
            {
                throw new InvalidOperationException("SetClientCertificateContext can only be called on a client-side session.");
            }

            _options.CertificateContext = context;

            // Drop the cached credentials handle (acquired without a client cert) so the
            // next ProcessHandshake re-acquires with the supplied context.
            _context.CredentialsHandle?.Dispose();
            _context.CredentialsHandle = null;
            _resumeAfterCredentials = true;
        }

        /// <summary>
        /// Client-side only. Returns the distinguished names of the certificate authorities
        /// the server listed in its TLS 1.2 <c>CertificateRequest</c> or TLS 1.3
        /// <c>certificate_authorities</c> extension. Intended to be called while the session
        /// is suspended on <see cref="TlsOperationStatus.WantCredentials"/> so the caller can
        /// pick a client certificate that chains to one of the listed CAs. Returns
        /// <see langword="null"/> when no security context exists yet, when the peer sent no
        /// hints, or on a server-side session.
        /// </summary>
        public IReadOnlyList<string>? GetAcceptableIssuers()
        {
            ThrowIfDisposed();

            if (_context.IsServer || _securityContext is null)
            {
                return null;
            }

            string[] issuers = CertificateValidationPal.GetRequestCertificateAuthorities(_securityContext);
            return issuers.Length == 0 ? null : issuers;
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

        private void DisposeExternalRemoteCertificates()
        {
            X509Certificate2Collection? certs = _externalRemoteCertificates;
            _externalRemoteCertificates = null;
            if (certs is null)
            {
                return;
            }
            foreach (X509Certificate2 c in certs)
            {
                c.Dispose();
            }
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
                    return _options.CertificateContext?.TargetCertificate;
                }

                if (_securityContext == null || _securityContext.IsInvalid)
                {
                    return null;
                }

                if (!CertificateValidationPal.IsLocalCertificateUsed(_context.CredentialsHandle, _securityContext))
                {
                    return null;
                }

                return _options.CertificateContext?.TargetCertificate;
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
            out int bytesConsumed,
            out int bytesWritten)
        {
            ThrowIfDisposed();
            bytesConsumed = 0;
            bytesWritten = 0;

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
                bytesWritten = DrainTo(output);
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
            // client-side ISC, which produces the ClientHello, or a client resume after
            // SetClientCertificateContext resolved a prior WantCredentials suspension.
            bool isInitialClientCall = !_context.IsServer && _securityContext is null;
            bool isCredentialResume = _resumeAfterCredentials;
            _resumeAfterCredentials = false;
            if (!isInitialClientCall && !isCredentialResume)
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
                        if (!_hasServerOptions)
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
                            _options.CertificateContext is null &&
                            _options.ServerCertSelectionDelegate is not null;

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
                        out bytesConsumed,
                        _options);
                }
                else
                {
                    EnsureCredentialsAcquired();

                    string hostName = TargetHostNameHelper.NormalizeHostName(_options.TargetHost);
                    token = SslStreamPal.InitializeSecurityContext(
                        ref _context.CredentialsHandle,
                        ref _securityContext,
                        hostName,
                        input,
                        out bytesConsumed,
                        _options);
                }

                // Stage any handshake bytes the PAL produced.
                if (token.Size > 0)
                {
                    Debug.Assert(token.Payload != null);
                    AppendPending(new ReadOnlySpan<byte>(token.Payload, 0, token.Size));
                }

                // Server-side ALPN selection ceremony (SChannel and SecureTransport).
                // After parsing the ClientHello the PAL pauses and asks the caller to
                // pick the application protocol before resuming. We re-enter ASC with
                // an empty input so the PAL can generate the ServerHello carrying the
                // selected ALPN value.
                if (token.Status.ErrorCode == SecurityStatusPalErrorCode.HandshakeStarted)
                {
                    ReadOnlySpan<byte> rawAlpn = ReadOnlySpan<byte>.Empty;
                    TlsFrameHelper.TlsFrameInfo frameInfo = default;
                    if (TlsFrameHelper.TryGetFrameInfo(input, ref frameInfo,
                            TlsFrameHelper.ProcessingOptions.ApplicationProtocol | TlsFrameHelper.ProcessingOptions.RawApplicationProtocol) &&
                        frameInfo.RawApplicationProtocols is byte[] rawAlpnBytes)
                    {
                        rawAlpn = rawAlpnBytes;
                    }

                    SecurityStatusPal selStatus = SslStreamPal.SelectApplicationProtocol(
                        _context.CredentialsHandle,
                        _securityContext!,
                        _options,
                        rawAlpn);

                    if (selStatus.ErrorCode != SecurityStatusPalErrorCode.OK)
                    {
                        throw new AuthenticationException(SR.net_auth_SSPI, selStatus.Exception);
                    }

                    token.ReleasePayload();

                    if (_context.IsServer)
                    {
                        token = SslStreamPal.AcceptSecurityContext(
                            ref _context.CredentialsHandle,
                            ref _securityContext,
                            ReadOnlySpan<byte>.Empty,
                            out _,
                            _options);
                    }
                    else
                    {
                        string hostName = TargetHostNameHelper.NormalizeHostName(_options.TargetHost);
                        token = SslStreamPal.InitializeSecurityContext(
                            ref _context.CredentialsHandle,
                            ref _securityContext,
                            hostName,
                            ReadOnlySpan<byte>.Empty,
                            out _,
                            _options);
                    }

                    if (token.Size > 0)
                    {
                        Debug.Assert(token.Payload != null);
                        AppendPending(new ReadOnlySpan<byte>(token.Payload, 0, token.Size));
                    }
                }

                if (token.Failed &&
                    token.Status.ErrorCode != SecurityStatusPalErrorCode.CredentialsNeeded &&
                    token.Status.ErrorCode != SecurityStatusPalErrorCode.CertValidationNeeded)
                {
                    Exception authExc = new AuthenticationException(SR.net_auth_SSPI, token.GetException());

                    // OpenSSL queued a TLS alert in the BIO during the failing SSL_do_handshake
                    // (e.g. bad_certificate after the client-side retry-verify callback rejected
                    // the peer). Drain the alert to the caller's output buffer before throwing so
                    // the peer observes an AuthenticationException instead of a connection reset.
                    // The fault is re-raised on the next ProcessHandshake call once the queue is
                    // empty. Only fires on the client path today; server-side never reaches this
                    // branch for external-validation reasons because CertVerifyCallback
                    // accepts-and-defers (see gating in Interop.OpenSsl.CertVerifyCallback).
                    if (_pendingLength > 0)
                    {
                        bytesWritten = DrainTo(output);
                        _externalValidationFault = authExc;
                        return TlsOperationStatus.WantWrite;
                    }

                    throw authExc;
                }

                bool done = token.Status.ErrorCode == SecurityStatusPalErrorCode.OK;
                bool needsCredentials = token.Status.ErrorCode == SecurityStatusPalErrorCode.CredentialsNeeded;
                bool needsCertValidation = token.Status.ErrorCode == SecurityStatusPalErrorCode.CertValidationNeeded;

                if (done)
                {
                    OnHandshakeCompleted();
                }
                else if (needsCertValidation)
                {
                    // PAL paused mid-handshake awaiting external certificate validation.
                    // Capture the peer cert + chain so the caller can validate, then return
                    // NeedsCertificateValidation. Not used by the current OpenSSL or SChannel
                    // paths but kept as a generic suspension hook.
                    CaptureRemoteCertificateForExternalValidation();
                }

                if (_pendingLength > 0)
                {
                    bytesWritten = DrainTo(output);
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
                if (bytesConsumed > 0 && bytesConsumed < input.Length)
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
            out int bytesConsumed,
            out int bytesWritten)
        {
            ThrowIfDisposed();
            ThrowIfPendingExternalValidation();
            bytesConsumed = 0;
            bytesWritten = 0;

            if (!_isHandshakeComplete)
            {
                throw new InvalidOperationException("Handshake has not yet completed.");
            }

            if (_pendingLength > 0)
            {
                bytesWritten = DrainTo(ciphertext);
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

                    bytesConsumed = chunk;

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

            bytesWritten = DrainTo(ciphertext);
            return _pendingLength > 0 ? TlsOperationStatus.WantWrite : TlsOperationStatus.Complete;
        }

        // ── Decrypt ───────────────────────────────────────────────────────

        public TlsOperationStatus Decrypt(
            ReadOnlySpan<byte> ciphertext,
            Span<byte> plaintext,
            out int bytesConsumed,
            out int bytesWritten)
        {
            ThrowIfDisposed();
            ThrowIfPendingExternalValidation();
            bytesConsumed = 0;
            bytesWritten = 0;

            if (!_isHandshakeComplete)
            {
                throw new InvalidOperationException("Handshake has not yet completed.");
            }

            if (_pendingLength > 0)
            {
                // Caller must drain before we accept new input.
                return TlsOperationStatus.WantWrite;
            }

            // Need at least a frame header. If the caller didn't provide a full frame, the PAL
            // may still have plaintext buffered internally — ciphertext absorbed by OpenSSL's
            // BIO during ProcessHandshake (e.g. the peer coalesced its Finished with the first
            // app-data record into one TCP segment) or a record consumed but not yet decrypted
            // by a prior Decrypt call. On platforms whose PAL maintains such a buffer, probe it
            // with an empty input before asking the caller for more wire bytes; otherwise the
            // session deadlocks waiting on data the peer already sent.
            if (ciphertext.Length < TlsFrameHelper.HeaderSize)
            {
                return TryDrainBufferedPlaintext(plaintext, out bytesWritten);
            }

            TlsFrameHeader header = default;
            if (!TlsFrameHelper.TryGetFrameHeader(ciphertext, ref header))
            {
                throw new IOException(SR.net_io_decrypt);
            }

            int frameSize = header.Length;
            if (ciphertext.Length < frameSize)
            {
                return TryDrainBufferedPlaintext(plaintext, out bytesWritten);
            }

            // PAL decrypts in place; copy into a writable scratch buffer.
            EnsureDecryptScratch(frameSize);
            ciphertext.Slice(0, frameSize).CopyTo(_decryptScratch);

            SecurityStatusPal status = SslStreamPal.DecryptMessage(
                _securityContext!,
                _decryptScratch.AsSpan(0, frameSize),
                plaintext,
                out int decBytesWritten,
                out int decLeftoverOffset,
                out int decLeftoverLength);

            switch (status.ErrorCode)
            {
                case SecurityStatusPalErrorCode.OK:
                    bytesConsumed = frameSize;
                    // Linux/macOS PALs write the plaintext directly into the destination span and
                    // (if it didn't fit, or the PAL prefers in-place) leave overflow in the encrypted
                    // span at leftoverOffset/leftoverLength. SChannel always decrypts in place and
                    // reports bytesWritten = 0 with leftoverOffset/leftoverLength pointing at the
                    // plaintext inside the encrypted span. Unify by appending the leftover slice
                    // after whatever was written into destination.
                    int needed = decBytesWritten + decLeftoverLength;
                    if (needed > plaintext.Length)
                    {
                        throw new InvalidOperationException(
                            $"Plaintext buffer too small: needed {needed}, got {plaintext.Length}.");
                    }
                    if (decLeftoverLength > 0)
                    {
                        _decryptScratch.AsSpan(decLeftoverOffset, decLeftoverLength)
                            .CopyTo(plaintext.Slice(decBytesWritten));
                    }
                    bytesWritten = needed;
                    return TlsOperationStatus.Complete;

                case SecurityStatusPalErrorCode.ContextExpired:
                case SecurityStatusPalErrorCode.ContextExpiredError:
                    bytesConsumed = frameSize;
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
                    bytesConsumed = frameSize;
                    if (decLeftoverLength > 0)
                    {
                        ProcessPostHandshakeMessage(_decryptScratch.AsSpan(decLeftoverOffset, decLeftoverLength));
                    }
                    // Return Complete (not WantRead): we consumed input bytes but
                    // produced no plaintext. The caller's loop should re-enter to
                    // process any remaining buffered ciphertext (e.g. application
                    // data that arrived in the same TCP segment as the NST).
                    return TlsOperationStatus.Complete;

                default:
                    throw new IOException(SR.net_io_decrypt, SslStreamPal.GetException(status));
            }
        }

        // Empty-input probe used when the caller's buffer doesn't yet hold a complete TLS
        // frame. On OpenSSL the PAL's record layer may still have plaintext queued from a
        // prior call (handshake input that included trailing app-data, or a second record
        // coalesced into the same TCP segment); calling DecryptMessage with an empty span
        // surfaces it. On SChannel / SecureTransport the equivalent buffer does not exist,
        // so the probe is skipped and the caller is asked for more bytes instead. The
        // bytesConsumed out-parameter on the public Decrypt method is necessarily 0 here:
        // no caller bytes were taken.
        private TlsOperationStatus TryDrainBufferedPlaintext(Span<byte> plaintext, out int bytesWritten)
        {
            bytesWritten = 0;

            if (!OperatingSystem.IsLinux() && !OperatingSystem.IsFreeBSD() && !OperatingSystem.IsAndroid())
            {
                return TlsOperationStatus.WantRead;
            }

            SecurityStatusPal status = SslStreamPal.DecryptMessage(
                _securityContext!,
                Span<byte>.Empty,
                plaintext,
                out int decBytesWritten,
                out int decLeftoverOffset,
                out int decLeftoverLength);

            if (status.ErrorCode != SecurityStatusPalErrorCode.OK)
            {
                // Anything other than success here means there's nothing to drain — the PAL
                // is genuinely waiting on wire bytes. Surface as WantRead; fatal errors will
                // resurface on the next regular Decrypt call with real ciphertext.
                return TlsOperationStatus.WantRead;
            }

            int produced = decBytesWritten + decLeftoverLength;
            if (produced == 0)
            {
                return TlsOperationStatus.WantRead;
            }

            if (produced > plaintext.Length)
            {
                throw new InvalidOperationException(
                    $"Plaintext buffer too small: needed {produced}, got {plaintext.Length}.");
            }

            if (decLeftoverLength > 0)
            {
                // PAL stashed overflow in the (empty) input span — impossible here, but mirror
                // the main Decrypt path for symmetry. With Span<byte>.Empty as input, the OpenSSL
                // PAL has nowhere to stash leftover and won't take this path.
                _decryptScratch.AsSpan(decLeftoverOffset, decLeftoverLength)
                    .CopyTo(plaintext.Slice(decBytesWritten));
            }

            bytesWritten = produced;
            return TlsOperationStatus.Complete;
        }

        // ── Post-handshake auth ──────────────────────────────────────────

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
        public TlsOperationStatus RequestClientCertificate(Span<byte> ciphertext, out int bytesWritten)
        {
            ThrowIfDisposed();
            bytesWritten = 0;

#if TARGET_APPLE
            // SecureTransport does not expose a post-handshake client-authentication
            // path, and Network.framework does not provide renegotiation primitives.
            throw new PlatformNotSupportedException(SR.net_ssl_renegotiate_not_supported);
#else
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
                    _options);
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

            bytesWritten = DrainTo(ciphertext);
            return _pendingLength > 0 ? TlsOperationStatus.WantWrite : TlsOperationStatus.Complete;
#endif
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
        public TlsOperationStatus Shutdown(Span<byte> ciphertext, out int bytesWritten)
        {
            ThrowIfDisposed();
            bytesWritten = 0;

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
                            _options);
                    }
                    else
                    {
                        string hostName = TargetHostNameHelper.NormalizeHostName(_options.TargetHost);
                        token = SslStreamPal.InitializeSecurityContext(
                            ref _context.CredentialsHandle,
                            ref _securityContext,
                            hostName,
                            ReadOnlySpan<byte>.Empty,
                            out _,
                            _options);
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

            bytesWritten = DrainTo(ciphertext);
            return _pendingLength > 0 ? TlsOperationStatus.WantWrite : TlsOperationStatus.Closed;
        }

        // ── Pending output ────────────────────────────────────────────────

        public TlsOperationStatus DrainPendingOutput(Span<byte> ciphertext, out int bytesWritten)
        {
            ThrowIfDisposed();
            bytesWritten = DrainTo(ciphertext);
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
                _options.TargetHost = frameInfo.TargetName;
            }

            ServerCertificateSelectionCallback? selector = _options.ServerCertSelectionDelegate;
            if (selector is null || _options.CertificateContext is not null)
            {
                return true;
            }

            X509Certificate? selected = selector(this, _options.TargetHost);
            if (selected is null)
            {
                throw new AuthenticationException(SR.net_ssl_io_no_server_cert);
            }

            X509Certificate2? withKey = SslStream.FindCertificateWithPrivateKey(this, isServer: true, selected);
            if (withKey is null)
            {
                throw new AuthenticationException(SR.net_ssl_io_no_server_cert);
            }

            _options.SetCertificateContextFromCert(withKey);
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

        internal TlsSecurityContext? SecurityContext => _securityContext;
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
        internal ProtocolToken HandshakeStepForSslStream(ReadOnlySpan<byte> input, out int bytesConsumed)
        {
            ThrowIfDisposed();

            ProtocolToken token;
            if (_context.IsServer)
            {
                token = SslStreamPal.AcceptSecurityContext(
                    ref _context.CredentialsHandle,
                    ref _securityContext,
                    input,
                    out bytesConsumed,
                    _options);
            }
            else
            {
                string hostName = TargetHostNameHelper.NormalizeHostName(_options.TargetHost);
                token = SslStreamPal.InitializeSecurityContext(
                    ref _context.CredentialsHandle,
                    ref _securityContext,
                    hostName,
                    input,
                    out bytesConsumed,
                    _options);
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
            // Client: always validate the server cert.
            // Server: always suspend so the caller's RemoteCertificateValidationCallback runs
            // (it must see optional client certs and the no-cert case alike — only the
            // RemoteCertificateNotAvailable error is suppressed in VerifyRemoteCertificateCore
            // when there is no user callback and RemoteCertRequired is false).
            if (_suppressInternalCertificateValidation)
            {
                return;
            }

            // If the caller already resolved validation via a prior suspension
            // (defensive — current OpenSSL/SChannel paths only suspend once via
            // the post-handshake hook below), don't re-suspend here.
            if (_externalValidationResolved)
            {
                return;
            }

            CaptureRemoteCertificateForExternalValidation();
        }

        // Capture the peer certificate and chain so the caller can perform validation
        // out of band. Keeps the cert in _externalPendingCert (not _remoteCertificate)
        // so VerifyRemoteCertificateCore's renegotiation shortcut doesn't dispose it
        // when AcceptWithDefaultValidation runs.
        private void CaptureRemoteCertificateForExternalValidation()
        {
            X509Chain? chain = null;
            _externalPendingCert = CertificateValidationPal.GetRemoteCertificate(
                _securityContext, ref chain, _options.CertificateChainPolicy);

            // Snapshot the peer-sent intermediates into a flat collection and dispose the
            // platform-built chain immediately. The chain instance never escapes the PAL
            // boundary into TlsSession state or its public surface.
            if (chain is not null)
            {
                if (chain.ChainElements.Count > 1)
                {
                    X509Certificate2Collection intermediates = new X509Certificate2Collection();
                    for (int i = 1; i < chain.ChainElements.Count; i++)
                    {
                        intermediates.Add(new X509Certificate2(chain.ChainElements[i].Certificate));
                    }
                    _externalRemoteCertificates = intermediates;
                }
                chain.Dispose();
            }

            _externalValidationPending = true;
        }

        // Acquire the SafeFreeCredentials the PAL needs for the first ASC/ISC
        // call. OpenSSL handles credential acquisition lazily inside the PAL,
        // but SChannel rejects ASC/ISC with a null credentials handle.
        //
        // Server requires a pre-set CertificateContext (or one resolved via
        // ServerCertSelectionDelegate above); the client connects anonymously.
        // SslSessionsCache, the legacy CertSelectionDelegate, and client
        // certificate selection are not yet integrated.
        private void EnsureCredentialsAcquired()
        {
            if (_context.CredentialsHandle is not null)
            {
                return;
            }

            _context.CredentialsHandle = SslStreamPal.AcquireCredentialsHandle(_options, false);
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
                        _options);
                }
                else
                {
                    string hostName = TargetHostNameHelper.NormalizeHostName(_options.TargetHost);
                    token = SslStreamPal.InitializeSecurityContext(
                        ref _context.CredentialsHandle,
                        ref _securityContext,
                        hostName,
                        data,
                        out _,
                        _options);
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

        // ── Socket-bound I/O ─────────────────────────────────────────────
        //
        // These methods are only valid when the session was created via
        // Create(TlsContext, SafeSocketHandle). They drive ciphertext on the
        // bound non-blocking socket and translate WouldBlock into WantRead/
        // WantWrite back to the caller so a select/epoll/IOCP-like loop can
        // schedule the next attempt.

        private const int SocketScratchSize = MaxRecordPlaintext + 256;

        private void ThrowIfNotSocketBound()
        {
            if (_socketHandle is null)
            {
                throw new InvalidOperationException("Session is not socket-bound.");
            }
        }

        // Drains any TLS bytes that we previously failed to fully send into the
        // socket. Returns true if pending output is now empty, false if the
        // socket would block (WantWrite should be surfaced).
        private bool TryDrainPendingToSocket(out SocketError lastError)
        {
            lastError = SocketError.Success;
            while (_pendingLength > 0)
            {
                int sent = _socket!.Send(
                    new ReadOnlySpan<byte>(_pending!, _pendingOffset, _pendingLength),
                    SocketFlags.None,
                    out SocketError err);
                lastError = err;
                if (sent > 0)
                {
                    _pendingOffset += sent;
                    _pendingLength -= sent;
                    if (_pendingLength == 0)
                    {
                        _pendingOffset = 0;
                        return true;
                    }
                    continue;
                }
                return false;
            }
            return true;
        }

        public TlsOperationStatus Handshake()
        {
            ThrowIfDisposed();
            ThrowIfNotSocketBound();

            if (_isHandshakeComplete && !_externalValidationPending && !_externalValidationResolved)
            {
                return TlsOperationStatus.Complete;
            }

            TlsOperationStatus? fast = null;
            TryFastHandshake(ref fast);
            if (fast.HasValue)
            {
                return fast.Value;
            }

            _socketInBuf ??= ArrayPool<byte>.Shared.Rent(SocketScratchSize);
            byte[] scratch = ArrayPool<byte>.Shared.Rent(SocketScratchSize);
            try
            {
                while (true)
                {
                    if (_pendingLength > 0)
                    {
                        if (!TryDrainPendingToSocket(out SocketError drainErr))
                        {
                            if (drainErr == SocketError.WouldBlock)
                            {
                                return TlsOperationStatus.WantWrite;
                            }
                            throw new SocketException((int)drainErr);
                        }
                    }

                    TlsOperationStatus status = ProcessHandshake(
                        new ReadOnlySpan<byte>(_socketInBuf, 0, _socketInUsed),
                        scratch,
                        out int consumed,
                        out int produced);

                    if (consumed > 0)
                    {
                        int remaining = _socketInUsed - consumed;
                        if (remaining > 0)
                        {
                            Buffer.BlockCopy(_socketInBuf, consumed, _socketInBuf, 0, remaining);
                        }
                        _socketInUsed = remaining;
                    }

                    if (produced > 0)
                    {
                        int offset = 0;
                        while (offset < produced)
                        {
                            int sent = _socket!.Send(
                                new ReadOnlySpan<byte>(scratch, offset, produced - offset),
                                SocketFlags.None,
                                out SocketError sendErr);
                            if (sent > 0)
                            {
                                offset += sent;
                                continue;
                            }
                            if (sendErr == SocketError.WouldBlock)
                            {
                                // Stash the unsent tail so the next call resumes the drain.
                                AppendPending(new ReadOnlySpan<byte>(scratch, offset, produced - offset));
                                return TlsOperationStatus.WantWrite;
                            }
                            throw new SocketException((int)sendErr);
                        }
                    }

                    switch (status)
                    {
                        case TlsOperationStatus.Complete:
                            return TlsOperationStatus.Complete;

                        case TlsOperationStatus.WantRead:
                            if (_socketInUsed >= _socketInBuf.Length)
                            {
                                // Should not happen with conservative scratch sizing, but guard.
                                Array.Resize(ref _socketInBuf, _socketInBuf.Length * 2);
                            }
                            int received = _socket!.Receive(
                                _socketInBuf.AsSpan(_socketInUsed),
                                SocketFlags.None,
                                out SocketError recvErr);
                            if (received > 0)
                            {
                                _socketInUsed += received;
                                continue;
                            }
                            if (recvErr == SocketError.WouldBlock)
                            {
                                return TlsOperationStatus.WantRead;
                            }
                            if (received == 0)
                            {
                                return TlsOperationStatus.Closed;
                            }
                            throw new SocketException((int)recvErr);

                        case TlsOperationStatus.WantWrite:
                            // Output is staged; loop drains it on next iteration.
                            continue;

                        default:
                            return status;
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(scratch);
            }
        }

        public TlsOperationStatus Read(Span<byte> buffer, out int bytesRead)
        {
            ThrowIfDisposed();
            ThrowIfNotSocketBound();
            bytesRead = 0;

            if (!_isHandshakeComplete)
            {
                throw new InvalidOperationException("Handshake has not yet completed.");
            }

            TlsOperationStatus? fast = null;
            TryFastRead(buffer, ref bytesRead, ref fast);
            if (fast.HasValue)
            {
                return fast.Value;
            }

            _socketInBuf ??= ArrayPool<byte>.Shared.Rent(SocketScratchSize);

            while (true)
            {
                if (_socketInUsed > 0)
                {
                    TlsOperationStatus status = Decrypt(
                        new ReadOnlySpan<byte>(_socketInBuf, 0, _socketInUsed),
                        buffer,
                        out int consumed,
                        out int produced);

                    if (consumed > 0)
                    {
                        int remaining = _socketInUsed - consumed;
                        if (remaining > 0)
                        {
                            Buffer.BlockCopy(_socketInBuf, consumed, _socketInBuf, 0, remaining);
                        }
                        _socketInUsed = remaining;
                    }

                    bytesRead = produced;

                    if (status == TlsOperationStatus.Complete && produced > 0)
                    {
                        return TlsOperationStatus.Complete;
                    }
                    if (status == TlsOperationStatus.Closed)
                    {
                        return TlsOperationStatus.Closed;
                    }
                    if (status == TlsOperationStatus.Complete && produced == 0)
                    {
                        // Post-handshake message consumed; loop to try more.
                        continue;
                    }
                    if (status != TlsOperationStatus.WantRead)
                    {
                        return status;
                    }
                    // WantRead: fall through to socket recv.
                }

                if (_socketInUsed >= _socketInBuf.Length)
                {
                    Array.Resize(ref _socketInBuf, _socketInBuf.Length * 2);
                }
                int received = _socket!.Receive(
                    _socketInBuf.AsSpan(_socketInUsed),
                    SocketFlags.None,
                    out SocketError recvErr);
                if (received > 0)
                {
                    _socketInUsed += received;
                    continue;
                }
                if (recvErr == SocketError.WouldBlock)
                {
                    return TlsOperationStatus.WantRead;
                }
                if (received == 0)
                {
                    return TlsOperationStatus.Closed;
                }
                throw new SocketException((int)recvErr);
            }
        }

        public TlsOperationStatus Write(ReadOnlySpan<byte> buffer, out int bytesWritten)
        {
            ThrowIfDisposed();
            ThrowIfNotSocketBound();
            bytesWritten = 0;

            if (!_isHandshakeComplete)
            {
                throw new InvalidOperationException("Handshake has not yet completed.");
            }

            TlsOperationStatus? fast = null;
            TryFastWrite(buffer, ref bytesWritten, ref fast);
            if (fast.HasValue)
            {
                return fast.Value;
            }

            // Drain any previously stashed ciphertext first.
            if (_pendingLength > 0)
            {
                if (!TryDrainPendingToSocket(out SocketError drainErr))
                {
                    if (drainErr == SocketError.WouldBlock)
                    {
                        return TlsOperationStatus.WantWrite;
                    }
                    throw new SocketException((int)drainErr);
                }
            }

            if (buffer.IsEmpty)
            {
                return TlsOperationStatus.Complete;
            }

            byte[] scratch = ArrayPool<byte>.Shared.Rent(SocketScratchSize);
            try
            {
                int totalConsumed = 0;
                while (totalConsumed < buffer.Length)
                {
                    TlsOperationStatus encStatus = Encrypt(
                        buffer.Slice(totalConsumed),
                        scratch,
                        out int consumed,
                        out int produced);

                    totalConsumed += consumed;

                    if (produced > 0)
                    {
                        int offset = 0;
                        while (offset < produced)
                        {
                            int sent = _socket!.Send(
                                new ReadOnlySpan<byte>(scratch, offset, produced - offset),
                                SocketFlags.None,
                                out SocketError sendErr);
                            if (sent > 0)
                            {
                                offset += sent;
                                continue;
                            }
                            if (sendErr == SocketError.WouldBlock)
                            {
                                AppendPending(new ReadOnlySpan<byte>(scratch, offset, produced - offset));
                                bytesWritten = totalConsumed;
                                return TlsOperationStatus.WantWrite;
                            }
                            throw new SocketException((int)sendErr);
                        }
                    }

                    if (encStatus == TlsOperationStatus.WantWrite)
                    {
                        // Pending output owed; resume next call.
                        bytesWritten = totalConsumed;
                        return TlsOperationStatus.WantWrite;
                    }
                    if (encStatus != TlsOperationStatus.Complete)
                    {
                        bytesWritten = totalConsumed;
                        return encStatus;
                    }
                    if (consumed == 0)
                    {
                        // Nothing more to do (shouldn't happen with non-empty buffer).
                        break;
                    }
                }

                bytesWritten = totalConsumed;
                return TlsOperationStatus.Complete;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(scratch);
            }
        }

        // Platform hooks. Implemented by the OpenSSL partial (TlsSession.OpenSsl.cs)
        // to bind the socket fd directly to the SSL object and drive ciphertext
        // through OpenSSL. On Windows (SChannel) these are no-ops and the buffered
        // ProcessHandshake/Encrypt/Decrypt path above is used unchanged.
        partial void EnableNativeSocketBinding(SafeSocketHandle socket, ref bool nativeBindingEnabled);
        partial void TryFastHandshake(ref TlsOperationStatus? result);
        partial void TryFastRead(Span<byte> buffer, ref int bytesRead, ref TlsOperationStatus? result);
        partial void TryFastWrite(ReadOnlySpan<byte> buffer, ref int bytesWritten, ref TlsOperationStatus? result);

        // Fires at the end of SetServerOptions. Platforms with a deferred-server
        // fast path (OpenSSL socket-bound sessions) use this hook to activate
        // native binding now that server options are known.
        partial void OnServerOptionsSet();

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;

            DisposeExternalRemoteCertificates();
            _externalPendingCert?.Dispose();
            _externalPendingCert = null;

            _securityContext?.Dispose();
            _securityContext = null;

            // Disposes the underlying SafeSocketHandle as well (ownership transferred at Create).
            _socket?.Dispose();
            _socket = null;
            _socketHandle = null;

            if (_ownsOptions)
            {
                _options.Dispose();
            }

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
            if (_socketInBuf != null)
            {
                ArrayPool<byte>.Shared.Return(_socketInBuf);
                _socketInBuf = null;
            }
        }
    }
}
