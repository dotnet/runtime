// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
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
        private byte[]? _tokenBuffer;
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

        internal string? ClientSpecifiedSpn => _clientSpecifiedSpn ??= GetClientSpecifiedSpn();

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
                _lastProtocolName ??= ProtocolName;
                return _lastProtocolName == NegotiationInfoClass.Kerberos;
            }
        }

        internal bool IsNTLM
        {
            get
            {
                _lastProtocolName ??= ProtocolName;
                return _lastProtocolName == NegotiationInfoClass.NTLM;
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
            _isCompleted = false;
        }

        internal NegotiateAuthenticationStatusCode Wrap(ReadOnlySpan<byte> input, IBufferWriter<byte> outputWriter, bool requestEncryption, out bool isEncrypted)
        {
            return NegotiateStreamPal.Wrap(_securityContext!, input, outputWriter, requestEncryption, out isEncrypted);
        }

        internal NegotiateAuthenticationStatusCode Unwrap(ReadOnlySpan<byte> input, IBufferWriter<byte> outputWriter, out bool wasEncrypted)
        {
            return NegotiateStreamPal.Unwrap(_securityContext!, input, outputWriter, out wasEncrypted);
        }

        internal NegotiateAuthenticationStatusCode UnwrapInPlace(Span<byte> input, out int unwrappedOffset, out int unwrappedLength, out bool wasEncrypted)
        {
            return NegotiateStreamPal.UnwrapInPlace(_securityContext!, input, out unwrappedOffset, out unwrappedLength, out wasEncrypted);
        }

        internal string? GetOutgoingBlob(string? incomingBlob)
        {
            return GetOutgoingBlob(incomingBlob, throwOnError: true, out _);
        }

        internal string? GetOutgoingBlob(string? incomingBlob, bool throwOnError, out SecurityStatusPal statusCode)
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
                statusCode = new SecurityStatusPal(SecurityStatusPalErrorCode.OK);
            }
            else
            {
                decodedOutgoingBlob = GetOutgoingBlob(decodedIncomingBlob, throwOnError, out statusCode);
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

        internal byte[]? GetOutgoingBlob(byte[]? incomingBlob, bool throwOnError)
        {
            return GetOutgoingBlob(incomingBlob.AsSpan(), throwOnError, out _);
        }

        // Accepts an incoming binary security blob and returns an outgoing binary security blob.
        internal byte[]? GetOutgoingBlob(byte[]? incomingBlob, bool throwOnError, out SecurityStatusPal statusCode)
        {
            return GetOutgoingBlob(incomingBlob.AsSpan(), throwOnError, out statusCode);
        }

        internal byte[]? GetOutgoingBlob(ReadOnlySpan<byte> incomingBlob, bool throwOnError, out SecurityStatusPal statusCode)
        {
            _tokenBuffer ??= _tokenSize == 0 ? Array.Empty<byte>() : new byte[_tokenSize];

            bool firstTime = _securityContext == null;
            int resultBlobLength;
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
                        ref _tokenBuffer,
                        out resultBlobLength,
                        ref _contextFlags);

                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"SSPIWrapper.InitializeSecurityContext() returns statusCode:0x{((int)statusCode.ErrorCode):x8} ({statusCode})");

                    if (statusCode.ErrorCode == SecurityStatusPalErrorCode.CompleteNeeded)
                    {
                        statusCode = NegotiateStreamPal.CompleteAuthToken(ref _securityContext, _tokenBuffer.AsSpan(0, resultBlobLength));

                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"SSPIWrapper.CompleteAuthToken() returns statusCode:0x{((int)statusCode.ErrorCode):x8} ({statusCode})");

                        resultBlobLength = 0;
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
                        ref _tokenBuffer,
                        out resultBlobLength,
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
                _tokenBuffer = null;
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

            byte[]? result =
                resultBlobLength == 0 || _tokenBuffer == null ? null :
                _tokenBuffer.Length == resultBlobLength ? _tokenBuffer :
                _tokenBuffer[0..resultBlobLength];

            // The return value will tell us correctly if the handshake is over or not
            if (statusCode.ErrorCode == SecurityStatusPalErrorCode.OK
                || (_isServer && statusCode.ErrorCode == SecurityStatusPalErrorCode.CompleteNeeded))
            {
                // Success.
                _isCompleted = true;
                _tokenBuffer = null;
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

        internal int Encrypt(ReadOnlySpan<byte> buffer, [NotNull] ref byte[]? output)
        {
            return NegotiateStreamPal.Encrypt(
                _securityContext!,
                buffer,
                (_contextFlags & ContextFlagsPal.Confidentiality) != 0,
                IsNTLM,
                ref output);
        }

        internal int Decrypt(Span<byte> payload, out int newOffset)
        {
            return NegotiateStreamPal.Decrypt(
                _securityContext!,
                payload,
                (_contextFlags & ContextFlagsPal.Confidentiality) != 0,
                IsNTLM,
                out newOffset);
        }
    }
}
