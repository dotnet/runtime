// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Apple;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class AppleCrypto
    {
        [DllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_RsaGenerateKey")]
        private static extern int AppleCryptoNative_RsaGenerateKey(
            int keySizeInBits,
            SafeKeychainHandle keychain,
            out SafeSecKeyRefHandle pPublicKey,
            out SafeSecKeyRefHandle pPrivateKey,
            out int pOSStatus);

        [DllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_RsaEncryptOaep")]
        private static extern int RsaEncryptOaep(
            SafeSecKeyRefHandle publicKey,
            byte[] pbData,
            int cbData,
            PAL_HashAlgorithm mgfAlgorithm,
            out SafeCFDataHandle pEncryptedOut,
            out SafeCFErrorHandle pErrorOut);

        [DllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_RsaEncryptPkcs")]
        private static extern int RsaEncryptPkcs(
            SafeSecKeyRefHandle publicKey,
            byte[] pbData,
            int cbData,
            out SafeCFDataHandle pEncryptedOut,
            out SafeCFErrorHandle pErrorOut);

        [DllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_RsaDecryptOaep")]
        private static extern int RsaDecryptOaep(
            SafeSecKeyRefHandle publicKey,
            byte[] pbData,
            int cbData,
            PAL_HashAlgorithm mgfAlgorithm,
            out SafeCFDataHandle pEncryptedOut,
            out SafeCFErrorHandle pErrorOut);

        [DllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_RsaDecryptPkcs")]
        private static extern int RsaDecryptPkcs(
            SafeSecKeyRefHandle publicKey,
            byte[] pbData,
            int cbData,
            out SafeCFDataHandle pEncryptedOut,
            out SafeCFErrorHandle pErrorOut);

        internal static void RsaGenerateKey(
            int keySizeInBits,
            out SafeSecKeyRefHandle pPublicKey,
            out SafeSecKeyRefHandle pPrivateKey)
        {
            using (SafeTemporaryKeychainHandle tempKeychain = CreateTemporaryKeychain())
            {
                SafeSecKeyRefHandle keychainPublic;
                SafeSecKeyRefHandle keychainPrivate;
                int osStatus;

                int result = AppleCryptoNative_RsaGenerateKey(
                    keySizeInBits,
                    tempKeychain,
                    out keychainPublic,
                    out keychainPrivate,
                    out osStatus);

                if (result == 1)
                {
                    pPublicKey = keychainPublic;
                    pPrivateKey = keychainPrivate;
                    return;
                }

                using (keychainPrivate)
                using (keychainPublic)
                {
                    if (result == 0)
                    {
                        throw CreateExceptionForOSStatus(osStatus);
                    }

                    Debug.Fail($"Unexpected result from AppleCryptoNative_RsaGenerateKey: {result}");
                    throw new CryptographicException();
                }
            }
        }

        internal static byte[] RsaEncrypt(
            SafeSecKeyRefHandle publicKey,
            byte[] data,
            RSAEncryptionPadding padding)
        {
            return ExecuteTransform(
                (out SafeCFDataHandle encrypted, out SafeCFErrorHandle error) =>
                {
                    if (padding == RSAEncryptionPadding.Pkcs1)
                    {
                        return RsaEncryptPkcs(publicKey, data, data.Length, out encrypted, out error);
                    }

                    Debug.Assert(padding.Mode == RSAEncryptionPaddingMode.Oaep);

                    return RsaEncryptOaep(
                        publicKey,
                        data,
                        data.Length,
                        PalAlgorithmFromAlgorithmName(padding.OaepHashAlgorithm),
                        out encrypted,
                        out error);
                });

        }

        internal static byte[] RsaDecrypt(
            SafeSecKeyRefHandle privateKey,
            byte[] data,
            RSAEncryptionPadding padding)
        {
            return ExecuteTransform(
                (out SafeCFDataHandle decrypted, out SafeCFErrorHandle error) =>
                {
                    if (padding == RSAEncryptionPadding.Pkcs1)
                    {
                        return RsaDecryptPkcs(privateKey, data, data.Length, out decrypted, out error);
                    }

                    Debug.Assert(padding.Mode == RSAEncryptionPaddingMode.Oaep);

                    return RsaDecryptOaep(
                        privateKey,
                        data,
                        data.Length,
                        PalAlgorithmFromAlgorithmName(padding.OaepHashAlgorithm),
                        out decrypted,
                        out error);
                });
        }

        private static Interop.AppleCrypto.PAL_HashAlgorithm PalAlgorithmFromAlgorithmName(
                HashAlgorithmName hashAlgorithmName)
        {
            if (hashAlgorithmName == HashAlgorithmName.MD5)
            {
                return Interop.AppleCrypto.PAL_HashAlgorithm.Md5;
            }
            else if (hashAlgorithmName == HashAlgorithmName.SHA1)
            {
                return Interop.AppleCrypto.PAL_HashAlgorithm.Sha1;
            }
            else if (hashAlgorithmName == HashAlgorithmName.SHA256)
            {
                return Interop.AppleCrypto.PAL_HashAlgorithm.Sha256;
            }
            else if (hashAlgorithmName == HashAlgorithmName.SHA384)
            {
                return Interop.AppleCrypto.PAL_HashAlgorithm.Sha384;
            }
            else if (hashAlgorithmName == HashAlgorithmName.SHA512)
            {
                return Interop.AppleCrypto.PAL_HashAlgorithm.Sha512;
            }

            throw new CryptographicException(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithmName.Name);
        }
    }
}
