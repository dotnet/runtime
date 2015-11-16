// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Security
{
    internal class SslState
    {
        //
        //  The public Client and Server classes enforce the parameters rules before
        //  calling into this .ctor.
        //
        internal SslState(Stream innerStream, RemoteCertValidationCallback certValidationCallback, LocalCertSelectionCallback certSelectionCallback, EncryptionPolicy encryptionPolicy)
        {
        }
        //
        //
        //
        internal void ValidateCreateContext(bool isServer, string targetHost, SslProtocols enabledSslProtocols, X509Certificate serverCertificate, X509CertificateCollection clientCertificates, bool remoteCertRequired, bool checkCertRevocationStatus)
        {
        }

        internal bool IsAuthenticated
        {
            get
            {
                return false;
            }
        }

        internal bool IsMutuallyAuthenticated
        {
            get
            {
                return false;
            }
        }

        internal bool RemoteCertRequired
        {
            get
            {
                return false;
            }
        }

        internal bool IsServer
        {
            get
            {
                return false;
            }
        }

        //
        // This will return selected local cert for both client/server streams
        //
        internal X509Certificate LocalCertificate
        {
            get
            {
                return null;
            }
        }

        internal ChannelBinding GetChannelBinding(ChannelBindingKind kind)
        {
            return null;
        }

        internal bool CheckCertRevocationStatus
        {
            get
            {
                return false;
            }
        }

        internal CipherAlgorithmType CipherAlgorithm
        {
            get
            {
                return CipherAlgorithmType.Null;
            }
        }

        internal int CipherStrength
        {
            get
            {
                return 0;
            }
        }

        internal HashAlgorithmType HashAlgorithm
        {
            get
            {
                return HashAlgorithmType.None;
            }
        }

        internal int HashStrength
        {
            get
            {
                return 0;
            }
        }

        internal ExchangeAlgorithmType KeyExchangeAlgorithm
        {
            get
            {
                return ExchangeAlgorithmType.None;
            }
        }

        internal int KeyExchangeStrength
        {
            get
            {
                return 0;
            }
        }

        internal SslProtocols SslProtocol
        {
            get
            {
                return SslProtocols.None;
            }
        }

        internal _SslStream SecureStream
        {
            get
            {
                return null;
            }
        }

        internal void CheckThrow(bool authSucessCheck)
        {
        }

        internal void Flush()
        {
        }

        //
        // This is to not depend on GC&SafeHandle class if the context is not needed anymore.
        //
        internal void Close()
        {
        }
        
        //
        // This method assumes that a SSPI context is already in a good shape.
        // For example it is either a fresh context or already authenticated context that needs renegotiation.
        //
        internal void ProcessAuthentication(LazyAsyncResult lazyResult)
        {
        }
                        
        internal void EndProcessAuthentication(IAsyncResult result)
        {
        }       
    }

    internal class _SslStream : Stream
    {
        public override bool CanRead
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool CanSeek
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool CanWrite
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override long Length
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override long Position
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }

}
