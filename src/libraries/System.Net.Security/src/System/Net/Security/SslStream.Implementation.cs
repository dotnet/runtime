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

        private SslAuthenticationOptions? _sslAuthenticationOptions;

        private int _nestedAuth;

        private enum Framing
        {
            Unknown = 0,    // Initial before any frame is processd.
            BeforeSSL3,     // SSlv2
            SinceSSL3,      // SSlv3 & TLS
            Unified,        // Intermediate on first frame until response is processes.
            Invalid         // Somthing is wrong.
        }

        // This is set on the first packet to figure out the framing style.
        private Framing _framing = Framing.Unknown;

        private TlsAlertDescription _lastAlertDescription;
        private TlsFrameHandshakeInfo _lastFrame;

        private readonly object _handshakeLock = new object();
        private volatile TaskCompletionSource<bool>? _handshakeWaiter;

        private const int FrameOverhead = 32;
        private const int ReadBufferSize = 4096 * 4 + FrameOverhead;         // We read in 16K chunks + headers.
        private const int InitialHandshakeBufferSize = 4096 + FrameOverhead; // try to fit at least 4K ServerCertificate
        private ArrayBuffer _handshakeBuffer;

        private void ValidateCreateContext(SslClientAuthenticationOptions sslClientAuthenticationOptions, RemoteCertValidationCallback remoteCallback, LocalCertSelectionCallback? localCallback)
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
                if (_sslAuthenticationOptions.TargetHost!.Length == 0)
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

        private object? SyncLock => _context;

        private int MaxDataSize => _context!.MaxDataSize;

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
                byte[]? buffer = _internalBuffer;
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

            lock (_handshakeLock)
            {
                if (_handshakeWaiter != null)
                {
                    outSize = 0;
                    // avoid waiting under lock.
                    return new SecurityStatusPal(SecurityStatusPalErrorCode.TryAgain);
                }

                return _context!.Encrypt(buffer, ref outBuffer, out outSize);
            }
        }

        private SecurityStatusPal DecryptData()
        {
            ThrowIfExceptionalOrNotAuthenticated();
            return PrivateDecryptData(_internalBuffer, ref _decryptedBytesOffset, ref _decryptedBytesCount);
        }

        private SecurityStatusPal PrivateDecryptData(byte[]? buffer, ref int offset, ref int count)
        {
            return _context!.Decrypt(buffer, ref offset, ref count);
        }

        //
        // This method assumes that a SSPI context is already in a good shape.
        // For example it is either a fresh context or already authenticated context that needs renegotiation.
        //
        private Task? ProcessAuthentication(bool isAsync = false, bool isApm = false, CancellationToken cancellationToken = default)
        {
            Task? result;

            ThrowIfExceptional();

            if (isAsync)
            {
                result = ForceAuthenticationAsync(new AsyncReadWriteAdapter(InnerStream, cancellationToken), _context!.IsServer, null, isApm);
            }
            else
            {
                ForceAuthenticationAsync(new SyncReadWriteAdapter(InnerStream), _context!.IsServer, null).GetAwaiter().GetResult();
                result = null;
            }

            return result;
        }

        //
        // This is used to reply on re-handshake when received SEC_I_RENEGOTIATE on Read().
        //
        private async Task ReplyOnReAuthenticationAsync<TIOAdapter>(TIOAdapter adapter, byte[]? buffer)
            where TIOAdapter : IReadWriteAdapter
        {
            try
            {
                await ForceAuthenticationAsync(adapter, receiveFirst: false, buffer).ConfigureAwait(false);
            }
            finally
            {
                _handshakeWaiter!.SetResult(true);
                _handshakeWaiter = null;
            }
        }

        // reAuthenticationData is only used on Windows in case of renegotiation.
        private async Task ForceAuthenticationAsync<TIOAdapter>(TIOAdapter adapter, bool receiveFirst, byte[]? reAuthenticationData, bool isApm = false)
             where TIOAdapter : IReadWriteAdapter
        {
            ProtocolToken message;
            bool handshakeCompleted = false;

            if (reAuthenticationData == null)
            {
                // prevent nesting only when authentication functions are called explicitly. e.g. handle renegotiation transparently.
                if (Interlocked.Exchange(ref _nestedAuth, 1) == 1)
                {
                    throw new InvalidOperationException(SR.Format(SR.net_io_invalidnestedcall, isApm ? "BeginAuthenticate" : "Authenticate", "authenticate"));
                }
            }

            try
            {

                if (!receiveFirst)
                {
                    message = _context!.NextMessage(reAuthenticationData);
                    if (message.Size > 0)
                    {
                        await adapter.WriteAsync(message.Payload!, 0, message.Size).ConfigureAwait(false);
                    }

                    if (message.Failed)
                    {
                        // tracing done in NextMessage()
                        throw new AuthenticationException(SR.net_auth_SSPI, message.GetException());
                    }

                    if (message.Status.ErrorCode == SecurityStatusPalErrorCode.OK)
                    {
                        // We can finish renegotiation without doing any read.
                        handshakeCompleted = true;
                    }
                }

                if (!handshakeCompleted)
                {
                    // get ready to receive first frame
                    _handshakeBuffer = new ArrayBuffer(InitialHandshakeBufferSize);
                }

                while (!handshakeCompleted)
                {
                    message = await ReceiveBlobAsync(adapter).ConfigureAwait(false);
                    if (message.Size > 0)
                    {
                        // If there is message send it out even if call failed. It may contain TLS Alert.
                        await adapter.WriteAsync(message.Payload!, 0, message.Size).ConfigureAwait(false);
                    }

                    if (message.Failed)
                    {
                        if (_lastFrame.Header.Type == TlsContentType.Handshake && message.Size == 0)
                        {
                            // If we failed without OS sending out alert, inject one here to be consistent across platforms.
                            byte[] alert = TlsFrameHelper.CreateAlertFrame(_lastFrame.Header.Version, TlsAlertDescription.ProtocolVersion);
                            await adapter.WriteAsync(alert, 0, alert.Length).ConfigureAwait(false);
                        }
                        else if (_lastFrame.Header.Type == TlsContentType.Alert && _lastAlertDescription != TlsAlertDescription.CloseNotify &&
                                 message.Status.ErrorCode == SecurityStatusPalErrorCode.IllegalMessage)
                        {
                            // Improve generic message and show details if we failed because of TLS Alert.
                            throw new AuthenticationException(SR.Format(SR.net_auth_tls_alert, _lastAlertDescription.ToString()), message.GetException());
                        }

                        throw new AuthenticationException(SR.net_auth_SSPI, message.GetException());
                    }
                    else if (message.Status.ErrorCode == SecurityStatusPalErrorCode.OK)
                    {
                        // We can finish renegotiation without doing any read.
                        handshakeCompleted = true;
                    }
                }

                if (_handshakeBuffer.ActiveLength > 0)
                {
                    // If we read more than we needed for handshake, move it to input buffer for further processing.
                    ResetReadBuffer();
                    _handshakeBuffer.ActiveSpan.CopyTo(_internalBuffer);
                    _internalBufferCount = _handshakeBuffer.ActiveLength;
                }

                ProtocolToken? alertToken = null;
                if (!CompleteHandshake(ref alertToken))
                {
                    SendAuthResetSignal(alertToken, ExceptionDispatchInfo.Capture(new AuthenticationException(SR.net_ssl_io_cert_validation, null)));
                }
            }
            finally
            {
                _handshakeBuffer.Dispose();
                if (reAuthenticationData == null)
                {
                    _nestedAuth = 0;
                }
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

        private async ValueTask<ProtocolToken> ReceiveBlobAsync<TIOAdapter>(TIOAdapter adapter)
                 where TIOAdapter : IReadWriteAdapter
        {
            int readBytes = await FillHandshakeBufferAsync(adapter, SecureChannel.ReadHeaderSize).ConfigureAwait(false);
            if (readBytes == 0)
            {
                throw new IOException(SR.net_io_eof);
            }

            if (_framing == Framing.Unified || _framing == Framing.Unknown)
            {
                _framing = DetectFraming(_handshakeBuffer.ActiveReadOnlySpan);
            }

            if (_framing != Framing.SinceSSL3)
            {
#pragma warning disable 0618
                _lastFrame.Header.Version = SslProtocols.Ssl2;
#pragma warning restore 0618
                _lastFrame.Header.Length = GetFrameSize(_handshakeBuffer.ActiveReadOnlySpan) - TlsFrameHelper.HeaderSize;
            }
            else
            {
                TlsFrameHelper.TryGetFrameHeader(_handshakeBuffer.ActiveReadOnlySpan, ref _lastFrame.Header);
            }

            if (_lastFrame.Header.Length < 0)
            {
                throw new IOException(SR.net_frame_read_size);
            }

            // Header length is content only so we must add header size as well.
            int frameSize = _lastFrame.Header.Length + TlsFrameHelper.HeaderSize;
            if (_handshakeBuffer.ActiveLength < frameSize)
            {
                await FillHandshakeBufferAsync(adapter, frameSize).ConfigureAwait(false);
            }

            // At this point, we have at least one TLS frame.
            if (_lastFrame.Header.Type == TlsContentType.Alert)
            {
                TlsAlertLevel level = 0;
                if (TlsFrameHelper.TryGetAlertInfo(_handshakeBuffer.ActiveReadOnlySpan, ref level, ref _lastAlertDescription))
                {
                    if (NetEventSource.IsEnabled && _lastAlertDescription != TlsAlertDescription.CloseNotify) NetEventSource.Fail(this, $"Received TLS alert {_lastAlertDescription}");
                }
            }
            else if (_lastFrame.Header.Type == TlsContentType.Handshake)
            {
                if (_handshakeBuffer.ActiveReadOnlySpan[TlsFrameHelper.HeaderSize] == (byte)TlsHandshakeType.ClientHello &&
                    _sslAuthenticationOptions!.ServerCertSelectionDelegate != null)
                {
                    // Process SNI from Client Hello message
                    TlsFrameHelper.TryGetHandshakeInfo(_handshakeBuffer.ActiveReadOnlySpan, ref _lastFrame);
                    _sslAuthenticationOptions.TargetHost = _lastFrame.TargetName;
                }
            }

            return ProcessBlob(frameSize);
        }

        // Calls crypto on received data. No IO inside.
        private ProtocolToken ProcessBlob(int frameSize)
        {
            int chunkSize = frameSize;

            ReadOnlySpan<byte> availableData = _handshakeBuffer.ActiveReadOnlySpan;
            // Discard() does not touch data, it just increases start index so next
            // ActiveSpan will exclude the "discarded" data.
            _handshakeBuffer.Discard(frameSize);

            if (_framing == Framing.SinceSSL3)
            {
                // Often more TLS messages fit into same packet. Get as many complete frames as we can.
                while (_handshakeBuffer.ActiveLength > TlsFrameHelper.HeaderSize)
                {
                    TlsFrameHeader nextHeader = default;

                    if (!TlsFrameHelper.TryGetFrameHeader(_handshakeBuffer.ActiveReadOnlySpan, ref nextHeader))
                    {
                        break;
                    }

                    frameSize = nextHeader.Length + TlsFrameHelper.HeaderSize;
                    if (nextHeader.Type == TlsContentType.AppData || frameSize > _handshakeBuffer.ActiveLength)
                    {
                        // We don't have full frame left or we already have app data which needs to be processed by decrypt.
                        break;
                    }

                    chunkSize += frameSize;
                    _handshakeBuffer.Discard(frameSize);
                }
            }

            return _context!.NextMessage(availableData.Slice(0, chunkSize));
        }

        //
        //  This is to reset auth state on remote side.
        //  If this write succeeds we will allow auth retrying.
        //
        private void SendAuthResetSignal(ProtocolToken? message, ExceptionDispatchInfo exception)
        {
            SetException(exception.SourceException);

            if (message == null || message.Size == 0)
            {
                //
                // We don't have an alert to send so cannot retry and fail prematurely.
                //
                exception.Throw();
            }

            InnerStream.Write(message.Payload!, 0, message.Size);

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
        private bool CompleteHandshake(ref ProtocolToken? alertToken)
        {
            if (NetEventSource.IsEnabled)
                NetEventSource.Enter(this);

            _context!.ProcessHandshakeSuccess();

            if (!_context.VerifyRemoteCertificate(_sslAuthenticationOptions!.CertValidationDelegate, ref alertToken))
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

        private async ValueTask WriteAsyncChunked<TIOAdapter>(TIOAdapter writeAdapter, ReadOnlyMemory<byte> buffer)
            where TIOAdapter : struct, IReadWriteAdapter
        {
            do
            {
                int chunkBytes = Math.Min(buffer.Length, MaxDataSize);
                await WriteSingleChunk(writeAdapter, buffer.Slice(0, chunkBytes)).ConfigureAwait(false);
                buffer = buffer.Slice(chunkBytes);
            } while (buffer.Length != 0);
        }

        private ValueTask WriteSingleChunk<TIOAdapter>(TIOAdapter writeAdapter, ReadOnlyMemory<byte> buffer)
            where TIOAdapter : struct, IReadWriteAdapter
        {
            byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length + FrameOverhead);
            byte[] outBuffer = rentedBuffer;

            SecurityStatusPal status;
            int encryptedBytes;
            while (true)
            {
                status = EncryptData(buffer, ref outBuffer, out encryptedBytes);

                // TryAgain should be rare, when renegotiation happens exactly when we want to write.
                if (status.ErrorCode != SecurityStatusPalErrorCode.TryAgain)
                {
                    break;
                }

                TaskCompletionSource<bool>? waiter = _handshakeWaiter;
                if (waiter != null)
                {
                    Task waiterTask = writeAdapter.WaitAsync(waiter);
                    // We finished synchronously waiting for renegotiation. We can try again immediately.
                    if (waiterTask.IsCompletedSuccessfully)
                    {
                        continue;
                    }

                    // We need to wait asynchronously as well as for the write when EncryptData is finished.
                    return WaitAndWriteAsync(writeAdapter, buffer, waiterTask, rentedBuffer);
                }
            }

            if (status.ErrorCode != SecurityStatusPalErrorCode.OK)
            {
                ArrayPool<byte>.Shared.Return(rentedBuffer);
                return new ValueTask(Task.FromException(ExceptionDispatchInfo.SetCurrentStackTrace(new IOException(SR.net_io_encrypt, SslStreamPal.GetException(status)))));
            }

            ValueTask t = writeAdapter.WriteAsync(outBuffer, 0, encryptedBytes);
            if (t.IsCompletedSuccessfully)
            {
                ArrayPool<byte>.Shared.Return(rentedBuffer);
                return t;
            }
            else
            {
                return CompleteWriteAsync(t, rentedBuffer);
            }

            async ValueTask WaitAndWriteAsync(TIOAdapter writeAdapter, ReadOnlyMemory<byte> buffer, Task waitTask, byte[] rentedBuffer)
            {
                byte[]? bufferToReturn = rentedBuffer;
                byte[] outBuffer = rentedBuffer;
                try
                {
                    // Wait for renegotiation to finish.
                    await waitTask.ConfigureAwait(false);

                    SecurityStatusPal status = EncryptData(buffer, ref outBuffer, out int encryptedBytes);
                    if (status.ErrorCode == SecurityStatusPalErrorCode.TryAgain)
                    {
                        // No need to hold on the buffer any more.
                        ArrayPool<byte>.Shared.Return(bufferToReturn);
                        bufferToReturn = null;
                        // Call WriteSingleChunk() recursively to avoid code duplication.
                        // This should be extremely rare in cases when second renegotiation happens concurrently with Write.
                        await WriteSingleChunk(writeAdapter, buffer).ConfigureAwait(false);
                    }
                    else if (status.ErrorCode == SecurityStatusPalErrorCode.OK)
                    {
                        await writeAdapter.WriteAsync(outBuffer, 0, encryptedBytes).ConfigureAwait(false);
                    }
                    else
                    {
                        throw new IOException(SR.net_io_encrypt, SslStreamPal.GetException(status));
                    }
                }
                finally
                {
                    if (bufferToReturn != null)
                    {
                        ArrayPool<byte>.Shared.Return(bufferToReturn);
                    }
                }
            }

            async ValueTask CompleteWriteAsync(ValueTask writeTask, byte[] bufferToReturn)
            {
                try
                {
                    await writeTask.ConfigureAwait(false);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(bufferToReturn);
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

        private async ValueTask<int> ReadAsyncInternal<TIOAdapter>(TIOAdapter adapter, Memory<byte> buffer)
            where TIOAdapter : IReadWriteAdapter
        {
            if (Interlocked.Exchange(ref _nestedRead, 1) == 1)
            {
                throw new NotSupportedException(SR.Format(SR.net_io_invalidnestedcall, nameof(SslStream.ReadAsync), "read"));
            }

            try
            {
                while (true)
                {
                    if (_decryptedBytesCount != 0)
                    {
                        return CopyDecryptedData(buffer);
                    }

                    ResetReadBuffer();

                    // Read the next frame header.
                    if (_internalBufferCount < SecureChannel.ReadHeaderSize)
                    {
                        // We don't have enough bytes buffered, so issue an initial read to try to get enough.  This is
                        // done in this method both to better consolidate error handling logic (the first read is the special
                        // case that needs to differentiate reading 0 from > 0, and everything else needs to throw if it
                        // doesn't read enough), and to minimize the chances that in the common case the FillBufferAsync
                        // helper needs to yield and allocate a state machine.
                        int readBytes = await adapter.ReadAsync(_internalBuffer.AsMemory(_internalBufferCount)).ConfigureAwait(false);
                        if (readBytes == 0)
                        {
                            return 0;
                        }

                        _internalBufferCount += readBytes;
                        if (_internalBufferCount < SecureChannel.ReadHeaderSize)
                        {
                            await FillBufferAsync(adapter, SecureChannel.ReadHeaderSize).ConfigureAwait(false);
                        }
                    }
                    Debug.Assert(_internalBufferCount >= SecureChannel.ReadHeaderSize);

                    // Parse the frame header to determine the payload size (which includes the header size).
                    int payloadBytes = GetFrameSize(_internalBuffer.AsSpan(_internalOffset));
                    if (payloadBytes < 0)
                    {
                        throw new IOException(SR.net_frame_read_size);
                    }

                    // Read in the rest of the payload if we don't have it.
                    if (_internalBufferCount < payloadBytes)
                    {
                        await FillBufferAsync(adapter, payloadBytes).ConfigureAwait(false);
                    }

                    // Set _decrytpedBytesOffset/Count to the current frame we have (including header)
                    // DecryptData will decrypt in-place and modify these to point to the actual decrypted data, which may be smaller.
                    _decryptedBytesOffset = _internalOffset;
                    _decryptedBytesCount = payloadBytes;

                    SecurityStatusPal status;
                    lock (_handshakeLock)
                    {
                        status = DecryptData();
                        if (status.ErrorCode == SecurityStatusPalErrorCode.Renegotiate)
                        {
                            // The status indicates that peer wants to renegotiate. (Windows only)
                            // In practice, there can be some other reasons too - like TLS1.3 session creation
                            // of alert handling. We need to pass the data to lsass and it is not safe to do parallel
                            // write any more as that can change TLS state and the EncryptData() can fail in strange ways.

                            // To handle this we call DecryptData() under lock and we create TCS waiter.
                            // EncryptData() checks that under same lock and if it exist it will not call low-level crypto.
                            // Instead it will wait synchronously or asynchronously and it will try again after the wait.
                            // The result will be set when ReplyOnReAuthenticationAsync() is finished e.g. lsass business is over.
                            // If that happen before EncryptData() runs, _handshakeWaiter will be set to null
                            // and EncryptData() will work normally e.g. no waiting, just exclusion with DecryptData()


                            if (_sslAuthenticationOptions!.AllowRenegotiation || SslProtocol == SslProtocols.Tls13)
                            {
                                // create TCS only if we plan to proceed. If not, we will throw in block bellow outside of the lock.
                                // Tls1.3 does not have renegotiation. However on Windows this error code is used
                                // for session management e.g. anything lsass needs to see.
                                _handshakeWaiter = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                            }
                        }
                    }

                    // Treat the bytes we just decrypted as consumed
                    // Note, we won't do another buffer read until the decrypted bytes are processed
                    ConsumeBufferedBytes(payloadBytes);

                    if (status.ErrorCode != SecurityStatusPalErrorCode.OK)
                    {
                        byte[]? extraBuffer = null;
                        if (_decryptedBytesCount != 0)
                        {
                            extraBuffer = new byte[_decryptedBytesCount];
                            Buffer.BlockCopy(_internalBuffer!, _decryptedBytesOffset, extraBuffer, 0, _decryptedBytesCount);

                            _decryptedBytesCount = 0;
                        }

                        if (NetEventSource.IsEnabled)
                            NetEventSource.Info(null, $"***Processing an error Status = {status}");

                        if (status.ErrorCode == SecurityStatusPalErrorCode.Renegotiate)
                        {
                            // We determine above that we will not process it.
                            if (_handshakeWaiter == null)
                            {
                                if (NetEventSource.IsEnabled) NetEventSource.Fail(this, "Renegotiation was requested but it is disallowed");
                                throw new IOException(SR.net_ssl_io_renego);
                            }

                            await ReplyOnReAuthenticationAsync(adapter, extraBuffer).ConfigureAwait(false);
                            // Loop on read.
                            continue;
                        }

                        if (status.ErrorCode == SecurityStatusPalErrorCode.ContextExpired)
                        {
                            return 0;
                        }

                        throw new IOException(SR.net_io_decrypt, SslStreamPal.GetException(status));
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

        // This function tries to make sure buffer has at least minSize bytes available.
        // If we have enough data, it returns synchronously. If not, it will try to read
        // remaining bytes from given stream.
        private ValueTask<int> FillHandshakeBufferAsync<TIOAdapter>(TIOAdapter adapter, int minSize)
             where TIOAdapter : IReadWriteAdapter
        {
            if (_handshakeBuffer.ActiveLength >= minSize)
            {
                return new ValueTask<int>(minSize);
            }

            int bytesNeeded = minSize - _handshakeBuffer.ActiveLength;
            _handshakeBuffer.EnsureAvailableSpace(bytesNeeded);

            while (_handshakeBuffer.ActiveLength < minSize)
            {
                ValueTask<int> t = adapter.ReadAsync(_handshakeBuffer.AvailableMemory);
                if (!t.IsCompletedSuccessfully)
                {
                    return InternalFillHandshakeBufferAsync(adapter, t, minSize);
                }
                int bytesRead = t.Result;
                if (bytesRead == 0)
                {
                    return new ValueTask<int>(0);
                }

                _handshakeBuffer.Commit(bytesRead);
            }

            return new ValueTask<int>(minSize);

            async ValueTask<int> InternalFillHandshakeBufferAsync(TIOAdapter adap,  ValueTask<int> task, int minSize)
            {
                while (true)
                {
                    int bytesRead = await task.ConfigureAwait(false);
                    if (bytesRead == 0)
                    {
                        throw new IOException(SR.net_io_eof);
                    }

                    _handshakeBuffer.Commit(bytesRead);
                    if (_handshakeBuffer.ActiveLength >= minSize)
                    {
                        return minSize;
                    }

                    task = adap.ReadAsync(_handshakeBuffer.AvailableMemory);
                }
            }
        }

        private async ValueTask FillBufferAsync<TIOAdapter>(TIOAdapter adapter, int numBytesRequired)
            where TIOAdapter : IReadWriteAdapter
        {
            Debug.Assert(_internalBufferCount > 0);
            Debug.Assert(_internalBufferCount < numBytesRequired);

            while (_internalBufferCount < numBytesRequired)
            {
                int bytesRead = await adapter.ReadAsync(_internalBuffer.AsMemory(_internalBufferCount)).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    throw new IOException(SR.net_io_eof);
                }

                _internalBufferCount += bytesRead;
            }
        }

        private async ValueTask WriteAsyncInternal<TIOAdapter>(TIOAdapter writeAdapter, ReadOnlyMemory<byte> buffer)
            where TIOAdapter : struct, IReadWriteAdapter
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
                new ReadOnlySpan<byte>(_internalBuffer, _decryptedBytesOffset, copyBytes).CopyTo(buffer.Span);

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
                byte[]? saved = buffer;
                buffer = new byte[size];
                if (saved != null && copyCount != 0)
                {
                    Buffer.BlockCopy(saved, 0, buffer, 0, copyCount);
                }
            }

            return buffer;
        }

        // We need at least 5 bytes to determine what we have.
        private Framing DetectFraming(ReadOnlySpan<byte> bytes)
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

            if (bytes.Length == 0)
            {
                NetEventSource.Fail(this, "Header buffer is not allocated.");
            }

            // If the first byte is SSL3 HandShake, then check if we have a SSLv3 Type3 client hello.
            if (bytes[0] == (byte)TlsContentType.Handshake || bytes[0] == (byte)TlsContentType.AppData
                || bytes[0] == (byte)TlsContentType.Alert)
            {
                if (bytes.Length < 3)
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

            if (bytes.Length < 3)
            {
                return Framing.Invalid;
            }

            if (bytes[2] > 8)
            {
                return Framing.Invalid;
            }

            if (bytes[2] == 0x1)  // SSL_MT_CLIENT_HELLO
            {
                if (bytes.Length >= 5)
                {
                    version = (bytes[3] << 8) | bytes[4];
                }
            }
            else if (bytes[2] == 0x4) // SSL_MT_SERVER_HELLO
            {
                if (bytes.Length >= 7)
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
            if (!_context!.IsServer || _framing == Framing.Unified)
            {
                return Framing.BeforeSSL3;
            }

            return Framing.Unified; // Will use Ssl2 just for this frame.
        }

        // Returns TLS Frame size.
        private int GetFrameSize(ReadOnlySpan<byte> buffer)
        {
            if (NetEventSource.IsEnabled)
                NetEventSource.Enter(this, buffer.Length);

            int payloadSize = -1;
            switch (_framing)
            {
                case Framing.Unified:
                case Framing.BeforeSSL3:
                    if (buffer.Length < 2)
                    {
                        throw new IOException(SR.net_ssl_io_frame);
                    }
                    // Note: Cannot detect version mismatch for <= SSL2

                    if ((buffer[0] & 0x80) != 0)
                    {
                        // Two bytes
                        payloadSize = (((buffer[0] & 0x7f) << 8) | buffer[1]) + 2;
                    }
                    else
                    {
                        // Three bytes
                        payloadSize = (((buffer[0] & 0x3f) << 8) | buffer[1]) + 3;
                    }

                    break;
                case Framing.SinceSSL3:
                    if (buffer.Length < 5)
                    {
                        throw new IOException(SR.net_ssl_io_frame);
                    }

                    payloadSize = ((buffer[3] << 8) | buffer[4]) + 5;
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
