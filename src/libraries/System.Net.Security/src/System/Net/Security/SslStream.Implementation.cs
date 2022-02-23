// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;

namespace System.Net.Security
{
    public partial class SslStream
    {
        private SslAuthenticationOptions? _sslAuthenticationOptions;

        private int _nestedAuth;
        private bool _isRenego;

        private TlsFrameHelper.TlsFrameInfo _lastFrame;

        private object _handshakeLock => _sslAuthenticationOptions!;
        private volatile TaskCompletionSource<bool>? _handshakeWaiter;

        private bool _receivedEOF;

        // Used by Telemetry to ensure we log connection close exactly once
        // 0 = no handshake
        // 1 = handshake completed, connection opened
        // 2 = SslStream disposed, connection closed
        private int _connectionOpenedStatus;

        private void ValidateCreateContext(SslClientAuthenticationOptions sslClientAuthenticationOptions, RemoteCertificateValidationCallback? remoteCallback, LocalCertSelectionCallback? localCallback)
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

            ArgumentNullException.ThrowIfNull(sslClientAuthenticationOptions.TargetHost, nameof(sslClientAuthenticationOptions.TargetHost));

            _exception = null;
            try
            {
                _sslAuthenticationOptions = new SslAuthenticationOptions(sslClientAuthenticationOptions, remoteCallback, localCallback);
                _context = new SecureChannel(_sslAuthenticationOptions, this);
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
                _context = new SecureChannel(_sslAuthenticationOptions, this);
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
                _buffer.ReturnBuffer();
            }

            if (!_buffer.IsValid)
            {
                // Suppress finalizer since the read buffer was returned.
                GC.SuppressFinalize(this);
            }

