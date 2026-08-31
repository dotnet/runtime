// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    /// <summary>
    ///   Represents a Composite ML-KEM key.
    /// </summary>
    /// <remarks>
    ///   Developers are encouraged to program against the <see cref="CompositeMLKem"/> base class,
    ///   rather than any specific derived class. The derived classes are intended for interop with the underlying system
    ///   cryptographic libraries.
    /// </remarks>
    [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    public abstract class CompositeMLKem : IDisposable
#if DESIGNTIMEINTERFACES
#pragma warning disable SA1001
        , IImportExportShape<CompositeMLKem>
#pragma warning restore SA1001
#endif
    {
        private protected static readonly string[] KnownOids =
        [
            Oids.MLKem768WithRsaOaep2048Sha3_256,
            Oids.MLKem768WithRsaOaep3072Sha3_256,
            Oids.MLKem768WithRsaOaep4096Sha3_256,
            Oids.MLKem768WithX25519Sha3_256,
            Oids.MLKem768WithECDiffieHellmanP256Sha3_256,
            Oids.MLKem768WithECDiffieHellmanP384Sha3_256,
            Oids.MLKem768WithECDiffieHellmanBrainpoolP256r1Sha3_256,
            Oids.MLKem1024WithRsaOaep3072Sha3_256,
            Oids.MLKem1024WithECDiffieHellmanP384Sha3_256,
            Oids.MLKem1024WithECDiffieHellmanBrainpoolP384r1Sha3_256,
            Oids.MLKem1024WithX448Sha3_256,
            Oids.MLKem1024WithECDiffieHellmanP521Sha3_256,
        ];

        private bool _disposed;

        /// <summary>
        ///   Gets the specific Composite ML-KEM algorithm for this key.
        /// </summary>
        /// <value>
        ///   The specific Composite ML-KEM algorithm for this key.
        /// </value>
        public CompositeMLKemAlgorithm Algorithm { get; }

        /// <summary>
        ///   Initializes a new instance of the <see cref="CompositeMLKem" /> class.
        /// </summary>
        /// <param name="algorithm">
        ///   The specific Composite ML-KEM algorithm for this key.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="algorithm"/> is <see langword="null"/>.
        /// </exception>
        protected CompositeMLKem(CompositeMLKemAlgorithm algorithm)
        {
            ArgumentNullException.ThrowIfNull(algorithm);

            Algorithm = algorithm;
        }

        /// <summary>
        ///   Determines whether the specified algorithm is supported by the current platform.
        /// </summary>
        /// <param name="algorithm">
        ///   The <see cref="CompositeMLKemAlgorithm"/> to check for support.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the algorithm is supported; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="algorithm"/> is <see langword="null"/>.
        /// </exception>
        public static bool IsAlgorithmSupported(CompositeMLKemAlgorithm algorithm)
        {
            ArgumentNullException.ThrowIfNull(algorithm);

            return CompositeMLKemImplementation.IsAlgorithmSupportedImpl(algorithm);
        }

        /// <summary>
        ///   Generates a new Composite ML-KEM key.
        /// </summary>
        /// <param name="algorithm">
        ///   An algorithm identifying what kind of Composite ML-KEM key to generate.
        /// </param>
        /// <returns>
        ///   The generated key.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="algorithm" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred generating the Composite ML-KEM key.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support the specified Composite ML-KEM algorithm. Callers can use <see cref="IsAlgorithmSupported" />
        ///   to determine if the algorithm is supported.
        /// </exception>
        public static CompositeMLKem GenerateKey(CompositeMLKemAlgorithm algorithm)
        {
            ArgumentNullException.ThrowIfNull(algorithm);
            ThrowIfNotSupported(algorithm);

            return CompositeMLKemImplementation.GenerateKeyImpl(algorithm);
        }

        /// <summary>
        ///   Creates an encapsulation ciphertext and shared secret.
        /// </summary>
        /// <param name="ciphertext">
        ///   When this method returns, the ciphertext.
        /// </param>
        /// <param name="sharedSecret">
        ///   When this method returns, the shared secret.
        /// </param>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred during encapsulation.
        /// </exception>
        public void Encapsulate(out byte[] ciphertext, out byte[] sharedSecret)
        {
            ThrowIfDisposed();

            byte[] localCiphertext = new byte[Algorithm.CiphertextSizeInBytes];
            byte[] localSharedSecret = new byte[Algorithm.SharedSecretSizeInBytes];

            EncapsulateCore(localCiphertext, localSharedSecret);

            sharedSecret = localSharedSecret;
            ciphertext = localCiphertext;
        }

        /// <summary>
        ///   Creates an encapsulation ciphertext and shared secret, writing them into the provided buffers.
        /// </summary>
        /// <param name="ciphertext">
        ///   The buffer to receive the ciphertext. Its length must be exactly
        ///   <see cref="CompositeMLKemAlgorithm.CiphertextSizeInBytes"/>.
        /// </param>
        /// <param name="sharedSecret">
        ///   The buffer to receive the shared secret. Its length must be exactly
        ///   <see cref="CompositeMLKemAlgorithm.SharedSecretSizeInBytes"/>.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <para><paramref name="ciphertext" /> is not the correct size.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="sharedSecret" /> is not the correct size.</para>
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>An error occurred during encapsulation.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="ciphertext"/> overlaps with <paramref name="sharedSecret"/>.</para>
        /// </exception>
        public void Encapsulate(Span<byte> ciphertext, Span<byte> sharedSecret)
        {
            if (ciphertext.Length != Algorithm.CiphertextSizeInBytes)
            {
                throw new ArgumentException(
                    SR.Format(SR.Argument_DestinationImprecise, Algorithm.CiphertextSizeInBytes),
                    nameof(ciphertext));
            }

            if (sharedSecret.Length != Algorithm.SharedSecretSizeInBytes)
            {
                throw new ArgumentException(
                    SR.Format(SR.Argument_DestinationImprecise, Algorithm.SharedSecretSizeInBytes),
                    nameof(sharedSecret));
            }

            if (ciphertext.Overlaps(sharedSecret))
            {
                throw new CryptographicException(SR.Cryptography_OverlappingBuffers);
            }

            ThrowIfDisposed();

            EncapsulateCore(ciphertext, sharedSecret);
        }

        /// <summary>
        ///   When overridden in a derived class, creates an encapsulation ciphertext and shared secret, writing them
        ///   into the provided buffers.
        /// </summary>
        /// <param name="ciphertext">
        ///   The buffer to receive the ciphertext, whose length will be exactly
        ///   <see cref="CompositeMLKemAlgorithm.CiphertextSizeInBytes"/>.
        /// </param>
        /// <param name="sharedSecret">
        ///   The buffer to receive the shared secret, whose length will be exactly
        ///   <see cref="CompositeMLKemAlgorithm.SharedSecretSizeInBytes"/>.
        /// </param>
        /// <exception cref="CryptographicException">
        ///   An error occurred during encapsulation.
        /// </exception>
        protected abstract void EncapsulateCore(Span<byte> ciphertext, Span<byte> sharedSecret);

        /// <summary>
        ///   Decapsulates a shared secret from a provided ciphertext.
        /// </summary>
        /// <param name="ciphertext">
        ///   The ciphertext.
        /// </param>
        /// <returns>
        ///   The shared secret.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="ciphertext" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="ciphertext" /> is not the correct size.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>The instance represents only an encapsulation key.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred during decapsulation.</para>
        /// </exception>
        public byte[] Decapsulate(byte[] ciphertext)
        {
            ArgumentNullException.ThrowIfNull(ciphertext);

            if (ciphertext.Length != Algorithm.CiphertextSizeInBytes)
            {
                throw new ArgumentException(SR.Argument_KemInvalidCiphertextLength, nameof(ciphertext));
            }

            ThrowIfDisposed();

            byte[] sharedSecret = new byte[Algorithm.SharedSecretSizeInBytes];
            DecapsulateCore(ciphertext, sharedSecret);
            return sharedSecret;
        }

        /// <summary>
        ///   Decapsulates a shared secret from a provided ciphertext, writing it into the provided buffer.
        /// </summary>
        /// <param name="ciphertext">
        ///   The ciphertext.
        /// </param>
        /// <param name="sharedSecret">
        ///   The buffer to receive the shared secret. Its length must be exactly
        ///   <see cref="CompositeMLKemAlgorithm.SharedSecretSizeInBytes"/>.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <para><paramref name="ciphertext" /> is not the correct size.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="sharedSecret" /> is not the correct size.</para>
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>The instance represents only an encapsulation key.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred during decapsulation.</para>
        /// </exception>
        public void Decapsulate(ReadOnlySpan<byte> ciphertext, Span<byte> sharedSecret)
        {
            if (ciphertext.Length != Algorithm.CiphertextSizeInBytes)
            {
                throw new ArgumentException(SR.Argument_KemInvalidCiphertextLength, nameof(ciphertext));
            }

            if (sharedSecret.Length != Algorithm.SharedSecretSizeInBytes)
            {
                throw new ArgumentException(
                    SR.Format(SR.Argument_DestinationImprecise, Algorithm.SharedSecretSizeInBytes),
                    nameof(sharedSecret));
            }

            ThrowIfDisposed();

            DecapsulateCore(ciphertext, sharedSecret);
        }

        /// <summary>
        ///   When overridden in a derived class, decapsulates a shared secret from a provided ciphertext.
        /// </summary>
        /// <param name="ciphertext">
        ///   The ciphertext, whose length will be exactly <see cref="CompositeMLKemAlgorithm.CiphertextSizeInBytes"/>.
        /// </param>
        /// <param name="sharedSecret">
        ///   The buffer to receive the shared secret, whose length will be exactly
        ///   <see cref="CompositeMLKemAlgorithm.SharedSecretSizeInBytes"/>.
        /// </param>
        /// <exception cref="CryptographicException">
        ///   An error occurred during decapsulation.
        /// </exception>
        protected abstract void DecapsulateCore(ReadOnlySpan<byte> ciphertext, Span<byte> sharedSecret);

        /// <summary>
        ///   Imports a Composite ML-KEM key from an encapsulation key.
        /// </summary>
        /// <param name="algorithm">
        ///   The specific Composite ML-KEM algorithm for this key.
        /// </param>
        /// <param name="source">
        ///   The encapsulation key.
        /// </param>
        /// <returns>
        ///   The imported key.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="algorithm"/> or <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     <paramref name="source"/> length is the wrong size for the specified algorithm.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     An error occurred while importing the key.
        ///   </para>
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support the specified Composite ML-KEM algorithm. Callers can use <see cref="IsAlgorithmSupported" />
        ///   to determine if the algorithm is supported.
        /// </exception>
        public static CompositeMLKem ImportEncapsulationKey(CompositeMLKemAlgorithm algorithm, byte[] source)
        {
            ArgumentNullException.ThrowIfNull(algorithm);
            ArgumentNullException.ThrowIfNull(source);

            return ImportEncapsulationKey(algorithm, new ReadOnlySpan<byte>(source));
        }

        /// <summary>
        ///   Imports a Composite ML-KEM key from an encapsulation key.
        /// </summary>
        /// <param name="algorithm">
        ///   The specific Composite ML-KEM algorithm for this key.
        /// </param>
        /// <param name="source">
        ///   The encapsulation key.
        /// </param>
        /// <returns>
        ///   The imported key.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="algorithm" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     <paramref name="source"/> length is the wrong size for the specified algorithm.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     An error occurred while importing the key.
        ///   </para>
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support the specified Composite ML-KEM algorithm. Callers can use <see cref="IsAlgorithmSupported" />
        ///   to determine if the algorithm is supported.
        /// </exception>
        public static CompositeMLKem ImportEncapsulationKey(CompositeMLKemAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            ArgumentNullException.ThrowIfNull(algorithm);

            if (!algorithm.IsValidEncapsulationKeySize(source.Length))
            {
                throw new CryptographicException(SR.Argument_PublicKeyWrongSizeForAlgorithm);
            }

            ThrowIfNotSupported(algorithm);

            return CompositeMLKemImplementation.ImportEncapsulationKeyImpl(algorithm, source);
        }

        /// <summary>
        ///   Imports a Composite ML-KEM key from a decapsulation key.
        /// </summary>
        /// <param name="algorithm">
        ///   The specific Composite ML-KEM algorithm for this key.
        /// </param>
        /// <param name="source">
        ///   The decapsulation key.
        /// </param>
        /// <returns>
        ///   The imported key.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="algorithm"/> or <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     <paramref name="source"/> length is the wrong size for the specified algorithm.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     An error occurred while importing the key.
        ///   </para>
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support the specified Composite ML-KEM algorithm. Callers can use <see cref="IsAlgorithmSupported" />
        ///   to determine if the algorithm is supported.
        /// </exception>
        public static CompositeMLKem ImportDecapsulationKey(CompositeMLKemAlgorithm algorithm, byte[] source)
        {
            ArgumentNullException.ThrowIfNull(algorithm);
            ArgumentNullException.ThrowIfNull(source);

            return ImportDecapsulationKey(algorithm, new ReadOnlySpan<byte>(source));
        }

        /// <summary>
        ///   Imports a Composite ML-KEM key from a decapsulation key.
        /// </summary>
        /// <param name="algorithm">
        ///   The specific Composite ML-KEM algorithm for this key.
        /// </param>
        /// <param name="source">
        ///   The decapsulation key.
        /// </param>
        /// <returns>
        ///   The imported key.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="algorithm" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     <paramref name="source"/> length is the wrong size for the specified algorithm.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     An error occurred while importing the key.
        ///   </para>
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support the specified Composite ML-KEM algorithm. Callers can use <see cref="IsAlgorithmSupported" />
        ///   to determine if the algorithm is supported.
        /// </exception>
        public static CompositeMLKem ImportDecapsulationKey(CompositeMLKemAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            ArgumentNullException.ThrowIfNull(algorithm);

            if (!algorithm.IsValidDecapsulationKeySize(source.Length))
            {
                throw new CryptographicException(SR.Argument_PrivateKeyWrongSizeForAlgorithm);
            }

            ThrowIfNotSupported(algorithm);

            return CompositeMLKemImplementation.ImportDecapsulationKeyImpl(algorithm, source);
        }

        /// <summary>
        ///   Imports a Composite ML-KEM encapsulation key from an X.509 SubjectPublicKeyInfo structure.
        /// </summary>
        /// <param name="source">
        ///   The bytes of an X.509 SubjectPublicKeyInfo structure in the ASN.1-DER encoding.
        /// </param>
        /// <returns>
        ///   The imported key.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     The contents of <paramref name="source"/> do not represent an ASN.1-DER-encoded X.509 SubjectPublicKeyInfo structure.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The SubjectPublicKeyInfo value does not represent a Composite ML-KEM key.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="source" /> contains trailing data after the ASN.1 structure.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm-specific import failed.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The specified Composite ML-KEM algorithm is not supported.
        ///   </para>
        /// </exception>
        public static CompositeMLKem ImportSubjectPublicKeyInfo(byte[] source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return ImportSubjectPublicKeyInfo(new ReadOnlySpan<byte>(source));
        }

        /// <summary>
        ///   Imports a Composite ML-KEM encapsulation key from an X.509 SubjectPublicKeyInfo structure.
        /// </summary>
        /// <param name="source">
        ///   The bytes of an X.509 SubjectPublicKeyInfo structure in the ASN.1-DER encoding.
        /// </param>
        /// <returns>
        ///   The imported key.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     The contents of <paramref name="source"/> do not represent an ASN.1-DER-encoded X.509 SubjectPublicKeyInfo structure.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The SubjectPublicKeyInfo value does not represent a Composite ML-KEM key.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="source" /> contains trailing data after the ASN.1 structure.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm-specific import failed.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The specified Composite ML-KEM algorithm is not supported.
        ///   </para>
        /// </exception>
        public static CompositeMLKem ImportSubjectPublicKeyInfo(ReadOnlySpan<byte> source)
        {
            Helpers.ThrowIfAsnInvalidLength(source);

            KeyFormatHelper.ReadSubjectPublicKeyInfo(KnownOids, source, SubjectPublicKeyReader, out int read, out CompositeMLKem kem);
            Debug.Assert(read == source.Length);
            return kem;

            static void SubjectPublicKeyReader(ReadOnlySpan<byte> key, in ValueAlgorithmIdentifierAsn identifier, out CompositeMLKem kem)
            {
                CompositeMLKemAlgorithm algorithm = GetAlgorithmIdentifier(in identifier);

                if (!IsAlgorithmSupported(algorithm))
                {
                    throw new CryptographicException(SR.Format(SR.Cryptography_AlgorithmNotSupported, nameof(CompositeMLKem)));
                }

                if (!algorithm.IsValidEncapsulationKeySize(key.Length))
                {
                    throw new CryptographicException(SR.Argument_PublicKeyWrongSizeForAlgorithm);
                }

                kem = CompositeMLKemImplementation.ImportEncapsulationKeyImpl(algorithm, key);
            }
        }

        /// <summary>
        ///   Imports a Composite ML-KEM decapsulation key from a PKCS#8 PrivateKeyInfo structure.
        /// </summary>
        /// <param name="source">
        ///   The bytes of a PKCS#8 PrivateKeyInfo structure in the ASN.1-BER encoding.
        /// </param>
        /// <returns>
        ///   The imported key.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     The contents of <paramref name="source"/> do not represent an ASN.1-BER-encoded PKCS#8 PrivateKeyInfo structure.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The PrivateKeyInfo value does not represent a Composite ML-KEM key.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="source" /> contains trailing data after the ASN.1 structure.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm-specific import failed.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The specified Composite ML-KEM algorithm is not supported.
        ///   </para>
        /// </exception>
        public static CompositeMLKem ImportPkcs8PrivateKey(byte[] source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return ImportPkcs8PrivateKey(new ReadOnlySpan<byte>(source));
        }

        /// <summary>
        ///   Imports a Composite ML-KEM decapsulation key from a PKCS#8 PrivateKeyInfo structure.
        /// </summary>
        /// <param name="source">
        ///   The bytes of a PKCS#8 PrivateKeyInfo structure in the ASN.1-BER encoding.
        /// </param>
        /// <returns>
        ///   The imported key.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     The contents of <paramref name="source"/> do not represent an ASN.1-BER-encoded PKCS#8 PrivateKeyInfo structure.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The PrivateKeyInfo value does not represent a Composite ML-KEM key.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="source" /> contains trailing data after the ASN.1 structure.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm-specific import failed.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The specified Composite ML-KEM algorithm is not supported.
        ///   </para>
        /// </exception>
        public static CompositeMLKem ImportPkcs8PrivateKey(ReadOnlySpan<byte> source)
        {
            Helpers.ThrowIfAsnInvalidLength(source);

            KeyFormatHelper.ReadPkcs8(KnownOids, source, PrivateKeyReader, out int read, out CompositeMLKem kem);
            Debug.Assert(read == source.Length);
            return kem;

            static void PrivateKeyReader(
                ReadOnlySpan<byte> privateKeyContents,
                in ValueAlgorithmIdentifierAsn algorithmIdentifier,
                out CompositeMLKem kem)
            {
                CompositeMLKemAlgorithm algorithm = GetAlgorithmIdentifier(in algorithmIdentifier);

                if (!IsAlgorithmSupported(algorithm))
                {
                    throw new CryptographicException(SR.Format(SR.Cryptography_AlgorithmNotSupported, nameof(CompositeMLKem)));
                }

                if (!algorithm.IsValidDecapsulationKeySize(privateKeyContents.Length))
                {
                    throw new CryptographicException(SR.Argument_PrivateKeyWrongSizeForAlgorithm);
                }

                kem = CompositeMLKemImplementation.ImportDecapsulationKeyImpl(algorithm, privateKeyContents);
            }
        }

        /// <summary>
        ///   Imports a Composite ML-KEM decapsulation key from a PKCS#8 EncryptedPrivateKeyInfo structure.
        /// </summary>
        /// <param name="password">
        ///   The password to use when decrypting the key material.
        /// </param>
        /// <param name="source">
        ///   The bytes of a PKCS#8 EncryptedPrivateKeyInfo structure in the ASN.1-BER encoding.
        /// </param>
        /// <returns>
        ///   The imported key.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="password" /> or <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     The contents of <paramref name="source"/> do not represent an ASN.1-BER-encoded PKCS#8 EncryptedPrivateKeyInfo structure.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The specified password is incorrect.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The value does not represent a Composite ML-KEM key.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="source" /> contains trailing data after the ASN.1 structure.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm-specific import failed.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The specified Composite ML-KEM algorithm is not supported.
        ///   </para>
        /// </exception>
        public static CompositeMLKem ImportEncryptedPkcs8PrivateKey(string password, byte[] source)
        {
            ArgumentNullException.ThrowIfNull(password);
            ArgumentNullException.ThrowIfNull(source);

            return ImportEncryptedPkcs8PrivateKey(password.AsSpan(), new ReadOnlySpan<byte>(source));
        }

        /// <summary>
        ///   Imports a Composite ML-KEM decapsulation key from a PKCS#8 EncryptedPrivateKeyInfo structure.
        /// </summary>
        /// <param name="password">
        ///   The password to use when decrypting the key material.
        /// </param>
        /// <param name="source">
        ///   The bytes of a PKCS#8 EncryptedPrivateKeyInfo structure in the ASN.1-BER encoding.
        /// </param>
        /// <returns>
        ///   The imported key.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     The contents of <paramref name="source"/> do not represent an ASN.1-BER-encoded PKCS#8 EncryptedPrivateKeyInfo structure.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The specified password is incorrect.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The value does not represent a Composite ML-KEM key.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="source" /> contains trailing data after the ASN.1 structure.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm-specific import failed.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The specified Composite ML-KEM algorithm is not supported.
        ///   </para>
        /// </exception>
        public static CompositeMLKem ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, ReadOnlySpan<byte> source)
        {
            Helpers.ThrowIfAsnInvalidLength(source);

            return KeyFormatHelper.DecryptPkcs8(
                password,
                source,
                ImportPkcs8PrivateKey,
                out _);
        }

        /// <summary>
        ///   Imports a Composite ML-KEM decapsulation key from a PKCS#8 EncryptedPrivateKeyInfo structure.
        /// </summary>
        /// <param name="passwordBytes">
        ///   The bytes to use as a password when decrypting the key material.
        /// </param>
        /// <param name="source">
        ///   The bytes of a PKCS#8 EncryptedPrivateKeyInfo structure in the ASN.1-BER encoding.
        /// </param>
        /// <returns>
        ///   The imported key.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     The contents of <paramref name="source"/> do not represent an ASN.1-BER-encoded PKCS#8 EncryptedPrivateKeyInfo structure.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The specified password is incorrect.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The EncryptedPrivateKeyInfo indicates the Key Derivation Function (KDF) to apply is the legacy PKCS#12 KDF,
        ///     which requires <see cref="char"/>-based passwords.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The value does not represent a Composite ML-KEM key.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="source" /> contains trailing data after the ASN.1 structure.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm-specific import failed.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The specified Composite ML-KEM algorithm is not supported.
        ///   </para>
        /// </exception>
        public static CompositeMLKem ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, ReadOnlySpan<byte> source)
        {
            Helpers.ThrowIfAsnInvalidLength(source);

            return KeyFormatHelper.DecryptPkcs8(
                passwordBytes,
                source,
                ImportPkcs8PrivateKey,
                out _);
        }

        /// <summary>
        ///   Imports a Composite ML-KEM key from an RFC 7468 PEM-encoded string.
        /// </summary>
        /// <param name="source">
        ///   The text of the PEM key to import.
        /// </param>
        /// <returns>
        ///   The imported Composite ML-KEM key.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para><paramref name="source" /> contains an encrypted PEM-encoded key.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="source" /> contains multiple PEM-encoded Composite ML-KEM keys.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="source" /> contains no PEM-encoded Composite ML-KEM keys.</para>
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     The contents of the PEM-encoded key do not represent an ASN.1-BER encoded PKCS#8 PrivateKeyInfo structure,
        ///     or an ASN.1-DER encoded X.509 SubjectPublicKeyInfo structure.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The key is not a Composite ML-KEM key.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm-specific key import failed.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The specified Composite ML-KEM algorithm is not supported.
        ///   </para>
        /// </exception>
        /// <remarks>
        ///   <para>
        ///     Unsupported or malformed PEM-encoded objects will be ignored. If multiple supported PEM labels
        ///     are found, an exception is raised to prevent importing a key when the key is ambiguous.
        ///   </para>
        ///   <para>
        ///     This method supports the following PEM labels:
        ///     <list type="bullet">
        ///       <item><description>PUBLIC KEY</description></item>
        ///       <item><description>PRIVATE KEY</description></item>
        ///     </list>
        ///   </para>
        /// </remarks>
        public static CompositeMLKem ImportFromPem(string source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return ImportFromPem(source.AsSpan());
        }

        /// <summary>
        ///   Imports a Composite ML-KEM key from an RFC 7468 PEM-encoded string.
        /// </summary>
        /// <param name="source">
        ///   The text of the PEM key to import.
        /// </param>
        /// <returns>
        ///   The imported Composite ML-KEM key.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <para><paramref name="source" /> contains an encrypted PEM-encoded key.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="source" /> contains multiple PEM-encoded Composite ML-KEM keys.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="source" /> contains no PEM-encoded Composite ML-KEM keys.</para>
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     The contents of the PEM-encoded key do not represent an ASN.1-BER encoded PKCS#8 PrivateKeyInfo structure,
        ///     or an ASN.1-DER encoded X.509 SubjectPublicKeyInfo structure.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The key is not a Composite ML-KEM key.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm-specific key import failed.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The specified Composite ML-KEM algorithm is not supported.
        ///   </para>
        /// </exception>
        /// <remarks>
        ///   <para>
        ///     Unsupported or malformed PEM-encoded objects will be ignored. If multiple supported PEM labels
        ///     are found, an exception is raised to prevent importing a key when the key is ambiguous.
        ///   </para>
        ///   <para>
        ///     This method supports the following PEM labels:
        ///     <list type="bullet">
        ///       <item><description>PUBLIC KEY</description></item>
        ///       <item><description>PRIVATE KEY</description></item>
        ///     </list>
        ///   </para>
        /// </remarks>
        public static CompositeMLKem ImportFromPem(ReadOnlySpan<char> source)
        {
            return PemKeyHelpers.ImportFactoryPem<CompositeMLKem>(source, label =>
                label switch
                {
                    PemLabels.Pkcs8PrivateKey => ImportPkcs8PrivateKey,
                    PemLabels.SpkiPublicKey => ImportSubjectPublicKeyInfo,
                    _ => null,
                });
        }

        /// <summary>
        ///   Imports a Composite ML-KEM key from an encrypted RFC 7468 PEM-encoded string.
        /// </summary>
        /// <param name="source">
        ///   The PEM text of the encrypted key to import.
        /// </param>
        /// <param name="password">
        ///   The password to use for decrypting the key material.
        /// </param>
        /// <returns>
        ///   The imported Composite ML-KEM key.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> or <paramref name="password" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///     <paramref name="source"/> does not contain a PEM-encoded key with a recognized label.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="source"/> contains multiple PEM-encoded keys with a recognized label.
        ///   </para>
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     The password is incorrect.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The base-64 decoded contents of the PEM text from <paramref name="source" />
        ///     do not represent an ASN.1-BER-encoded PKCS#8 EncryptedPrivateKeyInfo structure.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The base-64 decoded contents of the PEM text from <paramref name="source" />
        ///     represent the key in a format that is not supported.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     An error occurred while importing the key.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The specified Composite ML-KEM algorithm is not supported.
        ///   </para>
        /// </exception>
        /// <remarks>
        ///   <para>
        ///     When the base-64 decoded contents of <paramref name="source" /> indicate an algorithm that uses PBKDF1
        ///     (Password-Based Key Derivation Function 1) or PBKDF2 (Password-Based Key Derivation Function 2),
        ///     the password is converted to bytes via the UTF-8 encoding.
        ///   </para>
        ///   <para>
        ///     Unsupported or malformed PEM-encoded objects will be ignored. If multiple supported PEM labels
        ///     are found, an exception is thrown to prevent importing a key when
        ///     the key is ambiguous.
        ///   </para>
        ///   <para>This method supports the <c>ENCRYPTED PRIVATE KEY</c> PEM label.</para>
        /// </remarks>
        public static CompositeMLKem ImportFromEncryptedPem(string source, string password)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(password);

            return ImportFromEncryptedPem(source.AsSpan(), password.AsSpan());
        }

        /// <summary>
        ///   Imports a Composite ML-KEM key from an encrypted RFC 7468 PEM-encoded string.
        /// </summary>
        /// <param name="source">
        ///   The PEM text of the encrypted key to import.
        /// </param>
        /// <param name="password">
        ///   The password to use for decrypting the key material.
        /// </param>
        /// <returns>
        ///   The imported Composite ML-KEM key.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///     <paramref name="source"/> does not contain a PEM-encoded key with a recognized label.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="source"/> contains multiple PEM-encoded keys with a recognized label.
        ///   </para>
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     The password is incorrect.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The base-64 decoded contents of the PEM text from <paramref name="source" />
        ///     do not represent an ASN.1-BER-encoded PKCS#8 EncryptedPrivateKeyInfo structure.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The base-64 decoded contents of the PEM text from <paramref name="source" />
        ///     represent the key in a format that is not supported.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     An error occurred while importing the key.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The specified Composite ML-KEM algorithm is not supported.
        ///   </para>
        /// </exception>
        /// <remarks>
        ///   <para>
        ///     When the base-64 decoded contents of <paramref name="source" /> indicate an algorithm that uses PBKDF1
        ///     (Password-Based Key Derivation Function 1) or PBKDF2 (Password-Based Key Derivation Function 2),
        ///     the password is converted to bytes via the UTF-8 encoding.
        ///   </para>
        ///   <para>
        ///     Unsupported or malformed PEM-encoded objects will be ignored. If multiple supported PEM labels
        ///     are found, an exception is thrown to prevent importing a key when
        ///     the key is ambiguous.
        ///   </para>
        ///   <para>This method supports the <c>ENCRYPTED PRIVATE KEY</c> PEM label.</para>
        /// </remarks>
        public static CompositeMLKem ImportFromEncryptedPem(ReadOnlySpan<char> source, ReadOnlySpan<char> password)
        {
            return PemKeyHelpers.ImportEncryptedFactoryPem<CompositeMLKem, char>(
                source,
                password,
                ImportEncryptedPkcs8PrivateKey);
        }

        /// <summary>
        ///   Imports a Composite ML-KEM key from an encrypted RFC 7468 PEM-encoded string.
        /// </summary>
        /// <param name="source">
        ///   The PEM text of the encrypted key to import.
        /// </param>
        /// <param name="passwordBytes">
        ///   The bytes to use as a password when decrypting the key material.
        /// </param>
        /// <returns>
        ///   The imported Composite ML-KEM key.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> or <paramref name="passwordBytes" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///     <paramref name="source"/> does not contain a PEM-encoded key with a recognized label.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="source"/> contains multiple PEM-encoded keys with a recognized label.
        ///   </para>
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     The password is incorrect.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The base-64 decoded contents of the PEM text from <paramref name="source" />
        ///     do not represent an ASN.1-BER-encoded PKCS#8 EncryptedPrivateKeyInfo structure.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The base-64 decoded contents of the PEM text from <paramref name="source" />
        ///     represent the key in a format that is not supported.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     An error occurred while importing the key.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The specified Composite ML-KEM algorithm is not supported.
        ///   </para>
        /// </exception>
        /// <remarks>
        ///   <para>
        ///     Unsupported or malformed PEM-encoded objects will be ignored. If multiple supported PEM labels
        ///     are found, an exception is thrown to prevent importing a key when
        ///     the key is ambiguous.
        ///   </para>
        ///   <para>This method supports the <c>ENCRYPTED PRIVATE KEY</c> PEM label.</para>
        /// </remarks>
        public static CompositeMLKem ImportFromEncryptedPem(string source, byte[] passwordBytes)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(passwordBytes);

            return ImportFromEncryptedPem(source.AsSpan(), new ReadOnlySpan<byte>(passwordBytes));
        }

        /// <summary>
        ///   Imports a Composite ML-KEM key from an encrypted RFC 7468 PEM-encoded string.
        /// </summary>
        /// <param name="source">
        ///   The PEM text of the encrypted key to import.
        /// </param>
        /// <param name="passwordBytes">
        ///   The bytes to use as a password when decrypting the key material.
        /// </param>
        /// <returns>
        ///   The imported Composite ML-KEM key.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///     <paramref name="source"/> does not contain a PEM-encoded key with a recognized label.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="source"/> contains multiple PEM-encoded keys with a recognized label.
        ///   </para>
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     The password is incorrect.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The base-64 decoded contents of the PEM text from <paramref name="source" />
        ///     do not represent an ASN.1-BER-encoded PKCS#8 EncryptedPrivateKeyInfo structure.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The base-64 decoded contents of the PEM text from <paramref name="source" />
        ///     represent the key in a format that is not supported.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     An error occurred while importing the key.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The specified Composite ML-KEM algorithm is not supported.
        ///   </para>
        /// </exception>
        /// <remarks>
        ///   <para>
        ///     Unsupported or malformed PEM-encoded objects will be ignored. If multiple supported PEM labels
        ///     are found, an exception is thrown to prevent importing a key when
        ///     the key is ambiguous.
        ///   </para>
        ///   <para>This method supports the <c>ENCRYPTED PRIVATE KEY</c> PEM label.</para>
        /// </remarks>
        public static CompositeMLKem ImportFromEncryptedPem(ReadOnlySpan<char> source, ReadOnlySpan<byte> passwordBytes)
        {
            return PemKeyHelpers.ImportEncryptedFactoryPem<CompositeMLKem, byte>(
                source,
                passwordBytes,
                ImportEncryptedPkcs8PrivateKey);
        }

        /// <summary>
        ///   Exports the current key in a PEM-encoded representation of the PKCS#8 EncryptedPrivateKeyInfo
        ///   representation of this key, using a string password.
        /// </summary>
        /// <param name="password">
        ///   The password to use when encrypting the key material.
        /// </param>
        /// <param name="pbeParameters">
        ///   The password-based encryption (PBE) parameters to use when encrypting the key material.
        /// </param>
        /// <returns>
        ///   A string containing the PEM-encoded PKCS#8 EncryptedPrivateKeyInfo.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="password"/> or <paramref name="pbeParameters"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para><paramref name="pbeParameters"/> does not represent a valid password-based encryption algorithm.</para>
        ///   <para>-or-</para>
        ///   <para>This instance only represents an encapsulation key.</para>
        ///   <para>-or-</para>
        ///   <para>The decapsulation key is not exportable.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        public string ExportEncryptedPkcs8PrivateKeyPem(string password, PbeParameters pbeParameters)
        {
            ArgumentNullException.ThrowIfNull(password);

            return ExportEncryptedPkcs8PrivateKeyPem(password.AsSpan(), pbeParameters);
        }

        /// <summary>
        ///   Exports the current key in a PEM-encoded representation of the PKCS#8 EncryptedPrivateKeyInfo
        ///   representation of this key, using a char-based password.
        /// </summary>
        /// <param name="password">
        ///   The password to use when encrypting the key material.
        /// </param>
        /// <param name="pbeParameters">
        ///   The password-based encryption (PBE) parameters to use when encrypting the key material.
        /// </param>
        /// <returns>
        ///   A string containing the PEM-encoded PKCS#8 EncryptedPrivateKeyInfo.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="pbeParameters"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para><paramref name="pbeParameters"/> does not represent a valid password-based encryption algorithm.</para>
        ///   <para>-or-</para>
        ///   <para>This instance only represents an encapsulation key.</para>
        ///   <para>-or-</para>
        ///   <para>The decapsulation key is not exportable.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        public string ExportEncryptedPkcs8PrivateKeyPem(ReadOnlySpan<char> password, PbeParameters pbeParameters)
        {
            ArgumentNullException.ThrowIfNull(pbeParameters);
            PasswordBasedEncryption.ValidatePbeParameters(pbeParameters, password, ReadOnlySpan<byte>.Empty);
            ThrowIfDisposed();

            AsnWriter writer = WriteEncryptedPkcs8PrivateKeyToAsnWriter(password, pbeParameters);

            // Skip clear since the data is already encrypted.
            return Helpers.EncodeAsnWriterToPem(PemLabels.EncryptedPkcs8PrivateKey, writer, clear: false);
        }

        /// <summary>
        ///   Exports the current key in a PEM-encoded representation of the PKCS#8 EncryptedPrivateKeyInfo
        ///   representation of this key, using a byte-based password.
        /// </summary>
        /// <param name="passwordBytes">
        ///   The bytes to use as a password when encrypting the key material.
        /// </param>
        /// <param name="pbeParameters">
        ///   The password-based encryption (PBE) parameters to use when encrypting the key material.
        /// </param>
        /// <returns>
        ///   A string containing the PEM-encoded PKCS#8 EncryptedPrivateKeyInfo.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="pbeParameters"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para><paramref name="pbeParameters"/> specifies a KDF that requires a char-based password.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="pbeParameters"/> does not represent a valid password-based encryption algorithm.</para>
        ///   <para>-or-</para>
        ///   <para>This instance only represents an encapsulation key.</para>
        ///   <para>-or-</para>
        ///   <para>The decapsulation key is not exportable.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        public string ExportEncryptedPkcs8PrivateKeyPem(ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters)
        {
            ArgumentNullException.ThrowIfNull(pbeParameters);
            PasswordBasedEncryption.ValidatePbeParameters(pbeParameters, ReadOnlySpan<char>.Empty, passwordBytes);
            ThrowIfDisposed();

            AsnWriter writer = WriteEncryptedPkcs8PrivateKeyToAsnWriter(passwordBytes, pbeParameters);

            // Skip clear since the data is already encrypted.
            return Helpers.EncodeAsnWriterToPem(PemLabels.EncryptedPkcs8PrivateKey, writer, clear: false);
        }

        /// <summary>
        ///   Exports the current key in the PKCS#8 EncryptedPrivateKeyInfo format with a char-based password.
        /// </summary>
        /// <param name="password">
        ///   The password to use when encrypting the key material.
        /// </param>
        /// <param name="pbeParameters">
        ///   The password-based encryption (PBE) parameters to use when encrypting the key material.
        /// </param>
        /// <returns>
        ///   A byte array containing the PKCS#8 EncryptedPrivateKeyInfo representation of this key.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="password"/> or <paramref name="pbeParameters"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para><paramref name="pbeParameters"/> does not represent a valid password-based encryption algorithm.</para>
        ///   <para>-or-</para>
        ///   <para>This instance only represents an encapsulation key.</para>
        ///   <para>-or-</para>
        ///   <para>The decapsulation key is not exportable.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        public byte[] ExportEncryptedPkcs8PrivateKey(string password, PbeParameters pbeParameters)
        {
            ArgumentNullException.ThrowIfNull(password);

            return ExportEncryptedPkcs8PrivateKey(password.AsSpan(), pbeParameters);
        }

        /// <summary>
        ///   Exports the current key in the PKCS#8 EncryptedPrivateKeyInfo format with a char-based password.
        /// </summary>
        /// <param name="password">
        ///   The password to use when encrypting the key material.
        /// </param>
        /// <param name="pbeParameters">
        ///   The password-based encryption (PBE) parameters to use when encrypting the key material.
        /// </param>
        /// <returns>
        ///   A byte array containing the PKCS#8 EncryptedPrivateKeyInfo representation of this key.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="pbeParameters"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para><paramref name="pbeParameters"/> does not represent a valid password-based encryption algorithm.</para>
        ///   <para>-or-</para>
        ///   <para>This instance only represents an encapsulation key.</para>
        ///   <para>-or-</para>
        ///   <para>The decapsulation key is not exportable.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        public byte[] ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, PbeParameters pbeParameters)
        {
            ArgumentNullException.ThrowIfNull(pbeParameters);
            PasswordBasedEncryption.ValidatePbeParameters(pbeParameters, password, ReadOnlySpan<byte>.Empty);
            ThrowIfDisposed();

            AsnWriter writer = WriteEncryptedPkcs8PrivateKeyToAsnWriter(password, pbeParameters);

            try
            {
                return writer.Encode();
            }
            finally
            {
                writer.Reset();
            }
        }

        /// <summary>
        ///   Exports the current key in the PKCS#8 EncryptedPrivateKeyInfo format with a byte-based password.
        /// </summary>
        /// <param name="passwordBytes">
        ///   The bytes to use as a password when encrypting the key material.
        /// </param>
        /// <param name="pbeParameters">
        ///   The password-based encryption (PBE) parameters to use when encrypting the key material.
        /// </param>
        /// <returns>
        ///   A byte array containing the PKCS#8 EncryptedPrivateKeyInfo representation of this key.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="pbeParameters"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para><paramref name="pbeParameters"/> specifies a KDF that requires a char-based password.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="pbeParameters"/> does not represent a valid password-based encryption algorithm.</para>
        ///   <para>-or-</para>
        ///   <para>This instance only represents an encapsulation key.</para>
        ///   <para>-or-</para>
        ///   <para>The decapsulation key is not exportable.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        public byte[] ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters)
        {
            ArgumentNullException.ThrowIfNull(pbeParameters);
            PasswordBasedEncryption.ValidatePbeParameters(pbeParameters, ReadOnlySpan<char>.Empty, passwordBytes);
            ThrowIfDisposed();

            AsnWriter writer = WriteEncryptedPkcs8PrivateKeyToAsnWriter(passwordBytes, pbeParameters);

            try
            {
                return writer.Encode();
            }
            finally
            {
                writer.Reset();
            }
        }

        /// <summary>
        ///   Attempts to export the current key in the PKCS#8 EncryptedPrivateKeyInfo format into a provided buffer,
        ///   using a char-based password.
        /// </summary>
        /// <param name="password">
        ///   The password to use when encrypting the key material.
        /// </param>
        /// <param name="pbeParameters">
        ///   The password-based encryption (PBE) parameters to use when encrypting the key material.
        /// </param>
        /// <param name="destination">
        ///   The buffer to receive the PKCS#8 EncryptedPrivateKeyInfo value.
        /// </param>
        /// <param name="bytesWritten">
        ///   When this method returns, contains the number of bytes written to the <paramref name="destination"/> buffer.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <returns>
        ///   <see langword="true" /> if <paramref name="destination"/> was large enough to hold the result;
        ///   otherwise, <see langword="false" />.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="password"/> or <paramref name="pbeParameters"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para><paramref name="pbeParameters"/> does not represent a valid password-based encryption algorithm.</para>
        ///   <para>-or-</para>
        ///   <para>This instance only represents an encapsulation key.</para>
        ///   <para>-or-</para>
        ///   <para>The decapsulation key is not exportable.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        public bool TryExportEncryptedPkcs8PrivateKey(
            string password,
            PbeParameters pbeParameters,
            Span<byte> destination,
            out int bytesWritten)
        {
            ArgumentNullException.ThrowIfNull(password);

            return TryExportEncryptedPkcs8PrivateKey(password.AsSpan(), pbeParameters, destination, out bytesWritten);
        }

        /// <summary>
        ///   Attempts to export the current key in the PKCS#8 EncryptedPrivateKeyInfo format into a provided buffer,
        ///   using a char-based password.
        /// </summary>
        /// <param name="password">
        ///   The password to use when encrypting the key material.
        /// </param>
        /// <param name="pbeParameters">
        ///   The password-based encryption (PBE) parameters to use when encrypting the key material.
        /// </param>
        /// <param name="destination">
        ///   The buffer to receive the PKCS#8 EncryptedPrivateKeyInfo value.
        /// </param>
        /// <param name="bytesWritten">
        ///   When this method returns, contains the number of bytes written to the <paramref name="destination"/> buffer.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <returns>
        ///   <see langword="true" /> if <paramref name="destination"/> was large enough to hold the result;
        ///   otherwise, <see langword="false" />.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="pbeParameters"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para><paramref name="pbeParameters"/> does not represent a valid password-based encryption algorithm.</para>
        ///   <para>-or-</para>
        ///   <para>This instance only represents an encapsulation key.</para>
        ///   <para>-or-</para>
        ///   <para>The decapsulation key is not exportable.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        public bool TryExportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<char> password,
            PbeParameters pbeParameters,
            Span<byte> destination,
            out int bytesWritten)
        {
            ArgumentNullException.ThrowIfNull(pbeParameters);
            PasswordBasedEncryption.ValidatePbeParameters(pbeParameters, password, ReadOnlySpan<byte>.Empty);
            ThrowIfDisposed();

            AsnWriter writer = WriteEncryptedPkcs8PrivateKeyToAsnWriter(password, pbeParameters);

            try
            {
                return writer.TryEncode(destination, out bytesWritten);
            }
            finally
            {
                writer.Reset();
            }
        }

        /// <summary>
        ///   Attempts to export the current key in the PKCS#8 EncryptedPrivateKeyInfo format into a provided buffer,
        ///   using a byte-based password.
        /// </summary>
        /// <param name="passwordBytes">
        ///   The bytes to use as a password when encrypting the key material.
        /// </param>
        /// <param name="pbeParameters">
        ///   The password-based encryption (PBE) parameters to use when encrypting the key material.
        /// </param>
        /// <param name="destination">
        ///   The buffer to receive the PKCS#8 EncryptedPrivateKeyInfo value.
        /// </param>
        /// <param name="bytesWritten">
        ///   When this method returns, contains the number of bytes written to the <paramref name="destination"/> buffer.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <returns>
        ///   <see langword="true" /> if <paramref name="destination"/> was large enough to hold the result;
        ///   otherwise, <see langword="false" />.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="pbeParameters"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para><paramref name="pbeParameters"/> specifies a KDF that requires a char-based password.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="pbeParameters"/> does not represent a valid password-based encryption algorithm.</para>
        ///   <para>-or-</para>
        ///   <para>This instance only represents an encapsulation key.</para>
        ///   <para>-or-</para>
        ///   <para>The decapsulation key is not exportable.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        public bool TryExportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<byte> passwordBytes,
            PbeParameters pbeParameters,
            Span<byte> destination,
            out int bytesWritten)
        {
            ArgumentNullException.ThrowIfNull(pbeParameters);
            PasswordBasedEncryption.ValidatePbeParameters(pbeParameters, ReadOnlySpan<char>.Empty, passwordBytes);
            ThrowIfDisposed();

            AsnWriter writer = WriteEncryptedPkcs8PrivateKeyToAsnWriter(passwordBytes, pbeParameters);

            try
            {
                return writer.TryEncode(destination, out bytesWritten);
            }
            finally
            {
                writer.Reset();
            }
        }

        /// <summary>
        ///   Exports the current key in a PEM-encoded representation of the PKCS#8 PrivateKeyInfo format.
        /// </summary>
        /// <returns>
        ///   A string containing the PEM-encoded representation of the PKCS#8 PrivateKeyInfo.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>This instance only represents an encapsulation key.</para>
        ///   <para>-or-</para>
        ///   <para>The decapsulation key is not exportable.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        public string ExportPkcs8PrivateKeyPem()
        {
            ThrowIfDisposed();

            return ExportPkcs8PrivateKeyCallback(static pkcs8 => PemEncoding.WriteString(PemLabels.Pkcs8PrivateKey, pkcs8));
        }

        /// <summary>
        ///   Exports the current key in the PKCS#8 PrivateKeyInfo format.
        /// </summary>
        /// <returns>
        ///   A byte array containing the PKCS#8 PrivateKeyInfo representation of this key.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>This instance only represents an encapsulation key.</para>
        ///   <para>-or-</para>
        ///   <para>The decapsulation key is not exportable.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        public byte[] ExportPkcs8PrivateKey()
        {
            ThrowIfDisposed();

            return ExportPkcs8PrivateKeyCallback(static pkcs8 => pkcs8.ToArray());
        }

        /// <summary>
        ///   Attempts to export the current key in the PKCS#8 PrivateKeyInfo format
        ///   into the provided buffer.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the PKCS#8 PrivateKeyInfo value.
        /// </param>
        /// <param name="bytesWritten">
        ///   When this method returns, contains the number of bytes written to the <paramref name="destination"/> buffer.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <returns>
        ///   <see langword="true" /> if <paramref name="destination"/> was large enough to hold the result;
        ///   otherwise, <see langword="false" />.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>This instance only represents an encapsulation key.</para>
        ///   <para>-or-</para>
        ///   <para>The decapsulation key is not exportable.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        public bool TryExportPkcs8PrivateKey(Span<byte> destination, out int bytesWritten)
        {
            ThrowIfDisposed();

            // The bound can be tightened but decapsulation key length of some traditional algorithms
            // can vary and aren't worth the complex calculation.
            int minimumPossiblePkcs8Key = Algorithm.MinDecapsulationKeySizeInBytes;

            if (destination.Length < minimumPossiblePkcs8Key)
            {
                bytesWritten = 0;
                return false;
            }

            return TryExportPkcs8PrivateKeyCore(destination, out bytesWritten);
        }

        /// <summary>
        ///   When overridden in a derived class, attempts to export the current key in the PKCS#8 PrivateKeyInfo format
        ///   into the provided buffer.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the PKCS#8 PrivateKeyInfo value.
        /// </param>
        /// <param name="bytesWritten">
        ///   When this method returns, contains the number of bytes written to the <paramref name="destination"/> buffer.
        /// </param>
        /// <returns>
        ///   <see langword="true" /> if <paramref name="destination"/> was large enough to hold the result;
        ///   otherwise, <see langword="false" />.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   An error occurred while exporting the key.
        /// </exception>
        protected abstract bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten);

        /// <summary>
        ///   Exports the encapsulation key portion of the current key in a PEM-encoded representation of
        ///   the X.509 SubjectPublicKeyInfo format.
        /// </summary>
        /// <returns>
        ///   A string containing the PEM-encoded representation of the X.509 SubjectPublicKeyInfo
        ///   representation of the encapsulation key portion of this key.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while exporting the key.
        /// </exception>
        public string ExportSubjectPublicKeyInfoPem()
        {
            ThrowIfDisposed();

            AsnWriter writer = WriteSubjectPublicKeyToAsnWriter();

            // SPKI does not contain sensitive data.
            return Helpers.EncodeAsnWriterToPem(PemLabels.SpkiPublicKey, writer, clear: false);
        }

        /// <summary>
        ///   Exports the encapsulation key portion of the current key in the X.509 SubjectPublicKeyInfo format.
        /// </summary>
        /// <returns>
        ///   A byte array containing the X.509 SubjectPublicKeyInfo representation of the encapsulation key portion
        ///   of this key.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while exporting the key.
        /// </exception>
        public byte[] ExportSubjectPublicKeyInfo()
        {
            ThrowIfDisposed();

            return WriteSubjectPublicKeyToAsnWriter().Encode();
        }

        /// <summary>
        ///   Attempts to export the encapsulation key portion of the current key in the X.509 SubjectPublicKeyInfo format
        ///   into the provided buffer.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the X.509 SubjectPublicKeyInfo value.
        /// </param>
        /// <param name="bytesWritten">
        ///   When this method returns, contains the number of bytes written to the <paramref name="destination"/> buffer.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <returns>
        ///   <see langword="true" /> if <paramref name="destination"/> was large enough to hold the result;
        ///   otherwise, <see langword="false" />.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while exporting the key.
        /// </exception>
        public bool TryExportSubjectPublicKeyInfo(Span<byte> destination, out int bytesWritten)
        {
            ThrowIfDisposed();

            AsnWriter writer = WriteSubjectPublicKeyToAsnWriter();
            return writer.TryEncode(destination, out bytesWritten);
        }

        /// <summary>
        ///   Exports the encapsulation key.
        /// </summary>
        /// <returns>
        ///   The encapsulation key.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while exporting the key.
        /// </exception>
        public byte[] ExportEncapsulationKey()
        {
            ThrowIfDisposed();

            byte[] encapsulationKey = new byte[Algorithm.MaxEncapsulationKeySizeInBytes];

            if (!TryExportEncapsulationKey(encapsulationKey, out int bytesWritten))
            {
                Debug.Fail("Max sized buffer was not large enough.");
                throw new CryptographicException();
            }

            if (bytesWritten < encapsulationKey.Length)
            {
                Array.Resize(ref encapsulationKey, bytesWritten);
            }

            return encapsulationKey;
        }

        /// <summary>
        ///   Exports the encapsulation key into the provided buffer.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the encapsulation key.
        /// </param>
        /// <returns>
        ///   The number of bytes written to the <paramref name="destination"/> buffer.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para><paramref name="destination"/> was not large enough to hold the result.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        public int ExportEncapsulationKey(Span<byte> destination)
        {
            ThrowIfDisposed();

            if (destination.Length < Algorithm.MinEncapsulationKeySizeInBytes)
            {
                throw new CryptographicException(SR.Argument_DestinationTooShort);
            }

            if (!TryExportEncapsulationKey(destination, out int bytesWritten))
            {
                throw new CryptographicException(SR.Argument_DestinationTooShort);
            }

            return bytesWritten;
        }

        /// <summary>
        ///   Attempts to export the encapsulation key into the provided buffer.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the encapsulation key.
        /// </param>
        /// <param name="bytesWritten">
        ///   When this method returns, contains the number of bytes written to the <paramref name="destination"/> buffer.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <returns>
        ///   <see langword="true" /> if <paramref name="destination"/> was large enough to hold the result;
        ///   otherwise, <see langword="false" />.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while exporting the key.
        /// </exception>
        public bool TryExportEncapsulationKey(Span<byte> destination, out int bytesWritten)
        {
            ThrowIfDisposed();

            if (destination.Length < Algorithm.MinEncapsulationKeySizeInBytes)
            {
                bytesWritten = 0;
                return false;
            }

            using (CryptoPoolLease lease = CryptoPoolLease.RentConditionally(Algorithm.MaxEncapsulationKeySizeInBytes, destination, skipClear: true))
            {
                int localBytesWritten = ExportEncapsulationKeyCore(lease.Span);

                if (!Algorithm.IsValidEncapsulationKeySize(localBytesWritten))
                {
                    bytesWritten = 0;
                    throw new CryptographicException();
                }

                if (lease.IsRented)
                {
                    if (localBytesWritten > destination.Length)
                    {
                        bytesWritten = 0;
                        return false;
                    }

                    lease.Span.Slice(0, localBytesWritten).CopyTo(destination);
                }

                bytesWritten = localBytesWritten;
                return true;
            }
        }

        /// <summary>
        ///   When overridden in a derived class, exports the encapsulation key into the provided buffer.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the encapsulation key.
        /// </param>
        /// <returns>
        ///   The number of bytes written to the <paramref name="destination"/> buffer.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   An error occurred while exporting the key.
        /// </exception>
        protected abstract int ExportEncapsulationKeyCore(Span<byte> destination);

        /// <summary>
        ///   Exports the decapsulation key.
        /// </summary>
        /// <returns>
        ///   The decapsulation key.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>The current instance cannot export a decapsulation key.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        public byte[] ExportDecapsulationKey()
        {
            ThrowIfDisposed();

            byte[] decapsulationKey = new byte[Algorithm.MaxDecapsulationKeySizeInBytes];

            if (!TryExportDecapsulationKey(decapsulationKey, out int bytesWritten))
            {
                Debug.Fail("Max sized buffer was not large enough.");
                throw new CryptographicException();
            }

            if (bytesWritten < decapsulationKey.Length)
            {
                byte[] temp = new byte[bytesWritten];
                Array.Copy(decapsulationKey, temp, bytesWritten);
                CryptographicOperations.ZeroMemory(decapsulationKey);
                decapsulationKey = temp;
            }

            return decapsulationKey;
        }

        /// <summary>
        ///   Exports the decapsulation key into the provided buffer.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the decapsulation key.
        /// </param>
        /// <returns>
        ///   The number of bytes written to the <paramref name="destination"/> buffer.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para><paramref name="destination"/> was not large enough to hold the result.</para>
        ///   <para>-or-</para>
        ///   <para>The current instance cannot export a decapsulation key.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        public int ExportDecapsulationKey(Span<byte> destination)
        {
            ThrowIfDisposed();

            if (destination.Length < Algorithm.MinDecapsulationKeySizeInBytes)
            {
                throw new CryptographicException(SR.Argument_DestinationTooShort);
            }

            if (!TryExportDecapsulationKey(destination, out int bytesWritten))
            {
                throw new CryptographicException(SR.Argument_DestinationTooShort);
            }

            return bytesWritten;
        }

        /// <summary>
        ///   Attempts to export the decapsulation key into the provided buffer.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the decapsulation key.
        /// </param>
        /// <param name="bytesWritten">
        ///   When this method returns, contains the number of bytes written to the <paramref name="destination"/> buffer.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <returns>
        ///   <see langword="true" /> if <paramref name="destination"/> was large enough to hold the result;
        ///   otherwise, <see langword="false" />.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>The current instance cannot export a decapsulation key.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        public bool TryExportDecapsulationKey(Span<byte> destination, out int bytesWritten)
        {
            ThrowIfDisposed();

            if (destination.Length < Algorithm.MinDecapsulationKeySizeInBytes)
            {
                bytesWritten = 0;
                return false;
            }

            using (CryptoPoolLease lease = CryptoPoolLease.RentConditionally(Algorithm.MaxDecapsulationKeySizeInBytes, destination, skipClearIfNotRented: true))
            {
                int localBytesWritten = ExportDecapsulationKeyCore(lease.Span);

                if (!Algorithm.IsValidDecapsulationKeySize(localBytesWritten))
                {
                    if (!lease.IsRented)
                    {
                        CryptographicOperations.ZeroMemory(destination);
                    }

                    bytesWritten = 0;
                    throw new CryptographicException();
                }

                if (lease.IsRented)
                {
                    if (localBytesWritten > destination.Length)
                    {
                        bytesWritten = 0;
                        return false;
                    }

                    lease.Span.Slice(0, localBytesWritten).CopyTo(destination);
                }

                bytesWritten = localBytesWritten;
                return true;
            }
        }

        /// <summary>
        ///   When overridden in a derived class, exports the decapsulation key into the provided buffer.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the decapsulation key.
        /// </param>
        /// <returns>
        ///   The number of bytes written to the <paramref name="destination"/> buffer.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   <para>The current instance cannot export a decapsulation key.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        protected abstract int ExportDecapsulationKeyCore(Span<byte> destination);

        /// <summary>
        ///   Releases all resources used by the <see cref="CompositeMLKem"/> class.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        ///   Called by the <see cref="Dispose()" /> method to release the managed and unmanaged
        ///   resources used by the current instance of the <see cref="CompositeMLKem"/> class.
        /// </summary>
        /// <param name="disposing">
        ///   <see langword="true" /> to release managed and unmanaged resources;
        ///   <see langword="false" /> to release only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
        }

        private protected bool TryExportPkcs8FromExportedDecapsulationKey(Span<byte> destination, out int bytesWritten)
        {
            AsnWriter? writer = null;

            try
            {
                using (CryptoPoolLease lease = CryptoPoolLease.Rent(Algorithm.MaxDecapsulationKeySizeInBytes))
                {
                    int decapsulationKeySize = ExportDecapsulationKeyCore(lease.Span);

                    if (!Algorithm.IsValidDecapsulationKeySize(decapsulationKeySize))
                    {
                        bytesWritten = 0;
                        throw new CryptographicException(SR.Argument_PrivateKeyWrongSizeForAlgorithm);
                    }

                    int initialCapacity = checked(32 + decapsulationKeySize);
                    writer = new AsnWriter(AsnEncodingRules.DER, initialCapacity);

                    using (writer.PushSequence())
                    {
                        writer.WriteInteger(0);

                        using (writer.PushSequence())
                        {
                            writer.WriteObjectIdentifier(Algorithm.Oid);
                        }

                        writer.WriteOctetString(lease.Span.Slice(0, decapsulationKeySize));
                    }

                    Debug.Assert(writer.GetEncodedLength() <= initialCapacity);
                    return writer.TryEncode(destination, out bytesWritten);
                }
            }
            finally
            {
                writer?.Reset();
            }
        }

        private AsnWriter WriteEncryptedPkcs8PrivateKeyToAsnWriter(ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters)
        {
            AsnWriter? tmp = null;

            try
            {
                tmp = WritePkcs8ToAsnWriter();
                return KeyFormatHelper.WriteEncryptedPkcs8(passwordBytes, tmp, pbeParameters);
            }
            finally
            {
                tmp?.Reset();
            }
        }

        private AsnWriter WriteEncryptedPkcs8PrivateKeyToAsnWriter(ReadOnlySpan<char> password, PbeParameters pbeParameters)
        {
            AsnWriter? tmp = null;

            try
            {
                tmp = WritePkcs8ToAsnWriter();
                return KeyFormatHelper.WriteEncryptedPkcs8(password, tmp, pbeParameters);
            }
            finally
            {
                tmp?.Reset();
            }
        }

        private AsnWriter WritePkcs8ToAsnWriter()
        {
            return ExportPkcs8PrivateKeyCallback(static pkcs8 =>
            {
                AsnWriter writer = new(AsnEncodingRules.BER, initialCapacity: pkcs8.Length);

                try
                {
                    writer.WriteEncodedValueForCrypto(pkcs8);
                }
                catch
                {
                    writer.Reset();
                    throw;
                }

                return writer;
            });
        }

        private AsnWriter WriteSubjectPublicKeyToAsnWriter()
        {
            byte[] buffer = new byte[Algorithm.MaxEncapsulationKeySizeInBytes];
            int written = ExportEncapsulationKeyCore(buffer);

            if (!Algorithm.IsValidEncapsulationKeySize(written))
            {
                throw new CryptographicException();
            }

            ReadOnlySpan<byte> encapsulationKey = buffer.AsSpan(0, written);

            // The ASN.1 overhead of a SubjectPublicKeyInfo encoding an encapsulation key is around 24 bytes.
            // Round it off to 32. This checked operation should never throw because the inputs are not
            // user provided.
            int capacity = checked(32 + encapsulationKey.Length);
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER, capacity);

            using (writer.PushSequence())
            {
                using (writer.PushSequence())
                {
                    writer.WriteObjectIdentifier(Algorithm.Oid);
                }

                writer.WriteBitString(encapsulationKey);
            }

            Debug.Assert(writer.GetEncodedLength() <= capacity);
            return writer;
        }

        private TResult ExportPkcs8PrivateKeyCallback<TResult>(ExportPkcs8PrivateKeyFunc<TResult> func)
        {
            // A Composite ML-KEM PKCS#8 private key has at most 23 bytes of ASN.1 overhead, assuming no attributes.
            // Make it an even 32 to provide a good starting point for the buffer size.
            int size = checked(Algorithm.MaxDecapsulationKeySizeInBytes + 32);
            CryptoPoolLease lease = CryptoPoolLease.Rent(size);

            try
            {
                int written;

                while (!TryExportPkcs8PrivateKeyCore(lease.Span, out written))
                {
                    size = checked(size * 2);
                    lease.Dispose();

                    // Dispose is idempotent, so even if this Rent fails,
                    // we won't corrupt the pool during cleanup.
                    lease = CryptoPoolLease.Rent(size);
                }

                if ((uint)written > (uint)lease.Span.Length)
                {
                    throw new CryptographicException();
                }

                return func(lease.Span.Slice(0, written));
            }
            finally
            {
                lease.Dispose();
            }
        }

        private static CompositeMLKemAlgorithm GetAlgorithmIdentifier(ref readonly ValueAlgorithmIdentifierAsn identifier)
        {
            CompositeMLKemAlgorithm? algorithm = CompositeMLKemAlgorithm.GetAlgorithmFromOid(identifier.Algorithm);
            Debug.Assert(algorithm is not null, "Algorithm identifier should have been pre-validated by KeyFormatHelper.");

            if (identifier.HasParameters)
            {
                throw Helpers.CreateAlgorithmUnknownException(in identifier);
            }

            return algorithm;
        }

        private static void ThrowIfNotSupported(CompositeMLKemAlgorithm algorithm)
        {
            if (!IsAlgorithmSupported(algorithm))
            {
                throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_AlgorithmNotSupported, nameof(CompositeMLKem)));
            }
        }

        private protected void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, typeof(CompositeMLKem));
    }
}
