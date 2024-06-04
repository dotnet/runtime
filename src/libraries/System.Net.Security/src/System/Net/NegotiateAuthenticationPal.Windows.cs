// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Principal;

namespace System.Net
{
    internal partial class NegotiateAuthenticationPal
    {
        public static NegotiateAuthenticationPal Create(NegotiateAuthenticationClientOptions clientOptions)
        {
            try
            {
                return new WindowsNegotiateAuthenticationPal(clientOptions);
            }
            catch (NotSupportedException)
            {
                return new UnsupportedNegotiateAuthenticationPal(clientOptions);
            }
        }

        public static NegotiateAuthenticationPal Create(NegotiateAuthenticationServerOptions serverOptions)
        {
            try
            {
                return new WindowsNegotiateAuthenticationPal(serverOptions);
            }
            catch (NotSupportedException)
            {
                return new UnsupportedNegotiateAuthenticationPal(serverOptions);
            }
        }

        internal sealed class WindowsNegotiateAuthenticationPal : NegotiateAuthenticationPal
        {
            private bool _isServer;
            private bool _isAuthenticated;
            private int _tokenSize;
            private byte[]? _tokenBuffer;
            private SafeFreeCredentials? _credentialsHandle;
            private SafeDeleteContext? _securityContext;
            private Interop.SspiCli.ContextFlags _requestedContextFlags;
            private Interop.SspiCli.ContextFlags _contextFlags;
            private string _package;
            private string? _protocolName;
            private string? _spn;
            private ChannelBinding? _channelBinding;

            public override bool IsAuthenticated => _isAuthenticated;

            public override bool IsSigned => (_contextFlags & (_isServer ? Interop.SspiCli.ContextFlags.AcceptIntegrity : Interop.SspiCli.ContextFlags.InitIntegrity)) != 0;

            public override bool IsEncrypted => (_contextFlags & Interop.SspiCli.ContextFlags.Confidentiality) != 0;

            public override bool IsMutuallyAuthenticated => (_contextFlags & Interop.SspiCli.ContextFlags.MutualAuth) != 0;

            public override string Package
            {
                get
                {
                    // Note: May return string.Empty if the auth is not done yet or failed.
                    if (_protocolName == null)
                    {
                        string? negotiationAuthenticationPackage = null;

                        if (_securityContext is not null)
                        {
                            SecPkgContext_NegotiationInfoW ctx = default;
                            bool success = SSPIWrapper.QueryBlittableContextAttributes(GlobalSSPI.SSPIAuth, _securityContext, Interop.SspiCli.ContextAttribute.SECPKG_ATTR_NEGOTIATION_INFO, typeof(SafeFreeContextBuffer), out SafeHandle? sspiHandle, ref ctx);
                            using (sspiHandle)
                            {
                                negotiationAuthenticationPackage = success ? NegotiationInfoClass.GetAuthenticationPackageName(sspiHandle!, (int)ctx.NegotiationState) : null;
                            }
                            if (_isAuthenticated)
                            {
                                _protocolName = negotiationAuthenticationPackage;
                            }
                        }

                        return negotiationAuthenticationPackage ?? string.Empty;
                    }

                    return _protocolName;
                }
            }

            public override string? TargetName
            {
                get
                {
                    if (_isServer && _spn == null)
                    {
                        Debug.Assert(_securityContext is not null && _isAuthenticated, "Trying to get the client SPN before handshaking is done!");
                        _spn = SSPIWrapper.QueryStringContextAttributes(GlobalSSPI.SSPIAuth, _securityContext, Interop.SspiCli.ContextAttribute.SECPKG_ATTR_CLIENT_SPECIFIED_TARGET);
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"The client specified SPN is [{_spn}]");
                    }
                    return _spn;
                }
            }

