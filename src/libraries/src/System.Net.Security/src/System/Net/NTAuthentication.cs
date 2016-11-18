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
    internal partial class NTAuthentication
    {
        static private ContextCallback s_InitializeCallback = new ContextCallback(InitializeCallback);
        private string _clientSpecifiedSpn;
        private string _protocolName;
        private string _lastProtocolName;

        internal string AssociatedName
        {
            get
            {
                if (!(IsValidContext && IsCompleted))
                {
                    throw new Win32Exception((int)SecurityStatusPalErrorCode.InvalidHandle);
                }

                string name = NegotiateStreamPal.QueryContextAssociatedName(_securityContext);
                if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"NTAuthentication: The context is associated with [{name}]");
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

        internal SafeDeleteContext GetContext(out SecurityStatusPal status)
        {
            status = new SecurityStatusPal(SecurityStatusPalErrorCode.OK);
            if (!(IsCompleted && IsValidContext))
            {
                NetEventSource.Fail(this, "Should be called only when completed with success, currently is not!");
            }

            if (!IsServer)
            {
                NetEventSource.Fail(this, "The method must not be called by the client side!");
            }

            if (!IsValidContext)
            {
                status = new SecurityStatusPal(SecurityStatusPalErrorCode.InvalidHandle);
                return null;
            }

            return _securityContext;
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
                NetEventSource.Fail(this, "Trying to get the client SPN before handshaking is done!");
            }

            string spn = NegotiateStreamPal.QueryContextClientSpecifiedSpn(_securityContext);

            if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"The client specified SPN is [{spn}]");

            return spn;
        }
    }
}
