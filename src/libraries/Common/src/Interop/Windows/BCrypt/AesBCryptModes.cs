// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;
using Internal.NativeCrypto;

namespace Internal.Cryptography
{
    internal static class AesBCryptModes
    {
        private static readonly Lazy<SafeAlgorithmHandle> s_hAlgCbc = OpenAesAlgorithm(Cng.BCRYPT_CHAIN_MODE_CBC);
        private static readonly Lazy<SafeAlgorithmHandle> s_hAlgEcb = OpenAesAlgorithm(Cng.BCRYPT_CHAIN_MODE_ECB);
        private static readonly Lazy<SafeAlgorithmHandle> s_hAlgCfb128 = OpenAesAlgorithm(Cng.BCRYPT_CHAIN_MODE_CFB, 16);
        private static readonly Lazy<SafeAlgorithmHandle> s_hAlgCfb8 = OpenAesAlgorithm(Cng.BCRYPT_CHAIN_MODE_CFB, 1);

        internal static SafeAlgorithmHandle GetSharedHandle(CipherMode cipherMode, int feedback) =>
            // Windows 8 added support to set the CipherMode value on a key,
            // but Windows 7 requires that it be set on the algorithm before key creation.
            (cipherMode, feedback) switch
            {
                (CipherMode.CBC, _) => s_hAlgCbc.Value,
                (CipherMode.ECB, _) => s_hAlgEcb.Value,
                (CipherMode.CFB, 16) => s_hAlgCfb128.Value,
                (CipherMode.CFB, 1) => s_hAlgCfb8.Value,
                _ => throw new NotSupportedException(),
            };

        internal static Lazy<SafeAlgorithmHandle> OpenAesAlgorithm(string cipherMode, int feedback = 0)
        {
            return new Lazy<SafeAlgorithmHandle>(() =>
            {
                SafeAlgorithmHandle hAlg = Cng.BCryptOpenAlgorithmProvider(Cng.BCRYPT_AES_ALGORITHM, null,
                    Cng.OpenAlgorithmProviderFlags.NONE);
                hAlg.SetCipherMode(cipherMode);

                // feedback is in bytes!
                // The default feedback size is 1 (CFB8) on Windows. Do not set the CNG property
                // if we would be setting it to the default. Windows 7 only supports CFB8 and
                // does not permit setting the feedback size, so we don't call the property
                // setter at all in that case.
                if (feedback > 0 && feedback != 1)
                {
                    try
                    {
                        hAlg.SetFeedbackSize(feedback);
                    }
                    catch (CryptographicException ex)
                    {
                        throw new CryptographicException(SR.Cryptography_FeedbackSizeNotSupported, ex);
                    }
                }

                return hAlg;
            });
        }
    }
}
