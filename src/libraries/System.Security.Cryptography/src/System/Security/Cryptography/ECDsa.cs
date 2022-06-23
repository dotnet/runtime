// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Cryptography;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Versioning;

namespace System.Security.Cryptography
{
    public abstract partial class ECDsa : ECAlgorithm
    {
        // secp521r1 maxes out at 139 bytes in the DER format, so 256 should always be enough
        private const int SignatureStackBufSize = 256;
        // The biggest supported hash algorithm is SHA-2-512, which is only 64 bytes.
        // One power of two bigger should cover most unknown algorithms, too.
        private const int HashBufferStackSize = 128;

        protected ECDsa() { }

        [UnsupportedOSPlatform("browser")]
        public static new partial ECDsa Create();

        [UnsupportedOSPlatform("browser")]
        public static partial ECDsa Create(ECCurve curve);

        [UnsupportedOSPlatform("browser")]
        public static partial ECDsa Create(ECParameters parameters);

        [RequiresUnreferencedCode(CryptoConfig.CreateFromNameUnreferencedCodeMessage)]
        public static new ECDsa? Create(string algorithm)
        {
            ArgumentNullException.ThrowIfNull(algorithm);

            return CryptoConfig.CreateFromName(algorithm) as ECDsa;
        }

        public virtual byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm)
        {
            ArgumentNullException.ThrowIfNull(data);

            // hashAlgorithm is verified in the overload

            return SignData(data, 0, data.Length, hashAlgorithm);
        }

        public virtual byte[] SignData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
        {
            ArgumentNullException.ThrowIfNull(data);
            if (offset < 0 || offset > data.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || count > data.Length - offset)
                throw new ArgumentOutOfRangeException(nameof(count));
            ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, nameof(hashAlgorithm));

