// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            resultBlob = outSecurityBuffer.token;
            contextFlags = ContextFlagsAdapterPal.GetContextFlagsPalFromInterop(outContextFlags);
            return SecurityStatusAdapterPal.GetSecurityStatusPalFromInterop(winStatus);
        }

        internal static SecurityStatusPal CompleteAuthToken(
            ref SafeDeleteContext? securityContext,
            byte[]? incomingBlob)
        {
            // There is only one SafeDeleteContext type on Windows which is SafeDeleteSslContext so this cast is safe.
            SafeDeleteSslContext? sslContext = (SafeDeleteSslContext?)securityContext;
            var inSecurityBuffer = new SecurityBuffer(incomingBlob, SecurityBufferType.SECBUFFER_TOKEN);
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

            resultBlob = outSecurityBuffer.token;
            securityContext = sslContext;
            contextFlags = ContextFlagsAdapterPal.GetContextFlagsPalFromInterop(outContextFlags);
            return SecurityStatusAdapterPal.GetSecurityStatusPalFromInterop(winStatus);
        }

        internal static Win32Exception CreateExceptionFromError(SecurityStatusPal statusCode)
        {
            return new Win32Exception((int)SecurityStatusAdapterPal.GetInteropFromSecurityStatusPal(statusCode));
        }

        internal static int VerifySignature(SafeDeleteContext securityContext, byte[] buffer, int offset, int count)
        {
            // validate offset within length
            if (offset < 0 || offset > (buffer == null ? 0 : buffer.Length))
            {
                NetEventSource.Info("Argument 'offset' out of range.");
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            // validate count within offset and end of buffer
            if (count < 0 ||
                count > (buffer == null ? 0 : buffer.Length - offset))
            {
                NetEventSource.Info("Argument 'count' out of range.");
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            // setup security buffers for ssp call
            // one points at signed data
            // two will receive payload if signature is valid
#if NETSTANDARD2_0
            Span<SecurityBuffer> securityBuffer = new SecurityBuffer[2];
#else
            TwoSecurityBuffers stackBuffer = default;
            Span<SecurityBuffer> securityBuffer = MemoryMarshal.CreateSpan(ref stackBuffer._item0, 2);
#endif
            securityBuffer[0] = new SecurityBuffer(buffer, offset, count, SecurityBufferType.SECBUFFER_STREAM);
            securityBuffer[1] = new SecurityBuffer(0, SecurityBufferType.SECBUFFER_DATA);

            // call SSP function
            int errorCode = SSPIWrapper.VerifySignature(
                                GlobalSSPI.SSPIAuth,
                                securityContext,
                                securityBuffer,
                                0);
            // throw if error
            if (errorCode != 0)
            {
                NetEventSource.Info($"VerifySignature threw error: {errorCode:x}");
                throw new Win32Exception(errorCode);
            }

            // not sure why this is here - retained from Encrypt code above
            if (securityBuffer[1].type != SecurityBufferType.SECBUFFER_DATA)
                throw new InternalException(securityBuffer[1].type);

            // return validated payload size
            return securityBuffer[1].size;
        }

        internal static int MakeSignature(SafeDeleteContext securityContext, byte[] buffer, int offset, int count, [AllowNull] ref byte[] output)
        {
            SecPkgContext_Sizes sizes = default;
            bool success = SSPIWrapper.QueryBlittableContextAttributes(GlobalSSPI.SSPIAuth, securityContext, Interop.SspiCli.ContextAttribute.SECPKG_ATTR_SIZES, ref sizes);
            Debug.Assert(success);

            // alloc new output buffer if not supplied or too small
            int resultSize = count + sizes.cbMaxSignature;
            if (output == null || output.Length < resultSize)
            {
                output = new byte[resultSize];
            }

            // make a copy of user data for in-place encryption
            Buffer.BlockCopy(buffer, offset, output, sizes.cbMaxSignature, count);

            // setup security buffers for ssp call
#if NETSTANDARD2_0
            Span<SecurityBuffer> securityBuffer = new SecurityBuffer[2];
#else
            TwoSecurityBuffers stackBuffer = default;
            Span<SecurityBuffer> securityBuffer = MemoryMarshal.CreateSpan(ref stackBuffer._item0, 2);
#endif
            securityBuffer[0] = new SecurityBuffer(output, 0, sizes.cbMaxSignature, SecurityBufferType.SECBUFFER_TOKEN);
            securityBuffer[1] = new SecurityBuffer(output, sizes.cbMaxSignature, count, SecurityBufferType.SECBUFFER_DATA);

            // call SSP Function
            int errorCode = SSPIWrapper.MakeSignature(GlobalSSPI.SSPIAuth, securityContext, securityBuffer, 0);

            // throw if error
            if (errorCode != 0)
            {
                NetEventSource.Info($"MakeSignature threw error: {errorCode:x}");
                throw new Win32Exception(errorCode);
            }

            // return signed size
            return securityBuffer[0].size + securityBuffer[1].size;
        }

        internal static int Encrypt(
            SafeDeleteContext securityContext,
            ReadOnlySpan<byte> buffer,
            bool isConfidential,
            bool isNtlm,
            [NotNull] ref byte[]? output,
            uint sequenceNumber)
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

            // Prepare buffers TOKEN(signature), DATA and Padding.
            ThreeSecurityBuffers buffers = default;
            var securityBuffer = MemoryMarshal.CreateSpan(ref buffers._item0, 3);
            securityBuffer[0] = new SecurityBuffer(output, 4, sizes.cbSecurityTrailer, SecurityBufferType.SECBUFFER_TOKEN);
            securityBuffer[1] = new SecurityBuffer(output, 4 + sizes.cbSecurityTrailer, buffer.Length, SecurityBufferType.SECBUFFER_DATA);
            securityBuffer[2] = new SecurityBuffer(output, 4 + sizes.cbSecurityTrailer + buffer.Length, sizes.cbBlockSize, SecurityBufferType.SECBUFFER_PADDING);

            int errorCode;
            if (isConfidential)
            {
                errorCode = SSPIWrapper.EncryptMessage(GlobalSSPI.SSPIAuth, securityContext, securityBuffer, sequenceNumber);
            }
            else
            {
                if (isNtlm)
                {
                    securityBuffer[1].type |= SecurityBufferType.SECBUFFER_READONLY;
                }

                errorCode = SSPIWrapper.MakeSignature(GlobalSSPI.SSPIAuth, securityContext, securityBuffer, 0);
            }

            if (errorCode != 0)
            {
                Exception e = new Win32Exception(errorCode);
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(null, e);
                throw e;
            }

            // Compacting the result.
            resultSize = securityBuffer[0].size;
            bool forceCopy = false;
            if (resultSize != sizes.cbSecurityTrailer)
            {
                forceCopy = true;
                Buffer.BlockCopy(output, securityBuffer[1].offset, output, 4 + resultSize, securityBuffer[1].size);
            }

            resultSize += securityBuffer[1].size;
            if (securityBuffer[2].size != 0 && (forceCopy || resultSize != (buffer.Length + sizes.cbSecurityTrailer)))
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

        internal static int Decrypt(
            SafeDeleteContext securityContext,
            byte[]? buffer,
            int offset,
            int count,
            bool isConfidential,
            bool isNtlm,
            out int newOffset,
            uint sequenceNumber)
        {
            if (offset < 0 || offset > (buffer == null ? 0 : buffer.Length))
            {
                Debug.Fail("Argument 'offset' out of range.");
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (count < 0 || count > (buffer == null ? 0 : buffer.Length - offset))
            {
                Debug.Fail("Argument 'count' out of range.");
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (isNtlm)
            {
                return DecryptNtlm(securityContext, buffer, offset, count, isConfidential, out newOffset, sequenceNumber);
            }

            //
            // Kerberos and up
            //
            TwoSecurityBuffers buffers = default;
            var securityBuffer = MemoryMarshal.CreateSpan(ref buffers._item0, 2);
            securityBuffer[0] = new SecurityBuffer(buffer, offset, count, SecurityBufferType.SECBUFFER_STREAM);
            securityBuffer[1] = new SecurityBuffer(0, SecurityBufferType.SECBUFFER_DATA);

            int errorCode = isConfidential ?
                SSPIWrapper.DecryptMessage(GlobalSSPI.SSPIAuth, securityContext, securityBuffer, sequenceNumber) :
                SSPIWrapper.VerifySignature(GlobalSSPI.SSPIAuth, securityContext, securityBuffer, sequenceNumber);

            if (errorCode != 0)
            {
                Exception e = new Win32Exception(errorCode);
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(null, e);
                throw e;
            }

            if (securityBuffer[1].type != SecurityBufferType.SECBUFFER_DATA)
            {
                throw new InternalException(securityBuffer[1].type);
            }

            newOffset = securityBuffer[1].offset;
            return securityBuffer[1].size;
        }

        private static int DecryptNtlm(
            SafeDeleteContext securityContext,
            byte[]? buffer,
            int offset,
            int count,
            bool isConfidential,
            out int newOffset,
            uint sequenceNumber)
        {
            const int ntlmSignatureLength = 16;
            // For the most part the arguments are verified in Decrypt().
            if (count < ntlmSignatureLength)
            {
                Debug.Fail("Argument 'count' out of range.");
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            TwoSecurityBuffers buffers = default;
            var securityBuffer = MemoryMarshal.CreateSpan(ref buffers._item0, 2);
            securityBuffer[0] = new SecurityBuffer(buffer, offset, ntlmSignatureLength, SecurityBufferType.SECBUFFER_TOKEN);
            securityBuffer[1] = new SecurityBuffer(buffer, offset + ntlmSignatureLength, count - ntlmSignatureLength, SecurityBufferType.SECBUFFER_DATA);

            int errorCode;
            SecurityBufferType realDataType = SecurityBufferType.SECBUFFER_DATA;

            if (isConfidential)
            {
                errorCode = SSPIWrapper.DecryptMessage(GlobalSSPI.SSPIAuth, securityContext, securityBuffer, sequenceNumber);
            }
            else
            {
                realDataType |= SecurityBufferType.SECBUFFER_READONLY;
                securityBuffer[1].type = realDataType;
                errorCode = SSPIWrapper.VerifySignature(GlobalSSPI.SSPIAuth, securityContext, securityBuffer, sequenceNumber);
            }

            if (errorCode != 0)
            {
                Exception e = new Win32Exception(errorCode);
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(null, e);
                throw new Win32Exception(errorCode);
            }

            if (securityBuffer[1].type != realDataType)
            {
                throw new InternalException(securityBuffer[1].type);
            }

            newOffset = securityBuffer[1].offset;
            return securityBuffer[1].size;
        }
    }
}
