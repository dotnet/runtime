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
            if (NetEventSource.IsEnabled) NetEventSource.Enter(null);

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
                            if (NetEventSource.IsEnabled) NetEventSource.Info(null, $"arrayBase: {arrayBaseHandle}");
                            if (errorCode != 0)
                            {
                                throw new Win32Exception(errorCode);
                            }

                            var securityPackages = new SecurityPackageInfoClass[moduleCount];

                            int i;
                            for (i = 0; i < moduleCount; i++)
                            {
                                securityPackages[i] = new SecurityPackageInfoClass(arrayBaseHandle, i);
                                if (NetEventSource.IsEnabled) NetEventSource.Log.EnumerateSecurityPackages(securityPackages[i].Name);
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

            if (NetEventSource.IsEnabled) NetEventSource.Exit(null);
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

            if (NetEventSource.IsEnabled) NetEventSource.Log.SspiPackageNotFound(packageName);

            if (throwIfMissing)
            {
                throw new NotSupportedException(SR.net_securitypackagesupport);
            }

            return null;
        }

        public static SafeFreeCredentials AcquireDefaultCredential(SSPIInterface secModule, string package, Interop.SspiCli.CredentialUse intent)
        {
            if (NetEventSource.IsEnabled)
            {
                NetEventSource.Enter(null, package);
                NetEventSource.Log.AcquireDefaultCredential(package, intent);
            }

            SafeFreeCredentials outCredential = null;
            int errorCode = secModule.AcquireDefaultCredential(package, intent, out outCredential);

            if (errorCode != 0)
            {
                if (NetEventSource.IsEnabled) NetEventSource.Error(null, SR.Format(SR.net_log_operation_failed_with_error, nameof(AcquireDefaultCredential), $"0x{errorCode:X}"));
                throw new Win32Exception(errorCode);
            }
            return outCredential;
        }

        public static SafeFreeCredentials AcquireCredentialsHandle(SSPIInterface secModule, string package, Interop.SspiCli.CredentialUse intent, ref Interop.SspiCli.SEC_WINNT_AUTH_IDENTITY_W authdata)
        {
            if (NetEventSource.IsEnabled)
            {
                NetEventSource.Enter(null, package);
                NetEventSource.Log.AcquireCredentialsHandle(package, intent, authdata);
            }

            SafeFreeCredentials credentialsHandle = null;
            int errorCode = secModule.AcquireCredentialsHandle(package,
                                                               intent,
                                                               ref authdata,
                                                               out credentialsHandle);

            if (errorCode != 0)
            {
                if (NetEventSource.IsEnabled) NetEventSource.Error(null, SR.Format(SR.net_log_operation_failed_with_error, nameof(AcquireCredentialsHandle), $"0x{errorCode:X}"));

                throw new Win32Exception(errorCode);
            }
            return credentialsHandle;
        }

        public static SafeFreeCredentials AcquireCredentialsHandle(SSPIInterface secModule, string package, Interop.SspiCli.CredentialUse intent, ref SafeSspiAuthDataHandle authdata)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Log.AcquireCredentialsHandle(package, intent, authdata);

            SafeFreeCredentials credentialsHandle = null;
            int errorCode = secModule.AcquireCredentialsHandle(package, intent, ref authdata, out credentialsHandle);

            if (errorCode != 0)
            {
                if (NetEventSource.IsEnabled) NetEventSource.Error(null, SR.Format(SR.net_log_operation_failed_with_error, nameof(AcquireCredentialsHandle), $"0x{errorCode:X}"));
                throw new Win32Exception(errorCode);
            }

            return credentialsHandle;
        }

        public static SafeFreeCredentials AcquireCredentialsHandle(SSPIInterface secModule, string package, Interop.SspiCli.CredentialUse intent, Interop.SspiCli.SCHANNEL_CRED scc)
        {
            if (NetEventSource.IsEnabled)
            {
                NetEventSource.Enter(null, package);
                NetEventSource.Log.AcquireCredentialsHandle(package, intent, scc);
            }

            SafeFreeCredentials outCredential = null;
            int errorCode = secModule.AcquireCredentialsHandle(
                                            package,
                                            intent,
                                            ref scc,
                                            out outCredential);

            if (errorCode != 0)
            {
                if (NetEventSource.IsEnabled) NetEventSource.Error(null, SR.Format(SR.net_log_operation_failed_with_error, nameof(AcquireCredentialsHandle), $"0x{errorCode:X}"));
                throw new Win32Exception(errorCode);
            }

            if (NetEventSource.IsEnabled) NetEventSource.Exit(null, outCredential);
            return outCredential;
        }

        internal static int InitializeSecurityContext(SSPIInterface secModule, ref SafeFreeCredentials credential, ref SafeDeleteContext context, string targetName, Interop.SspiCli.ContextFlags inFlags, Interop.SspiCli.Endianness datarep, SecurityBuffer inputBuffer, SecurityBuffer outputBuffer, ref Interop.SspiCli.ContextFlags outFlags)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Log.InitializeSecurityContext(credential, context, targetName, inFlags);

            int errorCode = secModule.InitializeSecurityContext(ref credential, ref context, targetName, inFlags, datarep, inputBuffer, outputBuffer, ref outFlags);

            if (NetEventSource.IsEnabled) NetEventSource.Log.SecurityContextInputBuffer(nameof(InitializeSecurityContext), inputBuffer?.size ?? 0, outputBuffer.size, (Interop.SECURITY_STATUS)errorCode);
            
            return errorCode;
        }

        internal static int InitializeSecurityContext(SSPIInterface secModule, SafeFreeCredentials credential, ref SafeDeleteContext context, string targetName, Interop.SspiCli.ContextFlags inFlags, Interop.SspiCli.Endianness datarep, SecurityBuffer[] inputBuffers, SecurityBuffer outputBuffer, ref Interop.SspiCli.ContextFlags outFlags)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Log.InitializeSecurityContext(credential, context, targetName, inFlags);

            int errorCode = secModule.InitializeSecurityContext(credential, ref context, targetName, inFlags, datarep, inputBuffers, outputBuffer, ref outFlags);

            if (NetEventSource.IsEnabled) NetEventSource.Log.SecurityContextInputBuffers(nameof(InitializeSecurityContext), inputBuffers?.Length ?? 0, outputBuffer.size, (Interop.SECURITY_STATUS)errorCode);

            return errorCode;
        }

        internal static int AcceptSecurityContext(SSPIInterface secModule, ref SafeFreeCredentials credential, ref SafeDeleteContext context, Interop.SspiCli.ContextFlags inFlags, Interop.SspiCli.Endianness datarep, SecurityBuffer inputBuffer, SecurityBuffer outputBuffer, ref Interop.SspiCli.ContextFlags outFlags)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Log.AcceptSecurityContext(credential, context, inFlags);

            int errorCode = secModule.AcceptSecurityContext(ref credential, ref context, inputBuffer, inFlags, datarep, outputBuffer, ref outFlags);

            if (NetEventSource.IsEnabled) NetEventSource.Log.SecurityContextInputBuffer(nameof(AcceptSecurityContext), inputBuffer?.size ?? 0, outputBuffer.size, (Interop.SECURITY_STATUS)errorCode);

            return errorCode;
        }

        internal static int AcceptSecurityContext(SSPIInterface secModule, SafeFreeCredentials credential, ref SafeDeleteContext context, Interop.SspiCli.ContextFlags inFlags, Interop.SspiCli.Endianness datarep, SecurityBuffer[] inputBuffers, SecurityBuffer outputBuffer, ref Interop.SspiCli.ContextFlags outFlags)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Log.AcceptSecurityContext(credential, context, inFlags);

            int errorCode = secModule.AcceptSecurityContext(credential, ref context, inputBuffers, inFlags, datarep, outputBuffer, ref outFlags);

            if (NetEventSource.IsEnabled) NetEventSource.Log.SecurityContextInputBuffers(nameof(AcceptSecurityContext), inputBuffers?.Length ?? 0, outputBuffer.size, (Interop.SECURITY_STATUS)errorCode);

            return errorCode;
        }

        internal static int CompleteAuthToken(SSPIInterface secModule, ref SafeDeleteContext context, SecurityBuffer[] inputBuffers)
        {
            int errorCode = secModule.CompleteAuthToken(ref context, inputBuffers);

            if (NetEventSource.IsEnabled) NetEventSource.Log.OperationReturnedSomething(nameof(CompleteAuthToken), (Interop.SECURITY_STATUS)errorCode);

            return errorCode;
        }

        internal static int ApplyControlToken(SSPIInterface secModule, ref SafeDeleteContext context, SecurityBuffer[] inputBuffers)
        {
            int errorCode = secModule.ApplyControlToken(ref context, inputBuffers);

            if (NetEventSource.IsEnabled) NetEventSource.Log.OperationReturnedSomething(nameof(ApplyControlToken), (Interop.SECURITY_STATUS)errorCode);

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
            Interop.SspiCli.SecBufferDesc sdcInOut = new Interop.SspiCli.SecBufferDesc(input.Length);
            var unmanagedBuffer = new Interop.SspiCli.SecBuffer[input.Length];

            fixed (Interop.SspiCli.SecBuffer* unmanagedBufferPtr = unmanagedBuffer)
            {
                sdcInOut.pBuffers = unmanagedBufferPtr;
                GCHandle[] pinnedBuffers = new GCHandle[input.Length];
                byte[][] buffers = new byte[input.Length][];
                try
                {
                    for (int i = 0; i < input.Length; i++)
                    {
                        SecurityBuffer iBuffer = input[i];
                        unmanagedBuffer[i].cbBuffer = iBuffer.size;
                        unmanagedBuffer[i].BufferType = iBuffer.type;
                        if (iBuffer.token == null || iBuffer.token.Length == 0)
                        {
                            unmanagedBuffer[i].pvBuffer = IntPtr.Zero;
                        }
                        else
                        {
                            pinnedBuffers[i] = GCHandle.Alloc(iBuffer.token, GCHandleType.Pinned);
                            unmanagedBuffer[i].pvBuffer = Marshal.UnsafeAddrOfPinnedArrayElement(iBuffer.token, iBuffer.offset);
                            buffers[i] = iBuffer.token;
                        }
                    }

                    // The result is written in the input Buffer passed as type=BufferType.Data.
                    int errorCode;
                    switch (op)
                    {
                        case OP.Encrypt:
                            errorCode = secModule.EncryptMessage(context, ref sdcInOut, sequenceNumber);
                            break;

                        case OP.Decrypt:
                            errorCode = secModule.DecryptMessage(context, ref sdcInOut, sequenceNumber);
                            break;

                        case OP.MakeSignature:
                            errorCode = secModule.MakeSignature(context, ref sdcInOut, sequenceNumber);
                            break;

                        case OP.VerifySignature:
                            errorCode = secModule.VerifySignature(context, ref sdcInOut, sequenceNumber);
                            break;

                        default:
                            NetEventSource.Fail(null, $"Unknown OP: {op}");
                            throw NotImplemented.ByDesignWithMessage(SR.net_MethodNotImplementedException);
                    }

                    // Marshalling back returned sizes / data.
                    for (int i = 0; i < input.Length; i++)
                    {
                        SecurityBuffer iBuffer = input[i];
                        iBuffer.size = unmanagedBuffer[i].cbBuffer;
                        iBuffer.type = unmanagedBuffer[i].BufferType;

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
                                    if ((byte*)unmanagedBuffer[i].pvBuffer >= bufferAddress &&
                                        (byte*)unmanagedBuffer[i].pvBuffer + iBuffer.size <= bufferAddress + buffers[j].Length)
                                    {
                                        iBuffer.offset = (int)((byte*)unmanagedBuffer[i].pvBuffer - bufferAddress);
                                        iBuffer.token = buffers[j];
                                        break;
                                    }
                                }

                                if (j >= input.Length)
                                {
                                    NetEventSource.Fail(null, "Output buffer out of range.");
                                    iBuffer.size = 0;
                                    iBuffer.offset = 0;
                                    iBuffer.token = null;
                                }
                            }
                        }

                        // Backup validate the new sizes.
                        if (iBuffer.offset < 0 || iBuffer.offset > (iBuffer.token == null ? 0 : iBuffer.token.Length))
                        {
                            NetEventSource.Fail(null, $"'offset' out of range.  [{iBuffer.offset}]");
                        }

                        if (iBuffer.size < 0 || iBuffer.size > (iBuffer.token == null ? 0 : iBuffer.token.Length - iBuffer.offset))
                        {
                            NetEventSource.Fail(null, $"'size' out of range.  [{iBuffer.size}]");
                        }
                    }

                    if (NetEventSource.IsEnabled && errorCode != 0)
                    {                         
                        if (errorCode == Interop.SspiCli.SEC_I_RENEGOTIATE)
                        {
                            NetEventSource.Error(null, SR.Format(SR.event_OperationReturnedSomething, op, "SEC_I_RENEGOTIATE"));
                        }
                        else
                        {
                            NetEventSource.Error(null, SR.Format(SR.net_log_operation_failed_with_error, op, $"0x{0:X}"));
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
            if (NetEventSource.IsEnabled) NetEventSource.Enter(null, contextAttribute);

            SafeFreeContextBufferChannelBinding result;
            int errorCode = secModule.QueryContextChannelBinding(securityContext, contextAttribute, out result);
            if (errorCode != 0)
            {
                if (NetEventSource.IsEnabled) NetEventSource.Exit(null, $"ERROR = {ErrorDescription(errorCode)}");
                return null;
            }

            if (NetEventSource.IsEnabled) NetEventSource.Exit(null, result);
            return result;
        }

        public static object QueryContextAttributes(SSPIInterface secModule, SafeDeleteContext securityContext, Interop.SspiCli.ContextAttribute contextAttribute)
        {
            int errorCode;
            return QueryContextAttributes(secModule, securityContext, contextAttribute, out errorCode);
        }

        public static object QueryContextAttributes(SSPIInterface secModule, SafeDeleteContext securityContext, Interop.SspiCli.ContextAttribute contextAttribute, out int errorCode)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(null, contextAttribute);

            int nativeBlockSize = IntPtr.Size;
            Type handleType = null;

            switch (contextAttribute)
            {
                case Interop.SspiCli.ContextAttribute.SECPKG_ATTR_SIZES:
                    nativeBlockSize = SecPkgContext_Sizes.SizeOf;
                    break;
                case Interop.SspiCli.ContextAttribute.SECPKG_ATTR_STREAM_SIZES:
                    nativeBlockSize = SecPkgContext_StreamSizes.SizeOf;
                    break;

                case Interop.SspiCli.ContextAttribute.SECPKG_ATTR_NAMES:
                    handleType = typeof(SafeFreeContextBuffer);
                    break;

                case Interop.SspiCli.ContextAttribute.SECPKG_ATTR_PACKAGE_INFO:
                    handleType = typeof(SafeFreeContextBuffer);
                    break;

                case Interop.SspiCli.ContextAttribute.SECPKG_ATTR_NEGOTIATION_INFO:
                    handleType = typeof(SafeFreeContextBuffer);
                    nativeBlockSize = Marshal.SizeOf<SecPkgContext_NegotiationInfoW>();
                    break;

                case Interop.SspiCli.ContextAttribute.SECPKG_ATTR_CLIENT_SPECIFIED_TARGET:
                    handleType = typeof(SafeFreeContextBuffer);
                    break;

                case Interop.SspiCli.ContextAttribute.SECPKG_ATTR_REMOTE_CERT_CONTEXT:
                    handleType = typeof(SafeFreeCertContext);
                    break;

                case Interop.SspiCli.ContextAttribute.SECPKG_ATTR_LOCAL_CERT_CONTEXT:
                    handleType = typeof(SafeFreeCertContext);
                    break;

                case Interop.SspiCli.ContextAttribute.SECPKG_ATTR_ISSUER_LIST_EX:
                    nativeBlockSize = Marshal.SizeOf<Interop.SspiCli.SecPkgContext_IssuerListInfoEx>();
                    handleType = typeof(SafeFreeContextBuffer);
                    break;

                case Interop.SspiCli.ContextAttribute.SECPKG_ATTR_CONNECTION_INFO:
                    nativeBlockSize = Marshal.SizeOf<SecPkgContext_ConnectionInfo>();
                    break;

                default:
                    throw new ArgumentException(SR.Format(SR.net_invalid_enum, nameof(contextAttribute)), nameof(contextAttribute));
            }

            SafeHandle sspiHandle = null;
            object attribute = null;

            try
            {
                var nativeBuffer = new byte[nativeBlockSize];
                errorCode = secModule.QueryContextAttributes(securityContext, contextAttribute, nativeBuffer, handleType, out sspiHandle);
                if (errorCode != 0)
                {
                    if (NetEventSource.IsEnabled) NetEventSource.Exit(null, $"ERROR = {ErrorDescription(errorCode)}");
                    return null;
                }

                switch (contextAttribute)
                {
                    case Interop.SspiCli.ContextAttribute.SECPKG_ATTR_SIZES:
                        attribute = new SecPkgContext_Sizes(nativeBuffer);
                        break;

                    case Interop.SspiCli.ContextAttribute.SECPKG_ATTR_STREAM_SIZES:
                        attribute = new SecPkgContext_StreamSizes(nativeBuffer);
                        break;

                    case Interop.SspiCli.ContextAttribute.SECPKG_ATTR_NAMES:
                        attribute = Marshal.PtrToStringUni(sspiHandle.DangerousGetHandle());
                        break;

                    case Interop.SspiCli.ContextAttribute.SECPKG_ATTR_PACKAGE_INFO:
                        attribute = new SecurityPackageInfoClass(sspiHandle, 0);
                        break;

                    case Interop.SspiCli.ContextAttribute.SECPKG_ATTR_NEGOTIATION_INFO:
                        unsafe
                        {
                            fixed (void* ptr = nativeBuffer)
                            {
                                attribute = new NegotiationInfoClass(sspiHandle, Marshal.ReadInt32(new IntPtr(ptr), SecPkgContext_NegotiationInfoW.NegotiationStateOffest));
                            }
                        }
                        break;

                    case Interop.SspiCli.ContextAttribute.SECPKG_ATTR_CLIENT_SPECIFIED_TARGET:
                        attribute = Marshal.PtrToStringUni(sspiHandle.DangerousGetHandle());
                        break;

                    case Interop.SspiCli.ContextAttribute.SECPKG_ATTR_LOCAL_CERT_CONTEXT:
                    // Fall-through to RemoteCertificate is intentional.
                    case Interop.SspiCli.ContextAttribute.SECPKG_ATTR_REMOTE_CERT_CONTEXT:
                        attribute = sspiHandle;
                        sspiHandle = null;
                        break;

                    case Interop.SspiCli.ContextAttribute.SECPKG_ATTR_ISSUER_LIST_EX:
                        attribute = new Interop.SspiCli.SecPkgContext_IssuerListInfoEx(sspiHandle, nativeBuffer);
                        sspiHandle = null;
                        break;

                    case Interop.SspiCli.ContextAttribute.SECPKG_ATTR_CONNECTION_INFO:
                        attribute = new SecPkgContext_ConnectionInfo(nativeBuffer);
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

            if (NetEventSource.IsEnabled) NetEventSource.Exit(null, attribute);
            return attribute;
        }

        public static string ErrorDescription(int errorCode)
        {
            if (errorCode == -1)
            {
                return "An exception when invoking Win32 API";
            }

            switch ((Interop.SECURITY_STATUS)errorCode)
            {
                case Interop.SECURITY_STATUS.InvalidHandle:
                    return "Invalid handle";
                case Interop.SECURITY_STATUS.InvalidToken:
                    return "Invalid token";
                case Interop.SECURITY_STATUS.ContinueNeeded:
                    return "Continue needed";
                case Interop.SECURITY_STATUS.IncompleteMessage:
                    return "Message incomplete";
                case Interop.SECURITY_STATUS.WrongPrincipal:
                    return "Wrong principal";
                case Interop.SECURITY_STATUS.TargetUnknown:
                    return "Target unknown";
                case Interop.SECURITY_STATUS.PackageNotFound:
                    return "Package not found";
                case Interop.SECURITY_STATUS.BufferNotEnough:
                    return "Buffer not enough";
                case Interop.SECURITY_STATUS.MessageAltered:
                    return "Message altered";
                case Interop.SECURITY_STATUS.UntrustedRoot:
                    return "Untrusted root";
                default:
                    return "0x" + errorCode.ToString("x", NumberFormatInfo.InvariantInfo);
            }
        }
    } // class SSPIWrapper
}
