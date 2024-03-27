// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
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
    public partial class SslStream
    {
        private readonly SslAuthenticationOptions _sslAuthenticationOptions = new SslAuthenticationOptions();
        private int _nestedAuth;
        private bool _isRenego;

        private TlsFrameHelper.TlsFrameInfo _lastFrame;

        private object _handshakeLock => _sslAuthenticationOptions;
        private volatile TaskCompletionSource<bool>? _handshakeWaiter;

        private const int HandshakeTypeOffsetSsl2 = 2;                       // Offset of HelloType in Sslv2 and Unified frames
        private const int HandshakeTypeOffsetTls = 5;                        // Offset of HelloType in Sslv3 and TLS frames

        private const int UnknownTlsFrameLength = int.MaxValue;              // frame too short to determine length

        private bool _receivedEOF;

        // Used by Telemetry to ensure we log connection close exactly once
        // 0 = no handshake
        // 1 = handshake completed, connection opened
        // 2 = SslStream disposed, connection closed
        private int _connectionOpenedStatus;

        private void SetException(Exception e)
        {
            Debug.Assert(e != null, $"Expected non-null Exception to be passed to {nameof(SetException)}");

            _exception ??= ExceptionDispatchInfo.Capture(e);

            CloseContext();
        }

        //
        // This is to not depend on GC&SafeHandle class if the context is not needed anymore.
        //
        private void CloseInternal()
        {
            _exception = s_disposedSentinel;
            CloseContext();

            // Ensure a Read or Auth operation is not in progress,
            // block potential future read and auth operations since SslStream is disposing.
            // This leaves the _nestedRead = 2 and _nestedAuth = 2, but that's ok, since
            // subsequent operations check the _exception sentinel first
            if (Interlocked.Exchange(ref _nestedRead, StreamDisposed) == StreamNotInUse &&
                Interlocked.Exchange(ref _nestedAuth, StreamDisposed) == StreamNotInUse)
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

        private ProtocolToken EncryptData(ReadOnlyMemory<byte> buffer)
        {
            ThrowIfExceptionalOrNotAuthenticated();

            lock (_handshakeLock)
            {
                if (_handshakeWaiter != null)
                {
                    ProtocolToken token = default;
                    // avoid waiting under lock.
                    token.Status = new SecurityStatusPal(SecurityStatusPalErrorCode.TryAgain);
                    return token;
                }

                return Encrypt(buffer);
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
                    ForceAuthenticationAsync<AsyncReadWriteAdapter>(IsServer, null, cancellationToken) :
                    ForceAuthenticationAsync<SyncReadWriteAdapter>(IsServer, null, cancellationToken);
            }
        }

        private async Task ProcessAuthenticationWithTelemetryAsync(bool isAsync, CancellationToken cancellationToken)
        {
            NetSecurityTelemetry.Log.HandshakeStart(IsServer, _sslAuthenticationOptions.TargetHost);
            long startingTimestamp = Stopwatch.GetTimestamp();

            try
            {
                Task task = isAsync ?
                    ForceAuthenticationAsync<AsyncReadWriteAdapter>(IsServer, null, cancellationToken) :
                    ForceAuthenticationAsync<SyncReadWriteAdapter>(IsServer, null, cancellationToken);

                await task.ConfigureAwait(false);

                // SslStream could already have been disposed at this point, in which case _connectionOpenedStatus == 2
                // Make sure that we increment the open connection counter only if it is guaranteed to be decremented in dispose/finalize
                bool connectionOpen = Interlocked.CompareExchange(ref _connectionOpenedStatus, 1, 0) == 0;

                NetSecurityTelemetry.Log.HandshakeCompleted(GetSslProtocolInternal(), startingTimestamp, connectionOpen);
            }
            catch (Exception ex)
            {
                NetSecurityTelemetry.Log.HandshakeFailed(IsServer, startingTimestamp, ex.Message);
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
            if (Interlocked.CompareExchange(ref _nestedAuth, StreamInUse, StreamNotInUse) != StreamNotInUse)
            {
                ObjectDisposedException.ThrowIf(_nestedAuth == StreamDisposed, this);
                throw new InvalidOperationException(SR.Format(SR.net_io_invalidnestedcall, "authenticate"));
            }

            if (Interlocked.CompareExchange(ref _nestedRead, StreamInUse, StreamNotInUse) != StreamNotInUse)
            {
                ObjectDisposedException.ThrowIf(_nestedRead == StreamDisposed, this);
                throw new NotSupportedException(SR.Format(SR.net_io_invalidnestedcall, "read"));
            }

            // Write is different since we do not do anything special in Dispose
            if (Interlocked.Exchange(ref _nestedWrite, StreamInUse) != StreamNotInUse)
            {
                _nestedRead = StreamNotInUse;
                throw new NotSupportedException(SR.Format(SR.net_io_invalidnestedcall, "write"));
            }

            ProtocolToken token = default;
            token.RentBuffer = true;
            try
            {
                if (_buffer.ActiveLength > 0)
                {
                    throw new InvalidOperationException(SR.net_ssl_renegotiate_buffer);
                }

                _sslAuthenticationOptions.RemoteCertRequired = true;
                _isRenego = true;


                token = Renegotiate();

                if (token.Size > 0)
                {
                    await TIOAdapter.WriteAsync(InnerStream, token.AsMemory(), cancellationToken).ConfigureAwait(false);
                    await TIOAdapter.FlushAsync(InnerStream, cancellationToken).ConfigureAwait(false);
                }

                token.ReleasePayload();

                if (token.Status.ErrorCode != SecurityStatusPalErrorCode.OK)
                {
                    if (token.Status.ErrorCode == SecurityStatusPalErrorCode.NoRenegotiation)
                    {
                        // Peer does not want to renegotiate. That should keep session usable.
                        return;
                    }

                    throw SslStreamPal.GetException(token.Status);
                }

                do
                {
                    int frameSize = await ReceiveHandshakeFrameAsync<TIOAdapter>(cancellationToken).ConfigureAwait(false);
                    token = ProcessTlsFrame(frameSize);

                    if (token.Size > 0)
                    {
                        await TIOAdapter.WriteAsync(InnerStream, token.AsMemory(), cancellationToken).ConfigureAwait(false);
                        await TIOAdapter.FlushAsync(InnerStream, cancellationToken).ConfigureAwait(false);
                    }
                    token.ReleasePayload();
                }
                while (token.Status.ErrorCode == SecurityStatusPalErrorCode.ContinueNeeded);

                CompleteHandshake(_sslAuthenticationOptions);
            }
            finally
            {
                if (_buffer.ActiveLength == 0)
                {
                    _buffer.ReturnBuffer();
                }

                token.ReleasePayload();

                _nestedRead = StreamNotInUse;
                _nestedWrite = StreamNotInUse;
                _isRenego = false;
                // We will not release _nestedAuth at this point to prevent another renegotiation attempt.
            }
        }

        // reAuthenticationData is only used on Windows in case of renegotiation.
        private async Task ForceAuthenticationAsync<TIOAdapter>(bool receiveFirst, byte[]? reAuthenticationData, CancellationToken cancellationToken)
            where TIOAdapter : IReadWriteAdapter
        {
            bool handshakeCompleted = false;
            ProtocolToken token = default;

            token.RentBuffer = true;

            if (reAuthenticationData == null)
            {
                // prevent nesting only when authentication functions are called explicitly. e.g. handle renegotiation transparently.
                if (Interlocked.Exchange(ref _nestedAuth, StreamInUse) == StreamInUse)
                {
                    throw new InvalidOperationException(SR.Format(SR.net_io_invalidnestedcall, "authenticate"));
                }
            }
            try
            {
                if (!receiveFirst)
                {
                    token = NextMessage(reAuthenticationData);

                    if (token.Size > 0)
                    {
                        Debug.Assert(token.Payload != null);
                        await TIOAdapter.WriteAsync(InnerStream, new ReadOnlyMemory<byte>(token.Payload!, 0, token.Size), cancellationToken).ConfigureAwait(false);
                        await TIOAdapter.FlushAsync(InnerStream, cancellationToken).ConfigureAwait(false);
                        if (NetEventSource.Log.IsEnabled())
                            NetEventSource.Log.SentFrame(this, token.Payload);
                    }

                    token.ReleasePayload();

                    if (token.Failed)
                    {
                        // tracing done in NextMessage()
                        throw new AuthenticationException(SR.net_auth_SSPI, token.GetException());
                    }
                    else if (token.Status.ErrorCode == SecurityStatusPalErrorCode.OK)
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
                    int frameSize = await ReceiveHandshakeFrameAsync<TIOAdapter>(cancellationToken).ConfigureAwait(false);
                    token = ProcessTlsFrame(frameSize);

                    ReadOnlyMemory<byte> payload = default;
                    if (token.Size > 0)
                    {
                        payload = token.AsMemory();
                    }
                    else if (token.Failed && (_lastFrame.Header.Type == TlsContentType.Handshake || _lastFrame.Header.Type == TlsContentType.ChangeCipherSpec))
                    {
                        // If we failed without OS sending out alert, inject one here to be consistent across platforms.
                        payload = TlsFrameHelper.CreateAlertFrame(_lastFrame.Header.Version, TlsAlertDescription.ProtocolVersion);
                    }

                    if (!payload.IsEmpty)
                    {
                        // If there is message send it out even if call failed. It may contain TLS Alert.
                        await TIOAdapter.WriteAsync(InnerStream, payload, cancellationToken).ConfigureAwait(false);
                        await TIOAdapter.FlushAsync(InnerStream, cancellationToken).ConfigureAwait(false);

                        if (NetEventSource.Log.IsEnabled())
                            NetEventSource.Log.SentFrame(this, payload.Span);
                    }

                    token.ReleasePayload();

                    if (token.Failed)
                    {
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, token.Status);

                        if (_lastFrame.Header.Type == TlsContentType.Alert && _lastFrame.AlertDescription != TlsAlertDescription.CloseNotify &&
                                 token.Status.ErrorCode == SecurityStatusPalErrorCode.IllegalMessage)
                        {
                            // Improve generic message and show details if we failed because of TLS Alert.
                            throw new AuthenticationException(SR.Format(SR.net_auth_tls_alert, _lastFrame.AlertDescription.ToString()), token.GetException());
                        }

                        throw new AuthenticationException(SR.net_auth_SSPI, token.GetException());
                    }
                    else if (token.Status.ErrorCode == SecurityStatusPalErrorCode.OK)
                    {
                        // We can finish renegotiation without doing any read.
                        handshakeCompleted = true;
                    }
                }

                CompleteHandshake(_sslAuthenticationOptions);
            }
            finally
            {
                if (reAuthenticationData == null)
                {
                    _nestedAuth = StreamNotInUse;
                    _isRenego = false;
                }

                token.ReleasePayload();
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

        // This method will make sure we have at least one full TLS frame buffered.
        private async ValueTask<int> ReceiveHandshakeFrameAsync<TIOAdapter>(CancellationToken cancellationToken)
            where TIOAdapter : IReadWriteAdapter
        {
            int frameSize = await EnsureFullTlsFrameAsync<TIOAdapter>(cancellationToken, InitialHandshakeBufferSize).ConfigureAwait(false);

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
#pragma warning disable CS0618
                    if (!_isRenego && _buffer.EncryptedReadOnlySpan[_lastFrame.Header.Version == SslProtocols.Ssl2 ? HandshakeTypeOffsetSsl2 : HandshakeTypeOffsetTls] == (byte)TlsHandshakeType.ClientHello &&
                        _sslAuthenticationOptions!.IsServer) // guard against malicious endpoints. We should not see ClientHello on client.
#pragma warning restore CS0618
                    {
                        TlsFrameHelper.ProcessingOptions options = NetEventSource.Log.IsEnabled() ?
                                                                    TlsFrameHelper.ProcessingOptions.All :
                                                                    TlsFrameHelper.ProcessingOptions.ServerName;
                        if (OperatingSystem.IsMacOS() && _sslAuthenticationOptions.IsServer)
                        {
                            // macOS cannot process ALPN on server at the moment.
                            // We fallback to our own process similar to SNI bellow.
                            options |= TlsFrameHelper.ProcessingOptions.RawApplicationProtocol;
                        }

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
                                _sslAuthenticationOptions.TargetHost = _lastFrame.TargetName;
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

            return frameSize;
        }

        // Calls crypto on received data. No IO inside.
        private ProtocolToken ProcessTlsFrame(int frameSize)
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

                frameSize = nextHeader.Length;

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

            return NextMessage(availableData.Slice(0, chunkSize));
        }

        //
        //  This is to reset auth state on remote side.
        //  If this write succeeds we will allow auth retrying.
        //
        private void SendAuthResetSignal(ReadOnlySpan<byte> alert, ExceptionDispatchInfo exception)
        {
            SetException(exception.SourceException);

            if (alert.Length == 0)
            {
                //
                // We don't have an alert to send so cannot retry and fail prematurely.
                //
                exception.Throw();
            }

            InnerStream.Write(alert);

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
        private bool CompleteHandshake(ref ProtocolToken alertToken, out SslPolicyErrors sslPolicyErrors, out X509ChainStatusFlags chainStatus)
        {
            ProcessHandshakeSuccess();

            if (_nestedAuth != StreamInUse)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, $"Ignoring unsolicited renegotiated certificate.");
                // ignore certificates received outside of handshake or requested renegotiation.
                sslPolicyErrors = SslPolicyErrors.None;
                chainStatus = X509ChainStatusFlags.NoError;
                return true;
            }

#if TARGET_ANDROID
            // On Android, the remote certificate verification can be invoked from Java TrustManager's callback
            // during the handshake process. If that has occurred, we shouldn't run the validation again and
            // return the existing validation result.
            //
            // The Java TrustManager callback is called only when the peer has a certificate. It's possible that
            // the peer didn't provide any certificate (for example when the peer is the client) and the validation
            // result hasn't been set. In that case we still need to run the verification at this point.
            if (TryGetRemoteCertificateValidationResult(out sslPolicyErrors, out chainStatus, ref alertToken, out bool isValid))
            {
                _handshakeCompleted = isValid;
                return isValid;
            }
#endif

            if (!VerifyRemoteCertificate(_sslAuthenticationOptions.CertValidationDelegate, _sslAuthenticationOptions.CertificateContext?.Trust, ref alertToken, out sslPolicyErrors, out chainStatus))
            {
                _handshakeCompleted = false;
                return false;
            }

            _handshakeCompleted = true;
            return true;
        }

        private void CompleteHandshake(SslAuthenticationOptions sslAuthenticationOptions)
        {
            ProtocolToken alertToken = default;
            if (!CompleteHandshake(ref alertToken, out SslPolicyErrors sslPolicyErrors, out X509ChainStatusFlags chainStatus))
            {
                if (sslAuthenticationOptions!.CertValidationDelegate != null)
                {
                    // there may be some chain errors but the decision was made by custom callback. Details should be tracing if enabled.
                    SendAuthResetSignal(new ReadOnlySpan<byte>(alertToken.Payload), ExceptionDispatchInfo.Capture(new AuthenticationException(SR.net_ssl_io_cert_custom_validation, null)));
                }
                else if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors && chainStatus != X509ChainStatusFlags.NoError)
                {
                    // We failed only because of chain and we have some insight.
                    SendAuthResetSignal(new ReadOnlySpan<byte>(alertToken.Payload), ExceptionDispatchInfo.Capture(new AuthenticationException(SR.Format(SR.net_ssl_io_cert_chain_validation, chainStatus), null)));
                }
                else
                {
                    // Simple add sslPolicyErrors as crude info.
                    SendAuthResetSignal(new ReadOnlySpan<byte>(alertToken.Payload), ExceptionDispatchInfo.Capture(new AuthenticationException(SR.Format(SR.net_ssl_io_cert_validation, sslPolicyErrors), null)));
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
            ProtocolToken token;
            while (true)
            {
                token = EncryptData(buffer);
                // TryAgain should be rare, when renegotiation happens exactly when we want to write.
                if (token.Status.ErrorCode != SecurityStatusPalErrorCode.TryAgain)
                {
                    break;
                }

                // We failed to encrypt because renegotiation is pending.
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
                    return WaitAndWriteAsync(buffer, waiterTask, cancellationToken);
                }
            }

            if (token.Status.ErrorCode != SecurityStatusPalErrorCode.OK)
            {
                token.ReleasePayload();
                return ValueTask.FromException(ExceptionDispatchInfo.SetCurrentStackTrace(new IOException(SR.net_io_encrypt, SslStreamPal.GetException(token.Status))));
            }

            ValueTask t = TIOAdapter.WriteAsync(InnerStream, token.AsMemory(), cancellationToken);
            if (t.IsCompletedSuccessfully)
            {
                token.ReleasePayload();
                return t;
            }
            else
            {
                return CompleteWriteAsync(t, token);
            }

            async ValueTask WaitAndWriteAsync(ReadOnlyMemory<byte> buffer, Task waitTask, CancellationToken cancellationToken)
            {
                ProtocolToken token = default;
                try
                {
                    // Wait for renegotiation to finish.
                    await waitTask.ConfigureAwait(false);

                    token = EncryptData(buffer);
                    if (token.Status.ErrorCode == SecurityStatusPalErrorCode.TryAgain)
                    {
                        // Call WriteSingleChunk() recursively to avoid code duplication.
                        // This should be extremely rare in cases when second renegotiation happens concurrently with Write.
                        await WriteSingleChunk<TIOAdapter>(buffer, cancellationToken).ConfigureAwait(false);
                    }
                    else if (token.Status.ErrorCode == SecurityStatusPalErrorCode.OK)
                    {
                        await TIOAdapter.WriteAsync(InnerStream, token.AsMemory(), cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        throw new IOException(SR.net_io_encrypt, SslStreamPal.GetException(token.Status));
                    }
                }
                finally
                {
                    token.ReleasePayload();
                }
            }

            static async ValueTask CompleteWriteAsync(ValueTask writeTask, ProtocolToken token)
            {
                try
                {
                    await writeTask.ConfigureAwait(false);
                }
                finally
                {
                    token.ReleasePayload();
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
            frameSize = GetFrameSize(_buffer.EncryptedReadOnlySpan);
            return _buffer.EncryptedLength >= frameSize;
        }

        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
        private async ValueTask<int> EnsureFullTlsFrameAsync<TIOAdapter>(CancellationToken cancellationToken, int estimatedSize)
            where TIOAdapter : IReadWriteAdapter
        {
            if (HaveFullTlsFrame(out int frameSize))
            {
                return frameSize;
            }

            await TIOAdapter.ReadAsync(InnerStream, Memory<byte>.Empty, cancellationToken).ConfigureAwait(false);

            // If we don't have enough data to determine the frame size, use the provided estimate
            // (e.g. a full TLS frame for reads, and a somewhat shorter frame for handshake / renegotiation).
            // If we do know the frame size, ensure we have space for the whole frame.
            _buffer.EnsureAvailableSpace(frameSize == UnknownTlsFrameLength ?
                estimatedSize :
                frameSize - _buffer.EncryptedLength);

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
                status = Decrypt(_buffer.EncryptedSpanSliced(frameSize), out int decryptedOffset, out int decryptedCount);
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

                    if (_sslAuthenticationOptions.AllowRenegotiation || SslProtocol == SslProtocols.Tls13 || _nestedAuth != 0)
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

        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
        private async ValueTask<int> ReadAsyncInternal<TIOAdapter>(Memory<byte> buffer, CancellationToken cancellationToken)
            where TIOAdapter : IReadWriteAdapter
        {

            // Throw first if we already have exception.
            // Check for disposal is not atomic so we will check again below.
            ThrowIfExceptionalOrNotAuthenticated();

            if (Interlocked.CompareExchange(ref _nestedRead, StreamInUse, StreamNotInUse) != StreamNotInUse)
            {
                ObjectDisposedException.ThrowIf(_nestedRead == StreamDisposed, this);
                throw new NotSupportedException(SR.Format(SR.net_io_invalidnestedcall, "read"));
            }

            try
            {
                int processedLength = 0;
                int nextTlsFrameLength = UnknownTlsFrameLength;

                if (_buffer.DecryptedLength != 0)
                {
                    processedLength = CopyDecryptedData(buffer);
                    if (processedLength == buffer.Length || !HaveFullTlsFrame(out nextTlsFrameLength))
                    {
                        // We either filled whole buffer or used all buffered frames.
                        return processedLength;
                    }

                    buffer = buffer.Slice(processedLength);
                }

                if (_receivedEOF && nextTlsFrameLength == UnknownTlsFrameLength)
                {
                    // there should be no frames waiting for processing
                    Debug.Assert(_buffer.EncryptedLength == 0);
                    // We received EOF during previous read but had buffered data to return.
                    return 0;
                }

                Debug.Assert(_buffer.DecryptedLength == 0);

                while (true)
                {
                    int payloadBytes = await EnsureFullTlsFrameAsync<TIOAdapter>(cancellationToken, ReadBufferSize).ConfigureAwait(false);
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
                        }
                        else if (status.ErrorCode == SecurityStatusPalErrorCode.ContextExpired)
                        {
                            _receivedEOF = true;
                            break;
                        }
                        else
                        {
                            throw new IOException(SR.net_io_decrypt, SslStreamPal.GetException(status));
                        }
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
                _nestedRead = StreamNotInUse;
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

            if (Interlocked.Exchange(ref _nestedWrite, StreamInUse) == StreamInUse)
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
                _nestedWrite = StreamNotInUse;
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
            if (buffer.Length < TlsFrameHelper.HeaderSize)
            {
                return UnknownTlsFrameLength;
            }

            if (!TlsFrameHelper.TryGetFrameHeader(buffer, ref _lastFrame.Header))
            {
                throw new IOException(SR.net_ssl_io_frame);
            }

            if (_lastFrame.Header.Length < 0)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(this, "invalid TLS frame size");
                throw new AuthenticationException(SR.net_frame_read_size);
            }

            return _lastFrame.Header.Length;
        }
    }
}
