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
    /// Windows (SChannel). Provides <see cref="TlsBufferSession.Handshake"/>,
    /// <see cref="TlsBufferSession.Write"/>, <see cref="TlsBufferSession.Read"/>, and a pending-output queue.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The session never performs any I/O. The caller drives ciphertext in/out
    /// via byte spans. Any ciphertext the TLS layer needs to send (handshake
    /// records, alerts, encrypted application data) is staged in an internal
    /// pending-output buffer and drained via <see cref="TlsBufferSession.DrainPendingOutput"/>.
    /// </para>
    /// <para>
    /// Contract: any operation may return <see cref="TlsOperationStatus.DestinationTooSmall"/>
    /// to indicate the caller must drain pending output before further progress
    /// is possible. The session does not consume new input while pending output
    /// is non-empty.
    /// </para>
    /// </remarks>
    [Experimental(Experimentals.LowLevelTlsDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    public abstract partial class TlsSession : IDisposable
    {
        // Matches StreamSizes.Default on Unix; conservative upper bound for a
        // single TLS record's plaintext payload.
        internal const int MaxRecordPlaintext = 16354;

        // Nullable until SetContext is called. All operations that depend on a
        // configured context validate this at entry.
        private TlsContext? _context;
        private SslAuthenticationOptions _options = null!;
        private bool _ownsOptions;
        private bool _hasServerOptions;
        private TlsSecurityContext? _securityContext;

        private ArrayBuffer _pendingBuffer = new ArrayBuffer(initialSize: 0, usePool: true);

        // Server-side only: SNI-resolved host name captured from the client's
        // ClientHello. Kept session-local so parallel sessions built from a
        // deferred TlsContext (SNI-dispatching bootstrap) cannot race on the
        // shared _options bag. Empty until the first ClientHello has parsed.
        // On client-side sessions the target host lives on _options.TargetHost
        // (immutable after SetContext, set by the caller via
        // SslClientAuthenticationOptions).
        private string _sessionTargetHost = string.Empty;

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
        private byte[]? _clientHelloBytesBuffered;
        // Session-local credentials handle. Non-null once SetClientCertificateContext
        // has been called; from that point on, this session's PAL calls route through
        // ActiveCredentialsRef() and never touch the shared TlsContext.CredentialsHandle.
        // Disposed when the session is disposed.
        private SafeFreeCredentials? _sessionCredentialsHandle;

        // Session-local view of the CertificateContext. Initialized from _options at
        // SetContext time and every mutation (SetClientCertificateContext, the
        // server-side selector path) routes through SessionCertificateContext so parallel
        // sessions built from the same TlsContext template never race on the shared cert
        // slot. _ownsSessionCertificateContext tracks whether the session itself built
        // this context (only true when constructed via SslStreamCertificateContext.Create
        // in the server-cert-selector path); Dispose releases it iff owned. The PAL
        // signature still reads _options.CertificateContext, so the setter mirrors the
        // new value onto the per-session cloned options bag; that mirror is the single
        // point that goes away when the PAL is later reshaped to consume the session
        // directly.
        private SslStreamCertificateContext? _sessionCertificateContext;
        private bool _ownsSessionCertificateContext;

        private SslStreamCertificateContext? SessionCertificateContext => _sessionCertificateContext;

        private void SetSessionCertificateContext(SslStreamCertificateContext? context, bool takeOwnership)
        {
            if (_ownsSessionCertificateContext && _sessionCertificateContext is not null && !ReferenceEquals(_sessionCertificateContext, context))
            {
                _sessionCertificateContext.ReleaseResources();
            }

            _sessionCertificateContext = context;
            _ownsSessionCertificateContext = takeOwnership && context is not null;

            // Mirror onto the per-session cloned options bag so the PAL (which reads
            // _options.CertificateContext directly) sees the effective value. Also flip
            // the bag's own ownership bit off — session now owns the disposal decision.
            _options.CertificateContext = context;
            _options.OwnsCertificateContext = false;
        }
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
        private ArrayBuffer _socketInBuffer = new ArrayBuffer(initialSize: 0, usePool: true);

        private protected TlsSession()
        {
        }

        // Called by TlsSocketSession's constructor to bind a socket handle before the
        // session receives any TlsContext. The socket is taken to ownership and disposed
        // with the session. Platforms with a native fd-binding fast path (OpenSSL) take
        // the socket directly; otherwise the socket is wrapped in a managed Socket for
        // the buffered I/O path.
        internal void AttachSocket(SafeSocketHandle socket)
        {
            Debug.Assert(socket != null);
            _socketHandle = socket;

            bool nativeBindingEnabled = false;
            EnableNativeSocketBinding(socket, ref nativeBindingEnabled);
            if (!nativeBindingEnabled)
            {
                _socket = new Socket(socket);
            }
        }

        internal SafeSocketHandle? SocketHandle => _socketHandle;

        private void InitializeFromContext(TlsContext context)
        {
            Debug.Assert(_context is null);
            _context = context;
            _ownsOptions = !context.ShareOptions;
            _options = context.CreateSessionOptions();
            _hasServerOptions = context.TemplateHasServerOptions;

            // Transfer CertificateContext ownership from the per-session options clone
            // to the session so subsequent mutations (SetClientCertificateContext,
            // server-selector build) live entirely on TlsSession's own fields. The bag
            // itself never owns after this point; TlsSession.Dispose is the sole releaser.
            _sessionCertificateContext = _options.CertificateContext;
            _ownsSessionCertificateContext = _options.OwnsCertificateContext;
            _options.OwnsCertificateContext = false;

            OnContextInitialized();
        }

        internal virtual void OnContextInitialized()
        {
        }


        // ── State ─────────────────────────────────────────────────────────

        public bool IsHandshakeComplete => _isHandshakeComplete;

        public bool HasPendingOutput => _pendingBuffer.ActiveLength > 0;

        /// <summary>
        /// Target host name for this session. On the client this is the value the
        /// caller supplied via <see cref="SslClientAuthenticationOptions.TargetHost"/>
        /// (used for SNI and hostname validation). On the server this is the SNI value
        /// parsed from the peer's ClientHello, or <see langword="null"/> if no
        /// ClientHello has been processed yet or the ClientHello carried no SNI
        /// extension. Setting the value on either side overrides the current value;
        /// setting to <see langword="null"/> clears it.
        /// </summary>
        public string? TargetHostName
        {
            get
            {
                ThrowIfContextNotSet();
                if (_context!.IsServer)
                {
                    return string.IsNullOrEmpty(_sessionTargetHost) ? null : _sessionTargetHost;
                }
                return string.IsNullOrEmpty(_options.TargetHost) ? null : _options.TargetHost;
            }
            set
            {
                ThrowIfContextNotSet();
                if (_context!.IsServer)
                {
                    _sessionTargetHost = value ?? string.Empty;
                }
                else
                {
                    _options.TargetHost = value ?? string.Empty;
                }
            }
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
        /// external validation result (after <see cref="TlsBufferSession.Handshake"/> returned
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
        /// Must be called only after <see cref="TlsBufferSession.Handshake"/> returned
        /// <see cref="TlsOperationStatus.NeedsCertificateValidation"/> and before
        /// <see cref="SetRemoteCertificateValidationResult"/> is called.
        /// </remarks>
        public SslPolicyErrors AcceptWithDefaultValidation()
        {
            ThrowIfDisposed();
            if (!_externalValidationPending)
            {
                throw new InvalidOperationException(
                    $"{nameof(AcceptWithDefaultValidation)} can only be called when certificate validation is pending.");
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
        /// subsequent calls to <see cref="TlsBufferSession.Handshake"/>, <see cref="TlsBufferSession.Write"/>,
        /// and <see cref="TlsBufferSession.Read"/> to throw <see cref="AuthenticationException"/>.
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
                    $"{nameof(SetRemoteCertificateValidationResult)} can only be called when certificate validation is pending.");
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
        /// Server-side only. The parsed ClientHello information, populated once the
        /// ClientHello has been received and stays populated for the lifetime of the
        /// session. Returns <see langword="null"/> before the ClientHello arrives,
        /// on client-side sessions, and on server sessions where ClientHello capture
        /// was disabled via the <c>System.Net.Security.CaptureClientHello</c> AppContext
        /// switch AND options were supplied at <see cref="TlsContext"/> creation time.
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
        /// Server-side only. Returns the number of bytes in the captured raw ClientHello
        /// record (5-byte TLS record header plus the ClientHello handshake message), or
        /// 0 if unavailable. Callers use this to size a destination buffer for
        /// <see cref="TryGetClientHelloBytes"/>.
        /// </summary>
        /// <remarks>
        /// The ClientHello is only captured on server-side sessions and requires the
        /// <c>System.Net.Security.CaptureClientHello</c> AppContext switch to be enabled
        /// (default true). Returns 0 on client-side sessions, before the ClientHello has
        /// been received, or when capture has been disabled.
        /// </remarks>
        public int GetClientHelloLength()
        {
            ThrowIfDisposed();

            ReadOnlySpan<byte> native = default;
            TryGetNativeClientHelloBytes(ref native);
            if (!native.IsEmpty)
            {
                return native.Length;
            }

            return _clientHelloBytesBuffered?.Length ?? 0;
        }

        /// <summary>
        /// Server-side only. Copies the captured raw ClientHello record into
        /// <paramref name="destination"/>. Returns <see langword="true"/> when the full
        /// record was written; <see langword="false"/> if the destination is too small
        /// or the ClientHello is not available.
        /// </summary>
        /// <param name="destination">Buffer that receives the ClientHello bytes.</param>
        /// <param name="bytesWritten">Number of bytes copied. Zero when the method returns false.</param>
        public bool TryGetClientHelloBytes(Span<byte> destination, out int bytesWritten)
        {
            ThrowIfDisposed();

            ReadOnlySpan<byte> source = default;
            TryGetNativeClientHelloBytes(ref source);
            if (source.IsEmpty)
            {
                if (_clientHelloBytesBuffered is null)
                {
                    bytesWritten = 0;
                    return false;
                }
                source = _clientHelloBytesBuffered;
            }

            if (destination.Length < source.Length)
            {
                bytesWritten = 0;
                return false;
            }

            source.CopyTo(destination);
            bytesWritten = source.Length;
            return true;
        }

        /// <summary>
        /// Assigns a <see cref="TlsContext"/> to this session. Must be called at least
        /// once before <see cref="TlsBufferSession.Handshake"/> or its socket-bound
        /// equivalent can make forward progress. May also be called on a server-side
        /// session that suspended with <see cref="TlsOperationStatus.NeedsTlsContext"/>
        /// to steer it onto the resolved per-tenant context.
        /// </summary>
        /// <param name="context">A fully-configured <see cref="TlsContext"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown when supplying a resolved context after
        /// <see cref="TlsOperationStatus.NeedsTlsContext"/> and the passed context is
        /// not server-side.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the session already has a context and is not currently awaiting
        /// server options (i.e., the caller tried to swap a context that was already
        /// fully configured).
        /// </exception>
        public void SetContext(TlsContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            ThrowIfDisposed();

            if (_context is null)
            {
                InitializeFromContext(context);
                return;
            }

            if (!_context!.IsServer)
            {
                throw new InvalidOperationException("SetContext can only be called on a server-side session.");
            }
            if (!context.IsServer)
            {
                throw new ArgumentException("TlsContext must be server-side.", nameof(context));
            }
            if (_hasServerOptions)
            {
                throw new InvalidOperationException("Server options were already supplied when the TlsContext was created.");
            }
            if (_clientHelloInfo is null)
            {
                throw new InvalidOperationException("SetContext can only be called after Handshake returned NeedsTlsContext.");
            }

            // Ask the supplied context for a session-options bag — this allocates its
            // long-lived SSL_CTX (if not already) and stamps PreallocatedSslContext on
            // the returned bag. Copy those fields (including PreallocatedSslContext) into
            // our session's options so subsequent AllocateSslHandle picks up the passed
            // context's SSL_CTX instead of falling back to the per-session cache path.
            SslAuthenticationOptions serverOpts = context.CreateSessionOptions();
            _options.CopyFrom(serverOpts);
#if !TARGET_WINDOWS && !SYSNETSECURITY_NO_OPENSSL
            _options.PreallocatedSslContext = serverOpts.PreallocatedSslContext;
#endif

            // CopyFrom sets _options.OwnsCertificateContext = false and copies the
            // template's CertificateContext reference into the bag. Re-seat our session
            // ownership from the freshly-copied serverOpts (the new template may have
            // brought its own owned context via ServerCertificate), releasing any prior
            // session-owned context. serverOpts itself is a per-session clone that Owns
            // = false, so its live-owner is the source TlsContext template that stays
            // alive across sessions — hence takeOwnership: false here.
            SetSessionCertificateContext(_options.CertificateContext, takeOwnership: false);

            _hasServerOptions = true;

            // The per-tenant options differ from the bootstrap context's template, so
            // credentials must be session-local. Otherwise EnsureCredentialsAcquired
            // would stamp this session's SChannel cred handle into the shared bootstrap
            // TlsContext.CredentialsHandle, and every subsequent session on the same
            // bootstrap would inherit those credentials regardless of which tenant it
            // resolved to (SChannel-only; OpenSSL routes per-tenant SSL_CTX via
            // PreallocatedSslContext on the session-local options bag). Acquire eagerly
            // so any AcquireCredentialsHandle failure surfaces from SetContext,
            // not from an opaque PAL call downstream.
            _sessionCredentialsHandle?.Dispose();
            _sessionCredentialsHandle = SslStreamPal.AcquireCredentialsHandle(_options, false);

            OnServerContextSet();
        }

        /// <summary>
        /// Client-side only. Supplies the certificate context the session should send
        /// in response to the server's CertificateRequest, or <see langword="null"/> to
        /// decline. Intended to resolve a session suspended on
        /// <see cref="TlsOperationStatus.CertificateRequested"/>: callers that need to
        /// fetch a certificate from an out-of-process source (e.g. a key vault) do so
        /// outside the session, then resume the handshake. May also be called before
        /// the first handshake call to seed the client credential when the
        /// <see cref="TlsContext"/> was created without one.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown on a server-side session, or before <see cref="SetContext"/> has been
        /// called.
        /// </exception>
        public void SetClientCertificateContext(SslStreamCertificateContext? context)
        {
            ThrowIfDisposed();
            ThrowIfContextNotSet();

            if (_context!.IsServer)
            {
                throw new InvalidOperationException("SetClientCertificateContext can only be called on a client-side session.");
            }
            SetSessionCertificateContext(context, takeOwnership: false);

            // Acquire a session-local credentials handle so we don't touch the shared
            // TlsContext.CredentialsHandle, which is used by any concurrent session on
            // the same context (racing/disposing it can cause handshake failures or
            // deliver the wrong certificate on SChannel). ActiveCredentialsRef() will
            // return this session-local handle for subsequent PAL calls. Acquire eagerly
            // so any AcquireCredentialsHandle failure surfaces here, not from an opaque
            // PAL call downstream.
            _sessionCredentialsHandle?.Dispose();
            _sessionCredentialsHandle = SslStreamPal.AcquireCredentialsHandle(_options, false);
            _resumeAfterCredentials = true;
        }

        /// <summary>
        /// Client-side only. Returns the distinguished names of the certificate authorities
        /// the server listed in its TLS 1.2 <c>CertificateRequest</c> or TLS 1.3
        /// <c>certificate_authorities</c> extension. Intended to be called while the session
        /// is suspended on <see cref="TlsOperationStatus.CertificateRequested"/> so the caller can
        /// pick a client certificate that chains to one of the listed CAs. Returns
        /// <see langword="null"/> when no security context exists yet, when the peer sent no
        /// hints, or on a server-side session.
        /// </summary>
        public IReadOnlyList<string>? GetAcceptableIssuers()
        {
            ThrowIfDisposed();

            if (_context is null || _context.IsServer || _securityContext is null)
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
                if (_context!.IsServer)
                {
                    return SessionCertificateContext?.TargetCertificate;
                }

                if (_securityContext == null || _securityContext.IsInvalid)
                {
                    return null;
                }

                if (!CertificateValidationPal.IsLocalCertificateUsed(ActiveCredentialsRef(), _securityContext))
                {
                    return null;
                }

                return SessionCertificateContext?.TargetCertificate;
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

        private protected TlsOperationStatus HandshakeBufferedCore(
            ReadOnlySpan<byte> input,
            Span<byte> output,
            out int bytesConsumed,
            out int bytesWritten)
        {
            ThrowIfDisposed();
            ThrowIfContextNotSet();
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

            if (_clientHelloInfo is not null && !_hasServerOptions)
            {
                // The caller previously saw NeedsServerOptions but hasn't supplied options yet.
                return TlsOperationStatus.NeedsTlsContext;
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
            if (_pendingBuffer.ActiveLength > 0)
            {
                bytesWritten = DrainTo(output);
                return _pendingBuffer.ActiveLength > 0 ? TlsOperationStatus.DestinationTooSmall : TlsOperationStatus.Complete;
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
            bool isInitialClientCall = !_context!.IsServer && _securityContext is null;
            bool isCredentialResume = _resumeAfterCredentials;
            _resumeAfterCredentials = false;
            if (!isInitialClientCall && !isCredentialResume)
            {
                if (input.Length < TlsFrameHelper.HeaderSize)
                {
                    return TlsOperationStatus.NeedMoreData;
                }

                TlsFrameHeader frameHeader = default;
                if (!TlsFrameHelper.TryGetFrameHeader(input, ref frameHeader))
                {
                    throw new IOException(SR.net_io_decrypt);
                }

                if (input.Length < frameHeader.Length)
                {
                    return TlsOperationStatus.NeedMoreData;
                }
            }

            ProtocolToken token = default;
            token.RentBuffer = true;
            try
            {
                if (_context!.IsServer)
                {
                    // Parse and capture the ClientHello managed-side so the ClientHelloInfo /
                    // TargetHostName / GetClientHelloBytes surface is consistent across paths.
                    // We check on every call while _clientHelloBytesBuffered is null because the
                    // first ProcessHandshake call may pass only a partial CH record - OpenSSL will
                    // allocate _securityContext even on partial input and return WantRead, so we
                    // can't rely on _securityContext being null as our re-entry gate.
                    if (_clientHelloBytesBuffered is null)
                    {
                        SslClientHelloInfo? parsed = TryParseClientHello(input, out int frameLength);
                        if (parsed is not null)
                        {
                            _clientHelloInfo = parsed;
                            if (!string.IsNullOrEmpty(parsed.Value.ServerName))
                            {
                                _sessionTargetHost = parsed.Value.ServerName;
                            }
                            if (frameLength > 0 && frameLength <= input.Length)
                            {
                                _clientHelloBytesBuffered = input.Slice(0, frameLength).ToArray();
                            }
                            // If frameLength is out of range (shouldn't happen after a successful
                            // parse), silently skip capture; the session continues to work,
                            // GetClientHelloLength just reports 0.
                        }
                        else if (_securityContext is null)
                        {
                            // No CH parse-able yet and no PAL context yet - wait for more bytes.
                            return TlsOperationStatus.NeedMoreData;
                        }
                    }

                    // On the very first server-side call, inspect the incoming
                    // ClientHello to surface SNI (TargetHost) and, if the caller
                    // supplied a ServerCertificateSelectionCallback, resolve the
                    // server certificate from it before AllocateSslHandle runs.
                    if (_securityContext is null)
                    {
                        if (!_hasServerOptions)
                        {
                            // Deferred / SNI-callback flow: caller resolves via SetContext.
                            // Leave input unconsumed; the caller re-feeds the same bytes on resume.
                            return TlsOperationStatus.NeedsTlsContext;
                        }

                        bool needsCertResolution =
                            SessionCertificateContext is null &&
                            _options.ServerCertSelectionDelegate is not null;

                        if (needsCertResolution && !ResolveServerCertificateFromClientHello(input))
                        {
                            // Need more bytes to parse the ClientHello (and run the
                            // ServerCertificateSelectionCallback).
                            return TlsOperationStatus.NeedMoreData;
                        }
                    }

                    EnsureCredentialsAcquired();

                    token = SslStreamPal.AcceptSecurityContext(
                        ref ActiveCredentialsRef(),
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
                        ref ActiveCredentialsRef(),
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
                        _context!.CredentialsHandle,
                        _securityContext!,
                        _options,
                        rawAlpn);

                    if (selStatus.ErrorCode != SecurityStatusPalErrorCode.OK)
                    {
                        throw new AuthenticationException(SR.net_auth_SSPI, selStatus.Exception);
                    }

                    token.ReleasePayload();

                    if (_context!.IsServer)
                    {
                        token = SslStreamPal.AcceptSecurityContext(
                            ref ActiveCredentialsRef(),
                            ref _securityContext,
                            ReadOnlySpan<byte>.Empty,
                            out _,
                            _options);
                    }
                    else
                    {
                        string hostName = TargetHostNameHelper.NormalizeHostName(_options.TargetHost);
                        token = SslStreamPal.InitializeSecurityContext(
                            ref ActiveCredentialsRef(),
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
                    if (_pendingBuffer.ActiveLength > 0)
                    {
                        bytesWritten = DrainTo(output);
                        _externalValidationFault = authExc;
                        return TlsOperationStatus.DestinationTooSmall;
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

                if (_pendingBuffer.ActiveLength > 0)
                {
                    bytesWritten = DrainTo(output);
                    if (_pendingBuffer.ActiveLength > 0)
                    {
                        return TlsOperationStatus.DestinationTooSmall;
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
                    return TlsOperationStatus.CertificateRequested;
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

                return TlsOperationStatus.NeedMoreData;
            }
            finally
            {
                token.ReleasePayload();
            }
        }

        private protected TlsOperationStatus WriteBufferedCore(
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

            if (_pendingBuffer.ActiveLength > 0)
            {
                bytesWritten = DrainTo(ciphertext);
                return _pendingBuffer.ActiveLength > 0 ? TlsOperationStatus.DestinationTooSmall : TlsOperationStatus.Complete;
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
            return _pendingBuffer.ActiveLength > 0 ? TlsOperationStatus.DestinationTooSmall : TlsOperationStatus.Complete;
        }

        // ── Decrypt ───────────────────────────────────────────────────────

        private protected TlsOperationStatus ReadBufferedCore(
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

            if (_pendingBuffer.ActiveLength > 0)
            {
                // Caller must drain before we accept new input.
                return TlsOperationStatus.DestinationTooSmall;
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
                return TlsOperationStatus.NeedMoreData;
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
                return TlsOperationStatus.NeedMoreData;
            }

            int produced = decBytesWritten + decLeftoverLength;
            if (produced == 0)
            {
                return TlsOperationStatus.NeedMoreData;
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
        /// must then continue normal <see cref="TlsBufferSession.Read"/> / <see cref="TlsBufferSession.Write"/>
        /// operations; OpenSSL processes the peer's response transparently
        /// inside subsequent <c>SSL_read</c> calls. Once the peer's
        /// certificate has been received, it becomes observable via
        /// <see cref="GetRemoteCertificate"/>.
        /// </para>
        /// </remarks>
        private protected TlsOperationStatus RequestClientCertificateBufferedCore(Span<byte> ciphertext, out int bytesWritten)
        {
            ThrowIfDisposed();
            bytesWritten = 0;

#if TARGET_APPLE
            // SecureTransport does not expose a post-handshake client-authentication
            // path, and Network.framework does not provide renegotiation primitives.
            throw new PlatformNotSupportedException(SR.net_ssl_renegotiate_not_supported);
#else
            if (!_context!.IsServer)
            {
                throw new InvalidOperationException("RequestClientCertificate can only be invoked on a server session.");
            }

            if (!_isHandshakeComplete || _securityContext == null || _securityContext.IsInvalid)
            {
                throw new InvalidOperationException("Handshake has not yet completed.");
            }

            if (_pendingBuffer.ActiveLength == 0)
            {
                ProtocolToken token = SslStreamPal.Renegotiate(
                    ref ActiveCredentialsRef(),
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
            return _pendingBuffer.ActiveLength > 0 ? TlsOperationStatus.DestinationTooSmall : TlsOperationStatus.Complete;
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
        /// Returns <see cref="TlsOperationStatus.DestinationTooSmall"/> if the caller must
        /// drain more output before the shutdown record is fully written;
        /// otherwise <see cref="TlsOperationStatus.Closed"/> once all bytes have
        /// been handed to the caller.
        /// </remarks>
        private protected TlsOperationStatus ShutdownBufferedCore(Span<byte> ciphertext, out int bytesWritten)
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
                    if (_context!.IsServer)
                    {
                        token = SslStreamPal.AcceptSecurityContext(
                            ref ActiveCredentialsRef(),
                            ref _securityContext,
                            ReadOnlySpan<byte>.Empty,
                            out _,
                            _options);
                    }
                    else
                    {
                        string hostName = TargetHostNameHelper.NormalizeHostName(_options.TargetHost);
                        token = SslStreamPal.InitializeSecurityContext(
                            ref ActiveCredentialsRef(),
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
            return _pendingBuffer.ActiveLength > 0 ? TlsOperationStatus.DestinationTooSmall : TlsOperationStatus.Closed;
        }

        // ── Pending output ────────────────────────────────────────────────

        private protected TlsOperationStatus DrainPendingOutputCore(Span<byte> ciphertext, out int bytesWritten)
        {
            ThrowIfDisposed();
            bytesWritten = DrainTo(ciphertext);
            return _pendingBuffer.ActiveLength > 0 ? TlsOperationStatus.DestinationTooSmall : TlsOperationStatus.Complete;
        }

        // ── Internals ─────────────────────────────────────────────────────

        private void AppendPending(ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty)
            {
                return;
            }

            _pendingBuffer.EnsureAvailableSpace(data.Length);
            data.CopyTo(_pendingBuffer.AvailableSpan);
            _pendingBuffer.Commit(data.Length);
        }

        private int DrainTo(Span<byte> output)
        {
            int n = Math.Min(output.Length, _pendingBuffer.ActiveLength);
            if (n == 0)
            {
                return 0;
            }

            _pendingBuffer.ActiveSpan.Slice(0, n).CopyTo(output);
            _pendingBuffer.Discard(n);
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

        private void ThrowIfContextNotSet()
        {
            if (_context is null)
            {
                throw new InvalidOperationException(SR.net_ssl_tlssession_context_not_set);
            }
        }

        // Server-side: parses the ClientHello and returns a populated
        // SslClientHelloInfo (SNI + supported versions), or null if more bytes
        // are needed or the record is not a ClientHello. Used by the
        // deferred-options path; does not mutate session state.
        private static SslClientHelloInfo? TryParseClientHello(ReadOnlySpan<byte> input, out int frameLength)
        {
            frameLength = 0;
            TlsFrameHelper.TlsFrameInfo frameInfo = default;
            if (!TlsFrameHelper.TryGetFrameInfo(input, ref frameInfo))
            {
                return null;
            }

            if (frameInfo.HandshakeType != TlsHandshakeType.ClientHello)
            {
                return null;
            }

            frameLength = frameInfo.Header.Length;
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
                _sessionTargetHost = frameInfo.TargetName;
            }

            ServerCertificateSelectionCallback? selector = _options.ServerCertSelectionDelegate;
            if (selector is null || SessionCertificateContext is not null)
            {
                return true;
            }

            X509Certificate? selected = selector(this, _sessionTargetHost);
            if (selected is null)
            {
                throw new AuthenticationException(SR.net_ssl_io_no_server_cert);
            }

            X509Certificate2? withKey = SslStream.FindCertificateWithPrivateKey(this, isServer: true, selected);
            if (withKey is null)
            {
                throw new AuthenticationException(SR.net_ssl_io_no_server_cert);
            }

            SetSessionCertificateContext(
                SslStreamCertificateContext.Create(withKey, additionalCertificates: null, offline: false, trust: null, noOcspFetch: true),
                takeOwnership: true);
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
        internal TlsContext Context => _context!;
        internal SafeFreeCredentials? CredentialsHandle
        {
            get => ActiveCredentialsRef();
            set
            {
                if (_sessionCredentialsHandle is not null)
                {
                    _sessionCredentialsHandle = value;
                }
                else
                {
                    _context!.CredentialsHandle = value;
                }
            }
        }

        // Returns a ref to the credentials handle this session should use for its next
        // PAL call. When _sessionCredentialsHandle is set (via SetClientCertificateContext),
        // it takes precedence; otherwise the shared TlsContext.CredentialsHandle is used.
        // Class instance refs have unrestricted lifetime, no [UnscopedRef] needed.
        private ref SafeFreeCredentials? ActiveCredentialsRef()
            => ref (_sessionCredentialsHandle is not null
                    ? ref _sessionCredentialsHandle
                    : ref _context!.CredentialsHandle);

        // SslStream's GenerateToken replacement. Drives one ASC/ISC step via PAL and
        // updates internal handshake-complete state. Returns the raw PAL token so the
        // caller can preserve existing ProtocolToken-based plumbing (alerts, error
        // mapping, NetEventSource).
        internal ProtocolToken HandshakeStepForSslStream(ReadOnlySpan<byte> input, out int bytesConsumed)
        {
            ThrowIfDisposed();

            ProtocolToken token;
            if (_context!.IsServer)
            {
                token = SslStreamPal.AcceptSecurityContext(
                    ref ActiveCredentialsRef(),
                    ref _securityContext,
                    input,
                    out bytesConsumed,
                    _options);
            }
            else
            {
                string hostName = TargetHostNameHelper.NormalizeHostName(_options.TargetHost);
                token = SslStreamPal.InitializeSecurityContext(
                    ref ActiveCredentialsRef(),
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
            // If SetContext or SetClientCertificateContext already produced a
            // session-local handle, ActiveCredentialsRef() will route the PAL
            // through it. Skip touching the shared TlsContext.CredentialsHandle
            // to avoid racing with concurrent sessions on the same context.
            if (_sessionCredentialsHandle is not null)
            {
                return;
            }

            if (_context!.CredentialsHandle is not null)
            {
                return;
            }

            // Multiple sessions on the same TlsContext can call EnsureCredentialsAcquired
            // concurrently and each see CredentialsHandle == null. Atomically install
            // ours; if another session beat us to it, dispose the loser to avoid a leak.
            // Non-Windows PALs return null here (OpenSSL has no cred handle concept); the
            // CompareExchange is a no-op in that case.
            SafeFreeCredentials? acquired = SslStreamPal.AcquireCredentialsHandle(_options, false);
            if (System.Threading.Interlocked.CompareExchange(ref _context!.CredentialsHandle, acquired, null) is not null)
            {
                acquired?.Dispose();
            }
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
                if (_context!.IsServer)
                {
                    token = SslStreamPal.AcceptSecurityContext(
                        ref ActiveCredentialsRef(),
                        ref _securityContext,
                        data,
                        out _,
                        _options);
                }
                else
                {
                    string hostName = TargetHostNameHelper.NormalizeHostName(_options.TargetHost);
                    token = SslStreamPal.InitializeSecurityContext(
                        ref ActiveCredentialsRef(),
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
            while (_pendingBuffer.ActiveLength > 0)
            {
                int sent = _socket!.Send(
                    _pendingBuffer.ActiveReadOnlySpan,
                    SocketFlags.None,
                    out SocketError err);
                lastError = err;
                if (sent > 0)
                {
                    _pendingBuffer.Discard(sent);
                    if (_pendingBuffer.ActiveLength == 0)
                    {
                        return true;
                    }
                    continue;
                }
                return false;
            }
            return true;
        }

        private protected TlsOperationStatus HandshakeSocketCore()
        {
            ThrowIfDisposed();
            ThrowIfContextNotSet();
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

            TryPeekClientHello(ref fast);
            if (fast.HasValue)
            {
                return fast.Value;
            }

            _socketInBuffer.EnsureAvailableSpace(SocketScratchSize);
            byte[] scratch = ArrayPool<byte>.Shared.Rent(SocketScratchSize);
            try
            {
                while (true)
                {
                    if (_pendingBuffer.ActiveLength > 0)
                    {
                        if (!TryDrainPendingToSocket(out SocketError drainErr))
                        {
                            if (drainErr == SocketError.WouldBlock)
                            {
                                return TlsOperationStatus.DestinationTooSmall;
                            }
                            throw new SocketException((int)drainErr);
                        }
                    }

                    TlsOperationStatus status = HandshakeBufferedCore(
                        _socketInBuffer.ActiveReadOnlySpan,
                        scratch,
                        out int consumed,
                        out int produced);

                    if (consumed > 0)
                    {
                        _socketInBuffer.Discard(consumed);
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
                                return TlsOperationStatus.DestinationTooSmall;
                            }
                            throw new SocketException((int)sendErr);
                        }
                    }

                    switch (status)
                    {
                        case TlsOperationStatus.Complete:
                            return TlsOperationStatus.Complete;

                        case TlsOperationStatus.NeedMoreData:
                            // Should not happen with conservative scratch sizing, but guard.
                            _socketInBuffer.EnsureAvailableSpace(1);
                            int received = _socket!.Receive(
                                _socketInBuffer.AvailableSpan,
                                SocketFlags.None,
                                out SocketError recvErr);
                            if (received > 0)
                            {
                                _socketInBuffer.Commit(received);
                                continue;
                            }
                            if (recvErr == SocketError.WouldBlock)
                            {
                                return TlsOperationStatus.NeedMoreData;
                            }
                            if (received == 0)
                            {
                                return TlsOperationStatus.Closed;
                            }
                            throw new SocketException((int)recvErr);

                        case TlsOperationStatus.DestinationTooSmall:
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

        private protected TlsOperationStatus ReadSocketCore(Span<byte> buffer, out int bytesRead)
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

            _socketInBuffer.EnsureAvailableSpace(SocketScratchSize);

            while (true)
            {
                if (_socketInBuffer.ActiveLength > 0)
                {
                    TlsOperationStatus status = ReadBufferedCore(
                        _socketInBuffer.ActiveReadOnlySpan,
                        buffer,
                        out int consumed,
                        out int produced);

                    if (consumed > 0)
                    {
                        _socketInBuffer.Discard(consumed);
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
                    if (status != TlsOperationStatus.NeedMoreData)
                    {
                        return status;
                    }
                    // WantRead: fall through to socket recv.
                }

                _socketInBuffer.EnsureAvailableSpace(1);
                int received = _socket!.Receive(
                    _socketInBuffer.AvailableSpan,
                    SocketFlags.None,
                    out SocketError recvErr);
                if (received > 0)
                {
                    _socketInBuffer.Commit(received);
                    continue;
                }
                if (recvErr == SocketError.WouldBlock)
                {
                    return TlsOperationStatus.NeedMoreData;
                }
                if (received == 0)
                {
                    return TlsOperationStatus.Closed;
                }
                throw new SocketException((int)recvErr);
            }
        }

        private protected TlsOperationStatus WriteSocketCore(ReadOnlySpan<byte> buffer, out int bytesWritten)
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
            if (_pendingBuffer.ActiveLength > 0)
            {
                if (!TryDrainPendingToSocket(out SocketError drainErr))
                {
                    if (drainErr == SocketError.WouldBlock)
                    {
                        return TlsOperationStatus.DestinationTooSmall;
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
                    TlsOperationStatus encStatus = WriteBufferedCore(
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
                                return TlsOperationStatus.DestinationTooSmall;
                            }
                            throw new SocketException((int)sendErr);
                        }
                    }

                    if (encStatus == TlsOperationStatus.DestinationTooSmall)
                    {
                        // Pending output owed; resume next call.
                        bytesWritten = totalConsumed;
                        return TlsOperationStatus.DestinationTooSmall;
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

        // Simple driver that runs a buffered "output-only" op (Shutdown /
        // RequestClientCertificate) and drains its staged ciphertext to the socket.
        private TlsOperationStatus DriveBufferedOpOverSocket(Func<Span<byte>, (TlsOperationStatus status, int written)> op)
        {
            ThrowIfDisposed();
            ThrowIfNotSocketBound();

            // Drain any leftover pending output before staging new bytes.
            if (_pendingBuffer.ActiveLength > 0)
            {
                if (!TryDrainPendingToSocket(out SocketError leftoverErr))
                {
                    if (leftoverErr == SocketError.WouldBlock)
                    {
                        return TlsOperationStatus.DestinationTooSmall;
                    }
                    throw new SocketException((int)leftoverErr);
                }
            }

            byte[] scratch = ArrayPool<byte>.Shared.Rent(SocketScratchSize);
            try
            {
                (TlsOperationStatus status, int written) = op(scratch);
                if (written > 0)
                {
                    int offset = 0;
                    while (offset < written)
                    {
                        int sent = _socket!.Send(
                            new ReadOnlySpan<byte>(scratch, offset, written - offset),
                            SocketFlags.None,
                            out SocketError sendErr);
                        if (sent > 0)
                        {
                            offset += sent;
                            continue;
                        }
                        if (sendErr == SocketError.WouldBlock)
                        {
                            AppendPending(new ReadOnlySpan<byte>(scratch, offset, written - offset));
                            return TlsOperationStatus.DestinationTooSmall;
                        }
                        throw new SocketException((int)sendErr);
                    }
                }
                return status;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(scratch);
            }
        }

        private protected TlsOperationStatus ShutdownSocketCore()
            => DriveBufferedOpOverSocket(dest =>
            {
                TlsOperationStatus s = ShutdownBufferedCore(dest, out int w);
                return (s, w);
            });

        private protected TlsOperationStatus RequestClientCertificateSocketCore()
            => DriveBufferedOpOverSocket(dest =>
            {
                TlsOperationStatus s = RequestClientCertificateBufferedCore(dest, out int w);
                return (s, w);
            });

        // Platform hooks. Implemented by the OpenSSL partial (TlsSession.OpenSsl.cs)
        // to bind the socket fd directly to the SSL object and drive ciphertext
        // through OpenSSL. On Windows (SChannel) these are no-ops and the buffered
        // ProcessHandshake/Encrypt/Decrypt path above is used unchanged.
        partial void EnableNativeSocketBinding(SafeSocketHandle socket, ref bool nativeBindingEnabled);
        partial void TryFastHandshake(ref TlsOperationStatus? result);
        partial void TryPeekClientHello(ref TlsOperationStatus? result);
        partial void TryFastRead(Span<byte> buffer, ref int bytesRead, ref TlsOperationStatus? result);
        partial void TryFastWrite(ReadOnlySpan<byte> buffer, ref int bytesWritten, ref TlsOperationStatus? result);

        // Fires at the end of SetContext. Platforms with a deferred-server
        // fast path (OpenSSL socket-bound sessions) use this hook to activate
        // native binding now that server options are known.
        partial void OnServerContextSet();

        // Fires from Dispose so the OpenSSL partial can release the peek BIO if the
        // session is disposed before its ownership is transferred to an SSL* handle.
        partial void OnDispose();

        // Fires from GetClientHelloBytes so the OpenSSL partial can return a span
        // over the socket-replay BIO's retained peek buffer. No-op on the buffered
        // path; the getter falls back to the managed byte[] copy.
        partial void TryGetNativeClientHelloBytes(ref ReadOnlySpan<byte> bytes);

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
            if (_socket is not null)
            {
                _socket.Dispose();
                _socket = null;
            }
            else
            {
                _socketHandle?.Dispose();
            }
            _socketHandle = null;

            if (_ownsOptions)
            {
                _options.Dispose();
            }

            _pendingBuffer.Dispose();
            if (_decryptScratch != null)
            {
                ArrayPool<byte>.Shared.Return(_decryptScratch);
                _decryptScratch = null;
            }
            _socketInBuffer.Dispose();

            // Release the session-local credentials handle acquired by
            // SetContext / SetClientCertificateContext. The shared handle on
            // _context is owned by TlsContext and released with it.
            _sessionCredentialsHandle?.Dispose();
            _sessionCredentialsHandle = null;

            // Release the session-owned CertificateContext (only true when we built
            // one via the server-cert-selector path). Caller-provided contexts and the
            // template context inherited from TlsContext are not disposed here.
            if (_ownsSessionCertificateContext && _sessionCertificateContext is not null)
            {
                _sessionCertificateContext.ReleaseResources();
            }
            _sessionCertificateContext = null;
            _ownsSessionCertificateContext = false;

            OnDispose();
        }
    }
}
