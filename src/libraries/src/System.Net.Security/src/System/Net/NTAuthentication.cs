// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Net.Security;
using System.Security.Authentication.ExtendedProtection;
using System.Threading;

namespace System.Net
{
    internal class NTAuthentication
    {
        static private ContextCallback s_InitializeCallback = new ContextCallback(InitializeCallback);

        private bool _isServer;

        private SafeFreeCredentials _credentialsHandle;
        private SafeDeleteContext _securityContext;
        private string _spn;
        private string _clientSpecifiedSpn;

        private int _tokenSize;
        private ContextFlagsPal _requestedContextFlags;
        private ContextFlagsPal _contextFlags;

        private bool _isCompleted;
        private string _protocolName;
        private string _lastProtocolName;
        private string _package;

        private ChannelBinding _channelBinding;

        // If set, no more calls should be made.
        internal bool IsCompleted
        {
            get
            {
                return _isCompleted;
            }
        }

        internal bool IsValidContext
        {
            get
            {
                return !(_securityContext == null || _securityContext.IsInvalid);
            }
        }

        internal string AssociatedName
        {
            get
            {
                if (!(IsValidContext && IsCompleted))
                {
                    throw new Win32Exception((int)SecurityStatusPalErrorCode.InvalidHandle);
                }

                string name = NegotiateStreamPal.QueryContextAssociatedName(_securityContext);
                if (GlobalLog.IsEnabled)
                {
                    GlobalLog.Print("NTAuthentication: The context is associated with [" + name + "]");
                }
                return name;
            }
        }

        internal bool IsConfidentialityFlag
        {
            get
            {
                return (_contextFlags & ContextFlagsPal.Confidentiality) != 0;
            }
        }

        internal bool IsIntegrityFlag
        {
            get
            {
                return (_contextFlags & (_isServer ? ContextFlagsPal.AcceptIntegrity : ContextFlagsPal.InitIntegrity)) != 0;
            }
        }

        internal bool IsMutualAuthFlag
        {
            get
            {
                return (_contextFlags & ContextFlagsPal.MutualAuth) != 0;
            }
        }

        internal bool IsDelegationFlag
        {
            get
            {
                return (_contextFlags & ContextFlagsPal.Delegate) != 0;
            }
        }

        internal bool IsIdentifyFlag
        {
            get
            {
                return (_contextFlags & (_isServer ? ContextFlagsPal.AcceptIdentify : ContextFlagsPal.InitIdentify)) != 0;
            }
        }

        internal string Spn
        {
            get
            {
                return _spn;
            }
        }

        internal string ClientSpecifiedSpn
        {
            get
            {
                if (_clientSpecifiedSpn == null)
                {
                    _clientSpecifiedSpn = GetClientSpecifiedSpn();
                }

                return _clientSpecifiedSpn;
            }
        }

        //
        // True indicates this instance is for Server and will use AcceptSecurityContext SSPI API.
        //
        internal bool IsServer
        {
            get
            {
                return _isServer;
            }
        }

        internal bool IsKerberos
        {
            get
            {
                if (_lastProtocolName == null)
                {
                    _lastProtocolName = ProtocolName;
                }

                return (object)_lastProtocolName == (object)NegotiationInfoClass.Kerberos;
            }
        }

        internal bool IsNTLM
        {
            get
            {
                if (_lastProtocolName == null)
                {
                    _lastProtocolName = ProtocolName;
                }

                return (object)_lastProtocolName == (object)NegotiationInfoClass.NTLM;
            }
        }

        internal string ProtocolName
        {
            get
            {
                // Note: May return string.Empty if the auth is not done yet or failed.
                if (_protocolName == null)
                {
                    string negotiationAuthenticationPackage = null;

                    if (IsValidContext)
                    {
                        negotiationAuthenticationPackage = NegotiateStreamPal.QueryContextAuthenticationPackage(_securityContext);
                        if (IsCompleted)
                        {
                            _protocolName = negotiationAuthenticationPackage;
                        }
                    }
                    return negotiationAuthenticationPackage ?? string.Empty;
                }

                return _protocolName;
            }
        }

