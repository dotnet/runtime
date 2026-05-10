// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Security.Cryptography
{
    /// <summary>
    ///   Provides a Cryptography Next Generation (CNG) implementation of X25519 Diffie-Hellman.
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
    public sealed partial class X25519DiffieHellmanCng : X25519DiffieHellman
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="X25519DiffieHellmanCng"/> class
        ///   by using the specified <see cref="CngKey"/>.
        /// </summary>
        /// <param name="key">
        ///   The key that will be used as input to the cryptographic operations performed by the current object.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="key"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="key"/> does not specify an X25519 Diffie-Hellman key.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   Cryptography Next Generation (CNG) classes are not supported on this system.
        /// </exception>
        [SupportedOSPlatform("windows")]
        public partial X25519DiffieHellmanCng(CngKey key);

        /// <summary>
        ///   Gets a new <see cref="CngKey" /> representing the key used by the current instance.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <remarks>
        ///   This <see cref="CngKey"/> object is not the same as the one passed to <see cref="X25519DiffieHellmanCng(CngKey)"/>,
        ///   if that constructor was used. However, it will point to the same CNG key.
        /// </remarks>
        public partial CngKey GetKey();

        /// <inheritdoc/>
        protected override partial void DeriveRawSecretAgreementCore(X25519DiffieHellman otherParty, Span<byte> destination);

        /// <inheritdoc/>
        protected override partial void ExportPublicKeyCore(Span<byte> destination);

        /// <inheritdoc/>
        protected override partial void ExportPrivateKeyCore(Span<byte> destination);

        /// <inheritdoc/>
        protected override partial bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten);

        /// <inheritdoc/>
        protected override partial void Dispose(bool disposing);
    }
}
