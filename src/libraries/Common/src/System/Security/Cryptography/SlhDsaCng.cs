// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;

namespace System.Security.Cryptography
{
    /// <summary>
    ///   Provides a Cryptography Next Generation (CNG) implementation of the Stateless Hash-Based Digital Signature
    ///   Algorithm (SLH-DSA).
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This algorithm is specified by FIPS-205.
    ///   </para>
    ///   <para>
    ///     Developers are encouraged to program against the <see cref="SlhDsa" /> base class,
    ///     rather than any specific derived class.
    ///     The derived classes are intended for interop with the underlying system
    ///     cryptographic libraries.
    ///   </para>
    /// </remarks>
    [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    public sealed partial class SlhDsaCng : SlhDsa
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="SlhDsaCng"/> class by using the specified <see cref="CngKey"/>.
        /// </summary>
        /// <param name="key">
        ///   The key that will be used as input to the cryptographic operations performed by the current object.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="key"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="key"/> does not specify a Stateless Hash-Based Digital Signature Algorithm (SLH-DSA) group.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   Cryptography Next Generation (CNG) classes are not supported on this system.
        /// </exception>
        [SupportedOSPlatform("windows")]
        public SlhDsaCng(CngKey key) : base(SlhDsaAlgorithm.SlhDsaShake256f) // We need to pass something to the base so we can throw PNSE.
        {
            ArgumentNullException.ThrowIfNull(key);
            throw new PlatformNotSupportedException();
        }

        /// <summary>
        ///   Gets a new <see cref="CngKey" /> representing the key used by the current instance.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <remarks>
        ///   This <see cref="CngKey"/> object is not the same as the one passed to <see cref="SlhDsaCng(CngKey)"/>,
        ///   if that constructor was used. However, it will point to the same CNG key.
        /// </remarks>
        public CngKey GetKey()
        {
            throw new PlatformNotSupportedException();
        }

        /// <inheritdoc />
        protected override void SignDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination) =>
            throw new PlatformNotSupportedException();

        /// <inheritdoc />
        protected override bool VerifyDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature) =>
            throw new PlatformNotSupportedException();

        /// <inheritdoc />
        protected override void SignPreHashCore(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> context, string hashAlgorithmOid, Span<byte> destination) =>
            throw new PlatformNotSupportedException();

        /// <inheritdoc />
        protected override bool VerifyPreHashCore(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> context, string hashAlgorithmOid, ReadOnlySpan<byte> signature) =>
            throw new PlatformNotSupportedException();

        /// <inheritdoc />
        protected override void ExportSlhDsaPublicKeyCore(Span<byte> destination) =>
            throw new PlatformNotSupportedException();

        /// <inheritdoc />
        protected override void ExportSlhDsaPrivateKeyCore(Span<byte> destination) =>
            throw new PlatformNotSupportedException();

        /// <inheritdoc />
        protected override bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten) =>
            throw new PlatformNotSupportedException();
    }
}
