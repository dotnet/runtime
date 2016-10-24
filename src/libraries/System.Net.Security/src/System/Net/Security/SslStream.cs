// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Security
{
    public enum EncryptionPolicy
    {
        // Prohibit null ciphers (current system defaults)
        RequireEncryption = 0,

        // Add null ciphers to current system defaults
        AllowNoEncryption,

        // Request null ciphers only
        NoEncryption
    }

    // A user delegate used to verify remote SSL certificate.
    public delegate bool RemoteCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors);

    // A user delegate used to select local SSL certificate.
    public delegate X509Certificate LocalCertificateSelectionCallback(object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers);

    // Internal versions of the above delegates.
    internal delegate bool RemoteCertValidationCallback(string host, X509Certificate2 certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors);
    internal delegate X509Certificate LocalCertSelectionCallback(string targetHost, X509CertificateCollection localCertificates, X509Certificate2 remoteCertificate, string[] acceptableIssuers);

    public class SslStream : AuthenticatedStream
    {
        private SslState _sslState;
        private RemoteCertificateValidationCallback _userCertificateValidationCallback;
        private LocalCertificateSelectionCallback _userCertificateSelectionCallback;
        private object _remoteCertificateOrBytes;

        public SslStream(Stream innerStream)
                : this(innerStream, false, null, null)
        {
        }

        public SslStream(Stream innerStream, bool leaveInnerStreamOpen)
                : this(innerStream, leaveInnerStreamOpen, null, null, EncryptionPolicy.RequireEncryption)
        {
        }

        public SslStream(Stream innerStream, bool leaveInnerStreamOpen, RemoteCertificateValidationCallback userCertificateValidationCallback)
                : this(innerStream, leaveInnerStreamOpen, userCertificateValidationCallback, null, EncryptionPolicy.RequireEncryption)
        {
        }

        public SslStream(Stream innerStream, bool leaveInnerStreamOpen, RemoteCertificateValidationCallback userCertificateValidationCallback,
            LocalCertificateSelectionCallback userCertificateSelectionCallback)
                : this(innerStream, leaveInnerStreamOpen, userCertificateValidationCallback, userCertificateSelectionCallback, EncryptionPolicy.RequireEncryption)
        {
        }

        public SslStream(Stream innerStream, bool leaveInnerStreamOpen, RemoteCertificateValidationCallback userCertificateValidationCallback,
            LocalCertificateSelectionCallback userCertificateSelectionCallback, EncryptionPolicy encryptionPolicy)
            : base(innerStream, leaveInnerStreamOpen)
        {
            if (encryptionPolicy != EncryptionPolicy.RequireEncryption && encryptionPolicy != EncryptionPolicy.AllowNoEncryption && encryptionPolicy != EncryptionPolicy.NoEncryption)
            {
                throw new ArgumentException(SR.Format(SR.net_invalid_enum, "EncryptionPolicy"), nameof(encryptionPolicy));
            }

            _userCertificateValidationCallback = userCertificateValidationCallback;
            _userCertificateSelectionCallback = userCertificateSelectionCallback;
            RemoteCertValidationCallback _userCertValidationCallbackWrapper = new RemoteCertValidationCallback(UserCertValidationCallbackWrapper);
            LocalCertSelectionCallback _userCertSelectionCallbackWrapper = userCertificateSelectionCallback == null ? null : new LocalCertSelectionCallback(UserCertSelectionCallbackWrapper);
            _sslState = new SslState(innerStream, _userCertValidationCallbackWrapper, _userCertSelectionCallbackWrapper, encryptionPolicy);
        }

        private bool UserCertValidationCallbackWrapper(string hostName, X509Certificate2 certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            _remoteCertificateOrBytes = certificate == null ? null : certificate.RawData;
            if (_userCertificateValidationCallback == null)
            {
                if (!_sslState.RemoteCertRequired)
                {
                    sslPolicyErrors &= ~SslPolicyErrors.RemoteCertificateNotAvailable;
                }

                return (sslPolicyErrors == SslPolicyErrors.None);
            }
            else
            {
                return _userCertificateValidationCallback(this, certificate, chain, sslPolicyErrors);
            }
        }

        private X509Certificate UserCertSelectionCallbackWrapper(string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers)
        {
            return _userCertificateSelectionCallback(this, targetHost, localCertificates, remoteCertificate, acceptableIssuers);
        }

        //
        // Client side auth.
        //
        public virtual IAsyncResult BeginAuthenticateAsClient(string targetHost, AsyncCallback asyncCallback, object asyncState)
        {
            return BeginAuthenticateAsClient(targetHost, new X509CertificateCollection(), SecurityProtocol.DefaultSecurityProtocols, false,
                                           asyncCallback, asyncState);
        }

        public virtual IAsyncResult BeginAuthenticateAsClient(string targetHost, X509CertificateCollection clientCertificates,
                                                            SslProtocols enabledSslProtocols, bool checkCertificateRevocation,
                                                            AsyncCallback asyncCallback, object asyncState)
        {
            SecurityProtocol.ThrowOnNotAllowed(enabledSslProtocols);
            _sslState.ValidateCreateContext(false, targetHost, enabledSslProtocols, null, clientCertificates, true, checkCertificateRevocation);

            LazyAsyncResult result = new LazyAsyncResult(_sslState, asyncState, asyncCallback);
            _sslState.ProcessAuthentication(result);
            return result;
        }

        public virtual void EndAuthenticateAsClient(IAsyncResult asyncResult)
        {
            _sslState.EndProcessAuthentication(asyncResult);
        }

        //
        // Server side auth.
        //
        public virtual IAsyncResult BeginAuthenticateAsServer(X509Certificate serverCertificate, AsyncCallback asyncCallback, object asyncState)

        {
            return BeginAuthenticateAsServer(serverCertificate, false, SecurityProtocol.DefaultSecurityProtocols, false,
                                                          asyncCallback,
                                                            asyncState);
        }

        public virtual IAsyncResult BeginAuthenticateAsServer(X509Certificate serverCertificate, bool clientCertificateRequired,
                                                            SslProtocols enabledSslProtocols, bool checkCertificateRevocation,
                                                            AsyncCallback asyncCallback,
                                                            object asyncState)
        {
            SecurityProtocol.ThrowOnNotAllowed(enabledSslProtocols);
            _sslState.ValidateCreateContext(true, string.Empty, enabledSslProtocols, serverCertificate, null, clientCertificateRequired, checkCertificateRevocation);
            LazyAsyncResult result = new LazyAsyncResult(_sslState, asyncState, asyncCallback);
            _sslState.ProcessAuthentication(result);
            return result;
        }

        public virtual void EndAuthenticateAsServer(IAsyncResult asyncResult)
        {
            _sslState.EndProcessAuthentication(asyncResult);
        }

        internal virtual IAsyncResult BeginShutdown(AsyncCallback asyncCallback, object asyncState)
        {
            return _sslState.BeginShutdown(asyncCallback, asyncState);
        }

        internal virtual void EndShutdown(IAsyncResult asyncResult)
        {
            _sslState.EndShutdown(asyncResult);
        }

        public TransportContext TransportContext
        {
            get
            {
                return new SslStreamContext(this);
            }
        }

        internal ChannelBinding GetChannelBinding(ChannelBindingKind kind)
        {
            return _sslState.GetChannelBinding(kind);
        }

        #region Synchronous methods
        public virtual void AuthenticateAsClient(string targetHost)
        {
            AuthenticateAsClient(targetHost, new X509CertificateCollection(), SecurityProtocol.DefaultSecurityProtocols, false);
        }

        public virtual void AuthenticateAsClient(string targetHost, X509CertificateCollection clientCertificates, SslProtocols enabledSslProtocols, bool checkCertificateRevocation)
        {
            SecurityProtocol.ThrowOnNotAllowed(enabledSslProtocols);
            _sslState.ValidateCreateContext(false, targetHost, enabledSslProtocols, null, clientCertificates, true, checkCertificateRevocation);
            _sslState.ProcessAuthentication(null);
        }

        public virtual void AuthenticateAsServer(X509Certificate serverCertificate)
        {
            AuthenticateAsServer(serverCertificate, false, SecurityProtocol.DefaultSecurityProtocols, false);
        }

        public virtual void AuthenticateAsServer(X509Certificate serverCertificate, bool clientCertificateRequired, SslProtocols enabledSslProtocols, bool checkCertificateRevocation)
        {
            SecurityProtocol.ThrowOnNotAllowed(enabledSslProtocols);
            _sslState.ValidateCreateContext(true, string.Empty, enabledSslProtocols, serverCertificate, null, clientCertificateRequired, checkCertificateRevocation);
            _sslState.ProcessAuthentication(null);
        }
        #endregion

        #region Task-based async public methods
        public virtual Task AuthenticateAsClientAsync(string targetHost)
        {
            return Task.Factory.FromAsync(BeginAuthenticateAsClient, EndAuthenticateAsClient, targetHost, null);
        }

        public virtual Task AuthenticateAsClientAsync(string targetHost, X509CertificateCollection clientCertificates, SslProtocols enabledSslProtocols, bool checkCertificateRevocation)
        {
            return Task.Factory.FromAsync((callback, state) => BeginAuthenticateAsClient(targetHost, clientCertificates, enabledSslProtocols, checkCertificateRevocation, callback, state), EndAuthenticateAsClient, null);
        }

        public virtual Task AuthenticateAsServerAsync(X509Certificate serverCertificate)
        {
            return Task.Factory.FromAsync(BeginAuthenticateAsServer, EndAuthenticateAsServer, serverCertificate, null);
        }

        public virtual Task AuthenticateAsServerAsync(X509Certificate serverCertificate, bool clientCertificateRequired, SslProtocols enabledSslProtocols, bool checkCertificateRevocation)
        {

            return Task.Factory.FromAsync((callback, state) => BeginAuthenticateAsServer(serverCertificate, clientCertificateRequired, enabledSslProtocols, checkCertificateRevocation, callback, state), EndAuthenticateAsServer, null);
        }

        public virtual Task ShutdownAsync()
        {
            return Task.Factory.FromAsync(
                (callback, state) => BeginShutdown(callback, state),
                EndShutdown, 
                null);
        }
        #endregion

        public override bool IsAuthenticated
        {
            get
            {
                return _sslState.IsAuthenticated;
            }
        }

        public override bool IsMutuallyAuthenticated
        {
            get
            {
                return _sslState.IsMutuallyAuthenticated;
            }
        }

        public override bool IsEncrypted
        {
            get
            {
                return IsAuthenticated;
            }
        }

        public override bool IsSigned
        {
            get
            {
                return IsAuthenticated;
            }
        }

        public override bool IsServer
        {
            get
            {
                return _sslState.IsServer;
            }
        }

        public virtual SslProtocols SslProtocol
        {
            get
            {
                return _sslState.SslProtocol;
            }
        }

        public virtual bool CheckCertRevocationStatus
        {
            get
            {
                return _sslState.CheckCertRevocationStatus;
            }
        }

        public virtual X509Certificate LocalCertificate
        {
            get
            {
                return _sslState.LocalCertificate;
            }
        }

        public virtual X509Certificate RemoteCertificate
        {
            get
            {
                _sslState.CheckThrow(true);

                object chkCertificateOrBytes = _remoteCertificateOrBytes;
                if (chkCertificateOrBytes != null && chkCertificateOrBytes.GetType() == typeof(byte[]))
                {
                    return (X509Certificate)(_remoteCertificateOrBytes = new X509Certificate2((byte[])chkCertificateOrBytes));
                }
                else
                {
                    return chkCertificateOrBytes as X509Certificate;
                }
            }
        }

        public virtual CipherAlgorithmType CipherAlgorithm
        {
            get
            {
                return _sslState.CipherAlgorithm;
            }
        }

        public virtual int CipherStrength
        {
            get
            {
                return _sslState.CipherStrength;
            }
        }

        public virtual HashAlgorithmType HashAlgorithm
        {
            get
            {
                return _sslState.HashAlgorithm;
            }
        }

        public virtual int HashStrength
        {
            get
            {
                return _sslState.HashStrength;
            }
        }

        public virtual ExchangeAlgorithmType KeyExchangeAlgorithm
        {
            get
            {
                return _sslState.KeyExchangeAlgorithm;
            }
        }

        public virtual int KeyExchangeStrength
        {
            get
            {
                return _sslState.KeyExchangeStrength;
            }
        }

        //
        // Stream contract implementation.
        //
        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override bool CanRead
        {
            get
            {
                return _sslState.IsAuthenticated && InnerStream.CanRead;
            }
        }

        public override bool CanTimeout
        {
            get
            {
                return InnerStream.CanTimeout;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return _sslState.IsAuthenticated && InnerStream.CanWrite && !_sslState.IsShutdown;
            }
        }

        public override int ReadTimeout
        {
            get
            {
                return InnerStream.ReadTimeout;
            }
            set
            {
                InnerStream.ReadTimeout = value;
            }
        }

        public override int WriteTimeout
        {
            get
            {
                return InnerStream.WriteTimeout;
            }
            set
            {
                InnerStream.WriteTimeout = value;
            }
        }

        public override long Length
        {
            get
            {
                return InnerStream.Length;
            }
        }

        public override long Position
        {
            get
            {
                return InnerStream.Position;
            }
            set
            {
                throw new NotSupportedException(SR.net_noseek);
            }
        }

        public override void SetLength(long value)
        {
            InnerStream.SetLength(value);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException(SR.net_noseek);
        }

        public override void Flush()
        {
            _sslState.Flush();
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                _sslState.Close();
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        public override int ReadByte()
        {
            return _sslState.SecureStream.ReadByte();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _sslState.SecureStream.Read(buffer, offset, count);
        }

        public void Write(byte[] buffer)
        {
            _sslState.SecureStream.Write(buffer, 0, buffer.Length);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _sslState.SecureStream.Write(buffer, offset, count);
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback asyncCallback, object asyncState)
        {
            return _sslState.SecureStream.BeginRead(buffer, offset, count, asyncCallback, asyncState);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return _sslState.SecureStream.EndRead(asyncResult);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback asyncCallback, object asyncState)
        {
            return _sslState.SecureStream.BeginWrite(buffer, offset, count, asyncCallback, asyncState);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            _sslState.SecureStream.EndWrite(asyncResult);
        }
    }
}
