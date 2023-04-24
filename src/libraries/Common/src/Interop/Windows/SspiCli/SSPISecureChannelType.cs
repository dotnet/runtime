// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security;
using System.Runtime.InteropServices;

namespace System.Net
{
    // Schannel SSPI interface.
    internal sealed class SSPISecureChannelType : ISSPIInterface
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

        public unsafe int AcquireCredentialsHandle(string moduleName, Interop.SspiCli.CredentialUse usage, Interop.SspiCli.SCHANNEL_CRED* authdata, out SafeFreeCredentials outCredential)
        {
            return SafeFreeCredentials.AcquireCredentialsHandle(moduleName, usage, authdata, out outCredential);
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

        public int EncryptMessage(SafeDeleteContext context, ref Interop.SspiCli.SecBufferDesc inputOutput, uint qop)
        {
            try
            {
                bool ignore = false;
                context.DangerousAddRef(ref ignore);
                return Interop.SspiCli.EncryptMessage(ref context._handle, qop, ref inputOutput, 0);
            }
            finally
            {
                context.DangerousRelease();
            }
        }

        public unsafe int DecryptMessage(SafeDeleteContext context, ref Interop.SspiCli.SecBufferDesc inputOutput, out uint qop)
        {
            int status = (int)Interop.SECURITY_STATUS.InvalidHandle;
            uint qopTemp = 0;

            try
            {
                bool ignore = false;
                context.DangerousAddRef(ref ignore);
                status = Interop.SspiCli.DecryptMessage(ref context._handle, ref inputOutput, 0, &qopTemp);
            }
            finally
            {
                context.DangerousRelease();
            }

            qop = qopTemp;
            return status;
        }

        public unsafe int QueryContextChannelBinding(SafeDeleteContext phContext, Interop.SspiCli.ContextAttribute attribute, out SafeFreeContextBufferChannelBinding refHandle)
        {
            refHandle = SafeFreeContextBufferChannelBinding.CreateEmptyHandle();

            // Bindings is on the stack, so there's no need for a fixed block.
            SecPkgContext_Bindings bindings = default;
            return SafeFreeContextBufferChannelBinding.QueryContextChannelBinding(phContext, attribute, &bindings, refHandle);
        }

        public unsafe int QueryContextAttributes(SafeDeleteContext phContext, Interop.SspiCli.ContextAttribute attribute, Span<byte> buffer, Type? handleType, out SafeHandle? refHandle)
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
                return SafeFreeContextBuffer.QueryContextAttributes(phContext, attribute, bufferPtr, refHandle);
            }
        }

        public int QuerySecurityContextToken(SafeDeleteContext phContext, out SecurityContextTokenHandle phToken)
        {
            throw new NotSupportedException();
        }

        public int CompleteAuthToken(ref SafeDeleteSslContext? refContext, in InputSecurityBuffer inputBuffer)
        {
            throw new NotSupportedException();
        }

        public int ApplyControlToken(ref SafeDeleteContext? refContext, in SecurityBuffer inputBuffer)
        {
            return SafeDeleteContext.ApplyControlToken(ref refContext, in inputBuffer);
        }
    }
}
