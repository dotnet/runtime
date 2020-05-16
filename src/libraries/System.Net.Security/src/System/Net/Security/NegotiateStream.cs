// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
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
        private const int ERROR_TRUST_FAILURE = 1790;   // Used to serialize protectionLevel or impersonationLevel mismatch error to the remote side.
        private const int MaxReadFrameSize = 64 * 1024;
        private const int MaxWriteDataSize = 63 * 1024; // 1k for the framing and trailer that is always less as per SSPI.
        private const string DefaultPackage = NegotiationInfoClass.Negotiate;

#pragma warning disable CA1825 // used in reference comparison, requires unique object identity
        private static readonly byte[] s_emptyMessage = new byte[0];
#pragma warning restore CA1825
        private static readonly AsyncCallback s_readCallback = new AsyncCallback(ReadCallback);
        private static readonly AsyncCallback s_writeCallback = new AsyncCallback(WriteCallback);

        private readonly byte[] _readHeader;
        private IIdentity? _remoteIdentity;
        private byte[] _buffer;
        private int _bufferOffset;
        private int _bufferCount;

        private volatile int _writeInProgress;
        private volatile int _readInProgress;
        private volatile int _authInProgress;

        private Exception? _exception;
        private StreamFramer? _framer;
        private NTAuthentication? _context;
        private bool _canRetryAuthentication;
        private ProtectionLevel _expectedProtectionLevel;
        private TokenImpersonationLevel _expectedImpersonationLevel;
        private uint _writeSequenceNumber;
        private uint _readSequenceNumber;
        private ExtendedProtectionPolicy? _extendedProtectionPolicy;

        /// <summary>
        /// SSPI does not send a server ack on successful auth.
        /// This is a state variable used to gracefully handle auth confirmation.
        /// </summary>
        private bool _remoteOk = false;

        public NegotiateStream(Stream innerStream) : this(innerStream, false)
        {
        }

        public NegotiateStream(Stream innerStream, bool leaveInnerStreamOpen) : base(innerStream, leaveInnerStreamOpen)
        {
            _readHeader = new byte[4];
            _buffer = Array.Empty<byte>();
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                _exception = new ObjectDisposedException(nameof(NegotiateStream));
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
                _exception = new ObjectDisposedException(nameof(NegotiateStream));
                _context?.CloseContext();
            }
            finally
            {
                await base.DisposeAsync().ConfigureAwait(false);
            }
        }

        public virtual IAsyncResult BeginAuthenticateAsClient(AsyncCallback? asyncCallback, object? asyncState) =>
            BeginAuthenticateAsClient((NetworkCredential)CredentialCache.DefaultCredentials, null, string.Empty,
                                      ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification,
                                      asyncCallback, asyncState);

        public virtual IAsyncResult BeginAuthenticateAsClient(NetworkCredential credential, string targetName, AsyncCallback? asyncCallback, object? asyncState) =>
            BeginAuthenticateAsClient(credential, null, targetName,
                                      ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification,
                                      asyncCallback, asyncState);

        public virtual IAsyncResult BeginAuthenticateAsClient(NetworkCredential credential, ChannelBinding? binding, string targetName, AsyncCallback? asyncCallback, object? asyncState) =>
            BeginAuthenticateAsClient(credential, binding, targetName,
                                      ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification,
                                      asyncCallback, asyncState);

        public virtual IAsyncResult BeginAuthenticateAsClient(
            NetworkCredential credential,
            string targetName,
            ProtectionLevel requiredProtectionLevel,
            TokenImpersonationLevel allowedImpersonationLevel,
            AsyncCallback? asyncCallback,
            object? asyncState) =>
            BeginAuthenticateAsClient(credential, null, targetName,
                                      requiredProtectionLevel, allowedImpersonationLevel,
                                      asyncCallback, asyncState);

        public virtual IAsyncResult BeginAuthenticateAsClient(
            NetworkCredential credential,
            ChannelBinding? binding,
            string targetName,
            ProtectionLevel requiredProtectionLevel,
            TokenImpersonationLevel allowedImpersonationLevel,
            AsyncCallback? asyncCallback,
            object? asyncState)
        {
            ValidateCreateContext(DefaultPackage, false, credential, targetName, binding, requiredProtectionLevel, allowedImpersonationLevel);

            var result = new LazyAsyncResult(this, asyncState, asyncCallback);
            ProcessAuthentication(result);

            return result;
        }

        public virtual void EndAuthenticateAsClient(IAsyncResult asyncResult) =>
            EndProcessAuthentication(asyncResult);

        public virtual void AuthenticateAsServer() =>
            AuthenticateAsServer((NetworkCredential)CredentialCache.DefaultCredentials, null, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);

        public virtual void AuthenticateAsServer(ExtendedProtectionPolicy? policy) =>
            AuthenticateAsServer((NetworkCredential)CredentialCache.DefaultCredentials, policy, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);

        public virtual void AuthenticateAsServer(NetworkCredential credential, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel requiredImpersonationLevel) =>
            AuthenticateAsServer(credential, null, requiredProtectionLevel, requiredImpersonationLevel);

        public virtual void AuthenticateAsServer(NetworkCredential credential, ExtendedProtectionPolicy? policy, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel requiredImpersonationLevel)
        {
            ValidateCreateContext(DefaultPackage, credential, string.Empty, policy, requiredProtectionLevel, requiredImpersonationLevel);
            ProcessAuthentication(null);
        }

        public virtual IAsyncResult BeginAuthenticateAsServer(AsyncCallback? asyncCallback, object? asyncState) =>
            BeginAuthenticateAsServer((NetworkCredential)CredentialCache.DefaultCredentials, null, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification, asyncCallback, asyncState);

        public virtual IAsyncResult BeginAuthenticateAsServer(ExtendedProtectionPolicy? policy, AsyncCallback? asyncCallback, object? asyncState) =>
            BeginAuthenticateAsServer((NetworkCredential)CredentialCache.DefaultCredentials, policy, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification, asyncCallback, asyncState);

        public virtual IAsyncResult BeginAuthenticateAsServer(
            NetworkCredential credential,
            ProtectionLevel requiredProtectionLevel,
            TokenImpersonationLevel requiredImpersonationLevel,
            AsyncCallback? asyncCallback,
            object? asyncState) =>
            BeginAuthenticateAsServer(credential, null, requiredProtectionLevel, requiredImpersonationLevel, asyncCallback, asyncState);

        public virtual IAsyncResult BeginAuthenticateAsServer(
            NetworkCredential credential,
            ExtendedProtectionPolicy? policy,
            ProtectionLevel requiredProtectionLevel,
            TokenImpersonationLevel requiredImpersonationLevel,
            AsyncCallback? asyncCallback,
            object? asyncState)
        {
            ValidateCreateContext(DefaultPackage, credential, string.Empty, policy, requiredProtectionLevel, requiredImpersonationLevel);

            var result = new LazyAsyncResult(this, asyncState, asyncCallback);
            ProcessAuthentication(result);

            return result;
        }

        public virtual void EndAuthenticateAsServer(IAsyncResult asyncResult) =>
            EndProcessAuthentication(asyncResult);

        public virtual void AuthenticateAsClient() =>
            AuthenticateAsClient((NetworkCredential)CredentialCache.DefaultCredentials, null, string.Empty, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);

        public virtual void AuthenticateAsClient(NetworkCredential credential, string targetName) =>
            AuthenticateAsClient(credential, null, targetName, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);

        public virtual void AuthenticateAsClient(NetworkCredential credential, ChannelBinding? binding, string targetName) =>
            AuthenticateAsClient(credential, binding, targetName, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);

        public virtual void AuthenticateAsClient(
            NetworkCredential credential, string targetName, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel allowedImpersonationLevel) =>
            AuthenticateAsClient(credential, null, targetName, requiredProtectionLevel, allowedImpersonationLevel);

        public virtual void AuthenticateAsClient(
            NetworkCredential credential, ChannelBinding? binding, string targetName, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel allowedImpersonationLevel)
        {
            ValidateCreateContext(DefaultPackage, false, credential, targetName, binding, requiredProtectionLevel, allowedImpersonationLevel);
            ProcessAuthentication(null);
        }

        public virtual Task AuthenticateAsClientAsync() =>
            Task.Factory.FromAsync(BeginAuthenticateAsClient, EndAuthenticateAsClient, null);

        public virtual Task AuthenticateAsClientAsync(NetworkCredential credential, string targetName) =>
            Task.Factory.FromAsync(BeginAuthenticateAsClient, EndAuthenticateAsClient, credential, targetName, null);

        public virtual Task AuthenticateAsClientAsync(
            NetworkCredential credential, string targetName,
            ProtectionLevel requiredProtectionLevel,
            TokenImpersonationLevel allowedImpersonationLevel) =>
            Task.Factory.FromAsync((callback, state) => BeginAuthenticateAsClient(credential, targetName, requiredProtectionLevel, allowedImpersonationLevel, callback, state), EndAuthenticateAsClient, null);

        public virtual Task AuthenticateAsClientAsync(NetworkCredential credential, ChannelBinding? binding, string targetName) =>
            Task.Factory.FromAsync(BeginAuthenticateAsClient, EndAuthenticateAsClient, credential, binding, targetName, null);

        public virtual Task AuthenticateAsClientAsync(
            NetworkCredential credential, ChannelBinding? binding,
            string targetName, ProtectionLevel requiredProtectionLevel,
            TokenImpersonationLevel allowedImpersonationLevel) =>
            Task.Factory.FromAsync((callback, state) => BeginAuthenticateAsClient(credential, binding, targetName, requiredProtectionLevel, allowedImpersonationLevel, callback, state), EndAuthenticateAsClient, null);

        public virtual Task AuthenticateAsServerAsync() =>
            Task.Factory.FromAsync(BeginAuthenticateAsServer, EndAuthenticateAsServer, null);

        public virtual Task AuthenticateAsServerAsync(ExtendedProtectionPolicy? policy) =>
            Task.Factory.FromAsync(BeginAuthenticateAsServer, EndAuthenticateAsServer, policy, null);

        public virtual Task AuthenticateAsServerAsync(NetworkCredential credential, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel requiredImpersonationLevel) =>
            Task.Factory.FromAsync(BeginAuthenticateAsServer, EndAuthenticateAsServer, credential, requiredProtectionLevel, requiredImpersonationLevel, null);

        public virtual Task AuthenticateAsServerAsync(
            NetworkCredential credential, ExtendedProtectionPolicy? policy,
            ProtectionLevel requiredProtectionLevel,
            TokenImpersonationLevel requiredImpersonationLevel) =>
            Task.Factory.FromAsync((callback, state) => BeginAuthenticateAsServer(credential, policy, requiredProtectionLevel, requiredImpersonationLevel, callback, state), EndAuthenticateAsClient, null);

        public override bool IsAuthenticated => IsAuthenticatedCore;

        private bool IsAuthenticatedCore => _context != null && HandshakeComplete && _exception == null && _remoteOk;

        public override bool IsMutuallyAuthenticated =>
            IsAuthenticatedCore &&
            !_context!.IsNTLM && // suppressing for NTLM since SSPI does not return correct value in the context flags.
            _context.IsMutualAuthFlag;

        public override bool IsEncrypted => IsAuthenticatedCore && _context!.IsConfidentialityFlag;

        public override bool IsSigned => IsAuthenticatedCore && (_context!.IsIntegrityFlag || _context.IsConfidentialityFlag);

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
            ValidateParameters(buffer, offset, count);

            ThrowIfFailed(authSuccessCheck: true);
            if (!CanGetSecureStream)
            {
                return InnerStream.Read(buffer, offset, count);
            }

            ValueTask<int> vt = ReadAsync(new SyncReadWriteAdapter(this), new Memory<byte>(buffer, offset, count));
            Debug.Assert(vt.IsCompleted, "Should have completed synchroously with sync adapter");
            return vt.GetAwaiter().GetResult();
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateParameters(buffer, offset, count);

            ThrowIfFailed(authSuccessCheck: true);
            if (!CanGetSecureStream)
            {
                return InnerStream.ReadAsync(buffer, offset, count, cancellationToken);
            }

            return ReadAsync(new AsyncReadWriteAdapter(this, cancellationToken), new Memory<byte>(buffer, offset, count)).AsTask();
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ThrowIfFailed(authSuccessCheck: true);
            if (!CanGetSecureStream)
            {
                return InnerStream.ReadAsync(buffer, cancellationToken);
            }

            return ReadAsync(new AsyncReadWriteAdapter(this, cancellationToken), buffer);
        }

        private async ValueTask<int> ReadAsync<TAdapter>(TAdapter adapter, Memory<byte> buffer, [CallerMemberName] string? callerName = null) where TAdapter : IReadWriteAdapter
        {
            if (Interlocked.Exchange(ref _readInProgress, 1) == 1)
            {
                throw new NotSupportedException(SR.Format(SR.net_io_invalidnestedcall, callerName, "read"));
            }

            try
            {
                if (_bufferCount != 0)
                {
                    int copyBytes = Math.Min(_bufferCount, buffer.Length);
                    if (copyBytes != 0)
                    {
                        _buffer.AsMemory(_bufferOffset, copyBytes).CopyTo(buffer);
                        _bufferOffset += copyBytes;
                        _bufferCount -= copyBytes;
                    }
                    return copyBytes;
                }

                while (true)
                {
                    int readBytes = await adapter.ReadAllAsync(_readHeader).ConfigureAwait(false);
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
                    _bufferCount = readBytes;
                    _bufferOffset = 0;
                    if (_buffer.Length < readBytes)
                    {
                        _buffer = new byte[readBytes];
                    }
                    readBytes = await adapter.ReadAllAsync(new Memory<byte>(_buffer, 0, readBytes)).ConfigureAwait(false);
                    if (readBytes == 0)
                    {
                        // We already checked that the frame body is bigger than 0 bytes. Hence, this is an EOF.
                        throw new IOException(SR.net_io_eof);
                    }

                    // Decrypt into internal buffer, change "readBytes" to count now _Decrypted Bytes_
                    // Decrypted data start from zero offset, the size can be shrunk after decryption.
                    _bufferCount = readBytes = DecryptData(_buffer!, 0, readBytes, out _bufferOffset);
                    if (readBytes == 0 && buffer.Length != 0)
                    {
                        // Read again.
                        continue;
                    }

                    if (readBytes > buffer.Length)
                    {
                        readBytes = buffer.Length;
                    }

                    _buffer.AsMemory(_bufferOffset, readBytes).CopyTo(buffer);
                    _bufferOffset += readBytes;
                    _bufferCount -= readBytes;

                    return readBytes;
                }
            }
            catch (Exception e) when (!(e is IOException))
            {
                throw new IOException(SR.net_io_read, e);
            }
            finally
            {
                _readInProgress = 0;
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ValidateParameters(buffer, offset, count);

            ThrowIfFailed(authSuccessCheck: true);
            if (!CanGetSecureStream)
            {
                InnerStream.Write(buffer, offset, count);
                return;
            }

            WriteAsync(new SyncReadWriteAdapter(this), new ReadOnlyMemory<byte>(buffer, offset, count)).GetAwaiter().GetResult();
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateParameters(buffer, offset, count);

            ThrowIfFailed(authSuccessCheck: true);
            if (!CanGetSecureStream)
            {
                return InnerStream.WriteAsync(buffer, offset, count, cancellationToken);
            }

            return WriteAsync(new AsyncReadWriteAdapter(this, cancellationToken), new ReadOnlyMemory<byte>(buffer, offset, count));
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ThrowIfFailed(authSuccessCheck: true);
            if (!CanGetSecureStream)
            {
                return InnerStream.WriteAsync(buffer, cancellationToken);
            }

            return new ValueTask(WriteAsync(new AsyncReadWriteAdapter(this, cancellationToken), buffer));
        }

        private async Task WriteAsync<TAdapter>(TAdapter adapter, ReadOnlyMemory<byte> buffer) where TAdapter : IReadWriteAdapter
        {
            if (Interlocked.Exchange(ref _writeInProgress, 1) == 1)
            {
                throw new NotSupportedException(SR.Format(SR.net_io_invalidnestedcall, nameof(Write), "write"));
            }

            try
            {
                byte[]? outBuffer = null;
                while (!buffer.IsEmpty)
                {
                    int chunkBytes = Math.Min(buffer.Length, MaxWriteDataSize);
                    int encryptedBytes;
                    try
                    {
                        encryptedBytes = EncryptData(buffer.Slice(0, chunkBytes).Span, ref outBuffer);
                    }
                    catch (Exception e)
                    {
                        throw new IOException(SR.net_io_encrypt, e);
                    }

                    await InnerStream.WriteAsync(new ReadOnlyMemory<byte>(outBuffer, 0, encryptedBytes)).ConfigureAwait(false);
                    buffer = buffer.Slice(chunkBytes);
                }
            }
            catch (Exception e) when (!(e is IOException))
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

        private interface IReadWriteAdapter
        {
            ValueTask<int> ReadAsync(Memory<byte> buffer);

            ValueTask WriteAsync(byte[] buffer, int offset, int count);

            CancellationToken CancellationToken { get; }

            public async ValueTask<int> ReadAllAsync(Memory<byte> buffer)
            {
                int length = buffer.Length;

                do
                {
                    int bytes = await ReadAsync(buffer).ConfigureAwait(false);
                    if (bytes == 0)
                    {
                        if (!buffer.IsEmpty)
                        {
                            throw new IOException(SR.net_io_eof);
                        }
                        break;
                    }

                    buffer = buffer.Slice(bytes);
                }
                while (!buffer.IsEmpty);

                return length;
            }
        }

        private readonly struct AsyncReadWriteAdapter : IReadWriteAdapter
        {
            private readonly NegotiateStream _negotiateStream;

            public AsyncReadWriteAdapter(NegotiateStream negotiateStream, CancellationToken cancellationToken)
            {
                _negotiateStream = negotiateStream;
                CancellationToken = cancellationToken;
            }

            public ValueTask<int> ReadAsync(Memory<byte> buffer) =>
                _negotiateStream.InnerStream.ReadAsync(buffer, CancellationToken);

            public ValueTask WriteAsync(byte[] buffer, int offset, int count) =>
                _negotiateStream.InnerStream.WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), CancellationToken);

            public CancellationToken CancellationToken { get; }
        }

        private readonly struct SyncReadWriteAdapter : IReadWriteAdapter
        {
            private readonly NegotiateStream _negotiateStream;

            public SyncReadWriteAdapter(NegotiateStream negotiateStream) =>
                _negotiateStream = negotiateStream;

            public ValueTask<int> ReadAsync(Memory<byte> buffer) =>
                new ValueTask<int>(_negotiateStream.InnerStream.Read(buffer.Span));

            public ValueTask WriteAsync(byte[] buffer, int offset, int count)
            {
                _negotiateStream.InnerStream.Write(buffer, offset, count);
                return default;
            }

            public CancellationToken CancellationToken => default;
        }

        /// <summary>Validates user parameters for all Read/Write methods.</summary>
        private static void ValidateParameters(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (count > buffer.Length - offset)
            {
                throw new ArgumentOutOfRangeException(nameof(count), SR.net_offset_plus_count);
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

            ValidateCreateContext(package, true, credential, servicePrincipalName, _extendedProtectionPolicy!.CustomChannelBinding, protectionLevel, impersonationLevel);
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
            if (_exception != null && !_canRetryAuthentication)
            {
                ExceptionDispatchInfo.Throw(_exception);
            }

            if (_context != null && _context.IsValidContext)
            {
                throw new InvalidOperationException(SR.net_auth_reauth);
            }

            if (credential == null)
            {
                throw new ArgumentNullException(nameof(credential));
            }

            if (servicePrincipalName == null)
            {
                throw new ArgumentNullException(nameof(servicePrincipalName));
            }

            NegotiateStreamPal.ValidateImpersonationLevel(impersonationLevel);
            if (_context != null && IsServer != isServer)
            {
                throw new InvalidOperationException(SR.net_auth_client_server);
            }

            _exception = null;
            _remoteOk = false;
            _framer = new StreamFramer(InnerStream);
            _framer.WriteHeader.MessageId = FrameHeader.HandshakeId;

            _expectedProtectionLevel = protectionLevel;
            _expectedImpersonationLevel = isServer ? impersonationLevel : TokenImpersonationLevel.None;
            _writeSequenceNumber = 0;
            _readSequenceNumber = 0;

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

        private Exception SetFailed(Exception e)
        {
            if (_exception == null || !(_exception is ObjectDisposedException))
            {
                _exception = e;
            }

            _context?.CloseContext();

            return _exception;
        }

        private void ThrowIfFailed(bool authSuccessCheck)
        {
            if (_exception != null)
            {
                ExceptionDispatchInfo.Throw(_exception);
            }

            if (authSuccessCheck && !IsAuthenticatedCore)
            {
                throw new InvalidOperationException(SR.net_auth_noauth);
            }
        }

        private void ProcessAuthentication(LazyAsyncResult? lazyResult)
        {
            ThrowIfFailed(authSuccessCheck: false);
            if (Interlocked.Exchange(ref _authInProgress, 1) == 1)
            {
                throw new InvalidOperationException(SR.Format(SR.net_io_invalidnestedcall, lazyResult == null ? "BeginAuthenticate" : "Authenticate", "authenticate"));
            }

            try
            {
                if (_context!.IsServer)
                {
                    // Listen for a client blob.
                    StartReceiveBlob(lazyResult);
                }
                else
                {
                    // Start with the first blob.
                    StartSendBlob(null, lazyResult);
                }
            }
            catch (Exception e)
            {
                e = SetFailed(e);
                throw;
            }
            finally
            {
                if (lazyResult == null || _exception != null)
                {
                    _authInProgress = 0;
                }
            }
        }

        private void EndProcessAuthentication(IAsyncResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException("asyncResult");
            }

            LazyAsyncResult? lazyResult = result as LazyAsyncResult;
            if (lazyResult == null)
            {
                throw new ArgumentException(SR.Format(SR.net_io_async_result, result.GetType().FullName), "asyncResult");
            }

            if (Interlocked.Exchange(ref _authInProgress, 0) == 0)
            {
                throw new InvalidOperationException(SR.Format(SR.net_io_invalidendcall, "EndAuthenticate"));
            }

            // No "artificial" timeouts implemented so far, InnerStream controls that.
            lazyResult.InternalWaitForCompletion();

            if (lazyResult.Result is Exception e)
            {
                e = SetFailed(e);
                ExceptionDispatchInfo.Throw(e);
            }
        }

        private bool CheckSpn()
        {
            if (_context!.IsKerberos ||
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

        //
        // Client side starts here, but server also loops through this method.
        //
        private void StartSendBlob(byte[]? message, LazyAsyncResult? lazyResult)
        {
            Exception? exception = null;
            if (message != s_emptyMessage)
            {
                message = GetOutgoingBlob(message, ref exception);
            }

            if (exception != null)
            {
                // Signal remote side on a failed attempt.
                StartSendAuthResetSignal(lazyResult, message!, exception);
                return;
            }

            if (HandshakeComplete)
            {
                if (_context!.IsServer && !CheckSpn())
                {
                    exception = new AuthenticationException(SR.net_auth_bad_client_creds_or_target_mismatch);
                    int statusCode = ERROR_TRUST_FAILURE;
                    message = new byte[sizeof(long)];

                    for (int i = message.Length - 1; i >= 0; --i)
                    {
                        message[i] = (byte)(statusCode & 0xFF);
                        statusCode = (int)((uint)statusCode >> 8);
                    }

                    StartSendAuthResetSignal(lazyResult, message, exception);
                    return;
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

                    StartSendAuthResetSignal(lazyResult, message, exception);
                    return;
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

                    StartSendAuthResetSignal(lazyResult, message, exception);
                    return;
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
                if (lazyResult == null)
                {
                    _framer!.WriteMessage(message);
                }
                else
                {
                    IAsyncResult ar = _framer!.BeginWriteMessage(message, s_writeCallback, lazyResult);
                    if (!ar.CompletedSynchronously)
                    {
                        return;
                    }
                    _framer.EndWriteMessage(ar);
                }
            }

            CheckCompletionBeforeNextReceive(lazyResult);
        }

        //
        // This will check and logically complete the auth handshake.
        //
        private void CheckCompletionBeforeNextReceive(LazyAsyncResult? lazyResult)
        {
            if (HandshakeComplete && _remoteOk)
            {
                // We are done with success.
                lazyResult?.InvokeCallback();
                return;
            }

            StartReceiveBlob(lazyResult);
        }

        //
        // Server side starts here, but client also loops through this method.
        //
        private void StartReceiveBlob(LazyAsyncResult? lazyResult)
        {
            Debug.Assert(_framer != null);

            byte[]? message;
            if (lazyResult == null)
            {
                message = _framer.ReadMessage();
            }
            else
            {
                IAsyncResult ar = _framer.BeginReadMessage(s_readCallback, lazyResult);
                if (!ar.CompletedSynchronously)
                {
                    return;
                }

                message = _framer.EndReadMessage(ar);
            }

            ProcessReceivedBlob(message, lazyResult);
        }

        private void ProcessReceivedBlob(byte[]? message, LazyAsyncResult? lazyResult)
        {
            // This is an EOF otherwise we would get at least *empty* message but not a null one.
            if (message == null)
            {
                throw new AuthenticationException(SR.net_auth_eof, null);
            }

            // Process Header information.
            if (_framer!.ReadHeader.MessageId == FrameHeader.HandshakeErrId)
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

                throw new AuthenticationException(SR.net_auth_alert, null);
            }

            if (_framer.ReadHeader.MessageId == FrameHeader.HandshakeDoneId)
            {
                _remoteOk = true;
            }
            else if (_framer.ReadHeader.MessageId != FrameHeader.HandshakeId)
            {
                throw new AuthenticationException(SR.Format(SR.net_io_header_id, "MessageId", _framer.ReadHeader.MessageId, FrameHeader.HandshakeId), null);
            }

            CheckCompletionBeforeNextSend(message, lazyResult);
        }

        //
        // This will check and logically complete the auth handshake.
        //
        private void CheckCompletionBeforeNextSend(byte[] message, LazyAsyncResult? lazyResult)
        {
            //If we are done don't go into send.
            if (HandshakeComplete)
            {
                if (!_remoteOk)
                {
                    throw new AuthenticationException(SR.Format(SR.net_io_header_id, "MessageId", _framer!.ReadHeader.MessageId, FrameHeader.HandshakeDoneId), null);
                }

                lazyResult?.InvokeCallback();
                return;
            }

            // Not yet done, get a new blob and send it if any.
            StartSendBlob(message, lazyResult);
        }

        //
        //  This is to reset auth state on the remote side.
        //  If this write succeeds we will allow auth retrying.
        //
        private void StartSendAuthResetSignal(LazyAsyncResult? lazyResult, byte[] message, Exception exception)
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

            if (lazyResult == null)
            {
                _framer.WriteMessage(message);
            }
            else
            {
                lazyResult.Result = exception;
                IAsyncResult ar = _framer.BeginWriteMessage(message, s_writeCallback, lazyResult);
                if (!ar.CompletedSynchronously)
                {
                    return;
                }

                _framer.EndWriteMessage(ar);
            }

            _canRetryAuthentication = true;
            ExceptionDispatchInfo.Throw(exception);
        }

        private static void WriteCallback(IAsyncResult transportResult)
        {
            if (!(transportResult.AsyncState is LazyAsyncResult))
            {
                NetEventSource.Fail(transportResult, "State type is wrong, expected LazyAsyncResult.");
            }

            if (transportResult.CompletedSynchronously)
            {
                return;
            }

            // Async completion.
            LazyAsyncResult lazyResult = (LazyAsyncResult)transportResult.AsyncState!;
            try
            {
                NegotiateStream authState = (NegotiateStream)lazyResult.AsyncObject!;
                authState._framer!.EndWriteMessage(transportResult);

                // Special case for an error notification.
                if (lazyResult.Result is Exception e)
                {
                    authState._canRetryAuthentication = true;
                    ExceptionDispatchInfo.Throw(e);
                }

                authState.CheckCompletionBeforeNextReceive(lazyResult);
            }
            catch (Exception e) when (!lazyResult.InternalPeekCompleted) // this will throw on a worker thread.
            {
                lazyResult.InvokeCallback(e);
            }
        }

        private static void ReadCallback(IAsyncResult transportResult)
        {
            if (!(transportResult.AsyncState is LazyAsyncResult))
            {
                NetEventSource.Fail(transportResult, "State type is wrong, expected LazyAsyncResult.");
            }

            if (transportResult.CompletedSynchronously)
            {
                return;
            }

            // Async completion.
            LazyAsyncResult lazyResult = (LazyAsyncResult)transportResult.AsyncState!;
            try
            {
                NegotiateStream authState = (NegotiateStream)lazyResult.AsyncObject!;
                byte[]? message = authState._framer!.EndReadMessage(transportResult);
                authState.ProcessReceivedBlob(message, lazyResult);
            }
            catch (Exception e) when (!lazyResult.InternalPeekCompleted) // this will throw on a worker thread.
            {
                lazyResult.InvokeCallback(e);
            }
        }

        private static bool IsError(SecurityStatusPal status) =>
            (int)status.ErrorCode >= (int)SecurityStatusPalErrorCode.OutOfMemory;

        private unsafe byte[]? GetOutgoingBlob(byte[]? incomingBlob, ref Exception? e)
        {
            byte[]? message = _context!.GetOutgoingBlob(incomingBlob, false, out SecurityStatusPal statusCode);

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

        private int EncryptData(ReadOnlySpan<byte> buffer, [NotNull] ref byte[]? outBuffer)
        {
            ThrowIfFailed(authSuccessCheck: true);

            // SSPI seems to ignore this sequence number.
            ++_writeSequenceNumber;
            return _context!.Encrypt(buffer, ref outBuffer, _writeSequenceNumber);
        }

        private int DecryptData(byte[] buffer, int offset, int count, out int newOffset)
        {
            ThrowIfFailed(authSuccessCheck: true);

            // SSPI seems to ignore this sequence number.
            ++_readSequenceNumber;
            return _context!.Decrypt(buffer, offset, count, out newOffset, _readSequenceNumber);
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
