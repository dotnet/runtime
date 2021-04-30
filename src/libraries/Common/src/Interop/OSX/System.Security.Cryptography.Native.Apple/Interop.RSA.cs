// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        [DllImport(Libraries.AppleCryptoNative)]
        private static extern int AppleCryptoNative_RsaGenerateKey(
            int keySizeInBits,
            out SafeSecKeyRefHandle pPublicKey,
            out SafeSecKeyRefHandle pPrivateKey,
            out SafeCFErrorHandle pErrorOut);

        [DllImport(Libraries.AppleCryptoNative)]
        private static extern int AppleCryptoNative_RsaSignaturePrimitive(
            SafeSecKeyRefHandle privateKey,
            ref byte pbData,
            int cbData,
            out SafeCFDataHandle pDataOut,
            out SafeCFErrorHandle pErrorOut);

        [DllImport(Libraries.AppleCryptoNative)]
        private static extern int AppleCryptoNative_RsaVerificationPrimitive(
            SafeSecKeyRefHandle publicKey,
            ref byte pbData,
            int cbData,
            out SafeCFDataHandle pDataOut,
            out SafeCFErrorHandle pErrorOut);

        [DllImport(Libraries.AppleCryptoNative)]
        private static extern int AppleCryptoNative_RsaDecryptionPrimitive(
            SafeSecKeyRefHandle privateKey,
            ref byte pbData,
            int cbData,
            out SafeCFDataHandle pDataOut,
            out SafeCFErrorHandle pErrorOut);

        [DllImport(Libraries.AppleCryptoNative)]
        private static extern int AppleCryptoNative_RsaEncryptionPrimitive(
            SafeSecKeyRefHandle publicKey,
            ref byte pbData,
            int cbData,
            out SafeCFDataHandle pDataOut,
            out SafeCFErrorHandle pErrorOut);

        private static int RsaEncryptOaep(
            SafeSecKeyRefHandle publicKey,
            ReadOnlySpan<byte> pbData,
            int cbData,
            PAL_HashAlgorithm mgfAlgorithm,
            out SafeCFDataHandle pEncryptedOut,
            out SafeCFErrorHandle pErrorOut) =>
            RsaEncryptOaep(publicKey, ref MemoryMarshal.GetReference(pbData), cbData, mgfAlgorithm, out pEncryptedOut, out pErrorOut);

        [DllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_RsaEncryptOaep")]
        private static extern int RsaEncryptOaep(
            SafeSecKeyRefHandle publicKey,
            ref byte pbData,
            int cbData,
            PAL_HashAlgorithm mgfAlgorithm,
            out SafeCFDataHandle pEncryptedOut,
            out SafeCFErrorHandle pErrorOut);

        private static int RsaEncryptPkcs(
            SafeSecKeyRefHandle publicKey,
            ReadOnlySpan<byte> pbData,
            int cbData,
            out SafeCFDataHandle pEncryptedOut,
            out SafeCFErrorHandle pErrorOut) =>
            RsaEncryptPkcs(publicKey, ref MemoryMarshal.GetReference(pbData), cbData, out pEncryptedOut, out pErrorOut);

        [DllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_RsaEncryptPkcs")]
        private static extern int RsaEncryptPkcs(
            SafeSecKeyRefHandle publicKey,
            ref byte pbData,
            int cbData,
            out SafeCFDataHandle pEncryptedOut,
            out SafeCFErrorHandle pErrorOut);

        private static int RsaDecryptOaep(
            SafeSecKeyRefHandle publicKey,
            ReadOnlySpan<byte> pbData,
            int cbData,
            PAL_HashAlgorithm mgfAlgorithm,
            out SafeCFDataHandle pEncryptedOut,
            out SafeCFErrorHandle pErrorOut) =>
            RsaDecryptOaep(publicKey, ref MemoryMarshal.GetReference(pbData), cbData, mgfAlgorithm, out pEncryptedOut, out pErrorOut);

        [DllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_RsaDecryptOaep")]
        private static extern int RsaDecryptOaep(
            SafeSecKeyRefHandle publicKey,
            ref byte pbData,
            int cbData,
            PAL_HashAlgorithm mgfAlgorithm,
            out SafeCFDataHandle pEncryptedOut,
            out SafeCFErrorHandle pErrorOut);

        private static int RsaDecryptPkcs(
            SafeSecKeyRefHandle publicKey,
            ReadOnlySpan<byte> pbData,
            int cbData,
            out SafeCFDataHandle pEncryptedOut,
            out SafeCFErrorHandle pErrorOut) =>
            RsaDecryptPkcs(publicKey, ref MemoryMarshal.GetReference(pbData), cbData, out pEncryptedOut, out pErrorOut);

        [DllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_RsaDecryptPkcs")]
        private static extern int RsaDecryptPkcs(
            SafeSecKeyRefHandle publicKey,
            ref byte pbData,
            int cbData,
            out SafeCFDataHandle pEncryptedOut,
            out SafeCFErrorHandle pErrorOut);

        internal static void RsaGenerateKey(
            int keySizeInBits,
            out SafeSecKeyRefHandle pPublicKey,
            out SafeSecKeyRefHandle pPrivateKey)
        {
            SafeSecKeyRefHandle publicKey;
            SafeSecKeyRefHandle privateKey;
            SafeCFErrorHandle error;

            int result = AppleCryptoNative_RsaGenerateKey(
                keySizeInBits,
                out publicKey,
                out privateKey,
                out error);

            using (error)
            {
                if (result == kSuccess)
                {
                    pPublicKey = publicKey;
                    pPrivateKey = privateKey;
                    return;
                }

                using (privateKey)
                using (publicKey)
                {
                    if (result == kErrorSeeError)
                    {
                        throw CreateExceptionForCFError(error);
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
                data,
                (ReadOnlySpan<byte> source, out SafeCFDataHandle encrypted, out SafeCFErrorHandle error) =>
                {
                    if (padding == RSAEncryptionPadding.Pkcs1)
                    {
                        return RsaEncryptPkcs(publicKey, source, source.Length, out encrypted, out error);
                    }

                    Debug.Assert(padding.Mode == RSAEncryptionPaddingMode.Oaep);

                    return RsaEncryptOaep(
                        publicKey,
                        source,
                        source.Length,
                        PalAlgorithmFromAlgorithmName(padding.OaepHashAlgorithm),
                        out encrypted,
                        out error);
                });
        }

        internal static bool TryRsaEncrypt(
            SafeSecKeyRefHandle publicKey,
            ReadOnlySpan<byte> source,
            Span<byte> destination,
            RSAEncryptionPadding padding,
            out int bytesWritten)
        {
            Debug.Assert(padding.Mode == RSAEncryptionPaddingMode.Pkcs1 || padding.Mode == RSAEncryptionPaddingMode.Oaep);
            return TryExecuteTransform(
                source,
                destination,
                out bytesWritten,
                delegate (ReadOnlySpan<byte> innerSource, out SafeCFDataHandle outputHandle, out SafeCFErrorHandle errorHandle)
                {
                    return padding.Mode == RSAEncryptionPaddingMode.Pkcs1 ?
                        RsaEncryptPkcs(publicKey, innerSource, innerSource.Length, out outputHandle, out errorHandle) :
                        RsaEncryptOaep(publicKey, innerSource, innerSource.Length, PalAlgorithmFromAlgorithmName(padding.OaepHashAlgorithm), out outputHandle, out errorHandle);
                });
        }

        internal static byte[] RsaDecrypt(
            SafeSecKeyRefHandle privateKey,
            byte[] data,
            RSAEncryptionPadding padding)
        {
            return ExecuteTransform(
                data,
                (ReadOnlySpan<byte> source, out SafeCFDataHandle decrypted, out SafeCFErrorHandle error) =>
                {
                    if (padding == RSAEncryptionPadding.Pkcs1)
                    {
                        return RsaDecryptPkcs(privateKey, source, source.Length, out decrypted, out error);
                    }

                    Debug.Assert(padding.Mode == RSAEncryptionPaddingMode.Oaep);

                    return RsaDecryptOaep(
                        privateKey,
                        source,
                        source.Length,
                        PalAlgorithmFromAlgorithmName(padding.OaepHashAlgorithm),
                        out decrypted,
                        out error);
                });
        }

        internal static bool TryRsaDecrypt(
            SafeSecKeyRefHandle privateKey,
            ReadOnlySpan<byte> source,
            Span<byte> destination,
            RSAEncryptionPadding padding,
            out int bytesWritten)
        {
            Debug.Assert(padding.Mode == RSAEncryptionPaddingMode.Pkcs1 || padding.Mode == RSAEncryptionPaddingMode.Oaep);
            return TryExecuteTransform(
                source,
                destination,
                out bytesWritten,
                delegate (ReadOnlySpan<byte> innerSource, out SafeCFDataHandle outputHandle, out SafeCFErrorHandle errorHandle)
                {
                    return padding.Mode == RSAEncryptionPaddingMode.Pkcs1 ?
                        RsaDecryptPkcs(privateKey, innerSource, innerSource.Length, out outputHandle, out errorHandle) :
                        RsaDecryptOaep(privateKey, innerSource, innerSource.Length, PalAlgorithmFromAlgorithmName(padding.OaepHashAlgorithm), out outputHandle, out errorHandle);
                });
        }

        private static bool ProcessPrimitiveResponse(
            int returnValue,
            SafeCFDataHandle cfData,
            SafeCFErrorHandle cfError,
            Span<byte> destination,
            out int bytesWritten)
        {
            if (returnValue == kErrorSeeError)
            {
                throw CreateExceptionForCFError(cfError);
            }

            if (returnValue == kSuccess && !cfData.IsInvalid)
            {
                return CoreFoundation.TryCFWriteData(cfData, destination, out bytesWritten);
            }

            Debug.Fail($"Unknown return value ({returnValue}) or no data object returned");
            throw new CryptographicException();
        }

        internal static bool TryRsaDecryptionPrimitive(
            SafeSecKeyRefHandle privateKey,
            ReadOnlySpan<byte> source,
            Span<byte> destination,
            out int bytesWritten)
        {
            int returnValue = AppleCryptoNative_RsaDecryptionPrimitive(
                privateKey,
                ref MemoryMarshal.GetReference(source),
                source.Length,
                out SafeCFDataHandle cfData,
                out SafeCFErrorHandle cfError);

            return ProcessPrimitiveResponse(returnValue, cfData, cfError, destination, out bytesWritten);
        }

        internal static bool TryRsaEncryptionPrimitive(
            SafeSecKeyRefHandle publicKey,
            ReadOnlySpan<byte> source,
            Span<byte> destination,
            out int bytesWritten)
        {
            int returnValue = AppleCryptoNative_RsaEncryptionPrimitive(
                publicKey,
                ref MemoryMarshal.GetReference(source),
                source.Length,
                out SafeCFDataHandle cfData,
                out SafeCFErrorHandle cfError);

            return ProcessPrimitiveResponse(returnValue, cfData, cfError, destination, out bytesWritten);
        }

        internal static bool TryRsaSignaturePrimitive(
            SafeSecKeyRefHandle privateKey,
            ReadOnlySpan<byte> source,
            Span<byte> destination,
            out int bytesWritten)
        {
            int returnValue = AppleCryptoNative_RsaSignaturePrimitive(
                privateKey,
                ref MemoryMarshal.GetReference(source),
                source.Length,
                out SafeCFDataHandle cfData,
                out SafeCFErrorHandle cfError);

            return ProcessPrimitiveResponse(returnValue, cfData, cfError, destination, out bytesWritten);
        }

        internal static bool TryRsaVerificationPrimitive(
            SafeSecKeyRefHandle publicKey,
            ReadOnlySpan<byte> source,
            Span<byte> destination,
            out int bytesWritten)
        {
            int returnValue = AppleCryptoNative_RsaVerificationPrimitive(
                publicKey,
                ref MemoryMarshal.GetReference(source),
                source.Length,
                out SafeCFDataHandle cfData,
                out SafeCFErrorHandle cfError);

            return ProcessPrimitiveResponse(returnValue, cfData, cfError, destination, out bytesWritten);
        }

        private static PAL_HashAlgorithm PalAlgorithmFromAlgorithmName(HashAlgorithmName hashAlgorithmName) =>
            hashAlgorithmName == HashAlgorithmName.MD5 ? PAL_HashAlgorithm.Md5 :
            hashAlgorithmName == HashAlgorithmName.SHA1 ? PAL_HashAlgorithm.Sha1 :
            hashAlgorithmName == HashAlgorithmName.SHA256 ? PAL_HashAlgorithm.Sha256 :
            hashAlgorithmName == HashAlgorithmName.SHA384 ? PAL_HashAlgorithm.Sha384 :
            hashAlgorithmName == HashAlgorithmName.SHA512 ? PAL_HashAlgorithm.Sha512 :
            throw new CryptographicException(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithmName.Name);
    }
}
