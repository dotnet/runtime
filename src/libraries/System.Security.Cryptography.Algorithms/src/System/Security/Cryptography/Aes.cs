// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                return s_legalBlockSizes.CloneKeySizesArray();
            }
        }

        public override KeySizes[] LegalKeySizes
        {
            get
            {
                return s_legalKeySizes.CloneKeySizesArray();
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
