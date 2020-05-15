// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Principal;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Net.Security
{
    /// <summary>
    /// Provides a stream that uses the Negotiate security protocol to authenticate the client, and optionally the server, in client-server communication.
    /// </summary>
    public partial class NegotiateStream : AuthenticatedStream
    {
        private readonly NegoState _negoState;
        private readonly byte[] _readHeader;

        private IIdentity? _remoteIdentity;
        private byte[] _buffer;
        private int _bufferOffset;
        private int _bufferCount;

        private volatile int _writeInProgress;
        private volatile int _readInProgress;

        public NegotiateStream(Stream innerStream) : this(innerStream, false)
        {
        }

        public NegotiateStream(Stream innerStream, bool leaveInnerStreamOpen) : base(innerStream, leaveInnerStreamOpen)
        {
            _negoState = new NegoState(innerStream);
            _readHeader = new byte[4];
            _buffer = Array.Empty<byte>();
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                _negoState.Close();
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
                _negoState.Close();
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
            _negoState.ValidateCreateContext(NegoState.DefaultPackage, false, credential, targetName, binding, requiredProtectionLevel, allowedImpersonationLevel);

            LazyAsyncResult result = new LazyAsyncResult(_negoState, asyncState, asyncCallback);
            _negoState.ProcessAuthentication(result);

            return result;
        }

        public virtual void EndAuthenticateAsClient(IAsyncResult asyncResult) =>
            _negoState.EndProcessAuthentication(asyncResult);

        public virtual void AuthenticateAsServer() =>
            AuthenticateAsServer((NetworkCredential)CredentialCache.DefaultCredentials, null, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);

        public virtual void AuthenticateAsServer(ExtendedProtectionPolicy? policy) =>
            AuthenticateAsServer((NetworkCredential)CredentialCache.DefaultCredentials, policy, ProtectionLevel.EncryptAndSign, TokenImpersonationLevel.Identification);

        public virtual void AuthenticateAsServer(NetworkCredential credential, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel requiredImpersonationLevel) =>
            AuthenticateAsServer(credential, null, requiredProtectionLevel, requiredImpersonationLevel);

        public virtual void AuthenticateAsServer(NetworkCredential credential, ExtendedProtectionPolicy? policy, ProtectionLevel requiredProtectionLevel, TokenImpersonationLevel requiredImpersonationLevel)
        {
            _negoState.ValidateCreateContext(NegoState.DefaultPackage, credential, string.Empty, policy, requiredProtectionLevel, requiredImpersonationLevel);
            _negoState.ProcessAuthentication(null);
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
            _negoState.ValidateCreateContext(NegoState.DefaultPackage, credential, string.Empty, policy, requiredProtectionLevel, requiredImpersonationLevel);

            LazyAsyncResult result = new LazyAsyncResult(_negoState, asyncState, asyncCallback);
            _negoState.ProcessAuthentication(result);

            return result;
        }

        public virtual void EndAuthenticateAsServer(IAsyncResult asyncResult) =>
            _negoState.EndProcessAuthentication(asyncResult);

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
            _negoState.ValidateCreateContext(NegoState.DefaultPackage, false, credential, targetName, binding, requiredProtectionLevel, allowedImpersonationLevel);
            _negoState.ProcessAuthentication(null);
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

        public override bool IsAuthenticated => _negoState.IsAuthenticated;

        public override bool IsMutuallyAuthenticated => _negoState.IsMutuallyAuthenticated;

        public override bool IsEncrypted => _negoState.IsEncrypted;

        public override bool IsSigned => _negoState.IsSigned;

        public override bool IsServer => _negoState.IsServer;

        public virtual TokenImpersonationLevel ImpersonationLevel => _negoState.AllowedImpersonation;

        public virtual IIdentity RemoteIdentity => _remoteIdentity ??= _negoState.GetIdentity();

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

            _negoState.CheckThrow(true);
            if (!_negoState.CanGetSecureStream)
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

            _negoState.CheckThrow(true);
            if (!_negoState.CanGetSecureStream)
            {
                return InnerStream.ReadAsync(buffer, offset, count, cancellationToken);
            }

            return ReadAsync(new AsyncReadWriteAdapter(this, cancellationToken), new Memory<byte>(buffer, offset, count)).AsTask();
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            _negoState.CheckThrow(true);
            if (!_negoState.CanGetSecureStream)
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
                    if (readBytes <= 4 || readBytes > NegoState.MaxReadFrameSize)
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
                    _bufferCount = readBytes = _negoState.DecryptData(_buffer!, 0, readBytes, out _bufferOffset);
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

            _negoState.CheckThrow(true);
            if (!_negoState.CanGetSecureStream)
            {
                InnerStream.Write(buffer, offset, count);
                return;
            }

            WriteAsync(new SyncReadWriteAdapter(this), new ReadOnlyMemory<byte>(buffer, offset, count)).GetAwaiter().GetResult();
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateParameters(buffer, offset, count);

            _negoState.CheckThrow(true);
            if (!_negoState.CanGetSecureStream)
            {
                return InnerStream.WriteAsync(buffer, offset, count, cancellationToken);
            }

            return WriteAsync(new AsyncReadWriteAdapter(this, cancellationToken), new ReadOnlyMemory<byte>(buffer, offset, count));
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            _negoState.CheckThrow(true);
            if (!_negoState.CanGetSecureStream)
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
                    int chunkBytes = Math.Min(buffer.Length, NegoState.MaxWriteDataSize);
                    int encryptedBytes;
                    try
                    {
                        encryptedBytes = _negoState.EncryptData(buffer.Slice(0, chunkBytes).Span, ref outBuffer);
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
    }
}
