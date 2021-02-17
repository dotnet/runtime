// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class AndroidCrypto
    {
        // [Sig Changed]
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetX509NotBefore")]
        internal static extern ulong GetX509NotBefore(SafeX509Handle x509);

        // [Sig Changed]
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetX509NotAfter")]
        internal static extern ulong GetX509NotAfter(SafeX509Handle x509);

        // [Sig Changed]
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetX509SignatureAlgorithm")]
        private static extern int GetX509SignatureAlgorithm(SafeX509Handle x509, byte[]? buf, int cBuf);
        internal static string GetX509SignatureAlgorithm(SafeX509Handle x509)
        {
            // Null terminator is included in byte array.
            byte[] oidBytes = Crypto.GetDynamicBuffer((handle, buf, i) => GetX509SignatureAlgorithm(handle, buf, i), x509);
            if (oidBytes.Length <= 1)
                throw Interop.Crypto.CreateOpenSslCryptographicException();

            return System.Text.Encoding.UTF8.GetString(oidBytes[..^1]);
        }

        // [Sig Changed]
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetX509PublicKeyAlgorithm")]
        private static extern int GetX509PublicKeyAlgorithm(SafeX509Handle x509, byte[]? buf, int cBuf);
        internal static string GetX509PublicKeyAlgorithm(SafeX509Handle x509)
        {
            // Null terminator is included in byte array.
            byte[] bytes = Crypto.GetDynamicBuffer((handle, buf, i) => GetX509PublicKeyAlgorithm(handle, buf, i), x509);
            if (bytes.Length <= 1)
                throw Interop.Crypto.CreateOpenSslCryptographicException();

            return System.Text.Encoding.UTF8.GetString(bytes[..^1]);
        }

        // [Sig Changed]
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetX509PublicKeyBytes")]
        private static extern int GetX509PublicKeyBytes(SafeX509Handle x509, byte[]? buf, int cBuf);
        internal static byte[] GetX509PublicKeyBytes(SafeX509Handle x509)
        {
            return Crypto.GetDynamicBuffer((handle, buf, i) => GetX509PublicKeyBytes(handle, buf, i), x509);
        }

        // [Sig Changed]
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EncodeX509")]
        private static extern int EncodeX509(SafeX509Handle x, [Out] byte[]? buf, int len);
        internal static byte[] EncodeX509(SafeX509Handle x)
        {
            return Crypto.GetDynamicBuffer((ptr, buf, i) => EncodeX509(ptr, buf, i), x);
        }

        // [Sig Changed]
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509GetSerialNumber")]
        private static extern int X509GetSerialNumber(SafeX509Handle x, [Out] byte[]? buf, int len);
        internal static byte[] X509GetSerialNumber(SafeX509Handle x)
        {
            return Crypto.GetDynamicBuffer((ptr, buf, i) => X509GetSerialNumber(ptr, buf, i), x);
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_X509EnumExtensions")]
        internal static unsafe extern void X509EnumExtensions(
            SafeX509Handle x,
            delegate* unmanaged<byte*, int, byte*, int, byte, void*, void> callback,
            void* callbackContext);

        // [Sig Changed]
        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_X509FindExtensionData")]
        private static extern int X509FindExtensionData(SafeX509Handle x, string oid, [Out] byte[]? buf, int len);
        internal static byte[] X509FindExtensionData(SafeX509Handle x, string oid)
        {
            return Crypto.GetDynamicBuffer((ptr, buf, i) => X509FindExtensionData(ptr, oid, buf, i), x);
        }
    }
}
