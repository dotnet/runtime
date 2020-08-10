// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;
using Internal.NativeCrypto;

namespace Internal.Cryptography
{
    internal static class AesBCryptModes
    {
        private static readonly SafeAlgorithmHandle s_hAlgCbc = OpenAesAlgorithm(Cng.BCRYPT_CHAIN_MODE_CBC);
        private static readonly SafeAlgorithmHandle s_hAlgEcb = OpenAesAlgorithm(Cng.BCRYPT_CHAIN_MODE_ECB);
        private static readonly SafeAlgorithmHandle s_hAlgCfb128 = OpenAesAlgorithm(Cng.BCRYPT_CHAIN_MODE_CFB, 16);
        private static readonly SafeAlgorithmHandle s_hAlgCfb8 = OpenAesAlgorithm(Cng.BCRYPT_CHAIN_MODE_CFB, 1);

        internal static SafeAlgorithmHandle GetSharedHandle(CipherMode cipherMode, int feedback) =>
            // Windows 8 added support to set the CipherMode value on a key,
            // but Windows 7 requires that it be set on the algorithm before key creation.
            (cipherMode, feedback) switch
            {
                (CipherMode.CBC, _) => s_hAlgCbc,
                (CipherMode.ECB, _) => s_hAlgEcb,
                (CipherMode.CFB, 16) => s_hAlgCfb128,
                (CipherMode.CFB, 1) => s_hAlgCfb8,
                _ => throw new NotSupportedException(),
            };

        internal static SafeAlgorithmHandle OpenAesAlgorithm(string cipherMode, int feedback = 0)
        {
            SafeAlgorithmHandle hAlg = Cng.BCryptOpenAlgorithmProvider(Cng.BCRYPT_AES_ALGORITHM, null, Cng.OpenAlgorithmProviderFlags.NONE);
            hAlg.SetCipherMode(cipherMode);

            // feedback is in bytes!
            if (feedback > 0)
            {
                hAlg.SetFeedbackSize(feedback);
            }

            return hAlg;
        }
    }
}
