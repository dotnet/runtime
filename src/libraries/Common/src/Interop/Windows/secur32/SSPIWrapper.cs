// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Globalization;
using System.Net.Security;
using System.Runtime.InteropServices;

namespace System.Net
{
    internal static class SSPIWrapper
    {
        internal static SecurityPackageInfoClass[] EnumerateSecurityPackages(SSPIInterface secModule)
        {
            GlobalLog.Enter("EnumerateSecurityPackages");
            if (secModule.SecurityPackages == null)
            {
                lock (secModule)
                {
                    if (secModule.SecurityPackages == null)
                    {
                        int moduleCount = 0;
                        SafeFreeContextBuffer arrayBaseHandle = null;
                        try
                        {
                            int errorCode = secModule.EnumerateSecurityPackages(out moduleCount, out arrayBaseHandle);
                            GlobalLog.Print("SSPIWrapper::arrayBase: " + (arrayBaseHandle.DangerousGetHandle().ToString("x")));
                            if (errorCode != 0)
                            {
                                throw new Win32Exception(errorCode);
                            }

                            SecurityPackageInfoClass[] securityPackages = new SecurityPackageInfoClass[moduleCount];
                            if (Logging.On)
                            {
                                Logging.PrintInfo(Logging.Web, SR.net_log_sspi_enumerating_security_packages);
                            }

                            int i;
                            for (i = 0; i < moduleCount; i++)
                            {
                                securityPackages[i] = new SecurityPackageInfoClass(arrayBaseHandle, i);
                                if (Logging.On)
                                {
                                    Logging.PrintInfo(Logging.Web, "    " + securityPackages[i].Name);
                                }
                            }

                            secModule.SecurityPackages = securityPackages;
                        }
                        finally
                        {
                            if (arrayBaseHandle != null)
                            {
                                arrayBaseHandle.Dispose();
                            }
                        }
                    }
                }
            }

            GlobalLog.Leave("EnumerateSecurityPackages");
            return secModule.SecurityPackages;
        }

        internal static SecurityPackageInfoClass GetVerifyPackageInfo(SSPIInterface secModule, string packageName)
        {
            return GetVerifyPackageInfo(secModule, packageName, false);
        }

