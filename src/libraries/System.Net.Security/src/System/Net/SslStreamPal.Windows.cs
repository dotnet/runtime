// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;

namespace System.Net
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
            int win32Code = (int)GetInteropFromSecurityStatusPal(status.ErrorCode);
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
                Interop.SspiCli.Endianness.Native,
                inputBuffer,
                outputBuffer,
                ref unusedAttributes);

            return GetSecurityStatusPalFromWin32Int(errorCode);
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
                Interop.SspiCli.Endianness.Native,
                inputBuffer,
                outputBuffer,
                ref unusedAttributes);

            return GetSecurityStatusPalFromWin32Int(errorCode);
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
                            Interop.SspiCli.Endianness.Native,
                            inputBuffers,
                            outputBuffer,
                            ref unusedAttributes);

            return GetSecurityStatusPalFromWin32Int(errorCode);
        }

        public static SafeFreeCredentials AcquireCredentialsHandle(X509Certificate certificate, SslProtocols protocols, EncryptionPolicy policy, bool isServer)
        {
            int protocolFlags = GetProtocolFlagsFromSslProtocols(protocols, isServer);
            Interop.SspiCli.SecureCredential.Flags flags;
            Interop.SspiCli.CredentialUse direction;

            if (!isServer)
            {
                direction = Interop.SspiCli.CredentialUse.Outbound;
                flags = Interop.SspiCli.SecureCredential.Flags.ValidateManual | Interop.SspiCli.SecureCredential.Flags.NoDefaultCred;

                // CoreFX: always opt-in SCH_USE_STRONG_CRYPTO except for SSL3.
                if (((protocolFlags & (Interop.SChannel.SP_PROT_TLS1_0 | Interop.SChannel.SP_PROT_TLS1_1 | Interop.SChannel.SP_PROT_TLS1_2)) != 0)
                     && (policy != EncryptionPolicy.AllowNoEncryption) && (policy != EncryptionPolicy.NoEncryption))
                {
                    flags |= Interop.SspiCli.SecureCredential.Flags.UseStrongCrypto;
                }
            }
            else
            {
                direction = Interop.SspiCli.CredentialUse.Inbound;
                flags = Interop.SspiCli.SecureCredential.Flags.Zero;
            }

            Interop.SspiCli.SecureCredential secureCredential = CreateSecureCredential(
                Interop.SspiCli.SecureCredential.CurrentVersion,
                certificate,
                flags,
                protocolFlags,
                policy);

            return AcquireCredentialsHandle(direction, secureCredential);
        }

        public static SecurityStatusPal EncryptMessage(SafeDeleteContext securityContext, byte[] writeBuffer, int size, int headerSize, int trailerSize, out int resultSize)
        {
            // Encryption using SCHANNEL requires 4 buffers: header, payload, trailer, empty.
            SecurityBuffer[] securityBuffer = new SecurityBuffer[4];

            securityBuffer[0] = new SecurityBuffer(writeBuffer, 0, headerSize, SecurityBufferType.Header);
            securityBuffer[1] = new SecurityBuffer(writeBuffer, headerSize, size, SecurityBufferType.Data);
            securityBuffer[2] = new SecurityBuffer(writeBuffer, headerSize + size, trailerSize, SecurityBufferType.Trailer);
            securityBuffer[3] = new SecurityBuffer(null, SecurityBufferType.Empty);

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

            return GetSecurityStatusPalFromWin32Int(errorCode);
        }

        public static SecurityStatusPal DecryptMessage(SafeDeleteContext securityContext, byte[] buffer, ref int offset, ref int count)
        {
            // Decryption using SCHANNEL requires four buffers.
            SecurityBuffer[] decspc = new SecurityBuffer[4];
            decspc[0] = new SecurityBuffer(buffer, offset, count, SecurityBufferType.Data);
            decspc[1] = new SecurityBuffer(null, SecurityBufferType.Empty);
            decspc[2] = new SecurityBuffer(null, SecurityBufferType.Empty);
            decspc[3] = new SecurityBuffer(null, SecurityBufferType.Empty);

            Interop.SecurityStatus errorCode = (Interop.SecurityStatus)SSPIWrapper.DecryptMessage(
                GlobalSSPI.SSPISecureChannel,
                securityContext,
                decspc,
                0);

            count = 0;
            for (int i = 0; i < decspc.Length; i++)
            {
                // Successfully decoded data and placed it at the following position in the buffer,
                if ((errorCode == Interop.SecurityStatus.OK && decspc[i].type == SecurityBufferType.Data)
                    // or we failed to decode the data, here is the encoded data.
                    || (errorCode != Interop.SecurityStatus.OK && decspc[i].type == SecurityBufferType.Extra))
                {
                    offset = decspc[i].offset;
                    count = decspc[i].size;
                    break;
                }
            }

            return new SecurityStatusPal(SecurityStatusPalErrorCodeFromInterop(errorCode));
        }

        public unsafe static SafeFreeContextBufferChannelBinding QueryContextChannelBinding(SafeDeleteContext securityContext, ChannelBindingKind attribute)
        {
            return SSPIWrapper.QueryContextChannelBinding(GlobalSSPI.SSPISecureChannel, securityContext, (Interop.SspiCli.ContextAttribute)attribute);
        }

        public static void QueryContextStreamSizes(SafeDeleteContext securityContext, out StreamSizes streamSizes)
        {
            streamSizes = SSPIWrapper.QueryContextAttributes(
                GlobalSSPI.SSPISecureChannel,
                securityContext,
                Interop.SspiCli.ContextAttribute.StreamSizes) as StreamSizes;
        }

        public static void QueryContextConnectionInfo(SafeDeleteContext securityContext, out SslConnectionInfo connectionInfo)
        {
            connectionInfo = SSPIWrapper.QueryContextAttributes(
                GlobalSSPI.SSPISecureChannel,
                securityContext,
                Interop.SspiCli.ContextAttribute.ConnectionInfo) as SslConnectionInfo;
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

        private static Interop.SspiCli.SecureCredential CreateSecureCredential(
            int version,
            X509Certificate certificate,
            Interop.SspiCli.SecureCredential.Flags flags,
            int protocols, EncryptionPolicy policy)
        {
            var credential = new Interop.SspiCli.SecureCredential()
            {
                rootStore = IntPtr.Zero,
                phMappers = IntPtr.Zero,
                palgSupportedAlgs = IntPtr.Zero,
                certContextArray = IntPtr.Zero,
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

            credential.version = version;
            credential.dwFlags = flags;
            credential.grbitEnabledProtocols = protocols;
            if (certificate != null)
            {
                credential.certContextArray = certificate.Handle;
                credential.cCreds = 1;
            }

            return credential;
        }

        //
        // Security: we temporarily reset thread token to open the handle under process account.
        //
        private static SafeFreeCredentials AcquireCredentialsHandle(Interop.SspiCli.CredentialUse credUsage, Interop.SspiCli.SecureCredential secureCredential)
        {
            // First try without impersonation, if it fails, then try the process account.
            // I.E. We don't know which account the certificate context was created under.
            try
            {
                //
                // For app-compat we want to ensure the credential are accessed under >>process<< acount.
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

        private static SecurityStatusPal GetSecurityStatusPalFromWin32Int(int win32SecurityStatus)
        {
            return new SecurityStatusPal(SecurityStatusPalErrorCodeFromInterop((Interop.SecurityStatus)win32SecurityStatus));
        }

        private static SecurityStatusPalErrorCode SecurityStatusPalErrorCodeFromInterop(Interop.SecurityStatus win32SecurityStatus)
        {
            switch (win32SecurityStatus)
            {
                case Interop.SecurityStatus.OK:
                    return SecurityStatusPalErrorCode.OK;
                case Interop.SecurityStatus.ContinueNeeded:
                    return SecurityStatusPalErrorCode.ContinueNeeded;
                case Interop.SecurityStatus.CompleteNeeded:
                    return SecurityStatusPalErrorCode.CompleteNeeded;
                case Interop.SecurityStatus.CompAndContinue:
                    return SecurityStatusPalErrorCode.CompAndContinue;
                case Interop.SecurityStatus.ContextExpired:
                    return SecurityStatusPalErrorCode.ContextExpired;
                case Interop.SecurityStatus.CredentialsNeeded:
                    return SecurityStatusPalErrorCode.CredentialsNeeded;
                case Interop.SecurityStatus.Renegotiate:
                    return SecurityStatusPalErrorCode.Renegotiate;
                case Interop.SecurityStatus.OutOfMemory:
                    return SecurityStatusPalErrorCode.OutOfMemory;
                case Interop.SecurityStatus.InvalidHandle:
                    return SecurityStatusPalErrorCode.InvalidHandle;
                case Interop.SecurityStatus.Unsupported:
                    return SecurityStatusPalErrorCode.Unsupported;
                case Interop.SecurityStatus.TargetUnknown:
                    return SecurityStatusPalErrorCode.TargetUnknown;
                case Interop.SecurityStatus.InternalError:
                    return SecurityStatusPalErrorCode.InternalError;
                case Interop.SecurityStatus.PackageNotFound:
                    return SecurityStatusPalErrorCode.PackageNotFound;
                case Interop.SecurityStatus.NotOwner:
                    return SecurityStatusPalErrorCode.NotOwner;
                case Interop.SecurityStatus.CannotInstall:
                    return SecurityStatusPalErrorCode.CannotInstall;
                case Interop.SecurityStatus.InvalidToken:
                    return SecurityStatusPalErrorCode.InvalidToken;
                case Interop.SecurityStatus.CannotPack:
                    return SecurityStatusPalErrorCode.CannotPack;
                case Interop.SecurityStatus.QopNotSupported:
                    return SecurityStatusPalErrorCode.QopNotSupported;
                case Interop.SecurityStatus.NoImpersonation:
                    return SecurityStatusPalErrorCode.NoImpersonation;
                case Interop.SecurityStatus.LogonDenied:
                    return SecurityStatusPalErrorCode.LogonDenied;
                case Interop.SecurityStatus.UnknownCredentials:
                    return SecurityStatusPalErrorCode.UnknownCredentials;
                case Interop.SecurityStatus.NoCredentials:
                    return SecurityStatusPalErrorCode.NoCredentials;
                case Interop.SecurityStatus.MessageAltered:
                    return SecurityStatusPalErrorCode.MessageAltered;
                case Interop.SecurityStatus.OutOfSequence:
                    return SecurityStatusPalErrorCode.OutOfSequence;
                case Interop.SecurityStatus.NoAuthenticatingAuthority:
                    return SecurityStatusPalErrorCode.NoAuthenticatingAuthority;
                case Interop.SecurityStatus.IncompleteMessage:
                    return SecurityStatusPalErrorCode.IncompleteMessage;
                case Interop.SecurityStatus.IncompleteCredentials:
                    return SecurityStatusPalErrorCode.IncompleteCredentials;
                case Interop.SecurityStatus.BufferNotEnough:
                    return SecurityStatusPalErrorCode.BufferNotEnough;
                case Interop.SecurityStatus.WrongPrincipal:
                    return SecurityStatusPalErrorCode.WrongPrincipal;
                case Interop.SecurityStatus.TimeSkew:
                    return SecurityStatusPalErrorCode.TimeSkew;
                case Interop.SecurityStatus.UntrustedRoot:
                    return SecurityStatusPalErrorCode.UntrustedRoot;
                case Interop.SecurityStatus.IllegalMessage:
                    return SecurityStatusPalErrorCode.IllegalMessage;
                case Interop.SecurityStatus.CertUnknown:
                    return SecurityStatusPalErrorCode.CertUnknown;
                case Interop.SecurityStatus.CertExpired:
                    return SecurityStatusPalErrorCode.CertExpired;
                case Interop.SecurityStatus.AlgorithmMismatch:
                    return SecurityStatusPalErrorCode.AlgorithmMismatch;
                case Interop.SecurityStatus.SecurityQosFailed:
                    return SecurityStatusPalErrorCode.SecurityQosFailed;
                case Interop.SecurityStatus.SmartcardLogonRequired:
                    return SecurityStatusPalErrorCode.SmartcardLogonRequired;
                case Interop.SecurityStatus.UnsupportedPreauth:
                    return SecurityStatusPalErrorCode.UnsupportedPreauth;
                case Interop.SecurityStatus.BadBinding:
                    return SecurityStatusPalErrorCode.BadBinding;
                default:
                    Debug.Fail("Unknown Interop.SecurityStatus value: " + win32SecurityStatus);
                    throw new InternalException();
            }
        }

        private static Interop.SecurityStatus GetInteropFromSecurityStatusPal(SecurityStatusPalErrorCode status)
        {
            switch (status)
            {
                case SecurityStatusPalErrorCode.NotSet:
                    Debug.Fail("SecurityStatus NotSet");
                    throw new InternalException();
                case SecurityStatusPalErrorCode.OK:
                    return Interop.SecurityStatus.OK;
                case SecurityStatusPalErrorCode.ContinueNeeded:
                    return Interop.SecurityStatus.ContinueNeeded;
                case SecurityStatusPalErrorCode.CompleteNeeded:
                    return Interop.SecurityStatus.CompleteNeeded;
                case SecurityStatusPalErrorCode.CompAndContinue:
                    return Interop.SecurityStatus.CompAndContinue;
                case SecurityStatusPalErrorCode.ContextExpired:
                    return Interop.SecurityStatus.ContextExpired;
                case SecurityStatusPalErrorCode.CredentialsNeeded:
                    return Interop.SecurityStatus.CredentialsNeeded;
                case SecurityStatusPalErrorCode.Renegotiate:
                    return Interop.SecurityStatus.Renegotiate;
                case SecurityStatusPalErrorCode.OutOfMemory:
                    return Interop.SecurityStatus.OutOfMemory;
                case SecurityStatusPalErrorCode.InvalidHandle:
                    return Interop.SecurityStatus.InvalidHandle;
                case SecurityStatusPalErrorCode.Unsupported:
                    return Interop.SecurityStatus.Unsupported;
                case SecurityStatusPalErrorCode.TargetUnknown:
                    return Interop.SecurityStatus.TargetUnknown;
                case SecurityStatusPalErrorCode.InternalError:
                    return Interop.SecurityStatus.InternalError;
                case SecurityStatusPalErrorCode.PackageNotFound:
                    return Interop.SecurityStatus.PackageNotFound;
                case SecurityStatusPalErrorCode.NotOwner:
                    return Interop.SecurityStatus.NotOwner;
                case SecurityStatusPalErrorCode.CannotInstall:
                    return Interop.SecurityStatus.CannotInstall;
                case SecurityStatusPalErrorCode.InvalidToken:
                    return Interop.SecurityStatus.InvalidToken;
                case SecurityStatusPalErrorCode.CannotPack:
                    return Interop.SecurityStatus.CannotPack;
                case SecurityStatusPalErrorCode.QopNotSupported:
                    return Interop.SecurityStatus.QopNotSupported;
                case SecurityStatusPalErrorCode.NoImpersonation:
                    return Interop.SecurityStatus.NoImpersonation;
                case SecurityStatusPalErrorCode.LogonDenied:
                    return Interop.SecurityStatus.LogonDenied;
                case SecurityStatusPalErrorCode.UnknownCredentials:
                    return Interop.SecurityStatus.UnknownCredentials;
                case SecurityStatusPalErrorCode.NoCredentials:
                    return Interop.SecurityStatus.NoCredentials;
                case SecurityStatusPalErrorCode.MessageAltered:
                    return Interop.SecurityStatus.MessageAltered;
                case SecurityStatusPalErrorCode.OutOfSequence:
                    return Interop.SecurityStatus.OutOfSequence;
                case SecurityStatusPalErrorCode.NoAuthenticatingAuthority:
                    return Interop.SecurityStatus.NoAuthenticatingAuthority;
                case SecurityStatusPalErrorCode.IncompleteMessage:
                    return Interop.SecurityStatus.IncompleteMessage;
                case SecurityStatusPalErrorCode.IncompleteCredentials:
                    return Interop.SecurityStatus.IncompleteCredentials;
                case SecurityStatusPalErrorCode.BufferNotEnough:
                    return Interop.SecurityStatus.BufferNotEnough;
                case SecurityStatusPalErrorCode.WrongPrincipal:
                    return Interop.SecurityStatus.WrongPrincipal;
                case SecurityStatusPalErrorCode.TimeSkew:
                    return Interop.SecurityStatus.TimeSkew;
                case SecurityStatusPalErrorCode.UntrustedRoot:
                    return Interop.SecurityStatus.UntrustedRoot;
                case SecurityStatusPalErrorCode.IllegalMessage:
                    return Interop.SecurityStatus.IllegalMessage;
                case SecurityStatusPalErrorCode.CertUnknown:
                    return Interop.SecurityStatus.CertUnknown;
                case SecurityStatusPalErrorCode.CertExpired:
                    return Interop.SecurityStatus.CertExpired;
                case SecurityStatusPalErrorCode.AlgorithmMismatch:
                    return Interop.SecurityStatus.AlgorithmMismatch;
                case SecurityStatusPalErrorCode.SecurityQosFailed:
                    return Interop.SecurityStatus.SecurityQosFailed;
                case SecurityStatusPalErrorCode.SmartcardLogonRequired:
                    return Interop.SecurityStatus.SmartcardLogonRequired;
                case SecurityStatusPalErrorCode.UnsupportedPreauth:
                    return Interop.SecurityStatus.UnsupportedPreauth;
                case SecurityStatusPalErrorCode.BadBinding:
                    return Interop.SecurityStatus.BadBinding;
                default:
                    Debug.Fail("Unknown Interop.SecurityStatus value: " + status);
                    throw new InternalException();
            }
        }
    }
}
