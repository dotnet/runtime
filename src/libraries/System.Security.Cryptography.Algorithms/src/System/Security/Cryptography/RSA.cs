// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    public abstract partial class RSA : AsymmetricAlgorithm
    {
        private static readonly string[] s_validOids =
        {
            Oids.Rsa,
        };

        public static new RSA Create(string algName)
        {
            return (RSA)CryptoConfig.CreateFromName(algName);
        }

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

        protected virtual byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm) => throw DerivedClassMustOverride();
        protected virtual byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm) => throw DerivedClassMustOverride();

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
            byte[] result;
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

        public byte[] SignData(byte[] data, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            return SignData(data, 0, data.Length, hashAlgorithm, padding);
        }

        public virtual byte[] SignData(
            byte[] data,
            int offset,
            int count,
            HashAlgorithmName hashAlgorithm,
            RSASignaturePadding padding)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (offset < 0 || offset > data.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || count > data.Length - offset)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw HashAlgorithmNameNullOrEmpty();
            if (padding == null)
                throw new ArgumentNullException(nameof(padding));

            byte[] hash = HashData(data, offset, count, hashAlgorithm);
            return SignHash(hash, hashAlgorithm, padding);
        }

        public virtual byte[] SignData(Stream data, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw HashAlgorithmNameNullOrEmpty();
            if (padding == null)
                throw new ArgumentNullException(nameof(padding));

            byte[] hash = HashData(data, hashAlgorithm);
            return SignHash(hash, hashAlgorithm, padding);
        }

        public virtual bool TrySignData(ReadOnlySpan<byte> data, Span<byte> destination, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding, out int bytesWritten)
        {
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
            {
                throw HashAlgorithmNameNullOrEmpty();
            }
            if (padding == null)
            {
                throw new ArgumentNullException(nameof(padding));
            }

            if (TryHashData(data, destination, hashAlgorithm, out int hashLength) &&
                TrySignHash(destination.Slice(0, hashLength), destination, hashAlgorithm, padding, out bytesWritten))
            {
                return true;
            }

            bytesWritten = 0;
            return false;
        }

        public bool VerifyData(byte[] data, byte[] signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

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
            if (padding == null)
                throw new ArgumentNullException(nameof(padding));

            byte[] hash = HashData(data, offset, count, hashAlgorithm);
            return VerifyHash(hash, signature, hashAlgorithm, padding);
        }

        public bool VerifyData(Stream data, byte[] signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (signature == null)
                throw new ArgumentNullException(nameof(signature));
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
                throw HashAlgorithmNameNullOrEmpty();
            if (padding == null)
                throw new ArgumentNullException(nameof(padding));

            byte[] hash = HashData(data, hashAlgorithm);
            return VerifyHash(hash, signature, hashAlgorithm, padding);
        }

        public virtual bool VerifyData(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
        {
            if (string.IsNullOrEmpty(hashAlgorithm.Name))
            {
                throw HashAlgorithmNameNullOrEmpty();
            }
            if (padding == null)
            {
                throw new ArgumentNullException(nameof(padding));
            }

            for (int i = 256; ; i = checked(i * 2))
            {
                int hashLength = 0;
                byte[] hash = ArrayPool<byte>.Shared.Rent(i);
                try
                {
                    if (TryHashData(data, hash, hashAlgorithm, out hashLength))
                    {
                        return VerifyHash(new ReadOnlySpan<byte>(hash, 0, hashLength), signature, hashAlgorithm, padding);
                    }
                }
                finally
                {
                    Array.Clear(hash, 0, hashLength);
                    ArrayPool<byte>.Shared.Return(hash);
                }
            }
        }

        public virtual byte[] ExportRSAPrivateKey()
        {
            using (AsnWriter pkcs1PrivateKey = WritePkcs1PrivateKey())
            {
                return pkcs1PrivateKey.Encode();
            }
        }

        public virtual bool TryExportRSAPrivateKey(Span<byte> destination, out int bytesWritten)
        {
            using (AsnWriter pkcs1PrivateKey = WritePkcs1PrivateKey())
            {
                return pkcs1PrivateKey.TryEncode(destination, out bytesWritten);
            }
        }

        public virtual byte[] ExportRSAPublicKey()
        {
            using (AsnWriter pkcs1PublicKey = WritePkcs1PublicKey())
            {
                return pkcs1PublicKey.Encode();
            }
        }

        public virtual bool TryExportRSAPublicKey(Span<byte> destination, out int bytesWritten)
        {
            using (AsnWriter pkcs1PublicKey = WritePkcs1PublicKey())
            {
                return pkcs1PublicKey.TryEncode(destination, out bytesWritten);
            }
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
                byte[] rented = ArrayPool<byte>.Shared.Rent(rentSize);
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

                        using (AsnWriter writer = new AsnWriter(AsnEncodingRules.DER))
                        {
                            writer.PushSequence();
                            WriteAlgorithmIdentifier(writer);
                            writer.WriteBitString(rented.AsSpan(0, pkcs1Size));
                            writer.PopSequence();

                            return writer.TryEncode(destination, out bytesWritten);
                        }
                    }
                    finally
                    {
                        CryptographicOperations.ZeroMemory(rented.AsSpan(0, pkcs1Size));
                        ArrayPool<byte>.Shared.Return(rented);
                    }
                }
            }
        }

        public override bool TryExportPkcs8PrivateKey(Span<byte> destination, out int bytesWritten)
        {
            using (AsnWriter writer = WritePkcs8PrivateKey())
            {
                return writer.TryEncode(destination, out bytesWritten);
            }
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
                byte[] rented = ArrayPool<byte>.Shared.Rent(rentSize);
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

                        AsnWriter writer = new AsnWriter(AsnEncodingRules.BER);

                        try
                        {
                            writer.PushSequence();
                            // Version 0 format (no attributes)
                            writer.WriteInteger(0);
                            WriteAlgorithmIdentifier(writer);
                            writer.WriteOctetString(rented.AsSpan(0, pkcs1Size));
                            writer.PopSequence();

                            return writer;
                        }
                        catch
                        {
                            writer.Dispose();
                            throw;
                        }
                    }
                    finally
                    {
                        CryptographicOperations.ZeroMemory(rented.AsSpan(0, pkcs1Size));
                        ArrayPool<byte>.Shared.Return(rented);
                    }
                }
            }
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

            using (AsnWriter pkcs8PrivateKey = WritePkcs8PrivateKey())
            using (AsnWriter writer = KeyFormatHelper.WriteEncryptedPkcs8(
                password,
                pkcs8PrivateKey,
                pbeParameters))
            {
                return writer.TryEncode(destination, out bytesWritten);
            }
        }

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

            using (AsnWriter pkcs8PrivateKey = WritePkcs8PrivateKey())
            using (AsnWriter writer = KeyFormatHelper.WriteEncryptedPkcs8(
                passwordBytes,
                pkcs8PrivateKey,
                pbeParameters))
            {
                return writer.TryEncode(destination, out bytesWritten);
            }
        }
       
        private static void WriteAlgorithmIdentifier(AsnWriter writer)
        {
            writer.PushSequence();

            // https://tools.ietf.org/html/rfc3447#appendix-C
            //
            // --
            // -- When rsaEncryption is used in an AlgorithmIdentifier the
            // -- parameters MUST be present and MUST be NULL.
            // --
            writer.WriteObjectIdentifier(Oids.Rsa);
            writer.WriteNull();

            writer.PopSequence();
        }

        private AsnWriter WritePkcs1PublicKey()
        {
            RSAParameters rsaParameters = ExportParameters(false);

            if (rsaParameters.Modulus == null || rsaParameters.Exponent == null)
            {
                throw new InvalidOperationException(SR.Cryptography_InvalidRsaParameters);
            }

            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            writer.PushSequence();
            writer.WriteKeyParameterInteger(rsaParameters.Modulus);
            writer.WriteKeyParameterInteger(rsaParameters.Exponent);
            writer.PopSequence();

            return writer;
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
                    if (rsaParameters.Modulus == null || rsaParameters.Exponent == null)
                    {
                        throw new InvalidOperationException(SR.Cryptography_InvalidRsaParameters);
                    }

                    if (rsaParameters.D == null ||
                        rsaParameters.P == null ||
                        rsaParameters.Q == null ||
                        rsaParameters.DP == null ||
                        rsaParameters.DQ == null ||
                        rsaParameters.InverseQ == null)
                    {
                        throw new InvalidOperationException(SR.Cryptography_NotValidPrivateKey);
                    }

                    AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

                    writer.PushSequence();

                    // Format version 0
                    writer.WriteInteger(0);
                    writer.WriteKeyParameterInteger(rsaParameters.Modulus);
                    writer.WriteKeyParameterInteger(rsaParameters.Exponent);
                    writer.WriteKeyParameterInteger(rsaParameters.D);
                    writer.WriteKeyParameterInteger(rsaParameters.P);
                    writer.WriteKeyParameterInteger(rsaParameters.Q);
                    writer.WriteKeyParameterInteger(rsaParameters.DP);
                    writer.WriteKeyParameterInteger(rsaParameters.DQ);
                    writer.WriteKeyParameterInteger(rsaParameters.InverseQ);

                    writer.PopSequence();
                    return writer;
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
                    ReadOnlyMemory<byte> pkcs1 = KeyFormatHelper.ReadSubjectPublicKeyInfo(
                        s_validOids,
                        manager.Memory,
                        out int localRead);

                    ImportRSAPublicKey(pkcs1.Span, out _);
                    bytesRead = localRead;
                }
            }
        }

        public virtual unsafe void ImportRSAPublicKey(ReadOnlySpan<byte> source, out int bytesRead)
        {
            fixed (byte* ptr = &MemoryMarshal.GetReference(source))
            {
                using (MemoryManager<byte> manager = new PointerMemoryManager<byte>(ptr, source.Length))
                {
                    RSAPublicKey key = AsnSerializer.Deserialize<RSAPublicKey>(
                        manager.Memory,
                        AsnEncodingRules.BER,
                        out int localRead);

                    RSAParameters rsaParameters = new RSAParameters
                    {
                        Modulus = key.Modulus.ToByteArray(isUnsigned: true, isBigEndian: true),
                        Exponent = key.PublicExponent.ToByteArray(isUnsigned: true, isBigEndian: true),
                    };

                    ImportParameters(rsaParameters);
                    bytesRead = localRead;
                }
            }
        }

        public virtual unsafe void ImportRSAPrivateKey(ReadOnlySpan<byte> source, out int bytesRead)
        {
            fixed (byte* ptr = &MemoryMarshal.GetReference(source))
            {
                using (MemoryManager<byte> manager = new PointerMemoryManager<byte>(ptr, source.Length))
                {
                    RSAPrivateKeyAsn key =
                        AsnSerializer.Deserialize<RSAPrivateKeyAsn>(
                            manager.Memory,
                            AsnEncodingRules.BER,
                            out int localRead);

                    AlgorithmIdentifierAsn ignored = default;
                    FromPkcs1PrivateKey(key, ignored, out RSAParameters rsaParameters);

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

        public override unsafe void ImportPkcs8PrivateKey(ReadOnlySpan<byte> source, out int bytesRead)
        {
            fixed (byte* ptr = &MemoryMarshal.GetReference(source))
            {
                using (MemoryManager<byte> manager = new PointerMemoryManager<byte>(ptr, source.Length))
                {
                    ReadOnlyMemory<byte> pkcs1 = KeyFormatHelper.ReadPkcs8(
                        s_validOids,
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
            KeyFormatHelper.ReadEncryptedPkcs8<RSAParameters, RSAPrivateKeyAsn>(
                s_validOids,
                source,
                passwordBytes,
                FromPkcs1PrivateKey,
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
            KeyFormatHelper.ReadEncryptedPkcs8<RSAParameters, RSAPrivateKeyAsn>(
                s_validOids,
                source,
                password,
                FromPkcs1PrivateKey,
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

        private static void FromPkcs1PrivateKey(
            in RSAPrivateKeyAsn key,
            in AlgorithmIdentifierAsn algId,
            out RSAParameters ret)
        {
            if (!algId.HasNullEquivalentParameters())
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            const int MaxSupportedVersion = 0;

            if (key.Version > MaxSupportedVersion)
            {
                throw new CryptographicException(
                    SR.Format(
                        SR.Cryptography_RSAPrivateKey_VersionTooNew,
                        key.Version,
                        MaxSupportedVersion));
            }

            // The modulus size determines the encoded output size of the CRT parameters.
            byte[] n = key.Modulus.ToByteArray(isUnsigned: true, isBigEndian: true);
            int halfModulusLength = (n.Length + 1) / 2;

            ret = new RSAParameters
            {
                Modulus = n,
                Exponent = key.PublicExponent.ToByteArray(isUnsigned: true, isBigEndian: true),
                D = key.PrivateExponent.ExportKeyParameter(n.Length),
                P = key.Prime1.ExportKeyParameter(halfModulusLength),
                Q = key.Prime2.ExportKeyParameter(halfModulusLength),
                DP = key.Exponent1.ExportKeyParameter(halfModulusLength),
                DQ = key.Exponent2.ExportKeyParameter(halfModulusLength),
                InverseQ = key.Coefficient.ExportKeyParameter(halfModulusLength),
            };
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

        public override string KeyExchangeAlgorithm => "RSA";
        public override string SignatureAlgorithm => "RSA";

        private static Exception HashAlgorithmNameNullOrEmpty() =>
            new ArgumentException(SR.Cryptography_HashAlgorithmNameNullOrEmpty, "hashAlgorithm");
    }
}
