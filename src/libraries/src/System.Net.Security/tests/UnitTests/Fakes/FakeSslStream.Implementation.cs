// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace System.Net.Security
{
    public partial class SslStream
    {
        private void ValidateCreateContext(SslClientAuthenticationOptions sslClientAuthenticationOptions, RemoteCertValidationCallback remoteCallback, LocalCertSelectionCallback localCallback)
        {
            // Without setting (or using) these members you will get a build exception in the unit test project.
            // The code that normally uses these in the main solution is in the implementation of SslStream.

            if(_nestedWrite == 0)
            {

            }
            _context = null;
            _exception = null;
            _internalBuffer = null;
            _internalBufferCount = 0;
            _internalOffset = 0;
            _nestedWrite = 0;
            _handshakeCompleted = false;
        }

        private void ValidateParameters(byte[] buffer, int offset, int count)
        {
        }

        private void ValidateCreateContext(SslAuthenticationOptions sslAuthenticationOptions)
        {
        }

        private Task WriteAsyncInternal<TWriteAdapter>(TWriteAdapter writeAdapter, ReadOnlyMemory<byte> buffer)
            where TWriteAdapter : struct, ISslWriteAdapter => default;

        private ValueTask<int> ReadAsyncInternal<TReadAdapter>(TReadAdapter adapter, Memory<byte> buffer) => default;

        private Task CheckEnqueueWriteAsync() => default;

        private void CheckEnqueueWrite()
        {
        }

        private ValueTask<int> CheckEnqueueReadAsync(Memory<byte> buffer) => default;

        private int CheckEnqueueRead(Memory<byte> buffer) => default;

        private bool RemoteCertRequired => default;

        private void CheckThrow(bool authSuccessCheck, bool shutdownCheck = false)
        {
        }

        private void CloseInternal()
        {
        }
        //
        // This method assumes that a SSPI context is already in a good shape.
        // For example it is either a fresh context or already authenticated context that needs renegotiation.
        //
        private void ProcessAuthentication(LazyAsyncResult lazyResult)
        {
        }

        private void EndProcessAuthentication(IAsyncResult result)
        {
        }

        private void ReturnReadBufferIfEmpty()
        {
        }
    }

    internal class SecureChannel
    {
        internal bool IsValidContext => default;
        internal bool IsServer => default;
        internal SslConnectionInfo ConnectionInfo => default;
        internal ChannelBinding GetChannelBinding(ChannelBindingKind kind) => default;
        internal X509Certificate LocalServerCertificate => default;
        internal bool IsRemoteCertificateAvailable => default;
        internal SslApplicationProtocol NegotiatedApplicationProtocol => default;
        internal X509Certificate LocalClientCertificate => default;
        internal X509RevocationMode CheckCertRevocationStatus => default;
        internal ProtocolToken CreateShutdownToken() => default;
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
