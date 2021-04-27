// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;
using System.Security.Cryptography.Pkcs;

namespace System.Security.Cryptography
{
    internal static class PasswordBasedEncryption
    {
        internal const int IterationLimit = 600000;

        private static CryptographicException AlgorithmKdfRequiresChars(string algId)
        {
            return new CryptographicException(SR.Cryptography_AlgKdfRequiresChars, algId);
        }

        internal static void ValidatePbeParameters(
            PbeParameters pbeParameters,
            ReadOnlySpan<char> password,
            ReadOnlySpan<byte> passwordBytes)
        {
            // Leave the ArgumentNullException in the public entrypoints.
            Debug.Assert(pbeParameters != null);

            // Constructor promise.
            Debug.Assert(pbeParameters.IterationCount > 0);

            PbeEncryptionAlgorithm encryptionAlgorithm = pbeParameters.EncryptionAlgorithm;

            switch (encryptionAlgorithm)
            {
                case PbeEncryptionAlgorithm.Aes128Cbc:
                case PbeEncryptionAlgorithm.Aes192Cbc:
                case PbeEncryptionAlgorithm.Aes256Cbc:
                    return;
                case PbeEncryptionAlgorithm.TripleDes3KeyPkcs12:
                {
                    if (pbeParameters.HashAlgorithm != HashAlgorithmName.SHA1)
                    {
                        throw new CryptographicException(
                            SR.Cryptography_UnknownHashAlgorithm,
                            pbeParameters.HashAlgorithm.Name);
                    }

                    if (passwordBytes.Length > 0 && password.Length == 0)
                    {
                        throw AlgorithmKdfRequiresChars(
                            encryptionAlgorithm.ToString());
                    }

                    return;
                }
            }

            throw new CryptographicException(
                SR.Cryptography_UnknownAlgorithmIdentifier,
                encryptionAlgorithm.ToString());
        }

