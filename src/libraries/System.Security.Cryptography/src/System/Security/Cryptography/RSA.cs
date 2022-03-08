// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography.Asn1;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    public abstract partial class RSA : AsymmetricAlgorithm
    {
        [UnsupportedOSPlatform("browser")]
        public static new partial RSA Create();

        [RequiresUnreferencedCode(CryptoConfig.CreateFromNameUnreferencedCodeMessage)]
        public static new RSA? Create(string algName)
        {
            return (RSA?)CryptoConfig.CreateFromName(algName);
        }

        [UnsupportedOSPlatform("browser")]
        public static RSA Create(int keySizeInBits)
        {
            RSA rsa = Create();

            try
            {
                rsa.KeySize = keySizeInBits;
                return rsa;
            }
            catch
            {
                rsa.Dispose();
                throw;
            }
        }

        [UnsupportedOSPlatform("browser")]
        public static RSA Create(RSAParameters parameters)
        {
            RSA rsa = Create();

            try
            {
                rsa.ImportParameters(parameters);
                return rsa;
            }
            catch
            {
                rsa.Dispose();
                throw;
            }
        }

        public abstract RSAParameters ExportParameters(bool includePrivateParameters);
        public abstract void ImportParameters(RSAParameters parameters);
        public virtual byte[] Encrypt(byte[] data, RSAEncryptionPadding padding) => throw DerivedClassMustOverride();
        public virtual byte[] Decrypt(byte[] data, RSAEncryptionPadding padding) => throw DerivedClassMustOverride();
        public virtual byte[] SignHash(byte[] hash, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding) => throw DerivedClassMustOverride();
        public virtual bool VerifyHash(byte[] hash, byte[] signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding) => throw DerivedClassMustOverride();

        protected virtual byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm) =>
            HashOneShotHelpers.HashData(hashAlgorithm, new ReadOnlySpan<byte>(data, offset, count));

        protected virtual byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm) =>
            HashOneShotHelpers.HashData(hashAlgorithm, data);

        public virtual bool TryDecrypt(ReadOnlySpan<byte> data, Span<byte> destination, RSAEncryptionPadding padding, out int bytesWritten)
        {
            byte[] result = Decrypt(data.ToArray(), padding);

            if (destination.Length >= result.Length)
            {
                new ReadOnlySpan<byte>(result).CopyTo(destination);
                bytesWritten = result.Length;
                return true;
            }

            bytesWritten = 0;
            return false;
        }

        public virtual bool TryEncrypt(ReadOnlySpan<byte> data, Span<byte> destination, RSAEncryptionPadding padding, out int bytesWritten)
        {
            byte[] result = Encrypt(data.ToArray(), padding);

            if (destination.Length >= result.Length)
            {
                new ReadOnlySpan<byte>(result).CopyTo(destination);
                bytesWritten = result.Length;
                return true;
            }

            bytesWritten = 0;
            return false;
        }

        protected virtual bool TryHashData(ReadOnlySpan<byte> data, Span<byte> destination, HashAlgorithmName hashAlgorithm, out int bytesWritten)
        {
            // If this is an algorithm that we ship, then we can use the hash one-shot.
            if (this is IRuntimeAlgorithm)
            {
                return HashOneShotHelpers.TryHashData(hashAlgorithm, data, destination, out bytesWritten);
            }

            // If this is not our algorithm implementation, for compatibility purposes we need to
            // call out to the HashData virtual.
            byte[] result;
            // Use ArrayPool.Shared instead of CryptoPool because the array is passed out.
            byte[] array = ArrayPool<byte>.Shared.Rent(data.Length);
            try
            {
                data.CopyTo(array);
                result = HashData(array, 0, data.Length, hashAlgorithm);
            }
            finally
            {
                Array.Clear(array, 0, data.Length);
                ArrayPool<byte>.Shared.Return(array);
            }

            if (destination.Length >= result.Length)
            {
                new ReadOnlySpan<byte>(result).CopyTo(destination);
                bytesWritten = result.Length;
                return true;
            }

            bytesWritten = 0;
            return false;
        }

        public virtual bool TrySignHash(ReadOnlySpan<byte> hash, Span<byte> destination, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding, out int bytesWritten)
        {
            byte[] result = SignHash(hash.ToArray(), hashAlgorithm, padding);

            if (destination.Length >= result.Length)
            {
                new ReadOnlySpan<byte>(result).CopyTo(destination);
                bytesWritten = result.Length;
                return true;
            }

            bytesWritten = 0;
            return false;
        }

        public virtual bool VerifyHash(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding) =>
            VerifyHash(hash.ToArray(), signature.ToArray(), hashAlgorithm, padding);

        private static Exception DerivedClassMustOverride() =>
            new NotImplementedException(SR.NotSupported_SubclassOverride);

        public virtual byte[] DecryptValue(byte[] rgb) =>
            throw new NotSupportedException(SR.NotSupported_Method); // Same as Desktop

        public virtual byte[] EncryptValue(byte[] rgb) =>
            throw new NotSupportedException(SR.NotSupported_Method); // Same as Desktop

        public byte[] SignData(byte[] data!!, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
        {
            return SignData(data, 0, data.Length, hashAlgorithm, padding);
        }

        public virtual byte[] SignData(
            byte[] data,
            int offset,
            int count,
            HashAlgorithmName hashAlgorithm,
            RSASignaturePadding padding)
        {
            ArgumentNullException.ThrowIfNull(data);
            if (offset < 0 || offset > data.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || count > data.Length - offset)
                throw new ArgumentOutOfRangeException(nameof(count));
            ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, nameof(hashAlgorithm));
            ArgumentNullException.ThrowIfNull(padding);

            byte[] hash = HashData(data, offset, count, hashAlgorithm);
            return SignHash(hash, hashAlgorithm, padding);
        }

        public virtual byte[] SignData(Stream data, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
        {
            ArgumentNullException.ThrowIfNull(data);
            ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, nameof(hashAlgorithm));
            ArgumentNullException.ThrowIfNull(padding);

            byte[] hash = HashData(data, hashAlgorithm);
            return SignHash(hash, hashAlgorithm, padding);
        }

        public virtual bool TrySignData(ReadOnlySpan<byte> data, Span<byte> destination, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding, out int bytesWritten)
        {
            ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, nameof(hashAlgorithm));
            ArgumentNullException.ThrowIfNull(padding);

            if (TryHashData(data, destination, hashAlgorithm, out int hashLength) &&
                TrySignHash(destination.Slice(0, hashLength), destination, hashAlgorithm, padding, out bytesWritten))
            {
                return true;
            }

            bytesWritten = 0;
            return false;
        }

        public bool VerifyData(byte[] data!!, byte[] signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
        {
            return VerifyData(data, 0, data.Length, signature, hashAlgorithm, padding);
        }

        public virtual bool VerifyData(
            byte[] data,
            int offset,
            int count,
            byte[] signature,
            HashAlgorithmName hashAlgorithm,
            RSASignaturePadding padding)
        {
            ArgumentNullException.ThrowIfNull(data);
            if (offset < 0 || offset > data.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || count > data.Length - offset)
                throw new ArgumentOutOfRangeException(nameof(count));
            ArgumentNullException.ThrowIfNull(signature);
            ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, nameof(hashAlgorithm));
            ArgumentNullException.ThrowIfNull(padding);

            byte[] hash = HashData(data, offset, count, hashAlgorithm);
            return VerifyHash(hash, signature, hashAlgorithm, padding);
        }

        public bool VerifyData(Stream data, byte[] signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
        {
            ArgumentNullException.ThrowIfNull(data);
            ArgumentNullException.ThrowIfNull(signature);
            ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, nameof(hashAlgorithm));
            ArgumentNullException.ThrowIfNull(padding);

            byte[] hash = HashData(data, hashAlgorithm);
            return VerifyHash(hash, signature, hashAlgorithm, padding);
        }

        public virtual bool VerifyData(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
        {
            ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, nameof(hashAlgorithm));
            ArgumentNullException.ThrowIfNull(padding);

            for (int i = 256; ; i = checked(i * 2))
            {
                int hashLength = 0;
                byte[] hash = CryptoPool.Rent(i);
                try
                {
                    if (TryHashData(data, hash, hashAlgorithm, out hashLength))
                    {
                        return VerifyHash(new ReadOnlySpan<byte>(hash, 0, hashLength), signature, hashAlgorithm, padding);
                    }
                }
                finally
                {
                    CryptoPool.Return(hash, hashLength);
                }
            }
        }

        public virtual byte[] ExportRSAPrivateKey()
        {
            AsnWriter pkcs1PrivateKey = WritePkcs1PrivateKey();
            return pkcs1PrivateKey.Encode();
        }

        public virtual bool TryExportRSAPrivateKey(Span<byte> destination, out int bytesWritten)
        {
            AsnWriter pkcs1PrivateKey = WritePkcs1PrivateKey();
            return pkcs1PrivateKey.TryEncode(destination, out bytesWritten);
        }

        public virtual byte[] ExportRSAPublicKey()
        {
            AsnWriter pkcs1PublicKey = WritePkcs1PublicKey();
            return pkcs1PublicKey.Encode();
        }

        public virtual bool TryExportRSAPublicKey(Span<byte> destination, out int bytesWritten)
        {
            AsnWriter pkcs1PublicKey = WritePkcs1PublicKey();
            return pkcs1PublicKey.TryEncode(destination, out bytesWritten);
        }

        public override unsafe bool TryExportSubjectPublicKeyInfo(Span<byte> destination, out int bytesWritten)
        {
            // The PKCS1 RSAPublicKey format is just the modulus (KeySize bits) and Exponent (usually 3 bytes),
            // with each field having up to 7 bytes of overhead and then up to 6 extra bytes of overhead for the
            // SEQUENCE tag.
            //
            // So KeySize / 4 is ideally enough to start.
            int rentSize = KeySize / 4;

            while (true)
            {
                byte[] rented = CryptoPool.Rent(rentSize);
                rentSize = rented.Length;
                int pkcs1Size = 0;

                fixed (byte* rentPtr = rented)
                {
                    try
                    {
                        if (!TryExportRSAPublicKey(rented, out pkcs1Size))
                        {
                            rentSize = checked(rentSize * 2);
                            continue;
                        }

                        AsnWriter writer = RSAKeyFormatHelper.WriteSubjectPublicKeyInfo(rented.AsSpan(0, pkcs1Size));
                        return writer.TryEncode(destination, out bytesWritten);
                    }
                    finally
                    {
                        CryptoPool.Return(rented, pkcs1Size);
                    }
                }
            }
        }

        public override bool TryExportPkcs8PrivateKey(Span<byte> destination, out int bytesWritten)
        {
            AsnWriter writer = WritePkcs8PrivateKey();
            return writer.TryEncode(destination, out bytesWritten);
        }

        private unsafe AsnWriter WritePkcs8PrivateKey()
        {
            // A PKCS1 RSAPrivateKey is the Modulus (KeySize bits), D (~KeySize bits)
            // P, Q, DP, DQ, InverseQ (all ~KeySize/2 bits)
            // Each field can have up to 7 bytes of overhead, and then another 9 bytes
            // of fixed overhead.
            // So it should fit in 5 * KeySizeInBytes, but Exponent is a wildcard.

            int rentSize = checked(5 * KeySize / 8);

            while (true)
            {
                byte[] rented = CryptoPool.Rent(rentSize);
                rentSize = rented.Length;
                int pkcs1Size = 0;

                fixed (byte* rentPtr = rented)
                {
                    try
                    {
                        if (!TryExportRSAPrivateKey(rented, out pkcs1Size))
                        {
                            rentSize = checked(rentSize * 2);
                            continue;
                        }

                        return RSAKeyFormatHelper.WritePkcs8PrivateKey(new ReadOnlySpan<byte>(rented, 0, pkcs1Size));
                    }
                    finally
                    {
                        CryptoPool.Return(rented, pkcs1Size);
                    }
                }
            }
        }

        public override bool TryExportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<char> password,
            PbeParameters pbeParameters!!,
            Span<byte> destination,
            out int bytesWritten)
        {
            PasswordBasedEncryption.ValidatePbeParameters(
                pbeParameters,
                password,
                ReadOnlySpan<byte>.Empty);

            AsnWriter pkcs8PrivateKey = WritePkcs8PrivateKey();

            AsnWriter writer = KeyFormatHelper.WriteEncryptedPkcs8(
                password,
                pkcs8PrivateKey,
                pbeParameters);

            return writer.TryEncode(destination, out bytesWritten);
        }

        public override bool TryExportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<byte> passwordBytes,
            PbeParameters pbeParameters!!,
            Span<byte> destination,
            out int bytesWritten)
        {
            PasswordBasedEncryption.ValidatePbeParameters(
                pbeParameters,
                ReadOnlySpan<char>.Empty,
                passwordBytes);

            AsnWriter pkcs8PrivateKey = WritePkcs8PrivateKey();

            AsnWriter writer = KeyFormatHelper.WriteEncryptedPkcs8(
                passwordBytes,
                pkcs8PrivateKey,
                pbeParameters);

            return writer.TryEncode(destination, out bytesWritten);
        }

        private AsnWriter WritePkcs1PublicKey()
        {
            RSAParameters rsaParameters = ExportParameters(false);
            return RSAKeyFormatHelper.WritePkcs1PublicKey(rsaParameters);
        }

        private unsafe AsnWriter WritePkcs1PrivateKey()
        {
            RSAParameters rsaParameters = ExportParameters(true);

            fixed (byte* dPin = rsaParameters.D)
            fixed (byte* pPin = rsaParameters.P)
            fixed (byte* qPin = rsaParameters.Q)
            fixed (byte* dpPin = rsaParameters.DP)
            fixed (byte* dqPin = rsaParameters.DQ)
            fixed (byte* qInvPin = rsaParameters.InverseQ)
            {
                try
                {
                    return RSAKeyFormatHelper.WritePkcs1PrivateKey(rsaParameters);
                }
                finally
                {
                    ClearPrivateParameters(rsaParameters);
                }
            }
        }

        public override unsafe void ImportSubjectPublicKeyInfo(ReadOnlySpan<byte> source, out int bytesRead)
        {
            fixed (byte* ptr = &MemoryMarshal.GetReference(source))
            {
                using (MemoryManager<byte> manager = new PointerMemoryManager<byte>(ptr, source.Length))
                {
                    ReadOnlyMemory<byte> pkcs1 = RSAKeyFormatHelper.ReadSubjectPublicKeyInfo(
                        manager.Memory,
                        out int localRead);

                    ImportRSAPublicKey(pkcs1.Span, out _);
                    bytesRead = localRead;
                }
            }
        }

        public virtual unsafe void ImportRSAPublicKey(ReadOnlySpan<byte> source, out int bytesRead)
        {
            try
            {
                AsnDecoder.ReadEncodedValue(
                    source,
                    AsnEncodingRules.BER,
                    out _,
                    out _,
                    out int localRead);

                fixed (byte* ptr = &MemoryMarshal.GetReference(source))
                {
                    using (MemoryManager<byte> manager = new PointerMemoryManager<byte>(ptr, localRead))
                    {
                        AlgorithmIdentifierAsn ignored = default;
                        RSAKeyFormatHelper.ReadRsaPublicKey(manager.Memory, ignored, out RSAParameters rsaParameters);

                        ImportParameters(rsaParameters);

                        bytesRead = localRead;
                    }
                }
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        public virtual unsafe void ImportRSAPrivateKey(ReadOnlySpan<byte> source, out int bytesRead)
        {
            try
            {
                AsnDecoder.ReadEncodedValue(
                    source,
                    AsnEncodingRules.BER,
                    out _,
                    out _,
                    out int firstValueLength);

                fixed (byte* ptr = &MemoryMarshal.GetReference(source))
                {
                    using (MemoryManager<byte> manager = new PointerMemoryManager<byte>(ptr, firstValueLength))
                    {
                        ReadOnlyMemory<byte> firstValue = manager.Memory;
                        int localRead = firstValue.Length;

                        AlgorithmIdentifierAsn ignored = default;
                        RSAKeyFormatHelper.FromPkcs1PrivateKey(firstValue, ignored, out RSAParameters rsaParameters);

                        fixed (byte* dPin = rsaParameters.D)
                        fixed (byte* pPin = rsaParameters.P)
                        fixed (byte* qPin = rsaParameters.Q)
                        fixed (byte* dpPin = rsaParameters.DP)
                        fixed (byte* dqPin = rsaParameters.DQ)
                        fixed (byte* qInvPin = rsaParameters.InverseQ)
                        {
                            try
                            {
                                ImportParameters(rsaParameters);
                            }
                            finally
                            {
                                ClearPrivateParameters(rsaParameters);
                            }
                        }

                        bytesRead = localRead;
                    }
                }
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        public override unsafe void ImportPkcs8PrivateKey(ReadOnlySpan<byte> source, out int bytesRead)
        {
            fixed (byte* ptr = &MemoryMarshal.GetReference(source))
            {
                using (MemoryManager<byte> manager = new PointerMemoryManager<byte>(ptr, source.Length))
                {
                    ReadOnlyMemory<byte> pkcs1 = RSAKeyFormatHelper.ReadPkcs8(
                        manager.Memory,
                        out int localRead);

                    ImportRSAPrivateKey(pkcs1.Span, out _);
                    bytesRead = localRead;
                }
            }
        }

        public override unsafe void ImportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<byte> passwordBytes,
            ReadOnlySpan<byte> source,
            out int bytesRead)
        {
            RSAKeyFormatHelper.ReadEncryptedPkcs8(
                source,
                passwordBytes,
                out int localRead,
                out RSAParameters ret);

            fixed (byte* dPin = ret.D)
            fixed (byte* pPin = ret.P)
            fixed (byte* qPin = ret.Q)
            fixed (byte* dpPin = ret.DP)
            fixed (byte* dqPin = ret.DQ)
            fixed (byte* qInvPin = ret.InverseQ)
            {
                try
                {
                    ImportParameters(ret);
                }
                finally
                {
                    ClearPrivateParameters(ret);
                }
            }

            bytesRead = localRead;
        }

        public override unsafe void ImportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<char> password,
            ReadOnlySpan<byte> source,
            out int bytesRead)
        {
            RSAKeyFormatHelper.ReadEncryptedPkcs8(
                source,
                password,
                out int localRead,
                out RSAParameters ret);

            fixed (byte* dPin = ret.D)
            fixed (byte* pPin = ret.P)
            fixed (byte* qPin = ret.Q)
            fixed (byte* dpPin = ret.DP)
            fixed (byte* dqPin = ret.DQ)
            fixed (byte* qInvPin = ret.InverseQ)
            {
                try
                {
                    ImportParameters(ret);
                }
                finally
                {
                    ClearPrivateParameters(ret);
                }
            }

            bytesRead = localRead;
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
        ///     <item><description>RSA PRIVATE KEY</description></item>
        ///     <item><description>RSA PUBLIC KEY</description></item>
        ///   </list>
        ///   </para>
        /// </remarks>
        public override void ImportFromPem(ReadOnlySpan<char> input)
        {
            PemKeyHelpers.ImportPem(input, label => {
                if (label.SequenceEqual(PemLabels.RsaPrivateKey))
                {
                    return ImportRSAPrivateKey;
                }
                else if (label.SequenceEqual(PemLabels.Pkcs8PrivateKey))
                {
                    return ImportPkcs8PrivateKey;
                }
                else if (label.SequenceEqual(PemLabels.RsaPublicKey))
                {
                    return ImportRSAPublicKey;
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
            // Implementation has been pushed down to AsymmetricAlgorithm. The
            // override remains for compatibility.
            base.ImportFromEncryptedPem(input, password);
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
            // Implementation has been pushed down to AsymmetricAlgorithm. The
            // override remains for compatibility.
            base.ImportFromEncryptedPem(input, passwordBytes);
        }

        /// <summary>
        /// Exports the current key in the PKCS#1 RSAPrivateKey format, PEM encoded.
        /// </summary>
        /// <returns>A string containing the PEM-encoded PKCS#1 RSAPrivateKey.</returns>
        /// <exception cref="CryptographicException">
        /// The key could not be exported.
        /// </exception>
        /// <remarks>
        /// <p>
        ///   A PEM-encoded PKCS#1 RSAPrivateKey will begin with <c>-----BEGIN RSA PRIVATE KEY-----</c>
        ///   and end with <c>-----END RSA PRIVATE KEY-----</c>, with the base64 encoded DER
        ///   contents of the key between the PEM boundaries.
        /// </p>
        /// <p>
        ///   The PEM is encoded according to the IETF RFC 7468 &quot;strict&quot;
        ///   encoding rules.
        /// </p>
        /// </remarks>
        public unsafe string ExportRSAPrivateKeyPem()
        {
            byte[] exported = ExportRSAPrivateKey();

            // Fixed to prevent GC moves.
            fixed (byte* pExported = exported)
            {
                try
                {
                    return PemKeyHelpers.CreatePemFromData(PemLabels.RsaPrivateKey, exported);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(exported);
                }
            }
        }

        /// <summary>
        /// Exports the public-key portion of the current key in the PKCS#1
        /// RSAPublicKey format, PEM encoded.
        /// </summary>
        /// <returns>A string containing the PEM-encoded PKCS#1 RSAPublicKey.</returns>
        /// <exception cref="CryptographicException">
        /// The key could not be exported.
        /// </exception>
        /// <remarks>
        /// <p>
        ///   A PEM-encoded PKCS#1 RSAPublicKey will begin with <c>-----BEGIN RSA PUBLIC KEY-----</c>
        ///   and end with <c>-----END RSA PUBLIC KEY-----</c>, with the base64 encoded DER
        ///   contents of the key between the PEM boundaries.
        /// </p>
        /// <p>
        ///   The PEM is encoded according to the IETF RFC 7468 &quot;strict&quot;
        ///   encoding rules.
        /// </p>
        /// </remarks>
        public string ExportRSAPublicKeyPem()
        {
            byte[] exported = ExportRSAPublicKey();
            return PemKeyHelpers.CreatePemFromData(PemLabels.RsaPublicKey, exported);
        }

        /// <summary>
        /// Attempts to export the current key in the PEM-encoded PKCS#1
        /// RSAPrivateKey format into a provided buffer.
        /// </summary>
        /// <param name="destination">
        /// The character span to receive the PEM-encoded PKCS#1 RSAPrivateKey data.
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
        /// <exception cref="CryptographicException">
        /// The key could not be exported.
        /// </exception>
        /// <remarks>
        /// <p>
        ///   A PEM-encoded PKCS#1 RSAPrivateKey will begin with
        ///   <c>-----BEGIN RSA PRIVATE KEY-----</c> and end with
        ///   <c>-----END RSA PRIVATE KEY-----</c>, with the base64 encoded DER
        ///   contents of the key between the PEM boundaries.
        /// </p>
        /// <p>
        ///   The PEM is encoded according to the IETF RFC 7468 &quot;strict&quot;
        ///   encoding rules.
        /// </p>
        /// </remarks>
        public bool TryExportRSAPrivateKeyPem(Span<char> destination, out int charsWritten)
        {
            static bool Export(RSA alg, Span<byte> destination, out int bytesWritten)
            {
                return alg.TryExportRSAPrivateKey(destination, out bytesWritten);
            }

            return PemKeyHelpers.TryExportToPem(
                this,
                PemLabels.RsaPrivateKey,
                Export,
                destination,
                out charsWritten);
        }

        /// <summary>
        /// Attempts to export the current key in the PEM-encoded PKCS#1
        /// RSAPublicKey format into a provided buffer.
        /// </summary>
        /// <param name="destination">
        /// The character span to receive the PEM-encoded PKCS#1 RSAPublicKey data.
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
        /// <exception cref="CryptographicException">
        /// The key could not be exported.
        /// </exception>
        /// <remarks>
        /// <p>
        ///   A PEM-encoded PKCS#1 RSAPublicKey will begin with
        ///   <c>-----BEGIN RSA PUBLIC KEY-----</c> and end with
        ///   <c>-----END RSA PUBLIC KEY-----</c>, with the base64 encoded DER
        ///   contents of the key between the PEM boundaries.
        /// </p>
        /// <p>
        ///   The PEM is encoded according to the IETF RFC 7468 &quot;strict&quot;
        ///   encoding rules.
        /// </p>
        /// </remarks>
        public bool TryExportRSAPublicKeyPem(Span<char> destination, out int charsWritten)
        {
            static bool Export(RSA alg, Span<byte> destination, out int bytesWritten)
            {
                return alg.TryExportRSAPublicKey(destination, out bytesWritten);
            }

            return PemKeyHelpers.TryExportToPem(
                this,
                PemLabels.RsaPublicKey,
                Export,
                destination,
                out charsWritten);
        }

        private static void ClearPrivateParameters(in RSAParameters rsaParameters)
        {
            CryptographicOperations.ZeroMemory(rsaParameters.D);
            CryptographicOperations.ZeroMemory(rsaParameters.P);
            CryptographicOperations.ZeroMemory(rsaParameters.Q);
            CryptographicOperations.ZeroMemory(rsaParameters.DP);
            CryptographicOperations.ZeroMemory(rsaParameters.DQ);
            CryptographicOperations.ZeroMemory(rsaParameters.InverseQ);
        }

        public override string? KeyExchangeAlgorithm => "RSA";
        public override string SignatureAlgorithm => "RSA";
    }
}
