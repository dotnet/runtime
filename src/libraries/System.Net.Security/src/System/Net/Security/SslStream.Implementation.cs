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
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Security
{
    public partial class SslStream
    {
        private static int s_uniqueNameInteger = 123;
        private static AsyncProtocolCallback s_partialFrameCallback = new AsyncProtocolCallback(PartialFrameCallback);
        private static AsyncProtocolCallback s_readFrameCallback = new AsyncProtocolCallback(ReadFrameCallback);
        private static AsyncCallback s_writeCallback = new AsyncCallback(WriteCallback);

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

        // This block is used by re-handshake code to buffer data decrypted with the old key.
        private byte[] _queuedReadData;
        private int _queuedReadCount;
        private bool _pendingReHandshake;
        private const int MaxQueuedReadBytes = 1024 * 128;

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
        private object _queuedWriteStateRequest;

        private int _lockReadState;
        private object _queuedReadStateRequest;

        /// <summary>Set as the _exception when the instance is disposed.</summary>
        private static readonly ExceptionDispatchInfo s_disposedSentinel = ExceptionDispatchInfo.Capture(new ObjectDisposedException(nameof(SslStream)));

        private void ThrowIfExceptional()
        {
            ExceptionDispatchInfo e = _exception;
            if (e != null)
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

        private void CheckThrow(bool authSuccessCheck, bool shutdownCheck = false)
        {
            ThrowIfExceptional();

            if (authSuccessCheck && !IsAuthenticated)
            {
                throw new InvalidOperationException(SR.net_auth_noauth);
            }

            if (shutdownCheck && _shutdown)
            {
                throw new InvalidOperationException(SR.net_ssl_io_already_shutdown);
            }
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
            CheckThrow(true);
            return _context.Encrypt(buffer, ref outBuffer, out outSize);
        }

        private SecurityStatusPal DecryptData()
        {
            CheckThrow(true);
            return PrivateDecryptData(_internalBuffer, ref _decryptedBytesOffset, ref _decryptedBytesCount);
        }

        private SecurityStatusPal PrivateDecryptData(byte[] buffer, ref int offset, ref int count)
        {
            return _context.Decrypt(buffer, ref offset, ref count);
        }

        //
        //  Called by re-handshake if found data decrypted with the old key
        //
        private Exception EnqueueOldKeyDecryptedData(byte[] buffer, int offset, int count)
        {
            lock (SyncLock)
            {
                if (_queuedReadCount + count > MaxQueuedReadBytes)
                {
                    return new IOException(SR.Format(SR.net_auth_ignored_reauth, MaxQueuedReadBytes.ToString(NumberFormatInfo.CurrentInfo)));
                }

                if (count != 0)
                {
                    // This is inefficient yet simple and that should be a rare case of receiving data encrypted with "old" key.
                    _queuedReadData = EnsureBufferSize(_queuedReadData, _queuedReadCount, _queuedReadCount + count);
                    Buffer.BlockCopy(buffer, offset, _queuedReadData, _queuedReadCount, count);
                    _queuedReadCount += count;
                    FinishHandshakeRead(LockHandshake);
                }
            }
            return null;
        }

        //
        // When re-handshaking the "old" key decrypted data are queued until the handshake is done.
        // When stream calls for decryption we will feed it queued data left from "old" encryption key.
        //
        // Must be called under the lock in case concurrent handshake is going.
        //
        private int CheckOldKeyDecryptedData(Memory<byte> buffer)
        {
            CheckThrow(true);
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
        private void ProcessAuthentication(LazyAsyncResult lazyResult)
        {
            if (Interlocked.Exchange(ref _nestedAuth, 1) == 1)
            {
                throw new InvalidOperationException(SR.Format(SR.net_io_invalidnestedcall, lazyResult == null ? "BeginAuthenticate" : "Authenticate", "authenticate"));
            }

            try
            {
                CheckThrow(false);
                AsyncProtocolRequest asyncRequest = null;
                if (lazyResult != null)
                {
                    asyncRequest = new AsyncProtocolRequest(lazyResult);
                    asyncRequest.Buffer = null;
#if DEBUG
                    lazyResult._debugAsyncChain = asyncRequest;
#endif
                }

                //  A trick to discover and avoid cached sessions.
                _CachedSession = CachedSessionStatus.Unknown;

                ForceAuthentication(_context.IsServer, null, asyncRequest);

                // Not aync so the connection is completed at this point.
                if (lazyResult == null && NetEventSource.IsEnabled)
                {
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
            catch (Exception)
            {
                // If an exception emerges synchronously, the asynchronous operation was not
                // initiated, so no operation is in progress.
                _nestedAuth = 0;
                throw;
            }
            finally
            {
                // For synchronous operations, the operation has completed.
                if (lazyResult == null)
                {
                    _nestedAuth = 0;
                }
            }
        }

        //
        // This is used to reply on re-handshake when received SEC_I_RENEGOTIATE on Read().
        //
        private void ReplyOnReAuthentication(byte[] buffer)
        {
            lock (SyncLock)
            {
                // Note we are already inside the read, so checking for already going concurrent handshake.
                _lockReadState = LockHandshake;

                if (_pendingReHandshake)
                {
                    // A concurrent handshake is pending, resume.
                    FinishRead(buffer);
                    return;
                }
            }

            // Start rehandshake from here.

            // Forcing async mode.  The caller will queue another Read as soon as we return using its preferred
            // calling convention, which will be woken up when the handshake completes.  The callback is just
            // to capture any SocketErrors that happen during the handshake so they can be surfaced from the Read.
            AsyncProtocolRequest asyncRequest = new AsyncProtocolRequest(new LazyAsyncResult(this, null, new AsyncCallback(RehandshakeCompleteCallback)));
            // Buffer contains a result from DecryptMessage that will be passed to ISC/ASC
            asyncRequest.Buffer = buffer;
            ForceAuthentication(false, buffer, asyncRequest);
        }

        //
        // This method attempts to start authentication.
        // Incoming buffer is either null or is the result of "renegotiate" decrypted message
        // If write is in progress the method will either wait or be put on hold
        //
        private void ForceAuthentication(bool receiveFirst, byte[] buffer, AsyncProtocolRequest asyncRequest)
        {
            if (CheckEnqueueHandshake(buffer, asyncRequest))
            {
                // Async handshake is enqueued and will resume later.
                return;
            }
            // Either Sync handshake is ready to go or async handshake won the race over write.

            // This will tell that we don't know the framing yet (what SSL version is)
            _Framing = Framing.Unknown;

            try
            {
                if (receiveFirst)
                {
                    // Listen for a client blob.
                    StartReceiveBlob(buffer, asyncRequest);
                }
                else
                {
                    // We start with the first blob.
                    StartSendBlob(buffer, (buffer == null ? 0 : buffer.Length), asyncRequest);
                }
            }
            catch (Exception e)
            {
                // Failed auth, reset the framing if any.
                _Framing = Framing.Unknown;
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
                    FinishHandshake(null, null);
                }
            }
        }

        private void EndProcessAuthentication(IAsyncResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException("asyncResult");
            }

            LazyAsyncResult lazyResult = result as LazyAsyncResult;
            if (lazyResult == null)
            {
                throw new ArgumentException(SR.Format(SR.net_io_async_result, result.GetType().FullName), "asyncResult");
            }

            if (Interlocked.Exchange(ref _nestedAuth, 0) == 0)
            {
                throw new InvalidOperationException(SR.Format(SR.net_io_invalidendcall, "EndAuthenticate"));
            }

            InternalEndProcessAuthentication(lazyResult);

            // Connection is completed at this point.
            if (NetEventSource.IsEnabled)
            {
                if (NetEventSource.IsEnabled)
                    NetEventSource.Log.SspiSelectedCipherSuite(nameof(EndProcessAuthentication),
                                                                SslProtocol,
                                                                CipherAlgorithm,
                                                                CipherStrength,
                                                                HashAlgorithm,
                                                                HashStrength,
                                                                KeyExchangeAlgorithm,
                                                                KeyExchangeStrength);
            }
        }

        private void InternalEndProcessAuthentication(LazyAsyncResult lazyResult)
        {
            // No "artificial" timeouts implemented so far, InnerStream controls that.
            lazyResult.InternalWaitForCompletion();
            Exception e = lazyResult.Result as Exception;

            if (e != null)
            {
                // Failed auth, reset the framing if any.
                _Framing = Framing.Unknown;
                _handshakeCompleted = false;

                SetException(e);
                ThrowIfExceptional();
            }
        }

        //
        // Client side starts here, but server also loops through this method.
        //
        private void StartSendBlob(byte[] incoming, int count, AsyncProtocolRequest asyncRequest)
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

                if (_Framing == Framing.Unified)
                {
                    _Framing = DetectFraming(message.Payload, message.Payload.Length);
                }

                if (asyncRequest == null)
                {
                    InnerStream.Write(message.Payload, 0, message.Size);
                }
                else
                {
                    asyncRequest.AsyncState = message;
                    Task t = InnerStream.WriteAsync(message.Payload, 0, message.Size);
                    if (t.IsCompleted)
                    {
                        t.GetAwaiter().GetResult();
                    }
                    else
                    {
                        IAsyncResult ar = TaskToApm.Begin(t, s_writeCallback, asyncRequest);
                        if (!ar.CompletedSynchronously)
                        {
#if DEBUG
                            asyncRequest._DebugAsyncChain = ar;
#endif
                            return;
                        }
                        TaskToApm.End(ar);
                    }
                }
            }

            CheckCompletionBeforeNextReceive(message, asyncRequest);
        }

        //
        // This will check and logically complete / fail the auth handshake.
        //
        private void CheckCompletionBeforeNextReceive(ProtocolToken message, AsyncProtocolRequest asyncRequest)
        {
            if (message.Failed)
            {
                StartSendAuthResetSignal(null, asyncRequest, ExceptionDispatchInfo.Capture(new AuthenticationException(SR.net_auth_SSPI, message.GetException())));
                return;
            }
            else if (message.Done && !_pendingReHandshake)
            {
                ProtocolToken alertToken = null;

                if (!CompleteHandshake(ref alertToken))
                {
                    StartSendAuthResetSignal(alertToken, asyncRequest, ExceptionDispatchInfo.Capture(new AuthenticationException(SR.net_ssl_io_cert_validation, null)));
                    return;
                }

                // Release waiting IO if any. Presumably it should not throw.
                // Otherwise application may get not expected type of the exception.
                FinishHandshake(null, asyncRequest);
                return;
            }

            StartReceiveBlob(message.Payload, asyncRequest);
        }

        //
        // Server side starts here, but client also loops through this method.
        //
        private void StartReceiveBlob(byte[] buffer, AsyncProtocolRequest asyncRequest)
        {
            if (_pendingReHandshake)
            {
                if (CheckEnqueueHandshakeRead(ref buffer, asyncRequest))
                {
                    return;
                }

                if (!_pendingReHandshake)
                {
                    // Renegotiate: proceed to the next step.
                    ProcessReceivedBlob(buffer, buffer.Length, asyncRequest);
                    return;
                }
            }

            //This is first server read.
            buffer = EnsureBufferSize(buffer, 0, SecureChannel.ReadHeaderSize);

            int readBytes = 0;
            if (asyncRequest == null)
            {
                readBytes = FixedSizeReader.ReadPacket(_innerStream, buffer, 0, SecureChannel.ReadHeaderSize);
            }
            else
            {
                asyncRequest.SetNextRequest(buffer, 0, SecureChannel.ReadHeaderSize, s_partialFrameCallback);
                _ = FixedSizeReader.ReadPacketAsync(_innerStream, asyncRequest);
                if (!asyncRequest.MustCompleteSynchronously)
                {
                    return;
                }

                readBytes = asyncRequest.Result;
            }

            StartReadFrame(buffer, readBytes, asyncRequest);
        }

        //
        private void StartReadFrame(byte[] buffer, int readBytes, AsyncProtocolRequest asyncRequest)
        {
            if (readBytes == 0)
            {
                // EOF received
                throw new IOException(SR.net_auth_eof);
            }

            if (_Framing == Framing.Unknown)
            {
                _Framing = DetectFraming(buffer, readBytes);
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

            if (asyncRequest == null)
            {
                restBytes = FixedSizeReader.ReadPacket(_innerStream, buffer, readBytes, restBytes);
            }
            else
            {
                asyncRequest.SetNextRequest(buffer, readBytes, restBytes, s_readFrameCallback);
                _ = FixedSizeReader.ReadPacketAsync(_innerStream, asyncRequest);
                if (!asyncRequest.MustCompleteSynchronously)
                {
                    return;
                }

                restBytes = asyncRequest.Result;
                if (restBytes == 0)
                {
                    //EOF received: fail.
                    readBytes = 0;
                }
            }
            ProcessReceivedBlob(buffer, readBytes + restBytes, asyncRequest);
        }

        private void ProcessReceivedBlob(byte[] buffer, int count, AsyncProtocolRequest asyncRequest)
        {
            if (count == 0)
            {
                // EOF received.
                throw new AuthenticationException(SR.net_auth_eof, null);
            }

            if (_pendingReHandshake)
            {
                int offset = 0;
                SecurityStatusPal status = PrivateDecryptData(buffer, ref offset, ref count);

                if (status.ErrorCode == SecurityStatusPalErrorCode.OK)
                {
                    Exception e = EnqueueOldKeyDecryptedData(buffer, offset, count);
                    if (e != null)
                    {
                        StartSendAuthResetSignal(null, asyncRequest, ExceptionDispatchInfo.Capture(e));
                        return;
                    }

                    _Framing = Framing.Unknown;
                    StartReceiveBlob(buffer, asyncRequest);
                    return;
                }
                else if (status.ErrorCode != SecurityStatusPalErrorCode.Renegotiate)
                {
                    // Fail re-handshake.
                    ProtocolToken message = new ProtocolToken(null, status);
                    StartSendAuthResetSignal(null, asyncRequest, ExceptionDispatchInfo.Capture(new AuthenticationException(SR.net_auth_SSPI, message.GetException())));
                    return;
                }

                // We expect only handshake messages from now.
                _pendingReHandshake = false;
                if (offset != 0)
                {
                    Buffer.BlockCopy(buffer, offset, buffer, 0, count);
                }
            }

            StartSendBlob(buffer, count, asyncRequest);
        }

        //
        //  This is to reset auth state on remote side.
        //  If this write succeeds we will allow auth retrying.
        //
        private void StartSendAuthResetSignal(ProtocolToken message, AsyncProtocolRequest asyncRequest, ExceptionDispatchInfo exception)
        {
            if (message == null || message.Size == 0)
            {
                //
                // We don't have an alert to send so cannot retry and fail prematurely.
                //
                exception.Throw();
            }

            if (asyncRequest == null)
            {
                InnerStream.Write(message.Payload, 0, message.Size);
            }
            else
            {
                asyncRequest.AsyncState = exception;
                Task t = InnerStream.WriteAsync(message.Payload, 0, message.Size);
                if (t.IsCompleted)
                {
                    t.GetAwaiter().GetResult();
                }
                else
                {
                    IAsyncResult ar = TaskToApm.Begin(t, s_writeCallback, asyncRequest);
                    if (!ar.CompletedSynchronously)
                    {
                        return;
                    }
                    TaskToApm.End(ar);
                }
            }

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

        private static void WriteCallback(IAsyncResult transportResult)
        {
            if (transportResult.CompletedSynchronously)
            {
                return;
            }

            AsyncProtocolRequest asyncRequest;
            SslStream sslState;

#if DEBUG
            try
            {
#endif
                asyncRequest = (AsyncProtocolRequest)transportResult.AsyncState;
                sslState = (SslStream)asyncRequest.AsyncObject;
#if DEBUG
            }
            catch (Exception exception) when (!ExceptionCheck.IsFatal(exception))
            {
                NetEventSource.Fail(null, $"Exception while decoding context: {exception}");
                throw;
            }
#endif

            // Async completion.
            try
            {
                TaskToApm.End(transportResult);

                // Special case for an error notification.
                object asyncState = asyncRequest.AsyncState;
                ExceptionDispatchInfo exception = asyncState as ExceptionDispatchInfo;
                if (exception != null)
                {
                    exception.Throw();
                }

                sslState.CheckCompletionBeforeNextReceive((ProtocolToken)asyncState, asyncRequest);
            }
            catch (Exception e)
            {
                if (asyncRequest.IsUserCompleted)
                {
                    // This will throw on a worker thread.
                    throw;
                }

                sslState.FinishHandshake(e, asyncRequest);
            }
        }

        private static void PartialFrameCallback(AsyncProtocolRequest asyncRequest)
        {
            if (NetEventSource.IsEnabled)
                NetEventSource.Enter(null);

            // Async ONLY completion.
            SslStream sslState = (SslStream)asyncRequest.AsyncObject;
            try
            {
                sslState.StartReadFrame(asyncRequest.Buffer, asyncRequest.Result, asyncRequest);
            }
            catch (Exception e)
            {
                if (asyncRequest.IsUserCompleted)
                {
                    // This will throw on a worker thread.
                    throw;
                }

                sslState.FinishHandshake(e, asyncRequest);
            }
        }

        //
        //
        private static void ReadFrameCallback(AsyncProtocolRequest asyncRequest)
        {
            if (NetEventSource.IsEnabled)
                NetEventSource.Enter(null);

            // Async ONLY completion.
            SslStream sslState = (SslStream)asyncRequest.AsyncObject;
            try
            {
                if (asyncRequest.Result == 0)
                {
                    //EOF received: will fail.
                    asyncRequest.Offset = 0;
                }

                sslState.ProcessReceivedBlob(asyncRequest.Buffer, asyncRequest.Offset + asyncRequest.Result, asyncRequest);
            }
            catch (Exception e)
            {
                if (asyncRequest.IsUserCompleted)
                {
                    // This will throw on a worker thread.
                    throw;
                }

                sslState.FinishHandshake(e, asyncRequest);
            }
        }

        private bool CheckEnqueueHandshakeRead(ref byte[] buffer, AsyncProtocolRequest request)
        {
            LazyAsyncResult lazyResult = null;
            lock (SyncLock)
            {
                if (_lockReadState == LockPendingRead)
                {
                    return false;
                }

                int lockState = Interlocked.Exchange(ref _lockReadState, LockHandshake);
                if (lockState != LockRead)
                {
                    return false;
                }

                if (request != null)
                {
                    _queuedReadStateRequest = request;
                    return true;
                }

                lazyResult = new LazyAsyncResult(null, null, /*must be */ null);
                _queuedReadStateRequest = lazyResult;
            }

            // Need to exit from lock before waiting.
            lazyResult.InternalWaitForCompletion();
            buffer = (byte[])lazyResult.Result;
            return false;
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
                HandleQueuedCallback(ref _queuedReadStateRequest);
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

                lazyResult = new LazyAsyncResult(null, null, /*must be */ null);
                _queuedReadStateRequest = lazyResult;
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
                _queuedReadStateRequest = taskCompletionSource;
                return new ValueTask<int>(taskCompletionSource.Task);
            }
        }

        private void FinishRead(byte[] renegotiateBuffer)
        {
            int lockState = Interlocked.CompareExchange(ref _lockReadState, LockNone, LockRead);

            if (lockState != LockHandshake)
            {
                return;
            }

            lock (SyncLock)
            {
                LazyAsyncResult ar = _queuedReadStateRequest as LazyAsyncResult;
                if (ar != null)
                {
                    _queuedReadStateRequest = null;
                    ar.InvokeCallback(renegotiateBuffer);
                }
                else
                {
                    AsyncProtocolRequest request = (AsyncProtocolRequest)_queuedReadStateRequest;
                    request.Buffer = renegotiateBuffer;
                    _queuedReadStateRequest = null;
                    ThreadPool.QueueUserWorkItem(s => s.sslState.AsyncResumeHandshakeRead(s.request), (sslState: this, request), preferLocal: false);
                }
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
                    CheckThrow(authSuccessCheck: true);
                    return Task.CompletedTask;
                }

                _lockWriteState = LockPendingWrite;
                TaskCompletionSource<int> completionSource = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                _queuedWriteStateRequest = completionSource;
                return completionSource.Task;
            }
        }

        private void CheckEnqueueWrite()
        {
            // Clear previous request.
            _queuedWriteStateRequest = null;
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
                    CheckThrow(authSuccessCheck: true);
                    return;
                }

                _lockWriteState = LockPendingWrite;

                lazyResult = new LazyAsyncResult(null, null, /*must be */null);
                _queuedWriteStateRequest = lazyResult;
            }

            // Need to exit from lock before waiting.
            lazyResult.InternalWaitForCompletion();
            CheckThrow(authSuccessCheck: true);
            return;
        }

        private void FinishWrite()
        {
            int lockState = Interlocked.CompareExchange(ref _lockWriteState, LockNone, LockWrite);
            if (lockState != LockHandshake)
            {
                return;
            }

            lock (SyncLock)
            {
                HandleQueuedCallback(ref _queuedWriteStateRequest);
            }
        }

        private void HandleQueuedCallback(ref object queuedStateRequest)
        {
            object obj = queuedStateRequest;
            if (obj == null)
            {
                return;
            }
            queuedStateRequest = null;

            switch (obj)
            {
                case LazyAsyncResult lazy:
                    lazy.InvokeCallback();
                    break;
                case TaskCompletionSource<int> taskCompletionSource when taskCompletionSource.Task.AsyncState != null:
                    Memory<byte> array = (Memory<byte>)taskCompletionSource.Task.AsyncState;
                    int oldKeyResult = -1;
                    try
                    {
                        oldKeyResult = CheckOldKeyDecryptedData(array);
                    }
                    catch (Exception exc)
                    {
                        taskCompletionSource.SetException(exc);
                        break;
                    }
                    taskCompletionSource.SetResult(oldKeyResult);
                    break;
                case TaskCompletionSource<int> taskCompletionSource:
                    taskCompletionSource.SetResult(0);
                    break;
                default:
                    ThreadPool.QueueUserWorkItem(s => s.sslState.AsyncResumeHandshake(s.obj), (sslState: this, obj), preferLocal: false);
                    break;
            }
        }

        // Returns:
        // true  - operation queued
        // false - operation can proceed
        private bool CheckEnqueueHandshake(byte[] buffer, AsyncProtocolRequest asyncRequest)
        {
            LazyAsyncResult lazyResult = null;

            lock (SyncLock)
            {
                if (_lockWriteState == LockPendingWrite)
                {
                    return false;
                }

                int lockState = Interlocked.Exchange(ref _lockWriteState, LockHandshake);
                if (lockState != LockWrite)
                {
                    // Proceed with handshake.
                    return false;
                }

                if (asyncRequest != null)
                {
                    asyncRequest.Buffer = buffer;
                    _queuedWriteStateRequest = asyncRequest;
                    return true;
                }

                lazyResult = new LazyAsyncResult(null, null, /*must be*/null);
                _queuedWriteStateRequest = lazyResult;
            }
            lazyResult.InternalWaitForCompletion();
            return false;
        }

        private void FinishHandshake(Exception e, AsyncProtocolRequest asyncRequest)
        {
            try
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
                    HandleQueuedCallback(ref _queuedWriteStateRequest);
                }
            }
            finally
            {
                if (asyncRequest != null)
                {
                    if (e != null)
                    {
                        asyncRequest.CompleteUserWithError(e);
                    }
                    else
                    {
                        asyncRequest.CompleteUser();
                    }
                }
            }
        }

        private async Task WriteAsyncChunked<TWriteAdapter>(TWriteAdapter writeAdapter, ReadOnlyMemory<byte> buffer)
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
                return new ValueTask(WaitForWriteIOSlot(writeAdapter, ioSlot, buffer));
            }

            byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length + FrameOverhead);
            byte[] outBuffer = rentedBuffer;

            SecurityStatusPal status = EncryptData(buffer, ref outBuffer, out int encryptedBytes);

            if (status.ErrorCode != SecurityStatusPalErrorCode.OK)
            {
                // Re-handshake status is not supported.
                ArrayPool<byte>.Shared.Return(rentedBuffer);
                ProtocolToken message = new ProtocolToken(null, status);
                return new ValueTask(Task.FromException(new IOException(SR.net_io_encrypt, message.GetException())));
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
                return new ValueTask(CompleteAsync(t, rentedBuffer));
            }

            async Task WaitForWriteIOSlot(TWriteAdapter wAdapter, Task lockTask, ReadOnlyMemory<byte> buff)
            {
                await lockTask.ConfigureAwait(false);
                await WriteSingleChunk(wAdapter, buff).ConfigureAwait(false);
            }

            async Task CompleteAsync(ValueTask writeTask, byte[] bufferToReturn)
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

                        FinishRead(null);

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
                                throw new IOException(SR.net_ssl_io_renego);
                            }

                            ReplyOnReAuthentication(extraBuffer);

                            // Loop on read.
                            continue;
                        }

                        if (message.CloseConnection)
                        {
                            FinishRead(null);
                            return 0;
                        }

                        throw new IOException(SR.net_io_decrypt, message.GetException());
                    }
                }
            }
            catch (Exception e)
            {
                FinishRead(null);

                if (e is IOException)
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

        private async Task WriteAsyncInternal<TWriteAdapter>(TWriteAdapter writeAdapter, ReadOnlyMemory<byte> buffer)
            where TWriteAdapter : struct, ISslWriteAdapter
        {
            CheckThrow(authSuccessCheck: true, shutdownCheck: true);

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
                    new ValueTask(WriteAsyncChunked(writeAdapter, buffer));
                await t.ConfigureAwait(false);
            }
            catch (Exception e)
            {
                FinishWrite();

                if (e is IOException)
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

        private enum Framing
        {
            Unknown = 0,
            BeforeSSL3,
            SinceSSL3,
            Unified,
            Invalid
        }

        // This is set on the first packet to figure out the framing style.
        private Framing _Framing = Framing.Unknown;

        // SSL3/TLS protocol frames definitions.
        private enum FrameType : byte
        {
            ChangeCipherSpec = 20,
            Alert = 21,
            Handshake = 22,
            AppData = 23
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
                if (_Framing == Framing.Unknown)
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
            if (!_context.IsServer || _Framing == Framing.Unified)
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
            switch (_Framing)
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

        //
        // Called with no user stack.
        //
        private void AsyncResumeHandshake(object state)
        {
            AsyncProtocolRequest request = state as AsyncProtocolRequest;
            Debug.Assert(request != null, "Expected an AsyncProtocolRequest reference.");

            try
            {
                ForceAuthentication(_context.IsServer, request.Buffer, request);
            }
            catch (Exception e)
            {
                request.CompleteUserWithError(e);
            }
        }

        //
        // Called with no user stack.
        //
        private void AsyncResumeHandshakeRead(AsyncProtocolRequest asyncRequest)
        {
            try
            {
                if (_pendingReHandshake)
                {
                    // Resume as read a blob.
                    StartReceiveBlob(asyncRequest.Buffer, asyncRequest);
                }
                else
                {
                    // Resume as process the blob.
                    ProcessReceivedBlob(asyncRequest.Buffer, asyncRequest.Buffer == null ? 0 : asyncRequest.Buffer.Length, asyncRequest);
                }
            }
            catch (Exception e)
            {
                if (asyncRequest.IsUserCompleted)
                {
                    // This will throw on a worker thread.
                    throw;
                }

                FinishHandshake(e, asyncRequest);
            }
        }

        private void RehandshakeCompleteCallback(IAsyncResult result)
        {
            LazyAsyncResult lazyAsyncResult = (LazyAsyncResult)result;
            if (lazyAsyncResult == null)
            {
                NetEventSource.Fail(this, "result is null!");
            }

            if (!lazyAsyncResult.InternalPeekCompleted)
            {
                NetEventSource.Fail(this, "result is not completed!");
            }

            // If the rehandshake succeeded, FinishHandshake has already been called; if there was a SocketException
            // during the handshake, this gets called directly from FixedSizeReader, and we need to call
            // FinishHandshake to wake up the Read that triggered this rehandshake so the error gets back to the caller
            Exception exception = lazyAsyncResult.InternalWaitForCompletion() as Exception;
            if (exception != null)
            {
                // We may be calling FinishHandshake reentrantly, as FinishHandshake can call
                // asyncRequest.CompleteWithError, which will result in this method being called.
                // This is not a problem because:
                //
                // 1. We pass null as the asyncRequest parameter, so this second call to FinishHandshake won't loop
                //    back here.
                //
                // 2. _QueuedWriteStateRequest and _QueuedReadStateRequest are set to null after the first call,
                //    so, we won't invoke their callbacks again.
                //
                // 3. SetException won't overwrite an already-set _Exception.
                //
                // 4. There are three possibilities for _LockReadState and _LockWriteState:
                //
                //    a. They were set back to None by the first call to FinishHandshake, and this will set them to
                //       None again: a no-op.
                //
                //    b. They were set to None by the first call to FinishHandshake, but as soon as the lock was given
                //       up, another thread took a read/write lock.  Calling FinishHandshake again will set them back
                //       to None, but that's fine because that thread will be throwing _Exception before it actually
                //       does any reading or writing and setting them back to None in a catch block anyways.
                //
                //    c. If there is a Read/Write going on another thread, and the second FinishHandshake clears its
                //       read/write lock, it's fine because no other Read/Write can look at the lock until the current
                //       one gives up _SslStream._NestedRead/Write, and no handshake will look at the lock because
                //       handshakes are only triggered in response to successful reads (which won't happen once
                //       _Exception is set).

                FinishHandshake(exception, null);
            }
        }
    }
}
