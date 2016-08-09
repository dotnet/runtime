// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;

namespace System.Net.Security
{
    internal static class SslStreamPal
    {
        private const string SecurityPackage = "Microsoft Unified Security Protocol Provider";

        private const Interop.SspiCli.ContextFlags RequiredFlags =
            Interop.SspiCli.ContextFlags.ReplayDetect |
            Interop.SspiCli.ContextFlags.SequenceDetect |
            Interop.SspiCli.ContextFlags.Confidentiality |
            Interop.SspiCli.ContextFlags.AllocateMemory;

        private const Interop.SspiCli.ContextFlags ServerRequiredFlags =
            RequiredFlags | Interop.SspiCli.ContextFlags.AcceptStream;

        public static Exception GetException(SecurityStatusPal status)
        {
            int win32Code = (int)SecurityStatusAdapterPal.GetInteropFromSecurityStatusPal(status);
            return new Win32Exception(win32Code);
        }

        internal const bool StartMutualAuthAsAnonymous = true;

        public static void VerifyPackageInfo()
        {
            SSPIWrapper.GetVerifyPackageInfo(GlobalSSPI.SSPISecureChannel, SecurityPackage, true);
        }

        public static SecurityStatusPal AcceptSecurityContext(ref SafeFreeCredentials credentialsHandle, ref SafeDeleteContext context, SecurityBuffer inputBuffer, SecurityBuffer outputBuffer, bool remoteCertRequired)
        {
            Interop.SspiCli.ContextFlags unusedAttributes = default(Interop.SspiCli.ContextFlags);

            int errorCode = SSPIWrapper.AcceptSecurityContext(
                GlobalSSPI.SSPISecureChannel,
                ref credentialsHandle,
                ref context,
                ServerRequiredFlags | (remoteCertRequired ? Interop.SspiCli.ContextFlags.MutualAuth : Interop.SspiCli.ContextFlags.Zero),
                Interop.SspiCli.Endianness.SECURITY_NATIVE_DREP,
                inputBuffer,
                outputBuffer,
                ref unusedAttributes);

            return SecurityStatusAdapterPal.GetSecurityStatusPalFromNativeInt(errorCode);
        }

        public static SecurityStatusPal InitializeSecurityContext(ref SafeFreeCredentials credentialsHandle, ref SafeDeleteContext context, string targetName, SecurityBuffer inputBuffer, SecurityBuffer outputBuffer)
        {
            Interop.SspiCli.ContextFlags unusedAttributes = default(Interop.SspiCli.ContextFlags);

            int errorCode = SSPIWrapper.InitializeSecurityContext(
                GlobalSSPI.SSPISecureChannel,
                ref credentialsHandle,
                ref context,
                targetName,
                RequiredFlags | Interop.SspiCli.ContextFlags.InitManualCredValidation,
                Interop.SspiCli.Endianness.SECURITY_NATIVE_DREP,
                inputBuffer,
                outputBuffer,
                ref unusedAttributes);

            return SecurityStatusAdapterPal.GetSecurityStatusPalFromNativeInt(errorCode);
        }

        public static SecurityStatusPal InitializeSecurityContext(SafeFreeCredentials credentialsHandle, ref SafeDeleteContext context, string targetName, SecurityBuffer[] inputBuffers, SecurityBuffer outputBuffer)
        {
            Interop.SspiCli.ContextFlags unusedAttributes = default(Interop.SspiCli.ContextFlags);

            int errorCode = SSPIWrapper.InitializeSecurityContext(
                            GlobalSSPI.SSPISecureChannel,
                            credentialsHandle,
                            ref context,
                            targetName,
                            RequiredFlags | Interop.SspiCli.ContextFlags.InitManualCredValidation,
                            Interop.SspiCli.Endianness.SECURITY_NATIVE_DREP,
                            inputBuffers,
                            outputBuffer,
                            ref unusedAttributes);

            return SecurityStatusAdapterPal.GetSecurityStatusPalFromNativeInt(errorCode);
        }

