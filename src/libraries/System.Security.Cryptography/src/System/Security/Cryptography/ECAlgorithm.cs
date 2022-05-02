// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Cryptography;
using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography
{
    /// <summary>
    /// Represents the abstract class from which elliptic-curve asymmetric
    /// algorithms can inherit from.
    /// </summary>
    public abstract class ECAlgorithm : AsymmetricAlgorithm
    {
        private static readonly string[] s_validOids =
        {
            Oids.EcPublicKey,
            // ECDH and ECMQV are not valid in this context.
        };

        /// <summary>
        /// When overridden in a derived class, exports the named or explicit <see cref="ECParameters" /> for an ECCurve.
        /// If the curve has a name, the Curve property will contain named curve parameters otherwise it will contain explicit parameters.
        /// </summary>
        /// <param name="includePrivateParameters">
        ///   <see langword="true" /> to include private parameters, otherwise, <see langword="false" />.
        /// </param>
        /// <exception cref="NotSupportedException">
        /// A derived class has not provided an implementation.
        /// </exception>
        /// <returns>The exported parameters.</returns>
        public virtual ECParameters ExportParameters(bool includePrivateParameters)
        {
            throw new NotSupportedException(SR.NotSupported_SubclassOverride);
        }

        /// <summary>
        /// When overridden in a derived class, exports the explicit <see cref="ECParameters" /> for an ECCurve.
        /// </summary>
        /// <param name="includePrivateParameters">
        ///   <see langword="true" /> to include private parameters, otherwise, <see langword="false" />.
        /// </param>
        /// <exception cref="NotSupportedException">
        /// A derived class has not provided an implementation.
        /// </exception>
        /// <returns>The exported explicit parameters.</returns>
        public virtual ECParameters ExportExplicitParameters(bool includePrivateParameters)
        {
            throw new NotSupportedException(SR.NotSupported_SubclassOverride);
        }

        /// <summary>
        /// When overridden in a derived class, imports the specified <see cref="ECParameters" />.
        /// </summary>
        /// <param name="parameters">The curve parameters.</param>
        /// <exception cref="NotSupportedException">
        /// A derived class has not provided an implementation.
        /// </exception>
        public virtual void ImportParameters(ECParameters parameters)
        {
            throw new NotSupportedException(SR.NotSupported_SubclassOverride);
        }

        /// <summary>
        /// When overridden in a derived class, generates a new public/private keypair for the specified curve.
        /// </summary>
        /// <param name="curve">The curve to use.</param>
        /// <exception cref="NotSupportedException">
        /// A derived class has not provided an implementation.
        /// </exception>
        public virtual void GenerateKey(ECCurve curve)
        {
            throw new NotSupportedException(SR.NotSupported_SubclassOverride);
        }

        /// <summary>
        /// Attempts to export the current key in the PKCS#8 EncryptedPrivateKeyInfo
        /// format into a provided buffer, using a byte-based password.
        /// </summary>
        /// <param name="passwordBytes">
        /// The bytes to use as a password when encrypting the key material.
        /// </param>
        /// <param name="pbeParameters">
        /// The password-based encryption (PBE) parameters to use when encrypting
        /// the key material.
        /// </param>
        /// <param name="destination">
        /// The byte span to receive the PKCS#8 EncryptedPrivateKeyInfo data.
        /// </param>
        /// <param name="bytesWritten">
        /// When this method returns, contains a value that indicates the number
        /// of bytes written to <paramref name="destination" />. This parameter
        /// is treated as uninitialized.
        /// </param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="destination" /> is big enough
        /// to receive the output; otherwise, <see langword="false" />.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="pbeParameters" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// A derived class has not provided an implementation for <see cref="ExportParameters" />.
        /// </exception>
        /// <exception cref="CryptographicException">
        /// <p>
        ///   The key could not be exported.
        /// </p>
        /// <p>-or-</p>
        /// <p>
        ///   <paramref name="pbeParameters" /> indicates that <see cref="PbeEncryptionAlgorithm.TripleDes3KeyPkcs12" />
        ///   should be used, which requires <see langword="char" />-based passwords.
        /// </p>
        /// </exception>
        /// <remarks>
        /// The password bytes are passed directly into the Key Derivation Function (KDF)
        /// used by the algorithm indicated by <paramref name="pbeParameters" />. This
        /// enables compatibility with other systems which use a text encoding other than
        /// UTF-8 when processing passwords with PBKDF2 (Password-Based Key Derivation Function 2).
        /// </remarks>
        public override unsafe bool TryExportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<byte> passwordBytes,
            PbeParameters pbeParameters,
            Span<byte> destination,
            out int bytesWritten)
        {
            ArgumentNullException.ThrowIfNull(pbeParameters);

            PasswordBasedEncryption.ValidatePbeParameters(
                pbeParameters,
                ReadOnlySpan<char>.Empty,
                passwordBytes);

            ECParameters ecParameters = ExportParameters(true);

            fixed (byte* privPtr = ecParameters.D)
            {
                try
                {
                    AsnWriter pkcs8PrivateKey = EccKeyFormatHelper.WritePkcs8PrivateKey(ecParameters);

                    AsnWriter writer = KeyFormatHelper.WriteEncryptedPkcs8(
                        passwordBytes,
                        pkcs8PrivateKey,
                        pbeParameters);

                    return writer.TryEncode(destination, out bytesWritten);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(ecParameters.D);
                }
            }
        }

        /// <summary>
        /// Attempts to export the current key in the PKCS#8 EncryptedPrivateKeyInfo
        /// format into a provided buffer, using a char-based password.
        /// </summary>
        /// <param name="password">
        /// The password to use when encrypting the key material.
        /// </param>
        /// <param name="pbeParameters">
        /// The password-based encryption (PBE) parameters to use when encrypting
        /// the key material.
        /// </param>
        /// <param name="destination">
        /// The byte span to receive the PKCS#8 EncryptedPrivateKeyInfo data.
        /// </param>
        /// <param name="bytesWritten">
        /// When this method returns, contains a value that indicates the number
        /// of bytes written to <paramref name="destination" />. This parameter
        /// is treated as uninitialized.
        /// </param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="destination" /> is big enough
        /// to receive the output; otherwise, <see langword="false" />.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="pbeParameters" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// A derived class has not provided an implementation for <see cref="ExportParameters" />.
        /// </exception>
        /// <exception cref="CryptographicException">
        /// The key could not be exported.
        /// </exception>
        /// <remarks>
        /// When <paramref name="pbeParameters" /> indicates an algorithm that uses PBKDF2
        /// (Password-Based Key Derivation Function 2), the password is converted
        /// to bytes via the UTF-8 encoding.
        /// </remarks>
        public override unsafe bool TryExportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<char> password,
            PbeParameters pbeParameters,
            Span<byte> destination,
            out int bytesWritten)
        {
            ArgumentNullException.ThrowIfNull(pbeParameters);

            PasswordBasedEncryption.ValidatePbeParameters(
                pbeParameters,
                password,
                ReadOnlySpan<byte>.Empty);

            ECParameters ecParameters = ExportParameters(true);

            fixed (byte* privPtr = ecParameters.D)
            {
                try
                {
                    AsnWriter pkcs8PrivateKey = EccKeyFormatHelper.WritePkcs8PrivateKey(ecParameters);

                    AsnWriter writer = KeyFormatHelper.WriteEncryptedPkcs8(
                        password,
                        pkcs8PrivateKey,
                        pbeParameters);

                    return writer.TryEncode(destination, out bytesWritten);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(ecParameters.D);
                }
            }
        }

        /// <summary>
        /// Attempts to export the current key in the PKCS#8 PrivateKeyInfo format
        /// into a provided buffer.
        /// </summary>
        /// <param name="destination">The byte span to receive the PKCS#8 PrivateKeyInfo data.</param>
        /// <param name="bytesWritten">
        /// When this method returns, contains a value that indicates the number
        /// of bytes written to <paramref name="destination" />. This parameter
        /// is treated as uninitialized.
        /// </param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="destination" /> is big enough
        /// to receive the output; otherwise, <see langword="false" />.
        /// </returns>
        /// <exception cref="CryptographicException">
        /// The key could not be exported.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// A derived class has not provided an implementation for <see cref="ExportParameters" />.
        /// </exception>
        public override unsafe bool TryExportPkcs8PrivateKey(
            Span<byte> destination,
            out int bytesWritten)
        {
            ECParameters ecParameters = ExportParameters(true);

            fixed (byte* privPtr = ecParameters.D)
            {
                try
                {
                    AsnWriter writer = EccKeyFormatHelper.WritePkcs8PrivateKey(ecParameters);
                    return writer.TryEncode(destination, out bytesWritten);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(ecParameters.D);
                }
            }
        }

        /// <summary>
        /// Attempts to export the current key in the X.509 SubjectPublicKeyInfo
        /// format into a provided buffer.
        /// </summary>
        /// <param name="destination">The byte span to receive the X.509 SubjectPublicKeyInfo data.</param>
        /// <param name="bytesWritten">
        /// When this method returns, contains a value
        /// that indicates the number of bytes written to <paramref name="destination" />.
        /// This parameter is treated as uninitialized.
        /// </param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="destination" /> is big enough
        /// to receive the output; otherwise, <see langword="false" />.
        /// </returns>
        /// <exception cref="CryptographicException">
        /// The key could not be exported.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// A derived class has not provided an implementation for <see cref="ExportParameters" />.
        /// </exception>
        public override bool TryExportSubjectPublicKeyInfo(
            Span<byte> destination,
            out int bytesWritten)
        {
            ECParameters ecParameters = ExportParameters(false);

            AsnWriter writer = EccKeyFormatHelper.WriteSubjectPublicKeyInfo(ecParameters);
            return writer.TryEncode(destination, out bytesWritten);
        }

        /// <summary>
        /// Imports the public/private keypair from a PKCS#8 EncryptedPrivateKeyInfo
        /// structure after decrypting with a byte-based password, replacing the
        /// keys for this object.
        /// </summary>
        /// <param name="passwordBytes">The bytes to use as a password when decrypting the key material.</param>
        /// <param name="source">
        /// The bytes of a PKCS#8 EncryptedPrivateKeyInfo structure in the ASN.1-BER encoding.
        /// </param>
        /// <param name="bytesRead">
        /// When this method returns, contains a value that indicates the number
        /// of bytes read from <paramref name="source" />. This parameter is treated as uninitialized.
        /// </param>
        /// <exception cref="CryptographicException">
        /// <p>The password is incorrect.</p>
        /// <p>-or-</p>
        /// <p>
        ///   The contents of <paramref name="source" /> indicate the Key Derivation Function (KDF)
        ///   to apply is the legacy PKCS#12 KDF, which requires <see langword="char" />-based passwords.
        /// </p>
        /// <p>-or-</p>
        /// <p>
        ///   The contents of <paramref name="source" /> do not represent an
        ///   ASN.1-BER-encoded PKCS#8 EncryptedPrivateKeyInfo structure.
        /// </p>
        /// <p>-or-</p>
        /// <p>
        ///   The contents of <paramref name="source" /> indicate the key is for
        ///   an algorithm other than the algorithm represented by this instance.
        /// </p>
        /// <p>-or-</p>
        /// <p>
        ///   The contents of <paramref name="source" /> represent the key in a format
        ///   that is not supported.
        /// </p>
        /// <p>-or-</p>
        /// <p>The algorithm-specific key import failed.</p>
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// A derived class has not provided an implementation for <see cref="ImportParameters" />.
        /// </exception>
        /// <remarks>
        /// <p>
        ///   The password bytes are passed directly into the Key Derivation Function (KDF)
        ///   used by the algorithm indicated by the EncryptedPrivateKeyInfo contents.
        ///   This enables compatibility with other systems which use a text encoding
        ///   other than UTF-8 when processing passwords with PBKDF2 (Password-Based Key Derivation Function 2).
        /// </p>
        /// <p>
        ///   This method only supports the binary (BER/CER/DER) encoding of EncryptedPrivateKeyInfo.
        ///   If the value is Base64-encoded, the caller must Base64-decode the contents before calling this method.
        ///   If the contents are PEM-encoded, <see cref="ImportFromEncryptedPem(ReadOnlySpan{char}, ReadOnlySpan{byte})" />
        ///   should be used.
        /// </p>
        /// </remarks>
        public override unsafe void ImportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<byte> passwordBytes,
            ReadOnlySpan<byte> source,
            out int bytesRead)
        {
            KeyFormatHelper.ReadEncryptedPkcs8<ECParameters>(
                s_validOids,
                source,
                passwordBytes,
                EccKeyFormatHelper.FromECPrivateKey,
                out int localRead,
                out ECParameters ret);

            fixed (byte* privPin = ret.D)
            {
                try
                {
                    ImportParameters(ret);
                    bytesRead = localRead;
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(ret.D);
                }
            }
        }

        /// <summary>
        /// Imports the public/private keypair from a PKCS#8 EncryptedPrivateKeyInfo
        /// structure after decrypting with a byte-based password, replacing the
        /// keys for this object.
        /// </summary>
        /// <param name="password">The password to use when decrypting the key material.</param>
        /// <param name="source">
        /// The bytes of a PKCS#8 EncryptedPrivateKeyInfo structure in the ASN.1-BER encoding.
        /// </param>
        /// <param name="bytesRead">
        /// When this method returns, contains a value that indicates the number
        /// of bytes read from <paramref name="source" />. This parameter is treated as uninitialized.
        /// </param>
        /// <exception cref="CryptographicException">
        /// <p>
        ///   The contents of <paramref name="source" /> do not represent an
        ///   ASN.1-BER-encoded PKCS#8 EncryptedPrivateKeyInfo structure.
        /// </p>
        /// <p>-or-</p>
        /// <p>
        ///   The contents of <paramref name="source" /> indicate the key is for
        ///   an algorithm other than the algorithm represented by this instance.
        /// </p>
        /// <p>-or-</p>
        /// <p>
        ///   The contents of <paramref name="source" /> represent the key in a format
        ///   that is not supported.
        /// </p>
        /// <p>-or-</p>
        /// <p>The algorithm-specific key import failed.</p>
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// A derived class has not provided an implementation for <see cref="ImportParameters" />.
        /// </exception>
        /// <remarks>
        /// <p>
        ///   When the contents of <paramref name="source" /> indicate an algorithm that uses PBKDF1
        ///   (Password-Based Key Derivation Function 1) or PBKDF2 (Password-Based Key Derivation Function 2),
        ///   the password is converted to bytes via the UTF-8 encoding.
        /// </p>
        /// <p>
        ///   This method only supports the binary (BER/CER/DER) encoding of EncryptedPrivateKeyInfo.
        ///   If the value is Base64-encoded, the caller must Base64-decode the contents before calling this method.
        ///   If the contents are PEM-encoded, <see cref="ImportFromEncryptedPem(ReadOnlySpan{char}, ReadOnlySpan{char})" />
        ///   should be used.
        /// </p>
        /// </remarks>
        public override unsafe void ImportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<char> password,
            ReadOnlySpan<byte> source,
            out int bytesRead)
        {
            KeyFormatHelper.ReadEncryptedPkcs8<ECParameters>(
                s_validOids,
                source,
                password,
                EccKeyFormatHelper.FromECPrivateKey,
                out int localRead,
                out ECParameters ret);

            fixed (byte* privPin = ret.D)
            {
                try
                {
                    ImportParameters(ret);
                    bytesRead = localRead;
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(ret.D);
                }
            }
        }

        /// <summary>
        /// Imports the public/private keypair from a PKCS#8 PrivateKeyInfo structure
        /// after decryption, replacing the keys for this object.
        /// </summary>
        /// <param name="source">The bytes of a PKCS#8 PrivateKeyInfo structure in the ASN.1-BER encoding.</param>
        /// <param name="bytesRead">
        /// When this method returns, contains a value that indicates the number
        /// of bytes read from <paramref name="source" />. This parameter is treated as uninitialized.
        /// </param>
        /// <exception cref="NotSupportedException">
        /// A derived class has not provided an implementation for <see cref="ImportParameters" />.
        /// </exception>
        /// <exception cref="CryptographicException">
        /// <p>
        ///   The contents of <paramref name="source" /> do not represent an ASN.1-BER-encoded
        ///   PKCS#8 PrivateKeyInfo structure.
        /// </p>
        /// <p>-or-</p>
        /// <p>
        ///   The contents of <paramref name="source" /> indicate the key is for an algorithm
        ///   other than the algorithm represented by this instance.
        /// </p>
        /// <p>-or-</p>
        /// <p>The contents of <paramref name="source" /> represent the key in a format that is not supported.</p>
        /// <p>-or-</p>
        /// <p>
        ///   The algorithm-specific key import failed.
        /// </p>
        /// </exception>
        /// <remarks>
        /// This method only supports the binary (BER/CER/DER) encoding of PrivateKeyInfo.
        /// If the value is Base64-encoded, the caller must Base64-decode the contents before calling this method.
        /// If the value is PEM-encoded, <see cref="ImportFromPem" /> should be used.
        /// </remarks>
        public override unsafe void ImportPkcs8PrivateKey(
            ReadOnlySpan<byte> source,
            out int bytesRead)
        {
            KeyFormatHelper.ReadPkcs8<ECParameters>(
                s_validOids,
                source,
                EccKeyFormatHelper.FromECPrivateKey,
                out int localRead,
                out ECParameters key);

            fixed (byte* privPin = key.D)
            {
                try
                {
                    ImportParameters(key);
                    bytesRead = localRead;
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(key.D);
                }
            }
        }

        /// <summary>
        /// Imports the public key from an X.509 SubjectPublicKeyInfo structure after decryption,
        /// replacing the keys for this object
        /// </summary>
        /// <param name="source">The bytes of an X.509 SubjectPublicKeyInfo structure in the ASN.1-DER encoding.</param>
        /// <param name="bytesRead">
        /// When this method returns, contains a value that indicates the number
        /// of bytes read from <paramref name="source" />. This parameter is treated as uninitialized.
        /// </param>
        /// <exception cref="NotSupportedException">
        /// A derived class has not provided an implementation for <see cref="ImportParameters" />.
        /// </exception>
        /// <exception cref="CryptographicException">
        /// <p>
        ///   The contents of <paramref name="source" /> do not represent an
        ///   ASN.1-DER-encoded X.509 SubjectPublicKeyInfo structure.
        /// </p>
        /// <p>-or-</p>
        /// <p>
        ///   The contents of <paramref name="source" /> indicate the key is for an algorithm
        /// other than the algorithm represented by this instance.
        /// </p>
        /// <p>-or-</p>
        /// <p>
        ///   The contents of <paramref name="source" /> represent the key in a format that is not supported.
        /// </p>
        /// <p>-or-</p>
        /// <p>The algorithm-specific key import failed.</p>
        /// </exception>
        /// <remarks>
        /// This method only supports the binary (DER) encoding of SubjectPublicKeyInfo.
        /// If the value is Base64-encoded, the caller must Base64-decode the contents before calling this method.
        /// If this value is PEM-encoded, <see cref="ImportFromPem" /> should be used.
        /// </remarks>
        public override void ImportSubjectPublicKeyInfo(
            ReadOnlySpan<byte> source,
            out int bytesRead)
        {
            KeyFormatHelper.ReadSubjectPublicKeyInfo<ECParameters>(
                s_validOids,
                source,
                EccKeyFormatHelper.FromECPublicKey,
                out int localRead,
                out ECParameters key);

            ImportParameters(key);
            bytesRead = localRead;
        }

        /// <summary>
        /// Imports the public/private keypair from an ECPrivateKey structure,
        /// replacing the keys for this object.
        /// </summary>
        /// <param name="source">The bytes of an ECPrivateKey structure in the ASN.1-BER encoding.</param>
        /// <param name="bytesRead">
        /// When this method returns, contains a value that indicates the number
        /// of bytes read from <paramref name="source" />. This parameter is treated as uninitialized.
        /// </param>
        /// <exception cref="NotSupportedException">
        /// A derived class has not provided an implementation for <see cref="ImportParameters" />.
        /// </exception>
        /// <exception cref="CryptographicException">
        /// <p>
        ///   The contents of <paramref name="source" /> do not represent an
        ///   ASN.1-BER-encoded PKCS#8 ECPrivateKey structure.
        /// </p>
        /// <p>-or-</p>
        /// <p>The key import failed.</p>
        /// </exception>
        /// <remarks>
        /// This method only supports the binary (BER/CER/DER) encoding of ECPrivateKey.
        /// If the value is Base64-encoded, the caller must Base64-decode the contents before calling this method.
        /// If the value is PEM-encoded, <see cref="ImportFromPem" /> should be used.
        /// </remarks>
        public virtual unsafe void ImportECPrivateKey(ReadOnlySpan<byte> source, out int bytesRead)
        {
            ECParameters ecParameters = EccKeyFormatHelper.FromECPrivateKey(source, out int localRead);

            fixed (byte* privPin = ecParameters.D)
            {
                try
                {
                    ImportParameters(ecParameters);
                    bytesRead = localRead;
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(ecParameters.D);
                }
            }
        }

        /// <summary>Exports the current key in the ECPrivateKey format.</summary>
        /// <returns>A byte array containing the ECPrivateKey representation of this key.</returns>
        /// <exception cref="CryptographicException">The key could not be exported.</exception>
        public virtual unsafe byte[] ExportECPrivateKey()
        {
            ECParameters ecParameters = ExportParameters(true);

            fixed (byte* privPin = ecParameters.D)
            {
                try
                {
                    AsnWriter writer = EccKeyFormatHelper.WriteECPrivateKey(ecParameters);
                    return writer.Encode();
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(ecParameters.D);
                }
            }
        }

        /// <summary>
        /// Attempts to export the current key in the ECPrivateKey format into a provided buffer.
        /// </summary>
        /// <param name="destination">The byte span to receive the ECPrivateKey data.</param>
        /// <param name="bytesWritten">When this method returns, contains a value
        /// that indicates the number of bytes written to <paramref name="destination" />.
        /// This parameter is treated as uninitialized.
        /// </param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="destination" /> is big enough
        /// to receive the output; otherwise, <see langword="false" />.
        /// </returns>
        /// <exception cref="CryptographicException">
        /// The key could not be exported.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// A derived class has not provided an implementation for <see cref="ExportParameters" />.
        /// </exception>
        public virtual unsafe bool TryExportECPrivateKey(Span<byte> destination, out int bytesWritten)
        {
            ECParameters ecParameters = ExportParameters(true);

            fixed (byte* privPin = ecParameters.D)
            {
                try
                {
                    AsnWriter writer = EccKeyFormatHelper.WriteECPrivateKey(ecParameters);
                    return writer.TryEncode(destination, out bytesWritten);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(ecParameters.D);
                }
            }
        }

        /// <summary>
        /// Imports an RFC 7468 PEM-encoded key, replacing the keys for this object.
        /// </summary>
        /// <param name="input">The PEM text of the key to import.</param>
        /// <exception cref="ArgumentException">
        /// <para>
        ///   <paramref name="input"/> does not contain a PEM-encoded key with a recognized label.
        /// </para>
        /// <para>
        ///   -or-
        /// </para>
        /// <para>
        ///   <paramref name="input"/> contains multiple PEM-encoded keys with a recognized label.
        /// </para>
        /// <para>
        ///     -or-
        /// </para>
        /// <para>
        ///   <paramref name="input"/> contains an encrypted PEM-encoded key.
        /// </para>
        /// </exception>
        /// <remarks>
        ///   <para>
        ///   Unsupported or malformed PEM-encoded objects will be ignored. If multiple supported PEM labels
        ///   are found, an exception is raised to prevent importing a key when
        ///   the key is ambiguous.
        ///   </para>
        ///   <para>
        ///   This method supports the following PEM labels:
        ///   <list type="bullet">
        ///     <item><description>PUBLIC KEY</description></item>
        ///     <item><description>PRIVATE KEY</description></item>
        ///     <item><description>EC PRIVATE KEY</description></item>
        ///   </list>
        ///   </para>
        /// </remarks>
        public override void ImportFromPem(ReadOnlySpan<char> input)
        {
            PemKeyHelpers.ImportPem(input, label => {
                if (label.SequenceEqual(PemLabels.Pkcs8PrivateKey))
                {
                    return ImportPkcs8PrivateKey;
                }
                else if (label.SequenceEqual(PemLabels.SpkiPublicKey))
                {
                    return ImportSubjectPublicKeyInfo;
                }
                else if (label.SequenceEqual(PemLabels.EcPrivateKey))
                {
                    return ImportECPrivateKey;
                }
                else
                {
                    return null;
                }
            });
        }

        /// <summary>
        /// Imports an encrypted RFC 7468 PEM-encoded private key, replacing the keys for this object.
        /// </summary>
        /// <param name="input">The PEM text of the encrypted key to import.</param>
        /// <param name="password">
        /// The password to use for decrypting the key material.
        /// </param>
        /// <exception cref="ArgumentException">
        /// <para>
        ///   <paramref name="input"/> does not contain a PEM-encoded key with a recognized label.
        /// </para>
        /// <para>
        ///    -or-
        /// </para>
        /// <para>
        ///   <paramref name="input"/> contains multiple PEM-encoded keys with a recognized label.
        /// </para>
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///   The password is incorrect.
        ///   </para>
        ///   <para>
        ///       -or-
        ///   </para>
        ///   <para>
        ///   The base-64 decoded contents of the PEM text from <paramref name="input" />
        ///   do not represent an ASN.1-BER-encoded PKCS#8 EncryptedPrivateKeyInfo structure.
        ///   </para>
        ///   <para>
        ///       -or-
        ///   </para>
        ///   <para>
        ///   The base-64 decoded contents of the PEM text from <paramref name="input" />
        ///   indicate the key is for an algorithm other than the algorithm
        ///   represented by this instance.
        ///   </para>
        ///   <para>
        ///       -or-
        ///   </para>
        ///   <para>
        ///   The base-64 decoded contents of the PEM text from <paramref name="input" />
        ///   represent the key in a format that is not supported.
        ///   </para>
        ///   <para>
        ///       -or-
        ///   </para>
        ///   <para>
        ///   The algorithm-specific key import failed.
        ///   </para>
        /// </exception>
        /// <remarks>
        ///   <para>
        ///   When the base-64 decoded contents of <paramref name="input" /> indicate an algorithm that uses PBKDF1
        ///   (Password-Based Key Derivation Function 1) or PBKDF2 (Password-Based Key Derivation Function 2),
        ///   the password is converted to bytes via the UTF-8 encoding.
        ///   </para>
        ///   <para>
        ///   Unsupported or malformed PEM-encoded objects will be ignored. If multiple supported PEM labels
        ///   are found, an exception is thrown to prevent importing a key when
        ///   the key is ambiguous.
        ///   </para>
        ///   <para>This method supports the <c>ENCRYPTED PRIVATE KEY</c> PEM label.</para>
        /// </remarks>
        public override void ImportFromEncryptedPem(ReadOnlySpan<char> input, ReadOnlySpan<char> password)
        {
            // Implementation has been pushed down to AsymmetricAlgorithm. The
            // override remains for compatibility.
            base.ImportFromEncryptedPem(input, password);
        }

        /// <summary>
        /// Imports an encrypted RFC 7468 PEM-encoded private key, replacing the keys for this object.
        /// </summary>
        /// <param name="input">The PEM text of the encrypted key to import.</param>
        /// <param name="passwordBytes">
        /// The bytes to use as a password when decrypting the key material.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///     <paramref name="input"/> does not contain a PEM-encoded key with a recognized label.
        ///   </para>
        ///   <para>
        ///       -or-
        ///   </para>
        ///   <para>
        ///     <paramref name="input"/> contains multiple PEM-encoded keys with a recognized label.
        ///   </para>
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///   The password is incorrect.
        ///   </para>
        ///   <para>
        ///       -or-
        ///   </para>
        ///   <para>
        ///   The base-64 decoded contents of the PEM text from <paramref name="input" />
        ///   do not represent an ASN.1-BER-encoded PKCS#8 EncryptedPrivateKeyInfo structure.
        ///   </para>
        ///   <para>
        ///       -or-
        ///   </para>
        ///   <para>
        ///   The base-64 decoded contents of the PEM text from <paramref name="input" />
        ///   indicate the key is for an algorithm other than the algorithm
        ///   represented by this instance.
        ///   </para>
        ///   <para>
        ///       -or-
        ///   </para>
        ///   <para>
        ///   The base-64 decoded contents of the PEM text from <paramref name="input" />
        ///   represent the key in a format that is not supported.
        ///   </para>
        ///   <para>
        ///       -or-
        ///   </para>
        ///   <para>
        ///   The algorithm-specific key import failed.
        ///   </para>
        /// </exception>
        /// <remarks>
        ///   <para>
        ///   The password bytes are passed directly into the Key Derivation Function (KDF)
        ///   used by the algorithm indicated by <c>pbeParameters</c>. This enables compatibility
        ///   with other systems which use a text encoding other than UTF-8 when processing
        ///   passwords with PBKDF2 (Password-Based Key Derivation Function 2).
        ///   </para>
        ///   <para>
        ///   Unsupported or malformed PEM-encoded objects will be ignored. If multiple supported PEM labels
        ///   are found, an exception is thrown to prevent importing a key when
        ///   the key is ambiguous.
        ///   </para>
        ///   <para>This method supports the <c>ENCRYPTED PRIVATE KEY</c> PEM label.</para>
        /// </remarks>
        public override void ImportFromEncryptedPem(ReadOnlySpan<char> input, ReadOnlySpan<byte> passwordBytes)
        {
            // Implementation has been pushed down to AsymmetricAlgorithm. The
            // override remains for compatibility.
            base.ImportFromEncryptedPem(input, passwordBytes);
        }

        /// <summary>
        /// Exports the current key in the ECPrivateKey format, PEM encoded.
        /// </summary>
        /// <returns>A string containing the PEM-encoded ECPrivateKey.</returns>
        /// <exception cref="CryptographicException">
        /// The key could not be exported.
        /// </exception>
        /// <remarks>
        /// <p>
        ///   A PEM-encoded ECPrivateKey will begin with <c>-----BEGIN EC PRIVATE KEY-----</c>
        ///   and end with <c>-----END EC PRIVATE KEY-----</c>, with the base64 encoded DER
        ///   contents of the key between the PEM boundaries.
        /// </p>
        /// <p>
        ///   The PEM is encoded according to the IETF RFC 7468 &quot;strict&quot;
        ///   encoding rules.
        /// </p>
        /// </remarks>
        public unsafe string ExportECPrivateKeyPem()
        {
            byte[] exported = ExportECPrivateKey();

            // Fixed to prevent GC moves.
            fixed (byte* pExported = exported)
            {
                try
                {
                    return PemKeyHelpers.CreatePemFromData(PemLabels.EcPrivateKey, exported);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(exported);
                }
            }
        }

        /// <summary>
        /// Attempts to export the current key in the PEM-encoded
        /// ECPrivateKey format into a provided buffer.
        /// </summary>
        /// <param name="destination">
        /// The character span to receive the PEM-encoded ECPrivateKey data.
        /// </param>
        /// <param name="charsWritten">
        /// When this method returns, contains a value that indicates the number
        /// of characters written to <paramref name="destination" />. This
        /// parameter is treated as uninitialized.
        /// </param>
        /// <returns>
        /// <see langword="true" /> if <paramref name="destination" /> is big enough
        /// to receive the output; otherwise, <see langword="false" />.
        /// </returns>
        /// <exception cref="CryptographicException">
        /// The key could not be exported.
        /// </exception>
        /// <remarks>
        /// <p>
        ///   A PEM-encoded ECPrivateKey will begin with
        ///   <c>-----BEGIN EC PRIVATE KEY-----</c> and end with
        ///   <c>-----END EC PRIVATE KEY-----</c>, with the base64 encoded DER
        ///   contents of the key between the PEM boundaries.
        /// </p>
        /// <p>
        ///   The PEM is encoded according to the IETF RFC 7468 &quot;strict&quot;
        ///   encoding rules.
        /// </p>
        /// </remarks>
        public bool TryExportECPrivateKeyPem(Span<char> destination, out int charsWritten)
        {
            static bool Export(ECAlgorithm alg, Span<byte> destination, out int bytesWritten)
            {
                return alg.TryExportECPrivateKey(destination, out bytesWritten);
            }

            return PemKeyHelpers.TryExportToPem(
                this,
                PemLabels.EcPrivateKey,
                Export,
                destination,
                out charsWritten);
        }
    }
}