        internal static SecurityPackageInfoClass GetVerifyPackageInfo(SSPIInterface secModule, string packageName, bool throwIfMissing)
        {
            SecurityPackageInfoClass[] supportedSecurityPackages = EnumerateSecurityPackages(secModule);
            if (supportedSecurityPackages != null)
            {
                for (int i = 0; i < supportedSecurityPackages.Length; i++)
                {
                    if (string.Compare(supportedSecurityPackages[i].Name, packageName, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        return supportedSecurityPackages[i];
                    }
                }
            }

            if (Logging.On)
            {
                Logging.PrintInfo(Logging.Web, SR.Format(SR.net_log_sspi_security_package_not_found, packageName));
            }

            if (throwIfMissing)
            {
                throw new NotSupportedException(SR.net_securitypackagesupport);
            }

            return null;
        }

        public static SafeFreeCredentials AcquireDefaultCredential(SSPIInterface secModule, string package, Interop.Secur32.CredentialUse intent)
        {
            GlobalLog.Print("SSPIWrapper::AcquireDefaultCredential(): using " + package);
            if (Logging.On)
            {
                Logging.PrintInfo(
                    Logging.Web,
                    "AcquireDefaultCredential(" +
                    "package = " + package + ", " +
                    "intent  = " + intent + ")");
            }

            SafeFreeCredentials outCredential = null;
            int errorCode = secModule.AcquireDefaultCredential(package, intent, out outCredential);

            if (errorCode != 0)
            {
#if TRACE_VERBOSE
                GlobalLog.Print("SSPIWrapper::AcquireDefaultCredential(): error " + Interop.MapSecurityStatus((uint)errorCode));
#endif

                if (Logging.On)
                {
                    Logging.PrintError(Logging.Web, SR.Format(SR.net_log_operation_failed_with_error, "AcquireDefaultCredential()", String.Format(CultureInfo.CurrentCulture, "0X{0:X}", errorCode)));
                }

                throw new Win32Exception(errorCode);
            }
            return outCredential;
        }

        public static SafeFreeCredentials AcquireCredentialsHandle(SSPIInterface secModule, string package, Interop.Secur32.CredentialUse intent, ref Interop.Secur32.AuthIdentity authdata)
        {
            GlobalLog.Print("SSPIWrapper::AcquireCredentialsHandle#2(): using " + package);

            if (Logging.On)
            {
                Logging.PrintInfo(Logging.Web,
                    "AcquireCredentialsHandle(" +
                    "package  = " + package + ", " +
                    "intent   = " + intent + ", " +
                    "authdata = " + authdata + ")");
            }

            SafeFreeCredentials credentialsHandle = null;
            int errorCode = secModule.AcquireCredentialsHandle(package,
                                                               intent,
                                                               ref authdata,
                                                               out credentialsHandle);

            if (errorCode != 0)
            {
#if TRACE_VERBOSE
                GlobalLog.Print("SSPIWrapper::AcquireCredentialsHandle#2(): error " + Interop.MapSecurityStatus((uint)errorCode));
#endif
                if (Logging.On)
                {
                    Logging.PrintError(Logging.Web, SR.Format(SR.net_log_operation_failed_with_error, "AcquireCredentialsHandle()", String.Format(CultureInfo.CurrentCulture, "0X{0:X}", errorCode)));
                }

                throw new Win32Exception(errorCode);
            }
            return credentialsHandle;
        }

        public static SafeFreeCredentials AcquireCredentialsHandle(SSPIInterface secModule, string package, Interop.Secur32.CredentialUse intent, ref SafeSspiAuthDataHandle authdata)
        {
            if (Logging.On)
            {
                Logging.PrintInfo(Logging.Web,
                    "AcquireCredentialsHandle(" +
                    "package  = " + package + ", " +
                    "intent   = " + intent + ", " +
                    "authdata = " + authdata + ")");
            }

            SafeFreeCredentials credentialsHandle = null;
            int errorCode = secModule.AcquireCredentialsHandle(package, intent, ref authdata, out credentialsHandle);

            if (errorCode != 0)
            {
                if (Logging.On)
                {
                    Logging.PrintError(Logging.Web, SR.Format(SR.net_log_operation_failed_with_error, "AcquireCredentialsHandle()", String.Format(CultureInfo.CurrentCulture, "0X{0:X}", errorCode)));
                }

                throw new Win32Exception(errorCode);
            }

            return credentialsHandle;
        }

        public static SafeFreeCredentials AcquireCredentialsHandle(SSPIInterface secModule, string package, Interop.Secur32.CredentialUse intent, Interop.Secur32.SecureCredential scc)
        {
            GlobalLog.Print("SSPIWrapper::AcquireCredentialsHandle#3(): using " + package);

            if (Logging.On)
            {
                Logging.PrintInfo(Logging.Web,
                    "AcquireCredentialsHandle(" +
                    "package = " + package + ", " +
                    "intent  = " + intent + ", " +
                    "scc     = " + scc + ")");
            }

            SafeFreeCredentials outCredential = null;
            int errorCode = secModule.AcquireCredentialsHandle(
                                            package,
                                            intent,
                                            ref scc,
                                            out outCredential);

            if (errorCode != 0)
            {
#if TRACE_VERBOSE
                GlobalLog.Print("SSPIWrapper::AcquireCredentialsHandle#3(): error " + Interop.MapSecurityStatus((uint)errorCode));
#endif

                if (Logging.On)
                {
                    Logging.PrintError(Logging.Web, SR.Format(SR.net_log_operation_failed_with_error, "AcquireCredentialsHandle()", String.Format(CultureInfo.CurrentCulture, "0X{0:X}", errorCode)));
                }

                throw new Win32Exception(errorCode);
            }

#if TRACE_VERBOSE
            GlobalLog.Print("SSPIWrapper::AcquireCredentialsHandle#3(): cred handle = " + outCredential.ToString());
#endif
            return outCredential;
        }

        internal static int InitializeSecurityContext(SSPIInterface secModule, ref SafeFreeCredentials credential, ref SafeDeleteContext context, string targetName, Interop.Secur32.ContextFlags inFlags, Interop.Secur32.Endianness datarep, SecurityBuffer inputBuffer, SecurityBuffer outputBuffer, ref Interop.Secur32.ContextFlags outFlags)
        {
            if (Logging.On)
            {
                Logging.PrintInfo(Logging.Web,
                    "InitializeSecurityContext(" +
                    "credential = " + credential.ToString() + ", " +
                    "context = " + Logging.ObjectToString(context) + ", " +
                    "targetName = " + targetName + ", " +
                    "inFlags = " + inFlags + ")");
            }

            int errorCode = secModule.InitializeSecurityContext(ref credential, ref context, targetName, inFlags, datarep, inputBuffer, outputBuffer, ref outFlags);

            if (Logging.On)
            {
                Logging.PrintInfo(Logging.Web, SR.Format(SR.net_log_sspi_security_context_input_buffer, "InitializeSecurityContext", (inputBuffer == null ? 0 : inputBuffer.size), outputBuffer.size, (Interop.SecurityStatus)errorCode));
            }

            return errorCode;
        }

        internal static int InitializeSecurityContext(SSPIInterface secModule, SafeFreeCredentials credential, ref SafeDeleteContext context, string targetName, Interop.Secur32.ContextFlags inFlags, Interop.Secur32.Endianness datarep, SecurityBuffer[] inputBuffers, SecurityBuffer outputBuffer, ref Interop.Secur32.ContextFlags outFlags)
        {
            if (Logging.On)
            {
                Logging.PrintInfo(Logging.Web,
                    "InitializeSecurityContext(" +
                    "credential = " + credential.ToString() + ", " +
                    "context = " + Logging.ObjectToString(context) + ", " +
                    "targetName = " + targetName + ", " +
                    "inFlags = " + inFlags + ")");
            }

            int errorCode = secModule.InitializeSecurityContext(credential, ref context, targetName, inFlags, datarep, inputBuffers, outputBuffer, ref outFlags);

            if (Logging.On)
            {
                Logging.PrintInfo(Logging.Web, SR.Format(SR.net_log_sspi_security_context_input_buffers, "InitializeSecurityContext", (inputBuffers == null ? 0 : inputBuffers.Length), outputBuffer.size, (Interop.SecurityStatus)errorCode));
            }

            return errorCode;
        }

        internal static int AcceptSecurityContext(SSPIInterface secModule, ref SafeFreeCredentials credential, ref SafeDeleteContext context, Interop.Secur32.ContextFlags inFlags, Interop.Secur32.Endianness datarep, SecurityBuffer inputBuffer, SecurityBuffer outputBuffer, ref Interop.Secur32.ContextFlags outFlags)
        {
            if (Logging.On)
            {
                Logging.PrintInfo(Logging.Web,
                    "AcceptSecurityContext(" +
                    "credential = " + credential.ToString() + ", " +
                    "context = " + Logging.ObjectToString(context) + ", " +
                    "inFlags = " + inFlags + ")");
            }

            int errorCode = secModule.AcceptSecurityContext(ref credential, ref context, inputBuffer, inFlags, datarep, outputBuffer, ref outFlags);

            if (Logging.On)
            {
                Logging.PrintInfo(Logging.Web, SR.Format(SR.net_log_sspi_security_context_input_buffer, "AcceptSecurityContext", (inputBuffer == null ? 0 : inputBuffer.size), outputBuffer.size, (Interop.SecurityStatus)errorCode));
            }

            return errorCode;
        }

        internal static int AcceptSecurityContext(SSPIInterface secModule, SafeFreeCredentials credential, ref SafeDeleteContext context, Interop.Secur32.ContextFlags inFlags, Interop.Secur32.Endianness datarep, SecurityBuffer[] inputBuffers, SecurityBuffer outputBuffer, ref Interop.Secur32.ContextFlags outFlags)
        {
            if (Logging.On)
            {
                Logging.PrintInfo(Logging.Web,
                    "AcceptSecurityContext(" +
                    "credential = " + credential.ToString() + ", " +
                    "context = " + Logging.ObjectToString(context) + ", " +
                    "inFlags = " + inFlags + ")");
            }

            int errorCode = secModule.AcceptSecurityContext(credential, ref context, inputBuffers, inFlags, datarep, outputBuffer, ref outFlags);

            if (Logging.On)
            {
                Logging.PrintInfo(Logging.Web, SR.Format(SR.net_log_sspi_security_context_input_buffers, "AcceptSecurityContext", (inputBuffers == null ? 0 : inputBuffers.Length), outputBuffer.size, (Interop.SecurityStatus)errorCode));
            }

            return errorCode;
        }

        internal static int CompleteAuthToken(SSPIInterface secModule, ref SafeDeleteContext context, SecurityBuffer[] inputBuffers)
        {
            int errorCode = secModule.CompleteAuthToken(ref context, inputBuffers);

            if (Logging.On)
            {
                Logging.PrintInfo(Logging.Web, SR.Format(SR.net_log_operation_returned_something, "CompleteAuthToken()", (Interop.SecurityStatus)errorCode));
            }

            return errorCode;
        }

        public static int QuerySecurityContextToken(SSPIInterface secModule, SafeDeleteContext context, out SecurityContextTokenHandle token)
        {
            return secModule.QuerySecurityContextToken(context, out token);
        }

        public static int EncryptMessage(SSPIInterface secModule, SafeDeleteContext context, SecurityBuffer[] input, uint sequenceNumber)
        {
            return EncryptDecryptHelper(OP.Encrypt, secModule, context, input, sequenceNumber);
        }

        public static int DecryptMessage(SSPIInterface secModule, SafeDeleteContext context, SecurityBuffer[] input, uint sequenceNumber)
        {
            return EncryptDecryptHelper(OP.Decrypt, secModule, context, input, sequenceNumber);
        }

        internal static int MakeSignature(SSPIInterface secModule, SafeDeleteContext context, SecurityBuffer[] input, uint sequenceNumber)
        {
            return EncryptDecryptHelper(OP.MakeSignature, secModule, context, input, sequenceNumber);
        }

        public static int VerifySignature(SSPIInterface secModule, SafeDeleteContext context, SecurityBuffer[] input, uint sequenceNumber)
        {
            return EncryptDecryptHelper(OP.VerifySignature, secModule, context, input, sequenceNumber);
        }

        private enum OP
        {
            Encrypt = 1,
            Decrypt,
            MakeSignature,
            VerifySignature
        }

        private unsafe static int EncryptDecryptHelper(OP op, SSPIInterface secModule, SafeDeleteContext context, SecurityBuffer[] input, uint sequenceNumber)
        {
            Interop.Secur32.SecurityBufferDescriptor sdcInOut = new Interop.Secur32.SecurityBufferDescriptor(input.Length);
            var unmanagedBuffer = new Interop.Secur32.SecurityBufferStruct[input.Length];

            fixed (Interop.Secur32.SecurityBufferStruct* unmanagedBufferPtr = unmanagedBuffer)
            {
                sdcInOut.UnmanagedPointer = unmanagedBufferPtr;
                GCHandle[] pinnedBuffers = new GCHandle[input.Length];
                byte[][] buffers = new byte[input.Length][];
                try
                {
                    for (int i = 0; i < input.Length; i++)
                    {
                        SecurityBuffer iBuffer = input[i];
                        unmanagedBuffer[i].count = iBuffer.size;
                        unmanagedBuffer[i].type = iBuffer.type;
                        if (iBuffer.token == null || iBuffer.token.Length == 0)
                        {
                            unmanagedBuffer[i].token = IntPtr.Zero;
                        }
                        else
                        {
                            pinnedBuffers[i] = GCHandle.Alloc(iBuffer.token, GCHandleType.Pinned);
                            unmanagedBuffer[i].token = Marshal.UnsafeAddrOfPinnedArrayElement(iBuffer.token, iBuffer.offset);
                            buffers[i] = iBuffer.token;
                        }
                    }

                    // The result is written in the input Buffer passed as type=BufferType.Data.
                    int errorCode;
                    switch (op)
                    {
                        case OP.Encrypt:
                            errorCode = secModule.EncryptMessage(context, sdcInOut, sequenceNumber);
                            break;

                        case OP.Decrypt:
                            errorCode = secModule.DecryptMessage(context, sdcInOut, sequenceNumber);
                            break;

                        case OP.MakeSignature:
                            errorCode = secModule.MakeSignature(context, sdcInOut, sequenceNumber);
                            break;

                        case OP.VerifySignature:
                            errorCode = secModule.VerifySignature(context, sdcInOut, sequenceNumber);
                            break;

                        default:
                            GlobalLog.Assert("SSPIWrapper::EncryptDecryptHelper", "Unknown OP: " + op);
                            throw NotImplemented.ByDesignWithMessage(SR.net_MethodNotImplementedException);
                    }

                    // Marshalling back returned sizes / data.
                    for (int i = 0; i < input.Length; i++)
                    {
                        SecurityBuffer iBuffer = input[i];
                        iBuffer.size = unmanagedBuffer[i].count;
                        iBuffer.type = unmanagedBuffer[i].type;

                        if (iBuffer.size == 0)
                        {
                            iBuffer.offset = 0;
                            iBuffer.token = null;
                        }
                        else
                        {
                            checked
                            {
                                // Find the buffer this is inside of.  Usually they all point inside buffer 0.
                                int j;
                                for (j = 0; j < input.Length; j++)
                                {
                                    if (buffers[j] == null)
                                    {
                                        continue;
                                    }

                                    byte* bufferAddress = (byte*)Marshal.UnsafeAddrOfPinnedArrayElement(buffers[j], 0);
                                    if ((byte*)unmanagedBuffer[i].token >= bufferAddress &&
                                        (byte*)unmanagedBuffer[i].token + iBuffer.size <= bufferAddress + buffers[j].Length)
                                    {
                                        iBuffer.offset = (int)((byte*)unmanagedBuffer[i].token - bufferAddress);
                                        iBuffer.token = buffers[j];
                                        break;
                                    }
                                }

                                if (j >= input.Length)
                                {
                                    GlobalLog.Assert("SSPIWrapper::EncryptDecryptHelper", "Output buffer out of range.");
                                    iBuffer.size = 0;
                                    iBuffer.offset = 0;
                                    iBuffer.token = null;
                                }
                            }
                        }
                        
                        // Backup validate the new sizes.
                        GlobalLog.Assert(iBuffer.offset >= 0 && iBuffer.offset <= (iBuffer.token == null ? 0 : iBuffer.token.Length), "SSPIWrapper::EncryptDecryptHelper|'offset' out of range.  [{0}]", iBuffer.offset);
                        GlobalLog.Assert(iBuffer.size >= 0 && iBuffer.size <= (iBuffer.token == null ? 0 : iBuffer.token.Length - iBuffer.offset), "SSPIWrapper::EncryptDecryptHelper|'size' out of range.  [{0}]", iBuffer.size);
                    }

                    if (errorCode != 0 && Logging.On)
                    {                         
                        if (errorCode == Interop.Secur32.SEC_I_RENEGOTIATE)
                        {
                            Logging.PrintError(Logging.Web, SR.Format(SR.net_log_operation_returned_something, op, "SEC_I_RENEGOTIATE"));
                        }
                        else
                        {
                            Logging.PrintError(Logging.Web, SR.Format(SR.net_log_operation_failed_with_error, op, String.Format(CultureInfo.CurrentCulture, "0X{0:X}", errorCode)));
                        }
                    }

                    return errorCode;
                }
                finally
                {
                    for (int i = 0; i < pinnedBuffers.Length; ++i)
                    {
                        if (pinnedBuffers[i].IsAllocated)
                        {
                            pinnedBuffers[i].Free();
                        }
                    }
                }
            }
        }

        public static SafeFreeContextBufferChannelBinding QueryContextChannelBinding(SSPIInterface secModule, SafeDeleteContext securityContext, Interop.Secur32.ContextAttribute contextAttribute)
        {
            GlobalLog.Enter("QueryContextChannelBinding", contextAttribute.ToString());

            SafeFreeContextBufferChannelBinding result;
            int errorCode = secModule.QueryContextChannelBinding(securityContext, contextAttribute, out result);
            if (errorCode != 0)
            {
                GlobalLog.Leave("QueryContextChannelBinding", "ERROR = " + ErrorDescription(errorCode));
                return null;
            }

            GlobalLog.Leave("QueryContextChannelBinding", Logging.HashString(result));
            return result;
        }

        public static object QueryContextAttributes(SSPIInterface secModule, SafeDeleteContext securityContext, Interop.Secur32.ContextAttribute contextAttribute)
        {
            int errorCode;
            return QueryContextAttributes(secModule, securityContext, contextAttribute, out errorCode);
        }

        public static object QueryContextAttributes(SSPIInterface secModule, SafeDeleteContext securityContext, Interop.Secur32.ContextAttribute contextAttribute, out int errorCode)
        {
            GlobalLog.Enter("QueryContextAttributes", contextAttribute.ToString());

            int nativeBlockSize = IntPtr.Size;
            Type handleType = null;

            switch (contextAttribute)
            {
                case Interop.Secur32.ContextAttribute.Sizes:
                    nativeBlockSize = SecSizes.SizeOf;
                    break;
                case Interop.Secur32.ContextAttribute.StreamSizes:
                    nativeBlockSize = StreamSizes.SizeOf;
                    break;

                case Interop.Secur32.ContextAttribute.Names:
                    handleType = typeof(SafeFreeContextBuffer);
                    break;

                case Interop.Secur32.ContextAttribute.PackageInfo:
                    handleType = typeof(SafeFreeContextBuffer);
                    break;

                case Interop.Secur32.ContextAttribute.NegotiationInfo:
                    handleType = typeof(SafeFreeContextBuffer);
                    nativeBlockSize = Marshal.SizeOf<NegotiationInfo>();
                    break;

                case Interop.Secur32.ContextAttribute.ClientSpecifiedSpn:
                    handleType = typeof(SafeFreeContextBuffer);
                    break;

                case Interop.Secur32.ContextAttribute.RemoteCertificate:
                    handleType = typeof(SafeFreeCertContext);
                    break;

                case Interop.Secur32.ContextAttribute.LocalCertificate:
                    handleType = typeof(SafeFreeCertContext);
                    break;

                case Interop.Secur32.ContextAttribute.IssuerListInfoEx:
                    nativeBlockSize = Marshal.SizeOf<Interop.Secur32.IssuerListInfoEx>();
                    handleType = typeof(SafeFreeContextBuffer);
                    break;

                case Interop.Secur32.ContextAttribute.ConnectionInfo:
                    nativeBlockSize = Marshal.SizeOf<SslConnectionInfo>();
                    break;

                default:
                    throw new ArgumentException(SR.Format(SR.net_invalid_enum, "ContextAttribute"), "contextAttribute");
            }

            SafeHandle sspiHandle = null;
            object attribute = null;

            try
            {
                byte[] nativeBuffer = new byte[nativeBlockSize];
                errorCode = secModule.QueryContextAttributes(securityContext, contextAttribute, nativeBuffer, handleType, out sspiHandle);
                if (errorCode != 0)
                {
                    GlobalLog.Leave("Win32:QueryContextAttributes", "ERROR = " + ErrorDescription(errorCode));
                    return null;
                }

                switch (contextAttribute)
                {
                    case Interop.Secur32.ContextAttribute.Sizes:
                        attribute = new SecSizes(nativeBuffer);
                        break;

                    case Interop.Secur32.ContextAttribute.StreamSizes:
                        attribute = new StreamSizes(nativeBuffer);
                        break;

                    case Interop.Secur32.ContextAttribute.Names:
                        attribute = Marshal.PtrToStringUni(sspiHandle.DangerousGetHandle());
                        break;

                    case Interop.Secur32.ContextAttribute.PackageInfo:
                        attribute = new SecurityPackageInfoClass(sspiHandle, 0);
                        break;

                    case Interop.Secur32.ContextAttribute.NegotiationInfo:
                        unsafe
                        {
                            fixed (void* ptr = nativeBuffer)
                            {
                                attribute = new NegotiationInfoClass(sspiHandle, Marshal.ReadInt32(new IntPtr(ptr), NegotiationInfo.NegotiationStateOffest));
                            }
                        }
                        break;

                    case Interop.Secur32.ContextAttribute.ClientSpecifiedSpn:
                        attribute = Marshal.PtrToStringUni(sspiHandle.DangerousGetHandle());
                        break;

                    case Interop.Secur32.ContextAttribute.LocalCertificate:
                        // Fall-through to RemoteCertificate is intentional.
                    case Interop.Secur32.ContextAttribute.RemoteCertificate:
                        attribute = sspiHandle;
                        sspiHandle = null;
                        break;

                    case Interop.Secur32.ContextAttribute.IssuerListInfoEx:
                        attribute = new Interop.Secur32.IssuerListInfoEx(sspiHandle, nativeBuffer);
                        sspiHandle = null;
                        break;

                    case Interop.Secur32.ContextAttribute.ConnectionInfo:
                        attribute = new SslConnectionInfo(nativeBuffer);
                        break;
                    default:
                        // Will return null.
                        break;
                }
            }
            finally
            {
                if (sspiHandle != null)
                {
                    sspiHandle.Dispose();
                }
            }
            GlobalLog.Leave("QueryContextAttributes", Logging.ObjectToString(attribute));
            return attribute;
        }

        public static string ErrorDescription(int errorCode)
        {
            if (errorCode == -1)
            {
                return "An exception when invoking Win32 API";
            }

            switch ((Interop.SecurityStatus)errorCode)
            {
                case Interop.SecurityStatus.InvalidHandle:
                    return "Invalid handle";
                case Interop.SecurityStatus.InvalidToken:
                    return "Invalid token";
                case Interop.SecurityStatus.ContinueNeeded:
                    return "Continue needed";
                case Interop.SecurityStatus.IncompleteMessage:
                    return "Message incomplete";
                case Interop.SecurityStatus.WrongPrincipal:
                    return "Wrong principal";
                case Interop.SecurityStatus.TargetUnknown:
                    return "Target unknown";
                case Interop.SecurityStatus.PackageNotFound:
                    return "Package not found";
                case Interop.SecurityStatus.BufferNotEnough:
                    return "Buffer not enough";
                case Interop.SecurityStatus.MessageAltered:
                    return "Message altered";
                case Interop.SecurityStatus.UntrustedRoot:
                    return "Untrusted root";
                default:
                    return "0x" + errorCode.ToString("x", NumberFormatInfo.InvariantInfo);
            }
        }
    } // class SSPIWrapper
}
