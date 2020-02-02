// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Security
{
    public partial class SslStream
    {
        private static int s_uniqueNameInteger = 123;

        private SslAuthenticationOptions _sslAuthenticationOptions;

        private int _nestedAuth;

        private SecurityStatusPal _securityStatus;

        private enum CachedSessionStatus : byte
        {
            Unknown = 0,
            IsNotCached = 1,
            IsCached = 2,
            Renegotiated = 3
        }
        private CachedSessionStatus _CachedSession;

        private enum Framing
        {
            Unknown = 0,
            BeforeSSL3,
            SinceSSL3,
            Unified,
            Invalid
        }

        // This is set on the first packet to figure out the framing style.
        private Framing _framing = Framing.Unknown;

        // SSL3/TLS protocol frames definitions.
        private enum FrameType : byte
        {
            ChangeCipherSpec = 20,
            Alert = 21,
            Handshake = 22,
            AppData = 23
        }

        // This block is used by re-handshake code to buffer data decrypted with the old key.
        private byte[] _queuedReadData;
        private int _queuedReadCount;

        //
        // This block is used to rule the >>re-handshakes<< that are concurrent with read/write I/O requests.
        //
        private const int LockNone = 0;
        private const int LockWrite = 1;
        private const int LockHandshake = 2;
        private const int LockPendingWrite = 3;
        private const int LockRead = 4;
        private const int LockPendingRead = 6;

        private const int FrameOverhead = 32;
        private const int ReadBufferSize = 4096 * 4 + FrameOverhead;         // We read in 16K chunks + headers.

        private int _lockWriteState;
        private int _lockReadState;

        private void ValidateCreateContext(SslClientAuthenticationOptions sslClientAuthenticationOptions, RemoteCertValidationCallback remoteCallback, LocalCertSelectionCallback localCallback)
        {
            ThrowIfExceptional();

            if (_context != null && _context.IsValidContext)
            {
                throw new InvalidOperationException(SR.net_auth_reauth);
            }

            if (_context != null && IsServer)
            {
                throw new InvalidOperationException(SR.net_auth_client_server);
            }

            if (sslClientAuthenticationOptions.TargetHost == null)
            {
                throw new ArgumentNullException(nameof(sslClientAuthenticationOptions.TargetHost));
            }

            _exception = null;
            try
            {
                _sslAuthenticationOptions = new SslAuthenticationOptions(sslClientAuthenticationOptions, remoteCallback, localCallback);
                if (_sslAuthenticationOptions.TargetHost.Length == 0)
                {
                    _sslAuthenticationOptions.TargetHost = "?" + Interlocked.Increment(ref s_uniqueNameInteger).ToString(NumberFormatInfo.InvariantInfo);
                }
                _context = new SecureChannel(_sslAuthenticationOptions);
            }
            catch (Win32Exception e)
            {
                throw new AuthenticationException(SR.net_auth_SSPI, e);
            }
        }

        private void ValidateCreateContext(SslAuthenticationOptions sslAuthenticationOptions)
        {
            ThrowIfExceptional();

            if (_context != null && _context.IsValidContext)
            {
                throw new InvalidOperationException(SR.net_auth_reauth);
            }

            if (_context != null && !IsServer)
            {
                throw new InvalidOperationException(SR.net_auth_client_server);
            }

            _exception = null;
            _sslAuthenticationOptions = sslAuthenticationOptions;

            try
            {
                _context = new SecureChannel(_sslAuthenticationOptions);
            }
            catch (Win32Exception e)
            {
                throw new AuthenticationException(SR.net_auth_SSPI, e);
            }
        }

        private bool RemoteCertRequired => _context == null || _context.RemoteCertRequired;

        private object SyncLock => _context;

        private int MaxDataSize => _context.MaxDataSize;

        private void SetException(Exception e)
        {
            Debug.Assert(e != null, $"Expected non-null Exception to be passed to {nameof(SetException)}");

            if (_exception == null)
            {
                _exception = ExceptionDispatchInfo.Capture(e);
            }

            _context?.Close();
        }

        //
        // This is to not depend on GC&SafeHandle class if the context is not needed anymore.
        //
        private void CloseInternal()
        {
            _exception = s_disposedSentinel;
            _context?.Close();

            // Ensure a Read operation is not in progress,
            // block potential reads since SslStream is disposing.
            // This leaves the _nestedRead = 1, but that's ok, since
            // subsequent Reads first check if the context is still available.
            if (Interlocked.CompareExchange(ref _nestedRead, 1, 0) == 0)
            {
                byte[] buffer = _internalBuffer;
                if (buffer != null)
                {
                    _internalBuffer = null;
                    _internalBufferCount = 0;
                    _internalOffset = 0;
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }

            if (_internalBuffer == null)
            {
                // Suppress finalizer if the read buffer was returned.
                GC.SuppressFinalize(this);
            }
        }

        private SecurityStatusPal EncryptData(ReadOnlyMemory<byte> buffer, ref byte[] outBuffer, out int outSize)
        {
            ThrowIfExceptionalOrNotAuthenticated();
            return _context.Encrypt(buffer, ref outBuffer, out outSize);
        }

        private SecurityStatusPal DecryptData()
        {
            ThrowIfExceptionalOrNotAuthenticated();
            return PrivateDecryptData(_internalBuffer, ref _decryptedBytesOffset, ref _decryptedBytesCount);
        }

        private SecurityStatusPal PrivateDecryptData(byte[] buffer, ref int offset, ref int count)
        {
            return _context.Decrypt(buffer, ref offset, ref count);
        }

        //
        // When re-handshaking the "old" key decrypted data are queued until the handshake is done.
        // When stream calls for decryption we will feed it queued data left from "old" encryption key.
        //
        // Must be called under the lock in case concurrent handshake is going.
        //
        private int CheckOldKeyDecryptedData(Memory<byte> buffer)
        {
            ThrowIfExceptionalOrNotAuthenticated();
            if (_queuedReadData != null)
            {
                // This is inefficient yet simple and should be a REALLY rare case.
                int toCopy = Math.Min(_queuedReadCount, buffer.Length);
                new Span<byte>(_queuedReadData, 0, toCopy).CopyTo(buffer.Span);
                _queuedReadCount -= toCopy;
                if (_queuedReadCount == 0)
                {
                    _queuedReadData = null;
                }
                else
                {
                    Buffer.BlockCopy(_queuedReadData, toCopy, _queuedReadData, 0, _queuedReadCount);
                }

                return toCopy;
            }
            return -1;
        }

        //
        // This method assumes that a SSPI context is already in a good shape.
        // For example it is either a fresh context or already authenticated context that needs renegotiation.
        //
        private Task ProcessAuthentication(bool isAsync = false, bool isApm = false, CancellationToken cancellationToken = default)
        {
            Task result = null;
            if (Interlocked.Exchange(ref _nestedAuth, 1) == 1)
            {
                throw new InvalidOperationException(SR.Format(SR.net_io_invalidnestedcall, isApm ? "BeginAuthenticate" : "Authenticate", "authenticate"));
            }

            try
            {
                ThrowIfExceptional();

                //  A trick to discover and avoid cached sessions.
                _CachedSession = CachedSessionStatus.Unknown;

                if (isAsync)
                {
                    result = ForceAuthenticationAsync(_context.IsServer, null, cancellationToken);
                }
                else
                {
                    ForceAuthentication(_context.IsServer, null);

                    if (NetEventSource.IsEnabled)
                        NetEventSource.Log.SspiSelectedCipherSuite(nameof(ProcessAuthentication),
                                                                    SslProtocol,
                                                                    CipherAlgorithm,
                                                                    CipherStrength,
                                                                    HashAlgorithm,
                                                                    HashStrength,
                                                                    KeyExchangeAlgorithm,
                                                                    KeyExchangeStrength);
                }
            }
            finally
            {
                // Operation has completed.
                _nestedAuth = 0;
            }

            return result;
        }

        //
        // This is used to reply on re-handshake when received SEC_I_RENEGOTIATE on Read().
        //
        private async Task ReplyOnReAuthenticationAsync(byte[] buffer, CancellationToken cancellationToken)
        {
            lock (SyncLock)
            {
                // Note we are already inside the read, so checking for already going concurrent handshake.
                _lockReadState = LockHandshake;
            }

            await ForceAuthenticationAsync(false, buffer, cancellationToken).ConfigureAwait(false);
            FinishHandshakeRead(LockNone);
        }

        //
        // This method attempts to start authentication.
        // Incoming buffer is either null or is the result of "renegotiate" decrypted message
        // If write is in progress the method will either wait or be put on hold
        //
        private void ForceAuthentication(bool receiveFirst, byte[] buffer)
        {
            // This will tell that we don't know the framing yet (what SSL version is)
            _framing = Framing.Unknown;

            try
            {
                if (receiveFirst)
                {
                    // Listen for a client blob.
                    ReceiveBlob(buffer);
                }
                else
                {
                    // We start with the first blob.
                    SendBlob(buffer, (buffer == null ? 0 : buffer.Length));
                }
            }
            catch (Exception e)
            {
                // Failed auth, reset the framing if any.
                _framing = Framing.Unknown;
                _handshakeCompleted = false;

                SetException(e);
                if (_exception.SourceException != e)
                {
                    ThrowIfExceptional();
                }
                throw;
            }
            finally
            {
                if (_exception != null)
                {
                    // This a failed handshake. Release waiting IO if any.
                    FinishHandshake(null);
                }
            }
        }

        internal async Task ForceAuthenticationAsync(bool receiveFirst, byte[] buffer, CancellationToken cancellationToken)
        {
            _framing = Framing.Unknown;
            ProtocolToken message;
            SslReadAsync adapter = new SslReadAsync(this, cancellationToken);

            if (!receiveFirst)
            {
                message = _context.NextMessage(buffer, 0, (buffer == null ? 0 : buffer.Length));
                if (message.Failed)
                {
                    // tracing done in NextMessage()
                    throw new AuthenticationException(SR.net_auth_SSPI, message.GetException());
                }

                await InnerStream.WriteAsync(message.Payload, cancellationToken).ConfigureAwait(false);
            }

            do
            {
                message = await ReceiveBlobAsync(adapter, buffer, cancellationToken).ConfigureAwait(false);
                if (message.Size > 0)
                {
                    // If there is message send it out even if call failed. It may contain TLS Alert.
                    await InnerStream.WriteAsync(message.Payload, cancellationToken).ConfigureAwait(false);
                }

                if (message.Failed)
                {
                    throw new AuthenticationException(SR.net_auth_SSPI, message.GetException());
                }
            } while (message.Status.ErrorCode != SecurityStatusPalErrorCode.OK);

            ProtocolToken alertToken = null;
            if (!CompleteHandshake(ref alertToken))
            {
                SendAuthResetSignal(alertToken, ExceptionDispatchInfo.Capture(new AuthenticationException(SR.net_ssl_io_cert_validation, null)));
            }

            if (NetEventSource.IsEnabled)
                NetEventSource.Log.SspiSelectedCipherSuite(nameof(ForceAuthenticationAsync),
                                                                    SslProtocol,
                                                                    CipherAlgorithm,
                                                                    CipherStrength,
                                                                    HashAlgorithm,
                                                                    HashStrength,
                                                                    KeyExchangeAlgorithm,
                                                                    KeyExchangeStrength);

        }

        //
        // Client side starts here, but server also loops through this method.
        //
        private void SendBlob(byte[] incoming, int count)
        {
            ProtocolToken message = _context.NextMessage(incoming, 0, count);
            _securityStatus = message.Status;

            if (message.Size != 0)
            {
                if (_context.IsServer && _CachedSession == CachedSessionStatus.Unknown)
                {
                    //
                    //[Schannel] If the first call to ASC returns a token less than 200 bytes,
                    //           then it's a reconnect (a handshake based on a cache entry).
                    //
                    _CachedSession = message.Size < 200 ? CachedSessionStatus.IsCached : CachedSessionStatus.IsNotCached;
                }

                if (_framing == Framing.Unified)
                {
                    _framing = DetectFraming(message.Payload, message.Payload.Length);
                }

                InnerStream.Write(message.Payload, 0, message.Size);
            }

            CheckCompletionBeforeNextReceive(message);
        }

        //
        // This will check and logically complete / fail the auth handshake.
        //
        private void CheckCompletionBeforeNextReceive(ProtocolToken message)
        {
            if (message.Failed)
            {
                SendAuthResetSignal(null, ExceptionDispatchInfo.Capture(new AuthenticationException(SR.net_auth_SSPI, message.GetException())));
                return;
            }
            else if (message.Done)
            {
                ProtocolToken alertToken = null;

                if (!CompleteHandshake(ref alertToken))
                {
                    SendAuthResetSignal(alertToken, ExceptionDispatchInfo.Capture(new AuthenticationException(SR.net_ssl_io_cert_validation, null)));
                    return;
                }

                // Release waiting IO if any. Presumably it should not throw.
                // Otherwise application may get not expected type of the exception.
                FinishHandshake(null);
                return;
            }

            ReceiveBlob(message.Payload);
        }

        //
        // Server side starts here, but client also loops through this method.
        //
        private void ReceiveBlob(byte[] buffer)
        {
            //This is first server read.
            buffer = EnsureBufferSize(buffer, 0, SecureChannel.ReadHeaderSize);

            int readBytes = FixedSizeReader.ReadPacket(_innerStream, buffer, 0, SecureChannel.ReadHeaderSize);

            if (readBytes == 0)
            {
                // EOF received
                throw new IOException(SR.net_auth_eof);
            }

            if (_framing == Framing.Unknown)
            {
                _framing = DetectFraming(buffer, readBytes);
            }

            int restBytes = GetRemainingFrameSize(buffer, 0, readBytes);

            if (restBytes < 0)
            {
                throw new IOException(SR.net_ssl_io_frame);
            }

            if (restBytes == 0)
            {
                // EOF received
                throw new AuthenticationException(SR.net_auth_eof, null);
            }

            buffer = EnsureBufferSize(buffer, readBytes, readBytes + restBytes);

            restBytes = FixedSizeReader.ReadPacket(_innerStream, buffer, readBytes, restBytes);

            SendBlob(buffer, readBytes + restBytes);
        }

        private async ValueTask<ProtocolToken> ReceiveBlobAsync(SslReadAsync adapter, byte[] buffer, CancellationToken cancellationToken)
        {
            ResetReadBuffer();
            int readBytes = await FillBufferAsync(adapter, SecureChannel.ReadHeaderSize).ConfigureAwait(false);
            if (readBytes == 0)
            {
                throw new IOException(SR.net_io_eof);
            }

            if (_framing == Framing.Unified || _framing == Framing.Unknown)
            {
                _framing = DetectFraming(_internalBuffer, readBytes);
            }

            int payloadBytes = GetRemainingFrameSize(_internalBuffer, _internalOffset, readBytes);
            if (payloadBytes < 0)
            {
                throw new IOException(SR.net_frame_read_size);
            }

            int frameSize = SecureChannel.ReadHeaderSize + payloadBytes;

            if (readBytes < frameSize)
            {
                readBytes = await FillBufferAsync(adapter, frameSize).ConfigureAwait(false);
                Debug.Assert(readBytes >= 0);
                if (readBytes == 0)
                {
                    throw new IOException(SR.net_io_eof);
                }
            }

            ProtocolToken token = _context.NextMessage(_internalBuffer, _internalOffset, frameSize);
            ConsumeBufferedBytes(frameSize);

            return token;
        }

        //
        //  This is to reset auth state on remote side.
        //  If this write succeeds we will allow auth retrying.
        //
        private void SendAuthResetSignal(ProtocolToken message, ExceptionDispatchInfo exception)
        {
            SetException(exception.SourceException);

            if (message == null || message.Size == 0)
            {
                //
                // We don't have an alert to send so cannot retry and fail prematurely.
                //
                exception.Throw();
            }

            InnerStream.Write(message.Payload, 0, message.Size);

            exception.Throw();
        }

        // - Loads the channel parameters
        // - Optionally verifies the Remote Certificate
        // - Sets HandshakeCompleted flag
        // - Sets the guarding event if other thread is waiting for
        //   handshake completion
        //
        // - Returns false if failed to verify the Remote Cert
        //
        private bool CompleteHandshake(ref ProtocolToken alertToken)
        {
            if (NetEventSource.IsEnabled)
                NetEventSource.Enter(this);

            _context.ProcessHandshakeSuccess();

            if (!_context.VerifyRemoteCertificate(_sslAuthenticationOptions.CertValidationDelegate, ref alertToken))
            {
                _handshakeCompleted = false;

                if (NetEventSource.IsEnabled)
                    NetEventSource.Exit(this, false);
                return false;
            }

            _handshakeCompleted = true;

            if (NetEventSource.IsEnabled)
                NetEventSource.Exit(this, true);
            return true;
        }

        private void FinishHandshakeRead(int newState)
        {
            lock (SyncLock)
            {
                // Lock is redundant here. Included for clarity.
                int lockState = Interlocked.Exchange(ref _lockReadState, newState);

                if (lockState != LockPendingRead)
                {
                    return;
                }

                _lockReadState = LockRead;
            }
        }

        // Returns:
        // -1    - proceed
        // 0     - queued
        // X     - some bytes are ready, no need for IO
        private int CheckEnqueueRead(Memory<byte> buffer)
        {
            int lockState = Interlocked.CompareExchange(ref _lockReadState, LockRead, LockNone);

            if (lockState != LockHandshake)
            {
                // Proceed, no concurrent handshake is ongoing so no need for a lock.
                return CheckOldKeyDecryptedData(buffer);
            }

            LazyAsyncResult lazyResult = null;
            lock (SyncLock)
            {
                int result = CheckOldKeyDecryptedData(buffer);
                if (result != -1)
                {
                    return result;
                }

                // Check again under lock.
                if (_lockReadState != LockHandshake)
                {
                    // The other thread has finished before we grabbed the lock.
                    _lockReadState = LockRead;
                    return -1;
                }

                _lockReadState = LockPendingRead;
            }
            // Need to exit from lock before waiting.
            lazyResult.InternalWaitForCompletion();
            lock (SyncLock)
            {
                return CheckOldKeyDecryptedData(buffer);
            }
        }

        private ValueTask<int> CheckEnqueueReadAsync(Memory<byte> buffer)
        {
            int lockState = Interlocked.CompareExchange(ref _lockReadState, LockRead, LockNone);

            if (lockState != LockHandshake)
            {
                // Proceed, no concurrent handshake is ongoing so no need for a lock.
                return new ValueTask<int>(CheckOldKeyDecryptedData(buffer));
            }

            lock (SyncLock)
            {
                int result = CheckOldKeyDecryptedData(buffer);
                if (result != -1)
                {
                    return new ValueTask<int>(result);
                }

                // Check again under lock.
                if (_lockReadState != LockHandshake)
                {
                    // The other thread has finished before we grabbed the lock.
                    _lockReadState = LockRead;
                    return new ValueTask<int>(-1);
                }

                _lockReadState = LockPendingRead;
                TaskCompletionSource<int> taskCompletionSource = new TaskCompletionSource<int>(buffer, TaskCreationOptions.RunContinuationsAsynchronously);
                return new ValueTask<int>(taskCompletionSource.Task);
            }
        }

        private Task CheckEnqueueWriteAsync()
        {
            // Clear previous request.
            int lockState = Interlocked.CompareExchange(ref _lockWriteState, LockWrite, LockNone);
            if (lockState != LockHandshake)
            {
                return Task.CompletedTask;
            }

            lock (SyncLock)
            {
                if (_lockWriteState != LockHandshake)
                {
                    ThrowIfExceptionalOrNotAuthenticated();
                    return Task.CompletedTask;
                }

                _lockWriteState = LockPendingWrite;
                TaskCompletionSource<int> completionSource = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                return completionSource.Task;
            }
        }

        private void CheckEnqueueWrite()
        {
            // Clear previous request.
            int lockState = Interlocked.CompareExchange(ref _lockWriteState, LockWrite, LockNone);
            if (lockState != LockHandshake)
            {
                // Proceed with write.
                return;
            }

            LazyAsyncResult lazyResult = null;
            lock (SyncLock)
            {
                if (_lockWriteState != LockHandshake)
                {
                    // Handshake has completed before we grabbed the lock.
                    ThrowIfExceptionalOrNotAuthenticated();
                    return;
                }

                _lockWriteState = LockPendingWrite;
            }

            // Need to exit from lock before waiting.
            lazyResult.InternalWaitForCompletion();
            ThrowIfExceptionalOrNotAuthenticated();
            return;
        }

        private void FinishWrite()
        {
            int lockState = Interlocked.CompareExchange(ref _lockWriteState, LockNone, LockWrite);
            if (lockState != LockHandshake)
            {
                return;
            }
        }

        private void FinishHandshake(Exception e)
        {
            lock (SyncLock)
            {
                if (e != null)
                {
                    SetException(e);
                }

                // Release read if any.
                FinishHandshakeRead(LockNone);

                // If there is a pending write we want to keep it's lock state.
                int lockState = Interlocked.CompareExchange(ref _lockWriteState, LockNone, LockHandshake);
                if (lockState != LockPendingWrite)
                {
                    return;
                }

                _lockWriteState = LockWrite;
            }
        }

        private async ValueTask WriteAsyncChunked<TWriteAdapter>(TWriteAdapter writeAdapter, ReadOnlyMemory<byte> buffer)
            where TWriteAdapter : struct, ISslWriteAdapter
        {
            do
            {
                int chunkBytes = Math.Min(buffer.Length, MaxDataSize);
                await WriteSingleChunk(writeAdapter, buffer.Slice(0, chunkBytes)).ConfigureAwait(false);
                buffer = buffer.Slice(chunkBytes);
            } while (buffer.Length != 0);
        }

        private ValueTask WriteSingleChunk<TWriteAdapter>(TWriteAdapter writeAdapter, ReadOnlyMemory<byte> buffer)
            where TWriteAdapter : struct, ISslWriteAdapter
        {
            // Request a write IO slot.
            Task ioSlot = writeAdapter.LockAsync();
            if (!ioSlot.IsCompletedSuccessfully)
            {
                // Operation is async and has been queued, return.
                return WaitForWriteIOSlot(writeAdapter, ioSlot, buffer);
            }

            byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length + FrameOverhead);
            byte[] outBuffer = rentedBuffer;

            SecurityStatusPal status = EncryptData(buffer, ref outBuffer, out int encryptedBytes);

            if (status.ErrorCode != SecurityStatusPalErrorCode.OK)
            {
                // Re-handshake status is not supported.
                ArrayPool<byte>.Shared.Return(rentedBuffer);
                ProtocolToken message = new ProtocolToken(null, status);
                return new ValueTask(Task.FromException(ExceptionDispatchInfo.SetCurrentStackTrace(new IOException(SR.net_io_encrypt, message.GetException()))));
            }

            ValueTask t = writeAdapter.WriteAsync(outBuffer, 0, encryptedBytes);
            if (t.IsCompletedSuccessfully)
            {
                ArrayPool<byte>.Shared.Return(rentedBuffer);
                FinishWrite();
                return t;
            }
            else
            {
                return CompleteAsync(t, rentedBuffer);
            }

            async ValueTask WaitForWriteIOSlot(TWriteAdapter wAdapter, Task lockTask, ReadOnlyMemory<byte> buff)
            {
                await lockTask.ConfigureAwait(false);
                await WriteSingleChunk(wAdapter, buff).ConfigureAwait(false);
            }

            async ValueTask CompleteAsync(ValueTask writeTask, byte[] bufferToReturn)
            {
                try
                {
                    await writeTask.ConfigureAwait(false);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(bufferToReturn);
                    FinishWrite();
                }
            }
        }

        //
        // Validates user parameters for all Read/Write methods.
        //
        private void ValidateParameters(byte[] buffer, int offset, int count)
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

        ~SslStream()
        {
            Dispose(disposing: false);
        }

        //We will only free the read buffer if it
        //actually contains no decrypted or encrypted bytes
        private void ReturnReadBufferIfEmpty()
        {
            if (_internalBuffer != null && _decryptedBytesCount == 0 && _internalBufferCount == 0)
            {
                ArrayPool<byte>.Shared.Return(_internalBuffer);
                _internalBuffer = null;
                _internalBufferCount = 0;
                _internalOffset = 0;
                _decryptedBytesCount = 0;
                _decryptedBytesOffset = 0;
            }
        }

        private async ValueTask<int> ReadAsyncInternal<TReadAdapter>(TReadAdapter adapter, Memory<byte> buffer)
            where TReadAdapter : ISslReadAdapter
        {
            if (Interlocked.Exchange(ref _nestedRead, 1) == 1)
            {
                throw new NotSupportedException(SR.Format(SR.net_io_invalidnestedcall, nameof(SslStream.ReadAsync), "read"));
            }

            try
            {
                while (true)
                {
                    int copyBytes;
                    if (_decryptedBytesCount != 0)
                    {
                        copyBytes = CopyDecryptedData(buffer);

                        return copyBytes;
                    }

                    copyBytes = await adapter.LockAsync(buffer).ConfigureAwait(false);
                    if (copyBytes > 0)
                    {
                        return copyBytes;
                    }

                    ResetReadBuffer();
                    int readBytes = await FillBufferAsync(adapter, SecureChannel.ReadHeaderSize).ConfigureAwait(false);
                    if (readBytes == 0)
                    {
                        return 0;
                    }

                    int payloadBytes = GetRemainingFrameSize(_internalBuffer, _internalOffset, readBytes);
                    if (payloadBytes < 0)
                    {
                        throw new IOException(SR.net_frame_read_size);
                    }

                    readBytes = await FillBufferAsync(adapter, SecureChannel.ReadHeaderSize + payloadBytes).ConfigureAwait(false);
                    Debug.Assert(readBytes >= 0);
                    if (readBytes == 0)
                    {
                        throw new IOException(SR.net_io_eof);
                    }

                    // At this point, readBytes contains the size of the header plus body.
                    // Set _decrytpedBytesOffset/Count to the current frame we have (including header)
                    // DecryptData will decrypt in-place and modify these to point to the actual decrypted data, which may be smaller.
                    _decryptedBytesOffset = _internalOffset;
                    _decryptedBytesCount = readBytes;
                    SecurityStatusPal status = DecryptData();

                    // Treat the bytes we just decrypted as consumed
                    // Note, we won't do another buffer read until the decrypted bytes are processed
                    ConsumeBufferedBytes(readBytes);

                    if (status.ErrorCode != SecurityStatusPalErrorCode.OK)
                    {
                        byte[] extraBuffer = null;
                        if (_decryptedBytesCount != 0)
                        {
                            extraBuffer = new byte[_decryptedBytesCount];
                            Buffer.BlockCopy(_internalBuffer, _decryptedBytesOffset, extraBuffer, 0, _decryptedBytesCount);

                            _decryptedBytesCount = 0;
                        }

                        ProtocolToken message = new ProtocolToken(null, status);
                        if (NetEventSource.IsEnabled)
                            NetEventSource.Info(null, $"***Processing an error Status = {message.Status}");

                        if (message.Renegotiate)
                        {
                            if (!_sslAuthenticationOptions.AllowRenegotiation)
                            {
                                if (NetEventSource.IsEnabled) NetEventSource.Fail(this, "Renegotiation was requested but it is disallowed");
                                throw new IOException(SR.net_ssl_io_renego);
                            }

                            await ReplyOnReAuthenticationAsync(extraBuffer, adapter.CancellationToken).ConfigureAwait(false);
                            // Loop on read.
                            continue;
                        }

                        if (message.CloseConnection)
                        {
                            return 0;
                        }

                        throw new IOException(SR.net_io_decrypt, message.GetException());
                    }
                }
            }
            catch (Exception e)
            {
                if (e is IOException || (e is OperationCanceledException && adapter.CancellationToken.IsCancellationRequested))
                {
                    throw;
                }

                throw new IOException(SR.net_io_read, e);
            }
            finally
            {
                _nestedRead = 0;
            }
        }

        private ValueTask<int> FillBufferAsync<TReadAdapter>(TReadAdapter adapter, int minSize)
            where TReadAdapter : ISslReadAdapter
        {
            if (_internalBufferCount >= minSize)
            {
                return new ValueTask<int>(minSize);
            }

            int initialCount = _internalBufferCount;
            do
            {
                ValueTask<int> t = adapter.ReadAsync(_internalBuffer, _internalBufferCount, _internalBuffer.Length - _internalBufferCount);
                if (!t.IsCompletedSuccessfully)
                {
                    return InternalFillBufferAsync(adapter, t, minSize, initialCount);
                }
                int bytes = t.Result;
                if (bytes == 0)
                {
                    if (_internalBufferCount != initialCount)
                    {
                        // We read some bytes, but not as many as we expected, so throw.
                        throw new IOException(SR.net_io_eof);
                    }

                    return new ValueTask<int>(0);
                }

                _internalBufferCount += bytes;
            } while (_internalBufferCount < minSize);

            return new ValueTask<int>(minSize);

            async ValueTask<int> InternalFillBufferAsync(TReadAdapter adap, ValueTask<int> task, int min, int initial)
            {
                while (true)
                {
                    int b = await task.ConfigureAwait(false);
                    if (b == 0)
                    {
                        if (_internalBufferCount != initial)
                        {
                            throw new IOException(SR.net_io_eof);
                        }

                        return 0;
                    }

                    _internalBufferCount += b;
                    if (_internalBufferCount >= min)
                    {
                        return min;
                    }

                    task = adap.ReadAsync(_internalBuffer, _internalBufferCount, _internalBuffer.Length - _internalBufferCount);
                }
            }
        }

        private async ValueTask WriteAsyncInternal<TWriteAdapter>(TWriteAdapter writeAdapter, ReadOnlyMemory<byte> buffer)
            where TWriteAdapter : struct, ISslWriteAdapter
        {
            ThrowIfExceptionalOrNotAuthenticatedOrShutdown();

            if (buffer.Length == 0 && !SslStreamPal.CanEncryptEmptyMessage)
            {
                // If it's an empty message and the PAL doesn't support that, we're done.
                return;
            }

            if (Interlocked.Exchange(ref _nestedWrite, 1) == 1)
            {
                throw new NotSupportedException(SR.Format(SR.net_io_invalidnestedcall, nameof(WriteAsync), "write"));
            }

            try
            {
                ValueTask t = buffer.Length < MaxDataSize ?
                    WriteSingleChunk(writeAdapter, buffer) :
                    WriteAsyncChunked(writeAdapter, buffer);
                await t.ConfigureAwait(false);
            }
            catch (Exception e)
            {
                FinishWrite();

                if (e is IOException || (e is OperationCanceledException && writeAdapter.CancellationToken.IsCancellationRequested))
                {
                    throw;
                }

                throw new IOException(SR.net_io_write, e);
            }
            finally
            {
                _nestedWrite = 0;
            }
        }

        private void ConsumeBufferedBytes(int byteCount)
        {
            Debug.Assert(byteCount >= 0);
            Debug.Assert(byteCount <= _internalBufferCount);

            _internalOffset += byteCount;
            _internalBufferCount -= byteCount;

            ReturnReadBufferIfEmpty();
        }

        private int CopyDecryptedData(Memory<byte> buffer)
        {
            Debug.Assert(_decryptedBytesCount > 0);

            int copyBytes = Math.Min(_decryptedBytesCount, buffer.Length);
            if (copyBytes != 0)
            {
                new Span<byte>(_internalBuffer, _decryptedBytesOffset, copyBytes).CopyTo(buffer.Span);

                _decryptedBytesOffset += copyBytes;
                _decryptedBytesCount -= copyBytes;
            }

            ReturnReadBufferIfEmpty();
            return copyBytes;
        }

        private void ResetReadBuffer()
        {
            Debug.Assert(_decryptedBytesCount == 0);

            if (_internalBuffer == null)
            {
                _internalBuffer = ArrayPool<byte>.Shared.Rent(ReadBufferSize);
            }
            else if (_internalOffset > 0)
            {
                // We have buffered data at a non-zero offset.
                // To maximize the buffer space available for the next read,
                // copy the existing data down to the beginning of the buffer.
                Buffer.BlockCopy(_internalBuffer, _internalOffset, _internalBuffer, 0, _internalBufferCount);
                _internalOffset = 0;
            }
        }

        private static byte[] EnsureBufferSize(byte[] buffer, int copyCount, int size)
        {
            if (buffer == null || buffer.Length < size)
            {
                byte[] saved = buffer;
                buffer = new byte[size];
                if (saved != null && copyCount != 0)
                {
                    Buffer.BlockCopy(saved, 0, buffer, 0, copyCount);
                }
            }
            return buffer;
        }

        // We need at least 5 bytes to determine what we have.
        private Framing DetectFraming(byte[] bytes, int length)
        {
            /* PCTv1.0 Hello starts with
             * RECORD_LENGTH_MSB  (ignore)
             * RECORD_LENGTH_LSB  (ignore)
             * PCT1_CLIENT_HELLO  (must be equal)
             * PCT1_CLIENT_VERSION_MSB (if version greater than PCTv1)
             * PCT1_CLIENT_VERSION_LSB (if version greater than PCTv1)
             *
             * ... PCT hello ...
             */

            /* Microsoft Unihello starts with
             * RECORD_LENGTH_MSB  (ignore)
             * RECORD_LENGTH_LSB  (ignore)
             * SSL2_CLIENT_HELLO  (must be equal)
             * SSL2_CLIENT_VERSION_MSB (if version greater than SSLv2) ( or v3)
             * SSL2_CLIENT_VERSION_LSB (if version greater than SSLv2) ( or v3)
             *
             * ... SSLv2 Compatible Hello ...
             */

            /* SSLv2 CLIENT_HELLO starts with
             * RECORD_LENGTH_MSB  (ignore)
             * RECORD_LENGTH_LSB  (ignore)
             * SSL2_CLIENT_HELLO  (must be equal)
             * SSL2_CLIENT_VERSION_MSB (if version greater than SSLv2) ( or v3)
             * SSL2_CLIENT_VERSION_LSB (if version greater than SSLv2) ( or v3)
             *
             * ... SSLv2 CLIENT_HELLO ...
             */

            /* SSLv2 SERVER_HELLO starts with
             * RECORD_LENGTH_MSB  (ignore)
             * RECORD_LENGTH_LSB  (ignore)
             * SSL2_SERVER_HELLO  (must be equal)
             * SSL2_SESSION_ID_HIT (ignore)
             * SSL2_CERTIFICATE_TYPE (ignore)
             * SSL2_CLIENT_VERSION_MSB (if version greater than SSLv2) ( or v3)
             * SSL2_CLIENT_VERSION_LSB (if version greater than SSLv2) ( or v3)
             *
             * ... SSLv2 SERVER_HELLO ...
             */

            /* SSLv3 Type 2 Hello starts with
              * RECORD_LENGTH_MSB  (ignore)
              * RECORD_LENGTH_LSB  (ignore)
              * SSL2_CLIENT_HELLO  (must be equal)
              * SSL2_CLIENT_VERSION_MSB (if version greater than SSLv3)
              * SSL2_CLIENT_VERSION_LSB (if version greater than SSLv3)
              *
              * ... SSLv2 Compatible Hello ...
              */

            /* SSLv3 Type 3 Hello starts with
             * 22 (HANDSHAKE MESSAGE)
             * VERSION MSB
             * VERSION LSB
             * RECORD_LENGTH_MSB  (ignore)
             * RECORD_LENGTH_LSB  (ignore)
             * HS TYPE (CLIENT_HELLO)
             * 3 bytes HS record length
             * HS Version
             * HS Version
             */

            /* SSLv2 message codes
             * SSL_MT_ERROR                0
             * SSL_MT_CLIENT_HELLO         1
             * SSL_MT_CLIENT_MASTER_KEY    2
             * SSL_MT_CLIENT_FINISHED      3
             * SSL_MT_SERVER_HELLO         4
             * SSL_MT_SERVER_VERIFY        5
             * SSL_MT_SERVER_FINISHED      6
             * SSL_MT_REQUEST_CERTIFICATE  7
             * SSL_MT_CLIENT_CERTIFICATE   8
             */

            int version = -1;

            if ((bytes == null || bytes.Length <= 0))
            {
                NetEventSource.Fail(this, "Header buffer is not allocated.");
            }

            // If the first byte is SSL3 HandShake, then check if we have a SSLv3 Type3 client hello.
            if (bytes[0] == (byte)FrameType.Handshake || bytes[0] == (byte)FrameType.AppData
                || bytes[0] == (byte)FrameType.Alert)
            {
                if (length < 3)
                {
                    return Framing.Invalid;
                }

#if TRACE_VERBOSE
                if (bytes[1] != 3 && NetEventSource.IsEnabled)
                {
                    if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"WARNING: SslState::DetectFraming() SSL protocol is > 3, trying SSL3 framing in retail = {bytes[1]:x}");
                }
#endif

                version = (bytes[1] << 8) | bytes[2];
                if (version < 0x300 || version >= 0x500)
                {
                    return Framing.Invalid;
                }

                //
                // This is an SSL3 Framing
                //
                return Framing.SinceSSL3;
            }

#if TRACE_VERBOSE
            if ((bytes[0] & 0x80) == 0 && NetEventSource.IsEnabled)
            {
                // We have a three-byte header format
                if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"WARNING: SslState::DetectFraming() SSL v <=2 HELLO has no high bit set for 3 bytes header, we are broken, received byte = {bytes[0]:x}");
            }
#endif

            if (length < 3)
            {
                return Framing.Invalid;
            }

            if (bytes[2] > 8)
            {
                return Framing.Invalid;
            }

            if (bytes[2] == 0x1)  // SSL_MT_CLIENT_HELLO
            {
                if (length >= 5)
                {
                    version = (bytes[3] << 8) | bytes[4];
                }
            }
            else if (bytes[2] == 0x4) // SSL_MT_SERVER_HELLO
            {
                if (length >= 7)
                {
                    version = (bytes[5] << 8) | bytes[6];
                }
            }

            if (version != -1)
            {
                // If this is the first packet, the client may start with an SSL2 packet
                // but stating that the version is 3.x, so check the full range.
                // For the subsequent packets we assume that an SSL2 packet should have a 2.x version.
                if (_framing == Framing.Unknown)
                {
                    if (version != 0x0002 && (version < 0x200 || version >= 0x500))
                    {
                        return Framing.Invalid;
                    }
                }
                else
                {
                    if (version != 0x0002)
                    {
                        return Framing.Invalid;
                    }
                }
            }

            // When server has replied the framing is already fixed depending on the prior client packet
            if (!_context.IsServer || _framing == Framing.Unified)
            {
                return Framing.BeforeSSL3;
            }

            return Framing.Unified; // Will use Ssl2 just for this frame.
        }

        //
        // This is called from SslStream class too.
        private int GetRemainingFrameSize(byte[] buffer, int offset, int dataSize)
        {
            if (NetEventSource.IsEnabled)
                NetEventSource.Enter(this, buffer, offset, dataSize);

            int payloadSize = -1;
            switch (_framing)
            {
                case Framing.Unified:
                case Framing.BeforeSSL3:
                    if (dataSize < 2)
                    {
                        throw new System.IO.IOException(SR.net_ssl_io_frame);
                    }
                    // Note: Cannot detect version mismatch for <= SSL2

                    if ((buffer[offset] & 0x80) != 0)
                    {
                        // Two bytes
                        payloadSize = (((buffer[offset] & 0x7f) << 8) | buffer[offset + 1]) + 2;
                        payloadSize -= dataSize;
                    }
                    else
                    {
                        // Three bytes
                        payloadSize = (((buffer[offset] & 0x3f) << 8) | buffer[offset + 1]) + 3;
                        payloadSize -= dataSize;
                    }

                    break;
                case Framing.SinceSSL3:
                    if (dataSize < 5)
                    {
                        throw new System.IO.IOException(SR.net_ssl_io_frame);
                    }

                    payloadSize = ((buffer[offset + 3] << 8) | buffer[offset + 4]) + 5;
                    payloadSize -= dataSize;
                    break;
                default:
                    break;
            }

            if (NetEventSource.IsEnabled)
                NetEventSource.Exit(this, payloadSize);
            return payloadSize;
        }
    }
}
