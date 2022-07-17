// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Net.Security;
using System.Runtime.InteropServices;

namespace System.Net
{
    internal static class SSPIWrapper
    {
        internal static SecurityPackageInfoClass[] EnumerateSecurityPackages(ISSPIInterface secModule)
        {
            if (secModule.SecurityPackages == null)
            {
                lock (secModule)
                {
                    if (secModule.SecurityPackages == null)
                    {
                        int moduleCount = 0;
                        SafeFreeContextBuffer? arrayBaseHandle = null;
                        try
                        {
                            int errorCode = secModule.EnumerateSecurityPackages(out moduleCount, out arrayBaseHandle);
                            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"arrayBase: {arrayBaseHandle}");
                            if (errorCode != 0)
                            {
                                throw new Win32Exception(errorCode);
                            }

                            var securityPackages = new SecurityPackageInfoClass[moduleCount];

                            int i;
                            for (i = 0; i < moduleCount; i++)
                            {
                                securityPackages[i] = new SecurityPackageInfoClass(arrayBaseHandle, i);
                                if (NetEventSource.Log.IsEnabled()) NetEventSource.Log.EnumerateSecurityPackages(securityPackages[i].Name);
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

            return secModule.SecurityPackages;
        }

        internal static SecurityPackageInfoClass? GetVerifyPackageInfo(ISSPIInterface secModule, string packageName, bool throwIfMissing)
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

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Log.SspiPackageNotFound(packageName);

            if (throwIfMissing)
            {
                throw new NotSupportedException(SR.net_securitypackagesupport);
            }

            return null;
        }

        public static SafeFreeCredentials AcquireDefaultCredential(ISSPIInterface secModule, string package, Interop.SspiCli.CredentialUse intent)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Log.AcquireDefaultCredential(package, intent);

            SafeFreeCredentials? outCredential = null;
            int errorCode = secModule.AcquireDefaultCredential(package, intent, out outCredential);

            if (errorCode != 0)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(null, SR.Format(SR.net_log_operation_failed_with_error, nameof(AcquireDefaultCredential), $"0x{errorCode:X}"));
                throw new Win32Exception(errorCode);
            }
            return outCredential;
        }

        public static SafeFreeCredentials AcquireCredentialsHandle(ISSPIInterface secModule, string package, Interop.SspiCli.CredentialUse intent, ref SafeSspiAuthDataHandle authdata)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Log.AcquireCredentialsHandle(package, intent, authdata);

            SafeFreeCredentials? credentialsHandle = null;
            int errorCode = secModule.AcquireCredentialsHandle(package, intent, ref authdata, out credentialsHandle);

            if (errorCode != 0)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(null, SR.Format(SR.net_log_operation_failed_with_error, nameof(AcquireCredentialsHandle), $"0x{errorCode:X}"));
                throw new Win32Exception(errorCode);
            }

            return credentialsHandle;
        }

        public static unsafe SafeFreeCredentials AcquireCredentialsHandle(ISSPIInterface secModule, string package, Interop.SspiCli.CredentialUse intent, Interop.SspiCli.SCHANNEL_CRED* scc)
        {
            int errorCode = secModule.AcquireCredentialsHandle(
                                            package,
                                            intent,
                                            scc,
                                            out SafeFreeCredentials outCredential);

            if (errorCode != 0)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(null, SR.Format(SR.net_log_operation_failed_with_error, nameof(AcquireCredentialsHandle), $"0x{errorCode:X}"));
                throw new Win32Exception(errorCode);
            }

