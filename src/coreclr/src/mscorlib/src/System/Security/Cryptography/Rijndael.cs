// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Security.Cryptography
{
[System.Runtime.InteropServices.ComVisible(true)]

    public abstract class Rijndael : SymmetricAlgorithm
    {
        private static  KeySizes[] s_legalBlockSizes = {
          new KeySizes(128, 256, 64)
        };

        private static  KeySizes[] s_legalKeySizes = {
            new KeySizes(128, 256, 64)
        };

        //
        // protected constructors
        //

        protected Rijndael() {
            KeySizeValue = 256;
            BlockSizeValue = 128;
            FeedbackSizeValue = BlockSizeValue;
            LegalBlockSizesValue = s_legalBlockSizes;
            LegalKeySizesValue = s_legalKeySizes;
        }

        //
        // public methods
        //

        new static public Rijndael Create() {
            return Create("System.Security.Cryptography.Rijndael");
        }

        new static public Rijndael Create(String algName) {
            return (Rijndael) CryptoConfig.CreateFromName(algName);
        }
    }
}
