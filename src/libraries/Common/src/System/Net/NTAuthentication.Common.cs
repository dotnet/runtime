// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Authentication.ExtendedProtection;

namespace System.Net
{
    internal sealed partial class NTAuthentication
    {
        private bool _isServer;

        private SafeFreeCredentials? _credentialsHandle;
        private SafeDeleteContext? _securityContext;
        private string? _spn;

        private int _tokenSize;
        private ContextFlagsPal _requestedContextFlags;
        private ContextFlagsPal _contextFlags;

        private bool _isCompleted;
        private string _package;
        private string? _lastProtocolName;
        private string? _protocolName;
        private string? _clientSpecifiedSpn;

        private ChannelBinding? _channelBinding;

        // If set, no more calls should be made.
        internal bool IsCompleted => _isCompleted;
        internal bool IsValidContext => !(_securityContext == null || _securityContext.IsInvalid);
        internal string Package => _package;

        // True indicates this instance is for Server and will use AcceptSecurityContext SSPI API.
        internal bool IsServer => _isServer;

        internal string? ClientSpecifiedSpn
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

        internal string ProtocolName
        {
            get
            {
                // Note: May return string.Empty if the auth is not done yet or failed.
                if (_protocolName == null)
                {
                    string? negotiationAuthenticationPackage = null;

                    if (IsValidContext)
                    {
                        negotiationAuthenticationPackage = NegotiateStreamPal.QueryContextAuthenticationPackage(_securityContext!);
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

        //
        // This overload does not attempt to impersonate because the caller either did it already or the original thread context is still preserved.
        //
        internal NTAuthentication(bool isServer, string package, NetworkCredential credential, string? spn, ContextFlagsPal requestedContextFlags, ChannelBinding? channelBinding)
        {
            Initialize(isServer, package, credential, spn, requestedContextFlags, channelBinding);
        }

        [MemberNotNull(nameof(_package))]
        private void Initialize(bool isServer, string package, NetworkCredential credential, string? spn, ContextFlagsPal requestedContextFlags, ChannelBinding? channelBinding)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"package={package}, spn={spn}, requestedContextFlags={requestedContextFlags}");

            _tokenSize = NegotiateStreamPal.QueryMaxTokenSize(package);
            _isServer = isServer;
            _spn = spn;
            _securityContext = null;
            _requestedContextFlags = requestedContextFlags;
            _package = package;
            _channelBinding = channelBinding;

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Peer SPN-> '{_spn}'");

            //
            // Check if we're using DefaultCredentials.
            //

            Debug.Assert(CredentialCache.DefaultCredentials == CredentialCache.DefaultNetworkCredentials);
            if (credential == CredentialCache.DefaultCredentials)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "using DefaultCredentials");
                _credentialsHandle = NegotiateStreamPal.AcquireDefaultCredential(package, _isServer);
            }
            else
            {
                _credentialsHandle = NegotiateStreamPal.AcquireCredentialsHandle(package, _isServer, credential);
            }
        }

