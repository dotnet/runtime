// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EcKeyCreateByOid", StringMarshalling = StringMarshalling.Utf8)]
        private static partial SafeEcKeyHandle CryptoNative_EcKeyCreateByOid(string oid);
        internal static SafeEcKeyHandle? EcKeyCreateByOid(string oid)
        {
            SafeEcKeyHandle handle = CryptoNative_EcKeyCreateByOid(oid);
            if (handle == null || handle.IsInvalid)
            {
                ErrClearError();
            }

            return handle;
        }

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EcKeyDestroy")]
        internal static partial void EcKeyDestroy(IntPtr a);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EcKeyGenerateKey")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool EcKeyGenerateKey(SafeEcKeyHandle eckey);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EcKeyUpRef")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool EcKeyUpRef(IntPtr r);

        [LibraryImport(Libraries.CryptoNative)]
        private static partial int CryptoNative_EcKeyGetSize(SafeEcKeyHandle ecKey, out int keySize);
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

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EcKeyGetCurveName2")]
        private static partial int CryptoNative_EcKeyGetCurveName(SafeEcKeyHandle ecKey, out int nid);

        internal static string EcKeyGetCurveName(SafeEcKeyHandle key)
        {
            int nidCurveName;
            int rc = CryptoNative_EcKeyGetCurveName(key, out nidCurveName);
            if (rc == 1)
            {
                if (nidCurveName == Interop.Crypto.NID_undef)
                {
                    Debug.Fail("Key is invalid or doesn't have a curve");
                    return string.Empty;
                }

                IntPtr objCurveName = Interop.Crypto.ObjNid2Obj(nidCurveName);
                if (objCurveName != IntPtr.Zero)
                {
                    return Interop.Crypto.GetOidValue(objCurveName);
                }
            }
            throw Interop.Crypto.CreateOpenSslCryptographicException();
        }

        internal static bool EcKeyHasCurveName(SafeEcKeyHandle key)
        {
            int nidCurveName;
            int rc = CryptoNative_EcKeyGetCurveName(key, out nidCurveName);
            if (rc == 1)
            {
                // Key is invalid or doesn't have a curve
                return (nidCurveName != Interop.Crypto.NID_undef);
            }
            throw Interop.Crypto.CreateOpenSslCryptographicException();
        }
    }
}
