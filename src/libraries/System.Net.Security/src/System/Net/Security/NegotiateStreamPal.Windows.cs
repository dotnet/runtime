// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using System.Security.Authentication.ExtendedProtection;

namespace System.Net.Security
{
    //
    // The class does the real work in authentication and
    // user data encryption with NEGO SSPI package.
    //
    // This is part of the NegotiateStream PAL.
    //
    internal static partial class NegotiateStreamPal
    {
        internal static int QueryMaxTokenSize(string package)
        {
            return SSPIWrapper.GetVerifyPackageInfo(GlobalSSPI.SSPIAuth, package, true)!.MaxToken;
        }

        internal static SafeFreeCredentials AcquireDefaultCredential(string package, bool isServer)
        {
            return SSPIWrapper.AcquireDefaultCredential(
                GlobalSSPI.SSPIAuth,
                package,
                (isServer ? Interop.SspiCli.CredentialUse.SECPKG_CRED_INBOUND : Interop.SspiCli.CredentialUse.SECPKG_CRED_OUTBOUND));
        }

        internal static SafeFreeCredentials AcquireCredentialsHandle(string package, bool isServer, NetworkCredential credential)
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

        internal static string? QueryContextAssociatedName(SafeDeleteContext securityContext)
        {
            return SSPIWrapper.QueryStringContextAttributes(GlobalSSPI.SSPIAuth, securityContext, Interop.SspiCli.ContextAttribute.SECPKG_ATTR_NAMES);
        }

        internal static string? QueryContextClientSpecifiedSpn(SafeDeleteContext securityContext)
        {
            return SSPIWrapper.QueryStringContextAttributes(GlobalSSPI.SSPIAuth, securityContext, Interop.SspiCli.ContextAttribute.SECPKG_ATTR_CLIENT_SPECIFIED_TARGET);
        }

        internal static string? QueryContextAuthenticationPackage(SafeDeleteContext securityContext)
        {
            SecPkgContext_NegotiationInfoW ctx = default;
            bool success = SSPIWrapper.QueryBlittableContextAttributes(GlobalSSPI.SSPIAuth, securityContext, Interop.SspiCli.ContextAttribute.SECPKG_ATTR_NEGOTIATION_INFO, typeof(SafeFreeContextBuffer), out SafeHandle? sspiHandle, ref ctx);
            using (sspiHandle)
            {
                return success ? NegotiationInfoClass.GetAuthenticationPackageName(sspiHandle!, (int)ctx.NegotiationState) : null;
            }
        }

        internal static SecurityStatusPal InitializeSecurityContext(
            ref SafeFreeCredentials? credentialsHandle,
            ref SafeDeleteContext? securityContext,
            string? spn,
            ContextFlagsPal requestedContextFlags,
            ReadOnlySpan<byte> incomingBlob,
            ChannelBinding? channelBinding,
            ref byte[]? resultBlob,
            out int resultBlobLength,
            ref ContextFlagsPal contextFlags)
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

            var outSecurityBuffer = new SecurityBuffer(resultBlob, SecurityBufferType.SECBUFFER_TOKEN);

            Interop.SspiCli.ContextFlags outContextFlags = Interop.SspiCli.ContextFlags.Zero;
            // There is only one SafeDeleteContext type on Windows which is SafeDeleteSslContext so this cast is safe.
            SafeDeleteSslContext? sslContext = (SafeDeleteSslContext?)securityContext;
            Interop.SECURITY_STATUS winStatus = (Interop.SECURITY_STATUS)SSPIWrapper.InitializeSecurityContext(
                GlobalSSPI.SSPIAuth,
                ref credentialsHandle,
                ref sslContext,
                spn,
                ContextFlagsAdapterPal.GetInteropFromContextFlagsPal(requestedContextFlags),
                Interop.SspiCli.Endianness.SECURITY_NETWORK_DREP,
                inputBuffers,
                ref outSecurityBuffer,
                ref outContextFlags);
            securityContext = sslContext;
            Debug.Assert(outSecurityBuffer.offset == 0);
            resultBlob = outSecurityBuffer.token;
            resultBlobLength = outSecurityBuffer.size;
            contextFlags = ContextFlagsAdapterPal.GetContextFlagsPalFromInterop(outContextFlags);
            return SecurityStatusAdapterPal.GetSecurityStatusPalFromInterop(winStatus);
        }

