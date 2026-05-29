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
    /// PoC scope: holds the resolved options bag. Multi-connection sharing /
    /// session cache reuse is not yet wired through; each <see cref="TlsSession"/>
    /// gets its own native context allocated lazily on the first handshake call.
    /// </remarks>
    public sealed class TlsContext : IDisposable
    {
        private readonly SslAuthenticationOptions _options;
        private readonly bool _ownsOptions;

        // Credential handle is owned by TlsContext so it can be shared across multiple
        // TlsSession instances. In wedge mode (WrapShared) SslStream owns the lifetime
        // and we skip disposing here to avoid double-free; the field acts as shared
        // storage that SslStream and TlsSession both read/write via ref.
        internal SafeFreeCredentials? CredentialsHandle;

        private TlsContext(SslAuthenticationOptions options, bool ownsOptions)
        {
            _options = options;
            _ownsOptions = ownsOptions;
        }

        internal SslAuthenticationOptions Options => _options;

        public bool IsServer => _options.IsServer;

        public static TlsContext Create(SslServerAuthenticationOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            SslAuthenticationOptions bag = new SslAuthenticationOptions();
            bag.UpdateOptions(options);
            return new TlsContext(bag, ownsOptions: true);
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
            return new TlsContext(bag, ownsOptions: true);
        }

        // Used by SslStream's TlsSession wedge: share the existing options bag so
        // SNI / client-cert selection results made by SslStream are visible to the
        // TlsSession-driven PAL calls, and to avoid double Dispose on the bag.
        internal static TlsContext WrapShared(SslAuthenticationOptions sharedOptions)
        {
            Debug.Assert(sharedOptions != null);
            return new TlsContext(sharedOptions, ownsOptions: false);
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
