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
            ArgumentNullException.ThrowIfNull(suite);
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
            return HpkeImplementation.GenerateKeyImpl(suite);
        }

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
    }
}
