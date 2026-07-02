// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Net.Security
{
    /// <summary>
    /// Long-lived TLS configuration. Wraps an <see cref="SslAuthenticationOptions"/>
    /// constructed from either <see cref="SslClientAuthenticationOptions"/> or
    /// <see cref="SslServerAuthenticationOptions"/>. Role (client vs. server) is
    /// determined by which factory is used.
    /// </summary>
    /// <remarks>
    /// Holds the resolved options bag. Multi-connection sharing / session
    /// cache reuse is not yet wired through; each <see cref="TlsSession"/>
    /// gets its own native context allocated lazily on the first handshake call.
    /// </remarks>
    [Experimental(Experimentals.LowLevelTlsDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    public sealed partial class TlsContext : IDisposable
    {
        private readonly SslAuthenticationOptions _options;
        private readonly bool _ownsOptions;
        private readonly bool _shareOptions;
        private readonly bool _templateHasServerOptions;

        // SChannel credentials handle (an SSPI CredHandle from AcquireCredentialsHandle).
        // Owned by TlsContext so it can be shared across multiple TlsSession instances.
        // In wedge mode (WrapShared) SslStream owns the lifetime and we skip disposing
        // here to avoid double-free. Stays null on Unix — the OpenSSL SSL_CTX equivalent
        // lives in TlsContext.OpenSsl.cs.
        internal SafeFreeCredentials? CredentialsHandle;

        private TlsContext(SslAuthenticationOptions options, bool ownsOptions, bool shareOptions, bool templateHasServerOptions)
        {
            _options = options;
            _ownsOptions = ownsOptions;
            _shareOptions = shareOptions;
            _templateHasServerOptions = templateHasServerOptions;
        }

        internal SslAuthenticationOptions Options => _options;

        // Internal accessor for the obsolete EncryptionPolicy carried in options. Not exposed
        // publicly: a brand-new type should not re-publish a SYSLIB0040-obsolete concept. The
        // setting is honored at handshake time via the options bag; internal consumers that
        // need to introspect it (e.g. SslStream when re-platformed on TlsSession) read it here.
        internal EncryptionPolicy EncryptionPolicy => _options.EncryptionPolicy;

        // True when sessions should reuse the context's options bag directly (wedge mode).
        // False when each session must take a private clone before mutating any field.
        internal bool ShareOptions => _shareOptions;

        // True if the template was constructed with non-null server options. Sessions seed
        // their own per-session HasServerOptions from this and flip it in SetServerOptions.
        internal bool TemplateHasServerOptions => _templateHasServerOptions;

        // Returns a per-session options bag. For normal contexts each call returns a fresh
        // clone of the template so session-scoped mutations (TargetHost, SafeSslHandle,
        // resolved server cert, ...) don't leak between sessions. In wedge mode the bag is
        // owned by SslStream and we hand it out by reference. On platforms that own a
        // long-lived native context (e.g. OpenSSL SSL_CTX), the platform partial stamps it
        // onto the returned bag so the PAL can reuse it across sessions.
        internal SslAuthenticationOptions CreateSessionOptions()
        {
            SslAuthenticationOptions sessionOptions = _shareOptions ? _options : _options.Clone();
            sessionOptions.ForceSyncPal = true;
            AttachSharedNativeContext(sessionOptions);
            return sessionOptions;
        }

        // Platform hook: lets the OpenSSL partial attach the TlsContext-owned SSL_CTX to
        // the per-session options bag. No-op on Windows (which uses CredentialsHandle) and
        // on macOS/iOS/Android (no reusable native context to share).
        partial void AttachSharedNativeContext(SslAuthenticationOptions sessionOptions);

        // Platform hook: lets the OpenSSL partial dispose the owned SSL_CTX. No-op elsewhere.
        partial void DisposeNativeContext();

        public bool IsServer => _options.IsServer;

        /// <summary>
        /// Creates a server-side TLS context.
        /// </summary>
        /// <param name="options">
        /// The server authentication options, or <see langword="null"/> to defer
        /// configuration. When null, the first <see cref="TlsSession.ProcessHandshake"/>
        /// call on a session built from this context returns
        /// <see cref="TlsOperationStatus.NeedsServerOptions"/> with
        /// <see cref="TlsSession.ClientHelloInfo"/> populated; the caller must then
        /// invoke <see cref="TlsSession.SetServerOptions"/> before continuing the
        /// handshake. Useful for SNI-based options selection that involves I/O.
        /// </param>
        public static TlsContext Create(SslServerAuthenticationOptions? options)
        {
            SslAuthenticationOptions bag = new SslAuthenticationOptions();
            if (options is null)
            {
                bag.IsServer = true;
                return new TlsContext(bag, ownsOptions: true, shareOptions: false, templateHasServerOptions: false);
            }

            bag.UpdateOptions(options);
            return new TlsContext(bag, ownsOptions: true, shareOptions: false, templateHasServerOptions: true);
        }

        /// <summary>
        /// Creates a client-side TLS context.
        /// </summary>
        /// <remarks>
        /// Peer certificate validation always runs outside the TLS state machine: after the
        /// handshake reaches the point at which the peer cert is available, <see cref="TlsSession.ProcessHandshake"/>
        /// returns <see cref="TlsOperationStatus.NeedsCertificateValidation"/> and the caller
        /// must record a result via <see cref="TlsSession.SetRemoteCertificateValidationResult(System.Net.Security.SslPolicyErrors)"/>
        /// or <see cref="TlsSession.AcceptWithDefaultValidation"/>. Any
        /// <see cref="SslClientAuthenticationOptions.RemoteCertificateValidationCallback"/> set on
        /// <paramref name="options"/> is invoked only by <see cref="TlsSession.AcceptWithDefaultValidation"/>.
        /// </remarks>
        public static TlsContext Create(SslClientAuthenticationOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            SslAuthenticationOptions bag = new SslAuthenticationOptions();
            bag.UpdateOptions(options);
            return new TlsContext(bag, ownsOptions: true, shareOptions: false, templateHasServerOptions: false);
        }

        // Used by SslStream's TlsSession wedge: share the existing options bag so
        // SNI / client-cert selection results made by SslStream are visible to the
        // TlsSession-driven PAL calls, and to avoid double Dispose on the bag.
        internal static TlsContext WrapShared(SslAuthenticationOptions sharedOptions)
        {
            Debug.Assert(sharedOptions != null);
            return new TlsContext(sharedOptions, ownsOptions: false, shareOptions: true, templateHasServerOptions: sharedOptions.IsServer);
        }

        public void Dispose()
        {
            if (_ownsOptions)
            {
                CredentialsHandle?.Dispose();
                CredentialsHandle = null;
                DisposeNativeContext();
                _options.Dispose();
            }
        }
    }
}
