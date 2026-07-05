// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;

namespace System.Net.Security
{
    /// <summary>
    /// Stub implementation used on platforms where TlsSession is not yet supported.
    /// All operations throw <see cref="PlatformNotSupportedException"/>.
    /// </summary>
    [Experimental(Experimentals.LowLevelTlsDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    public sealed class TlsSession : IDisposable
    {
        private TlsSession() { }

        public bool IsHandshakeComplete => throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);
        public bool HasPendingOutput => throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);
        public string? TargetHostName
        {
            get => throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);
            set => throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);
        }
        public SslClientHelloInfo? ClientHelloInfo => throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);
        public SslProtocols NegotiatedProtocol => throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);
        [System.CLSCompliant(false)]
        public TlsCipherSuite NegotiatedCipherSuite => throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);
        public SslApplicationProtocol NegotiatedApplicationProtocol => throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);

        public static TlsSession Create(TlsContext context) =>
            throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);

        public static TlsSession Create(TlsContext context, SafeSocketHandle socket) =>
            throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);

        public SafeSocketHandle? Socket => throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);

        public TlsOperationStatus Handshake() =>
            throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);

        public TlsOperationStatus Read(Span<byte> buffer, out int bytesRead) =>
            throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);

        public TlsOperationStatus Write(ReadOnlySpan<byte> buffer, out int bytesWritten) =>
            throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);

        public TlsOperationStatus ProcessHandshake(ReadOnlySpan<byte> input, Span<byte> output, out int bytesConsumed, out int bytesWritten) =>
            throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);

        public TlsOperationStatus Encrypt(ReadOnlySpan<byte> plaintext, Span<byte> ciphertext, out int bytesConsumed, out int bytesWritten) =>
            throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);

        public TlsOperationStatus Decrypt(ReadOnlySpan<byte> ciphertext, Span<byte> plaintext, out int bytesConsumed, out int bytesWritten) =>
            throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);

        public TlsOperationStatus DrainPendingOutput(Span<byte> ciphertext, out int bytesWritten) =>
            throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);

        public TlsOperationStatus Shutdown(Span<byte> ciphertext, out int bytesWritten) =>
            throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);

        public X509Certificate2? GetRemoteCertificate() =>
            throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);

        public X509Certificate2Collection? GetRemoteCertificates() =>
            throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);

        public SslPolicyErrors AcceptWithDefaultValidation() =>
            throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);

        public void SetRemoteCertificateValidationResult(SslPolicyErrors errors) =>
            throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);

        public void SetServerContext(TlsContext serverContext) =>
            throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);

        public void SetClientCertificateContext(SslStreamCertificateContext context) =>
            throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);

        public IReadOnlyList<string>? GetAcceptableIssuers() =>
            throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);

        public X509Certificate2? LocalCertificate =>
            throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);

        public ChannelBinding? GetChannelBinding(ChannelBindingKind kind) =>
            throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);

        public TlsOperationStatus RequestClientCertificate(Span<byte> ciphertext, out int bytesWritten) =>
            throw new PlatformNotSupportedException(SR.SystemNetSecurity_PlatformNotSupported);

        public void Dispose() { }
    }
}
