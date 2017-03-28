// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Security;
using System.Security.Principal;
using System.Threading;
using System.ComponentModel;
using System.Runtime.ExceptionServices;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;

namespace System.Net.Security
{
    //
    // The class maintains the state of the authentication process and the security context.
    // It encapsulates security context and does the real work in authentication and
    // user data encryption
    //
    internal class NegoState
    {
        private static readonly byte[] s_emptyMessage = new byte[0]; // used in reference comparisons
        private static readonly AsyncCallback s_readCallback = new AsyncCallback(ReadCallback);
        private static readonly AsyncCallback s_writeCallback = new AsyncCallback(WriteCallback);

        private Stream _innerStream;
        private bool _leaveStreamOpen;

        private Exception _exception;

        private StreamFramer _framer;
        private NTAuthentication _context;

        private int _nestedAuth;

        internal const int ERROR_TRUST_FAILURE = 1790;   // Used to serialize protectionLevel or impersonationLevel mismatch error to the remote side.
        internal const int MaxReadFrameSize = 64 * 1024;
        internal const int MaxWriteDataSize = 63 * 1024; // 1k for the framing and trailer that is always less as per SSPI.

        private bool _canRetryAuthentication;
        private ProtectionLevel _expectedProtectionLevel;
        private TokenImpersonationLevel _expectedImpersonationLevel;
        private uint _writeSequenceNumber;
        private uint _readSequenceNumber;

        private ExtendedProtectionPolicy _extendedProtectionPolicy;

        // SSPI does not send a server ack on successful auth.
        // This is a state variable used to gracefully handle auth confirmation.
        private bool _remoteOk = false;

        internal NegoState(Stream innerStream, bool leaveStreamOpen)
        {
            if (innerStream == null)
            {
                throw new ArgumentNullException("stream");
            }

            _innerStream = innerStream;
            _leaveStreamOpen = leaveStreamOpen;
        }

        internal static string DefaultPackage
        {
            get
            {
                return NegotiationInfoClass.Negotiate;
            }
        }

        internal IIdentity GetIdentity()
        {
            CheckThrow(true);
            return NegotiateStreamPal.GetIdentity(_context);
        }

        internal void ValidateCreateContext(
            string package,
            NetworkCredential credential,
            string servicePrincipalName,
            ExtendedProtectionPolicy policy,
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

            ValidateCreateContext(package, true, credential, servicePrincipalName, _extendedProtectionPolicy.CustomChannelBinding, protectionLevel, impersonationLevel);
        }

        internal void ValidateCreateContext(
            string package,
            bool isServer,
            NetworkCredential credential,
            string servicePrincipalName,
            ChannelBinding channelBinding,
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
            _framer = new StreamFramer(_innerStream);
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
                flags |= (ContextFlagsPal.ReplayDetect | ContextFlagsPal.SequenceDetect | ContextFlagsPal.InitIntegrity);
            }

            if (isServer)
            {
                if (_extendedProtectionPolicy.PolicyEnforcement == PolicyEnforcement.WhenSupported)
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
                _context = new NTAuthentication(isServer, package, credential, servicePrincipalName, flags, channelBinding);
            }
            catch (Win32Exception e)
            {
                throw new AuthenticationException(SR.net_auth_SSPI, e);
            }
        }

        private Exception SetException(Exception e)
        {
            if (_exception == null || !(_exception is ObjectDisposedException))
            {
                _exception = e;
            }

            if (_exception != null && _context != null)
            {
                _context.CloseContext();
            }

            return _exception;
        }

        internal bool IsAuthenticated
        {
            get
            {
                return _context != null && HandshakeComplete && _exception == null && _remoteOk;
            }
        }

        internal bool IsMutuallyAuthenticated
        {
            get
            {
                if (!IsAuthenticated)
                {
                    return false;
                }

                // Suppressing for NTLM since SSPI does not return correct value in the context flags.
                if (_context.IsNTLM)
                {
                    return false;
                }

                return _context.IsMutualAuthFlag;
            }
        }

        internal bool IsEncrypted
        {
            get
            {
                return IsAuthenticated && _context.IsConfidentialityFlag;
            }
        }

        internal bool IsSigned
        {
            get
            {
                return IsAuthenticated && (_context.IsIntegrityFlag || _context.IsConfidentialityFlag);
            }
        }

        internal bool IsServer
        {
            get
            {
                return _context != null && _context.IsServer;
            }
        }

        internal bool CanGetSecureStream
        {
            get
            {
                return (_context.IsConfidentialityFlag || _context.IsIntegrityFlag);
            }
        }

        internal TokenImpersonationLevel AllowedImpersonation
        {
            get
            {
                CheckThrow(true);
                return PrivateImpersonationLevel;
            }
        }

        private TokenImpersonationLevel PrivateImpersonationLevel
        {
            get
            {
                // We should suppress the delegate flag in NTLM case.
                return (_context.IsDelegationFlag && _context.ProtocolName != NegotiationInfoClass.NTLM) ? TokenImpersonationLevel.Delegation
                        : _context.IsIdentifyFlag ? TokenImpersonationLevel.Identification
                        : TokenImpersonationLevel.Impersonation;
            }
        }

