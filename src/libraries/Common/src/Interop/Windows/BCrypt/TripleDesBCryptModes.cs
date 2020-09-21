// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;
using Internal.NativeCrypto;

namespace Internal.Cryptography
{
    internal static class TripleDesBCryptModes
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350", Justification = "We are providing the implementation for TripleDES, not consuming it")]
        private static readonly Lazy<SafeAlgorithmHandle> s_hAlgCbc = Open3DesAlgorithm(Cng.BCRYPT_CHAIN_MODE_CBC);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350", Justification = "We are providing the implementation for TripleDES, not consuming it")]
        private static readonly Lazy<SafeAlgorithmHandle> s_hAlgEcb = Open3DesAlgorithm(Cng.BCRYPT_CHAIN_MODE_ECB);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350", Justification = "We are providing the implementation for TripleDES, not consuming it")]
        private static readonly Lazy<SafeAlgorithmHandle> s_hAlgCfb8 = Open3DesAlgorithm(Cng.BCRYPT_CHAIN_MODE_CFB, 1);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350", Justification = "We are providing the implementation for TripleDES, not consuming it")]
        private static readonly Lazy<SafeAlgorithmHandle> s_hAlgCfb64 = Open3DesAlgorithm(Cng.BCRYPT_CHAIN_MODE_CFB, 8);

        internal static SafeAlgorithmHandle GetSharedHandle(CipherMode cipherMode, int feedback) =>
            // Windows 8 added support to set the CipherMode value on a key,
            // but Windows 7 requires that it be set on the algorithm before key creation.
            (cipherMode, feedback) switch
            {
                (CipherMode.CBC, _) => s_hAlgCbc.Value,
                (CipherMode.ECB, _) => s_hAlgEcb.Value,
                (CipherMode.CFB, 1) => s_hAlgCfb8.Value,
                (CipherMode.CFB, 8) => s_hAlgCfb64.Value,
                _ => throw new NotSupportedException(),
            };

        private static Lazy<SafeAlgorithmHandle> Open3DesAlgorithm(string cipherMode, int feedback = 0)
        {
            return new Lazy<SafeAlgorithmHandle>(() =>
            {
                SafeAlgorithmHandle hAlg = Cng.BCryptOpenAlgorithmProvider(Cng.BCRYPT_3DES_ALGORITHM, null,
                    Cng.OpenAlgorithmProviderFlags.NONE);
                hAlg.SetCipherMode(cipherMode);

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
