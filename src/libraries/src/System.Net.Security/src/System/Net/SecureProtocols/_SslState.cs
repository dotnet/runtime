// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace System.Net.Security
{
    internal class SslState
    {
        private static int s_uniqueNameInteger = 123;
        private static AsyncProtocolCallback s_partialFrameCallback = new AsyncProtocolCallback(PartialFrameCallback);
        private static AsyncProtocolCallback s_readFrameCallback = new AsyncProtocolCallback(ReadFrameCallback);
        private static AsyncCallback s_writeCallback = new AsyncCallback(WriteCallback);

        private RemoteCertValidationCallback _certValidationDelegate;
        private LocalCertSelectionCallback _certSelectionDelegate;

        private Stream _innerStream;

        // TODO (Issue #3114): Implement using TPL instead of APM.
        private StreamAsyncHelper _innerStreamAPM;

        private _SslStream _secureStream;

        private FixedSizeReader _reader;

        private int _nestedAuth;
        private SecureChannel _context;

        private bool _handshakeCompleted;
        private bool _certValidationFailed;
        private Interop.SecurityStatus _securityStatus;
        private Exception _exception;

        private enum CachedSessionStatus : byte
        {
            Unknown = 0,
            IsNotCached = 1,
            IsCached = 2,
            Renegotiated = 3
        }
        private CachedSessionStatus _CachedSession;

        // This block is used by rehandshake code to buffer data decryptred with the old key.
        private byte[] _queuedReadData;
        private int _queuedReadCount;
        private bool _pendingReHandshake;
        private const int MaxQueuedReadBytes = 1024 * 128;

        //
        // This block is used to rule the >>re-handshakes<< that are concurent with read/write io requests.
        //
        private const int LockNone = 0;
        private const int LockWrite = 1;
        private const int LockHandshake = 2;
        private const int LockPendingWrite = 3;
        private const int LockRead = 4;
        private const int LockPendingRead = 6;

        private int _lockWriteState;
        private object _queuedWriteStateRequest;

        private int _lockReadState;
        private object _queuedReadStateRequest;

        private readonly EncryptionPolicy _encryptionPolicy;

        //
        //  The public Client and Server classes enforce the parameters rules before
        //  calling into this .ctor.
        //
        internal SslState(Stream innerStream, RemoteCertValidationCallback certValidationCallback, LocalCertSelectionCallback certSelectionCallback, EncryptionPolicy encryptionPolicy)
        {
            _innerStream = innerStream;
            _innerStreamAPM = new StreamAsyncHelper(innerStream);
            _reader = new FixedSizeReader(innerStream);
            _certValidationDelegate = certValidationCallback;
            _certSelectionDelegate = certSelectionCallback;
            _encryptionPolicy = encryptionPolicy;
        }
        //
        //
        //
        internal void ValidateCreateContext(bool isServer, string targetHost, SslProtocols enabledSslProtocols, X509Certificate serverCertificate, X509CertificateCollection clientCertificates, bool remoteCertRequired, bool checkCertRevocationStatus)
        {
            ValidateCreateContext(isServer, targetHost, enabledSslProtocols, serverCertificate, clientCertificates, remoteCertRequired,
                                   checkCertRevocationStatus, !isServer);
        }

        internal void ValidateCreateContext(bool isServer, string targetHost, SslProtocols enabledSslProtocols, X509Certificate serverCertificate, X509CertificateCollection clientCertificates, bool remoteCertRequired, bool checkCertRevocationStatus, bool checkCertName)
        {
            //
            // We don't support SSL alerts right now, hence any exception is fatal and cannot be retried.
            //
            if (_exception != null)
            {
                throw _exception;
            }

            if (Context != null && Context.IsValidContext)
            {
                throw new InvalidOperationException(SR.net_auth_reauth);
            }

            if (Context != null && IsServer != isServer)
            {
                throw new InvalidOperationException(SR.net_auth_client_server);
            }

            if (targetHost == null)
            {
                throw new ArgumentNullException("targetHost");
            }
            
            if (isServer)
            {
                enabledSslProtocols &= (SslProtocols)Interop.SChannel.ServerProtocolMask;
                if (serverCertificate == null)
                {
                    throw new ArgumentNullException("serverCertificate");
                }
            }
            else
            {
                enabledSslProtocols &= (SslProtocols)Interop.SChannel.ClientProtocolMask;
            }

            if ((int)enabledSslProtocols == 0)
            {
                throw new ArgumentException(SR.Format(SR.net_invalid_enum, "SslProtocolType"), "sslProtocolType");
            }

            if (clientCertificates == null)
            {
                clientCertificates = new X509CertificateCollection();
            }

            if (targetHost.Length == 0)
            {
                targetHost = "?" + Interlocked.Increment(ref s_uniqueNameInteger).ToString(NumberFormatInfo.InvariantInfo);
            }

            _exception = null;
            try
            {
                _context = new SecureChannel(targetHost, isServer, (int)enabledSslProtocols, serverCertificate, clientCertificates, remoteCertRequired,
                                                               checkCertName, checkCertRevocationStatus, _encryptionPolicy, _certSelectionDelegate);
            }
            catch (Win32Exception e)
            {
                throw new AuthenticationException(SR.net_auth_SSPI, e);
            }
        }

        internal bool IsAuthenticated
        {
            get
            {
                return _context != null && _context.IsValidContext && _exception == null && HandshakeCompleted;
            }
        }

        internal bool IsMutuallyAuthenticated
        {
            get
            {
                return
                    IsAuthenticated &&
                    (Context.IsServer ? Context.LocalServerCertificate : Context.LocalClientCertificate) != null &&
                    Context.IsRemoteCertificateAvailable; /* does not work: Context.IsMutualAuthFlag;*/
            }
        }

        internal bool RemoteCertRequired
        {
            get
            {
                return Context == null || Context.RemoteCertRequired;
            }
        }

        internal bool IsServer
        {
            get
            {
                return Context != null && Context.IsServer;
            }
        }

        //
        // SSL related properties
        //
        internal void SetCertValidationDelegate(RemoteCertValidationCallback certValidationCallback)
        {
            _certValidationDelegate = certValidationCallback;
        }

        //
        // This will return selected local cert for both client/server streams
        //
        internal X509Certificate LocalCertificate
        {
            get
            {
                CheckThrow(true);
                return InternalLocalCertificate;
            }
        }

        private X509Certificate InternalLocalCertificate
        {
            get
            {
                return Context.IsServer ? Context.LocalServerCertificate : Context.LocalClientCertificate;
            }
        }

        internal ChannelBinding GetChannelBinding(ChannelBindingKind kind)
        {
            return (Context == null) ? null : Context.GetChannelBinding(kind);
        }

        internal bool CheckCertRevocationStatus
        {
            get
            {
                return Context != null && Context.CheckCertRevocationStatus;
            }
        }

        internal Interop.SecurityStatus LastSecurityStatus
        {
            get { return _securityStatus; }
        }

        internal bool IsCertValidationFailed
        {
            get
            {
                return _certValidationFailed;
            }
        }

        internal bool DataAvailable
        {
            get
            {
                return IsAuthenticated && (SecureStream.DataAvailable || _queuedReadCount != 0);
            }
        }

        internal CipherAlgorithmType CipherAlgorithm
        {
            get
            {
                CheckThrow(true);
                SslConnectionInfo info = Context.ConnectionInfo;
                if (info == null)
                {
                    return CipherAlgorithmType.None;
                }
                return (CipherAlgorithmType)info.DataCipherAlg;
            }
        }

        internal int CipherStrength
        {
            get
            {
                CheckThrow(true);
                SslConnectionInfo info = Context.ConnectionInfo;
                if (info == null)
                {
                    return 0;
                }

                return info.DataKeySize;
            }
        }

        internal HashAlgorithmType HashAlgorithm
        {
            get
            {
                CheckThrow(true);
                SslConnectionInfo info = Context.ConnectionInfo;
                if (info == null)
                {
                    return (HashAlgorithmType)0;
                }
                return (HashAlgorithmType)info.DataHashAlg;
            }
        }

        internal int HashStrength
        {
            get
            {
                CheckThrow(true);
                SslConnectionInfo info = Context.ConnectionInfo;
                if (info == null)
                {
                    return 0;
                }

                return info.DataHashKeySize;
            }
        }

        internal ExchangeAlgorithmType KeyExchangeAlgorithm
        {
            get
            {
                CheckThrow(true);
                SslConnectionInfo info = Context.ConnectionInfo;
                if (info == null)
                {
                    return (ExchangeAlgorithmType)0;
                }

                return (ExchangeAlgorithmType)info.KeyExchangeAlg;
            }
        }

        internal int KeyExchangeStrength
        {
            get
            {
                CheckThrow(true);
                SslConnectionInfo info = Context.ConnectionInfo;
                if (info == null)
                {
                    return 0;
                }

                return info.KeyExchKeySize;
            }
        }

        internal SslProtocols SslProtocol
        {
            get
            {
                CheckThrow(true);
                SslConnectionInfo info = Context.ConnectionInfo;
                if (info == null)
                {
                    return SslProtocols.None;
                }

                SslProtocols proto = (SslProtocols)info.Protocol;
                SslProtocols ret = SslProtocols.None;

                // Restore client/server bits so the result maps exactly on published constants.
                if ((proto & SslProtocols.Ssl2) != 0)
                {
                    ret |= SslProtocols.Ssl2;
                }

                if ((proto & SslProtocols.Ssl3) != 0)
                {
                    ret |= SslProtocols.Ssl3;
                }

                if ((proto & SslProtocols.Tls) != 0)
                {
                    ret |= SslProtocols.Tls;
                }

                if ((proto & SslProtocols.Tls11) != 0)
                {
                    ret |= SslProtocols.Tls11;
                }

                if ((proto & SslProtocols.Tls12) != 0)
                {
                    ret |= SslProtocols.Tls12;
                }

                return ret;
            }
        }

        internal Stream InnerStream
        {
            get
            {
                return _innerStream;
            }
        }

        internal StreamAsyncHelper InnerStreamAPM
        {
            get
            {
                return _innerStreamAPM;
            }
        }

        internal _SslStream SecureStream
        {
            get
            {
                CheckThrow(true);
                if (_secureStream == null)
                {
                    Interlocked.CompareExchange<_SslStream>(ref _secureStream, new _SslStream(this), null);
                }

                return _secureStream;
            }
        }

        internal int HeaderSize
        {
            get
            {
                return Context.HeaderSize;
            }
        }

        internal int MaxDataSize
        {
            get
            {
                return Context.MaxDataSize;
            }
        }

        private Exception SetException(Exception e)
        {
            if (_exception == null)
            {
                _exception = e;
            }

            if (_exception != null && Context != null)
            {
                Context.Close();
            }

            return _exception;
        }

        private bool HandshakeCompleted
        {
            get
            {
                return _handshakeCompleted;
            }
        }

        private SecureChannel Context
        {
            get
            {
                return _context;
            }
        }

        internal void CheckThrow(bool authSucessCheck)
        {
            if (_exception != null)
            {
                throw _exception;
            }

            if (authSucessCheck && !IsAuthenticated)
            {
                throw new InvalidOperationException(SR.net_auth_noauth);
            }
        }

        internal void Flush()
        {
            InnerStream.Flush();
        }

        //
        // This is to not depend on GC&SafeHandle class if the context is not needed anymore.
        //
        internal void Close()
        {
            _exception = new ObjectDisposedException("SslStream");
            if (Context != null)
            {
                Context.Close();
            }
        }

        internal Interop.SecurityStatus EncryptData(byte[] buffer, int offset, int count, ref byte[] outBuffer, out int outSize)
        {
            CheckThrow(true);
            return Context.Encrypt(buffer, offset, count, ref outBuffer, out outSize);
        }

        internal Interop.SecurityStatus DecryptData(byte[] buffer, ref int offset, ref int count)
        {
            CheckThrow(true);
            return PrivateDecryptData(buffer, ref offset, ref count);
        }

        private Interop.SecurityStatus PrivateDecryptData(byte[] buffer, ref int offset, ref int count)
        {
            return Context.Decrypt(buffer, ref offset, ref count);
        }

        //
        //  Called by re-handshake if found data decrypted with the old key
        //
        private Exception EnqueueOldKeyDecryptedData(byte[] buffer, int offset, int count)
        {
            lock (this)
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
        // Must be called under the lock in case concurent handshake is going.
        //
        internal int CheckOldKeyDecryptedData(byte[] buffer, int offset, int count)
        {
            CheckThrow(true);
            if (_queuedReadData != null)
            {
                // This is inefficient yet simple and should be a REALLY rare case.
                int toCopy = Math.Min(_queuedReadCount, count);
                Buffer.BlockCopy(_queuedReadData, 0, buffer, offset, toCopy);
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
        internal void ProcessAuthentication(LazyAsyncResult lazyResult)
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
                    lazyResult._DebugAsyncChain = asyncRequest;
#endif
                }

                //  A trick to discover and avoid cached sessions.
                _CachedSession = CachedSessionStatus.Unknown;

                ForceAuthentication(Context.IsServer, null, asyncRequest);

                // Not aync so the connection is completed at this point.
                if (lazyResult == null && Logging.On)
                {
                    Logging.PrintInfo(Logging.Web, SR.Format(SR.net_log_sspi_selected_cipher_suite,
                        "ProcessAuthentication",
                        SslProtocol,
                        CipherAlgorithm,
                        CipherStrength,
                        HashAlgorithm,
                        HashStrength,
                        KeyExchangeAlgorithm,
                        KeyExchangeStrength));
                }
            }
            finally
            {
                if (lazyResult == null || _exception != null)
                {
                    _nestedAuth = 0;
                }
            }
        }

        //
        // This is used to reply on re-handshake when received SEC_I_RENEGOTIATE on Read().
        //
        internal void ReplyOnReAuthentication(byte[] buffer)
        {
            lock (this)
            {
                // Note we are already inside the read, so checking for already going concurent handshake.
                _lockReadState = LockHandshake;

                if (_pendingReHandshake)
                {
                    // A concurent handshake is pending, resume.
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

                if (SetException(e) == e)
                {
                    throw;
                }
                else
                {
                    throw _exception;
                }
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

        internal void EndProcessAuthentication(IAsyncResult result)
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
            if (Logging.On)
            {
                Logging.PrintInfo(Logging.Web, SR.Format(SR.net_log_sspi_selected_cipher_suite,
                    "EndProcessAuthentication",
                    SslProtocol,
                    CipherAlgorithm,
                    CipherStrength,
                    HashAlgorithm,
                    HashStrength,
                    KeyExchangeAlgorithm,
                    KeyExchangeStrength));
            }
        }
        //
        //
        //
        internal void InternalEndProcessAuthentication(LazyAsyncResult lazyResult)
        {
            // No "artificial" timeouts implemented so far, InnerStream controls that.
            lazyResult.InternalWaitForCompletion();
            Exception e = lazyResult.Result as Exception;

            if (e != null)
            {
                // Failed auth, reset the framing if any.
                _Framing = Framing.Unknown;
                _handshakeCompleted = false;

                throw SetException(e);
            }
        }

        //
        // Client side starts here, but server also loops through this method.
        //
        private void StartSendBlob(byte[] incoming, int count, AsyncProtocolRequest asyncRequest)
        {
            ProtocolToken message = Context.NextMessage(incoming, 0, count);
            _securityStatus = message.Status;

            if (message.Size != 0)
            {
                if (Context.IsServer && _CachedSession == CachedSessionStatus.Unknown)
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
                    IAsyncResult ar = InnerStreamAPM.BeginWrite(message.Payload, 0, message.Size, s_writeCallback, asyncRequest);
                    if (!ar.CompletedSynchronously)
                    {
#if DEBUG
                        asyncRequest._DebugAsyncChain = ar;
#endif
                        return;
                    }

                    InnerStreamAPM.EndWrite(ar);
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
                StartSendAuthResetSignal(null, asyncRequest, new AuthenticationException(SR.net_auth_SSPI, message.GetException()));
                return;
            }
            else if (message.Done && !_pendingReHandshake)
            {
                if (!CompleteHandshake())
                {
                    StartSendAuthResetSignal(null, asyncRequest, new AuthenticationException(SR.net_ssl_io_cert_validation, null));
                    return;
                }

                // Release waiting IO if any. Presumably it should not throw.
                // Otheriwse application may get not expected type of the exception.
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
                readBytes = _reader.ReadPacket(buffer, 0, SecureChannel.ReadHeaderSize);
            }
            else
            {
                asyncRequest.SetNextRequest(buffer, 0, SecureChannel.ReadHeaderSize, s_partialFrameCallback);
                _reader.AsyncReadPacket(asyncRequest);
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

            int restBytes = GetRemainingFrameSize(buffer, readBytes);

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
                restBytes = _reader.ReadPacket(buffer, readBytes, restBytes);
            }
            else
            {
                asyncRequest.SetNextRequest(buffer, readBytes, restBytes, s_readFrameCallback);
                _reader.AsyncReadPacket(asyncRequest);
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
                Interop.SecurityStatus status = PrivateDecryptData(buffer, ref offset, ref count);

                if (status == Interop.SecurityStatus.OK)
                {
                    Exception e = EnqueueOldKeyDecryptedData(buffer, offset, count);
                    if (e != null)
                    {
                        StartSendAuthResetSignal(null, asyncRequest, e);
                        return;
                    }

                    _Framing = Framing.Unknown;
                    StartReceiveBlob(buffer, asyncRequest);
                    return;
                }
                else if (status != Interop.SecurityStatus.Renegotiate)
                {
                    // Fail re-handshake.
                    ProtocolToken message = new ProtocolToken(null, status);
                    StartSendAuthResetSignal(null, asyncRequest, new AuthenticationException(SR.net_auth_SSPI, message.GetException()));
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
        private void StartSendAuthResetSignal(ProtocolToken message, AsyncProtocolRequest asyncRequest, Exception exception)
        {
            if (message == null || message.Size == 0)
            {
                //
                // We don't have an alert to send so cannot retry and fail prematurely.
                //
                throw exception;
            }

            if (asyncRequest == null)
            {
                InnerStream.Write(message.Payload, 0, message.Size);
            }
            else
            {
                asyncRequest.AsyncState = exception;
                IAsyncResult ar = InnerStreamAPM.BeginWrite(message.Payload, 0, message.Size, s_writeCallback, asyncRequest);
                if (!ar.CompletedSynchronously)
                {
                    return;
                }
                InnerStreamAPM.EndWrite(ar);
            }

            throw exception;
        }

        // - Loads the channel parameters
        // - Optionally verifies the Remote Certificate
        // - Sets HandshakeCompleted flag
        // - Sets the guarding event if other thread is waiting for
        //   handshake completino
        //
        // - Returns false if failed to verify the Remote Cert
        //
        private bool CompleteHandshake()
        {
            GlobalLog.Enter("CompleteHandshake");
            Context.ProcessHandshakeSuccess();

            if (!Context.VerifyRemoteCertificate(_certValidationDelegate))
            {
                _handshakeCompleted = false;
                _certValidationFailed = true;
                GlobalLog.Leave("CompleteHandshake", false);
                return false;
            }

            _certValidationFailed = false;
            _handshakeCompleted = true;
            GlobalLog.Leave("CompleteHandshake", true);
            return true;
        }

        private static void WriteCallback(IAsyncResult transportResult)
        {
            if (transportResult.CompletedSynchronously)
            {
                return;
            }

            AsyncProtocolRequest asyncRequest;
            SslState sslState;

#if DEBUG
            try
            {
#endif
                asyncRequest = (AsyncProtocolRequest)transportResult.AsyncState;
                sslState = (SslState)asyncRequest.AsyncObject;
#if DEBUG
            }
            catch (Exception exception)
            {
                if (!ExceptionCheck.IsFatal(exception))
                {
                    GlobalLog.Assert("SslState::WriteCallback", "Exception while decoding context. type:" + exception.GetType().ToString() + " message:" + exception.Message);
                }

                throw;
            }
#endif

            // Async completion.
            try
            {
                sslState.InnerStreamAPM.EndWrite(transportResult);

                // Special case for an error notification.
                object asyncState = asyncRequest.AsyncState;
                Exception exception = asyncState as Exception;
                if (exception != null)
                {
                    throw exception;
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
            GlobalLog.Print("SslState::PartialFrameCallback()");

            // Async ONLY completion.
            SslState sslState = (SslState)asyncRequest.AsyncObject;
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
            GlobalLog.Print("SslState::ReadFrameCallback()");

            // Async ONLY completion.
            SslState sslState = (SslState)asyncRequest.AsyncObject;
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
            lock (this)
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
            lock (this)
            {
                // Lock is redundant here. Included for clarity.
                int lockState = Interlocked.Exchange(ref _lockReadState, newState);

                if (lockState != LockPendingRead)
                {
                    return;
                }

                _lockReadState = LockRead;
                object obj = _queuedReadStateRequest;
                if (obj == null)
                {
                    // Other thread did not get under the lock yet.
                    return;
                }

                _queuedReadStateRequest = null;
                if (obj is LazyAsyncResult)
                {
                    ((LazyAsyncResult)obj).InvokeCallback();
                }
                else
                {
                    ThreadPool.QueueUserWorkItem(new WaitCallback(CompleteRequestWaitCallback), obj);
                }
            }
        }

        // Returns:
        // -1    - proceed
        // 0     - queued
        // X     - some bytes are ready, no need for IO
        internal int CheckEnqueueRead(byte[] buffer, int offset, int count, AsyncProtocolRequest request)
        {
            int lockState = Interlocked.CompareExchange(ref _lockReadState, LockRead, LockNone);

            if (lockState != LockHandshake)
            {
                // Proceed, no concurent handshake is ongoing so no need for a lock.
                return CheckOldKeyDecryptedData(buffer, offset, count);
            }

            LazyAsyncResult lazyResult = null;
            lock (this)
            {
                int result = CheckOldKeyDecryptedData(buffer, offset, count);
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

                if (request != null)
                {
                    // Request queued.
                    _queuedReadStateRequest = request;
                    return 0;
                }
                lazyResult = new LazyAsyncResult(null, null, /*must be */ null);
                _queuedReadStateRequest = lazyResult;
            }
            // Need to exit from lock before waiting.
            lazyResult.InternalWaitForCompletion();
            lock (this)
            {
                return CheckOldKeyDecryptedData(buffer, offset, count);
            }
        }

        internal void FinishRead(byte[] renegotiateBuffer)
        {
            int lockState = Interlocked.CompareExchange(ref _lockReadState, LockNone, LockRead);

            if (lockState != LockHandshake)
            {
                return;
            }

            lock (this)
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
                    ThreadPool.QueueUserWorkItem(new WaitCallback(AsyncResumeHandshakeRead), request);
                }
            }
        }

        // Returns: 
        // true  - operation queued
        // false - operation can proceed
        internal bool CheckEnqueueWrite(AsyncProtocolRequest asyncRequest)
        {
            // Clear previous request.
            _queuedWriteStateRequest = null;
            int lockState = Interlocked.CompareExchange(ref _lockWriteState, LockWrite, LockNone);
            if (lockState != LockHandshake)
            {
                // Proceed with write.
                return false;
            }

            LazyAsyncResult lazyResult = null;
            lock (this)
            {
                if (_lockWriteState != LockHandshake)
                {
                    // Handshake has completed before we grabbed the lock.
                    CheckThrow(true);
                    return false;
                }

                _lockWriteState = LockPendingWrite;

                // Still pending, wait or enqueue.
                if (asyncRequest != null)
                {
                    _queuedWriteStateRequest = asyncRequest;
                    return true;
                }

                lazyResult = new LazyAsyncResult(null, null, /*must be */null);
                _queuedWriteStateRequest = lazyResult;
            }

            // Need to exit from lock before waiting.
            lazyResult.InternalWaitForCompletion();
            CheckThrow(true);
            return false;
        }

        internal void FinishWrite()
        {
            int lockState = Interlocked.CompareExchange(ref _lockWriteState, LockNone, LockWrite);
            if (lockState != LockHandshake)
            {
                return;
            }

            lock (this)
            {
                object obj = _queuedWriteStateRequest;
                if (obj == null)
                {
                    // A repeated call.
                    return;
                }

                _queuedWriteStateRequest = null;
                if (obj is LazyAsyncResult)
                {
                    // Sync handshake is waiting on other thread.
                    ((LazyAsyncResult)obj).InvokeCallback();
                }
                else
                {
                    // Async handshake is pending, start it on other thread.
                    // Consider: we could start it in on this thread but that will delay THIS write completion
                    ThreadPool.QueueUserWorkItem(new WaitCallback(AsyncResumeHandshake), obj);
                }
            }
        }

        // Returns:
        // true  - operation queued
        // false - operation can proceed
        private bool CheckEnqueueHandshake(byte[] buffer, AsyncProtocolRequest asyncRequest)
        {
            LazyAsyncResult lazyResult = null;

            lock (this)
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
                lock (this)
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
                    object obj = _queuedWriteStateRequest;
                    if (obj == null)
                    {
                        // We finished before Write has grabbed the lock.
                        return;
                    }

                    _queuedWriteStateRequest = null;

                    if (obj is LazyAsyncResult)
                    {
                        // Sync write is waiting on other thread.
                        ((LazyAsyncResult)obj).InvokeCallback();
                    }
                    else
                    {
                        // Async write is pending, start it on other thread.
                        // Consider: we could start it in on this thread but that will delay THIS handshake completion
                        ThreadPool.QueueUserWorkItem(new WaitCallback(CompleteRequestWaitCallback), obj);
                    }
                }
            }
            finally
            {
                if (asyncRequest != null)
                {
                    if (e != null)
                    {
                        asyncRequest.CompleteWithError(e);
                    }
                    else
                    {
                        asyncRequest.CompleteUser();
                    }
                }
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

            /* Microsft Unihello starts with
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

            GlobalLog.Assert((bytes != null && bytes.Length > 0), "SslState::DetectFraming()|Header buffer is not allocated will boom shortly.");

            // If the first byte is SSL3 HandShake, then check if we have a SSLv3 Type3 client hello.
            if (bytes[0] == (byte)FrameType.Handshake || bytes[0] == (byte)FrameType.AppData
                || bytes[0] == (byte)FrameType.Alert)
            {
                if (length < 3)
                {
                    return Framing.Invalid;
                }

#if TRACE_VERBOSE
                if (bytes[1] != 3) 
                {
                    GlobalLog.Print("WARNING: SslState::DetectFraming() SSL protocol is > 3, trying SSL3 framing in retail = " + bytes[1].ToString("x", NumberFormatInfo.InvariantInfo));
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
            if ((bytes[0] & 0x80) == 0)
            {
                // We have a three-byte header format
                GlobalLog.Print("WARNING: SslState::DetectFraming() SSL v <=2 HELLO has no high bit set for 3 bytes header, we are broken, received byte = " + bytes[0].ToString("x", NumberFormatInfo.InvariantInfo));
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
            if (!Context.IsServer || _Framing == Framing.Unified)
            {
                return Framing.BeforeSSL3;
            }

            return Framing.Unified; // Will use Ssl2 just for this frame.
        }

        //
        // This is called from SslStream class too.
        internal int GetRemainingFrameSize(byte[] buffer, int dataSize)
        {
            GlobalLog.Enter("GetRemainingFrameSize", "dataSize = " + dataSize);
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

                    if ((buffer[0] & 0x80) != 0)
                    {
                        // Two bytes
                        payloadSize = (((buffer[0] & 0x7f) << 8) | buffer[1]) + 2;
                        payloadSize -= dataSize;
                    }
                    else
                    {
                        // Three bytes
                        payloadSize = (((buffer[0] & 0x3f) << 8) | buffer[1]) + 3;
                        payloadSize -= dataSize;
                    }

                    break;
                case Framing.SinceSSL3:
                    if (dataSize < 5)
                    {
                        throw new System.IO.IOException(SR.net_ssl_io_frame);
                    }

                    payloadSize = ((buffer[3] << 8) | buffer[4]) + 5;
                    payloadSize -= dataSize;
                    break;
                default:
                    break;
            }
            GlobalLog.Leave("GetRemainingFrameSize", payloadSize);
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
                ForceAuthentication(Context.IsServer, request.Buffer, request);
            }
            catch (Exception e)
            {
                request.CompleteWithError(e);
            }
        }

        //
        // Called with no user stack.
        //
        private void AsyncResumeHandshakeRead(object state)
        {
            AsyncProtocolRequest asyncRequest = (AsyncProtocolRequest)state;
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

        //
        // Called with no user stack.
        //
        private void CompleteRequestWaitCallback(object state)
        {
            AsyncProtocolRequest request = (AsyncProtocolRequest)state;

            // Force async completion.
            if (request.MustCompleteSynchronously)
            {
                throw new InternalException();
            }

            request.CompleteRequest(0);
        }

        private void RehandshakeCompleteCallback(IAsyncResult result)
        {
            LazyAsyncResult lazyAsyncResult = (LazyAsyncResult)result;
            GlobalLog.Assert(lazyAsyncResult != null, "SslState::RehandshakeCompleteCallback()|result is null!");
            GlobalLog.Assert(lazyAsyncResult.InternalPeekCompleted, "SslState::RehandshakeCompleteCallback()|result is not completed!");

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
                // 4. There are three possibilites for _LockReadState and _LockWriteState:
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
