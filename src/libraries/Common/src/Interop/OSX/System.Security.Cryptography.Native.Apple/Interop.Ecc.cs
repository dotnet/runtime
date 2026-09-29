// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Swift;
using System.Security.Cryptography;
using System.Security.Cryptography.Apple;
using Microsoft.Win32.SafeHandles;
using Swift.Runtime;

#pragma warning disable CS3016 // Arrays as attribute arguments are not CLS Compliant

internal static partial class Interop
{
    internal static partial class AppleCrypto
    {
        [LibraryImport(Libraries.AppleCryptoNative)]
        private static partial int AppleCryptoNative_EccGenerateKey(
            int keySizeInBits,
            out SafeSecKeyRefHandle pPublicKey,
            out SafeSecKeyRefHandle pPrivateKey,
            out SafeCFErrorHandle pErrorOut);

        [LibraryImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_EccGetKeySizeInBits")]
        internal static partial int EccGetKeySizeInBits(SafeSecKeyRefHandle publicKey);

        [LibraryImport(Libraries.AppleCryptoNative)]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        private static unsafe partial int AppleCryptoNative_EccExportPublicKeyFromPrivateKey(
            int keySizeInBits,
            UnsafeBufferPointer<byte> privateKey,
            UnsafeMutableBufferPointer<byte> destination);

        internal static void EccExportPublicKeyFromPrivateKey(
            int keySizeInBits,
            ReadOnlySpan<byte> privateKey,
            Span<byte> destination)
        {
            Debug.Assert(!privateKey.IsEmpty);
            Debug.Assert(!destination.IsEmpty);

            const int Success = 1;
            const int InvalidKey = 0;

            int result;

            unsafe
            {
                fixed (byte* privateKeyPtr = privateKey)
                fixed (byte* destinationPtr = destination)
                {
                    result = AppleCryptoNative_EccExportPublicKeyFromPrivateKey(
                        keySizeInBits,
                        new UnsafeBufferPointer<byte>(privateKeyPtr, privateKey.Length),
                        new UnsafeMutableBufferPointer<byte>(destinationPtr, destination.Length));
                }
            }

            switch (result)
            {
                case Success:
                    return;
                case InvalidKey:
                    throw new CryptographicException(SR.Cryptography_NotValidPublicOrPrivateKey);
                default:
                    Debug.Fail(
                        $"Unexpected result from {nameof(AppleCryptoNative_EccExportPublicKeyFromPrivateKey)}: {result}");
                    throw new CryptographicException();
            }
        }

        internal static void EccGenerateKey(
            int keySizeInBits,
            out SafeSecKeyRefHandle pPublicKey,
            out SafeSecKeyRefHandle pPrivateKey)
        {
            SafeSecKeyRefHandle publicKey;
            SafeSecKeyRefHandle privateKey;
            SafeCFErrorHandle error;

            int result = AppleCryptoNative_EccGenerateKey(
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

                    Debug.Fail($"Unexpected result from AppleCryptoNative_EccGenerateKey: {result}");
                    throw new CryptographicException();
                }
            }
        }
    }
}