        private bool HandshakeComplete
        {
            get
            {
                return _context.IsCompleted && _context.IsValidContext;
            }
        }

        internal void CheckThrow(bool authSucessCheck)
        {
            if (_exception != null)
            {
                ExceptionDispatchInfo.Throw(_exception);
            }

            if (authSucessCheck && !IsAuthenticated)
            {
                throw new InvalidOperationException(SR.net_auth_noauth);
            }
        }

        //
        // This is to not depend on GC&SafeHandle class if the context is not needed anymore.
        //
        internal void Close()
        {
            // Mark this instance as disposed.
            _exception = new ObjectDisposedException("NegotiateStream");
            if (_context != null)
            {
                _context.CloseContext();
            }
        }

        internal void ProcessAuthentication(LazyAsyncResult lazyResult)
        {
            CheckThrow(false);
            if (Interlocked.Exchange(ref _nestedAuth, 1) == 1)
            {
                throw new InvalidOperationException(SR.Format(SR.net_io_invalidnestedcall, lazyResult == null ? "BeginAuthenticate" : "Authenticate", "authenticate"));
            }

            try
            {
                if (_context.IsServer)
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
                // Round-trip it through SetException().
                e = SetException(e);
                throw;
            }
            finally
            {
                if (lazyResult == null || _exception != null)
                {
                    _nestedAuth = 0;
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

            // No "artificial" timeouts implemented so far, InnerStream controls that.
            lazyResult.InternalWaitForCompletion();

            Exception e = lazyResult.Result as Exception;

            if (e != null)
            {
                // Round-trip it through the SetException().
                e = SetException(e);
                ExceptionDispatchInfo.Throw(e);
            }
        }

        private bool CheckSpn()
        {
            if (_context.IsKerberos)
            {
                return true;
            }

            if (_extendedProtectionPolicy.PolicyEnforcement == PolicyEnforcement.Never ||
                    _extendedProtectionPolicy.CustomServiceNames == null)
            {
                return true;
            }

            string clientSpn = _context.ClientSpecifiedSpn;

            if (String.IsNullOrEmpty(clientSpn))
            {
                if (_extendedProtectionPolicy.PolicyEnforcement == PolicyEnforcement.WhenSupported)
                {
                    return true;
                }
            }
            else
            {
                return _extendedProtectionPolicy.CustomServiceNames.Contains(clientSpn);
            }

            return false;
        }

        //
        // Client side starts here, but server also loops through this method.
        //
        private void StartSendBlob(byte[] message, LazyAsyncResult lazyResult)
        {
            Exception exception = null;
            if (message != s_emptyMessage)
            {
                message = GetOutgoingBlob(message, ref exception);
            }

            if (exception != null)
            {
                // Signal remote side on a failed attempt.
                StartSendAuthResetSignal(lazyResult, message, exception);
                return;
            }

            if (HandshakeComplete)
            {
                if (_context.IsServer && !CheckSpn())
                {
                    exception = new AuthenticationException(SR.net_auth_bad_client_creds_or_target_mismatch);
                    int statusCode = ERROR_TRUST_FAILURE;
                    message = new byte[8];  //sizeof(long)

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
                    message = new byte[8];  //sizeof(long)

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
                    message = new byte[8];  //sizeof(long)

                    for (int i = message.Length - 1; i >= 0; --i)
                    {
                        message[i] = (byte)(statusCode & 0xFF);
                        statusCode = (int)((uint)statusCode >> 8);
                    }

                    StartSendAuthResetSignal(lazyResult, message, exception);
                    return;
                }

                // Signal remote party that we are done
                _framer.WriteHeader.MessageId = FrameHeader.HandshakeDoneId;
                if (_context.IsServer)
                {
                    // Server may complete now because client SSPI would not complain at this point.
                    _remoteOk = true;

                    // However the client will wait for server to send this ACK
                    //Force signaling server OK to the client
                    if (message == null)
                    {
                        message = s_emptyMessage;
                    }
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
                    _framer.WriteMessage(message);
                }
                else
                {
                    IAsyncResult ar = _framer.BeginWriteMessage(message, s_writeCallback, lazyResult);
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
        private void CheckCompletionBeforeNextReceive(LazyAsyncResult lazyResult)
        {
            if (HandshakeComplete && _remoteOk)
            {
                // We are done with success.
                if (lazyResult != null)
                {
                    lazyResult.InvokeCallback();
                }

                return;
            }

            StartReceiveBlob(lazyResult);
        }

        //
        // Server side starts here, but client also loops through this method.
        //
        private void StartReceiveBlob(LazyAsyncResult lazyResult)
        {
            byte[] message;
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

        private void ProcessReceivedBlob(byte[] message, LazyAsyncResult lazyResult)
        {
            // This is an EOF otherwise we would get at least *empty* message but not a null one.
            if (message == null)
            {
                throw new AuthenticationException(SR.net_auth_eof, null);
            }

            // Process Header information.
            if (_framer.ReadHeader.MessageId == FrameHeader.HandshakeErrId)
            {
                if (message.Length >= 8)    // sizeof(long)
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
        private void CheckCompletionBeforeNextSend(byte[] message, LazyAsyncResult lazyResult)
        {
            //If we are done don't go into send.
            if (HandshakeComplete)
            {
                if (!_remoteOk)
                {
                    throw new AuthenticationException(SR.Format(SR.net_io_header_id, "MessageId", _framer.ReadHeader.MessageId, FrameHeader.HandshakeDoneId), null);
                }
                if (lazyResult != null)
                {
                    lazyResult.InvokeCallback();
                }

                return;
            }

            // Not yet done, get a new blob and send it if any.
            StartSendBlob(message, lazyResult);
        }

        //
        //  This is to reset auth state on the remote side.
        //  If this write succeeds we will allow auth retrying.
        //
        private void StartSendAuthResetSignal(LazyAsyncResult lazyResult, byte[] message, Exception exception)
        {
            _framer.WriteHeader.MessageId = FrameHeader.HandshakeErrId;

            if (IsLogonDeniedException(exception))
            {
                if (IsServer)
                {
                    exception = new InvalidCredentialException(SR.net_auth_bad_client_creds, exception);
                }
                else
                {
                    exception = new InvalidCredentialException(SR.net_auth_bad_client_creds_or_target_mismatch, exception);
                }
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

            LazyAsyncResult lazyResult = (LazyAsyncResult)transportResult.AsyncState;

            // Async completion.
            try
            {
                NegoState authState = (NegoState)lazyResult.AsyncObject;
                authState._framer.EndWriteMessage(transportResult);

                // Special case for an error notification.
                if (lazyResult.Result is Exception e)
                {
                    authState._canRetryAuthentication = true;
                    ExceptionDispatchInfo.Throw(e);
                }

                authState.CheckCompletionBeforeNextReceive(lazyResult);
            }
            catch (Exception e)
            {
                if (lazyResult.InternalPeekCompleted)
                {
                    // This will throw on a worker thread.
                    throw;
                }

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

            LazyAsyncResult lazyResult = (LazyAsyncResult)transportResult.AsyncState;

            // Async completion.
            try
            {
                NegoState authState = (NegoState)lazyResult.AsyncObject;
                byte[] message = authState._framer.EndReadMessage(transportResult);
                authState.ProcessReceivedBlob(message, lazyResult);
            }
            catch (Exception e)
            {
                if (lazyResult.InternalPeekCompleted)
                {
                    // This will throw on a worker thread.
                    throw;
                }

                lazyResult.InvokeCallback(e);
            }
        }

        internal static bool IsError(SecurityStatusPal status)
        {
            return ((int)status.ErrorCode >= (int)SecurityStatusPalErrorCode.OutOfMemory);
        }

        private unsafe byte[] GetOutgoingBlob(byte[] incomingBlob, ref Exception e)
        {
            SecurityStatusPal statusCode;
            byte[] message = _context.GetOutgoingBlob(incomingBlob, false, out statusCode);

            if (IsError(statusCode))
            {
                e = NegotiateStreamPal.CreateExceptionFromError(statusCode);
                uint error = (uint)e.HResult;

                message = new byte[sizeof(long)];
                for (int i = message.Length - 1; i >= 0; --i)
                {
                    message[i] = (byte)(error & 0xFF);
                    error = (error >> 8);
                }
            }

            if (message != null && message.Length == 0)
            {
                message = s_emptyMessage;
            }

            return message;
        }

        internal int EncryptData(byte[] buffer, int offset, int count, ref byte[] outBuffer)
        {
            CheckThrow(true);

            // SSPI seems to ignore this sequence number.
            ++_writeSequenceNumber;
            return _context.Encrypt(buffer, offset, count, ref outBuffer, _writeSequenceNumber);
        }

        internal int DecryptData(byte[] buffer, int offset, int count, out int newOffset)
        {
            CheckThrow(true);

            // SSPI seems to ignore this sequence number.
            ++_readSequenceNumber;
            return _context.Decrypt(buffer, offset, count, out newOffset, _readSequenceNumber);
        }

        internal static void ThrowCredentialException(long error)
        {
            Win32Exception e = new Win32Exception((int)error);

            if (e.NativeErrorCode == (int)SecurityStatusPalErrorCode.LogonDenied)
            {
                throw new InvalidCredentialException(SR.net_auth_bad_client_creds, e);
            }

            if (e.NativeErrorCode == NegoState.ERROR_TRUST_FAILURE)
            {
                throw new AuthenticationException(SR.net_auth_context_expectation_remote, e);
            }

            throw new AuthenticationException(SR.net_auth_alert, e);
        }

        internal static bool IsLogonDeniedException(Exception exception)
        {
            Win32Exception win32exception = exception as Win32Exception;

            return (win32exception != null) && (win32exception.NativeErrorCode == (int)SecurityStatusPalErrorCode.LogonDenied);
        }
    }
}
