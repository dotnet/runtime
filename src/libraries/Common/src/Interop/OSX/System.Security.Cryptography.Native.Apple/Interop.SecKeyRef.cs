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
        private const int kSuccess = 1;
        private const int kErrorSeeError = -2;
        private const int kPlatformNotSupported = -5;

        private static int AppleCryptoNative_SecKeyImportEphemeral(
            ReadOnlySpan<byte> pbKeyBlob,
            int isPrivateKey,
            out SafeSecKeyRefHandle ppKeyOut,
            out int pOSStatus) =>
            AppleCryptoNative_SecKeyImportEphemeral(
                ref MemoryMarshal.GetReference(pbKeyBlob),
                pbKeyBlob.Length,
                isPrivateKey,
                out ppKeyOut,
                out pOSStatus);

        [DllImport(Libraries.AppleCryptoNative)]
        private static extern int AppleCryptoNative_SecKeyImportEphemeral(
            ref byte pbKeyBlob,
            int cbKeyBlob,
            int isPrivateKey,
            out SafeSecKeyRefHandle ppKeyOut,
            out int pOSStatus);

        [DllImport(Libraries.AppleCryptoNative)]
        private static extern ulong AppleCryptoNative_SecKeyGetSimpleKeySizeInBytes(SafeSecKeyRefHandle publicKey);

        private delegate int SecKeyTransform(ReadOnlySpan<byte> source, out SafeCFDataHandle outputHandle, out SafeCFErrorHandle errorHandle);

        private static byte[] ExecuteTransform(ReadOnlySpan<byte> source, SecKeyTransform transform)
        {
            SafeCFDataHandle data;
            SafeCFErrorHandle error;

            int ret = transform(source, out data, out error);

            using (error)
            using (data)
            {
                if (ret == kSuccess)
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
                switch (ret)
                {
                    case kSuccess:
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

        internal static SafeSecKeyRefHandle ImportEphemeralKey(ReadOnlySpan<byte> keyBlob, bool hasPrivateKey)
        {
            Debug.Assert(keyBlob != null);

            SafeSecKeyRefHandle keyHandle;
            int osStatus;

            int ret = AppleCryptoNative_SecKeyImportEphemeral(
                keyBlob,
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
