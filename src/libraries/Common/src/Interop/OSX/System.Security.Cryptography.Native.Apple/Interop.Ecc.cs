// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        private static extern int AppleCryptoNative_EccGenerateKey(
            int keySizeInBits,
            out SafeSecKeyRefHandle pPublicKey,
            out SafeSecKeyRefHandle pPrivateKey,
            out SafeCFErrorHandle pErrorOut);

        [DllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_EccGetKeySizeInBits")]
        internal static extern long EccGetKeySizeInBits(SafeSecKeyRefHandle publicKey);

        internal static void EccGenerateKey(
            int keySizeInBits,
            out SafeSecKeyRefHandle pPublicKey,
            out SafeSecKeyRefHandle pPrivateKey)
        {
            SafeSecKeyRefHandle keychainPublic;
            SafeSecKeyRefHandle keychainPrivate;
            SafeCFErrorHandle error;

            int result = AppleCryptoNative_EccGenerateKey(
                keySizeInBits,
                out keychainPublic,
                out keychainPrivate,
                out error);

            using (error)
            {
                if (result == kSuccess)
                {
                    pPublicKey = keychainPublic;
                    pPrivateKey = keychainPrivate;
                    return;
                }

                using (keychainPrivate)
                using (keychainPublic)
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
