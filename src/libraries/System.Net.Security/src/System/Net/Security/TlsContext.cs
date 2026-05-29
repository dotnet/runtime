// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

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
    public sealed class TlsContext : IDisposable
    {
        private readonly SslAuthenticationOptions _options;
        private readonly bool _ownsOptions;
        private bool _hasServerOptions;

        // Credential handle is owned by TlsContext so it can be shared across multiple
        // TlsSession instances. In wedge mode (WrapShared) SslStream owns the lifetime
        // and we skip disposing here to avoid double-free; the field acts as shared
        // storage that SslStream and TlsSession both read/write via ref.
        internal SafeFreeCredentials? CredentialsHandle;

        private TlsContext(SslAuthenticationOptions options, bool ownsOptions, bool hasServerOptions)
        {
            _options = options;
            _ownsOptions = ownsOptions;
            _hasServerOptions = hasServerOptions;
        }

        internal SslAuthenticationOptions Options => _options;

        // Server-only. False if the context was created with null options and
        // SetServerOptions has not yet been called on a session.
        internal bool HasServerOptions => _hasServerOptions;

        // Applies user-supplied server options to the deferred bag. Called from
        // TlsSession.SetServerOptions once the caller has inspected the ClientHello.
        internal void ApplyServerOptions(SslServerAuthenticationOptions options)
        {
            Debug.Assert(_options.IsServer);
            Debug.Assert(!_hasServerOptions);
            _options.UpdateOptions(options);
            _hasServerOptions = true;
        }

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
                return new TlsContext(bag, ownsOptions: true, hasServerOptions: false);
            }

            bag.UpdateOptions(options);
            return new TlsContext(bag, ownsOptions: true, hasServerOptions: true);
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
            return new TlsContext(bag, ownsOptions: true, hasServerOptions: false);
        }

        // Used by SslStream's TlsSession wedge: share the existing options bag so
        // SNI / client-cert selection results made by SslStream are visible to the
        // TlsSession-driven PAL calls, and to avoid double Dispose on the bag.
        internal static TlsContext WrapShared(SslAuthenticationOptions sharedOptions)
        {
            Debug.Assert(sharedOptions != null);
            return new TlsContext(sharedOptions, ownsOptions: false, hasServerOptions: sharedOptions.IsServer);
        }

        public void Dispose()
        {
            if (_ownsOptions)
            {
                CredentialsHandle?.Dispose();
                CredentialsHandle = null;
                _options.Dispose();
            }
        }
    }
}
