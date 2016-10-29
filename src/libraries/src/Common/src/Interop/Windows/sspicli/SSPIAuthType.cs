// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.Net.Security;
using System.Runtime.InteropServices;

namespace System.Net
{
    // Authentication SSPI (Kerberos, NTLM, Negotiate and WDigest):
    internal class SSPIAuthType : SSPIInterface
    {
        private static volatile SecurityPackageInfoClass[] s_securityPackages;

        public SecurityPackageInfoClass[] SecurityPackages
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
            if (NetEventSource.IsEnabled) NetEventSource.Info(this);
            return SafeFreeContextBuffer.EnumeratePackages(out pkgnum, out pkgArray);
        }

        public int AcquireCredentialsHandle(string moduleName, Interop.SspiCli.CredentialUse usage, ref Interop.SspiCli.SEC_WINNT_AUTH_IDENTITY_W authdata, out SafeFreeCredentials outCredential)
        {
            return SafeFreeCredentials.AcquireCredentialsHandle(moduleName, usage, ref authdata, out outCredential);
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

        public int AcceptSecurityContext(ref SafeFreeCredentials credential, ref SafeDeleteContext context, SecurityBuffer inputBuffer, Interop.SspiCli.ContextFlags inFlags, Interop.SspiCli.Endianness endianness, SecurityBuffer outputBuffer, ref Interop.SspiCli.ContextFlags outFlags)
        {
            return SafeDeleteContext.AcceptSecurityContext(ref credential, ref context, inFlags, endianness, inputBuffer, null, outputBuffer, ref outFlags);
        }

        public int AcceptSecurityContext(SafeFreeCredentials credential, ref SafeDeleteContext context, SecurityBuffer[] inputBuffers, Interop.SspiCli.ContextFlags inFlags, Interop.SspiCli.Endianness endianness, SecurityBuffer outputBuffer, ref Interop.SspiCli.ContextFlags outFlags)
        {
            return SafeDeleteContext.AcceptSecurityContext(ref credential, ref context, inFlags, endianness, null, inputBuffers, outputBuffer, ref outFlags);
        }

        public int InitializeSecurityContext(ref SafeFreeCredentials credential, ref SafeDeleteContext context, string targetName, Interop.SspiCli.ContextFlags inFlags, Interop.SspiCli.Endianness endianness, SecurityBuffer inputBuffer, SecurityBuffer outputBuffer, ref Interop.SspiCli.ContextFlags outFlags)
        {
            return SafeDeleteContext.InitializeSecurityContext(ref credential, ref context, targetName, inFlags, endianness, inputBuffer, null, outputBuffer, ref outFlags);
        }

        public int InitializeSecurityContext(SafeFreeCredentials credential, ref SafeDeleteContext context, string targetName, Interop.SspiCli.ContextFlags inFlags, Interop.SspiCli.Endianness endianness, SecurityBuffer[] inputBuffers, SecurityBuffer outputBuffer, ref Interop.SspiCli.ContextFlags outFlags)
        {
            return SafeDeleteContext.InitializeSecurityContext(ref credential, ref context, targetName, inFlags, endianness, null, inputBuffers, outputBuffer, ref outFlags);
        }

        public int EncryptMessage(SafeDeleteContext context, Interop.SspiCli.SecBufferDesc inputOutput, uint sequenceNumber)
        {
            try
            {
                bool ignore = false;

                context.DangerousAddRef(ref ignore);
                return Interop.SspiCli.EncryptMessage(ref context._handle, 0, inputOutput, sequenceNumber);
            }
            finally
            {
                context.DangerousRelease();
            }
        }

        public unsafe int DecryptMessage(SafeDeleteContext context, Interop.SspiCli.SecBufferDesc inputOutput, uint sequenceNumber)
        {
            int status = (int)Interop.SECURITY_STATUS.InvalidHandle;
            uint qop = 0;

            try
            {
                bool ignore = false;
                context.DangerousAddRef(ref ignore);
                status = Interop.SspiCli.DecryptMessage(ref context._handle, inputOutput, sequenceNumber, &qop);
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

        public int MakeSignature(SafeDeleteContext context, Interop.SspiCli.SecBufferDesc inputOutput, uint sequenceNumber)
        {
            try
            {
                bool ignore = false;

                context.DangerousAddRef(ref ignore);

                return Interop.SspiCli.EncryptMessage(ref context._handle, Interop.SspiCli.SECQOP_WRAP_NO_ENCRYPT, inputOutput, sequenceNumber);
            }
            finally
            {
                context.DangerousRelease();
            }
        }

        public unsafe int VerifySignature(SafeDeleteContext context, Interop.SspiCli.SecBufferDesc inputOutput, uint sequenceNumber)
        {
            try
            {
                bool ignore = false;
                uint qop = 0;

                context.DangerousAddRef(ref ignore);
                return Interop.SspiCli.DecryptMessage(ref context._handle, inputOutput, sequenceNumber, &qop);
            }
            finally
            {
                context.DangerousRelease();
            }
        }

        public int QueryContextChannelBinding(SafeDeleteContext context, Interop.SspiCli.ContextAttribute attribute, out SafeFreeContextBufferChannelBinding binding)
        {
            // Querying an auth SSP for a CBT is not supported.
            binding = null;
            throw new NotSupportedException();
        }

        public unsafe int QueryContextAttributes(SafeDeleteContext context, Interop.SspiCli.ContextAttribute attribute, byte[] buffer, Type handleType, out SafeHandle refHandle)
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

        public int SetContextAttributes(SafeDeleteContext context, Interop.SspiCli.ContextAttribute attribute, byte[] buffer)
        {
            throw NotImplemented.ByDesignWithMessage(SR.net_MethodNotImplementedException);
        }

        public int QuerySecurityContextToken(SafeDeleteContext phContext, out SecurityContextTokenHandle phToken)
        {
            return GetSecurityContextToken(phContext, out phToken);
        }

        public int CompleteAuthToken(ref SafeDeleteContext refContext, SecurityBuffer[] inputBuffers)
        {
            return SafeDeleteContext.CompleteAuthToken(ref refContext, inputBuffers);
        }

        private static int GetSecurityContextToken(SafeDeleteContext phContext, out SecurityContextTokenHandle safeHandle)
        {
            safeHandle = null;

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

        public int ApplyControlToken(ref SafeDeleteContext refContext, SecurityBuffer[] inputBuffers)
        {
            throw new NotSupportedException();
        }
    }
}
