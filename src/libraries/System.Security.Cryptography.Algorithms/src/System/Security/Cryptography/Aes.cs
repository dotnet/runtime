// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Cryptography;

namespace System.Security.Cryptography
{
    public abstract class Aes : SymmetricAlgorithm
    {
        protected Aes()
        {
            LegalBlockSizesValue = s_legalBlockSizes.CloneKeySizesArray();
            LegalKeySizesValue = s_legalKeySizes.CloneKeySizesArray();

            BlockSizeValue = 128;
            FeedbackSizeValue = 8;
            KeySizeValue = 256;
            ModeValue = CipherMode.CBC;
        }

        protected internal int GetPaddingSize()
        {
            // CFB8 does not require any padding at all
            // otherwise, it is always required to pad for block size
            if (Mode == CipherMode.CFB && FeedbackSize == 8)
                return 1;

            return BlockSize / 8;
        }

        public static new Aes Create()
        {
            return new AesImplementation();
        }

        public static new Aes? Create(string algorithmName)
        {
            return (Aes?)CryptoConfig.CreateFromName(algorithmName);
        }

        private static readonly KeySizes[] s_legalBlockSizes = { new KeySizes(128, 128, 0) };
        private static readonly KeySizes[] s_legalKeySizes = { new KeySizes(128, 256, 64) };
    }
}