            if (NetSecurityTelemetry.Log.IsEnabled())
            {
                // Set the status to disposed. If it was opened before, log ConnectionClosed
                if (Interlocked.Exchange(ref _connectionOpenedStatus, 2) == 1)
                {
                    NetSecurityTelemetry.Log.ConnectionClosed(GetSslProtocolInternal());
                }
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

        //
        // This method assumes that a SSPI context is already in a good shape.
        // For example it is either a fresh context or already authenticated context that needs renegotiation.
        //
        private Task ProcessAuthenticationAsync(bool isAsync = false, CancellationToken cancellationToken = default)
        {
            ThrowIfExceptional();

            if (NetSecurityTelemetry.Log.IsEnabled())
            {
                return ProcessAuthenticationWithTelemetryAsync(isAsync, cancellationToken);
            }
            else
            {
                return isAsync ?
                    ForceAuthenticationAsync<AsyncReadWriteAdapter>(_context!.IsServer, null, cancellationToken) :
                    ForceAuthenticationAsync<SyncReadWriteAdapter>(_context!.IsServer, null, cancellationToken);
            }
        }

        private async Task ProcessAuthenticationWithTelemetryAsync(bool isAsync, CancellationToken cancellationToken)
        {
            NetSecurityTelemetry.Log.HandshakeStart(_context!.IsServer, _sslAuthenticationOptions!.TargetHost);
            ValueStopwatch stopwatch = ValueStopwatch.StartNew();

            try
            {
                Task task = isAsync?
                    ForceAuthenticationAsync<AsyncReadWriteAdapter>(_context!.IsServer, null, cancellationToken) :
                    ForceAuthenticationAsync<SyncReadWriteAdapter>(_context!.IsServer, null, cancellationToken);

                await task.ConfigureAwait(false);

                // SslStream could already have been disposed at this point, in which case _connectionOpenedStatus == 2
                // Make sure that we increment the open connection counter only if it is guaranteed to be decremented in dispose/finalize
                bool connectionOpen = Interlocked.CompareExchange(ref _connectionOpenedStatus, 1, 0) == 0;

                NetSecurityTelemetry.Log.HandshakeCompleted(GetSslProtocolInternal(), stopwatch, connectionOpen);
            }
            catch (Exception ex)
            {
                NetSecurityTelemetry.Log.HandshakeFailed(_context.IsServer, stopwatch, ex.Message);
                throw;
            }
        }

        //
        // This is used to reply on re-handshake when received SEC_I_RENEGOTIATE on Read().
        //
        private async Task ReplyOnReAuthenticationAsync<TIOAdapter>(byte[]? buffer, CancellationToken cancellationToken)
            where TIOAdapter : IReadWriteAdapter
        {
            try
            {
                await ForceAuthenticationAsync<TIOAdapter>(receiveFirst: false, buffer, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _handshakeWaiter!.SetResult(true);
                _handshakeWaiter = null;
            }
        }

        // This will initiate renegotiation or PHA for Tls1.3
        private async Task RenegotiateAsync<TIOAdapter>(CancellationToken cancellationToken)
            where TIOAdapter : IReadWriteAdapter
        {
            if (Interlocked.Exchange(ref _nestedAuth, 1) == 1)
            {
                throw new InvalidOperationException(SR.Format(SR.net_io_invalidnestedcall, "authenticate"));
            }

            if (Interlocked.Exchange(ref _nestedRead, 1) == 1)
            {
                throw new NotSupportedException(SR.Format(SR.net_io_invalidnestedcall, "read"));
            }

            if (Interlocked.Exchange(ref _nestedWrite, 1) == 1)
            {
                _nestedRead = 0;
                throw new NotSupportedException(SR.Format(SR.net_io_invalidnestedcall, "write"));
            }

            try
            {
                if (_buffer.ActiveLength > 0)
                {
                    throw new InvalidOperationException(SR.net_ssl_renegotiate_buffer);
                }

                _sslAuthenticationOptions!.RemoteCertRequired = true;
                _isRenego = true;


                SecurityStatusPal status = _context!.Renegotiate(out byte[]? nextmsg);

                if (nextmsg is { Length: > 0 })
                {
                    await TIOAdapter.WriteAsync(InnerStream, nextmsg, 0, nextmsg.Length, cancellationToken).ConfigureAwait(false);
                    await TIOAdapter.FlushAsync(InnerStream, cancellationToken).ConfigureAwait(false);
                }

                if (status.ErrorCode != SecurityStatusPalErrorCode.OK)
                {
                    if (status.ErrorCode == SecurityStatusPalErrorCode.NoRenegotiation)
                    {
                        // Peer does not want to renegotiate. That should keep session usable.
                        return;
                    }

                    throw SslStreamPal.GetException(status);
                }

                _buffer.EnsureAvailableSpace(InitialHandshakeBufferSize);

                ProtocolToken message;
                do
                {
                    message = await ReceiveBlobAsync<TIOAdapter>(cancellationToken).ConfigureAwait(false);
                    if (message.Size > 0)
                    {
                        await TIOAdapter.WriteAsync(InnerStream, message.Payload!, 0, message.Size, cancellationToken).ConfigureAwait(false);
                        await TIOAdapter.FlushAsync(InnerStream, cancellationToken).ConfigureAwait(false);
                    }
                }
                while (message.Status.ErrorCode == SecurityStatusPalErrorCode.ContinueNeeded);

                CompleteHandshake(_sslAuthenticationOptions!);
            }
            finally
            {
                if (_buffer.ActiveLength == 0)
                {
                    _buffer.ReturnBuffer();
                }

                _nestedRead = 0;
                _nestedWrite = 0;
                _isRenego = false;
                // We will not release _nestedAuth at this point to prevent another renegotiation attempt.
            }
        }

        // reAuthenticationData is only used on Windows in case of renegotiation.
        private async Task ForceAuthenticationAsync<TIOAdapter>(bool receiveFirst, byte[]? reAuthenticationData, CancellationToken cancellationToken)
            where TIOAdapter : IReadWriteAdapter
        {
            ProtocolToken message;
            bool handshakeCompleted = false;

            if (reAuthenticationData == null)
            {
                // prevent nesting only when authentication functions are called explicitly. e.g. handle renegotiation transparently.
                if (Interlocked.Exchange(ref _nestedAuth, 1) == 1)
                {
                    throw new InvalidOperationException(SR.Format(SR.net_io_invalidnestedcall, "authenticate"));
                }
            }

            try
            {
                if (!receiveFirst)
                {
                    message = _context!.NextMessage(reAuthenticationData);
                    if (message.Size > 0)
                    {
                        await TIOAdapter.WriteAsync(InnerStream, message.Payload!, 0, message.Size, cancellationToken).ConfigureAwait(false);
                        await TIOAdapter.FlushAsync(InnerStream, cancellationToken).ConfigureAwait(false);
                        if (NetEventSource.Log.IsEnabled())
                            NetEventSource.Log.SentFrame(this, message.Payload);
                    }

                    if (message.Failed)
                    {
                        // tracing done in NextMessage()
                        throw new AuthenticationException(SR.net_auth_SSPI, message.GetException());
                    }
                    else if (message.Status.ErrorCode == SecurityStatusPalErrorCode.OK)
                    {
                        // We can finish renegotiation without doing any read.
                        handshakeCompleted = true;
                    }
                }

                if (!handshakeCompleted)
                {
                    _buffer.EnsureAvailableSpace(InitialHandshakeBufferSize);
                }

                while (!handshakeCompleted)
                {
                    message = await ReceiveBlobAsync<TIOAdapter>(cancellationToken).ConfigureAwait(false);

                    byte[]? payload = null;
                    int size = 0;
                    if (message.Size > 0)
                    {
                        payload = message.Payload;
                        size = message.Size;
                    }
                    else if (message.Failed && (_lastFrame.Header.Type == TlsContentType.Handshake || _lastFrame.Header.Type == TlsContentType.ChangeCipherSpec))
                    {
                        // If we failed without OS sending out alert, inject one here to be consistent across platforms.
                        payload = TlsFrameHelper.CreateAlertFrame(_lastFrame.Header.Version, TlsAlertDescription.ProtocolVersion);
                        size = payload.Length;
                    }

                    if (payload != null && size > 0)
                    {
                        // If there is message send it out even if call failed. It may contain TLS Alert.
                        await TIOAdapter.WriteAsync(InnerStream, payload!, 0, size, cancellationToken).ConfigureAwait(false);
                        await TIOAdapter.FlushAsync(InnerStream, cancellationToken).ConfigureAwait(false);

                        if (NetEventSource.Log.IsEnabled())
                            NetEventSource.Log.SentFrame(this, payload);
                    }

                    if (message.Failed)
                    {
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, message.Status);

                        if (_lastFrame.Header.Type == TlsContentType.Alert && _lastFrame.AlertDescription != TlsAlertDescription.CloseNotify &&
                                 message.Status.ErrorCode == SecurityStatusPalErrorCode.IllegalMessage)
                        {
                            // Improve generic message and show details if we failed because of TLS Alert.
                            throw new AuthenticationException(SR.Format(SR.net_auth_tls_alert, _lastFrame.AlertDescription.ToString()), message.GetException());
                        }

                        throw new AuthenticationException(SR.net_auth_SSPI, message.GetException());
                    }
                    else if (message.Status.ErrorCode == SecurityStatusPalErrorCode.OK)
                    {
                        // We can finish renegotiation without doing any read.
                        handshakeCompleted = true;
                    }
                }

                CompleteHandshake(_sslAuthenticationOptions!);
            }
            finally
            {
                if (reAuthenticationData == null)
                {
                    _nestedAuth = 0;
                    _isRenego = false;
                }
            }

            if (NetEventSource.Log.IsEnabled())
                NetEventSource.Log.SspiSelectedCipherSuite(nameof(ForceAuthenticationAsync),
                                                                    SslProtocol,
                                                                    CipherAlgorithm,
                                                                    CipherStrength,
                                                                    HashAlgorithm,
                                                                    HashStrength,
                                                                    KeyExchangeAlgorithm,
                                                                    KeyExchangeStrength);

        }

        private async ValueTask<ProtocolToken> ReceiveBlobAsync<TIOAdapter>(CancellationToken cancellationToken)
            where TIOAdapter : IReadWriteAdapter
        {
            int frameSize = await EnsureFullTlsFrameAsync<TIOAdapter>(cancellationToken).ConfigureAwait(false);

            if (frameSize == 0)
            {
                // We expect to receive at least one frame
                throw new IOException(SR.net_io_eof);
            }

            // At this point, we have at least one TLS frame.
            switch (_lastFrame.Header.Type)
            {
                case TlsContentType.Alert:
                    if (TlsFrameHelper.TryGetFrameInfo(_buffer.EncryptedReadOnlySpan, ref _lastFrame))
                    {
                        if (NetEventSource.Log.IsEnabled() && _lastFrame.AlertDescription != TlsAlertDescription.CloseNotify) NetEventSource.Error(this, $"Received TLS alert {_lastFrame.AlertDescription}");
                    }
                    break;
                case TlsContentType.Handshake:
                    if (!_isRenego && _buffer.EncryptedReadOnlySpan[TlsFrameHelper.HeaderSize] == (byte)TlsHandshakeType.ClientHello &&
                        _sslAuthenticationOptions!.IsServer) // guard against malicious endpoints. We should not see ClientHello on client.
                     {
                        TlsFrameHelper.ProcessingOptions options = NetEventSource.Log.IsEnabled() ?
                                                                    TlsFrameHelper.ProcessingOptions.All :
                                                                    TlsFrameHelper.ProcessingOptions.ServerName;

                        // Process SNI from Client Hello message
                        if (!TlsFrameHelper.TryGetFrameInfo(_buffer.EncryptedReadOnlySpan, ref _lastFrame, options))
                        {
                            if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, $"Failed to parse TLS hello.");
                        }

                        if (_lastFrame.HandshakeType == TlsHandshakeType.ClientHello)
                        {
                            // SNI if it exist. Even if we could not parse the hello, we can fall-back to default certificate.
                            if (_lastFrame.TargetName != null)
                            {
                                _sslAuthenticationOptions!.TargetHost = _lastFrame.TargetName;
                            }

                            if (_sslAuthenticationOptions.ServerOptionDelegate != null)
                            {
                                SslServerAuthenticationOptions userOptions =
                                    await _sslAuthenticationOptions.ServerOptionDelegate(this, new SslClientHelloInfo(_sslAuthenticationOptions.TargetHost, _lastFrame.SupportedVersions),
                                        _sslAuthenticationOptions.UserState, cancellationToken).ConfigureAwait(false);
                                _sslAuthenticationOptions.UpdateOptions(userOptions);
                            }
                        }

                        if (NetEventSource.Log.IsEnabled())
                        {
                            NetEventSource.Log.ReceivedFrame(this, _lastFrame);
                        }
                    }
                    break;
                case TlsContentType.AppData:
                    // TLS1.3 it is not possible to distinguish between late Handshake and Application Data
                    if (_isRenego && SslProtocol != SslProtocols.Tls13)
                    {
                        throw new InvalidOperationException(SR.net_ssl_renegotiate_data);
                    }
                    break;

            }

            return ProcessBlob(frameSize);
        }

