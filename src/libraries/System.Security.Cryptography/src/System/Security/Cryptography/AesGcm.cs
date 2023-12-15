// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.Versioning;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    [UnsupportedOSPlatform("browser")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    public sealed partial class AesGcm : IDisposable
    {
        private const int NonceSize = 12;
        public static KeySizes NonceByteSizes { get; } = new KeySizes(NonceSize, NonceSize, 1);

        [Obsolete(Obsoletions.AesGcmTagConstructorMessage, DiagnosticId = Obsoletions.AesGcmTagConstructorDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public AesGcm(ReadOnlySpan<byte> key)
        {
            ThrowIfNotSupported();

            AesAEAD.CheckKeySize(key.Length);
            ImportKey(key);
        }

        [Obsolete(Obsoletions.AesGcmTagConstructorMessage, DiagnosticId = Obsoletions.AesGcmTagConstructorDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public AesGcm(byte[] key)
            : this(new ReadOnlySpan<byte>(key ?? throw new ArgumentNullException(nameof(key))))
        {
        }

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

        /// <summary>
        /// Gets the size of the tag, in bytes.
        /// </summary>
        /// <value>
        /// The size of the tag that must be used for encryption or decryption, or <see langword="null" /> if the
        /// tag size is unspecified.
        /// </value>
        public int? TagSizeInBytes { get; }

        public void Encrypt(byte[] nonce, byte[] plaintext, byte[] ciphertext, byte[] tag, byte[]? associatedData = null)
        {
            ArgumentNullException.ThrowIfNull(nonce);
            ArgumentNullException.ThrowIfNull(plaintext);
            ArgumentNullException.ThrowIfNull(ciphertext);
            ArgumentNullException.ThrowIfNull(tag);

            Encrypt((ReadOnlySpan<byte>)nonce, plaintext, ciphertext, tag, associatedData);
        }

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

        public void Decrypt(byte[] nonce, byte[] ciphertext, byte[] tag, byte[] plaintext, byte[]? associatedData = null)
        {
            ArgumentNullException.ThrowIfNull(nonce);
            ArgumentNullException.ThrowIfNull(ciphertext);
            ArgumentNullException.ThrowIfNull(tag);
            ArgumentNullException.ThrowIfNull(plaintext);

            Decrypt((ReadOnlySpan<byte>)nonce, ciphertext, tag, plaintext, associatedData);
        }

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
