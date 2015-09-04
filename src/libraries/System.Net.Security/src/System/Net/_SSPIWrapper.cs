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
                                                               out credentialsHandle
                                                               );

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
                                            out outCredential
                                            );
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


    [StructLayout(LayoutKind.Sequential)]
    internal class StreamSizes
    {
        public int header;
        public int trailer;
        public int maximumMessage;
        public int buffersCount;
        public int blockSize;

        internal unsafe StreamSizes(byte[] memory)
        {
            fixed (void* voidPtr = memory)
            {
                IntPtr unmanagedAddress = new IntPtr(voidPtr);
                try
                {
                    header = (int)checked((uint)Marshal.ReadInt32(unmanagedAddress));
                    trailer = (int)checked((uint)Marshal.ReadInt32(unmanagedAddress, 4));
                    maximumMessage = (int)checked((uint)Marshal.ReadInt32(unmanagedAddress, 8));
                    buffersCount = (int)checked((uint)Marshal.ReadInt32(unmanagedAddress, 12));
                    blockSize = (int)checked((uint)Marshal.ReadInt32(unmanagedAddress, 16));
                }
                catch (OverflowException)
                {
                    GlobalLog.Assert(false, "StreamSizes::.ctor", "Negative size.");
                    throw;
                }
            }
        }
        public static readonly int SizeOf = Marshal.SizeOf<StreamSizes>();
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class SecSizes
    {
        public readonly int MaxToken;
        public readonly int MaxSignature;
        public readonly int BlockSize;
        public readonly int SecurityTrailer;

        internal unsafe SecSizes(byte[] memory)
        {
            fixed (void* voidPtr = memory)
            {
                IntPtr unmanagedAddress = new IntPtr(voidPtr);
                try
                {
                    MaxToken = (int)checked((uint)Marshal.ReadInt32(unmanagedAddress));
                    MaxSignature = (int)checked((uint)Marshal.ReadInt32(unmanagedAddress, 4));
                    BlockSize = (int)checked((uint)Marshal.ReadInt32(unmanagedAddress, 8));
                    SecurityTrailer = (int)checked((uint)Marshal.ReadInt32(unmanagedAddress, 12));
                }
                catch (OverflowException)
                {
                    GlobalLog.Assert(false, "SecSizes::.ctor", "Negative size.");
                    throw;
                }
            }
        }
        public static readonly int SizeOf = Marshal.SizeOf<SecSizes>();
    }

    // TODO (Issue #3114): Move to Interop. 
    // Investigate if this can be safely converted to a struct.
    // From Schannel.h
    [StructLayout(LayoutKind.Sequential)]
    internal class SslConnectionInfo
    {
        public readonly int Protocol;
        public readonly int DataCipherAlg;
        public readonly int DataKeySize;
        public readonly int DataHashAlg;
        public readonly int DataHashKeySize;
        public readonly int KeyExchangeAlg;
        public readonly int KeyExchKeySize;

        internal unsafe SslConnectionInfo(byte[] nativeBuffer)
        {
            fixed (void* voidPtr = nativeBuffer)
            {
                try
                {
                    // TODO (Issue #3114): replace with Marshal.PtrToStructure.
                    IntPtr unmanagedAddress = new IntPtr(voidPtr);
                    Protocol = Marshal.ReadInt32(unmanagedAddress);
                    DataCipherAlg = Marshal.ReadInt32(unmanagedAddress, 4);
                    DataKeySize = Marshal.ReadInt32(unmanagedAddress, 8);
                    DataHashAlg = Marshal.ReadInt32(unmanagedAddress, 12);
                    DataHashKeySize = Marshal.ReadInt32(unmanagedAddress, 16);
                    KeyExchangeAlg = Marshal.ReadInt32(unmanagedAddress, 20);
                    KeyExchKeySize = Marshal.ReadInt32(unmanagedAddress, 24);
                }
                catch (OverflowException)
                {
                    GlobalLog.Assert(false, "SslConnectionInfo::.ctor", "Negative size.");
                    throw;
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct NegotiationInfo
    {
        // see SecPkgContext_NegotiationInfoW in <sspi.h>

        // [MarshalAs(UnmanagedType.LPStruct)] internal SecurityPackageInfo PackageInfo;
        internal IntPtr PackageInfo;
        internal uint NegotiationState;
        internal static readonly int Size = Marshal.SizeOf<NegotiationInfo>();
        internal static readonly int NegotiationStateOffest = (int)Marshal.OffsetOf<NegotiationInfo>("NegotiationState");
    }

    // we keep it simple since we use this only to know if NTLM or
    // Kerberos are used in the context of a Negotiate handshake
    internal class NegotiationInfoClass
    {
        internal const string NTLM = "NTLM";
        internal const string Kerberos = "Kerberos";
        internal const string WDigest = "WDigest";
        internal const string Negotiate = "Negotiate";
        internal string AuthenticationPackage;

        internal NegotiationInfoClass(SafeHandle safeHandle, int negotiationState)
        {
            if (safeHandle.IsInvalid)
            {
                GlobalLog.Print("NegotiationInfoClass::.ctor() the handle is invalid:" + (safeHandle.DangerousGetHandle()).ToString("x"));
                return;
            }

            IntPtr packageInfo = safeHandle.DangerousGetHandle();
            GlobalLog.Print("NegotiationInfoClass::.ctor() packageInfo:" + packageInfo.ToString("x8") + " negotiationState:" + negotiationState.ToString("x8"));

            if (negotiationState == Interop.Secur32.SECPKG_NEGOTIATION_COMPLETE 
                || negotiationState == Interop.Secur32.SECPKG_NEGOTIATION_OPTIMISTIC)
            {
                IntPtr unmanagedString = Marshal.ReadIntPtr(packageInfo, SecurityPackageInfo.NameOffest);
                string name = null;
                if (unmanagedString != IntPtr.Zero)
                {
                    name = Marshal.PtrToStringUni(unmanagedString);
                }

                GlobalLog.Print("NegotiationInfoClass::.ctor() packageInfo:" + packageInfo.ToString("x8") + " negotiationState:" + negotiationState.ToString("x8") + " name:" + Logging.ObjectToString(name));

                // an optimization for future string comparisons
                if (string.Compare(name, Kerberos, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    AuthenticationPackage = Kerberos;
                }
                else if (string.Compare(name, NTLM, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    AuthenticationPackage = NTLM;
                }
                else if (string.Compare(name, WDigest, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    AuthenticationPackage = WDigest;
                }
                else
                {
                    AuthenticationPackage = name;
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SecurityPackageInfo
    {
        // see SecPkgInfoW in <sspi.h>
        internal int Capabilities;
        internal short Version;
        internal short RPCID;
        internal int MaxToken;
        internal IntPtr Name;
        internal IntPtr Comment;

        internal static readonly int Size = Marshal.SizeOf<SecurityPackageInfo>();
        internal static readonly int NameOffest = (int)Marshal.OffsetOf<SecurityPackageInfo>("Name");
    }

    internal class SecurityPackageInfoClass
    {
        internal int Capabilities = 0;
        internal short Version = 0;
        internal short RPCID = 0;
        internal int MaxToken = 0;
        internal string Name = null;
        internal string Comment = null;

        /*
         *  This is to support SSL under semi trusted environment.
         *  Note that it is only for SSL with no client cert
         *
         *  Important: safeHandle should not be Disposed during construction of this object.
         */
        internal SecurityPackageInfoClass(SafeHandle safeHandle, int index)
        {
            if (safeHandle.IsInvalid)
            {
                GlobalLog.Print("SecurityPackageInfoClass::.ctor() the pointer is invalid: " + (safeHandle.DangerousGetHandle()).ToString("x"));
                return;
            }

            IntPtr unmanagedAddress = IntPtrHelper.Add(safeHandle.DangerousGetHandle(), SecurityPackageInfo.Size * index);
            GlobalLog.Print("SecurityPackageInfoClass::.ctor() unmanagedPointer: " + ((long)unmanagedAddress).ToString("x"));

            Capabilities = Marshal.ReadInt32(unmanagedAddress, (int)Marshal.OffsetOf<SecurityPackageInfo>("Capabilities"));
            Version = Marshal.ReadInt16(unmanagedAddress, (int)Marshal.OffsetOf<SecurityPackageInfo>("Version"));
            RPCID = Marshal.ReadInt16(unmanagedAddress, (int)Marshal.OffsetOf<SecurityPackageInfo>("RPCID"));
            MaxToken = Marshal.ReadInt32(unmanagedAddress, (int)Marshal.OffsetOf<SecurityPackageInfo>("MaxToken"));

            IntPtr unmanagedString;

            unmanagedString = Marshal.ReadIntPtr(unmanagedAddress, (int)Marshal.OffsetOf<SecurityPackageInfo>("Name"));
            if (unmanagedString != IntPtr.Zero)
            {
                Name = Marshal.PtrToStringUni(unmanagedString);
                GlobalLog.Print("Name: " + Name);
            }

            unmanagedString = Marshal.ReadIntPtr(unmanagedAddress, (int)Marshal.OffsetOf<SecurityPackageInfo>("Comment"));
            if (unmanagedString != IntPtr.Zero)
            {
                Comment = Marshal.PtrToStringUni(unmanagedString);
                GlobalLog.Print("Comment: " + Comment);
            }

            GlobalLog.Print("SecurityPackageInfoClass::.ctor(): " + ToString());
        }

        public override string ToString()
        {
            return "Capabilities:" + String.Format(CultureInfo.InvariantCulture, "0x{0:x}", Capabilities)
                + " Version:" + Version.ToString(NumberFormatInfo.InvariantInfo)
                + " RPCID:" + RPCID.ToString(NumberFormatInfo.InvariantInfo)
                + " MaxToken:" + MaxToken.ToString(NumberFormatInfo.InvariantInfo)
                + " Name:" + ((Name == null) ? "(null)" : Name)
                + " Comment:" + ((Comment == null) ? "(null)" : Comment
                );
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Bindings
    {
        // see SecPkgContext_Bindings in <sspi.h>
        internal int BindingsLength;
        internal IntPtr pBindings;
    }
}
