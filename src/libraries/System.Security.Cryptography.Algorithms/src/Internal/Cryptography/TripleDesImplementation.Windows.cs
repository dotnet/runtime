// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.Cryptography;
using Internal.NativeCrypto;

namespace Internal.Cryptography
{
    partial class TripleDesImplementation
    {
        private static ICryptoTransform CreateTransformCore(
            CipherMode cipherMode,
            PaddingMode paddingMode,
            byte[] key,
            byte[] iv,
            int blockSize,
            bool encrypting)
        {
            SafeAlgorithmHandle algorithm = GetCipherAlgorithm(cipherMode);

            BasicSymmetricCipher cipher = new CngCipher(algorithm, cipherMode, blockSize, key, iv, encrypting);
            return UniversalCryptoTransform.Create(paddingMode, cipher, encrypting);
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350")] // We are providing the implementation for 3DES not consuming it
        private static readonly SafeAlgorithmHandle s_hAlgCbc = Open3DesAlgorithm(Cng.BCRYPT_CHAIN_MODE_CBC);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350")] // We are providing the implementation for 3DES not consuming it
        private static readonly SafeAlgorithmHandle s_hAlgEcb = Open3DesAlgorithm(Cng.BCRYPT_CHAIN_MODE_ECB);

        private static SafeAlgorithmHandle GetCipherAlgorithm(CipherMode cipherMode)
        {
            // Windows 8 added support to set the CipherMode value on a key,
            // but Windows 7 requires that it be set on the algorithm before key creation.
            switch (cipherMode)
            {
                case CipherMode.CBC:
                    return s_hAlgCbc;
                case CipherMode.ECB:
                    return s_hAlgEcb;
                default:
                    throw new NotSupportedException();
            }
        }

        private static SafeAlgorithmHandle Open3DesAlgorithm(string cipherMode)
        {
            SafeAlgorithmHandle hAlg = Cng.BCryptOpenAlgorithmProvider(Cng.BCRYPT_3DES_ALGORITHM, null, Cng.OpenAlgorithmProviderFlags.NONE);
            hAlg.SetCipherMode(cipherMode);

            return hAlg;
        }
    }
}