        // Calls crypto on received data. No IO inside.
        private ProtocolToken ProcessBlob(int frameSize)
        {
            int chunkSize = frameSize;

            ReadOnlySpan<byte> availableData = _buffer.EncryptedReadOnlySpan;
            // DiscardEncrypted() does not touch data, it just increases start index so next
            // EncryptedSpan will exclude the "discarded" data.
            _buffer.DiscardEncrypted(frameSize);

            // Often more TLS messages fit into same packet. Get as many complete frames as we can.
            while (_buffer.EncryptedLength > TlsFrameHelper.HeaderSize)
            {
                TlsFrameHeader nextHeader = default;

                if (!TlsFrameHelper.TryGetFrameHeader(_buffer.EncryptedReadOnlySpan, ref nextHeader))
                {
                    break;
                }

                frameSize = nextHeader.Length + TlsFrameHelper.HeaderSize;

                // Can process more handshake frames in single step or during TLS1.3 post-handshake auth, but we should
                // avoid processing too much so as to preserve API boundary between handshake and I/O.
                if ((nextHeader.Type != TlsContentType.Handshake && nextHeader.Type != TlsContentType.ChangeCipherSpec) && !_isRenego || frameSize > _buffer.EncryptedLength)
                {
                    // We don't have full frame left or we already have app data which needs to be processed by decrypt.
                    break;
                }

                chunkSize += frameSize;
                _buffer.DiscardEncrypted(frameSize);
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
        private bool CompleteHandshake(ref ProtocolToken? alertToken, out SslPolicyErrors sslPolicyErrors, out X509ChainStatusFlags chainStatus)
        {
            _context!.ProcessHandshakeSuccess();

            if (_nestedAuth != 1)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, $"Ignoring unsolicited renegotiated certificate.");
                // ignore certificates received outside of handshake or requested renegotiation.
                sslPolicyErrors = SslPolicyErrors.None;
                chainStatus = X509ChainStatusFlags.NoError;
                return true;
            }

            if (!_context.VerifyRemoteCertificate(_sslAuthenticationOptions!.CertValidationDelegate, _sslAuthenticationOptions!.CertificateContext?.Trust, ref alertToken, out sslPolicyErrors, out chainStatus))
            {
                _handshakeCompleted = false;
                return false;
            }

            _handshakeCompleted = true;
            return true;
        }

