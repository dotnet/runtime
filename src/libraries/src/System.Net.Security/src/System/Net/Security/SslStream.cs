// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Runtime.ExceptionServices;
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

    public delegate X509Certificate ServerCertificateSelectionCallback(object sender, string hostName);

    // Internal versions of the above delegates.
    internal delegate bool RemoteCertValidationCallback(string host, X509Certificate2 certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors);
    internal delegate X509Certificate LocalCertSelectionCallback(string targetHost, X509CertificateCollection localCertificates, X509Certificate2 remoteCertificate, string[] acceptableIssuers);
    internal delegate X509Certificate ServerCertSelectionCallback(string hostName);

    public partial class SslStream : AuthenticatedStream
    {
        private X509Certificate2 _remoteCertificate;
        private bool _remoteCertificateExposed;

        internal RemoteCertificateValidationCallback _userCertificateValidationCallback;
        internal LocalCertificateSelectionCallback _userCertificateSelectionCallback;
        internal ServerCertificateSelectionCallback _userServerCertificateSelectionCallback;
        internal RemoteCertValidationCallback _certValidationDelegate;
        internal LocalCertSelectionCallback _certSelectionDelegate;
        internal EncryptionPolicy _encryptionPolicy;

        private readonly Stream _innerStream;
        private SecureChannel _context;

        private ExceptionDispatchInfo _exception;
        private bool _shutdown;
        private bool _handshakeCompleted;

        // Never updated directly, special properties are used.  This is the read buffer.
        internal byte[] _internalBuffer;
        internal int _internalOffset;
        internal int _internalBufferCount;
        internal int _decryptedBytesOffset;
        internal int _decryptedBytesCount;

        private int _nestedWrite;
        private int _nestedRead;

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
            _encryptionPolicy = encryptionPolicy;
            _certValidationDelegate = new RemoteCertValidationCallback(UserCertValidationCallbackWrapper);
            _certSelectionDelegate = userCertificateSelectionCallback == null ? null : new LocalCertSelectionCallback(UserCertSelectionCallbackWrapper);

            _innerStream = innerStream;
        }

        public SslApplicationProtocol NegotiatedApplicationProtocol
        {
            get
            {
                if (_context == null)
                    return default;

                return _context.NegotiatedApplicationProtocol;
            }
        }

        private void SetAndVerifyValidationCallback(RemoteCertificateValidationCallback callback)
        {
            if (_userCertificateValidationCallback == null)
            {
                _userCertificateValidationCallback = callback;
                _certValidationDelegate = new RemoteCertValidationCallback(UserCertValidationCallbackWrapper);
            }
            else if (callback != null && _userCertificateValidationCallback != callback)
            {
                throw new InvalidOperationException(SR.Format(SR.net_conflicting_options, nameof(RemoteCertificateValidationCallback)));
            }
        }

        private void SetAndVerifySelectionCallback(LocalCertificateSelectionCallback callback)
        {
            if (_userCertificateSelectionCallback == null)
            {
                _userCertificateSelectionCallback = callback;
                _certSelectionDelegate = _userCertificateSelectionCallback == null ? null : new LocalCertSelectionCallback(UserCertSelectionCallbackWrapper);
            }
            else if (callback != null && _userCertificateSelectionCallback != callback)
            {
                throw new InvalidOperationException(SR.Format(SR.net_conflicting_options, nameof(LocalCertificateSelectionCallback)));
            }
        }

        private bool UserCertValidationCallbackWrapper(string hostName, X509Certificate2 certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            _remoteCertificate = certificate == null ? null : new X509Certificate2(certificate);
            if (_userCertificateValidationCallback == null)
            {
                if (!RemoteCertRequired)
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

        private X509Certificate ServerCertSelectionCallbackWrapper(string targetHost) => _userServerCertificateSelectionCallback(this, targetHost);

        private SslAuthenticationOptions CreateAuthenticationOptions(SslServerAuthenticationOptions sslServerAuthenticationOptions)
        {
            if (sslServerAuthenticationOptions.ServerCertificate == null && sslServerAuthenticationOptions.ServerCertificateSelectionCallback == null && _certSelectionDelegate == null)
            {
                throw new ArgumentNullException(nameof(sslServerAuthenticationOptions.ServerCertificate));
            }

            if ((sslServerAuthenticationOptions.ServerCertificate != null || _certSelectionDelegate != null) && sslServerAuthenticationOptions.ServerCertificateSelectionCallback != null)
            {
                throw new InvalidOperationException(SR.Format(SR.net_conflicting_options, nameof(ServerCertificateSelectionCallback)));
            }

            var authOptions = new SslAuthenticationOptions(sslServerAuthenticationOptions);

            _userServerCertificateSelectionCallback = sslServerAuthenticationOptions.ServerCertificateSelectionCallback;
            authOptions.ServerCertSelectionDelegate = _userServerCertificateSelectionCallback == null ? null : new ServerCertSelectionCallback(ServerCertSelectionCallbackWrapper);

            authOptions.CertValidationDelegate = _certValidationDelegate;
            authOptions.CertSelectionDelegate = _certSelectionDelegate;

            return authOptions;
        }

        //
        // Client side auth.
        //
        public virtual IAsyncResult BeginAuthenticateAsClient(string targetHost, AsyncCallback asyncCallback, object asyncState)
        {
            return BeginAuthenticateAsClient(targetHost, null, SecurityProtocol.SystemDefaultSecurityProtocols, false,
                                           asyncCallback, asyncState);
        }

        public virtual IAsyncResult BeginAuthenticateAsClient(string targetHost, X509CertificateCollection clientCertificates,
                                                            bool checkCertificateRevocation, AsyncCallback asyncCallback, object asyncState)
        {
            return BeginAuthenticateAsClient(targetHost, clientCertificates, SecurityProtocol.SystemDefaultSecurityProtocols, checkCertificateRevocation, asyncCallback, asyncState);
        }

        public virtual IAsyncResult BeginAuthenticateAsClient(string targetHost, X509CertificateCollection clientCertificates,
                                                            SslProtocols enabledSslProtocols, bool checkCertificateRevocation,
                                                            AsyncCallback asyncCallback, object asyncState)
        {
            SslClientAuthenticationOptions options = new SslClientAuthenticationOptions
            {
                TargetHost = targetHost,
                ClientCertificates = clientCertificates,
                EnabledSslProtocols = enabledSslProtocols,
                CertificateRevocationCheckMode = checkCertificateRevocation ? X509RevocationMode.Online : X509RevocationMode.NoCheck,
                EncryptionPolicy = _encryptionPolicy,
            };

            return BeginAuthenticateAsClient(options, CancellationToken.None, asyncCallback, asyncState);
        }

        internal virtual IAsyncResult BeginAuthenticateAsClient(SslClientAuthenticationOptions sslClientAuthenticationOptions, CancellationToken cancellationToken, AsyncCallback asyncCallback, object asyncState)
        {
            SetAndVerifyValidationCallback(sslClientAuthenticationOptions.RemoteCertificateValidationCallback);
            SetAndVerifySelectionCallback(sslClientAuthenticationOptions.LocalCertificateSelectionCallback);

            ValidateCreateContext(sslClientAuthenticationOptions, _certValidationDelegate, _certSelectionDelegate);

            LazyAsyncResult result = new LazyAsyncResult(this, asyncState, asyncCallback);
            ProcessAuthentication(result);
            return result;
        }

        public virtual void EndAuthenticateAsClient(IAsyncResult asyncResult) => EndProcessAuthentication(asyncResult);

        //
        // Server side auth.
        //
        public virtual IAsyncResult BeginAuthenticateAsServer(X509Certificate serverCertificate, AsyncCallback asyncCallback, object asyncState)

        {
            return BeginAuthenticateAsServer(serverCertificate, false, SecurityProtocol.SystemDefaultSecurityProtocols, false,
                                                          asyncCallback,
                                                            asyncState);
        }

        public virtual IAsyncResult BeginAuthenticateAsServer(X509Certificate serverCertificate, bool clientCertificateRequired,
                                                            bool checkCertificateRevocation, AsyncCallback asyncCallback, object asyncState)
        {
            return BeginAuthenticateAsServer(serverCertificate, clientCertificateRequired, SecurityProtocol.SystemDefaultSecurityProtocols, checkCertificateRevocation, asyncCallback, asyncState);
        }

        public virtual IAsyncResult BeginAuthenticateAsServer(X509Certificate serverCertificate, bool clientCertificateRequired,
                                                            SslProtocols enabledSslProtocols, bool checkCertificateRevocation,
                                                            AsyncCallback asyncCallback,
                                                            object asyncState)
        {
            SslServerAuthenticationOptions options = new SslServerAuthenticationOptions
            {
                ServerCertificate = serverCertificate,
                ClientCertificateRequired = clientCertificateRequired,
                EnabledSslProtocols = enabledSslProtocols,
                CertificateRevocationCheckMode = checkCertificateRevocation ? X509RevocationMode.Online : X509RevocationMode.NoCheck,
                EncryptionPolicy = _encryptionPolicy,
            };

            return BeginAuthenticateAsServer(options, CancellationToken.None, asyncCallback, asyncState);
        }

        private IAsyncResult BeginAuthenticateAsServer(SslServerAuthenticationOptions sslServerAuthenticationOptions, CancellationToken cancellationToken, AsyncCallback asyncCallback, object asyncState)
        {
            SetAndVerifyValidationCallback(sslServerAuthenticationOptions.RemoteCertificateValidationCallback);

            ValidateCreateContext(CreateAuthenticationOptions(sslServerAuthenticationOptions));

            LazyAsyncResult result = new LazyAsyncResult(this, asyncState, asyncCallback);
            ProcessAuthentication(result);
            return result;
        }

        public virtual void EndAuthenticateAsServer(IAsyncResult asyncResult) => EndProcessAuthentication(asyncResult);

        internal virtual IAsyncResult BeginShutdown(AsyncCallback asyncCallback, object asyncState)
        {
            CheckThrow(authSuccessCheck: true, shutdownCheck: true);

            ProtocolToken message = _context.CreateShutdownToken();
            return TaskToApm.Begin(InnerStream.WriteAsync(message.Payload, 0, message.Payload.Length), asyncCallback, asyncState);
        }

        internal virtual void EndShutdown(IAsyncResult asyncResult)
        {
            CheckThrow(authSuccessCheck: true, shutdownCheck: true);

            TaskToApm.End(asyncResult);
            _shutdown = true;
        }

        public TransportContext TransportContext => new SslStreamContext(this);

        internal ChannelBinding GetChannelBinding(ChannelBindingKind kind) => _context?.GetChannelBinding(kind);

        #region Synchronous methods
        public virtual void AuthenticateAsClient(string targetHost)
        {
            AuthenticateAsClient(targetHost, null, SecurityProtocol.SystemDefaultSecurityProtocols, false);
        }

        public virtual void AuthenticateAsClient(string targetHost, X509CertificateCollection clientCertificates, bool checkCertificateRevocation)
        {
            AuthenticateAsClient(targetHost, clientCertificates, SecurityProtocol.SystemDefaultSecurityProtocols, checkCertificateRevocation);
        }

        public virtual void AuthenticateAsClient(string targetHost, X509CertificateCollection clientCertificates, SslProtocols enabledSslProtocols, bool checkCertificateRevocation)
        {
            SslClientAuthenticationOptions options = new SslClientAuthenticationOptions
            {
                TargetHost = targetHost,
                ClientCertificates = clientCertificates,
                EnabledSslProtocols = enabledSslProtocols,
                CertificateRevocationCheckMode = checkCertificateRevocation ? X509RevocationMode.Online : X509RevocationMode.NoCheck,
                EncryptionPolicy = _encryptionPolicy,
            };

            AuthenticateAsClient(options);
        }

        private void AuthenticateAsClient(SslClientAuthenticationOptions sslClientAuthenticationOptions)
        {
            SetAndVerifyValidationCallback(sslClientAuthenticationOptions.RemoteCertificateValidationCallback);
            SetAndVerifySelectionCallback(sslClientAuthenticationOptions.LocalCertificateSelectionCallback);

            ValidateCreateContext(sslClientAuthenticationOptions, _certValidationDelegate, _certSelectionDelegate);
            ProcessAuthentication(null);
        }

        public virtual void AuthenticateAsServer(X509Certificate serverCertificate)
        {
            AuthenticateAsServer(serverCertificate, false, SecurityProtocol.SystemDefaultSecurityProtocols, false);
        }

        public virtual void AuthenticateAsServer(X509Certificate serverCertificate, bool clientCertificateRequired, bool checkCertificateRevocation)
        {
            AuthenticateAsServer(serverCertificate, clientCertificateRequired, SecurityProtocol.SystemDefaultSecurityProtocols, checkCertificateRevocation);
        }

        public virtual void AuthenticateAsServer(X509Certificate serverCertificate, bool clientCertificateRequired, SslProtocols enabledSslProtocols, bool checkCertificateRevocation)
        {
            SslServerAuthenticationOptions options = new SslServerAuthenticationOptions
            {
                ServerCertificate = serverCertificate,
                ClientCertificateRequired = clientCertificateRequired,
                EnabledSslProtocols = enabledSslProtocols,
                CertificateRevocationCheckMode = checkCertificateRevocation ? X509RevocationMode.Online : X509RevocationMode.NoCheck,
                EncryptionPolicy = _encryptionPolicy,
            };

            AuthenticateAsServer(options);
        }

        private void AuthenticateAsServer(SslServerAuthenticationOptions sslServerAuthenticationOptions)
        {
            SetAndVerifyValidationCallback(sslServerAuthenticationOptions.RemoteCertificateValidationCallback);

            ValidateCreateContext(CreateAuthenticationOptions(sslServerAuthenticationOptions));
            ProcessAuthentication(null);
        }
        #endregion

        #region Task-based async public methods
        public virtual Task AuthenticateAsClientAsync(string targetHost) =>
            Task.Factory.FromAsync(
                (arg1, callback, state) => ((SslStream)state).BeginAuthenticateAsClient(arg1, callback, state),
                iar => ((SslStream)iar.AsyncState).EndAuthenticateAsClient(iar),
                targetHost,
                this);

        public virtual Task AuthenticateAsClientAsync(string targetHost, X509CertificateCollection clientCertificates, bool checkCertificateRevocation) =>
            Task.Factory.FromAsync(
                (arg1, arg2, arg3, callback, state) => ((SslStream)state).BeginAuthenticateAsClient(arg1, arg2, SecurityProtocol.SystemDefaultSecurityProtocols, arg3, callback, state),
                iar => ((SslStream)iar.AsyncState).EndAuthenticateAsClient(iar),
                targetHost, clientCertificates, checkCertificateRevocation,
                this);

        public virtual Task AuthenticateAsClientAsync(string targetHost, X509CertificateCollection clientCertificates, SslProtocols enabledSslProtocols, bool checkCertificateRevocation)
        {
            var beginMethod = checkCertificateRevocation ? (Func<string, X509CertificateCollection, SslProtocols, AsyncCallback, object, IAsyncResult>)
                ((arg1, arg2, arg3, callback, state) => ((SslStream)state).BeginAuthenticateAsClient(arg1, arg2, arg3, true, callback, state)) :
                ((arg1, arg2, arg3, callback, state) => ((SslStream)state).BeginAuthenticateAsClient(arg1, arg2, arg3, false, callback, state));
            return Task.Factory.FromAsync(
                beginMethod,
                iar => ((SslStream)iar.AsyncState).EndAuthenticateAsClient(iar),
                targetHost, clientCertificates, enabledSslProtocols,
                this);
        }

        public Task AuthenticateAsClientAsync(SslClientAuthenticationOptions sslClientAuthenticationOptions, CancellationToken cancellationToken)
        {
            return Task.Factory.FromAsync(
                (arg1, arg2, callback, state) => ((SslStream)state).BeginAuthenticateAsClient(arg1, arg2, callback, state),
                iar => ((SslStream)iar.AsyncState).EndAuthenticateAsClient(iar),
                sslClientAuthenticationOptions, cancellationToken,
                this);
        }

        public virtual Task AuthenticateAsServerAsync(X509Certificate serverCertificate) =>
            Task.Factory.FromAsync(
                (arg1, callback, state) => ((SslStream)state).BeginAuthenticateAsServer(arg1, callback, state),
                iar => ((SslStream)iar.AsyncState).EndAuthenticateAsServer(iar),
                serverCertificate,
                this);

        public virtual Task AuthenticateAsServerAsync(X509Certificate serverCertificate, bool clientCertificateRequired, bool checkCertificateRevocation) =>
            Task.Factory.FromAsync(
                (arg1, arg2, arg3, callback, state) => ((SslStream)state).BeginAuthenticateAsServer(arg1, arg2, SecurityProtocol.SystemDefaultSecurityProtocols, arg3, callback, state),
                iar => ((SslStream)iar.AsyncState).EndAuthenticateAsServer(iar),
                serverCertificate, clientCertificateRequired, checkCertificateRevocation,
                this);

        public virtual Task AuthenticateAsServerAsync(X509Certificate serverCertificate, bool clientCertificateRequired, SslProtocols enabledSslProtocols, bool checkCertificateRevocation)
        {
            var beginMethod = checkCertificateRevocation ? (Func<X509Certificate, bool, SslProtocols, AsyncCallback, object, IAsyncResult>)
                ((arg1, arg2, arg3, callback, state) => ((SslStream)state).BeginAuthenticateAsServer(arg1, arg2, arg3, true, callback, state)) :
                ((arg1, arg2, arg3, callback, state) => ((SslStream)state).BeginAuthenticateAsServer(arg1, arg2, arg3, false, callback, state));
            return Task.Factory.FromAsync(
                beginMethod,
                iar => ((SslStream)iar.AsyncState).EndAuthenticateAsServer(iar),
                serverCertificate, clientCertificateRequired, enabledSslProtocols,
                this);
        }

        public Task AuthenticateAsServerAsync(SslServerAuthenticationOptions sslServerAuthenticationOptions, CancellationToken cancellationToken)
        {
            return Task.Factory.FromAsync(
                (arg1, arg2, callback, state) => ((SslStream)state).BeginAuthenticateAsServer(arg1, arg2, callback, state),
                iar => ((SslStream)iar.AsyncState).EndAuthenticateAsServer(iar),
                sslServerAuthenticationOptions, cancellationToken,
                this);
        }

        public virtual Task ShutdownAsync() =>
            Task.Factory.FromAsync(
                (callback, state) => ((SslStream)state).BeginShutdown(callback, state),
                iar => ((SslStream)iar.AsyncState).EndShutdown(iar),
                this);
        #endregion

        public override bool IsAuthenticated => _context != null && _context.IsValidContext && _exception == null && _handshakeCompleted;

        public override bool IsMutuallyAuthenticated
        {
            get
            {
                return
                    IsAuthenticated &&
                    (_context.IsServer ? _context.LocalServerCertificate : _context.LocalClientCertificate) != null &&
                    _context.IsRemoteCertificateAvailable; /* does not work: Context.IsMutualAuthFlag;*/
            }
        }

        public override bool IsEncrypted => IsAuthenticated;

        public override bool IsSigned => IsAuthenticated;

        public override bool IsServer => _context != null && _context.IsServer;

        public virtual SslProtocols SslProtocol
        {
            get
            {
                CheckThrow(true);
                SslConnectionInfo info = _context.ConnectionInfo;
                if (info == null)
                {
                    return SslProtocols.None;
                }

                SslProtocols proto = (SslProtocols)info.Protocol;
                SslProtocols ret = SslProtocols.None;

#pragma warning disable 0618 // Ssl2, Ssl3 are deprecated.
                // Restore client/server bits so the result maps exactly on published constants.
                if ((proto & SslProtocols.Ssl2) != 0)
                {
                    ret |= SslProtocols.Ssl2;
                }

                if ((proto & SslProtocols.Ssl3) != 0)
                {
                    ret |= SslProtocols.Ssl3;
                }
#pragma warning restore

                if ((proto & SslProtocols.Tls) != 0)
                {
                    ret |= SslProtocols.Tls;
                }

                if ((proto & SslProtocols.Tls11) != 0)
                {
                    ret |= SslProtocols.Tls11;
                }

                if ((proto & SslProtocols.Tls12) != 0)
                {
                    ret |= SslProtocols.Tls12;
                }

                if ((proto & SslProtocols.Tls13) != 0)
                {
                    ret |= SslProtocols.Tls13;
                }

                return ret;
            }
        }

        public virtual bool CheckCertRevocationStatus => _context != null && _context.CheckCertRevocationStatus != X509RevocationMode.NoCheck;

        //
        // This will return selected local cert for both client/server streams
        //
        public virtual X509Certificate LocalCertificate
        {
            get
            {
                CheckThrow(true);
                return _context.IsServer ? _context.LocalServerCertificate : _context.LocalClientCertificate;
            }
        }

        public virtual X509Certificate RemoteCertificate
        {
            get
            {
                CheckThrow(true);
                _remoteCertificateExposed = true;
                return _remoteCertificate;
            }
        }

        [CLSCompliant(false)]
        public virtual TlsCipherSuite NegotiatedCipherSuite
        {
            get
            {
                CheckThrow(true);
                return _context.ConnectionInfo?.TlsCipherSuite ?? default(TlsCipherSuite);
            }
        }

        public virtual CipherAlgorithmType CipherAlgorithm
        {
            get
            {
                CheckThrow(true);
                SslConnectionInfo info = _context.ConnectionInfo;
                if (info == null)
                {
                    return CipherAlgorithmType.None;
                }
                return (CipherAlgorithmType)info.DataCipherAlg;
            }
        }

        public virtual int CipherStrength
        {
            get
            {
                CheckThrow(true);
                SslConnectionInfo info = _context.ConnectionInfo;
                if (info == null)
                {
                    return 0;
                }

                return info.DataKeySize;
            }
        }

        public virtual HashAlgorithmType HashAlgorithm
        {
            get
            {
                CheckThrow(true);
                SslConnectionInfo info = _context.ConnectionInfo;
                if (info == null)
                {
                    return (HashAlgorithmType)0;
                }
                return (HashAlgorithmType)info.DataHashAlg;
            }
        }

        public virtual int HashStrength
        {
            get
            {
                CheckThrow(true);
                SslConnectionInfo info = _context.ConnectionInfo;
                if (info == null)
                {
                    return 0;
                }

                return info.DataHashKeySize;
            }
        }

        public virtual ExchangeAlgorithmType KeyExchangeAlgorithm
        {
            get
            {
                CheckThrow(true);
                SslConnectionInfo info = _context.ConnectionInfo;
                if (info == null)
                {
                    return (ExchangeAlgorithmType)0;
                }

                return (ExchangeAlgorithmType)info.KeyExchangeAlg;
            }
        }

        public virtual int KeyExchangeStrength
        {
            get
            {
                CheckThrow(true);
                SslConnectionInfo info = _context.ConnectionInfo;
                if (info == null)
                {
                    return 0;
                }

                return info.KeyExchKeySize;
            }
        }

        //
        // Stream contract implementation.
        //
        public override bool CanSeek => false;

        public override bool CanRead => IsAuthenticated && InnerStream.CanRead;

        public override bool CanTimeout => InnerStream.CanTimeout;

        public override bool CanWrite => IsAuthenticated && InnerStream.CanWrite && !_shutdown;

        public override int ReadTimeout
        {
            get => InnerStream.ReadTimeout;
            set => InnerStream.ReadTimeout = value;
        }

        public override int WriteTimeout
        {
            get => InnerStream.WriteTimeout;
            set => InnerStream.WriteTimeout = value;
        }

        public override long Length => InnerStream.Length;

        public override long Position
        {
            get => InnerStream.Position;
            set => throw new NotSupportedException(SR.net_noseek);
        }

        public override void SetLength(long value) => InnerStream.SetLength(value);

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException(SR.net_noseek);

        public override void Flush() => InnerStream.Flush();

        public override Task FlushAsync(CancellationToken cancellationToken) => InnerStream.FlushAsync(cancellationToken);

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (!_remoteCertificateExposed)
                {
                    _remoteCertificate?.Dispose();
                    _remoteCertificate = null;
                    _remoteCertificateExposed = false;
                }
                CloseInternal();
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        public override async ValueTask DisposeAsync()
        {
            try
            {
                CloseInternal();
            }
            finally
            {
                await base.DisposeAsync().ConfigureAwait(false);
            }
        }

        public override int ReadByte()
        {
            CheckThrow(true);
            if (Interlocked.Exchange(ref _nestedRead, 1) == 1)
            {
                throw new NotSupportedException(SR.Format(SR.net_io_invalidnestedcall, "ReadByte", "read"));
            }

            // If there's any data in the buffer, take one byte, and we're done.
            try
            {
                if (_decryptedBytesCount > 0)
                {
                    int b = _internalBuffer[_decryptedBytesOffset++];
                    _decryptedBytesCount--;
                    ReturnReadBufferIfEmpty();
                    return b;
                }
            }
            finally
            {
                // Regardless of whether we were able to read a byte from the buffer,
                // reset the read tracking.  If we weren't able to read a byte, the
                // subsequent call to Read will set the flag again.
                _nestedRead = 0;
            }

            // Otherwise, fall back to reading a byte via Read, the same way Stream.ReadByte does.
            // This allocation is unfortunate but should be relatively rare, as it'll only occur once
            // per buffer fill internally by Read.
            byte[] oneByte = new byte[1];
            int bytesRead = Read(oneByte, 0, 1);
            Debug.Assert(bytesRead == 0 || bytesRead == 1);
            return bytesRead == 1 ? oneByte[0] : -1;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            CheckThrow(true);
            ValidateParameters(buffer, offset, count);
            SslReadSync reader = new SslReadSync(this);
            return ReadAsyncInternal(reader, new Memory<byte>(buffer, offset, count)).GetAwaiter().GetResult();
        }

        public void Write(byte[] buffer) => Write(buffer, 0, buffer.Length);

        public override void Write(byte[] buffer, int offset, int count)
        {
            CheckThrow(true);
            ValidateParameters(buffer, offset, count);

            SslWriteSync writeAdapter = new SslWriteSync(this);
            WriteAsyncInternal(writeAdapter, new ReadOnlyMemory<byte>(buffer, offset, count)).GetAwaiter().GetResult();
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback asyncCallback, object asyncState)
        {
            CheckThrow(true);
            return TaskToApm.Begin(ReadAsync(buffer, offset, count, CancellationToken.None), asyncCallback, asyncState);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            CheckThrow(true);
            return TaskToApm.End<int>(asyncResult);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback asyncCallback, object asyncState)
        {
            CheckThrow(true);
            return TaskToApm.Begin(WriteAsync(buffer, offset, count, CancellationToken.None), asyncCallback, asyncState);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            CheckThrow(true);
            TaskToApm.End(asyncResult);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            CheckThrow(true);
            ValidateParameters(buffer, offset, count);
            return WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).AsTask();
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            CheckThrow(true);
            SslWriteAsync writeAdapter = new SslWriteAsync(this, cancellationToken);
            return new ValueTask(WriteAsyncInternal(writeAdapter, buffer));
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            CheckThrow(true);
            ValidateParameters(buffer, offset, count);
            SslReadAsync read = new SslReadAsync(this, cancellationToken);
            return ReadAsyncInternal(read, new Memory<byte>(buffer, offset, count)).AsTask();
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            CheckThrow(true);
            SslReadAsync read = new SslReadAsync(this, cancellationToken);
            return ReadAsyncInternal(read, buffer);
        }
    }
}
