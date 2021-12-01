// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;
using Internal.NativeCrypto;

namespace Internal.Cryptography
{
    internal static class DesBCryptModes
    {
        private static readonly Lazy<SafeAlgorithmHandle> s_hAlgCbc = OpenDesAlgorithm(Cng.BCRYPT_CHAIN_MODE_CBC);
        private static readonly Lazy<SafeAlgorithmHandle> s_hAlgEcb = OpenDesAlgorithm(Cng.BCRYPT_CHAIN_MODE_ECB);
        private static readonly Lazy<SafeAlgorithmHandle> s_hAlgCfb8 = OpenDesAlgorithm(Cng.BCRYPT_CHAIN_MODE_CFB);

        internal static SafeAlgorithmHandle GetSharedHandle(CipherMode cipherMode, int feedback) =>
            // Windows 8 added support to set the CipherMode value on a key,
            // but Windows 7 requires that it be set on the algorithm before key creation.
            (cipherMode, feedback) switch
            {
                (CipherMode.CBC, _) => s_hAlgCbc.Value,
                (CipherMode.ECB, _) => s_hAlgEcb.Value,
                (CipherMode.CFB, 1) => s_hAlgCfb8.Value,
                _ => throw new NotSupportedException(),
            };

        private static Lazy<SafeAlgorithmHandle> OpenDesAlgorithm(string cipherMode)
        {
            return new Lazy<SafeAlgorithmHandle>(() =>
            {
                SafeAlgorithmHandle hAlg = Cng.BCryptOpenAlgorithmProvider(
                    Cng.BCRYPT_DES_ALGORITHM,
                    null,
                    Cng.OpenAlgorithmProviderFlags.NONE);
                hAlg.SetCipherMode(cipherMode);

                return hAlg;
            });
        }
    }
}
