// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Cryptography;
using System.ComponentModel;

namespace System.Security.Cryptography
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class Rijndael : SymmetricAlgorithm
    {
        public static Rijndael Create()
        {
            return new RijndaelImplementation();
        }

        protected Rijndael()
        {
            LegalBlockSizesValue = s_legalBlockSizes.CloneKeySizesArray();
            LegalKeySizesValue = s_legalKeySizes.CloneKeySizesArray();
            KeySizeValue = 256;
            BlockSizeValue = 128;
        }

        private static readonly KeySizes[] s_legalBlockSizes =
        {
            new KeySizes(minSize: 128, maxSize: 256, skipSize: 64)
        };

        private static readonly KeySizes[] s_legalKeySizes =
        {
            new KeySizes(minSize: 128, maxSize: 256, skipSize: 64)
        };
    }
}
