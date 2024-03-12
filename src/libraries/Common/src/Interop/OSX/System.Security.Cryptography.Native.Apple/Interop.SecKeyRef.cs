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

        internal enum PAL_KeyAlgorithm : uint
        {
            Unknown = 0,
            EC = 1,
            RSA = 2,
        }

        [LibraryImport(Libraries.AppleCryptoNative)]
        private static partial ulong AppleCryptoNative_SecKeyGetSimpleKeySizeInBytes(SafeSecKeyRefHandle publicKey);

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

        internal static unsafe SafeSecKeyRefHandle CreateDataKey(
            ReadOnlySpan<byte> keyData,
            PAL_KeyAlgorithm keyAlgorithm,
            bool isPublic)
        {
            fixed (byte* pKey = keyData)
            {
                int result = AppleCryptoNative_SecKeyCreateWithData(
                    pKey,
                    keyData.Length,
                    keyAlgorithm,
                    isPublic ? 1 : 0,
                    out SafeSecKeyRefHandle dataKey,
                    out SafeCFErrorHandle errorHandle);

                using (errorHandle)
                {
                    switch (result)
                    {
                        case kSuccess:
                            return dataKey;
                        case kErrorSeeError:
                            throw CreateExceptionForCFError(errorHandle);
                        default:
                            Debug.Fail($"SecKeyCreateWithData returned {result}");
                            throw new CryptographicException();
                    }
                }
            }
        }

        internal static bool TrySecKeyCopyExternalRepresentation(
            SafeSecKeyRefHandle key,
            out byte[] externalRepresentation)
        {
            const int errSecPassphraseRequired = -25260;

            int result = AppleCryptoNative_SecKeyCopyExternalRepresentation(
                key,
                out SafeCFDataHandle data,
                out SafeCFErrorHandle errorHandle);

            using (errorHandle)
            using (data)
            {
                switch (result)
                {
                    case kSuccess:
                        externalRepresentation = CoreFoundation.CFGetData(data);
                        return true;
                    case kErrorSeeError:
                        if (Interop.CoreFoundation.GetErrorCode(errorHandle) == errSecPassphraseRequired)
                        {
                            externalRepresentation = Array.Empty<byte>();
                            return false;
                        }
                        throw CreateExceptionForCFError(errorHandle);
                    default:
                        Debug.Fail($"SecKeyCopyExternalRepresentation returned {result}");
                        throw new CryptographicException();
                }
            }
        }

        [LibraryImport(Libraries.AppleCryptoNative)]
        private static unsafe partial int AppleCryptoNative_SecKeyCreateWithData(
            byte* pKey,
            int cbKey,
            PAL_KeyAlgorithm keyAlgorithm,
            int isPublic,
            out SafeSecKeyRefHandle pDataKey,
            out SafeCFErrorHandle pErrorOut);

        [LibraryImport(Libraries.AppleCryptoNative)]
        private static unsafe partial int AppleCryptoNative_SecKeyCopyExternalRepresentation(
            SafeSecKeyRefHandle key,
            out SafeCFDataHandle pDataOut,
            out SafeCFErrorHandle pErrorOut);

        [LibraryImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_SecKeyCopyPublicKey")]
        internal static unsafe partial SafeSecKeyRefHandle CopyPublicKey(SafeSecKeyRefHandle privateKey);
    }
}

namespace System.Security.Cryptography.Apple
{
    internal sealed class SafeSecKeyRefHandle : SafeHandle
    {
        private SafeHandle? _parentHandle;

        public SafeSecKeyRefHandle()
            : base(IntPtr.Zero, ownsHandle: true)
        {
        }

        internal void SetParentHandle(SafeHandle parentHandle)
        {
            Debug.Assert(_parentHandle is null);

            bool added = false;
            parentHandle.DangerousAddRef(ref added);
            _parentHandle = parentHandle;

            // If we became invalid while the parent handle was being incremented, release the parent handle since
            // ReleaseHandle will not get called.
            if (IsInvalid)
            {
                _parentHandle.DangerousRelease();
                _parentHandle = null;
            }
        }

        protected override bool ReleaseHandle()
        {
            Interop.CoreFoundation.CFRelease(handle);
            SetHandle(IntPtr.Zero);

            _parentHandle?.DangerousRelease();
            _parentHandle = null;

            return true;
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

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
