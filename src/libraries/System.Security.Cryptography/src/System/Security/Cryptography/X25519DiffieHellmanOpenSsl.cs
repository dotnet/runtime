// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Security.Cryptography
{
    /// <summary>
    ///   Represents an X25519 Diffie-Hellman key backed by OpenSSL.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This algorithm is specified by RFC 7748.
    ///   </para>
    ///   <para>
    ///     Developers are encouraged to program against the <c>X25519DiffieHellman</c> base class,
    ///     rather than any specific derived class.
    ///     The derived classes are intended for interop with the underlying system
    ///     cryptographic libraries.
    ///   </para>
    /// </remarks>
    public sealed partial class X25519DiffieHellmanOpenSsl : X25519DiffieHellman
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="X25519DiffieHellmanOpenSsl" /> class from an existing OpenSSL key
        ///   represented as an <c>EVP_PKEY*</c>.
        /// </summary>
        /// <param name="pkeyHandle">
        ///   The OpenSSL <c>EVP_PKEY*</c> value to use as the key, represented as a <see cref="SafeEvpPKeyHandle" />.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="pkeyHandle" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>The handle in <paramref name="pkeyHandle" /> is not recognized as an X25519 Diffie-Hellman key.</para>
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
        public partial X25519DiffieHellmanOpenSsl(SafeEvpPKeyHandle pkeyHandle);

        /// <summary>
        ///   Gets a <see cref="SafeEvpPKeyHandle" /> representation of the cryptographic key.
        /// </summary>
        /// <returns>A <see cref="SafeEvpPKeyHandle" /> representation of the cryptographic key.</returns>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        public partial SafeEvpPKeyHandle DuplicateKeyHandle();
    }
}
