// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
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

        internal static unsafe int Encrypt(
            SafeDeleteContext securityContext,
            ReadOnlySpan<byte> buffer,
            bool isConfidential,
            bool isNtlm,
            [NotNull] ref byte[]? output)
        {
            SecPkgContext_Sizes sizes = default;
            bool success = SSPIWrapper.QueryBlittableContextAttributes(GlobalSSPI.SSPIAuth, securityContext, Interop.SspiCli.ContextAttribute.SECPKG_ATTR_SIZES, ref sizes);
            Debug.Assert(success);

            int maxCount = checked(int.MaxValue - 4 - sizes.cbBlockSize - sizes.cbSecurityTrailer);
            if (buffer.Length > maxCount)
            {
                throw new ArgumentOutOfRangeException(nameof(buffer.Length), SR.Format(SR.net_io_out_range, maxCount));
            }

            int resultSize = buffer.Length + sizes.cbSecurityTrailer + sizes.cbBlockSize;
            if (output == null || output.Length < resultSize + 4)
            {
                output = new byte[resultSize + 4];
            }

            // Make a copy of user data for in-place encryption.
            buffer.CopyTo(output.AsSpan(4 + sizes.cbSecurityTrailer));

            fixed (byte* outputPtr = output)
            {
                // Prepare buffers TOKEN(signature), DATA and Padding.
                Interop.SspiCli.SecBuffer* unmanagedBuffer = stackalloc Interop.SspiCli.SecBuffer[3];
                Interop.SspiCli.SecBuffer* tokenBuffer = &unmanagedBuffer[0];
                Interop.SspiCli.SecBuffer* dataBuffer = &unmanagedBuffer[1];
                Interop.SspiCli.SecBuffer* paddingBuffer = &unmanagedBuffer[2];
                tokenBuffer->BufferType = SecurityBufferType.SECBUFFER_TOKEN;
                tokenBuffer->pvBuffer = (IntPtr)(outputPtr + 4);
                tokenBuffer->cbBuffer = sizes.cbSecurityTrailer;
                dataBuffer->BufferType = SecurityBufferType.SECBUFFER_DATA;
                dataBuffer->pvBuffer = (IntPtr)(outputPtr + 4 + sizes.cbSecurityTrailer);
                dataBuffer->cbBuffer = buffer.Length;
                paddingBuffer->BufferType = SecurityBufferType.SECBUFFER_PADDING;
                paddingBuffer->pvBuffer = (IntPtr)(outputPtr + 4 + sizes.cbSecurityTrailer + buffer.Length);
                paddingBuffer->cbBuffer = sizes.cbBlockSize;

                Interop.SspiCli.SecBufferDesc sdcInOut = new Interop.SspiCli.SecBufferDesc(3)
                {
                    pBuffers = unmanagedBuffer
                };

                if (isNtlm && !isConfidential)
                {
                    dataBuffer->BufferType |= SecurityBufferType.SECBUFFER_READONLY;
                }

                uint qop = isConfidential ? 0 : Interop.SspiCli.SECQOP_WRAP_NO_ENCRYPT;
                int errorCode = GlobalSSPI.SSPIAuth.EncryptMessage(securityContext, ref sdcInOut, qop);

                if (errorCode != 0)
                {
                    Exception e = new Win32Exception(errorCode);
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(null, e);
                    throw new Win32Exception(errorCode);
                }

                // Compacting the result.
                resultSize = tokenBuffer->cbBuffer;
                bool forceCopy = false;
                if (resultSize != sizes.cbSecurityTrailer)
                {
                    forceCopy = true;
                    output.AsSpan(4 + sizes.cbSecurityTrailer, dataBuffer->cbBuffer).CopyTo(output.AsSpan(4 + resultSize, dataBuffer->cbBuffer));
                }

                resultSize += dataBuffer->cbBuffer;
                if (paddingBuffer->cbBuffer != 0 && (forceCopy || resultSize != (buffer.Length + sizes.cbSecurityTrailer)))
                {
                    output.AsSpan(4 + sizes.cbSecurityTrailer + buffer.Length, paddingBuffer->cbBuffer).CopyTo(output.AsSpan(4 + resultSize, paddingBuffer->cbBuffer));
                }

                resultSize += paddingBuffer->cbBuffer;
                BinaryPrimitives.WriteInt32LittleEndian(output, resultSize);

                return resultSize + 4;
            }
        }

        internal static unsafe int Decrypt(
            SafeDeleteContext securityContext,
            Span<byte> buffer,
            bool isConfidential,
            bool isNtlm,
            out int newOffset)
        {
            if (isNtlm)
            {
                return DecryptNtlm(securityContext, buffer, isConfidential, out newOffset);
            }

            //
            // Kerberos and up
            //
            fixed (byte* bufferPtr = buffer)
            {
                Interop.SspiCli.SecBuffer* unmanagedBuffer = stackalloc Interop.SspiCli.SecBuffer[2];
                Interop.SspiCli.SecBuffer* streamBuffer = &unmanagedBuffer[0];
                Interop.SspiCli.SecBuffer* dataBuffer = &unmanagedBuffer[1];
                streamBuffer->BufferType = SecurityBufferType.SECBUFFER_STREAM;
                streamBuffer->pvBuffer = (IntPtr)bufferPtr;
                streamBuffer->cbBuffer = buffer.Length;
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
                    Exception e = new Win32Exception(errorCode);
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(null, e);
                    throw new Win32Exception(errorCode);
                }

                if (qop == Interop.SspiCli.SECQOP_WRAP_NO_ENCRYPT && isConfidential)
                {
                    Debug.Fail($"Expected qop = 0, returned value = {qop}");
                    throw new InvalidOperationException(SR.net_auth_message_not_encrypted);
                }

                if (dataBuffer->BufferType != SecurityBufferType.SECBUFFER_DATA)
                {
                    throw new InternalException(dataBuffer->BufferType);
                }

                Debug.Assert((nint)dataBuffer->pvBuffer >= (nint)bufferPtr);
                Debug.Assert((nint)dataBuffer->pvBuffer + dataBuffer->cbBuffer <= (nint)bufferPtr + buffer.Length);
                newOffset = (int)((byte*)dataBuffer->pvBuffer - bufferPtr);
                return dataBuffer->cbBuffer;
            }
        }

        private static unsafe int DecryptNtlm(
            SafeDeleteContext securityContext,
            Span<byte> buffer,
            bool isConfidential,
            out int newOffset)
        {
            const int NtlmSignatureLength = 16;

            // For the most part the arguments are verified in Decrypt().
            if (buffer.Length < NtlmSignatureLength)
            {
                Debug.Fail("Argument 'count' out of range.");
                throw new Win32Exception((int)Interop.SECURITY_STATUS.InvalidToken);
            }

            fixed (byte* bufferPtr = buffer)
            {
                SecurityBufferType realDataType = SecurityBufferType.SECBUFFER_DATA;
                Interop.SspiCli.SecBuffer* unmanagedBuffer = stackalloc Interop.SspiCli.SecBuffer[2];
                Interop.SspiCli.SecBuffer* tokenBuffer = &unmanagedBuffer[0];
                Interop.SspiCli.SecBuffer* dataBuffer = &unmanagedBuffer[1];
                tokenBuffer->BufferType = SecurityBufferType.SECBUFFER_TOKEN;
                tokenBuffer->pvBuffer = (IntPtr)bufferPtr;
                tokenBuffer->cbBuffer = NtlmSignatureLength;
                dataBuffer->BufferType = SecurityBufferType.SECBUFFER_DATA;
                dataBuffer->pvBuffer = (IntPtr)(bufferPtr + NtlmSignatureLength);
                dataBuffer->cbBuffer = buffer.Length - NtlmSignatureLength;

                Interop.SspiCli.SecBufferDesc sdcInOut = new Interop.SspiCli.SecBufferDesc(2)
                {
                    pBuffers = unmanagedBuffer
                };
                uint qop;
                int errorCode;

                if (!isConfidential)
                {
                    realDataType |= SecurityBufferType.SECBUFFER_READONLY;
                    dataBuffer->BufferType = realDataType;
                }

                errorCode = GlobalSSPI.SSPIAuth.DecryptMessage(securityContext, ref sdcInOut, out qop);

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

                if (dataBuffer->BufferType != realDataType)
                {
                    throw new InternalException(dataBuffer->BufferType);
                }

                Debug.Assert((nint)dataBuffer->pvBuffer >= (nint)bufferPtr);
                Debug.Assert((nint)dataBuffer->pvBuffer + dataBuffer->cbBuffer <= (nint)bufferPtr + buffer.Length);
                newOffset = (int)((byte*)dataBuffer->pvBuffer - bufferPtr);
                return dataBuffer->cbBuffer;
            }
        }
    }
}
