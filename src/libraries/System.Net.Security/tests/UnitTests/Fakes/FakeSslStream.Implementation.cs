// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

// Disable warning about unused or unasiggned variables
#pragma warning disable CS0649
#pragma warning disable CS0414

namespace System.Net.Security
{
    public partial class SslStream
    {
        private class FakeOptions
        {
            public string TargetHost;
            public EncryptionPolicy EncryptionPolicy;
            public bool IsServer;
            public RemoteCertificateValidationCallback? CertValidationDelegate;
            public LocalCertificateSelectionCallback? CertSelectionDelegate;
            public X509RevocationMode CertificateRevocationCheckMode;

            public void UpdateOptions(SslServerAuthenticationOptions sslServerAuthenticationOptions)
            {
            }

            public void UpdateOptions(SslClientAuthenticationOptions sslClientAuthenticationOptions)
            {
            }

            internal void UpdateOptions(ServerOptionsSelectionCallback optionCallback, object? state)
            {
            }
        }

        private FakeOptions _sslAuthenticationOptions = new FakeOptions();
        private SslConnectionInfo _connectionInfo;
        internal ChannelBinding? GetChannelBinding(ChannelBindingKind kind) => null;
        private bool _remoteCertificateExposed;
        private X509Certificate2? LocalClientCertificate;
        private X509Certificate2? LocalServerCertificate;
        private bool IsRemoteCertificateAvailable;
        private bool IsValidContext;
        private X509Certificate2? _remoteCertificate;


        private void ValidateCreateContext(SslClientAuthenticationOptions sslClientAuthenticationOptions, RemoteCertificateValidationCallback? remoteCallback, LocalCertificateSelectionCallback? localCallback)
        {
            // Without setting (or using) these members you will get a build exception in the unit test project.
            // The code that normally uses these in the main solution is in the implementation of SslStream.

            if (_nestedWrite == 0)
            {

            }
            _exception = null;
            _nestedWrite = 0;
            _handshakeCompleted = false;
        }

        private void ValidateCreateContext(SslAuthenticationOptions sslAuthenticationOptions)
        {
            _sslAuthenticationOptions = new FakeOptions() { TargetHost = sslAuthenticationOptions.TargetHost };
        }

        private ValueTask WriteAsyncInternal<TWriteAdapter>(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
            where TWriteAdapter : IReadWriteAdapter => default;

        private ValueTask<int> ReadAsyncInternal<TReadAdapter>(Memory<byte> buffer, CancellationToken cancellationToken)
            where TReadAdapter : IReadWriteAdapter => default;

        private bool RemoteCertRequired => default;

        private void CloseInternal()
        {
        }
        //
        // This method assumes that a SSPI context is already in a good shape.
        // For example it is either a fresh context or already authenticated context that needs renegotiation.
        //
        private Task ProcessAuthenticationAsync(bool isAsync = false, CancellationToken cancellationToken = default)
        {
            return Task.Run(() => { });
        }

        private Task RenegotiateAsync<AsyncReadWriteAdapter>(CancellationToken cancellationToken) => throw new PlatformNotSupportedException();

        private void ReturnReadBufferIfEmpty()
        {
        }

        private ProtocolToken? CreateShutdownToken()
        {
            return null;
        }

        internal static X509Certificate2? FindCertificateWithPrivateKey(object instance, bool isServer, X509Certificate certificate)
        {
            return null;
        }
    }

    internal class ProtocolToken
    {
        public ProtocolToken()
        {
            Payload = null;
        }
        internal byte[] Payload;
    }
}