            byte[] hash = HashData(data, offset, count, hashAlgorithm);
            return SignHash(hash);
        }

        /// <summary>
        ///   Computes the hash value of the specified data and signs it using the specified signature format.
        /// </summary>
        /// <param name="data">The data to sign.</param>
        /// <param name="offset">The offset into <paramref name="data"/> at which to begin hashing.</param>
        /// <param name="count">The number of bytes to read from <paramref name="data"/>.</param>
        /// <param name="hashAlgorithm">The hash algorithm to use to create the hash value.</param>
        /// <param name="signatureFormat">The encoding format to use for the signature.</param>
        /// <returns>
        ///   The ECDSA signature for the specified data.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="data"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="signatureFormat"/> is not a known format.
        ///
        ///   -or-
        ///
        ///   <paramref name="offset" /> is less than zero.
        ///
        ///   -or-
        ///
        ///   <paramref name="count" /> is less than zero.
        ///
        ///   -or-
        ///
        ///   <paramref name="offset" /> + <paramref name="count"/> - 1 results in an index that is
        ///   beyond the upper bound of <paramref name="data"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="hashAlgorithm"/> has a <see langword="null"/> or empty <see cref="HashAlgorithmName.Name"/>.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred in the hashing or signing operation.
        /// </exception>
        public byte[] SignData(
            byte[] data,
            int offset,
            int count,
            HashAlgorithmName hashAlgorithm,
            DSASignatureFormat signatureFormat)
        {
            ArgumentNullException.ThrowIfNull(data);
            if (offset < 0 || offset > data.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || count > data.Length - offset)
                throw new ArgumentOutOfRangeException(nameof(count));
            ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, nameof(hashAlgorithm));
            if (!signatureFormat.IsKnownValue())
                throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);

            return SignDataCore(new ReadOnlySpan<byte>(data, offset, count), hashAlgorithm, signatureFormat);
        }

        /// <summary>
        ///   Computes the hash value of the specified data and signs it using the specified signature format.
        /// </summary>
        /// <param name="data">The data to sign.</param>
        /// <param name="hashAlgorithm">The hash algorithm to use to create the hash value.</param>
        /// <param name="signatureFormat">The encoding format to use for the signature.</param>
        /// <returns>
        ///   The ECDSA signature for the specified data.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   An error occurred in the hashing or signing operation.
        /// </exception>
        protected virtual byte[] SignDataCore(
            ReadOnlySpan<byte> data,
            HashAlgorithmName hashAlgorithm,
            DSASignatureFormat signatureFormat)
        {
            Span<byte> signature = stackalloc byte[SignatureStackBufSize];
            int maxSignatureSize = GetMaxSignatureSize(signatureFormat);
            byte[]? rented = null;
            bool returnArray = false;
            int bytesWritten = 0;

            if (maxSignatureSize > signature.Length)
            {
                // Use the shared pool because the buffer is passed out.
                rented = ArrayPool<byte>.Shared.Rent(maxSignatureSize);
                signature = rented;
            }

            try
            {
                if (!TrySignDataCore(data, signature, hashAlgorithm, signatureFormat, out bytesWritten))
                {
                    Debug.Fail($"GetMaxSignatureSize returned insufficient size for format {signatureFormat}");
                    throw new CryptographicException();
                }

                byte[] ret = signature.Slice(0, bytesWritten).ToArray();
                returnArray = true;
                return ret;
            }
            finally
            {
                if (rented != null)
                {
                    CryptographicOperations.ZeroMemory(rented.AsSpan(0, bytesWritten));

                    if (returnArray)
                    {
                        ArrayPool<byte>.Shared.Return(rented);
                    }
                }
            }
        }

        /// <summary>
        ///   Computes the hash value of the specified data and signs it using the specified signature format.
        /// </summary>
        /// <param name="data">The data to sign.</param>
        /// <param name="hashAlgorithm">The hash algorithm to use to create the hash value.</param>
        /// <param name="signatureFormat">The encoding format to use for the signature.</param>
        /// <returns>
        ///   The ECDSA signature for the specified data.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="data"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="signatureFormat"/> is not a known format.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="hashAlgorithm"/> has a <see langword="null"/> or empty <see cref="HashAlgorithmName.Name"/>.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred in the hashing or signing operation.
        /// </exception>
        public byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat)
        {
            ArgumentNullException.ThrowIfNull(data);
            ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, nameof(hashAlgorithm));
            if (!signatureFormat.IsKnownValue())
                throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);

            return SignDataCore(data, hashAlgorithm, signatureFormat);
        }

        /// <summary>
        ///   Computes the hash value of the specified data and signs it using the specified signature format.
        /// </summary>
        /// <param name="data">The data to sign.</param>
        /// <param name="hashAlgorithm">The hash algorithm to use to create the hash value.</param>
        /// <param name="signatureFormat">The encoding format to use for the signature.</param>
        /// <returns>
        ///   The ECDSA signature for the specified data.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="data"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="signatureFormat"/> is not a known format.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="hashAlgorithm"/> has a <see langword="null"/> or empty <see cref="HashAlgorithmName.Name"/>.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred in the hashing or signing operation.
        /// </exception>
        public byte[] SignData(Stream data, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat)
        {
            ArgumentNullException.ThrowIfNull(data);
            ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, nameof(hashAlgorithm));
            if (!signatureFormat.IsKnownValue())
                throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);

            return SignDataCore(data, hashAlgorithm, signatureFormat);
        }

        /// <summary>
        ///   Computes the hash value of the specified data and signs it using the specified signature format.
        /// </summary>
        /// <param name="data">The data to sign.</param>
        /// <param name="hashAlgorithm">The hash algorithm to use to create the hash value.</param>
        /// <param name="signatureFormat">The encoding format to use for the signature.</param>
        /// <returns>
        ///   The ECDSA signature for the specified data.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   An error occurred in the hashing or signing operation.
        /// </exception>
        protected virtual byte[] SignDataCore(
            Stream data,
            HashAlgorithmName hashAlgorithm,
            DSASignatureFormat signatureFormat)
        {
            byte[] hash = HashData(data, hashAlgorithm);
            return SignHashCore(hash, signatureFormat);
        }

        /// <summary>
        ///   Computes the ECDSA signature for the specified hash value in the indicated format.
        /// </summary>
        /// <param name="hash">The hash value to sign.</param>
        /// <param name="signatureFormat">The encoding format to use for the signature.</param>
        /// <returns>
        ///   The ECDSA signature for the specified data.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="hash"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="signatureFormat"/> is not a known format.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred in the signing operation.
        /// </exception>
        public byte[] SignHash(byte[] hash, DSASignatureFormat signatureFormat)
        {
            ArgumentNullException.ThrowIfNull(hash);

            if (!signatureFormat.IsKnownValue())
                throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);

            return SignHashCore(hash, signatureFormat);
        }

        /// <summary>
        ///   Computes the ECDSA signature for the specified hash value in the indicated format.
        /// </summary>
        /// <param name="hash">The hash value to sign.</param>
        /// <param name="signatureFormat">The encoding format to use for the signature.</param>
        /// <returns>
        ///   The ECDSA signature for the specified data.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   An error occurred in the signing operation.
        /// </exception>
        protected virtual byte[] SignHashCore(ReadOnlySpan<byte> hash, DSASignatureFormat signatureFormat)
        {
            Span<byte> signature = stackalloc byte[SignatureStackBufSize];
            int maxSignatureSize = GetMaxSignatureSize(signatureFormat);
            byte[]? rented = null;
            bool returnArray = false;
            int bytesWritten = 0;

            if (maxSignatureSize > signature.Length)
            {
                // Use the shared pool because the buffer is passed out.
                rented = ArrayPool<byte>.Shared.Rent(maxSignatureSize);
                signature = rented;
            }

            try
            {
                if (!TrySignHashCore(hash, signature, signatureFormat, out bytesWritten))
                {
                    Debug.Fail($"GetMaxSignatureSize returned insufficient size for format {signatureFormat}");
                    throw new CryptographicException();
                }

                byte[] ret = signature.Slice(0, bytesWritten).ToArray();
                returnArray = true;
                return ret;
            }
            finally
            {
                if (rented != null)
                {
                    CryptographicOperations.ZeroMemory(rented.AsSpan(0, bytesWritten));

                    if (returnArray)
                    {
                        ArrayPool<byte>.Shared.Return(rented);
                    }
                }
            }
        }

        public virtual bool TrySignData(
            ReadOnlySpan<byte> data,
            Span<byte> destination,
            HashAlgorithmName hashAlgorithm,
            out int bytesWritten)
        {
            ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, nameof(hashAlgorithm));

            Span<byte> hashTmp = stackalloc byte[HashBufferStackSize];
            ReadOnlySpan<byte> hash = HashSpanToTmp(data, hashAlgorithm, hashTmp);
            return TrySignHash(hash, destination, out bytesWritten);
        }

        /// <summary>
        ///   Attempts to create the ECDSA signature for the specified data in the indicated format
        ///   into the provided buffer.
        /// </summary>
        /// <param name="data">The data to hash and sign.</param>
        /// <param name="destination">The buffer to receive the signature.</param>
        /// <param name="hashAlgorithm">The hash algorithm to use to create the hash value.</param>
        /// <param name="signatureFormat">The encoding format to use for the signature.</param>
        /// <param name="bytesWritten">
        ///   When this method returns, contains a value that indicates the number of bytes written to
        ///   <paramref name="destination"/>. This parameter is treated as uninitialized.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if <paramref name="destination"/> is big enough to receive the signature;
        ///   otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="signatureFormat"/> is not a known format.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="hashAlgorithm"/> has a <see langword="null"/> or empty <see cref="HashAlgorithmName.Name"/>.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred in the signing operation.
        /// </exception>
        public bool TrySignData(
            ReadOnlySpan<byte> data,
            Span<byte> destination,
            HashAlgorithmName hashAlgorithm,
            DSASignatureFormat signatureFormat,
            out int bytesWritten)
        {
            ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, nameof(hashAlgorithm));
            if (!signatureFormat.IsKnownValue())
                throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);

            return TrySignDataCore(data, destination, hashAlgorithm, signatureFormat, out bytesWritten);
        }

        /// <summary>
        ///   Attempts to create the ECDSA signature for the specified data in the indicated format
        ///   into the provided buffer.
        /// </summary>
        /// <param name="data">The data to hash and sign.</param>
        /// <param name="destination">The buffer to receive the signature.</param>
        /// <param name="hashAlgorithm">The hash algorithm to use to create the hash value.</param>
        /// <param name="signatureFormat">The encoding format to use for the signature.</param>
        /// <param name="bytesWritten">
        ///   When this method returns, contains a value that indicates the number of bytes written to
        ///   <paramref name="destination"/>. This parameter is treated as uninitialized.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if <paramref name="destination"/> is big enough to receive the signature;
        ///   otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   An error occurred in the signing operation.
        /// </exception>
        protected virtual bool TrySignDataCore(
            ReadOnlySpan<byte> data,
            Span<byte> destination,
            HashAlgorithmName hashAlgorithm,
            DSASignatureFormat signatureFormat,
            out int bytesWritten)
        {
            Span<byte> hashTmp = stackalloc byte[HashBufferStackSize];
            ReadOnlySpan<byte> hash = HashSpanToTmp(data, hashAlgorithm, hashTmp);

            return TrySignHashCore(hash, destination, signatureFormat, out bytesWritten);
        }

        public virtual byte[] SignData(Stream data, HashAlgorithmName hashAlgorithm)
        {
            ArgumentNullException.ThrowIfNull(data);
            ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, nameof(hashAlgorithm));

            byte[] hash = HashData(data, hashAlgorithm);
            return SignHash(hash);
        }

        public bool VerifyData(byte[] data, byte[] signature, HashAlgorithmName hashAlgorithm)
        {
            ArgumentNullException.ThrowIfNull(data);

            return VerifyData(data, 0, data.Length, signature, hashAlgorithm);
        }

        public virtual bool VerifyData(byte[] data, int offset, int count, byte[] signature, HashAlgorithmName hashAlgorithm)
        {
            ArgumentNullException.ThrowIfNull(data);
            if (offset < 0 || offset > data.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || count > data.Length - offset)
                throw new ArgumentOutOfRangeException(nameof(count));
            ArgumentNullException.ThrowIfNull(signature);
            ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, nameof(hashAlgorithm));

            byte[] hash = HashData(data, offset, count, hashAlgorithm);
            return VerifyHash(hash, signature);
        }

        /// <summary>
        ///   Verifies that a digital signature is valid for the provided data.
        /// </summary>
        /// <param name="data">An array that contains the signed data.</param>
        /// <param name="offset">The starting index of the signed portion of <paramref name="data"/>.</param>
        /// <param name="count">The number of bytes in <paramref name="data"/> that were signed.</param>
        /// <param name="signature">The signature to verify.</param>
        /// <param name="hashAlgorithm">The hash algorithm used to hash the data for the verification process.</param>
        /// <param name="signatureFormat">The encoding format for <paramref name="signature"/>.</param>
        /// <returns>
        ///   <see langword="true"/> if the digital signature is valid for the provided data; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="data"/> or <paramref name="signature"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="signatureFormat"/> is not a known format.
        ///
        ///   -or-
        ///
        ///   <paramref name="offset" /> is less than zero.
        ///
        ///   -or-
        ///
        ///   <paramref name="count" /> is less than zero.
        ///
        ///   -or-
        ///
        ///   <paramref name="offset" /> + <paramref name="count"/> - 1 results in an index that is
        ///   beyond the upper bound of <paramref name="data"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="hashAlgorithm"/> has a <see langword="null"/> or empty <see cref="HashAlgorithmName.Name"/>.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred in the hashing or verification operation.
        /// </exception>
        public bool VerifyData(
            byte[] data,
            int offset,
            int count,
            byte[] signature,
            HashAlgorithmName hashAlgorithm,
            DSASignatureFormat signatureFormat)
        {
            ArgumentNullException.ThrowIfNull(data);
            if (offset < 0 || offset > data.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || count > data.Length - offset)
                throw new ArgumentOutOfRangeException(nameof(count));
            ArgumentNullException.ThrowIfNull(signature);
            ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, nameof(hashAlgorithm));
            if (!signatureFormat.IsKnownValue())
                throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);

            return VerifyDataCore(
                new ReadOnlySpan<byte>(data, offset, count),
                signature,
                hashAlgorithm,
                signatureFormat);
        }

        /// <summary>
        ///   Verifies that a digital signature is valid for the provided data.
        /// </summary>
        /// <param name="data">The signed data.</param>
        /// <param name="signature">The signature to verify.</param>
        /// <param name="hashAlgorithm">The hash algorithm used to hash the data for the verification process.</param>
        /// <param name="signatureFormat">The encoding format for <paramref name="signature"/>.</param>
        /// <returns>
        ///   <see langword="true"/> if the digital signature is valid for the provided data; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="data"/> or <paramref name="signature"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="signatureFormat"/> is not a known format.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="hashAlgorithm"/> has a <see langword="null"/> or empty <see cref="HashAlgorithmName.Name"/>.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred in the hashing or verification operation.
        /// </exception>
        public bool VerifyData(byte[] data, byte[] signature, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat)
        {
            ArgumentNullException.ThrowIfNull(data);
            ArgumentNullException.ThrowIfNull(signature);
            ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, nameof(hashAlgorithm));
            if (!signatureFormat.IsKnownValue())
                throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);

            return VerifyDataCore(data, signature, hashAlgorithm, signatureFormat);
        }

        public virtual bool VerifyData(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature, HashAlgorithmName hashAlgorithm)
        {
            ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, nameof(hashAlgorithm));

            Span<byte> hashTmp = stackalloc byte[HashBufferStackSize];
            ReadOnlySpan<byte> hash = HashSpanToTmp(data, hashAlgorithm, hashTmp);
            return VerifyHash(hash, signature);
        }

        /// <summary>
        ///   Verifies that a digital signature is valid for the provided data.
        /// </summary>
        /// <param name="data">The signed data.</param>
        /// <param name="signature">The signature to verify.</param>
        /// <param name="hashAlgorithm">The hash algorithm used to hash the data for the verification process.</param>
        /// <param name="signatureFormat">The encoding format for <paramref name="signature"/>.</param>
        /// <returns>
        ///   <see langword="true"/> if the digital signature is valid for the provided data; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="signatureFormat"/> is not a known format.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred in the hashing or verification operation.
        /// </exception>
        public bool VerifyData(
            ReadOnlySpan<byte> data,
            ReadOnlySpan<byte> signature,
            HashAlgorithmName hashAlgorithm,
            DSASignatureFormat signatureFormat)
        {
            ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, nameof(hashAlgorithm));
            if (!signatureFormat.IsKnownValue())
                throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);

            return VerifyDataCore(data, signature, hashAlgorithm, signatureFormat);
        }

        /// <summary>
        ///   Verifies that a digital signature is valid for the provided data.
        /// </summary>
        /// <param name="data">The signed data.</param>
        /// <param name="signature">The signature to verify.</param>
        /// <param name="hashAlgorithm">The hash algorithm used to hash the data for the verification process.</param>
        /// <param name="signatureFormat">The encoding format for <paramref name="signature"/>.</param>
        /// <returns>
        ///   <see langword="true"/> if the digital signature is valid for the provided data; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   An error occurred in the hashing or verification operation.
        /// </exception>
        protected virtual bool VerifyDataCore(
            ReadOnlySpan<byte> data,
            ReadOnlySpan<byte> signature,
            HashAlgorithmName hashAlgorithm,
            DSASignatureFormat signatureFormat)
        {
            // SHA-2-512 is the biggest hash we know about.
            Span<byte> hashSpan = stackalloc byte[512 / 8];

            if (TryHashData(data, hashSpan, hashAlgorithm, out int bytesWritten))
            {
                hashSpan = hashSpan.Slice(0, bytesWritten);
            }
            else
            {
                // TryHashData didn't work, the algorithm must be exotic,
                // call the array-returning variant.
                hashSpan = HashData(data.ToArray(), 0, data.Length, hashAlgorithm);
            }

            return VerifyHashCore(hashSpan, signature, signatureFormat);
        }

        public bool VerifyData(Stream data, byte[] signature, HashAlgorithmName hashAlgorithm)
        {
            ArgumentNullException.ThrowIfNull(data);
            ArgumentNullException.ThrowIfNull(signature);
            ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, nameof(hashAlgorithm));

            byte[] hash = HashData(data, hashAlgorithm);
            return VerifyHash(hash, signature);
        }

        /// <summary>
        ///   Verifies that a digital signature is valid for the provided data.
        /// </summary>
        /// <param name="data">The signed data.</param>
        /// <param name="signature">The signature to verify.</param>
        /// <param name="hashAlgorithm">The hash algorithm used to hash the data for the verification process.</param>
        /// <param name="signatureFormat">The encoding format for <paramref name="signature"/>.</param>
        /// <returns>
        ///   <see langword="true"/> if the digital signature is valid for the provided data; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="data"/> or <paramref name="signature"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="signatureFormat"/> is not a known format.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="hashAlgorithm"/> has a <see langword="null"/> or empty <see cref="HashAlgorithmName.Name"/>.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred in the hashing or verification operation.
        /// </exception>
        public bool VerifyData(
            Stream data,
            byte[] signature,
            HashAlgorithmName hashAlgorithm,
            DSASignatureFormat signatureFormat)
        {
            ArgumentNullException.ThrowIfNull(data);
            ArgumentNullException.ThrowIfNull(signature);

            ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, nameof(hashAlgorithm));
            if (!signatureFormat.IsKnownValue())
                throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);

            return VerifyDataCore(data, signature, hashAlgorithm, signatureFormat);
        }

        /// <summary>
        ///   Verifies that a digital signature is valid for the provided data.
        /// </summary>
        /// <param name="data">The signed data.</param>
        /// <param name="signature">The signature to verify.</param>
        /// <param name="hashAlgorithm">The hash algorithm used to hash the data for the verification process.</param>
        /// <param name="signatureFormat">The encoding format for <paramref name="signature"/>.</param>
        /// <returns>
        ///   <see langword="true"/> if the digital signature is valid for the provided data; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   An error occurred in the hashing or verification operation.
        /// </exception>
        protected virtual bool VerifyDataCore(
            Stream data,
            ReadOnlySpan<byte> signature,
            HashAlgorithmName hashAlgorithm,
            DSASignatureFormat signatureFormat)
        {
            byte[] hash = HashData(data, hashAlgorithm);
            return VerifyHashCore(hash, signature, signatureFormat);
        }

        public abstract byte[] SignHash(byte[] hash);
        public abstract bool VerifyHash(byte[] hash, byte[] signature);

        public override string? KeyExchangeAlgorithm => null;
        public override string SignatureAlgorithm => "ECDsa";

        protected virtual byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm) =>
            HashOneShotHelpers.HashData(hashAlgorithm, new ReadOnlySpan<byte>(data, offset, count));

        protected virtual byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm) =>
            HashOneShotHelpers.HashData(hashAlgorithm, data);

        protected virtual bool TryHashData(ReadOnlySpan<byte> data, Span<byte> destination, HashAlgorithmName hashAlgorithm, out int bytesWritten)
        {
            // If this is an algorithm that we ship, then we can use the hash one-shot.
            if (this is IRuntimeAlgorithm)
            {
                return HashOneShotHelpers.TryHashData(hashAlgorithm, data, destination, out bytesWritten);
            }

            // If this is not our algorithm implementation, for compatibility purposes we need to
            // call out to the HashData virtual.
            // Use ArrayPool.Shared instead of CryptoPool because the array is passed out.
            byte[] array = ArrayPool<byte>.Shared.Rent(data.Length);
            bool returnArray = false;

            try
            {
                data.CopyTo(array);
                byte[] hash = HashData(array, 0, data.Length, hashAlgorithm);
                returnArray = true;

                if (hash.Length <= destination.Length)
                {
                    new ReadOnlySpan<byte>(hash).CopyTo(destination);
                    bytesWritten = hash.Length;
                    return true;
                }
                else
                {
                    bytesWritten = 0;
                    return false;
                }
            }
            finally
            {
                Array.Clear(array, 0, data.Length);

                if (returnArray)
                {
                    ArrayPool<byte>.Shared.Return(array);
                }
            }
        }

        public virtual bool TrySignHash(ReadOnlySpan<byte> hash, Span<byte> destination, out int bytesWritten)
            => TrySignHashCore(hash, destination, DSASignatureFormat.IeeeP1363FixedFieldConcatenation, out bytesWritten);

        /// <summary>
        ///   Attempts to create the ECDSA signature for the specified hash value in the indicated format
        ///   into the provided buffer.
        /// </summary>
        /// <param name="hash">The hash value to sign.</param>
        /// <param name="destination">The buffer to receive the signature.</param>
        /// <param name="signatureFormat">The encoding format to use for the signature.</param>
        /// <param name="bytesWritten">
        ///   When this method returns, contains a value that indicates the number of bytes written to
        ///   <paramref name="destination"/>. This parameter is treated as uninitialized.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if <paramref name="destination"/> is big enough to receive the signature;
        ///   otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="signatureFormat"/> is not a known format.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred in the signing operation.
        /// </exception>
        public bool TrySignHash(
            ReadOnlySpan<byte> hash,
            Span<byte> destination,
            DSASignatureFormat signatureFormat,
            out int bytesWritten)
        {
            if (!signatureFormat.IsKnownValue())
                throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);

            return TrySignHashCore(hash, destination, signatureFormat, out bytesWritten);
        }

        /// <summary>
        ///   Attempts to create the ECDSA signature for the specified hash value in the indicated format
        ///   into the provided buffer.
        /// </summary>
        /// <param name="hash">The hash value to sign.</param>
        /// <param name="destination">The buffer to receive the signature.</param>
        /// <param name="signatureFormat">The encoding format to use for the signature.</param>
        /// <param name="bytesWritten">
        ///   When this method returns, contains a value that indicates the number of bytes written to
        ///   <paramref name="destination"/>. This parameter is treated as uninitialized.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if <paramref name="destination"/> is big enough to receive the signature;
        ///   otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   An error occurred in the signing operation.
        /// </exception>
        protected virtual bool TrySignHashCore(
            ReadOnlySpan<byte> hash,
            Span<byte> destination,
            DSASignatureFormat signatureFormat,
            out int bytesWritten)
        {
            // This method is expected to be overriden with better implementation

            // The only available implementation here is abstract method, use it
            byte[] result = SignHash(hash.ToArray());
            byte[] converted = AsymmetricAlgorithmHelpers.ConvertFromIeeeP1363Signature(result, signatureFormat);
            return Helpers.TryCopyToDestination(converted, destination, out bytesWritten);
        }

        public virtual bool VerifyHash(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature) =>
            VerifyHashCore(hash, signature, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);

        /// <summary>
        ///   Verifies that a digital signature is valid for the provided hash.
        /// </summary>
        /// <param name="hash">The signed hash.</param>
        /// <param name="signature">The signature to verify.</param>
        /// <param name="signatureFormat">The encoding format for <paramref name="signature"/>.</param>
        /// <returns>
        ///   <see langword="true"/> if the digital signature is valid for the provided data; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="hash"/> or <paramref name="signature"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="signatureFormat"/> is not a known format.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred in the verification operation.
        /// </exception>
        public bool VerifyHash(byte[] hash, byte[] signature, DSASignatureFormat signatureFormat)
        {
            ArgumentNullException.ThrowIfNull(hash);
            ArgumentNullException.ThrowIfNull(signature);

            if (!signatureFormat.IsKnownValue())
                throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);

            return VerifyHashCore(hash, signature, signatureFormat);
        }

        /// <summary>
        ///   Verifies that a digital signature is valid for the provided hash.
        /// </summary>
        /// <param name="hash">The signed hash.</param>
        /// <param name="signature">The signature to verify.</param>
        /// <param name="signatureFormat">The encoding format for <paramref name="signature"/>.</param>
        /// <returns>
        ///   <see langword="true"/> if the digital signature is valid for the provided data; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="signatureFormat"/> is not a known format.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred in the verification operation.
        /// </exception>
        public bool VerifyHash(
            ReadOnlySpan<byte> hash,
            ReadOnlySpan<byte> signature,
            DSASignatureFormat signatureFormat)
        {
            if (!signatureFormat.IsKnownValue())
                throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);

            return VerifyHashCore(hash, signature, signatureFormat);
        }

        /// <summary>
        ///   Verifies that a digital signature is valid for the provided hash.
        /// </summary>
        /// <param name="hash">The signed hash.</param>
        /// <param name="signature">The signature to verify.</param>
        /// <param name="signatureFormat">The encoding format for <paramref name="signature"/>.</param>
        /// <returns>
        ///   <see langword="true"/> if the digital signature is valid for the provided data; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   An error occurred in the verification operation.
        /// </exception>
        protected virtual bool VerifyHashCore(
            ReadOnlySpan<byte> hash,
            ReadOnlySpan<byte> signature,
            DSASignatureFormat signatureFormat)
        {
            // This method is expected to be overriden with better implementation

            byte[]? sig = this.ConvertSignatureToIeeeP1363(signatureFormat, signature);

            // If the signature failed normalization to P1363, it obviously doesn't verify.
            if (sig == null)
            {
                return false;
            }

            // The only available implementation here is abstract method, use it
            return VerifyHash(hash.ToArray(), sig);
        }

        private ReadOnlySpan<byte> HashSpanToTmp(
            ReadOnlySpan<byte> data,
            HashAlgorithmName hashAlgorithm,
            Span<byte> tmp)
        {
            Debug.Assert(tmp.Length == HashBufferStackSize);

            if (TryHashData(data, tmp, hashAlgorithm, out int hashSize))
            {
                return tmp.Slice(0, hashSize);
            }

            // This is not expected, but a poor virtual implementation of TryHashData,
            // or an exotic new algorithm, will hit this fallback.
            return HashSpanToArray(data, hashAlgorithm);
        }

        private byte[] HashSpanToArray(ReadOnlySpan<byte> data, HashAlgorithmName hashAlgorithm)
        {
            // Use ArrayPool.Shared instead of CryptoPool because the array is passed out.
            byte[] array = ArrayPool<byte>.Shared.Rent(data.Length);
            bool returnArray = false;
            try
            {
                data.CopyTo(array);
                byte[] ret = HashData(array, 0, data.Length, hashAlgorithm);
                returnArray = true;
                return ret;
            }
            finally
            {
                Array.Clear(array, 0, data.Length);

                if (returnArray)
                {
                    ArrayPool<byte>.Shared.Return(array);
                }
            }
        }

        /// <summary>
        ///   Gets the largest size, in bytes, for a signature produced by this key in the indicated format.
        /// </summary>
        /// <param name="signatureFormat">The encoding format for a signature.</param>
        /// <returns>
        ///   The largest size, in bytes, for a signature produced by this key in the indicated format.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="signatureFormat"/> is not a known format.
        /// </exception>
        public int GetMaxSignatureSize(DSASignatureFormat signatureFormat)
        {
            int fieldSizeBits = KeySize;

            if (fieldSizeBits == 0)
            {
                // Coerce the key/key-size into existence
                ExportParameters(false);

                fieldSizeBits = KeySize;

                // This implementation of ECDsa doesn't set KeySize, we can't
                if (fieldSizeBits == 0)
                {
                    throw new NotSupportedException(SR.Cryptography_InvalidKeySize);
                }
            }

            switch (signatureFormat)
            {
                case DSASignatureFormat.IeeeP1363FixedFieldConcatenation:
                    return AsymmetricAlgorithmHelpers.BitsToBytes(fieldSizeBits) * 2;
                case DSASignatureFormat.Rfc3279DerSequence:
                    return AsymmetricAlgorithmHelpers.GetMaxDerSignatureSize(fieldSizeBits);
                default:
                    throw new ArgumentOutOfRangeException(nameof(signatureFormat));
            }
        }
    }
}
