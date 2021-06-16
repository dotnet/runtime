// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
using System.IO;
using System.Runtime.Versioning;
using System.Security.Cryptography.Asn1;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    [UnsupportedOSPlatform("browser")]
    public abstract partial class DSA : AsymmetricAlgorithm
    {
        // As of FIPS 186-4 the maximum Q size is 256 bits (32 bytes).
        // The DER signature format thus maxes out at 2 + 3 + 33 + 3 + 33 => 74 bytes.
        // So 128 should always work.
        private const int SignatureStackSize = 128;
        // The biggest supported hash algorithm is SHA-2-512, which is only 64 bytes.
        // One power of two bigger should cover most unknown algorithms, too.
        private const int HashBufferStackSize = 128;

        public abstract DSAParameters ExportParameters(bool includePrivateParameters);

        public abstract void ImportParameters(DSAParameters parameters);

        protected DSA() { }

        [RequiresUnreferencedCode(CryptoConfig.CreateFromNameUnreferencedCodeMessage)]
        public static new DSA? Create(string algName)
        {
            return (DSA?)CryptoConfig.CreateFromName(algName);
        }

        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("maccatalyst")]
        [UnsupportedOSPlatform("tvos")]
        public static new DSA Create()
        {
            return CreateCore();
        }

        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("maccatalyst")]
        [UnsupportedOSPlatform("tvos")]
        public static DSA Create(int keySizeInBits)
        {
            DSA dsa = CreateCore();

            try
            {
                dsa.KeySize = keySizeInBits;
                return dsa;
            }
            catch
            {
                dsa.Dispose();
                throw;
            }
        }

        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("maccatalyst")]
        [UnsupportedOSPlatform("tvos")]
        public static DSA Create(DSAParameters parameters)
        {
            DSA dsa = CreateCore();

            try
            {
                dsa.ImportParameters(parameters);
                return dsa;
            }
            catch
            {
                dsa.Dispose();
                throw;
            }
        }

        // DSA does not encode the algorithm identifier into the signature blob, therefore CreateSignature and
        // VerifySignature do not need the HashAlgorithmName value, only SignData and VerifyData do.
        public abstract byte[] CreateSignature(byte[] rgbHash);

        public abstract bool VerifySignature(byte[] rgbHash, byte[] rgbSignature);

        protected virtual byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
        {
            throw DerivedClassMustOverride();
        }

        protected virtual byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm)
        {
            throw DerivedClassMustOverride();
        }

        public byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            // hashAlgorithm is verified in the overload

            return SignData(data, 0, data.Length, hashAlgorithm);
        }

        /// <summary>
        ///   Computes the hash value of the specified data and signs it using the specified signature format.
        /// </summary>
        /// <param name="data">The data to sign.</param>
        /// <param name="hashAlgorithm">The hash algorithm to use to create the hash value.</param>
        /// <param name="signatureFormat">The encoding format to use for the signature.</param>
        /// <returns>
        ///   The DSA signature for the specified data.
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
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw HashAlgorithmNameNullOrEmpty();
            if (!signatureFormat.IsKnownValue())
                throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);

            return SignDataCore(data, hashAlgorithm, signatureFormat);
        }

        public virtual byte[] SignData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (offset < 0 || offset > data.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || count > data.Length - offset)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw HashAlgorithmNameNullOrEmpty();

            byte[] hash = HashData(data, offset, count, hashAlgorithm);
            return CreateSignature(hash);
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
        ///   The DSA signature for the specified data.
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
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (offset < 0 || offset > data.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || count > data.Length - offset)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw HashAlgorithmNameNullOrEmpty();
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
        ///   The DSA signature for the specified data.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   An error occurred in the hashing or signing operation.
        /// </exception>
        protected virtual byte[] SignDataCore(
            ReadOnlySpan<byte> data,
            HashAlgorithmName hashAlgorithm,
            DSASignatureFormat signatureFormat)
        {
            Span<byte> signature = stackalloc byte[SignatureStackSize];

            if (TrySignDataCore(data, signature, hashAlgorithm, signatureFormat, out int bytesWritten))
            {
                return signature.Slice(0, bytesWritten).ToArray();
            }

            // If that didn't work, fall back on older approaches.

            byte[] hash = HashSpanToArray(data, hashAlgorithm);
            byte[] sig = CreateSignature(hash);
            return AsymmetricAlgorithmHelpers.ConvertFromIeeeP1363Signature(sig, signatureFormat);
        }

        public virtual byte[] SignData(Stream data, HashAlgorithmName hashAlgorithm)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw HashAlgorithmNameNullOrEmpty();

            byte[] hash = HashData(data, hashAlgorithm);
            return CreateSignature(hash);
        }

        /// <summary>
        ///   Computes the hash value of the specified data and signs it using the specified signature format.
        /// </summary>
        /// <param name="data">The data to sign.</param>
        /// <param name="hashAlgorithm">The hash algorithm to use to create the hash value.</param>
        /// <param name="signatureFormat">The encoding format to use for the signature.</param>
        /// <returns>
        ///   The DSA signature for the specified data.
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
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw HashAlgorithmNameNullOrEmpty();
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
        ///   The DSA signature for the specified data.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   An error occurred in the hashing or signing operation.
        /// </exception>
        protected virtual byte[] SignDataCore(Stream data, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat)
        {
            byte[] hash = HashData(data, hashAlgorithm);
            return CreateSignatureCore(hash, signatureFormat);
        }

        public bool VerifyData(byte[] data, byte[] signature, HashAlgorithmName hashAlgorithm)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            return VerifyData(data, 0, data.Length, signature, hashAlgorithm);
        }

        public virtual bool VerifyData(byte[] data, int offset, int count, byte[] signature, HashAlgorithmName hashAlgorithm)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (offset < 0 || offset > data.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || count > data.Length - offset)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (signature == null)
                throw new ArgumentNullException(nameof(signature));
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw HashAlgorithmNameNullOrEmpty();

            byte[] hash = HashData(data, offset, count, hashAlgorithm);
            return VerifySignature(hash, signature);
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
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (offset < 0 || offset > data.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || count > data.Length - offset)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (signature == null)
                throw new ArgumentNullException(nameof(signature));
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw HashAlgorithmNameNullOrEmpty();
            if (!signatureFormat.IsKnownValue())
                throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);

            return VerifyDataCore(new ReadOnlySpan<byte>(data, offset, count), signature, hashAlgorithm, signatureFormat);
        }

        public virtual bool VerifyData(Stream data, byte[] signature, HashAlgorithmName hashAlgorithm)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (signature == null)
                throw new ArgumentNullException(nameof(signature));
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw HashAlgorithmNameNullOrEmpty();

            byte[] hash = HashData(data, hashAlgorithm);
            return VerifySignature(hash, signature);
        }

        /// <summary>
        ///   Creates the DSA signature for the specified hash value in the indicated format.
        /// </summary>
        /// <param name="rgbHash">The hash value to sign.</param>
        /// <param name="signatureFormat">The encoding format to use for the signature.</param>
        /// <returns>
        ///   The DSA signature for the specified data.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="rgbHash"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="signatureFormat"/> is not a known format.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred in the signing operation.
        /// </exception>
        public byte[] CreateSignature(byte[] rgbHash, DSASignatureFormat signatureFormat)
        {
            if (rgbHash == null)
                throw new ArgumentNullException(nameof(rgbHash));
            if (!signatureFormat.IsKnownValue())
                throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);

            return CreateSignatureCore(rgbHash, signatureFormat);
        }

        /// <summary>
        ///   Creates the DSA signature for the specified hash value in the indicated format.
        /// </summary>
        /// <param name="hash">The hash value to sign.</param>
        /// <param name="signatureFormat">The encoding format to use for the signature.</param>
        /// <returns>
        ///   The DSA signature for the specified data.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   An error occurred in the signing operation.
        /// </exception>
        protected virtual byte[] CreateSignatureCore(ReadOnlySpan<byte> hash, DSASignatureFormat signatureFormat)
        {
            Span<byte> signature = stackalloc byte[SignatureStackSize];

            if (TryCreateSignatureCore(hash, signature, signatureFormat, out int bytesWritten))
            {
                return signature.Slice(0, bytesWritten).ToArray();
            }

            // If that didn't work, fall back to the older overload and convert formats.
            byte[] sig = CreateSignature(hash.ToArray());
            return AsymmetricAlgorithmHelpers.ConvertFromIeeeP1363Signature(sig, signatureFormat);
        }

        public virtual bool TryCreateSignature(ReadOnlySpan<byte> hash, Span<byte> destination, out int bytesWritten)
            => TryCreateSignatureCore(hash, destination, DSASignatureFormat.IeeeP1363FixedFieldConcatenation, out bytesWritten);

        /// <summary>
        ///   Attempts to create the DSA signature for the specified hash value in the indicated format
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
        public bool TryCreateSignature(
            ReadOnlySpan<byte> hash,
            Span<byte> destination,
            DSASignatureFormat signatureFormat,
            out int bytesWritten)
        {
            if (!signatureFormat.IsKnownValue())
                throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);

            return TryCreateSignatureCore(hash, destination, signatureFormat, out bytesWritten);
        }

        /// <summary>
        ///   Attempts to create the DSA signature for the specified hash value in the indicated format
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
        protected virtual bool TryCreateSignatureCore(
            ReadOnlySpan<byte> hash,
            Span<byte> destination,
            DSASignatureFormat signatureFormat,
            out int bytesWritten)
        {
            // This method is expected to be overriden with better implementation

            // The only available implementation here is abstract method, use it
            byte[] sig = CreateSignature(hash.ToArray());

            if (signatureFormat != DSASignatureFormat.IeeeP1363FixedFieldConcatenation)
            {
                sig = AsymmetricAlgorithmHelpers.ConvertFromIeeeP1363Signature(sig, signatureFormat);
            }

            return Helpers.TryCopyToDestination(sig, destination, out bytesWritten);
        }

        protected virtual bool TryHashData(
            ReadOnlySpan<byte> data,
            Span<byte> destination,
            HashAlgorithmName hashAlgorithm,
            out int bytesWritten)
        {
            byte[] hash = HashSpanToArray(data, hashAlgorithm);
            return Helpers.TryCopyToDestination(hash, destination, out bytesWritten);
        }

        public virtual bool TrySignData(
            ReadOnlySpan<byte> data,
            Span<byte> destination,
            HashAlgorithmName hashAlgorithm,
            out int bytesWritten)
        {
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw HashAlgorithmNameNullOrEmpty();

            if (TryHashData(data, destination, hashAlgorithm, out int hashLength) &&
                TryCreateSignature(destination.Slice(0, hashLength), destination, out bytesWritten))
            {
                return true;
            }

            bytesWritten = 0;
            return false;
        }

        /// <summary>
        ///   Attempts to create the DSA signature for the specified data in the indicated format
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
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw HashAlgorithmNameNullOrEmpty();
            if (!signatureFormat.IsKnownValue())
                throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);

            return TrySignDataCore(data, destination, hashAlgorithm, signatureFormat, out bytesWritten);
        }

        /// <summary>
        ///   Attempts to create the DSA signature for the specified data in the indicated format
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
            Span<byte> tmp = stackalloc byte[HashBufferStackSize];
            ReadOnlySpan<byte> hash = HashSpanToTmp(data, hashAlgorithm, tmp);

            return TryCreateSignatureCore(hash, destination, signatureFormat, out bytesWritten);
        }

        public virtual bool VerifyData(
            ReadOnlySpan<byte> data,
            ReadOnlySpan<byte> signature,
            HashAlgorithmName hashAlgorithm)
        {
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw HashAlgorithmNameNullOrEmpty();

            return VerifyDataCore(data, signature, hashAlgorithm, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
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
            byte[] data,
            byte[] signature,
            HashAlgorithmName hashAlgorithm,
            DSASignatureFormat signatureFormat)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (signature == null)
                throw new ArgumentNullException(nameof(signature));
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw HashAlgorithmNameNullOrEmpty();
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
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (signature == null)
                throw new ArgumentNullException(nameof(signature));
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw HashAlgorithmNameNullOrEmpty();
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
            return VerifySignatureCore(hash, signature, signatureFormat);
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
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw HashAlgorithmNameNullOrEmpty();
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
            Span<byte> tmp = stackalloc byte[HashBufferStackSize];
            ReadOnlySpan<byte> hash = HashSpanToTmp(data, hashAlgorithm, tmp);

            return VerifySignatureCore(hash, signature, signatureFormat);
        }

        /// <summary>
        ///   Verifies that a digital signature is valid for the provided hash.
        /// </summary>
        /// <param name="rgbHash">The signed hash.</param>
        /// <param name="rgbSignature">The signature to verify.</param>
        /// <param name="signatureFormat">The encoding format for <paramref name="rgbSignature"/>.</param>
        /// <returns>
        ///   <see langword="true"/> if the digital signature is valid for the provided data; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="rgbHash"/> or <paramref name="rgbSignature"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="signatureFormat"/> is not a known format.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred in the verification operation.
        /// </exception>
        public bool VerifySignature(byte[] rgbHash, byte[] rgbSignature, DSASignatureFormat signatureFormat)
        {
            if (rgbHash == null)
                throw new ArgumentNullException(nameof(rgbHash));
            if (rgbSignature == null)
                throw new ArgumentNullException(nameof(rgbSignature));
            if (!signatureFormat.IsKnownValue())
                throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);

            return VerifySignatureCore(rgbHash, rgbSignature, signatureFormat);
        }

        public virtual bool VerifySignature(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature) =>
            VerifySignature(hash.ToArray(), signature.ToArray());

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
        public bool VerifySignature(
            ReadOnlySpan<byte> hash,
            ReadOnlySpan<byte> signature,
            DSASignatureFormat signatureFormat)
        {
            if (!signatureFormat.IsKnownValue())
                throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);

            return VerifySignatureCore(hash, signature, signatureFormat);
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
        protected virtual bool VerifySignatureCore(
            ReadOnlySpan<byte> hash,
            ReadOnlySpan<byte> signature,
            DSASignatureFormat signatureFormat)
        {
            // This method is expected to be overriden with better implementation

            byte[]? sig = this.ConvertSignatureToIeeeP1363(signatureFormat, signature);

            // If the signature failed normalization to IEEE P1363 then it
            // obviously doesn't verify.
            if (sig == null)
            {
                return false;
            }

            // The only available implementation here is abstract method, use it.
            // Since it requires an exactly-sized array, skip pooled arrays.
            return VerifySignature(hash, sig);
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

        private static Exception DerivedClassMustOverride() =>
            new NotImplementedException(SR.NotSupported_SubclassOverride);

        internal static Exception HashAlgorithmNameNullOrEmpty() =>
            new ArgumentException(SR.Cryptography_HashAlgorithmNameNullOrEmpty, "hashAlgorithm");

        public override bool TryExportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<byte> passwordBytes,
            PbeParameters pbeParameters,
            Span<byte> destination,
            out int bytesWritten)
        {
            if (pbeParameters == null)
                throw new ArgumentNullException(nameof(pbeParameters));

            PasswordBasedEncryption.ValidatePbeParameters(
                pbeParameters,
                ReadOnlySpan<char>.Empty,
                passwordBytes);

            AsnWriter pkcs8PrivateKey = WritePkcs8();

            AsnWriter writer = KeyFormatHelper.WriteEncryptedPkcs8(
                passwordBytes,
                pkcs8PrivateKey,
                pbeParameters);

            return writer.TryEncode(destination, out bytesWritten);
        }

        public override bool TryExportEncryptedPkcs8PrivateKey(
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

            AsnWriter pkcs8PrivateKey = WritePkcs8();
            AsnWriter writer = KeyFormatHelper.WriteEncryptedPkcs8(
                password,
                pkcs8PrivateKey,
                pbeParameters);

            return writer.TryEncode(destination, out bytesWritten);
        }

        public override bool TryExportPkcs8PrivateKey(
            Span<byte> destination,
            out int bytesWritten)
        {
            AsnWriter writer = WritePkcs8();
            return writer.TryEncode(destination, out bytesWritten);
        }

        public override bool TryExportSubjectPublicKeyInfo(
            Span<byte> destination,
            out int bytesWritten)
        {
            AsnWriter writer = WriteSubjectPublicKeyInfo();
            return writer.TryEncode(destination, out bytesWritten);
        }

        private unsafe AsnWriter WritePkcs8()
        {
            DSAParameters dsaParameters = ExportParameters(true);

            fixed (byte* privPin = dsaParameters.X)
            {
                try
                {
                    return DSAKeyFormatHelper.WritePkcs8(dsaParameters);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(dsaParameters.X);
                }
            }
        }

        private AsnWriter WriteSubjectPublicKeyInfo()
        {
            DSAParameters dsaParameters = ExportParameters(false);
            return DSAKeyFormatHelper.WriteSubjectPublicKeyInfo(dsaParameters);
        }

        public override unsafe void ImportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<byte> passwordBytes,
            ReadOnlySpan<byte> source,
            out int bytesRead)
        {
            DSAKeyFormatHelper.ReadEncryptedPkcs8(
                source,
                passwordBytes,
                out int localRead,
                out DSAParameters ret);

            fixed (byte* privPin = ret.X)
            {
                try
                {
                    ImportParameters(ret);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(ret.X);
                }
            }

            bytesRead = localRead;
        }

        public override unsafe void ImportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<char> password,
            ReadOnlySpan<byte> source,
            out int bytesRead)
        {
            DSAKeyFormatHelper.ReadEncryptedPkcs8(
                source,
                password,
                out int localRead,
                out DSAParameters ret);

            fixed (byte* privPin = ret.X)
            {
                try
                {
                    ImportParameters(ret);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(ret.X);
                }
            }

            bytesRead = localRead;
        }

        public override unsafe void ImportPkcs8PrivateKey(
            ReadOnlySpan<byte> source,
            out int bytesRead)
        {
            DSAKeyFormatHelper.ReadPkcs8(
                source,
                out int localRead,
                out DSAParameters key);

            fixed (byte* privPin = key.X)
            {
                try
                {
                    ImportParameters(key);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(key.X);
                }
            }

            bytesRead = localRead;
        }

        public override void ImportSubjectPublicKeyInfo(
            ReadOnlySpan<byte> source,
            out int bytesRead)
        {
            DSAKeyFormatHelper.ReadSubjectPublicKeyInfo(
                source,
                out int localRead,
                out DSAParameters key);

            ImportParameters(key);
            bytesRead = localRead;
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
            DSAParameters dsaParameters = ExportParameters(false);
            int qLength = dsaParameters.Q!.Length;

            switch (signatureFormat)
            {
                case DSASignatureFormat.IeeeP1363FixedFieldConcatenation:
                    return qLength * 2;
                case DSASignatureFormat.Rfc3279DerSequence:
                    return AsymmetricAlgorithmHelpers.GetMaxDerSignatureSize(fieldSizeBits: qLength * 8);
                default:
                    throw new ArgumentOutOfRangeException(nameof(signatureFormat));
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
        ///   </list>
        ///   </para>
        /// </remarks>
        public override void ImportFromPem(ReadOnlySpan<char> input)
        {
            PemKeyImportHelpers.ImportPem(input, label => {
                if (label.SequenceEqual(PemLabels.Pkcs8PrivateKey))
                {
                    return ImportPkcs8PrivateKey;
                }
                else if (label.SequenceEqual(PemLabels.SpkiPublicKey))
                {
                    return ImportSubjectPublicKeyInfo;
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
            PemKeyImportHelpers.ImportEncryptedPem<char>(input, password, ImportEncryptedPkcs8PrivateKey);
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
            PemKeyImportHelpers.ImportEncryptedPem<byte>(input, passwordBytes, ImportEncryptedPkcs8PrivateKey);
        }
    }
}