        //
        // This overload does not attempt to impersonate because the caller either did it already or the original thread context is still preserved.
        //
        internal NTAuthentication(bool isServer, string package, NetworkCredential credential, string spn, ContextFlagsPal requestedContextFlags, ChannelBinding channelBinding)
        {
            Initialize(isServer, package, credential, spn, requestedContextFlags, channelBinding);
        }

        private class InitializeCallbackContext
        {
            internal InitializeCallbackContext(NTAuthentication thisPtr, bool isServer, string package, NetworkCredential credential, string spn, ContextFlagsPal requestedContextFlags, ChannelBinding channelBinding)
            {
                ThisPtr = thisPtr;
                IsServer = isServer;
                Package = package;
                Credential = credential;
                Spn = spn;
                RequestedContextFlags = requestedContextFlags;
                ChannelBinding = channelBinding;
            }

            internal readonly NTAuthentication ThisPtr;
            internal readonly bool IsServer;
            internal readonly string Package;
            internal readonly NetworkCredential Credential;
            internal readonly string Spn;
            internal readonly ContextFlagsPal RequestedContextFlags;
            internal readonly ChannelBinding ChannelBinding;
        }

        private static void InitializeCallback(object state)
        {
            InitializeCallbackContext context = (InitializeCallbackContext)state;
            context.ThisPtr.Initialize(context.IsServer, context.Package, context.Credential, context.Spn, context.RequestedContextFlags, context.ChannelBinding);
        }

        private void Initialize(bool isServer, string package, NetworkCredential credential, string spn, ContextFlagsPal requestedContextFlags, ChannelBinding channelBinding)
        {
            if (GlobalLog.IsEnabled)
            {
                GlobalLog.Print("NTAuthentication#" + LoggingHash.HashString(this) + "::.ctor() package:" + LoggingHash.ObjectToString(package) + " spn:" + LoggingHash.ObjectToString(spn) + " flags :" + requestedContextFlags.ToString());
            }

            _tokenSize = NegotiateStreamPal.QueryMaxTokenSize(package);
            _isServer = isServer;
            _spn = spn;
            _securityContext = null;
            _requestedContextFlags = requestedContextFlags;
            _package = package;
            _channelBinding = channelBinding;

            if (GlobalLog.IsEnabled)
            {
                GlobalLog.Print("Peer SPN-> '" + _spn + "'");
            }

            //
            // Check if we're using DefaultCredentials.
            //

            Debug.Assert(CredentialCache.DefaultCredentials == CredentialCache.DefaultNetworkCredentials);
            if (credential == CredentialCache.DefaultCredentials)
            {
                if (GlobalLog.IsEnabled)
                {
                    GlobalLog.Print("NTAuthentication#" + LoggingHash.HashString(this) + "::.ctor(): using DefaultCredentials");
                }

                _credentialsHandle = NegotiateStreamPal.AcquireDefaultCredential(package, _isServer);
            }
            else
            {
                _credentialsHandle = NegotiateStreamPal.AcquireCredentialsHandle(package, _isServer, credential);
            }
        }

        internal SafeDeleteContext GetContext(out SecurityStatusPal status)
        {
            status = new SecurityStatusPal(SecurityStatusPalErrorCode.OK);
            if (!(IsCompleted && IsValidContext))
            {
                if (GlobalLog.IsEnabled)
                {
                    GlobalLog.AssertFormat("NTAuthentication#{0}::GetContextToken|Should be called only when completed with success, currently is not!", LoggingHash.HashString(this));
                }

                Debug.Fail("NTAuthentication#" + LoggingHash.HashString(this) + "::GetContextToken |Should be called only when completed with success, currently is not!");
            }

            if (!IsServer)
            {
                if (GlobalLog.IsEnabled)
                {
                    GlobalLog.AssertFormat("NTAuthentication#{0}::GetContextToken|The method must not be called by the client side!", LoggingHash.HashString(this));
                }

                Debug.Fail("NTAuthentication#" + LoggingHash.HashString(this) + "::GetContextToken |The method must not be called by the client side!");
            }

            if (!IsValidContext)
            {
                status = new SecurityStatusPal(SecurityStatusPalErrorCode.InvalidHandle);
                return null;
            }

            return _securityContext;
        }

