// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

using Internal.Cryptography;

namespace System.Security.Cryptography
{
    public abstract partial class RSA : AsymmetricAlgorithm
    {
        public abstract RSAParameters ExportParameters(bool includePrivateParameters);
        public abstract void ImportParameters(RSAParameters parameters);
        public abstract byte[] Encrypt(byte[] data, RSAEncryptionPadding padding);
        public abstract byte[] Decrypt(byte[] data, RSAEncryptionPadding padding);
        public abstract byte[] SignHash(byte[] hash, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding);
        public abstract bool VerifyHash(byte[] hash, byte[] signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding);

        protected abstract byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm);
        protected abstract byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm);

        public byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
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
                throw new ArgumentNullException("data");
            if (offset < 0 || offset > data.Length)
                throw new ArgumentOutOfRangeException("offset");
            if (count < 0 || count > data.Length - offset)
                throw new ArgumentOutOfRangeException("count");
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw HashAlgorithmNameNullOrEmpty();
            if (padding == null)
                throw new ArgumentNullException("padding");

            byte[] hash = HashData(data, offset, count, hashAlgorithm);
            return SignHash(hash, hashAlgorithm, padding);
        }

        public virtual byte[] SignData(Stream data, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw HashAlgorithmNameNullOrEmpty();
            if (padding == null)
                throw new ArgumentNullException("padding");

            byte[] hash = HashData(data, hashAlgorithm);
            return SignHash(hash, hashAlgorithm, padding);
        }

        public bool VerifyData(byte[] data, byte[] signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
        {
            if (data == null)
                throw new ArgumentNullException("data");

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
                throw new ArgumentNullException("data");
            if (offset < 0 || offset > data.Length)
                throw new ArgumentOutOfRangeException("offset");
            if (count < 0 || count > data.Length - offset)
                throw new ArgumentOutOfRangeException("count");
            if (signature == null)
                throw new ArgumentNullException("signature");
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw HashAlgorithmNameNullOrEmpty();
            if (padding == null)
                throw new ArgumentNullException("padding");

            byte[] hash = HashData(data, offset, count, hashAlgorithm);
            return VerifyHash(hash, signature, hashAlgorithm, padding);
        }

        public bool VerifyData(Stream data, byte[] signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (signature == null)
                throw new ArgumentNullException("signature");
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw HashAlgorithmNameNullOrEmpty();
            if (padding == null)
                throw new ArgumentNullException("padding");

            byte[] hash = HashData(data, hashAlgorithm);
            return VerifyHash(hash, signature, hashAlgorithm, padding);
        }

        private static Exception HashAlgorithmNameNullOrEmpty()
        {
            return new ArgumentException(SR.Cryptography_HashAlgorithmNameNullOrEmpty, "hashAlgorithm");
        }
    }
}
