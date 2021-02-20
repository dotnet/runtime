// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class AndroidCrypto
    {
        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_DecodeX509")]
        internal static extern SafeX509Handle DecodeX509(ref byte buf, int len);

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_EncodeX509")]
        private static extern int EncodeX509(SafeX509Handle x, [Out] byte[]? buf, int len);
        internal static byte[] EncodeX509(SafeX509Handle x)
        {
            return Crypto.GetDynamicBuffer((ptr, buf, i) => EncodeX509(ptr, buf, i), x);
        }

        internal struct X509BasicInformation
        {
            public int Version { get; }
            public DateTime NotAfter { get; }
            public DateTime NotBefore { get; }

            public X509BasicInformation(int version, DateTime notAfter, DateTime notBefore)
            {
                Version = version;
                NotAfter = notAfter;
                NotBefore = notBefore;
            }

            internal struct Interop
            {
                public int Version;
                public long NotAfter;     // In milliseconds since Unix epoch
                public long NotBefore;    // In milliseconds since Unix epoch
            }
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_X509GetBasicInformation")]
        private static extern byte X509GetBasicInformationImpl(SafeX509Handle x509, out X509BasicInformation.Interop info);
        internal static X509BasicInformation X509GetBasicInformation(SafeX509Handle x509)
        {
            X509BasicInformation.Interop info;
            byte success = X509GetBasicInformationImpl(x509, out info);
            if (success == 0 || info.Version < 1)
                throw new CryptographicException();

            return new X509BasicInformation(
                info.Version,
                DateTime.UnixEpoch.AddMilliseconds(info.NotAfter),
                DateTime.UnixEpoch.AddMilliseconds(info.NotBefore));
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_X509GetPublicKeyAlgorithm")]
        private static extern int X509GetPublicKeyAlgorithm(SafeX509Handle x509, byte[]? buf, int len);
        internal static string X509GetPublicKeyAlgorithm(SafeX509Handle x509)
        {
            // Null terminator is included in byte array, so we ignore the last byte.
            byte[] bytes = Crypto.GetDynamicBuffer((handle, buf, i) => X509GetPublicKeyAlgorithm(handle, buf, i), x509);
            if (bytes.Length <= 1)
                throw Interop.Crypto.CreateOpenSslCryptographicException();

            return System.Text.Encoding.UTF8.GetString(bytes[..^1]);
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_X509GetPublicKeyBytes")]
        private static extern int X509GetPublicKeyBytes(SafeX509Handle x509, byte[]? buf, int len);
        internal static byte[] X509GetPublicKeyBytes(SafeX509Handle x509)
        {
            return Crypto.GetDynamicBuffer((handle, buf, i) => X509GetPublicKeyBytes(handle, buf, i), x509);
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_X509GetPublicKeyParameterBytes")]
        private static extern int X509GetPublicKeyParameterBytes(SafeX509Handle x509, byte[]? buf, int len);
        internal static byte[] X509GetPublicKeyParameterBytes(SafeX509Handle x509)
        {
            return Crypto.GetDynamicBuffer((handle, buf, i) => X509GetPublicKeyParameterBytes(handle, buf, i), x509);
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_X509GetSerialNumber")]
        private static extern int X509GetSerialNumber(SafeX509Handle x509, [Out] byte[]? buf, int len);
        internal static byte[] X509GetSerialNumber(SafeX509Handle x509)
        {
            return Crypto.GetDynamicBuffer((handle, buf, i) => X509GetSerialNumber(handle, buf, i), x509);
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_X509GetSignatureAlgorithm")]
        private static extern int X509GetSignatureAlgorithm(SafeX509Handle x509, byte[]? buf, int len);
        internal static string X509GetSignatureAlgorithm(SafeX509Handle x509)
        {
            // Null terminator is included in byte array, so we ignore the last byte.
            byte[] oidBytes = Crypto.GetDynamicBuffer((handle, buf, i) => X509GetSignatureAlgorithm(handle, buf, i), x509);
            if (oidBytes.Length <= 1)
                throw Interop.Crypto.CreateOpenSslCryptographicException();

            return System.Text.Encoding.UTF8.GetString(oidBytes[..^1]);
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_X509GetThumbprint")]
        private static extern int X509GetThumbprint(SafeX509Handle x509, byte[]? buf, int len);
        internal static byte[] X509GetThumbprint(SafeX509Handle x509)
        {
            return Crypto.GetDynamicBuffer((handle, buf, i) => X509GetThumbprint(handle, buf, i), x509);
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_X509GetIssuerNameBytes")]
        internal static extern int X509GetIssuerNameBytes(SafeX509Handle x509, byte[]? buf, int len);
        internal static X500DistinguishedName X509GetIssuerName(SafeX509Handle x509)
        {
            byte[] buf = Crypto.GetDynamicBuffer((handle, buf, i) => X509GetIssuerNameBytes(handle, buf, i), x509);
            return new X500DistinguishedName(buf);
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_X509GetSubjectNameBytes")]
        internal static extern int X509GetSubjectNameBytes(SafeX509Handle x509, byte[]? buf, int len);
        internal static X500DistinguishedName X509GetSubjectName(SafeX509Handle x509)
        {
            byte[] buf = Crypto.GetDynamicBuffer((handle, buf, i) => X509GetSubjectNameBytes(handle, buf, i), x509);
            return new X500DistinguishedName(buf);
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_X509EnumExtensions")]
        internal static unsafe extern void X509EnumExtensions(
            SafeX509Handle x,
            delegate* unmanaged<byte*, int, byte*, int, byte, void*, void> callback,
            void* callbackContext);

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_X509FindExtensionData")]
        private static extern int X509FindExtensionData(SafeX509Handle x509, string oid, [Out] byte[]? buf, int len);
        internal static byte[] X509FindExtensionData(SafeX509Handle x509, string oid)
        {
            return Crypto.GetDynamicBuffer((handle, buf, i) => X509FindExtensionData(handle, oid, buf, i), x509);
        }
    }
}