        internal void CloseContext()
        {
            if (_securityContext != null && !_securityContext.IsClosed)
            {
                _securityContext.Dispose();
            }
        }

        // Accepts an incoming binary security blob and returns an outgoing binary security blob.
        internal byte[] GetOutgoingBlob(byte[] incomingBlob, bool throwOnError, out SecurityStatusPal statusCode)
        {
            if (GlobalLog.IsEnabled)
            {
                GlobalLog.Enter("NTAuthentication#" + LoggingHash.HashString(this) + "::GetOutgoingBlob", ((incomingBlob == null) ? "0" : incomingBlob.Length.ToString(NumberFormatInfo.InvariantInfo)) + " bytes");
            }

            var list = new List<SecurityBuffer>(2);

            if (incomingBlob != null)
            {
                list.Add(new SecurityBuffer(incomingBlob, SecurityBufferType.Token));
            }
            
            if (_channelBinding != null)
            {
                list.Add(new SecurityBuffer(_channelBinding));
            }

            SecurityBuffer[] inSecurityBufferArray = null;
            if (list.Count > 0)
            {
                inSecurityBufferArray = list.ToArray();
            }

            var outSecurityBuffer = new SecurityBuffer(_tokenSize, SecurityBufferType.Token);

            bool firstTime = _securityContext == null;
            try
            {
                if (!_isServer)
                {
                    // client session
                    statusCode = NegotiateStreamPal.InitializeSecurityContext(
                        _credentialsHandle,
                        ref _securityContext,
                        _spn,
                        _requestedContextFlags,
                        inSecurityBufferArray,
                        outSecurityBuffer,
                        ref _contextFlags);

                    if (GlobalLog.IsEnabled)
                    {
                        GlobalLog.Print("NTAuthentication#" + LoggingHash.HashString(this) + "::GetOutgoingBlob() SSPIWrapper.InitializeSecurityContext() returns statusCode:0x" + ((int)statusCode.ErrorCode).ToString("x8", NumberFormatInfo.InvariantInfo) + " (" + statusCode.ToString() + ")");
                    }

                    if (statusCode.ErrorCode == SecurityStatusPalErrorCode.CompleteNeeded)
                    {
                        var inSecurityBuffers = new SecurityBuffer[1];
                        inSecurityBuffers[0] = outSecurityBuffer;

                        statusCode = NegotiateStreamPal.CompleteAuthToken(ref _securityContext, inSecurityBuffers);

                        if (GlobalLog.IsEnabled)
                        {
                            GlobalLog.Print("NTAuthentication#" + LoggingHash.HashString(this) + "::GetOutgoingDigestBlob() SSPIWrapper.CompleteAuthToken() returns statusCode:0x" + ((int)statusCode.ErrorCode).ToString("x8", NumberFormatInfo.InvariantInfo) + " (" + statusCode.ToString() + ")");
                        }

                        outSecurityBuffer.token = null;
                    }
                }
                else
                {
                    // Server session.
                    statusCode = NegotiateStreamPal.AcceptSecurityContext(
                        _credentialsHandle,
                        ref _securityContext,
                        _requestedContextFlags,
                        inSecurityBufferArray,
                        outSecurityBuffer,
                        ref _contextFlags);

                    if (GlobalLog.IsEnabled)
                    {
                        GlobalLog.Print("NTAuthentication#" + LoggingHash.HashString(this) + "::GetOutgoingBlob() SSPIWrapper.AcceptSecurityContext() returns statusCode:0x" + ((int)statusCode.ErrorCode).ToString("x8", NumberFormatInfo.InvariantInfo) + " (" + statusCode.ToString() + ")");
                    }
                }
            }
            finally
            {
                //
                // Assuming the ISC or ASC has referenced the credential on the first successful call,
                // we want to decrement the effective ref count by "disposing" it.
                // The real dispose will happen when the security context is closed.
                // Note if the first call was not successful the handle is physically destroyed here.
                //
                if (firstTime && _credentialsHandle != null)
                {
                    _credentialsHandle.Dispose();
                }
            }


            if (NegoState.IsError(statusCode))
            {
                CloseContext();
                _isCompleted = true;
                if (throwOnError)
                {
                    Exception exception = NegotiateStreamPal.CreateExceptionFromError(statusCode);
                    if (GlobalLog.IsEnabled)
                    {
                        GlobalLog.Leave("NTAuthentication#" + LoggingHash.HashString(this) + "::GetOutgoingBlob", "Win32Exception:" + exception);
                    }
                    throw exception;
                }

                if (GlobalLog.IsEnabled)
                {
                    GlobalLog.Leave("NTAuthentication#" + LoggingHash.HashString(this) + "::GetOutgoingBlob", "null statusCode:0x" + ((int)statusCode.ErrorCode).ToString("x8", NumberFormatInfo.InvariantInfo) + " (" + statusCode.ToString() + ")");
                }
                return null;
            }
            else if (firstTime && _credentialsHandle != null)
            {
                // Cache until it is pushed out by newly incoming handles.
                SSPIHandleCache.CacheCredential(_credentialsHandle);
            }

            // The return value will tell us correctly if the handshake is over or not
            if (statusCode.ErrorCode == SecurityStatusPalErrorCode.OK)
            {
                // Success.
                _isCompleted = true;
            }
            else if (GlobalLog.IsEnabled)
            {
                // We need to continue.
                GlobalLog.Print("NTAuthentication#" + LoggingHash.HashString(this) + "::GetOutgoingBlob() need continue statusCode:[0x" + ((int)statusCode.ErrorCode).ToString("x8", NumberFormatInfo.InvariantInfo) + "] (" + statusCode.ToString() + ") m_SecurityContext#" + LoggingHash.HashString(_securityContext) + "::Handle:" + LoggingHash.ObjectToString(_securityContext) + "]");
            }

            if (GlobalLog.IsEnabled)
            {
                GlobalLog.Leave("NTAuthentication#" + LoggingHash.HashString(this) + "::GetOutgoingBlob", "IsCompleted:" + IsCompleted.ToString());
            }

            return outSecurityBuffer.token;
        }

