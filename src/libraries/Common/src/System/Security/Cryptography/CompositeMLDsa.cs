// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    /// <summary>
    ///   Represents a Composite ML-DSA key.
    /// </summary>
    /// <remarks>
    ///   Developers are encouraged to program against the <see cref="CompositeMLDsa"/> base class,
    ///   rather than any specific derived class. The derived classes are intended for interop with the underlying system
    ///   cryptographic libraries.
    /// </remarks>
    [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    public abstract class CompositeMLDsa : IDisposable
#if DESIGNTIMEINTERFACES
#pragma warning disable SA1001
        , IImportExportShape<CompositeMLDsa>
#pragma warning restore SA1001
#endif
    {
        private static readonly string[] s_knownOids =
        [
            Oids.MLDsa44WithRSA2048PssPreHashSha256,
            Oids.MLDsa44WithRSA2048Pkcs15PreHashSha256,
            Oids.MLDsa44WithEd25519PreHashSha512,
            Oids.MLDsa44WithECDsaP256PreHashSha256,
            Oids.MLDsa65WithRSA3072PssPreHashSha512,
            Oids.MLDsa65WithRSA3072Pkcs15PreHashSha512,
            Oids.MLDsa65WithRSA4096PssPreHashSha512,
            Oids.MLDsa65WithRSA4096Pkcs15PreHashSha512,
            Oids.MLDsa65WithECDsaP256PreHashSha512,
            Oids.MLDsa65WithECDsaP384PreHashSha512,
            Oids.MLDsa65WithECDsaBrainpoolP256r1PreHashSha512,
            Oids.MLDsa65WithEd25519PreHashSha512,
            Oids.MLDsa87WithECDsaP384PreHashSha512,
            Oids.MLDsa87WithECDsaBrainpoolP384r1PreHashSha512,
            Oids.MLDsa87WithEd448PreHashShake256_512,
            Oids.MLDsa87WithRSA3072PssPreHashSha512,
            Oids.MLDsa87WithRSA4096PssPreHashSha512,
            Oids.MLDsa87WithECDsaP521PreHashSha512,
        ];

        private const int MaxContextLength = 255;

        private bool _disposed;

        /// <summary>
        ///   Gets a value indicating whether the current platform supports Composite ML-DSA.
        /// </summary>
        /// <value>
        ///   <see langword="true" /> if the current platform supports Composite ML-DSA; otherwise, <see langword="false" />.
        /// </value>
        public static bool IsSupported { get; } = CompositeMLDsaImplementation.SupportsAny();

        /// <summary>
        ///   Gets the specific Composite ML-DSA algorithm for this key.
        /// </summary>
        public CompositeMLDsaAlgorithm Algorithm { get; }

        /// <summary>
        ///   Initializes a new instance of the <see cref="CompositeMLDsa" /> class.
        /// </summary>
        /// <param name="algorithm">
        ///   The specific Composite ML-DSA algorithm for this key.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="algorithm"/> parameter is <see langword="null"/>.
        /// </exception>
        protected CompositeMLDsa(CompositeMLDsaAlgorithm algorithm)
        {
            ArgumentNullException.ThrowIfNull(algorithm);

            Algorithm = algorithm;
        }

        /// <summary>
        ///   Determines whether the specified algorithm is supported by the current platform.
        /// </summary>
        /// <param name="algorithm">
        ///   The <see cref="CompositeMLDsaAlgorithm"/> to check for support.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the algorithm is supported; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="algorithm"/> is <see langword="null"/>.
        /// </exception>
        public static bool IsAlgorithmSupported(CompositeMLDsaAlgorithm algorithm)
        {
            ArgumentNullException.ThrowIfNull(algorithm);

            return CompositeMLDsaImplementation.IsAlgorithmSupportedImpl(algorithm);
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
        ///   The Composite ML-DSA signature of the specified data.
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

            if (context?.Length > MaxContextLength)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(context),
                    context.Length,
                    SR.Argument_SignatureContextTooLong255);
            }

            ThrowIfDisposed();

            if (Algorithm.MinSignatureSizeInBytes == Algorithm.MaxSignatureSizeInBytes)
            {
                byte[] signature = new byte[Algorithm.MaxSignatureSizeInBytes];
                int bytesWritten = SignDataCore(new ReadOnlySpan<byte>(data), new ReadOnlySpan<byte>(context), signature);

                if (signature.Length != bytesWritten)
                {
                    throw new CryptographicException();
                }

                return signature;
            }

            using (CryptoPoolLease lease = CryptoPoolLease.Rent(Algorithm.MaxSignatureSizeInBytes, skipClear: true))
            {
                int bytesWritten = SignDataCore(
                    new ReadOnlySpan<byte>(data),
                    new ReadOnlySpan<byte>(context),
                    lease.Span);

                if (!Algorithm.IsValidSignatureSize(bytesWritten))
                {
                    throw new CryptographicException();
                }

                return lease.Span.Slice(0, bytesWritten).ToArray();
            }
        }

        /// <summary>
        ///   Signs the specified data, writing the signature into the provided buffer.
        /// </summary>
        /// <param name="data">
        ///   The data to sign.
        /// </param>
        /// <param name="destination">
        ///   The buffer to receive the signature. Its length must be at least <see cref="CompositeMLDsaAlgorithm.MaxSignatureSizeInBytes"/>.
        /// </param>
        /// <param name="context">
        ///   An optional context-specific value to limit the scope of the signature.
        ///   The default value is an empty buffer.
        /// </param>
        /// <returns>
        ///   The number of bytes written to the <paramref name="destination"/> buffer.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="destination"/> is less than <see cref="CompositeMLDsaAlgorithm.MaxSignatureSizeInBytes"/> in length.
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
        public int SignData(ReadOnlySpan<byte> data, Span<byte> destination, ReadOnlySpan<byte> context = default)
        {
            if (context.Length > MaxContextLength)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(context),
                    context.Length,
                    SR.Argument_SignatureContextTooLong255);
            }

            if (destination.Length < Algorithm.MaxSignatureSizeInBytes)
            {
                throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));
            }

            ThrowIfDisposed();

            int bytesWritten = SignDataCore(data, context, destination.Slice(0, Algorithm.MaxSignatureSizeInBytes));

            if (!Algorithm.IsValidSignatureSize(bytesWritten))
            {
                CryptographicOperations.ZeroMemory(destination);

                throw new CryptographicException();
            }

            return bytesWritten;
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
        ///   The buffer to receive the signature, whose length will be exactly <see cref="CompositeMLDsaAlgorithm.MaxSignatureSizeInBytes" />.
        /// </param>
        /// <returns>
        ///   The number of bytes written to the <paramref name="destination"/> buffer.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   An error occurred while signing the data.
        /// </exception>
        protected abstract int SignDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination);

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

            if (!Algorithm.IsValidSignatureSize(signature.Length))
            {
                return false;
            }

            return VerifyDataCore(data, context, signature);
        }

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
        ///   Generates a new Composite ML-DSA key.
        /// </summary>
        /// <param name="algorithm">
        ///   An algorithm identifying what kind of Composite ML-DSA key to generate.
        /// </param>
        /// <returns>
        ///   The generated key.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="algorithm" /> is <see langword="null" />
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred generating the Composite ML-DSA key.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support the specified Composite ML-DSA algorithm. Callers can use <see cref="IsAlgorithmSupported" />
        ///   to determine if the algorithm is supported.
        /// </exception>
        public static CompositeMLDsa GenerateKey(CompositeMLDsaAlgorithm algorithm)
        {
            ArgumentNullException.ThrowIfNull(algorithm);
            ThrowIfNotSupported(algorithm);

            return CompositeMLDsaImplementation.GenerateKeyImpl(algorithm);
        }

        /// <inheritdoc cref="ImportFromEncryptedPem(ReadOnlySpan{char}, ReadOnlySpan{char})" />
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> or <paramref name="password" /> is <see langword="null" />.
        /// </exception>
        public static CompositeMLDsa ImportFromEncryptedPem(string source, string password)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(password);
            ThrowIfNotSupported();

            return ImportFromEncryptedPem(source.AsSpan(), password.AsSpan());
        }

        /// <summary>
        ///   Imports a Composite ML-DSA key from an encrypted RFC 7468 PEM-encoded string.
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
        ///   <para>-or-</para>
        ///   <para>
        ///     The specified Composite ML-DSA algorithm is not supported.
        ///   </para>
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support Composite ML-DSA. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports Composite ML-DSA.
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
        public static CompositeMLDsa ImportFromEncryptedPem(ReadOnlySpan<char> source, ReadOnlySpan<char> password)
        {
            ThrowIfNotSupported();

            return PemKeyHelpers.ImportEncryptedFactoryPem<CompositeMLDsa, char>(
                source,
                password,
                ImportEncryptedPkcs8PrivateKey);
        }

        /// <inheritdoc cref="ImportFromEncryptedPem(ReadOnlySpan{char}, ReadOnlySpan{byte})" />
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> or <paramref name="passwordBytes" /> is <see langword="null" />.
        /// </exception>
        public static CompositeMLDsa ImportFromEncryptedPem(string source, byte[] passwordBytes)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(passwordBytes);
            ThrowIfNotSupported();

            return ImportFromEncryptedPem(source.AsSpan(), new ReadOnlySpan<byte>(passwordBytes));
        }

        /// <summary>
        ///   Imports a Composite ML-DSA key from an encrypted RFC 7468 PEM-encoded string.
        /// </summary>
        /// <param name="source">
        ///   The PEM text of the encrypted key to import.
        /// </param>
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
        ///   <para>-or-</para>
        ///   <para>
        ///     The specified Composite ML-DSA algorithm is not supported.
        ///   </para>
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support Composite ML-DSA. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports Composite ML-DSA.
        /// </exception>
        /// <remarks>
        ///   <para>
        ///     Unsupported or malformed PEM-encoded objects will be ignored. If multiple supported PEM labels
        ///     are found, an exception is thrown to prevent importing a key when
        ///     the key is ambiguous.
        ///   </para>
        ///   <para>This method supports the <c>ENCRYPTED PRIVATE KEY</c> PEM label.</para>
        /// </remarks>
        public static CompositeMLDsa ImportFromEncryptedPem(ReadOnlySpan<char> source, ReadOnlySpan<byte> passwordBytes)
        {
            ThrowIfNotSupported();

            return PemKeyHelpers.ImportEncryptedFactoryPem<CompositeMLDsa, byte>(
                source,
                passwordBytes,
                ImportEncryptedPkcs8PrivateKey);
        }

        /// <inheritdoc cref="ImportFromPem(ReadOnlySpan{char})" />
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        public static CompositeMLDsa ImportFromPem(string source)
        {
            ArgumentNullException.ThrowIfNull(source);
            ThrowIfNotSupported();

            return ImportFromPem(source.AsSpan());
        }

        /// <summary>
        ///   Imports a Composite ML-DSA key from an RFC 7468 PEM-encoded string.
        /// </summary>
        /// <param name="source">
        ///   The text of the PEM key to import.
        /// </param>
        /// <returns>
        ///   The imported Composite ML-DSA key.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <para><paramref name="source" /> contains an encrypted PEM-encoded key.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="source" /> contains multiple PEM-encoded Composite ML-DSA keys.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="source" /> contains no PEM-encoded Composite ML-DSA keys.</para>
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>An error occurred while importing the key.</para>
        ///   <para>-or-</para>
        ///   <para>The specified Composite ML-DSA algorithm is not supported.</para>
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support Composite ML-DSA. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports Composite ML-DSA.
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
        public static CompositeMLDsa ImportFromPem(ReadOnlySpan<char> source)
        {
            ThrowIfNotSupported();

            return PemKeyHelpers.ImportFactoryPem<CompositeMLDsa>(source, label =>
                label switch
                {
                    PemLabels.Pkcs8PrivateKey => ImportPkcs8PrivateKey,
                    PemLabels.SpkiPublicKey => ImportSubjectPublicKeyInfo,
                    _ => null,
                });
        }

        /// <inheritdoc cref="ImportSubjectPublicKeyInfo(ReadOnlySpan{byte})" />
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        public static CompositeMLDsa ImportSubjectPublicKeyInfo(byte[] source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return ImportSubjectPublicKeyInfo(new ReadOnlySpan<byte>(source));
        }

        /// <summary>
        ///   Imports a Composite ML-DSA public key from an X.509 SubjectPublicKeyInfo structure.
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
        ///     The SubjectPublicKeyInfo value does not represent a Composite ML-DSA key.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="source" /> contains trailing data after the ASN.1 structure.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm-specific import failed.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The specified Composite ML-DSA algorithm is not supported.
        ///   </para>
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support Composite ML-DSA. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports Composite ML-DSA.
        /// </exception>
        public static CompositeMLDsa ImportSubjectPublicKeyInfo(ReadOnlySpan<byte> source)
        {
            Helpers.ThrowIfAsnInvalidLength(source);
            ThrowIfNotSupported();

            KeyFormatHelper.ReadSubjectPublicKeyInfo(s_knownOids, source, SubjectPublicKeyReader, out int read, out CompositeMLDsa dsa);
            Debug.Assert(read == source.Length);
            return dsa;

            static void SubjectPublicKeyReader(ReadOnlyMemory<byte> key, in AlgorithmIdentifierAsn identifier, out CompositeMLDsa dsa)
            {
                CompositeMLDsaAlgorithm algorithm = GetAlgorithmIdentifier(in identifier);

                if (!algorithm.IsValidPublicKeySize(key.Length))
                {
                    throw new CryptographicException(SR.Argument_PublicKeyWrongSizeForAlgorithm);
                }

                dsa = CompositeMLDsaImplementation.ImportCompositeMLDsaPublicKeyImpl(algorithm, key.Span);
            }
        }

        /// <inheritdoc cref="ImportEncryptedPkcs8PrivateKey(ReadOnlySpan{char}, ReadOnlySpan{byte})" />
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="password" /> or <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        public static CompositeMLDsa ImportEncryptedPkcs8PrivateKey(string password, byte[] source)
        {
            ArgumentNullException.ThrowIfNull(password);
            ArgumentNullException.ThrowIfNull(source);

            return ImportEncryptedPkcs8PrivateKey(password.AsSpan(), new ReadOnlySpan<byte>(source));
        }

        /// <summary>
        ///   Imports a Composite ML-DSA private key from a PKCS#8 EncryptedPrivateKeyInfo structure.
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
        ///     The value does not represent a Composite ML-DSA key.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="source" /> contains trailing data after the ASN.1 structure.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm-specific import failed.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The specified Composite ML-DSA algorithm is not supported.
        ///   </para>
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support Composite ML-DSA. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports Composite ML-DSA.
        /// </exception>
        public static CompositeMLDsa ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, ReadOnlySpan<byte> source)
        {
            Helpers.ThrowIfAsnInvalidLength(source);
            ThrowIfNotSupported();

            return KeyFormatHelper.DecryptPkcs8(
                password,
                source,
                ImportPkcs8PrivateKey,
                out _);
        }

        /// <summary>
        ///   Imports a Composite ML-DSA private key from a PKCS#8 EncryptedPrivateKeyInfo structure.
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
        ///     The value does not represent a Composite ML-DSA key.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="source" /> contains trailing data after the ASN.1 structure.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm-specific import failed.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The specified Composite ML-DSA algorithm is not supported.
        ///   </para>
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support Composite ML-DSA. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports Composite ML-DSA.
        /// </exception>
        public static CompositeMLDsa ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, ReadOnlySpan<byte> source)
        {
            Helpers.ThrowIfAsnInvalidLength(source);
            ThrowIfNotSupported();

            return KeyFormatHelper.DecryptPkcs8(
                passwordBytes,
                source,
                ImportPkcs8PrivateKey,
                out _);
        }

        /// <inheritdoc cref="ImportPkcs8PrivateKey(ReadOnlySpan{byte})" />>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        public static CompositeMLDsa ImportPkcs8PrivateKey(byte[] source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return ImportPkcs8PrivateKey(new ReadOnlySpan<byte>(source));
        }

        /// <summary>
        ///   Imports a Composite ML-DSA private key from a PKCS#8 PrivateKeyInfo structure.
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
        ///     The PrivateKeyInfo value does not represent a Composite ML-DSA key.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="source" /> contains trailing data after the ASN.1 structure.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm-specific import failed.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The specified Composite ML-DSA algorithm is not supported.
        ///   </para>
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support Composite ML-DSA. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports Composite ML-DSA.
        /// </exception>
        public static CompositeMLDsa ImportPkcs8PrivateKey(ReadOnlySpan<byte> source)
        {
            Helpers.ThrowIfAsnInvalidLength(source);
            ThrowIfNotSupported();

            KeyFormatHelper.ReadPkcs8(s_knownOids, source, PrivateKeyReader, out int read, out CompositeMLDsa dsa);
            Debug.Assert(read == source.Length);
            return dsa;

            static void PrivateKeyReader(
                ReadOnlyMemory<byte> privateKeyContents,
                in AlgorithmIdentifierAsn algorithmIdentifier,
                out CompositeMLDsa dsa)
            {
                CompositeMLDsaAlgorithm algorithm = GetAlgorithmIdentifier(in algorithmIdentifier);
                AsnValueReader reader = new AsnValueReader(privateKeyContents.Span, AsnEncodingRules.BER);

                if (!reader.TryReadPrimitiveOctetString(out ReadOnlySpan<byte> key) || reader.HasData)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                if (!algorithm.IsValidPrivateKeySize(key.Length))
                {
                    throw new CryptographicException(SR.Argument_PrivateKeyWrongSizeForAlgorithm);
                }

                dsa = CompositeMLDsaImplementation.ImportCompositeMLDsaPrivateKeyImpl(algorithm, key);
            }
        }

        /// <inheritdoc cref="ImportCompositeMLDsaPublicKey(CompositeMLDsaAlgorithm, ReadOnlySpan{byte})" />
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="algorithm"/> or <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        public static CompositeMLDsa ImportCompositeMLDsaPublicKey(CompositeMLDsaAlgorithm algorithm, byte[] source)
        {
            ArgumentNullException.ThrowIfNull(algorithm);
            ArgumentNullException.ThrowIfNull(source);

            return ImportCompositeMLDsaPublicKey(algorithm, new ReadOnlySpan<byte>(source));
        }

        /// <summary>
        ///   Imports a Composite ML-DSA public key.
        /// </summary>
        /// <param name="algorithm">
        ///   The specific Composite ML-DSA algorithm for this key.
        /// </param>
        /// <param name="source">
        ///   The bytes of the public key.
        /// </param>
        /// <returns>
        ///   The imported key.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     <paramref name="source"/> length is the wrong size for the specified algorithm.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     An error occurred while importing the key.
        ///   </para>
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support the specified Composite ML-DSA algorithm. Callers can use <see cref="IsAlgorithmSupported" />
        ///   to determine if the algorithm is supported.
        /// </exception>
        public static CompositeMLDsa ImportCompositeMLDsaPublicKey(CompositeMLDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            ArgumentNullException.ThrowIfNull(algorithm);
            ThrowIfNotSupported(algorithm);

            if (!algorithm.IsValidPublicKeySize(source.Length))
            {
                throw new CryptographicException(SR.Argument_PublicKeyWrongSizeForAlgorithm);
            }

            return CompositeMLDsaImplementation.ImportCompositeMLDsaPublicKeyImpl(algorithm, source);
        }

        /// <inheritdoc cref="ImportCompositeMLDsaPrivateKey(CompositeMLDsaAlgorithm, ReadOnlySpan{byte})" />
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="algorithm"/> or <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        public static CompositeMLDsa ImportCompositeMLDsaPrivateKey(CompositeMLDsaAlgorithm algorithm, byte[] source)
        {
            ArgumentNullException.ThrowIfNull(algorithm);
            ArgumentNullException.ThrowIfNull(source);

            return ImportCompositeMLDsaPrivateKey(algorithm, new ReadOnlySpan<byte>(source));
        }

        /// <summary>
        ///   Imports a Composite ML-DSA private key.
        /// </summary>
        /// <param name="algorithm">
        ///   The specific Composite ML-DSA algorithm for this key.
        /// </param>
        /// <param name="source">
        ///   The bytes of the public key.
        /// </param>
        /// <returns>
        ///   The imported key.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     <paramref name="source"/> length is the wrong size for the specified algorithm.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     An error occurred while importing the key.
        ///   </para>
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support the specified Composite ML-DSA algorithm. Callers can use <see cref="IsAlgorithmSupported" />
        ///   to determine if the algorithm is supported.
        /// </exception>
        public static CompositeMLDsa ImportCompositeMLDsaPrivateKey(CompositeMLDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            ArgumentNullException.ThrowIfNull(algorithm);
            ThrowIfNotSupported(algorithm);

            if (!algorithm.IsValidPrivateKeySize(source.Length))
            {
                throw new CryptographicException(SR.Argument_PrivateKeyWrongSizeForAlgorithm);
            }

            return CompositeMLDsaImplementation.ImportCompositeMLDsaPrivateKeyImpl(algorithm, source);
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

            AsnWriter writer = WriteEncryptedPkcs8PrivateKeyToAsnWriter(password, pbeParameters);

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
        public string ExportEncryptedPkcs8PrivateKeyPem(
            ReadOnlySpan<byte> passwordBytes,
            PbeParameters pbeParameters)
        {
            ArgumentNullException.ThrowIfNull(pbeParameters);
            PasswordBasedEncryption.ValidatePbeParameters(pbeParameters, ReadOnlySpan<char>.Empty, passwordBytes);
            ThrowIfDisposed();

            AsnWriter writer = WriteEncryptedPkcs8PrivateKeyToAsnWriter(passwordBytes, pbeParameters);

            // Skip clear since the data is already encrypted.
            return Helpers.EncodeAsnWriterToPem(PemLabels.EncryptedPkcs8PrivateKey, writer, clear: false);
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

            AsnWriter writer = WriteEncryptedPkcs8PrivateKeyToAsnWriter(passwordBytes, pbeParameters);

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

            AsnWriter writer = WriteEncryptedPkcs8PrivateKeyToAsnWriter(password, pbeParameters);

            try
            {
                return writer.Encode();
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

            AsnWriter writer = WriteEncryptedPkcs8PrivateKeyToAsnWriter(password, pbeParameters);

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

            AsnWriter writer = WriteEncryptedPkcs8PrivateKeyToAsnWriter(passwordBytes, pbeParameters);

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
        public string ExportPkcs8PrivateKeyPem()
        {
            ThrowIfDisposed();

            return ExportPkcs8PrivateKeyCallback(static pkcs8 => PemEncoding.WriteString(PemLabels.Pkcs8PrivateKey, pkcs8));
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

            // The bound can be tightened but private key length of some traditional algorithms
            // can vary and aren't worth the complex calculation.
            int minimumPossiblePkcs8Key = Algorithm.MinPrivateKeySizeInBytes;

            if (destination.Length < minimumPossiblePkcs8Key)
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
        protected abstract bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten);

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

            AsnWriter writer = WriteSubjectPublicKeyToAsnWriter();

            // SPKI does not contain sensitive data.
            return Helpers.EncodeAsnWriterToPem(PemLabels.SpkiPublicKey, writer, clear: false);
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

            return WriteSubjectPublicKeyToAsnWriter().Encode();
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

            AsnWriter writer = WriteSubjectPublicKeyToAsnWriter();
            return writer.TryEncode(destination, out bytesWritten);
        }

        /// <summary>
        ///   Exports the public-key portion of the current key.
        /// </summary>
        /// <returns>
        ///   The Composite ML-DSA public key.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        public byte[] ExportCompositeMLDsaPublicKey()
        {
            ThrowIfDisposed();

            byte[] publicKey = new byte[Algorithm.MaxPublicKeySizeInBytes];

            if (!TryExportCompositeMLDsaPublicKey(publicKey, out int bytesWritten))
            {
                Debug.Fail("Max sized buffer was not large enough.");
                throw new CryptographicException();
            }

            if (bytesWritten < publicKey.Length)
            {
                Array.Resize(ref publicKey, bytesWritten);
            }

            return publicKey;
        }

        /// <summary>
        ///   Exports the public-key portion of the current key into the provided buffer.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the Composite ML-DSA public key value.
        /// </param>
        /// <returns>
        ///   The number of bytes written to the <paramref name="destination"/> buffer.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para><paramref name="destination"/> was too not large enough to hold the result.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        public int ExportCompositeMLDsaPublicKey(Span<byte> destination)
        {
            ThrowIfDisposed();

            if (destination.Length < Algorithm.MinPublicKeySizeInBytes)
            {
                throw new CryptographicException(SR.Argument_DestinationTooShort);
            }

            if (!TryExportCompositeMLDsaPublicKey(destination, out int bytesWritten))
            {
                throw new CryptographicException(SR.Argument_DestinationTooShort);
            }

            return bytesWritten;
        }

        /// <summary>
        ///   Attempts to export public key portion of the current key into the provided buffer.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the public key value.
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
        public bool TryExportCompositeMLDsaPublicKey(Span<byte> destination, out int bytesWritten)
        {
            ThrowIfDisposed();

            if (destination.Length < Algorithm.MinPublicKeySizeInBytes)
            {
                bytesWritten = 0;
                return false;
            }

            using (CryptoPoolLease lease = CryptoPoolLease.RentConditionally(Algorithm.MaxPublicKeySizeInBytes, destination, skipClear: true))
            {
                int localBytesWritten = ExportCompositeMLDsaPublicKeyCore(lease.Span);

                if (!Algorithm.IsValidPublicKeySize(localBytesWritten))
                {
                    bytesWritten = 0;
                    throw new CryptographicException();
                }

                if (lease.IsRented)
                {
                    if (localBytesWritten > destination.Length)
                    {
                        bytesWritten = 0;
                        return false;
                    }

                    lease.Span.Slice(0, localBytesWritten).CopyTo(destination);
                }

                bytesWritten = localBytesWritten;
                return true;
            }
        }

        /// <summary>
        ///   Exports the private-key portion of the current key.
        /// </summary>
        /// <returns>
        ///   The Composite ML-DSA private key.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   <para>The current instance cannot export a private key.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        public byte[] ExportCompositeMLDsaPrivateKey()
        {
            ThrowIfDisposed();

            byte[] privateKey = new byte[Algorithm.MaxPrivateKeySizeInBytes];

            if (!TryExportCompositeMLDsaPrivateKey(privateKey, out int bytesWritten))
            {
                Debug.Fail("Max sized buffer was not large enough.");
                throw new CryptographicException();
            }

            if (bytesWritten < privateKey.Length)
            {
                byte[] temp = new byte[bytesWritten];
                Array.Copy(privateKey, temp, bytesWritten);
                CryptographicOperations.ZeroMemory(privateKey);
                privateKey = temp;
            }

            return privateKey;
        }

        /// <summary>
        ///   Exports the private-key portion of the current key into the provided buffer.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the Composite ML-DSA private key value.
        /// </param>
        /// <returns>
        ///   The number of bytes written to the <paramref name="destination"/> buffer.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para><paramref name="destination"/> was too not large enough to hold the result.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        public int ExportCompositeMLDsaPrivateKey(Span<byte> destination)
        {
            ThrowIfDisposed();

            if (destination.Length < Algorithm.MinPrivateKeySizeInBytes)
            {
                throw new CryptographicException(SR.Argument_DestinationTooShort);
            }

            if (!TryExportCompositeMLDsaPrivateKey(destination, out int bytesWritten))
            {
                throw new CryptographicException(SR.Argument_DestinationTooShort);
            }

            return bytesWritten;
        }

        /// <summary>
        ///   Attempts to export private key portion of the current key into the provided buffer.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the private key value.
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
        public bool TryExportCompositeMLDsaPrivateKey(Span<byte> destination, out int bytesWritten)
        {
            ThrowIfDisposed();

            if (destination.Length < Algorithm.MinPrivateKeySizeInBytes)
            {
                bytesWritten = 0;
                return false;
            }

            using (CryptoPoolLease lease = CryptoPoolLease.RentConditionally(Algorithm.MaxPrivateKeySizeInBytes, destination, skipClearIfNotRented: true))
            {
                int localBytesWritten = ExportCompositeMLDsaPrivateKeyCore(lease.Span);

                if (!Algorithm.IsValidPrivateKeySize(localBytesWritten))
                {
                    if (!lease.IsRented)
                    {
                        CryptographicOperations.ZeroMemory(destination);
                    }

                    bytesWritten = 0;
                    throw new CryptographicException();
                }

                if (lease.IsRented)
                {
                    if (localBytesWritten > destination.Length)
                    {
                        bytesWritten = 0;
                        return false;
                    }

                    lease.Span.Slice(0, localBytesWritten).CopyTo(destination);
                }

                bytesWritten = localBytesWritten;
                return true;
            }
        }

        /// <summary>
        ///   When overridden in a derived class, exports the public key portion of the current key.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the public key value.
        /// </param>
        /// <returns>
        ///   The number of bytes written to the <paramref name="destination"/> buffer.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while exporting the key.
        /// </exception>
        protected abstract int ExportCompositeMLDsaPublicKeyCore(Span<byte> destination);

        /// <summary>
        ///   When overridden in a derived class, exports the private key portion of the current key.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the private key value.
        /// </param>
        /// <returns>
        ///   The number of bytes written to the <paramref name="destination"/> buffer.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while exporting the key.
        /// </exception>
        protected abstract int ExportCompositeMLDsaPrivateKeyCore(Span<byte> destination);

        /// <summary>
        ///   Releases all resources used by the <see cref="CompositeMLDsa"/> class.
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
        ///   Called by the <see cref="Dispose()" /> method to release the managed and unmanaged
        ///   resources used by the current instance of the <see cref="CompositeMLDsa"/> class.
        /// </summary>
        /// <param name="disposing">
        ///   <see langword="true" /> to release managed and unmanaged resources;
        ///   <see langword="false" /> to release only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
        }

        private AsnWriter WriteEncryptedPkcs8PrivateKeyToAsnWriter(ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters)
        {
            AsnWriter? tmp = null;

            try
            {
                tmp = WritePkcs8ToAsnWriter();
                return KeyFormatHelper.WriteEncryptedPkcs8(passwordBytes, tmp, pbeParameters);
            }
            finally
            {
                tmp?.Reset();
            }
        }

        private AsnWriter WriteEncryptedPkcs8PrivateKeyToAsnWriter(ReadOnlySpan<char> password, PbeParameters pbeParameters)
        {
            AsnWriter? tmp = null;

            try
            {
                tmp = WritePkcs8ToAsnWriter();
                return KeyFormatHelper.WriteEncryptedPkcs8(password, tmp, pbeParameters);
            }
            finally
            {
                tmp?.Reset();
            }
        }

        private AsnWriter WritePkcs8ToAsnWriter()
        {
            return ExportPkcs8PrivateKeyCallback(static pkcs8 =>
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
        }

        private AsnWriter WriteSubjectPublicKeyToAsnWriter()
        {
            byte[] buffer = new byte[Algorithm.MaxPublicKeySizeInBytes];
            int written = ExportCompositeMLDsaPublicKeyCore(buffer);

            if (!Algorithm.IsValidPublicKeySize(written))
            {
                throw new CryptographicException();
            }

            ReadOnlySpan<byte> publicKey = buffer.AsSpan(0, written);

            // TODO verify overhead

            // TODO: The ASN.1 overhead of a SubjectPublicKeyInfo encoding a public key is ___ bytes.
            // Round it off to 32. This checked operation should never throw because the inputs are not
            // user provided.
            int capacity = checked(32 + publicKey.Length);
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

        private delegate TResult ProcessExportedContent<TResult>(ReadOnlySpan<byte> exportedContent);

        private TResult ExportPkcs8PrivateKeyCallback<TResult>(ProcessExportedContent<TResult> func)
        {
            int size = Algorithm.MaxPrivateKeySizeInBytes;
            byte[] buffer = CryptoPool.Rent(size);
            int written;

            while (!TryExportPkcs8PrivateKeyCore(buffer, out written))
            {
                size = buffer.Length;
                CryptoPool.Return(buffer);
                size = checked(size * 2);
                buffer = CryptoPool.Rent(size);
            }

            if ((uint)written > (uint)buffer.Length)
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

        private static CompositeMLDsaAlgorithm GetAlgorithmIdentifier(ref readonly AlgorithmIdentifierAsn identifier)
        {
            CompositeMLDsaAlgorithm? algorithm = CompositeMLDsaAlgorithm.GetAlgorithmFromOid(identifier.Algorithm);
            Debug.Assert(algorithm is not null, "Algorithm identifier should have been pre-validated by KeyFormatHelper.");

            if (identifier.Parameters.HasValue)
            {
                AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
                identifier.Encode(writer);
                throw Helpers.CreateAlgorithmUnknownException(writer);
            }

            return algorithm;
        }

        private static void ThrowIfNotSupported()
        {
            if (!IsSupported)
            {
                throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_AlgorithmNotSupported, nameof(CompositeMLDsa)));
            }
        }

        private static void ThrowIfNotSupported(CompositeMLDsaAlgorithm algorithm)
        {
            if (!IsSupported || !IsAlgorithmSupported(algorithm))
            {
                throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_AlgorithmNotSupported, nameof(CompositeMLDsa)));
            }
        }

        private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, typeof(CompositeMLDsa));
    }
}
