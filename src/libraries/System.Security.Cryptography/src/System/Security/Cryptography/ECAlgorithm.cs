// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Cryptography;
using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography
{
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

        public override unsafe bool TryExportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<byte> passwordBytes,
            PbeParameters pbeParameters,
            Span<byte> destination,
            out int bytesWritten)
        {
            if (pbeParameters is null)
                throw new ArgumentNullException(nameof(pbeParameters));

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

        public override unsafe bool TryExportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<char> password,
            PbeParameters pbeParameters,
            Span<byte> destination,
            out int bytesWritten)
        {
            if (pbeParameters == null)
                throw new ArgumentNullException(nameof(pbeParameters));

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

        public override bool TryExportSubjectPublicKeyInfo(
            Span<byte> destination,
            out int bytesWritten)
        {
            ECParameters ecParameters = ExportParameters(false);

            AsnWriter writer = EccKeyFormatHelper.WriteSubjectPublicKeyInfo(ecParameters);
            return writer.TryEncode(destination, out bytesWritten);
        }

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
    }
}
