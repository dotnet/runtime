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

        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        internal static SafeSecKeyRefHandle ImportEphemeralKey(ReadOnlySpan<byte> keyBlob, bool hasPrivateKey)
        {
            throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        internal static byte[] GenerateSignature(SafeSecKeyRefHandle privateKey, ReadOnlySpan<byte> dataHash)
        {
            throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        internal static byte[] GenerateSignature(
            SafeSecKeyRefHandle privateKey,
            ReadOnlySpan<byte> dataHash,
            PAL_HashAlgorithm hashAlgorithm)
        {
            throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        internal static bool TryGenerateSignature(
            SafeSecKeyRefHandle privateKey,
            ReadOnlySpan<byte> source,
            Span<byte> destination,
            PAL_HashAlgorithm hashAlgorithm,
            out int bytesWritten)
        {
            throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        internal static bool VerifySignature(
            SafeSecKeyRefHandle publicKey,
            ReadOnlySpan<byte> dataHash,
            ReadOnlySpan<byte> signature)
        {
            throw new PlatformNotSupportedException();
        }

        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        internal static bool VerifySignature(
            SafeSecKeyRefHandle publicKey,
            ReadOnlySpan<byte> dataHash,
            ReadOnlySpan<byte> signature,
            PAL_HashAlgorithm hashAlgorithm)
        {
            throw new PlatformNotSupportedException();
        }
    }
}

namespace System.Security.Cryptography.Apple
{
    internal sealed class SafeSecKeyRefHandle : SafeKeychainItemHandle
    {
        protected override void Dispose(bool disposing)
        {
            if (disposing && SafeHandleCache<SafeSecKeyRefHandle>.IsCachedInvalidHandle(this))
            {
                return;
            }

            base.Dispose(disposing);
        }

        public static SafeSecKeyRefHandle InvalidHandle =>
            SafeHandleCache<SafeSecKeyRefHandle>.GetInvalidHandle(
                () => new SafeSecKeyRefHandle());
    }
}