            public override IIdentity RemoteIdentity
            {
                get
                {
                    IIdentity? result;
                    string? name = _isServer ? null : TargetName;
                    string protocol = Package;

                    Debug.Assert(_securityContext is not null);

                    if (_isServer)
                    {
                        SecurityContextTokenHandle? token = null;
                        try
                        {
                            name = SSPIWrapper.QueryStringContextAttributes(GlobalSSPI.SSPIAuth, _securityContext, Interop.SspiCli.ContextAttribute.SECPKG_ATTR_NAMES);
                            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"NTAuthentication: The context is associated with [{name}]");

                            // This will return a client token when conducted authentication on server side.
                            // This token can be used for impersonation. We use it to create a WindowsIdentity and hand it out to the server app.
                            Interop.SECURITY_STATUS winStatus = (Interop.SECURITY_STATUS)SSPIWrapper.QuerySecurityContextToken(
                                GlobalSSPI.SSPIAuth,
                                _securityContext,
                                out token);
                            if (winStatus != Interop.SECURITY_STATUS.OK)
                            {
                                throw new Win32Exception((int)winStatus);
                            }

                            // The following call was also specifying WindowsAccountType.Normal, true.
                            // WindowsIdentity.IsAuthenticated is no longer supported in .NET Core
                            result = new WindowsIdentity(token.DangerousGetHandle(), protocol);
                            return result;
                        }
                        catch (SecurityException)
                        {
                            // Ignore and construct generic Identity if failed due to security problem.
                        }
                        finally
                        {
                            token?.Dispose();
                        }
                    }

                    // On the client we don't have access to the remote side identity.
                    result = new GenericIdentity(name ?? string.Empty, protocol);
                    return result;
                }
            }

            public override System.Security.Principal.TokenImpersonationLevel ImpersonationLevel
            {
                get
                {
                    return
                        (_contextFlags & Interop.SspiCli.ContextFlags.Delegate) != 0 && Package != NegotiationInfoClass.NTLM ? TokenImpersonationLevel.Delegation :
                        (_contextFlags & (_isServer ? Interop.SspiCli.ContextFlags.AcceptIdentify : Interop.SspiCli.ContextFlags.InitIdentify)) != 0 ? TokenImpersonationLevel.Identification :
                        TokenImpersonationLevel.Impersonation;
                }
            }

