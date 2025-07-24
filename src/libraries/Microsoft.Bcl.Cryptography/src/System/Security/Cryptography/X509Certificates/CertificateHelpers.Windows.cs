// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;
using System.Reflection;

namespace System.Security.Cryptography.X509Certificates
{
    internal static partial class CertificateHelpers
    {
        private static CryptographicException GetExceptionForLastError()
        {
#if NETFRAMEWORK
            return Marshal.GetHRForLastWin32Error().ToCryptographicException();
#else
            return Marshal.GetLastPInvokeError().ToCryptographicException();
#endif
        }

#if NETFRAMEWORK
        private static readonly ConstructorInfo? s_safeNCryptKeyHandleCtor_IntPtr_SafeHandle =
            typeof(SafeNCryptKeyHandle).GetConstructor([typeof(IntPtr), typeof(SafeHandle)]);
#endif

        [SupportedOSPlatform("windows")]
        private static SafeNCryptKeyHandle CreateSafeNCryptKeyHandle(IntPtr handle, SafeHandle parentHandle)
        {
#if NETFRAMEWORK
            if (s_safeNCryptKeyHandleCtor_IntPtr_SafeHandle == null)
            {
                // TODO resx
                throw new PlatformNotSupportedException();
            }

            return (SafeNCryptKeyHandle)s_safeNCryptKeyHandleCtor_IntPtr_SafeHandle.Invoke([handle, parentHandle]);
#else
            return new SafeNCryptKeyHandle(handle, parentHandle);
#endif
        }

        [UnsupportedOSPlatform("browser")]
        private static X509Certificate2 CopyFromRawBytes(X509Certificate2 certificate)
        {
            return X509CertificateLoader.LoadCertificate(certificate.RawData);
        }

#pragma warning disable IDE0060 // Remove unused parameter
        private static int GuessKeySpec(CngProvider provider, string keyName, bool machineKey, CngAlgorithmGroup? algorithmGroup) => 0;
#pragma warning restore IDE0060 // Remove unused parameter
    }
}
