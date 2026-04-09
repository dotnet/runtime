// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;

namespace System.Security.Cryptography
{
    /// <summary>
    ///   Represents an SLH-DSA key backed by OpenSSL.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This algorithm is specified by FIPS-205.
    ///   </para>
    ///   <para>
    ///     Developers are encouraged to program against the <c>SlhDsa</c> base class,
    ///     rather than any specific derived class.
    ///     The derived classes are intended for interop with the underlying system
    ///     cryptographic libraries.
    ///   </para>
    /// </remarks>
    [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    public sealed partial class SlhDsaOpenSsl : SlhDsa
    {
        private readonly SafeEvpPKeyHandle _key;

        /// <summary>
        ///   Initializes a new instance of the <see cref="SlhDsaOpenSsl" /> class from an existing OpenSSL key
        ///   represented as an <c>EVP_PKEY*</c>.
        /// </summary>
        /// <param name="pkeyHandle">
        ///   The OpenSSL <c>EVP_PKEY*</c> value to use as the key, represented as a <see cref="SafeEvpPKeyHandle" />.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="pkeyHandle" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>The handle in <paramref name="pkeyHandle" /> is not recognized as an SLH-DSA key.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while creating the algorithm instance.</para>
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The handle in <paramref name="pkeyHandle" /> is already disposed.
        /// </exception>
        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("osx")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public SlhDsaOpenSsl(SafeEvpPKeyHandle pkeyHandle) : base(AlgorithmFromHandle(pkeyHandle, out SafeEvpPKeyHandle upRefHandle))
        {
            _key = upRefHandle;
        }

        private static partial SlhDsaAlgorithm AlgorithmFromHandle(SafeEvpPKeyHandle pkeyHandle, out SafeEvpPKeyHandle upRefHandle);

        /// <summary>
        /// Gets a <see cref="SafeEvpPKeyHandle" /> representation of the cryptographic key.
        /// </summary>
        /// <returns>A <see cref="SafeEvpPKeyHandle" /> representation of the cryptographic key.</returns>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        public partial SafeEvpPKeyHandle DuplicateKeyHandle();
    }
}
