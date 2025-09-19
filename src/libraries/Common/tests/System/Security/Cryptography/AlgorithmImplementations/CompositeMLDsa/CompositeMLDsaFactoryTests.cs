// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;
using Microsoft.DotNet.RemoteExecutor;
using Test.Cryptography;
using Xunit;
using Xunit.Sdk;

namespace System.Security.Cryptography.Tests
{
    public static class CompositeMLDsaFactoryTests
    {
        [Fact]
        public static void NullArgumentValidation()
        {
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () => CompositeMLDsa.GenerateKey(null));
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () => CompositeMLDsa.IsAlgorithmSupported(null));
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () => CompositeMLDsa.ImportCompositeMLDsaPrivateKey(null, null));
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () => CompositeMLDsa.ImportCompositeMLDsaPrivateKey(null, ReadOnlySpan<byte>.Empty));
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () => CompositeMLDsa.ImportCompositeMLDsaPublicKey(null, null));
            AssertExtensions.Throws<ArgumentNullException>("algorithm", static () => CompositeMLDsa.ImportCompositeMLDsaPublicKey(null, ReadOnlySpan<byte>.Empty));

            AssertExtensions.Throws<ArgumentNullException>("source", static () => CompositeMLDsa.ImportCompositeMLDsaPrivateKey(CompositeMLDsaAlgorithm.MLDsa44WithECDsaP256, null));
            AssertExtensions.Throws<ArgumentNullException>("source", static () => CompositeMLDsa.ImportCompositeMLDsaPublicKey(CompositeMLDsaAlgorithm.MLDsa44WithECDsaP256, null));
            AssertExtensions.Throws<ArgumentNullException>("source", static () => CompositeMLDsa.ImportPkcs8PrivateKey(null));
            AssertExtensions.Throws<ArgumentNullException>("source", static () => CompositeMLDsa.ImportSubjectPublicKeyInfo(null));
            AssertExtensions.Throws<ArgumentNullException>("source", static () => CompositeMLDsa.ImportFromPem(null));
            AssertExtensions.Throws<ArgumentNullException>("source", static () => CompositeMLDsa.ImportEncryptedPkcs8PrivateKey("PLACEHOLDER", null));
            AssertExtensions.Throws<ArgumentNullException>("source", static () => CompositeMLDsa.ImportFromEncryptedPem(null, (string)null));
            AssertExtensions.Throws<ArgumentNullException>("source", static () => CompositeMLDsa.ImportFromEncryptedPem(null, (byte[])null));

            AssertExtensions.Throws<ArgumentNullException>("password", static () => CompositeMLDsa.ImportEncryptedPkcs8PrivateKey((string)null, null));
            AssertExtensions.Throws<ArgumentNullException>("password", static () => CompositeMLDsa.ImportFromEncryptedPem(string.Empty, (string)null));

            AssertExtensions.Throws<ArgumentNullException>("passwordBytes", static () => CompositeMLDsa.ImportFromEncryptedPem(string.Empty, (byte[])null));
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void ImportBadPrivateKey_Empty(CompositeMLDsaAlgorithm algorithm)
        {
            AssertImportBadPrivateKey(algorithm, Array.Empty<byte>());
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void ImportBadPrivateKey_ShortMLDsaSeed(CompositeMLDsaAlgorithm algorithm)
        {
            MLDsaKeyInfo mldsaVector = CompositeMLDsaTestData.GetMLDsaIetfTestVector(algorithm);
            AssertImportBadPrivateKey(algorithm, new byte[mldsaVector.PrivateSeed.Length - 1]);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void ImportBadPrivateKey_OnlyMLDsaSeed(CompositeMLDsaAlgorithm algorithm)
        {
            MLDsaKeyInfo mldsaVector = CompositeMLDsaTestData.GetMLDsaIetfTestVector(algorithm);
            AssertImportBadPrivateKey(algorithm, mldsaVector.PrivateSeed);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void ImportBadPrivateKey_ShortTradKey(CompositeMLDsaAlgorithm algorithm)
        {
            MLDsaKeyInfo mldsaVector = CompositeMLDsaTestData.GetMLDsaIetfTestVector(algorithm);
            byte[] shortTradKey = mldsaVector.PrivateSeed;
            Array.Resize(ref shortTradKey, shortTradKey.Length + 1);

            AssertImportBadPrivateKey(algorithm, shortTradKey);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.SupportedAlgorithmIetfVectorsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void ImportBadPrivateKey_TrailingData(CompositeMLDsaTestData.CompositeMLDsaTestVector vector)
        {
            byte[] key = vector.SecretKey;
            Array.Resize(ref key, key.Length + 1);

            AssertImportBadPrivateKey(vector.Algorithm, key);
        }

        [Fact]
        public static void ImportBadPrivateKey_Rsa_WrongAlgorithm()
        {
            // Get vector for MLDsa65WithRSA3072Pss
            CompositeMLDsaTestData.CompositeMLDsaTestVector differentTradKey =
                CompositeMLDsaTestData.GetIetfTestVector(CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pss);

            // But use MLDsa65WithRSA4096Pss
            AssertImportBadPrivateKey(CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pss, differentTradKey.SecretKey);

            // And flip
            differentTradKey =
                CompositeMLDsaTestData.GetIetfTestVector(CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pss);

            AssertImportBadPrivateKey(CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pss, differentTradKey.SecretKey);
        }

        [Fact]
        public static void ImportBadPrivateKey_ECDsa_WrongAlgorithm()
        {
            // Get vector for MLDsa65WithECDsaP256
            CompositeMLDsaTestData.CompositeMLDsaTestVector differentTradKey =
                CompositeMLDsaTestData.GetIetfTestVector(CompositeMLDsaAlgorithm.MLDsa65WithECDsaP256);

            // But use MLDsa65WithECDsaP384
            AssertImportBadPrivateKey(CompositeMLDsaAlgorithm.MLDsa65WithECDsaP384, differentTradKey.SecretKey);

            // And flip
            differentTradKey =
                CompositeMLDsaTestData.GetIetfTestVector(CompositeMLDsaAlgorithm.MLDsa65WithECDsaP384);

            AssertImportBadPrivateKey(CompositeMLDsaAlgorithm.MLDsa65WithECDsaP256, differentTradKey.SecretKey);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void ImportPrivateKey_LowerBound(CompositeMLDsaAlgorithm algorithm)
        {
            int bound = CompositeMLDsaTestHelpers.ExpectedPrivateKeySizeLowerBound(algorithm);

            AssertImportBadPrivateKey(algorithm, new byte[bound - 1]);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void ImportPrivateKey_UpperBound(CompositeMLDsaAlgorithm algorithm)
        {
            int bound = CompositeMLDsaTestHelpers.ExpectedPrivateKeySizeUpperBound(algorithm);

            AssertImportBadPrivateKey(algorithm, new byte[bound + 1]);
        }

        [Fact]
        public static void ImportBadPrivateKey_ECDsa_InvalidVersion()
        {
            CompositeMLDsaAlgorithm algorithm = CompositeMLDsaAlgorithm.MLDsa65WithECDsaP256;

            // No version
            AssertImportBadPrivateKey(algorithm, CreateKeyWithVersion(null));

            // Unsupported version
            AssertImportBadPrivateKey(algorithm, CreateKeyWithVersion(0));
            AssertImportBadPrivateKey(algorithm, CreateKeyWithVersion(2));

            // Correct version, don't throw (unless platform does not support Composite ML-DSA)
            CompositeMLDsaTestHelpers.AssertImportPrivateKey(
                import => AssertThrowIfNotSupported(() => import(), algorithm),
                algorithm,
                CreateKeyWithVersion(1));

            static byte[] CreateKeyWithVersion(int? version)
            {
                ECParameters ecdsaKey = EccTestData.GetNistP256ReferenceKey();

                return ComposeKeys(
                    MLDsaTestsData.IetfMLDsa65.PrivateSeed,
                    WriteECPrivateKey(version, ecdsaKey.D, ecdsaKey.Curve.Oid.Value, ecdsaKey.Q));
            }
        }

        [Fact]
        public static void ImportBadPrivateKey_ECDsa_NoPrivateKey()
        {
            ECParameters ecdsaKey = EccTestData.GetNistP256ReferenceKey();

            // no private key
            byte[] compositeKey = ComposeKeys(
                MLDsaTestsData.IetfMLDsa65.PrivateSeed,
                WriteECPrivateKey(version: 1, d: null, ecdsaKey.Curve.Oid.Value, point: ecdsaKey.Q));

            AssertImportBadPrivateKey(CompositeMLDsaAlgorithm.MLDsa65WithECDsaP256, compositeKey);
        }

        [Fact]
        public static void ImportBadPrivateKey_ECDsa_WrongCurve()
        {
            CompositeMLDsaAlgorithm algorithm = CompositeMLDsaAlgorithm.MLDsa65WithECDsaP256;

            // Wrong curve OID
            AssertImportBadPrivateKey(
                algorithm,
                CreateKeyWithCurveOid(ECCurve.NamedCurves.nistP521.Oid.Value));

            AssertImportBadPrivateKey(
                algorithm,
                CreateKeyWithCurveOid("1.3.36.3.3.2.8.1.1.7")); // brainpoolP256r1

            // Domain parameters are optional, don't throw (unless platform does not support Composite ML-DSA)
            CompositeMLDsaTestHelpers.AssertImportPrivateKey(
                import => AssertThrowIfNotSupported(() => import(), algorithm),
                algorithm,
                CreateKeyWithCurveOid(ECCurve.NamedCurves.nistP256.Oid.Value));

            static byte[] CreateKeyWithCurveOid(string? oid)
            {
                ECParameters ecdsaKey = EccTestData.GetNistP256ReferenceKey();

                return ComposeKeys(
                    MLDsaTestsData.IetfMLDsa65.PrivateSeed,
                    WriteECPrivateKey(version: 1, ecdsaKey.D, oid, ecdsaKey.Q));
            }
        }

        [Fact]
        public static void ImportPrivateKey_ECDsa_NoPublicKey()
        {
            CompositeMLDsaAlgorithm algorithm = CompositeMLDsaAlgorithm.MLDsa65WithECDsaP256;
            ECParameters ecdsaKey = EccTestData.GetNistP256ReferenceKey();

            // no public key
            byte[] compositeKey = ComposeKeys(
                MLDsaTestsData.IetfMLDsa65.PrivateSeed,
                WriteECPrivateKey(version: 1, ecdsaKey.D, ecdsaKey.Curve.Oid.Value, point: null));

            // Public key is optional, don't throw (unless platform does not support Composite ML-DSA)
            CompositeMLDsaTestHelpers.AssertImportPrivateKey(
                import => AssertThrowIfNotSupported(() => import(), algorithm),
                algorithm,
                compositeKey);
        }

        static byte[] ComposeKeys(byte[] mldsaKey, AsnWriter tradKey)
        {
            byte[] compositeKey = new byte[mldsaKey.Length + tradKey.GetEncodedLength()];
            mldsaKey.CopyTo(compositeKey, 0);
            tradKey.Encode(compositeKey.AsSpan(mldsaKey.Length));
            return compositeKey;
        }

        private static AsnWriter WriteECPrivateKey(int? version, byte[]? d, string? oid, ECPoint? point)
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

            // ECPrivateKey
            using (writer.PushSequence())
            {
                // version
                if (version is int v)
                {
                    writer.WriteInteger(v);
                }

                // privateKey
                if (d is not null)
                {
                    writer.WriteOctetString(d);
                }

                // domainParameters
                if (oid is not null)
                {
                    using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 0, isConstructed: true)))
                    {
                        writer.WriteObjectIdentifier(oid);
                    }
                }

                // publicKey
                if (point is ECPoint q)
                {
                    using (writer.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 1, isConstructed: true)))
                    {
                        int publicKeyLength = 1 + q.X.Length + q.Y.Length;
                        byte[] publicKeyBytes = new byte[publicKeyLength];

                        publicKeyBytes[0] = 0x04;
                        q.X.CopyTo(publicKeyBytes.AsSpan(1));
                        q.Y.CopyTo(publicKeyBytes.AsSpan(1 + q.X.Length));

                        writer.WriteBitString(publicKeyBytes);
                    }
                }
            }

            return writer;
        }

        private static void AssertImportBadPrivateKey(CompositeMLDsaAlgorithm algorithm, byte[] key)
        {
            CompositeMLDsaTestHelpers.AssertImportPrivateKey(
                import => AssertThrowIfNotSupported(
                    () => AssertExtensions.Throws<CryptographicException>(() => import()),
                    algorithm),
                algorithm,
                key);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void ImportBadPublicKey_Empty(CompositeMLDsaAlgorithm algorithm)
        {
            AssertImportBadPublicKey(algorithm, Array.Empty<byte>());
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void ImportBadPublicKey_ShortMLDsaKey(CompositeMLDsaAlgorithm algorithm)
        {
            MLDsaKeyInfo mldsaVector = CompositeMLDsaTestData.GetMLDsaIetfTestVector(algorithm);
            AssertImportBadPublicKey(algorithm, new byte[mldsaVector.PublicKey.Length - 1]);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void ImportBadPublicKey_OnlyMLDsaKey(CompositeMLDsaAlgorithm algorithm)
        {
            MLDsaKeyInfo mldsaVector = CompositeMLDsaTestData.GetMLDsaIetfTestVector(algorithm);
            AssertImportBadPublicKey(algorithm, mldsaVector.PublicKey);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void ImportBadPublicKey_ShortTradKey(CompositeMLDsaAlgorithm algorithm)
        {
            MLDsaKeyInfo mldsaVector = CompositeMLDsaTestData.GetMLDsaIetfTestVector(algorithm);
            byte[] shortTradKey = mldsaVector.PublicKey;
            Array.Resize(ref shortTradKey, shortTradKey.Length + 1);

            AssertImportBadPublicKey(algorithm, shortTradKey);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.SupportedAlgorithmIetfVectorsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void ImportBadPublicKey_TrailingData(CompositeMLDsaTestData.CompositeMLDsaTestVector vector)
        {
            byte[] key = vector.PublicKey;
            Array.Resize(ref key, key.Length + 1);

            AssertImportBadPublicKey(vector.Algorithm, key);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.SupportedECDsaAlgorithmIetfVectorsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void ImportBadPublicKey_ECDsa_Uncompressed(CompositeMLDsaTestData.CompositeMLDsaTestVector vector)
        {
            byte[] key = vector.PublicKey.AsSpan().ToArray();
            int formatIndex = CompositeMLDsaTestHelpers.MLDsaAlgorithms[vector.Algorithm].PublicKeySizeInBytes;

            // Uncompressed
            Assert.Equal(4, key[formatIndex]);

            key[formatIndex] = 0;
            AssertImportBadPublicKey(vector.Algorithm, key);

            key[formatIndex] = 1;
            AssertImportBadPublicKey(vector.Algorithm, key);

            key[formatIndex] = 2;
            AssertImportBadPublicKey(vector.Algorithm, key);

            key[formatIndex] = 3;
            AssertImportBadPublicKey(vector.Algorithm, key);
        }

        [Fact]
        public static void ImportBadPublicKey_Rsa_WrongAlgorithm()
        {
            // Get vector for MLDsa65WithRSA3072Pss
            CompositeMLDsaTestData.CompositeMLDsaTestVector differentTradKey =
                CompositeMLDsaTestData.GetIetfTestVector(CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pss);

            // But use MLDsa65WithRSA4096Pss
            AssertImportBadPublicKey(CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pss, differentTradKey.PublicKey);

            // And flip
            differentTradKey =
                CompositeMLDsaTestData.GetIetfTestVector(CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pss);

            AssertImportBadPublicKey(CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pss, differentTradKey.PublicKey);
        }

        [Fact]
        public static void ImportBadPublicKey_ECDsa_WrongAlgorithm()
        {
            // Get vector for MLDsa65WithECDsaP256
            CompositeMLDsaTestData.CompositeMLDsaTestVector differentTradKey =
                CompositeMLDsaTestData.GetIetfTestVector(CompositeMLDsaAlgorithm.MLDsa65WithECDsaP256);

            // But use MLDsa65WithECDsaP384
            AssertImportBadPublicKey(CompositeMLDsaAlgorithm.MLDsa65WithECDsaP384, differentTradKey.PublicKey);

            // And flip
            differentTradKey =
                CompositeMLDsaTestData.GetIetfTestVector(CompositeMLDsaAlgorithm.MLDsa65WithECDsaP384);

            AssertImportBadPublicKey(CompositeMLDsaAlgorithm.MLDsa65WithECDsaP256, differentTradKey.PublicKey);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void ImportPublicKey_LowerBound(CompositeMLDsaAlgorithm algorithm)
        {
            int bound = CompositeMLDsaTestHelpers.ExpectedPublicKeySizeLowerBound(algorithm);

            AssertImportBadPublicKey(algorithm, new byte[bound - 1]);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void ImportPublicKey_UpperBound(CompositeMLDsaAlgorithm algorithm)
        {
            int bound = CompositeMLDsaTestHelpers.ExpectedPublicKeySizeUpperBound(algorithm);

            AssertImportBadPublicKey(algorithm, new byte[bound + 1]);
        }

        private static void AssertImportBadPublicKey(CompositeMLDsaAlgorithm algorithm, byte[] key)
        {
            CompositeMLDsaTestHelpers.AssertImportPublicKey(
                import => AssertThrowIfNotSupported(
                    () => AssertExtensions.Throws<CryptographicException>(() => import()),
                    algorithm),
                algorithm,
                key);
        }

        [Fact]
        public static void ArgumentValidation_MalformedAsnEncoding()
        {
            // Generate a valid ASN.1 encoding
            byte[] encodedBytes = CreateAsn1EncodedBytes();
            int actualEncodedLength = encodedBytes.Length;

            // Add a trailing byte so the length indicated in the encoding will be smaller than the actual data.
            Array.Resize(ref encodedBytes, actualEncodedLength + 1);
            AssertThrows(encodedBytes);

            // Remove the last byte so the length indicated in the encoding will be larger than the actual data.
            Array.Resize(ref encodedBytes, actualEncodedLength - 1);
            AssertThrows(encodedBytes);

            static void AssertThrows(byte[] encodedBytes)
            {
                CompositeMLDsaTestHelpers.AssertImportSubjectPublicKeyInfo(
                    import => Assert.Throws<CryptographicException>(() => import(encodedBytes)),
                    import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(() => import(encodedBytes))));

                CompositeMLDsaTestHelpers.AssertImportPkcs8PrivateKey(
                    import => Assert.Throws<CryptographicException>(() => import(encodedBytes)),
                    import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(() => import(encodedBytes))));

                CompositeMLDsaTestHelpers.AssertImportEncryptedPkcs8PrivateKey(
                    import => Assert.Throws<CryptographicException>(() => import("PLACEHOLDER", encodedBytes)),
                    import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(() => import("PLACEHOLDER", encodedBytes))));
            }
        }

        [Fact]
        public static void ImportSpki_BerEncoding()
        {
            byte[] spki = CompositeMLDsaTestData.GetIetfTestVector(CompositeMLDsaAlgorithm.MLDsa65WithECDsaP384).Spki;
            byte[] berSpki = AsnUtils.ConvertDerToNonDerBer(spki);

            CompositeMLDsaTestHelpers.AssertImportSubjectPublicKeyInfo(import =>
                AssertThrowIfNotSupported(() =>
                    Assert.Throws<CryptographicException>(() => import(berSpki))));
        }

        [Fact]
        public static void Import_WrongAsnType()
        {
            // Create an incorrect ASN.1 structure to pass into the import methods.
            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
            AlgorithmIdentifierAsn algorithmIdentifier = new AlgorithmIdentifierAsn
            {
                Algorithm = CompositeMLDsaTestHelpers.AlgorithmToOid(CompositeMLDsaAlgorithm.MLDsa65WithECDsaP384),
            };
            algorithmIdentifier.Encode(writer);
            byte[] wrongAsnType = writer.Encode();

            CompositeMLDsaTestHelpers.AssertImportSubjectPublicKeyInfo(
                import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(() => import(wrongAsnType))));

            CompositeMLDsaTestHelpers.AssertImportPkcs8PrivateKey(
                import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(() => import(wrongAsnType))));

            CompositeMLDsaTestHelpers.AssertImportEncryptedPkcs8PrivateKey(
                import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(() => import("PLACEHOLDER", wrongAsnType))));
        }

        [Fact]
        public static void ImportSubjectPublicKeyInfo_AlgorithmErrorsInAsn()
        {
#if !NETFRAMEWORK // Does not support exporting RSA SPKI
            if (!OperatingSystem.IsBrowser())
            {
                // RSA key
                using RSA rsa = RSA.Create();
                byte[] rsaSpkiBytes = rsa.ExportSubjectPublicKeyInfo();
                CompositeMLDsaTestHelpers.AssertImportSubjectPublicKeyInfo(
                    import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(() => import(rsaSpkiBytes))));
            }
#endif

            // Create an invalid Composite ML-DSA SPKI with parameters
            SubjectPublicKeyInfoAsn spki = new SubjectPublicKeyInfoAsn
            {
                Algorithm = new AlgorithmIdentifierAsn
                {
                    Algorithm = CompositeMLDsaTestHelpers.AlgorithmToOid(CompositeMLDsaAlgorithm.MLDsa65WithECDsaP384),
                    Parameters = CompositeMLDsaTestHelpers.s_derBitStringFoo, // <-- Invalid
                },
                SubjectPublicKey = CompositeMLDsaTestData.GetIetfTestVector(CompositeMLDsaAlgorithm.MLDsa65WithECDsaP384).PublicKey,
            };

            CompositeMLDsaTestHelpers.AssertImportSubjectPublicKeyInfo(
                import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(() => import(spki.Encode()))));

            spki.Algorithm.Parameters = AsnUtils.DerNull;

            CompositeMLDsaTestHelpers.AssertImportSubjectPublicKeyInfo(
                import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(() => import(spki.Encode()))));

            // Sanity check
            spki.Algorithm.Parameters = null;
            CompositeMLDsaTestHelpers.AssertImportSubjectPublicKeyInfo(import => AssertThrowIfNotSupported(() => import(spki.Encode())));
        }

        [Fact]
        public static void ImportPkcs8PrivateKey_AlgorithmErrorsInAsn()
        {
#if !NETFRAMEWORK // Does not support exporting RSA PKCS#8 private key
            if (!OperatingSystem.IsBrowser())
            {
                // RSA key isn't valid for ML-DSA
                using RSA rsa = RSA.Create();
                byte[] rsaPkcs8Bytes = rsa.ExportPkcs8PrivateKey();
                CompositeMLDsaTestHelpers.AssertImportPkcs8PrivateKey(
                    import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(() => import(rsaPkcs8Bytes))));
            }
