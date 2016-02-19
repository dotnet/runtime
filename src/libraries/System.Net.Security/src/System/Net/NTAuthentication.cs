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
        private Interop.SspiCli.ContextFlags _requestedContextFlags;
        private Interop.SspiCli.ContextFlags _contextFlags;

        private bool _isCompleted;
        private string _protocolName;
        private SecSizes _sizes;
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
                    throw new Win32Exception((int)Interop.SecurityStatus.InvalidHandle);
                }

                string name = SSPIWrapper.QueryContextAttributes(GlobalSSPI.SSPIAuth, _securityContext, Interop.SspiCli.ContextAttribute.Names) as string;
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
                return (_contextFlags & Interop.SspiCli.ContextFlags.Confidentiality) != 0;
            }
        }

        internal bool IsIntegrityFlag
        {
            get
            {
                return (_contextFlags & (_isServer ? Interop.SspiCli.ContextFlags.AcceptIntegrity : Interop.SspiCli.ContextFlags.InitIntegrity)) != 0;
            }
        }

        internal bool IsMutualAuthFlag
        {
            get
            {
                return (_contextFlags & Interop.SspiCli.ContextFlags.MutualAuth) != 0;
            }
        }

        internal bool IsDelegationFlag
        {
            get
            {
                return (_contextFlags & Interop.SspiCli.ContextFlags.Delegate) != 0;
            }
        }

        internal bool IsIdentifyFlag
        {
            get
            {
                return (_contextFlags & (_isServer ? Interop.SspiCli.ContextFlags.AcceptIdentify : Interop.SspiCli.ContextFlags.InitIdentify)) != 0;
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
                    NegotiationInfoClass negotiationInfo = null;

                    if (IsValidContext)
                    {
                        negotiationInfo = SSPIWrapper.QueryContextAttributes(GlobalSSPI.SSPIAuth, _securityContext, Interop.SspiCli.ContextAttribute.NegotiationInfo) as NegotiationInfoClass;
                        if (IsCompleted)
                        {
                            if (negotiationInfo != null)
                            {
                                // Cache it only when it's completed.
                                _protocolName = negotiationInfo.AuthenticationPackage;
                            }
                        }
                    }

                    return negotiationInfo == null ? string.Empty : negotiationInfo.AuthenticationPackage;
                }

                return _protocolName;
            }
        }

        internal SecSizes Sizes
        {
            get
            {
                if (!(IsCompleted && IsValidContext))
                {
                    if (GlobalLog.IsEnabled)
                    {
                        GlobalLog.AssertFormat("NTAuthentication#{0}::MaxDataSize|The context is not completed or invalid.", LoggingHash.HashString(this));
                    }

                    Debug.Fail("NTAuthentication#" + LoggingHash.HashString(this) + "::MaxDataSize |The context is not completed or invalid.");
                }

                if (_sizes == null)
                {
                    _sizes = SSPIWrapper.QueryContextAttributes(
                                  GlobalSSPI.SSPIAuth,
                                  _securityContext,
                                  Interop.SspiCli.ContextAttribute.Sizes
                                  ) as SecSizes;
                }

                return _sizes;
            }
        }

        //
        // This overload does not attempt to impersonate because the caller either did it already or the original thread context is still preserved.
        //
        internal NTAuthentication(bool isServer, string package, NetworkCredential credential, string spn, Interop.SspiCli.ContextFlags requestedContextFlags, ChannelBinding channelBinding)
        {
            Initialize(isServer, package, credential, spn, requestedContextFlags, channelBinding);
        }

        private class InitializeCallbackContext
        {
            internal InitializeCallbackContext(NTAuthentication thisPtr, bool isServer, string package, NetworkCredential credential, string spn, Interop.SspiCli.ContextFlags requestedContextFlags, ChannelBinding channelBinding)
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
            internal readonly Interop.SspiCli.ContextFlags RequestedContextFlags;
            internal readonly ChannelBinding ChannelBinding;
        }

        private static void InitializeCallback(object state)
        {
            InitializeCallbackContext context = (InitializeCallbackContext)state;
            context.ThisPtr.Initialize(context.IsServer, context.Package, context.Credential, context.Spn, context.RequestedContextFlags, context.ChannelBinding);
        }

        private void Initialize(bool isServer, string package, NetworkCredential credential, string spn, Interop.SspiCli.ContextFlags requestedContextFlags, ChannelBinding channelBinding)
        {
            if (GlobalLog.IsEnabled)
            {
                GlobalLog.Print("NTAuthentication#" + LoggingHash.HashString(this) + "::.ctor() package:" + LoggingHash.ObjectToString(package) + " spn:" + LoggingHash.ObjectToString(spn) + " flags :" + requestedContextFlags.ToString());
            }

            _tokenSize = SSPIWrapper.GetVerifyPackageInfo(GlobalSSPI.SSPIAuth, package, true).MaxToken;
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

                _credentialsHandle = SSPIWrapper.AcquireDefaultCredential(
                    GlobalSSPI.SSPIAuth,
                    package,
                    (_isServer ? Interop.SspiCli.CredentialUse.Inbound : Interop.SspiCli.CredentialUse.Outbound));
            }
            else
            {
                unsafe
                {
                    SafeSspiAuthDataHandle authData = null;
                    try
                    {
                        Interop.SecurityStatus result = Interop.SspiCli.SspiEncodeStringsAsAuthIdentity(
                            credential.UserName, credential.Domain,
                            credential.Password, out authData);

                        if (result != Interop.SecurityStatus.OK)
                        {
                            if (NetEventSource.Log.IsEnabled())
                            {
                                NetEventSource.PrintError(
                                    NetEventSource.ComponentType.Security, 
                                    SR.Format(
                                        SR.net_log_operation_failed_with_error, 
                                        "SspiEncodeStringsAsAuthIdentity()", 
                                        String.Format(CultureInfo.CurrentCulture, "0x{0:X}", (int)result)));
                            }

                            throw new Win32Exception((int)result);
                        }

                        _credentialsHandle = SSPIWrapper.AcquireCredentialsHandle(GlobalSSPI.SSPIAuth,
                            package, (_isServer ? Interop.SspiCli.CredentialUse.Inbound : Interop.SspiCli.CredentialUse.Outbound), ref authData);
                    }
                    finally
                    {
                        if (authData != null)
                        {
                            authData.Dispose();
                        }
                    }
                }
            }
        }

        // This will return a client token when conducted authentication on server side.
        // This token can be used for impersonation. We use it to create a WindowsIdentity and hand it out to the server app.
        internal SecurityContextTokenHandle GetContextToken(out Interop.SecurityStatus status)
        {
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
                throw new Win32Exception((int)Interop.SecurityStatus.InvalidHandle);
            }

            SecurityContextTokenHandle token = null;
            status = (Interop.SecurityStatus)SSPIWrapper.QuerySecurityContextToken(
                GlobalSSPI.SSPIAuth,
                _securityContext,
                out token);

            return token;
        }

        internal SecurityContextTokenHandle GetContextToken()
        {
            Interop.SecurityStatus status;
            SecurityContextTokenHandle token = GetContextToken(out status);
            if (status != Interop.SecurityStatus.OK)
            {
                throw new Win32Exception((int)status);
            }

            return token;
        }

        internal void CloseContext()
        {
            if (_securityContext != null && !_securityContext.IsClosed)
            {
                _securityContext.Dispose();
            }
        }

        // Accepts an incoming binary security blob and returns an outgoing binary security blob.
        internal byte[] GetOutgoingBlob(byte[] incomingBlob, bool throwOnError, out Interop.SecurityStatus statusCode)
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
                    statusCode = (Interop.SecurityStatus)SSPIWrapper.InitializeSecurityContext(
                        GlobalSSPI.SSPIAuth,
                        _credentialsHandle,
                        ref _securityContext,
                        _spn,
                        _requestedContextFlags,
                        Interop.SspiCli.Endianness.Network,
                        inSecurityBufferArray,
                        outSecurityBuffer,
                        ref _contextFlags);

                    if (GlobalLog.IsEnabled)
                    {
                        GlobalLog.Print("NTAuthentication#" + LoggingHash.HashString(this) + "::GetOutgoingBlob() SSPIWrapper.InitializeSecurityContext() returns statusCode:0x" + ((int)statusCode).ToString("x8", NumberFormatInfo.InvariantInfo) + " (" + statusCode.ToString() + ")");
                    }

                    if (statusCode == Interop.SecurityStatus.CompleteNeeded)
                    {
                        var inSecurityBuffers = new SecurityBuffer[1];
                        inSecurityBuffers[0] = outSecurityBuffer;

                        statusCode = (Interop.SecurityStatus)SSPIWrapper.CompleteAuthToken(
                            GlobalSSPI.SSPIAuth,
                            ref _securityContext,
                            inSecurityBuffers);

                        if (GlobalLog.IsEnabled)
                        {
                            GlobalLog.Print("NTAuthentication#" + LoggingHash.HashString(this) + "::GetOutgoingDigestBlob() SSPIWrapper.CompleteAuthToken() returns statusCode:0x" + ((int)statusCode).ToString("x8", NumberFormatInfo.InvariantInfo) + " (" + statusCode.ToString() + ")");
                        }

                        outSecurityBuffer.token = null;
                    }
                }
                else
                {
                    // Server session.
                    statusCode = (Interop.SecurityStatus)SSPIWrapper.AcceptSecurityContext(
                        GlobalSSPI.SSPIAuth,
                        _credentialsHandle,
                        ref _securityContext,
                        _requestedContextFlags,
                        Interop.SspiCli.Endianness.Network,
                        inSecurityBufferArray,
                        outSecurityBuffer,
                        ref _contextFlags);

                    if (GlobalLog.IsEnabled)
                    {
                        GlobalLog.Print("NTAuthentication#" + LoggingHash.HashString(this) + "::GetOutgoingBlob() SSPIWrapper.AcceptSecurityContext() returns statusCode:0x" + ((int)statusCode).ToString("x8", NumberFormatInfo.InvariantInfo) + " (" + statusCode.ToString() + ")");
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


            if (((int)statusCode & unchecked((int)0x80000000)) != 0)
            {
                CloseContext();
                _isCompleted = true;
                if (throwOnError)
                {
                    var exception = new Win32Exception((int)statusCode);
                    if (GlobalLog.IsEnabled)
                    {
                        GlobalLog.Leave("NTAuthentication#" + LoggingHash.HashString(this) + "::GetOutgoingBlob", "Win32Exception:" + exception);
                    }
                    throw exception;
                }

                if (GlobalLog.IsEnabled)
                {
                    GlobalLog.Leave("NTAuthentication#" + LoggingHash.HashString(this) + "::GetOutgoingBlob", "null statusCode:0x" + ((int)statusCode).ToString("x8", NumberFormatInfo.InvariantInfo) + " (" + statusCode.ToString() + ")");
                }
                return null;
            }
            else if (firstTime && _credentialsHandle != null)
            {
                // Cache until it is pushed out by newly incoming handles.
                SSPIHandleCache.CacheCredential(_credentialsHandle);
            }

            // The return value from SSPI will tell us correctly if the
            // handshake is over or not: http://msdn.microsoft.com/library/psdk/secspi/sspiref_67p0.htm
            // we also have to consider the case in which SSPI formed a new context, in this case we're done as well.
            if (statusCode == Interop.SecurityStatus.OK)
            {
                // Success.
                if (statusCode != Interop.SecurityStatus.OK)
                {
                    if (GlobalLog.IsEnabled)
                    {
                        GlobalLog.AssertFormat("NTAuthentication#{0}::GetOutgoingBlob()|statusCode:[0x{1:x8}] ({2}) m_SecurityContext#{3}::Handle:[{4}] [STATUS != OK]", LoggingHash.HashString(this), (int)statusCode, statusCode, LoggingHash.HashString(_securityContext), LoggingHash.ObjectToString(_securityContext));
                    }

                    Debug.Fail("NTAuthentication#" + LoggingHash.HashString(this) + "::GetOutgoingBlob()|statusCode:[0x" + ((int)statusCode).ToString("x8") + "] (" + statusCode + ") m_SecurityContext#" + LoggingHash.HashString(_securityContext) + "::Handle:[" + LoggingHash.ObjectToString(_securityContext) + "] [STATUS != OK]");
                }

                _isCompleted = true;
            }
            else if (GlobalLog.IsEnabled)
            {
                // We need to continue.
                GlobalLog.Print("NTAuthentication#" + LoggingHash.HashString(this) + "::GetOutgoingBlob() need continue statusCode:[0x" + ((int)statusCode).ToString("x8", NumberFormatInfo.InvariantInfo) + "] (" + statusCode.ToString() + ") m_SecurityContext#" + LoggingHash.HashString(_securityContext) + "::Handle:" + LoggingHash.ObjectToString(_securityContext) + "]");
            }

            if (GlobalLog.IsEnabled)
            {
                GlobalLog.Leave("NTAuthentication#" + LoggingHash.HashString(this) + "::GetOutgoingBlob", "IsCompleted:" + IsCompleted.ToString());
            }

            return outSecurityBuffer.token;
        }

        internal int Encrypt(byte[] buffer, int offset, int count, ref byte[] output, uint sequenceNumber)
        {
            SecSizes sizes = Sizes;

            try
            {
                int maxCount = checked(Int32.MaxValue - 4 - sizes.BlockSize - sizes.SecurityTrailer);

                if (count > maxCount || count < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(count), SR.Format(SR.net_io_out_range, maxCount));
                }
            }
            catch (Exception e)
            {
                if (!ExceptionCheck.IsFatal(e))
                {
                    if (GlobalLog.IsEnabled)
                    {
                        GlobalLog.Assert("NTAuthentication#" + LoggingHash.HashString(this) + "::Encrypt", "Arguments out of range.");
                    }

                    Debug.Fail("NTAuthentication#" + LoggingHash.HashString(this) + "::Encrypt", "Arguments out of range.");
                }

                throw;
            }

            int resultSize = count + sizes.SecurityTrailer + sizes.BlockSize;
            if (output == null || output.Length < resultSize + 4)
            {
                output = new byte[resultSize + 4];
            }

            // Make a copy of user data for in-place encryption.
            Buffer.BlockCopy(buffer, offset, output, 4 + sizes.SecurityTrailer, count);

            // Prepare buffers TOKEN(signature), DATA and Padding.
            var securityBuffer = new SecurityBuffer[3];
            securityBuffer[0] = new SecurityBuffer(output, 4, sizes.SecurityTrailer, SecurityBufferType.Token);
            securityBuffer[1] = new SecurityBuffer(output, 4 + sizes.SecurityTrailer, count, SecurityBufferType.Data);
            securityBuffer[2] = new SecurityBuffer(output, 4 + sizes.SecurityTrailer + count, sizes.BlockSize, SecurityBufferType.Padding);

            int errorCode;
            if (IsConfidentialityFlag)
            {
                errorCode = SSPIWrapper.EncryptMessage(GlobalSSPI.SSPIAuth, _securityContext, securityBuffer, sequenceNumber);
            }
            else
            {
                if (IsNTLM)
                {
                    securityBuffer[1].type |= SecurityBufferType.ReadOnlyFlag;
                }

                errorCode = SSPIWrapper.MakeSignature(GlobalSSPI.SSPIAuth, _securityContext, securityBuffer, 0);
            }

            if (errorCode != 0)
            {
                if (GlobalLog.IsEnabled)
                {
                    GlobalLog.Print("NTAuthentication#" + LoggingHash.HashString(this) + "::Encrypt() throw Error = " + errorCode.ToString("x", NumberFormatInfo.InvariantInfo));
                }
                throw new Win32Exception(errorCode);
            }

            // Compacting the result.
            resultSize = securityBuffer[0].size;
            bool forceCopy = false;
            if (resultSize != sizes.SecurityTrailer)
            {
                forceCopy = true;
                Buffer.BlockCopy(output, securityBuffer[1].offset, output, 4 + resultSize, securityBuffer[1].size);
            }

            resultSize += securityBuffer[1].size;
            if (securityBuffer[2].size != 0 && (forceCopy || resultSize != (count + sizes.SecurityTrailer)))
            {
                Buffer.BlockCopy(output, securityBuffer[2].offset, output, 4 + resultSize, securityBuffer[2].size);
            }

            resultSize += securityBuffer[2].size;

            unchecked
            {
                output[0] = (byte)((resultSize) & 0xFF);
                output[1] = (byte)(((resultSize) >> 8) & 0xFF);
                output[2] = (byte)(((resultSize) >> 16) & 0xFF);
                output[3] = (byte)(((resultSize) >> 24) & 0xFF);
            }
            
            return resultSize + 4;
        }

        internal int Decrypt(byte[] payload, int offset, int count, out int newOffset, uint expectedSeqNumber)
        {
            if (offset < 0 || offset > (payload == null ? 0 : payload.Length))
            {
                if (GlobalLog.IsEnabled)
                {
                    GlobalLog.Assert("NTAuthentication#" + LoggingHash.HashString(this) + "::Decrypt", "Argument 'offset' out of range.");
                }

                Debug.Fail("NTAuthentication#" + LoggingHash.HashString(this) + "::Decrypt", "Argument 'offset' out of range.");

                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            
            if (count < 0 || count > (payload == null ? 0 : payload.Length - offset))
            {
                if (GlobalLog.IsEnabled)
                {
                    GlobalLog.Assert("NTAuthentication#" + LoggingHash.HashString(this) + "::Decrypt", "Argument 'count' out of range.");
                }

                Debug.Fail("NTAuthentication#" + LoggingHash.HashString(this) + "::Decrypt", "Argument 'count' out of range.");

                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (IsNTLM)
            {
                return DecryptNtlm(payload, offset, count, out newOffset, expectedSeqNumber);
            }

            //
            // Kerberos and up
            //
            var securityBuffer = new SecurityBuffer[2];
            securityBuffer[0] = new SecurityBuffer(payload, offset, count, SecurityBufferType.Stream);
            securityBuffer[1] = new SecurityBuffer(0, SecurityBufferType.Data);

            int errorCode;
            if (IsConfidentialityFlag)
            {
                errorCode = SSPIWrapper.DecryptMessage(GlobalSSPI.SSPIAuth, _securityContext, securityBuffer, expectedSeqNumber);
            }
            else
            {
                errorCode = SSPIWrapper.VerifySignature(GlobalSSPI.SSPIAuth, _securityContext, securityBuffer, expectedSeqNumber);
            }

            if (errorCode != 0)
            {
                if (GlobalLog.IsEnabled)
                {
                    GlobalLog.Print("NTAuthentication#" + LoggingHash.HashString(this) + "::Decrypt() throw Error = " + errorCode.ToString("x", NumberFormatInfo.InvariantInfo));
                }
                throw new Win32Exception(errorCode);
            }

            if (securityBuffer[1].type != SecurityBufferType.Data)
            {
                throw new InternalException();
            }

            newOffset = securityBuffer[1].offset;
            return securityBuffer[1].size;
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

            string spn = SSPIWrapper.QueryContextAttributes(GlobalSSPI.SSPIAuth, _securityContext,
                Interop.SspiCli.ContextAttribute.ClientSpecifiedSpn) as string;

            if (GlobalLog.IsEnabled)
            {
                GlobalLog.Print("NTAuthentication: The client specified SPN is [" + spn + "]");
            }
            return spn;
        }

        private int DecryptNtlm(byte[] payload, int offset, int count, out int newOffset, uint expectedSeqNumber)
        {
            // For the most part the arguments are verified in Encrypt().
            if (count < 16)
            {
                if (GlobalLog.IsEnabled)
                {
                    GlobalLog.Assert("NTAuthentication#" + LoggingHash.HashString(this) + "::DecryptNtlm", "Argument 'count' out of range.");
                }

                Debug.Fail("NTAuthentication#" + LoggingHash.HashString(this) + "::DecryptNtlm", "Argument 'count' out of range.");

                throw new ArgumentOutOfRangeException(nameof(count));
            }

            var securityBuffer = new SecurityBuffer[2];
            securityBuffer[0] = new SecurityBuffer(payload, offset, 16, SecurityBufferType.Token);
            securityBuffer[1] = new SecurityBuffer(payload, offset + 16, count - 16, SecurityBufferType.Data);

            int errorCode;
            SecurityBufferType realDataType = SecurityBufferType.Data;

            if (IsConfidentialityFlag)
            {
                errorCode = SSPIWrapper.DecryptMessage(GlobalSSPI.SSPIAuth, _securityContext, securityBuffer, expectedSeqNumber);
            }
            else
            {
                realDataType |= SecurityBufferType.ReadOnlyFlag;
                securityBuffer[1].type = realDataType;
                errorCode = SSPIWrapper.VerifySignature(GlobalSSPI.SSPIAuth, _securityContext, securityBuffer, expectedSeqNumber);
            }

            if (errorCode != 0)
            {
                if (GlobalLog.IsEnabled)
                {
                    GlobalLog.Print("NTAuthentication#" + LoggingHash.HashString(this) + "::Decrypt() throw Error = " + errorCode.ToString("x", NumberFormatInfo.InvariantInfo));
                }

                throw new Win32Exception(errorCode);
            }

            if (securityBuffer[1].type != realDataType)
            {
                throw new InternalException();
            }

            newOffset = securityBuffer[1].offset;
            return securityBuffer[1].size;
        }
    }
}
