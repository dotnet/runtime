// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;

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
        internal static IIdentity GetIdentity(NTAuthentication context)
        {
            IIdentity result = null;
            string name = context.IsServer ? context.AssociatedName : context.Spn;
            string protocol = context.ProtocolName;

            if (context.IsServer)
            {
                SecurityContextTokenHandle token = null;
                try
                {
                    SecurityStatusPal status;
                    SafeDeleteContext securityContext = context.GetContext(out status);
                    if (status.ErrorCode != SecurityStatusPalErrorCode.OK)
                    {
                        throw new Win32Exception((int)SecurityStatusAdapterPal.GetInteropFromSecurityStatusPal(status));
                    }

                    // This will return a client token when conducted authentication on server side.
                    // This token can be used for impersonation. We use it to create a WindowsIdentity and hand it out to the server app.
                    Interop.SECURITY_STATUS winStatus = (Interop.SECURITY_STATUS)SSPIWrapper.QuerySecurityContextToken(
                        GlobalSSPI.SSPIAuth,
                        securityContext,
                        out token);
                    if (winStatus != Interop.SECURITY_STATUS.OK)
                    {
                        throw new Win32Exception((int)winStatus);
                    }
                    string authtype = context.ProtocolName;

                    // TODO #5241:
                    // The following call was also specifying WindowsAccountType.Normal, true.
                    // WindowsIdentity.IsAuthenticated is no longer supported in CoreFX.
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
            result = new GenericIdentity(name, protocol);
            return result;
        }

        internal static string QueryContextAssociatedName(SafeDeleteContext securityContext)
        {
            return SSPIWrapper.QueryStringContextAttributes(GlobalSSPI.SSPIAuth, securityContext, Interop.SspiCli.ContextAttribute.SECPKG_ATTR_NAMES);
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

        internal static int Encrypt(
            SafeDeleteContext securityContext,
            byte[] buffer,
            int offset,
            int count,
            bool isConfidential,
            bool isNtlm,
            ref byte[] output,
            uint sequenceNumber)
        {
            SecPkgContext_Sizes sizes = default;
            bool success = SSPIWrapper.QueryBlittableContextAttributes(GlobalSSPI.SSPIAuth, securityContext, Interop.SspiCli.ContextAttribute.SECPKG_ATTR_SIZES, ref sizes);
            Debug.Assert(success);

            try
            {
                int maxCount = checked(int.MaxValue - 4 - sizes.cbBlockSize - sizes.cbSecurityTrailer);

                if (count > maxCount || count < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(count), SR.Format(SR.net_io_out_range, maxCount));
                }
            }
            catch (Exception e) when (!ExceptionCheck.IsFatal(e))
            {
                NetEventSource.Fail(null, "Arguments out of range.");
                throw;
            }

            int resultSize = count + sizes.cbSecurityTrailer + sizes.cbBlockSize;
            if (output == null || output.Length < resultSize + 4)
            {
                output = new byte[resultSize + 4];
            }

            // Make a copy of user data for in-place encryption.
            Buffer.BlockCopy(buffer, offset, output, 4 + sizes.cbSecurityTrailer, count);

            // Prepare buffers TOKEN(signature), DATA and Padding.
            ThreeSecurityBuffers buffers = default;
            var securityBuffer = MemoryMarshal.CreateSpan(ref buffers._item0, 3);
            securityBuffer[0] = new SecurityBuffer(output, 4, sizes.cbSecurityTrailer, SecurityBufferType.SECBUFFER_TOKEN);
            securityBuffer[1] = new SecurityBuffer(output, 4 + sizes.cbSecurityTrailer, count, SecurityBufferType.SECBUFFER_DATA);
            securityBuffer[2] = new SecurityBuffer(output, 4 + sizes.cbSecurityTrailer + count, sizes.cbBlockSize, SecurityBufferType.SECBUFFER_PADDING);

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
                if (NetEventSource.IsEnabled) NetEventSource.Error(null, e);
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
            if (securityBuffer[2].size != 0 && (forceCopy || resultSize != (count + sizes.cbSecurityTrailer)))
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
            byte[] buffer,
            int offset,
            int count,
            bool isConfidential,
            bool isNtlm,
            out int newOffset,
            uint sequenceNumber)
        {
            if (offset < 0 || offset > (buffer == null ? 0 : buffer.Length))
            {
                NetEventSource.Fail(null, "Argument 'offset' out of range.");
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (count < 0 || count > (buffer == null ? 0 : buffer.Length - offset))
            {
                NetEventSource.Fail(null, "Argument 'count' out of range.");
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

            int errorCode;
            if (isConfidential)
            {
                errorCode = SSPIWrapper.DecryptMessage(GlobalSSPI.SSPIAuth, securityContext, securityBuffer, sequenceNumber);
            }
            else
            {
                errorCode = SSPIWrapper.VerifySignature(GlobalSSPI.SSPIAuth, securityContext, securityBuffer, sequenceNumber);
            }

            if (errorCode != 0)
            {
                Exception e = new Win32Exception(errorCode);
                if (NetEventSource.IsEnabled) NetEventSource.Error(null, e);
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
            byte[] buffer,
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
                NetEventSource.Fail(null, "Argument 'count' out of range.");
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
                if (NetEventSource.IsEnabled) NetEventSource.Error(null, e);
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