        internal static SecurityStatusPal CompleteAuthToken(
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

        internal static SecurityStatusPal AcceptSecurityContext(
            SafeFreeCredentials? credentialsHandle,
            ref SafeDeleteContext? securityContext,
            ContextFlagsPal requestedContextFlags,
            ReadOnlySpan<byte> incomingBlob,
            ChannelBinding? channelBinding,
            ref byte[]? resultBlob,
            out int resultBlobLength,
            ref ContextFlagsPal contextFlags)
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

            var outSecurityBuffer = new SecurityBuffer(resultBlob, SecurityBufferType.SECBUFFER_TOKEN);

            Interop.SspiCli.ContextFlags outContextFlags = Interop.SspiCli.ContextFlags.Zero;
            // There is only one SafeDeleteContext type on Windows which is SafeDeleteSslContext so this cast is safe.
            SafeDeleteSslContext? sslContext = (SafeDeleteSslContext?)securityContext;
            Interop.SECURITY_STATUS winStatus = (Interop.SECURITY_STATUS)SSPIWrapper.AcceptSecurityContext(
                GlobalSSPI.SSPIAuth,
                credentialsHandle,
                ref sslContext,
                ContextFlagsAdapterPal.GetInteropFromContextFlagsPal(requestedContextFlags),
                Interop.SspiCli.Endianness.SECURITY_NETWORK_DREP,
                inputBuffers,
                ref outSecurityBuffer,
                ref outContextFlags);

            // SSPI Workaround
            // If a client sends up a blob on the initial request, Negotiate returns SEC_E_INVALID_HANDLE
            // when it should return SEC_E_INVALID_TOKEN.
            if (winStatus == Interop.SECURITY_STATUS.InvalidHandle && securityContext == null && !incomingBlob.IsEmpty)
            {
                winStatus = Interop.SECURITY_STATUS.InvalidToken;
            }

            Debug.Assert(outSecurityBuffer.offset == 0);
            resultBlob = outSecurityBuffer.token;
            resultBlobLength = outSecurityBuffer.size;
            securityContext = sslContext;
            contextFlags = ContextFlagsAdapterPal.GetContextFlagsPalFromInterop(outContextFlags);
            return SecurityStatusAdapterPal.GetSecurityStatusPalFromInterop(winStatus);
        }

        internal static Win32Exception CreateExceptionFromError(SecurityStatusPal statusCode)
        {
            return new Win32Exception((int)SecurityStatusAdapterPal.GetInteropFromSecurityStatusPal(statusCode));
        }

