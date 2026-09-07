// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography
{
    /// <summary>
    ///   Represents a Hybrid Public Key Encryption (HPKE) key.
    /// </summary>
    [Experimental(Experimentals.HpkeExperimentalDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    public abstract class Hpke : IDisposable
    {
        private bool _disposed;

        /// <summary>
        ///   Gets the cipher suite associated with this key.
        /// </summary>
        /// <value>
        ///   The cipher suite associated with this key.
        /// </value>
        public HpkeSuite Suite { get; }

        /// <summary>
        ///   Initializes a new instance of the <see cref="Hpke" /> class with the specified cipher suite.
        /// </summary>
        /// <param name="suite">
        ///   The cipher suite associated with this key.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="suite" /> is <see langword="null" />.
        /// </exception>
        protected Hpke(HpkeSuite suite)
        {
            ArgumentNullException.ThrowIfNull(suite);
            Suite = suite;
        }

        /// <summary>
        ///   Determines whether the specified cipher suite is supported on the current platform.
        /// </summary>
        /// <param name="suite">
        ///   The cipher suite to check.
        /// </param>
        /// <returns>
        ///   <see langword="true" /> if the cipher suite is supported; otherwise, <see langword="false" />.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="suite" /> is <see langword="null" />.
        /// </exception>
        public static bool IsSupported(HpkeSuite suite)
        {
            ArgumentNullException.ThrowIfNull(suite);
            return HpkeImplementation.IsSupportedImpl(suite);
        }

        /// <summary>
        ///   Derives an HPKE key for the specified cipher suite from input keying material.
        /// </summary>
        /// <param name="suite">
        ///   The cipher suite for the derived key.
        /// </param>
        /// <param name="ikm">
        ///   The input keying material from which to derive the key.
        /// </param>
        /// <returns>
        ///   The derived HPKE key.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="suite" /> or <paramref name="ikm" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   <paramref name="suite" /> is not supported on the current platform.
        /// </exception>
        public static Hpke DeriveKey(HpkeSuite suite, byte[] ikm)
        {
            ArgumentNullException.ThrowIfNull(ikm);
            return DeriveKey(suite, new ReadOnlySpan<byte>(ikm));
        }

        /// <summary>
        ///   Derives an HPKE key for the specified cipher suite from input keying material.
        /// </summary>
        /// <param name="suite">
        ///   The cipher suite for the derived key.
        /// </param>
        /// <param name="ikm">
        ///   The input keying material from which to derive the key.
        /// </param>
        /// <returns>
        ///   The derived HPKE key.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="suite" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   <paramref name="suite" /> is not supported on the current platform.
        /// </exception>
        public static Hpke DeriveKey(HpkeSuite suite, ReadOnlySpan<byte> ikm)
        {
            ArgumentNullException.ThrowIfNull(suite);
            ThrowIfNotSupported(suite);
            return HpkeImplementation.DeriveKeyImpl(suite, ikm);
        }

        /// <summary>
        ///   Generates a new HPKE key for the specified cipher suite.
        /// </summary>
        /// <param name="suite">
        ///   The cipher suite for the new key.
        /// </param>
        /// <returns>
        ///   A new HPKE key.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="suite" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   <paramref name="suite" /> is not supported on the current platform.
        /// </exception>
        public static Hpke GenerateKey(HpkeSuite suite)
        {
            ArgumentNullException.ThrowIfNull(suite);
            ThrowIfNotSupported(suite);
            return HpkeImplementation.GenerateKeyImpl(suite);
        }

        /// <summary>
        ///   Exports the decapsulation key.
        /// </summary>
        /// <returns>
        ///   A new byte array containing the serialized decapsulation key, with a length of
        ///   <see cref="HpkeSuite.DecapsulationKeySizeInBytes" /> bytes.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   The current instance does not contain a decapsulation key, or an error occurred while exporting the key.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The object has already been disposed.
        /// </exception>
        /// <remarks>
        ///   <para>
        ///     The key is exported in the private-key format defined by the cipher suite's KEM,
        ///     without a PKCS#8 or other ASN.1 wrapper. For DHKEM with NIST curves, this is the fixed-width,
        ///     big-endian private scalar. For DHKEM with X25519, this is the raw 32-byte X25519 private key.
        ///     For ML-KEM and hybrid ML-KEM cipher suites, this is the private seed.
        ///   </para>
        ///   <para>
        ///     The returned key is not the original input keying material supplied to
        ///     <see cref="DeriveKey(HpkeSuite, ReadOnlySpan{byte})" />.
        ///     The caller is responsible for protecting the returned secret bytes and clearing them when no longer needed.
        ///   </para>
        /// </remarks>
        public byte[] ExportDecapsulationKey()
        {
            ThrowIfDisposed();
            byte[] key = new byte[Suite.DecapsulationKeySizeInBytes];

            try
            {
                ExportDecapsulationKeyCore(key);
                return key;
            }
            catch
            {
                CryptographicOperations.ZeroMemory(key);
                throw;
            }
        }

        /// <summary>
        ///   Exports the decapsulation key into the provided buffer.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the serialized decapsulation key.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="destination" /> is not exactly
        ///   <see cref="HpkeSuite.DecapsulationKeySizeInBytes" /> bytes long.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   The current instance does not contain a decapsulation key, or an error occurred while exporting the key.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The object has already been disposed.
        /// </exception>
        /// <remarks>
        ///   The key format is the same as for <see cref="ExportDecapsulationKey()" />.
        ///   On success, the entire destination is filled with the serialized key.
        ///   The caller is responsible for protecting the secret bytes and clearing the buffer when no longer needed.
        /// </remarks>
        public void ExportDecapsulationKey(Span<byte> destination)
        {
            if (destination.Length != Suite.DecapsulationKeySizeInBytes)
            {
                throw new ArgumentException(
                    SR.Format(SR.Argument_DestinationImprecise, Suite.DecapsulationKeySizeInBytes),
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
        /// <exception cref="CryptographicException">
        ///   The current instance does not contain a decapsulation key, or an error occurred while exporting the key.
        /// </exception>
        /// <remarks>
        ///   The calling method has verified that this instance is not disposed and that
        ///   <paramref name="destination" /> is exactly
        ///   <see cref="HpkeSuite.DecapsulationKeySizeInBytes" /> bytes long.
        ///   Implementations must fill the entire destination using the key format described by
        ///   <see cref="ExportDecapsulationKey()" /> and throw <see cref="CryptographicException" />
        ///   if the decapsulation key cannot be exported.
        /// </remarks>
        protected abstract void ExportDecapsulationKeyCore(Span<byte> destination);

        /// <summary>
        ///   Exports the encapsulation key.
        /// </summary>
        /// <returns>
        ///   The encapsulation key.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   The current instance does not contain an encapsulation key, or an error occurred while exporting the key.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The object has already been disposed.
        /// </exception>
        public byte[] ExportEncapsulationKey()
        {
            ThrowIfDisposed();
            byte[] key = new byte[Suite.EncapsulationKeySizeInBytes];
            ExportEncapsulationKeyCore(key);
            return key;
        }

        /// <summary>
        ///   Exports the encapsulation key into the provided buffer.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the encapsulation key.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="destination" /> is not exactly
        ///   <see cref="HpkeSuite.EncapsulationKeySizeInBytes" /> bytes long.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   The current instance does not contain an encapsulation key, or an error occurred while exporting the key.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The object has already been disposed.
        /// </exception>
        public void ExportEncapsulationKey(Span<byte> destination)
        {
            if (destination.Length != Suite.EncapsulationKeySizeInBytes)
            {
                throw new ArgumentException(
                    SR.Format(SR.Argument_DestinationImprecise, Suite.EncapsulationKeySizeInBytes),
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
        /// <exception cref="CryptographicException">
        ///   The current instance does not contain an encapsulation key, or an error occurred while exporting the key.
        /// </exception>
        /// <remarks>
        ///   <paramref name="destination" /> is exactly
        ///   <see cref="HpkeSuite.EncapsulationKeySizeInBytes" /> bytes long.
        /// </remarks>
        protected abstract void ExportEncapsulationKeyCore(Span<byte> destination);

        /// <summary>
        ///   Releases all resources used by the <see cref="Hpke" /> class.
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
        ///   resources used by the current instance of the <see cref="Hpke" /> class.
        /// </summary>
        /// <param name="disposing">
        ///   <see langword="true" /> to release managed and unmanaged resources;
        ///   <see langword="false" /> to release only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
        }

        private static void ThrowIfNotSupported(HpkeSuite suite)
        {
            if (!IsSupported(suite))
            {
                throw new PlatformNotSupportedException();
            }
        }

        private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
