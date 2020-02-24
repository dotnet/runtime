// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Cryptography;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography
{
    public abstract partial class ECDsa : AsymmetricAlgorithm
    {
        // secp521r1 maxes out at 139 bytes in the DER format, so 256 should always be enough
        private const int SignatureStackBufSize = 256;

        private static readonly string[] s_validOids =
        {
            Oids.EcPublicKey,
            // ECDH and ECMQV are not valid in this context.
        };

        protected ECDsa() { }

        public static new ECDsa? Create(string algorithm)
        {
            if (algorithm == null)
            {
                throw new ArgumentNullException(nameof(algorithm));
            }

            return CryptoConfig.CreateFromName(algorithm) as ECDsa;
        }

        /// <summary>
        /// When overridden in a derived class, exports the named or explicit ECParameters for an ECCurve.
        /// If the curve has a name, the Curve property will contain named curve parameters otherwise it will contain explicit parameters.
        /// </summary>
        /// <param name="includePrivateParameters">true to include private parameters, otherwise, false.</param>
        /// <returns></returns>
        public virtual ECParameters ExportParameters(bool includePrivateParameters)
        {
            throw new NotSupportedException(SR.NotSupported_SubclassOverride);
        }

        /// <summary>
        /// When overridden in a derived class, exports the explicit ECParameters for an ECCurve.
        /// </summary>
        /// <param name="includePrivateParameters">true to include private parameters, otherwise, false.</param>
        /// <returns></returns>
        public virtual ECParameters ExportExplicitParameters(bool includePrivateParameters)
        {
            throw new NotSupportedException(SR.NotSupported_SubclassOverride);
        }

        /// <summary>
        /// When overridden in a derived class, imports the specified ECParameters.
        /// </summary>
        /// <param name="parameters">The curve parameters.</param>
        public virtual void ImportParameters(ECParameters parameters)
        {
            throw new NotSupportedException(SR.NotSupported_SubclassOverride);
        }

        /// <summary>
        /// When overridden in a derived class, generates a new public/private keypair for the specified curve.
        /// </summary>
        /// <param name="curve">The curve to use.</param>
        public virtual void GenerateKey(ECCurve curve)
        {
            throw new NotSupportedException(SR.NotSupported_SubclassOverride);
        }

        public virtual byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw new ArgumentException(SR.Cryptography_HashAlgorithmNameNullOrEmpty, nameof(hashAlgorithm));

            return SignDataCore(data, hashAlgorithm, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
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
                throw new ArgumentException(SR.Cryptography_HashAlgorithmNameNullOrEmpty, nameof(hashAlgorithm));

            return SignDataCore(
                new ReadOnlySpan<byte>(data, offset, count),
                hashAlgorithm,
                DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        }

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
                throw new ArgumentException(SR.Cryptography_HashAlgorithmNameNullOrEmpty, nameof(hashAlgorithm));
            if (!signatureFormat.IsKnownValue())
                throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);

            return SignDataCore(new ReadOnlySpan<byte>(data, offset, count), hashAlgorithm, signatureFormat);
        }

        protected virtual byte[] SignDataCore(
            ReadOnlySpan<byte> data,
            HashAlgorithmName hashAlgorithm,
            DSASignatureFormat signatureFormat)
        {
            Span<byte> signature = stackalloc byte[SignatureStackBufSize];
            int maxSignatureSize = GetMaxSignatureSize(signatureFormat);
            byte[] rented = null;
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

                return signature.Slice(0, bytesWritten).ToArray();
            }
            finally
            {
                if (rented != null)
                {
                    CryptographicOperations.ZeroMemory(rented.AsSpan(0, bytesWritten));
                    ArrayPool<byte>.Shared.Return(rented);
                }
            }
        }

        public byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw new ArgumentException(SR.Cryptography_HashAlgorithmNameNullOrEmpty, nameof(hashAlgorithm));
            if (!signatureFormat.IsKnownValue())
                throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);

            return SignDataCore(data, hashAlgorithm, signatureFormat);
        }

        public byte[] SignData(Stream data, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw new ArgumentException(SR.Cryptography_HashAlgorithmNameNullOrEmpty, nameof(hashAlgorithm));
            if (!signatureFormat.IsKnownValue())
                throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);

            return SignDataCore(data, hashAlgorithm, signatureFormat);
        }

        protected virtual byte[] SignDataCore(
            Stream data,
            HashAlgorithmName hashAlgorithm,
            DSASignatureFormat signatureFormat)
        {
            byte[] hash = HashData(data, hashAlgorithm);
            return SignHashCore(hash, signatureFormat);
        }

        public byte[] SignHash(byte[] hash, DSASignatureFormat signatureFormat)
        {
            if (hash == null)
                throw new ArgumentNullException(nameof(hash));
            if (!signatureFormat.IsKnownValue())
                throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);

            return SignHashCore(hash, signatureFormat);
        }

        protected virtual byte[] SignHashCore(ReadOnlySpan<byte> hash, DSASignatureFormat signatureFormat)
        {
            Span<byte> signature = stackalloc byte[SignatureStackBufSize];
            int maxSignatureSize = GetMaxSignatureSize(signatureFormat);
            byte[] rented = null;
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

                return signature.Slice(0, bytesWritten).ToArray();
            }
            finally
            {
                if (rented != null)
                {
                    CryptographicOperations.ZeroMemory(rented.AsSpan(0, bytesWritten));
                    ArrayPool<byte>.Shared.Return(rented);
                }
            }
        }

        public virtual bool TrySignData(
            ReadOnlySpan<byte> data,
            Span<byte> destination,
            HashAlgorithmName hashAlgorithm,
            out int bytesWritten)
        {
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw new ArgumentException(SR.Cryptography_HashAlgorithmNameNullOrEmpty, nameof(hashAlgorithm));

            return TrySignDataCore(data,
                destination,
                hashAlgorithm,
                DSASignatureFormat.IeeeP1363FixedFieldConcatenation,
                out bytesWritten);
        }

        public bool TrySignData(
            ReadOnlySpan<byte> data,
            Span<byte> destination,
            HashAlgorithmName hashAlgorithm,
            DSASignatureFormat signatureFormat,
            out int bytesWritten)
        {
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw new ArgumentException(SR.Cryptography_HashAlgorithmNameNullOrEmpty, nameof(hashAlgorithm));
            if (!signatureFormat.IsKnownValue())
                throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);

            return TrySignDataCore(data, destination, hashAlgorithm, signatureFormat, out bytesWritten);
        }

        protected virtual bool TrySignDataCore(
            ReadOnlySpan<byte> data,
            Span<byte> destination,
            HashAlgorithmName hashAlgorithm,
            DSASignatureFormat signatureFormat,
            out int bytesWritten)
        {
            // SHA-2-512 is the biggest hash we know about.
            Span<byte> hashSpan = stackalloc byte[512 / 8];

            if (TryHashData(data, hashSpan, hashAlgorithm, out int hashSize))
            {
                hashSpan = hashSpan.Slice(0, hashSize);
            }
            else
            {
                // TryHashData didn't work, the algorithm must be exotic,
                // call the array-returning variant.
                hashSpan = HashData(data.ToArray(), 0, data.Length, hashAlgorithm);
            }

            return TrySignHashCore(hashSpan, destination, signatureFormat, out bytesWritten);
        }

        public virtual byte[] SignData(Stream data, HashAlgorithmName hashAlgorithm)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw new ArgumentException(SR.Cryptography_HashAlgorithmNameNullOrEmpty, nameof(hashAlgorithm));

            byte[] hash = HashData(data, hashAlgorithm);
            return SignHash(hash);
        }

        public bool VerifyData(byte[] data, byte[] signature, HashAlgorithmName hashAlgorithm)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

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
                throw new ArgumentException(SR.Cryptography_HashAlgorithmNameNullOrEmpty, nameof(hashAlgorithm));

            return VerifyDataCore(
                new ReadOnlySpan<byte>(data, offset, count),
                signature,
                hashAlgorithm,
                DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        }

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
                throw new ArgumentException(SR.Cryptography_HashAlgorithmNameNullOrEmpty, nameof(hashAlgorithm));
            if (!signatureFormat.IsKnownValue())
                throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);

            return VerifyDataCore(
                new ReadOnlySpan<byte>(data, offset, count),
                signature,
                hashAlgorithm,
                signatureFormat);
        }

        public bool VerifyData(byte[] data, byte[] signature, HashAlgorithmName hashAlgorithm, DSASignatureFormat signatureFormat)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (signature == null)
                throw new ArgumentNullException(nameof(signature));
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw new ArgumentException(SR.Cryptography_HashAlgorithmNameNullOrEmpty, nameof(hashAlgorithm));
            if (!signatureFormat.IsKnownValue())
                throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);

            return VerifyDataCore(data, signature, hashAlgorithm, signatureFormat);
        }

        public virtual bool VerifyData(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature, HashAlgorithmName hashAlgorithm)
        {
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw new ArgumentException(SR.Cryptography_HashAlgorithmNameNullOrEmpty, nameof(hashAlgorithm));

            return VerifyDataCore(data, signature, hashAlgorithm, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        }

        public bool VerifyData(
            ReadOnlySpan<byte> data,
            ReadOnlySpan<byte> signature,
            HashAlgorithmName hashAlgorithm,
            DSASignatureFormat signatureFormat)
        {
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw new ArgumentException(SR.Cryptography_HashAlgorithmNameNullOrEmpty, nameof(hashAlgorithm));
            if (!signatureFormat.IsKnownValue())
                throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);

            return VerifyDataCore(data, signature, hashAlgorithm, signatureFormat);
        }

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
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (signature == null)
                throw new ArgumentNullException(nameof(signature));
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw new ArgumentException(SR.Cryptography_HashAlgorithmNameNullOrEmpty, nameof(hashAlgorithm));

            return VerifyDataCore(data, signature, hashAlgorithm, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        }

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
                throw new ArgumentException(SR.Cryptography_HashAlgorithmNameNullOrEmpty, nameof(hashAlgorithm));
            if (!signatureFormat.IsKnownValue())
                throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);

            return VerifyDataCore(data, signature, hashAlgorithm, signatureFormat);
        }

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

        protected virtual byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
        {
            throw new NotSupportedException(SR.NotSupported_SubclassOverride);
        }

        protected virtual byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm)
        {
            throw new NotSupportedException(SR.NotSupported_SubclassOverride);
        }

        protected virtual bool TryHashData(ReadOnlySpan<byte> data, Span<byte> destination, HashAlgorithmName hashAlgorithm, out int bytesWritten)
        {
            // Use ArrayPool.Shared instead of CryptoPool because the array is passed out.
            byte[] array = ArrayPool<byte>.Shared.Rent(data.Length);
            try
            {
                data.CopyTo(array);
                byte[] hash = HashData(array, 0, data.Length, hashAlgorithm);
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
                ArrayPool<byte>.Shared.Return(array);
            }
        }

        public virtual bool TrySignHash(ReadOnlySpan<byte> hash, Span<byte> destination, out int bytesWritten)
            => TrySignHashCore(hash, destination, DSASignatureFormat.IeeeP1363FixedFieldConcatenation, out bytesWritten);

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

        public bool VerifyHash(byte[] hash, byte[] signature, DSASignatureFormat signatureFormat)
        {
            if (hash == null)
                throw new ArgumentNullException(nameof(hash));
            if (signature == null)
                throw new ArgumentNullException(nameof(signature));
            if (!signatureFormat.IsKnownValue())
                throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);

            return VerifyHashCore(hash, signature, signatureFormat);
        }

        public bool VerifyHash(
            ReadOnlySpan<byte> hash,
            ReadOnlySpan<byte> signature,
            DSASignatureFormat signatureFormat)
        {
            if (!signatureFormat.IsKnownValue())
                throw DSASignatureFormatHelpers.CreateUnknownValueException(signatureFormat);

            return VerifyHashCore(hash, signature, signatureFormat);
        }

        protected virtual bool VerifyHashCore(
            ReadOnlySpan<byte> hash,
            ReadOnlySpan<byte> signature,
            DSASignatureFormat signatureFormat)
        {
            // This method is expected to be overriden with better implementation

            byte[] sig = this.ConvertSignatureToIeeeP1363(signatureFormat, signature);

            if (sig == null)
                return false;

            // The only available implmentation here is abstract method, use it
            return VerifyHash(hash.ToArray(), sig);
        }

        public override unsafe bool TryExportEncryptedPkcs8PrivateKey(
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

            ECParameters ecParameters = ExportParameters(true);

            fixed (byte* privPtr = ecParameters.D)
            {
                try
                {
                    using (AsnWriter pkcs8PrivateKey = EccKeyFormatHelper.WritePkcs8PrivateKey(ecParameters))
                    using (AsnWriter writer = KeyFormatHelper.WriteEncryptedPkcs8(
                        passwordBytes,
                        pkcs8PrivateKey,
                        pbeParameters))
                    {
                        return writer.TryEncode(destination, out bytesWritten);
                    }
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
                    using (AsnWriter pkcs8PrivateKey = EccKeyFormatHelper.WritePkcs8PrivateKey(ecParameters))
                    using (AsnWriter writer = KeyFormatHelper.WriteEncryptedPkcs8(
                        password,
                        pkcs8PrivateKey,
                        pbeParameters))
                    {
                        return writer.TryEncode(destination, out bytesWritten);
                    }
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
                    using (AsnWriter writer = EccKeyFormatHelper.WritePkcs8PrivateKey(ecParameters))
                    {
                        return writer.TryEncode(destination, out bytesWritten);
                    }
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

            using (AsnWriter writer = EccKeyFormatHelper.WriteSubjectPublicKeyInfo(ecParameters))
            {
                return writer.TryEncode(destination, out bytesWritten);
            }
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
                    using (AsnWriter writer = EccKeyFormatHelper.WriteECPrivateKey(ecParameters))
                    {
                        return writer.Encode();
                    }
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
                    using (AsnWriter writer = EccKeyFormatHelper.WriteECPrivateKey(ecParameters))
                    {
                        return writer.TryEncode(destination, out bytesWritten);
                    }
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(ecParameters.D);
                }
            }
        }

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