        private void CompleteHandshake(SslAuthenticationOptions sslAuthenticationOptions)
        {
            ProtocolToken? alertToken = null;
            if (!CompleteHandshake(ref alertToken, out SslPolicyErrors sslPolicyErrors, out X509ChainStatusFlags chainStatus))
            {
                if (sslAuthenticationOptions!.CertValidationDelegate != null)
                {
                    // there may be some chain errors but the decision was made by custom callback. Details should be tracing if enabled.
                    SendAuthResetSignal(alertToken, ExceptionDispatchInfo.Capture(new AuthenticationException(SR.net_ssl_io_cert_custom_validation, null)));
                }
                else if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors && chainStatus != X509ChainStatusFlags.NoError)
                {
                    // We failed only because of chain and we have some insight.
                    SendAuthResetSignal(alertToken, ExceptionDispatchInfo.Capture(new AuthenticationException(SR.Format(SR.net_ssl_io_cert_chain_validation, chainStatus), null)));
                }
                else
                {
                    // Simple add sslPolicyErrors as crude info.
                    SendAuthResetSignal(alertToken, ExceptionDispatchInfo.Capture(new AuthenticationException(SR.Format(SR.net_ssl_io_cert_validation, sslPolicyErrors), null)));
                }
            }
        }

        private async ValueTask WriteAsyncChunked<TIOAdapter>(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
            where TIOAdapter : IReadWriteAdapter
        {
            do
            {
                int chunkBytes = Math.Min(buffer.Length, MaxDataSize);
                await WriteSingleChunk<TIOAdapter>(buffer.Slice(0, chunkBytes), cancellationToken).ConfigureAwait(false);
                buffer = buffer.Slice(chunkBytes);
            } while (buffer.Length != 0);
        }

        private ValueTask WriteSingleChunk<TIOAdapter>(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
            where TIOAdapter : IReadWriteAdapter
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
                    Task waiterTask = TIOAdapter.WaitAsync(waiter);
                    // We finished synchronously waiting for renegotiation. We can try again immediately.
                    if (waiterTask.IsCompletedSuccessfully)
                    {
                        continue;
                    }

                    // We need to wait asynchronously as well as for the write when EncryptData is finished.
                    return WaitAndWriteAsync(buffer, waiterTask, rentedBuffer, cancellationToken);
                }
            }

