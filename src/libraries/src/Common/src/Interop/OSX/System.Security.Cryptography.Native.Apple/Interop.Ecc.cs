// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Apple;

internal static partial class Interop
{
    internal static partial class AppleCrypto
    {
        [DllImport(Libraries.AppleCryptoNative)]
        private static extern int AppleCryptoNative_EccGenerateKey(
            int keySizeInBits,
            SafeKeychainHandle tempKeychain,
            out SafeSecKeyRefHandle pPublicKey,
            out SafeSecKeyRefHandle pPrivateKey,
            out int pOSStatus);

        [DllImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_EccGetKeySizeInBits")]
        internal static extern long EccGetKeySizeInBits(SafeSecKeyRefHandle publicKey);

        internal static void EccGenerateKey(
            int keySizeInBits,
            out SafeSecKeyRefHandle pPublicKey,
            out SafeSecKeyRefHandle pPrivateKey)
        {
            using (SafeTemporaryKeychainHandle tempKeychain = CreateTemporaryKeychain())
            {
                SafeSecKeyRefHandle keychainPublic;
                SafeSecKeyRefHandle keychainPrivate;
                int osStatus;

                int result = AppleCryptoNative_EccGenerateKey(
                    keySizeInBits,
                    tempKeychain,
                    out keychainPublic,
                    out keychainPrivate,
                    out osStatus);

                if (result == 1)
                {
                    pPublicKey = keychainPublic;
                    pPrivateKey = keychainPrivate;
                    return;
                }

                using (keychainPrivate)
                using (keychainPublic)
                {
                    if (result == 0)
                    {
                        throw CreateExceptionForOSStatus(osStatus);
                    }

                    Debug.Fail($"Unexpected result from AppleCryptoNative_EccGenerateKey: {result}");
                    throw new CryptographicException();
                }
            }
        }
    }
}
