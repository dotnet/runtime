// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Security.Authentication;
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
        [System.ObsoleteAttribute(Obsoletions.EncryptionPolicyMessage, DiagnosticId = Obsoletions.EncryptionPolicyDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        AllowNoEncryption,

        // Request null ciphers only
        [System.ObsoleteAttribute(Obsoletions.EncryptionPolicyMessage, DiagnosticId = Obsoletions.EncryptionPolicyDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        NoEncryption
    }

    // A user delegate used to verify remote SSL certificate.
    public delegate bool RemoteCertificateValidationCallback(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors);

    // A user delegate used to select local SSL certificate.
    public delegate X509Certificate LocalCertificateSelectionCallback(object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate? remoteCertificate, string[] acceptableIssuers);

    public delegate X509Certificate ServerCertificateSelectionCallback(object sender, string? hostName);

    public delegate ValueTask<SslServerAuthenticationOptions> ServerOptionsSelectionCallback(SslStream stream, SslClientHelloInfo clientHelloInfo, object? state, CancellationToken cancellationToken);

    public partial class SslStream : AuthenticatedStream
    {
        /// <summary>Set as the _exception when the instance is disposed.</summary>
        private static readonly ExceptionDispatchInfo s_disposedSentinel = ExceptionDispatchInfo.Capture(new ObjectDisposedException(nameof(SslStream), (string?)null));

        private ExceptionDispatchInfo? _exception;
        private bool _shutdown;
        private bool _handshakeCompleted;

        // FrameOverhead = 5 byte header + HMAC trailer + padding (if block cipher)
        // HMAC: 32 bytes for SHA-256 or 20 bytes for SHA-1 or 16 bytes for the MD5
        private const int FrameOverhead = 64;
        private const int InitialHandshakeBufferSize = 4096 + FrameOverhead; // try to fit at least 4K ServerCertificate
        private const int ReadBufferSize = 4096 * 4 + FrameOverhead;         // We read in 16K chunks + headers.

        private SslBuffer _buffer;

        // internal buffer for storing incoming data. Wrapper around ArrayBuffer which adds
        // separation between decrypted and still encrypted part of the active region.
        //   - Encrypted: Contains incoming TLS frames, the last such frame may be incomplete
        //   - Decrypted: Contains decrypted data from *one* TLS frame which have not been read by the user yet.
        private struct SslBuffer
        {
            private ArrayBuffer _buffer;
            private int _decryptedLength;

            // padding between decrypted part of the active memory and following undecrypted TLS frame.
            private int _decryptedPadding;

            private bool _isValid;

            public SslBuffer(int initialSize)
            {
                _buffer = new ArrayBuffer(initialSize, true);
                _decryptedLength = 0;
                _decryptedPadding = 0;
                _isValid = true;
            }

            public bool IsValid => _isValid;

            public Span<byte> DecryptedSpan => _buffer.ActiveSpan.Slice(0, _decryptedLength);

            public ReadOnlySpan<byte> DecryptedReadOnlySpanSliced(int length)
            {
                Debug.Assert(length <= DecryptedLength, "length <= DecryptedLength");
                return _buffer.ActiveSpan.Slice(0, length);
            }

            public int DecryptedLength => _decryptedLength;

            public int ActiveLength => _buffer.ActiveLength;

            public Span<byte> EncryptedSpanSliced(int length) => _buffer.ActiveSpan.Slice(_decryptedLength + _decryptedPadding, length);

            public ReadOnlySpan<byte> EncryptedReadOnlySpan => _buffer.ActiveSpan.Slice(_decryptedLength + _decryptedPadding);

            public int EncryptedLength => _buffer.ActiveLength - _decryptedPadding - _decryptedLength;

            public Memory<byte> AvailableMemory => _buffer.AvailableMemory;

            public int AvailableLength => _buffer.AvailableLength;

            public int Capacity => _buffer.Capacity;

            public void Commit(int byteCount) => _buffer.Commit(byteCount);

            public void EnsureAvailableSpace(int byteCount)
            {
                if (_isValid)
                {
                    _buffer.EnsureAvailableSpace(byteCount);
                }
                else
                {
                    _isValid = true;
                    _buffer = new ArrayBuffer(byteCount, true);
                }
            }

            public void Discard(int byteCount)
            {
                Debug.Assert(byteCount <= _decryptedLength, "byteCount <= _decryptedBytes");

                _buffer.Discard(byteCount);
                _decryptedLength -= byteCount;

                // if drained all decrypted data, discard also the tail of the frame so that only
                // encrypted part of the active memory of the _buffer remains
                if (_decryptedLength == 0)
                {
                    _buffer.Discard(_decryptedPadding);
                    _decryptedPadding = 0;
                }
            }

            public void DiscardEncrypted(int byteCount)
            {
                // should be called only during handshake -> no pending decrypted data
                Debug.Assert(_decryptedLength == 0, "_decryptedBytes == 0");
                Debug.Assert(_decryptedPadding == 0, "_encryptedOffset == 0");

                _buffer.Discard(byteCount);
            }

            public void OnDecrypted(int decryptedOffset, int decryptedCount, int frameSize)
            {
                Debug.Assert(_decryptedLength == 0, "_decryptedBytes == 0");
                Debug.Assert(_decryptedPadding == 0, "_encryptedOffset == 0");

                if (decryptedCount > 0)
                {
                    // discard padding before decrypted contents
                    _buffer.Discard(decryptedOffset);

                    _decryptedPadding = frameSize - decryptedOffset - decryptedCount;
                    _decryptedLength = decryptedCount;
                }
                else
                {
                    // No user data available, discard entire frame
                    _buffer.Discard(frameSize);
                }
            }

            public void ReturnBuffer()
            {
                _buffer.Dispose();
                _decryptedLength = 0;
                _decryptedPadding = 0;
                _isValid = false;
            }
        }

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

        public SslStream(Stream innerStream, bool leaveInnerStreamOpen, RemoteCertificateValidationCallback? userCertificateValidationCallback)
                : this(innerStream, leaveInnerStreamOpen, userCertificateValidationCallback, null, EncryptionPolicy.RequireEncryption)
        {
        }

        public SslStream(Stream innerStream, bool leaveInnerStreamOpen, RemoteCertificateValidationCallback? userCertificateValidationCallback,
            LocalCertificateSelectionCallback? userCertificateSelectionCallback)
                : this(innerStream, leaveInnerStreamOpen, userCertificateValidationCallback, userCertificateSelectionCallback, EncryptionPolicy.RequireEncryption)
        {
        }

        public SslStream(Stream innerStream, bool leaveInnerStreamOpen, RemoteCertificateValidationCallback? userCertificateValidationCallback,
            LocalCertificateSelectionCallback? userCertificateSelectionCallback, EncryptionPolicy encryptionPolicy)
            : base(innerStream, leaveInnerStreamOpen)
        {
#pragma warning disable SYSLIB0040 // NoEncryption and AllowNoEncryption are obsolete
            if (encryptionPolicy != EncryptionPolicy.RequireEncryption && encryptionPolicy != EncryptionPolicy.AllowNoEncryption && encryptionPolicy != EncryptionPolicy.NoEncryption)
            {
                throw new ArgumentException(SR.Format(SR.net_invalid_enum, "EncryptionPolicy"), nameof(encryptionPolicy));
            }
#pragma warning restore SYSLIB0040

            _sslAuthenticationOptions.EncryptionPolicy = encryptionPolicy;
            _sslAuthenticationOptions.CertValidationDelegate = userCertificateValidationCallback;
            _sslAuthenticationOptions.CertSelectionDelegate = userCertificateSelectionCallback;

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Log.SslStreamCtor(this, innerStream);
        }

        //
        // Client side auth.
        //
        public virtual IAsyncResult BeginAuthenticateAsClient(string targetHost, AsyncCallback? asyncCallback, object? asyncState)
        {
            return BeginAuthenticateAsClient(targetHost, null, SecurityProtocol.SystemDefaultSecurityProtocols, false,
                                           asyncCallback, asyncState);
        }

        public virtual IAsyncResult BeginAuthenticateAsClient(string targetHost, X509CertificateCollection? clientCertificates,
                                                            bool checkCertificateRevocation, AsyncCallback? asyncCallback, object? asyncState)
        {
            return BeginAuthenticateAsClient(targetHost, clientCertificates, SecurityProtocol.SystemDefaultSecurityProtocols, checkCertificateRevocation, asyncCallback, asyncState);
        }

        public virtual IAsyncResult BeginAuthenticateAsClient(string targetHost, X509CertificateCollection? clientCertificates,
                                                            SslProtocols enabledSslProtocols, bool checkCertificateRevocation,
                                                            AsyncCallback? asyncCallback, object? asyncState)
        {
            SslClientAuthenticationOptions options = new SslClientAuthenticationOptions
            {
                TargetHost = targetHost,
                ClientCertificates = clientCertificates,
                EnabledSslProtocols = enabledSslProtocols,
                CertificateRevocationCheckMode = checkCertificateRevocation ? X509RevocationMode.Online : X509RevocationMode.NoCheck,
                EncryptionPolicy = _sslAuthenticationOptions.EncryptionPolicy,
            };

            return BeginAuthenticateAsClient(options, CancellationToken.None, asyncCallback, asyncState);
        }

        internal IAsyncResult BeginAuthenticateAsClient(SslClientAuthenticationOptions sslClientAuthenticationOptions, CancellationToken cancellationToken, AsyncCallback? asyncCallback, object? asyncState) =>
            TaskToApm.Begin(AuthenticateAsClientAsync(sslClientAuthenticationOptions, cancellationToken)!, asyncCallback, asyncState);

        public virtual void EndAuthenticateAsClient(IAsyncResult asyncResult) => TaskToApm.End(asyncResult);

        //
        // Server side auth.
        //
        public virtual IAsyncResult BeginAuthenticateAsServer(X509Certificate serverCertificate, AsyncCallback? asyncCallback, object? asyncState)

        {
            return BeginAuthenticateAsServer(serverCertificate, false, SecurityProtocol.SystemDefaultSecurityProtocols, false,
                                                          asyncCallback,
                                                          asyncState);
        }

        public virtual IAsyncResult BeginAuthenticateAsServer(X509Certificate serverCertificate, bool clientCertificateRequired,
                                                            bool checkCertificateRevocation, AsyncCallback? asyncCallback, object? asyncState)
        {
            return BeginAuthenticateAsServer(serverCertificate, clientCertificateRequired, SecurityProtocol.SystemDefaultSecurityProtocols, checkCertificateRevocation, asyncCallback, asyncState);
        }

        public virtual IAsyncResult BeginAuthenticateAsServer(X509Certificate serverCertificate, bool clientCertificateRequired,
                                                            SslProtocols enabledSslProtocols, bool checkCertificateRevocation,
                                                            AsyncCallback? asyncCallback,
                                                            object? asyncState)
        {
            SslServerAuthenticationOptions options = new SslServerAuthenticationOptions
            {
                ServerCertificate = serverCertificate,
                ClientCertificateRequired = clientCertificateRequired,
                EnabledSslProtocols = enabledSslProtocols,
                CertificateRevocationCheckMode = checkCertificateRevocation ? X509RevocationMode.Online : X509RevocationMode.NoCheck,
                EncryptionPolicy = _sslAuthenticationOptions.EncryptionPolicy,
            };

            return BeginAuthenticateAsServer(options, CancellationToken.None, asyncCallback, asyncState);
        }

        private IAsyncResult BeginAuthenticateAsServer(SslServerAuthenticationOptions sslServerAuthenticationOptions, CancellationToken cancellationToken, AsyncCallback? asyncCallback, object? asyncState) =>
            TaskToApm.Begin(AuthenticateAsServerAsync(sslServerAuthenticationOptions, cancellationToken)!, asyncCallback, asyncState);

        public virtual void EndAuthenticateAsServer(IAsyncResult asyncResult) => TaskToApm.End(asyncResult);

        internal IAsyncResult BeginShutdown(AsyncCallback? asyncCallback, object? asyncState) => TaskToApm.Begin(ShutdownAsync(), asyncCallback, asyncState);

        internal static void EndShutdown(IAsyncResult asyncResult) => TaskToApm.End(asyncResult);

        public TransportContext TransportContext => new SslStreamContext(this);

        #region Synchronous methods
        public virtual void AuthenticateAsClient(string targetHost)
        {
            AuthenticateAsClient(targetHost, null, SecurityProtocol.SystemDefaultSecurityProtocols, false);
        }

        public virtual void AuthenticateAsClient(string targetHost, X509CertificateCollection? clientCertificates, bool checkCertificateRevocation)
        {
            AuthenticateAsClient(targetHost, clientCertificates, SecurityProtocol.SystemDefaultSecurityProtocols, checkCertificateRevocation);
        }

        public virtual void AuthenticateAsClient(string targetHost, X509CertificateCollection? clientCertificates, SslProtocols enabledSslProtocols, bool checkCertificateRevocation)
        {
            SslClientAuthenticationOptions options = new SslClientAuthenticationOptions
            {
                TargetHost = targetHost,
                ClientCertificates = clientCertificates,
                EnabledSslProtocols = enabledSslProtocols,
                CertificateRevocationCheckMode = checkCertificateRevocation ? X509RevocationMode.Online : X509RevocationMode.NoCheck,
                EncryptionPolicy = _sslAuthenticationOptions.EncryptionPolicy,
            };

            AuthenticateAsClient(options);
        }

        public void AuthenticateAsClient(SslClientAuthenticationOptions sslClientAuthenticationOptions)
        {
            ArgumentNullException.ThrowIfNull(sslClientAuthenticationOptions);

            ThrowIfExceptional();

            _sslAuthenticationOptions.UpdateOptions(sslClientAuthenticationOptions);
            ProcessAuthenticationAsync().GetAwaiter().GetResult();
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
                EncryptionPolicy = _sslAuthenticationOptions.EncryptionPolicy,
            };

            AuthenticateAsServer(options);
        }

        public void AuthenticateAsServer(SslServerAuthenticationOptions sslServerAuthenticationOptions)
        {
            ArgumentNullException.ThrowIfNull(sslServerAuthenticationOptions);

            _sslAuthenticationOptions.UpdateOptions(sslServerAuthenticationOptions);
            ProcessAuthenticationAsync().GetAwaiter().GetResult();
        }
        #endregion

        #region Task-based async public methods
        public virtual Task AuthenticateAsClientAsync(string targetHost) => AuthenticateAsClientAsync(targetHost, null, false);

        public virtual Task AuthenticateAsClientAsync(string targetHost, X509CertificateCollection? clientCertificates, bool checkCertificateRevocation) => AuthenticateAsClientAsync(targetHost, clientCertificates, SecurityProtocol.SystemDefaultSecurityProtocols, checkCertificateRevocation);

        public virtual Task AuthenticateAsClientAsync(string targetHost, X509CertificateCollection? clientCertificates, SslProtocols enabledSslProtocols, bool checkCertificateRevocation)
        {
            SslClientAuthenticationOptions options = new SslClientAuthenticationOptions()
            {
                TargetHost = targetHost,
                ClientCertificates = clientCertificates,
                EnabledSslProtocols = enabledSslProtocols,
                CertificateRevocationCheckMode = checkCertificateRevocation ? X509RevocationMode.Online : X509RevocationMode.NoCheck,
                EncryptionPolicy = _sslAuthenticationOptions.EncryptionPolicy,
            };

            return AuthenticateAsClientAsync(options);
        }

        public Task AuthenticateAsClientAsync(SslClientAuthenticationOptions sslClientAuthenticationOptions, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(sslClientAuthenticationOptions);

            ThrowIfExceptional();
            _sslAuthenticationOptions.UpdateOptions(sslClientAuthenticationOptions);
            return ProcessAuthenticationAsync(isAsync: true, cancellationToken);
        }

        public virtual Task AuthenticateAsServerAsync(X509Certificate serverCertificate) =>
            AuthenticateAsServerAsync(serverCertificate, false, SecurityProtocol.SystemDefaultSecurityProtocols, false);

        public virtual Task AuthenticateAsServerAsync(X509Certificate serverCertificate, bool clientCertificateRequired, bool checkCertificateRevocation)
        {
            SslServerAuthenticationOptions options = new SslServerAuthenticationOptions
            {
                ServerCertificate = serverCertificate,
                ClientCertificateRequired = clientCertificateRequired,
                CertificateRevocationCheckMode = checkCertificateRevocation ? X509RevocationMode.Online : X509RevocationMode.NoCheck,
                EncryptionPolicy = _sslAuthenticationOptions.EncryptionPolicy,
            };

            return AuthenticateAsServerAsync(options);
        }

        public virtual Task AuthenticateAsServerAsync(X509Certificate serverCertificate, bool clientCertificateRequired, SslProtocols enabledSslProtocols, bool checkCertificateRevocation)
        {
            SslServerAuthenticationOptions options = new SslServerAuthenticationOptions
            {
                ServerCertificate = serverCertificate,
                ClientCertificateRequired = clientCertificateRequired,
                EnabledSslProtocols = enabledSslProtocols,
                CertificateRevocationCheckMode = checkCertificateRevocation ? X509RevocationMode.Online : X509RevocationMode.NoCheck,
                EncryptionPolicy = _sslAuthenticationOptions.EncryptionPolicy,
            };

            return AuthenticateAsServerAsync(options);
        }

        public Task AuthenticateAsServerAsync(SslServerAuthenticationOptions sslServerAuthenticationOptions, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(sslServerAuthenticationOptions);
            _sslAuthenticationOptions.UpdateOptions(sslServerAuthenticationOptions);
            return ProcessAuthenticationAsync(isAsync: true, cancellationToken);
        }

        public Task AuthenticateAsServerAsync(ServerOptionsSelectionCallback optionsCallback, object? state, CancellationToken cancellationToken = default)
        {
            _sslAuthenticationOptions.UpdateOptions(optionsCallback, state);

            return ProcessAuthenticationAsync(isAsync: true, cancellationToken);
        }

        public virtual Task ShutdownAsync()
        {
            ThrowIfExceptionalOrNotAuthenticatedOrShutdown();

            ProtocolToken message = CreateShutdownToken()!;
            _shutdown = true;
            return InnerStream.WriteAsync(message.Payload, default).AsTask();
        }
        #endregion

        public override bool IsAuthenticated => IsValidContext && _exception == null && _handshakeCompleted;

        public override bool IsMutuallyAuthenticated
        {
            get
            {
                return
                    IsAuthenticated &&
                    (IsServer ? LocalServerCertificate : LocalClientCertificate) != null &&
                    IsRemoteCertificateAvailable; /* does not work: Context.IsMutualAuthFlag;*/
            }
        }

        public override bool IsEncrypted => IsAuthenticated;

        public override bool IsSigned => IsAuthenticated;

        public override bool IsServer => _sslAuthenticationOptions.IsServer;

        public virtual SslProtocols SslProtocol
        {
            get
            {
                ThrowIfExceptionalOrNotHandshake();
                return GetSslProtocolInternal();
            }
        }

        // Skips the ThrowIfExceptionalOrNotHandshake() check
        private SslProtocols GetSslProtocolInternal()
        {
            if (_connectionInfo.Protocol == 0)
            {
                return SslProtocols.None;
            }

            SslProtocols proto = (SslProtocols)_connectionInfo.Protocol;
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

#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
            if ((proto & SslProtocols.Tls) != 0)
            {
                ret |= SslProtocols.Tls;
            }

            if ((proto & SslProtocols.Tls11) != 0)
            {
                ret |= SslProtocols.Tls11;
            }
#pragma warning restore SYSLIB0039

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

        public virtual bool CheckCertRevocationStatus => _sslAuthenticationOptions.CertificateRevocationCheckMode != X509RevocationMode.NoCheck;

        //
        // This will return selected local cert for both client/server streams
        //
        public virtual X509Certificate? LocalCertificate
        {
            get
            {
                ThrowIfExceptionalOrNotAuthenticated();
                return IsServer ? LocalServerCertificate : LocalClientCertificate;
            }
        }


        public virtual X509Certificate? RemoteCertificate
        {
            get
            {
                ThrowIfExceptionalOrNotAuthenticated();
                _remoteCertificateExposed = true;
                return _remoteCertificate;
            }
        }

        public SslApplicationProtocol NegotiatedApplicationProtocol
        {
            get
            {
                ThrowIfExceptionalOrNotHandshake();
                return _connectionInfo.ApplicationProtocol != null ? new SslApplicationProtocol(_connectionInfo.ApplicationProtocol, false) : default;
            }
        }

        [CLSCompliant(false)]
        public virtual TlsCipherSuite NegotiatedCipherSuite
        {
            get
            {
                ThrowIfExceptionalOrNotHandshake();
                return _connectionInfo.TlsCipherSuite;
            }
        }

        public virtual CipherAlgorithmType CipherAlgorithm
        {
            get
            {
                ThrowIfExceptionalOrNotHandshake();
                return (CipherAlgorithmType)_connectionInfo.DataCipherAlg;
            }
        }

        public virtual int CipherStrength
        {
            get
            {
                ThrowIfExceptionalOrNotHandshake();
                return _connectionInfo.DataKeySize;
            }
        }

        public virtual HashAlgorithmType HashAlgorithm
        {
            get
            {
                ThrowIfExceptionalOrNotHandshake();
                return (HashAlgorithmType)_connectionInfo.DataHashAlg;
            }
        }

        public virtual int HashStrength
        {
            get
            {
                ThrowIfExceptionalOrNotHandshake();
                return _connectionInfo.DataHashKeySize;
            }
        }

        public virtual ExchangeAlgorithmType KeyExchangeAlgorithm
        {
            get
            {
                ThrowIfExceptionalOrNotHandshake();
                return (ExchangeAlgorithmType)_connectionInfo.KeyExchangeAlg;
            }
        }

        public virtual int KeyExchangeStrength
        {
            get
            {
                ThrowIfExceptionalOrNotHandshake();
                return _connectionInfo.KeyExchKeySize;
            }
        }

        public string TargetHostName
        {
            get
            {
                return _sslAuthenticationOptions.TargetHost;
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

        public virtual Task NegotiateClientCertificateAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfExceptionalOrNotAuthenticated();
            if (RemoteCertificate != null)
            {
                throw new InvalidOperationException(SR.net_ssl_certificate_exist);
            }

            return RenegotiateAsync<AsyncReadWriteAdapter>(cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
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
            ThrowIfExceptionalOrNotAuthenticated();
            if (Interlocked.Exchange(ref _nestedRead, 1) == 1)
            {
                throw new NotSupportedException(SR.Format(SR.net_io_invalidnestedcall, "read"));
            }

            // If there's any data in the buffer, take one byte, and we're done.
            try
            {
                if (_buffer.DecryptedLength > 0)
                {
                    int b = _buffer.DecryptedSpan[0];
                    _buffer.Discard(1);
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
            ThrowIfExceptionalOrNotAuthenticated();
            ValidateBufferArguments(buffer, offset, count);
            ValueTask<int> vt = ReadAsyncInternal<SyncReadWriteAdapter>(new Memory<byte>(buffer, offset, count), default(CancellationToken));
            Debug.Assert(vt.IsCompleted, "Sync operation must have completed synchronously");
            return vt.GetAwaiter().GetResult();
        }

        public void Write(byte[] buffer) => Write(buffer, 0, buffer.Length);

        public override void Write(byte[] buffer, int offset, int count)
        {
            ThrowIfExceptionalOrNotAuthenticated();
            ValidateBufferArguments(buffer, offset, count);

            ValueTask vt = WriteAsyncInternal<SyncReadWriteAdapter>(new ReadOnlyMemory<byte>(buffer, offset, count), default(CancellationToken));
            Debug.Assert(vt.IsCompleted, "Sync operation must have completed synchronously");
            vt.GetAwaiter().GetResult();
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? asyncCallback, object? asyncState)
        {
            ThrowIfExceptionalOrNotAuthenticated();
            return TaskToApm.Begin(ReadAsync(buffer, offset, count, CancellationToken.None), asyncCallback, asyncState);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            ThrowIfExceptionalOrNotAuthenticated();
            return TaskToApm.End<int>(asyncResult);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? asyncCallback, object? asyncState)
        {
            ThrowIfExceptionalOrNotAuthenticated();
            return TaskToApm.Begin(WriteAsync(buffer, offset, count, CancellationToken.None), asyncCallback, asyncState);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            ThrowIfExceptionalOrNotAuthenticated();
            TaskToApm.End(asyncResult);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ThrowIfExceptionalOrNotAuthenticated();
            ValidateBufferArguments(buffer, offset, count);
            return WriteAsyncInternal<AsyncReadWriteAdapter>(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).AsTask();
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ThrowIfExceptionalOrNotAuthenticated();
            return WriteAsyncInternal<AsyncReadWriteAdapter>(buffer, cancellationToken);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ThrowIfExceptionalOrNotAuthenticated();
            ValidateBufferArguments(buffer, offset, count);
            return ReadAsyncInternal<AsyncReadWriteAdapter>(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ThrowIfExceptionalOrNotAuthenticated();
            return ReadAsyncInternal<AsyncReadWriteAdapter>(buffer, cancellationToken);
        }

        private void ThrowIfExceptional()
        {
            ExceptionDispatchInfo? e = _exception;
            if (e != null)
            {
                ThrowExceptional(e);
            }

            // Local function to make the check method more inline friendly.
            static void ThrowExceptional(ExceptionDispatchInfo e)
            {
                // If the stored exception just indicates disposal, throw a new ODE rather than the stored one,
                // so as to not continually build onto the shared exception's stack.
                if (ReferenceEquals(e, s_disposedSentinel))
                {
                    throw new ObjectDisposedException(nameof(SslStream));
                }

                // Throw the stored exception.
                e.Throw();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfExceptionalOrNotAuthenticated()
        {
            ThrowIfExceptional();

            if (!IsAuthenticated)
            {
                ThrowNotAuthenticated();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfExceptionalOrNotHandshake()
        {
            ThrowIfExceptional();

            if (!IsAuthenticated)
            {
                ThrowNotAuthenticated();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfExceptionalOrNotAuthenticatedOrShutdown()
        {
            ThrowIfExceptional();

            if (!IsAuthenticated)
            {
                ThrowNotAuthenticated();
            }

            if (_shutdown)
            {
                ThrowAlreadyShutdown();
            }

            // Local function to make the check method more inline friendly.
            static void ThrowAlreadyShutdown()
            {
                throw new InvalidOperationException(SR.net_ssl_io_already_shutdown);
            }
        }

        // Static non-returning throw method to make the check methods more inline friendly.
        private static void ThrowNotAuthenticated()
        {
            throw new InvalidOperationException(SR.net_auth_noauth);
        }
    }
}
