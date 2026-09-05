// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    [ConditionalClass(typeof(CompositeMLKemTestHelpers), nameof(CompositeMLKemTestHelpers.IsCngSupported))]
    [PlatformSpecific(TestPlatforms.Windows)]
    public sealed class CompositeMLKemCngTests_AllowPlaintextExport : CompositeMLKemCngTestsWithExportPolicy
    {
        protected override CngExportPolicies ExportPolicy =>
            CngExportPolicies.AllowExport | CngExportPolicies.AllowPlaintextExport;
    }

    // Windows doesn't support PKCS#8 export so we can't implement encrypted exports.
    [ActiveIssue("https://github.com/dotnet/runtime/issues/129633")]
    [ConditionalClass(typeof(CompositeMLKemTestHelpers), nameof(CompositeMLKemTestHelpers.IsCngSupported))]
    [PlatformSpecific(TestPlatforms.Windows)]
    public sealed class CompositeMLKemCngTests_AllowExport : CompositeMLKemCngTestsWithExportPolicy
    {
        protected override CngExportPolicies ExportPolicy =>
            CngExportPolicies.AllowExport;
    }

    public abstract class CompositeMLKemCngTestsWithExportPolicy : CompositeMLKemTestsBase
    {
        protected abstract CngExportPolicies ExportPolicy { get; }

        protected override CompositeMLKem GenerateKey(CompositeMLKemAlgorithm algorithm)
        {
            using CngKey key = CompositeMLKemTestHelpers.GenerateCngKey(algorithm, ExportPolicy);
            return new CompositeMLKemCng(key);
        }

        protected override CompositeMLKem ImportDecapsulationKey(
            CompositeMLKemAlgorithm algorithm,
            ReadOnlySpan<byte> source)
        {
            using CngKey key =
                CompositeMLKemTestHelpers.ImportCngDecapsulationKey(algorithm, source, ExportPolicy);
            return new CompositeMLKemCng(key);
        }

        protected override CompositeMLKem ImportEncapsulationKey(
            CompositeMLKemAlgorithm algorithm,
            ReadOnlySpan<byte> source)
        {
            using CngKey key = CompositeMLKemTestHelpers.ImportCngEncapsulationKey(algorithm, source);
            return new CompositeMLKemCng(key);
        }

        protected override void AssertDecapsulationWithEncapsulationKeyFails(Action test)
        {
            if (PlatformDetection.IsWindows)
            {
                const int NTE_NO_KEY = unchecked((int)0x8009000D);

                CryptographicException ex = Assert.ThrowsAny<CryptographicException>(test);

                Assert.Equal(NTE_NO_KEY, ex.HResult);
            }
            else
            {
                base.AssertDecapsulationWithEncapsulationKeyFails(test);
            }
        }
    }

    [ConditionalClass(typeof(CompositeMLKemTestHelpers), nameof(CompositeMLKemTestHelpers.IsCngSupported))]
    [PlatformSpecific(TestPlatforms.Windows)]
    public static class CompositeMLKemCngTests
    {
        private const CngExportPolicies PlaintextExport = CngExportPolicies.AllowPlaintextExport | CngExportPolicies.AllowExport;

        [Fact]
        public static void Constructor_WrongAlgorithm()
        {
            using CngKey key = CngKey.Create(CngAlgorithm.Rsa, keyName: null);
            AssertExtensions.Throws<ArgumentException>("key", () => new CompositeMLKemCng(key));
        }

        [Fact]
        public static void ImportDecapsulationKey_NoExportFlag()
        {
            CompositeMLKemTestVector vector =
                CompositeMLKemTestData.AllIetfVectors
                    .First(v => v.Algorithm == CompositeMLKemAlgorithm.MLKem768WithECDiffieHellmanP256);

            using (CngKey key = CompositeMLKemTestHelpers.ImportCngDecapsulationKey(vector.Algorithm, vector.DecapsulationKey, CngExportPolicies.None))
            using (CompositeMLKemCng kem = new(key))
            {
                CompositeMLKemTestHelpers.AssertExportEncapsulationKey(
                    export => AssertExtensions.SequenceEqual(vector.EncapsulationKey, export(kem)));

                CompositeMLKemTestHelpers.AssertExportDecapsulationKey(
                    export => Assert.Throws<CryptographicException>(() => export(kem)));

                kem.Encapsulate(out byte[] ciphertext, out byte[] sharedSecret);
                AssertExtensions.SequenceEqual(sharedSecret, kem.Decapsulate(ciphertext));

                AssertExtensions.SequenceEqual(vector.SharedSecret, kem.Decapsulate(vector.Ciphertext.ToArray()));
            }
        }

        [Fact]
        public static void ImportDecapsulationKey_Persisted()
        {
            CompositeMLKemTestVector vector =
                CompositeMLKemTestData.AllIetfVectors
                    .First(v => v.Algorithm == CompositeMLKemAlgorithm.MLKem768WithECDiffieHellmanP256);

            CngKey key = PqcBlobHelpers.EncodeCompositeMLKemBlob(
                PqcBlobHelpers.TryGetCompositeMLKemParameterSet(vector.Algorithm, out string? parameterSet)
                    ? parameterSet
                    : throw new CryptographicException(),
                vector.DecapsulationKey,
                Interop.BCrypt.KeyBlobType.BCRYPT_COMPOSITE_MLKEM_PRIVATE_BLOB,
                state: default(object),
                static (_, blobKind, blob) =>
                {
                    CngProperty kemBlob = new CngProperty(
                        blobKind,
                        blob.ToArray(),
                        CngPropertyOptions.None);

                    CngKeyCreationParameters creationParams = new();
                    creationParams.Parameters.Add(kemBlob);
                    creationParams.ExportPolicy = CngExportPolicies.AllowPlaintextExport;
                    creationParams.KeyCreationOptions = CngKeyCreationOptions.OverwriteExistingKey;

                    CngKey key = CngKey.Create(
                        CngAlgorithm.CompositeMLKem,
                        $"{nameof(CompositeMLKemCngTests)}_{nameof(ImportDecapsulationKey_Persisted)}",
                        creationParams);

                    return key;
                });

            try
            {
                using (CompositeMLKemCng kem = new(key))
                {
                    kem.Encapsulate(out byte[] ciphertext, out byte[] sharedSecret);
                    AssertExtensions.SequenceEqual(sharedSecret, kem.Decapsulate(ciphertext));

                    AssertExtensions.SequenceEqual(vector.SharedSecret, kem.Decapsulate(vector.Ciphertext.ToArray()));

                    AssertExtensions.SequenceEqual(vector.DecapsulationKey, kem.ExportDecapsulationKey());
                    AssertExtensions.SequenceEqual(vector.EncapsulationKey, kem.ExportEncapsulationKey());
                }
            }
            finally
            {
                key.Delete();
            }
        }

        [Theory]
        [InlineData(default(string))]
        [InlineData($"{nameof(CompositeMLKemCngTests)}_{nameof(Constructor_DuplicatesHandle)}")]
        public static void Constructor_DuplicatesHandle(string? name)
        {
            CompositeMLKemAlgorithm algorithm = CompositeMLKemAlgorithm.MLKem768WithECDiffieHellmanP256;

            CngKey key = CompositeMLKemTestHelpers.GenerateCngKey(algorithm, PlaintextExport, name);

            try
            {
                IEnumerable<CompositeMLKemCng> generateFive = Enumerable.Range(0, 5).Select(_ => new CompositeMLKemCng(key));
                List<CompositeMLKemCng> disposables = new List<CompositeMLKemCng>(10);
                disposables.AddRange(generateFive);

                using (CompositeMLKemCng kem = new CompositeMLKemCng(key))
                {
                    disposables.AddRange(generateFive);

                    foreach (CompositeMLKemCng disposable in disposables)
                    {
                        disposable.Dispose();
                    }

                    kem.Encapsulate(out byte[] ciphertext, out byte[] sharedSecret);
                    AssertExtensions.SequenceEqual(sharedSecret, kem.Decapsulate(ciphertext));
                }

            }
            finally
            {
                if (name is null)
                {
                    key.Dispose();
                }
                else
                {
                    key.Delete();
                }
            }
        }

        [Fact]
        public static void GetKey_DuplicatesHandle()
        {
            CompositeMLKemAlgorithm algo = CompositeMLKemAlgorithm.MLKem768WithECDiffieHellmanP256;

            using (CngKey key = CompositeMLKemTestHelpers.GenerateCngKey(algo, PlaintextExport))
            using (CompositeMLKemCng kemKey = new(key))
            using (CngKey getKey1 = kemKey.GetKey())
            {
                using (CngKey getKey2 = kemKey.GetKey())
                {
                    Assert.NotSame(key, getKey1);
                    Assert.NotSame(getKey1, getKey2);
                }

                Assert.Equal(key.Algorithm, getKey1.Algorithm); // Assert.NoThrow on getKey1.Algorithm
            }
        }

        [Theory]
        [MemberData(nameof(CompositeMLKemTestData.SupportedAlgorithmsTestData), MemberType = typeof(CompositeMLKemTestData))]
        public static void GetKey_AfterDispose(CompositeMLKemAlgorithm algorithm)
        {
            using (CngKey key = CompositeMLKemTestHelpers.GenerateCngKey(algorithm, PlaintextExport))
            {
                CompositeMLKemCng kem = new(key);
                kem.Dispose();

                Assert.Throws<ObjectDisposedException>(kem.GetKey);
            }
        }
    }
}
