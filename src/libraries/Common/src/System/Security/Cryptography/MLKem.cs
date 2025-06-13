// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.Asn1;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    /// <summary>
    ///   Represents an ML-KEM key.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This algorithm is specified by FIPS-203.
    ///   </para>
    ///   <para>
    ///     Developers are encouraged to program against the <c>MLKem</c> base class,
    ///     rather than any specific derived class.
    ///     The derived classes are intended for interop with the underlying system
    ///     cryptographic libraries.
    ///   </para>
    /// </remarks>
    [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    public abstract class MLKem : IDisposable
    {
        private static readonly string[] s_knownOids = [Oids.MlKem512, Oids.MlKem768, Oids.MlKem1024];

        private bool _disposed;

        /// <summary>
        ///   Gets a value that indicates whether the algorithm is supported on the current platform.
        /// </summary>
        /// <value>
        ///   <see langword="true" /> if the algorithm is supported; otherwise, <see langword="false" />.
        /// </value>
        public static bool IsSupported => MLKemImplementation.IsSupported;

        /// <summary>
        ///   Gets the algorithm of the current instance.
        /// </summary>
        /// <value>
        ///   A value representing the ML-KEM algorithm.
        /// </value>
        public MLKemAlgorithm Algorithm { get; }

        /// <summary>
        ///   Initializes a new instance of the <see cref="MLKem" /> class.
        /// </summary>
        /// <param name="algorithm">
        ///   The specific ML-KEM algorithm for this key.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="algorithm" /> is <see langword="null" />.
        /// </exception>
        protected MLKem(MLKemAlgorithm algorithm)
        {
            ArgumentNullException.ThrowIfNull(algorithm);
            Algorithm = algorithm;
        }

        /// <summary>
        ///   Generates a new ML-KEM key.
        /// </summary>
        /// <param name="algorithm">
        ///   An algorithm identifying what kind of ML-KEM key to generate.
        /// </param>
        /// <returns>
        ///   The generated key.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="algorithm" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred generating the ML-KEM key.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support ML-KEM. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports ML-KEM.
        /// </exception>
        public static MLKem GenerateKey(MLKemAlgorithm algorithm)
        {
            ArgumentNullException.ThrowIfNull(algorithm);
            ThrowIfNotSupported();
            return MLKemImplementation.GenerateKeyImpl(algorithm);
        }

        /// <summary>
        ///   Creates an encapsulation ciphertext and shared secret, writing them into the provided buffers.
        /// </summary>
        /// <param name="ciphertext">
        ///   The buffer to receive the ciphertext.
        /// </param>
        /// <param name="sharedSecret">
        ///   The buffer to receive the shared secret.
        /// </param>
        /// <exception cref="CryptographicException">
        ///   <para>An error occurred during encapsulation.</para>
        ///   <para>-or -</para>
        ///   <para><paramref name="ciphertext"/> overlaps with <paramref name="sharedSecret"/>.</para>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para><paramref name="ciphertext" /> is not the correct size.</para>
        ///   <para> -or- </para>
        ///   <para><paramref name="sharedSecret" /> is not the correct size.</para>
        /// </exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
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
        ///   Creates an encapsulation ciphertext and shared secret.
        /// </summary>
        /// <param name="ciphertext">
        ///   When this method returns, the ciphertext.
        /// </param>
        /// <param name="sharedSecret">
        ///   When this method returns, the shared secret.
        /// </param>
        /// <exception cref="CryptographicException">
        ///   <para>An error occurred during encapsulation.</para>
        /// </exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
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
        ///   When overridden in a derived class, creates an encapsulation ciphertext and shared secret, writing them
        ///   into the provided buffers.
        /// </summary>
        /// <param name="ciphertext">
        ///   The buffer to receive the ciphertext.
        /// </param>
        /// <param name="sharedSecret">
        ///   The buffer to receive the shared secret.
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
        /// <param name="sharedSecret">
        ///   The buffer to receive the shared secret.
        /// </param>
        /// <exception cref="CryptographicException">
        ///   An error occurred during decapsulation.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para><paramref name="ciphertext" /> is not the correct size.</para>
        ///   <para> -or- </para>
        ///   <para><paramref name="sharedSecret" /> is not the correct size.</para>
        /// </exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
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
        ///   Decapsulates a shared secret from a provided ciphertext.
        /// </summary>
        /// <param name="ciphertext">
        ///   The ciphertext.
        /// </param>
        /// <returns>
        ///   The shared secret.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   An error occurred during decapsulation.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="ciphertext" /> is not the correct size.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="ciphertext" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        public byte[] Decapsulate(byte[] ciphertext)
        {
            ArgumentNullException.ThrowIfNull(ciphertext);

            if (ciphertext.Length != Algorithm.CiphertextSizeInBytes)
                throw new ArgumentException(SR.Argument_KemInvalidCiphertextLength, nameof(ciphertext));

            ThrowIfDisposed();

            byte[] sharedSecret = new byte[Algorithm.SharedSecretSizeInBytes];
            DecapsulateCore(ciphertext, sharedSecret);
            return sharedSecret;
        }

        /// <summary>
        ///   When overridden in a derived class, decapsulates a shared secret from a provided ciphertext.
        /// </summary>
        /// <param name="ciphertext">
        ///   The ciphertext.
        /// </param>
        /// <param name="sharedSecret">
        ///   The buffer to receive the shared secret.
        /// </param>
        /// <exception cref="CryptographicException">
        ///   An error occurred during decapsulation.
        /// </exception>
        protected abstract void DecapsulateCore(ReadOnlySpan<byte> ciphertext, Span<byte> sharedSecret);

        /// <summary>
        ///   Exports the private seed into the provided buffer.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the private seed.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="destination"/> is the incorrect length to receive the private seed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>The current instance cannot export a seed.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        public void ExportPrivateSeed(Span<byte> destination)
        {
            if (destination.Length != Algorithm.PrivateSeedSizeInBytes)
            {
                throw new ArgumentException(
                    SR.Format(SR.Argument_DestinationImprecise, Algorithm.PrivateSeedSizeInBytes),
                    nameof(destination));
            }

            ThrowIfDisposed();
            ExportPrivateSeedCore(destination);
        }

        /// <summary>
        ///   Exports the private seed.
        /// </summary>
        /// <returns>
        ///   The private seed.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   <para>The current instance cannot export a seed.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        public byte[] ExportPrivateSeed()
        {
            ThrowIfDisposed();
            byte[] seed = new byte[Algorithm.PrivateSeedSizeInBytes];
            ExportPrivateSeedCore(seed);
            return seed;
        }

        /// <summary>
        ///   When overridden in a derived class, exports the private seed into the provided buffer.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the private seed.
        /// </param>
        protected abstract void ExportPrivateSeedCore(Span<byte> destination);

        /// <summary>
        /// Imports an ML-KEM key from its private seed value.
        /// </summary>
        /// <param name="algorithm">The specific ML-KEM algorithm for this key.</param>
        /// <param name="source">The private seed.</param>
        /// <returns>The imported key.</returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="source"/> has a length that is not the
        ///   <see cref="MLKemAlgorithm.PrivateSeedSizeInBytes" /> from <paramref name="algorithm" />.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="algorithm" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while importing the key.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support ML-KEM. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports ML-KEM.
        /// </exception>
        public static MLKem ImportPrivateSeed(MLKemAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            ArgumentNullException.ThrowIfNull(algorithm);

            if (source.Length != algorithm.PrivateSeedSizeInBytes)
                throw new ArgumentException(SR.Argument_KemInvalidSeedLength, nameof(source));

            ThrowIfNotSupported();
            return MLKemImplementation.ImportPrivateSeedImpl(algorithm, source);
        }

        /// <summary>
        /// Imports an ML-KEM key from its private seed value.
        /// </summary>
        /// <param name="algorithm">The specific ML-KEM algorithm for this key.</param>
        /// <param name="source">The private seed.</param>
        /// <returns>The imported key.</returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="source"/> has a length that is not the
        ///   <see cref="MLKemAlgorithm.PrivateSeedSizeInBytes" /> from <paramref name="algorithm" />.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <para><paramref name="algorithm" /> is <see langword="null" />.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="source" /> is <see langword="null" />.</para>
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while importing the key.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support ML-KEM. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports ML-KEM.
        /// </exception>
        public static MLKem ImportPrivateSeed(MLKemAlgorithm algorithm, byte[] source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return ImportPrivateSeed(algorithm, new ReadOnlySpan<byte>(source));
        }

        /// <summary>
        /// Imports an ML-KEM key from a decapsulation key.
        /// </summary>
        /// <param name="algorithm">The specific ML-KEM algorithm for this key.</param>
        /// <param name="source">The decapsulation key.</param>
        /// <returns>The imported key.</returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="source"/> has a length that is not valid for the ML-KEM algorithm.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="algorithm" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while importing the key.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support ML-KEM. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports ML-KEM.
        /// </exception>
        public static MLKem ImportDecapsulationKey(MLKemAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            ArgumentNullException.ThrowIfNull(algorithm);

            if (source.Length != algorithm.DecapsulationKeySizeInBytes)
                throw new ArgumentException(SR.Argument_KemInvalidDecapsulationKeyLength, nameof(source));

            ThrowIfNotSupported();
            return MLKemImplementation.ImportDecapsulationKeyImpl(algorithm, source);
        }

        /// <summary>
        /// Imports an ML-KEM key from a decapsulation key.
        /// </summary>
        /// <param name="algorithm">The specific ML-KEM algorithm for this key.</param>
        /// <param name="source">The decapsulation key.</param>
        /// <returns>The imported key.</returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="source"/> has a length that is not valid for the ML-KEM algorithm.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <para><paramref name="algorithm" /> is <see langword="null" />.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="source" /> is <see langword="null" />.</para>
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while importing the key.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support ML-KEM. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports ML-KEM.
        /// </exception>
        public static MLKem ImportDecapsulationKey(MLKemAlgorithm algorithm, byte[] source)
        {
            ArgumentNullException.ThrowIfNull(source);
            return ImportDecapsulationKey(algorithm, new ReadOnlySpan<byte>(source));
        }

        /// <summary>
        /// Imports an ML-KEM key from a encapsulation key.
        /// </summary>
        /// <param name="algorithm">The specific ML-KEM algorithm for this key.</param>
        /// <param name="source">The encapsulation key.</param>
        /// <returns>The imported key.</returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="source"/> has a length that is not valid for the ML-KEM algorithm.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="algorithm" /> is <see langword="null" />
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while importing the key.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support ML-KEM. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports ML-KEM.
        /// </exception>
        public static MLKem ImportEncapsulationKey(MLKemAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            ArgumentNullException.ThrowIfNull(algorithm);

            if (source.Length != algorithm.EncapsulationKeySizeInBytes)
                throw new ArgumentException(SR.Argument_KemInvalidEncapsulationKeyLength, nameof(source));

            ThrowIfNotSupported();
            return MLKemImplementation.ImportEncapsulationKeyImpl(algorithm, source);
        }

        /// <summary>
        /// Imports an ML-KEM key from a encapsulation key.
        /// </summary>
        /// <param name="algorithm">The specific ML-KEM algorithm for this key.</param>
        /// <param name="source">The encapsulation key.</param>
        /// <returns>The imported key.</returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="source"/> has a length that is not valid for the ML-KEM algorithm.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <para><paramref name="algorithm" /> is <see langword="null" />.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="source" /> is <see langword="null" />.</para>
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while importing the key.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support ML-KEM. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports ML-KEM.
        /// </exception>
        public static MLKem ImportEncapsulationKey(MLKemAlgorithm algorithm, byte[] source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return ImportEncapsulationKey(algorithm, new ReadOnlySpan<byte>(source));
        }

        /// <summary>
        ///   Exports the decapsulation key into the provided buffer.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the decapsulation key.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="destination"/> is the incorrect length to receive the decapsulation key.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>The current instance cannot export a decapsulation key.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while importing the key.</para>
        /// </exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        public void ExportDecapsulationKey(Span<byte> destination)
        {
            if (destination.Length != Algorithm.DecapsulationKeySizeInBytes)
            {
                throw new ArgumentException(
                    SR.Format(SR.Argument_DestinationImprecise, Algorithm.DecapsulationKeySizeInBytes),
                    nameof(destination));
            }

            ThrowIfDisposed();
            ExportDecapsulationKeyCore(destination);
        }

        /// <summary>
        ///   Exports the decapsulation key.
        /// </summary>
        /// <returns>
        ///   The decapsulation key.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   <para>The current instance cannot export a decapsulation key.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while importing the key.</para>
        /// </exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        public byte[] ExportDecapsulationKey()
        {
            ThrowIfDisposed();
            byte[] decapsulationKey = new byte[Algorithm.DecapsulationKeySizeInBytes];
            ExportDecapsulationKeyCore(decapsulationKey);
            return decapsulationKey;
        }

        /// <summary>
        ///   When overridden in a derived class, exports the decapsulation key into the provided buffer.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the decapsulation key.
        /// </param>
        protected abstract void ExportDecapsulationKeyCore(Span<byte> destination);

        /// <summary>
        ///   Exports the encapsulation key into the provided buffer.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the encapsulation key.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="destination"/> is the incorrect length to receive the encapsulation key.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred exporting the encapsulation key.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        public void ExportEncapsulationKey(Span<byte> destination)
        {
            if (destination.Length != Algorithm.EncapsulationKeySizeInBytes)
            {
                throw new ArgumentException(
                    SR.Format(SR.Argument_DestinationImprecise, Algorithm.EncapsulationKeySizeInBytes),
                    nameof(destination));
            }

            ThrowIfDisposed();
            ExportEncapsulationKeyCore(destination);
        }

        /// <summary>
        ///   Exports the encapsulation key.
        /// </summary>
        /// <returns>
        ///   The encapsulation key.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   An error occurred exporting the encapsulation key.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        public byte[] ExportEncapsulationKey()
        {
            ThrowIfDisposed();
            byte[] encapsulationKey = new byte[Algorithm.EncapsulationKeySizeInBytes];
            ExportEncapsulationKeyCore(encapsulationKey);
            return encapsulationKey;
        }

        /// <summary>
        ///   When overridden in a derived class, exports the encapsulation key into the provided buffer.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the encapsulation key.
        /// </param>
        protected abstract void ExportEncapsulationKeyCore(Span<byte> destination);

        /// <summary>
        ///   Attempts to export the public-key portion of the current key in the X.509 SubjectPublicKeyInfo format
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
            return ExportSubjectPublicKeyInfoCore().TryEncode(destination, out bytesWritten);
        }

        /// <summary>
        ///   Exports the public-key portion of the current key in the X.509 SubjectPublicKeyInfo format.
        /// </summary>
        /// <returns>
        ///   A byte array containing the X.509 SubjectPublicKeyInfo representation of the public-key portion of this key.
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
            return ExportSubjectPublicKeyInfoCore().Encode();
        }

        /// <summary>
        ///   Exports the public-key portion of the current key in a PEM-encoded representation of
        ///   the X.509 SubjectPublicKeyInfo format.
        /// </summary>
        /// <returns>
        ///   A string containing the PEM-encoded representation of the X.509 SubjectPublicKeyInfo
        ///   representation of the public-key portion of this key.
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
            AsnWriter writer = ExportSubjectPublicKeyInfoCore();
            // SPKI does not contain sensitive data.
            return EncodeAsnWriterToPem(PemLabels.SpkiPublicKey, writer, clear: false);
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
        ///   An error occurred while exporting the key.
        /// </exception>
        public bool TryExportPkcs8PrivateKey(Span<byte> destination, out int bytesWritten)
        {
            ThrowIfDisposed();

            // An ML-KEM-512 "seed" export with no attributes is 86 bytes. A buffer smaller than that cannot hold a
            // PKCS#8 encoded key. If we happen to get a buffer smaller than that, it won't export.
            const int MinimumPossiblePkcs8MLKemKey = 86;

            if (destination.Length < MinimumPossiblePkcs8MLKemKey)
            {
                bytesWritten = 0;
                return false;
            }

            return TryExportPkcs8PrivateKeyCore(destination, out bytesWritten);
        }

        /// <summary>
        ///   Export the current key in the PKCS#8 PrivateKeyInfo format.
        /// </summary>
        /// <returns>
        ///   A byte array containing the PKCS#8 PrivateKeyInfo representation of this key.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while exporting the key.
        /// </exception>
        public byte[] ExportPkcs8PrivateKey()
        {
            ThrowIfDisposed();
            return ExportPkcs8PrivateKeyCallback(static pkcs8 => pkcs8.ToArray());
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
        ///   An error occurred while exporting the key.
        /// </exception>
        public string ExportPkcs8PrivateKeyPem()
        {
            ThrowIfDisposed();
            return ExportPkcs8PrivateKeyCallback(static pkcs8 => PemEncoding.WriteString(PemLabels.Pkcs8PrivateKey, pkcs8));
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
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while exporting the key.
        /// </exception>
        protected abstract bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten);

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
        ///    <paramref name="pbeParameters"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>This instance only represents a public key.</para>
        ///   <para>-or-</para>
        ///   <para>The private key is not exportable.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="pbeParameters"/> does not represent a valid password-based encryption algorithm.</para>
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

            AsnWriter writer = ExportEncryptedPkcs8PrivateKeyCore<char>(
                password,
                pbeParameters,
                KeyFormatHelper.WriteEncryptedPkcs8);
            return writer.TryEncode(destination, out bytesWritten);
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
        ///    <paramref name="password"/> or <paramref name="pbeParameters"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>This instance only represents a public key.</para>
        ///   <para>-or-</para>
        ///   <para>The private key is not exportable.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="pbeParameters"/> does not represent a valid password-based encryption algorithm.</para>
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
        ///   using a byte-based password.
        /// </summary>
        /// <param name="passwordBytes">
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
        ///    <paramref name="pbeParameters"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>This instance only represents a public key.</para>
        ///   <para>-or-</para>
        ///   <para>The private key is not exportable.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="pbeParameters"/> does not represent a valid password-based encryption algorithm.</para>
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

            AsnWriter writer = ExportEncryptedPkcs8PrivateKeyCore<byte>(
                passwordBytes,
                pbeParameters,
                KeyFormatHelper.WriteEncryptedPkcs8);
            return writer.TryEncode(destination, out bytesWritten);
        }

        /// <summary>
        ///   Exports the current key in the PKCS#8 EncryptedPrivateKeyInfo format with a byte-based password.
        /// </summary>
        /// <param name="passwordBytes">
        ///   The password to use when encrypting the key material.
        /// </param>
        /// <param name="pbeParameters">
        ///   The password-based encryption (PBE) parameters to use when encrypting the key material.
        /// </param>
        /// <returns>
        ///   A byte array containing the PKCS#8 EncryptedPrivateKeyInfo representation of this key.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///    <paramref name="pbeParameters"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>This instance only represents a public key.</para>
        ///   <para>-or-</para>
        ///   <para>The private key is not exportable.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="pbeParameters"/> does not represent a valid password-based encryption algorithm.</para>
        /// </exception>
        public byte[] ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters)
        {
            ArgumentNullException.ThrowIfNull(pbeParameters);
            PasswordBasedEncryption.ValidatePbeParameters(pbeParameters, ReadOnlySpan<char>.Empty, passwordBytes);
            ThrowIfDisposed();

            AsnWriter writer = ExportEncryptedPkcs8PrivateKeyCore<byte>(
                passwordBytes,
                pbeParameters,
                KeyFormatHelper.WriteEncryptedPkcs8);
            return writer.Encode();
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
        ///    <paramref name="pbeParameters"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>This instance only represents a public key.</para>
        ///   <para>-or-</para>
        ///   <para>The private key is not exportable.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="pbeParameters"/> does not represent a valid password-based encryption algorithm.</para>
        /// </exception>
        public byte[] ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, PbeParameters pbeParameters)
        {
            ArgumentNullException.ThrowIfNull(pbeParameters);
            PasswordBasedEncryption.ValidatePbeParameters(pbeParameters, password, ReadOnlySpan<byte>.Empty);
            ThrowIfDisposed();

            AsnWriter writer = ExportEncryptedPkcs8PrivateKeyCore<char>(
                password,
                pbeParameters,
                KeyFormatHelper.WriteEncryptedPkcs8);
            return writer.Encode();
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
        ///    <paramref name="pbeParameters" /> or <paramref name="password" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>This instance only represents a public key.</para>
        ///   <para>-or-</para>
        ///   <para>The private key is not exportable.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="pbeParameters"/> does not represent a valid password-based encryption algorithm.</para>
        /// </exception>
        public byte[] ExportEncryptedPkcs8PrivateKey(string password, PbeParameters pbeParameters)
        {
            ArgumentNullException.ThrowIfNull(password);
            return ExportEncryptedPkcs8PrivateKey(password.AsSpan(), pbeParameters);
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
        ///   <para>This instance only represents a public key.</para>
        ///   <para>-or-</para>
        ///   <para>The private key is not exportable.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        public string ExportEncryptedPkcs8PrivateKeyPem(ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters)
        {
            ArgumentNullException.ThrowIfNull(pbeParameters);
            PasswordBasedEncryption.ValidatePbeParameters(pbeParameters, ReadOnlySpan<char>.Empty, passwordBytes);
            ThrowIfDisposed();

            AsnWriter writer = ExportEncryptedPkcs8PrivateKeyCore<byte>(
                passwordBytes,
                pbeParameters,
                KeyFormatHelper.WriteEncryptedPkcs8);

            // Skip clear since the data is already encrypted.
            return EncodeAsnWriterToPem(PemLabels.EncryptedPkcs8PrivateKey, writer, clear: false);
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
        ///    <paramref name="pbeParameters"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>This instance only represents a public key.</para>
        ///   <para>-or-</para>
        ///   <para>The private key is not exportable.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        public string ExportEncryptedPkcs8PrivateKeyPem(ReadOnlySpan<char> password, PbeParameters pbeParameters)
        {
            ArgumentNullException.ThrowIfNull(pbeParameters);
            PasswordBasedEncryption.ValidatePbeParameters(pbeParameters, password, ReadOnlySpan<byte>.Empty);
            ThrowIfDisposed();

            AsnWriter writer = ExportEncryptedPkcs8PrivateKeyCore<char>(
                password,
                pbeParameters,
                KeyFormatHelper.WriteEncryptedPkcs8);

            // Skip clear since the data is already encrypted.
            return EncodeAsnWriterToPem(PemLabels.EncryptedPkcs8PrivateKey, writer, clear: false);
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
        ///    <paramref name="password"/> or <paramref name="pbeParameters"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>This instance only represents a public key.</para>
        ///   <para>-or-</para>
        ///   <para>The private key is not exportable.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        public string ExportEncryptedPkcs8PrivateKeyPem(string password, PbeParameters pbeParameters)
        {
            ArgumentNullException.ThrowIfNull(password);
            return ExportEncryptedPkcs8PrivateKeyPem(password.AsSpan(), pbeParameters);
        }

        /// <summary>
        ///   Imports an ML-KEM encapsulation key from an X.509 SubjectPublicKeyInfo structure.
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
        ///     The SubjectPublicKeyInfo value does not represent an ML-KEM key.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm-specific import failed.
        ///   </para>
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support ML-KEM. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports ML-KEM.
        /// </exception>
        public static MLKem ImportSubjectPublicKeyInfo(ReadOnlySpan<byte> source)
        {
            ThrowIfTrailingData(source);
            ThrowIfNotSupported();

            KeyFormatHelper.ReadSubjectPublicKeyInfo(s_knownOids, source, SubjectPublicKeyReader, out int read, out MLKem kem);
            Debug.Assert(read == source.Length);
            return kem;

            static void SubjectPublicKeyReader(ReadOnlyMemory<byte> key, in AlgorithmIdentifierAsn identifier, out MLKem kem)
            {
                MLKemAlgorithm algorithm = GetAlgorithmIdentifier(in identifier);

                if (key.Length != algorithm.EncapsulationKeySizeInBytes)
                {
                    throw new CryptographicException(SR.Argument_KemInvalidEncapsulationKeyLength);
                }

                kem = MLKemImplementation.ImportEncapsulationKeyImpl(algorithm, key.Span);
            }
        }

        /// <inheritdoc cref="ImportSubjectPublicKeyInfo(ReadOnlySpan{byte})" />
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />
        /// </exception>
        public static MLKem ImportSubjectPublicKeyInfo(byte[] source)
        {
            ArgumentNullException.ThrowIfNull(source);
            return ImportSubjectPublicKeyInfo(new ReadOnlySpan<byte>(source));
        }

        /// <summary>
        ///   Imports an ML-KEM private key from a PKCS#8 PrivateKeyInfo structure.
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
        ///     The PrivateKeyInfo value does not represent an ML-KEM key.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="source" /> contains trailing data after the ASN.1 structure.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm-specific import failed.
        ///   </para>
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support ML-KEM. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports ML-KEM.
        /// </exception>
        public static MLKem ImportPkcs8PrivateKey(ReadOnlySpan<byte> source)
        {
            ThrowIfTrailingData(source);
            ThrowIfNotSupported();

            KeyFormatHelper.ReadPkcs8(s_knownOids, source, MLKemKeyReader, out int read, out MLKem kem);
            Debug.Assert(read == source.Length);
            return kem;
        }

        /// <inheritdoc cref="ImportPkcs8PrivateKey(ReadOnlySpan{byte})" />
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />
        /// </exception>
        public static MLKem ImportPkcs8PrivateKey(byte[] source)
        {
            ArgumentNullException.ThrowIfNull(source);
            return ImportPkcs8PrivateKey(new ReadOnlySpan<byte>(source));
        }

        /// <summary>
        ///   Imports an ML-KEM private key from a PKCS#8 EncryptedPrivateKeyInfo structure.
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
        ///     The value does not represent an ML-KEM key.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm-specific import failed.
        ///   </para>
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support ML-KEM. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports ML-KEM.
        /// </exception>
        public static MLKem ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, ReadOnlySpan<byte> source)
        {
            ThrowIfTrailingData(source);
            ThrowIfNotSupported();

            return KeyFormatHelper.DecryptPkcs8(
                passwordBytes,
                source,
                ImportPkcs8PrivateKey,
                out _);
        }

        /// <summary>
        ///   Imports an ML-KEM private key from a PKCS#8 EncryptedPrivateKeyInfo structure.
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
        ///     The value does not represent an ML-KEM key.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm-specific import failed.
        ///   </para>
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support ML-KEM. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports ML-KEM.
        /// </exception>
        public static MLKem ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, ReadOnlySpan<byte> source)
        {
            ThrowIfTrailingData(source);
            ThrowIfNotSupported();

            return KeyFormatHelper.DecryptPkcs8(
                password,
                source,
                ImportPkcs8PrivateKey,
                out _);
        }

        /// <summary>
        ///   Imports an ML-KEM private key from a PKCS#8 EncryptedPrivateKeyInfo structure.
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
        ///     The value does not represent an ML-KEM key.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm-specific import failed.
        ///   </para>
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support ML-KEM. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports ML-KEM.
        /// </exception>
        public static MLKem ImportEncryptedPkcs8PrivateKey(string password, byte[] source)
        {
            ArgumentNullException.ThrowIfNull(password);
            ArgumentNullException.ThrowIfNull(source);
            ThrowIfTrailingData(source);
            ThrowIfNotSupported();

            return KeyFormatHelper.DecryptPkcs8(
                password,
                source,
                ImportPkcs8PrivateKey,
                out _);
        }

        /// <summary>
        ///   Imports an ML-KEM key from an RFC 7468 PEM-encoded string.
        /// </summary>
        /// <param name="source">
        ///   The text of the PEM key to import.
        /// </param>
        /// <returns>
        ///   The imported ML-KEM key.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <para><paramref name="source" /> contains an encrypted PEM-encoded key.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="source" /> contains multiple PEM-encoded ML-KEM keys.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="source" /> contains no PEM-encoded ML-KEM keys.</para>
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while importing the key.
        /// </exception>
        /// <remarks>
        ///   <para>
        ///   Unsupported or malformed PEM-encoded objects will be ignored. If multiple supported PEM labels
        ///   are found, an exception is raised to prevent importing a key when the key is ambiguous.
        ///   </para>
        ///   <para>
        ///   This method supports the following PEM labels:
        ///   <list type="bullet">
        ///     <item><description>PUBLIC KEY</description></item>
        ///     <item><description>PRIVATE KEY</description></item>
        ///   </list>
        ///   </para>
        /// </remarks>
        public static MLKem ImportFromPem(ReadOnlySpan<char> source)
        {
            ThrowIfNotSupported();

            return PemKeyHelpers.ImportFactoryPem<MLKem>(source, label =>
                label switch
                {
                    PemLabels.Pkcs8PrivateKey => ImportPkcs8PrivateKey,
                    PemLabels.SpkiPublicKey => ImportSubjectPublicKeyInfo,
                    _ => null,
                });
        }

        /// <inheritdoc cref="ImportFromPem(ReadOnlySpan{char})" />
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />
        /// </exception>
        public static MLKem ImportFromPem(string source)
        {
            ArgumentNullException.ThrowIfNull(source);
            return ImportFromPem(source.AsSpan());
        }

        /// <summary>
        ///   Imports an ML-KEM key from an encrypted RFC 7468 PEM-encoded string.
        /// </summary>
        /// <param name="source">
        ///   The PEM text of the encrypted key to import.</param>
        /// <param name="password">
        ///   The password to use for decrypting the key material.
        /// </param>
        /// <exception cref="ArgumentException">
        /// <para>
        ///   <paramref name="source"/> does not contain a PEM-encoded key with a recognized label.
        /// </para>
        /// <para>-or-</para>
        /// <para>
        ///   <paramref name="source"/> contains multiple PEM-encoded keys with a recognized label.
        /// </para>
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///   The password is incorrect.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///   The base-64 decoded contents of the PEM text from <paramref name="source" />
        ///   do not represent an ASN.1-BER-encoded PKCS#8 EncryptedPrivateKeyInfo structure.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///   The base-64 decoded contents of the PEM text from <paramref name="source" />
        ///   indicate the key is for an algorithm other than the algorithm
        ///   represented by this instance.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///   The base-64 decoded contents of the PEM text from <paramref name="source" />
        ///   represent the key in a format that is not supported.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     An error occurred while importing the key.
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
        public static MLKem ImportFromEncryptedPem(ReadOnlySpan<char> source, ReadOnlySpan<char> password)
        {
            return PemKeyHelpers.ImportEncryptedFactoryPem<MLKem, char>(
                source,
                password,
                ImportEncryptedPkcs8PrivateKey);
        }

        /// <summary>
        ///   Imports an ML-KEM key from an encrypted RFC 7468 PEM-encoded string.
        /// </summary>
        /// <param name="source">
        ///   The PEM text of the encrypted key to import.</param>
        /// <param name="passwordBytes">
        ///   The password to use for decrypting the key material.
        /// </param>
        /// <exception cref="ArgumentException">
        /// <para>
        ///   <paramref name="source"/> does not contain a PEM-encoded key with a recognized label.
        /// </para>
        /// <para>-or-</para>
        /// <para>
        ///   <paramref name="source"/> contains multiple PEM-encoded keys with a recognized label.
        /// </para>
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///   The password is incorrect.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///   The base-64 decoded contents of the PEM text from <paramref name="source" />
        ///   do not represent an ASN.1-BER-encoded PKCS#8 EncryptedPrivateKeyInfo structure.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///   The base-64 decoded contents of the PEM text from <paramref name="source" />
        ///   indicate the key is for an algorithm other than the algorithm
        ///   represented by this instance.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///   The base-64 decoded contents of the PEM text from <paramref name="source" />
        ///   represent the key in a format that is not supported.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     An error occurred while importing the key.
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
        public static MLKem ImportFromEncryptedPem(ReadOnlySpan<char> source, ReadOnlySpan<byte> passwordBytes)
        {
            return PemKeyHelpers.ImportEncryptedFactoryPem<MLKem, byte>(
                source,
                passwordBytes,
                ImportEncryptedPkcs8PrivateKey);
        }

        /// <inheritdoc cref="ImportFromEncryptedPem(ReadOnlySpan{char}, ReadOnlySpan{char})" />
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> or <paramref name="password" /> is <see langword="null" />
        /// </exception>
        public static MLKem ImportFromEncryptedPem(string source, string password)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(password);

            return ImportFromEncryptedPem(source.AsSpan(), password.AsSpan());
        }

        /// <inheritdoc cref="ImportFromEncryptedPem(ReadOnlySpan{char}, ReadOnlySpan{byte})" />
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> or <paramref name="passwordBytes" /> is <see langword="null" />
        /// </exception>
        public static MLKem ImportFromEncryptedPem(string source, byte[] passwordBytes)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(passwordBytes);

            return ImportFromEncryptedPem(source.AsSpan(), new ReadOnlySpan<byte>(passwordBytes));
        }

        /// <summary>
        ///   Releases all resources used by the <see cref="MLKem"/> class.
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
        ///   Called by the <c>Dispose()</c> and <c>Finalize()</c> methods to release the managed and unmanaged
        ///   resources used by the current instance of the <see cref="MLKem"/> class.
        /// </summary>
        /// <param name="disposing">
        ///   <see langword="true" /> to release managed and unmanaged resources;
        ///   <see langword="false" /> to release only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
        }

        private AsnWriter ExportSubjectPublicKeyInfoCore()
        {
            int encapsulationKeySize = Algorithm.EncapsulationKeySizeInBytes;
            byte[] encapsulationKeyBuffer = CryptoPool.Rent(encapsulationKeySize);
            Memory<byte> encapsulationKey = encapsulationKeyBuffer.AsMemory(0, encapsulationKeySize);

            try
            {
                ExportEncapsulationKeyCore(encapsulationKey.Span);

                SubjectPublicKeyInfoAsn spki = new SubjectPublicKeyInfoAsn
                {
                    Algorithm = new AlgorithmIdentifierAsn
                    {
                        Algorithm = Algorithm.Oid,
                        Parameters = default(ReadOnlyMemory<byte>?),
                    },
                    SubjectPublicKey = encapsulationKey,
                };

                // The ASN.1 overhead of a SubjectPublicKeyInfo encoding an encapsulation key is 22 bytes.
                // Round it off to 32. This checked operation should never throw because the inputs are not
                // user provided.
                int capacity = checked(32 + encapsulationKeySize);
                AsnWriter writer = new AsnWriter(AsnEncodingRules.DER, capacity);
                spki.Encode(writer);
                return writer;
            }
            finally
            {
                CryptoPool.Return(encapsulationKeyBuffer, clearSize: 0); // SPKI is public info, skip clear.
            }
        }

        private protected static void ThrowIfNotSupported()
        {
            if (!IsSupported)
            {
                throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_AlgorithmNotSupported, nameof(MLKem)));
            }
        }

        private static MLKemAlgorithm GetAlgorithmIdentifier(ref readonly AlgorithmIdentifierAsn identifier)
        {
            MLKemAlgorithm? algorithm = MLKemAlgorithm.FromOid(identifier.Algorithm);
            Debug.Assert(algorithm is not null, "Algorithm identifier should have been pre-validated by KeyFormatHelper.");

            if (identifier.Parameters.HasValue)
            {
                AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
                identifier.Encode(writer);
                throw Helpers.CreateAlgorithmUnknownException(writer);
            }

            return algorithm;
        }

        private static void MLKemKeyReader(
            ReadOnlyMemory<byte> privateKeyContents,
            in AlgorithmIdentifierAsn algorithmIdentifier,
            out MLKem kem)
        {
            MLKemAlgorithm algorithm = GetAlgorithmIdentifier(in algorithmIdentifier);
            MLKemPrivateKeyAsn kemKey = MLKemPrivateKeyAsn.Decode(privateKeyContents, AsnEncodingRules.BER);

            if (kemKey.Seed is ReadOnlyMemory<byte> seed)
            {
                if (seed.Length != algorithm.PrivateSeedSizeInBytes)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                kem = MLKemImplementation.ImportPrivateSeedImpl(algorithm, seed.Span);
            }
            else if (kemKey.ExpandedKey is ReadOnlyMemory<byte> expandedKey)
            {
                if (expandedKey.Length != algorithm.DecapsulationKeySizeInBytes)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                kem = MLKemImplementation.ImportDecapsulationKeyImpl(algorithm, expandedKey.Span);
            }
            else if (kemKey.Both is MLKemPrivateKeyBothAsn both)
            {
                int decapsulationKeySize = algorithm.DecapsulationKeySizeInBytes;

                if (both.Seed.Length != algorithm.PrivateSeedSizeInBytes ||
                    both.ExpandedKey.Length != decapsulationKeySize)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                MLKem key = MLKemImplementation.ImportPrivateSeedImpl(algorithm, both.Seed.Span);
                byte[] rent = CryptoPool.Rent(decapsulationKeySize);
                Span<byte> buffer = rent.AsSpan(0, decapsulationKeySize);

                try
                {
                    key.ExportDecapsulationKey(buffer);

                    if (CryptographicOperations.FixedTimeEquals(buffer, both.ExpandedKey.Span))
                    {
                        kem = key;
                    }
                    else
                    {
                        throw new CryptographicException(SR.Cryptography_KemPkcs8KeyMismatch);
                    }
                }
                catch
                {
                    key.Dispose();
                    throw;
                }
                finally
                {
                    CryptoPool.Return(rent, decapsulationKeySize);
                }
            }
            else
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }
        }

        private static void ThrowIfTrailingData(ReadOnlySpan<byte> data)
        {
            // The only thing we are checking here is that TryReadEncodedValue was able to decode it and that, given
            // the length of the data, that it the same length as the span. The encoding rules don't matter for length
            // checking, so just use BER.
            bool success = AsnDecoder.TryReadEncodedValue(data, AsnEncodingRules.BER, out _, out _, out _, out int bytesRead);

            if (!success || bytesRead != data.Length)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }
        }

        private protected void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, typeof(MLKem));
        }

        private AsnWriter ExportEncryptedPkcs8PrivateKeyCore<TChar>(
            ReadOnlySpan<TChar> password,
            PbeParameters pbeParameters,
            WriteEncryptedPkcs8Func<TChar> encryptor)
        {
            // There are 28 bytes of overhead on a plain PKCS#8 export for an expanded key. Add a little extra for
            // some extra space.
            int initialSize = Algorithm.DecapsulationKeySizeInBytes + 32;
            byte[] rented = CryptoPool.Rent(initialSize);
            int written;

            while (!TryExportPkcs8PrivateKey(rented, out written))
            {
                CryptoPool.Return(rented, 0);
                rented = CryptoPool.Rent(rented.Length * 2);
            }

            AsnWriter tmp = new(AsnEncodingRules.BER, initialCapacity: written);

            try
            {
                tmp.WriteEncodedValueForCrypto(rented.AsSpan(0, written));
                return encryptor(password, tmp, pbeParameters);
            }
            finally
            {
                tmp.Reset();
                CryptoPool.Return(rented, written);
            }
        }

        private TResult ExportPkcs8PrivateKeyCallback<TResult>(ExportPkcs8PrivateKeyFunc<TResult> func)
        {
            // A PKCS#8 ML-KEM-1024 ExpandedKey has an ASN.1 overhead of 28 bytes, assuming no attributes.
            // Make it an even 32 and that should give a good starting point for a buffer size.
            // Decapsulation keys are always larger than the seed, so if we end up with a seed export it should
            // fit in the initial buffer.
            int size = Algorithm.DecapsulationKeySizeInBytes + 32;
            byte[] buffer = CryptoPool.Rent(size); // Only passed out as span, callees can't keep a reference to it
            int written;

            while (!TryExportPkcs8PrivateKeyCore(buffer, out written))
            {
                CryptoPool.Return(buffer);
                size = checked(size * 2);
                buffer = CryptoPool.Rent(size);
            }

            if (written < 0 || written > buffer.Length)
            {
                // We got a nonsense value written back. Clear the buffer, but don't put it back in the pool.
                CryptographicOperations.ZeroMemory(buffer);
                throw new CryptographicException();
            }

            TResult result = func(buffer.AsSpan(0, written));
            CryptoPool.Return(buffer, written);
            return result;
        }

        private static string EncodeAsnWriterToPem(string label, AsnWriter writer, bool clear = true)
        {
#if NET10_0_OR_GREATER
            return writer.Encode(label, static (label, span) => PemEncoding.WriteString(label, span));
#else
            int length = writer.GetEncodedLength();
            byte[] rent = CryptoPool.Rent(length);

            try
            {
                int written = writer.Encode(rent);
                Debug.Assert(written == length);
                return PemEncoding.WriteString(label, rent.AsSpan(0, written));
            }
            finally
            {
                CryptoPool.Return(rent, clear ? length : 0);
            }
#endif
        }

        private protected static void ThrowIfNoSeed(bool hasSeed)
        {
            if (!hasSeed)
            {
                throw new CryptographicException(SR.Cryptography_PqcNoSeed);
            }
        }

        private protected static void ThrowIfNoDecapsulationKey(bool hasDecapsulationKey)
        {
            if (!hasDecapsulationKey)
            {
                throw new CryptographicException(SR.Cryptography_KemNoDecapsulationKey);
            }
        }

        private delegate TResult ExportPkcs8PrivateKeyFunc<TResult>(ReadOnlySpan<byte> pkcs8);

        private delegate AsnWriter WriteEncryptedPkcs8Func<TChar>(
            ReadOnlySpan<TChar> password,
            AsnWriter writer,
            PbeParameters pbeParameters);
    }
}
