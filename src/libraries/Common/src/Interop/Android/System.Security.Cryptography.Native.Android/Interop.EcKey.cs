// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

internal static partial class Interop
{
    internal static partial class AndroidCrypto
    {
        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_EcKeyCreateByOid")]
        private static extern SafeEcKeyHandle AndroidCryptoNative_EcKeyCreateByOid(string oid);
        internal static SafeEcKeyHandle? EcKeyCreateByOid(string oid)
        {
            SafeEcKeyHandle handle = AndroidCryptoNative_EcKeyCreateByOid(oid);

            return handle;
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_EcKeyDestroy")]
        internal static extern void EcKeyDestroy(IntPtr a);

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_EcKeyUpRef")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EcKeyUpRef(IntPtr r);

        [DllImport(Libraries.CryptoNative)]
        private static extern int AndroidCryptoNative_EcKeyGetSize(SafeEcKeyHandle ecKey, out int keySize);
        internal static int EcKeyGetSize(SafeEcKeyHandle key)
        {
            int keySize;
            int rc = AndroidCryptoNative_EcKeyGetSize(key, out keySize);
            if (rc == 1)
            {
                return keySize;
            }
            throw new CryptographicException();
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_EcKeyGetCurveName")]
        private static extern int AndroidCryptoNative_EcKeyGetCurveName(SafeEcKeyHandle ecKey, out IntPtr curveName);

        internal static string? EcKeyGetCurveName(SafeEcKeyHandle key)
        {
            int rc = AndroidCryptoNative_EcKeyGetCurveName(key, out IntPtr curveName);
            if (rc == 1)
            {
                if (curveName == IntPtr.Zero)
                {
                    return null;
                }

                string curveNameStr = Marshal.PtrToStringUni(curveName)!;
                Marshal.FreeHGlobal(curveName);
                return curveNameStr;
            }
            throw new CryptographicException();
        }

        internal static bool EcKeyHasCurveName(SafeEcKeyHandle key)
        {
            int rc = AndroidCryptoNative_EcKeyGetCurveName(key, out IntPtr curveName);
            if (rc == 1)
            {
                bool hasName = curveName != IntPtr.Zero;
                Marshal.FreeHGlobal(curveName);
                return hasName;
            }
            throw new CryptographicException();
        }
    }
}
