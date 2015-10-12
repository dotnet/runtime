// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Security;
using System.Runtime.InteropServices;

namespace System.Net
{
    // Schannel SSPI interface.
    internal class SSPISecureChannelType : SSPIInterface
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
            GlobalLog.Print("SSPISecureChannelType::EnumerateSecurityPackages()");
            return SafeFreeContextBuffer.EnumeratePackages(out pkgnum, out pkgArray);
        }

        public int AcquireCredentialsHandle(string moduleName, Interop.Secur32.CredentialUse usage, ref Interop.Secur32.AuthIdentity authdata, out SafeFreeCredentials outCredential)
        {
            return SafeFreeCredentials.AcquireCredentialsHandle(moduleName, usage, ref authdata, out outCredential);
        }

        public int AcquireCredentialsHandle(string moduleName, Interop.Secur32.CredentialUse usage, ref SafeSspiAuthDataHandle authdata, out SafeFreeCredentials outCredential)
        {
            return SafeFreeCredentials.AcquireCredentialsHandle(moduleName, usage, ref authdata, out outCredential);
        }

        public int AcquireDefaultCredential(string moduleName, Interop.Secur32.CredentialUse usage, out SafeFreeCredentials outCredential)
        {
            return SafeFreeCredentials.AcquireDefaultCredential(moduleName, usage, out outCredential);
        }

        public int AcquireCredentialsHandle(string moduleName, Interop.Secur32.CredentialUse usage, ref Interop.Secur32.SecureCredential authdata, out SafeFreeCredentials outCredential)
        {
            return SafeFreeCredentials.AcquireCredentialsHandle(moduleName, usage, ref authdata, out outCredential);
        }

        public int AcceptSecurityContext(ref SafeFreeCredentials credential, ref SafeDeleteContext context, SecurityBuffer inputBuffer, Interop.Secur32.ContextFlags inFlags, Interop.Secur32.Endianness endianness, SecurityBuffer outputBuffer, ref Interop.Secur32.ContextFlags outFlags)
        {
            return SafeDeleteContext.AcceptSecurityContext(ref credential, ref context, inFlags, endianness, inputBuffer, null, outputBuffer, ref outFlags);
        }

        public int AcceptSecurityContext(SafeFreeCredentials credential, ref SafeDeleteContext context, SecurityBuffer[] inputBuffers, Interop.Secur32.ContextFlags inFlags, Interop.Secur32.Endianness endianness, SecurityBuffer outputBuffer, ref Interop.Secur32.ContextFlags outFlags)
        {
            return SafeDeleteContext.AcceptSecurityContext(ref credential, ref context, inFlags, endianness, null, inputBuffers, outputBuffer, ref outFlags);
        }

        public int InitializeSecurityContext(ref SafeFreeCredentials credential, ref SafeDeleteContext context, string targetName, Interop.Secur32.ContextFlags inFlags, Interop.Secur32.Endianness endianness, SecurityBuffer inputBuffer, SecurityBuffer outputBuffer, ref Interop.Secur32.ContextFlags outFlags)
        {
            return SafeDeleteContext.InitializeSecurityContext(ref credential, ref context, targetName, inFlags, endianness, inputBuffer, null, outputBuffer, ref outFlags);
        }

        public int InitializeSecurityContext(SafeFreeCredentials credential, ref SafeDeleteContext context, string targetName, Interop.Secur32.ContextFlags inFlags, Interop.Secur32.Endianness endianness, SecurityBuffer[] inputBuffers, SecurityBuffer outputBuffer, ref Interop.Secur32.ContextFlags outFlags)
        {
            return SafeDeleteContext.InitializeSecurityContext(ref credential, ref context, targetName, inFlags, endianness, null, inputBuffers, outputBuffer, ref outFlags);
        }

        public int EncryptMessage(SafeDeleteContext context, Interop.Secur32.SecurityBufferDescriptor inputOutput, uint sequenceNumber)
        {
            try
            {
                bool ignore = false;
                context.DangerousAddRef(ref ignore);
                return Interop.Secur32.EncryptMessage(ref context._handle, 0, inputOutput, sequenceNumber);
            }
            finally
            {
                context.DangerousRelease();
            }
        }

        public unsafe int DecryptMessage(SafeDeleteContext context, Interop.Secur32.SecurityBufferDescriptor inputOutput,
            uint sequenceNumber)
        {
            try
            {
                bool ignore = false;
                context.DangerousAddRef(ref ignore);
                return Interop.Secur32.DecryptMessage(ref context._handle, inputOutput, sequenceNumber, null);
            }
            finally
            {
                context.DangerousRelease();
            }
        }

        public int MakeSignature(SafeDeleteContext context, Interop.Secur32.SecurityBufferDescriptor inputOutput, uint sequenceNumber)
        {
            throw NotImplemented.ByDesignWithMessage(SR.net_MethodNotImplementedException);
        }

        public int VerifySignature(SafeDeleteContext context, Interop.Secur32.SecurityBufferDescriptor inputOutput, uint sequenceNumber)
        {
            throw NotImplemented.ByDesignWithMessage(SR.net_MethodNotImplementedException);
        }

        public unsafe int QueryContextChannelBinding(SafeDeleteContext phContext, Interop.Secur32.ContextAttribute attribute, out SafeFreeContextBufferChannelBinding refHandle)
        {
            refHandle = SafeFreeContextBufferChannelBinding.CreateEmptyHandle();

            // Bindings is on the stack, so there's no need for a fixed block.
            Bindings bindings = new Bindings();
            return SafeFreeContextBufferChannelBinding.QueryContextChannelBinding(phContext, attribute, &bindings, refHandle);
        }

        public unsafe int QueryContextAttributes(SafeDeleteContext phContext, Interop.Secur32.ContextAttribute attribute, byte[] buffer, Type handleType, out SafeHandle refHandle)
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
                    throw new ArgumentException(SR.Format(SR.SSPIInvalidHandleType, handleType.FullName), "handleType");
                }
            }
            fixed (byte* bufferPtr = buffer)
            {
                return SafeFreeContextBuffer.QueryContextAttributes(phContext, attribute, bufferPtr, refHandle);
            }
        }

        public int SetContextAttributes(SafeDeleteContext phContext, Interop.Secur32.ContextAttribute attribute, byte[] buffer)
        {
            return SafeFreeContextBuffer.SetContextAttributes(phContext, attribute, buffer);
        }

        public int QuerySecurityContextToken(SafeDeleteContext phContext, out SecurityContextTokenHandle phToken)
        {
            throw new NotSupportedException();
        }

        public int CompleteAuthToken(ref SafeDeleteContext refContext, SecurityBuffer[] inputBuffers)
        {
            throw new NotSupportedException();
        }
    }
}
