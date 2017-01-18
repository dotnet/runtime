// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

using Internal.Cryptography;

namespace System.Security.Cryptography
{
    public abstract partial class RSA : AsymmetricAlgorithm
    {
        public static new RSA Create(String algName)
        {
            return (RSA)CryptoConfig.CreateFromName(algName);
        }

        public abstract RSAParameters ExportParameters(bool includePrivateParameters);
        public abstract void ImportParameters(RSAParameters parameters);
        public virtual byte[] Encrypt(byte[] data, RSAEncryptionPadding padding) { throw DerivedClassMustOverride(); }
        public virtual byte[] Decrypt(byte[] data, RSAEncryptionPadding padding) { throw DerivedClassMustOverride(); }
        public virtual byte[] SignHash(byte[] hash, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding) { throw DerivedClassMustOverride(); }
        public virtual bool VerifyHash(byte[] hash, byte[] signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding) { throw DerivedClassMustOverride(); }

        protected virtual byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm) { throw DerivedClassMustOverride(); }
        protected virtual byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm) { throw DerivedClassMustOverride(); }

        private static Exception DerivedClassMustOverride()
        {
            return new NotImplementedException(SR.NotSupported_SubclassOverride);
        }

        public virtual byte[] DecryptValue(byte[] rgb) 
        {
            throw new NotSupportedException(SR.NotSupported_Method); // Same as Desktop
        }

        public virtual byte[] EncryptValue(byte[] rgb) 
        {
            throw new NotSupportedException(SR.NotSupported_Method); // Same as Desktop
        }

        public byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            return SignData(data, 0, data.Length, hashAlgorithm, padding);
        }

        public virtual byte[] SignData(
            byte[] data,
            int offset,
            int count,
            HashAlgorithmName hashAlgorithm,
            RSASignaturePadding padding)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (offset < 0 || offset > data.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || count > data.Length - offset)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw HashAlgorithmNameNullOrEmpty();
            if (padding == null)
                throw new ArgumentNullException(nameof(padding));

            byte[] hash = HashData(data, offset, count, hashAlgorithm);
            return SignHash(hash, hashAlgorithm, padding);
        }

        public virtual byte[] SignData(Stream data, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw HashAlgorithmNameNullOrEmpty();
            if (padding == null)
                throw new ArgumentNullException(nameof(padding));

            byte[] hash = HashData(data, hashAlgorithm);
            return SignHash(hash, hashAlgorithm, padding);
        }

        public bool VerifyData(byte[] data, byte[] signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            return VerifyData(data, 0, data.Length, signature, hashAlgorithm, padding);
        }

        public virtual bool VerifyData(
            byte[] data,
            int offset,
            int count,
            byte[] signature,
            HashAlgorithmName hashAlgorithm,
            RSASignaturePadding padding)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (offset < 0 || offset > data.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || count > data.Length - offset)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (signature == null)
                throw new ArgumentNullException(nameof(signature));
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw HashAlgorithmNameNullOrEmpty();
            if (padding == null)
                throw new ArgumentNullException(nameof(padding));

            byte[] hash = HashData(data, offset, count, hashAlgorithm);
            return VerifyHash(hash, signature, hashAlgorithm, padding);
        }

        public bool VerifyData(Stream data, byte[] signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (signature == null)
                throw new ArgumentNullException(nameof(signature));
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw HashAlgorithmNameNullOrEmpty();
            if (padding == null)
                throw new ArgumentNullException(nameof(padding));

            byte[] hash = HashData(data, hashAlgorithm);
            return VerifyHash(hash, signature, hashAlgorithm, padding);
        }

        public override string KeyExchangeAlgorithm => "RSA";
        public override string SignatureAlgorithm => "RSA";

        private static Exception HashAlgorithmNameNullOrEmpty()
        {
            return new ArgumentException(SR.Cryptography_HashAlgorithmNameNullOrEmpty, "hashAlgorithm");
        }
    }
}