        [SuppressMessage("Microsoft.Security", "CA5350", Justification = "3DES used when specified by the input data")]
        [SuppressMessage("Microsoft.Security", "CA5351", Justification = "DES used when specified by the input data")]
        internal static unsafe int Decrypt(
            in AlgorithmIdentifierAsn algorithmIdentifier,
            ReadOnlySpan<char> password,
            ReadOnlySpan<byte> passwordBytes,
            ReadOnlySpan<byte> encryptedData,
            Span<byte> destination)
        {
            Debug.Assert(destination.Length >= encryptedData.Length);

            // Don't check that algorithmIdentifier.Parameters is set here.
            // Maybe some future PBES3 will have one with a default.

            HashAlgorithmName digestAlgorithmName;
            SymmetricAlgorithm? cipher = null;

            bool pkcs12 = false;

            switch (algorithmIdentifier.Algorithm)
            {
                case Oids.PbeWithMD5AndDESCBC:
                    digestAlgorithmName = HashAlgorithmName.MD5;
                    cipher = DES.Create();
                    break;
                case Oids.PbeWithMD5AndRC2CBC:
                    digestAlgorithmName = HashAlgorithmName.MD5;
                    cipher = RC2.Create();
                    break;
                case Oids.PbeWithSha1AndDESCBC:
                    digestAlgorithmName = HashAlgorithmName.SHA1;
                    cipher = DES.Create();
                    break;
                case Oids.PbeWithSha1AndRC2CBC:
                    digestAlgorithmName = HashAlgorithmName.SHA1;
                    cipher = RC2.Create();
                    break;
                case Oids.Pkcs12PbeWithShaAnd3Key3Des:
                    digestAlgorithmName = HashAlgorithmName.SHA1;
                    cipher = TripleDES.Create();
                    pkcs12 = true;
                    break;
                case Oids.Pkcs12PbeWithShaAnd2Key3Des:
                    digestAlgorithmName = HashAlgorithmName.SHA1;
                    cipher = TripleDES.Create();
                    cipher.KeySize = 128;
                    pkcs12 = true;
                    break;
                case Oids.Pkcs12PbeWithShaAnd128BitRC2:
                    digestAlgorithmName = HashAlgorithmName.SHA1;
                    cipher = RC2.Create();
                    cipher.KeySize = 128;
                    pkcs12 = true;
                    break;
                case Oids.Pkcs12PbeWithShaAnd40BitRC2:
                    digestAlgorithmName = HashAlgorithmName.SHA1;
                    cipher = RC2.Create();
                    cipher.KeySize = 40;
                    pkcs12 = true;
                    break;
                case Oids.PasswordBasedEncryptionScheme2:
                    return Pbes2Decrypt(
                        algorithmIdentifier.Parameters,
                        password,
                        passwordBytes,
                        encryptedData,
                        destination);
                default:
                    throw new CryptographicException(
                        SR.Format(
                            SR.Cryptography_UnknownAlgorithmIdentifier,
                            algorithmIdentifier.Algorithm));
            }

            Debug.Assert(digestAlgorithmName.Name != null);
            Debug.Assert(cipher != null);

            using (cipher)
            {
                if (pkcs12)
                {
                    if (password.IsEmpty && passwordBytes.Length > 0)
                    {
                        throw AlgorithmKdfRequiresChars(algorithmIdentifier.Algorithm);
                    }

                    return Pkcs12PbeDecrypt(
                        algorithmIdentifier,
                        password,
                        digestAlgorithmName,
                        cipher,
                        encryptedData,
                        destination);
                }

                using (IncrementalHash hasher = IncrementalHash.CreateHash(digestAlgorithmName))
                {
                    Span<byte> buf = stackalloc byte[128];
                    ReadOnlySpan<byte> effectivePasswordBytes = stackalloc byte[0];
                    byte[]? rented = null;
                    System.Text.Encoding? encoding = null;

                    if (passwordBytes.Length > 0 || password.Length == 0)
                    {
                        effectivePasswordBytes = passwordBytes;
                    }
                    else
                    {
                        encoding = System.Text.Encoding.UTF8;
                        int byteCount = encoding.GetByteCount(password);

                        if (byteCount > buf.Length)
                        {
                            rented = CryptoPool.Rent(byteCount);
                            buf = rented.AsSpan(0, byteCount);
                        }
                        else
                        {
                            buf = buf.Slice(0, byteCount);
                        }
                    }

                    fixed (byte* maybeRentedPtr = &MemoryMarshal.GetReference(buf))
                    {
                        if (encoding != null)
                        {
                            int written = encoding.GetBytes(password, buf);
                            Debug.Assert(written == buf.Length);
                            buf = buf.Slice(0, written);
                            effectivePasswordBytes = buf;
                        }

                        try
                        {
                            return Pbes1Decrypt(
                                algorithmIdentifier.Parameters,
                                effectivePasswordBytes,
                                hasher,
                                cipher,
                                encryptedData,
                                destination);
                        }
                        finally
                        {
                            CryptographicOperations.ZeroMemory(buf);

                            if (rented != null)
                            {
                                CryptoPool.Return(rented, clearSize: 0);
                            }
                        }
                    }
                }
            }
        }

        [SuppressMessage("Microsoft.Security", "CA5350", Justification = "3DES used when specified by the input data")]
        internal static void InitiateEncryption(
            PbeParameters pbeParameters,
            out SymmetricAlgorithm cipher,
            out string hmacOid,
            out string encryptionAlgorithmOid,
            out bool isPkcs12)
        {
            Debug.Assert(pbeParameters != null);

            isPkcs12 = false;

            switch (pbeParameters.EncryptionAlgorithm)
            {
                case PbeEncryptionAlgorithm.Aes128Cbc:
                    cipher = Aes.Create();
                    cipher.KeySize = 128;
                    encryptionAlgorithmOid = Oids.Aes128Cbc;
                    break;
                case PbeEncryptionAlgorithm.Aes192Cbc:
                    cipher = Aes.Create();
                    cipher.KeySize = 192;
                    encryptionAlgorithmOid = Oids.Aes192Cbc;
                    break;
                case PbeEncryptionAlgorithm.Aes256Cbc:
                    cipher = Aes.Create();
                    cipher.KeySize = 256;
                    encryptionAlgorithmOid = Oids.Aes256Cbc;
                    break;
                case PbeEncryptionAlgorithm.TripleDes3KeyPkcs12:
                    cipher = TripleDES.Create();
                    cipher.KeySize = 192;
                    encryptionAlgorithmOid = Oids.Pkcs12PbeWithShaAnd3Key3Des;
                    isPkcs12 = true;
                    break;
                default:
                    throw new CryptographicException(
                        SR.Format(
                            SR.Cryptography_UnknownAlgorithmIdentifier,
                            pbeParameters.HashAlgorithm.Name));
            }

            HashAlgorithmName prf = pbeParameters.HashAlgorithm;

            if (prf == HashAlgorithmName.SHA256)
            {
                hmacOid = Oids.HmacWithSha256;
            }
            else if (prf == HashAlgorithmName.SHA384)
            {
                hmacOid = Oids.HmacWithSha384;
            }
            else if (prf == HashAlgorithmName.SHA512)
            {
                hmacOid = Oids.HmacWithSha512;
            }
            else if (prf == HashAlgorithmName.SHA1)
            {
                hmacOid = Oids.HmacWithSha1;
            }
            else
            {
                cipher.Dispose();
                throw new CryptographicException(SR.Cryptography_UnknownHashAlgorithm, prf.Name);
            }

            // PKCS12-PBE should have been verified to be using SHA-1 already.
            Debug.Assert(hmacOid == Oids.HmacWithSha1 || !isPkcs12);
        }

