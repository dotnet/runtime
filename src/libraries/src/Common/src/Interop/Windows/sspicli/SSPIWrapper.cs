// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Net.Security;
using System.Runtime.InteropServices;

namespace System.Net
{
    internal static class SSPIWrapper
    {
        internal static SecurityPackageInfoClass[] EnumerateSecurityPackages(SSPIInterface secModule)
        {
            if (GlobalLog.IsEnabled)
            {
                GlobalLog.Enter("EnumerateSecurityPackages");
            }

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
                            if (GlobalLog.IsEnabled)
                            {
                                GlobalLog.Print("SSPIWrapper::arrayBase: " + (arrayBaseHandle.DangerousGetHandle().ToString("x")));
                            }
                            if (errorCode != 0)
                            {
                                throw new Win32Exception(errorCode);
                            }

                            var securityPackages = new SecurityPackageInfoClass[moduleCount];

                            int i;
                            for (i = 0; i < moduleCount; i++)
                            {
                                securityPackages[i] = new SecurityPackageInfoClass(arrayBaseHandle, i);
                                if (SecurityEventSource.Log.IsEnabled())
                                {
                                    SecurityEventSource.Log.EnumerateSecurityPackages(securityPackages[i].Name);
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

            if (GlobalLog.IsEnabled)
            {
                GlobalLog.Leave("EnumerateSecurityPackages");
            }
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

            if (SecurityEventSource.Log.IsEnabled())
            {
                SecurityEventSource.Log.SspiPackageNotFound(packageName);
            }

            if (throwIfMissing)
            {
                throw new NotSupportedException(SR.net_securitypackagesupport);
            }

            return null;
        }

        public static SafeFreeCredentials AcquireDefaultCredential(SSPIInterface secModule, string package, Interop.SspiCli.CredentialUse intent)
        {
            if (GlobalLog.IsEnabled)
            {
                GlobalLog.Print("SSPIWrapper::AcquireDefaultCredential(): using " + package);
            }

            if (SecurityEventSource.Log.IsEnabled())
            {
                SecurityEventSource.Log.AcquireDefaultCredential(package, intent);
            }

            SafeFreeCredentials outCredential = null;
            int errorCode = secModule.AcquireDefaultCredential(package, intent, out outCredential);

            if (errorCode != 0)
            {
#if TRACE_VERBOSE
                if (GlobalLog.IsEnabled)
                {
                    GlobalLog.Print("SSPIWrapper::AcquireDefaultCredential(): error " + Interop.MapSecurityStatus((uint)errorCode));
                }
#endif

                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.PrintError(NetEventSource.ComponentType.Security, SR.Format(SR.net_log_operation_failed_with_error, "AcquireDefaultCredential()", String.Format(CultureInfo.CurrentCulture, "0X{0:X}", errorCode)));
                }

                throw new Win32Exception(errorCode);
            }
            return outCredential;
        }

        public static SafeFreeCredentials AcquireCredentialsHandle(SSPIInterface secModule, string package, Interop.SspiCli.CredentialUse intent, ref Interop.SspiCli.AuthIdentity authdata)
        {
            if (GlobalLog.IsEnabled)
            {
                GlobalLog.Print("SSPIWrapper::AcquireCredentialsHandle#2(): using " + package);
            }

            if (SecurityEventSource.Log.IsEnabled())
            {
                SecurityEventSource.AcquireCredentialsHandle(package, intent, authdata);
            }

            SafeFreeCredentials credentialsHandle = null;
            int errorCode = secModule.AcquireCredentialsHandle(package,
                                                               intent,
                                                               ref authdata,
                                                               out credentialsHandle);

            if (errorCode != 0)
            {
#if TRACE_VERBOSE
                if (GlobalLog.IsEnabled)
                {
                    GlobalLog.Print("SSPIWrapper::AcquireCredentialsHandle#2(): error " + Interop.MapSecurityStatus((uint)errorCode));
                }
#endif
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.PrintError(NetEventSource.ComponentType.Security, SR.Format(SR.net_log_operation_failed_with_error, "AcquireCredentialsHandle()", String.Format(CultureInfo.CurrentCulture, "0X{0:X}", errorCode)));
                }

                throw new Win32Exception(errorCode);
            }
            return credentialsHandle;
        }

