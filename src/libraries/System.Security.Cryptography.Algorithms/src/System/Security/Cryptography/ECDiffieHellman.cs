// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography
{
    /// <summary>
    ///     Abstract base class for implementations of elliptic curve Diffie-Hellman to derive from
    /// </summary>
    public abstract partial class ECDiffieHellman : AsymmetricAlgorithm
    {
        private static readonly string[] s_validOids =
        {
            Oids.EcPublicKey,
            // Neither Windows nor OpenSSL seem to read id-ecDH Pkcs8/SPKI.
            // ECMQV is not valid in this context.
        };

        public override string KeyExchangeAlgorithm
        {
            get { return "ECDiffieHellman"; }
        }

        public override string SignatureAlgorithm
        {
            get { return null; }
        }

        public static new ECDiffieHellman Create(string algorithm)
        {
            if (algorithm == null)
            {
                throw new ArgumentNullException(nameof(algorithm));
            }

            return CryptoConfig.CreateFromName(algorithm) as ECDiffieHellman;
        }

        public abstract ECDiffieHellmanPublicKey PublicKey { get; }

        // This method must be implemented by derived classes. In order to conform to the contract, it cannot be abstract.
        public virtual byte[] DeriveKeyMaterial(ECDiffieHellmanPublicKey otherPartyPublicKey)
        {
            throw DerivedClassMustOverride();
        }

        /// <summary>
        /// Derive key material using the formula HASH(x) where x is the computed result of the EC Diffie-Hellman algorithm.
        /// </summary>
        /// <param name="otherPartyPublicKey">The public key of the party with which to derive a mutual secret.</param>
        /// <param name="hashAlgorithm">The identifier for the hash algorithm to use.</param>
        /// <returns>A hashed output suitable for key material</returns>
        /// <exception cref="ArgumentException"><paramref name="otherPartyPublicKey"/> is over a different curve than this key</exception>
        public byte[] DeriveKeyFromHash(ECDiffieHellmanPublicKey otherPartyPublicKey, HashAlgorithmName hashAlgorithm)
        {
            return DeriveKeyFromHash(otherPartyPublicKey, hashAlgorithm, null, null);
        }

        /// <summary>
        /// Derive key material using the formula HASH(secretPrepend || x || secretAppend) where x is the computed
        /// result of the EC Diffie-Hellman algorithm.
        /// </summary>
        /// <param name="otherPartyPublicKey">The public key of the party with which to derive a mutual secret.</param>
        /// <param name="hashAlgorithm">The identifier for the hash algorithm to use.</param>
        /// <param name="secretPrepend">A value to prepend to the derived secret before hashing. A <c>null</c> value is treated as an empty array.</param>
        /// <param name="secretAppend">A value to append to the derived secret before hashing. A <c>null</c> value is treated as an empty array.</param>
        /// <returns>A hashed output suitable for key material</returns>
        /// <exception cref="ArgumentException"><paramref name="otherPartyPublicKey"/> is over a different curve than this key</exception>
        public virtual byte[] DeriveKeyFromHash(
            ECDiffieHellmanPublicKey otherPartyPublicKey,
            HashAlgorithmName hashAlgorithm,
            byte[] secretPrepend,
            byte[] secretAppend)
        {
            throw DerivedClassMustOverride();
        }

        /// <summary>
        /// Derive key material using the formula HMAC(hmacKey, x) where x is the computed
        /// result of the EC Diffie-Hellman algorithm.
        /// </summary>
        /// <param name="otherPartyPublicKey">The public key of the party with which to derive a mutual secret.</param>
        /// <param name="hashAlgorithm">The identifier for the hash algorithm to use.</param>
        /// <param name="hmacKey">The key to use in the HMAC. A <c>null</c> value indicates that the result of the EC Diffie-Hellman algorithm should be used as the HMAC key.</param>
        /// <returns>A hashed output suitable for key material</returns>
        /// <exception cref="ArgumentException"><paramref name="otherPartyPublicKey"/> is over a different curve than this key</exception>
        public byte[] DeriveKeyFromHmac(
            ECDiffieHellmanPublicKey otherPartyPublicKey,
            HashAlgorithmName hashAlgorithm,
            byte[] hmacKey)
        {
            return DeriveKeyFromHmac(otherPartyPublicKey, hashAlgorithm, hmacKey, null, null);
        }

        /// <summary>
        /// Derive key material using the formula HMAC(hmacKey, secretPrepend || x || secretAppend) where x is the computed
        /// result of the EC Diffie-Hellman algorithm.
        /// </summary>
        /// <param name="otherPartyPublicKey">The public key of the party with which to derive a mutual secret.</param>
        /// <param name="hashAlgorithm">The identifier for the hash algorithm to use.</param>
        /// <param name="hmacKey">The key to use in the HMAC. A <c>null</c> value indicates that the result of the EC Diffie-Hellman algorithm should be used as the HMAC key.</param>
        /// <param name="secretPrepend">A value to prepend to the derived secret before hashing. A <c>null</c> value is treated as an empty array.</param>
        /// <param name="secretAppend">A value to append to the derived secret before hashing. A <c>null</c> value is treated as an empty array.</param>
        /// <returns>A hashed output suitable for key material</returns>
        /// <exception cref="ArgumentException"><paramref name="otherPartyPublicKey"/> is over a different curve than this key</exception>
        public virtual byte[] DeriveKeyFromHmac(
            ECDiffieHellmanPublicKey otherPartyPublicKey,
            HashAlgorithmName hashAlgorithm,
            byte[] hmacKey,
            byte[] secretPrepend,
            byte[] secretAppend)
        {
            throw DerivedClassMustOverride();
        }

        /// <summary>
        /// Derive key material using the TLS pseudo-random function (PRF) derivation algorithm.
        /// </summary>
        /// <param name="otherPartyPublicKey">The public key of the party with which to derive a mutual secret.</param>
        /// <param name="prfLabel">The ASCII encoded PRF label.</param>
        /// <param name="prfSeed">The 64-byte PRF seed.</param>
        /// <returns>A 48-byte output of the TLS pseudo-random function.</returns>
        /// <exception cref="ArgumentException"><paramref name="otherPartyPublicKey"/> is over a different curve than this key</exception>
        /// <exception cref="ArgumentNullException"><paramref name="prfLabel"/> is null</exception>
        /// <exception cref="ArgumentNullException"><paramref name="prfSeed"/> is null</exception>
        /// <exception cref="CryptographicException"><paramref name="prfSeed"/> is not exactly 64 bytes in length</exception>
        public virtual byte[] DeriveKeyTls(ECDiffieHellmanPublicKey otherPartyPublicKey, byte[] prfLabel, byte[] prfSeed)
        {
            throw DerivedClassMustOverride();
        }

        private static Exception DerivedClassMustOverride()
        {
            return new NotImplementedException(SR.NotSupported_SubclassOverride);
        }

        /// <summary>
        /// When overridden in a derived class, exports the named or explicit ECParameters for an ECCurve.
        /// If the curve has a name, the Curve property will contain named curve parameters, otherwise it
        /// will contain explicit parameters.
        /// </summary>
        /// <param name="includePrivateParameters">true to include private parameters, otherwise, false.</param>
        /// <returns>The ECParameters representing the point on the curve for this key.</returns>
        public virtual ECParameters ExportParameters(bool includePrivateParameters)
        {
            throw DerivedClassMustOverride();
        }

        /// <summary>
        /// When overridden in a derived class, exports the explicit ECParameters for an ECCurve.
        /// </summary>
        /// <param name="includePrivateParameters">true to include private parameters, otherwise, false.</param>
        /// <returns>The ECParameters representing the point on the curve for this key, using the explicit curve format.</returns>
        public virtual ECParameters ExportExplicitParameters(bool includePrivateParameters)
        {
            throw DerivedClassMustOverride();
        }

        /// <summary>
        /// When overridden in a derived class, imports the specified ECParameters.
        /// </summary>
        /// <param name="parameters">The curve parameters.</param>
        public virtual void ImportParameters(ECParameters parameters)
        {
            throw DerivedClassMustOverride();
        }

        /// <summary>
        /// When overridden in a derived class, generates a new public/private keypair for the specified curve.
        /// </summary>
        /// <param name="curve">The curve to use.</param>
        public virtual void GenerateKey(ECCurve curve)
        {
            throw new NotSupportedException(SR.NotSupported_SubclassOverride);
        }

        public override unsafe bool TryExportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<byte> passwordBytes,
            PbeParameters pbeParameters,
            Span<byte> destination,
            out int bytesWritten)
        {
            if (pbeParameters == null)
                throw new ArgumentNullException(nameof(pbeParameters));

            PasswordBasedEncryption.ValidatePbeParameters(
                pbeParameters,
                ReadOnlySpan<char>.Empty,
                passwordBytes);

            ECParameters ecParameters = ExportParameters(true);

            fixed (byte* privPtr = ecParameters.D)
            {
                try
                {
                    using (AsnWriter pkcs8PrivateKey = EccKeyFormatHelper.WritePkcs8PrivateKey(ecParameters))
                    using (AsnWriter writer = KeyFormatHelper.WriteEncryptedPkcs8(
                        passwordBytes,
                        pkcs8PrivateKey,
                        pbeParameters))
                    {
                        return writer.TryEncode(destination, out bytesWritten);
                    }
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(ecParameters.D);
                }
            }
        }

        public override unsafe bool TryExportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<char> password,
            PbeParameters pbeParameters,
            Span<byte> destination,
            out int bytesWritten)
        {
            if (pbeParameters == null)
                throw new ArgumentNullException(nameof(pbeParameters));

            PasswordBasedEncryption.ValidatePbeParameters(
                pbeParameters,
                password,
                ReadOnlySpan<byte>.Empty);

            ECParameters ecParameters = ExportParameters(true);

            fixed (byte* privPtr = ecParameters.D)
            {
                try
                {
                    using (AsnWriter pkcs8PrivateKey = EccKeyFormatHelper.WritePkcs8PrivateKey(ecParameters))
                    using (AsnWriter writer = KeyFormatHelper.WriteEncryptedPkcs8(
                        password,
                        pkcs8PrivateKey,
                        pbeParameters))
                    {
                        return writer.TryEncode(destination, out bytesWritten);
                    }
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(ecParameters.D);
                }
            }
        }

        public override unsafe bool TryExportPkcs8PrivateKey(
            Span<byte> destination,
            out int bytesWritten)
        {
            ECParameters ecParameters = ExportParameters(true);

            fixed (byte* privPtr = ecParameters.D)
            {
                try
                {
                    using (AsnWriter writer = EccKeyFormatHelper.WritePkcs8PrivateKey(ecParameters))
                    {
                        return writer.TryEncode(destination, out bytesWritten);
                    }
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(ecParameters.D);
                }
            }
        }

        public override bool TryExportSubjectPublicKeyInfo(
            Span<byte> destination,
            out int bytesWritten)
        {
            ECParameters ecParameters = ExportParameters(false);

            using (AsnWriter writer = EccKeyFormatHelper.WriteSubjectPublicKeyInfo(ecParameters))
            {
                return writer.TryEncode(destination, out bytesWritten);
            }
        }

        public override unsafe void ImportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<byte> passwordBytes,
            ReadOnlySpan<byte> source,
            out int bytesRead)
        {
            KeyFormatHelper.ReadEncryptedPkcs8<ECParameters>(
                s_validOids,
                source,
                passwordBytes,
                EccKeyFormatHelper.FromECPrivateKey,
                out int localRead,
                out ECParameters ret);

            fixed (byte* privPin = ret.D)
            {
                try
                {
                    ImportParameters(ret);
                    bytesRead = localRead;
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(ret.D);
                }
            }
        }

        public override unsafe void ImportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<char> password,
            ReadOnlySpan<byte> source,
            out int bytesRead)
        {
            KeyFormatHelper.ReadEncryptedPkcs8<ECParameters>(
                s_validOids,
                source,
                password,
                EccKeyFormatHelper.FromECPrivateKey,
                out int localRead,
                out ECParameters ret);

            fixed (byte* privPin = ret.D)
            {
                try
                {
                    ImportParameters(ret);
                    bytesRead = localRead;
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(ret.D);
                }
            }
        }

        public override unsafe void ImportPkcs8PrivateKey(
            ReadOnlySpan<byte> source,
            out int bytesRead)
        {
            KeyFormatHelper.ReadPkcs8<ECParameters>(
                s_validOids,
                source,
                EccKeyFormatHelper.FromECPrivateKey,
                out int localRead,
                out ECParameters key);

            fixed (byte* privPin = key.D)
            {
                try
                {
                    ImportParameters(key);
                    bytesRead = localRead;
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(key.D);
                }
            }
        }

        public override void ImportSubjectPublicKeyInfo(
            ReadOnlySpan<byte> source,
            out int bytesRead)
        {
            KeyFormatHelper.ReadSubjectPublicKeyInfo<ECParameters>(
                s_validOids,
                source,
                EccKeyFormatHelper.FromECPublicKey,
                out int localRead,
                out ECParameters key);

            ImportParameters(key);
            bytesRead = localRead;
        }

        public virtual unsafe void ImportECPrivateKey(ReadOnlySpan<byte> source, out int bytesRead)
        {
            ECParameters ecParameters = EccKeyFormatHelper.FromECPrivateKey(source, out int localRead);

            fixed (byte* privPin = ecParameters.D)
            {
                try
                {
                    ImportParameters(ecParameters);
                    bytesRead = localRead;
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(ecParameters.D);
                }
            }
        }

        public virtual unsafe byte[] ExportECPrivateKey()
        {
            ECParameters ecParameters = ExportParameters(true);

            fixed (byte* privPin = ecParameters.D)
            {
                try
                {
                    using (AsnWriter writer = EccKeyFormatHelper.WriteECPrivateKey(ecParameters))
                    {
                        return writer.Encode();
                    }
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(ecParameters.D);
                }
            }
        }

        public virtual unsafe bool TryExportECPrivateKey(Span<byte> destination, out int bytesWritten)
        {
            ECParameters ecParameters = ExportParameters(true);

            fixed (byte* privPin = ecParameters.D)
            {
                try
                {
                    using (AsnWriter writer = EccKeyFormatHelper.WriteECPrivateKey(ecParameters))
                    {
                        return writer.TryEncode(destination, out bytesWritten);
                    }
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(ecParameters.D);
                }
            }
        }
    }
}
