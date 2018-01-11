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
        [DllImport(Libraries.AppleCryptoNative)]
        private static extern int AppleCryptoNative_SecKeyImportEphemeral(
            byte[] pbKeyBlob,
            int cbKeyBlob,
            int isPrivateKey,
            out SafeSecKeyRefHandle ppKeyOut,
            out int pOSStatus);

        private static int AppleCryptoNative_GenerateSignature(
            SafeSecKeyRefHandle privateKey,
            ReadOnlySpan<byte> pbDataHash,
            int cbDataHash,
            out SafeCFDataHandle pSignatureOut,
            out SafeCFErrorHandle pErrorOut) =>
            AppleCryptoNative_GenerateSignature(
                privateKey, ref MemoryMarshal.GetReference(pbDataHash), cbDataHash, out pSignatureOut, out pErrorOut);

        [DllImport(Libraries.AppleCryptoNative)]
        private static extern int AppleCryptoNative_GenerateSignature(
            SafeSecKeyRefHandle privateKey,
            ref byte pbDataHash,
            int cbDataHash,
            out SafeCFDataHandle pSignatureOut,
            out SafeCFErrorHandle pErrorOut);

        private static int AppleCryptoNative_GenerateSignatureWithHashAlgorithm(
            SafeSecKeyRefHandle privateKey,
            ReadOnlySpan<byte> pbDataHash,
            int cbDataHash,
            PAL_HashAlgorithm hashAlgorithm,
            out SafeCFDataHandle pSignatureOut,
            out SafeCFErrorHandle pErrorOut) =>
            AppleCryptoNative_GenerateSignatureWithHashAlgorithm(
                privateKey, ref MemoryMarshal.GetReference(pbDataHash), cbDataHash, hashAlgorithm, out pSignatureOut, out pErrorOut);

        [DllImport(Libraries.AppleCryptoNative)]
        private static extern int AppleCryptoNative_GenerateSignatureWithHashAlgorithm(
            SafeSecKeyRefHandle privateKey,
            ref byte pbDataHash,
            int cbDataHash,
            PAL_HashAlgorithm hashAlgorithm,
            out SafeCFDataHandle pSignatureOut,
            out SafeCFErrorHandle pErrorOut);

        private static int AppleCryptoNative_VerifySignature(
            SafeSecKeyRefHandle publicKey,
            ReadOnlySpan<byte> pbDataHash,
            int cbDataHash,
            ReadOnlySpan<byte> pbSignature,
            int cbSignature,
            out SafeCFErrorHandle pErrorOut) =>
            AppleCryptoNative_VerifySignature(
                publicKey,
                ref MemoryMarshal.GetReference(pbDataHash),
                cbDataHash,
                ref MemoryMarshal.GetReference(pbSignature),
                cbSignature,
                out pErrorOut);

        [DllImport(Libraries.AppleCryptoNative)]
        private static extern int AppleCryptoNative_VerifySignature(
            SafeSecKeyRefHandle publicKey,
            ref byte pbDataHash,
            int cbDataHash,
            ref byte pbSignature,
            int cbSignature,
            out SafeCFErrorHandle pErrorOut);

        private static int AppleCryptoNative_VerifySignatureWithHashAlgorithm(
            SafeSecKeyRefHandle publicKey,
            ReadOnlySpan<byte> pbDataHash,
            int cbDataHash,
            ReadOnlySpan<byte> pbSignature,
            int cbSignature,
            PAL_HashAlgorithm hashAlgorithm,
            out SafeCFErrorHandle pErrorOut) =>
            AppleCryptoNative_VerifySignatureWithHashAlgorithm(
                publicKey,
                ref MemoryMarshal.GetReference(pbDataHash),
                cbDataHash,
                ref MemoryMarshal.GetReference(pbSignature),
                cbSignature,
                hashAlgorithm,
                out pErrorOut);

        [DllImport(Libraries.AppleCryptoNative)]
        private static extern int AppleCryptoNative_VerifySignatureWithHashAlgorithm(
            SafeSecKeyRefHandle publicKey,
            ref byte pbDataHash,
            int cbDataHash,
            ref byte pbSignature,
            int cbSignature,
            PAL_HashAlgorithm hashAlgorithm,
            out SafeCFErrorHandle pErrorOut);

        [DllImport(Libraries.AppleCryptoNative)]
        private static extern ulong AppleCryptoNative_SecKeyGetSimpleKeySizeInBytes(SafeSecKeyRefHandle publicKey);

        private delegate int SecKeyTransform(ReadOnlySpan<byte> source, out SafeCFDataHandle outputHandle, out SafeCFErrorHandle errorHandle);

        private static byte[] ExecuteTransform(ReadOnlySpan<byte> source, SecKeyTransform transform)
        {
            const int Success = 1;
            const int kErrorSeeError = -2;

            SafeCFDataHandle data;
            SafeCFErrorHandle error;

            int ret = transform(source, out data, out error);

            using (error)
            using (data)
            {
                if (ret == Success)
                {
                    return CoreFoundation.CFGetData(data);
                }

                if (ret == kErrorSeeError)
                {
                    throw CreateExceptionForCFError(error);
                }

                Debug.Fail($"transform returned {ret}");
                throw new CryptographicException();
            }
        }

        private static bool TryExecuteTransform(
            ReadOnlySpan<byte> source,
            Span<byte> destination,
            out int bytesWritten,
            SecKeyTransform transform)
        {
            SafeCFDataHandle outputHandle;
            SafeCFErrorHandle errorHandle;

            int ret = transform(source, out outputHandle, out errorHandle);

            using (errorHandle)
            using (outputHandle)
            {
                const int Success = 1;
                const int kErrorSeeError = -2;
                switch (ret)
                {
                    case Success:
                        return CoreFoundation.TryCFWriteData(outputHandle, destination, out bytesWritten);
                    case kErrorSeeError:
                        throw CreateExceptionForCFError(errorHandle);
                    default:
                        Debug.Fail($"transform returned {ret}");
                        throw new CryptographicException();
                }
            }
        }
        
        internal static int GetSimpleKeySizeInBits(SafeSecKeyRefHandle publicKey)
        {
            ulong keySizeInBytes = AppleCryptoNative_SecKeyGetSimpleKeySizeInBytes(publicKey);

            checked
            {
                return (int)(keySizeInBytes * 8);
            }
        }

        internal static SafeSecKeyRefHandle ImportEphemeralKey(byte[] keyBlob, bool hasPrivateKey)
        {
            Debug.Assert(keyBlob != null);

            SafeSecKeyRefHandle keyHandle;
            int osStatus;

            int ret = AppleCryptoNative_SecKeyImportEphemeral(
                keyBlob,
                keyBlob.Length,
                hasPrivateKey ? 1 : 0,
                out keyHandle,
                out osStatus);

            if (ret == 1 && !keyHandle.IsInvalid)
            {
                return keyHandle;
            }

            if (ret == 0)
            {
                throw CreateExceptionForOSStatus(osStatus);
            }

            Debug.Fail($"SecKeyImportEphemeral returned {ret}");
            throw new CryptographicException();
        }

        internal static byte[] GenerateSignature(SafeSecKeyRefHandle privateKey, ReadOnlySpan<byte> dataHash)
        {
            Debug.Assert(privateKey != null, "privateKey != null");

            return ExecuteTransform(
                dataHash,
                (ReadOnlySpan<byte> source, out SafeCFDataHandle signature, out SafeCFErrorHandle error) =>
                    AppleCryptoNative_GenerateSignature(
                        privateKey,
                        source,
                        source.Length,
                        out signature,
                        out error));
        }

        internal static byte[] GenerateSignature(
            SafeSecKeyRefHandle privateKey,
            ReadOnlySpan<byte> dataHash,
            PAL_HashAlgorithm hashAlgorithm)
        {
            Debug.Assert(privateKey != null, "privateKey != null");
            Debug.Assert(hashAlgorithm != PAL_HashAlgorithm.Unknown, "hashAlgorithm != PAL_HashAlgorithm.Unknown");

            return ExecuteTransform(
                dataHash,
                (ReadOnlySpan<byte> source, out SafeCFDataHandle signature, out SafeCFErrorHandle error) =>
                    AppleCryptoNative_GenerateSignatureWithHashAlgorithm(
                        privateKey,
                        source,
                        source.Length,
                        hashAlgorithm,
                        out signature,
                        out error));
        }

        internal static bool TryGenerateSignature(
            SafeSecKeyRefHandle privateKey,
            ReadOnlySpan<byte> source,
            Span<byte> destination,
            PAL_HashAlgorithm hashAlgorithm,
            out int bytesWritten)
        {
            Debug.Assert(privateKey != null, "privateKey != null");
            Debug.Assert(hashAlgorithm != PAL_HashAlgorithm.Unknown, "hashAlgorithm != PAL_HashAlgorithm.Unknown");

            return TryExecuteTransform(
                source,
                destination,
                out bytesWritten,
                delegate (ReadOnlySpan<byte> innerSource, out SafeCFDataHandle outputHandle, out SafeCFErrorHandle errorHandle)
                {
                    return AppleCryptoNative_GenerateSignatureWithHashAlgorithm(
                        privateKey, innerSource, innerSource.Length, hashAlgorithm, out outputHandle, out errorHandle);
                });
        }

        internal static bool VerifySignature(
            SafeSecKeyRefHandle publicKey,
            ReadOnlySpan<byte> dataHash,
            ReadOnlySpan<byte> signature)
        {
            Debug.Assert(publicKey != null, "publicKey != null");

            SafeCFErrorHandle error;

            int ret = AppleCryptoNative_VerifySignature(
                publicKey,
                dataHash,
                dataHash.Length,
                signature,
                signature.Length,
                out error);

            const int True = 1;
            const int False = 0;
            const int kErrorSeeError = -2;

            using (error)
            {
                switch (ret)
                {
                    case True:
                        return true;
                    case False:
                        return false;
                    case kErrorSeeError:
                        throw CreateExceptionForCFError(error);
                    default:
                        Debug.Fail($"VerifySignature returned {ret}");
                        throw new CryptographicException();
                }
            }
        }

        internal static bool VerifySignature(
            SafeSecKeyRefHandle publicKey,
            ReadOnlySpan<byte> dataHash,
            ReadOnlySpan<byte> signature,
            PAL_HashAlgorithm hashAlgorithm)
        {
            Debug.Assert(publicKey != null, "publicKey != null");
            Debug.Assert(hashAlgorithm != PAL_HashAlgorithm.Unknown);

            SafeCFErrorHandle error;

            int ret = AppleCryptoNative_VerifySignatureWithHashAlgorithm(
                publicKey,
                dataHash,
                dataHash.Length,
                signature,
                signature.Length,
                hashAlgorithm,
                out error);

            const int True = 1;
            const int False = 0;
            const int kErrorSeeError = -2;

            using (error)
            {
                switch (ret)
                {
                    case True:
                        return true;
                    case False:
                        return false;
                    case kErrorSeeError:
                        throw CreateExceptionForCFError(error);
                    default:
                        Debug.Fail($"VerifySignature returned {ret}");
                        throw new CryptographicException();
                }
            }
        }
    }
}

namespace System.Security.Cryptography.Apple
{
    internal sealed class SafeSecKeyRefHandle : SafeKeychainItemHandle
    {
    }
}
