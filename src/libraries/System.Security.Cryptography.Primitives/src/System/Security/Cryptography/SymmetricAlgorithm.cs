// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    public abstract class SymmetricAlgorithm : IDisposable
    {
        protected SymmetricAlgorithm()
        {
            ModeValue = CipherMode.CBC;
            PaddingValue = PaddingMode.PKCS7;
        }

        [Obsolete(Obsoletions.DefaultCryptoAlgorithmsMessage, DiagnosticId = Obsoletions.DefaultCryptoAlgorithmsDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public static SymmetricAlgorithm Create() =>
            throw new PlatformNotSupportedException(SR.Cryptography_DefaultAlgorithm_NotSupported);

        [RequiresUnreferencedCode(CryptoConfigForwarder.CreateFromNameUnreferencedCodeMessage)]
        public static SymmetricAlgorithm? Create(string algName) =>
            (SymmetricAlgorithm?)CryptoConfigForwarder.CreateFromName(algName);

        public virtual int FeedbackSize
        {
            get
            {
                return FeedbackSizeValue;
            }
            set
            {
                if (value <= 0 || value > BlockSizeValue || (value % 8) != 0)
                    throw new CryptographicException(SR.Cryptography_InvalidFeedbackSize);
                FeedbackSizeValue = value;
            }
        }

        public virtual int BlockSize
        {
            get
            {
                return BlockSizeValue;
            }

            set
            {
                bool validatedByZeroSkipSizeKeySizes;
                if (!value.IsLegalSize(this.LegalBlockSizes, out validatedByZeroSkipSizeKeySizes))
                    throw new CryptographicException(SR.Cryptography_InvalidBlockSize);

                if (BlockSizeValue == value && !validatedByZeroSkipSizeKeySizes) // The !validatedByZeroSkipSizeKeySizes check preserves a very obscure back-compat behavior.
                    return;

                BlockSizeValue = value;
                IVValue = null;
                return;
            }
        }

        public virtual byte[] IV
        {
            get
            {
                if (IVValue == null)
                    GenerateIV();
                return IVValue.CloneByteArray()!;
            }

            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                if (value.Length != this.BlockSize / 8)
                    throw new CryptographicException(SR.Cryptography_InvalidIVSize);

                IVValue = value.CloneByteArray();
            }
        }

        public virtual byte[] Key
        {
            get
            {
                if (KeyValue == null)
                    GenerateKey();
                return KeyValue.CloneByteArray()!;
            }

            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                long bitLength = value.Length * 8L;
                if (bitLength > int.MaxValue || !ValidKeySize((int)bitLength))
                    throw new CryptographicException(SR.Cryptography_InvalidKeySize);

                // must convert bytes to bits
                this.KeySize = (int)bitLength;
                KeyValue = value.CloneByteArray();
            }
        }

        public virtual int KeySize
        {
            get
            {
                return KeySizeValue;
            }

            set
            {
                if (!ValidKeySize(value))
                    throw new CryptographicException(SR.Cryptography_InvalidKeySize);

                KeySizeValue = value;
                KeyValue = null;
            }
        }

        public virtual KeySizes[] LegalBlockSizes
        {
            get
            {
                // .NET Framework compat: No null check is performed.
                return (KeySizes[])LegalBlockSizesValue!.Clone();
            }
        }

        public virtual KeySizes[] LegalKeySizes
        {
            get
            {
                // .NET Framework compat: No null check is performed.
                return (KeySizes[])LegalKeySizesValue!.Clone();
            }
        }

        public virtual CipherMode Mode
        {
            get
            {
                return ModeValue;
            }

            set
            {
                if (!(value == CipherMode.CBC || value == CipherMode.ECB || value == CipherMode.CFB))
                    throw new CryptographicException(SR.Cryptography_InvalidCipherMode);

                ModeValue = value;
            }
        }

        public virtual PaddingMode Padding
        {
            get
            {
                return PaddingValue;
            }

            set
            {
                if ((value < PaddingMode.None) || (value > PaddingMode.ISO10126))
                    throw new CryptographicException(SR.Cryptography_InvalidPaddingMode);
                PaddingValue = value;
            }
        }

        public virtual ICryptoTransform CreateDecryptor()
        {
            return CreateDecryptor(Key, IV);
        }

        public abstract ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[]? rgbIV);

        public virtual ICryptoTransform CreateEncryptor()
        {
            return CreateEncryptor(Key, IV);
        }

        public abstract ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[]? rgbIV);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Clear()
        {
            (this as IDisposable).Dispose();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (KeyValue != null)
                {
                    Array.Clear(KeyValue);
                    KeyValue = null;
                }
                if (IVValue != null)
                {
                    Array.Clear(IVValue);
                    IVValue = null;
                }
            }
        }

        public abstract void GenerateIV();

        public abstract void GenerateKey();

        public bool ValidKeySize(int bitLength)
        {
            KeySizes[] validSizes = this.LegalKeySizes;
            if (validSizes == null)
                return false;
            return bitLength.IsLegalSize(validSizes);
        }

        /// <summary>
        /// Gets the length of a ciphertext with a given padding mode and plaintext length in ECB mode.
        /// </summary>
        /// <param name="paddingMode">The padding mode used to pad the plaintext to the algorithm's block size.</param>
        /// <param name="plaintextLength">The plaintext length, in bytes.</param>
        /// <returns>The length, in bytes, of the ciphertext with padding.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <para>
        ///   <paramref name="plaintextLength" /> is a negative number.
        ///   </para>
        ///   <para>
        ///   - or -
        ///   </para>
        ///   <para>
        ///   <paramref name="plaintextLength" /> when padded is too large to represent as
        ///   a signed 32-bit integer.
        ///   </para>
        ///   <para>
        ///   - or -
        ///   </para>
        ///   <para>
        ///   <paramref name="paddingMode" /> is not a valid padding mode.
        ///   </para>
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///   <see cref="BlockSize" /> is not a positive integer.
        ///   </para>
        ///   <para>
        ///   - or -
        ///   </para>
        ///   <para>
        ///   <see cref="BlockSize" /> is not a whole number of bytes. It must be divisible by 8.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   The padding mode <see cref="PaddingMode.None" /> was used, but <paramref name="plaintextLength" />
        ///   is not a whole number of blocks.
        ///   </para>
        /// </exception>
        public int GetCiphertextLengthEcb(int plaintextLength, PaddingMode paddingMode) =>
            GetCiphertextLengthBlockAligned(plaintextLength, paddingMode);

        /// <summary>
        /// Gets the length of a ciphertext with a given padding mode and plaintext length in CBC mode.
        /// </summary>
        /// <param name="paddingMode">The padding mode used to pad the plaintext to the algorithm's block size.</param>
        /// <param name="plaintextLength">The plaintext length, in bytes.</param>
        /// <returns>The length, in bytes, of the ciphertext with padding.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <para>
        ///   <paramref name="plaintextLength" /> is a negative number.
        ///   </para>
        ///   <para>
        ///   - or -
        ///   </para>
        ///   <para>
        ///   <paramref name="plaintextLength" /> when padded is too large to represent as
        ///   a signed 32-bit integer.
        ///   </para>
        ///   <para>
        ///   - or -
        ///   </para>
        ///   <para>
        ///   <paramref name="paddingMode" /> is not a valid padding mode.
        ///   </para>
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///   <see cref="BlockSize" /> is not a positive integer.
        ///   </para>
        ///   <para>
        ///   - or -
        ///   </para>
        ///   <para>
        ///   <see cref="BlockSize" /> is not a whole number of bytes. It must be divisible by 8.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   The padding mode <see cref="PaddingMode.None" /> was used, but <paramref name="plaintextLength" />
        ///   is not a whole number of blocks.
        ///   </para>
        /// </exception>
        public int GetCiphertextLengthCbc(int plaintextLength, PaddingMode paddingMode = PaddingMode.PKCS7) =>
            GetCiphertextLengthBlockAligned(plaintextLength, paddingMode);

        private int GetCiphertextLengthBlockAligned(int plaintextLength, PaddingMode paddingMode)
        {
            if (plaintextLength < 0)
                throw new ArgumentOutOfRangeException(nameof(plaintextLength), SR.ArgumentOutOfRange_NeedNonNegNum);

            int blockSizeBits = BlockSize; // The BlockSize property is in bits.

            if (blockSizeBits <= 0 || (blockSizeBits & 0b111) != 0)
                throw new InvalidOperationException(SR.InvalidOperation_UnsupportedBlockSize);

            int blockSizeBytes = blockSizeBits >> 3;
            int wholeBlocks = Math.DivRem(plaintextLength, blockSizeBytes, out int remainder) * blockSizeBytes;

            switch (paddingMode)
            {
                case PaddingMode.None when remainder != 0:
                    throw new ArgumentException(SR.Cryptography_MatchBlockSize, nameof(plaintextLength));
                case PaddingMode.None:
                case PaddingMode.Zeros when remainder == 0:
                    return plaintextLength;
                case PaddingMode.Zeros:
                case PaddingMode.PKCS7:
                case PaddingMode.ANSIX923:
                case PaddingMode.ISO10126:
                    if (int.MaxValue - wholeBlocks < blockSizeBytes)
                    {
                        throw new ArgumentOutOfRangeException(nameof(plaintextLength), SR.Cryptography_PlaintextTooLarge);
                    }

                    return wholeBlocks + blockSizeBytes;
                default:
                    throw new ArgumentOutOfRangeException(nameof(paddingMode), SR.Cryptography_InvalidPaddingMode);
            }
        }

        /// <summary>
        /// Gets the length of a ciphertext with a given padding mode and plaintext length in CFB mode.
        /// </summary>
        /// <param name="paddingMode">The padding mode used to pad the plaintext to the feedback size.</param>
        /// <param name="plaintextLength">The plaintext length, in bytes.</param>
        /// <param name="feedbackSizeInBits">The feedback size, in bits.</param>
        /// <returns>The length, in bytes, of the ciphertext with padding.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <para>
        ///   <paramref name="feedbackSizeInBits" /> is not a positive number.
        ///   </para>
        ///   <para>
        ///   - or -
        ///   </para>
        ///   <para>
        ///   <paramref name="plaintextLength" /> is a negative number.
        ///   </para>
        ///   <para>
        ///   - or -
        ///   </para>
        ///   <para>
        ///   <paramref name="plaintextLength" /> when padded is too large to represent as
        ///   a signed 32-bit integer.
        ///   </para>
        ///   <para>
        ///   - or -
        ///   </para>
        ///   <para>
        ///   <paramref name="paddingMode" /> is not a valid padding mode.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   The padding mode <see cref="PaddingMode.None" /> was used, but <paramref name="plaintextLength" />
        ///   is not a whole number of blocks.
        ///   </para>
        ///   <para>
        ///   - or -
        ///   </para>
        ///   <para>
        ///   <paramref name="feedbackSizeInBits" /> is not a whole number of bytes. It must be divisible by 8.
        ///   </para>
        /// </exception>
        /// <remarks>
        /// <paramref name="feedbackSizeInBits" /> accepts any value that is a valid feedback size, regardless if the algorithm
        /// supports the specified feedback size.
        /// </remarks>
        public int GetCiphertextLengthCfb(int plaintextLength, PaddingMode paddingMode = PaddingMode.None, int feedbackSizeInBits = 8)
        {
            if (plaintextLength < 0)
                throw new ArgumentOutOfRangeException(nameof(plaintextLength), SR.ArgumentOutOfRange_NeedNonNegNum);

            if (feedbackSizeInBits <= 0)
                throw new ArgumentOutOfRangeException(nameof(feedbackSizeInBits), SR.ArgumentOutOfRange_NeedPosNum);

            if ((feedbackSizeInBits & 0b111) != 0)
                throw new ArgumentException(SR.Argument_BitsMustBeWholeBytes, nameof(feedbackSizeInBits));

            int feedbackSizeInBytes = feedbackSizeInBits >> 3;
            int feedbackAligned = Math.DivRem(plaintextLength, feedbackSizeInBytes, out int remainder) * feedbackSizeInBytes;

            switch (paddingMode)
            {
                case PaddingMode.None when remainder != 0:
                    throw new ArgumentException(SR.Cryptography_MatchFeedbackSize, nameof(plaintextLength));
                case PaddingMode.None:
                case PaddingMode.Zeros when remainder == 0:
                    return plaintextLength;
                case PaddingMode.Zeros:
                case PaddingMode.PKCS7:
                case PaddingMode.ANSIX923:
                case PaddingMode.ISO10126:
                    if (int.MaxValue - feedbackAligned < feedbackSizeInBytes)
                    {
                        throw new ArgumentOutOfRangeException(nameof(plaintextLength), SR.Cryptography_PlaintextTooLarge);
                    }

                    return feedbackAligned + feedbackSizeInBytes;
                default:
                    throw new ArgumentOutOfRangeException(nameof(paddingMode), SR.Cryptography_InvalidPaddingMode);
            }
        }

        /// <summary>
        ///   Decrypts data using ECB mode with the specified padding mode.
        /// </summary>
        /// <param name="ciphertext">The data to decrypt.</param>
        /// <param name="paddingMode">The padding mode used to produce the ciphertext and remove during decryption.</param>
        /// <returns>The decrypted plaintext data.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="ciphertext" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="paddingMode" /> is not a valid padding mode.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   The ciphertext could not be decrypted successfully.
        /// </exception>
        /// <remarks>
        ///   This method's behavior is defined by <see cref="TryDecryptEcbCore" />.
        /// </remarks>
        public byte[] DecryptEcb(byte[] ciphertext, PaddingMode paddingMode)
        {
            // Padding mode is validated by callee.
            if (ciphertext is null)
                throw new ArgumentNullException(nameof(ciphertext));

            return DecryptEcb(new ReadOnlySpan<byte>(ciphertext), paddingMode);
        }

        /// <summary>
        ///   Decrypts data using ECB mode with the specified padding mode.
        /// </summary>
        /// <param name="ciphertext">The data to decrypt.</param>
        /// <param name="paddingMode">The padding mode used to produce the ciphertext and remove during decryption.</param>
        /// <returns>The decrypted plaintext data.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="paddingMode" /> is not a valid padding mode.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   The ciphertext could not be decrypted successfully.
        /// </exception>
        /// <remarks>
        ///   This method's behavior is defined by <see cref="TryDecryptEcbCore" />.
        /// </remarks>
        public byte[] DecryptEcb(ReadOnlySpan<byte> ciphertext, PaddingMode paddingMode)
        {
            CheckPaddingMode(paddingMode);

            // This could get returned directly to the caller if we there was no padding
            // that needed to get removed, so don't rent from a pool.
            byte[] decryptBuffer = GC.AllocateUninitializedArray<byte>(ciphertext.Length);

            if (!TryDecryptEcbCore(ciphertext, decryptBuffer, paddingMode, out int written)
                || (uint)written > decryptBuffer.Length)
            {
                // This means decrypting the ciphertext grew in to a larger plaintext or overflowed.
                // A user-derived class could do this, but it is not expected in any of the
                // implementations that we ship.

                throw new CryptographicException(SR.Argument_DestinationTooShort);
            }

            // Array.Resize will no-op if the array does not need to be resized.
            Array.Resize(ref decryptBuffer, written);
            return decryptBuffer;
        }

        /// <summary>
        ///   Decrypts data into the specified buffer, using ECB mode with the specified padding mode.
        /// </summary>
        /// <param name="ciphertext">The data to decrypt.</param>
        /// <param name="destination">The buffer to receive the plaintext data.</param>
        /// <param name="paddingMode">The padding mode used to produce the ciphertext and remove during decryption.</param>
        /// <returns>The total number of bytes written to <paramref name="destination" /></returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="paddingMode" /> is not a valid padding mode.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   The ciphertext could not be decrypted successfully.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   The buffer in <paramref name="destination"/> is too small to hold the plaintext data.
        /// </exception>
        /// <remarks>
        ///   This method's behavior is defined by <see cref="TryDecryptEcbCore" />.
        /// </remarks>
        public int DecryptEcb(ReadOnlySpan<byte> ciphertext, Span<byte> destination, PaddingMode paddingMode)
        {
            CheckPaddingMode(paddingMode);

            if (!TryDecryptEcbCore(ciphertext, destination, paddingMode, out int written))
            {
                throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));
            }

            return written;
        }

        /// <summary>
        ///   Attempts to decrypt data into the specified buffer, using ECB mode with the specified padding mode.
        /// </summary>
        /// <param name="ciphertext">The data to decrypt.</param>
        /// <param name="destination">The buffer to receive the plaintext data.</param>
        /// <param name="paddingMode">The padding mode used to produce the ciphertext and remove during decryption.</param>
        /// <param name="bytesWritten">When this method returns, the total number of bytes written to <paramref name="destination" />.</param>
        /// <returns><see langword="true"/> if <paramref name="destination"/> was large enough to receive the decrypted data; otherwise, <see langword="false" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="paddingMode" /> is not a valid padding mode.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   The ciphertext could not be decrypted successfully.
        /// </exception>
        /// <remarks>
        ///   This method's behavior is defined by <see cref="TryDecryptEcbCore" />.
        /// </remarks>
        public bool TryDecryptEcb(ReadOnlySpan<byte> ciphertext, Span<byte> destination, PaddingMode paddingMode, out int bytesWritten)
        {
            CheckPaddingMode(paddingMode);
            return TryDecryptEcbCore(ciphertext, destination, paddingMode, out bytesWritten);
        }

        /// <summary>
        ///   Encrypts data using ECB mode with the specified padding mode.
        /// </summary>
        /// <param name="plaintext">The data to encrypt.</param>
        /// <param name="paddingMode">The padding mode used to produce the ciphertext and remove during decryption.</param>
        /// <returns>The encrypted ciphertext data.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="plaintext" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="paddingMode" /> is not a valid padding mode.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <see cref="TryEncryptEcbCore" /> could not encrypt the plaintext.
        /// </exception>
        /// <remarks>
        ///   This method's behavior is defined by <see cref="TryEncryptEcbCore" />.
        /// </remarks>
        public byte[] EncryptEcb(byte[] plaintext, PaddingMode paddingMode)
        {
            // paddingMode is validated by callee
            if (plaintext is null)
                throw new ArgumentNullException(nameof(plaintext));

            return EncryptEcb(new ReadOnlySpan<byte>(plaintext), paddingMode);
        }

        /// <summary>
        ///   Encrypts data using ECB mode with the specified padding mode.
        /// </summary>
        /// <param name="plaintext">The data to encrypt.</param>
        /// <param name="paddingMode">The padding mode used to produce the ciphertext and remove during decryption.</param>
        /// <returns>The encrypted ciphertext data.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="paddingMode" /> is not a valid padding mode.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   The plaintext could not be encrypted successfully.
        /// </exception>
        /// <remarks>
        ///   This method's behavior is defined by <see cref="TryEncryptEcbCore" />.
        /// </remarks>
        public byte[] EncryptEcb(ReadOnlySpan<byte> plaintext, PaddingMode paddingMode)
        {
            CheckPaddingMode(paddingMode);

            int ciphertextLength = GetCiphertextLengthEcb(plaintext.Length, paddingMode);

            // We expect most if not all uses to encrypt to exactly the ciphertextLength
            byte[] buffer = GC.AllocateUninitializedArray<byte>(ciphertextLength);

            if (!TryEncryptEcbCore(plaintext, buffer, paddingMode, out int written) ||
                written != ciphertextLength)
            {
                // This means a user-derived imiplementation added more padding than we expected or
                // did something non-standard (encrypt to a partial block). This can't happen for
                // multiple padding blocks since the buffer would have been too small in the first
                // place. It doesn't make sense to try and support partial block encryption, likely
                // something went very wrong. So throw.
                throw new CryptographicException(SR.Format(SR.Cryptography_EncryptedIncorrectLength, nameof(TryEncryptEcbCore)));
            }

            return buffer;
        }

        /// <summary>
        ///   Encrypts data into the specified buffer, using ECB mode with the specified padding mode.
        /// </summary>
        /// <param name="plaintext">The data to encrypt.</param>
        /// <param name="destination">The buffer to receive the ciphertext data.</param>
        /// <param name="paddingMode">The padding mode used to produce the ciphertext and remove during decryption.</param>
        /// <returns>The total number of bytes written to <paramref name="destination" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="paddingMode" /> is not a valid padding mode.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   The plaintext could not be encrypted successfully.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   The buffer in <paramref name="destination"/> is too small to hold the ciphertext data.
        /// </exception>
        /// <remarks>
        ///   This method's behavior is defined by <see cref="TryEncryptEcbCore" />.
        /// </remarks>
        public int EncryptEcb(ReadOnlySpan<byte> plaintext, Span<byte> destination, PaddingMode paddingMode)
        {
            CheckPaddingMode(paddingMode);

            if (!TryEncryptEcbCore(plaintext, destination, paddingMode, out int written))
            {
                throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));
            }

            return written;
        }

        /// <summary>
        ///   Attempts to encrypt data into the specified buffer, using ECB mode with the specified padding mode.
        /// </summary>
        /// <param name="plaintext">The data to encrypt.</param>
        /// <param name="destination">The buffer to receive the ciphertext data.</param>
        /// <param name="paddingMode">The padding mode used to produce the ciphertext and remove during decryption.</param>
        /// <param name="bytesWritten">When this method returns, the total number of bytes written to <paramref name="destination" />.</param>
        /// <returns><see langword="true"/> if <paramref name="destination"/> was large enough to receive the encrypted data; otherwise, <see langword="false" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="paddingMode" /> is not a valid padding mode.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   The plaintext could not be encrypted successfully.
        /// </exception>
        /// <remarks>
        ///   This method's behavior is defined by <see cref="TryEncryptEcbCore" />.
        /// </remarks>
        public bool TryEncryptEcb(ReadOnlySpan<byte> plaintext, Span<byte> destination, PaddingMode paddingMode, out int bytesWritten)
        {
            CheckPaddingMode(paddingMode);
            return TryEncryptEcbCore(plaintext, destination, paddingMode, out bytesWritten);
        }

        /// <summary>
        ///   When overridden in a derived class, attempts to encrypt data into the specified
        ///   buffer, using ECB mode with the specified padding mode.
        /// </summary>
        /// <param name="plaintext">The data to encrypt.</param>
        /// <param name="destination">The buffer to receive the ciphertext data.</param>
        /// <param name="paddingMode">The padding mode used to produce the ciphertext and remove during decryption.</param>
        /// <param name="bytesWritten">When this method returns, the total number of bytes written to <paramref name="destination" />.</param>
        /// <returns><see langword="true"/> if <paramref name="destination"/> was large enough to receive the encrypted data; otherwise, <see langword="false" />.</returns>
        /// <exception cref="NotSupportedException">
        ///   A derived class has not provided an implementation.
        /// </exception>
        /// <remarks>
        ///   <para>Derived classes must override this and provide an implementation.</para>
        ///   <para>
        ///      Implementations of this method must write precisely
        ///      <c>GetCiphertextLengthEcb(plaintext.Length, paddingMode)</c> bytes to <paramref name="destination"/>
        ///      and report that via <paramref name="bytesWritten"/>.
        ///   </para>
        /// </remarks>
        protected virtual bool TryEncryptEcbCore(
            ReadOnlySpan<byte> plaintext,
            Span<byte> destination,
            PaddingMode paddingMode,
            out int bytesWritten)
        {
            throw new NotSupportedException(SR.NotSupported_SubclassOverride);
        }

        /// <summary>
        ///   When overridden in a derived class, attempts to decrypt data
        ///   into the specified buffer, using ECB mode with the specified padding mode.
        /// </summary>
        /// <param name="ciphertext">The data to decrypt.</param>
        /// <param name="destination">The buffer to receive the plaintext data.</param>
        /// <param name="paddingMode">The padding mode used to produce the ciphertext and remove during decryption.</param>
        /// <param name="bytesWritten">When this method returns, the total number of bytes written to <paramref name="destination" />.</param>
        /// <returns><see langword="true"/> if <paramref name="destination"/> was large enough to receive the decrypted data; otherwise, <see langword="false" />.</returns>
        /// <exception cref="NotSupportedException">
        ///   A derived class has not provided an implementation.
        /// </exception>
        /// <remarks>
        ///   Derived classes must override this and provide an implementation.
        /// </remarks>
        protected virtual bool TryDecryptEcbCore(
            ReadOnlySpan<byte> ciphertext,
            Span<byte> destination,
            PaddingMode paddingMode,
            out int bytesWritten)
        {
            throw new NotSupportedException(SR.NotSupported_SubclassOverride);
        }

        private static void CheckPaddingMode(PaddingMode paddingMode)
        {
            if (paddingMode < PaddingMode.None || paddingMode > PaddingMode.ISO10126)
                throw new ArgumentOutOfRangeException(nameof(paddingMode), SR.Cryptography_InvalidPaddingMode);
        }

        protected CipherMode ModeValue;
        protected PaddingMode PaddingValue;
        protected byte[]? KeyValue;
        protected byte[]? IVValue;
        protected int BlockSizeValue;
        protected int FeedbackSizeValue;
        protected int KeySizeValue;
        [MaybeNull] protected KeySizes[] LegalBlockSizesValue = null!;
        [MaybeNull] protected KeySizes[] LegalKeySizesValue = null!;
    }
}
