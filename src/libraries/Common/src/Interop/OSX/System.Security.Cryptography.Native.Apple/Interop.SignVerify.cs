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
        internal enum PAL_SignatureAlgorithm : uint
        {
            Unknown = 0,
            DSA = 1,
            RsaPkcs1 = 2,
            RsaPss = 3,
            RsaRaw = 4,
            EC = 5,
        }

        private static unsafe int AppleCryptoNative_SecKeyVerifySignature(
            SafeSecKeyRefHandle publicKey,
            ReadOnlySpan<byte> dataHash,
            ReadOnlySpan<byte> signature,
            PAL_HashAlgorithm hashAlgorithm,
            PAL_SignatureAlgorithm signatureAlgorithm,
            out SafeCFErrorHandle pErrorOut)
        {
            fixed (byte* pDataHash = dataHash)
            fixed (byte* pSignature = signature)
            {
                return AppleCryptoNative_SecKeyVerifySignature(
                    publicKey,
                    pDataHash,
                    dataHash.Length,
                    pSignature,
                    signature.Length,
                    hashAlgorithm,
                    signatureAlgorithm,
                    out pErrorOut);
            }
        }

        [LibraryImport(Libraries.AppleCryptoNative)]
        private static unsafe partial int AppleCryptoNative_SecKeyVerifySignature(
            SafeSecKeyRefHandle publicKey,
            byte* pbDataHash,
            int cbDataHash,
            byte* pbSignature,
            int cbSignature,
            PAL_HashAlgorithm hashAlgorithm,
            PAL_SignatureAlgorithm signatureAlgorithm,
            out SafeCFErrorHandle pErrorOut);

        private static unsafe int AppleCryptoNative_SecKeyCreateSignature(
            SafeSecKeyRefHandle privateKey,
            ReadOnlySpan<byte> dataHash,
            PAL_HashAlgorithm hashAlgorithm,
            PAL_SignatureAlgorithm signatureAlgorithm,
            out SafeCFDataHandle pSignatureOut,
            out SafeCFErrorHandle pErrorOut)
        {
            fixed (byte* pDataHash = dataHash)
            {
                return AppleCryptoNative_SecKeyCreateSignature(
                    privateKey,
                    pDataHash,
                    dataHash.Length,
                    hashAlgorithm,
                    signatureAlgorithm,
                    out pSignatureOut,
                    out pErrorOut);
            }
        }

        [LibraryImport(Libraries.AppleCryptoNative)]
        private static unsafe partial int AppleCryptoNative_SecKeyCreateSignature(
            SafeSecKeyRefHandle privateKey,
            byte* pbDataHash,
            int cbDataHash,
            PAL_HashAlgorithm hashAlgorithm,
            PAL_SignatureAlgorithm signatureAlgorithm,
            out SafeCFDataHandle pSignatureOut,
            out SafeCFErrorHandle pErrorOut);

        internal static bool VerifySignature(
            SafeSecKeyRefHandle publicKey,
            ReadOnlySpan<byte> dataHash,
            ReadOnlySpan<byte> signature,
            PAL_HashAlgorithm hashAlgorithm,
            PAL_SignatureAlgorithm signatureAlgorithm)
        {
            const int Valid = 1;
            const int Invalid = 0;

            int result = AppleCryptoNative_SecKeyVerifySignature(
                publicKey,
                dataHash,
                signature,
                hashAlgorithm,
                signatureAlgorithm,
                out SafeCFErrorHandle errorHandle);

            using (errorHandle)
            {
                switch (result)
                {
                    case Valid:
                        return true;
                    case Invalid:
                        return false;
                    case kErrorSeeError:
                        throw CreateExceptionForCFError(errorHandle);
                    case kPlatformNotSupported:
                        throw new PlatformNotSupportedException();
                    default:
                        Debug.Fail($"verify signature returned {result}");
                        throw new CryptographicException();
                }
            }
        }

        private static SafeCFDataHandle NativeCreateSignature(
            SafeSecKeyRefHandle privateKey,
            ReadOnlySpan<byte> dataHash,
            PAL_HashAlgorithm hashAlgorithm,
            PAL_SignatureAlgorithm signatureAlgorithm)
        {
            int result = AppleCryptoNative_SecKeyCreateSignature(
                privateKey,
                dataHash,
                hashAlgorithm,
                signatureAlgorithm,
                out SafeCFDataHandle signature,
                out SafeCFErrorHandle errorHandle);

            using (errorHandle)
            {
                switch (result)
                {
                    case kSuccess:
                        return signature;
                    case kErrorSeeError:
                        throw CreateExceptionForCFError(errorHandle);
                    case kPlatformNotSupported:
                        throw new PlatformNotSupportedException();
                    default:
                        Debug.Fail($"create signature returned {result}");
                        throw new CryptographicException();
                }
            }
        }

        internal static byte[] CreateSignature(
            SafeSecKeyRefHandle privateKey,
            ReadOnlySpan<byte> dataHash,
            PAL_HashAlgorithm hashAlgorithm,
            PAL_SignatureAlgorithm signatureAlgorithm)
        {
            using (SafeCFDataHandle signature = NativeCreateSignature(privateKey, dataHash, hashAlgorithm, signatureAlgorithm))
            {
                return CoreFoundation.CFGetData(signature);
            }
        }

        internal static bool TryCreateSignature(
            SafeSecKeyRefHandle privateKey,
            ReadOnlySpan<byte> dataHash,
            Span<byte> destination,
            PAL_HashAlgorithm hashAlgorithm,
            PAL_SignatureAlgorithm signatureAlgorithm,
            out int bytesWritten)
        {
            using (SafeCFDataHandle signature = NativeCreateSignature(privateKey, dataHash, hashAlgorithm, signatureAlgorithm))
            {
                return CoreFoundation.TryCFWriteData(signature, destination, out bytesWritten);
            }
        }
    }
}
