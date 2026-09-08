// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography
{
    /// <summary>
    ///   Represents a Hybrid Public Key Encryption (HPKE) key.
    /// </summary>
    [Experimental(Experimentals.HpkeExperimentalDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    public abstract class Hpke : IDisposable
    {
        private bool _disposed;

        /// <summary>
        ///   Gets the cipher suite associated with this key.
        /// </summary>
        /// <value>
        ///   The cipher suite associated with this key.
        /// </value>
        public HpkeSuite Suite { get; }

        /// <summary>
        ///   Initializes a new instance of the <see cref="Hpke" /> class with the specified cipher suite.
        /// </summary>
        /// <param name="suite">
        ///   The cipher suite associated with this key.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="suite" /> is <see langword="null" />.
        /// </exception>
        protected Hpke(HpkeSuite suite)
        {
            ArgumentNullException.ThrowIfNull(suite);
            Suite = suite;
        }

        /// <summary>
        ///   Determines whether the specified cipher suite is supported on the current platform.
        /// </summary>
        /// <param name="suite">
        ///   The cipher suite to check.
        /// </param>
        /// <returns>
        ///   <see langword="true" /> if the cipher suite is supported; otherwise, <see langword="false" />.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="suite" /> is <see langword="null" />.
        /// </exception>
        public static bool IsSupported(HpkeSuite suite)
        {
            ArgumentNullException.ThrowIfNull(suite);
            return HpkeImplementation.IsSupportedImpl(suite);
        }

        /// <summary>
        ///   Derives an HPKE key for the specified cipher suite from input keying material.
        /// </summary>
        /// <param name="suite">
        ///   The cipher suite for the derived key.
        /// </param>
        /// <param name="ikm">
        ///   The input keying material from which to derive the key.
        /// </param>
        /// <returns>
        ///   The derived HPKE key.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="suite" /> or <paramref name="ikm" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   <paramref name="suite" /> is not supported on the current platform.
        /// </exception>
        public static Hpke DeriveKey(HpkeSuite suite, byte[] ikm)
        {
            ArgumentNullException.ThrowIfNull(ikm);
            return DeriveKey(suite, new ReadOnlySpan<byte>(ikm));
        }

        /// <summary>
        ///   Derives an HPKE key for the specified cipher suite from input keying material.
        /// </summary>
        /// <param name="suite">
        ///   The cipher suite for the derived key.
        /// </param>
        /// <param name="ikm">
        ///   The input keying material from which to derive the key.
        /// </param>
        /// <returns>
        ///   The derived HPKE key.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="suite" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   <paramref name="suite" /> is not supported on the current platform.
        /// </exception>
        public static Hpke DeriveKey(HpkeSuite suite, ReadOnlySpan<byte> ikm)
        {
            ArgumentNullException.ThrowIfNull(suite);
            ThrowIfNotSupported(suite);
            return HpkeImplementation.DeriveKeyImpl(suite, ikm);
        }

        /// <summary>
        ///   Generates a new HPKE key for the specified cipher suite.
        /// </summary>
        /// <param name="suite">
        ///   The cipher suite for the new key.
        /// </param>
        /// <returns>
        ///   A new HPKE key.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="suite" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   <paramref name="suite" /> is not supported on the current platform.
        /// </exception>
        public static Hpke GenerateKey(HpkeSuite suite)
        {
            ArgumentNullException.ThrowIfNull(suite);
            ThrowIfNotSupported(suite);
            return HpkeImplementation.GenerateKeyImpl(suite);
        }

        /// <summary>
        ///   Exports the decapsulation key.
        /// </summary>
        /// <returns>
        ///   A new byte array containing the serialized decapsulation key, with a length of
        ///   <see cref="HpkeSuite.DecapsulationKeySizeInBytes" /> bytes.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   The current instance does not contain a decapsulation key, or an error occurred while exporting the key.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The object has already been disposed.
        /// </exception>
        /// <remarks>
        ///   <para>
        ///     The key is exported in the private-key format defined by the cipher suite's KEM,
        ///     without a PKCS#8 or other ASN.1 wrapper. For DHKEM with NIST curves, this is the fixed-width,
        ///     big-endian private scalar. For DHKEM with X25519, this is the raw 32-byte X25519 private key.
        ///     For ML-KEM and hybrid ML-KEM cipher suites, this is the private seed.
        ///   </para>
        ///   <para>
        ///     The returned key is not the original input keying material supplied to
        ///     <see cref="DeriveKey(HpkeSuite, ReadOnlySpan{byte})" />.
        ///     The caller is responsible for protecting the returned secret bytes and clearing them when no longer needed.
        ///   </para>
        /// </remarks>
        public byte[] ExportDecapsulationKey()
        {
            ThrowIfDisposed();
            byte[] key = new byte[Suite.DecapsulationKeySizeInBytes];

            try
            {
                ExportDecapsulationKeyCore(key);
                return key;
            }
            catch
            {
                CryptographicOperations.ZeroMemory(key);
                throw;
            }
        }

        /// <summary>
        ///   Exports the decapsulation key into the provided buffer.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the serialized decapsulation key.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="destination" /> is not exactly
        ///   <see cref="HpkeSuite.DecapsulationKeySizeInBytes" /> bytes long.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   The current instance does not contain a decapsulation key, or an error occurred while exporting the key.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The object has already been disposed.
        /// </exception>
        /// <remarks>
        ///   The key format is the same as for <see cref="ExportDecapsulationKey()" />.
        ///   On success, the entire destination is filled with the serialized key.
        ///   The caller is responsible for protecting the secret bytes and clearing the buffer when no longer needed.
        /// </remarks>
        public void ExportDecapsulationKey(Span<byte> destination)
        {
            if (destination.Length != Suite.DecapsulationKeySizeInBytes)
            {
                throw new ArgumentException(
                    SR.Format(SR.Argument_DestinationImprecise, Suite.DecapsulationKeySizeInBytes),
                    nameof(destination));
            }

            ThrowIfDisposed();
            ExportDecapsulationKeyCore(destination);
        }

        /// <summary>
        ///   When overridden in a derived class, exports the decapsulation key into the provided buffer.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the decapsulation key.
        /// </param>
        /// <exception cref="CryptographicException">
        ///   The current instance does not contain a decapsulation key, or an error occurred while exporting the key.
        /// </exception>
        /// <remarks>
        ///   The calling method has verified that this instance is not disposed and that
        ///   <paramref name="destination" /> is exactly
        ///   <see cref="HpkeSuite.DecapsulationKeySizeInBytes" /> bytes long.
        ///   Implementations must fill the entire destination using the key format described by
        ///   <see cref="ExportDecapsulationKey()" /> and throw <see cref="CryptographicException" />
        ///   if the decapsulation key cannot be exported.
        /// </remarks>
        protected abstract void ExportDecapsulationKeyCore(Span<byte> destination);

        /// <summary>
        ///   Exports the encapsulation key.
        /// </summary>
        /// <returns>
        ///   The encapsulation key.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   The current instance does not contain an encapsulation key, or an error occurred while exporting the key.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The object has already been disposed.
        /// </exception>
        public byte[] ExportEncapsulationKey()
        {
            ThrowIfDisposed();
            byte[] key = new byte[Suite.EncapsulationKeySizeInBytes];
            ExportEncapsulationKeyCore(key);
            return key;
        }

        /// <summary>
        ///   Exports the encapsulation key into the provided buffer.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the encapsulation key.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="destination" /> is not exactly
        ///   <see cref="HpkeSuite.EncapsulationKeySizeInBytes" /> bytes long.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   The current instance does not contain an encapsulation key, or an error occurred while exporting the key.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The object has already been disposed.
        /// </exception>
        public void ExportEncapsulationKey(Span<byte> destination)
        {
            if (destination.Length != Suite.EncapsulationKeySizeInBytes)
            {
                throw new ArgumentException(
                    SR.Format(SR.Argument_DestinationImprecise, Suite.EncapsulationKeySizeInBytes),
                    nameof(destination));
            }

            ThrowIfDisposed();
            ExportEncapsulationKeyCore(destination);
        }

        /// <summary>
        ///   When overridden in a derived class, exports the encapsulation key into the provided buffer.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the encapsulation key.
        /// </param>
        /// <exception cref="CryptographicException">
        ///   The current instance does not contain an encapsulation key, or an error occurred while exporting the key.
        /// </exception>
        /// <remarks>
        ///   <paramref name="destination" /> is exactly
        ///   <see cref="HpkeSuite.EncapsulationKeySizeInBytes" /> bytes long.
        /// </remarks>
        protected abstract void ExportEncapsulationKeyCore(Span<byte> destination);

        public void Seal(
            ReadOnlySpan<byte> plaintext,
            out byte[] encapsulatedSecret,
            out byte[] ciphertext,
            ReadOnlySpan<byte> associatedData = default,
            ReadOnlySpan<byte> info = default)
        {
            ThrowIfInfoExceedsLimit(info);
            ThrowIfDisposed();

            byte[] ciphertextBuffer = new byte[Suite.GetCiphertextLength(plaintext.Length)];
            byte[] encapsulatedSecretBuffer = new byte[Suite.EncapsulatedSecretSizeInBytes];

            SealCore(plaintext, encapsulatedSecretBuffer, ciphertextBuffer, associatedData, info);

            encapsulatedSecret = encapsulatedSecretBuffer;
            ciphertext = ciphertextBuffer;
        }

        public void Seal(
            byte[] plaintext,
            out byte[] encapsulatedSecret,
            out byte[] ciphertext,
            byte[]? associatedData = null,
            byte[]? info = null)
        {
            ArgumentNullException.ThrowIfNull(plaintext);
            ThrowIfInfoExceedsLimit(info);
            ThrowIfDisposed();

            byte[] ciphertextBuffer = new byte[Suite.GetCiphertextLength(plaintext.Length)];
            byte[] encapsulatedSecretBuffer = new byte[Suite.EncapsulatedSecretSizeInBytes];

            // associatedData and info null's implicity convert to empty span.
            SealCore(plaintext, encapsulatedSecretBuffer, ciphertextBuffer, associatedData, info);

            encapsulatedSecret = encapsulatedSecretBuffer;
            ciphertext = ciphertextBuffer;
        }

        public void Seal(
            ReadOnlySpan<byte> plaintext,
            Span<byte> encapsulatedSecret,
            Span<byte> ciphertext,
            ReadOnlySpan<byte> associatedData = default,
            ReadOnlySpan<byte> info = default)
        {
            ThrowIfInfoExceedsLimit(info);
            ThrowIfDisposed();

            if (encapsulatedSecret.Length != Suite.EncapsulatedSecretSizeInBytes)
            {
                throw new ArgumentException(
                    SR.Format(SR.Argument_DestinationImprecise, Suite.EncapsulatedSecretSizeInBytes),
                    nameof(encapsulatedSecret));
            }

            int expectedCiphertextLength = Suite.GetCiphertextLength(plaintext.Length);

            if (ciphertext.Length != expectedCiphertextLength)
            {
                throw new ArgumentException(
                    SR.Format(SR.Argument_DestinationImprecise, expectedCiphertextLength),
                    nameof(ciphertext));
            }

            SealCore(plaintext, encapsulatedSecret, ciphertext, associatedData, info);
        }

        protected abstract void SealCore(
            ReadOnlySpan<byte> plaintext,
            Span<byte> encapsulatedSecret,
            Span<byte> ciphertext,
            ReadOnlySpan<byte> associatedData,
            ReadOnlySpan<byte> info);

        /// <summary>
        ///   Decrypts and authenticates a single HPKE ciphertext using Base mode.
        /// </summary>
        /// <param name="encapsulatedSecret">
        ///   The encapsulated secret produced by the sender.
        /// </param>
        /// <param name="ciphertext">
        ///   The ciphertext, including its trailing authentication tag.
        /// </param>
        /// <param name="associatedData">
        ///   The additional authenticated data, which must match the value used by the sender.
        /// </param>
        /// <param name="info">
        ///   The application context, which must match the value used by the sender.
        /// </param>
        /// <returns>
        ///   A new byte array containing the authenticated plaintext.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///     <paramref name="encapsulatedSecret" /> is not exactly
        ///     <see cref="HpkeSuite.EncapsulatedSecretSizeInBytes" /> bytes long.
        ///   </para>
        ///   <para> -or- </para>
        ///   <para>
        ///     <paramref name="ciphertext" /> is shorter than <see cref="HpkeSuite.AeadTagSizeInBytes" /> bytes.
        ///   </para>
        ///   <para> -or- </para>
        ///   <para>
        ///     <paramref name="info" /> exceeds the maximum length supported by the cipher suite's KDF.
        ///   </para>
        /// </exception>
        /// <exception cref="AuthenticationTagMismatchException">
        ///   The authentication tag could not be verified.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   The current instance does not contain a decapsulation key, the encapsulated secret is invalid,
        ///   or an error occurred during decryption.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The object has already been disposed.
        /// </exception>
        public byte[] Open(
            ReadOnlySpan<byte> encapsulatedSecret,
            ReadOnlySpan<byte> ciphertext,
            ReadOnlySpan<byte> associatedData = default,
            ReadOnlySpan<byte> info = default)
        {
            int plaintextLength = ValidateOpenInputs(encapsulatedSecret, ciphertext, info);
            byte[] plaintext = new byte[plaintextLength];

            try
            {
                OpenCore(encapsulatedSecret, ciphertext, plaintext, associatedData, info);
                return plaintext;
            }
            catch
            {
                CryptographicOperations.ZeroMemory(plaintext);
                throw;
            }
        }

        /// <summary>
        ///   Decrypts and authenticates a single HPKE ciphertext using Base mode.
        /// </summary>
        /// <param name="encapsulatedSecret">
        ///   The encapsulated secret produced by the sender.
        /// </param>
        /// <param name="ciphertext">
        ///   The ciphertext, including its trailing authentication tag.
        /// </param>
        /// <param name="associatedData">
        ///   The additional authenticated data, which must match the value used by the sender,
        ///   or <see langword="null" /> to use no additional authenticated data.
        /// </param>
        /// <param name="info">
        ///   The application context, which must match the value used by the sender,
        ///   or <see langword="null" /> to use an empty context.
        /// </param>
        /// <returns>
        ///   A new byte array containing the authenticated plaintext.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="encapsulatedSecret" /> or <paramref name="ciphertext" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///     <paramref name="encapsulatedSecret" /> is not exactly
        ///     <see cref="HpkeSuite.EncapsulatedSecretSizeInBytes" /> bytes long.
        ///   </para>
        ///   <para> -or- </para>
        ///   <para>
        ///     <paramref name="ciphertext" /> is shorter than <see cref="HpkeSuite.AeadTagSizeInBytes" /> bytes.
        ///   </para>
        ///   <para> -or- </para>
        ///   <para>
        ///     <paramref name="info" /> exceeds the maximum length supported by the cipher suite's KDF.
        ///   </para>
        /// </exception>
        /// <exception cref="AuthenticationTagMismatchException">
        ///   The authentication tag could not be verified.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   The current instance does not contain a decapsulation key, the encapsulated secret is invalid,
        ///   or an error occurred during decryption.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The object has already been disposed.
        /// </exception>
        public byte[] Open(
            byte[] encapsulatedSecret,
            byte[] ciphertext,
            byte[]? associatedData = null,
            byte[]? info = null)
        {
            ArgumentNullException.ThrowIfNull(encapsulatedSecret);
            ArgumentNullException.ThrowIfNull(ciphertext);
            return Open(
                new ReadOnlySpan<byte>(encapsulatedSecret),
                ciphertext,
                new ReadOnlySpan<byte>(associatedData),
                info);
        }

        /// <summary>
        ///   Decrypts and authenticates a single HPKE ciphertext into the provided buffer using Base mode.
        /// </summary>
        /// <param name="encapsulatedSecret">
        ///   The encapsulated secret produced by the sender.
        /// </param>
        /// <param name="ciphertext">
        ///   The ciphertext, including its trailing authentication tag.
        /// </param>
        /// <param name="plaintext">
        ///   The buffer to receive the authenticated plaintext.
        /// </param>
        /// <param name="associatedData">
        ///   The additional authenticated data, which must match the value used by the sender.
        /// </param>
        /// <param name="info">
        ///   The application context, which must match the value used by the sender.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///     <paramref name="encapsulatedSecret" /> is not exactly
        ///     <see cref="HpkeSuite.EncapsulatedSecretSizeInBytes" /> bytes long.
        ///   </para>
        ///   <para> -or- </para>
        ///   <para>
        ///     <paramref name="ciphertext" /> is shorter than <see cref="HpkeSuite.AeadTagSizeInBytes" /> bytes.
        ///   </para>
        ///   <para> -or- </para>
        ///   <para>
        ///     The length of <paramref name="plaintext" /> is not exactly the length of
        ///     <paramref name="ciphertext" /> minus <see cref="HpkeSuite.AeadTagSizeInBytes" />.
        ///   </para>
        ///   <para> -or- </para>
        ///   <para>
        ///     <paramref name="info" /> exceeds the maximum length supported by the cipher suite's KDF.
        ///   </para>
        /// </exception>
        /// <exception cref="AuthenticationTagMismatchException">
        ///   The authentication tag could not be verified.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   The current instance does not contain a decapsulation key, the encapsulated secret is invalid,
        ///   or an error occurred during decryption.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The object has already been disposed.
        /// </exception>
        public void Open(
            ReadOnlySpan<byte> encapsulatedSecret,
            ReadOnlySpan<byte> ciphertext,
            Span<byte> plaintext,
            ReadOnlySpan<byte> associatedData = default,
            ReadOnlySpan<byte> info = default)
        {
            int plaintextLength = ValidateOpenInputs(encapsulatedSecret, ciphertext, info);

            if (plaintext.Length != plaintextLength)
            {
                throw new ArgumentException(
                    SR.Format(SR.Argument_DestinationImprecise, plaintextLength),
                    nameof(plaintext));
            }

            OpenCore(encapsulatedSecret, ciphertext, plaintext, associatedData, info);
        }

        /// <summary>
        ///   When overridden in a derived class, decrypts and authenticates a single HPKE ciphertext using Base mode.
        /// </summary>
        /// <param name="encapsulatedSecret">
        ///   The encapsulated secret produced by the sender.
        /// </param>
        /// <param name="ciphertext">
        ///   The ciphertext, including its trailing authentication tag.
        /// </param>
        /// <param name="plaintext">
        ///   The buffer to receive the authenticated plaintext.
        /// </param>
        /// <param name="associatedData">
        ///   The additional authenticated data.
        /// </param>
        /// <param name="info">
        ///   The application context.
        /// </param>
        /// <exception cref="AuthenticationTagMismatchException">
        ///   The authentication tag could not be verified.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   The current instance does not contain a decapsulation key, the encapsulated secret is invalid,
        ///   or an error occurred during decryption.
        /// </exception>
        /// <remarks>
        ///   The calling method has verified that this instance is not disposed, the input and output lengths
        ///   are valid for <see cref="Suite" />, and <paramref name="info" /> satisfies the KDF's length limit.
        ///   Implementations must fill the entire plaintext buffer on success and must not leave
        ///   unauthenticated plaintext in the buffer when authentication fails.
        /// </remarks>
        protected abstract void OpenCore(
            ReadOnlySpan<byte> encapsulatedSecret,
            ReadOnlySpan<byte> ciphertext,
            Span<byte> plaintext,
            ReadOnlySpan<byte> associatedData,
            ReadOnlySpan<byte> info);

        /// <summary>
        ///   Creates an HPKE sender context using Base mode.
        /// </summary>
        /// <param name="encapsulatedSecret">
        ///   When this method returns, contains the encapsulated secret to send to the recipient.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <param name="info">
        ///   The application context, which must match the value used by the recipient.
        /// </param>
        /// <returns>
        ///   A new sender context for this key's cipher suite.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="info" /> exceeds the maximum length supported by the cipher suite's KDF.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   The current instance does not contain an encapsulation key, or an error occurred while creating the sender.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   Creating a sender is not supported on the current platform.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The object has already been disposed.
        /// </exception>
        public HpkeSender CreateSender(out byte[] encapsulatedSecret, ReadOnlySpan<byte> info = default)
        {
            ThrowIfInfoExceedsLimit(info);
            ThrowIfDisposed();

            byte[] encapsulatedSecretBuffer = new byte[Suite.EncapsulatedSecretSizeInBytes];
            HpkeSender sender = CreateSenderCore(encapsulatedSecretBuffer, info);
            encapsulatedSecret = encapsulatedSecretBuffer;
            return sender;
        }

        /// <summary>
        ///   Creates an HPKE sender context using Base mode and writes the encapsulated secret into the provided buffer.
        /// </summary>
        /// <param name="encapsulatedSecret">
        ///   The buffer to receive the encapsulated secret to send to the recipient.
        /// </param>
        /// <param name="info">
        ///   The application context, which must match the value used by the recipient.
        /// </param>
        /// <returns>
        ///   A new sender context for this key's cipher suite.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///     <paramref name="encapsulatedSecret" /> is not exactly
        ///     <see cref="HpkeSuite.EncapsulatedSecretSizeInBytes" /> bytes long.
        ///   </para>
        ///   <para> -or- </para>
        ///   <para>
        ///     <paramref name="info" /> exceeds the maximum length supported by the cipher suite's KDF.
        ///   </para>
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   The current instance does not contain an encapsulation key, or an error occurred while creating the sender.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   Creating a sender is not supported on the current platform.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The object has already been disposed.
        /// </exception>
        public HpkeSender CreateSender(Span<byte> encapsulatedSecret, ReadOnlySpan<byte> info = default)
        {
            ThrowIfInfoExceedsLimit(info);
            ThrowIfDisposed();

            if (encapsulatedSecret.Length != Suite.EncapsulatedSecretSizeInBytes)
            {
                throw new ArgumentException(
                    SR.Format(SR.Argument_DestinationImprecise, Suite.EncapsulatedSecretSizeInBytes),
                    nameof(encapsulatedSecret));
            }

            return CreateSenderCore(encapsulatedSecret, info);
        }

        /// <summary>
        ///   When overridden in a derived class, creates an HPKE sender context using Base mode.
        /// </summary>
        /// <param name="encapsulatedSecret">
        ///   The buffer to receive the encapsulated secret.
        /// </param>
        /// <param name="info">
        ///   The application context.
        /// </param>
        /// <returns>
        ///   A new sender context for this key's cipher suite.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   The current instance does not contain an encapsulation key, or an error occurred while creating the sender.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   Creating a sender is not supported on the current platform.
        /// </exception>
        /// <remarks>
        ///   The calling method has verified that this instance is not disposed, the encapsulated secret buffer
        ///   has the exact required length, and <paramref name="info" /> satisfies the KDF's length limit.
        ///   Implementations must fill the entire buffer and return an initialized sender for <see cref="Suite" />.
        /// </remarks>
        protected abstract HpkeSender CreateSenderCore(Span<byte> encapsulatedSecret, ReadOnlySpan<byte> info);

        /// <summary>
        ///   Creates an HPKE recipient context using Base mode.
        /// </summary>
        /// <param name="encapsulatedSecret">
        ///   The encapsulated secret produced by the sender.
        /// </param>
        /// <param name="info">
        ///   The application context, which must match the value used by the sender.
        /// </param>
        /// <returns>
        ///   A new recipient context for this key's cipher suite.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///     <paramref name="encapsulatedSecret" /> is not exactly
        ///     <see cref="HpkeSuite.EncapsulatedSecretSizeInBytes" /> bytes long.
        ///   </para>
        ///   <para> -or- </para>
        ///   <para>
        ///     <paramref name="info" /> exceeds the maximum length supported by the cipher suite's KDF.
        ///   </para>
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   The current instance does not contain a decapsulation key, the encapsulated secret is invalid,
        ///   or an error occurred while creating the recipient.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   Creating a recipient is not supported on the current platform.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The object has already been disposed.
        /// </exception>
        public HpkeRecipient CreateRecipient(
            ReadOnlySpan<byte> encapsulatedSecret,
            ReadOnlySpan<byte> info = default)
        {
            ThrowIfInfoExceedsLimit(info);
            ThrowIfDisposed();
            ThrowIfInvalidEncapsulatedSecretLength(encapsulatedSecret);
            return CreateRecipientCore(encapsulatedSecret, info);
        }

        /// <summary>
        ///   Creates an HPKE recipient context using Base mode.
        /// </summary>
        /// <param name="encapsulatedSecret">
        ///   The encapsulated secret produced by the sender.
        /// </param>
        /// <param name="info">
        ///   The application context, which must match the value used by the sender,
        ///   or <see langword="null" /> to use an empty context.
        /// </param>
        /// <returns>
        ///   A new recipient context for this key's cipher suite.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="encapsulatedSecret" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///     <paramref name="encapsulatedSecret" /> is not exactly
        ///     <see cref="HpkeSuite.EncapsulatedSecretSizeInBytes" /> bytes long.
        ///   </para>
        ///   <para> -or- </para>
        ///   <para>
        ///     <paramref name="info" /> exceeds the maximum length supported by the cipher suite's KDF.
        ///   </para>
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   The current instance does not contain a decapsulation key, the encapsulated secret is invalid,
        ///   or an error occurred while creating the recipient.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   Creating a recipient is not supported on the current platform.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The object has already been disposed.
        /// </exception>
        public HpkeRecipient CreateRecipient(byte[] encapsulatedSecret, byte[]? info = null)
        {
            ArgumentNullException.ThrowIfNull(encapsulatedSecret);
            return CreateRecipient(new ReadOnlySpan<byte>(encapsulatedSecret), info);
        }

        /// <summary>
        ///   When overridden in a derived class, creates an HPKE recipient context using Base mode.
        /// </summary>
        /// <param name="encapsulatedSecret">
        ///   The encapsulated secret produced by the sender.
        /// </param>
        /// <param name="info">
        ///   The application context.
        /// </param>
        /// <returns>
        ///   A new recipient context for this key's cipher suite.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   The current instance does not contain a decapsulation key, the encapsulated secret is invalid,
        ///   or an error occurred while creating the recipient.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   Creating a recipient is not supported on the current platform.
        /// </exception>
        /// <remarks>
        ///   The calling method has verified that this instance is not disposed, the encapsulated secret
        ///   has the exact required length, and <paramref name="info" /> satisfies the KDF's length limit.
        ///   Implementations must return an initialized recipient for <see cref="Suite" />.
        /// </remarks>
        protected abstract HpkeRecipient CreateRecipientCore(
            ReadOnlySpan<byte> encapsulatedSecret,
            ReadOnlySpan<byte> info);

        /// <summary>
        ///   Releases all resources used by the <see cref="Hpke" /> class.
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
        ///   resources used by the current instance of the <see cref="Hpke" /> class.
        /// </summary>
        /// <param name="disposing">
        ///   <see langword="true" /> to release managed and unmanaged resources;
        ///   <see langword="false" /> to release only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
        }

        private static void ThrowIfNotSupported(HpkeSuite suite)
        {
            if (!IsSupported(suite))
            {
                throw new PlatformNotSupportedException();
            }
        }

        private void ThrowIfInfoExceedsLimit(ReadOnlySpan<byte> info)
        {
            if (info.Length > Suite.KdfMetadata.MaximumInfoLength)
            {
                throw new ArgumentException(
                    SR.Format(SR.Argument_HpkeKdfInfoLength, Suite.KdfMetadata.MaximumInfoLength),
                    nameof(info));
            }
        }

        private void ThrowIfInvalidEncapsulatedSecretLength(ReadOnlySpan<byte> encapsulatedSecret)
        {
            if (encapsulatedSecret.Length != Suite.EncapsulatedSecretSizeInBytes)
            {
                throw new ArgumentException(
                    SR.Format(SR.Argument_HpkeEncapsulatedSecretLength, Suite.EncapsulatedSecretSizeInBytes),
                    nameof(encapsulatedSecret));
            }
        }

        private int ValidateOpenInputs(
            ReadOnlySpan<byte> encapsulatedSecret,
            ReadOnlySpan<byte> ciphertext,
            ReadOnlySpan<byte> info)
        {
            ThrowIfInfoExceedsLimit(info);
            ThrowIfDisposed();
            ThrowIfInvalidEncapsulatedSecretLength(encapsulatedSecret);

            int tagSize = Suite.AeadTagSizeInBytes;

            if (ciphertext.Length < tagSize)
            {
                throw new ArgumentException(
                    SR.Format(SR.Argument_HpkeCiphertextTooShort, tagSize),
                    nameof(ciphertext));
            }

            return ciphertext.Length - tagSize;
        }

        private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
