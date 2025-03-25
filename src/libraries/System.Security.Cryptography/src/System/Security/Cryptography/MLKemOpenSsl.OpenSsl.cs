// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    /// <summary>
    ///   Represents an ML-KEM key backed by OpenSSL.
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
    [UnsupportedOSPlatform("android")]
    [UnsupportedOSPlatform("browser")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("osx")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("windows")]
    [Experimental(Experimentals.PostQuantumCryptographyDiagId)]
    public sealed class MLKemOpenSsl : MLKem
    {
        private SafeEvpPKeyHandle _key;

        /// <summary>
        ///   Initializes a new instance of the <see cref="MLKemOpenSsl" /> class from an existing OpenSSL key
        ///   represented as an <c>EVP_PKEY*</c>.
        /// </summary>
        /// <param name="pkeyHandle">
        ///   The OpenSSL <c>EVP_PKEY*</c> value to use as the key, represented as a <see cref="SafeEvpPKeyHandle" />.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="pkeyHandle" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>The handle in <paramref name="pkeyHandle" /> is not recognized as an ML-KEM key.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while creating the algorithm instance.</para>
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The handle in <paramref name="pkeyHandle" /> is already disposed.
        /// </exception>
        public MLKemOpenSsl(SafeEvpPKeyHandle pkeyHandle)
            : base(AlgorithmFromHandle(pkeyHandle, out SafeEvpPKeyHandle upRefHandle))
        {
            _key = upRefHandle;
        }

        /// <summary>
        /// Creates a duplicate handle.
        /// </summary>
        /// <returns>A SafeHandle for the ML-KEM key in OpenSSL.</returns>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        public SafeEvpPKeyHandle DuplicateKeyHandle()
        {
            ThrowIfDisposed();
            return _key.DuplicateHandle();
        }

        private static MLKemAlgorithm AlgorithmFromHandle(SafeEvpPKeyHandle pkeyHandle, out SafeEvpPKeyHandle upRefHandle)
        {
            ArgumentNullException.ThrowIfNull(pkeyHandle);
            upRefHandle = pkeyHandle.DuplicateHandle();

            try
            {
                string name = Interop.Crypto.EvpKemGetName(upRefHandle);

                if (name == MLKemAlgorithm.MLKem512.Name)
                {
                    return MLKemAlgorithm.MLKem512;
                }
                if (name == MLKemAlgorithm.MLKem768.Name)
                {
                    return MLKemAlgorithm.MLKem768;
                }
                if (name == MLKemAlgorithm.MLKem1024.Name)
                {
                    return MLKemAlgorithm.MLKem1024;
                }

                throw new CryptographicException(SR.Format(SR.Argument_KemInvalidAlgorithmHandle, name));
            }
            catch
            {
                upRefHandle.Dispose();
                throw;
            }
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _key.Dispose();
            }
        }

        /// <inheritdoc />
        protected override void DecapsulateCore(ReadOnlySpan<byte> ciphertext, Span<byte> sharedSecret)
        {
            Interop.Crypto.EvpKemDecapsulate(_key, ciphertext, sharedSecret);
        }

        /// <inheritdoc />
        protected override void EncapsulateCore(Span<byte> ciphertext, Span<byte> sharedSecret)
        {
            Interop.Crypto.EvpKemEncapsulate(_key, ciphertext, sharedSecret);
        }

        /// <inheritdoc />
        protected override void ExportPrivateSeedCore(Span<byte> destination)
        {
            Interop.Crypto.EvpKemExportPrivateSeed(_key, destination);
        }

        /// <inheritdoc />
        protected override void ExportDecapsulationKeyCore(Span<byte> destination)
        {
            Interop.Crypto.EvpKemExportDecapsulationKey(_key, destination);
        }

        /// <inheritdoc />
        protected override void ExportEncapsulationKeyCore(Span<byte> destination)
        {
            Interop.Crypto.EvpKemExportEncapsulationKey(_key, destination);
        }
    }
}
