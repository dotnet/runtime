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
            // TODO(12368): FeedbackSizeValue = 8;, but the field is not yet available.
            KeySizeValue = 256;
            ModeValue = CipherMode.CBC;
        }

        public static Aes Create()
        {
            return new AesImplementation();
        }

        private static readonly KeySizes[] s_legalBlockSizes = { new KeySizes(128, 128, 0) };
        private static readonly KeySizes[] s_legalKeySizes = { new KeySizes(128, 256, 64) };
    }
}
