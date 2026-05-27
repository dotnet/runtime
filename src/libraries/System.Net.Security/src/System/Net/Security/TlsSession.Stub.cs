// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Security
{
    /// <summary>
    /// Stub implementation used on platforms where TlsSession is not yet supported.
    /// All operations throw <see cref="PlatformNotSupportedException"/>.
    /// </summary>
    public sealed class TlsSession : IDisposable
    {
        private TlsSession() { }

        public bool IsServer => throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);
        public bool IsHandshakeComplete => throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);
        public bool HasPendingOutput => throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);
        public string? TargetHostName
        {
            get => throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);
            set => throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);
        }
        public SslProtocols NegotiatedProtocol => throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);
        [System.CLSCompliant(false)]
        public TlsCipherSuite NegotiatedCipherSuite => throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);
        public SslApplicationProtocol NegotiatedApplicationProtocol => throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);

        public static TlsSession Create(TlsContext context) =>
            throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);

        public TlsOperationStatus ProcessHandshake(ReadOnlySpan<byte> input, Span<byte> output, out int consumed, out int produced) =>
            throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);

        public TlsOperationStatus Encrypt(ReadOnlySpan<byte> plaintext, Span<byte> ciphertext, out int consumed, out int produced) =>
            throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);

        public TlsOperationStatus Decrypt(ReadOnlySpan<byte> ciphertext, Span<byte> plaintext, out int consumed, out int produced) =>
            throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);

        public TlsOperationStatus DrainPendingOutput(Span<byte> ciphertext, out int produced) =>
            throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);

        public TlsOperationStatus Shutdown(Span<byte> ciphertext, out int produced) =>
            throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);

        public X509Certificate2? GetRemoteCertificate() =>
            throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);

        public X509Certificate2? LocalCertificate =>
            throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);

        public ChannelBinding? GetChannelBinding(ChannelBindingKind kind) =>
            throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);

        public TlsOperationStatus RequestClientCertificate(Span<byte> ciphertext, out int produced) =>
            throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);

        public TlsOperationStatus RequestRenegotiation(Span<byte> ciphertext, out int produced) =>
            throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);

        public void Dispose() { }
    }
}
