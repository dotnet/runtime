// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using System.Globalization;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.ComponentModel;
using System.Security.Principal;
using System.Security.Authentication.ExtendedProtection;
using System.Security;
using System.Collections;
using System.Diagnostics;
using Microsoft.Win32.SafeHandles;
using System.Security.Authentication;

namespace System.Net
{ 

    // For Authentication (Kerberos, NTLM, Negotiate and WDigest):
    internal class SSPIAuthType: SSPIInterfaceNego
    {

        public void VerifyPackageInfo()
        {
            throw new NotImplementedException();
        }

        public int AcquireCredentialsHandle(string moduleName, bool IsInBoundCred, ref SafeSspiAuthDataHandle authdata, out SafeFreeCredentials outCredential)
        {
            Interop.Secur32.CredentialUse intent = IsInBoundCred ? Interop.Secur32.CredentialUse.Inbound : Interop.Secur32.CredentialUse.Outbound;
            return SafeFreeCredentials.AcquireCredentialsHandle(moduleName, intent, ref authdata, out outCredential);
        }

        public int AcquireDefaultCredential(string moduleName, bool IsInBoundCred, out SafeFreeCredentials outCredential)
        {
            Interop.Secur32.CredentialUse intent = IsInBoundCred ? Interop.Secur32.CredentialUse.Inbound : Interop.Secur32.CredentialUse.Outbound;
            return SafeFreeCredentials.AcquireDefaultCredential(moduleName, intent, out outCredential);
        }

        public int AcquireCredentialsHandle(string moduleName, bool IsInBoundCred, ref Interop.Secur32.SecureCredential authdata, out SafeFreeCredentials outCredential)
        {
            Interop.Secur32.CredentialUse intent = IsInBoundCred ? Interop.Secur32.CredentialUse.Inbound : Interop.Secur32.CredentialUse.Outbound;
            return SafeFreeCredentials.AcquireCredentialsHandle(moduleName, intent, ref authdata, out outCredential);
        }

        public int AcceptSecurityContext(ref SafeFreeCredentials credential, ref SafeDeleteContext context, SecurityBuffer inputBuffer, SecurityBuffer outputBuffer, bool remoteCertRequired)
        {
            Interop.Secur32.ContextFlags outFlags = Interop.Secur32.ContextFlags.Zero;
            return SafeDeleteContext.AcceptSecurityContext(
                                        ref credential,
                                        ref context,
                                        ServerRequiredFlags | (remoteCertRequired ? Interop.Secur32.ContextFlags.MutualAuth : Interop.Secur32.ContextFlags.Zero),
                                        Interop.Secur32.Endianness.Native,
                                        inputBuffer,
                                        null,
                                        outputBuffer,
                                        ref outFlags
                                        );
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
            int status = (int)Interop.SecurityStatus.InvalidHandle;
            bool ignore = false;

            context.DangerousAddRef(ref ignore);
            status = Interop.Secur32.EncryptMessage(ref context._handle, 0, inputOutput, sequenceNumber);
            context.DangerousRelease();

            return status;
        }

        public unsafe int DecryptMessage(SafeDeleteContext context, Interop.Secur32.SecurityBufferDescriptor inputOutput, uint sequenceNumber)
        {
            int status = (int)Interop.SecurityStatus.InvalidHandle;
            uint qop = 0;

            try
            {
                bool ignore = false;
                context.DangerousAddRef(ref ignore);
                status = Interop.Secur32.DecryptMessage(ref context._handle, inputOutput, sequenceNumber, &qop);
            }
            finally
            {
                context.DangerousRelease();
            }

            const uint SECQOP_WRAP_NO_ENCRYPT = 0x80000001;
            if (status == 0 && qop == SECQOP_WRAP_NO_ENCRYPT)
            {
                GlobalLog.Assert("Secur32.DecryptMessage", "Expected qop = 0, returned value = " + qop.ToString("x", CultureInfo.InvariantCulture));
                throw new InvalidOperationException(SR.net_auth_message_not_encrypted);
            }


            return status;
        }

        public int MakeSignature(SafeDeleteContext context, Interop.Secur32.SecurityBufferDescriptor inputOutput, uint sequenceNumber)
        {
            try
            {
                bool ignore = false;

                context.DangerousAddRef(ref ignore);
                
                return Interop.Secur32.EncryptMessage(ref context._handle, Interop.Secur32.SECQOP_WRAP_NO_ENCRYPT, inputOutput, sequenceNumber);
            }
            finally
            {
                context.DangerousRelease();
            }
        }

        public Interop.SecurityStatus VerifySignature(SafeDeleteContext securityContext, byte[] payload, uint sequenceNumber)
        {
            try
            {
               bool ignore = false;
                uint qop = 0;

                context.DangerousAddRef(ref ignore);
                return Interop.Secur32.DecryptMessage(ref context._handle, inputOutput, sequenceNumber, &qop);
            }
            finally
            {
                context.DangerousRelease();
            }
        }

        public int QueryContextChannelBinding(SafeDeleteContext context, Interop.Secur32.ContextAttribute attribute, out SafeFreeContextBufferChannelBinding binding)
        {
            // Querying an auth SSP for a CBT doesn't make sense
            binding = null;
            throw new NotSupportedException();
        }

        public unsafe int QueryContextAttributes(SafeDeleteContext context, Interop.Secur32.ContextAttribute attribute, byte[] buffer, Type handleType, out SafeHandle refHandle)
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
                return SafeFreeContextBuffer.QueryContextAttributes(context, attribute, bufferPtr, refHandle);
            }
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
            int status = (int)Interop.SecurityStatus.InvalidHandle;
            safeHandle = null;

            try
            {
                bool ignore = false;
                phContext.DangerousAddRef(ref ignore);
                status = Interop.Secur32.QuerySecurityContextToken(ref phContext._handle, out safeHandle);
            }
            finally
            {
                phContext.DangerousRelease();
            }

            return status;
        }
    }


}