        public static SafeFreeCredentials AcquireCredentialsHandle(SSPIInterface secModule, string package, Interop.SspiCli.CredentialUse intent, ref SafeSspiAuthDataHandle authdata)
        {
            if (SecurityEventSource.Log.IsEnabled())
            {
                SecurityEventSource.AcquireCredentialsHandle(package, intent, authdata);
            }

            SafeFreeCredentials credentialsHandle = null;
            int errorCode = secModule.AcquireCredentialsHandle(package, intent, ref authdata, out credentialsHandle);

            if (errorCode != 0)
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.PrintError(NetEventSource.ComponentType.Security, SR.Format(SR.net_log_operation_failed_with_error, "AcquireCredentialsHandle()", String.Format(CultureInfo.CurrentCulture, "0X{0:X}", errorCode)));
                }

                throw new Win32Exception(errorCode);
            }

            return credentialsHandle;
        }

        public static SafeFreeCredentials AcquireCredentialsHandle(SSPIInterface secModule, string package, Interop.SspiCli.CredentialUse intent, Interop.SspiCli.SecureCredential scc)
        {
            if (GlobalLog.IsEnabled)
            {
                GlobalLog.Print("SSPIWrapper::AcquireCredentialsHandle#3(): using " + package);
            }

            if (SecurityEventSource.Log.IsEnabled())
            {
                SecurityEventSource.AcquireCredentialsHandle(package, intent, scc);
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
                if (GlobalLog.IsEnabled)
                {
                    GlobalLog.Print("SSPIWrapper::AcquireCredentialsHandle#3(): error " + Interop.MapSecurityStatus((uint)errorCode));
                }
#endif

                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.PrintError(NetEventSource.ComponentType.Security, SR.Format(SR.net_log_operation_failed_with_error, "AcquireCredentialsHandle()", String.Format(CultureInfo.CurrentCulture, "0X{0:X}", errorCode)));
                }

                throw new Win32Exception(errorCode);
            }

#if TRACE_VERBOSE
            if (GlobalLog.IsEnabled)
            {
                GlobalLog.Print("SSPIWrapper::AcquireCredentialsHandle#3(): cred handle = " + outCredential.ToString());
            }