#endif

            // Create an invalid Composite ML-DSA PKCS8 with parameters
            PrivateKeyInfoAsn pkcs8 = new PrivateKeyInfoAsn
            {
                PrivateKeyAlgorithm = new AlgorithmIdentifierAsn
                {
                    Algorithm = CompositeMLDsaTestHelpers.AlgorithmToOid(CompositeMLDsaAlgorithm.MLDsa65WithECDsaP384),
                    Parameters = CompositeMLDsaTestHelpers.s_derBitStringFoo, // <-- Invalid
                },
                PrivateKey = CompositeMLDsaTestData.GetIetfTestVector(CompositeMLDsaAlgorithm.MLDsa65WithECDsaP384).SecretKey,
            };

            CompositeMLDsaTestHelpers.AssertImportPkcs8PrivateKey(
                import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(() => import(pkcs8.Encode()))));

            pkcs8.PrivateKeyAlgorithm.Parameters = AsnUtils.DerNull;

            CompositeMLDsaTestHelpers.AssertImportPkcs8PrivateKey(
                import => AssertThrowIfNotSupported(() => Assert.Throws<CryptographicException>(() => import(pkcs8.Encode()))));

            // Sanity check
            pkcs8.PrivateKeyAlgorithm.Parameters = null;
            CompositeMLDsaTestHelpers.AssertImportPkcs8PrivateKey(import => AssertThrowIfNotSupported(() => import(pkcs8.Encode())));
        }

        [Fact]
        public static void ImportFromPem_MalformedPem()
        {
            AssertThrows(WritePemRaw("UNKNOWN LABEL", []));
            AssertThrows(string.Empty);
            AssertThrows(WritePemRaw("ENCRYPTED PRIVATE KEY", []));
            AssertThrows(WritePemRaw("PUBLIC KEY", []) + '\n' + WritePemRaw("PUBLIC KEY", []));
            AssertThrows(WritePemRaw("PRIVATE KEY", []) + '\n' + WritePemRaw("PUBLIC KEY", []));
            AssertThrows(WritePemRaw("PUBLIC KEY", []) + '\n' + WritePemRaw("PRIVATE KEY", []));
            AssertThrows(WritePemRaw("PRIVATE KEY", []) + '\n' + WritePemRaw("PRIVATE KEY", []));
            AssertThrows(WritePemRaw("PRIVATE KEY", "%"));
            AssertThrows(WritePemRaw("PUBLIC KEY", "%"));

            static void AssertThrows(string pem)
            {
                AssertThrowIfNotSupported(() =>
                    AssertExtensions.Throws<ArgumentException>("source", () => CompositeMLDsa.ImportFromPem(pem)));
                AssertThrowIfNotSupported(() =>
                    AssertExtensions.Throws<ArgumentException>("source", () => CompositeMLDsa.ImportFromPem(pem.AsSpan())));
            }
        }

        [Fact]
        public static void ImportFromEncryptedPem_MalformedPem()
        {
            AssertThrows(WritePemRaw("UNKNOWN LABEL", []));
            AssertThrows(WritePemRaw("CERTIFICATE", []));
            AssertThrows(string.Empty);
            AssertThrows(WritePemRaw("ENCRYPTED PRIVATE KEY", []) + '\n' + WritePemRaw("ENCRYPTED PRIVATE KEY", []));
            AssertThrows(WritePemRaw("ENCRYPTED PRIVATE KEY", "%"));

            static void AssertThrows(string encryptedPem)
            {
                AssertThrowIfNotSupported(() =>
                    AssertExtensions.Throws<ArgumentException>("source", () => CompositeMLDsa.ImportFromEncryptedPem(encryptedPem, "PLACEHOLDER")));
                AssertThrowIfNotSupported(() =>
                    AssertExtensions.Throws<ArgumentException>("source", () => CompositeMLDsa.ImportFromEncryptedPem(encryptedPem, "PLACEHOLDER"u8)));
                AssertThrowIfNotSupported(() =>
                    AssertExtensions.Throws<ArgumentException>("source", () => CompositeMLDsa.ImportFromEncryptedPem(encryptedPem.AsSpan(), "PLACEHOLDER")));
                AssertThrowIfNotSupported(() =>
                    AssertExtensions.Throws<ArgumentException>("source", () => CompositeMLDsa.ImportFromEncryptedPem(encryptedPem, "PLACEHOLDER"u8.ToArray())));
            }
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.SupportedAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void AlgorithmMatches_GenerateKey(CompositeMLDsaAlgorithm algorithm)
        {
            AssertThrowIfNotSupported(
                () =>
                {
                    using CompositeMLDsa dsa = CompositeMLDsa.GenerateKey(algorithm);
                    Assert.Equal(algorithm, dsa.Algorithm);
                },
                algorithm);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.SupportedAlgorithmIetfVectorsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void AlgorithmMatches_Import(CompositeMLDsaTestData.CompositeMLDsaTestVector vector)
        {
            CompositeMLDsaTestHelpers.AssertImportPublicKey(
                import => AssertThrowIfNotSupported(() => Assert.Equal(vector.Algorithm, import().Algorithm), vector.Algorithm),
                vector.Algorithm,
                vector.PublicKey);

            CompositeMLDsaTestHelpers.AssertImportPrivateKey(
                import => AssertThrowIfNotSupported(() => Assert.Equal(vector.Algorithm, import().Algorithm), vector.Algorithm),
                vector.Algorithm,
                vector.SecretKey);
        }

        [Fact]
        public static void IsSupported_AgreesWithPlatform()
        {
            // Composites are supported everywhere MLDsa is supported
            Assert.Equal(MLDsa.IsSupported, CompositeMLDsa.IsSupported);
        }

        [Theory]
        [MemberData(nameof(CompositeMLDsaTestData.AllAlgorithmsTestData), MemberType = typeof(CompositeMLDsaTestData))]
        public static void IsAlgorithmSupported_AgreesWithPlatform(CompositeMLDsaAlgorithm algorithm)
        {
            bool supported = CompositeMLDsaTestHelpers.ExecuteComponentFunc(
                algorithm,
                rsa => MLDsa.IsSupported,
                ecdsa => ecdsa.IsSec && MLDsa.IsSupported,
                eddsa => false);

            Assert.Equal(
                supported,
                CompositeMLDsa.IsAlgorithmSupported(algorithm));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void IsSupported_InitializesCrypto()
        {
            string arg = CompositeMLDsa.IsSupported ? "1" : "0";

            // This ensures that Composite ML-DSA is the first cryptographic algorithm touched in the process, which kicks off
            // the initialization of the crypto layer on some platforms. Running in a remote executor ensures no other
            // test has pre-initialized anything.
            RemoteExecutor.Invoke(static (string isSupportedStr) =>
            {
                bool isSupported = isSupportedStr == "1";
                return CompositeMLDsa.IsSupported == isSupported ? RemoteExecutor.SuccessExitCode : 0;
            }, arg).Dispose();
        }

        // Asserts the test throws PlatformNotSupportedException if Composite ML-DSA is supported;
        // otherwise runs the test normally.
        private static void AssertThrowIfNotSupported(Action test, CompositeMLDsaAlgorithm? algorithm = null)
        {
            bool isSupported = algorithm is null ? CompositeMLDsa.IsSupported : CompositeMLDsa.IsAlgorithmSupported(algorithm);

            if (isSupported)
            {
                test();
            }
            else
            {
                try
                {
                    test();
                }
                catch (PlatformNotSupportedException pnse)
                {
                    Assert.Contains("CompositeMLDsa", pnse.Message);
                }
                catch (ThrowsException te) when (te.InnerException is PlatformNotSupportedException pnse)
                {
                    Assert.Contains("CompositeMLDsa", pnse.Message);
                }
            }
        }

        private static byte[] CreateAsn1EncodedBytes()
        {
            AsnWriter writer = new AsnWriter(AsnEncodingRules.BER);
            writer.WriteOctetString("some data"u8);
            byte[] encodedBytes = writer.Encode();
            return encodedBytes;
        }

        private static string WritePemRaw(string label, ReadOnlySpan<char> data) =>
            $"-----BEGIN {label}-----\n{data.ToString()}\n-----END {label}-----";
    }
}
