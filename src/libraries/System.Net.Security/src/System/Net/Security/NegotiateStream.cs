// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
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

        private readonly byte[] _writeHeader;
        private readonly byte[] _readHeader;
        private byte[] _readBuffer;
        private int _readBufferOffset;
        private int _readBufferCount;
        private ArrayBufferWriter<byte>? _writeBuffer;

        private volatile int _writeInProgress;
        private volatile int _readInProgress;
        private volatile int _authInProgress;

        private ExceptionDispatchInfo? _exception;
        private StreamFramer? _framer;
        private NegotiateAuthentication? _context;
        private bool _canRetryAuthentication;
        private ProtectionLevel _expectedProtectionLevel;
        private TokenImpersonationLevel _expectedImpersonationLevel;
        private ExtendedProtectionPolicy? _extendedProtectionPolicy;

        private bool isNtlm;

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
            _writeHeader = new byte[4];
            _readHeader = new byte[4];
            _readBuffer = Array.Empty<byte>();
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                _exception = s_disposedSentinel;
                _context?.Dispose();
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
                _context?.Dispose();
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
            TaskToAsyncResult.Begin(AuthenticateAsClientAsync(credential, binding, targetName, requiredProtectionLevel, allowedImpersonationLevel), asyncCallback, asyncState);

        public virtual void EndAuthenticateAsClient(IAsyncResult asyncResult) => TaskToAsyncResult.End(asyncResult);

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
            TaskToAsyncResult.Begin(AuthenticateAsServerAsync(credential, policy, requiredProtectionLevel, requiredImpersonationLevel), asyncCallback, asyncState);

        public virtual void EndAuthenticateAsServer(IAsyncResult asyncResult) => TaskToAsyncResult.End(asyncResult);

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
            !string.Equals(_context.Package, NegotiationInfoClass.NTLM) && // suppressing for NTLM since SSPI does not return correct value in the context flags.
            _context.IsMutuallyAuthenticated;

        public override bool IsEncrypted => IsAuthenticatedCore && _context.IsEncrypted;

        public override bool IsSigned => IsAuthenticatedCore && (_context.IsSigned || _context.IsEncrypted);

        public override bool IsServer => _context != null && _context.IsServer;

        public virtual TokenImpersonationLevel ImpersonationLevel
        {
            get
            {
                ThrowIfFailed(authSuccessCheck: true);
                return PrivateImpersonationLevel;
            }
        }

        private TokenImpersonationLevel PrivateImpersonationLevel => _context!.ImpersonationLevel;

        private bool HandshakeComplete => _context!.IsAuthenticated;

        private bool CanGetSecureStream => _context!.IsEncrypted || _context.IsSigned;

        public virtual IIdentity RemoteIdentity
        {
            get
            {
                ThrowIfFailed(authSuccessCheck: true);
                return _context!.RemoteIdentity;
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
                ThrowIfFailed(authSuccessCheck: true);

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
                    readBytes = BinaryPrimitives.ReadInt32LittleEndian(_readHeader);

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

                    // Decrypt into the same buffer (decrypted data size can be shrunk after decryption).
                    NegotiateAuthenticationStatusCode statusCode;
                    if (isNtlm && !_context.IsEncrypted)
                    {
                        // Non-encrypted NTLM uses an encoding quirk
                        const int NtlmSignatureLength = 16;

                        if (readBytes < NtlmSignatureLength ||
                            !_context.VerifyIntegrityCheck(_readBuffer.AsSpan(NtlmSignatureLength, readBytes - NtlmSignatureLength), _readBuffer.AsSpan(0, NtlmSignatureLength)))
                        {
                            statusCode = NegotiateAuthenticationStatusCode.InvalidToken;
                        }
                        else
                        {
                            _readBufferOffset = NtlmSignatureLength;
                            _readBufferCount = readBytes - NtlmSignatureLength;
                            statusCode = NegotiateAuthenticationStatusCode.Completed;
                        }
                    }
                    else
                    {
                        statusCode = _context.UnwrapInPlace(_readBuffer.AsSpan(0, readBytes), out _readBufferOffset, out _readBufferCount, out _);
                    }

                    if (statusCode != NegotiateAuthenticationStatusCode.Completed)
                    {
                        // TODO: Better exception
                        throw new IOException(SR.net_io_read);
                    }

                    // Decrypted data can be shrunk after decryption.
                    if (_readBufferCount == 0 && buffer.Length != 0)
                    {
                        // Read again.
                        continue;
                    }

                    int copyBytes = Math.Min(_readBufferCount, buffer.Length);
                    _readBuffer.AsMemory(_readBufferOffset, copyBytes).CopyTo(buffer);
                    _readBufferOffset += copyBytes;
                    _readBufferCount -= copyBytes;

                    return copyBytes;
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
            Debug.Assert(_writeBuffer is not null);

            if (Interlocked.Exchange(ref _writeInProgress, 1) == 1)
            {
                throw new NotSupportedException(SR.Format(SR.net_io_invalidnestedcall, "write"));
            }

            try
            {
                ThrowIfFailed(authSuccessCheck: true);

                while (!buffer.IsEmpty)
                {
                    int chunkBytes = Math.Min(buffer.Length, MaxWriteDataSize);

                    bool isEncrypted = _context.IsEncrypted;
                    NegotiateAuthenticationStatusCode statusCode;
                    ReadOnlyMemory<byte> bufferToWrap = buffer.Slice(0, chunkBytes);

                    if (isNtlm && !isEncrypted)
                    {
                        // Non-encrypted NTLM uses an encoding quirk
                        _context.ComputeIntegrityCheck(bufferToWrap.Span, _writeBuffer);
                        _writeBuffer.Write(bufferToWrap.Span);
                        statusCode = NegotiateAuthenticationStatusCode.Completed;
                    }
                    else
                    {
                        statusCode = _context.Wrap(bufferToWrap.Span, _writeBuffer, isEncrypted, out _);
                    }

                    if (statusCode != NegotiateAuthenticationStatusCode.Completed)
                    {
                        // TODO: Trace the error
                        throw new IOException(SR.net_io_encrypt);
                    }

                    BinaryPrimitives.WriteInt32LittleEndian(_writeHeader, _writeBuffer.WrittenCount);
                    await TIOAdapter.WriteAsync(InnerStream, _writeHeader, cancellationToken).ConfigureAwait(false);

                    await TIOAdapter.WriteAsync(InnerStream, _writeBuffer.WrittenMemory, cancellationToken).ConfigureAwait(false);
                    buffer = buffer.Slice(chunkBytes);
                    _writeBuffer.Clear();
                }
            }
            catch (Exception e) when (!(e is IOException || e is OperationCanceledException))
            {
                throw new IOException(SR.net_io_write, e);
            }
            finally
            {
                _writeBuffer.Clear();
                _writeInProgress = 0;
            }
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? asyncCallback, object? asyncState) =>
            TaskToAsyncResult.Begin(ReadAsync(buffer, offset, count), asyncCallback, asyncState);

        public override int EndRead(IAsyncResult asyncResult) =>
            TaskToAsyncResult.End<int>(asyncResult);

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? asyncCallback, object? asyncState) =>
            TaskToAsyncResult.Begin(WriteAsync(buffer, offset, count), asyncCallback, asyncState);

        public override void EndWrite(IAsyncResult asyncResult) =>
            TaskToAsyncResult.End(asyncResult);

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

            if (_context != null)
            {
                throw new InvalidOperationException(SR.net_auth_reauth);
            }

            ArgumentNullException.ThrowIfNull(credential);
            ArgumentNullException.ThrowIfNull(servicePrincipalName);

            if (impersonationLevel != TokenImpersonationLevel.Identification &&
                impersonationLevel != TokenImpersonationLevel.Impersonation &&
                impersonationLevel != TokenImpersonationLevel.Delegation)
            {
                throw new ArgumentOutOfRangeException(nameof(impersonationLevel), impersonationLevel.ToString(), SR.net_auth_supported_impl_levels);
            }

            if (_context is not null && IsServer != isServer)
            {
                throw new InvalidOperationException(SR.net_auth_client_server);
            }

            _exception = null;
            _remoteOk = false;
            _framer = new StreamFramer();
            _framer.WriteHeader.MessageId = FrameHeader.HandshakeId;

            _canRetryAuthentication = false;

            // A workaround for the client when talking to Win9x on the server side.
            if (protectionLevel == ProtectionLevel.None && !isServer)
            {
                package = NegotiationInfoClass.NTLM;
            }

            if (isServer)
            {
                _expectedProtectionLevel = protectionLevel;
                _expectedImpersonationLevel = impersonationLevel;
                _context = new NegotiateAuthentication(
                    new NegotiateAuthenticationServerOptions
                    {
                        Package = package,
                        Credential = credential,
                        Binding = channelBinding,
                        RequiredProtectionLevel = protectionLevel,
                        RequiredImpersonationLevel = impersonationLevel,
                        Policy = _extendedProtectionPolicy,
                    });
            }
            else
            {
                _expectedProtectionLevel = protectionLevel;
                _expectedImpersonationLevel = TokenImpersonationLevel.None;
                _context = new NegotiateAuthentication(
                    new NegotiateAuthenticationClientOptions
                    {
                        Package = package,
                        Credential = credential,
                        TargetName = servicePrincipalName,
                        Binding = channelBinding,
                        RequiredProtectionLevel = protectionLevel,
                        AllowedImpersonationLevel = impersonationLevel,
                        RequireMutualAuthentication = protectionLevel != ProtectionLevel.None
                    });
            }
        }

        private void SetFailed(Exception e)
        {
            if (_exception == null || !(_exception.SourceException is ObjectDisposedException))
            {
                _exception = ExceptionDispatchInfo.Capture(e);
            }

            _context?.Dispose();
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

        // Client authentication starts here, but server also loops through this method.
        private async Task SendBlobAsync<TIOAdapter>(byte[]? message, CancellationToken cancellationToken)
            where TIOAdapter : IReadWriteAdapter
        {
            Debug.Assert(_context != null);

            NegotiateAuthenticationStatusCode statusCode = NegotiateAuthenticationStatusCode.Completed;
            if (message != s_emptyMessage)
            {
                message = _context.GetOutgoingBlob(message, out statusCode);
            }

            if (statusCode is NegotiateAuthenticationStatusCode.BadBinding or
                NegotiateAuthenticationStatusCode.TargetUnknown or
                NegotiateAuthenticationStatusCode.ImpersonationValidationFailed or
                NegotiateAuthenticationStatusCode.SecurityQosFailed)
            {
                Exception exception = statusCode switch
                {
                    NegotiateAuthenticationStatusCode.BadBinding =>
                        new AuthenticationException(SR.net_auth_bad_client_creds_or_target_mismatch),
                    NegotiateAuthenticationStatusCode.TargetUnknown =>
                        new AuthenticationException(SR.net_auth_bad_client_creds_or_target_mismatch),
                    NegotiateAuthenticationStatusCode.ImpersonationValidationFailed =>
                        new AuthenticationException(SR.Format(SR.net_auth_context_expectation, _expectedImpersonationLevel.ToString(), PrivateImpersonationLevel.ToString())),
                    _ => // NegotiateAuthenticationStatusCode.SecurityQosFailed
                        new AuthenticationException(SR.Format(SR.net_auth_context_expectation, _context.ProtectionLevel.ToString(), _expectedProtectionLevel.ToString())),
                };

                message = new byte[sizeof(long)];
                BinaryPrimitives.WriteInt64LittleEndian(message, ERROR_TRUST_FAILURE);

                await SendAuthResetSignalAndThrowAsync<TIOAdapter>(message, exception, cancellationToken).ConfigureAwait(false);
                Debug.Fail("Unreachable");
            }
            else if (statusCode == NegotiateAuthenticationStatusCode.Completed)
            {
                _writeBuffer = new ArrayBufferWriter<byte>();

                isNtlm = string.Equals(_context.Package, NegotiationInfoClass.NTLM);

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

                if (message != null)
                {
                    //even if we are completed, there could be a blob for sending.
                    await _framer!.WriteMessageAsync<TIOAdapter>(InnerStream, message, cancellationToken).ConfigureAwait(false);
                }

                if (_remoteOk)
                {
                    // We are done with success.
                    return;
                }
            }
            else if (statusCode != NegotiateAuthenticationStatusCode.ContinueNeeded)
            {
                int errorCode = statusCode switch
                {
                    NegotiateAuthenticationStatusCode.BadBinding => (int)Interop.SECURITY_STATUS.BadBinding,
                    NegotiateAuthenticationStatusCode.Unsupported => (int)Interop.SECURITY_STATUS.Unsupported,
                    NegotiateAuthenticationStatusCode.MessageAltered => (int)Interop.SECURITY_STATUS.MessageAltered,
                    NegotiateAuthenticationStatusCode.ContextExpired => (int)Interop.SECURITY_STATUS.ContextExpired,
                    NegotiateAuthenticationStatusCode.CredentialsExpired => (int)Interop.SECURITY_STATUS.CertExpired,
                    NegotiateAuthenticationStatusCode.InvalidCredentials => (int)Interop.SECURITY_STATUS.LogonDenied,
                    NegotiateAuthenticationStatusCode.InvalidToken => (int)Interop.SECURITY_STATUS.InvalidToken,
                    NegotiateAuthenticationStatusCode.UnknownCredentials => (int)Interop.SECURITY_STATUS.UnknownCredentials,
                    NegotiateAuthenticationStatusCode.QopNotSupported => (int)Interop.SECURITY_STATUS.QopNotSupported,
                    NegotiateAuthenticationStatusCode.OutOfSequence => (int)Interop.SECURITY_STATUS.OutOfSequence,
                    _ => (int)Interop.SECURITY_STATUS.InternalError
                };
                Win32Exception win32Exception = new Win32Exception(errorCode);
                Exception exception = statusCode switch
                {
                    NegotiateAuthenticationStatusCode.InvalidCredentials =>
                        new InvalidCredentialException(IsServer ? SR.net_auth_bad_client_creds : SR.net_auth_bad_client_creds_or_target_mismatch, win32Exception),
                    _ => new AuthenticationException(SR.net_auth_SSPI, win32Exception)
                };

                message = new byte[sizeof(long)];
                BinaryPrimitives.WriteInt64LittleEndian(message, errorCode);

                // Signal remote side on a failed attempt.
                await SendAuthResetSignalAndThrowAsync<TIOAdapter>(message!, exception, cancellationToken).ConfigureAwait(false);
                Debug.Fail("Unreachable");
            }
            else
            {
                if (message == null || message == s_emptyMessage)
                {
                    throw new InternalException();
                }

                await _framer!.WriteMessageAsync<TIOAdapter>(InnerStream, message, cancellationToken).ConfigureAwait(false);
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
                    long error = BinaryPrimitives.ReadInt64LittleEndian(message);
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

            await _framer.WriteMessageAsync<TIOAdapter>(InnerStream, message, cancellationToken).ConfigureAwait(false);

            _canRetryAuthentication = true;
            ExceptionDispatchInfo.Throw(exception);
        }

        private static void ThrowCredentialException(long error)
        {
            var e = new Win32Exception((int)error);
            throw e.NativeErrorCode switch
            {
                // Compatibility quirk: .NET Core and .NET 5/6 incorrectly report internal status code instead of Win32 error number
                (int)SecurityStatusPalErrorCode.LogonDenied => new InvalidCredentialException(SR.net_auth_bad_client_creds, e),
                (int)Interop.SECURITY_STATUS.LogonDenied => new InvalidCredentialException(SR.net_auth_bad_client_creds, e),
                ERROR_TRUST_FAILURE => new AuthenticationException(SR.net_auth_context_expectation_remote, e),
                _ => new AuthenticationException(SR.net_auth_alert, e)
            };
        }
    }
}
