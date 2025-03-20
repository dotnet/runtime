// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

#pragma warning disable CA1510, CA1513

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
    [Experimental(Experimentals.PostQuantumCryptographyDiagId)]
    public abstract class MLKem : IDisposable
    {
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
        ///   <paramref name="algorithm" /> is <see langword="null" />
        /// </exception>
        protected MLKem(MLKemAlgorithm algorithm)
        {
            if (algorithm is null)
            {
                throw new ArgumentNullException(nameof(algorithm));
            }

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
        ///   <paramref name="algorithm" /> is <see langword="null" />
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occured generating the ML-KEM key.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support ML-KEM. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports MK-KEM.
        /// </exception>
        public static MLKem GenerateKey(MLKemAlgorithm algorithm)
        {
            if (algorithm is null)
            {
                throw new ArgumentNullException(nameof(algorithm));
            }

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
        ///   Throws <see cref="ObjectDisposedException" /> if the current instance is disposed.
        /// </summary>
        protected void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(MLKem));
            }
        }

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
        ///   <paramref name="algorithm" /> is <see langword="null" />
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while importing the key.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support ML-KEM. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports MK-KEM.
        /// </exception>
        public static MLKem ImportPrivateSeed(MLKemAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            if (algorithm is null)
            {
                throw new ArgumentNullException(nameof(algorithm));
            }

            if (source.Length != algorithm.PrivateSeedSizeInBytes)
            {
                throw new ArgumentException(SR.Argument_KemInvalidSeedLength, nameof(source));
            }

            ThrowIfNotSupported();
            return MLKemImplementation.ImportPrivateSeedImpl(algorithm, source);
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
        ///   <paramref name="algorithm" /> is <see langword="null" />
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while importing the key.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support ML-KEM. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports MK-KEM.
        /// </exception>
        public static MLKem ImportDecapsulationKey(MLKemAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            if (algorithm is null)
            {
                throw new ArgumentNullException(nameof(algorithm));
            }

            if (source.Length != algorithm.DecapsulationKeySizeInBytes)
            {
                throw new ArgumentException(SR.Argument_KemInvalidDecapsulationKeyLength, nameof(source));
            }

            ThrowIfNotSupported();
            return MLKemImplementation.ImportDecapsulationKeyImpl(algorithm, source);
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
        ///   to determine if the platform supports MK-KEM.
        /// </exception>
        public static MLKem ImportEncapsulationKey(MLKemAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            if (algorithm is null)
            {
                throw new ArgumentNullException(nameof(algorithm));
            }

            if (source.Length != algorithm.EncapsulationKeySizeInBytes)
            {
                throw new ArgumentException(SR.Argument_KemInvalidEncapsulationKeyLength, nameof(source));
            }

            ThrowIfNotSupported();
            return MLKemImplementation.ImportEncapsulationKeyImpl(algorithm, source);
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
        ///   When overridden in a derived class, exports the encapsulation key into the provided buffer.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the encapsulation key.
        /// </param>
        protected abstract void ExportEncapsulationKeyCore(Span<byte> destination);

        /// <summary>
        ///  Releases all resources used by the <see cref="MLKem"/> class.
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

        private protected static void ThrowIfNotSupported()
        {
            if (!IsSupported)
            {
                throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_AlgorithmNotSupported, nameof(MLKem)));
            }
        }
    }
}
