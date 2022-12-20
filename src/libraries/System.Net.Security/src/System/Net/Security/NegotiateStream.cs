// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.Versioning;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Security
{
    /// <summary>
    /// Provides a stream that uses the Negotiate security protocol to authenticate the client, and optionally the server, in client-server communication.
    /// </summary>
    [UnsupportedOSPlatform("tvos")]
    public partial class NegotiateStream : AuthenticatedStream
    {
        /// <summary>Set as the _exception when the instance is disposed.</summary>
        private static readonly ExceptionDispatchInfo s_disposedSentinel = ExceptionDispatchInfo.Capture(new ObjectDisposedException(nameof(NegotiateStream), (string?)null));

        private const int ERROR_TRUST_FAILURE = 1790;   // Used to serialize protectionLevel or impersonationLevel mismatch error to the remote side.
        private const int MaxReadFrameSize = 64 * 1024;
        private const int MaxWriteDataSize = 63 * 1024; // 1k for the framing and trailer that is always less as per SSPI.
        private const string DefaultPackage = NegotiationInfoClass.Negotiate;

#pragma warning disable CA1825 // used in reference comparison, requires unique object identity
        private static readonly byte[] s_emptyMessage = new byte[0];
#pragma warning restore CA1825

        private readonly byte[] _readHeader;
        private IIdentity? _remoteIdentity;
        private byte[] _readBuffer;
        private int _readBufferOffset;
        private int _readBufferCount;
        private byte[]? _writeBuffer;

        private volatile int _writeInProgress;
        private volatile int _readInProgress;
        private volatile int _authInProgress;

        private ExceptionDispatchInfo? _exception;
        private StreamFramer? _framer;
        private NTAuthentication? _context;
        private bool _canRetryAuthentication;
        private ProtectionLevel _expectedProtectionLevel;
        private TokenImpersonationLevel _expectedImpersonationLevel;
        private ExtendedProtectionPolicy? _extendedProtectionPolicy;

        /// <summary>
        /// SSPI does not send a server ack on successful auth.
        /// This is a state variable used to gracefully handle auth confirmation.
        /// </summary>
        private bool _remoteOk;

        public NegotiateStream(Stream innerStream) : this(innerStream, false)
        {
        }

        public NegotiateStream(Stream innerStream, bool leaveInnerStreamOpen) : base(innerStream, leaveInnerStreamOpen)
        {
            _readHeader = new byte[4];
            _readBuffer = Array.Empty<byte>();
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                _exception = s_disposedSentinel;
                _context?.CloseContext();
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
                _exception = s_disposedSentinel;
                _context?.CloseContext();
            }
            finally
            {
                await base.DisposeAsync().ConfigureAwait(false);
            }
        }

        public virtual IAsyncResult BeginAuthenticateAsClient(AsyncCallback? asyncCallback, object? asyncState) =>
            BeginAuthenticateAsClient((NetworkCredential)CredentialCache.DefaultCredentials, binding: null, string.Empty, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification,
                                      asyncCallback, asyncState);

        public virtual IAsyncResult BeginAuthenticateAsClient(NetworkCredential credential, string targetName, AsyncCallback? asyncCallback, object? asyncState) =>
            BeginAuthenticateAsClient(credential, binding: null, targetName, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification,
                                      asyncCallback, asyncState);

        public virtual IAsyncResult BeginAuthenticateAsClient(NetworkCredential credential, ChannelBinding? binding, string targetName, AsyncCallback? asyncCallback, object? asyncState) =>
            BeginAuthenticateAsClient(credential, binding, targetName, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification,
                                      asyncCallback, asyncState);

        public virtual IAsyncResult BeginAuthenticateAsClient(
            NetworkCredential credential, string targetName, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel allowedImpersonationLevel,
            AsyncCallback? asyncCallback, object? asyncState) =>
            BeginAuthenticateAsClient(credential, binding: null, targetName, requiredProtectionLevel, allowedImpersonationLevel,
                                      asyncCallback, asyncState);

        public virtual IAsyncResult BeginAuthenticateAsClient(
            NetworkCredential credential, ChannelBinding? binding, string targetName, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel allowedImpersonationLevel,
            AsyncCallback? asyncCallback, object? asyncState) =>
            TaskToApm.Begin(AuthenticateAsClientAsync(credential, binding, targetName, requiredProtectionLevel, allowedImpersonationLevel), asyncCallback, asyncState);

        public virtual void EndAuthenticateAsClient(IAsyncResult asyncResult) => TaskToApm.End(asyncResult);

        public virtual void AuthenticateAsServer() =>
            AuthenticateAsServer((NetworkCredential)CredentialCache.DefaultCredentials, policy: null, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);

        public virtual void AuthenticateAsServer(ExtendedProtectionPolicy? policy) =>
            AuthenticateAsServer((NetworkCredential)CredentialCache.DefaultCredentials, policy, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);

        public virtual void AuthenticateAsServer(NetworkCredential credential, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel requiredImpersonationLevel) =>
            AuthenticateAsServer(credential, policy: null, requiredProtectionLevel, requiredImpersonationLevel);

        public virtual void AuthenticateAsServer(NetworkCredential credential, ExtendedProtectionPolicy? policy, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel requiredImpersonationLevel)
        {
            ValidateCreateContext(DefaultPackage, credential, string.Empty, policy, requiredProtectionLevel, requiredImpersonationLevel);
            AuthenticateAsync<SyncReadWriteAdapter>(default(CancellationToken)).GetAwaiter().GetResult();
        }

        public virtual IAsyncResult BeginAuthenticateAsServer(AsyncCallback? asyncCallback, object? asyncState) =>
            BeginAuthenticateAsServer((NetworkCredential)CredentialCache.DefaultCredentials, policy: null, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification, asyncCallback, asyncState);

        public virtual IAsyncResult BeginAuthenticateAsServer(ExtendedProtectionPolicy? policy, AsyncCallback? asyncCallback, object? asyncState) =>
            BeginAuthenticateAsServer((NetworkCredential)CredentialCache.DefaultCredentials, policy, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification, asyncCallback, asyncState);

        public virtual IAsyncResult BeginAuthenticateAsServer(
            NetworkCredential credential, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel requiredImpersonationLevel,
            AsyncCallback? asyncCallback, object? asyncState) =>
            BeginAuthenticateAsServer(credential, policy: null, requiredProtectionLevel, requiredImpersonationLevel, asyncCallback, asyncState);

        public virtual IAsyncResult BeginAuthenticateAsServer(
            NetworkCredential credential, ExtendedProtectionPolicy? policy, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel requiredImpersonationLevel,
            AsyncCallback? asyncCallback, object? asyncState) =>
            TaskToApm.Begin(AuthenticateAsServerAsync(credential, policy, requiredProtectionLevel, requiredImpersonationLevel), asyncCallback, asyncState);

        public virtual void EndAuthenticateAsServer(IAsyncResult asyncResult) => TaskToApm.End(asyncResult);

        public virtual void AuthenticateAsClient() =>
            AuthenticateAsClient((NetworkCredential)CredentialCache.DefaultCredentials, binding: null, string.Empty, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);

        public virtual void AuthenticateAsClient(NetworkCredential credential, string targetName) =>
            AuthenticateAsClient(credential, binding: null, targetName, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);

        public virtual void AuthenticateAsClient(NetworkCredential credential, ChannelBinding? binding, string targetName) =>
            AuthenticateAsClient(credential, binding, targetName, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);

        public virtual void AuthenticateAsClient(
            NetworkCredential credential, string targetName, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel allowedImpersonationLevel) =>
            AuthenticateAsClient(credential, binding: null, targetName, requiredProtectionLevel, allowedImpersonationLevel);

        public virtual void AuthenticateAsClient(
            NetworkCredential credential, ChannelBinding? binding, string targetName, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel allowedImpersonationLevel)
        {
            ValidateCreateContext(DefaultPackage, isServer: false, credential, targetName, binding, requiredProtectionLevel, allowedImpersonationLevel);
            AuthenticateAsync<SyncReadWriteAdapter>(default(CancellationToken)).GetAwaiter().GetResult();
        }

        public virtual Task AuthenticateAsClientAsync() =>
            AuthenticateAsClientAsync((NetworkCredential)CredentialCache.DefaultCredentials, binding: null, string.Empty, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);

        public virtual Task AuthenticateAsClientAsync(NetworkCredential credential, string targetName) =>
            AuthenticateAsClientAsync(credential, binding: null, targetName, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);

        public virtual Task AuthenticateAsClientAsync(
            NetworkCredential credential, string targetName,
            ProtectionLevel requiredProtectionLevel,
            TokenImpersonationLevel allowedImpersonationLevel) =>
            AuthenticateAsClientAsync(credential, binding: null, targetName, requiredProtectionLevel, allowedImpersonationLevel);

        public virtual Task AuthenticateAsClientAsync(NetworkCredential credential, ChannelBinding? binding, string targetName) =>
            AuthenticateAsClientAsync(credential, binding, targetName, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);

        public virtual Task AuthenticateAsClientAsync(
            NetworkCredential credential, ChannelBinding? binding, string targetName, ProtectionLevel requiredProtectionLevel,
            TokenImpersonationLevel allowedImpersonationLevel)
        {
            ValidateCreateContext(DefaultPackage, isServer: false, credential, targetName, binding, requiredProtectionLevel, allowedImpersonationLevel);
            return AuthenticateAsync<AsyncReadWriteAdapter>(default(CancellationToken));
        }

        public virtual Task AuthenticateAsServerAsync() =>
            AuthenticateAsServerAsync((NetworkCredential)CredentialCache.DefaultCredentials, policy: null, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);

        public virtual Task AuthenticateAsServerAsync(ExtendedProtectionPolicy? policy) =>
            AuthenticateAsServerAsync((NetworkCredential)CredentialCache.DefaultCredentials, policy, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);

        public virtual Task AuthenticateAsServerAsync(NetworkCredential credential, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel requiredImpersonationLevel) =>
            AuthenticateAsServerAsync(credential, policy: null, requiredProtectionLevel, requiredImpersonationLevel);

        public virtual Task AuthenticateAsServerAsync(
            NetworkCredential credential, ExtendedProtectionPolicy? policy, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel requiredImpersonationLevel)
        {
            ValidateCreateContext(DefaultPackage, credential, string.Empty, policy, requiredProtectionLevel, requiredImpersonationLevel);
            return AuthenticateAsync<AsyncReadWriteAdapter>(default(CancellationToken));
        }

        public override bool IsAuthenticated => IsAuthenticatedCore;

        [MemberNotNullWhen(true, nameof(_context))]
        private bool IsAuthenticatedCore => _context != null && HandshakeComplete && _exception == null && _remoteOk;

        public override bool IsMutuallyAuthenticated =>
            IsAuthenticatedCore &&
            !_context.IsNTLM && // suppressing for NTLM since SSPI does not return correct value in the context flags.
            _context.IsMutualAuthFlag;

        public override bool IsEncrypted => IsAuthenticatedCore && _context.IsConfidentialityFlag;

        public override bool IsSigned => IsAuthenticatedCore && (_context.IsIntegrityFlag || _context.IsConfidentialityFlag);

        public override bool IsServer => _context != null && _context.IsServer;

        public virtual TokenImpersonationLevel ImpersonationLevel
        {
            get
            {
                ThrowIfFailed(authSuccessCheck: true);
                return PrivateImpersonationLevel;
            }
        }

        private TokenImpersonationLevel PrivateImpersonationLevel =>
            _context!.IsDelegationFlag && _context.ProtocolName != NegotiationInfoClass.NTLM ? TokenImpersonationLevel.Delegation : // We should suppress the delegate flag in NTLM case.
            _context.IsIdentifyFlag ? TokenImpersonationLevel.Identification :
            TokenImpersonationLevel.Impersonation;

        private bool HandshakeComplete => _context!.IsCompleted && _context.IsValidContext;

        private bool CanGetSecureStream => _context!.IsConfidentialityFlag || _context.IsIntegrityFlag;

        public virtual IIdentity RemoteIdentity
        {
            get
            {
                IIdentity? identity = _remoteIdentity;
                if (identity is null)
                {
                    ThrowIfFailed(authSuccessCheck: true);
                    _remoteIdentity = identity = NegotiateStreamPal.GetIdentity(_context!);
                }
                return identity;
            }
        }

        public override bool CanSeek => false;

        public override bool CanRead => IsAuthenticated && InnerStream.CanRead;

        public override bool CanTimeout => InnerStream.CanTimeout;

        public override bool CanWrite => IsAuthenticated && InnerStream.CanWrite;

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

        public override void SetLength(long value) =>
            InnerStream.SetLength(value);

        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException(SR.net_noseek);

        public override void Flush() =>
            InnerStream.Flush();

        public override Task FlushAsync(CancellationToken cancellationToken) =>
            InnerStream.FlushAsync(cancellationToken);

        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);

            ThrowIfFailed(authSuccessCheck: true);
            if (!CanGetSecureStream)
            {
                return InnerStream.Read(buffer, offset, count);
            }

            ValueTask<int> vt = ReadAsync<SyncReadWriteAdapter>(new Memory<byte>(buffer, offset, count), default(CancellationToken));
            Debug.Assert(vt.IsCompleted, "Should have completed synchroously with sync adapter");
            return vt.GetAwaiter().GetResult();
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);

            ThrowIfFailed(authSuccessCheck: true);
            if (!CanGetSecureStream)
            {
                return InnerStream.ReadAsync(buffer, offset, count, cancellationToken);
            }

            return ReadAsync<AsyncReadWriteAdapter>(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ThrowIfFailed(authSuccessCheck: true);
            if (!CanGetSecureStream)
            {
                return InnerStream.ReadAsync(buffer, cancellationToken);
            }

            return ReadAsync<AsyncReadWriteAdapter>(buffer, cancellationToken);
        }

        private async ValueTask<int> ReadAsync<TIOAdapter>(Memory<byte> buffer, CancellationToken cancellationToken)
            where TIOAdapter : IReadWriteAdapter
        {
            Debug.Assert(_context is not null);

            if (Interlocked.Exchange(ref _readInProgress, 1) == 1)
            {
                throw new NotSupportedException(SR.Format(SR.net_io_invalidnestedcall, "read"));
            }

            try
            {
                if (_readBufferCount != 0)
                {
                    int copyBytes = Math.Min(_readBufferCount, buffer.Length);
                    if (copyBytes != 0)
                    {
                        _readBuffer.AsMemory(_readBufferOffset, copyBytes).CopyTo(buffer);
                        _readBufferOffset += copyBytes;
                        _readBufferCount -= copyBytes;
                    }
                    return copyBytes;
                }

                while (true)
                {
                    int readBytes = await ReadAllAsync(InnerStream, _readHeader, allowZeroRead: true, cancellationToken).ConfigureAwait(false);
                    if (readBytes == 0)
                    {
                        return 0;
                    }

                    // Replace readBytes with the body size recovered from the header content.
                    readBytes = BitConverter.ToInt32(_readHeader, 0);

                    // The body carries 4 bytes for trailer size slot plus trailer, hence <= 4 frame size is always an error.
                    // Additionally we'd like to restrict the read frame size to 64k.
                    if (readBytes <= 4 || readBytes > MaxReadFrameSize)
                    {
                        throw new IOException(SR.net_frame_read_size);
                    }

                    // Always pass InternalBuffer for SSPI "in place" decryption.
                    // A user buffer can be shared by many threads in that case decryption/integrity check may fail cause of data corruption.
                    _readBufferCount = readBytes;
                    _readBufferOffset = 0;
                    if (_readBuffer.Length < readBytes)
                    {
                        _readBuffer = new byte[readBytes];
                    }

                    readBytes = await ReadAllAsync(InnerStream, new Memory<byte>(_readBuffer, 0, readBytes), allowZeroRead: false, cancellationToken).ConfigureAwait(false);

                    // Decrypt into internal buffer, change "readBytes" to count now _Decrypted Bytes_
                    // Decrypted data start from zero offset, the size can be shrunk after decryption.
                    _readBufferCount = readBytes = _context.Decrypt(_readBuffer.AsSpan(0, readBytes), out _readBufferOffset);
                    if (readBytes == 0 && buffer.Length != 0)
                    {
                        // Read again.
                        continue;
                    }

                    if (readBytes > buffer.Length)
                    {
                        readBytes = buffer.Length;
                    }

                    _readBuffer.AsMemory(_readBufferOffset, readBytes).CopyTo(buffer);
                    _readBufferOffset += readBytes;
                    _readBufferCount -= readBytes;

                    return readBytes;
                }
            }
            catch (Exception e) when (!(e is IOException || e is OperationCanceledException))
            {
                throw new IOException(SR.net_io_read, e);
            }
            finally
            {
                _readInProgress = 0;
            }

            static async ValueTask<int> ReadAllAsync(Stream stream, Memory<byte> buffer, bool allowZeroRead, CancellationToken cancellationToken)
            {
                int read = await TIOAdapter.ReadAtLeastAsync(
                    stream, buffer, buffer.Length, throwOnEndOfStream: false, cancellationToken).ConfigureAwait(false);
                if (read < buffer.Length)
                {
                    if (read != 0 || !allowZeroRead)
                    {
                        throw new IOException(SR.net_io_eof);
                    }
                }

                return read;
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);

            ThrowIfFailed(authSuccessCheck: true);
            if (!CanGetSecureStream)
            {
                InnerStream.Write(buffer, offset, count);
                return;
            }

            WriteAsync<SyncReadWriteAdapter>(new ReadOnlyMemory<byte>(buffer, offset, count), default(CancellationToken)).GetAwaiter().GetResult();
        }

        /// <returns>A <see cref="Task"/> that represents the asynchronous read operation.</returns>
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);

            ThrowIfFailed(authSuccessCheck: true);
            if (!CanGetSecureStream)
            {
                return InnerStream.WriteAsync(buffer, offset, count, cancellationToken);
            }

            return WriteAsync<AsyncReadWriteAdapter>(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken);
        }

        /// <returns>A <see cref="ValueTask"/> that represents the asynchronous read operation.</returns>
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ThrowIfFailed(authSuccessCheck: true);
            if (!CanGetSecureStream)
            {
                return InnerStream.WriteAsync(buffer, cancellationToken);
            }

            return new ValueTask(WriteAsync<AsyncReadWriteAdapter>(buffer, cancellationToken));
        }

        private async Task WriteAsync<TIOAdapter>(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
            where TIOAdapter : IReadWriteAdapter
        {
            Debug.Assert(_context is not null);

            if (Interlocked.Exchange(ref _writeInProgress, 1) == 1)
            {
                throw new NotSupportedException(SR.Format(SR.net_io_invalidnestedcall, "write"));
            }

            try
            {
                while (!buffer.IsEmpty)
                {
                    int chunkBytes = Math.Min(buffer.Length, MaxWriteDataSize);
                    int encryptedBytes;
                    try
                    {
                        encryptedBytes = _context.Encrypt(buffer.Slice(0, chunkBytes).Span, ref _writeBuffer);
                    }
                    catch (Exception e)
                    {
                        throw new IOException(SR.net_io_encrypt, e);
                    }

                    await TIOAdapter.WriteAsync(InnerStream, new ReadOnlyMemory<byte>(_writeBuffer, 0, encryptedBytes), cancellationToken).ConfigureAwait(false);
                    buffer = buffer.Slice(chunkBytes);
                }
            }
            catch (Exception e) when (!(e is IOException || e is OperationCanceledException))
            {
                throw new IOException(SR.net_io_write, e);
            }
            finally
            {
                _writeInProgress = 0;
            }
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? asyncCallback, object? asyncState) =>
            TaskToApm.Begin(ReadAsync(buffer, offset, count), asyncCallback, asyncState);

        public override int EndRead(IAsyncResult asyncResult) =>
            TaskToApm.End<int>(asyncResult);

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? asyncCallback, object? asyncState) =>
            TaskToApm.Begin(WriteAsync(buffer, offset, count), asyncCallback, asyncState);

        public override void EndWrite(IAsyncResult asyncResult) =>
            TaskToApm.End(asyncResult);

        private void ThrowIfExceptional()
        {
            ExceptionDispatchInfo? e = _exception;
            if (e != null)
            {
                ThrowExceptional(e);
            }

            // Local function to make the check method more inline friendly.
            void ThrowExceptional(ExceptionDispatchInfo e)
            {
                // If the stored exception just indicates disposal, throw a new ODE rather than the stored one,
                // so as to not continually build onto the shared exception's stack.
                ObjectDisposedException.ThrowIf(ReferenceEquals(e, s_disposedSentinel), this);

                // Throw the stored exception.
                e.Throw();
            }
        }

        private void ValidateCreateContext(
            string package,
            NetworkCredential credential,
            string servicePrincipalName,
            ExtendedProtectionPolicy? policy,
            ProtectionLevel protectionLevel,
            TokenImpersonationLevel impersonationLevel)
        {
            if (policy != null)
            {
                // One of these must be set if EP is turned on
                if (policy.CustomChannelBinding == null && policy.CustomServiceNames == null)
                {
                    throw new ArgumentException(SR.net_auth_must_specify_extended_protection_scheme, nameof(policy));
                }

                _extendedProtectionPolicy = policy;
            }
            else
            {
                _extendedProtectionPolicy = new ExtendedProtectionPolicy(PolicyEnforcement.Never);
            }

            ValidateCreateContext(package, isServer: true, credential, servicePrincipalName, _extendedProtectionPolicy.CustomChannelBinding, protectionLevel, impersonationLevel);
        }

        private void ValidateCreateContext(
            string package,
            bool isServer,
            NetworkCredential credential,
            string? servicePrincipalName,
            ChannelBinding? channelBinding,
            ProtectionLevel protectionLevel,
            TokenImpersonationLevel impersonationLevel)
        {
            if (!_canRetryAuthentication)
            {
                ThrowIfExceptional();
            }

            if (_context != null && _context.IsValidContext)
            {
                throw new InvalidOperationException(SR.net_auth_reauth);
            }

            ArgumentNullException.ThrowIfNull(credential);
            ArgumentNullException.ThrowIfNull(servicePrincipalName);

            NegotiateStreamPal.ValidateImpersonationLevel(impersonationLevel);
            if (_context != null && IsServer != isServer)
            {
                throw new InvalidOperationException(SR.net_auth_client_server);
            }

            _exception = null;
            _remoteOk = false;
            _framer = new StreamFramer();
            _framer.WriteHeader.MessageId = FrameHeader.HandshakeId;

            _expectedProtectionLevel = protectionLevel;
            _expectedImpersonationLevel = isServer ? impersonationLevel : TokenImpersonationLevel.None;

            ContextFlagsPal flags = ContextFlagsPal.Connection;

            // A workaround for the client when talking to Win9x on the server side.
            if (protectionLevel == ProtectionLevel.None && !isServer)
            {
                package = NegotiationInfoClass.NTLM;
            }
            else if (protectionLevel == ProtectionLevel.EncryptAndSign)
            {
                flags |= ContextFlagsPal.Confidentiality;
            }
            else if (protectionLevel == ProtectionLevel.Sign)
            {
                // Assuming user expects NT4 SP4 and above.
                flags |= ContextFlagsPal.ReplayDetect | ContextFlagsPal.SequenceDetect | ContextFlagsPal.InitIntegrity;
            }

            if (isServer)
            {
                if (_extendedProtectionPolicy!.PolicyEnforcement == PolicyEnforcement.WhenSupported)
                {
                    flags |= ContextFlagsPal.AllowMissingBindings;
                }

                if (_extendedProtectionPolicy.PolicyEnforcement != PolicyEnforcement.Never &&
                    _extendedProtectionPolicy.ProtectionScenario == ProtectionScenario.TrustedProxy)
                {
                    flags |= ContextFlagsPal.ProxyBindings;
                }
            }
            else
            {
                // Server side should not request any of these flags.
                if (protectionLevel != ProtectionLevel.None)
                {
                    flags |= ContextFlagsPal.MutualAuth;
                }

                if (impersonationLevel == TokenImpersonationLevel.Identification)
                {
                    flags |= ContextFlagsPal.InitIdentify;
                }

                if (impersonationLevel == TokenImpersonationLevel.Delegation)
                {
                    flags |= ContextFlagsPal.Delegate;
                }
            }

            _canRetryAuthentication = false;

            try
            {
                _context = new NTAuthentication(isServer, package, credential, servicePrincipalName, flags, channelBinding!);
            }
            catch (Win32Exception e)
            {
                throw new AuthenticationException(SR.net_auth_SSPI, e);
            }
        }

        private void SetFailed(Exception e)
        {
            if (_exception == null || !(_exception.SourceException is ObjectDisposedException))
            {
                _exception = ExceptionDispatchInfo.Capture(e);
            }

            _context?.CloseContext();
        }

        private void ThrowIfFailed(bool authSuccessCheck)
        {
            ThrowIfExceptional();

            if (authSuccessCheck && !IsAuthenticatedCore)
            {
                throw new InvalidOperationException(SR.net_auth_noauth);
            }
        }

        private async Task AuthenticateAsync<TIOAdapter>(CancellationToken cancellationToken)
            where TIOAdapter : IReadWriteAdapter
        {
            Debug.Assert(_context != null);

            ThrowIfFailed(authSuccessCheck: false);
            if (Interlocked.Exchange(ref _authInProgress, 1) == 1)
            {
                throw new InvalidOperationException(SR.Format(SR.net_io_invalidnestedcall, "authenticate"));
            }

            try
            {
                await (_context.IsServer ?
                    ReceiveBlobAsync<TIOAdapter>(cancellationToken) : // server should listen for a client blob
                    SendBlobAsync<TIOAdapter>(message: null, cancellationToken)).ConfigureAwait(false); // client should send the first blob
            }
            catch (Exception e)
            {
                SetFailed(e);
                throw;
            }
            finally
            {
                _authInProgress = 0;
            }
        }

        private bool CheckSpn()
        {
            Debug.Assert(_context != null);

            if (_context.IsKerberos ||
                _extendedProtectionPolicy!.PolicyEnforcement == PolicyEnforcement.Never ||
                _extendedProtectionPolicy.CustomServiceNames == null)
            {
                return true;
            }

            string? clientSpn = _context.ClientSpecifiedSpn;

            if (string.IsNullOrEmpty(clientSpn))
            {
                return _extendedProtectionPolicy.PolicyEnforcement == PolicyEnforcement.WhenSupported;
            }

            return _extendedProtectionPolicy.CustomServiceNames.Contains(clientSpn);
        }

        // Client authentication starts here, but server also loops through this method.
        private async Task SendBlobAsync<TIOAdapter>(byte[]? message, CancellationToken cancellationToken)
            where TIOAdapter : IReadWriteAdapter
        {
            Debug.Assert(_context != null);

            Exception? exception = null;
            if (message != s_emptyMessage)
            {
                message = GetOutgoingBlob(message, ref exception);
            }

            if (exception != null)
            {
                // Signal remote side on a failed attempt.
                await SendAuthResetSignalAndThrowAsync<TIOAdapter>(message!, exception, cancellationToken).ConfigureAwait(false);
                Debug.Fail("Unreachable");
            }

            if (HandshakeComplete)
            {
                if (_context.IsServer && !CheckSpn())
                {
                    exception = new AuthenticationException(SR.net_auth_bad_client_creds_or_target_mismatch);
                    int statusCode = ERROR_TRUST_FAILURE;
                    message = new byte[sizeof(long)];

                    for (int i = message.Length - 1; i >= 0; --i)
                    {
                        message[i] = (byte)(statusCode & 0xFF);
                        statusCode = (int)((uint)statusCode >> 8);
                    }

                    await SendAuthResetSignalAndThrowAsync<TIOAdapter>(message, exception, cancellationToken).ConfigureAwait(false);
                    Debug.Fail("Unreachable");
                }

                if (PrivateImpersonationLevel < _expectedImpersonationLevel)
                {
                    exception = new AuthenticationException(SR.Format(SR.net_auth_context_expectation, _expectedImpersonationLevel.ToString(), PrivateImpersonationLevel.ToString()));
                    int statusCode = ERROR_TRUST_FAILURE;
                    message = new byte[sizeof(long)];

                    for (int i = message.Length - 1; i >= 0; --i)
                    {
                        message[i] = (byte)(statusCode & 0xFF);
                        statusCode = (int)((uint)statusCode >> 8);
                    }

                    await SendAuthResetSignalAndThrowAsync<TIOAdapter>(message, exception, cancellationToken).ConfigureAwait(false);
                    Debug.Fail("Unreachable");
                }

                ProtectionLevel result = _context.IsConfidentialityFlag ? ProtectionLevel.EncryptAndSign : _context.IsIntegrityFlag ? ProtectionLevel.Sign : ProtectionLevel.None;

                if (result < _expectedProtectionLevel)
                {
                    exception = new AuthenticationException(SR.Format(SR.net_auth_context_expectation, result.ToString(), _expectedProtectionLevel.ToString()));
                    int statusCode = ERROR_TRUST_FAILURE;
                    message = new byte[sizeof(long)];

                    for (int i = message.Length - 1; i >= 0; --i)
                    {
                        message[i] = (byte)(statusCode & 0xFF);
                        statusCode = (int)((uint)statusCode >> 8);
                    }

                    await SendAuthResetSignalAndThrowAsync<TIOAdapter>(message, exception, cancellationToken).ConfigureAwait(false);
                    Debug.Fail("Unreachable");
                }

                // Signal remote party that we are done
                _framer!.WriteHeader.MessageId = FrameHeader.HandshakeDoneId;
                if (_context.IsServer)
                {
                    // Server may complete now because client SSPI would not complain at this point.
                    _remoteOk = true;

                    // However the client will wait for server to send this ACK
                    // Force signaling server OK to the client
                    message ??= s_emptyMessage;
                }
            }
            else if (message == null || message == s_emptyMessage)
            {
                throw new InternalException();
            }

            if (message != null)
            {
                //even if we are completed, there could be a blob for sending.
                await _framer!.WriteMessageAsync<TIOAdapter>(InnerStream, message, cancellationToken).ConfigureAwait(false);
            }

            if (HandshakeComplete && _remoteOk)
            {
                // We are done with success.
                return;
            }

            await ReceiveBlobAsync<TIOAdapter>(cancellationToken).ConfigureAwait(false);
        }

        // Server authentication starts here, but client also loops through this method.
        private async Task ReceiveBlobAsync<TIOAdapter>(CancellationToken cancellationToken)
            where TIOAdapter : IReadWriteAdapter
        {
            Debug.Assert(_framer != null);

            byte[]? message = await _framer.ReadMessageAsync<TIOAdapter>(InnerStream, cancellationToken).ConfigureAwait(false);
            if (message == null)
            {
                // This is an EOF otherwise we would get at least *empty* message but not a null one.
                throw new AuthenticationException(SR.net_auth_eof);
            }

            // Process Header information.
            if (_framer.ReadHeader.MessageId == FrameHeader.HandshakeErrId)
            {
                if (message.Length >= sizeof(long))
                {
                    // Try to recover remote win32 Exception.
                    long error = 0;
                    for (int i = 0; i < 8; ++i)
                    {
                        error = (error << 8) + message[i];
                    }

                    ThrowCredentialException(error);
                }

                throw new AuthenticationException(SR.net_auth_alert);
            }

            if (_framer.ReadHeader.MessageId == FrameHeader.HandshakeDoneId)
            {
                _remoteOk = true;
            }
            else if (_framer.ReadHeader.MessageId != FrameHeader.HandshakeId)
            {
                throw new AuthenticationException(SR.Format(SR.net_io_header_id, nameof(FrameHeader.MessageId), _framer.ReadHeader.MessageId, FrameHeader.HandshakeId));
            }

            // If we are done don't go into send.
            if (HandshakeComplete)
            {
                if (!_remoteOk)
                {
                    throw new AuthenticationException(SR.Format(SR.net_io_header_id, nameof(FrameHeader.MessageId), _framer.ReadHeader.MessageId, FrameHeader.HandshakeDoneId));
                }

                return;
            }

            // Not yet done, get a new blob and send it if any.
            await SendBlobAsync<TIOAdapter>(message, cancellationToken).ConfigureAwait(false);
        }

        //  This is to reset auth state on the remote side.
        //  If this write succeeds we will allow auth retrying.
        private async Task SendAuthResetSignalAndThrowAsync<TIOAdapter>(byte[] message, Exception exception, CancellationToken cancellationToken)
            where TIOAdapter : IReadWriteAdapter
        {
            _framer!.WriteHeader.MessageId = FrameHeader.HandshakeErrId;

            if (IsLogonDeniedException(exception))
            {
                exception = new InvalidCredentialException(IsServer ? SR.net_auth_bad_client_creds : SR.net_auth_bad_client_creds_or_target_mismatch, exception);
            }

            if (!(exception is AuthenticationException))
            {
                exception = new AuthenticationException(SR.net_auth_SSPI, exception);
            }

            await _framer.WriteMessageAsync<TIOAdapter>(InnerStream, message, cancellationToken).ConfigureAwait(false);

            _canRetryAuthentication = true;
            ExceptionDispatchInfo.Throw(exception);
        }

        private static bool IsError(SecurityStatusPal status) =>
            (int)status.ErrorCode >= (int)SecurityStatusPalErrorCode.OutOfMemory;

        private unsafe byte[]? GetOutgoingBlob(byte[]? incomingBlob, ref Exception? e)
        {
            Debug.Assert(_context != null);

            byte[]? message = _context.GetOutgoingBlob(incomingBlob, false, out SecurityStatusPal statusCode);

            if (IsError(statusCode))
            {
                e = NegotiateStreamPal.CreateExceptionFromError(statusCode);
                uint error = (uint)e.HResult;

                message = new byte[sizeof(long)];
                for (int i = message.Length - 1; i >= 0; --i)
                {
                    message[i] = (byte)(error & 0xFF);
                    error >>= 8;
                }
            }

            if (message != null && message.Length == 0)
            {
                message = s_emptyMessage;
            }

            return message;
        }

        private static void ThrowCredentialException(long error)
        {
            var e = new Win32Exception((int)error);
            throw e.NativeErrorCode switch
            {
                (int)SecurityStatusPalErrorCode.LogonDenied => new InvalidCredentialException(SR.net_auth_bad_client_creds, e),
                ERROR_TRUST_FAILURE => new AuthenticationException(SR.net_auth_context_expectation_remote, e),
                _ => new AuthenticationException(SR.net_auth_alert, e)
            };
        }

        private static bool IsLogonDeniedException(Exception exception) =>
            exception is Win32Exception win32exception &&
            win32exception.NativeErrorCode == (int)SecurityStatusPalErrorCode.LogonDenied;
    }
}
