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
    ///   Represents an SLH-DSA key.
    /// </summary>
    /// <remarks>
    ///   Developers are encouraged to program against the <c>SlhDsa</c> base class,
    ///   rather than any specific derived class.
    ///   The derived classes are intended for interop with the underlying system
    ///   cryptographic libraries.
    /// </remarks>
    [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    public abstract partial class SlhDsa : IDisposable
#if DESIGNTIMEINTERFACES
#pragma warning disable SA1001
        , IImportExportShape<SlhDsa>
#pragma warning restore SA1001
#endif
    {
        private static readonly string[] s_knownOids =
        [
            Oids.SlhDsaSha2_128s,
            Oids.SlhDsaShake128s,
            Oids.SlhDsaSha2_128f,
            Oids.SlhDsaShake128f,
            Oids.SlhDsaSha2_192s,
            Oids.SlhDsaShake192s,
            Oids.SlhDsaSha2_192f,
            Oids.SlhDsaShake192f,
            Oids.SlhDsaSha2_256s,
            Oids.SlhDsaShake256s,
            Oids.SlhDsaSha2_256f,
            Oids.SlhDsaShake256f,
        ];

        private const int MaxContextLength = 255;

        private bool _disposed;

        /// <summary>
        ///   Initializes a new instance of the <see cref="SlhDsa" /> class.
        /// </summary>
        /// <param name="algorithm">
        ///   The specific SLH-DSA algorithm for this key.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="algorithm" /> is <see langword="null" />.
        /// </exception>
        protected SlhDsa(SlhDsaAlgorithm algorithm)
        {
            ArgumentNullException.ThrowIfNull(algorithm);
            Algorithm = algorithm;
        }

        /// <summary>
        ///   Throws <see cref="ObjectDisposedException" /> if the current instance is disposed.
        /// </summary>
        private protected void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, typeof(SlhDsa));

        /// <summary>
        ///   Gets a value indicating whether the current platform supports SLH-DSA.
        /// </summary>
        /// <value>
        ///   <see langword="true" /> if the current platform supports SLH-DSA; otherwise, <see langword="false" />.
        /// </value>
        public static bool IsSupported { get; } = SlhDsaImplementation.SupportsAny();

        /// <summary>
        ///   Gets the specific SLH-DSA algorithm for this key.
        /// </summary>
        /// <value>
        ///   The specific SLH-DSA algorithm for this key.
        /// </value>
        public SlhDsaAlgorithm Algorithm { get; }

        /// <summary>
        ///   Releases all resources used by the <see cref="SlhDsa"/> class.
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
        ///   Signs the specified data, writing the signature into the provided buffer.
        /// </summary>
        /// <param name="data">
        ///   The data to sign.
        /// </param>
        /// <param name="destination">
        ///   The buffer to receive the signature. Its length must be exactly
        ///   <see cref="SlhDsaAlgorithm.SignatureSizeInBytes"/>.
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
            int signatureSizeInBytes = Algorithm.SignatureSizeInBytes;

            if (destination.Length != signatureSizeInBytes)
            {
                throw new ArgumentException(
                    SR.Format(SR.Argument_DestinationImprecise, signatureSizeInBytes),
                    nameof(destination));
            }

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
        ///   Signs the specified hash using the FIPS 205 pre-hash signing algorithm, writing the signature into the provided buffer.
        /// </summary>
        /// <param name="hash">
        ///   The hash to sign.
        /// </param>
        /// <param name="destination">
        ///   The buffer to receive the signature. Its length must be exactly
        ///   <see cref="SlhDsaAlgorithm.SignatureSizeInBytes"/>.
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
        public void SignPreHash(ReadOnlySpan<byte> hash, Span<byte> destination, string hashAlgorithmOid, ReadOnlySpan<byte> context = default)
        {
            ArgumentNullException.ThrowIfNull(hashAlgorithmOid);

            if (destination.Length != Algorithm.SignatureSizeInBytes)
            {
                throw new ArgumentException(
                    SR.Format(SR.Argument_DestinationImprecise, Algorithm.SignatureSizeInBytes),
                    nameof(destination));
            }

            if (context.Length > MaxContextLength)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(context),
                    context.Length,
                    SR.Argument_SignatureContextTooLong255);
            }

            ValidateHashAlgorithm(hash, hashAlgorithmOid);
            ThrowIfDisposed();

            SignPreHashCore(hash, context, hashAlgorithmOid, destination);
        }

        /// <summary>
        ///   Signs the specified hash using the FIPS 205 pre-hash signing algorithm.
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
        ///   <para>The instance represents only a public key.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while signing the hash.</para>
        /// </exception>
        /// <remarks>
        ///   A <see langword="null" /> context is treated as empty.
        /// </remarks>
        public byte[] SignPreHash(byte[] hash, string hashAlgorithmOid, byte[]? context = default)
        {
            ArgumentNullException.ThrowIfNull(hash);
            ArgumentNullException.ThrowIfNull(hashAlgorithmOid);

            byte[] destination = new byte[Algorithm.SignatureSizeInBytes];
            SignPreHash(new ReadOnlySpan<byte>(hash), destination.AsSpan(), hashAlgorithmOid, new ReadOnlySpan<byte>(context));
            return destination;
        }

        /// <summary>
        ///   Verifies that the specified FIPS 205 pre-hash signature is valid for this key and the provided hash.
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

            ValidateHashAlgorithm(hash, hashAlgorithmOid);
            ThrowIfDisposed();

            if (signature.Length != Algorithm.SignatureSizeInBytes)
            {
                return false;
            }

            return VerifyPreHashCore(hash, context, hashAlgorithmOid, signature);
        }

        /// <summary>
        ///   Verifies that the specified FIPS 205 pre-hash signature is valid for this key and the provided hash.
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
        public bool TryExportSubjectPublicKeyInfo(Span<byte> destination, out int bytesWritten)
        {
            ThrowIfDisposed();

            return ExportSubjectPublicKeyInfoCore().TryEncode(destination, out bytesWritten);
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
        public string ExportSubjectPublicKeyInfoPem()
        {
            ThrowIfDisposed();

            AsnWriter writer = ExportSubjectPublicKeyInfoCore();
            // SPKI does not contain sensitive data.
            return EncodeAsnWriterToPem(PemLabels.SpkiPublicKey, writer, clear: false);
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
        public bool TryExportPkcs8PrivateKey(Span<byte> destination, out int bytesWritten)
        {
            ThrowIfDisposed();

            // A private key export with no attributes has at least 12 bytes overhead so a buffer smaller than that cannot hold a
            // PKCS#8 encoded key. If we happen to get a buffer smaller than that, it won't export.
            int MinimumPossiblePkcs8SlhDsaKey =
                2 + // PrivateKeyInfo Sequence
                3 + // Version Integer
                2 + // AlgorithmIdentifier Sequence
                3 + // AlgorithmIdentifier OID value, undervalued to be safe
                2 + // Secret key Octet String prefix, undervalued to be safe
                Algorithm.SecretKeySizeInBytes;

            if (destination.Length < MinimumPossiblePkcs8SlhDsaKey)
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
        protected virtual bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten)
        {
            // Secret key size for SLH-DSA is at most 128 bytes so we can stack allocate it.
            int secretKeySizeInBytes = Algorithm.SecretKeySizeInBytes;
            Debug.Assert(secretKeySizeInBytes is <= 128);
            Span<byte> secretKey = (stackalloc byte[128])[..secretKeySizeInBytes];

            try
            {
                ExportSlhDsaSecretKey(secretKey);

                // The ASN.1 overhead of a PrivateKeyInfo encoding a private key is 22 bytes.
                // Round it off to 32. This checked operation should never throw because the inputs are not
                // user provided.
                int capacity = checked(32 + secretKeySizeInBytes);
                AsnWriter writer = new AsnWriter(AsnEncodingRules.DER, capacity);

                using (writer.PushSequence())
                {
                    writer.WriteInteger(0); // Version

                    using (writer.PushSequence())
                    {
                        writer.WriteObjectIdentifier(Algorithm.Oid);
                    }

                    writer.WriteOctetString(secretKey);
                }

                Debug.Assert(writer.GetEncodedLength() <= capacity);
                return writer.TryEncode(destination, out bytesWritten);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(secretKey);
            }
        }

        /// <summary>
        ///   Exports the current key in a PEM-encoded representation of the PKCS#8 PrivateKeyInfo format.
        /// </summary>
        /// <returns>
        ///   A string containing the PEM-encoded representation of the PKCS#8 PrivateKeyInfo.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while exporting the key.
        /// </exception>
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
        public string ExportEncryptedPkcs8PrivateKeyPem(
            ReadOnlySpan<char> password,
            PbeParameters pbeParameters)
        {
            ArgumentNullException.ThrowIfNull(pbeParameters);
            PasswordBasedEncryption.ValidatePbeParameters(pbeParameters, password, ReadOnlySpan<byte>.Empty);
            ThrowIfDisposed();

            AsnWriter writer = ExportEncryptedPkcs8PrivateKeyCore(password, pbeParameters);

            try
            {
                // Skip clear since the data is already encrypted.
                return EncodeAsnWriterToPem(PemLabels.EncryptedPkcs8PrivateKey, writer, clear: false);
            }
            finally
            {
                writer.Reset();
            }
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
        public string ExportEncryptedPkcs8PrivateKeyPem(
            ReadOnlySpan<byte> passwordBytes,
            PbeParameters pbeParameters)
        {
            ArgumentNullException.ThrowIfNull(pbeParameters);
            PasswordBasedEncryption.ValidatePbeParameters(pbeParameters, ReadOnlySpan<char>.Empty, passwordBytes);
            ThrowIfDisposed();

            AsnWriter writer = ExportEncryptedPkcs8PrivateKeyCore(passwordBytes, pbeParameters);

            try
            {
                // Skip clear since the data is already encrypted.
                return EncodeAsnWriterToPem(PemLabels.EncryptedPkcs8PrivateKey, writer, clear: false);
            }
            finally
            {
                writer.Reset();
            }
        }

        /// <inheritdoc cref="ExportEncryptedPkcs8PrivateKeyPem(ReadOnlySpan{char}, PbeParameters)"/>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="password"/> or <paramref name="pbeParameters"/> is <see langword="null"/>.
        /// </exception>
        public string ExportEncryptedPkcs8PrivateKeyPem(
            string password,
            PbeParameters pbeParameters)
        {
            ArgumentNullException.ThrowIfNull(password);

            return ExportEncryptedPkcs8PrivateKeyPem(password.AsSpan(), pbeParameters);
        }

        /// <summary>
        ///   Exports the public-key portion of the current key in the FIPS 205 public key format.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the public key. Its length must be exactly
        ///   <see cref="SlhDsaAlgorithm.PublicKeySizeInBytes"/>.
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
        ///   <see cref="SlhDsaAlgorithm.PublicKeySizeInBytes"/> in length.
        /// </remarks>
        public void ExportSlhDsaPublicKey(Span<byte> destination)
        {
            int publicKeySizeInBytes = Algorithm.PublicKeySizeInBytes;

            if (destination.Length != publicKeySizeInBytes)
            {
                throw new ArgumentException(
                    SR.Format(SR.Argument_DestinationImprecise, publicKeySizeInBytes),
                    nameof(destination));
            }

            ThrowIfDisposed();

            ExportSlhDsaPublicKeyCore(destination);
        }

        /// <summary>
        ///   Exports the public-key portion of the current key in the FIPS 205 public key format.
        /// </summary>
        /// <returns>
        ///   The FIPS 205 public key.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        public byte[] ExportSlhDsaPublicKey()
        {
            ThrowIfDisposed();

            byte[] destination = new byte[Algorithm.PublicKeySizeInBytes];
            ExportSlhDsaPublicKeyCore(destination);
            return destination;
        }

        /// <summary>
        ///   Exports the current key in the FIPS 205 secret key format.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the secret key. Its length must be exactly
        ///   <see cref="SlhDsaAlgorithm.SecretKeySizeInBytes"/>.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="destination"/> is the incorrect length to receive the secret key.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>The current instance cannot export a secret key.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        public void ExportSlhDsaSecretKey(Span<byte> destination)
        {
            int secretKeySizeInBytes = Algorithm.SecretKeySizeInBytes;

            if (destination.Length != secretKeySizeInBytes)
            {
                throw new ArgumentException(
                    SR.Format(SR.Argument_DestinationImprecise, secretKeySizeInBytes),
                    nameof(destination));
            }

            ThrowIfDisposed();

            ExportSlhDsaSecretKeyCore(destination);
        }

        /// <summary>
        ///   Exports the current key in the FIPS 205 secret key format.
        /// </summary>
        /// <returns>
        ///   The FIPS 205 secret key.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   <para>The current instance cannot export a secret key.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        public byte[] ExportSlhDsaSecretKey()
        {
            ThrowIfDisposed();

            byte[] destination = new byte[Algorithm.SecretKeySizeInBytes];
            ExportSlhDsaSecretKeyCore(destination);
            return destination;
        }

        /// <summary>
        ///   Generates a new SLH-DSA key for the specified algorithm.
        /// </summary>
        /// <param name="algorithm">
        ///   An algorithm identifying what kind of SLH-DSA key to generate.
        /// </param>
        /// <returns>
        ///   The generated object.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="algorithm" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred generating the SLH-DSA key.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support SLH-DSA. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports SLH-DSA.
        /// </exception>
        public static SlhDsa GenerateKey(SlhDsaAlgorithm algorithm)
        {
            ArgumentNullException.ThrowIfNull(algorithm);
            ThrowIfNotSupported();

            return SlhDsaImplementation.GenerateKeyCore(algorithm);
        }

        /// <summary>
        ///   Imports an SLH-DSA public key from an X.509 SubjectPublicKeyInfo structure.
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
        ///     The SubjectPublicKeyInfo value does not represent an SLH-DSA key.
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
        ///   The platform does not support SLH-DSA. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports SLH-DSA.
        /// </exception>
        public static SlhDsa ImportSubjectPublicKeyInfo(ReadOnlySpan<byte> source)
        {
            ThrowIfInvalidLength(source);
            ThrowIfNotSupported();

            KeyFormatHelper.ReadSubjectPublicKeyInfo(s_knownOids, source, SubjectPublicKeyReader, out int read, out SlhDsa slhDsa);
            Debug.Assert(read == source.Length);
            return slhDsa;

            static void SubjectPublicKeyReader(ReadOnlyMemory<byte> key, in AlgorithmIdentifierAsn identifier, out SlhDsa slhDsa)
            {
                SlhDsaAlgorithm algorithm = GetAlgorithmIdentifier(in identifier);

                if (key.Length != algorithm.PublicKeySizeInBytes)
                {
                    throw new CryptographicException(SR.Argument_PublicKeyWrongSizeForAlgorithm);
                }

                slhDsa = SlhDsaImplementation.ImportPublicKey(algorithm, key.Span);
            }
        }

        /// <inheritdoc cref="ImportSubjectPublicKeyInfo(ReadOnlySpan{byte})" />
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        public static SlhDsa ImportSubjectPublicKeyInfo(byte[] source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return ImportSubjectPublicKeyInfo(new ReadOnlySpan<byte>(source));
        }

        /// <summary>
        ///   Imports an SLH-DSA private key from a PKCS#8 PrivateKeyInfo structure.
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
        ///     The PrivateKeyInfo value does not represent an SLH-DSA key.
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
        ///   The platform does not support SLH-DSA. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports SLH-DSA.
        /// </exception>
        public static SlhDsa ImportPkcs8PrivateKey(ReadOnlySpan<byte> source)
        {
            ThrowIfInvalidLength(source);
            ThrowIfNotSupported();

            KeyFormatHelper.ReadPkcs8(
                s_knownOids,
                source,
                (ReadOnlyMemory<byte> key, in AlgorithmIdentifierAsn algId, out SlhDsa ret) =>
                {
                    SlhDsaAlgorithm info = GetAlgorithmIdentifier(in algId);
                    ReadOnlySpan<byte> privateKey = key.Span;

                    if (privateKey.Length != info.SecretKeySizeInBytes)
                    {
                        throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                    }

                    ret = ImportSlhDsaSecretKey(info, key.Span);
                },
                out int read,
                out SlhDsa slhDsa);

            Debug.Assert(read == source.Length);
            return slhDsa;
        }

        /// <inheritdoc cref="ImportPkcs8PrivateKey(ReadOnlySpan{byte})" />>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        public static SlhDsa ImportPkcs8PrivateKey(byte[] source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return ImportPkcs8PrivateKey(new ReadOnlySpan<byte>(source));
        }

        /// <summary>
        ///   Imports an SLH-DSA private key from a PKCS#8 EncryptedPrivateKeyInfo structure.
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
        ///     The value does not represent an SLH-DSA key.
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
        ///   The platform does not support SLH-DSA. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports SLH-DSA.
        /// </exception>
        public static SlhDsa ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, ReadOnlySpan<byte> source)
        {
            ThrowIfInvalidLength(source);
            ThrowIfNotSupported();

            return KeyFormatHelper.DecryptPkcs8(
                passwordBytes,
                source,
                ImportPkcs8PrivateKey,
                out _);
        }

        /// <summary>
        ///   Imports an SLH-DSA private key from a PKCS#8 EncryptedPrivateKeyInfo structure.
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
        ///     The value does not represent an SLH-DSA key.
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
        ///   The platform does not support SLH-DSA. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports SLH-DSA.
        /// </exception>
        public static SlhDsa ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, ReadOnlySpan<byte> source)
        {
            ThrowIfInvalidLength(source);
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
        public static SlhDsa ImportEncryptedPkcs8PrivateKey(string password, byte[] source)
        {
            ArgumentNullException.ThrowIfNull(password);
            ArgumentNullException.ThrowIfNull(source);

            return ImportEncryptedPkcs8PrivateKey(password.AsSpan(), new ReadOnlySpan<byte>(source));
        }

        /// <summary>
        ///   Imports an SLH-DSA key from an RFC 7468 PEM-encoded string.
        /// </summary>
        /// <param name="source">
        ///   The text of the PEM key to import.
        /// </param>
        /// <returns>
        ///   The imported SLH-DSA key.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <para><paramref name="source" /> contains an encrypted PEM-encoded key.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="source" /> contains multiple PEM-encoded SLH-DSA keys.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="source" /> contains no PEM-encoded SLH-DSA keys.</para>
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while importing the key.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support SLH-DSA. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports SLH-DSA.
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
        public static SlhDsa ImportFromPem(ReadOnlySpan<char> source)
        {
            ThrowIfNotSupported();

            return PemKeyHelpers.ImportFactoryPem<SlhDsa>(source, label =>
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
        public static SlhDsa ImportFromPem(string source)
        {
            ArgumentNullException.ThrowIfNull(source);
            ThrowIfNotSupported();

            return ImportFromPem(source.AsSpan());
        }

        /// <summary>
        ///   Imports an SLH-DSA key from an encrypted RFC 7468 PEM-encoded string.
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
        ///   The platform does not support SLH-DSA. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports SLH-DSA.
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
        public static SlhDsa ImportFromEncryptedPem(ReadOnlySpan<char> source, ReadOnlySpan<char> password)
        {
            ThrowIfNotSupported();

            return PemKeyHelpers.ImportEncryptedFactoryPem<SlhDsa, char>(
                source,
                password,
                ImportEncryptedPkcs8PrivateKey);
        }

        /// <summary>
        ///   Imports an SLH-DSA key from an encrypted RFC 7468 PEM-encoded string.
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
        ///   The platform does not support SLH-DSA. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports SLH-DSA.
        /// </exception>
        /// <remarks>
        ///   <para>
        ///     Unsupported or malformed PEM-encoded objects will be ignored. If multiple supported PEM labels
        ///     are found, an exception is thrown to prevent importing a key when
        ///     the key is ambiguous.
        ///   </para>
        ///   <para>This method supports the <c>ENCRYPTED PRIVATE KEY</c> PEM label.</para>
        /// </remarks>
        public static SlhDsa ImportFromEncryptedPem(ReadOnlySpan<char> source, ReadOnlySpan<byte> passwordBytes)
        {
            ThrowIfNotSupported();

            return PemKeyHelpers.ImportEncryptedFactoryPem<SlhDsa, byte>(
                source,
                passwordBytes,
                ImportEncryptedPkcs8PrivateKey);
        }

        /// <inheritdoc cref="ImportFromEncryptedPem(ReadOnlySpan{char}, ReadOnlySpan{char})" />
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> or <paramref name="password" /> is <see langword="null" />.
        /// </exception>
        public static SlhDsa ImportFromEncryptedPem(string source, string password)
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
        public static SlhDsa ImportFromEncryptedPem(string source, byte[] passwordBytes)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(passwordBytes);
            ThrowIfNotSupported();

            return ImportFromEncryptedPem(source.AsSpan(), new ReadOnlySpan<byte>(passwordBytes));
        }

        /// <summary>
        ///   Imports an SLH-DSA public key in the FIPS 205 public key format.
        /// </summary>
        /// <param name="algorithm">
        ///   The specific SLH-DSA algorithm for this key.
        /// </param>
        /// <param name="source">
        ///   The bytes of a FIPS 205 public key.
        /// </param>
        /// <returns>
        ///   The imported key.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="source"/> has a length that is not valid for the SLH-DSA algorithm.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="algorithm" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while importing the key.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support SLH-DSA. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports SLH-DSA.
        /// </exception>
        public static SlhDsa ImportSlhDsaPublicKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            ArgumentNullException.ThrowIfNull(algorithm);

            if (source.Length != algorithm.PublicKeySizeInBytes)
            {
                throw new ArgumentException(SR.Argument_PublicKeyWrongSizeForAlgorithm, nameof(source));
            }

            ThrowIfNotSupported();

            return SlhDsaImplementation.ImportPublicKey(algorithm, source);
        }

        /// <inheritdoc cref="ImportSlhDsaPublicKey(SlhDsaAlgorithm, ReadOnlySpan{byte})" />
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="algorithm"/> or <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        public static SlhDsa ImportSlhDsaPublicKey(SlhDsaAlgorithm algorithm, byte[] source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return ImportSlhDsaPublicKey(algorithm, new ReadOnlySpan<byte>(source));
        }

        /// <summary>
        ///   Imports an SLH-DSA private key in the FIPS 205 secret key format.
        /// </summary>
        /// <param name="algorithm">
        ///   The specific SLH-DSA algorithm for this key.
        /// </param>
        /// <param name="source">
        ///   The bytes of a FIPS 205 secret key.
        /// </param>
        /// <returns>
        ///   The imported key.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="source"/> has a length that is not valid for the SLH-DSA algorithm.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="algorithm" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while importing the key.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support SLH-DSA. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports SLH-DSA.
        /// </exception>
        public static SlhDsa ImportSlhDsaSecretKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            ArgumentNullException.ThrowIfNull(algorithm);

            if (source.Length != algorithm.SecretKeySizeInBytes)
            {
                throw new ArgumentException(SR.Argument_SecretKeyWrongSizeForAlgorithm, nameof(source));
            }

            ThrowIfNotSupported();

            return SlhDsaImplementation.ImportSecretKey(algorithm, source);
        }

        /// <inheritdoc cref="ImportSlhDsaSecretKey(SlhDsaAlgorithm, ReadOnlySpan{byte})" />
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="algorithm"/> or <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        public static SlhDsa ImportSlhDsaSecretKey(SlhDsaAlgorithm algorithm, byte[] source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return ImportSlhDsaSecretKey(algorithm, new ReadOnlySpan<byte>(source));
        }

        /// <summary>
        ///   Called by the <c>Dispose()</c> and <c>Finalize()</c> methods to release the managed and unmanaged
        ///   resources used by the current instance of the <see cref="SlhDsa"/> class.
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
        protected abstract bool VerifyPreHashCore(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> context, string hashAlgorithmOid, ReadOnlySpan<byte> signature);

        /// <summary>
        ///   When overridden in a derived class, exports the FIPS 205 public key to the specified buffer.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the public key.
        /// </param>
        protected abstract void ExportSlhDsaPublicKeyCore(Span<byte> destination);

        /// <summary>
        ///   When overridden in a derived class, exports the FIPS 205 secret key to the specified buffer.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the secret key.
        /// </param>
        protected abstract void ExportSlhDsaSecretKeyCore(Span<byte> destination);

        private AsnWriter ExportSubjectPublicKeyInfoCore()
        {
            // Public key size for SLH-DSA is at most 64 bytes so we can stack allocate it.
            int publicKeySizeInBytes = Algorithm.PublicKeySizeInBytes;
            Debug.Assert(publicKeySizeInBytes is <= 64);
            Span<byte> publicKey = (stackalloc byte[64])[..publicKeySizeInBytes];

            ExportSlhDsaPublicKeyCore(publicKey);

            // The ASN.1 overhead of a SubjectPublicKeyInfo encoding a public key is 18 bytes.
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

        private TResult ExportPkcs8PrivateKeyCallback<TResult>(ExportPkcs8PrivateKeyFunc<TResult> func)
        {
            // A PKCS#8 SLH-DSA-SHA2-256s private key has an ASN.1 overhead of 22 bytes, assuming no attributes.
            // Make it an even 32 and that should give a good starting point for a buffer size.
            int size = Algorithm.SecretKeySizeInBytes + 32;
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

        private static SlhDsaAlgorithm GetAlgorithmIdentifier(ref readonly AlgorithmIdentifierAsn identifier)
        {
            SlhDsaAlgorithm? algorithm = SlhDsaAlgorithm.GetAlgorithmFromOid(identifier.Algorithm);
            Debug.Assert(algorithm is not null, "Algorithm identifier should have been pre-validated by KeyFormatHelper.");

            if (identifier.Parameters.HasValue)
            {
                AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
                identifier.Encode(writer);
                throw Helpers.CreateAlgorithmUnknownException(writer);
            }

            return algorithm;
        }

        private static void ValidateHashAlgorithm(ReadOnlySpan<byte> hash, ReadOnlySpan<char> hashAlgorithmOid)
        {
            int? outputSize = hashAlgorithmOid switch
            {
                Oids.Md5 => 128 / 8,
                Oids.Sha1 => 160 / 8,
                Oids.Sha256 => 256 / 8,
                Oids.Sha384 => 384 / 8,
                Oids.Sha512 => 512 / 8,
                Oids.Sha3_256 => 256 / 8,
                Oids.Sha3_384 => 384 / 8,
                Oids.Sha3_512 => 512 / 8,
                Oids.Shake128 => 256 / 8,
                Oids.Shake256 => 512 / 8,
                _ => null,
            };

            if (outputSize is not null)
            {
                if (hash.Length != outputSize)
                {
                    throw new CryptographicException(SR.Cryptography_HashLengthMismatch);
                }
            }
            else
            {
                // The OIDs for the algorithms above have max length 11. We'll just round up for a conservative initial estimate.
                const int MaxEncodedOidLengthForCommonHashAlgorithms = 16;
                AsnWriter writer = new AsnWriter(AsnEncodingRules.DER, MaxEncodedOidLengthForCommonHashAlgorithms);

                try
                {
                    // Only the format of the OID is validated here. The derived classes can decide to do more if they want to.
                    writer.WriteObjectIdentifier(hashAlgorithmOid);
                }
                catch (ArgumentException ae)
                {
                    throw new CryptographicException(SR.Cryptography_HashLengthMismatch, ae);
                }
            }
        }

        private static void ThrowIfNotSupported()
        {
            if (!IsSupported)
            {
                throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_AlgorithmNotSupported, nameof(SlhDsa)));
            }
        }

        private static void ThrowIfInvalidLength(ReadOnlySpan<byte> data)
        {
            int bytesRead;
            try
            {
                AsnDecoder.ReadEncodedValue(data, AsnEncodingRules.BER, out _, out _, out bytesRead);
            }
            catch (AsnContentException ace)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, ace);
            }

            if (bytesRead != data.Length)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }
        }

        private static string EncodeAsnWriterToPem(string label, AsnWriter writer, bool clear = true)
        {
#if NET10_0_OR_GREATER
            return writer.Encode(label, static (label, span) => PemEncoding.WriteString(label, span));
#else
            int length = writer.GetEncodedLength();
            byte[] rent = CryptoPool.Rent(length);

            try
            {
                int written = writer.Encode(rent);
                Debug.Assert(written == length);
                return PemEncoding.WriteString(label, rent.AsSpan(0, written));
            }
            finally
            {
                CryptoPool.Return(rent, clear ? length : 0);
            }
#endif
        }

        private delegate TResult ExportPkcs8PrivateKeyFunc<TResult>(ReadOnlySpan<byte> pkcs8);
    }
}
