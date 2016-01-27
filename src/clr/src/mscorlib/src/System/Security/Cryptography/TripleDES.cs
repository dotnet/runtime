// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Contracts;
namespace System.Security.Cryptography {
[System.Runtime.InteropServices.ComVisible(true)]
    public abstract class TripleDES : SymmetricAlgorithm
    {
        private static  KeySizes[] s_legalBlockSizes = {
            new KeySizes(64, 64, 0)
        };

        private static  KeySizes[] s_legalKeySizes = {
            new KeySizes(2*64, 3*64, 64)
        };
      
        //
        // protected constructors
        //
    
        protected TripleDES() {
            KeySizeValue = 3*64;
            BlockSizeValue = 64;
            FeedbackSizeValue = BlockSizeValue;
            LegalBlockSizesValue = s_legalBlockSizes;
            LegalKeySizesValue = s_legalKeySizes;
        }
    
        //
        // public properties
        //

        public override byte[] Key {
            get { 
                if (KeyValue == null) {
                    // Never hand back a weak key
                    do {
                        GenerateKey();
                    } while (IsWeakKey(KeyValue));
                }
                return (byte[]) KeyValue.Clone(); 
            }
            set {
                if (value == null) throw new ArgumentNullException("value");
                Contract.EndContractBlock();
                if (!ValidKeySize(value.Length * 8)) { // must convert bytes to bits
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidKeySize"));
                }
                if (IsWeakKey(value)) {
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidKey_Weak"),"TripleDES");
                }
                KeyValue = (byte[]) value.Clone();
                KeySizeValue = value.Length * 8;
            }
        }
        
        //
        // public methods
        //

        new static public TripleDES Create() {
            return Create("System.Security.Cryptography.TripleDES");
        }

        new static public TripleDES Create(String str) {
            return (TripleDES) CryptoConfig.CreateFromName(str);
        }

        public static bool IsWeakKey(byte[] rgbKey) {
            // All we have to check for here is (a) we're in 3-key mode (192 bits), and
            // (b) either K1 == K2 or K2 == K3
            if (!IsLegalKeySize(rgbKey)) {
                throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidKeySize"));
            }
            byte[] rgbOddParityKey = Utils.FixupKeyParity(rgbKey);
            if (EqualBytes(rgbOddParityKey,0,8,8)) return(true);
            if ((rgbOddParityKey.Length == 24) && EqualBytes(rgbOddParityKey,8,16,8)) return(true);
            return(false);
        }
    
        //
        // private methods
        //

        private static bool EqualBytes(byte[] rgbKey, int start1, int start2, int count) {
            if (start1 < 0) throw new ArgumentOutOfRangeException("start1", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (start2 < 0) throw new ArgumentOutOfRangeException("start2", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if ((start1+count) > rgbKey.Length) throw new ArgumentException(Environment.GetResourceString("Argument_InvalidValue"));
            if ((start2+count) > rgbKey.Length) throw new ArgumentException(Environment.GetResourceString("Argument_InvalidValue"));
            Contract.EndContractBlock();
            for (int i = 0; i < count; i++) {
                if (rgbKey[start1+i] != rgbKey[start2+i]) return(false);
            }
            return(true);
        }

        private static bool IsLegalKeySize(byte[] rgbKey) {
            if (rgbKey != null && ((rgbKey.Length == 16) || (rgbKey.Length == 24))) 
                return(true);
            return(false);
        }
    }
}    
