// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates
{
    internal static partial class CertificateHelpers
    {
        private static partial int GuessKeySpec(CngProvider provider, string keyName, bool machineKey, CngAlgorithmGroup? algorithmGroup) => 0;

        [UnsupportedOSPlatform("browser")]
        private static partial X509Certificate2 CopyFromRawBytes(X509Certificate2 certificate) =>
            X509CertificateLoader.LoadCertificate(certificate.RawData);

        private static partial CryptographicException GetExceptionForLastError() =>
#if NETFRAMEWORK
            Marshal.GetHRForLastWin32Error().ToCryptographicException();
#else
            Marshal.GetLastPInvokeError().ToCryptographicException();
#endif

#if NETFRAMEWORK
        private static readonly System.Reflection.ConstructorInfo? s_safeNCryptKeyHandleCtor_IntPtr_SafeHandle =
            typeof(SafeNCryptKeyHandle).GetConstructor([typeof(IntPtr), typeof(SafeHandle)]);
#endif

        [SupportedOSPlatform("windows")]
        private static partial SafeNCryptKeyHandle CreateSafeNCryptKeyHandle(IntPtr handle, SafeHandle parentHandle)
        {
#if NETFRAMEWORK
            if (s_safeNCryptKeyHandleCtor_IntPtr_SafeHandle == null)
            {
                throw new PlatformNotSupportedException();
            }

            return (SafeNCryptKeyHandle)s_safeNCryptKeyHandleCtor_IntPtr_SafeHandle.Invoke([handle, parentHandle]);
#else
            return new SafeNCryptKeyHandle(handle, parentHandle);
#endif
        }
    }
}
