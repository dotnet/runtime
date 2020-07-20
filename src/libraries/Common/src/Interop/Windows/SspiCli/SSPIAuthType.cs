// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Net.Security;
using System.Runtime.InteropServices;

namespace System.Net
{
    // Authentication SSPI (Kerberos, NTLM, Negotiate and WDigest):
    internal sealed class SSPIAuthType : ISSPIInterface
    {
        private static volatile SecurityPackageInfoClass[]? s_securityPackages;

        public SecurityPackageInfoClass[]? SecurityPackages
        {
            get
            {
                return s_securityPackages;
            }
            set
            {
                s_securityPackages = value;
            }
        }

        public int EnumerateSecurityPackages(out int pkgnum, out SafeFreeContextBuffer pkgArray)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this);
            return SafeFreeContextBuffer.EnumeratePackages(out pkgnum, out pkgArray);
        }

        public int AcquireCredentialsHandle(string moduleName, Interop.SspiCli.CredentialUse usage, ref SafeSspiAuthDataHandle authdata, out SafeFreeCredentials outCredential)
        {
            return SafeFreeCredentials.AcquireCredentialsHandle(moduleName, usage, ref authdata, out outCredential);
        }

        public int AcquireDefaultCredential(string moduleName, Interop.SspiCli.CredentialUse usage, out SafeFreeCredentials outCredential)
        {
            return SafeFreeCredentials.AcquireDefaultCredential(moduleName, usage, out outCredential);
        }

        public int AcquireCredentialsHandle(string moduleName, Interop.SspiCli.CredentialUse usage, ref Interop.SspiCli.SCHANNEL_CRED authdata, out SafeFreeCredentials outCredential)
        {
            return SafeFreeCredentials.AcquireCredentialsHandle(moduleName, usage, ref authdata, out outCredential);
        }

        public unsafe int AcquireCredentialsHandle(string moduleName, Interop.SspiCli.CredentialUse usage, Interop.SspiCli.SCH_CREDENTIALS* authdata, out SafeFreeCredentials outCredential)
        {
            return SafeFreeCredentials.AcquireCredentialsHandle(moduleName, usage, authdata, out outCredential);
        }

        public int AcceptSecurityContext(SafeFreeCredentials? credential, ref SafeDeleteSslContext? context, InputSecurityBuffers inputBuffers, Interop.SspiCli.ContextFlags inFlags, Interop.SspiCli.Endianness endianness, ref SecurityBuffer outputBuffer, ref Interop.SspiCli.ContextFlags outFlags)
        {
            return SafeDeleteContext.AcceptSecurityContext(ref credential, ref context, inFlags, endianness, inputBuffers, ref outputBuffer, ref outFlags);
        }

        public int InitializeSecurityContext(ref SafeFreeCredentials? credential, ref SafeDeleteSslContext? context, string? targetName, Interop.SspiCli.ContextFlags inFlags, Interop.SspiCli.Endianness endianness, InputSecurityBuffers inputBuffers, ref SecurityBuffer outputBuffer, ref Interop.SspiCli.ContextFlags outFlags)
        {
            return SafeDeleteContext.InitializeSecurityContext(ref credential, ref context, targetName, inFlags, endianness, inputBuffers, ref outputBuffer, ref outFlags);
        }

        public int EncryptMessage(SafeDeleteContext context, ref Interop.SspiCli.SecBufferDesc inputOutput, uint sequenceNumber)
        {
            try
            {
                bool ignore = false;

                context.DangerousAddRef(ref ignore);
                return Interop.SspiCli.EncryptMessage(ref context._handle, 0, ref inputOutput, sequenceNumber);
            }
            finally
            {
                context.DangerousRelease();
            }
        }

        public unsafe int DecryptMessage(SafeDeleteContext context, ref Interop.SspiCli.SecBufferDesc inputOutput, uint sequenceNumber)
        {
            int status = (int)Interop.SECURITY_STATUS.InvalidHandle;
            uint qop = 0;

            try
            {
                bool ignore = false;
                context.DangerousAddRef(ref ignore);
                status = Interop.SspiCli.DecryptMessage(ref context._handle, ref inputOutput, sequenceNumber, &qop);
            }
            finally
            {
                context.DangerousRelease();
            }

            if (status == 0 && qop == Interop.SspiCli.SECQOP_WRAP_NO_ENCRYPT)
            {
                NetEventSource.Fail(this, $"Expected qop = 0, returned value = {qop}");
                throw new InvalidOperationException(SR.net_auth_message_not_encrypted);
            }

            return status;
        }

        public int MakeSignature(SafeDeleteContext context, ref Interop.SspiCli.SecBufferDesc inputOutput, uint sequenceNumber)
        {
            try
            {
                bool ignore = false;

                context.DangerousAddRef(ref ignore);

                return Interop.SspiCli.EncryptMessage(ref context._handle, Interop.SspiCli.SECQOP_WRAP_NO_ENCRYPT, ref inputOutput, sequenceNumber);
            }
            finally
            {
                context.DangerousRelease();
            }
        }

        public unsafe int VerifySignature(SafeDeleteContext context, ref Interop.SspiCli.SecBufferDesc inputOutput, uint sequenceNumber)
        {
            try
            {
                bool ignore = false;
                uint qop = 0;

                context.DangerousAddRef(ref ignore);
                return Interop.SspiCli.DecryptMessage(ref context._handle, ref inputOutput, sequenceNumber, &qop);
            }
            finally
            {
                context.DangerousRelease();
            }
        }

        public int QueryContextChannelBinding(SafeDeleteContext context, Interop.SspiCli.ContextAttribute attribute, out SafeFreeContextBufferChannelBinding binding)
        {
            // Querying an auth SSP for a CBT is not supported.
            throw new NotSupportedException();
        }

        public unsafe int QueryContextAttributes(SafeDeleteContext context, Interop.SspiCli.ContextAttribute attribute, Span<byte> buffer, Type? handleType, out SafeHandle? refHandle)
        {
            refHandle = null;
            if (handleType != null)
            {
                if (handleType == typeof(SafeFreeContextBuffer))
                {
                    refHandle = SafeFreeContextBuffer.CreateEmptyHandle();
                }
                else if (handleType == typeof(SafeFreeCertContext))
                {
                    refHandle = new SafeFreeCertContext();
                }
                else
                {
                    throw new ArgumentException(SR.Format(SR.SSPIInvalidHandleType, handleType.FullName), nameof(handleType));
                }
            }

            fixed (byte* bufferPtr = buffer)
            {
                return SafeFreeContextBuffer.QueryContextAttributes(context, attribute, bufferPtr, refHandle);
            }
        }

        public int QuerySecurityContextToken(SafeDeleteContext phContext, out SecurityContextTokenHandle phToken)
        {
            return GetSecurityContextToken(phContext, out phToken);
        }

        public int CompleteAuthToken(ref SafeDeleteSslContext? refContext, in SecurityBuffer inputBuffer)
        {
            return SafeDeleteContext.CompleteAuthToken(ref refContext, in inputBuffer);
        }

        private static int GetSecurityContextToken(SafeDeleteContext phContext, out SecurityContextTokenHandle safeHandle)
        {
            try
            {
                bool ignore = false;
                phContext.DangerousAddRef(ref ignore);
                return Interop.SspiCli.QuerySecurityContextToken(ref phContext._handle, out safeHandle);
            }
            finally
            {
                phContext.DangerousRelease();
            }
        }

        public int ApplyControlToken(ref SafeDeleteContext? refContext, in SecurityBuffer inputBuffers)
        {
            throw new NotSupportedException();
        }
    }
}
