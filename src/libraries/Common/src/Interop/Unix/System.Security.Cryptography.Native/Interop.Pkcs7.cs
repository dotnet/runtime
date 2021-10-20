// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_PemReadBioPkcs7")]
        internal static partial SafePkcs7Handle PemReadBioPkcs7(SafeBioHandle bp);

        internal static SafePkcs7Handle DecodePkcs7(ReadOnlySpan<byte> buf) =>
            DecodePkcs7(ref MemoryMarshal.GetReference(buf), buf.Length);

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_DecodePkcs7")]
        private static partial SafePkcs7Handle DecodePkcs7(ref byte buf, int len);

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_D2IPkcs7Bio")]
        internal static partial SafePkcs7Handle D2IPkcs7Bio(SafeBioHandle bp);

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_Pkcs7CreateCertificateCollection")]
        internal static partial SafePkcs7Handle Pkcs7CreateCertificateCollection(SafeX509StackHandle certs);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_Pkcs7Destroy")]
        internal static extern void Pkcs7Destroy(IntPtr p7);

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetPkcs7Certificates")]
        private static partial int GetPkcs7Certificates(SafePkcs7Handle p7, out SafeSharedX509StackHandle certs);

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetPkcs7DerSize")]
        internal static partial int GetPkcs7DerSize(SafePkcs7Handle p7);

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EncodePkcs7")]
        internal static partial int EncodePkcs7(SafePkcs7Handle p7, byte[] buf);

        internal static SafeSharedX509StackHandle GetPkcs7Certificates(SafePkcs7Handle p7)
        {
            if (p7 == null || p7.IsInvalid)
            {
                return SafeSharedX509StackHandle.InvalidHandle;
            }

            SafeSharedX509StackHandle certs;
            int result = GetPkcs7Certificates(p7, out certs);

            if (result != 1)
            {
                throw Interop.Crypto.CreateOpenSslCryptographicException();
            }

            // Track the parent relationship for the interior pointer so lifetime is well-managed.
            certs.SetParent(p7);

            return certs;
        }
    }
}