#endif
            return outCredential;
        }

        internal static int InitializeSecurityContext(SSPIInterface secModule, ref SafeFreeCredentials credential, ref SafeDeleteContext context, string targetName, Interop.SspiCli.ContextFlags inFlags, Interop.SspiCli.Endianness datarep, SecurityBuffer inputBuffer, SecurityBuffer outputBuffer, ref Interop.SspiCli.ContextFlags outFlags)
        {
            if (SecurityEventSource.Log.IsEnabled())
            {
                SecurityEventSource.Log.InitializeSecurityContext(credential.ToString(),
                    LoggingHash.ObjectToString(context),
                    targetName,
                    inFlags);
            }

            int errorCode = secModule.InitializeSecurityContext(ref credential, ref context, targetName, inFlags, datarep, inputBuffer, outputBuffer, ref outFlags);

            if (SecurityEventSource.Log.IsEnabled())
            {
                SecurityEventSource.Log.SecurityContextInputBuffer("InitializeSecurityContext", (inputBuffer == null ? 0 : inputBuffer.size), outputBuffer.size, (Interop.SecurityStatus)errorCode);
            }

            return errorCode;
        }

        internal static int InitializeSecurityContext(SSPIInterface secModule, SafeFreeCredentials credential, ref SafeDeleteContext context, string targetName, Interop.SspiCli.ContextFlags inFlags, Interop.SspiCli.Endianness datarep, SecurityBuffer[] inputBuffers, SecurityBuffer outputBuffer, ref Interop.SspiCli.ContextFlags outFlags)
        {
            if (SecurityEventSource.Log.IsEnabled())
            {
                SecurityEventSource.Log.InitializeSecurityContext(credential.ToString(),
                    LoggingHash.ObjectToString(context),
                    targetName,
                    inFlags);
            }

            int errorCode = secModule.InitializeSecurityContext(credential, ref context, targetName, inFlags, datarep, inputBuffers, outputBuffer, ref outFlags);

            if (SecurityEventSource.Log.IsEnabled())
            {
                SecurityEventSource.Log.SecurityContextInputBuffers("InitializeSecurityContext", (inputBuffers == null ? 0 : inputBuffers.Length), outputBuffer.size, (Interop.SecurityStatus)errorCode);
            }

            return errorCode;
        }

        internal static int AcceptSecurityContext(SSPIInterface secModule, ref SafeFreeCredentials credential, ref SafeDeleteContext context, Interop.SspiCli.ContextFlags inFlags, Interop.SspiCli.Endianness datarep, SecurityBuffer inputBuffer, SecurityBuffer outputBuffer, ref Interop.SspiCli.ContextFlags outFlags)
        {
            if (SecurityEventSource.Log.IsEnabled())
            {
                SecurityEventSource.Log.AcceptSecurityContext(credential.ToString(), LoggingHash.ObjectToString(context), inFlags);
            }

            int errorCode = secModule.AcceptSecurityContext(ref credential, ref context, inputBuffer, inFlags, datarep, outputBuffer, ref outFlags);

            if (SecurityEventSource.Log.IsEnabled())
            {
                SecurityEventSource.Log.SecurityContextInputBuffer("AcceptSecurityContext", (inputBuffer == null ? 0 : inputBuffer.size), outputBuffer.size, (Interop.SecurityStatus)errorCode);
            }

            return errorCode;
        }

        internal static int AcceptSecurityContext(SSPIInterface secModule, SafeFreeCredentials credential, ref SafeDeleteContext context, Interop.SspiCli.ContextFlags inFlags, Interop.SspiCli.Endianness datarep, SecurityBuffer[] inputBuffers, SecurityBuffer outputBuffer, ref Interop.SspiCli.ContextFlags outFlags)
        {
            if (SecurityEventSource.Log.IsEnabled())
            {
                SecurityEventSource.Log.AcceptSecurityContext(credential.ToString(), LoggingHash.ObjectToString(context), inFlags);
            }

            int errorCode = secModule.AcceptSecurityContext(credential, ref context, inputBuffers, inFlags, datarep, outputBuffer, ref outFlags);

            if (SecurityEventSource.Log.IsEnabled())
            {
                SecurityEventSource.Log.SecurityContextInputBuffers("AcceptSecurityContext", (inputBuffers == null ? 0 : inputBuffers.Length), outputBuffer.size, (Interop.SecurityStatus)errorCode);
            }

            return errorCode;
        }

        internal static int CompleteAuthToken(SSPIInterface secModule, ref SafeDeleteContext context, SecurityBuffer[] inputBuffers)
        {
            int errorCode = secModule.CompleteAuthToken(ref context, inputBuffers);

            if (SecurityEventSource.Log.IsEnabled())
            {
                SecurityEventSource.Log.OperationReturnedSomething("CompleteAuthToken()", (Interop.SecurityStatus)errorCode);
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
            Interop.SspiCli.SecurityBufferDescriptor sdcInOut = new Interop.SspiCli.SecurityBufferDescriptor(input.Length);
            var unmanagedBuffer = new Interop.SspiCli.SecurityBufferStruct[input.Length];

            fixed (Interop.SspiCli.SecurityBufferStruct* unmanagedBufferPtr = unmanagedBuffer)
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
                            if (GlobalLog.IsEnabled)
                            {
                                GlobalLog.Assert("SSPIWrapper::EncryptDecryptHelper", "Unknown OP: " + op);
                            }

                            Debug.Fail("SSPIWrapper::EncryptDecryptHelper", "Unknown OP: " + op);
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
                                    if (GlobalLog.IsEnabled)
                                    {
                                        GlobalLog.Assert("SSPIWrapper::EncryptDecryptHelper", "Output buffer out of range.");
                                    }

                                    Debug.Fail("SSPIWrapper::EncryptDecryptHelper", "Output buffer out of range.");
                                    iBuffer.size = 0;
                                    iBuffer.offset = 0;
                                    iBuffer.token = null;
                                }
                            }
                        }

                        // Backup validate the new sizes.
                        if (iBuffer.offset < 0 || iBuffer.offset > (iBuffer.token == null ? 0 : iBuffer.token.Length))
                        {
                            if (GlobalLog.IsEnabled)
                            {
                                GlobalLog.AssertFormat("SSPIWrapper::EncryptDecryptHelper|'offset' out of range.  [{0}]", iBuffer.offset);
                            }

                            Debug.Fail("SSPIWrapper::EncryptDecryptHelper|'offset' out of range.  [" + iBuffer.offset + "]");
                        }

                        if (iBuffer.size < 0 || iBuffer.size > (iBuffer.token == null ? 0 : iBuffer.token.Length - iBuffer.offset))
                        {
                            if (GlobalLog.IsEnabled)
                            {
                                GlobalLog.AssertFormat("SSPIWrapper::EncryptDecryptHelper|'size' out of range.  [{0}]", iBuffer.size);
                            }

                            Debug.Fail("SSPIWrapper::EncryptDecryptHelper|'size' out of range.  [" + iBuffer.size + "]");
                        }
                    }

                    if (errorCode != 0 && NetEventSource.Log.IsEnabled())
                    {                         
                        if (errorCode == Interop.SspiCli.SEC_I_RENEGOTIATE)
                        {
                            NetEventSource.PrintError(NetEventSource.ComponentType.Security, SR.Format(SR.event_OperationReturnedSomething, op, "SEC_I_RENEGOTIATE"));
                        }
                        else
                        {
                            NetEventSource.PrintError(NetEventSource.ComponentType.Security, SR.Format(SR.net_log_operation_failed_with_error, op, String.Format(CultureInfo.CurrentCulture, "0X{0:X}", errorCode)));
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

        public static SafeFreeContextBufferChannelBinding QueryContextChannelBinding(SSPIInterface secModule, SafeDeleteContext securityContext, Interop.SspiCli.ContextAttribute contextAttribute)
        {
            if (GlobalLog.IsEnabled)
            {
                GlobalLog.Enter("QueryContextChannelBinding", contextAttribute.ToString());
            }

            SafeFreeContextBufferChannelBinding result;
            int errorCode = secModule.QueryContextChannelBinding(securityContext, contextAttribute, out result);
            if (errorCode != 0)
            {
                if (GlobalLog.IsEnabled)
                {
                    GlobalLog.Leave("QueryContextChannelBinding", "ERROR = " + ErrorDescription(errorCode));
                }
                return null;
            }

            if (GlobalLog.IsEnabled)
            {
                GlobalLog.Leave("QueryContextChannelBinding", LoggingHash.HashString(result));
            }
            return result;
        }

        public static object QueryContextAttributes(SSPIInterface secModule, SafeDeleteContext securityContext, Interop.SspiCli.ContextAttribute contextAttribute)
        {
            int errorCode;
            return QueryContextAttributes(secModule, securityContext, contextAttribute, out errorCode);
        }

        public static object QueryContextAttributes(SSPIInterface secModule, SafeDeleteContext securityContext, Interop.SspiCli.ContextAttribute contextAttribute, out int errorCode)
        {
            if (GlobalLog.IsEnabled)
            {
                GlobalLog.Enter("QueryContextAttributes", contextAttribute.ToString());
            }

            int nativeBlockSize = IntPtr.Size;
            Type handleType = null;

            switch (contextAttribute)
            {
                case Interop.SspiCli.ContextAttribute.Sizes:
                    nativeBlockSize = SecSizes.SizeOf;
                    break;
                case Interop.SspiCli.ContextAttribute.StreamSizes:
                    nativeBlockSize = StreamSizes.SizeOf;
                    break;

                case Interop.SspiCli.ContextAttribute.Names:
                    handleType = typeof(SafeFreeContextBuffer);
                    break;

                case Interop.SspiCli.ContextAttribute.PackageInfo:
                    handleType = typeof(SafeFreeContextBuffer);
                    break;

                case Interop.SspiCli.ContextAttribute.NegotiationInfo:
                    handleType = typeof(SafeFreeContextBuffer);
                    nativeBlockSize = Marshal.SizeOf<NegotiationInfo>();
                    break;

                case Interop.SspiCli.ContextAttribute.ClientSpecifiedSpn:
                    handleType = typeof(SafeFreeContextBuffer);
                    break;

                case Interop.SspiCli.ContextAttribute.RemoteCertificate:
                    handleType = typeof(SafeFreeCertContext);
                    break;

                case Interop.SspiCli.ContextAttribute.LocalCertificate:
                    handleType = typeof(SafeFreeCertContext);
                    break;

                case Interop.SspiCli.ContextAttribute.IssuerListInfoEx:
                    nativeBlockSize = Marshal.SizeOf<Interop.SspiCli.IssuerListInfoEx>();
                    handleType = typeof(SafeFreeContextBuffer);
                    break;

                case Interop.SspiCli.ContextAttribute.ConnectionInfo:
                    nativeBlockSize = Marshal.SizeOf<SslConnectionInfo>();
                    break;

                default:
                    throw new ArgumentException(SR.Format(SR.net_invalid_enum, "ContextAttribute"), nameof(contextAttribute));
            }

            SafeHandle sspiHandle = null;
            object attribute = null;

            try
            {
                var nativeBuffer = new byte[nativeBlockSize];
                errorCode = secModule.QueryContextAttributes(securityContext, contextAttribute, nativeBuffer, handleType, out sspiHandle);
                if (errorCode != 0)
                {
                    if (GlobalLog.IsEnabled)
                    {
                        GlobalLog.Leave("Win32:QueryContextAttributes", "ERROR = " + ErrorDescription(errorCode));
                    }
                    return null;
                }

                switch (contextAttribute)
                {
                    case Interop.SspiCli.ContextAttribute.Sizes:
                        attribute = new SecSizes(nativeBuffer);
                        break;

                    case Interop.SspiCli.ContextAttribute.StreamSizes:
                        attribute = new StreamSizes(nativeBuffer);
                        break;

                    case Interop.SspiCli.ContextAttribute.Names:
                        attribute = Marshal.PtrToStringUni(sspiHandle.DangerousGetHandle());
                        break;

                    case Interop.SspiCli.ContextAttribute.PackageInfo:
                        attribute = new SecurityPackageInfoClass(sspiHandle, 0);
                        break;

                    case Interop.SspiCli.ContextAttribute.NegotiationInfo:
                        unsafe
                        {
                            fixed (void* ptr = nativeBuffer)
                            {
                                attribute = new NegotiationInfoClass(sspiHandle, Marshal.ReadInt32(new IntPtr(ptr), NegotiationInfo.NegotiationStateOffest));
                            }
                        }
                        break;

                    case Interop.SspiCli.ContextAttribute.ClientSpecifiedSpn:
                        attribute = Marshal.PtrToStringUni(sspiHandle.DangerousGetHandle());
                        break;

                    case Interop.SspiCli.ContextAttribute.LocalCertificate:
                    // Fall-through to RemoteCertificate is intentional.
                    case Interop.SspiCli.ContextAttribute.RemoteCertificate:
                        attribute = sspiHandle;
                        sspiHandle = null;
                        break;

                    case Interop.SspiCli.ContextAttribute.IssuerListInfoEx:
                        attribute = new Interop.SspiCli.IssuerListInfoEx(sspiHandle, nativeBuffer);
                        sspiHandle = null;
                        break;

                    case Interop.SspiCli.ContextAttribute.ConnectionInfo:
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

            if (GlobalLog.IsEnabled)
            {
                GlobalLog.Leave("QueryContextAttributes", LoggingHash.ObjectToString(attribute));
            }

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
