// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Security.Cryptography.Apple;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class AppleCrypto
    {
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

        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        internal static void RsaGenerateKey(
            int keySizeInBits,
            out SafeSecKeyRefHandle pPublicKey,
            out SafeSecKeyRefHandle pPrivateKey)
        {
            throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        internal static byte[] RsaEncrypt(
            SafeSecKeyRefHandle publicKey,
            byte[] data,
            RSAEncryptionPadding padding)
        {
            throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        internal static bool TryRsaEncrypt(
            SafeSecKeyRefHandle publicKey,
            ReadOnlySpan<byte> source,
            Span<byte> destination,
            RSAEncryptionPadding padding,
            out int bytesWritten)
        {
            throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        internal static byte[] RsaDecrypt(
            SafeSecKeyRefHandle privateKey,
            byte[] data,
            RSAEncryptionPadding padding)
        {
            throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        internal static bool TryRsaDecrypt(
            SafeSecKeyRefHandle privateKey,
            ReadOnlySpan<byte> source,
            Span<byte> destination,
            RSAEncryptionPadding padding,
            out int bytesWritten)
        {
            throw new PlatformNotSupportedException();
        }

        private static bool ProcessPrimitiveResponse(
            int returnValue,
            SafeCFDataHandle cfData,
            SafeCFErrorHandle cfError,
            Span<byte> destination,
            out int bytesWritten)
        {
            const int kErrorSeeError = -2;
            const int kSuccess = 1;

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