        public static SafeFreeCredentials AcquireCredentialsHandle(X509Certificate certificate, SslProtocols protocols, EncryptionPolicy policy, bool isServer)
        {
            int protocolFlags = GetProtocolFlagsFromSslProtocols(protocols, isServer);
            Interop.SspiCli.SCHANNEL_CRED.Flags flags;
            Interop.SspiCli.CredentialUse direction;

            if (!isServer)
            {
                direction = Interop.SspiCli.CredentialUse.SECPKG_CRED_OUTBOUND;
                flags = 
                    Interop.SspiCli.SCHANNEL_CRED.Flags.SCH_CRED_MANUAL_CRED_VALIDATION | 
                    Interop.SspiCli.SCHANNEL_CRED.Flags.SCH_CRED_NO_DEFAULT_CREDS | 
                    Interop.SspiCli.SCHANNEL_CRED.Flags.SCH_SEND_AUX_RECORD;

                // CoreFX: always opt-in SCH_USE_STRONG_CRYPTO except for SSL3.
                if (((protocolFlags & (Interop.SChannel.SP_PROT_TLS1_0 | Interop.SChannel.SP_PROT_TLS1_1 | Interop.SChannel.SP_PROT_TLS1_2)) != 0)
                     && (policy != EncryptionPolicy.AllowNoEncryption) && (policy != EncryptionPolicy.NoEncryption))
                {
                    flags |= Interop.SspiCli.SCHANNEL_CRED.Flags.SCH_USE_STRONG_CRYPTO;
                }
            }
            else
            {
                direction = Interop.SspiCli.CredentialUse.SECPKG_CRED_INBOUND;
                flags = Interop.SspiCli.SCHANNEL_CRED.Flags.SCH_SEND_AUX_RECORD;
            }

            Interop.SspiCli.SCHANNEL_CRED secureCredential = CreateSecureCredential(
                Interop.SspiCli.SCHANNEL_CRED.CurrentVersion,
                certificate,
                flags,
                protocolFlags,
                policy);

            return AcquireCredentialsHandle(direction, secureCredential);
        }

        public static SecurityStatusPal EncryptMessage(SafeDeleteContext securityContext, byte[] input, int offset, int size, int headerSize, int trailerSize, ref byte[] output, out int resultSize)
        {
            // Ensure that there is sufficient space for the message output.
            try
            {
                int bufferSizeNeeded = checked(size + headerSize + trailerSize);

                if (output == null || output.Length < bufferSizeNeeded)
                {
                    output = new byte[bufferSizeNeeded];
                }
            }
            catch (Exception e)
            {
                if (!ExceptionCheck.IsFatal(e))
                {
                    if (GlobalLog.IsEnabled)
                    {
                        GlobalLog.Assert("SslStreamPal.Windows: SecureChannel#" + LoggingHash.HashString(securityContext) + "::Encrypt", "Arguments out of range.");
                    }

                    Debug.Fail("SslStreamPal.Windows: SecureChannel#" + LoggingHash.HashString(securityContext) + "::Encrypt", "Arguments out of range.");
                }

                throw;
            }

            byte[] writeBuffer = output;

            // Copy the input into the output buffer to prepare for SCHANNEL's expectations
            Buffer.BlockCopy(input, offset, writeBuffer, headerSize, size);

            // Encryption using SCHANNEL requires 4 buffers: header, payload, trailer, empty.
            SecurityBuffer[] securityBuffer = new SecurityBuffer[4];

            securityBuffer[0] = new SecurityBuffer(writeBuffer, 0, headerSize, SecurityBufferType.SECBUFFER_STREAM_HEADER);
            securityBuffer[1] = new SecurityBuffer(writeBuffer, headerSize, size, SecurityBufferType.SECBUFFER_DATA);
            securityBuffer[2] = new SecurityBuffer(writeBuffer, headerSize + size, trailerSize, SecurityBufferType.SECBUFFER_STREAM_TRAILER);
            securityBuffer[3] = new SecurityBuffer(null, SecurityBufferType.SECBUFFER_EMPTY);

            int errorCode = SSPIWrapper.EncryptMessage(GlobalSSPI.SSPISecureChannel, securityContext, securityBuffer, 0);

            if (errorCode != 0)
            {
                if (GlobalLog.IsEnabled)
                {
                    GlobalLog.Print("SslStreamPal.Windows: SecureChannel#" + LoggingHash.HashString(securityContext) + "::Encrypt ERROR" + errorCode.ToString("x"));
                }
                resultSize = 0;
            }
            else
            {
                // The full buffer may not be used.
                resultSize = securityBuffer[0].size + securityBuffer[1].size + securityBuffer[2].size;
            }

            return SecurityStatusAdapterPal.GetSecurityStatusPalFromNativeInt(errorCode);
        }