        internal int Encrypt(byte[] buffer, int offset, int count, ref byte[] output, uint sequenceNumber)
        {
            return NegotiateStreamPal.Encrypt(
                _securityContext,
                buffer,
                offset,
                count,
                IsConfidentialityFlag,
                IsNTLM,
                ref output,
                sequenceNumber);
        }

        internal int Decrypt(byte[] payload, int offset, int count, out int newOffset, uint expectedSeqNumber)
        {
            return NegotiateStreamPal.Decrypt(_securityContext, payload, offset, count, IsConfidentialityFlag, IsNTLM, out newOffset, expectedSeqNumber);
        }

        private string GetClientSpecifiedSpn()
        {
            if (!(IsValidContext && IsCompleted))
            {
                if (GlobalLog.IsEnabled)
                {
                    GlobalLog.Assert("NTAuthentication: Trying to get the client SPN before handshaking is done!");
                }

                Debug.Fail("NTAuthentication: Trying to get the client SPN before handshaking is done!");
            }

            string spn = NegotiateStreamPal.QueryContextClientSpecifiedSpn(_securityContext);

            if (GlobalLog.IsEnabled)
            {
                GlobalLog.Print("NTAuthentication: The client specified SPN is [" + spn + "]");
            }
            return spn;
        }

        private int DecryptNtlm(byte[] payload, int offset, int count, out int newOffset, uint expectedSeqNumber)
        {
            return NegotiateStreamPal.DecryptNtlm(_securityContext, payload, offset, count, IsConfidentialityFlag, out newOffset, expectedSeqNumber);
        }
    }
}
