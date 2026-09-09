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
        ///   Imports an HPKE key pair from a serialized decapsulation key.
        /// </summary>
        /// <param name="suite">
        ///   The cipher suite associated with the key.
        /// </param>
        /// <param name="source">
        ///   The serialized decapsulation key.
        /// </param>
        /// <returns>
        ///   A new HPKE key containing the decapsulation key and its corresponding encapsulation key.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="suite" /> or <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="source" /> is not exactly <see cref="HpkeSuite.DecapsulationKeySizeInBytes" /> bytes long.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   The decapsulation key is invalid, or an error occurred while importing the key.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   <paramref name="suite" /> is not supported on the current platform.
        /// </exception>
        /// <remarks>
        ///   The key must use the format returned by <see cref="ExportDecapsulationKey()" />,
        ///   not the input keying material accepted by <see cref="DeriveKey(HpkeSuite, byte[])" />.
        ///   The imported key does not retain a reference to <paramref name="source" />.
        ///   The caller remains responsible for protecting and clearing the source key bytes.
        /// </remarks>
        public static Hpke ImportDecapsulationKey(HpkeSuite suite, byte[] source)
        {
            ArgumentNullException.ThrowIfNull(source);
            return ImportDecapsulationKey(suite, new ReadOnlySpan<byte>(source));
        }

        /// <summary>
        ///   Imports an HPKE key pair from a serialized decapsulation key.
        /// </summary>
        /// <param name="suite">
        ///   The cipher suite associated with the key.
        /// </param>
        /// <param name="source">
        ///   The serialized decapsulation key.
        /// </param>
        /// <returns>
        ///   A new HPKE key containing the decapsulation key and its corresponding encapsulation key.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="suite" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="source" /> is not exactly <see cref="HpkeSuite.DecapsulationKeySizeInBytes" /> bytes long.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   The decapsulation key is invalid, or an error occurred while importing the key.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   <paramref name="suite" /> is not supported on the current platform.
        /// </exception>
        /// <remarks>
        ///   The key must use the format returned by <see cref="ExportDecapsulationKey()" />,
        ///   not the input keying material accepted by <see cref="DeriveKey(HpkeSuite, ReadOnlySpan{byte})" />.
        ///   The imported key does not retain a reference to <paramref name="source" />.
        ///   The caller remains responsible for protecting and clearing the source key bytes.
        /// </remarks>
        public static Hpke ImportDecapsulationKey(HpkeSuite suite, ReadOnlySpan<byte> source)
        {
            ArgumentNullException.ThrowIfNull(suite);

            if (source.Length != suite.DecapsulationKeySizeInBytes)
            {
                throw new ArgumentException(SR.Argument_PrivateKeyWrongSizeForAlgorithm, nameof(source));
            }

            ThrowIfNotSupported(suite);
            return HpkeImplementation.ImportDecapsulationKeyImpl(suite, source);
        }

        /// <summary>
        ///   Imports an HPKE key from a serialized encapsulation key.
        /// </summary>
        /// <param name="suite">
        ///   The cipher suite associated with the key.
        /// </param>
        /// <param name="source">
        ///   The serialized encapsulation key.
        /// </param>
        /// <returns>
        ///   A new HPKE key containing only the encapsulation key.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="suite" /> or <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="source" /> is not exactly <see cref="HpkeSuite.EncapsulationKeySizeInBytes" /> bytes long.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   The encapsulation key is invalid, or an error occurred while importing the key.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   <paramref name="suite" /> is not supported on the current platform.
        /// </exception>
        /// <remarks>
        ///   The key must use the format returned by <see cref="ExportEncapsulationKey()" />.
        ///   The imported key can encrypt messages and create sender contexts, but cannot decrypt messages,
        ///   create recipient contexts, or export a decapsulation key.
        ///   The imported key does not retain a reference to <paramref name="source" />.
        /// </remarks>
        public static Hpke ImportEncapsulationKey(HpkeSuite suite, byte[] source)
        {
            ArgumentNullException.ThrowIfNull(source);
            return ImportEncapsulationKey(suite, new ReadOnlySpan<byte>(source));
        }

        /// <summary>
        ///   Imports an HPKE key from a serialized encapsulation key.
        /// </summary>
        /// <param name="suite">
        ///   The cipher suite associated with the key.
        /// </param>
        /// <param name="source">
        ///   The serialized encapsulation key.
        /// </param>
        /// <returns>
        ///   A new HPKE key containing only the encapsulation key.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="suite" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="source" /> is not exactly <see cref="HpkeSuite.EncapsulationKeySizeInBytes" /> bytes long.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   The encapsulation key is invalid, or an error occurred while importing the key.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   <paramref name="suite" /> is not supported on the current platform.
        /// </exception>
        /// <remarks>
        ///   The key must use the format returned by <see cref="ExportEncapsulationKey()" />.
        ///   The imported key can encrypt messages and create sender contexts, but cannot decrypt messages,
        ///   create recipient contexts, or export a decapsulation key.
        ///   The imported key does not retain a reference to <paramref name="source" />.
        /// </remarks>
        public static Hpke ImportEncapsulationKey(HpkeSuite suite, ReadOnlySpan<byte> source)
        {
            ArgumentNullException.ThrowIfNull(suite);

            if (source.Length != suite.EncapsulationKeySizeInBytes)
            {
                throw new ArgumentException(SR.Argument_PublicKeyWrongSizeForAlgorithm, nameof(source));
            }

            ThrowIfNotSupported(suite);
            return HpkeImplementation.ImportEncapsulationKeyImpl(suite, source);
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

        /// <summary>
        ///   Encrypts and authenticates a single message into the provided buffers using Base mode.
        /// </summary>
        /// <param name="plaintext">
        ///   The message to encrypt.
        /// </param>
        /// <param name="encapsulatedSecret">
        ///   The buffer to receive the encapsulated secret to send to the recipient.
        /// </param>
        /// <param name="ciphertext">
        ///   The buffer to receive the ciphertext followed by its authentication tag.
        /// </param>
        /// <param name="associatedData">
        ///   The additional data to authenticate without encrypting.
        /// </param>
        /// <param name="info">
        ///   The application context, which must match the value used by the recipient.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="encapsulatedSecret" /> is not exactly <see cref="HpkeSuite.EncapsulatedSecretSizeInBytes" />
        ///   bytes long, <paramref name="ciphertext" /> is not exactly the length returned by
        ///   <see cref="HpkeSuite.GetCiphertextLength" /> for <paramref name="plaintext" />,
        ///   or <paramref name="info" /> exceeds the cipher suite's KDF length limit.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The ciphertext length would exceed <see cref="int.MaxValue" />.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     One or more provided buffers overlap.
        ///   </para>
        ///   <para> -or- </para>
        ///   <para>
        ///     The current instance does not contain an encapsulation key, or an error occurred during encryption.
        ///   </para>
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The object has already been disposed.
        /// </exception>
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

            if (encapsulatedSecret.Overlaps(ciphertext) ||
                plaintext.Overlaps(encapsulatedSecret) ||
                associatedData.Overlaps(encapsulatedSecret) ||
                info.Overlaps(encapsulatedSecret) ||
                plaintext.Overlaps(ciphertext) ||
                associatedData.Overlaps(ciphertext) ||
                info.Overlaps(ciphertext))
            {
                throw new CryptographicException(SR.Cryptography_OverlappingBuffers);
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
        ///   <para>
        ///     One or more provided buffers overlap.
        ///   </para>
        ///   <para> -or- </para>
        ///   <para>
        ///     The current instance does not contain an encapsulation key, or an error occurred while creating the sender.
        ///   </para>
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

            if (info.Overlaps(encapsulatedSecret))
            {
                throw new CryptographicException(SR.Cryptography_OverlappingBuffers);
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
        ///   Creates an HPKE sender context using a pre-shared key.
        /// </summary>
        /// <param name="psk">
        ///   The pre-shared key, which must be at least 32 bytes long.
        /// </param>
        /// <param name="pskId">
        ///   The nonempty identifier for the pre-shared key.
        /// </param>
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
        ///   <paramref name="psk" /> is shorter than 32 bytes, <paramref name="pskId" /> is empty,
        ///   or an input exceeds the maximum length supported by the cipher suite's KDF.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   The current instance does not contain an encapsulation key, or an error occurred while creating the sender.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   Creating a PSK sender is not supported on the current platform.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The object has already been disposed.
        /// </exception>
        /// <remarks>
        ///   The sender and recipient must use the same pre-shared key and identifier.
        ///   The caller must ensure that the pre-shared key has at least 32 bytes of entropy;
        ///   length validation alone does not guarantee this. A low-entropy password is not a suitable pre-shared key.
        /// </remarks>
        public HpkeSender CreatePskSender(
            ReadOnlySpan<byte> psk,
            ReadOnlySpan<byte> pskId,
            out byte[] encapsulatedSecret,
            ReadOnlySpan<byte> info = default)
        {
            ThrowIfInvalidPskInputs(psk, pskId);
            ThrowIfInfoExceedsLimit(info);
            ThrowIfDisposed();

            byte[] encapsulatedSecretBuffer = new byte[Suite.EncapsulatedSecretSizeInBytes];
            HpkeSender sender = CreatePskSenderCore(encapsulatedSecretBuffer, info, psk, pskId);
            encapsulatedSecret = encapsulatedSecretBuffer;
            return sender;
        }

        /// <summary>
        ///   Creates an HPKE sender context using a pre-shared key.
        /// </summary>
        /// <param name="psk">
        ///   The pre-shared key, which must be at least 32 bytes long.
        /// </param>
        /// <param name="pskId">
        ///   The nonempty identifier for the pre-shared key.
        /// </param>
        /// <param name="encapsulatedSecret">
        ///   When this method returns, contains the encapsulated secret to send to the recipient.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <param name="info">
        ///   The application context, which must match the value used by the recipient,
        ///   or <see langword="null" /> to use an empty context.
        /// </param>
        /// <returns>
        ///   A new sender context for this key's cipher suite.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="psk" /> or <paramref name="pskId" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="psk" /> is shorter than 32 bytes, <paramref name="pskId" /> is empty,
        ///   or an input exceeds the maximum length supported by the cipher suite's KDF.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   The current instance does not contain an encapsulation key, or an error occurred while creating the sender.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   Creating a PSK sender is not supported on the current platform.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The object has already been disposed.
        /// </exception>
        /// <remarks>
        ///   The caller must ensure that the pre-shared key has at least 32 bytes of entropy.
        ///   The sender and recipient must use the same pre-shared key and identifier.
        /// </remarks>
        public HpkeSender CreatePskSender(
            byte[] psk,
            byte[] pskId,
            out byte[] encapsulatedSecret,
            byte[]? info = null)
        {
            ArgumentNullException.ThrowIfNull(psk);
            ArgumentNullException.ThrowIfNull(pskId);
            return CreatePskSender(new ReadOnlySpan<byte>(psk), pskId, out encapsulatedSecret, info);
        }

        /// <summary>
        ///   Creates an HPKE sender context using a pre-shared key and writes the encapsulated secret into the provided buffer.
        /// </summary>
        /// <param name="psk">
        ///   The pre-shared key, which must be at least 32 bytes long.
        /// </param>
        /// <param name="pskId">
        ///   The nonempty identifier for the pre-shared key.
        /// </param>
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
        ///   <paramref name="psk" /> is shorter than 32 bytes, <paramref name="pskId" /> is empty,
        ///   an input exceeds the maximum length supported by the cipher suite's KDF,
        ///   or <paramref name="encapsulatedSecret" /> is not exactly
        ///   <see cref="HpkeSuite.EncapsulatedSecretSizeInBytes" /> bytes long.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     One or more provided buffers overlap.
        ///   </para>
        ///   <para> -or- </para>
        ///   <para>
        ///     The current instance does not contain an encapsulation key, or an error occurred while creating the sender.
        ///   </para>
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   Creating a PSK sender is not supported on the current platform.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The object has already been disposed.
        /// </exception>
        /// <remarks>
        ///   The caller must ensure that the pre-shared key has at least 32 bytes of entropy.
        ///   The sender and recipient must use the same pre-shared key and identifier.
        /// </remarks>
        public HpkeSender CreatePskSender(
            ReadOnlySpan<byte> psk,
            ReadOnlySpan<byte> pskId,
            Span<byte> encapsulatedSecret,
            ReadOnlySpan<byte> info = default)
        {
            ThrowIfInvalidPskInputs(psk, pskId);
            ThrowIfInfoExceedsLimit(info);
            ThrowIfDisposed();

            if (encapsulatedSecret.Length != Suite.EncapsulatedSecretSizeInBytes)
            {
                throw new ArgumentException(
                    SR.Format(SR.Argument_DestinationImprecise, Suite.EncapsulatedSecretSizeInBytes),
                    nameof(encapsulatedSecret));
            }

            if (psk.Overlaps(encapsulatedSecret) ||
                pskId.Overlaps(encapsulatedSecret) ||
                info.Overlaps(encapsulatedSecret))
            {
                throw new CryptographicException(SR.Cryptography_OverlappingBuffers);
            }

            return CreatePskSenderCore(encapsulatedSecret, info, psk, pskId);
        }

        /// <summary>
        ///   When overridden in a derived class, creates an HPKE sender context using a pre-shared key.
        /// </summary>
        /// <param name="encapsulatedSecret">
        ///   The buffer to receive the encapsulated secret.
        /// </param>
        /// <param name="info">
        ///   The application context.
        /// </param>
        /// <param name="psk">
        ///   The pre-shared key.
        /// </param>
        /// <param name="pskId">
        ///   The identifier for the pre-shared key.
        /// </param>
        /// <returns>
        ///   A new sender context for this key's cipher suite.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   The current instance does not contain an encapsulation key, or an error occurred while creating the sender.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   Creating a PSK sender is not supported on the current platform.
        /// </exception>
        /// <remarks>
        ///   The calling method has verified that this instance is not disposed, the encapsulated secret buffer
        ///   has the exact required length, the pre-shared key is at least 32 bytes long, the identifier is nonempty,
        ///   and all inputs satisfy the KDF's length limits. Implementations must fill the entire buffer
        ///   and return an initialized sender for <see cref="Suite" />.
        /// </remarks>
        protected abstract HpkeSender CreatePskSenderCore(
            Span<byte> encapsulatedSecret,
            ReadOnlySpan<byte> info,
            ReadOnlySpan<byte> psk,
            ReadOnlySpan<byte> pskId);

        /// <summary>
        ///   Creates an HPKE recipient context using a pre-shared key.
        /// </summary>
        /// <param name="encapsulatedSecret">
        ///   The encapsulated secret produced by the sender.
        /// </param>
        /// <param name="psk">
        ///   The pre-shared key, which must be at least 32 bytes long.
        /// </param>
        /// <param name="pskId">
        ///   The nonempty identifier for the pre-shared key.
        /// </param>
        /// <param name="info">
        ///   The application context, which must match the value used by the sender.
        /// </param>
        /// <returns>
        ///   A new recipient context for this key's cipher suite.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="psk" /> is shorter than 32 bytes, <paramref name="pskId" /> is empty,
        ///   an input exceeds the maximum length supported by the cipher suite's KDF,
        ///   or <paramref name="encapsulatedSecret" /> is not exactly
        ///   <see cref="HpkeSuite.EncapsulatedSecretSizeInBytes" /> bytes long.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   The current instance does not contain a decapsulation key, the encapsulated secret is invalid,
        ///   or an error occurred while creating the recipient.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   Creating a PSK recipient is not supported on the current platform.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The object has already been disposed.
        /// </exception>
        /// <remarks>
        ///   The caller must ensure that the pre-shared key has at least 32 bytes of entropy.
        ///   The sender and recipient must use the same pre-shared key and identifier.
        /// </remarks>
        public HpkeRecipient CreatePskRecipient(
            ReadOnlySpan<byte> encapsulatedSecret,
            ReadOnlySpan<byte> psk,
            ReadOnlySpan<byte> pskId,
            ReadOnlySpan<byte> info = default)
        {
            ThrowIfInvalidPskInputs(psk, pskId);
            ThrowIfInfoExceedsLimit(info);
            ThrowIfDisposed();
            ThrowIfInvalidEncapsulatedSecretLength(encapsulatedSecret);
            return CreatePskRecipientCore(encapsulatedSecret, info, psk, pskId);
        }

        /// <summary>
        ///   Creates an HPKE recipient context using a pre-shared key.
        /// </summary>
        /// <param name="encapsulatedSecret">
        ///   The encapsulated secret produced by the sender.
        /// </param>
        /// <param name="psk">
        ///   The pre-shared key, which must be at least 32 bytes long.
        /// </param>
        /// <param name="pskId">
        ///   The nonempty identifier for the pre-shared key.
        /// </param>
        /// <param name="info">
        ///   The application context, which must match the value used by the sender,
        ///   or <see langword="null" /> to use an empty context.
        /// </param>
        /// <returns>
        ///   A new recipient context for this key's cipher suite.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="encapsulatedSecret" />, <paramref name="psk" />, or <paramref name="pskId" />
        ///   is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="psk" /> is shorter than 32 bytes, <paramref name="pskId" /> is empty,
        ///   an input exceeds the maximum length supported by the cipher suite's KDF,
        ///   or <paramref name="encapsulatedSecret" /> is not exactly
        ///   <see cref="HpkeSuite.EncapsulatedSecretSizeInBytes" /> bytes long.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   The current instance does not contain a decapsulation key, the encapsulated secret is invalid,
        ///   or an error occurred while creating the recipient.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   Creating a PSK recipient is not supported on the current platform.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   The object has already been disposed.
        /// </exception>
        /// <remarks>
        ///   The caller must ensure that the pre-shared key has at least 32 bytes of entropy.
        ///   The sender and recipient must use the same pre-shared key and identifier.
        /// </remarks>
        public HpkeRecipient CreatePskRecipient(
            byte[] encapsulatedSecret,
            byte[] psk,
            byte[] pskId,
            byte[]? info = null)
        {
            ArgumentNullException.ThrowIfNull(encapsulatedSecret);
            ArgumentNullException.ThrowIfNull(psk);
            ArgumentNullException.ThrowIfNull(pskId);
            return CreatePskRecipient(new ReadOnlySpan<byte>(encapsulatedSecret), psk, pskId, info);
        }

        /// <summary>
        ///   When overridden in a derived class, creates an HPKE recipient context using a pre-shared key.
        /// </summary>
        /// <param name="encapsulatedSecret">
        ///   The encapsulated secret produced by the sender.
        /// </param>
        /// <param name="info">
        ///   The application context.
        /// </param>
        /// <param name="psk">
        ///   The pre-shared key.
        /// </param>
        /// <param name="pskId">
        ///   The identifier for the pre-shared key.
        /// </param>
        /// <returns>
        ///   A new recipient context for this key's cipher suite.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   The current instance does not contain a decapsulation key, the encapsulated secret is invalid,
        ///   or an error occurred while creating the recipient.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   Creating a PSK recipient is not supported on the current platform.
        /// </exception>
        /// <remarks>
        ///   The calling method has verified that this instance is not disposed, the encapsulated secret
        ///   has the exact required length, the pre-shared key is at least 32 bytes long, the identifier is nonempty,
        ///   and all inputs satisfy the KDF's length limits. Implementations must return an initialized recipient
        ///   for <see cref="Suite" />.
        /// </remarks>
        protected abstract HpkeRecipient CreatePskRecipientCore(
            ReadOnlySpan<byte> encapsulatedSecret,
            ReadOnlySpan<byte> info,
            ReadOnlySpan<byte> psk,
            ReadOnlySpan<byte> pskId);

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

        private void ThrowIfInvalidPskInputs(ReadOnlySpan<byte> psk, ReadOnlySpan<byte> pskId)
        {
            // A shorter key cannot meet HPKE's requirement for 32 bytes of PSK entropy.
            // https://datatracker.ietf.org/doc/html/draft-ietf-hpke-hpke-04#section-5.1.2
            const int MinimumPskLength = 32;

            if (psk.Length < MinimumPskLength)
            {
                throw new ArgumentException(SR.Format(SR.Argument_HpkePskTooShort, MinimumPskLength), nameof(psk));
            }

            if (pskId.IsEmpty)
            {
                throw new ArgumentException(SR.Argument_HpkePskIdEmpty, nameof(pskId));
            }

            if (psk.Length > Suite.KdfMetadata.MaximumPskLength)
            {
                throw new ArgumentException(
                    SR.Format(SR.Argument_HpkePskTooLong, Suite.KdfMetadata.MaximumPskLength),
                    nameof(psk));
            }

            if (pskId.Length > Suite.KdfMetadata.MaximumPskIdLength)
            {
                throw new ArgumentException(
                    SR.Format(SR.Argument_HpkePskIdTooLong, Suite.KdfMetadata.MaximumPskIdLength),
                    nameof(pskId));
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