        [SuppressMessage("Microsoft.Security", "CA5379", Justification = "SHA1 used if specified by argument")]
        internal static unsafe int Encrypt(
            ReadOnlySpan<char> password,
            ReadOnlySpan<byte> passwordBytes,
            SymmetricAlgorithm cipher,
            bool isPkcs12,
            AsnWriter source,
            PbeParameters pbeParameters,
            ReadOnlySpan<byte> salt,
            byte[] destination,
            Span<byte> ivDest)
        {
            byte[]? pwdTmpBytes = null;
            byte[] derivedKey;
            byte[] iv = cipher.IV;

            int sourceLength = source.GetEncodedLength();
            byte[] sourceRent = CryptoPool.Rent(sourceLength);
            int keySizeBytes = cipher.KeySize / 8;
            int iterationCount = pbeParameters.IterationCount;
            HashAlgorithmName prf = pbeParameters.HashAlgorithm;
            System.Text.Encoding encoding = System.Text.Encoding.UTF8;

            if (!isPkcs12)
            {
                if (passwordBytes.Length == 0 && password.Length > 0)
                {
                    pwdTmpBytes = new byte[encoding.GetByteCount(password)];
                }
                else if (passwordBytes.Length == 0)
                {
                    pwdTmpBytes = Array.Empty<byte>();
                }
                else
                {
                    pwdTmpBytes = new byte[passwordBytes.Length];
                }
            }

            fixed (byte* pkcs8RentPin = sourceRent)
            fixed (byte* pwdTmpBytesPtr = pwdTmpBytes)
            {
                if (isPkcs12)
                {
                    // Verified by ValidatePbeParameters, which should be called at entrypoints.
                    Debug.Assert(password.Length > 0 || passwordBytes.IsEmpty);
                    Debug.Assert(pbeParameters.HashAlgorithm == HashAlgorithmName.SHA1);

                    derivedKey = new byte[keySizeBytes];

                    Pkcs12Kdf.DeriveCipherKey(
                        password,
                        prf,
                        iterationCount,
                        salt,
                        derivedKey);

                    Pkcs12Kdf.DeriveIV(
                        password,
                        prf,
                        iterationCount,
                        salt,
                        iv);

                    ivDest.Clear();
                }
                else
                {
                    if (passwordBytes.Length > 0)
                    {
                        Debug.Assert(pwdTmpBytes!.Length == passwordBytes.Length);
                        passwordBytes.CopyTo(pwdTmpBytes);
                    }
                    else if (password.Length > 0)
                    {
                        int length = encoding.GetBytes(password, pwdTmpBytes);

                        if (length != pwdTmpBytes!.Length)
                        {
                            Debug.Fail($"UTF-8 encoding size changed between GetByteCount and GetBytes");
                            throw new CryptographicException();
                        }
                    }
                    else
                    {
                        Debug.Assert(pwdTmpBytes!.Length == 0);
                    }

                    using (var pbkdf2 = new Rfc2898DeriveBytes(pwdTmpBytes, salt.ToArray(), iterationCount, prf))
                    {
                        derivedKey = pbkdf2.GetBytes(keySizeBytes);
                    }

                    iv.CopyTo(ivDest);
                }

                fixed (byte* keyPtr = derivedKey)
                {
                    CryptographicOperations.ZeroMemory(pwdTmpBytes);

                    using (ICryptoTransform encryptor = cipher.CreateEncryptor(derivedKey, iv))
                    {
                        Debug.Assert(encryptor.CanTransformMultipleBlocks);

                        int blockSizeBytes = (cipher.BlockSize / 8);
                        int remaining = sourceLength % blockSizeBytes;
                        int fullBlocksLength = sourceLength - remaining;

                        try
                        {
                            if (!source.TryEncode(sourceRent, out _))
                            {
                                Debug.Fail("TryEncode failed with a pre-allocated buffer");
                                throw new CryptographicException();
                            }

                            int written = 0;

                            if (fullBlocksLength != 0)
                            {
                                written = encryptor.TransformBlock(
                                    sourceRent,
                                    0,
                                    fullBlocksLength,
                                    destination,
                                    0);
                            }

                            byte[] lastBlock = encryptor.TransformFinalBlock(
                                sourceRent,
                                written,
                                remaining);

                            lastBlock.AsSpan().CopyTo(destination.AsSpan(written));
                            return written + lastBlock.Length;
                        }
                        finally
                        {
                            CryptoPool.Return(sourceRent, sourceLength);
                        }
                    }
                }
            }
        }

