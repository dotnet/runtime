// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EcKeyCreateByOid")]
        private static extern SafeEcKeyHandle CryptoNative_EcKeyCreateByOid(string oid);
        internal static SafeEcKeyHandle? EcKeyCreateByOid(string oid)
        {
            SafeEcKeyHandle handle = CryptoNative_EcKeyCreateByOid(oid);
            if (handle == null || handle.IsInvalid)
            {
                ErrClearError();
            }

            return handle;
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EcKeyDestroy")]
        internal static extern void EcKeyDestroy(IntPtr a);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EcKeyUpRef")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EcKeyUpRef(IntPtr r);

        [DllImport(Libraries.CryptoNative)]
        private static extern int CryptoNative_EcKeyGetSize(SafeEcKeyHandle ecKey, out int keySize);
        internal static int EcKeyGetSize(SafeEcKeyHandle key)
        {
            int keySize;
            int rc = CryptoNative_EcKeyGetSize(key, out keySize);
            if (rc == 1)
            {
                return keySize;
            }
            throw Interop.Crypto.CreateOpenSslCryptographicException();
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EcKeyGetCurveName")]
        private static extern int CryptoNative_EcKeyGetCurveName(SafeEcKeyHandle ecKey, out IntPtr curveName);

        internal static string EcKeyGetCurveName(SafeEcKeyHandle key)
        {
            int rc = CryptoNative_EcKeyGetCurveName(key, out IntPtr curveName);
            if (rc == 1)
            {
                if (curveName == IntPtr.Zero)
                {
                    Debug.Fail("Key is invalid or doesn't have a curve");
                    return string.Empty;
                }

                string curveNameStr = Marshal.PtrToStringUni(curveName)!;
                Marshal.ZeroFreeCoTaskMemUnicode(curveName);
                return curveNameStr;
            }
            throw Interop.Crypto.CreateOpenSslCryptographicException();
        }

        internal static bool EcKeyHasCurveName(SafeEcKeyHandle key)
        {
            int rc = CryptoNative_EcKeyGetCurveName(key, out IntPtr curveName);
            if (rc == 1)
            {
                bool hasName = curveName != IntPtr.Zero;
                Marshal.ZeroFreeCoTaskMemUnicode(curveName);
                return hasName;
            }
            throw Interop.Crypto.CreateOpenSslCryptographicException();
        }
    }
}
