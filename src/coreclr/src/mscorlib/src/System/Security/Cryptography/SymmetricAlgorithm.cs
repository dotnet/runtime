// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Contracts;

namespace System.Security.Cryptography {
[System.Runtime.InteropServices.ComVisible(true)]
    public abstract class SymmetricAlgorithm : IDisposable {
        protected int         BlockSizeValue;
        protected int         FeedbackSizeValue;
        protected byte[]      IVValue;
        protected byte[]      KeyValue;
        protected KeySizes[]  LegalBlockSizesValue;
        protected KeySizes[]  LegalKeySizesValue;
        protected int         KeySizeValue;
        protected CipherMode  ModeValue;
        protected PaddingMode PaddingValue;

        //
        // protected constructors
        //
    
        protected SymmetricAlgorithm() {
            // Default to cipher block chaining (CipherMode.CBC) and
            // PKCS-style padding (pad n bytes with value n)
            ModeValue = CipherMode.CBC;
            PaddingValue = PaddingMode.PKCS7;
        }

        // SymmetricAlgorithm implements IDisposable

        // To keep mscorlib compatibility with Orcas, CoreCLR's SymmetricAlgorithm has an explicit IDisposable
        // implementation. Post-Orcas the desktop has an implicit IDispoable implementation.
#if FEATURE_CORECLR
        void IDisposable.Dispose()
        {
            Dispose();
        }
#endif // FEATURE_CORECLR

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Clear() {
            (this as IDisposable).Dispose();
        }

        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                // Note: we always want to zeroize the sensitive key material
                if (KeyValue != null) {
                    Array.Clear(KeyValue, 0, KeyValue.Length);
                    KeyValue = null;
                }
                if (IVValue != null) {
                    Array.Clear(IVValue, 0, IVValue.Length);
                    IVValue = null;
                }
            }
        }

        //
        // public properties
        //

        public virtual int BlockSize {
            get { return BlockSizeValue; }
            set {
                int   i;
                int   j;

                for (i=0; i<LegalBlockSizesValue.Length; i++) {
                    // If a cipher has only one valid key size, MinSize == MaxSize and SkipSize will be 0
                    if (LegalBlockSizesValue[i].SkipSize == 0) {
                        if (LegalBlockSizesValue[i].MinSize == value) { // assume MinSize = MaxSize
                            BlockSizeValue = value;
                            IVValue = null;
                            return;
                        }
                    } else {
                        for (j = LegalBlockSizesValue[i].MinSize; j<=LegalBlockSizesValue[i].MaxSize;
                            j += LegalBlockSizesValue[i].SkipSize) {
                            if (j == value) {
                                if (BlockSizeValue != value) {
                                    BlockSizeValue = value;
                                    IVValue = null;      // Wrong length now
                                }
                                return;
                            }
                        }
                    }
                }
                throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidBlockSize"));
            }
        }

        public virtual int FeedbackSize {
            get { return FeedbackSizeValue; }
            set {
               if (value <= 0 || value > BlockSizeValue || (value % 8) != 0)
                   throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidFeedbackSize"));

               FeedbackSizeValue = value;
            }
        }

        public virtual byte[] IV {
            get { 
                if (IVValue == null) GenerateIV();
                return (byte[]) IVValue.Clone();
            }
            set {
                if (value == null) throw new ArgumentNullException("value");
                Contract.EndContractBlock();
                if (value.Length != BlockSizeValue / 8)
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidIVSize"));

                IVValue = (byte[]) value.Clone();
            }
        }

        public virtual byte[] Key {
            get { 
                if (KeyValue == null) GenerateKey();
                return (byte[]) KeyValue.Clone();
            }
            set { 
                if (value == null) throw new ArgumentNullException("value");
                Contract.EndContractBlock();
                if (!ValidKeySize(value.Length * 8))
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidKeySize"));

                // must convert bytes to bits
                KeyValue = (byte[]) value.Clone();
                KeySizeValue = value.Length * 8;
            }
        }

        public virtual KeySizes[] LegalBlockSizes {
            get { return (KeySizes[]) LegalBlockSizesValue.Clone(); }
        }
    
        public virtual KeySizes[] LegalKeySizes {
            get { return (KeySizes[]) LegalKeySizesValue.Clone(); }
        }
    
        public virtual int KeySize {
            get { return KeySizeValue; }
            set {
                if (!ValidKeySize(value))
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidKeySize"));

                KeySizeValue = value;
                KeyValue = null;
            }
        }
    
        public virtual CipherMode Mode {
            get { return ModeValue; }
            set { 
                if ((value < CipherMode.CBC) || (CipherMode.CFB < value))
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidCipherMode"));

                ModeValue = value;
            }
        }
    
        public virtual PaddingMode Padding {
            get { return PaddingValue; }
            set { 
                if ((value < PaddingMode.None) || (PaddingMode.ISO10126 < value))
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidPaddingMode"));

                PaddingValue = value;
            }
        }

        //
        // public methods
        //

        // The following method takes a bit length input and returns whether that length is a valid size
        // according to LegalKeySizes
        public bool ValidKeySize(int bitLength) {
            KeySizes[] validSizes = this.LegalKeySizes;
            int i,j;
            
            if (validSizes == null) return false;
            for (i=0; i< validSizes.Length; i++) {
                if (validSizes[i].SkipSize == 0) {
                    if (validSizes[i].MinSize == bitLength) { // assume MinSize = MaxSize
                        return true;
                    }
                } else {
                    for (j = validSizes[i].MinSize; j<= validSizes[i].MaxSize;
                         j += validSizes[i].SkipSize) {
                        if (j == bitLength) {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        static public SymmetricAlgorithm Create() {
            // use the crypto config system to return an instance of
            // the default SymmetricAlgorithm on this machine
            return Create("System.Security.Cryptography.SymmetricAlgorithm");
        }

        static public SymmetricAlgorithm Create(String algName) {
            return (SymmetricAlgorithm) CryptoConfig.CreateFromName(algName);
        }

        public virtual ICryptoTransform CreateEncryptor() {
            return CreateEncryptor(Key, IV);
        }

        public abstract ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV);

        public virtual ICryptoTransform CreateDecryptor() {
            return CreateDecryptor(Key, IV);
        }

        public abstract ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV);
        
        public abstract void GenerateKey();

        public abstract void GenerateIV();
    }
}