        public static SecurityStatusPal DecryptMessage(SafeDeleteContext securityContext, byte[] buffer, ref int offset, ref int count)
        {
            // Decryption using SCHANNEL requires four buffers.
            SecurityBuffer[] decspc = new SecurityBuffer[4];
            decspc[0] = new SecurityBuffer(buffer, offset, count, SecurityBufferType.SECBUFFER_DATA);
            decspc[1] = new SecurityBuffer(null, SecurityBufferType.SECBUFFER_EMPTY);
            decspc[2] = new SecurityBuffer(null, SecurityBufferType.SECBUFFER_EMPTY);
            decspc[3] = new SecurityBuffer(null, SecurityBufferType.SECBUFFER_EMPTY);

            Interop.SECURITY_STATUS errorCode = (Interop.SECURITY_STATUS)SSPIWrapper.DecryptMessage(
                GlobalSSPI.SSPISecureChannel,
                securityContext,
                decspc,
                0);

            count = 0;
            for (int i = 0; i < decspc.Length; i++)
            {
                // Successfully decoded data and placed it at the following position in the buffer,
                if ((errorCode == Interop.SECURITY_STATUS.OK && decspc[i].type == SecurityBufferType.SECBUFFER_DATA)
                    // or we failed to decode the data, here is the encoded data.
                    || (errorCode != Interop.SECURITY_STATUS.OK && decspc[i].type == SecurityBufferType.SECBUFFER_EXTRA))
                {
                    offset = decspc[i].offset;
                    count = decspc[i].size;
                    break;
                }
            }

            return SecurityStatusAdapterPal.GetSecurityStatusPalFromInterop(errorCode);
        }

        public static SecurityStatusPal ApplyAlertToken(ref SafeFreeCredentials credentialsHandle, SafeDeleteContext securityContext, TlsAlertType alertType, TlsAlertMessage alertMessage)
        {
            Interop.SChannel.SCHANNEL_ALERT_TOKEN alertToken;
            alertToken.dwTokenType = Interop.SChannel.SCHANNEL_ALERT;
            alertToken.dwAlertType = (uint)alertType;
            alertToken.dwAlertNumber = (uint)alertMessage;

            var bufferDesc = new SecurityBuffer[1];

            int alertTokenByteSize = Marshal.SizeOf<Interop.SChannel.SCHANNEL_ALERT_TOKEN>();
            IntPtr p = Marshal.AllocHGlobal(alertTokenByteSize);

            try
            {
                var buffer = new byte[alertTokenByteSize];
                Marshal.StructureToPtr<Interop.SChannel.SCHANNEL_ALERT_TOKEN>(alertToken, p, false);
                Marshal.Copy(p, buffer, 0, alertTokenByteSize);

                bufferDesc[0] = new SecurityBuffer(buffer, SecurityBufferType.SECBUFFER_TOKEN);
                var errorCode = (Interop.SECURITY_STATUS)SSPIWrapper.ApplyControlToken(
                    GlobalSSPI.SSPISecureChannel,
                    ref securityContext,
                    bufferDesc);

                return SecurityStatusAdapterPal.GetSecurityStatusPalFromInterop(errorCode, attachException:true);
            }
            finally
            {
                Marshal.FreeHGlobal(p);
            }
        }

        public static SecurityStatusPal ApplyShutdownToken(ref SafeFreeCredentials credentialsHandle, SafeDeleteContext securityContext)
        {
            int shutdownToken = Interop.SChannel.SCHANNEL_SHUTDOWN;

            var bufferDesc = new SecurityBuffer[1];
            var buffer = BitConverter.GetBytes(shutdownToken);

            bufferDesc[0] = new SecurityBuffer(buffer, SecurityBufferType.SECBUFFER_TOKEN);
            var errorCode = (Interop.SECURITY_STATUS)SSPIWrapper.ApplyControlToken(
                GlobalSSPI.SSPISecureChannel,
                ref securityContext,
                bufferDesc);

            return SecurityStatusAdapterPal.GetSecurityStatusPalFromInterop(errorCode, attachException:true);
        }

        public unsafe static SafeFreeContextBufferChannelBinding QueryContextChannelBinding(SafeDeleteContext securityContext, ChannelBindingKind attribute)
        {
            return SSPIWrapper.QueryContextChannelBinding(GlobalSSPI.SSPISecureChannel, securityContext, (Interop.SspiCli.ContextAttribute)attribute);
        }

