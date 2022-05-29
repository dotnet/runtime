// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Cryptography;
using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography
{
    public abstract class AsymmetricAlgorithm : IDisposable
    {
        protected int KeySizeValue;
        [MaybeNull] protected KeySizes[] LegalKeySizesValue = null!;

        protected AsymmetricAlgorithm() { }

        [Obsolete(Obsoletions.DefaultCryptoAlgorithmsMessage, DiagnosticId = Obsoletions.DefaultCryptoAlgorithmsDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public static AsymmetricAlgorithm Create() =>
            throw new PlatformNotSupportedException(SR.Cryptography_DefaultAlgorithm_NotSupported);

        [RequiresUnreferencedCode(CryptoConfigForwarder.CreateFromNameUnreferencedCodeMessage)]
        public static AsymmetricAlgorithm? Create(string algName) =>
            (AsymmetricAlgorithm?)CryptoConfigForwarder.CreateFromName(algName);

        public virtual int KeySize
        {
            get
            {
                return KeySizeValue;
            }

            set
            {
                if (!value.IsLegalSize(this.LegalKeySizes))
                    throw new CryptographicException(SR.Cryptography_InvalidKeySize);
                KeySizeValue = value;
                return;
            }
        }

        public virtual KeySizes[] LegalKeySizes
        {
            get
            {
                // .NET Framework compat: No null check is performed
                return (KeySizes[])LegalKeySizesValue!.Clone();
            }
        }

        public virtual string? SignatureAlgorithm
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual string? KeyExchangeAlgorithm
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual void FromXmlString(string xmlString)
        {
            throw new NotImplementedException();
        }

        public virtual string ToXmlString(bool includePrivateParameters)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose()
        {
            Clear();
        }

        protected virtual void Dispose(bool disposing)
        {
            return;
        }

        public virtual void ImportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<byte> passwordBytes,
            ReadOnlySpan<byte> source,
            out int bytesRead)
        {
            throw new NotImplementedException(SR.NotSupported_SubclassOverride);
        }

        public virtual void ImportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<char> password,
            ReadOnlySpan<byte> source,
            out int bytesRead)
        {
            throw new NotImplementedException(SR.NotSupported_SubclassOverride);
        }

        public virtual void ImportPkcs8PrivateKey(ReadOnlySpan<byte> source, out int bytesRead) =>
            throw new NotImplementedException(SR.NotSupported_SubclassOverride);

        public virtual void ImportSubjectPublicKeyInfo(ReadOnlySpan<byte> source, out int bytesRead) =>
            throw new NotImplementedException(SR.NotSupported_SubclassOverride);

        public virtual byte[] ExportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<byte> passwordBytes,
            PbeParameters pbeParameters)
        {
            return ExportArray(
                passwordBytes,
                pbeParameters,
                (ReadOnlySpan<byte> span, PbeParameters parameters, Span<byte> destination, out int i) =>
                    TryExportEncryptedPkcs8PrivateKey(span, parameters, destination, out i));
        }

        public virtual byte[] ExportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<char> password,
            PbeParameters pbeParameters)
        {
            return ExportArray(
                password,
                pbeParameters,
                (ReadOnlySpan<char> span, PbeParameters parameters, Span<byte> destination, out int i) =>
                    TryExportEncryptedPkcs8PrivateKey(span, parameters, destination, out i));
        }

        public virtual byte[] ExportPkcs8PrivateKey() =>
            ExportArray(
                (Span<byte> destination, out int i) => TryExportPkcs8PrivateKey(destination, out i));

        public virtual byte[] ExportSubjectPublicKeyInfo() =>
            ExportArray(
                (Span<byte> destination, out int i) => TryExportSubjectPublicKeyInfo(destination, out i));

        public virtual bool TryExportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<byte> passwordBytes,
            PbeParameters pbeParameters,
            Span<byte> destination,
            out int bytesWritten)
        {
            throw new NotImplementedException(SR.NotSupported_SubclassOverride);
        }

        public virtual bool TryExportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<char> password,
            PbeParameters pbeParameters,
            Span<byte> destination,
            out int bytesWritten)
        {
            throw new NotImplementedException(SR.NotSupported_SubclassOverride);
        }

        public virtual bool TryExportPkcs8PrivateKey(Span<byte> destination, out int bytesWritten) =>
            throw new NotImplementedException(SR.NotSupported_SubclassOverride);

        public virtual bool TryExportSubjectPublicKeyInfo(Span<byte> destination, out int bytesWritten) =>
            throw new NotImplementedException(SR.NotSupported_SubclassOverride);

        /// <summary>
        /// Imports an encrypted RFC 7468 PEM-encoded key, replacing the keys for this object.
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
        /// <exception cref="NotImplementedException">
        ///   A derived type has not provided an implementation for
        ///  <see cref="ImportEncryptedPkcs8PrivateKey(ReadOnlySpan{char}, ReadOnlySpan{byte}, out int)" />.
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
        ///   <para>
        ///   Types that override this method may support additional PEM labels.
        ///   </para>
        /// </remarks>
        public virtual void ImportFromEncryptedPem(ReadOnlySpan<char> input, ReadOnlySpan<char> password)
        {
            PemKeyHelpers.ImportEncryptedPem<char>(input, password, ImportEncryptedPkcs8PrivateKey);
        }

        /// <summary>
        /// Imports an encrypted RFC 7468 PEM-encoded key, replacing the keys for this object.
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
        /// <exception cref="NotImplementedException">
        ///   A derived type has not provided an implementation for
        ///  <see cref="ImportEncryptedPkcs8PrivateKey(ReadOnlySpan{byte}, ReadOnlySpan{byte}, out int)" />.
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
        ///   <para>
        ///   Types that override this method may support additional PEM labels.
        ///   </para>
        /// </remarks>
        public virtual void ImportFromEncryptedPem(ReadOnlySpan<char> input, ReadOnlySpan<byte> passwordBytes)
        {
            PemKeyHelpers.ImportEncryptedPem<byte>(input, passwordBytes, ImportEncryptedPkcs8PrivateKey);
        }

        /// <summary>
        /// Imports an RFC 7468 textually encoded key, replacing the keys for this object.
        /// </summary>
        /// <param name="input">The text of the PEM key to import.</param>
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
        /// <exception cref="NotImplementedException">
        ///   A derived type has not provided an implementation for <see cref="ImportPkcs8PrivateKey" />
        ///   or <see cref="ImportSubjectPublicKeyInfo" />.
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
        ///   </list>
        ///   </para>
        ///   <para>
        ///   Types that override this method may support additional PEM labels.
        ///   </para>
        /// </remarks>
        public virtual void ImportFromPem(ReadOnlySpan<char> input)
        {
            PemKeyHelpers.ImportPem(input, label =>
                label switch
                {
                    PemLabels.Pkcs8PrivateKey => ImportPkcs8PrivateKey,
                    PemLabels.SpkiPublicKey => ImportSubjectPublicKeyInfo,
                    _ => null,
                });
        }

        /// <summary>
        /// Exports the current key in the PKCS#8 PrivateKeyInfo format, PEM encoded.
        /// </summary>
        /// <returns>A string containing the PEM-encoded PKCS#8 PrivateKeyInfo.</returns>
        /// <exception cref="NotImplementedException">
        /// An implementation for <see cref="ExportPkcs8PrivateKey" /> or
        /// <see cref="TryExportPkcs8PrivateKey" /> has not been provided.
        /// </exception>
        /// <exception cref="CryptographicException">
        /// The key could not be exported.
        /// </exception>
        /// <remarks>
        /// <p>
        ///   A PEM-encoded PKCS#8 PrivateKeyInfo will begin with <c>-----BEGIN PRIVATE KEY-----</c>
        ///   and end with <c>-----END PRIVATE KEY-----</c>, with the base64 encoded DER
        ///   contents of the key between the PEM boundaries.
        /// </p>
        /// <p>
        ///   The PEM is encoded according to the IETF RFC 7468 &quot;strict&quot;
        ///   encoding rules.
        /// </p>
        /// </remarks>
        public unsafe string ExportPkcs8PrivateKeyPem()
        {
            byte[] exported = ExportPkcs8PrivateKey();

            // Fixed to prevent GC moves.
            fixed (byte* pExported = exported)
            {
                try
                {
                    return PemEncoding.WriteString(PemLabels.Pkcs8PrivateKey, exported);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(exported);
                }
            }
        }

        /// <summary>
        /// Exports the current key in the PKCS#8 EncryptedPrivateKeyInfo format
        /// with a char-based password, PEM encoded.
        /// </summary>
        /// <param name="password">
        /// The password to use when encrypting the key material.
        /// </param>
        /// <param name="pbeParameters">
        /// The password-based encryption (PBE) parameters to use when encrypting the key material.
        /// </param>
        /// <returns>A string containing the PEM-encoded PKCS#8 EncryptedPrivateKeyInfo.</returns>
        /// <exception cref="NotImplementedException">
        /// An implementation for <see cref="ExportEncryptedPkcs8PrivateKey(ReadOnlySpan{char}, PbeParameters)" /> or
        /// <see cref="TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan{char}, PbeParameters, Span{byte}, out int)" /> has not been provided.
        /// </exception>
        /// <exception cref="CryptographicException">
        /// The key could not be exported.
        /// </exception>
        /// <remarks>
        /// <p>
        ///   When <paramref name="pbeParameters" /> indicates an algorithm that
        ///   uses PBKDF2 (Password-Based Key Derivation Function 2), the password
        ///   is converted to bytes via the UTF-8 encoding.
        /// </p>
        /// <p>
        ///   A PEM-encoded PKCS#8 EncryptedPrivateKeyInfo will begin with
        ///  <c>-----BEGIN ENCRYPTED PRIVATE KEY-----</c> and end with
        ///  <c>-----END ENCRYPTED PRIVATE KEY-----</c>, with the base64 encoded DER
        ///   contents of the key between the PEM boundaries.
        /// </p>
        /// <p>
        ///   The PEM is encoded according to the IETF RFC 7468 &quot;strict&quot;
        ///   encoding rules.
        /// </p>
        /// </remarks>
        public unsafe string ExportEncryptedPkcs8PrivateKeyPem(ReadOnlySpan<char> password, PbeParameters pbeParameters)
        {
            byte[] exported = ExportEncryptedPkcs8PrivateKey(password, pbeParameters);

            // Fixed to prevent GC moves.
            fixed (byte* pExported = exported)
            {
                try
                {
                    return PemEncoding.WriteString(PemLabels.EncryptedPkcs8PrivateKey, exported);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(exported);
                }
            }
        }

        /// <summary>
        /// Exports the public-key portion of the current key in the X.509
        /// SubjectPublicKeyInfo format, PEM encoded.
        /// </summary>
        /// <returns>A string containing the PEM-encoded X.509 SubjectPublicKeyInfo.</returns>
        /// <exception cref="NotImplementedException">
        /// An implementation for <see cref="ExportSubjectPublicKeyInfo" /> or
        /// <see cref="TryExportSubjectPublicKeyInfo" /> has not been provided.
        /// </exception>
        /// <exception cref="CryptographicException">
        /// The key could not be exported.
        /// </exception>
        /// <remarks>
        /// <p>
        ///   A PEM-encoded X.509 SubjectPublicKeyInfo will begin with
        ///  <c>-----BEGIN PUBLIC KEY-----</c> and end with
        ///  <c>-----END PUBLIC KEY-----</c>, with the base64 encoded DER
        ///   contents of the key between the PEM boundaries.
        /// </p>
        /// <p>
        ///   The PEM is encoded according to the IETF RFC 7468 &quot;strict&quot;
        ///   encoding rules.
        /// </p>
        /// </remarks>
        public string ExportSubjectPublicKeyInfoPem()
        {
            byte[] exported = ExportSubjectPublicKeyInfo();
            return PemEncoding.WriteString(PemLabels.SpkiPublicKey, exported);
        }

        /// <summary>
        /// Attempts to export the current key in the PEM-encoded X.509
        /// SubjectPublicKeyInfo format into a provided buffer.
        /// </summary>
        /// <param name="destination">
        /// The character span to receive the PEM-encoded X.509 SubjectPublicKeyInfo data.
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
        /// <exception cref="NotImplementedException">
        /// An implementation for <see cref="TryExportSubjectPublicKeyInfo" />
        /// has not been provided.
        /// </exception>
        /// <exception cref="CryptographicException">
        /// The key could not be exported.
        /// </exception>
        /// <remarks>
        /// <p>
        ///   A PEM-encoded X.509 SubjectPublicKeyInfo will begin with
        /// <c>-----BEGIN PUBLIC KEY-----</c> and end with
        /// <c>-----END PUBLIC KEY-----</c>, with the base64 encoded DER
        ///   contents of the key between the PEM boundaries.
        /// </p>
        /// <p>
        ///   The PEM is encoded according to the IETF RFC 7468 &quot;strict&quot;
        ///   encoding rules.
        /// </p>
        /// </remarks>
        public bool TryExportSubjectPublicKeyInfoPem(Span<char> destination, out int charsWritten)
        {
            static bool Export(AsymmetricAlgorithm alg, Span<byte> destination, out int bytesWritten)
            {
                return alg.TryExportSubjectPublicKeyInfo(destination, out bytesWritten);
            }

            return PemKeyHelpers.TryExportToPem(
                this,
                PemLabels.SpkiPublicKey,
                Export,
                destination,
                out charsWritten);
        }

        /// <summary>
        /// Attempts to export the current key in the PEM-encoded PKCS#8
        /// PrivateKeyInfo format into a provided buffer.
        /// </summary>
        /// <param name="destination">
        /// The character span to receive the PEM-encoded PKCS#8 PrivateKeyInfo data.
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
        /// <exception cref="NotImplementedException">
        /// An implementation for <see cref="TryExportPkcs8PrivateKey" />
        /// has not been provided.
        /// </exception>
        /// <exception cref="CryptographicException">
        /// The key could not be exported.
        /// </exception>
        /// <remarks>
        /// <p>
        ///   A PEM-encoded PKCS#8 PrivateKeyInfo will begin with <c>-----BEGIN PRIVATE KEY-----</c>
        ///   and end with <c>-----END PRIVATE KEY-----</c>, with the base64 encoded DER
        ///   contents of the key between the PEM boundaries.
        /// </p>
        /// <p>
        ///   The PEM is encoded according to the IETF RFC 7468 &quot;strict&quot;
        ///   encoding rules.
        /// </p>
        /// </remarks>
        public bool TryExportPkcs8PrivateKeyPem(Span<char> destination, out int charsWritten)
        {
            static bool Export(AsymmetricAlgorithm alg, Span<byte> destination, out int bytesWritten)
            {
                return alg.TryExportPkcs8PrivateKey(destination, out bytesWritten);
            }

            return PemKeyHelpers.TryExportToPem(
                this,
                PemLabels.Pkcs8PrivateKey,
                Export,
                destination,
                out charsWritten);
        }

        /// <summary>
        /// Exports the current key in the PKCS#8 EncryptedPrivateKeyInfo format
        /// with a char-based password, PEM encoded.
        /// </summary>
        /// <param name="password">
        /// The password to use when encrypting the key material.
        /// </param>
        /// <param name="pbeParameters">
        /// The password-based encryption (PBE) parameters to use when encrypting the key material.
        /// </param>
        /// <param name="destination">
        /// The character span to receive the PEM-encoded PKCS#8 EncryptedPrivateKeyInfo data.
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
        /// <exception cref="NotImplementedException">
        /// An implementation for <see cref="TryExportEncryptedPkcs8PrivateKey(ReadOnlySpan{char}, PbeParameters, Span{byte}, out int)" />
        /// has not been provided.
        /// </exception>
        /// <exception cref="CryptographicException">
        /// The key could not be exported.
        /// </exception>
        /// <remarks>
        /// <p>
        ///   When <paramref name="pbeParameters" /> indicates an algorithm that
        ///   uses PBKDF2 (Password-Based Key Derivation Function 2), the password
        ///   is converted to bytes via the UTF-8 encoding.
        /// </p>
        /// <p>
        ///   A PEM-encoded PKCS#8 EncryptedPrivateKeyInfo will begin with
        /// <c>-----BEGIN ENCRYPTED PRIVATE KEY-----</c> and end with
        /// <c>-----END ENCRYPTED PRIVATE KEY-----</c>, with the base64 encoded DER
        ///   contents of the key between the PEM boundaries.
        /// </p>
        /// <p>
        ///   The PEM is encoded according to the IETF RFC 7468 &quot;strict&quot;
        ///   encoding rules.
        /// </p>
        /// </remarks>
        public bool TryExportEncryptedPkcs8PrivateKeyPem(ReadOnlySpan<char> password, PbeParameters pbeParameters, Span<char> destination, out int charsWritten)
        {
            static bool Export(
                AsymmetricAlgorithm alg,
                ReadOnlySpan<char> password,
                PbeParameters pbeParameters,
                Span<byte> destination,
                out int bytesWritten)
            {
                return alg.TryExportEncryptedPkcs8PrivateKey(password, pbeParameters, destination, out bytesWritten);
            }

            return PemKeyHelpers.TryExportToEncryptedPem(
                this,
                password,
                pbeParameters,
                Export,
                destination,
                out charsWritten);
        }

        private delegate bool TryExportPbe<T>(
            ReadOnlySpan<T> password,
            PbeParameters pbeParameters,
            Span<byte> destination,
            out int bytesWritten);

        private delegate bool TryExport(Span<byte> destination, out int bytesWritten);

        private static unsafe byte[] ExportArray<T>(
            ReadOnlySpan<T> password,
            PbeParameters pbeParameters,
            TryExportPbe<T> exporter)
        {
            int bufSize = 4096;

            while (true)
            {
                byte[] buf = CryptoPool.Rent(bufSize);
                int bytesWritten = 0;
                bufSize = buf.Length;

                fixed (byte* bufPtr = buf)
                {
                    try
                    {
                        if (exporter(password, pbeParameters, buf, out bytesWritten))
                        {
                            Span<byte> writtenSpan = new Span<byte>(buf, 0, bytesWritten);
                            return writtenSpan.ToArray();
                        }
                    }
                    finally
                    {
                        CryptoPool.Return(buf, bytesWritten);
                    }

                    bufSize = checked(bufSize * 2);
                }
            }
        }

        private static unsafe byte[] ExportArray(TryExport exporter)
        {
            int bufSize = 4096;

            while (true)
            {
                byte[] buf = CryptoPool.Rent(bufSize);
                int bytesWritten = 0;
                bufSize = buf.Length;

                fixed (byte* bufPtr = buf)
                {
                    try
                    {
                        if (exporter(buf, out bytesWritten))
                        {
                            Span<byte> writtenSpan = new Span<byte>(buf, 0, bytesWritten);
                            return writtenSpan.ToArray();
                        }
                    }
                    finally
                    {
                        CryptoPool.Return(buf, bytesWritten);
                    }

                    bufSize = checked(bufSize * 2);
                }
            }
        }
    }
}
