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
                            arrayBaseHandle?.Dispose();
                        }
                    }
                }
            }

            if (NetEventSource.IsEnabled) NetEventSource.Exit(null);
            return secModule.SecurityPackages;
        }

        internal static SecurityPackageInfoClass GetVerifyPackageInfo(SSPIInterface secModule, string packageName, bool throwIfMissing)
        {
            SecurityPackageInfoClass[] supportedSecurityPackages = EnumerateSecurityPackages(secModule);
            if (supportedSecurityPackages != null)
            {
                for (int i = 0; i < supportedSecurityPackages.Length; i++)
                {
                    if (string.Equals(supportedSecurityPackages[i].Name, packageName, StringComparison.OrdinalIgnoreCase))
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

        internal static int InitializeSecurityContext(SSPIInterface secModule, ref SafeFreeCredentials credential, ref SafeDeleteContext context, string targetName, Interop.SspiCli.ContextFlags inFlags, Interop.SspiCli.Endianness datarep, ReadOnlySpan<SecurityBuffer> inputBuffers, ref SecurityBuffer outputBuffer, ref Interop.SspiCli.ContextFlags outFlags)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Log.InitializeSecurityContext(credential, context, targetName, inFlags);

            int errorCode = secModule.InitializeSecurityContext(ref credential, ref context, targetName, inFlags, datarep, inputBuffers, ref outputBuffer, ref outFlags);

            if (NetEventSource.IsEnabled) NetEventSource.Log.SecurityContextInputBuffers(nameof(InitializeSecurityContext), inputBuffers.Length, outputBuffer.size, (Interop.SECURITY_STATUS)errorCode);

            return errorCode;
        }

        internal static int AcceptSecurityContext(SSPIInterface secModule, SafeFreeCredentials credential, ref SafeDeleteContext context, Interop.SspiCli.ContextFlags inFlags, Interop.SspiCli.Endianness datarep, ReadOnlySpan<SecurityBuffer> inputBuffers, ref SecurityBuffer outputBuffer, ref Interop.SspiCli.ContextFlags outFlags)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Log.AcceptSecurityContext(credential, context, inFlags);

            int errorCode = secModule.AcceptSecurityContext(credential, ref context, inputBuffers, inFlags, datarep, ref outputBuffer, ref outFlags);

            if (NetEventSource.IsEnabled) NetEventSource.Log.SecurityContextInputBuffers(nameof(AcceptSecurityContext), inputBuffers.Length, outputBuffer.size, (Interop.SECURITY_STATUS)errorCode);

            return errorCode;
        }

        internal static int CompleteAuthToken(SSPIInterface secModule, ref SafeDeleteContext context, in SecurityBuffer inputBuffer)
        {
            int errorCode = secModule.CompleteAuthToken(ref context, in inputBuffer);

            if (NetEventSource.IsEnabled) NetEventSource.Log.OperationReturnedSomething(nameof(CompleteAuthToken), (Interop.SECURITY_STATUS)errorCode);

            return errorCode;
        }

        internal static int ApplyControlToken(SSPIInterface secModule, ref SafeDeleteContext context, in SecurityBuffer inputBuffer)
        {
            int errorCode = secModule.ApplyControlToken(ref context, in inputBuffer);

            if (NetEventSource.IsEnabled) NetEventSource.Log.OperationReturnedSomething(nameof(ApplyControlToken), (Interop.SECURITY_STATUS)errorCode);

            return errorCode;
        }

        public static int QuerySecurityContextToken(SSPIInterface secModule, SafeDeleteContext context, out SecurityContextTokenHandle token)
        {
            return secModule.QuerySecurityContextToken(context, out token);
        }

        public static int EncryptMessage(SSPIInterface secModule, SafeDeleteContext context, Span<SecurityBuffer> input, uint sequenceNumber)
        {
            return EncryptDecryptHelper(OP.Encrypt, secModule, context, input, sequenceNumber);
        }

        public static int DecryptMessage(SSPIInterface secModule, SafeDeleteContext context, Span<SecurityBuffer> input, uint sequenceNumber)
        {
            return EncryptDecryptHelper(OP.Decrypt, secModule, context, input, sequenceNumber);
        }

        internal static int MakeSignature(SSPIInterface secModule, SafeDeleteContext context, Span<SecurityBuffer> input, uint sequenceNumber)
        {
            return EncryptDecryptHelper(OP.MakeSignature, secModule, context, input, sequenceNumber);
        }

        public static int VerifySignature(SSPIInterface secModule, SafeDeleteContext context, Span<SecurityBuffer> input, uint sequenceNumber)
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

        private static unsafe int EncryptDecryptHelper(OP op, SSPIInterface secModule, SafeDeleteContext context, Span<SecurityBuffer> input, uint sequenceNumber)
        {
            Interop.SspiCli.SecBufferDesc sdcInOut = new Interop.SspiCli.SecBufferDesc(input.Length);
            Span<Interop.SspiCli.SecBuffer> unmanagedBuffer = stackalloc Interop.SspiCli.SecBuffer[input.Length];
            unmanagedBuffer.Clear();

            fixed (Interop.SspiCli.SecBuffer* unmanagedBufferPtr = unmanagedBuffer)
            {
                sdcInOut.pBuffers = unmanagedBufferPtr;
                Span<GCHandle> pinnedBuffers = stackalloc GCHandle[input.Length];
                pinnedBuffers.Clear();
                byte[][] buffers = new byte[input.Length][];
                try
                {
                    for (int i = 0; i < input.Length; i++)
                    {
                        ref readonly SecurityBuffer iBuffer = ref input[i];
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
                        ref SecurityBuffer iBuffer = ref input[i];
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

        public static bool QueryBlittableContextAttributes<T>(SSPIInterface secModule, SafeDeleteContext securityContext, Interop.SspiCli.ContextAttribute contextAttribute, ref T attribute) where T : unmanaged
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(null, contextAttribute);

            Span<T> span =
#if netstandard
                stackalloc T[1] { attribute };
#else
                MemoryMarshal.CreateSpan(ref attribute, 1);
#endif
            int errorCode = secModule.QueryContextAttributes(
                securityContext, contextAttribute,
                MemoryMarshal.AsBytes(span),
                null,
                out SafeHandle sspiHandle);
#if netstandard
            attribute = span[0];
#endif

            using (sspiHandle)
            {
                if (errorCode != 0)
                {
                    if (NetEventSource.IsEnabled) NetEventSource.Exit(null, $"ERROR = {ErrorDescription(errorCode)}");
                    return false;
                }

                if (NetEventSource.IsEnabled) NetEventSource.Exit(null, attribute);
                return true;
            }
        }

        public static bool QueryBlittableContextAttributes<T>(SSPIInterface secModule, SafeDeleteContext securityContext, Interop.SspiCli.ContextAttribute contextAttribute, Type safeHandleType, out SafeHandle sspiHandle, ref T attribute) where T : unmanaged
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(null, contextAttribute);

            Span<T> span =
#if netstandard
                stackalloc T[1] { attribute };
#else
                MemoryMarshal.CreateSpan(ref attribute, 1);
#endif
            int errorCode = secModule.QueryContextAttributes(
                securityContext, contextAttribute,
                MemoryMarshal.AsBytes(span),
                safeHandleType,
                out sspiHandle);
#if netstandard
            attribute = span[0];
#endif

            if (errorCode != 0)
            {
                if (NetEventSource.IsEnabled) NetEventSource.Exit(null, $"ERROR = {ErrorDescription(errorCode)}");
                return false;
            }

            if (NetEventSource.IsEnabled) NetEventSource.Exit(null, attribute);
            return true;
        }

        public static string QueryStringContextAttributes(SSPIInterface secModule, SafeDeleteContext securityContext, Interop.SspiCli.ContextAttribute contextAttribute)
        {
            Debug.Assert(
                contextAttribute == Interop.SspiCli.ContextAttribute.SECPKG_ATTR_NAMES ||
                contextAttribute == Interop.SspiCli.ContextAttribute.SECPKG_ATTR_CLIENT_SPECIFIED_TARGET);

            if (NetEventSource.IsEnabled) NetEventSource.Enter(null, contextAttribute);

            Span<IntPtr> buffer = stackalloc IntPtr[1];
            int errorCode = secModule.QueryContextAttributes(
                securityContext,
                contextAttribute,
                MemoryMarshal.AsBytes(buffer),
                typeof(SafeFreeContextBuffer),
                out SafeHandle sspiHandle);
            using (sspiHandle)
            {
                if (errorCode != 0)
                {
                    if (NetEventSource.IsEnabled) NetEventSource.Exit(null, $"ERROR = {ErrorDescription(errorCode)}");
                    return null;
                }

                string result = Marshal.PtrToStringUni(sspiHandle.DangerousGetHandle());
                if (NetEventSource.IsEnabled) NetEventSource.Exit(null, result);
                return result;
            }
        }

        public static SafeFreeCertContext QueryContextAttributes_SECPKG_ATTR_REMOTE_CERT_CONTEXT(SSPIInterface secModule, SafeDeleteContext securityContext)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(null);

            Span<IntPtr> buffer = stackalloc IntPtr[1];
            int errorCode = secModule.QueryContextAttributes(
                securityContext,
                Interop.SspiCli.ContextAttribute.SECPKG_ATTR_REMOTE_CERT_CONTEXT,
                MemoryMarshal.AsBytes(buffer),
                typeof(SafeFreeCertContext),
                out SafeHandle sspiHandle);

            if (errorCode != 0)
            {
                sspiHandle?.Dispose();
                if (NetEventSource.IsEnabled) NetEventSource.Exit(null, $"ERROR = {ErrorDescription(errorCode)}");
                return null;
            }

            var result = (SafeFreeCertContext)sspiHandle;
            if (NetEventSource.IsEnabled) NetEventSource.Exit(null, result);
            return result;
        }

        public static bool QueryContextAttributes_SECPKG_ATTR_ISSUER_LIST_EX(SSPIInterface secModule, SafeDeleteContext securityContext, ref Interop.SspiCli.SecPkgContext_IssuerListInfoEx ctx, out SafeHandle sspiHandle)
        {
            if (NetEventSource.IsEnabled) NetEventSource.Enter(null);

            Span<Interop.SspiCli.SecPkgContext_IssuerListInfoEx> buffer =
#if netstandard
                stackalloc Interop.SspiCli.SecPkgContext_IssuerListInfoEx[1] { ctx };
#else
                MemoryMarshal.CreateSpan(ref ctx, 1);
#endif
            int errorCode = secModule.QueryContextAttributes(
                securityContext,
                Interop.SspiCli.ContextAttribute.SECPKG_ATTR_ISSUER_LIST_EX,
                MemoryMarshal.AsBytes(buffer),
                typeof(SafeFreeContextBuffer),
                out sspiHandle);
#if netstandard
            ctx = buffer[0];
#endif

            if (errorCode != 0)
            {
                if (NetEventSource.IsEnabled) NetEventSource.Exit(null, $"ERROR = {ErrorDescription(errorCode)}");
                return false;
            }

            if (NetEventSource.IsEnabled) NetEventSource.Exit(null, ctx);
            return true;
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