        public static void QueryContextStreamSizes(SafeDeleteContext securityContext, out StreamSizes streamSizes)
        {
            var interopStreamSizes = SSPIWrapper.QueryContextAttributes(
                GlobalSSPI.SSPISecureChannel,
                securityContext,
                Interop.SspiCli.ContextAttribute.SECPKG_ATTR_STREAM_SIZES) as SecPkgContext_StreamSizes;

            streamSizes = new StreamSizes(interopStreamSizes);
        }

        public static void QueryContextConnectionInfo(SafeDeleteContext securityContext, out SslConnectionInfo connectionInfo)
        {
            var interopConnectionInfo = SSPIWrapper.QueryContextAttributes(
                GlobalSSPI.SSPISecureChannel,
                securityContext,
                Interop.SspiCli.ContextAttribute.SECPKG_ATTR_CONNECTION_INFO) as SecPkgContext_ConnectionInfo;

            connectionInfo = new SslConnectionInfo(interopConnectionInfo);
        }

        private static int GetProtocolFlagsFromSslProtocols(SslProtocols protocols, bool isServer)
        {
            int protocolFlags = (int)protocols;

            if (isServer)
            {
                protocolFlags &= Interop.SChannel.ServerProtocolMask;
            }
            else
            {
                protocolFlags &= Interop.SChannel.ClientProtocolMask;
            }

            return protocolFlags;
        }

        private static Interop.SspiCli.SCHANNEL_CRED CreateSecureCredential(
            int version,
            X509Certificate certificate,
            Interop.SspiCli.SCHANNEL_CRED.Flags flags,
            int protocols, EncryptionPolicy policy)
        {
            var credential = new Interop.SspiCli.SCHANNEL_CRED()
            {
                hRootStore = IntPtr.Zero,
                aphMappers = IntPtr.Zero,
                palgSupportedAlgs = IntPtr.Zero,
                paCred = IntPtr.Zero,
                cCreds = 0,
                cMappers = 0,
                cSupportedAlgs = 0,
                dwSessionLifespan = 0,
                reserved = 0
            };

            if (policy == EncryptionPolicy.RequireEncryption)
            {
                // Prohibit null encryption cipher.
                credential.dwMinimumCipherStrength = 0;
                credential.dwMaximumCipherStrength = 0;
            }
            else if (policy == EncryptionPolicy.AllowNoEncryption)
            {
                // Allow null encryption cipher in addition to other ciphers.
                credential.dwMinimumCipherStrength = -1;
                credential.dwMaximumCipherStrength = 0;
            }
            else if (policy == EncryptionPolicy.NoEncryption)
            {
                // Suppress all encryption and require null encryption cipher only
                credential.dwMinimumCipherStrength = -1;
                credential.dwMaximumCipherStrength = -1;
            }
            else
            {
                throw new ArgumentException(SR.Format(SR.net_invalid_enum, "EncryptionPolicy"), nameof(policy));
            }

            credential.dwVersion = version;
            credential.dwFlags = flags;
            credential.grbitEnabledProtocols = protocols;
            if (certificate != null)
            {
                credential.paCred = certificate.Handle;
                credential.cCreds = 1;
            }

            return credential;
        }

        //
        // Security: we temporarily reset thread token to open the handle under process account.
        //
        private static SafeFreeCredentials AcquireCredentialsHandle(Interop.SspiCli.CredentialUse credUsage, Interop.SspiCli.SCHANNEL_CRED secureCredential)
        {
            // First try without impersonation, if it fails, then try the process account.
            // I.E. We don't know which account the certificate context was created under.
            try
            {
                //
                // For app-compat we want to ensure the credential are accessed under >>process<< account.
                //
                return WindowsIdentity.RunImpersonated<SafeFreeCredentials>(SafeAccessTokenHandle.InvalidHandle, () =>
                {
                    return SSPIWrapper.AcquireCredentialsHandle(GlobalSSPI.SSPISecureChannel, SecurityPackage, credUsage, secureCredential);
                });
            }
            catch
            {
                return SSPIWrapper.AcquireCredentialsHandle(GlobalSSPI.SSPISecureChannel, SecurityPackage, credUsage, secureCredential);
            }
        }
    }
}
