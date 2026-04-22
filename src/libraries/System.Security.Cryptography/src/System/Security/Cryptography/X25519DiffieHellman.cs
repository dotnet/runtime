// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    /// <summary>
    ///   Represents an X25519 Diffie-Hellman key.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Developers are encouraged to program against the <c>X25519DiffieHellman</c> base class,
    ///     rather than any specific derived class.
    ///     The derived classes are intended for interop with the underlying system
    ///     cryptographic libraries.
    ///   </para>
    /// </remarks>
    public abstract class X25519DiffieHellman : IDisposable
    {
        private static readonly string[] s_knownOids = [Oids.X25519];

        private bool _disposed;

        /// <summary>
        ///   The size of the secret agreement, in bytes.
        /// </summary>
        public const int SecretAgreementSizeInBytes = 32;

        /// <summary>
        ///   The size of the private key, in bytes.
        /// </summary>
        public const int PrivateKeySizeInBytes = 32;

        /// <summary>
        ///   The size of the public key, in bytes.
        /// </summary>
        public const int PublicKeySizeInBytes = 32;

        // Pre-encoded SPKI for X25519 is 44 bytes: 12 byte preamble + 32 byte public key.
        private const int SpkiSizeInBytes = 12 + PublicKeySizeInBytes;

        /// <summary>
        ///   Gets a value that indicates whether the algorithm is supported on the current platform.
        /// </summary>
        /// <value>
        ///   <see langword="true" /> if the algorithm is supported; otherwise, <see langword="false" />.
        /// </value>
        public static bool IsSupported => X25519DiffieHellmanImplementation.IsSupported;

        /// <summary>
        ///   Derives a raw secret agreement with the other party's key.
        /// </summary>
        /// <param name="otherParty">
        ///   The other party's key.
        /// </param>
        /// <returns>
        ///   The secret agreement.
        /// </returns>
        /// <remarks>
        ///   The raw secret agreement value is expected to be used as input into a Key Derivation Function,
        ///   and not used directly as key material.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="otherParty" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred during the secret agreement derivation.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        public byte[] DeriveRawSecretAgreement(X25519DiffieHellman otherParty)
        {
            ArgumentNullException.ThrowIfNull(otherParty);
            ThrowIfDisposed();

            byte[] buffer = new byte[SecretAgreementSizeInBytes];
            DeriveRawSecretAgreementCore(otherParty, buffer);
            return buffer;
        }

        /// <summary>
        ///   Derives a raw secret agreement with the other party's key, writing it into the provided buffer.
        /// </summary>
        /// <param name="otherParty">
        ///   The other party's key.
        /// </param>
        /// <param name="destination">
        ///   The buffer to receive the secret agreement.
        /// </param>
        /// <remarks>
        ///   The raw secret agreement value is expected to be used as input into a Key Derivation Function,
        ///   and not used directly as key material.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="otherParty" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="destination" /> is the incorrect length to receive the secret agreement.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred during the secret agreement derivation.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        public void DeriveRawSecretAgreement(X25519DiffieHellman otherParty, Span<byte> destination)
        {
            ArgumentNullException.ThrowIfNull(otherParty);

            if (destination.Length != SecretAgreementSizeInBytes)
            {
                throw new ArgumentException(
                    SR.Format(SR.Argument_DestinationImprecise, SecretAgreementSizeInBytes),
                    nameof(destination));
            }

            ThrowIfDisposed();
            DeriveRawSecretAgreementCore(otherParty, destination);
        }

        /// <summary>
        ///   Generates a new X25519 Diffie-Hellman key.
        /// </summary>
        /// <returns>
        ///   The generated key.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   An error occurred generating the X25519 Diffie-Hellman key.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support X25519 Diffie-Hellman. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports X25519 Diffie-Hellman.
        /// </exception>
        public static X25519DiffieHellman GenerateKey()
        {
            ThrowIfNotSupported();
            return X25519DiffieHellmanImplementation.GenerateKeyImpl();
        }

        /// <summary>
        ///   Exports the private key.
        /// </summary>
        /// <returns>
        ///   The private key.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   An error occurred while exporting the key.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        public byte[] ExportPrivateKey()
        {
            ThrowIfDisposed();

            byte[] buffer = new byte[PrivateKeySizeInBytes];
            ExportPrivateKeyCore(buffer);
            return buffer;
        }

        /// <summary>
        ///   Exports the private key into the provided buffer.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the private key.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="destination" /> is the incorrect length to receive the private key.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while exporting the key.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        public void ExportPrivateKey(Span<byte> destination)
        {
            if (destination.Length != PrivateKeySizeInBytes)
            {
                throw new ArgumentException(
                    SR.Format(SR.Argument_DestinationImprecise, PrivateKeySizeInBytes),
                    nameof(destination));
            }

            ThrowIfDisposed();
            ExportPrivateKeyCore(destination);
        }

        /// <summary>
        ///   Exports the public key.
        /// </summary>
        /// <returns>
        ///   The public key.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   An error occurred while exporting the key.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        public byte[] ExportPublicKey()
        {
            ThrowIfDisposed();

            byte[] buffer = new byte[PublicKeySizeInBytes];
            ExportPublicKeyCore(buffer);
            return buffer;
        }

        /// <summary>
        ///   Exports the public key into the provided buffer.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the public key.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="destination" /> is the incorrect length to receive the public key.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while exporting the key.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        public void ExportPublicKey(Span<byte> destination)
        {
            if (destination.Length != PublicKeySizeInBytes)
            {
                throw new ArgumentException(
                    SR.Format(SR.Argument_DestinationImprecise, PublicKeySizeInBytes),
                    nameof(destination));
            }

            ThrowIfDisposed();
            ExportPublicKeyCore(destination);
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
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while exporting the key.
        /// </exception>
        public bool TryExportSubjectPublicKeyInfo(Span<byte> destination, out int bytesWritten)
        {
            ThrowIfDisposed();
            return TryExportSubjectPublicKeyInfoCore(destination, out bytesWritten);
        }

        /// <summary>
        ///   Exports the public-key portion of the current key in the X.509 SubjectPublicKeyInfo format.
        /// </summary>
        /// <returns>
        ///   A byte array containing the X.509 SubjectPublicKeyInfo representation of the public-key portion of this key.
        /// </returns>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while exporting the key.
        /// </exception>
        public byte[] ExportSubjectPublicKeyInfo()
        {
            ThrowIfDisposed();
            byte[] result = new byte[SpkiSizeInBytes];
            bool exported = TryExportSubjectPublicKeyInfoCore(result, out int bytesWritten);

            if (!exported || bytesWritten != SpkiSizeInBytes)
            {
                Debug.Fail("Export unexpectedly failed to pre-sized buffer or wrote an unexpected number of bytes.");
                throw new CryptographicException();
            }

            return result;
        }

        /// <summary>
        ///   Exports the public-key portion of the current key in a PEM-encoded representation of
        ///   the X.509 SubjectPublicKeyInfo format.
        /// </summary>
        /// <returns>
        ///   A string containing the PEM-encoded representation of the X.509 SubjectPublicKeyInfo
        ///   representation of the public-key portion of this key.
        /// </returns>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while exporting the key.
        /// </exception>
        public string ExportSubjectPublicKeyInfoPem()
        {
            ThrowIfDisposed();
            Span<byte> spki = stackalloc byte[SpkiSizeInBytes];
            bool exported = TryExportSubjectPublicKeyInfoCore(spki, out int bytesWritten);

            if (!exported || bytesWritten != SpkiSizeInBytes)
            {
                Debug.Fail("Export unexpectedly failed to pre-sized buffer or wrote an unexpected number of bytes.");
                throw new CryptographicException();
            }

            return PemEncoding.WriteString(PemLabels.SpkiPublicKey, spki);
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
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while exporting the key.
        /// </exception>
        public bool TryExportPkcs8PrivateKey(Span<byte> destination, out int bytesWritten)
        {
            ThrowIfDisposed();

            // An X25519 PKCS#8 PrivateKeyInfo with no attributes is 48 bytes:
            //   SEQUENCE (2) + INTEGER version (3) + SEQUENCE AlgorithmIdentifier (7) +
            //   OCTET STRING outer (2) + OCTET STRING CurvePrivateKey (2) + 32 byte key = 48.
            // A buffer smaller than that cannot hold a PKCS#8 encoded key.
            const int MinimumPossiblePkcs8X25519Key = 48;

            if (destination.Length < MinimumPossiblePkcs8X25519Key)
            {
                bytesWritten = 0;
                return false;
            }

            return TryExportPkcs8PrivateKeyCore(destination, out bytesWritten);
        }

        /// <summary>
        ///   Exports the current key in the PKCS#8 PrivateKeyInfo format.
        /// </summary>
        /// <returns>
        ///   A byte array containing the PKCS#8 PrivateKeyInfo representation of this key.
        /// </returns>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while exporting the key.
        /// </exception>
        public byte[] ExportPkcs8PrivateKey()
        {
            ThrowIfDisposed();
            return ExportPkcs8PrivateKeyCallback(static pkcs8 => pkcs8.ToArray());
        }

        /// <summary>
        ///   Exports the current key in a PEM-encoded representation of the PKCS#8 PrivateKeyInfo format.
        /// </summary>
        /// <returns>
        ///   A string containing the PEM-encoded representation of the PKCS#8 PrivateKeyInfo.
        /// </returns>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while exporting the key.
        /// </exception>
        public string ExportPkcs8PrivateKeyPem()
        {
            ThrowIfDisposed();
            return ExportPkcs8PrivateKeyCallback(static pkcs8 => PemEncoding.WriteString(PemLabels.Pkcs8PrivateKey, pkcs8));
        }

        /// <summary>
        ///   Attempts to export the current key in the PKCS#8 EncryptedPrivateKeyInfo format into a provided buffer,
        ///   using a byte-based password.
        /// </summary>
        /// <param name="passwordBytes">
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
        ///    <paramref name="pbeParameters"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        /// <exception cref="CryptographicException">
        ///   <para>This instance only represents a public key.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="pbeParameters"/> does not represent a valid password-based encryption algorithm.</para>
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

            AsnWriter writer = ExportEncryptedPkcs8PrivateKeyCore<byte>(
                passwordBytes,
                pbeParameters,
                KeyFormatHelper.WriteEncryptedPkcs8);
            return writer.TryEncode(destination, out bytesWritten);
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
        ///    <paramref name="pbeParameters"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        /// <exception cref="CryptographicException">
        ///   <para>This instance only represents a public key.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="pbeParameters"/> does not represent a valid password-based encryption algorithm.</para>
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

            AsnWriter writer = ExportEncryptedPkcs8PrivateKeyCore<char>(
                password,
                pbeParameters,
                KeyFormatHelper.WriteEncryptedPkcs8);
            return writer.TryEncode(destination, out bytesWritten);
        }

        /// <summary>
        ///   Attempts to export the current key in the PKCS#8 EncryptedPrivateKeyInfo format into a provided buffer,
        ///   using a string password.
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
        ///    <paramref name="password"/> or <paramref name="pbeParameters"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        /// <exception cref="CryptographicException">
        ///   <para>This instance only represents a public key.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="pbeParameters"/> does not represent a valid password-based encryption algorithm.</para>
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
        ///   Exports the current key in the PKCS#8 EncryptedPrivateKeyInfo format with a byte-based password.
        /// </summary>
        /// <param name="passwordBytes">
        ///   The password to use when encrypting the key material.
        /// </param>
        /// <param name="pbeParameters">
        ///   The password-based encryption (PBE) parameters to use when encrypting the key material.
        /// </param>
        /// <returns>
        ///   A byte array containing the PKCS#8 EncryptedPrivateKeyInfo representation of this key.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///    <paramref name="pbeParameters"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        /// <exception cref="CryptographicException">
        ///   <para>This instance only represents a public key.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="pbeParameters"/> does not represent a valid password-based encryption algorithm.</para>
        /// </exception>
        public byte[] ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters)
        {
            ArgumentNullException.ThrowIfNull(pbeParameters);
            PasswordBasedEncryption.ValidatePbeParameters(pbeParameters, ReadOnlySpan<char>.Empty, passwordBytes);
            ThrowIfDisposed();

            AsnWriter writer = ExportEncryptedPkcs8PrivateKeyCore<byte>(
                passwordBytes,
                pbeParameters,
                KeyFormatHelper.WriteEncryptedPkcs8);
            return writer.Encode();
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
        ///   A byte array containing the PKCS#8 EncryptedPrivateKeyInfo representation of this key.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///    <paramref name="pbeParameters"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        /// <exception cref="CryptographicException">
        ///   <para>This instance only represents a public key.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="pbeParameters"/> does not represent a valid password-based encryption algorithm.</para>
        /// </exception>
        public byte[] ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, PbeParameters pbeParameters)
        {
            ArgumentNullException.ThrowIfNull(pbeParameters);
            PasswordBasedEncryption.ValidatePbeParameters(pbeParameters, password, ReadOnlySpan<byte>.Empty);
            ThrowIfDisposed();

            AsnWriter writer = ExportEncryptedPkcs8PrivateKeyCore<char>(
                password,
                pbeParameters,
                KeyFormatHelper.WriteEncryptedPkcs8);
            return writer.Encode();
        }

        /// <summary>
        ///   Exports the current key in the PKCS#8 EncryptedPrivateKeyInfo format with a string password.
        /// </summary>
        /// <param name="password">
        ///   The password to use when encrypting the key material.
        /// </param>
        /// <param name="pbeParameters">
        ///   The password-based encryption (PBE) parameters to use when encrypting the key material.
        /// </param>
        /// <returns>
        ///   A byte array containing the PKCS#8 EncryptedPrivateKeyInfo representation of this key.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///    <paramref name="pbeParameters" /> or <paramref name="password" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        /// <exception cref="CryptographicException">
        ///   <para>This instance only represents a public key.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="pbeParameters"/> does not represent a valid password-based encryption algorithm.</para>
        /// </exception>
        public byte[] ExportEncryptedPkcs8PrivateKey(string password, PbeParameters pbeParameters)
        {
            ArgumentNullException.ThrowIfNull(password);
            return ExportEncryptedPkcs8PrivateKey(password.AsSpan(), pbeParameters);
        }

        /// <summary>
        ///   Exports the current key in a PEM-encoded representation of the PKCS#8 EncryptedPrivateKeyInfo
        ///   format, using a byte-based password.
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
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        /// <exception cref="CryptographicException">
        ///   <para><paramref name="pbeParameters"/> specifies a KDF that requires a char-based password.</para>
        ///   <para>-or-</para>
        ///   <para>This instance only represents a public key.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        public string ExportEncryptedPkcs8PrivateKeyPem(ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters)
        {
            ArgumentNullException.ThrowIfNull(pbeParameters);
            PasswordBasedEncryption.ValidatePbeParameters(pbeParameters, ReadOnlySpan<char>.Empty, passwordBytes);
            ThrowIfDisposed();

            AsnWriter writer = ExportEncryptedPkcs8PrivateKeyCore<byte>(
                passwordBytes,
                pbeParameters,
                KeyFormatHelper.WriteEncryptedPkcs8);

            // Skip clear since the data is already encrypted.
            return Helpers.EncodeAsnWriterToPem(PemLabels.EncryptedPkcs8PrivateKey, writer, clear: false);
        }

        /// <summary>
        ///   Exports the current key in a PEM-encoded representation of the PKCS#8 EncryptedPrivateKeyInfo
        ///   format, using a char-based password.
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
        ///    <paramref name="pbeParameters"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        /// <exception cref="CryptographicException">
        ///   <para>This instance only represents a public key.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        public string ExportEncryptedPkcs8PrivateKeyPem(ReadOnlySpan<char> password, PbeParameters pbeParameters)
        {
            ArgumentNullException.ThrowIfNull(pbeParameters);
            PasswordBasedEncryption.ValidatePbeParameters(pbeParameters, password, ReadOnlySpan<byte>.Empty);
            ThrowIfDisposed();

            AsnWriter writer = ExportEncryptedPkcs8PrivateKeyCore<char>(
                password,
                pbeParameters,
                KeyFormatHelper.WriteEncryptedPkcs8);

            // Skip clear since the data is already encrypted.
            return Helpers.EncodeAsnWriterToPem(PemLabels.EncryptedPkcs8PrivateKey, writer, clear: false);
        }

        /// <summary>
        ///   Exports the current key in a PEM-encoded representation of the PKCS#8 EncryptedPrivateKeyInfo
        ///   format, using a string password.
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
        ///    <paramref name="password"/> or <paramref name="pbeParameters"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The object has already been disposed.</exception>
        /// <exception cref="CryptographicException">
        ///   <para>This instance only represents a public key.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        public string ExportEncryptedPkcs8PrivateKeyPem(string password, PbeParameters pbeParameters)
        {
            ArgumentNullException.ThrowIfNull(password);
            return ExportEncryptedPkcs8PrivateKeyPem(password.AsSpan(), pbeParameters);
        }

        /// <summary>
        ///   When overridden in a derived class, derives a raw secret agreement with the other party's key,
        ///   writing it into the provided buffer.
        /// </summary>
        /// <param name="otherParty">
        ///   The other party's key.
        /// </param>
        /// <param name="destination">
        ///   The buffer to receive the secret agreement.
        /// </param>
        /// <exception cref="CryptographicException">
        ///   An error occurred during the secret agreement derivation.
        /// </exception>
        protected abstract void DeriveRawSecretAgreementCore(X25519DiffieHellman otherParty, Span<byte> destination);

        /// <summary>
        ///   When overridden in a derived class, exports the private key into the provided buffer.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the private key.
        /// </param>
        protected abstract void ExportPrivateKeyCore(Span<byte> destination);

        /// <summary>
        ///   When overridden in a derived class, exports the public key into the provided buffer.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the public key.
        /// </param>
        protected abstract void ExportPublicKeyCore(Span<byte> destination);

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
        /// <exception cref="CryptographicException">
        ///   An error occurred while exporting the key.
        /// </exception>
        protected abstract bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten);

        /// <summary>
        ///   Imports an X25519 Diffie-Hellman key from a private key.
        /// </summary>
        /// <param name="source">
        ///   The private key.
        /// </param>
        /// <returns>
        ///   The imported key.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="source" /> has a length that is not <see cref="PrivateKeySizeInBytes" />.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while importing the key.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support X25519 Diffie-Hellman. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports X25519 Diffie-Hellman.
        /// </exception>
        public static X25519DiffieHellman ImportPrivateKey(byte[] source)
        {
            ArgumentNullException.ThrowIfNull(source);
            return ImportPrivateKey(new ReadOnlySpan<byte>(source));
        }

        /// <summary>
        ///   Imports an X25519 Diffie-Hellman key from a private key.
        /// </summary>
        /// <param name="source">
        ///   The private key.
        /// </param>
        /// <returns>
        ///   The imported key.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="source" /> has a length that is not <see cref="PrivateKeySizeInBytes" />.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while importing the key.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support X25519 Diffie-Hellman. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports X25519 Diffie-Hellman.
        /// </exception>
        public static X25519DiffieHellman ImportPrivateKey(ReadOnlySpan<byte> source)
        {
            if (source.Length != PrivateKeySizeInBytes)
                throw new ArgumentException(SR.Argument_PrivateKeyWrongSizeForAlgorithm, nameof(source));

            ThrowIfNotSupported();
            return X25519DiffieHellmanImplementation.ImportPrivateKeyImpl(source);
        }

        /// <summary>
        ///   Imports an X25519 Diffie-Hellman key from a public key.
        /// </summary>
        /// <param name="source">
        ///   The public key.
        /// </param>
        /// <returns>
        ///   The imported key.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="source" /> has a length that is not <see cref="PublicKeySizeInBytes" />.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while importing the key.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support X25519 Diffie-Hellman. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports X25519 Diffie-Hellman.
        /// </exception>
        public static X25519DiffieHellman ImportPublicKey(byte[] source)
        {
            ArgumentNullException.ThrowIfNull(source);
            return ImportPublicKey(new ReadOnlySpan<byte>(source));
        }

        /// <summary>
        ///   Imports an X25519 Diffie-Hellman key from a public key.
        /// </summary>
        /// <param name="source">
        ///   The public key.
        /// </param>
        /// <returns>
        ///   The imported key.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="source" /> has a length that is not <see cref="PublicKeySizeInBytes" />.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while importing the key.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support X25519 Diffie-Hellman. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports X25519 Diffie-Hellman.
        /// </exception>
        public static X25519DiffieHellman ImportPublicKey(ReadOnlySpan<byte> source)
        {
            if (source.Length != PublicKeySizeInBytes)
                throw new ArgumentException(SR.Argument_PublicKeyWrongSizeForAlgorithm, nameof(source));

            ThrowIfNotSupported();
            return X25519DiffieHellmanImplementation.ImportPublicKeyImpl(source);
        }

        /// <summary>
        ///   Imports an X25519 Diffie-Hellman key from an X.509 SubjectPublicKeyInfo structure.
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
        ///     The SubjectPublicKeyInfo value does not represent an X25519 Diffie-Hellman key.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm-specific import failed.
        ///   </para>
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support X25519 Diffie-Hellman. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports X25519 Diffie-Hellman.
        /// </exception>
        public static X25519DiffieHellman ImportSubjectPublicKeyInfo(ReadOnlySpan<byte> source)
        {
            Helpers.ThrowIfAsnInvalidLength(source);
            ThrowIfNotSupported();

            KeyFormatHelper.ReadSubjectPublicKeyInfo(
                s_knownOids,
                source,
                SubjectPublicKeyReader,
                out int read,
                out X25519DiffieHellman key);

            Debug.Assert(read == source.Length);
            return key;

            static void SubjectPublicKeyReader(
                ReadOnlySpan<byte> key,
                in ValueAlgorithmIdentifierAsn identifier,
                out X25519DiffieHellman result)
            {
                if (identifier.HasParameters)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                if (key.Length != PublicKeySizeInBytes)
                {
                    throw new CryptographicException(SR.Argument_PublicKeyWrongSizeForAlgorithm);
                }

                result = X25519DiffieHellmanImplementation.ImportPublicKeyImpl(key);
            }
        }

        /// <inheritdoc cref="ImportSubjectPublicKeyInfo(ReadOnlySpan{byte})" />
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        public static X25519DiffieHellman ImportSubjectPublicKeyInfo(byte[] source)
        {
            ArgumentNullException.ThrowIfNull(source);
            return ImportSubjectPublicKeyInfo(new ReadOnlySpan<byte>(source));
        }

        /// <summary>
        ///   Imports an X25519 Diffie-Hellman private key from a PKCS#8 PrivateKeyInfo structure.
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
        ///     The PrivateKeyInfo value does not represent an X25519 Diffie-Hellman key.
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
        ///   The platform does not support X25519 Diffie-Hellman. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports X25519 Diffie-Hellman.
        /// </exception>
        public static X25519DiffieHellman ImportPkcs8PrivateKey(ReadOnlySpan<byte> source)
        {
            Helpers.ThrowIfAsnInvalidLength(source);
            ThrowIfNotSupported();

            KeyFormatHelper.ReadPkcs8(s_knownOids, source, Pkcs8KeyReader, out int read, out X25519DiffieHellman key);
            Debug.Assert(read == source.Length);
            return key;
        }

        /// <inheritdoc cref="ImportPkcs8PrivateKey(ReadOnlySpan{byte})" />
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        public static X25519DiffieHellman ImportPkcs8PrivateKey(byte[] source)
        {
            ArgumentNullException.ThrowIfNull(source);
            return ImportPkcs8PrivateKey(new ReadOnlySpan<byte>(source));
        }

        /// <summary>
        ///   Imports an X25519 Diffie-Hellman private key from a PKCS#8 EncryptedPrivateKeyInfo structure.
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
        ///     The value does not represent an X25519 Diffie-Hellman key.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm-specific import failed.
        ///   </para>
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support X25519 Diffie-Hellman. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports X25519 Diffie-Hellman.
        /// </exception>
        public static X25519DiffieHellman ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, ReadOnlySpan<byte> source)
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
        ///   Imports an X25519 Diffie-Hellman private key from a PKCS#8 EncryptedPrivateKeyInfo structure.
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
        ///     The value does not represent an X25519 Diffie-Hellman key.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm-specific import failed.
        ///   </para>
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support X25519 Diffie-Hellman. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports X25519 Diffie-Hellman.
        /// </exception>
        public static X25519DiffieHellman ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, ReadOnlySpan<byte> source)
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
        ///   Imports an X25519 Diffie-Hellman private key from a PKCS#8 EncryptedPrivateKeyInfo structure.
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
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="password" /> or <paramref name="source" /> is <see langword="null" />.
        /// </exception>
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
        ///     The value does not represent an X25519 Diffie-Hellman key.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm-specific import failed.
        ///   </para>
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support X25519 Diffie-Hellman. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports X25519 Diffie-Hellman.
        /// </exception>
        public static X25519DiffieHellman ImportEncryptedPkcs8PrivateKey(string password, byte[] source)
        {
            ArgumentNullException.ThrowIfNull(password);
            ArgumentNullException.ThrowIfNull(source);
            Helpers.ThrowIfAsnInvalidLength(source);
            ThrowIfNotSupported();

            return KeyFormatHelper.DecryptPkcs8(
                password,
                source,
                ImportPkcs8PrivateKey,
                out _);
        }

        /// <summary>
        ///   Imports an X25519 Diffie-Hellman key from an RFC 7468 PEM-encoded string.
        /// </summary>
        /// <param name="source">
        ///   The text of the PEM key to import.
        /// </param>
        /// <returns>
        ///   The imported key.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <para><paramref name="source" /> contains an encrypted PEM-encoded key.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="source" /> contains multiple PEM-encoded X25519 Diffie-Hellman keys.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="source" /> contains no PEM-encoded X25519 Diffie-Hellman keys.</para>
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while importing the key.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support X25519 Diffie-Hellman. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports X25519 Diffie-Hellman.
        /// </exception>
        /// <remarks>
        ///   <para>
        ///   Unsupported or malformed PEM-encoded objects will be ignored. If multiple supported PEM labels
        ///   are found, an exception is raised to prevent importing a key when the key is ambiguous.
        ///   </para>
        ///   <para>
        ///   This method supports the following PEM labels:
        ///   <list type="bullet">
        ///     <item><description>PUBLIC KEY</description></item>
        ///     <item><description>PRIVATE KEY</description></item>
        ///   </list>
        ///   </para>
        /// </remarks>
        public static X25519DiffieHellman ImportFromPem(ReadOnlySpan<char> source)
        {
            ThrowIfNotSupported();

            return PemKeyHelpers.ImportFactoryPem<X25519DiffieHellman>(source, label =>
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
        public static X25519DiffieHellman ImportFromPem(string source)
        {
            ArgumentNullException.ThrowIfNull(source);
            return ImportFromPem(source.AsSpan());
        }

        /// <summary>
        ///   Imports an X25519 Diffie-Hellman key from an encrypted RFC 7468 PEM-encoded string.
        /// </summary>
        /// <param name="source">
        ///   The PEM text of the encrypted key to import.
        /// </param>
        /// <param name="password">
        ///   The password to use for decrypting the key material.
        /// </param>
        /// <returns>
        ///   The imported key.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <para><paramref name="source"/> does not contain a PEM-encoded key with a recognized label.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="source"/> contains multiple PEM-encoded keys with a recognized label.</para>
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>The password is incorrect.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while importing the key.</para>
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support X25519 Diffie-Hellman. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports X25519 Diffie-Hellman.
        /// </exception>
        /// <remarks>
        ///   <para>
        ///     When the base-64 decoded contents of <paramref name="source" /> indicate an algorithm that uses PBKDF1
        ///     (Password-Based Key Derivation Function 1) or PBKDF2 (Password-Based Key Derivation Function 2),
        ///     the password is converted to bytes via the UTF-8 encoding.
        ///   </para>
        ///   <para>
        ///     Unsupported or malformed PEM-encoded objects will be ignored. If multiple supported PEM labels
        ///     are found, an exception is thrown to prevent importing a key when the key is ambiguous.
        ///   </para>
        ///   <para>This method supports the <c>ENCRYPTED PRIVATE KEY</c> PEM label.</para>
        /// </remarks>
        public static X25519DiffieHellman ImportFromEncryptedPem(ReadOnlySpan<char> source, ReadOnlySpan<char> password)
        {
            return PemKeyHelpers.ImportEncryptedFactoryPem<X25519DiffieHellman, char>(
                source,
                password,
                ImportEncryptedPkcs8PrivateKey);
        }

        /// <summary>
        ///   Imports an X25519 Diffie-Hellman key from an encrypted RFC 7468 PEM-encoded string.
        /// </summary>
        /// <param name="source">
        ///   The PEM text of the encrypted key to import.
        /// </param>
        /// <param name="passwordBytes">
        ///   The password to use for decrypting the key material.
        /// </param>
        /// <returns>
        ///   The imported key.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <para><paramref name="source"/> does not contain a PEM-encoded key with a recognized label.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="source"/> contains multiple PEM-encoded keys with a recognized label.</para>
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>The password is incorrect.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while importing the key.</para>
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The platform does not support X25519 Diffie-Hellman. Callers can use the <see cref="IsSupported" /> property
        ///   to determine if the platform supports X25519 Diffie-Hellman.
        /// </exception>
        /// <remarks>
        ///   <para>
        ///     Unsupported or malformed PEM-encoded objects will be ignored. If multiple supported PEM labels
        ///     are found, an exception is thrown to prevent importing a key when the key is ambiguous.
        ///   </para>
        ///   <para>This method supports the <c>ENCRYPTED PRIVATE KEY</c> PEM label.</para>
        /// </remarks>
        public static X25519DiffieHellman ImportFromEncryptedPem(ReadOnlySpan<char> source, ReadOnlySpan<byte> passwordBytes)
        {
            return PemKeyHelpers.ImportEncryptedFactoryPem<X25519DiffieHellman, byte>(
                source,
                passwordBytes,
                ImportEncryptedPkcs8PrivateKey);
        }

        /// <inheritdoc cref="ImportFromEncryptedPem(ReadOnlySpan{char}, ReadOnlySpan{char})" />
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> or <paramref name="password" /> is <see langword="null" />.
        /// </exception>
        public static X25519DiffieHellman ImportFromEncryptedPem(string source, string password)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(password);
            return ImportFromEncryptedPem(source.AsSpan(), password.AsSpan());
        }

        /// <inheritdoc cref="ImportFromEncryptedPem(ReadOnlySpan{char}, ReadOnlySpan{byte})" />
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="source" /> or <paramref name="passwordBytes" /> is <see langword="null" />.
        /// </exception>
        public static X25519DiffieHellman ImportFromEncryptedPem(string source, byte[] passwordBytes)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(passwordBytes);
            return ImportFromEncryptedPem(source.AsSpan(), new ReadOnlySpan<byte>(passwordBytes));
        }

        /// <summary>
        ///   Releases all resources used by the <see cref="X25519DiffieHellman"/> class.
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
        ///   Called by the <c>Dispose()</c> and <c>Finalize()</c> methods to release the managed and unmanaged
        ///   resources used by the current instance of the <see cref="X25519DiffieHellman"/> class.
        /// </summary>
        /// <param name="disposing">
        ///   <see langword="true" /> to release managed and unmanaged resources;
        ///   <see langword="false" /> to release only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
        }

        private bool TryExportSubjectPublicKeyInfoCore(Span<byte> destination, out int bytesWritten)
        {
            // Pre-encoded SubjectPublicKeyInfo for X25519 (RFC 8410):
            ReadOnlySpan<byte> spkiPreamble =
            [
                0x30, 0x2a, // SEQUENCE (42 bytes)
                0x30, 0x05, 0x06, 0x03, 0x2b, 0x65, 0x6e, // SEQUENCE { OID 1.3.101.110 }
                0x03, 0x21, 0x00, // BIT STRING (33 bytes, 0 unused bits)
            ];

            Debug.Assert(spkiPreamble.Length + PublicKeySizeInBytes == SpkiSizeInBytes);

            if (destination.Length < SpkiSizeInBytes)
            {
                bytesWritten = 0;
                return false;
            }

            spkiPreamble.CopyTo(destination);
            ExportPublicKeyCore(destination.Slice(spkiPreamble.Length, PublicKeySizeInBytes));
            bytesWritten = SpkiSizeInBytes;
            return true;
        }

        private TResult ExportPkcs8PrivateKeyCallback<TResult>(Func<ReadOnlySpan<byte>, TResult> func)
        {
            // A PKCS#8 X25519 PrivateKeyInfo has an ASN.1 overhead of 16 bytes, assuming no attributes.
            // Make it an even 32 and that should give a good starting point for a buffer size.
            int size = PrivateKeySizeInBytes + 32;
            byte[] buffer = CryptoPool.Rent(size);
            int written;

            while (!TryExportPkcs8PrivateKeyCore(buffer, out written))
            {
                CryptoPool.Return(buffer);
                size = checked(size * 2);
                buffer = CryptoPool.Rent(size);
            }

            if (written < 0 || written > buffer.Length)
            {
                CryptographicOperations.ZeroMemory(buffer);
                throw new CryptographicException();
            }

            TResult result = func(buffer.AsSpan(0, written));
            CryptoPool.Return(buffer, written);
            return result;
        }

        private protected bool TryExportPkcs8PrivateKeyImpl(Span<byte> destination, out int bytesWritten)
        {
            // Pre-encoded PKCS#8 PrivateKeyInfo for X25519 (RFC 8410):
            ReadOnlySpan<byte> pkcs8Preamble =
            [
                0x30, 0x2e,                         // SEQUENCE (46 bytes)
                0x02, 0x01, 0x00,                   // INTEGER 0
                0x30, 0x05, 0x06, 0x03, 0x2b, 0x65, 0x6e, // SEQUENCE { OID 1.3.101.110 }
                0x04, 0x22,                         // OCTET STRING (34 bytes)
                0x04, 0x20,                         // OCTET STRING (32 bytes)
            ];

            int pkcs8SizeInBytes = pkcs8Preamble.Length + PrivateKeySizeInBytes;

            if (destination.Length < pkcs8SizeInBytes)
            {
                bytesWritten = 0;
                return false;
            }

            pkcs8Preamble.CopyTo(destination);
            Span<byte> privateKeyBuffer = destination.Slice(pkcs8Preamble.Length, PrivateKeySizeInBytes);

            try
            {
                ExportPrivateKey(privateKeyBuffer);
                bytesWritten = pkcs8SizeInBytes;
                return true;
            }
            catch
            {
                CryptographicOperations.ZeroMemory(privateKeyBuffer);
                throw;
            }
        }

        private AsnWriter ExportEncryptedPkcs8PrivateKeyCore<TChar>(
            ReadOnlySpan<TChar> password,
            PbeParameters pbeParameters,
            Func<ReadOnlySpan<TChar>, AsnWriter, PbeParameters, AsnWriter> encryptor)
        {
            // A PKCS#8 X25519 PrivateKeyInfo has an ASN.1 overhead of 16 bytes, assuming no attributes.
            // Make it an even 32 and that should give a good starting point for a buffer size.
            int initialSize = PrivateKeySizeInBytes + 32;
            byte[] rented = CryptoPool.Rent(initialSize);
            int written;

            while (!TryExportPkcs8PrivateKey(rented, out written))
            {
                CryptoPool.Return(rented, 0);
                rented = CryptoPool.Rent(rented.Length * 2);
            }

            AsnWriter tmp = new(AsnEncodingRules.BER, initialCapacity: written);

            try
            {
                tmp.WriteEncodedValueForCrypto(rented.AsSpan(0, written));
                return encryptor(password, tmp, pbeParameters);
            }
            finally
            {
                tmp.Reset();
                CryptoPool.Return(rented, written);
            }
        }

        private static void Pkcs8KeyReader(
            ReadOnlySpan<byte> privateKeyContents,
            in ValueAlgorithmIdentifierAsn algorithmIdentifier,
            out X25519DiffieHellman key)
        {
            if (algorithmIdentifier.HasParameters)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            ValueAsnReader reader = new(privateKeyContents, AsnEncodingRules.BER);
            ReadOnlySpan<byte> privateKey = reader.ReadOctetString();
            reader.ThrowIfNotEmpty();

            if (privateKey.Length != PrivateKeySizeInBytes)
            {
                throw new CryptographicException(SR.Argument_PrivateKeyWrongSizeForAlgorithm);
            }

            key = X25519DiffieHellmanImplementation.ImportPrivateKeyImpl(privateKey);
        }

        private protected void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, typeof(X25519DiffieHellman));
        }

        private protected static void ThrowIfNotSupported()
        {
            if (!IsSupported)
            {
                throw new PlatformNotSupportedException(
                    SR.Format(SR.Cryptography_AlgorithmNotSupported,
                    nameof(X25519DiffieHellman)));
            }
        }
    }
}