        private static unsafe int Pbes2Decrypt(
            ReadOnlyMemory<byte>? algorithmParameters,
            ReadOnlySpan<char> password,
            ReadOnlySpan<byte> passwordBytes,
            ReadOnlySpan<byte> encryptedData,
            Span<byte> destination)
        {
            Span<byte> buf = stackalloc byte[128];
            ReadOnlySpan<byte> effectivePasswordBytes = stackalloc byte[0];
            byte[]? rented = null;
            System.Text.Encoding? encoding = null;

            if (passwordBytes.Length > 0 || password.Length == 0)
            {
                effectivePasswordBytes = passwordBytes;
            }
            else
            {
                encoding = System.Text.Encoding.UTF8;
                int byteCount = encoding.GetByteCount(password);

                if (byteCount > buf.Length)
                {
                    rented = CryptoPool.Rent(byteCount);
                    buf = rented.AsSpan(0, byteCount);
                }
                else
                {
                    buf = buf.Slice(0, byteCount);
                }
            }

            fixed (byte* maybeRentedPtr = &MemoryMarshal.GetReference(buf))
            {
                if (encoding != null)
                {
                    int written = encoding.GetBytes(password, buf);
                    Debug.Assert(written == buf.Length);
                    buf = buf.Slice(0, written);
                    effectivePasswordBytes = buf;
                }

                try
                {
                    return Pbes2Decrypt(
                        algorithmParameters,
                        effectivePasswordBytes,
                        encryptedData,
                        destination);
                }
                finally
                {
                    if (rented != null)
                    {
                        CryptoPool.Return(rented, buf.Length);
                    }
                }
            }
        }

        private static unsafe int Pbes2Decrypt(
            ReadOnlyMemory<byte>? algorithmParameters,
            ReadOnlySpan<byte> password,
            ReadOnlySpan<byte> encryptedData,
            Span<byte> destination)
        {
            if (!algorithmParameters.HasValue)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            PBES2Params pbes2Params = PBES2Params.Decode(algorithmParameters.Value, AsnEncodingRules.BER);

            if (pbes2Params.KeyDerivationFunc.Algorithm != Oids.Pbkdf2)
            {
                throw new CryptographicException(
                    SR.Format(
                        SR.Cryptography_UnknownAlgorithmIdentifier,
                        pbes2Params.EncryptionScheme.Algorithm));
            }

            Rfc2898DeriveBytes pbkdf2 =
                OpenPbkdf2(password, pbes2Params.KeyDerivationFunc.Parameters, out int? requestedKeyLength);

            using (pbkdf2)
            {
                // The biggest block size (for IV) we support is AES (128-bit / 16 byte)
                Span<byte> iv = stackalloc byte[16];

                SymmetricAlgorithm cipher = OpenCipher(
                    pbes2Params.EncryptionScheme,
                    requestedKeyLength,
                    ref iv);

                using (cipher)
                {
                    byte[] key = pbkdf2.GetBytes(cipher.KeySize / 8);

                    fixed (byte* keyPtr = key)
                    {
                        try
                        {
                            return Decrypt(cipher, key, iv, encryptedData, destination);
                        }
                        finally
                        {
                            CryptographicOperations.ZeroMemory(key);
                        }
                    }
                }
            }
        }

