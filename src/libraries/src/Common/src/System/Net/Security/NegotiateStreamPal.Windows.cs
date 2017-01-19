// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.ComponentModel;

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
            return SSPIWrapper.GetVerifyPackageInfo(GlobalSSPI.SSPIAuth, package, true).MaxToken;
        }

        internal static SafeFreeCredentials AcquireDefaultCredential(string package, bool isServer)
        {
            return SSPIWrapper.AcquireDefaultCredential(
                GlobalSSPI.SSPIAuth,
                package,
                (isServer ? Interop.SspiCli.CredentialUse.SECPKG_CRED_INBOUND : Interop.SspiCli.CredentialUse.SECPKG_CRED_OUTBOUND));
        }

        internal static unsafe SafeFreeCredentials AcquireCredentialsHandle(string package, bool isServer, NetworkCredential credential)
        {
            SafeSspiAuthDataHandle authData = null;
            try
            {
                Interop.SECURITY_STATUS result = Interop.SspiCli.SspiEncodeStringsAsAuthIdentity(
                    credential.UserName, credential.Domain,
                    credential.Password, out authData);

                if (result != Interop.SECURITY_STATUS.OK)
                {
                    if (NetEventSource.IsEnabled) NetEventSource.Error(null, SR.Format(SR.net_log_operation_failed_with_error, nameof(Interop.SspiCli.SspiEncodeStringsAsAuthIdentity), $"0x{(int)result:X}"));
                    throw new Win32Exception((int)result);
                }

                return SSPIWrapper.AcquireCredentialsHandle(GlobalSSPI.SSPIAuth,
                    package, (isServer ? Interop.SspiCli.CredentialUse.SECPKG_CRED_INBOUND : Interop.SspiCli.CredentialUse.SECPKG_CRED_OUTBOUND), ref authData);
            }
            finally
            {
                if (authData != null)
                {
                    authData.Dispose();
                }
            }
        }

        internal static string QueryContextClientSpecifiedSpn(SafeDeleteContext securityContext)
        {
            return SSPIWrapper.QueryContextAttributes(GlobalSSPI.SSPIAuth, securityContext, Interop.SspiCli.ContextAttribute.SECPKG_ATTR_CLIENT_SPECIFIED_TARGET) as string;
        }

        internal static string QueryContextAuthenticationPackage(SafeDeleteContext securityContext)
        {
            var negotiationInfoClass = SSPIWrapper.QueryContextAttributes(GlobalSSPI.SSPIAuth, securityContext, Interop.SspiCli.ContextAttribute.SECPKG_ATTR_NEGOTIATION_INFO) as NegotiationInfoClass;
            return negotiationInfoClass?.AuthenticationPackage;
        }

        internal static SecurityStatusPal InitializeSecurityContext(
            SafeFreeCredentials credentialsHandle,
            ref SafeDeleteContext securityContext,
            string spn,
            ContextFlagsPal requestedContextFlags,
            SecurityBuffer[] inSecurityBufferArray,
            SecurityBuffer outSecurityBuffer,
            ref ContextFlagsPal contextFlags)
        {
            Interop.SspiCli.ContextFlags outContextFlags = Interop.SspiCli.ContextFlags.Zero;
            Interop.SECURITY_STATUS winStatus = (Interop.SECURITY_STATUS)SSPIWrapper.InitializeSecurityContext(
                GlobalSSPI.SSPIAuth,
                credentialsHandle,
                ref securityContext,
                spn,
                ContextFlagsAdapterPal.GetInteropFromContextFlagsPal(requestedContextFlags),
                Interop.SspiCli.Endianness.SECURITY_NETWORK_DREP,
                inSecurityBufferArray,
                outSecurityBuffer,
                ref outContextFlags);

            contextFlags = ContextFlagsAdapterPal.GetContextFlagsPalFromInterop(outContextFlags);
            return SecurityStatusAdapterPal.GetSecurityStatusPalFromInterop(winStatus);
        }

        internal static SecurityStatusPal CompleteAuthToken(
            ref SafeDeleteContext securityContext,
            SecurityBuffer[] inSecurityBufferArray)
        {
            Interop.SECURITY_STATUS winStatus = (Interop.SECURITY_STATUS)SSPIWrapper.CompleteAuthToken(
                GlobalSSPI.SSPIAuth,
                ref securityContext,
                inSecurityBufferArray);
            return SecurityStatusAdapterPal.GetSecurityStatusPalFromInterop(winStatus);
        }

        internal static SecurityStatusPal AcceptSecurityContext(
            SafeFreeCredentials credentialsHandle,
            ref SafeDeleteContext securityContext,
            ContextFlagsPal requestedContextFlags,
            SecurityBuffer[] inSecurityBufferArray,
            SecurityBuffer outSecurityBuffer,
            ref ContextFlagsPal contextFlags)
        {
            Interop.SspiCli.ContextFlags outContextFlags = Interop.SspiCli.ContextFlags.Zero;
            Interop.SECURITY_STATUS winStatus = (Interop.SECURITY_STATUS)SSPIWrapper.AcceptSecurityContext(
                GlobalSSPI.SSPIAuth,
                credentialsHandle,
                ref securityContext,
                ContextFlagsAdapterPal.GetInteropFromContextFlagsPal(requestedContextFlags),
                Interop.SspiCli.Endianness.SECURITY_NETWORK_DREP,
                inSecurityBufferArray,
                outSecurityBuffer,
                ref outContextFlags);

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
            SecurityBuffer[] securityBuffer = new SecurityBuffer[2];
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
                NetEventSource.Info($"VerifySignature threw error: {errorCode.ToString("x", NumberFormatInfo.InvariantInfo)}");
                throw new Win32Exception(errorCode);
            }

            // not sure why this is here - retained from Encrypt code above
            if (securityBuffer[1].type != SecurityBufferType.SECBUFFER_DATA)
                throw new InternalException();

            // return validated payload size 
            return securityBuffer[1].size;
        }

        internal static int MakeSignature(SafeDeleteContext securityContext, byte[] buffer, int offset, int count, ref byte[] output)
        {
            SecPkgContext_Sizes sizes = SSPIWrapper.QueryContextAttributes(
                GlobalSSPI.SSPIAuth,
                securityContext,
                Interop.SspiCli.ContextAttribute.SECPKG_ATTR_SIZES
                ) as SecPkgContext_Sizes;

            // alloc new output buffer if not supplied or too small
            int resultSize = count + sizes.cbMaxSignature;
            if (output == null || output.Length < resultSize)
            {
                output = new byte[resultSize];
            }

            // make a copy of user data for in-place encryption
            Buffer.BlockCopy(buffer, offset, output, sizes.cbMaxSignature, count);

            // setup security buffers for ssp call
            SecurityBuffer[] securityBuffer = new SecurityBuffer[2];
            securityBuffer[0] = new SecurityBuffer(output, 0, sizes.cbMaxSignature, SecurityBufferType.SECBUFFER_TOKEN);
            securityBuffer[1] = new SecurityBuffer(output, sizes.cbMaxSignature, count, SecurityBufferType.SECBUFFER_DATA);

            // call SSP Function
            int errorCode = SSPIWrapper.MakeSignature(GlobalSSPI.SSPIAuth, securityContext, securityBuffer, 0);

            // throw if error
            if (errorCode != 0)
            {
                NetEventSource.Info($"MakeSignature threw error: {errorCode.ToString("x", NumberFormatInfo.InvariantInfo)}");
                throw new Win32Exception(errorCode);
            }

            // return signed size
            return securityBuffer[0].size + securityBuffer[1].size;
        }
    }
}
