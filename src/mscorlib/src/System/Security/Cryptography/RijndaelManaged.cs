// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Diagnostics.Contracts;

namespace System.Security.Cryptography
{
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class RijndaelManaged : Rijndael {
        public RijndaelManaged () {
#if FEATURE_CRYPTO
            if (CryptoConfig.AllowOnlyFipsAlgorithms)
                throw new InvalidOperationException(Environment.GetResourceString("Cryptography_NonCompliantFIPSAlgorithm"));
            Contract.EndContractBlock();
#endif // FEATURE_CRYPTO
        }

        // #CoreCLRRijndaelModes
        // 
        // On CoreCLR we limit the supported cipher modes and padding modes for the AES algorithm to a
        // single hard coded value.  This allows us to remove a lot of code by removing support for the
        // uncommon cases and forcing everyone to use the same common padding and ciper modes:
        // 
        //  - CipherMode: CipherMode.CBC
        //  - PaddingMode: PaddingMode.PKCS7
 
        public override ICryptoTransform CreateEncryptor (byte[] rgbKey, byte[] rgbIV) {
            return NewEncryptor(rgbKey,
                                ModeValue,
                                rgbIV,
                                FeedbackSizeValue,
                                RijndaelManagedTransformMode.Encrypt);
        }

        public override ICryptoTransform CreateDecryptor (byte[] rgbKey, byte[] rgbIV) {
            return NewEncryptor(rgbKey,
                                ModeValue,
                                rgbIV,
                                FeedbackSizeValue,
                                RijndaelManagedTransformMode.Decrypt);
        }

        public override void GenerateKey () {
            KeyValue = Utils.GenerateRandom(KeySizeValue / 8);
        }

        public override void GenerateIV () {
            IVValue = Utils.GenerateRandom(BlockSizeValue / 8);
        }

        private ICryptoTransform NewEncryptor (byte[] rgbKey,
                                               CipherMode mode,
                                               byte[] rgbIV,
                                               int feedbackSize,
                                               RijndaelManagedTransformMode encryptMode) {
            // Build the key if one does not already exist
            if (rgbKey == null) {
                rgbKey = Utils.GenerateRandom(KeySizeValue / 8);
            }

            // If not ECB mode, make sure we have an IV. In CoreCLR we do not support ECB, so we must have
            // an IV in all cases.
#if !FEATURE_CRYPTO
            if (mode != CipherMode.ECB) {
#endif // !FEATURE_CRYPTO
                if (rgbIV == null) {
                    rgbIV = Utils.GenerateRandom(BlockSizeValue / 8);
                }
#if !FEATURE_CRYPTO
            }
#endif // !FEATURE_CRYPTO

            // Create the encryptor/decryptor object
            return new RijndaelManagedTransform (rgbKey,
                                                 mode,
                                                 rgbIV,
                                                 BlockSizeValue,
                                                 feedbackSize,
                                                 PaddingValue,
                                                 encryptMode);
        }
    }
}
