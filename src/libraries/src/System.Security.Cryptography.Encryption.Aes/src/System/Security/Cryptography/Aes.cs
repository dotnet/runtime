// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

using Internal.Cryptography;

namespace System.Security.Cryptography
{
    public abstract class Aes : SymmetricAlgorithm
    {
        protected Aes()
        {
            this.BlockSize = 128;
            this.KeySize = 256;
            this.Mode = CipherMode.CBC;
        }

        public override KeySizes[] LegalBlockSizes
        {
            get
            {
                return (KeySizes[])(s_legalBlockSizes.Clone());
            }
        }

        public override KeySizes[] LegalKeySizes
        {
            get
            {
                return (KeySizes[])(s_legalKeySizes.Clone());
            }
        }

        public static Aes Create()
        {
            return new AesImplementation();
        }



        private static readonly KeySizes[] s_legalBlockSizes = { new KeySizes(128, 128, 0) };
        private static readonly KeySizes[] s_legalKeySizes = { new KeySizes(128, 256, 64) };
    }
}
