// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Xunit;

namespace System.Security.Cryptography.Algorithms.Tests
{
    internal static class CngUtility
    {
        private const string BCRYPT_LIB = "bcrypt.dll";
        private const string MS_PRIMITIVE_PROVIDER = "Microsoft Primitive Provider";

        public static bool IsAlgorithmSupported(string algId, string implementation = MS_PRIMITIVE_PROVIDER)
        {
            Assert.True(PlatformDetection.IsWindows, "Caller should not invoke this method for non-Windows platforms.");

            int ntStatus = BCryptOpenAlgorithmProvider(out SafeBCryptAlgorithmHandle handle, algId, implementation, 0);
            bool isSupported = ntStatus == 0 && handle != null && !handle.IsInvalid;
            handle?.Dispose();
            return isSupported;
        }

        // https://docs.microsoft.com/windows/win32/api/bcrypt/nf-bcrypt-bcryptclosealgorithmprovider
        [DllImport(BCRYPT_LIB, CallingConvention = CallingConvention.Winapi)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern int BCryptCloseAlgorithmProvider(
            [In] IntPtr hAlgorithm,
            [In] uint dwFlags);

        // https://docs.microsoft.com/windows/win32/api/bcrypt/nf-bcrypt-bcryptopenalgorithmprovider
        [DllImport(BCRYPT_LIB, CallingConvention = CallingConvention.Winapi)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern int BCryptOpenAlgorithmProvider(
            [Out] out SafeBCryptAlgorithmHandle phAlgorithm,
            [In, MarshalAs(UnmanagedType.LPWStr)] string pszAlgId,
            [In, MarshalAs(UnmanagedType.LPWStr)] string pszImplementation,
            [In] uint dwFlags);

        internal sealed class SafeBCryptAlgorithmHandle : SafeHandle
        {
            public SafeBCryptAlgorithmHandle()
                : base(IntPtr.Zero, ownsHandle: true)
            {
            }

            public override bool IsInvalid => handle == IntPtr.Zero;

            protected sealed override bool ReleaseHandle()
            {
                int ntStatus = BCryptCloseAlgorithmProvider(handle, 0);
                return ntStatus == 0;
            }
        }
    }
}
