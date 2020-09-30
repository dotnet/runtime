// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350", Justification = "We are providing the implementation for DES, not consuming it.")]
    internal sealed partial class DesImplementation : DES
    {
        private const int BitsPerByte = 8;

        public override ICryptoTransform CreateDecryptor()
        {
            return CreateTransform(Key, IV, encrypting: false);
        }

        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV)
        {
            return CreateTransform(rgbKey, rgbIV.CloneByteArray(), encrypting: false);
        }

        public override ICryptoTransform CreateEncryptor()
        {
            return CreateTransform(Key, IV, encrypting: true);
        }

        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV)
        {
            return CreateTransform(rgbKey, rgbIV.CloneByteArray(), encrypting: true);
        }

        public override void GenerateIV()
        {
            byte[] iv = new byte[BlockSize / BitsPerByte];
            RandomNumberGenerator.Fill(iv);
            IV = iv;
        }

        public sealed override void GenerateKey()
        {
            byte[] key = new byte[KeySize / BitsPerByte];
            RandomNumberGenerator.Fill(key);
            // Never hand back a weak or semi-weak key
            while (IsWeakKey(key) || IsSemiWeakKey(key))
            {
                RandomNumberGenerator.Fill(key);
            }
            KeyValue = key;
        }

        private ICryptoTransform CreateTransform(byte[] rgbKey, byte[]? rgbIV, bool encrypting)
        {
            // note: rgbIV is guaranteed to be cloned before this method, so no need to clone it again

            if (rgbKey == null)
                throw new ArgumentNullException(nameof(rgbKey));

            long keySize = rgbKey.Length * (long)BitsPerByte;
            if (keySize > int.MaxValue || !((int)keySize).IsLegalSize(LegalKeySizes))
                throw new ArgumentException(SR.Cryptography_InvalidKeySize, nameof(rgbKey));

            if (IsWeakKey(rgbKey))
                throw new CryptographicException(SR.Cryptography_InvalidKey_Weak, "DES");
            if (IsSemiWeakKey(rgbKey))
                throw new CryptographicException(SR.Cryptography_InvalidKey_SemiWeak, "DES");

            if (rgbIV != null)
            {
                long ivSize = rgbIV.Length * (long)BitsPerByte;
                if (ivSize != BlockSize)
                    throw new ArgumentException(SR.Cryptography_InvalidIVSize, nameof(rgbIV));
            }

            if (Mode == CipherMode.CFB)
            {
                ValidateCFBFeedbackSize(FeedbackSize);
            }

            return CreateTransformCore(Mode, Padding, rgbKey, rgbIV, BlockSize / BitsPerByte, FeedbackSize / BitsPerByte, this.GetPaddingSize(), encrypting);
        }

        private static void ValidateCFBFeedbackSize(int feedback)
        {
            // only 8bits feedback is available on all platforms
            if (feedback != 8)
            {
                throw new CryptographicException(string.Format(SR.Cryptography_CipherModeFeedbackNotSupported, feedback, CipherMode.CFB));
            }
        }
    }
}