            return outCredential;
        }

        public static unsafe SafeFreeCredentials AcquireCredentialsHandle(ISSPIInterface secModule, string package, Interop.SspiCli.CredentialUse intent, Interop.SspiCli.SCH_CREDENTIALS* scc)
        {
            int errorCode = secModule.AcquireCredentialsHandle(
                                            package,
                                            intent,
                                            scc,
                                            out SafeFreeCredentials outCredential);

            if (errorCode != 0)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(null, SR.Format(SR.net_log_operation_failed_with_error, nameof(AcquireCredentialsHandle), $"0x{errorCode:X}"));
                throw new Win32Exception(errorCode);
            }

            return outCredential;
        }

        internal static int InitializeSecurityContext(ISSPIInterface secModule, ref SafeFreeCredentials? credential, ref SafeDeleteSslContext? context, string? targetName, Interop.SspiCli.ContextFlags inFlags, Interop.SspiCli.Endianness datarep, InputSecurityBuffers inputBuffers, ref SecurityBuffer outputBuffer, ref Interop.SspiCli.ContextFlags outFlags)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Log.InitializeSecurityContext(credential, context, targetName, inFlags);

            int errorCode = secModule.InitializeSecurityContext(ref credential, ref context, targetName, inFlags, datarep, inputBuffers, ref outputBuffer, ref outFlags);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Log.SecurityContextInputBuffers(nameof(InitializeSecurityContext), inputBuffers.Count, outputBuffer.size, (Interop.SECURITY_STATUS)errorCode);

            return errorCode;
        }

        internal static int AcceptSecurityContext(ISSPIInterface secModule, SafeFreeCredentials? credential, ref SafeDeleteSslContext? context, Interop.SspiCli.ContextFlags inFlags, Interop.SspiCli.Endianness datarep, InputSecurityBuffers inputBuffers, ref SecurityBuffer outputBuffer, ref Interop.SspiCli.ContextFlags outFlags)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Log.AcceptSecurityContext(credential, context, inFlags);

            int errorCode = secModule.AcceptSecurityContext(credential, ref context, inputBuffers, inFlags, datarep, ref outputBuffer, ref outFlags);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Log.SecurityContextInputBuffers(nameof(AcceptSecurityContext), inputBuffers.Count, outputBuffer.size, (Interop.SECURITY_STATUS)errorCode);

            return errorCode;
        }

        internal static int CompleteAuthToken(ISSPIInterface secModule, ref SafeDeleteSslContext? context, in InputSecurityBuffer inputBuffer)
        {
            int errorCode = secModule.CompleteAuthToken(ref context, in inputBuffer);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Log.OperationReturnedSomething(nameof(CompleteAuthToken), (Interop.SECURITY_STATUS)errorCode);

            return errorCode;
        }

        internal static int ApplyControlToken(ISSPIInterface secModule, ref SafeDeleteContext? context, in SecurityBuffer inputBuffer)
        {
            int errorCode = secModule.ApplyControlToken(ref context, in inputBuffer);

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Log.OperationReturnedSomething(nameof(ApplyControlToken), (Interop.SECURITY_STATUS)errorCode);

            return errorCode;
        }

        public static int QuerySecurityContextToken(ISSPIInterface secModule, SafeDeleteContext context, out SecurityContextTokenHandle token)
        {
            return secModule.QuerySecurityContextToken(context, out token);
        }

        public static SafeFreeContextBufferChannelBinding? QueryContextChannelBinding(ISSPIInterface secModule, SafeDeleteContext securityContext, Interop.SspiCli.ContextAttribute contextAttribute)
        {
            SafeFreeContextBufferChannelBinding result;
            int errorCode = secModule.QueryContextChannelBinding(securityContext, contextAttribute, out result);
            if (errorCode != 0)
            {
                result.Dispose();
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(null, $"ERROR = {ErrorDescription(errorCode)}");
                return null;
            }

            return result;
        }

        public static bool QueryBlittableContextAttributes<T>(ISSPIInterface secModule, SafeDeleteContext securityContext, Interop.SspiCli.ContextAttribute contextAttribute, ref T attribute) where T : unmanaged
        {
            Span<T> span = new Span<T>(ref attribute);
            int errorCode = secModule.QueryContextAttributes(
                securityContext, contextAttribute,
                MemoryMarshal.AsBytes(span),
                null,
                out SafeHandle? sspiHandle);

            using (sspiHandle)
            {
                if (errorCode != 0)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(null, $"ERROR = {ErrorDescription(errorCode)}");
                    return false;
                }

                return true;
            }
        }

        public static bool QueryBlittableContextAttributes<T>(ISSPIInterface secModule, SafeDeleteContext securityContext, Interop.SspiCli.ContextAttribute contextAttribute, Type safeHandleType, out SafeHandle? sspiHandle, ref T attribute) where T : unmanaged
        {
            Span<T> span = new Span<T>(ref attribute);
            int errorCode = secModule.QueryContextAttributes(
                securityContext, contextAttribute,
                MemoryMarshal.AsBytes(span),
                safeHandleType,
                out sspiHandle);

            if (errorCode != 0)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(null, $"ERROR = {ErrorDescription(errorCode)}");
                return false;
            }

            return true;
        }

        public static string? QueryStringContextAttributes(ISSPIInterface secModule, SafeDeleteContext securityContext, Interop.SspiCli.ContextAttribute contextAttribute)
        {
            Debug.Assert(
                contextAttribute == Interop.SspiCli.ContextAttribute.SECPKG_ATTR_NAMES ||
                contextAttribute == Interop.SspiCli.ContextAttribute.SECPKG_ATTR_CLIENT_SPECIFIED_TARGET);


            Span<IntPtr> buffer = stackalloc IntPtr[1];
            int errorCode = secModule.QueryContextAttributes(
                securityContext,
                contextAttribute,
                MemoryMarshal.AsBytes(buffer),
                typeof(SafeFreeContextBuffer),
                out SafeHandle? sspiHandle);

            Debug.Assert(sspiHandle != null);

            using (sspiHandle)
            {
                if (errorCode != 0)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(null, $"ERROR = {ErrorDescription(errorCode)}");
                    return null;
                }

                string? result = Marshal.PtrToStringUni(sspiHandle.DangerousGetHandle());
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, result);
                return result;
            }
        }

        private static bool QueryCertContextAttribute(ISSPIInterface secModule, SafeDeleteContext securityContext, Interop.SspiCli.ContextAttribute attribute, out SafeFreeCertContext? certContext)
        {
            Span<IntPtr> buffer = stackalloc IntPtr[1];
            int errorCode = secModule.QueryContextAttributes(
                securityContext,
                attribute,
                MemoryMarshal.AsBytes(buffer),
                typeof(SafeFreeCertContext),
                out SafeHandle? sspiHandle);

            // certificate is not always present (e.g. on server when querying client certificate)
            // but we still want to consider such case as a success.
            bool success = errorCode == 0 || errorCode == (int)Interop.SECURITY_STATUS.NoCredentials;

            if (!success)
            {
                sspiHandle?.Dispose();
                sspiHandle = null;
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(null, $"ERROR = {ErrorDescription(errorCode)}");
            }

            certContext = sspiHandle as SafeFreeCertContext;
            return success;
        }

        public static bool QueryContextAttributes_SECPKG_ATTR_REMOTE_CERT_CONTEXT(ISSPIInterface secModule, SafeDeleteContext securityContext, out SafeFreeCertContext? certContext)
            => QueryCertContextAttribute(secModule, securityContext, Interop.SspiCli.ContextAttribute.SECPKG_ATTR_REMOTE_CERT_CONTEXT, out certContext);

        public static bool QueryContextAttributes_SECPKG_ATTR_REMOTE_CERT_CHAIN(ISSPIInterface secModule, SafeDeleteContext securityContext, out SafeFreeCertContext? certContext)
            => QueryCertContextAttribute(secModule, securityContext, Interop.SspiCli.ContextAttribute.SECPKG_ATTR_REMOTE_CERT_CHAIN, out certContext);

        public static bool QueryContextAttributes_SECPKG_ATTR_ISSUER_LIST_EX(ISSPIInterface secModule, SafeDeleteContext securityContext, ref Interop.SspiCli.SecPkgContext_IssuerListInfoEx ctx, out SafeHandle? sspiHandle)
        {
            Span<Interop.SspiCli.SecPkgContext_IssuerListInfoEx> buffer = new Span<Interop.SspiCli.SecPkgContext_IssuerListInfoEx>(ref ctx);
            int errorCode = secModule.QueryContextAttributes(
                securityContext,
                Interop.SspiCli.ContextAttribute.SECPKG_ATTR_ISSUER_LIST_EX,
                MemoryMarshal.AsBytes(buffer),
                typeof(SafeFreeContextBuffer),
                out sspiHandle);

            if (errorCode != 0)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Error(null, $"ERROR = {ErrorDescription(errorCode)}");
                return false;
            }

            return true;
        }

        public static string ErrorDescription(int errorCode)
        {
            if (errorCode == -1)
            {
                return "An exception when invoking Win32 API";
            }

            return (Interop.SECURITY_STATUS)errorCode switch
            {
                Interop.SECURITY_STATUS.InvalidHandle => "Invalid handle",
                Interop.SECURITY_STATUS.InvalidToken => "Invalid token",
                Interop.SECURITY_STATUS.ContinueNeeded => "Continue needed",
                Interop.SECURITY_STATUS.IncompleteMessage => "Message incomplete",
                Interop.SECURITY_STATUS.WrongPrincipal => "Wrong principal",
                Interop.SECURITY_STATUS.TargetUnknown => "Target unknown",
                Interop.SECURITY_STATUS.PackageNotFound => "Package not found",
                Interop.SECURITY_STATUS.BufferNotEnough => "Buffer not enough",
                Interop.SECURITY_STATUS.MessageAltered => "Message altered",
                Interop.SECURITY_STATUS.UntrustedRoot => "Untrusted root",
                _ => "0x" + errorCode.ToString("x", NumberFormatInfo.InvariantInfo),
            };
        }
    } // class SSPIWrapper
}
