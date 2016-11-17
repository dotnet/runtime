// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace System.Security.Cryptography
{
    public abstract partial class DSA : AsymmetricAlgorithm
    {
        public abstract DSAParameters ExportParameters(bool includePrivateParameters);

        public abstract void ImportParameters(DSAParameters parameters);

        protected DSA() { }

        public static new DSA Create(string algName)
        {
            return (DSA)CryptoConfig.CreateFromName(algName);
        }

        // DSA does not encode the algorithm identifier into the signature blob, therefore CreateSignature and
        // VerifySignature do not need the HashAlgorithmName value, only SignData and VerifyData do.
        abstract public byte[] CreateSignature(byte[] rgbHash);

        abstract public bool VerifySignature(byte[] rgbHash, byte[] rgbSignature);

        protected virtual byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
        {
            throw DerivedClassMustOverride();
        }

        protected virtual byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm)
        {
            throw DerivedClassMustOverride();
        }

        public byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm)
        {
            return SignData(data, 0, data.Length, hashAlgorithm);
        }

        public virtual byte[] SignData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
        {
            if (data == null) { throw new ArgumentNullException(nameof(data)); }
            if (offset < 0 || offset > data.Length) { throw new ArgumentOutOfRangeException(nameof(offset)); }
            if (count < 0 || count > data.Length - offset) { throw new ArgumentOutOfRangeException(nameof(count)); }
            if (String.IsNullOrEmpty(hashAlgorithm.Name)) { throw HashAlgorithmNameNullOrEmpty(); }

            byte[] hash = HashData(data, offset, count, hashAlgorithm);
            return CreateSignature(hash);
        }

        public virtual byte[] SignData(Stream data, HashAlgorithmName hashAlgorithm)
        {
            if (data == null) { throw new ArgumentNullException(nameof(data)); }
            if (String.IsNullOrEmpty(hashAlgorithm.Name)) { throw HashAlgorithmNameNullOrEmpty(); }

            byte[] hash = HashData(data, hashAlgorithm);
            return CreateSignature(hash);
        }

        public bool VerifyData(byte[] data, byte[] signature, HashAlgorithmName hashAlgorithm)
        {
            return VerifyData(data, 0, data.Length, signature, hashAlgorithm);
        }

        public virtual bool VerifyData(byte[] data, int offset, int count, byte[] signature, HashAlgorithmName hashAlgorithm)
        {
            if (data == null) { throw new ArgumentNullException(nameof(data)); }
            if (offset < 0 || offset > data.Length) { throw new ArgumentOutOfRangeException(nameof(offset)); }
            if (count < 0 || count > data.Length - offset) { throw new ArgumentOutOfRangeException(nameof(count)); }
            if (signature == null) { throw new ArgumentNullException(nameof(signature)); }
            if (String.IsNullOrEmpty(hashAlgorithm.Name)) { throw HashAlgorithmNameNullOrEmpty(); }

            byte[] hash = HashData(data, offset, count, hashAlgorithm);
            return VerifySignature(hash, signature);
        }

        public virtual bool VerifyData(Stream data, byte[] signature, HashAlgorithmName hashAlgorithm)
        {
            if (data == null) { throw new ArgumentNullException(nameof(data)); }
            if (signature == null) { throw new ArgumentNullException(nameof(signature)); }
            if (String.IsNullOrEmpty(hashAlgorithm.Name)) { throw HashAlgorithmNameNullOrEmpty(); }

            byte[] hash = HashData(data, hashAlgorithm);
            return VerifySignature(hash, signature);
        }

        private static Exception DerivedClassMustOverride()
        {
            return new NotImplementedException(SR.NotSupported_SubclassOverride);
        }

        internal static Exception HashAlgorithmNameNullOrEmpty()
        {
            return new ArgumentException(SR.Cryptography_HashAlgorithmNameNullOrEmpty, "hashAlgorithm");
        }
    }
}
