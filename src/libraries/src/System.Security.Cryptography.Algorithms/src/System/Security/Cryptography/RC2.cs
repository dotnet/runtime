// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

using Internal.Cryptography;

namespace System.Security.Cryptography
{
    public abstract class RC2 : SymmetricAlgorithm
    {
        protected RC2()
            : base()
        {
            KeySize = 128;
            BlockSize = 64;
        }

        public static RC2 Create()
        {
            throw new NotImplementedException(SR.WorkInProgress);
        }

        public override KeySizes[] LegalKeySizes
        {
            get
            {
                return s_legalKeySizes.CloneKeySizesArray();
            }
        }

        public override KeySizes[] LegalBlockSizes
        {
            get
            {
                return s_legalBlockSizes.CloneKeySizesArray();
            }
        }

        public override int KeySize
        {
            get
            {
                return base.KeySize;
            }

            set
            {
                if (value < _effectiveKeySize)
                    throw new CryptographicException(SR.Cryptography_RC2_EKSKS);
                base.KeySize = value;
            }
        }

        public virtual int EffectiveKeySize
        {
            get
            {
                if (_effectiveKeySize == 0)
                    return KeySize;
                return _effectiveKeySize;
            }

            set
            {
                if (value > KeySize)
                    throw new CryptographicException(SR.Cryptography_RC2_EKSKS);

                if (value == 0)
                {
                    _effectiveKeySize = value;
                }
                else if (value < 40)
                {
                    throw new CryptographicException(SR.Cryptography_RC2_EKS40);
                }
                else if (value.IsLegalSize(LegalKeySizes))
                {
                    _effectiveKeySize = value;
                }
                else
                { 
                    throw new CryptographicException(SR.Cryptography_InvalidKeySize);
                }
            }
        }

        private int _effectiveKeySize;

        private static readonly KeySizes[] s_legalBlockSizes = 
        {
            new KeySizes(minSize: 64, maxSize: 64, skipSize: 0)
        };

        private static readonly KeySizes[] s_legalKeySizes = 
        {
            new KeySizes(minSize: 40, maxSize: 1024, skipSize: 8)  // 1024 bits is theoretical max according to the RFC
        };
    }
}
