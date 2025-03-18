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
    ///   </para>
    ///   <para>
    ///     The derived classes are intended for interop with the underlying system
    ///     cryptographic libraries.
    ///   </para>
    /// </remarks>
    [Experimental(Experimentals.PostQuantumCryptographyDiagId)]
    public abstract class MLKem : IDisposable
    {
        private bool _disposed;
        private const int SharedSecretSize = 32; // FIPS 203, Table 3.

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
        protected MLKem(MLKemAlgorithm algorithm)
        {
            if (algorithm is null)
            {
                throw new ArgumentNullException(nameof(algorithm));
            }

            Algorithm = algorithm;
        }

        /// <summary>
        ///   Generates a new ML-KEM-512 key.
        /// </summary>
        /// <returns>
        ///   The generated key.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   An error occured generating the ML-KEM-512 key.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support ML-KEM-512. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports MK-KEM-512.
        /// </exception>
        public static MLKem GenerateMLKem512Key()
        {
            ThrowIfNotSupported();
            return MLKemImplementation.Generate(MLKemAlgorithm.MLKem512);
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
        ///   An error occurred during encapsulation.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para><paramref name="ciphertext" /> is not the correct size.</para>
        ///   <para> -or- </para>
        ///   <para><paramref name="sharedSecret" /> is not the correct size.</para>
        /// </exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        public void Encapsulate(Span<byte> ciphertext, Span<byte> sharedSecret)
        {
            ThrowIfDisposed();
            ValidatedCiphertextSize(ciphertext);
            ValidateSharedSecretSize(sharedSecret);
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
        ///   Decapsulate generates a shared secret with a provided ciphertext.
        /// </summary>
        /// <param name="ciphertext">
        ///   The ciphertext.
        /// </param>
        /// <param name="sharedSecret">
        ///   The buffer to receive the shared secret.
        /// </param>
        /// <exception cref="CryptographicException">
        ///   An error occurred during encapsulation.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para><paramref name="ciphertext" /> is not the correct size.</para>
        ///   <para> -or- </para>
        ///   <para><paramref name="sharedSecret" /> is not the correct size.</para>
        /// </exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        public void Decapsulate(ReadOnlySpan<byte> ciphertext, Span<byte> sharedSecret)
        {
            ThrowIfDisposed();
            ValidatedCiphertextSize(ciphertext);
            ValidateSharedSecretSize(sharedSecret);
            DecapsulateCore(ciphertext, sharedSecret);
        }

        /// <summary>
        ///   When overridden in a derived class, decapsulate generates a shared secret with a provided ciphertext.
        /// </summary>
        /// <param name="ciphertext">
        ///   The ciphertext.
        /// </param>
        /// <param name="sharedSecret">
        ///   The buffer to receive the shared secret.
        /// </param>
        /// <exception cref="CryptographicException">
        ///   An error occurred during encapsulation.
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
        ///  Releases all resources used by the <see cref="MLKem"/> class.
        /// </summary>
        public void Dispose()
        {
            _disposed = true;
            Dispose(true);
            GC.SuppressFinalize(this);
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

        private static void ValidateSharedSecretSize(ReadOnlySpan<byte> sharedSecret)
        {
            if (sharedSecret.Length != SharedSecretSize)
            {
                throw new ArgumentException("TODO", nameof(sharedSecret));
            }
        }

        private void ValidatedCiphertextSize(ReadOnlySpan<byte> ciphertext)
        {
            if (ciphertext.Length != Algorithm.CiphertextSizeInBytes)
            {
                throw new ArgumentException("TODO", nameof(ciphertext));
            }
        }

        /// <summary>
        ///   Releases any unmanged resources associated with the current instance.
        /// </summary>
        ~MLKem() => Dispose(false);
    }
}
