// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography.X509Certificates
{
    public abstract class X509SignatureGenerator
    {
        private PublicKey? _publicKey;

        public PublicKey PublicKey => _publicKey ??= BuildPublicKey();

        public abstract byte[] GetSignatureAlgorithmIdentifier(HashAlgorithmName hashAlgorithm);
        public abstract byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm);
        protected abstract PublicKey BuildPublicKey();

        public static X509SignatureGenerator CreateForECDsa(ECDsa key)
        {
            ArgumentNullException.ThrowIfNull(key);

            return new ECDsaX509SignatureGenerator(key);
        }

        public static X509SignatureGenerator CreateForRSA(RSA key, RSASignaturePadding signaturePadding)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(signaturePadding);

            if (signaturePadding == RSASignaturePadding.Pkcs1)
                return new RSAPkcs1X509SignatureGenerator(key);
            if (signaturePadding.Mode == RSASignaturePaddingMode.Pss)
                return new RSAPssX509SignatureGenerator(key, signaturePadding);

            throw new ArgumentException(SR.Cryptography_InvalidPaddingMode);
        }

        /// <summary>
        ///   Creates a signature generator for ML-DSA signatures using the specified key.
        /// </summary>
        /// <param name="key">
        ///   The private key.
        /// </param>
        /// <returns>
        ///   An <see cref="X509SignatureGenerator" /> object for ML-DSA signatures.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="key" /> is <see langword="null" />.
        /// </exception>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public static X509SignatureGenerator CreateForMLDsa(MLDsa key)
        {
            ArgumentNullException.ThrowIfNull(key);

            return new MLDsaX509SignatureGenerator(key);
        }

        /// <summary>
        ///   Creates a signature generator for SLH-DSA signatures using the specified key.
        /// </summary>
        /// <param name="key">
        ///   The private key.
        /// </param>
        /// <returns>
        ///   An <see cref="X509SignatureGenerator" /> object for SLH-DSA signatures.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="key" /> is <see langword="null" />.
        /// </exception>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public static X509SignatureGenerator CreateForSlhDsa(SlhDsa key)
        {
            ArgumentNullException.ThrowIfNull(key);

            return new SlhDsaX509SignatureGenerator(key);
        }

        /// <summary>
        ///   Creates a signature generator for Composite ML-DSA signatures using the specified key.
        /// </summary>
        /// <param name="key">
        ///   The private key.
        /// </param>
        /// <returns>
        ///   An <see cref="X509SignatureGenerator" /> object for Composite ML-DSA signatures.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="key" /> is <see langword="null" />.
        /// </exception>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public static X509SignatureGenerator CreateForCompositeMLDsa(CompositeMLDsa key)
        {
            ArgumentNullException.ThrowIfNull(key);

            throw new PlatformNotSupportedException();
        }
    }
}
