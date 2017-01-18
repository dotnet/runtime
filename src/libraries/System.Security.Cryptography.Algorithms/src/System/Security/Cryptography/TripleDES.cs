// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

using Internal.Cryptography;

namespace System.Security.Cryptography
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350")] // We are providing the implementation for 3DES not consuming it
    public abstract class TripleDES : SymmetricAlgorithm
    {
        protected TripleDES()
        {
            KeySizeValue = 3*64;
            BlockSizeValue = 64;
            FeedbackSizeValue = BlockSizeValue;
            LegalBlockSizesValue = s_legalBlockSizes.CloneKeySizesArray();
            LegalKeySizesValue = s_legalKeySizes.CloneKeySizesArray();
        }

        public static new TripleDES Create()
        {
            return new TripleDesImplementation();
        }

        public static new TripleDES Create(string str)
        {
            return (TripleDES)CryptoConfig.CreateFromName(str);
        }

        public override byte[] Key
        {
            get
            {
                byte[] key = base.Key;
                while (IsWeakKey(key))
                {
                    GenerateKey();
                    key = base.Key;
                }
                return key;
            }

            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (!(value.Length*8).IsLegalSize(s_legalKeySizes))
                    throw new ArgumentException(SR.Cryptography_InvalidKeySize);

                if (IsWeakKey(value))
                    throw new CryptographicException(SR.Format(SR.Cryptography_InvalidKey_Weak, "TripleDES"));

                base.Key = value;
            }
        }

        public static bool IsWeakKey(byte[] rgbKey)
        {
            if (rgbKey == null)
                throw new CryptographicException(SR.Cryptography_InvalidKeySize);  // Desktop compat: Strange exception for a null value, but this is what we threw in classic CLR. 

            if (!(rgbKey.Length*8).IsLegalSize(s_legalKeySizes))
                throw new CryptographicException(SR.Cryptography_InvalidKeySize);

            byte[] rgbOddParityKey = rgbKey.FixupKeyParity();
            if (EqualBytes(rgbOddParityKey, 0, 8, 8))
                return true;
            if ((rgbOddParityKey.Length == 24) && EqualBytes(rgbOddParityKey, 8, 16, 8))
                return true;
            return false;
        }

        private static bool EqualBytes(byte[] rgbKey, int start1, int start2, int count)
        {
            Debug.Assert(start1 >= 0);
            Debug.Assert(start2 >= 0);
            Debug.Assert((start1 + count) <= rgbKey.Length);
            Debug.Assert((start2 + count) <= rgbKey.Length);

            for (int i = 0; i < count; i++)
            {
                if (rgbKey[start1 + i] != rgbKey[start2 + i])
                    return false;
            }
            return true;
        }

        private static readonly KeySizes[] s_legalBlockSizes =
        {
            new KeySizes(minSize: 64, maxSize: 64, skipSize: 0)
        };

        private static readonly KeySizes[] s_legalKeySizes =
        {
            new KeySizes(minSize: 2*64, maxSize: 3*64, skipSize: 64)
        };
    }
}