        internal static NegotiateAuthenticationStatusCode Unwrap(
            SafeDeleteContext securityContext,
            ReadOnlySpan<byte> input,
            IBufferWriter<byte> outputWriter,
            out bool wasEncrypted)
        {
            Span<byte> outputBuffer = outputWriter.GetSpan(input.Length).Slice(0, input.Length);
            NegotiateAuthenticationStatusCode statusCode;

            input.CopyTo(outputBuffer);
            statusCode = UnwrapInPlace(securityContext, outputBuffer, out int unwrappedOffset, out int unwrappedLength, out wasEncrypted);

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

        internal static unsafe NegotiateAuthenticationStatusCode UnwrapInPlace(
            SafeDeleteContext securityContext,
            Span<byte> input,
            out int unwrappedOffset,
            out int unwrappedLength,
            out bool wasEncrypted)
        {
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
                int errorCode = GlobalSSPI.SSPIAuth.DecryptMessage(securityContext, ref sdcInOut, out qop);
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

        internal static unsafe NegotiateAuthenticationStatusCode Wrap(
            SafeDeleteContext securityContext,
            ReadOnlySpan<byte> input,
            IBufferWriter<byte> outputWriter,
            bool requestEncryption,
            out bool isEncrypted)
        {
            SecPkgContext_Sizes sizes = default;
            bool success = SSPIWrapper.QueryBlittableContextAttributes(GlobalSSPI.SSPIAuth, securityContext, Interop.SspiCli.ContextAttribute.SECPKG_ATTR_SIZES, ref sizes);
            Debug.Assert(success);

            // alloc new output buffer if not supplied or too small
            int resultSize = input.Length + sizes.cbMaxSignature;
            Span<byte> outputBuffer = outputWriter.GetSpan(resultSize);

            // make a copy of user data for in-place encryption
            input.CopyTo(outputBuffer.Slice(sizes.cbMaxSignature, input.Length));

            isEncrypted = requestEncryption;

            fixed (byte* outputPtr = outputBuffer)
            {
                // Prepare buffers TOKEN(signature), DATA and Padding.
                Interop.SspiCli.SecBuffer* unmanagedBuffer = stackalloc Interop.SspiCli.SecBuffer[2];
                Interop.SspiCli.SecBuffer* tokenBuffer = &unmanagedBuffer[0];
                Interop.SspiCli.SecBuffer* dataBuffer = &unmanagedBuffer[1];
                tokenBuffer->BufferType = SecurityBufferType.SECBUFFER_TOKEN;
                tokenBuffer->pvBuffer = (IntPtr)(outputPtr);
                tokenBuffer->cbBuffer = sizes.cbMaxSignature;
                dataBuffer->BufferType = SecurityBufferType.SECBUFFER_DATA;
                dataBuffer->pvBuffer = (IntPtr)(outputPtr + sizes.cbMaxSignature);
                dataBuffer->cbBuffer = input.Length;

                Interop.SspiCli.SecBufferDesc sdcInOut = new Interop.SspiCli.SecBufferDesc(2)
                {
                    pBuffers = unmanagedBuffer
                };

                uint qop = requestEncryption ? 0 : Interop.SspiCli.SECQOP_WRAP_NO_ENCRYPT;
                int errorCode = GlobalSSPI.SSPIAuth.EncryptMessage(securityContext, ref sdcInOut, qop);

                if (errorCode != 0)
                {
                    return errorCode switch
                    {
                        (int)Interop.SECURITY_STATUS.ContextExpired => NegotiateAuthenticationStatusCode.ContextExpired,
                        (int)Interop.SECURITY_STATUS.QopNotSupported => NegotiateAuthenticationStatusCode.QopNotSupported,
                        _ => NegotiateAuthenticationStatusCode.GenericFailure,
                    };
                }

                outputWriter.Advance(tokenBuffer->cbBuffer + dataBuffer->cbBuffer);
                return NegotiateAuthenticationStatusCode.Completed;
            }
        }

        internal static unsafe bool VerifyMIC(
            SafeDeleteContext securityContext,
            bool isConfidential,
            ReadOnlySpan<byte> message,
            ReadOnlySpan<byte> signature)
        {
            bool refAdded = false;

            try
            {
                securityContext.DangerousAddRef(ref refAdded);

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
                    int errorCode = Interop.SspiCli.VerifySignature(ref securityContext._handle, in sdcIn, 0, &qop);

                    if (errorCode != 0)
                    {
                        Exception e = new Win32Exception(errorCode);
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(null, e);
                        throw new Win32Exception(errorCode);
                    }

                    if (isConfidential && qop == Interop.SspiCli.SECQOP_WRAP_NO_ENCRYPT)
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
                    securityContext.DangerousRelease();
                }
            }
        }

        internal static unsafe void GetMIC(
            SafeDeleteContext securityContext,
            bool isConfidential,
            ReadOnlySpan<byte> message,
            IBufferWriter<byte> signature)
        {
            bool refAdded = false;

            try
            {
                securityContext.DangerousAddRef(ref refAdded);

                SecPkgContext_Sizes sizes = default;
                bool success = SSPIWrapper.QueryBlittableContextAttributes(GlobalSSPI.SSPIAuth, securityContext, Interop.SspiCli.ContextAttribute.SECPKG_ATTR_SIZES, ref sizes);
                Debug.Assert(success);

                Span<byte> signatureBuffer = signature.GetSpan(sizes.cbSecurityTrailer);

                fixed (byte* messagePtr = message)
                fixed (byte* signaturePtr = signatureBuffer)
                {
                    // Prepare buffers TOKEN(signature), DATA.
                    Interop.SspiCli.SecBuffer* unmanagedBuffer = stackalloc Interop.SspiCli.SecBuffer[2];
                    Interop.SspiCli.SecBuffer* tokenBuffer = &unmanagedBuffer[0];
                    Interop.SspiCli.SecBuffer* dataBuffer = &unmanagedBuffer[1];
                    tokenBuffer->BufferType = SecurityBufferType.SECBUFFER_TOKEN;
                    tokenBuffer->pvBuffer = (IntPtr)signaturePtr;
                    tokenBuffer->cbBuffer = sizes.cbSecurityTrailer;
                    dataBuffer->BufferType = SecurityBufferType.SECBUFFER_DATA;
                    dataBuffer->pvBuffer = (IntPtr)messagePtr;
                    dataBuffer->cbBuffer = message.Length;

                    Interop.SspiCli.SecBufferDesc sdcInOut = new Interop.SspiCli.SecBufferDesc(2)
                    {
                        pBuffers = unmanagedBuffer
                    };

                    uint qop = isConfidential ? 0 : Interop.SspiCli.SECQOP_WRAP_NO_ENCRYPT;
                    int errorCode = Interop.SspiCli.MakeSignature(ref securityContext._handle, qop, ref sdcInOut, 0);

                    if (errorCode != 0)
                    {
                        Exception e = new Win32Exception(errorCode);
                        if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(null, e);
                        throw new Win32Exception(errorCode);
                    }

                    signature.Advance(signatureBuffer.Length);
                }
            }
            finally
            {
                if (refAdded)
                {
                    securityContext.DangerousRelease();
                }
            }
        }

        internal static IIdentity GetIdentity(NTAuthentication context)
        {
            IIdentity? result;
            string? name = context.IsServer ? null : context.Spn;
            string protocol = context.ProtocolName;

            if (context.IsServer)
            {
                SecurityContextTokenHandle? token = null;
                try
                {
                    SafeDeleteContext? securityContext = context.GetContext(out SecurityStatusPal status);
                    if (status.ErrorCode != SecurityStatusPalErrorCode.OK)
                    {
                        throw new Win32Exception((int)SecurityStatusAdapterPal.GetInteropFromSecurityStatusPal(status));
                    }

                    name = QueryContextAssociatedName(securityContext!);
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(context, $"NTAuthentication: The context is associated with [{name}]");

                    // This will return a client token when conducted authentication on server side.
                    // This token can be used for impersonation. We use it to create a WindowsIdentity and hand it out to the server app.
                    Interop.SECURITY_STATUS winStatus = (Interop.SECURITY_STATUS)SSPIWrapper.QuerySecurityContextToken(
                        GlobalSSPI.SSPIAuth,
                        securityContext!,
                        out token);
                    if (winStatus != Interop.SECURITY_STATUS.OK)
                    {
                        throw new Win32Exception((int)winStatus);
                    }
                    string authtype = context.ProtocolName;

                    // The following call was also specifying WindowsAccountType.Normal, true.
                    // WindowsIdentity.IsAuthenticated is no longer supported in .NET Core
                    result = new WindowsIdentity(token.DangerousGetHandle(), authtype);
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

        internal static void ValidateImpersonationLevel(TokenImpersonationLevel impersonationLevel)
        {
            if (impersonationLevel != TokenImpersonationLevel.Identification &&
                impersonationLevel != TokenImpersonationLevel.Impersonation &&
                impersonationLevel != TokenImpersonationLevel.Delegation)
            {
                throw new ArgumentOutOfRangeException(nameof(impersonationLevel), impersonationLevel.ToString(), SR.net_auth_supported_impl_levels);
            }
        }
    }
}