            public WindowsNegotiateAuthenticationPal(NegotiateAuthenticationClientOptions clientOptions)
            {
                Interop.SspiCli.ContextFlags contextFlags = Interop.SspiCli.ContextFlags.Connection;

                contextFlags |= clientOptions.RequiredProtectionLevel switch
                {
                    ProtectionLevel.Sign => Interop.SspiCli.ContextFlags.InitIntegrity,
                    ProtectionLevel.EncryptAndSign => Interop.SspiCli.ContextFlags.InitIntegrity | Interop.SspiCli.ContextFlags.Confidentiality,
                    _ => 0
                };

                contextFlags |= clientOptions.RequireMutualAuthentication ? Interop.SspiCli.ContextFlags.MutualAuth : 0;

                contextFlags |= clientOptions.AllowedImpersonationLevel switch
                {
                    TokenImpersonationLevel.Identification => Interop.SspiCli.ContextFlags.InitIdentify,
                    TokenImpersonationLevel.Delegation => Interop.SspiCli.ContextFlags.Delegate,
                    _ => 0
                };

                _isServer = false;
                _tokenSize = SSPIWrapper.GetVerifyPackageInfo(GlobalSSPI.SSPIAuth, clientOptions.Package, true)!.MaxToken;
                _spn = clientOptions.TargetName;
                _securityContext = null;
                _requestedContextFlags = contextFlags;
                _package = clientOptions.Package;
                _channelBinding = clientOptions.Binding;

                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Peer SPN-> '{_spn}'");

                //
                // Check if we're using DefaultCredentials.
                //

                Debug.Assert(CredentialCache.DefaultCredentials == CredentialCache.DefaultNetworkCredentials);
                if (clientOptions.Credential == CredentialCache.DefaultCredentials)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "using DefaultCredentials");
                    _credentialsHandle = AcquireDefaultCredential(_package, _isServer);
                }
                else
                {
                    _credentialsHandle = AcquireCredentialsHandle(_package, _isServer, clientOptions.Credential);
                }
            }

            public WindowsNegotiateAuthenticationPal(NegotiateAuthenticationServerOptions serverOptions)
            {
                Interop.SspiCli.ContextFlags contextFlags = serverOptions.RequiredProtectionLevel switch
                {
                    ProtectionLevel.Sign => Interop.SspiCli.ContextFlags.AcceptIntegrity,
                    ProtectionLevel.EncryptAndSign => Interop.SspiCli.ContextFlags.AcceptIntegrity | Interop.SspiCli.ContextFlags.Confidentiality,
                    _ => 0
                } | Interop.SspiCli.ContextFlags.Connection;

                if (serverOptions.Policy is not null)
                {
                    if (serverOptions.Policy.PolicyEnforcement == PolicyEnforcement.WhenSupported)
                    {
                        contextFlags |= Interop.SspiCli.ContextFlags.AllowMissingBindings;
                    }

                    if (serverOptions.Policy.PolicyEnforcement != PolicyEnforcement.Never &&
                        serverOptions.Policy.ProtectionScenario == ProtectionScenario.TrustedProxy)
                    {
                        contextFlags |= Interop.SspiCli.ContextFlags.ProxyBindings;
                    }
                }

                _isServer = true;
                _tokenSize = SSPIWrapper.GetVerifyPackageInfo(GlobalSSPI.SSPIAuth, serverOptions.Package, true)!.MaxToken;
                _securityContext = null;
                _requestedContextFlags = contextFlags;
                _package = serverOptions.Package;
                _channelBinding = serverOptions.Binding;

                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Peer SPN-> '{_spn}'");

                //
                // Check if we're using DefaultCredentials.
                //

                Debug.Assert(CredentialCache.DefaultCredentials == CredentialCache.DefaultNetworkCredentials);
                if (serverOptions.Credential == CredentialCache.DefaultCredentials)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "using DefaultCredentials");
                    _credentialsHandle = AcquireDefaultCredential(_package, _isServer);
                }
                else
                {
                    _credentialsHandle = AcquireCredentialsHandle(_package, _isServer, serverOptions.Credential);
                }
            }

            public override void Dispose()
            {
                _securityContext?.Dispose();
            }

            public override byte[]? GetOutgoingBlob(ReadOnlySpan<byte> incomingBlob, out NegotiateAuthenticationStatusCode statusCode)
            {
                _tokenBuffer ??= _tokenSize == 0 ? Array.Empty<byte>() : new byte[_tokenSize];

                bool firstTime = _securityContext == null;
                int resultBlobLength;
                SecurityStatusPal platformStatusCode;
                try
                {
                    if (!_isServer)
                    {
                        // client session
                        platformStatusCode = InitializeSecurityContext(
                            ref _credentialsHandle!,
                            ref _securityContext,
                            _spn,
                            _requestedContextFlags,
                            incomingBlob,
                            _channelBinding,
                            ref _tokenBuffer,
                            out resultBlobLength,
                            ref _contextFlags);

                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"SSPIWrapper.InitializeSecurityContext() returns statusCode:0x{((int)platformStatusCode.ErrorCode):x8} ({platformStatusCode})");

                        if (platformStatusCode.ErrorCode == SecurityStatusPalErrorCode.CompleteNeeded)
                        {
                            platformStatusCode = CompleteAuthToken(ref _securityContext, _tokenBuffer.AsSpan(0, resultBlobLength));

                            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"SSPIWrapper.CompleteAuthToken() returns statusCode:0x{((int)platformStatusCode.ErrorCode):x8} ({platformStatusCode})");

                            resultBlobLength = 0;
                        }
                    }
                    else
                    {
                        // Server session.
                        platformStatusCode = AcceptSecurityContext(
                            _credentialsHandle,
                            ref _securityContext,
                            _requestedContextFlags,
                            incomingBlob,
                            _channelBinding,
                            ref _tokenBuffer,
                            out resultBlobLength,
                            ref _contextFlags);

                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"SSPIWrapper.AcceptSecurityContext() returns statusCode:0x{((int)platformStatusCode.ErrorCode):x8} ({platformStatusCode})");
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

                // Map error codes
                // TODO: Remove double mapping from Win32 codes
                statusCode = platformStatusCode.ErrorCode switch
                {
                    SecurityStatusPalErrorCode.OK => NegotiateAuthenticationStatusCode.Completed,
                    SecurityStatusPalErrorCode.ContinueNeeded => NegotiateAuthenticationStatusCode.ContinueNeeded,

                    // These code should never be returned and they should be handled internally
                    SecurityStatusPalErrorCode.CompleteNeeded => NegotiateAuthenticationStatusCode.Completed,
                    SecurityStatusPalErrorCode.CompAndContinue => NegotiateAuthenticationStatusCode.ContinueNeeded,

                    SecurityStatusPalErrorCode.ContextExpired => NegotiateAuthenticationStatusCode.ContextExpired,
                    SecurityStatusPalErrorCode.Unsupported => NegotiateAuthenticationStatusCode.Unsupported,
                    SecurityStatusPalErrorCode.PackageNotFound => NegotiateAuthenticationStatusCode.Unsupported,
                    SecurityStatusPalErrorCode.CannotInstall => NegotiateAuthenticationStatusCode.Unsupported,
                    SecurityStatusPalErrorCode.InvalidToken => NegotiateAuthenticationStatusCode.InvalidToken,
                    SecurityStatusPalErrorCode.QopNotSupported => NegotiateAuthenticationStatusCode.QopNotSupported,
                    SecurityStatusPalErrorCode.NoImpersonation => NegotiateAuthenticationStatusCode.UnknownCredentials,
                    SecurityStatusPalErrorCode.LogonDenied => NegotiateAuthenticationStatusCode.UnknownCredentials,
                    SecurityStatusPalErrorCode.UnknownCredentials => NegotiateAuthenticationStatusCode.UnknownCredentials,
                    SecurityStatusPalErrorCode.NoCredentials => NegotiateAuthenticationStatusCode.UnknownCredentials,
                    SecurityStatusPalErrorCode.MessageAltered => NegotiateAuthenticationStatusCode.MessageAltered,
                    SecurityStatusPalErrorCode.OutOfSequence => NegotiateAuthenticationStatusCode.OutOfSequence,
                    SecurityStatusPalErrorCode.NoAuthenticatingAuthority => NegotiateAuthenticationStatusCode.InvalidCredentials,
                    SecurityStatusPalErrorCode.IncompleteCredentials => NegotiateAuthenticationStatusCode.InvalidCredentials,
                    SecurityStatusPalErrorCode.IllegalMessage => NegotiateAuthenticationStatusCode.InvalidToken,
                    SecurityStatusPalErrorCode.CertExpired => NegotiateAuthenticationStatusCode.CredentialsExpired,
                    SecurityStatusPalErrorCode.SecurityQosFailed => NegotiateAuthenticationStatusCode.QopNotSupported,
                    SecurityStatusPalErrorCode.UnsupportedPreauth => NegotiateAuthenticationStatusCode.InvalidToken,
                    SecurityStatusPalErrorCode.BadBinding => NegotiateAuthenticationStatusCode.BadBinding,
                    SecurityStatusPalErrorCode.UntrustedRoot => NegotiateAuthenticationStatusCode.UnknownCredentials,
                    SecurityStatusPalErrorCode.SmartcardLogonRequired => NegotiateAuthenticationStatusCode.UnknownCredentials,
                    SecurityStatusPalErrorCode.WrongPrincipal => NegotiateAuthenticationStatusCode.UnknownCredentials,
                    SecurityStatusPalErrorCode.CannotPack => NegotiateAuthenticationStatusCode.InvalidToken,
                    SecurityStatusPalErrorCode.TimeSkew => NegotiateAuthenticationStatusCode.InvalidToken,
                    SecurityStatusPalErrorCode.AlgorithmMismatch => NegotiateAuthenticationStatusCode.InvalidToken,
                    SecurityStatusPalErrorCode.CertUnknown => NegotiateAuthenticationStatusCode.UnknownCredentials,

                    // Processing partial inputs is not supported, so this is result of incorrect input
                    SecurityStatusPalErrorCode.IncompleteMessage => NegotiateAuthenticationStatusCode.InvalidToken,

                    _ => NegotiateAuthenticationStatusCode.GenericFailure,
                };

                if (((int)platformStatusCode.ErrorCode >= (int)SecurityStatusPalErrorCode.OutOfMemory))
                {
                    //CloseContext();
                    _securityContext?.Dispose();
                    _isAuthenticated = true;
                    _tokenBuffer = null;
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
                if (platformStatusCode.ErrorCode == SecurityStatusPalErrorCode.OK
                    || (_isServer && platformStatusCode.ErrorCode == SecurityStatusPalErrorCode.CompleteNeeded))
                {
                    // Success.
                    _isAuthenticated = true;
                    _tokenBuffer = null;
                }
                else
                {
                    // We need to continue.
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"need continue statusCode:0x{((int)platformStatusCode.ErrorCode):x8} ({platformStatusCode}) _securityContext:{_securityContext}");
                }

                return result;
            }

            public override unsafe NegotiateAuthenticationStatusCode Wrap(ReadOnlySpan<byte> input, IBufferWriter<byte> outputWriter, bool requestEncryption, out bool isEncrypted)
            {
                Debug.Assert(_securityContext is not null);

                SecPkgContext_Sizes sizes = default;
                bool success = SSPIWrapper.QueryBlittableContextAttributes(GlobalSSPI.SSPIAuth, _securityContext, Interop.SspiCli.ContextAttribute.SECPKG_ATTR_SIZES, ref sizes);
                Debug.Assert(success);

                // alloc new output buffer if not supplied or too small
                int resultSize = input.Length + sizes.cbSecurityTrailer + sizes.cbBlockSize;
                Span<byte> outputBuffer = outputWriter.GetSpan(resultSize);

                // make a copy of user data for in-place encryption
                input.CopyTo(outputBuffer.Slice(sizes.cbSecurityTrailer, input.Length));

                isEncrypted = requestEncryption;

                fixed (byte* outputPtr = outputBuffer)
                {
                    // Prepare buffers TOKEN(signature), DATA and Padding.
                    Interop.SspiCli.SecBuffer* unmanagedBuffer = stackalloc Interop.SspiCli.SecBuffer[3];
                    Interop.SspiCli.SecBuffer* tokenBuffer = &unmanagedBuffer[0];
                    Interop.SspiCli.SecBuffer* dataBuffer = &unmanagedBuffer[1];
                    Interop.SspiCli.SecBuffer* paddingBuffer = &unmanagedBuffer[2];
                    tokenBuffer->BufferType = SecurityBufferType.SECBUFFER_TOKEN;
                    tokenBuffer->pvBuffer = (IntPtr)(outputPtr);
                    tokenBuffer->cbBuffer = sizes.cbSecurityTrailer;
                    dataBuffer->BufferType = SecurityBufferType.SECBUFFER_DATA;
                    dataBuffer->pvBuffer = (IntPtr)(outputPtr + sizes.cbSecurityTrailer);
                    dataBuffer->cbBuffer = input.Length;
                    paddingBuffer->BufferType = SecurityBufferType.SECBUFFER_PADDING;
                    paddingBuffer->pvBuffer = (IntPtr)(outputPtr + sizes.cbSecurityTrailer + input.Length);
                    paddingBuffer->cbBuffer = sizes.cbBlockSize;

                    Interop.SspiCli.SecBufferDesc sdcInOut = new Interop.SspiCli.SecBufferDesc(3)
                    {
                        pBuffers = unmanagedBuffer
                    };

                    uint qop = requestEncryption ? 0 : Interop.SspiCli.SECQOP_WRAP_NO_ENCRYPT;
                    int errorCode = GlobalSSPI.SSPIAuth.EncryptMessage(_securityContext, ref sdcInOut, qop);

                    if (errorCode != 0)
                    {
                        return errorCode switch
                        {
                            (int)Interop.SECURITY_STATUS.ContextExpired => NegotiateAuthenticationStatusCode.ContextExpired,
                            (int)Interop.SECURITY_STATUS.QopNotSupported => NegotiateAuthenticationStatusCode.QopNotSupported,
                            _ => NegotiateAuthenticationStatusCode.GenericFailure,
                        };
                    }

                    // Compact the result
                    if (tokenBuffer->cbBuffer != sizes.cbSecurityTrailer)
                    {
                        outputBuffer.Slice(sizes.cbSecurityTrailer, dataBuffer->cbBuffer).CopyTo(
                            outputBuffer.Slice(tokenBuffer->cbBuffer, dataBuffer->cbBuffer));
                    }
                    if (tokenBuffer->cbBuffer != sizes.cbSecurityTrailer ||
                        paddingBuffer->cbBuffer != sizes.cbBlockSize)
                    {
                        outputBuffer.Slice(sizes.cbSecurityTrailer + input.Length, paddingBuffer->cbBuffer).CopyTo(
                            outputBuffer.Slice(tokenBuffer->cbBuffer + dataBuffer->cbBuffer, paddingBuffer->cbBuffer));
                    }

                    outputWriter.Advance(tokenBuffer->cbBuffer + dataBuffer->cbBuffer + paddingBuffer->cbBuffer);
                    return NegotiateAuthenticationStatusCode.Completed;
                }
            }

            public override NegotiateAuthenticationStatusCode Unwrap(ReadOnlySpan<byte> input, IBufferWriter<byte> outputWriter, out bool wasEncrypted)
            {
                Span<byte> outputBuffer = outputWriter.GetSpan(input.Length).Slice(0, input.Length);
                NegotiateAuthenticationStatusCode statusCode;

                input.CopyTo(outputBuffer);
                statusCode = UnwrapInPlace(outputBuffer, out int unwrappedOffset, out int unwrappedLength, out wasEncrypted);

                if (statusCode == NegotiateAuthenticationStatusCode.Completed)
                {
                    if (unwrappedOffset > 0)
                    {
                        outputBuffer.Slice(unwrappedOffset, unwrappedLength).CopyTo(outputBuffer);
                    }
                    outputWriter.Advance(unwrappedLength);
                }

                return statusCode;
            }

            public override unsafe NegotiateAuthenticationStatusCode UnwrapInPlace(Span<byte> input, out int unwrappedOffset, out int unwrappedLength, out bool wasEncrypted)
            {
                Debug.Assert(_securityContext is not null);

                fixed (byte* inputPtr = input)
                {
                    Interop.SspiCli.SecBuffer* unmanagedBuffer = stackalloc Interop.SspiCli.SecBuffer[2];
                    Interop.SspiCli.SecBuffer* streamBuffer = &unmanagedBuffer[0];
                    Interop.SspiCli.SecBuffer* dataBuffer = &unmanagedBuffer[1];
                    streamBuffer->BufferType = SecurityBufferType.SECBUFFER_STREAM;
                    streamBuffer->pvBuffer = (IntPtr)inputPtr;
                    streamBuffer->cbBuffer = input.Length;
                    dataBuffer->BufferType = SecurityBufferType.SECBUFFER_DATA;
                    dataBuffer->pvBuffer = IntPtr.Zero;
                    dataBuffer->cbBuffer = 0;

                    Interop.SspiCli.SecBufferDesc sdcInOut = new Interop.SspiCli.SecBufferDesc(2)
                    {
                        pBuffers = unmanagedBuffer
                    };

                    uint qop;
                    int errorCode = GlobalSSPI.SSPIAuth.DecryptMessage(_securityContext, ref sdcInOut, out qop);
                    if (errorCode != 0)
                    {
                        unwrappedOffset = 0;
                        unwrappedLength = 0;
                        wasEncrypted = false;
                        return errorCode switch
                        {
                            (int)Interop.SECURITY_STATUS.MessageAltered => NegotiateAuthenticationStatusCode.MessageAltered,
                            _ => NegotiateAuthenticationStatusCode.InvalidToken
                        };
                    }

                    if (dataBuffer->BufferType != SecurityBufferType.SECBUFFER_DATA)
                    {
                        throw new InternalException(dataBuffer->BufferType);
                    }

                    wasEncrypted = qop != Interop.SspiCli.SECQOP_WRAP_NO_ENCRYPT;

                    Debug.Assert((nint)dataBuffer->pvBuffer >= (nint)inputPtr);
                    Debug.Assert((nint)dataBuffer->pvBuffer + dataBuffer->cbBuffer <= (nint)inputPtr + input.Length);
                    unwrappedOffset = (int)((byte*)dataBuffer->pvBuffer - inputPtr);
                    unwrappedLength = dataBuffer->cbBuffer;
                    return NegotiateAuthenticationStatusCode.Completed;
                }
            }

            public override unsafe void GetMIC(ReadOnlySpan<byte> message, IBufferWriter<byte> signature)
            {
                bool refAdded = false;

                Debug.Assert(_securityContext is not null);

                try
                {
                    _securityContext.DangerousAddRef(ref refAdded);

                    SecPkgContext_Sizes sizes = default;
                    bool success = SSPIWrapper.QueryBlittableContextAttributes(GlobalSSPI.SSPIAuth, _securityContext, Interop.SspiCli.ContextAttribute.SECPKG_ATTR_SIZES, ref sizes);
                    Debug.Assert(success);

                    Span<byte> signatureBuffer = signature.GetSpan(sizes.cbMaxSignature);

                    fixed (byte* messagePtr = message)
                    fixed (byte* signaturePtr = signatureBuffer)
                    {
                        // Prepare buffers TOKEN(signature), DATA.
                        Interop.SspiCli.SecBuffer* unmanagedBuffer = stackalloc Interop.SspiCli.SecBuffer[2];
                        Interop.SspiCli.SecBuffer* tokenBuffer = &unmanagedBuffer[0];
                        Interop.SspiCli.SecBuffer* dataBuffer = &unmanagedBuffer[1];
                        tokenBuffer->BufferType = SecurityBufferType.SECBUFFER_TOKEN;
                        tokenBuffer->pvBuffer = (IntPtr)signaturePtr;
                        tokenBuffer->cbBuffer = sizes.cbMaxSignature;
                        dataBuffer->BufferType = SecurityBufferType.SECBUFFER_DATA;
                        dataBuffer->pvBuffer = (IntPtr)messagePtr;
                        dataBuffer->cbBuffer = message.Length;

                        Interop.SspiCli.SecBufferDesc sdcInOut = new Interop.SspiCli.SecBufferDesc(2)
                        {
                            pBuffers = unmanagedBuffer
                        };

                        uint qop = IsEncrypted ? 0 : Interop.SspiCli.SECQOP_WRAP_NO_ENCRYPT;
                        int errorCode = Interop.SspiCli.MakeSignature(ref _securityContext._handle, qop, ref sdcInOut, 0);

                        if (errorCode != 0)
                        {
                            Exception e = new Win32Exception(errorCode);
                            if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(null, e);
                            throw new Win32Exception(errorCode);
                        }

                        signature.Advance(tokenBuffer->cbBuffer);
                    }
                }
                finally
                {
                    if (refAdded)
                    {
                        _securityContext.DangerousRelease();
                    }
                }
            }

            public override unsafe bool VerifyMIC(ReadOnlySpan<byte> message, ReadOnlySpan<byte> signature)
            {
                bool refAdded = false;

                Debug.Assert(_securityContext is not null);

                try
                {
                    _securityContext.DangerousAddRef(ref refAdded);

                    fixed (byte* messagePtr = message)
                    fixed (byte* signaturePtr = signature)
                    {
                        Interop.SspiCli.SecBuffer* unmanagedBuffer = stackalloc Interop.SspiCli.SecBuffer[2];
                        Interop.SspiCli.SecBuffer* tokenBuffer = &unmanagedBuffer[0];
                        Interop.SspiCli.SecBuffer* dataBuffer = &unmanagedBuffer[1];
                        tokenBuffer->BufferType = SecurityBufferType.SECBUFFER_TOKEN;
                        tokenBuffer->pvBuffer = (IntPtr)signaturePtr;
                        tokenBuffer->cbBuffer = signature.Length;
                        dataBuffer->BufferType = SecurityBufferType.SECBUFFER_DATA;
                        dataBuffer->pvBuffer = (IntPtr)messagePtr;
                        dataBuffer->cbBuffer = message.Length;

                        Interop.SspiCli.SecBufferDesc sdcIn = new Interop.SspiCli.SecBufferDesc(2)
                        {
                            pBuffers = unmanagedBuffer
                        };

                        uint qop;
                        int errorCode = Interop.SspiCli.VerifySignature(ref _securityContext._handle, in sdcIn, 0, &qop);

                        if (errorCode != 0)
                        {
                            Exception e = new Win32Exception(errorCode);
                            if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(null, e);
                            throw new Win32Exception(errorCode);
                        }

                        if (IsEncrypted && qop == Interop.SspiCli.SECQOP_WRAP_NO_ENCRYPT)
                        {
                            Debug.Fail($"Expected qop = 0, returned value = {qop}");
                            throw new InvalidOperationException(SR.net_auth_message_not_encrypted);
                        }

                        return true;
                    }
                }
                finally
                {
                    if (refAdded)
                    {
                        _securityContext.DangerousRelease();
                    }
                }
            }

            private static SafeFreeCredentials AcquireDefaultCredential(string package, bool isServer)
            {
                return SSPIWrapper.AcquireDefaultCredential(
                    GlobalSSPI.SSPIAuth,
                    package,
                    (isServer ? Interop.SspiCli.CredentialUse.SECPKG_CRED_INBOUND : Interop.SspiCli.CredentialUse.SECPKG_CRED_OUTBOUND));
            }

            private static SafeFreeCredentials AcquireCredentialsHandle(string package, bool isServer, NetworkCredential credential)
            {
                SafeSspiAuthDataHandle? authData = null;
                try
                {
                    Interop.SECURITY_STATUS result = Interop.SspiCli.SspiEncodeStringsAsAuthIdentity(
                        credential.UserName, credential.Domain,
                        credential.Password, out authData);

                    if (result != Interop.SECURITY_STATUS.OK)
                    {
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(null, SR.Format(SR.net_log_operation_failed_with_error, nameof(Interop.SspiCli.SspiEncodeStringsAsAuthIdentity), $"0x{(int)result:X}"));
                        throw new Win32Exception((int)result);
                    }

                    return SSPIWrapper.AcquireCredentialsHandle(GlobalSSPI.SSPIAuth,
                        package, (isServer ? Interop.SspiCli.CredentialUse.SECPKG_CRED_INBOUND : Interop.SspiCli.CredentialUse.SECPKG_CRED_OUTBOUND), ref authData);
                }
                finally
                {
                    authData?.Dispose();
                }
            }

            private static SecurityStatusPal InitializeSecurityContext(
                ref SafeFreeCredentials? credentialsHandle,
                ref SafeDeleteContext? securityContext,
                string? spn,
                Interop.SspiCli.ContextFlags requestedContextFlags,
                ReadOnlySpan<byte> incomingBlob,
                ChannelBinding? channelBinding,
                ref byte[]? resultBlob,
                out int resultBlobLength,
                ref Interop.SspiCli.ContextFlags contextFlags)
            {

                InputSecurityBuffers inputBuffers = default;
                if (!incomingBlob.IsEmpty)
                {
                    inputBuffers.SetNextBuffer(new InputSecurityBuffer(incomingBlob, SecurityBufferType.SECBUFFER_TOKEN));
                }

                if (channelBinding != null)
                {
                    inputBuffers.SetNextBuffer(new InputSecurityBuffer(channelBinding));
                }

                ProtocolToken token = default;
                if (resultBlob != null)
                {
                    token.Payload = resultBlob;
                    token.Size = resultBlob.Length;
                }

                contextFlags = Interop.SspiCli.ContextFlags.Zero;
                // There is only one SafeDeleteContext type on Windows which is SafeDeleteSslContext so this cast is safe.
                SafeDeleteSslContext? sslContext = (SafeDeleteSslContext?)securityContext;
                Interop.SECURITY_STATUS winStatus = (Interop.SECURITY_STATUS)SSPIWrapper.InitializeSecurityContext(
                    GlobalSSPI.SSPIAuth,
                    ref credentialsHandle,
                    ref sslContext,
                    spn,
                    requestedContextFlags,
                    Interop.SspiCli.Endianness.SECURITY_NETWORK_DREP,
                    inputBuffers,
                    ref token,
                    ref contextFlags);
                securityContext = sslContext;
                resultBlob = token.Payload;
                resultBlobLength = token.Size;

                return SecurityStatusAdapterPal.GetSecurityStatusPalFromInterop(winStatus);
            }

            private static SecurityStatusPal CompleteAuthToken(
                ref SafeDeleteContext? securityContext,
                ReadOnlySpan<byte> incomingBlob)
            {
                // There is only one SafeDeleteContext type on Windows which is SafeDeleteSslContext so this cast is safe.
                SafeDeleteSslContext? sslContext = (SafeDeleteSslContext?)securityContext;
                var inSecurityBuffer = new InputSecurityBuffer(incomingBlob, SecurityBufferType.SECBUFFER_TOKEN);
                Interop.SECURITY_STATUS winStatus = (Interop.SECURITY_STATUS)SSPIWrapper.CompleteAuthToken(
                    GlobalSSPI.SSPIAuth,
                    ref sslContext,
                    in inSecurityBuffer);
                securityContext = sslContext;
                return SecurityStatusAdapterPal.GetSecurityStatusPalFromInterop(winStatus);
            }

            private static SecurityStatusPal AcceptSecurityContext(
                SafeFreeCredentials? credentialsHandle,
                ref SafeDeleteContext? securityContext,
                Interop.SspiCli.ContextFlags requestedContextFlags,
                ReadOnlySpan<byte> incomingBlob,
                ChannelBinding? channelBinding,
                ref byte[]? resultBlob,
                out int resultBlobLength,
                ref Interop.SspiCli.ContextFlags contextFlags)
            {
                InputSecurityBuffers inputBuffers = default;
                if (!incomingBlob.IsEmpty)
                {
                    inputBuffers.SetNextBuffer(new InputSecurityBuffer(incomingBlob, SecurityBufferType.SECBUFFER_TOKEN));
                }

                if (channelBinding != null)
                {
                    inputBuffers.SetNextBuffer(new InputSecurityBuffer(channelBinding));
                }

                ProtocolToken token = default;
                if (resultBlob != null)
                {
                    token.Payload = resultBlob;
                    token.Size = resultBlob.Length;
                }

                contextFlags = Interop.SspiCli.ContextFlags.Zero;
                // There is only one SafeDeleteContext type on Windows which is SafeDeleteSslContext so this cast is safe.
                SafeDeleteSslContext? sslContext = (SafeDeleteSslContext?)securityContext;
                Interop.SECURITY_STATUS winStatus = (Interop.SECURITY_STATUS)SSPIWrapper.AcceptSecurityContext(
                    GlobalSSPI.SSPIAuth,
                    credentialsHandle,
                    ref sslContext,
                    requestedContextFlags,
                    Interop.SspiCli.Endianness.SECURITY_NETWORK_DREP,
                    inputBuffers,
                    ref token,
                    ref contextFlags);

                // SSPI Workaround
                // If a client sends up a blob on the initial request, Negotiate returns SEC_E_INVALID_HANDLE
                // when it should return SEC_E_INVALID_TOKEN.
                if (winStatus == Interop.SECURITY_STATUS.InvalidHandle && securityContext == null && !incomingBlob.IsEmpty)
                {
                    winStatus = Interop.SECURITY_STATUS.InvalidToken;
                }

                resultBlob = token.Payload;
                resultBlobLength = token.Size;

                securityContext = sslContext;
                return SecurityStatusAdapterPal.GetSecurityStatusPalFromInterop(winStatus);
            }
        }
    }
}
