// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Cryptography;
using System.Buffers.Binary;
using System.Diagnostics;

using static Interop.BCrypt;
using static Interop.NCrypt;
using KeyBlobMagicNumber = Interop.BCrypt.KeyBlobMagicNumber;

namespace System.Security.Cryptography
{
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
    internal static partial class DSAImplementation
    {
#endif
        public sealed partial class DSACng : DSA
        {
            // For keysizes up to and including 1024 bits, CNG's blob format is BCRYPT_DSA_KEY_BLOB.
            // For keysizes exceeding 1024 bits, CNG's blob format is BCRYPT_DSA_KEY_BLOB_V2.
            private const int MaxV1KeySize = 1024;

            private const int Sha1HashOutputSize = 20;
            private const int Sha256HashOutputSize = 32;
            private const int Sha512HashOutputSize = 64;

            public override void ImportParameters(DSAParameters parameters)
            {
                if (parameters.P == null || parameters.Q == null || parameters.G == null || parameters.Y == null)
                    throw new ArgumentException(SR.Cryptography_InvalidDsaParameters_MissingFields);

                // J is not required and is not even used on CNG blobs. It should however be less than P (J == (P-1) / Q). This validation check
                // is just to maintain parity with DSACryptoServiceProvider, which also performs this check.
                if (parameters.J != null && parameters.J.Length >= parameters.P.Length)
                    throw new ArgumentException(SR.Cryptography_InvalidDsaParameters_MismatchedPJ);

                bool hasPrivateKey = parameters.X != null;

                int keySizeInBytes = parameters.P.Length;
                int keySizeInBits = keySizeInBytes * 8;

                if (parameters.G.Length != keySizeInBytes || parameters.Y.Length != keySizeInBytes)
                    throw new ArgumentException(SR.Cryptography_InvalidDsaParameters_MismatchedPGY);

                if (hasPrivateKey && parameters.X!.Length != parameters.Q.Length)
                    throw new ArgumentException(SR.Cryptography_InvalidDsaParameters_MismatchedQX);

                byte[] blob;
                if (keySizeInBits <= MaxV1KeySize)
                {
                    GenerateV1DsaBlob(out blob, parameters, keySizeInBytes, hasPrivateKey);
                }
                else
                {
                    GenerateV2DsaBlob(out blob, parameters, keySizeInBytes, hasPrivateKey);
                }

                ImportKeyBlob(blob, hasPrivateKey);
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

            public override byte[] ExportEncryptedPkcs8PrivateKey(
                ReadOnlySpan<byte> passwordBytes,
                PbeParameters pbeParameters)
            {
                if (pbeParameters == null)
                    throw new ArgumentNullException(nameof(pbeParameters));

                return CngPkcs8.ExportEncryptedPkcs8PrivateKey(
                    this,
                    passwordBytes,
                    pbeParameters);
            }

            public override byte[] ExportEncryptedPkcs8PrivateKey(
                ReadOnlySpan<char> password,
                PbeParameters pbeParameters)
            {
                if (pbeParameters == null)
                {
                    throw new ArgumentNullException(nameof(pbeParameters));
                }

                PasswordBasedEncryption.ValidatePbeParameters(
                    pbeParameters,
                    password,
                    ReadOnlySpan<byte>.Empty);

                if (CngPkcs8.IsPlatformScheme(pbeParameters))
                {
                    return ExportEncryptedPkcs8(password, pbeParameters.IterationCount);
                }

                return CngPkcs8.ExportEncryptedPkcs8PrivateKey(
                    this,
                    password,
                    pbeParameters);
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

                return CngPkcs8.TryExportEncryptedPkcs8PrivateKey(
                    this,
                    passwordBytes,
                    pbeParameters,
                    destination,
                    out bytesWritten);
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

                if (CngPkcs8.IsPlatformScheme(pbeParameters))
                {
                    return TryExportEncryptedPkcs8(
                        password,
                        pbeParameters.IterationCount,
                        destination,
                        out bytesWritten);
                }

                return CngPkcs8.TryExportEncryptedPkcs8PrivateKey(
                    this,
                    password,
                    pbeParameters,
                    destination,
                    out bytesWritten);
            }

            private static unsafe void GenerateV1DsaBlob(out byte[] blob, DSAParameters parameters, int cbKey, bool includePrivate)
            {
                // We need to build a key blob structured as follows:
                //
                //     BCRYPT_DSA_KEY_BLOB       header
                //     byte[cbKey]               P
                //     byte[cbKey]               G
                //     byte[cbKey]               Y
                //     -- Private only --
                //     byte[Sha1HashOutputSize]  X

                int blobSize =
                    sizeof(BCRYPT_DSA_KEY_BLOB) +
                    cbKey +
                    cbKey +
                    cbKey;

                if (includePrivate)
                {
                    blobSize += Sha1HashOutputSize;
                }

                blob = new byte[blobSize];
                fixed (byte* pDsaBlob = &blob[0])
                {
                    // Build the header
                    BCRYPT_DSA_KEY_BLOB* pBcryptBlob = (BCRYPT_DSA_KEY_BLOB*)pDsaBlob;

                    pBcryptBlob->Magic = includePrivate ? KeyBlobMagicNumber.BCRYPT_DSA_PRIVATE_MAGIC : KeyBlobMagicNumber.BCRYPT_DSA_PUBLIC_MAGIC;
                    pBcryptBlob->cbKey = cbKey;

                    int offset = sizeof(KeyBlobMagicNumber) + sizeof(int); // skip Magic and cbKey

                    if (parameters.Seed != null)
                    {
                        // The Seed length is hardcoded into BCRYPT_DSA_KEY_BLOB, so check it now we can give a nicer error message.
                        if (parameters.Seed.Length != Sha1HashOutputSize)
                            throw new ArgumentException(SR.Cryptography_InvalidDsaParameters_SeedRestriction_ShortKey);

                        Interop.BCrypt.EmitBigEndian(blob, ref offset, parameters.Counter);
                        Interop.BCrypt.Emit(blob, ref offset, parameters.Seed);
                    }
                    else
                    {
                        // If Seed is not present, back fill both counter and seed with 0xff. Do not use parameters.Counter as CNG is more strict than CAPI and will reject
                        // anything other than 0xffffffff. That could complicate efforts to switch usage of DSACryptoServiceProvider to DSACng.
                        Interop.BCrypt.EmitByte(blob, ref offset, 0xff, Sha1HashOutputSize + sizeof(int));
                    }

                    // The Q length is hardcoded into BCRYPT_DSA_KEY_BLOB, so check it now we can give a nicer error message.
                    if (parameters.Q!.Length != Sha1HashOutputSize)
                        throw new ArgumentException(SR.Cryptography_InvalidDsaParameters_QRestriction_ShortKey);

                    Interop.BCrypt.Emit(blob, ref offset, parameters.Q);

                    Debug.Assert(offset == sizeof(BCRYPT_DSA_KEY_BLOB), $"Expected offset = sizeof(BCRYPT_DSA_KEY_BLOB), got {offset} != {sizeof(BCRYPT_DSA_KEY_BLOB)}");

                    Interop.BCrypt.Emit(blob, ref offset, parameters.P!);
                    Interop.BCrypt.Emit(blob, ref offset, parameters.G!);
                    Interop.BCrypt.Emit(blob, ref offset, parameters.Y!);
                    if (includePrivate)
                    {
                        Interop.BCrypt.Emit(blob, ref offset, parameters.X!);
                    }

                    Debug.Assert(offset == blobSize, $"Expected offset = blobSize, got {offset} != {blobSize}");
                }
            }

            private static unsafe void GenerateV2DsaBlob(out byte[] blob, DSAParameters parameters, int cbKey, bool includePrivateParameters)
            {
                // We need to build a key blob structured as follows:
                //     BCRYPT_DSA_KEY_BLOB_V2  header
                //     byte[cbSeedLength]      Seed
                //     byte[cbGroupSize]       Q
                //     byte[cbKey]             P
                //     byte[cbKey]             G
                //     byte[cbKey]             Y
                //     -- Private only --
                //     byte[cbGroupSize]       X

                int blobSize =
                    sizeof(BCRYPT_DSA_KEY_BLOB_V2) +
                    (parameters.Seed == null ? parameters.Q!.Length : parameters.Seed.Length) + // Use Q size if Seed is not present
                    parameters.Q!.Length +
                    parameters.P!.Length +
                    parameters.G!.Length +
                    parameters.Y!.Length +
                    (includePrivateParameters ? parameters.X!.Length : 0);

                blob = new byte[blobSize];
                fixed (byte* pDsaBlob = &blob[0])
                {
                    // Build the header
                    BCRYPT_DSA_KEY_BLOB_V2* pBcryptBlob = (BCRYPT_DSA_KEY_BLOB_V2*)pDsaBlob;

                    pBcryptBlob->Magic = includePrivateParameters ? KeyBlobMagicNumber.BCRYPT_DSA_PRIVATE_MAGIC_V2 : KeyBlobMagicNumber.BCRYPT_DSA_PUBLIC_MAGIC_V2;
                    pBcryptBlob->cbKey = cbKey;

                    // For some reason, Windows bakes the hash algorithm into the key itself. Furthermore, it demands that the Q length match the
                    // length of the named hash algorithm's output - otherwise, the Import fails. So we have to give it the hash algorithm that matches
                    // the Q length - and if there is no matching hash algorithm, we throw up our hands and throw a PlatformNotSupported.
                    //
                    // Note that this has no bearing on the hash algorithm you pass to SignData(). The class library (not Windows) hashes that according
                    // to the hash algorithm passed to SignData() and presents the hash result to NCryptSignHash(), truncating the hash to the Q length
                    // if necessary (and as demanded by the NIST spec.) Windows will be no wiser and we'll get the result we want.
                    pBcryptBlob->hashAlgorithm = parameters.Q.Length switch
                    {
                        Sha1HashOutputSize => HASHALGORITHM_ENUM.DSA_HASH_ALGORITHM_SHA1,
                        Sha256HashOutputSize => HASHALGORITHM_ENUM.DSA_HASH_ALGORITHM_SHA256,
                        Sha512HashOutputSize => HASHALGORITHM_ENUM.DSA_HASH_ALGORITHM_SHA512,
                        _ => throw new PlatformNotSupportedException(SR.Cryptography_InvalidDsaParameters_QRestriction_LargeKey),
                    };
                    pBcryptBlob->standardVersion = DSAFIPSVERSION_ENUM.DSA_FIPS186_3;

                    int offset = sizeof(BCRYPT_DSA_KEY_BLOB_V2) - 4; //skip to Count[4]

                    if (parameters.Seed != null)
                    {
                        Interop.BCrypt.EmitBigEndian(blob, ref offset, parameters.Counter);
                        Debug.Assert(offset == sizeof(BCRYPT_DSA_KEY_BLOB_V2), $"Expected offset = sizeof(BCRYPT_DSA_KEY_BLOB_V2), got {offset} != {sizeof(BCRYPT_DSA_KEY_BLOB_V2)}");
                        pBcryptBlob->cbSeedLength = parameters.Seed.Length;
                        pBcryptBlob->cbGroupSize = parameters.Q.Length;
                        Interop.BCrypt.Emit(blob, ref offset, parameters.Seed);
                    }
                    else
                    {
                        // If Seed is not present, back fill both counter and seed with 0xff. Do not use parameters.Counter as CNG is more strict than CAPI and will reject
                        // anything other than 0xffffffff. That could complicate efforts to switch usage of DSACryptoServiceProvider to DSACng.
                        Interop.BCrypt.EmitByte(blob, ref offset, 0xff, sizeof(int));
                        Debug.Assert(offset == sizeof(BCRYPT_DSA_KEY_BLOB_V2), $"Expected offset = sizeof(BCRYPT_DSA_KEY_BLOB_V2), got {offset} != {sizeof(BCRYPT_DSA_KEY_BLOB_V2)}");
                        int defaultSeedLength = parameters.Q.Length;
                        pBcryptBlob->cbSeedLength = defaultSeedLength;
                        pBcryptBlob->cbGroupSize = parameters.Q.Length;
                        Interop.BCrypt.EmitByte(blob, ref offset, 0xff, defaultSeedLength);
                    }

                    Interop.BCrypt.Emit(blob, ref offset, parameters.Q);
                    Interop.BCrypt.Emit(blob, ref offset, parameters.P);
                    Interop.BCrypt.Emit(blob, ref offset, parameters.G);
                    Interop.BCrypt.Emit(blob, ref offset, parameters.Y);

                    if (includePrivateParameters)
                    {
                        Interop.BCrypt.Emit(blob, ref offset, parameters.X!);
                    }

                    Debug.Assert(offset == blobSize, $"Expected offset = blobSize, got {offset} != {blobSize}");
                }
            }

            public override DSAParameters ExportParameters(bool includePrivateParameters)
            {
                byte[] dsaBlob = ExportKeyBlob(includePrivateParameters);

                KeyBlobMagicNumber magic = (KeyBlobMagicNumber)BitConverter.ToInt32(dsaBlob, 0);

                // Check the magic value in the key blob header. If the blob does not have the required magic,
                // then throw a CryptographicException.
                CheckMagicValueOfKey(magic, includePrivateParameters);

                unsafe
                {
                    DSAParameters dsaParams = default;

                    fixed (byte* pDsaBlob = dsaBlob)
                    {
                        int offset;
                        if (magic == KeyBlobMagicNumber.BCRYPT_DSA_PUBLIC_MAGIC || magic == KeyBlobMagicNumber.BCRYPT_DSA_PRIVATE_MAGIC)
                        {
                            if (dsaBlob.Length < sizeof(BCRYPT_DSA_KEY_BLOB))
                                throw ErrorCode.E_FAIL.ToCryptographicException();

                            BCRYPT_DSA_KEY_BLOB* pBcryptBlob = (BCRYPT_DSA_KEY_BLOB*)pDsaBlob;
                            // We now have a buffer laid out as follows:
                            //     BCRYPT_DSA_KEY_BLOB  header
                            //     byte[cbKey]               P
                            //     byte[cbKey]               G
                            //     byte[cbKey]               Y
                            //     -- Private only --
                            //     byte[Sha1HashOutputSize]  X

                            offset = sizeof(KeyBlobMagicNumber) + sizeof(int); // skip Magic and cbKey

                            // Read out a (V1) BCRYPT_DSA_KEY_BLOB structure.
                            dsaParams.Counter = BinaryPrimitives.ReadInt32BigEndian(Interop.BCrypt.Consume(dsaBlob, ref offset, 4));
                            dsaParams.Seed = Interop.BCrypt.Consume(dsaBlob, ref offset, Sha1HashOutputSize);
                            dsaParams.Q = Interop.BCrypt.Consume(dsaBlob, ref offset, Sha1HashOutputSize);

                            Debug.Assert(offset == sizeof(BCRYPT_DSA_KEY_BLOB), $"Expected offset = sizeof(BCRYPT_DSA_KEY_BLOB), got {offset} != {sizeof(BCRYPT_DSA_KEY_BLOB)}");
                            dsaParams.P = Interop.BCrypt.Consume(dsaBlob, ref offset, pBcryptBlob->cbKey);
                            dsaParams.G = Interop.BCrypt.Consume(dsaBlob, ref offset, pBcryptBlob->cbKey);
                            dsaParams.Y = Interop.BCrypt.Consume(dsaBlob, ref offset, pBcryptBlob->cbKey);
                            if (includePrivateParameters)
                            {
                                dsaParams.X = Interop.BCrypt.Consume(dsaBlob, ref offset, Sha1HashOutputSize);
                            }
                        }
                        else
                        {
                            Debug.Assert(magic == KeyBlobMagicNumber.BCRYPT_DSA_PUBLIC_MAGIC_V2 || magic == KeyBlobMagicNumber.BCRYPT_DSA_PRIVATE_MAGIC_V2);

                            if (dsaBlob.Length < sizeof(BCRYPT_DSA_KEY_BLOB_V2))
                                throw ErrorCode.E_FAIL.ToCryptographicException();

                            BCRYPT_DSA_KEY_BLOB_V2* pBcryptBlob = (BCRYPT_DSA_KEY_BLOB_V2*)pDsaBlob;
                            // We now have a buffer laid out as follows:
                            //     BCRYPT_DSA_KEY_BLOB_V2  header
                            //     byte[cbSeedLength]      Seed
                            //     byte[cbGroupSize]       Q
                            //     byte[cbKey]             P
                            //     byte[cbKey]             G
                            //     byte[cbKey]             Y
                            //     -- Private only --
                            //     byte[cbGroupSize]       X

                            offset = sizeof(BCRYPT_DSA_KEY_BLOB_V2) - 4; //skip to Count[4]

                            // Read out a BCRYPT_DSA_KEY_BLOB_V2 structure.
                            dsaParams.Counter = BinaryPrimitives.ReadInt32BigEndian(Interop.BCrypt.Consume(dsaBlob, ref offset, 4));

                            Debug.Assert(offset == sizeof(BCRYPT_DSA_KEY_BLOB_V2), $"Expected offset = sizeof(BCRYPT_DSA_KEY_BLOB_V2), got {offset} != {sizeof(BCRYPT_DSA_KEY_BLOB_V2)}");

                            dsaParams.Seed = Interop.BCrypt.Consume(dsaBlob, ref offset, pBcryptBlob->cbSeedLength);
                            dsaParams.Q = Interop.BCrypt.Consume(dsaBlob, ref offset, pBcryptBlob->cbGroupSize);
                            dsaParams.P = Interop.BCrypt.Consume(dsaBlob, ref offset, pBcryptBlob->cbKey);
                            dsaParams.G = Interop.BCrypt.Consume(dsaBlob, ref offset, pBcryptBlob->cbKey);
                            dsaParams.Y = Interop.BCrypt.Consume(dsaBlob, ref offset, pBcryptBlob->cbKey);
                            if (includePrivateParameters)
                            {
                                dsaParams.X = Interop.BCrypt.Consume(dsaBlob, ref offset, pBcryptBlob->cbGroupSize);
                            }
                        }

                        // If no counter/seed information is present, normalize Counter and Seed to 0/null to maintain parity with the CAPI version of DSA.
                        if (dsaParams.Counter == -1)
                        {
                            dsaParams.Counter = 0;
                            dsaParams.Seed = null;
                        }

                        Debug.Assert(offset == dsaBlob.Length, $"Expected offset = dsaBlob.Length, got {offset} != {dsaBlob.Length}");

                        return dsaParams;
                    }
                }
            }

            /// <summary>
            ///     This function checks the magic value in the key blob header
            /// </summary>
            /// <param name="magic">The expected magic number.</param>
            /// <param name="includePrivateParameters">Private blob if true else public key blob</param>
            private static void CheckMagicValueOfKey(KeyBlobMagicNumber magic, bool includePrivateParameters)
            {
                if (includePrivateParameters)
                {
                    if (magic != KeyBlobMagicNumber.BCRYPT_DSA_PRIVATE_MAGIC && magic != KeyBlobMagicNumber.BCRYPT_DSA_PRIVATE_MAGIC_V2)
                        throw new CryptographicException(SR.Cryptography_NotValidPrivateKey);
                }
                else
                {
                    if (magic != KeyBlobMagicNumber.BCRYPT_DSA_PUBLIC_MAGIC && magic != KeyBlobMagicNumber.BCRYPT_DSA_PUBLIC_MAGIC_V2)
                        throw new CryptographicException(SR.Cryptography_NotValidPublicOrPrivateKey);
                }
            }

        }
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
    }
#endif
}