        internal SafeDeleteContext? GetContext(out SecurityStatusPal status)
        {
            status = new SecurityStatusPal(SecurityStatusPalErrorCode.OK);
            Debug.Assert(IsCompleted && IsValidContext, "Should be called only when completed with success, currently is not!");
            Debug.Assert(IsServer, "The method must not be called by the client side!");

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

        internal int VerifySignature(byte[] buffer, int offset, int count)
        {
            return NegotiateStreamPal.VerifySignature(_securityContext!, buffer, offset, count);
        }

        internal int MakeSignature(byte[] buffer, int offset, int count, [AllowNull] ref byte[] output)
        {
            return NegotiateStreamPal.MakeSignature(_securityContext!, buffer, offset, count, ref output);
        }

        internal string? GetOutgoingBlob(string? incomingBlob)
        {
            byte[]? decodedIncomingBlob = null;
            if (incomingBlob != null && incomingBlob.Length > 0)
            {
                decodedIncomingBlob = Convert.FromBase64String(incomingBlob);
            }
            byte[]? decodedOutgoingBlob = null;

            if ((IsValidContext || IsCompleted) && decodedIncomingBlob == null)
            {
                // we tried auth previously, now we got a null blob, we're done. this happens
                // with Kerberos & valid credentials on the domain but no ACLs on the resource
                _isCompleted = true;
            }
            else
            {
                SecurityStatusPal statusCode;
                decodedOutgoingBlob = GetOutgoingBlob(decodedIncomingBlob, true, out statusCode);
            }

            string? outgoingBlob = null;
            if (decodedOutgoingBlob != null && decodedOutgoingBlob.Length > 0)
            {
                outgoingBlob = Convert.ToBase64String(decodedOutgoingBlob);
            }

            if (IsCompleted)
            {
                CloseContext();
            }

            return outgoingBlob;
        }

        internal byte[]? GetOutgoingBlob(byte[]? incomingBlob, bool thrownOnError)
        {
            SecurityStatusPal statusCode;
            return GetOutgoingBlob(incomingBlob, thrownOnError, out statusCode);
        }

        // Accepts an incoming binary security blob and returns an outgoing binary security blob.
        internal byte[]? GetOutgoingBlob(byte[]? incomingBlob, bool throwOnError, out SecurityStatusPal statusCode)
        {
            byte[]? result = new byte[_tokenSize];

            bool firstTime = _securityContext == null;
            try
            {
                if (!_isServer)
                {
                    // client session
                    statusCode = NegotiateStreamPal.InitializeSecurityContext(
                        ref _credentialsHandle!,
                        ref _securityContext,
                        _spn,
                        _requestedContextFlags,
                        incomingBlob,
                        _channelBinding,
                        ref result,
                        ref _contextFlags);

                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"SSPIWrapper.InitializeSecurityContext() returns statusCode:0x{((int)statusCode.ErrorCode):x8} ({statusCode})");

                    if (statusCode.ErrorCode == SecurityStatusPalErrorCode.CompleteNeeded)
                    {
                        statusCode = NegotiateStreamPal.CompleteAuthToken(ref _securityContext, result);

                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"SSPIWrapper.CompleteAuthToken() returns statusCode:0x{((int)statusCode.ErrorCode):x8} ({statusCode})");

                        result = null;
                    }
                }
                else
                {
                    // Server session.
                    statusCode = NegotiateStreamPal.AcceptSecurityContext(
                        _credentialsHandle,
                        ref _securityContext,
                        _requestedContextFlags,
                        incomingBlob,
                        _channelBinding,
                        ref result,
                        ref _contextFlags);

                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"SSPIWrapper.AcceptSecurityContext() returns statusCode:0x{((int)statusCode.ErrorCode):x8} ({statusCode})");
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
                if (firstTime)
                {
                    _credentialsHandle?.Dispose();
                }
            }


            if (((int)statusCode.ErrorCode >= (int)SecurityStatusPalErrorCode.OutOfMemory))
            {
                CloseContext();
                _isCompleted = true;
                if (throwOnError)
                {
                    throw NegotiateStreamPal.CreateExceptionFromError(statusCode);
                }

                return null;
            }
            else if (firstTime && _credentialsHandle != null)
            {
                // Cache until it is pushed out by newly incoming handles.
                SSPIHandleCache.CacheCredential(_credentialsHandle);
            }

            // The return value will tell us correctly if the handshake is over or not
            if (statusCode.ErrorCode == SecurityStatusPalErrorCode.OK
                || (_isServer && statusCode.ErrorCode == SecurityStatusPalErrorCode.CompleteNeeded))
            {
                // Success.
                _isCompleted = true;
            }
            else
            {
                // We need to continue.
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"need continue statusCode:0x{((int)statusCode.ErrorCode):x8} ({statusCode}) _securityContext:{_securityContext}");
            }

            return result;
        }

        private string? GetClientSpecifiedSpn()
        {
            Debug.Assert(IsValidContext && IsCompleted, "Trying to get the client SPN before handshaking is done!");

            string? spn = NegotiateStreamPal.QueryContextClientSpecifiedSpn(_securityContext!);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"The client specified SPN is [{spn}]");

            return spn;
        }
    }
}
