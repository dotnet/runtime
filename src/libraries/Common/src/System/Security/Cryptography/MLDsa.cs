// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    /// <summary>
    ///   Represents an ML-DSA key.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This algorithm is specified by FIPS-204.
    ///   </para>
    ///   <para>
    ///     Developers are encouraged to program against the <see cref="MLDsa"/> base class,
    ///     rather than any specific derived class.
    ///     The derived classes are intended for interop with the underlying system
    ///     cryptographic libraries.
    ///   </para>
    /// </remarks>
    public abstract partial class MLDsa : IDisposable
#if DESIGNTIMEINTERFACES
#pragma warning disable SA1001
        , IImportExportShape<MLDsa>
#pragma warning restore SA1001
#endif
    {
        private protected static readonly string[] KnownOids =
        [
            Oids.MLDsa44,
            Oids.MLDsa65,
            Oids.MLDsa87,
        ];

        private const int MaxContextLength = 255;

        /// <summary>
        ///   Gets the specific ML-DSA algorithm for this key.
        /// </summary>
        /// <value>
        ///   The specific ML-DSA algorithm for this key.
        /// </value>
        public MLDsaAlgorithm Algorithm { get; }
        private bool _disposed;

        /// <summary>
        ///   Initializes a new instance of the <see cref="MLDsa" /> class.
        /// </summary>
        /// <param name="algorithm">
        ///   The specific ML-DSA algorithm for this key.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="algorithm" /> is <see langword="null" />.
        /// </exception>
        protected MLDsa(MLDsaAlgorithm algorithm)
        {
            ArgumentNullException.ThrowIfNull(algorithm);

            Algorithm = algorithm;
        }

        private protected void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, typeof(MLDsa));

        /// <summary>
        ///   Gets a value indicating whether the current platform supports ML-DSA.
        /// </summary>
        /// <value>
        ///   <see langword="true" /> if the current platform supports ML-DSA; otherwise, <see langword="false" />.
        /// </value>
        public static bool IsSupported { get; } = MLDsaImplementation.SupportsAny();

        /// <summary>
        ///   Releases all resources used by the <see cref="MLDsa"/> class.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                Dispose(true);
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        ///   Signs the specified data, writing the signature into the provided buffer.
        /// </summary>
        /// <param name="data">
        ///   The data to sign.
        /// </param>
        /// <param name="destination">
        ///   The buffer to receive the signature. Its length must be exactly
        ///   <see cref="MLDsaAlgorithm.SignatureSizeInBytes"/>.
        /// </param>
        /// <param name="context">
        ///   An optional context-specific value to limit the scope of the signature.
        ///   The default value is an empty buffer.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   The buffer in <paramref name="destination"/> is the incorrect length to receive the signature.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="context"/> has a <see cref="ReadOnlySpan{T}.Length"/> in excess of
        ///   255 bytes.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>The instance represents only a public key.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while signing the data.</para>
        /// </exception>
        public void SignData(ReadOnlySpan<byte> data, Span<byte> destination, ReadOnlySpan<byte> context = default)
        {
            Helpers.ThrowIfDestinationWrongLength(destination, Algorithm.SignatureSizeInBytes);

            if (context.Length > MaxContextLength)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(context),
                    context.Length,
                    SR.Argument_SignatureContextTooLong255);
            }

            ThrowIfDisposed();
            SignDataCore(data, context, destination);
        }

        /// <summary>
        ///   Signs the specified data.
        /// </summary>
        /// <param name="data">
        ///   The data to sign.
        /// </param>
        /// <param name="context">
        ///   An optional context-specific value to limit the scope of the signature.
        ///   The default value is <see langword="null" />.
        /// </param>
        /// <returns>
        ///   ML-DSA signature for the specified data.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="data"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="context"/> has a length in excess of 255 bytes.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>The instance represents only a public key.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while signing the data.</para>
        /// </exception>
        /// <remarks>
        ///   A <see langword="null" /> context is treated as empty.
        /// </remarks>
        public byte[] SignData(byte[] data, byte[]? context = default)
        {
            ArgumentNullException.ThrowIfNull(data);

            byte[] destination = new byte[Algorithm.SignatureSizeInBytes];
            SignData(new ReadOnlySpan<byte>(data), destination.AsSpan(), new ReadOnlySpan<byte>(context));
            return destination;
        }

        /// <summary>
        ///   Verifies that the specified signature is valid for this key and the provided data.
        /// </summary>
        /// <param name="data">
        ///   The data to verify.
        /// </param>
        /// <param name="signature">
        ///   The signature to verify.
        /// </param>
        /// <param name="context">
        ///   The context value which was provided during signing.
        ///   The default value is an empty buffer.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the signature validates the data; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="context"/> has a <see cref="ReadOnlySpan{T}.Length"/> in excess of
        ///   255 bytes.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>The instance represents only a public key.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while verifying the data.</para>
        /// </exception>
        public bool VerifyData(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature, ReadOnlySpan<byte> context = default)
        {
            if (context.Length > MaxContextLength)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(context),
                    context.Length,
                    SR.Argument_SignatureContextTooLong255);
            }

            ThrowIfDisposed();

            if (signature.Length != Algorithm.SignatureSizeInBytes)
            {
                return false;
            }

            return VerifyDataCore(data, context, signature);
        }

        /// <summary>
        ///   Verifies that the specified signature is valid for this key and the provided data.
        /// </summary>
        /// <param name="data">
        ///   The data to verify.
        /// </param>
        /// <param name="signature">
        ///   The signature to verify.
        /// </param>
        /// <param name="context">
        ///   The context value which was provided during signing.
        ///   The default value is <see langword="null" />.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the signature validates the data; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="data"/> or <paramref name="signature"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="context"/> has a length in excess of 255 bytes.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>An error occurred while verifying the data.</para>
        /// </exception>
        /// <remarks>
        ///   A <see langword="null" /> context is treated as empty.
        /// </remarks>
        public bool VerifyData(byte[] data, byte[] signature, byte[]? context = default)
        {
            ArgumentNullException.ThrowIfNull(data);
            ArgumentNullException.ThrowIfNull(signature);

            return VerifyData(new ReadOnlySpan<byte>(data), new ReadOnlySpan<byte>(signature), new ReadOnlySpan<byte>(context));
        }

        /// <summary>
        ///   Signs the specified hash using the FIPS 204 pre-hash signing algorithm, writing the signature into the provided buffer.
        /// </summary>
        /// <param name="hash">
        ///   The hash to sign.
        /// </param>
        /// <param name="destination">
        ///   The buffer to receive the signature. Its length must be exactly
        ///   <see cref="MLDsaAlgorithm.SignatureSizeInBytes"/>.
        /// </param>
        /// <param name="hashAlgorithmOid">
        ///   The OID of the hash algorithm used to create the hash.
        /// </param>
        /// <param name="context">
        ///   An optional context-specific value to limit the scope of the signature.
        ///   The default value is an empty buffer.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="hashAlgorithmOid"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   The buffer in <paramref name="destination"/> is the incorrect length to receive the signature.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="context"/> has a <see cref="ReadOnlySpan{T}.Length"/> in excess of
        ///   255 bytes.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para><paramref name="hashAlgorithmOid"/> is not a well-formed OID.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="hashAlgorithmOid"/> is a well-known algorithm and <paramref name="hash"/> does not have the expected length.</para>
        ///   <para>-or-</para>
        ///   <para>The instance represents only a public key.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while signing the hash.</para>
        /// </exception>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public void SignPreHash(ReadOnlySpan<byte> hash, Span<byte> destination, string hashAlgorithmOid, ReadOnlySpan<byte> context = default)
        {
            ArgumentNullException.ThrowIfNull(hashAlgorithmOid);
            Helpers.ThrowIfDestinationWrongLength(destination, Algorithm.SignatureSizeInBytes);

            if (context.Length > MaxContextLength)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(context),
                    context.Length,
                    SR.Argument_SignatureContextTooLong255);
            }

            string? hashAlgorithmIdentifier = MapHashOidToAlgorithm(
                hashAlgorithmOid,
                out int hashLengthInBytes,
                out bool insufficientCollisionResistance);

            if (hashAlgorithmIdentifier is null)
            {
                throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithmOid));
            }

            if (insufficientCollisionResistance)
            {
                throw new CryptographicException(SR.Format(
                    SR.Cryptography_HashMLDsaAlgorithmMismatch,
                    Algorithm.Name,
                    hashAlgorithmIdentifier));
            }

            if (hashLengthInBytes != hash.Length)
            {
                throw new CryptographicException(SR.Cryptography_HashLengthMismatch);
            }

            ThrowIfDisposed();

            SignPreHashCore(hash, context, hashAlgorithmOid, destination);
        }

        /// <summary>
        ///   Signs the specified hash using the FIPS 204 pre-hash signing algorithm.
        /// </summary>
        /// <param name="hash">
        ///   The hash to sign.
        /// </param>
        /// <param name="hashAlgorithmOid">
        ///   The OID of the hash algorithm used to create the hash.
        /// </param>
        /// <param name="context">
        ///   An optional context-specific value to limit the scope of the signature.
        ///   The default value is <see langword="null" />.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="hash"/> or <paramref name="hashAlgorithmOid"/> is <see langword="null"/>.
        ///   <para>An error occurred while verifying the data.</para>
        ///   <para><paramref name="hashAlgorithmOid"/> is not a well-formed OID.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="hashAlgorithmOid"/> is a well-known algorithm and <paramref name="hash"/> does not have the expected length.</para>
        ///   <para>-or-</para>
        ///   <para>The instance represents only a public key.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while signing the hash.</para>
        /// </exception>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public byte[] SignPreHash(byte[] hash, string hashAlgorithmOid, byte[]? context = default)
        {
            ArgumentNullException.ThrowIfNull(hash);
            ArgumentNullException.ThrowIfNull(hashAlgorithmOid);

            byte[] destination = new byte[Algorithm.SignatureSizeInBytes];
            SignPreHash(new ReadOnlySpan<byte>(hash), destination.AsSpan(), hashAlgorithmOid, new ReadOnlySpan<byte>(context));
            return destination;
        }

        /// <summary>
        ///   Verifies that the specified FIPS 204 pre-hash signature is valid for this key and the provided hash.
        /// </summary>
        /// <param name="hash">
        ///   The hash to verify.
        /// </param>
        /// <param name="signature">
        ///   The signature to verify.
        /// </param>
        /// <param name="hashAlgorithmOid">
        ///   The OID of the hash algorithm used to create the hash.
        /// </param>
        /// <param name="context">
        ///   The context value which was provided during signing.
        ///   The default value is an empty buffer.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the signature validates the hash; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="hashAlgorithmOid"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="context"/> has a <see cref="ReadOnlySpan{T}.Length"/> in excess of
        ///   255 bytes.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para><paramref name="hashAlgorithmOid"/> is not a well-formed OID.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="hashAlgorithmOid"/> is a well-known algorithm and <paramref name="hash"/> does not have the expected length.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while verifying the hash.</para>
        /// </exception>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public bool VerifyPreHash(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature, string hashAlgorithmOid, ReadOnlySpan<byte> context = default)
        {
            ArgumentNullException.ThrowIfNull(hashAlgorithmOid);

            if (context.Length > MaxContextLength)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(context),
                    context.Length,
                    SR.Argument_SignatureContextTooLong255);
            }

            string? hashAlgorithmIdentifier = MapHashOidToAlgorithm(
                hashAlgorithmOid,
                out int hashLengthInBytes,
                out bool insufficientCollisionResistance);

            if (hashAlgorithmIdentifier is null || insufficientCollisionResistance ||
                signature.Length != Algorithm.SignatureSizeInBytes)
            {
                return false;
            }

            if (hashLengthInBytes != hash.Length)
            {
                throw new CryptographicException(SR.Cryptography_HashLengthMismatch);
            }

            ThrowIfDisposed();

            return VerifyPreHashCore(hash, context, hashAlgorithmOid, signature);
        }

        /// <summary>
        ///   Verifies that the specified FIPS 204 pre-hash signature is valid for this key and the provided hash.
        /// </summary>
        /// <param name="hash">
        ///   The hash to verify.
        /// </param>
        /// <param name="signature">
        ///   The signature to verify.
        /// </param>
        /// <param name="hashAlgorithmOid">
        ///   The OID of the hash algorithm used to create the hash.
        /// </param>
        /// <param name="context">
        ///   The context value which was provided during signing.
        ///   The default value is <see langword="null" />.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the signature validates the hash; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="hash"/> or <paramref name="signature"/> or <paramref name="hashAlgorithmOid"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="context"/> has a length in excess of 255 bytes.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para><paramref name="hashAlgorithmOid"/> is not a well-formed OID.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="hashAlgorithmOid"/> is a well-known algorithm and <paramref name="hash"/> does not have the expected length.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while verifying the hash.</para>
        /// </exception>
        /// <remarks>
        ///   A <see langword="null" /> context is treated as empty.
        /// </remarks>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public bool VerifyPreHash(byte[] hash, byte[] signature, string hashAlgorithmOid, byte[]? context = null)
        {
            ArgumentNullException.ThrowIfNull(hash);
            ArgumentNullException.ThrowIfNull(signature);
            ArgumentNullException.ThrowIfNull(hashAlgorithmOid);

            return VerifyPreHash(
                new ReadOnlySpan<byte>(hash),
                new ReadOnlySpan<byte>(signature),
                hashAlgorithmOid,
                new ReadOnlySpan<byte>(context));
        }

        /// <inheritdoc cref="SignMu(ReadOnlySpan{byte})"/>
        /// <exception cref="ArgumentNullException"><paramref name="externalMu"/> is <see langword="null"/>.</exception>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public byte[] SignMu(byte[] externalMu)
        {
            ArgumentNullException.ThrowIfNull(externalMu);

            return SignMu(new ReadOnlySpan<byte>(externalMu));
        }

        /// <summary>
        ///   Signs the specified externally computed signature mu (&#x3BC;) value.
        /// </summary>
        /// <param name="externalMu">
        ///   The signature mu value to sign.
        /// </param>
        /// <returns>
        ///   ML-DSA signature for the specified mu value.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   The buffer in <paramref name="externalMu"/> is the incorrect length for the signature mu value.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>The instance represents only a public key.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while signing the hash.</para>
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The current platform does not support signing with an externally computed mu value.
        /// </exception>
        /// <seealso cref="VerifyMu(byte[], byte[])"/>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public byte[] SignMu(ReadOnlySpan<byte> externalMu)
        {
            byte[] destination = new byte[Algorithm.SignatureSizeInBytes];
            SignMu(externalMu, destination.AsSpan());
            return destination;
        }

        /// <summary>
        ///   Signs the specified externally computed signature mu (&#x3BC;) value,
        ///   writing the signature into the provided buffer.
        /// </summary>
        /// <param name="externalMu">
        ///   The signature mu value to sign.
        /// </param>
        /// <param name="destination">
        ///   The buffer to receive the signature. Its length must be exactly
        ///   <see cref="MLDsaAlgorithm.SignatureSizeInBytes"/>.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///     The buffer in <paramref name="externalMu"/> is the incorrect length for the signature mu value.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The buffer in <paramref name="destination"/> is the incorrect length to receive the signature.
        ///   </para>
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>The instance represents only a public key.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while signing the hash.</para>
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The current platform does not support signing with an externally computed mu value.
        /// </exception>
        /// <seealso cref="VerifyMu(ReadOnlySpan{byte}, ReadOnlySpan{byte})"/>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public void SignMu(ReadOnlySpan<byte> externalMu, Span<byte> destination)
        {
            if (externalMu.Length != Algorithm.MuSizeInBytes)
                throw new ArgumentException(SR.Argument_MLDsaMuInvalidLength, nameof(externalMu));

            Helpers.ThrowIfDestinationWrongLength(destination, Algorithm.SignatureSizeInBytes);
            ThrowIfDisposed();

            SignMuCore(externalMu, destination);
        }

        /// <summary>
        ///   When overridden in a derived class, computes the remainder of the signature from the
        ///   precomputed mu (&#x3BC;) value, writing it into the provided buffer.
        /// </summary>
        /// <param name="externalMu">
        ///   The signature mu value to sign.
        /// </param>
        /// <param name="destination">
        ///   The buffer to receive the signature, which will always be the exactly correct size for the algorithm.
        /// </param>
        /// <exception cref="CryptographicException">
        ///   An error occurred while computing the signature.
        /// </exception>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        protected abstract void SignMuCore(ReadOnlySpan<byte> externalMu, Span<byte> destination);

        /// <inheritdoc cref="VerifyMu(ReadOnlySpan{byte}, ReadOnlySpan{byte})"/>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="externalMu"/> or <paramref name="signature"/> is <see langword="null"/>.
        /// </exception>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public bool VerifyMu(byte[] externalMu, byte[] signature)
        {
            ArgumentNullException.ThrowIfNull(externalMu);
            ArgumentNullException.ThrowIfNull(signature);

            return VerifyMu(new ReadOnlySpan<byte>(externalMu), new ReadOnlySpan<byte>(signature));
        }

        /// <summary>
        ///   Verifies that a digital signature is valid for the provided externally computed signature mu (&#x3BC;) value.
        /// </summary>
        /// <param name="externalMu">The signature mu value.</param>
        /// <param name="signature">The signature to verify.</param>
        /// <returns>
        ///   <see langword="true"/> if the digital signature is valid for the provided mu value;
        ///   otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">An error occurred while verifying the mu value.</exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The current platform does not support verification with an externally computed mu value.
        /// </exception>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public bool VerifyMu(ReadOnlySpan<byte> externalMu, ReadOnlySpan<byte> signature)
        {
            if (externalMu.Length != Algorithm.MuSizeInBytes || signature.Length != Algorithm.SignatureSizeInBytes)
            {
                return false;
            }

            ThrowIfDisposed();

            return VerifyMuCore(externalMu, signature);
        }

        /// <summary>
        ///   When overridden in a derived class,
        ///   verifies that a digital signature is valid for the provided externally computed signature mu (&#x3BC;) value.
        /// </summary>
        /// <param name="externalMu">The signature mu value.</param>
        /// <param name="signature">The signature to verify.</param>
        /// <returns>
        ///   <see langword="true"/> if the mu value is valid; otherwise, <see langword="false"/>.
        /// </returns>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        protected abstract bool VerifyMuCore(ReadOnlySpan<byte> externalMu, ReadOnlySpan<byte> signature);

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
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public byte[] ExportSubjectPublicKeyInfo()
        {
            ThrowIfDisposed();

            AsnWriter writer = ExportSubjectPublicKeyInfoCore();
            return writer.Encode();
        }

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
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public bool TryExportSubjectPublicKeyInfo(Span<byte> destination, out int bytesWritten)
        {
            ThrowIfDisposed();

            AsnWriter writer = ExportSubjectPublicKeyInfoCore();
            return writer.TryEncode(destination, out bytesWritten);
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
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public string ExportSubjectPublicKeyInfoPem()
        {
            ThrowIfDisposed();

            AsnWriter writer = ExportSubjectPublicKeyInfoCore();

            // SPKI does not contain sensitive data.
            return Helpers.EncodeAsnWriterToPem(PemLabels.SpkiPublicKey, writer, clear: false);
        }

        /// <summary>
        ///   Exports the current key in the PKCS#8 PrivateKeyInfo format.
        /// </summary>
        /// <returns>
        ///   A byte array containing the PKCS#8 PrivateKeyInfo representation of the this key.
        /// </returns>
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
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public byte[] ExportPkcs8PrivateKey()
        {
            ThrowIfDisposed();

            return ExportPkcs8PrivateKeyCallback(static pkcs8 => pkcs8.ToArray());
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
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public bool TryExportPkcs8PrivateKey(Span<byte> destination, out int bytesWritten)
        {
            ThrowIfDisposed();

            // A private key export with no attributes has at least 12 bytes overhead so a buffer smaller than that cannot hold a
            // PKCS#8 encoded key. If we happen to get a buffer smaller than that, it won't export.
            int minimumPossiblePkcs8MLDsaKey =
                2 + // PrivateKeyInfo Sequence
                3 + // Version Integer
                2 + // AlgorithmIdentifier Sequence
                3 + // AlgorithmIdentifier OID value, undervalued to be safe
                2 + // Private key Octet String prefix, undervalued to be safe
                Algorithm.PrivateSeedSizeInBytes;

            if (destination.Length < minimumPossiblePkcs8MLDsaKey)
            {
                bytesWritten = 0;
                return false;
            }

            return TryExportPkcs8PrivateKeyCore(destination, out bytesWritten);
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
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        protected abstract bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten);

        /// <summary>
        ///   Exports the current key in a PEM-encoded representation of the PKCS#8 PrivateKeyInfo format.
        /// </summary>
        /// <returns>
        ///   A string containing the PEM-encoded representation of the PKCS#8 PrivateKeyInfo
        ///   representation of the public-key portion of this key.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while exporting the key.
        /// </exception>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public string ExportPkcs8PrivateKeyPem()
        {
            ThrowIfDisposed();

            return ExportPkcs8PrivateKeyCallback(static pkcs8 => PemEncoding.WriteString(PemLabels.Pkcs8PrivateKey, pkcs8));
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
        ///   A byte array containing the PKCS#8 EncryptedPrivateKeyInfo representation of the this key.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="pbeParameters"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para><paramref name="pbeParameters"/> does not represent a valid password-based encryption algorithm.</para>
        ///   <para>-or-</para>
        ///   <para>This instance only represents a public key.</para>
        ///   <para>-or-</para>
        ///   <para>The private key is not exportable.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public byte[] ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, PbeParameters pbeParameters)
        {
            ArgumentNullException.ThrowIfNull(pbeParameters);
            PasswordBasedEncryption.ValidatePbeParameters(pbeParameters, password, ReadOnlySpan<byte>.Empty);
            ThrowIfDisposed();

            AsnWriter writer = ExportEncryptedPkcs8PrivateKeyCore(password, pbeParameters);

            try
            {
                return writer.Encode();
            }
            finally
            {
                writer.Reset();
            }
        }

        /// <summary>
        ///   Exports the current key in the PKCS#8 EncryptedPrivateKeyInfo format with a byte-based password.
        /// </summary>
        /// <param name="passwordBytes">
        ///   The bytes to use as a password when encrypting the key material.
        /// </param>
        /// <param name="pbeParameters">
        ///   The password-based encryption (PBE) parameters to use when encrypting the key material.
        /// </param>
        /// <returns>
        ///   A byte array containing the PKCS#8 EncryptedPrivateKeyInfo representation of the this key.
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
        ///   <para><paramref name="pbeParameters"/> does not represent a valid password-based encryption algorithm.</para>
        ///   <para>-or-</para>
        ///   <para>This instance only represents a public key.</para>
        ///   <para>-or-</para>
        ///   <para>The private key is not exportable.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public byte[] ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters)
        {
            ArgumentNullException.ThrowIfNull(pbeParameters);
            PasswordBasedEncryption.ValidatePbeParameters(pbeParameters, ReadOnlySpan<char>.Empty, passwordBytes);
            ThrowIfDisposed();

            AsnWriter writer = ExportEncryptedPkcs8PrivateKeyCore(passwordBytes, pbeParameters);

            try
            {
                return writer.Encode();
            }
            finally
            {
                writer.Reset();
            }
        }

        /// <inheritdoc cref="ExportEncryptedPkcs8PrivateKey(ReadOnlySpan{char}, PbeParameters)"/>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="password"/> or <paramref name="pbeParameters"/> is <see langword="null"/>.
        /// </exception>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public byte[] ExportEncryptedPkcs8PrivateKey(string password, PbeParameters pbeParameters)
        {
            ArgumentNullException.ThrowIfNull(password);

            return ExportEncryptedPkcs8PrivateKey(password.AsSpan(), pbeParameters);
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
        ///   <paramref name="pbeParameters"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para><paramref name="pbeParameters"/> does not represent a valid password-based encryption algorithm.</para>
        ///   <para>-or-</para>
        ///   <para>This instance only represents a public key.</para>
        ///   <para>-or-</para>
        ///   <para>The private key is not exportable.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public bool TryExportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<char> password,
            PbeParameters pbeParameters,
            Span<byte> destination,
            out int bytesWritten)
        {
            ArgumentNullException.ThrowIfNull(pbeParameters);
            PasswordBasedEncryption.ValidatePbeParameters(pbeParameters, password, ReadOnlySpan<byte>.Empty);
            ThrowIfDisposed();

            AsnWriter writer = ExportEncryptedPkcs8PrivateKeyCore(password, pbeParameters);

            try
            {
                return writer.TryEncode(destination, out bytesWritten);
            }
            finally
            {
                writer.Reset();
            }
        }

        /// <summary>
        ///   Attempts to export the current key in the PKCS#8 EncryptedPrivateKeyInfo format into a provided buffer,
        ///   using a byte-based password.
        /// </summary>
        /// <param name="passwordBytes">
        ///   The bytes to use as a password when encrypting the key material.
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
        ///   <paramref name="pbeParameters"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para><paramref name="pbeParameters"/> specifies a KDF that requires a char-based password.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="pbeParameters"/> does not represent a valid password-based encryption algorithm.</para>
        ///   <para>-or-</para>
        ///   <para>This instance only represents a public key.</para>
        ///   <para>-or-</para>
        ///   <para>The private key is not exportable.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public bool TryExportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<byte> passwordBytes,
            PbeParameters pbeParameters,
            Span<byte> destination,
            out int bytesWritten)
        {
            ArgumentNullException.ThrowIfNull(pbeParameters);
            PasswordBasedEncryption.ValidatePbeParameters(pbeParameters, ReadOnlySpan<char>.Empty, passwordBytes);
            ThrowIfDisposed();

            AsnWriter writer = ExportEncryptedPkcs8PrivateKeyCore(passwordBytes, pbeParameters);

            try
            {
                return writer.TryEncode(destination, out bytesWritten);
            }
            finally
            {
                writer.Reset();
            }
        }

        /// <inheritdoc cref="TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan{char}, PbeParameters, Span{byte}, out int)"/>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="password"/> or <paramref name="pbeParameters"/> is <see langword="null"/>.
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
        ///   <paramref name="pbeParameters"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para><paramref name="pbeParameters"/> does not represent a valid password-based encryption algorithm.</para>
        ///   <para>-or-</para>
        ///   <para>This instance only represents a public key.</para>
        ///   <para>-or-</para>
        ///   <para>The private key is not exportable.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public string ExportEncryptedPkcs8PrivateKeyPem(
            ReadOnlySpan<char> password,
            PbeParameters pbeParameters)
        {
            ArgumentNullException.ThrowIfNull(pbeParameters);
            PasswordBasedEncryption.ValidatePbeParameters(pbeParameters, password, ReadOnlySpan<byte>.Empty);
            ThrowIfDisposed();

            AsnWriter writer = ExportEncryptedPkcs8PrivateKeyCore(password, pbeParameters);

            // Skip clear since the data is already encrypted.
            return Helpers.EncodeAsnWriterToPem(PemLabels.EncryptedPkcs8PrivateKey, writer, clear: false);
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
        ///   <para><paramref name="pbeParameters"/> does not represent a valid password-based encryption algorithm.</para>
        ///   <para>-or-</para>
        ///   <para>This instance only represents a public key.</para>
        ///   <para>-or-</para>
        ///   <para>The private key is not exportable.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public string ExportEncryptedPkcs8PrivateKeyPem(
            ReadOnlySpan<byte> passwordBytes,
            PbeParameters pbeParameters)
        {
            ArgumentNullException.ThrowIfNull(pbeParameters);
            PasswordBasedEncryption.ValidatePbeParameters(pbeParameters, ReadOnlySpan<char>.Empty, passwordBytes);
            ThrowIfDisposed();

            AsnWriter writer = ExportEncryptedPkcs8PrivateKeyCore(passwordBytes, pbeParameters);

            // Skip clear since the data is already encrypted.
            return Helpers.EncodeAsnWriterToPem(PemLabels.EncryptedPkcs8PrivateKey, writer, clear: false);
        }

        /// <inheritdoc cref="ExportEncryptedPkcs8PrivateKeyPem(ReadOnlySpan{char}, PbeParameters)"/>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="password"/> or <paramref name="pbeParameters"/> is <see langword="null"/>.
        /// </exception>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public string ExportEncryptedPkcs8PrivateKeyPem(
            string password,
            PbeParameters pbeParameters)
        {
            ArgumentNullException.ThrowIfNull(password);

            return ExportEncryptedPkcs8PrivateKeyPem(password.AsSpan(), pbeParameters);
        }

        /// <summary>
        ///   Exports the public-key portion of the current key in the FIPS 204 public key format.
        /// </summary>
        /// <returns>
        ///   The FIPS 204 public key.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        public byte[] ExportMLDsaPublicKey()
        {
            ThrowIfDisposed();

            byte[] destination = new byte[Algorithm.PublicKeySizeInBytes];
            ExportMLDsaPublicKeyCore(destination);
            return destination;
        }

        /// <summary>
        ///   Exports the public-key portion of the current key in the FIPS 204 public key format.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the public key. Its length must be exactly
        ///   <see cref="MLDsaAlgorithm.PublicKeySizeInBytes"/>.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="destination"/> is the incorrect length to receive the public key.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        /// <remarks>
        ///   <paramref name="destination"/> is required to be exactly
        ///   <see cref="MLDsaAlgorithm.PublicKeySizeInBytes"/> in length.
        /// </remarks>
        public void ExportMLDsaPublicKey(Span<byte> destination)
        {
            Helpers.ThrowIfDestinationWrongLength(destination, Algorithm.PublicKeySizeInBytes);
            ThrowIfDisposed();

            ExportMLDsaPublicKeyCore(destination);
        }

        /// <summary>
        ///   Exports the current key in the FIPS 204 private key format.
        /// </summary>
        /// <returns>
        ///   The FIPS 204 private key.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   <para>The current instance cannot export a private key.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        public byte[] ExportMLDsaPrivateKey()
        {
            ThrowIfDisposed();

            byte[] destination = new byte[Algorithm.PrivateKeySizeInBytes];
            ExportMLDsaPrivateKeyCore(destination);
            return destination;
        }

        /// <summary>
        ///   Exports the current key in the FIPS 204 private key format.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the private key. Its length must be exactly
        ///   <see cref="MLDsaAlgorithm.PrivateKeySizeInBytes"/>.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="destination"/> is the incorrect length to receive the private key.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while exporting the key.
        /// </exception>
        public void ExportMLDsaPrivateKey(Span<byte> destination)
        {
            Helpers.ThrowIfDestinationWrongLength(destination, Algorithm.PrivateKeySizeInBytes);
            ThrowIfDisposed();

            ExportMLDsaPrivateKeyCore(destination);
        }

        /// <summary>
        ///   Exports the private seed in the FIPS 204 private seed format.
        /// </summary>
        /// <returns>
        ///   The FIPS 204 private seed.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        public byte[] ExportMLDsaPrivateSeed()
        {
            ThrowIfDisposed();

            byte[] destination = new byte[Algorithm.PrivateSeedSizeInBytes];
            ExportMLDsaPrivateSeedCore(destination);
            return destination;
        }

        /// <summary>
        ///   Exports the private seed of the current key.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the private seed. Its length must be exactly
        ///   <see cref="MLDsaAlgorithm.PrivateSeedSizeInBytes"/>.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="destination"/> is the incorrect length to receive the private seed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while exporting the private seed.
        /// </exception>
        public void ExportMLDsaPrivateSeed(Span<byte> destination)
        {
            Helpers.ThrowIfDestinationWrongLength(destination, Algorithm.PrivateSeedSizeInBytes);
            ThrowIfDisposed();

            ExportMLDsaPrivateSeedCore(destination);
        }

        /// <summary>
        ///   Generates a new ML-DSA key.
        /// </summary>
        /// <param name="algorithm">
        ///   An algorithm identifying what kind of ML-DSA key to generate.
        /// </param>
        /// <returns>
        ///   The generated key.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="algorithm" /> is <see langword="null" />
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occured generating the ML-DSA key.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support ML-DSA. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports ML-DSA.
        /// </exception>
        public static MLDsa GenerateKey(MLDsaAlgorithm algorithm)
        {
            ArgumentNullException.ThrowIfNull(algorithm);

            ThrowIfNotSupported();
            return MLDsaImplementation.GenerateKeyImpl(algorithm);
        }

        /// <summary>
        ///   Imports an ML-DSA public key from an X.509 SubjectPublicKeyInfo structure.
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
        ///     The SubjectPublicKeyInfo value does not represent an ML-DSA key.
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
        ///   The platform does not support ML-DSA. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports ML-DSA.
        /// </exception>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public static MLDsa ImportSubjectPublicKeyInfo(ReadOnlySpan<byte> source)
        {
            Helpers.ThrowIfAsnInvalidLength(source);
            ThrowIfNotSupported();

            unsafe
            {
                fixed (byte* pointer = source)
                {
                    using (PointerMemoryManager<byte> manager = new(pointer, source.Length))
                    {
                        AsnValueReader reader = new AsnValueReader(source, AsnEncodingRules.DER);
                        SubjectPublicKeyInfoAsn.Decode(ref reader, manager.Memory, out SubjectPublicKeyInfoAsn spki);

                        MLDsaAlgorithm algorithm = GetAlgorithmIdentifier(ref spki.Algorithm);
                        ReadOnlySpan<byte> publicKey = spki.SubjectPublicKey.Span;

                        if (publicKey.Length != algorithm.PublicKeySizeInBytes)
                        {
                            throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                        }

                        return MLDsaImplementation.ImportPublicKey(algorithm, spki.SubjectPublicKey.Span);
                    }
                }
            }
        }

        /// <inheritdoc cref="ImportSubjectPublicKeyInfo(ReadOnlySpan{byte})" />
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public static MLDsa ImportSubjectPublicKeyInfo(byte[] source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return ImportSubjectPublicKeyInfo(new ReadOnlySpan<byte>(source));
        }

        /// <summary>
        ///   Imports an ML-DSA private key from a PKCS#8 PrivateKeyInfo structure.
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
        ///     The PrivateKeyInfo value does not represent an ML-DSA key.
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
        ///   The platform does not support ML-DSA. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports ML-DSA.
        /// </exception>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public static MLDsa ImportPkcs8PrivateKey(ReadOnlySpan<byte> source)
        {
            Helpers.ThrowIfAsnInvalidLength(source);
            ThrowIfNotSupported();

            KeyFormatHelper.ReadPkcs8(KnownOids, source, MLDsaKeyReader, out int read, out MLDsa dsa);
            Debug.Assert(read == source.Length);
            return dsa;
        }

        /// <inheritdoc cref="ImportPkcs8PrivateKey(ReadOnlySpan{byte})" />
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public static MLDsa ImportPkcs8PrivateKey(byte[] source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return ImportPkcs8PrivateKey(new ReadOnlySpan<byte>(source));
        }

        /// <summary>
        ///   Imports an ML-DSA private key from a PKCS#8 EncryptedPrivateKeyInfo structure.
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
        ///     The value does not represent an ML-DSA key.
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
        ///   The platform does not support ML-DSA. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports ML-DSA.
        /// </exception>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public static MLDsa ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, ReadOnlySpan<byte> source)
        {
            Helpers.ThrowIfAsnInvalidLength(source);
            ThrowIfNotSupported();

            return KeyFormatHelper.DecryptPkcs8(
                passwordBytes,
                source,
                ImportPkcs8PrivateKey,
                out _);
        }

        /// <summary>
        ///   Imports an ML-DSA private key from a PKCS#8 EncryptedPrivateKeyInfo structure.
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
        ///     The value does not represent an ML-DSA key.
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
        ///   The platform does not support ML-DSA. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports ML-DSA.
        /// </exception>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public static MLDsa ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, ReadOnlySpan<byte> source)
        {
            Helpers.ThrowIfAsnInvalidLength(source);
            ThrowIfNotSupported();

            return KeyFormatHelper.DecryptPkcs8(
                password,
                source,
                ImportPkcs8PrivateKey,
                out _);
        }

        /// <inheritdoc cref="ImportEncryptedPkcs8PrivateKey(ReadOnlySpan{char}, ReadOnlySpan{byte})" />
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="password" /> or <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public static MLDsa ImportEncryptedPkcs8PrivateKey(string password, byte[] source)
        {
            ArgumentNullException.ThrowIfNull(password);
            ArgumentNullException.ThrowIfNull(source);

            return ImportEncryptedPkcs8PrivateKey(password.AsSpan(), new ReadOnlySpan<byte>(source));
        }

        /// <summary>
        ///   Imports an ML-DSA key from an RFC 7468 PEM-encoded string.
        /// </summary>
        /// <param name="source">
        ///   The text of the PEM key to import.
        /// </param>
        /// <returns>
        ///   The imported ML-DSA key.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <para><paramref name="source" /> contains an encrypted PEM-encoded key.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="source" /> contains multiple PEM-encoded ML-DSA keys.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="source" /> contains no PEM-encoded ML-DSA keys.</para>
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while importing the key.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support ML-DSA. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports ML-DSA.
        /// </exception>
        /// <remarks>
        ///   <para>
        ///     Unsupported or malformed PEM-encoded objects will be ignored. If multiple supported PEM labels
        ///     are found, an exception is raised to prevent importing a key when the key is ambiguous.
        ///   </para>
        ///   <para>
        ///     This method supports the following PEM labels:
        ///     <list type="bullet">
        ///       <item><description>PUBLIC KEY</description></item>
        ///       <item><description>PRIVATE KEY</description></item>
        ///     </list>
        ///   </para>
        /// </remarks>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public static MLDsa ImportFromPem(ReadOnlySpan<char> source)
        {
            ThrowIfNotSupported();

            return PemKeyHelpers.ImportFactoryPem<MLDsa>(source, label =>
                label switch
                {
                    PemLabels.Pkcs8PrivateKey => ImportPkcs8PrivateKey,
                    PemLabels.SpkiPublicKey => ImportSubjectPublicKeyInfo,
                    _ => null,
                });
        }

        /// <inheritdoc cref="ImportFromPem(ReadOnlySpan{char})" />
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public static MLDsa ImportFromPem(string source)
        {
            ArgumentNullException.ThrowIfNull(source);
            ThrowIfNotSupported();

            return ImportFromPem(source.AsSpan());
        }

        /// <summary>
        ///   Imports an ML-DSA key from an encrypted RFC 7468 PEM-encoded string.
        /// </summary>
        /// <param name="source">
        ///   The PEM text of the encrypted key to import.</param>
        /// <param name="password">
        ///   The password to use for decrypting the key material.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///     <paramref name="source"/> does not contain a PEM-encoded key with a recognized label.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="source"/> contains multiple PEM-encoded keys with a recognized label.
        ///   </para>
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     The password is incorrect.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The base-64 decoded contents of the PEM text from <paramref name="source" />
        ///     do not represent an ASN.1-BER-encoded PKCS#8 EncryptedPrivateKeyInfo structure.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The base-64 decoded contents of the PEM text from <paramref name="source" />
        ///     indicate the key is for an algorithm other than the algorithm
        ///     represented by this instance.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The base-64 decoded contents of the PEM text from <paramref name="source" />
        ///     represent the key in a format that is not supported.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     An error occurred while importing the key.
        ///   </para>
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support ML-DSA. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports ML-DSA.
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
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public static MLDsa ImportFromEncryptedPem(ReadOnlySpan<char> source, ReadOnlySpan<char> password)
        {
            ThrowIfNotSupported();

            return PemKeyHelpers.ImportEncryptedFactoryPem<MLDsa, char>(
                source,
                password,
                ImportEncryptedPkcs8PrivateKey);
        }

        /// <summary>
        ///   Imports an ML-DSA key from an encrypted RFC 7468 PEM-encoded string.
        /// </summary>
        /// <param name="source">
        ///   The PEM text of the encrypted key to import.</param>
        /// <param name="passwordBytes">
        ///   The bytes to use as a password when decrypting the key material.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///     <paramref name="source"/> does not contain a PEM-encoded key with a recognized label.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="source"/> contains multiple PEM-encoded keys with a recognized label.
        ///   </para>
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     The password is incorrect.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The base-64 decoded contents of the PEM text from <paramref name="source" />
        ///     do not represent an ASN.1-BER-encoded PKCS#8 EncryptedPrivateKeyInfo structure.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The base-64 decoded contents of the PEM text from <paramref name="source" />
        ///     indicate the key is for an algorithm other than the algorithm
        ///     represented by this instance.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The base-64 decoded contents of the PEM text from <paramref name="source" />
        ///     represent the key in a format that is not supported.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     An error occurred while importing the key.
        ///   </para>
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support ML-DSA. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports ML-DSA.
        /// </exception>
        /// <remarks>
        ///   <para>
        ///     Unsupported or malformed PEM-encoded objects will be ignored. If multiple supported PEM labels
        ///     are found, an exception is thrown to prevent importing a key when
        ///     the key is ambiguous.
        ///   </para>
        ///   <para>This method supports the <c>ENCRYPTED PRIVATE KEY</c> PEM label.</para>
        /// </remarks>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public static MLDsa ImportFromEncryptedPem(ReadOnlySpan<char> source, ReadOnlySpan<byte> passwordBytes)
        {
            ThrowIfNotSupported();

            return PemKeyHelpers.ImportEncryptedFactoryPem<MLDsa, byte>(
                source,
                passwordBytes,
                ImportEncryptedPkcs8PrivateKey);
        }

        /// <inheritdoc cref="ImportFromEncryptedPem(ReadOnlySpan{char}, ReadOnlySpan{char})" />
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> or <paramref name="password" /> is <see langword="null" />.
        /// </exception>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public static MLDsa ImportFromEncryptedPem(string source, string password)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(password);
            ThrowIfNotSupported();

            return ImportFromEncryptedPem(source.AsSpan(), password.AsSpan());
        }

        /// <inheritdoc cref="ImportFromEncryptedPem(ReadOnlySpan{char}, ReadOnlySpan{byte})" />
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> or <paramref name="passwordBytes" /> is <see langword="null" />.
        /// </exception>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public static MLDsa ImportFromEncryptedPem(string source, byte[] passwordBytes)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(passwordBytes);
            ThrowIfNotSupported();

            return ImportFromEncryptedPem(source.AsSpan(), new ReadOnlySpan<byte>(passwordBytes));
        }

        /// <summary>
        ///   Imports an ML-DSA public key in the FIPS 204 public key format.
        /// </summary>
        /// <param name="algorithm">
        ///   The specific ML-DSA algorithm for this key.
        /// </param>
        /// <param name="source">
        ///   The bytes of a FIPS 204 public key.
        /// </param>
        /// <returns>
        ///   The imported key.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     <paramref name="algorithm"/> is not a valid ML-DSA algorithm identifier.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="source"/> is not the correct size for the specified algorithm.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     An error occurred while importing the key.
        ///   </para>
        /// </exception>
        public static MLDsa ImportMLDsaPublicKey(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            ArgumentNullException.ThrowIfNull(algorithm);

            if (source.Length != algorithm.PublicKeySizeInBytes)
            {
                throw new ArgumentException(SR.Cryptography_KeyWrongSizeForAlgorithm, nameof(source));
            }

            ThrowIfNotSupported();
            return MLDsaImplementation.ImportPublicKey(algorithm, source);
        }

        /// <inheritdoc cref="ImportMLDsaPublicKey(MLDsaAlgorithm, ReadOnlySpan{byte})" />
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="algorithm"/> or <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        public static MLDsa ImportMLDsaPublicKey(MLDsaAlgorithm algorithm, byte[] source)
        {
            ArgumentNullException.ThrowIfNull(algorithm);
            ArgumentNullException.ThrowIfNull(source);

            return ImportMLDsaPublicKey(algorithm, new ReadOnlySpan<byte>(source));
        }

        /// <summary>
        ///   Imports an ML-DSA private key in the FIPS 204 private key format.
        /// </summary>
        /// <param name="algorithm">
        ///   The specific ML-DSA algorithm for this key.
        /// </param>
        /// <param name="source">
        ///   The bytes of a FIPS 204 private key.
        /// </param>
        /// <returns>
        ///   The imported key.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     <paramref name="algorithm"/> is not a valid ML-DSA algorithm identifier.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="source"/> is not the correct size for the specified algorithm.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     An error occurred while importing the key.
        ///   </para>
        /// </exception>
        public static MLDsa ImportMLDsaPrivateKey(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            ArgumentNullException.ThrowIfNull(algorithm);

            if (source.Length != algorithm.PrivateKeySizeInBytes)
            {
                throw new ArgumentException(SR.Argument_PrivateKeyWrongSizeForAlgorithm, nameof(source));
            }

            ThrowIfNotSupported();
            return MLDsaImplementation.ImportPrivateKey(algorithm, source);
        }

        /// <inheritdoc cref="ImportMLDsaPrivateKey(MLDsaAlgorithm, ReadOnlySpan{byte})" />
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="algorithm"/> or <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        public static MLDsa ImportMLDsaPrivateKey(MLDsaAlgorithm algorithm, byte[] source)
        {
            ArgumentNullException.ThrowIfNull(algorithm);
            ArgumentNullException.ThrowIfNull(source);

            return ImportMLDsaPrivateKey(algorithm, new ReadOnlySpan<byte>(source));
        }

        /// <summary>
        ///   Imports an ML-DSA private key from its private seed value.
        /// </summary>
        /// <param name="algorithm">
        ///   The specific ML-DSA algorithm for this key.
        /// </param>
        /// <param name="source">
        ///   The bytes the key seed.
        /// </param>
        /// <returns>
        ///   The imported key.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     <paramref name="algorithm"/> is not a valid ML-DSA algorithm identifier.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="source"/> is not the correct size for the specified algorithm.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     An error occurred while importing the key.
        ///   </para>
        /// </exception>
        public static MLDsa ImportMLDsaPrivateSeed(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            ArgumentNullException.ThrowIfNull(algorithm);

            if (source.Length != algorithm.PrivateSeedSizeInBytes)
            {
                throw new ArgumentException(SR.Cryptography_KeyWrongSizeForAlgorithm, nameof(source));
            }

            ThrowIfNotSupported();
            return MLDsaImplementation.ImportSeed(algorithm, source);
        }

        /// <inheritdoc cref="ImportMLDsaPrivateSeed(MLDsaAlgorithm, ReadOnlySpan{byte})" />
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="algorithm"/> or <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        public static MLDsa ImportMLDsaPrivateSeed(MLDsaAlgorithm algorithm, byte[] source)
        {
            ArgumentNullException.ThrowIfNull(algorithm);
            ArgumentNullException.ThrowIfNull(source);

            return ImportMLDsaPrivateSeed(algorithm, new ReadOnlySpan<byte>(source));
        }

        /// <summary>
        ///   Called by the <c>Dispose()</c> and <c>Finalize()</c> methods to release the managed and unmanaged
        ///   resources used by the current instance of the <see cref="MLDsa"/> class.
        /// </summary>
        /// <param name="disposing">
        ///   <see langword="true" /> to release managed and unmanaged resources;
        ///   <see langword="false" /> to release only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
        }

        /// <summary>
        ///   When overridden in a derived class, computes the signature of the specified data and context,
        ///   writing it into the provided buffer.
        /// </summary>
        /// <param name="data">
        ///   The data to sign.
        /// </param>
        /// <param name="context">
        ///   The signature context.
        /// </param>
        /// <param name="destination">
        ///   The buffer to receive the signature, which will always be the exactly correct size for the algorithm.
        /// </param>
        /// <exception cref="CryptographicException">
        ///   An error occurred while signing the data.
        /// </exception>
        protected abstract void SignDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination);

        /// <summary>
        ///   When overridden in a derived class, verifies the signature of the specified data and context.
        /// </summary>
        /// <param name="data">
        ///   The data to verify.
        /// </param>
        /// <param name="context">
        ///   The signature context.
        /// </param>
        /// <param name="signature">
        ///   The signature to verify.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the signature validates the data; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   An error occurred while verifying the data.
        /// </exception>
        protected abstract bool VerifyDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature);

        /// <summary>
        ///   When overridden in a derived class, computes the pre-hash signature of the specified hash and context,
        ///   writing it into the provided buffer.
        /// </summary>
        /// <param name="hash">
        ///   The hash to sign.
        /// </param>
        /// <param name="context">
        ///   The signature context.
        /// </param>
        /// <param name="hashAlgorithmOid">
        ///   The OID of the hash algorithm used to create the hash.
        /// </param>
        /// <param name="destination">
        ///   The buffer to receive the signature, which will always be the exactly correct size for the algorithm.
        /// </param>
        /// <exception cref="CryptographicException">
        ///   An error occurred while signing the hash.
        /// </exception>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        protected abstract void SignPreHashCore(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> context, string hashAlgorithmOid, Span<byte> destination);

        /// <summary>
        ///   When overridden in a derived class, verifies the pre-hash signature of the specified hash and context.
        /// </summary>
        /// <param name="hash">
        ///   The data to verify.
        /// </param>
        /// <param name="context">
        ///   The signature context.
        /// </param>
        /// <param name="hashAlgorithmOid">
        ///  The OID of the hash algorithm used to create the hash.
        /// </param>
        /// <param name="signature">
        ///   The signature to verify.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the signature validates the hash; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   An error occurred while verifying the hash.
        /// </exception>
        [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        protected abstract bool VerifyPreHashCore(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> context, string hashAlgorithmOid, ReadOnlySpan<byte> signature);

        /// <summary>
        ///   When overridden in a derived class, exports the FIPS 204 public key to the specified buffer.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the public key.
        /// </param>
        protected abstract void ExportMLDsaPublicKeyCore(Span<byte> destination);

        /// <summary>
        ///   When overridden in a derived class, exports the FIPS 204 private key to the specified buffer.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the private key.
        /// </param>
        protected abstract void ExportMLDsaPrivateKeyCore(Span<byte> destination);

        /// <summary>
        ///   When overridden in a derived class, exports the private seed to the specified buffer.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the private seed.
        /// </param>
        protected abstract void ExportMLDsaPrivateSeedCore(Span<byte> destination);

        private AsnWriter ExportSubjectPublicKeyInfoCore()
        {
            int publicKeySizeInBytes = Algorithm.PublicKeySizeInBytes;
            byte[] rented = CryptoPool.Rent(publicKeySizeInBytes);

            try
            {
                Span<byte> publicKey = rented.AsSpan(0, publicKeySizeInBytes);
                ExportMLDsaPublicKeyCore(publicKey);

                // The ASN.1 overhead of a SubjectPublicKeyInfo encoding a public key is 22 bytes.
                // Round it off to 32. This checked operation should never throw because the inputs are not
                // user provided.
                int capacity = checked(32 + publicKeySizeInBytes);
                AsnWriter writer = new AsnWriter(AsnEncodingRules.DER, capacity);

                using (writer.PushSequence())
                {
                    using (writer.PushSequence())
                    {
                        writer.WriteObjectIdentifier(Algorithm.Oid);
                    }

                    writer.WriteBitString(publicKey);
                }

                Debug.Assert(writer.GetEncodedLength() <= capacity);
                return writer;
            }
            finally
            {
                // Public key does not need to be cleared.
                CryptoPool.Return(rented, clearSize: 0);
            }
        }

        private AsnWriter ExportEncryptedPkcs8PrivateKeyCore(ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters)
        {
            AsnWriter tmp = ExportPkcs8PrivateKeyCallback(static pkcs8 =>
            {
                AsnWriter writer = new(AsnEncodingRules.BER, initialCapacity: pkcs8.Length);

                try
                {
                    writer.WriteEncodedValueForCrypto(pkcs8);
                }
                catch
                {
                    writer.Reset();
                    throw;
                }

                return writer;
            });

            try
            {
                return KeyFormatHelper.WriteEncryptedPkcs8(passwordBytes, tmp, pbeParameters);
            }
            finally
            {
                tmp.Reset();
            }
        }

        private AsnWriter ExportEncryptedPkcs8PrivateKeyCore(ReadOnlySpan<char> password, PbeParameters pbeParameters)
        {
            AsnWriter tmp = ExportPkcs8PrivateKeyCallback(static pkcs8 =>
            {
                AsnWriter writer = new(AsnEncodingRules.BER, initialCapacity: pkcs8.Length);

                try
                {
                    writer.WriteEncodedValueForCrypto(pkcs8);
                }
                catch
                {
                    writer.Reset();
                    throw;
                }

                return writer;
            });

            try
            {
                return KeyFormatHelper.WriteEncryptedPkcs8(password, tmp, pbeParameters);
            }
            finally
            {
                tmp.Reset();
            }
        }

        private TResult ExportPkcs8PrivateKeyCallback<TResult>(ExportPkcs8PrivateKeyFunc<TResult> func)
        {
            // A PKCS#8 ML-DSA private key has an ASN.1 overhead of 28 bytes, assuming no attributes.
            // Make it an even 32 and that should give a good starting point for a buffer size.
            // The private key is always larger than the seed so this buffer size can accommodate both.
            int size = Algorithm.PrivateKeySizeInBytes + 32;
            // The buffer is only being passed out as a span, so the derived type can't meaningfully
            // hold on to it without being malicious.
            byte[] buffer = CryptoPool.Rent(size);
            int written;

            while (!TryExportPkcs8PrivateKeyCore(buffer, out written))
            {
                CryptoPool.Return(buffer);
                size = checked(size * 2);
                buffer = CryptoPool.Rent(size);
            }

            if ((uint)written > buffer.Length)
            {
                // We got a nonsense value written back. Clear the buffer, but don't put it back in the pool.
                CryptographicOperations.ZeroMemory(buffer);
                throw new CryptographicException();
            }

            try
            {
                return func(buffer.AsSpan(0, written));
            }
            finally
            {
                CryptoPool.Return(buffer, written);
            }
        }

        private static void MLDsaKeyReader(
            ReadOnlyMemory<byte> privateKeyContents,
            in AlgorithmIdentifierAsn algorithmIdentifier,
            out MLDsa dsa)
        {
            MLDsaAlgorithm algorithm = GetAlgorithmIdentifier(in algorithmIdentifier);
            MLDsaPrivateKeyAsn dsaKey = MLDsaPrivateKeyAsn.Decode(privateKeyContents, AsnEncodingRules.BER);

            if (dsaKey.Seed is ReadOnlyMemory<byte> seed)
            {
                if (seed.Length != algorithm.PrivateSeedSizeInBytes)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                dsa = MLDsaImplementation.ImportMLDsaPrivateSeed(algorithm, seed.Span);
            }
            else if (dsaKey.ExpandedKey is ReadOnlyMemory<byte> expandedKey)
            {
                if (expandedKey.Length != algorithm.PrivateKeySizeInBytes)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                dsa = MLDsaImplementation.ImportPrivateKey(algorithm, expandedKey.Span);
            }
            else if (dsaKey.Both is MLDsaPrivateKeyBothAsn both)
            {
                int privateKeySize = algorithm.PrivateKeySizeInBytes;

                if (both.Seed.Length != algorithm.PrivateSeedSizeInBytes ||
                    both.ExpandedKey.Length != privateKeySize)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                MLDsa key = MLDsaImplementation.ImportMLDsaPrivateSeed(algorithm, both.Seed.Span);
                byte[] rent = CryptoPool.Rent(privateKeySize);
                Span<byte> buffer = rent.AsSpan(0, privateKeySize);

                try
                {
                    key.ExportMLDsaPrivateKey(buffer);

                    if (CryptographicOperations.FixedTimeEquals(buffer, both.ExpandedKey.Span))
                    {
                        dsa = key;
                    }
                    else
                    {
                        throw new CryptographicException(SR.Cryptography_MLDsaPkcs8KeyMismatch);
                    }
                }
                catch
                {
                    key.Dispose();
                    throw;
                }
                finally
                {
                    CryptoPool.Return(rent, privateKeySize);
                }
            }
            else
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }
        }

        private static MLDsaAlgorithm GetAlgorithmIdentifier(ref readonly AlgorithmIdentifierAsn identifier)
        {
            MLDsaAlgorithm algorithm = MLDsaAlgorithm.GetMLDsaAlgorithmFromOid(identifier.Algorithm) ??
                throw new CryptographicException(
                    SR.Format(SR.Cryptography_UnknownAlgorithmIdentifier, identifier.Algorithm));

            if (identifier.Parameters.HasValue)
            {
                AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
                identifier.Encode(writer);
                throw Helpers.CreateAlgorithmUnknownException(writer);
            }

            return algorithm;
        }

        internal static void ThrowIfNotSupported()
        {
            if (!IsSupported)
            {
                throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_AlgorithmNotSupported, nameof(MLDsa)));
            }
        }

        // Returns a hash algorithm identifier for an OID.
        // insufficientCollisionResistance is true if the hash algorithm is known, but does not meet the required
        // collision resistance from FIPS 204.
        private protected string? MapHashOidToAlgorithm(
            string hashOid,
            out int hashLengthInBytes,
            out bool insufficientCollisionResistance)
        {
            int hashLambda;
            string hashAlgorithmIdentifier;

            switch (hashOid)
            {
                case Oids.Md5:
                    hashLengthInBytes = 128 / 8;
                    insufficientCollisionResistance = true;
                    return HashAlgorithmNames.MD5;
                case Oids.Sha1:
                    hashLengthInBytes = 160 / 8;
                    insufficientCollisionResistance = true;
                    return HashAlgorithmNames.SHA1;
                case Oids.Sha256:
                    hashLengthInBytes = 256 / 8;
                    hashLambda = 256 / 2;
                    hashAlgorithmIdentifier = HashAlgorithmNames.SHA256;
                    break;
                case Oids.Sha3_256:
                    hashLengthInBytes = 256 / 8;
                    hashLambda = 256 / 2;
                    hashAlgorithmIdentifier = HashAlgorithmNames.SHA3_256;
                    break;
                case Oids.Sha384:
                    hashLengthInBytes = 384 / 8;
                    hashLambda = 384 / 2;
                    hashAlgorithmIdentifier = HashAlgorithmNames.SHA384;
                    break;
                case Oids.Sha3_384:
                    hashLengthInBytes = 384 / 8;
                    hashLambda = 384 / 2;
                    hashAlgorithmIdentifier = HashAlgorithmNames.SHA3_384;
                    break;
                case Oids.Sha512:
                    hashLengthInBytes = 512 / 8;
                    hashLambda = 512 / 2;
                    hashAlgorithmIdentifier = HashAlgorithmNames.SHA512;
                    break;
                case Oids.Sha3_512:
                    hashLengthInBytes = 512 / 8;
                    hashLambda = 512 / 2;
                    hashAlgorithmIdentifier = HashAlgorithmNames.SHA3_512;
                    break;
                case Oids.Shake128: // SHAKE-128 with 256-bits of output
                    hashLengthInBytes = 256 / 8;
                    hashLambda = 256 / 2;
                    hashAlgorithmIdentifier = HashAlgorithmNames.SHAKE128;
                    break;
                case Oids.Shake256: // SHAKE-256 with 512-bits of output
                    hashLengthInBytes = 512 / 8;
                    hashLambda = 512 / 2;
                    hashAlgorithmIdentifier = HashAlgorithmNames.SHAKE256;
                    break;
                default:
                    hashLengthInBytes = 0;
                    insufficientCollisionResistance = false;
                    return null;
            }

            insufficientCollisionResistance = hashLambda < Algorithm.LambdaCollisionStrength;
            return hashAlgorithmIdentifier;
        }

        private delegate TResult ExportPkcs8PrivateKeyFunc<TResult>(ReadOnlySpan<byte> pkcs8);
    }
}
