// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Formats.Asn1;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Apple;
using System.Security.Cryptography.Asn1;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
    public partial class RSA : AsymmetricAlgorithm
    {
        public static new RSA Create()
        {
            return new RSAImplementation.RSASecurityTransforms();
        }
    }
#endif

    internal static partial class RSAImplementation
    {
        public sealed partial class RSASecurityTransforms : RSA
        {
            private SecKeyPair? _keys;

            public RSASecurityTransforms()
                : this(2048)
            {
            }

            public RSASecurityTransforms(int keySize)
            {
                base.KeySize = keySize;
            }

            internal RSASecurityTransforms(SafeSecKeyRefHandle publicKey)
            {
                SetKey(SecKeyPair.PublicOnly(publicKey));
            }

            internal RSASecurityTransforms(SafeSecKeyRefHandle publicKey, SafeSecKeyRefHandle privateKey)
            {
                SetKey(SecKeyPair.PublicPrivatePair(publicKey, privateKey));
            }

            public override KeySizes[] LegalKeySizes
            {
                get
                {
                    return new KeySizes[]
                    {
                        // All values are in bits.
                        // 1024 was achieved via experimentation.
                        // 1024 and 1024+8 both generated successfully, 1024-8 produced errSecParam.
                        new KeySizes(minSize: 1024, maxSize: 16384, skipSize: 8),
                    };
                }
            }

            public override int KeySize
            {
                get
                {
                    return base.KeySize;
                }
                set
                {
                    if (KeySize == value)
                        return;

                    // Set the KeySize before freeing the key so that an invalid value doesn't throw away the key
                    base.KeySize = value;

                    ThrowIfDisposed();

                    if (_keys != null)
                    {
                        _keys.Dispose();
                        _keys = null;
                    }
                }
            }

            public override RSAParameters ExportParameters(bool includePrivateParameters)
            {
                // Apple requires all private keys to be exported encrypted, but since we're trying to export
                // as parsed structures we will need to decrypt it for the user.
                const string ExportPassword = "DotnetExportPassphrase";
                SecKeyPair keys = GetKeys();

                if (includePrivateParameters && keys.PrivateKey == null)
                {
                    throw new CryptographicException(SR.Cryptography_OpenInvalidHandle);
                }

                byte[] keyBlob = Interop.AppleCrypto.SecKeyExport(
                    includePrivateParameters ? keys.PrivateKey : keys.PublicKey,
                    exportPrivate: includePrivateParameters,
                    password: ExportPassword);

                try
                {
                    if (!includePrivateParameters)
                    {
                        // When exporting a key handle opened from a certificate, it seems to
                        // export as a PKCS#1 blob instead of an X509 SubjectPublicKeyInfo blob.
                        // So, check for that.
                        // NOTE: It doesn't affect macOS Mojave when SecCertificateCopyKey API
                        // is used.
                        RSAParameters key;

                        AsnReader reader = new AsnReader(keyBlob, AsnEncodingRules.BER);
                        AsnReader sequenceReader = reader.ReadSequence();

                        if (sequenceReader.PeekTag().Equals(Asn1Tag.Integer))
                        {
                            AlgorithmIdentifierAsn ignored = default;
                            RSAKeyFormatHelper.ReadRsaPublicKey(keyBlob, ignored, out key);
                        }
                        else
                        {
                            RSAKeyFormatHelper.ReadSubjectPublicKeyInfo(
                                keyBlob,
                                out int localRead,
                                out key);
                            Debug.Assert(localRead == keyBlob.Length);
                        }
                        return key;
                    }
                    else
                    {
                        RSAKeyFormatHelper.ReadEncryptedPkcs8(
                            keyBlob,
                            ExportPassword,
                            out int localRead,
                            out RSAParameters key);
                        return key;
                    }
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(keyBlob);
                }
            }

            public override void ImportParameters(RSAParameters parameters)
            {
                ValidateParameters(parameters);
                ThrowIfDisposed();

                bool isPrivateKey = parameters.D != null;

                if (isPrivateKey)
                {
                    // Start with the private key, in case some of the private key fields
                    // don't match the public key fields.
                    //
                    // Public import should go off without a hitch.
                    SafeSecKeyRefHandle privateKey = ImportKey(parameters);

                    RSAParameters publicOnly = new RSAParameters
                    {
                        Modulus = parameters.Modulus,
                        Exponent = parameters.Exponent,
                    };

                    SafeSecKeyRefHandle publicKey;
                    try
                    {
                        publicKey = ImportKey(publicOnly);
                    }
                    catch
                    {
                        privateKey.Dispose();
                        throw;
                    }

                    SetKey(SecKeyPair.PublicPrivatePair(publicKey, privateKey));
                }
                else
                {
                    SafeSecKeyRefHandle publicKey = ImportKey(parameters);
                    SetKey(SecKeyPair.PublicOnly(publicKey));
                }
            }

            public override unsafe void ImportSubjectPublicKeyInfo(
                ReadOnlySpan<byte> source,
                out int bytesRead)
            {
                ThrowIfDisposed();

                fixed (byte* ptr = &MemoryMarshal.GetReference(source))
                {
                    using (MemoryManager<byte> manager = new PointerMemoryManager<byte>(ptr, source.Length))
                    {
                        // Validate the DER value and get the number of bytes.
                        RSAKeyFormatHelper.ReadSubjectPublicKeyInfo(
                            manager.Memory,
                            out int localRead);

                        SafeSecKeyRefHandle publicKey = Interop.AppleCrypto.ImportEphemeralKey(source.Slice(0, localRead), false);
                        SetKey(SecKeyPair.PublicOnly(publicKey));

                        bytesRead = localRead;
                    }
                }
            }

            public override unsafe void ImportRSAPublicKey(ReadOnlySpan<byte> source, out int bytesRead)
            {
                ThrowIfDisposed();

                fixed (byte* ptr = &MemoryMarshal.GetReference(source))
                {
                    using (MemoryManager<byte> manager = new PointerMemoryManager<byte>(ptr, source.Length))
                    {
                        AsnReader reader = new AsnReader(manager.Memory, AsnEncodingRules.BER);
                        ReadOnlyMemory<byte> firstElement = reader.PeekEncodedValue();

                        SubjectPublicKeyInfoAsn spki = new SubjectPublicKeyInfoAsn
                        {
                            Algorithm = new AlgorithmIdentifierAsn
                            {
                                Algorithm = Oids.Rsa,
                                Parameters = AlgorithmIdentifierAsn.ExplicitDerNull,
                            },
                            SubjectPublicKey = firstElement,
                        };

                        AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
                        spki.Encode(writer);

                        ImportSubjectPublicKeyInfo(writer.Encode(), out _);
                        bytesRead = firstElement.Length;
                    }
                }
            }

            public override void ImportEncryptedPkcs8PrivateKey(
                ReadOnlySpan<byte> passwordBytes,
                ReadOnlySpan<byte> source,
                out int bytesRead)
            {
                ThrowIfDisposed();
                base.ImportEncryptedPkcs8PrivateKey(passwordBytes, source, out bytesRead);
            }

            public override void ImportEncryptedPkcs8PrivateKey(
                ReadOnlySpan<char> password,
                ReadOnlySpan<byte> source,
                out int bytesRead)
            {
                ThrowIfDisposed();
                base.ImportEncryptedPkcs8PrivateKey(password, source, out bytesRead);
            }

            public override byte[] Encrypt(byte[] data, RSAEncryptionPadding padding)
            {
                if (data == null)
                {
                    throw new ArgumentNullException(nameof(data));
                }
                if (padding == null)
                {
                    throw new ArgumentNullException(nameof(padding));
                }

                ThrowIfDisposed();

                // The size of encrypt is always the keysize (in ceiling-bytes)
                int outputSize = RsaPaddingProcessor.BytesRequiredForBitCount(KeySize);
                byte[] output = new byte[outputSize];

                if (!TryEncrypt(data, output, padding, out int bytesWritten))
                {
                    Debug.Fail($"TryEncrypt with a preallocated buffer should not fail");
                    throw new CryptographicException();
                }

                Debug.Assert(bytesWritten == outputSize);
                return output;
            }

            public override bool TryEncrypt(ReadOnlySpan<byte> data, Span<byte> destination, RSAEncryptionPadding padding, out int bytesWritten)
            {
                if (padding == null)
                {
                    throw new ArgumentNullException(nameof(padding));
                }

                ThrowIfDisposed();

                int rsaSize = RsaPaddingProcessor.BytesRequiredForBitCount(KeySize);

                if (destination.Length < rsaSize)
                {
                    bytesWritten = 0;
                    return false;
                }

                if (padding == RSAEncryptionPadding.Pkcs1 && data.Length > 0)
                {
                    const int Pkcs1PaddingOverhead = 11;
                    int maxAllowed = rsaSize - Pkcs1PaddingOverhead;

                    if (data.Length > maxAllowed)
                    {
                        throw new CryptographicException(
                            SR.Format(SR.Cryptography_Encryption_MessageTooLong, maxAllowed));
                    }

                    return Interop.AppleCrypto.TryRsaEncrypt(
                        GetKeys().PublicKey,
                        data,
                        destination,
                        padding,
                        out bytesWritten);
                }

                RsaPaddingProcessor? processor;

                switch (padding.Mode)
                {
                    case RSAEncryptionPaddingMode.Pkcs1:
                        processor = null;
                        break;
                    case RSAEncryptionPaddingMode.Oaep:
                        processor = RsaPaddingProcessor.OpenProcessor(padding.OaepHashAlgorithm);
                        break;
                    default:
                        throw new CryptographicException(SR.Cryptography_InvalidPaddingMode);
                }

                byte[] rented = CryptoPool.Rent(rsaSize);
                Span<byte> tmp = new Span<byte>(rented, 0, rsaSize);

                try
                {
                    if (processor != null)
                    {
                        processor.PadOaep(data, tmp);
                    }
                    else
                    {
                        Debug.Assert(padding.Mode == RSAEncryptionPaddingMode.Pkcs1);
                        RsaPaddingProcessor.PadPkcs1Encryption(data, tmp);
                    }

                    return Interop.AppleCrypto.TryRsaEncryptionPrimitive(
                        GetKeys().PublicKey,
                        tmp,
                        destination,
                        out bytesWritten);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(tmp);
                    CryptoPool.Return(rented, clearSize: 0);
                }
            }

            public override byte[] Decrypt(byte[] data, RSAEncryptionPadding padding)
            {
                if (data == null)
                {
                    throw new ArgumentNullException(nameof(data));
                }
                if (padding == null)
                {
                    throw new ArgumentNullException(nameof(padding));
                }

                SecKeyPair keys = GetKeys();

                if (keys.PrivateKey == null)
                {
                    throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);
                }

                int modulusSizeInBytes = RsaPaddingProcessor.BytesRequiredForBitCount(KeySize);

                if (data.Length != modulusSizeInBytes)
                {
                    throw new CryptographicException(SR.Cryptography_RSA_DecryptWrongSize);
                }

                if (padding.Mode == RSAEncryptionPaddingMode.Pkcs1)
                {
                    return Interop.AppleCrypto.RsaDecrypt(keys.PrivateKey, data, padding);
                }

                int maxOutputSize = RsaPaddingProcessor.BytesRequiredForBitCount(KeySize);
                byte[] rented = CryptoPool.Rent(maxOutputSize);
                int bytesWritten = 0;

                try
                {
                    if (!TryDecrypt(keys.PrivateKey, data, rented, padding, out bytesWritten))
                    {
                        Debug.Fail($"TryDecrypt returned false with a modulus-sized destination");
                        throw new CryptographicException();
                    }

                    Span<byte> contentsSpan = new Span<byte>(rented, 0, bytesWritten);
                    return contentsSpan.ToArray();
                }
                finally
                {
                    CryptoPool.Return(rented, bytesWritten);
                }
            }

            public override bool TryDecrypt(ReadOnlySpan<byte> data, Span<byte> destination, RSAEncryptionPadding padding, out int bytesWritten)
            {
                if (padding == null)
                {
                    throw new ArgumentNullException(nameof(padding));
                }

                SecKeyPair keys = GetKeys();

                if (keys.PrivateKey == null)
                {
                    throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);
                }

                return TryDecrypt(keys.PrivateKey, data, destination, padding, out bytesWritten);
            }

            private bool TryDecrypt(
                SafeSecKeyRefHandle privateKey,
                ReadOnlySpan<byte> data,
                Span<byte> destination,
                RSAEncryptionPadding padding,
                out int bytesWritten)
            {
                Debug.Assert(privateKey != null);

                if (padding.Mode != RSAEncryptionPaddingMode.Pkcs1 &&
                    padding.Mode != RSAEncryptionPaddingMode.Oaep)
                {
                    throw new CryptographicException(SR.Cryptography_InvalidPaddingMode);
                }

                int modulusSizeInBytes = RsaPaddingProcessor.BytesRequiredForBitCount(KeySize);

                if (data.Length != modulusSizeInBytes)
                {
                    throw new CryptographicException(SR.Cryptography_RSA_DecryptWrongSize);
                }

                if (padding.Mode == RSAEncryptionPaddingMode.Pkcs1 ||
                    padding == RSAEncryptionPadding.OaepSHA1)
                {
                    return Interop.AppleCrypto.TryRsaDecrypt(privateKey, data, destination, padding, out bytesWritten);
                }

                Debug.Assert(padding.Mode == RSAEncryptionPaddingMode.Oaep);
                RsaPaddingProcessor processor = RsaPaddingProcessor.OpenProcessor(padding.OaepHashAlgorithm);

                byte[] rented = CryptoPool.Rent(modulusSizeInBytes);
                Span<byte> unpaddedData = Span<byte>.Empty;

                try
                {
                    if (!Interop.AppleCrypto.TryRsaDecryptionPrimitive(privateKey, data, rented, out int paddedSize))
                    {
                        Debug.Fail($"Raw decryption failed with KeySize={KeySize} and a buffer length {rented.Length}");
                        throw new CryptographicException();
                    }

                    Debug.Assert(modulusSizeInBytes == paddedSize);
                    unpaddedData = new Span<byte>(rented, 0, paddedSize);
                    return processor.DepadOaep(unpaddedData, destination, out bytesWritten);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(unpaddedData);
                    CryptoPool.Return(rented, clearSize: 0);
                }
            }

            public override byte[] SignHash(byte[] hash, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
            {
                if (hash == null)
                    throw new ArgumentNullException(nameof(hash));
                if (string.IsNullOrEmpty(hashAlgorithm.Name))
                    throw HashAlgorithmNameNullOrEmpty();
                if (padding == null)
                    throw new ArgumentNullException(nameof(padding));

                ThrowIfDisposed();

                if (padding == RSASignaturePadding.Pkcs1)
                {
                    SecKeyPair keys = GetKeys();

                    if (keys.PrivateKey == null)
                    {
                        throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);
                    }

                    int expectedSize;
                    Interop.AppleCrypto.PAL_HashAlgorithm palAlgId =
                        PalAlgorithmFromAlgorithmName(hashAlgorithm, out expectedSize);

                    if (hash.Length != expectedSize)
                    {
                        // Windows: NTE_BAD_DATA ("Bad Data.")
                        // OpenSSL: RSA_R_INVALID_MESSAGE_LENGTH ("invalid message length")
                        throw new CryptographicException(
                            SR.Format(
                                SR.Cryptography_BadHashSize_ForAlgorithm,
                                hash.Length,
                                expectedSize,
                                hashAlgorithm.Name));
                    }

                    return Interop.AppleCrypto.GenerateSignature(
                        keys.PrivateKey,
                        hash,
                        palAlgId);
                }

                // A signature will always be the keysize (in ceiling-bytes) in length.
                int outputSize = RsaPaddingProcessor.BytesRequiredForBitCount(KeySize);
                byte[] output = new byte[outputSize];

                if (!TrySignHash(hash, output, hashAlgorithm, padding, out int bytesWritten))
                {
                    Debug.Fail("TrySignHash failed with a pre-allocated buffer");
                    throw new CryptographicException();
                }

                Debug.Assert(bytesWritten == outputSize);
                return output;
            }

            public override bool TrySignHash(ReadOnlySpan<byte> hash, Span<byte> destination, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding, out int bytesWritten)
            {
                if (string.IsNullOrEmpty(hashAlgorithm.Name))
                {
                    throw HashAlgorithmNameNullOrEmpty();
                }
                if (padding == null)
                {
                    throw new ArgumentNullException(nameof(padding));
                }

                ThrowIfDisposed();

                RsaPaddingProcessor? processor = null;

                if (padding.Mode == RSASignaturePaddingMode.Pss)
                {
                    processor = RsaPaddingProcessor.OpenProcessor(hashAlgorithm);
                }
                else if (padding != RSASignaturePadding.Pkcs1)
                {
                    throw new CryptographicException(SR.Cryptography_InvalidPaddingMode);
                }

                SecKeyPair keys = GetKeys();

                if (keys.PrivateKey == null)
                {
                    throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);
                }

                int keySize = KeySize;
                int rsaSize = RsaPaddingProcessor.BytesRequiredForBitCount(keySize);

                if (processor == null)
                {
                    Interop.AppleCrypto.PAL_HashAlgorithm palAlgId =
                        PalAlgorithmFromAlgorithmName(hashAlgorithm, out int expectedSize);

                    if (hash.Length != expectedSize)
                    {
                        // Windows: NTE_BAD_DATA ("Bad Data.")
                        // OpenSSL: RSA_R_INVALID_MESSAGE_LENGTH ("invalid message length")
                        throw new CryptographicException(
                            SR.Format(
                                SR.Cryptography_BadHashSize_ForAlgorithm,
                                hash.Length,
                                expectedSize,
                                hashAlgorithm.Name));
                    }

                    if (destination.Length < rsaSize)
                    {
                        bytesWritten = 0;
                        return false;
                    }

                    return Interop.AppleCrypto.TryGenerateSignature(
                        keys.PrivateKey,
                        hash,
                        destination,
                        palAlgId,
                        out bytesWritten);
                }

                Debug.Assert(padding.Mode == RSASignaturePaddingMode.Pss);

                if (destination.Length < rsaSize)
                {
                    bytesWritten = 0;
                    return false;
                }

                byte[] rented = CryptoPool.Rent(rsaSize);
                Span<byte> buf = new Span<byte>(rented, 0, rsaSize);
                processor.EncodePss(hash, buf, keySize);

                try
                {
                    return Interop.AppleCrypto.TryRsaSignaturePrimitive(keys.PrivateKey, buf, destination, out bytesWritten);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(buf);
                    CryptoPool.Return(rented, clearSize: 0);
                }
            }

            public override bool VerifyHash(
                byte[] hash,
                byte[] signature,
                HashAlgorithmName hashAlgorithm,
                RSASignaturePadding padding)
            {
                if (hash == null)
                {
                    throw new ArgumentNullException(nameof(hash));
                }
                if (signature == null)
                {
                    throw new ArgumentNullException(nameof(signature));
                }

                return VerifyHash((ReadOnlySpan<byte>)hash, (ReadOnlySpan<byte>)signature, hashAlgorithm, padding);
            }

            public override bool VerifyHash(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding)
            {
                if (string.IsNullOrEmpty(hashAlgorithm.Name))
                {
                    throw HashAlgorithmNameNullOrEmpty();
                }
                if (padding == null)
                {
                    throw new ArgumentNullException(nameof(padding));
                }

                ThrowIfDisposed();

                if (padding == RSASignaturePadding.Pkcs1)
                {
                    Interop.AppleCrypto.PAL_HashAlgorithm palAlgId =
                        PalAlgorithmFromAlgorithmName(hashAlgorithm, out int expectedSize);
                    return Interop.AppleCrypto.VerifySignature(GetKeys().PublicKey, hash, signature, palAlgId);
                }
                else if (padding.Mode == RSASignaturePaddingMode.Pss)
                {
                    RsaPaddingProcessor processor = RsaPaddingProcessor.OpenProcessor(hashAlgorithm);
                    SafeSecKeyRefHandle publicKey = GetKeys().PublicKey;

                    int keySize = KeySize;
                    int rsaSize = RsaPaddingProcessor.BytesRequiredForBitCount(keySize);

                    if (signature.Length != rsaSize)
                    {
                        return false;
                    }

                    if (hash.Length != processor.HashLength)
                    {
                        return false;
                    }

                    byte[] rented = CryptoPool.Rent(rsaSize);
                    Span<byte> unwrapped = new Span<byte>(rented, 0, rsaSize);

                    try
                    {
                        if (!Interop.AppleCrypto.TryRsaVerificationPrimitive(
                            publicKey,
                            signature,
                            unwrapped,
                            out int bytesWritten))
                        {
                            Debug.Fail($"TryRsaVerificationPrimitive with a pre-allocated buffer");
                            throw new CryptographicException();
                        }

                        Debug.Assert(bytesWritten == rsaSize);
                        return processor.VerifyPss(hash, unwrapped, keySize);
                    }
                    finally
                    {
                        CryptographicOperations.ZeroMemory(unwrapped);
                        CryptoPool.Return(rented, clearSize: 0);
                    }
                }

                throw new CryptographicException(SR.Cryptography_InvalidPaddingMode);
            }

            protected override byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm) =>
                AsymmetricAlgorithmHelpers.HashData(data, offset, count, hashAlgorithm);

            protected override byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm) =>
                AsymmetricAlgorithmHelpers.HashData(data, hashAlgorithm);

            protected override bool TryHashData(ReadOnlySpan<byte> data, Span<byte> destination, HashAlgorithmName hashAlgorithm, out int bytesWritten) =>
                AsymmetricAlgorithmHelpers.TryHashData(data, destination, hashAlgorithm, out bytesWritten);

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if (_keys != null)
                    {
                        // Do not set _keys to null, in order to prevent rehydration.
                        _keys.Dispose();
                    }
                }

                base.Dispose(disposing);
            }

            private static Interop.AppleCrypto.PAL_HashAlgorithm PalAlgorithmFromAlgorithmName(
                HashAlgorithmName hashAlgorithmName,
                out int hashSizeInBytes)
            {
                if (hashAlgorithmName == HashAlgorithmName.MD5)
                {
                    hashSizeInBytes = 128 >> 3;
                    return Interop.AppleCrypto.PAL_HashAlgorithm.Md5;
                }
                else if (hashAlgorithmName == HashAlgorithmName.SHA1)
                {
                    hashSizeInBytes = 160 >> 3;
                    return Interop.AppleCrypto.PAL_HashAlgorithm.Sha1;
                }
                else if (hashAlgorithmName == HashAlgorithmName.SHA256)
                {
                    hashSizeInBytes = 256 >> 3;
                    return Interop.AppleCrypto.PAL_HashAlgorithm.Sha256;
                }
                else if (hashAlgorithmName == HashAlgorithmName.SHA384)
                {
                    hashSizeInBytes = 384 >> 3;
                    return Interop.AppleCrypto.PAL_HashAlgorithm.Sha384;
                }
                else if (hashAlgorithmName == HashAlgorithmName.SHA512)
                {
                    hashSizeInBytes = 512 >> 3;
                    return Interop.AppleCrypto.PAL_HashAlgorithm.Sha512;
                }

                throw new CryptographicException(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithmName.Name);
            }

            private void ThrowIfDisposed()
            {
                SecKeyPair? current = _keys;

                if (current != null && current.PublicKey == null)
                {
                    throw new ObjectDisposedException(nameof(RSA));
                }
            }

            internal SecKeyPair GetKeys()
            {
                ThrowIfDisposed();
                SecKeyPair? current = _keys;

                if (current != null)
                {
                    return current;
                }

                SafeSecKeyRefHandle publicKey;
                SafeSecKeyRefHandle privateKey;

                Interop.AppleCrypto.RsaGenerateKey(KeySizeValue, out publicKey, out privateKey);

                current = SecKeyPair.PublicPrivatePair(publicKey, privateKey);
                _keys = current;
                return current;
            }

            private void SetKey(SecKeyPair newKeyPair)
            {
                ThrowIfDisposed();

                SecKeyPair? current = _keys;
                _keys = newKeyPair;
                current?.Dispose();

                if (newKeyPair != null)
                {
                    KeySizeValue = Interop.AppleCrypto.GetSimpleKeySizeInBits(newKeyPair.PublicKey);
                }
            }

            private static SafeSecKeyRefHandle ImportKey(RSAParameters parameters)
            {
                AsnWriter keyWriter;
                bool hasPrivateKey;

                if (parameters.D != null)
                {
                    keyWriter = RSAKeyFormatHelper.WritePkcs1PrivateKey(parameters);
                    hasPrivateKey = true;
                }
                else
                {
                    keyWriter = RSAKeyFormatHelper.WriteSubjectPublicKeyInfo(parameters);
                    hasPrivateKey = false;
                }

                byte[] rented = CryptoPool.Rent(keyWriter.GetEncodedLength());

                if (!keyWriter.TryEncode(rented, out int written))
                {
                    Debug.Fail("TryEncode failed with a pre-allocated buffer");
                    throw new InvalidOperationException();
                }

                // Explicitly clear the inner buffer
                keyWriter.Reset();

                try
                {
                    return Interop.AppleCrypto.ImportEphemeralKey(rented.AsSpan(0, written), hasPrivateKey);
                }
                finally
                {
                    CryptoPool.Return(rented, written);
                }
            }

            private static void ValidateParameters(in RSAParameters parameters)
            {
                if (parameters.Modulus == null || parameters.Exponent == null)
                    throw new CryptographicException(SR.Argument_InvalidValue);

                if (!HasConsistentPrivateKey(parameters))
                    throw new CryptographicException(SR.Argument_InvalidValue);
            }

            private static bool HasConsistentPrivateKey(in RSAParameters parameters)
            {
                if (parameters.D == null)
                {
                    if (parameters.P != null ||
                        parameters.DP != null ||
                        parameters.Q != null ||
                        parameters.DQ != null ||
                        parameters.InverseQ != null)
                    {
                        return false;
                    }
                }
                else
                {
                    if (parameters.P == null ||
                        parameters.DP == null ||
                        parameters.Q == null ||
                        parameters.DQ == null ||
                        parameters.InverseQ == null)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private static Exception HashAlgorithmNameNullOrEmpty() =>
            new ArgumentException(SR.Cryptography_HashAlgorithmNameNullOrEmpty, "hashAlgorithm");
    }
}
