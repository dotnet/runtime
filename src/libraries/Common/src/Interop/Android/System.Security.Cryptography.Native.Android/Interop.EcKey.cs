// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

internal static partial class Interop
{
    internal static partial class AndroidCrypto
    {
        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_EcKeyCreateByOid", StringMarshalling = StringMarshalling.Utf8)]
        private static partial SafeEcKeyHandle AndroidCryptoNative_EcKeyCreateByOid(string oid);
        internal static SafeEcKeyHandle? EcKeyCreateByOid(string oid)
        {
            SafeEcKeyHandle handle = AndroidCryptoNative_EcKeyCreateByOid(oid);

            return handle;
        }

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_EcKeyDestroy")]
        internal static partial void EcKeyDestroy(IntPtr a);

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_EcKeyUpRef")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool EcKeyUpRef(IntPtr r);

        [LibraryImport(Libraries.AndroidCryptoNative)]
        private static partial int AndroidCryptoNative_EcKeyGetSize(SafeEcKeyHandle ecKey, out int keySize);
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

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_EcKeyGetCurveName")]
        private static partial int AndroidCryptoNative_EcKeyGetCurveName(SafeEcKeyHandle ecKey, out IntPtr curveName);

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

namespace System.Security.Cryptography
{
    internal sealed class SafeEcKeyHandle : SafeKeyHandle
    {
        public SafeEcKeyHandle()
        {
        }

        internal SafeEcKeyHandle(IntPtr ptr)
        {
            SetHandle(ptr);
        }

        protected override bool ReleaseHandle()
        {
            Interop.AndroidCrypto.EcKeyDestroy(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }

        internal static SafeEcKeyHandle DuplicateHandle(IntPtr handle)
        {
            Debug.Assert(handle != IntPtr.Zero);

            // Reliability: Allocate the SafeHandle before calling EC_KEY_up_ref so
            // that we don't lose a tracked reference in low-memory situations.
            SafeEcKeyHandle safeHandle = new SafeEcKeyHandle();

            if (!Interop.AndroidCrypto.EcKeyUpRef(handle))
            {
                safeHandle.Dispose();
                throw new CryptographicException();
            }

            safeHandle.SetHandle(handle);
            return safeHandle;
        }

        internal override SafeEcKeyHandle DuplicateHandle() => DuplicateHandle(DangerousGetHandle());
    }
}