            if (status.ErrorCode != SecurityStatusPalErrorCode.OK)
            {
                ArrayPool<byte>.Shared.Return(rentedBuffer);
                return ValueTask.FromException(ExceptionDispatchInfo.SetCurrentStackTrace(new IOException(SR.net_io_encrypt, SslStreamPal.GetException(status))));
            }

            ValueTask t = TIOAdapter.WriteAsync(InnerStream, outBuffer, 0, encryptedBytes, cancellationToken);
            if (t.IsCompletedSuccessfully)
            {
                ArrayPool<byte>.Shared.Return(rentedBuffer);
                return t;
            }
            else
            {
                return CompleteWriteAsync(t, rentedBuffer);
            }

            async ValueTask WaitAndWriteAsync(ReadOnlyMemory<byte> buffer, Task waitTask, byte[] rentedBuffer, CancellationToken cancellationToken)
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
                        byte[] tmp = bufferToReturn;
                        bufferToReturn = null;
                        ArrayPool<byte>.Shared.Return(tmp);

                        // Call WriteSingleChunk() recursively to avoid code duplication.
                        // This should be extremely rare in cases when second renegotiation happens concurrently with Write.
                        await WriteSingleChunk<TIOAdapter>(buffer, cancellationToken).ConfigureAwait(false);
                    }
                    else if (status.ErrorCode == SecurityStatusPalErrorCode.OK)
                    {
                        await TIOAdapter.WriteAsync(InnerStream, outBuffer, 0, encryptedBytes, cancellationToken).ConfigureAwait(false);
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

            static async ValueTask CompleteWriteAsync(ValueTask writeTask, byte[] bufferToReturn)
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

        ~SslStream()
        {
            Dispose(disposing: false);
        }

        private void ReturnReadBufferIfEmpty()
        {
            if (_buffer.ActiveLength == 0)
            {
                _buffer.ReturnBuffer();
            }
        }

        private bool HaveFullTlsFrame(out int frameSize)
        {
            if (_buffer.EncryptedLength < TlsFrameHelper.HeaderSize)
            {
                frameSize = int.MaxValue;
                return false;
            }

            frameSize = GetFrameSize(_buffer.EncryptedReadOnlySpan);
            return _buffer.EncryptedLength >= frameSize;
        }


        private async ValueTask<int> EnsureFullTlsFrameAsync<TIOAdapter>(CancellationToken cancellationToken)
            where TIOAdapter : IReadWriteAdapter
        {
            int frameSize;
            if (HaveFullTlsFrame(out frameSize))
            {
                return frameSize;
            }

            while (_buffer.EncryptedLength < frameSize)
            {
                // there should be space left to read into
                Debug.Assert(_buffer.AvailableLength > 0, "_buffer.AvailableBytes > 0");

                // We either don't have full frame or we don't have enough data to even determine the size.
                int bytesRead = await TIOAdapter.ReadAsync(InnerStream, _buffer.AvailableMemory, cancellationToken).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    if (_buffer.EncryptedLength != 0)
                    {
                        // we got EOF in middle of TLS frame. Treat that as error.
                        throw new IOException(SR.net_io_eof);
                    }

                    return 0;
                }

                _buffer.Commit(bytesRead);
                if (frameSize == int.MaxValue && _buffer.EncryptedLength > TlsFrameHelper.HeaderSize)
                {
                    // recalculate frame size if needed e.g. we could not get it before.
                    frameSize = GetFrameSize(_buffer.EncryptedReadOnlySpan);
                    _buffer.EnsureAvailableSpace(frameSize - _buffer.EncryptedLength);
                }
            }

            return frameSize;
        }

        private SecurityStatusPal DecryptData(int frameSize)
        {
            SecurityStatusPal status;

            lock (_handshakeLock)
            {
                ThrowIfExceptionalOrNotAuthenticated();

                // Decrypt will decrypt in-place and modify these to point to the actual decrypted data, which may be smaller.
                status = _context!.Decrypt(_buffer.EncryptedSpanSliced(frameSize), out int decryptedOffset, out int decryptedCount);
                _buffer.OnDecrypted(decryptedOffset, decryptedCount, frameSize);

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

                    if (_sslAuthenticationOptions!.AllowRenegotiation || SslProtocol == SslProtocols.Tls13 || _nestedAuth != 0)
                    {
                        // create TCS only if we plan to proceed. If not, we will throw later outside of the lock.
                        // Tls1.3 does not have renegotiation. However on Windows this error code is used
                        // for session management e.g. anything lsass needs to see.
                        // We also allow it when explicitly requested using RenegotiateAsync().
                        _handshakeWaiter = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    }
                }
            }

            return status;
        }

        private async ValueTask<int> ReadAsyncInternal<TIOAdapter>(Memory<byte> buffer, CancellationToken cancellationToken)
            where TIOAdapter : IReadWriteAdapter
        {
            if (Interlocked.Exchange(ref _nestedRead, 1) == 1)
            {
                throw new NotSupportedException(SR.Format(SR.net_io_invalidnestedcall, "read"));
            }

            ThrowIfExceptionalOrNotAuthenticated();

            try
            {
                int processedLength = 0;

                if (_buffer.DecryptedLength != 0)
                {
                    processedLength = CopyDecryptedData(buffer);
                    if (processedLength == buffer.Length || !HaveFullTlsFrame(out _))
                    {
                        // We either filled whole buffer or used all buffered frames.
                        return processedLength;
                    }

                    buffer = buffer.Slice(processedLength);
                }

                if (_receivedEOF)
                {
                    Debug.Assert(_buffer.EncryptedLength == 0);
                    // We received EOF during previous read but had buffered data to return.
                    return 0;
                }

                if (buffer.Length == 0 && _buffer.ActiveLength == 0)
                {
                    // User requested a zero-byte read, and we have no data available in the buffer for processing.
                    // This zero-byte read indicates their desire to trade off the extra cost of a zero-byte read
                    // for reduced memory consumption when data is not immediately available.
                    // So, we will issue our own zero-byte read against the underlying stream and defer buffer allocation
                    // until data is actually available from the underlying stream.
                    // Note that if the underlying stream does not supporting blocking on zero byte reads, then this will
                    // complete immediately and won't save any memory, but will still function correctly.
                    await TIOAdapter.ReadAsync(InnerStream, Memory<byte>.Empty, cancellationToken).ConfigureAwait(false);
                }

                Debug.Assert(_buffer.DecryptedLength == 0);

                _buffer.EnsureAvailableSpace(ReadBufferSize - _buffer.ActiveLength);

                while (true)
                {
                    int payloadBytes = await EnsureFullTlsFrameAsync<TIOAdapter>(cancellationToken).ConfigureAwait(false);
                    if (payloadBytes == 0)
                    {
                        _receivedEOF = true;
                        break;
                    }

                    SecurityStatusPal status = DecryptData(payloadBytes);
                    if (status.ErrorCode != SecurityStatusPalErrorCode.OK)
                    {
                        byte[]? extraBuffer = null;
                        if (_buffer.DecryptedLength != 0)
                        {
                            extraBuffer = new byte[_buffer.DecryptedLength];
                            _buffer.DecryptedSpan.CopyTo(extraBuffer);

                            _buffer.Discard(_buffer.DecryptedLength);
                        }

                        if (NetEventSource.Log.IsEnabled())
                            NetEventSource.Info(null, $"***Processing an error Status = {status}");

                        if (status.ErrorCode == SecurityStatusPalErrorCode.Renegotiate)
                        {
                            // We determined above that we will not process it.
                            if (_handshakeWaiter == null)
                            {
                                throw new IOException(SR.net_ssl_io_renego);
                            }
                            await ReplyOnReAuthenticationAsync<TIOAdapter>(extraBuffer, cancellationToken).ConfigureAwait(false);
                            // Loop on read.
                            continue;
                        }

                        if (status.ErrorCode == SecurityStatusPalErrorCode.ContextExpired)
                        {
                            _receivedEOF = true;
                            break;
                        }

                        throw new IOException(SR.net_io_decrypt, SslStreamPal.GetException(status));
                    }

                    if (_buffer.DecryptedLength > 0)
                    {
                        // This will either copy data from rented buffer or adjust final buffer as needed.
                        // In both cases _decryptedBytesOffset and _decryptedBytesCount will be updated as needed.
                        int copyLength = CopyDecryptedData(buffer);
                        processedLength += copyLength;
                        if (copyLength == buffer.Length)
                        {
                            // We have more decrypted data after we filled provided buffer.
                            break;
                        }

                        buffer = buffer.Slice(copyLength);
                    }

                    if (processedLength == 0)
                    {
                        // We did not get any real data so far.
                        continue;
                    }

                    if (!HaveFullTlsFrame(out payloadBytes))
                    {
                        // We don't have another frame to process but we have some data to return to caller.
                        break;
                    }

                    TlsFrameHelper.TryGetFrameHeader(_buffer.EncryptedReadOnlySpan, ref _lastFrame.Header);
                    if (_lastFrame.Header.Type != TlsContentType.AppData)
                    {
                        // Alerts, handshake and anything else will be processed separately.
                        // This may not be necessary but it improves compatibility with older versions.
                        break;
                    }
                }

                return processedLength;
            }
            catch (Exception e)
            {
                if (e is IOException || (e is OperationCanceledException && cancellationToken.IsCancellationRequested))
                {
                    throw;
                }

                throw new IOException(SR.net_io_read, e);
            }
            finally
            {
                ReturnReadBufferIfEmpty();
                _nestedRead = 0;
            }
        }

        private async ValueTask WriteAsyncInternal<TIOAdapter>(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
            where TIOAdapter : IReadWriteAdapter
        {
            ThrowIfExceptionalOrNotAuthenticatedOrShutdown();

            if (buffer.Length == 0 && !SslStreamPal.CanEncryptEmptyMessage)
            {
                // If it's an empty message and the PAL doesn't support that, we're done.
                return;
            }

            if (Interlocked.Exchange(ref _nestedWrite, 1) == 1)
            {
                throw new NotSupportedException(SR.Format(SR.net_io_invalidnestedcall, "write"));
            }

            try
            {
                ValueTask t = buffer.Length < MaxDataSize ?
                    WriteSingleChunk<TIOAdapter>(buffer, cancellationToken) :
                    WriteAsyncChunked<TIOAdapter>(buffer, cancellationToken);
                await t.ConfigureAwait(false);
            }
            catch (Exception e)
            {
                if (e is IOException || (e is OperationCanceledException && cancellationToken.IsCancellationRequested))
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

        private int CopyDecryptedData(Memory<byte> buffer)
        {
            Debug.Assert(_buffer.DecryptedLength > 0);

            int copyBytes = Math.Min(_buffer.DecryptedLength, buffer.Length);
            if (copyBytes != 0)
            {
                _buffer.DecryptedReadOnlySpanSliced(copyBytes).CopyTo(buffer.Span);
                _buffer.Discard(copyBytes);
            }

            return copyBytes;
        }

        // Returns TLS Frame size including header size.
        private int GetFrameSize(ReadOnlySpan<byte> buffer)
        {
            if (!TlsFrameHelper.TryGetFrameHeader(buffer, ref _lastFrame.Header))
            {
                throw new IOException(SR.net_ssl_io_frame);
            }

            if (_lastFrame.Header.Length < 0)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, "invalid TLS frame size");
                throw new AuthenticationException(SR.net_frame_read_size);
            }

            return _lastFrame.Header.Length + TlsFrameHelper.HeaderSize;
        }
    }
}