        [SuppressMessage("Microsoft.Security", "CA5350", Justification = "3DES used when specified by the input data")]
        [SuppressMessage("Microsoft.Security", "CA5351", Justification = "DES used when specified by the input data")]
        private static SymmetricAlgorithm OpenCipher(
            AlgorithmIdentifierAsn encryptionScheme,
            int? requestedKeyLength,
            ref Span<byte> iv)
        {
            string? algId = encryptionScheme.Algorithm;

            if (algId == Oids.Aes128Cbc ||
                algId == Oids.Aes192Cbc ||
                algId == Oids.Aes256Cbc)
            {
                // https://tools.ietf.org/html/rfc8018#appendix-B.2.5
                int correctKeySize;

                switch (algId)
                {
                    case Oids.Aes128Cbc:
                        correctKeySize = 16;
                        break;
                    case Oids.Aes192Cbc:
                        correctKeySize = 24;
                        break;
                    case Oids.Aes256Cbc:
                        correctKeySize = 32;
                        break;
                    default:
                        Debug.Fail("Key-sized OID included in the if, but not the switch");
                        throw new CryptographicException();
                }

                if (requestedKeyLength != null && requestedKeyLength != correctKeySize)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                // The parameters field ... shall have type OCTET STRING (SIZE(16))
                // specifying the initialization vector ...

                ReadIvParameter(encryptionScheme.Parameters, 16, ref iv);

                Aes aes = Aes.Create();
                aes.KeySize = correctKeySize * 8;
                return aes;
            }

            if (algId == Oids.TripleDesCbc)
            {
                // https://tools.ietf.org/html/rfc8018#appendix-B.2.2

                // ... has a 24-octet encryption key ...
                if (requestedKeyLength != null && requestedKeyLength != 24)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                // The parameters field associated with this OID ... shall have type
                // OCTET STRING (SIZE(8)) specifying the initialization vector ...
                ReadIvParameter(encryptionScheme.Parameters, 8, ref iv);
                return TripleDES.Create();
            }

            if (algId == Oids.Rc2Cbc)
            {
                // https://tools.ietf.org/html/rfc8018#appendix-B.2.3

                if (encryptionScheme.Parameters == null)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                // RC2 has a variable key size. RFC 8018 does not define a default,
                // so of PBKDF2 didn't provide it, fail.
                if (requestedKeyLength == null)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                Rc2CbcParameters rc2Parameters = Rc2CbcParameters.Decode(
                    encryptionScheme.Parameters.Value,
                    AsnEncodingRules.BER);

                // iv is the eight-octet initialization vector
                if (rc2Parameters.Iv.Length != 8)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                RC2 rc2 = RC2.Create();
                rc2.KeySize = requestedKeyLength.Value * 8;
                rc2.EffectiveKeySize = rc2Parameters.GetEffectiveKeyBits();

                rc2Parameters.Iv.Span.CopyTo(iv);
                iv = iv.Slice(0, rc2Parameters.Iv.Length);
                return rc2;
            }

            if (algId == Oids.DesCbc)
            {
                // https://tools.ietf.org/html/rfc8018#appendix-B.2.1

                // ... has an eight-octet encryption key ...
                if (requestedKeyLength != null && requestedKeyLength != 8)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                // The parameters field associated with this OID ... shall have type
                // OCTET STRING (SIZE(8)) specifying the initialization vector ...
                ReadIvParameter(encryptionScheme.Parameters, 8, ref iv);
                return DES.Create();
            }

            throw new CryptographicException(SR.Cryptography_UnknownAlgorithmIdentifier, algId);
        }

