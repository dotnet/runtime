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
        internal enum PAL_KeyAlgorithm : uint
        {
            Unknown = 0,
            EC = 1,
            RSA = 2,
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

        internal static byte[] SecKeyCopyExternalRepresentation(
            SafeSecKeyRefHandle key)
        {
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
                        return CoreFoundation.CFGetData(data);
                    case kErrorSeeError:
                        throw CreateExceptionForCFError(errorHandle);
                    default:
                        Debug.Fail($"SecKeyCopyExternalRepresentation returned {result}");
                        throw new CryptographicException();
                }
            }
        }

        [DllImport(Libraries.AppleCryptoNative)]
        private static unsafe extern int AppleCryptoNative_SecKeyCreateWithData(
            byte* pKey,
            int cbKey,
            PAL_KeyAlgorithm keyAlgorithm,
            int isPublic,
            out SafeSecKeyRefHandle pDataKey,
            out SafeCFErrorHandle pErrorOut);

        [DllImport(Libraries.AppleCryptoNative)]
        private static unsafe extern int AppleCryptoNative_SecKeyCopyExternalRepresentation(
            SafeSecKeyRefHandle key,
            out SafeCFDataHandle pDataOut,
            out SafeCFErrorHandle pErrorOut);

        [DllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_SecKeyCopyPublicKey")]
        internal static unsafe extern SafeSecKeyRefHandle CopyPublicKey(SafeSecKeyRefHandle privateKey);
    }
}
