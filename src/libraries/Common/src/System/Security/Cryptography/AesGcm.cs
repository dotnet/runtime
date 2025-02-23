// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.Versioning;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    /// <summary>
    /// Represents an Advanced Encryption Standard (AES) key to be used with the Galois/Counter Mode (GCM) mode of operation.
    /// </summary>
    public sealed partial class AesGcm : IDisposable
    {
        private const int NonceSize = 12;

        /// <summary>
        /// Gets the nonce sizes, in bytes, supported by this instance.
        /// </summary>
        /// <value>
        /// The nonce sizes supported by this instance: 12 bytes (96 bits).
        /// </value>
        public static KeySizes NonceByteSizes { get; } = new KeySizes(NonceSize, NonceSize, 1);

        /// <summary>
        /// Gets the size of the tag, in bytes.
        /// </summary>
        /// <value>
        /// The size of the tag that must be used for encryption or decryption, or <see langword="null" /> if the
        /// tag size is unspecified.
        /// </value>
        public int? TagSizeInBytes { get; }

        /// <summary>
        /// Gets the tag sizes, in bytes, supported by this instance.
        /// </summary>
        /// <value>
        /// The tag sizes supported by this instance: 12, 13, 14, 15, or 16 bytes (96, 104, 112, 120, or 128 bits).
        /// </value>
        public static partial KeySizes TagByteSizes { get; }

        /// <summary>
        /// Gets a value that indicates whether the algorithm is supported on the current platform.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the algorithm is supported; otherwise, <see langword="false"/>.
        /// </value>
        public static partial bool IsSupported { get; }

        /// <summary>
        ///   Initializes a new instance of the <see cref="AesGcm" /> class with a provided key and required tag size.
        /// </summary>
        /// <param name="key">The secret key to use for this instance.</param>
        /// <param name="tagSizeInBytes">The size of the tag, in bytes, that encryption and decryption must use.</param>
        /// <exception cref="CryptographicException">
        ///   The <paramref name="key" /> parameter length is other than 16, 24, or 32 bytes (128, 192, or 256 bits).
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   The <paramref name="tagSizeInBytes" /> parameter is an unsupported tag size indicated by
        ///   <see cref="TagByteSizes" />.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The current platform does not support AES-GCM.
        /// </exception>
        /// <remarks>
        ///   The <paramref name="tagSizeInBytes" /> parameter is used to indicate that the tag parameter in <c>Encrypt</c>
        ///   or <c>Decrypt</c> must be exactly this size. Indicating the required tag size prevents issues where callers
        ///   of <c>Decrypt</c> may supply a tag as input and that input is truncated to an unexpected size.
        /// </remarks>
        public AesGcm(ReadOnlySpan<byte> key, int tagSizeInBytes)
        {
            ThrowIfNotSupported();

            AesAEAD.CheckKeySize(key.Length);

            if (!tagSizeInBytes.IsLegalSize(TagByteSizes))
            {
                throw new ArgumentException(SR.Cryptography_InvalidTagLength, nameof(tagSizeInBytes));
            }

            TagSizeInBytes = tagSizeInBytes;
            ImportKey(key);
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="AesGcm" /> class with a provided key and required tag size.
        /// </summary>
        /// <param name="key">The secret key to use for this instance.</param>
        /// <param name="tagSizeInBytes">The size of the tag, in bytes, that encryption and decryption must use.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="key" /> parameter is null.</exception>
        /// <exception cref="CryptographicException">
        ///   The <paramref name="key" /> parameter length is other than 16, 24, or 32 bytes (128, 192, or 256 bits).
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   The <paramref name="tagSizeInBytes" /> parameter is an unsupported tag size indicated by
        ///   <see cref="TagByteSizes" />.
        /// </exception>
        /// <exception cref="PlatformNotSupportedException">
        ///   The current platform does not support AES-GCM.
        /// </exception>
        /// <remarks>
        ///   The <paramref name="tagSizeInBytes" /> parameter is used to indicate that the tag parameter in <c>Encrypt</c>
        ///   or <c>Decrypt</c> must be exactly this size. Indicating the required tag size prevents issues where callers
        ///   of <c>Decrypt</c> may supply a tag as input and that input is truncated to an unexpected size.
        /// </remarks>
        public AesGcm(byte[] key, int tagSizeInBytes)
            : this(new ReadOnlySpan<byte>(key ?? throw new ArgumentNullException(nameof(key))), tagSizeInBytes)
        {
        }

        private partial void ImportKey(ReadOnlySpan<byte> key);

        private partial void DecryptCore(
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> ciphertext,
            ReadOnlySpan<byte> tag,
            Span<byte> plaintext,
            ReadOnlySpan<byte> associatedData);

        private partial void EncryptCore(
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> plaintext,
            Span<byte> ciphertext,
            Span<byte> tag,
            ReadOnlySpan<byte> associatedData);

        /// <summary>
        ///   Encrypts the plaintext into the ciphertext destination buffer and generates the authentication tag
        ///   into a separate buffer.
        /// </summary>
        /// <param name="nonce">
        ///   The nonce associated with this message, which should be a unique value for every operation with the
        ///   same key.
        /// </param>
        /// <param name="plaintext">The content to encrypt.</param>
        /// <param name="ciphertext">The byte array to receive the encrypted contents.</param>
        /// <param name="tag">The byte array to receive the generated authentication tag.</param>
        /// <param name="associatedData">
        ///   Extra data associated with this message, which must also be provided during decryption.
        /// </param>
        /// <remarks>
        ///   The security guarantees of the AES-GCM algorithm mode require that the same nonce value is never used
        ///   twice with the same key.
        /// </remarks>
        /// <exception cref="ArgumentException">
        ///   <para>The <paramref name="plaintext" /> parameter and the <paramref name="ciphertext" /> do not have the same length.</para>
        ///   <para> -or- </para>
        ///   <para>The <paramref name="nonce" /> parameter length is not permitted by <see cref="AesGcm.NonceByteSizes" />.</para>
        ///   <para> -or- </para>
        ///   <para>The <paramref name="tag" /> parameter length is not permitted by <see cref="AesGcm.TagByteSizes" />.</para>
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   The <paramref name="nonce" />, <paramref name="ciphertext" />, <paramref name="tag" />, or
        ///   <paramref name="plaintext" /> parameter is <see langword="null" />.
        /// </exception>
        /// <exception cref="CryptographicException">The encryption operation failed.</exception>
        public void Encrypt(byte[] nonce, byte[] plaintext, byte[] ciphertext, byte[] tag, byte[]? associatedData = null)
        {
#if NET
            ArgumentNullException.ThrowIfNull(nonce);
            ArgumentNullException.ThrowIfNull(plaintext);
            ArgumentNullException.ThrowIfNull(ciphertext);
            ArgumentNullException.ThrowIfNull(tag);
#else
            if (nonce is null)
                throw new ArgumentNullException(nameof(nonce));

            if (plaintext is null)
                throw new ArgumentNullException(nameof(plaintext));

            if (ciphertext is null)
                throw new ArgumentNullException(nameof(ciphertext));

            if (tag is null)
                throw new ArgumentNullException(nameof(tag));
#endif

            Encrypt((ReadOnlySpan<byte>)nonce, plaintext, ciphertext, tag, associatedData);
        }


        /// <summary>
        ///   Encrypts the plaintext into the ciphertext destination buffer and generates the authentication tag
        ///   into a separate buffer.
        /// </summary>
        /// <param name="nonce">
        ///   The nonce associated with this message, which should be a unique value for every operation with the
        ///   same key.
        /// </param>
        /// <param name="plaintext">The content to encrypt.</param>
        /// <param name="ciphertext">The byte array to receive the encrypted contents.</param>
        /// <param name="tag">The byte array to receive the generated authentication tag.</param>
        /// <param name="associatedData">
        ///   Extra data associated with this message, which must also be provided during decryption.
        /// </param>
        /// <remarks>
        ///   The security guarantees of the AES-GCM algorithm mode require that the same nonce value is never used
        ///   twice with the same key.
        /// </remarks>
        /// <exception cref="ArgumentException">
        ///   <para>The <paramref name="plaintext" /> parameter and the <paramref name="ciphertext" /> do not have the same length.</para>
        ///   <para> -or- </para>
        ///   <para>The <paramref name="nonce" /> parameter length is not permitted by <see cref="AesGcm.NonceByteSizes" />.</para>
        ///   <para> -or- </para>
        ///   <para>The <paramref name="tag" /> parameter length is not permitted by <see cref="AesGcm.TagByteSizes" />.</para>
        /// </exception>
        /// <exception cref="CryptographicException">The encryption operation failed.</exception>
        public void Encrypt(
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> plaintext,
            Span<byte> ciphertext,
            Span<byte> tag,
            ReadOnlySpan<byte> associatedData = default)
        {
            CheckParameters(plaintext, ciphertext, nonce, tag);
            EncryptCore(nonce, plaintext, ciphertext, tag, associatedData);
        }

        /// <summary>
        ///   Decrypts the ciphertext into the provided destination buffer if the authentication tag can be validated.
        /// </summary>
        /// <param name="nonce">
        ///   The nonce associated with this message, which must match the value provided during encryption
        /// </param>
        /// <param name="ciphertext">The encrypted content to decrypt.</param>
        /// <param name="tag">The authentication tag produced for this message during encryption.</param>
        /// <param name="plaintext">The byte array to receive the decrypted contents.</param>
        /// <param name="associatedData">
        ///   Extra data associated with this message, which must match the value provided during encryption
        /// </param>
        /// <remarks>
        ///   If <c>tag</c> cannot be validated (using the key, <c>nonce</c>, <c>ciphertext</c>, and
        ///   <c>associatedData</c> values), then <c>plaintext</c> is cleared.
        /// </remarks>
        /// <exception cref="ArgumentException">
        ///   <para>The <paramref name="plaintext" /> parameter and the <paramref name="ciphertext" /> do not have the same length.</para>
        ///   <para> -or- </para>
        ///   <para>The <paramref name="nonce" /> parameter length is not permitted by <see cref="AesGcm.NonceByteSizes" />.</para>
        ///   <para> -or- </para>
        ///   <para>The <paramref name="tag" /> parameter length is not permitted by <see cref="AesGcm.TagByteSizes" />.</para>
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   The <paramref name="nonce" />, <paramref name="ciphertext" />, <paramref name="tag" />, or
        ///   <paramref name="plaintext" /> parameter is <see langword="null" />.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   The decryption operation failed. Prior to .NET 8, indicates the tag value could not be verified.
        /// </exception>
        /// <exception cref="AuthenticationTagMismatchException">The tag value could not be verified.</exception>
        public void Decrypt(byte[] nonce, byte[] ciphertext, byte[] tag, byte[] plaintext, byte[]? associatedData = null)
        {
#if NET
            ArgumentNullException.ThrowIfNull(nonce);
            ArgumentNullException.ThrowIfNull(ciphertext);
            ArgumentNullException.ThrowIfNull(tag);
            ArgumentNullException.ThrowIfNull(plaintext);
#else
            if (nonce is null)
                throw new ArgumentNullException(nameof(nonce));

            if (ciphertext is null)
                throw new ArgumentNullException(nameof(ciphertext));

            if (tag is null)
                throw new ArgumentNullException(nameof(tag));

            if (plaintext is null)
                throw new ArgumentNullException(nameof(plaintext));
#endif

            Decrypt((ReadOnlySpan<byte>)nonce, ciphertext, tag, plaintext, associatedData);
        }

        /// <summary>
        ///   Decrypts the ciphertext into the provided destination buffer if the authentication tag can be validated.
        /// </summary>
        /// <param name="nonce">
        ///   The nonce associated with this message, which must match the value provided during encryption
        /// </param>
        /// <param name="ciphertext">The encrypted content to decrypt.</param>
        /// <param name="tag">The authentication tag produced for this message during encryption.</param>
        /// <param name="plaintext">The byte span to receive the decrypted contents.</param>
        /// <param name="associatedData">
        ///   Extra data associated with this message, which must match the value provided during encryption
        /// </param>
        /// <remarks>
        ///   If <c>tag</c> cannot be validated (using the key, <c>nonce</c>, <c>ciphertext</c>, and
        ///   <c>associatedData</c> values), then <c>plaintext</c> is cleared.
        /// </remarks>
        /// <exception cref="ArgumentException">
        ///   <para>The <paramref name="plaintext" /> parameter and the <paramref name="ciphertext" /> do not have the same length.</para>
        ///   <para> -or- </para>
        ///   <para>The <paramref name="nonce" /> parameter length is not permitted by <see cref="AesGcm.NonceByteSizes" />.</para>
        ///   <para> -or- </para>
        ///   <para>The <paramref name="tag" /> parameter length is not permitted by <see cref="AesGcm.TagByteSizes" />.</para>
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   The decryption operation failed. Prior to .NET 8, indicates the tag value could not be verified.
        /// </exception>
        /// <exception cref="AuthenticationTagMismatchException">The tag value could not be verified.</exception>
        public void Decrypt(
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> ciphertext,
            ReadOnlySpan<byte> tag,
            Span<byte> plaintext,
            ReadOnlySpan<byte> associatedData = default)
        {
            CheckParameters(plaintext, ciphertext, nonce, tag);
            DecryptCore(nonce, ciphertext, tag, plaintext, associatedData);
        }

        /// <summary>
        /// Releases the resources used by the current instance of the <see cref="AesGcm"/> class.
        /// </summary>
        public partial void Dispose();

        private void CheckParameters(
            ReadOnlySpan<byte> plaintext,
            ReadOnlySpan<byte> ciphertext,
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> tag)
        {
            if (plaintext.Length != ciphertext.Length)
                throw new ArgumentException(SR.Cryptography_PlaintextCiphertextLengthMismatch);

            if (!nonce.Length.IsLegalSize(NonceByteSizes))
                throw new ArgumentException(SR.Cryptography_InvalidNonceLength, nameof(nonce));

            if (TagSizeInBytes is int tagSizeInBytes)
            {
                // constructor promise
                Debug.Assert(tagSizeInBytes.IsLegalSize(TagByteSizes));

                if (tag.Length != tagSizeInBytes)
                {
                    throw new ArgumentException(SR.Format(SR.Cryptography_IncorrectTagLength, tagSizeInBytes), nameof(tag));
                }
            }
            else if (!tag.Length.IsLegalSize(TagByteSizes))
            {
                throw new ArgumentException(SR.Cryptography_InvalidTagLength, nameof(tag));
            }
        }

        private static void ThrowIfNotSupported()
        {
            if (!IsSupported)
            {
                throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_AlgorithmNotSupported, nameof(AesGcm)));
            }
        }
    }
}