        private static void ReadIvParameter(
            ReadOnlyMemory<byte>? encryptionSchemeParameters,
            int length,
            ref Span<byte> iv)
        {
            if (encryptionSchemeParameters == null)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            try
            {
                ReadOnlySpan<byte> source = encryptionSchemeParameters.Value.Span;

                bool gotIv = AsnDecoder.TryReadOctetString(
                    source,
                    iv,
                    AsnEncodingRules.BER,
                    out int consumed,
                    out int bytesWritten);

                if (!gotIv || bytesWritten != length || consumed != source.Length)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                iv = iv.Slice(0, bytesWritten);
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        [SuppressMessage("Microsoft.Security", "CA5379", Justification = "SHA1 used if specified by argument")]
        private static unsafe Rfc2898DeriveBytes OpenPbkdf2(
            ReadOnlySpan<byte> password,
            ReadOnlyMemory<byte>? parameters,
            out int? requestedKeyLength)
        {
            if (!parameters.HasValue)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            Pbkdf2Params pbkdf2Params = Pbkdf2Params.Decode(parameters.Value, AsnEncodingRules.BER);

            // No OtherSource is defined in RFC 2898 or RFC 8018, so whatever
            // algorithm was requested isn't one we know.
            if (pbkdf2Params.Salt.OtherSource != null)
            {
                throw new CryptographicException(
                    SR.Format(
                        SR.Cryptography_UnknownAlgorithmIdentifier,
                        pbkdf2Params.Salt.OtherSource.Value.Algorithm));
            }

            if (pbkdf2Params.Salt.Specified == null)
            {
                Debug.Fail($"No Specified Salt value is present, indicating a new choice was unhandled");
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            HashAlgorithmName prf = pbkdf2Params.Prf.Algorithm switch
            {
                Oids.HmacWithSha1 => HashAlgorithmName.SHA1,
                Oids.HmacWithSha256 => HashAlgorithmName.SHA256,
                Oids.HmacWithSha384 => HashAlgorithmName.SHA384,
                Oids.HmacWithSha512 => HashAlgorithmName.SHA512,
                _ => throw new CryptographicException(
                        SR.Format(
                            SR.Cryptography_UnknownAlgorithmIdentifier,
                            pbkdf2Params.Prf.Algorithm)),
            };

            // All of the PRFs that we know about have NULL parameters, so check that now that we know
            // it's not just that we don't know the algorithm.

            if (!pbkdf2Params.Prf.HasNullEquivalentParameters())
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            int iterationCount = NormalizeIterationCount(pbkdf2Params.IterationCount);
            ReadOnlyMemory<byte> saltMemory = pbkdf2Params.Salt.Specified.Value;

            byte[] tmpPassword = new byte[password.Length];
            byte[] tmpSalt = new byte[saltMemory.Length];

            fixed (byte* tmpPasswordPtr = tmpPassword)
            fixed (byte* tmpSaltPtr = tmpSalt)
            {
                password.CopyTo(tmpPassword);
                saltMemory.CopyTo(tmpSalt);

                try
                {
                    requestedKeyLength = pbkdf2Params.KeyLength;

                    return new Rfc2898DeriveBytes(
                        tmpPassword,
                        tmpSalt,
                        iterationCount,
                        prf);
                }
                catch (ArgumentException e)
                {
                    // Salt too small is the most likely candidate.
                    throw new CryptographicException(SR.Argument_InvalidValue, e);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(tmpPassword);
                    CryptographicOperations.ZeroMemory(tmpSalt);
                }
            }
        }

        private static int Pbes1Decrypt(
            ReadOnlyMemory<byte>? algorithmParameters,
            ReadOnlySpan<byte> password,
            IncrementalHash hasher,
            SymmetricAlgorithm cipher,
            ReadOnlySpan<byte> encryptedData,
            Span<byte> destination)
        {
            // https://tools.ietf.org/html/rfc2898#section-6.1.2

            // 1. Obtain the eight-octet salt S and iteration count c.
            if (!algorithmParameters.HasValue)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            PBEParameter pbeParameters = PBEParameter.Decode(algorithmParameters.Value, AsnEncodingRules.BER);

            if (pbeParameters.Salt.Length != 8)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            if (pbeParameters.IterationCount < 1)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            int iterationCount = NormalizeIterationCount(pbeParameters.IterationCount);

            // 2. Apply PBKDF1<hash>(P, S, c, 16) to produce a derived key DK of length 16 octets
            Span<byte> dk = stackalloc byte[16];

            try
            {
                Pbkdf1(hasher, password, pbeParameters.Salt.Span, iterationCount, dk);

                // 3. Separate the derived key DK into an encryption key K consisting of the
                // first eight octets of DK and an initialization vector IV consisting of the
                // next 8.
                Span<byte> k = dk.Slice(0, 8);
                Span<byte> iv = dk.Slice(8, 8);

                // 4 & 5 together are "use CBC with what eventually became called PKCS7 padding"
                return Decrypt(cipher, k, iv, encryptedData, destination);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(dk);
            }
        }

        private static unsafe int Pkcs12PbeDecrypt(
            AlgorithmIdentifierAsn algorithmIdentifier,
            ReadOnlySpan<char> password,
            HashAlgorithmName hashAlgorithm,
            SymmetricAlgorithm cipher,
            ReadOnlySpan<byte> encryptedData,
            Span<byte> destination)
        {
            // https://tools.ietf.org/html/rfc7292#appendix-C

            if (!algorithmIdentifier.Parameters.HasValue)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            // 3DES, "two-key" 3DES, RC2-128 and RC2-40 are the only ciphers that should be here.
            // That means 64-bit block sizes and 192-bit keys (3DES-3).  So stack allocated key/IV are safe.
            if (cipher.KeySize > 256 || cipher.BlockSize > 256)
            {
                Debug.Fail(
                    $"Unexpected cipher characteristics by {cipher.GetType().FullName}, KeySize={cipher.KeySize}, BlockSize={cipher.BlockSize}");

                throw new CryptographicException();
            }

            PBEParameter pbeParameters = PBEParameter.Decode(
                algorithmIdentifier.Parameters.Value,
                AsnEncodingRules.BER);

            int iterationCount = NormalizeIterationCount(pbeParameters.IterationCount, IterationLimit);
            Span<byte> iv = stackalloc byte[cipher.BlockSize / 8];
            Span<byte> key = stackalloc byte[cipher.KeySize / 8];
            ReadOnlySpan<byte> saltSpan = pbeParameters.Salt.Span;

            try
            {
                Pkcs12Kdf.DeriveIV(
                    password,
                    hashAlgorithm,
                    iterationCount,
                    saltSpan,
                    iv);

                Pkcs12Kdf.DeriveCipherKey(
                    password,
                    hashAlgorithm,
                    iterationCount,
                    saltSpan,
                    key);

                return Decrypt(cipher, key, iv, encryptedData, destination);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(key);
                CryptographicOperations.ZeroMemory(iv);
            }
        }

        private static unsafe int Decrypt(
            SymmetricAlgorithm cipher,
            ReadOnlySpan<byte> key,
            ReadOnlySpan<byte> iv,
            ReadOnlySpan<byte> encryptedData,
            Span<byte> destination)
        {
            // When we define a Span-based decryption API this should be changed to use it.
            byte[] tmpKey = new byte[key.Length];
            byte[] tmpIv = new byte[iv.Length];
            byte[] rentedEncryptedData = CryptoPool.Rent(encryptedData.Length);
            byte[] rentedDestination = CryptoPool.Rent(destination.Length);

            // Keep all the arrays pinned so they can be correctly cleared
            fixed (byte* tmpKeyPtr = tmpKey)
            fixed (byte* tmpIvPtr = tmpIv)
            fixed (byte* rentedEncryptedDataPtr = rentedEncryptedData)
            fixed (byte* rentedDestinationPtr = rentedDestination)
            {
                try
                {
                    key.CopyTo(tmpKey);
                    iv.CopyTo(tmpIv);

                    using (ICryptoTransform decryptor = cipher.CreateDecryptor(tmpKey, tmpIv))
                    {
                        Debug.Assert(decryptor.CanTransformMultipleBlocks);

                        encryptedData.CopyTo(rentedEncryptedData);

                        int writeOffset = decryptor.TransformBlock(
                            rentedEncryptedData,
                            0,
                            encryptedData.Length,
                            rentedDestination,
                            0);

                        rentedDestination.AsSpan(0, writeOffset).CopyTo(destination);

                        byte[] tmpEnd = decryptor.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

                        fixed (byte* tmpEndPtr = tmpEnd)
                        {
                            Span<byte> tmpEndSpan = tmpEnd.AsSpan();
                            tmpEndSpan.CopyTo(destination.Slice(writeOffset));
                            CryptographicOperations.ZeroMemory(tmpEndSpan);
                        }

                        return writeOffset + tmpEnd.Length;
                    }
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(tmpKey);
                    CryptographicOperations.ZeroMemory(tmpIv);

                    CryptoPool.Return(rentedEncryptedData, encryptedData.Length);
                    CryptoPool.Return(rentedDestination, destination.Length);
                }
            }
        }

        private static void Pbkdf1(
            IncrementalHash hasher,
            ReadOnlySpan<byte> password,
            ReadOnlySpan<byte> salt,
            int iterationCount,
            Span<byte> dk)
        {
            // The only two hashes that will call into this implementation are
            // MD5 (16 bytes) and SHA-1 (20 bytes).
            Span<byte> t = stackalloc byte[20];

            // https://tools.ietf.org/html/rfc2898#section-5.1

            // T_1 = Hash(P || S)
            hasher.AppendData(password);
            hasher.AppendData(salt);

            if (!hasher.TryGetHashAndReset(t, out int tLength))
            {
                Debug.Fail("TryGetHashAndReset failed with pre-allocated input");
                throw new CryptographicException();
            }

            t = t.Slice(0, tLength);

            // T_i = H(T_(i-1))
            for (int i = 1; i < iterationCount; i++)
            {
                hasher.AppendData(t);

                if (!hasher.TryGetHashAndReset(t, out tLength) || tLength != t.Length)
                {
                    Debug.Fail("TryGetHashAndReset failed with pre-allocated input");
                    throw new CryptographicException();
                }
            }

            // DK = T_c<0..dkLen-1>
            t.Slice(0, dk.Length).CopyTo(dk);
            CryptographicOperations.ZeroMemory(t);
        }

        internal static void WritePbeAlgorithmIdentifier(
            AsnWriter writer,
            bool isPkcs12,
            string encryptionAlgorithmOid,
            Span<byte> salt,
            int iterationCount,
            string hmacOid,
            Span<byte> iv)
        {
            writer.PushSequence();

            if (isPkcs12)
            {
                writer.WriteObjectIdentifierForCrypto(encryptionAlgorithmOid);

                // pkcs-12PbeParams
                {
                    writer.PushSequence();
                    writer.WriteOctetString(salt);
                    writer.WriteInteger(iterationCount);
                    writer.PopSequence();
                }
            }
            else
            {
                writer.WriteObjectIdentifierForCrypto(Oids.PasswordBasedEncryptionScheme2);

                // PBES2-params
                {
                    writer.PushSequence();

                    // keyDerivationFunc
                    {
                        writer.PushSequence();
                        writer.WriteObjectIdentifierForCrypto(Oids.Pbkdf2);

                        // PBKDF2-params
                        {
                            writer.PushSequence();

                            writer.WriteOctetString(salt);
                            writer.WriteInteger(iterationCount);

                            // prf
                            if (hmacOid != Oids.HmacWithSha1)
                            {
                                writer.PushSequence();
                                writer.WriteObjectIdentifierForCrypto(hmacOid);
                                writer.WriteNull();
                                writer.PopSequence();
                            }

                            writer.PopSequence();
                        }

                        writer.PopSequence();
                    }

                    // encryptionScheme
                    {
                        writer.PushSequence();
                        writer.WriteObjectIdentifierForCrypto(encryptionAlgorithmOid);
                        writer.WriteOctetString(iv);
                        writer.PopSequence();
                    }

                    writer.PopSequence();
                }
            }

            writer.PopSequence();
        }

        internal static int NormalizeIterationCount(int iterationCount, int? iterationLimit = null)
        {
            if (iterationCount <= 0 || (iterationLimit.HasValue && iterationCount > iterationLimit.Value))
            {
                throw new CryptographicException(SR.Argument_InvalidValue);
            }

            return iterationCount;
        }
    }
}
