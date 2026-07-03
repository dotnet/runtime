// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.Win32.SafeHandles;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    [ConditionalClass(typeof(X25519DiffieHellman), nameof(X25519DiffieHellman.IsSupported))]
    public sealed class X25519DiffieHellmanExportableCngTests : X25519DiffieHellmanCngTests
    {
        protected override CngExportPolicies ExportPolicy => CngExportPolicies.AllowExport;
    }

    [ConditionalClass(typeof(X25519DiffieHellman), nameof(X25519DiffieHellman.IsSupported))]
    public sealed class X25519DiffieHellmanPlaintextExportableCngTests : X25519DiffieHellmanCngTests
    {
        protected override CngExportPolicies ExportPolicy =>
            CngExportPolicies.AllowExport |
            CngExportPolicies.AllowPlaintextExport;
    }

    [ConditionalClass(typeof(X25519DiffieHellman), nameof(X25519DiffieHellman.IsSupported))]
    public static class X25519DiffieHellmanCngContractTests
    {
        [Fact]
        public static void ArgValidation_Ctor_NullKey_Throws()
        {
            Assert.Throws<ArgumentNullException>("key", static () => new X25519DiffieHellmanCng(null));
        }

        [Fact]
        public static void ArgValidation_Ctor_WrongAlgorithmGroup()
        {
            using CngKey key = CngKey.Create(CngAlgorithm.Rsa);
            Assert.Throws<ArgumentException>("key", () => new X25519DiffieHellmanCng(key));
        }

        [Fact]
        public static void ArgValidation_Ctor_WrongCurve()
        {
            using CngKey key = X25519DiffieHellmanCngTests.GenerateCngKey(nameof(ECCurve.NamedCurves.nistP256));
            Assert.Throws<ArgumentException>("key", () => new X25519DiffieHellmanCng(key));
        }

        [Fact]
        public static void GetKey()
        {
            using CngKey key = X25519DiffieHellmanCngTests.GenerateCngKey();

            using (X25519DiffieHellmanCng xdhKey = new(key))
            using (CngKey getKey1 = xdhKey.GetKey())
            {
                using (CngKey getKey2 = xdhKey.GetKey())
                {
                    Assert.NotSame(key, getKey1);
                    Assert.NotSame(getKey1, getKey2);
                }

                Assert.Equal(key.Algorithm, getKey1.Algorithm); // Assert.NoThrow on getKey1.Algorithm
            }
        }

        [Fact]
        public static void GetKey_Disposed()
        {
            using CngKey key = X25519DiffieHellmanCngTests.GenerateCngKey();
            X25519DiffieHellmanCng xdhKey = new(key);
            xdhKey.Dispose();
            xdhKey.Dispose(); // No-op
            Assert.Throws<ObjectDisposedException>(() => xdhKey.GetKey());
        }
    }

    [ConditionalClass(typeof(X25519DiffieHellman), nameof(X25519DiffieHellman.IsSupported))]
    public static class X25519DiffieHellmanCngNonExportableTests
    {
        [Fact]
        public static void X25519DiffieHellmanCng_NonExportable_ExportPrivateKeyThrows()
        {
            using CngKey key = X25519DiffieHellmanCngTests.GenerateCngKey(exportPolicy: CngExportPolicies.None);
            using X25519DiffieHellmanCng xdh = new(key);
            Assert.Throws<CryptographicException>(() => xdh.ExportPrivateKey());
        }

        [Fact]
        public static void X25519DiffieHellmanCng_NonExportable_ExportPublicKeyAlwaysWorks()
        {
            using CngKey key = X25519DiffieHellmanCngTests.GenerateCngKey(exportPolicy: CngExportPolicies.None);
            using X25519DiffieHellmanCng xdh = new(key);
            ReadOnlySpan<byte> publicKey = xdh.ExportPublicKey();
            AssertExtensions.TrueExpression(publicKey.IndexOfAnyExcept((byte)0) >= 0);
        }
    }

    public abstract class X25519DiffieHellmanCngTests : X25519DiffieHellmanBaseTests
    {
        private const int NCRYPT_NO_KEY_VALIDATION = 0x00000008;

        private static readonly Lazy<SafeNCryptProviderHandle> s_lazyDefaultProviderHandle = new(() =>
        {
            using CngKey key = GenerateCngKey();
            return key.ProviderHandle;
        });

        private static SafeNCryptProviderHandle DefaultProviderHandle => s_lazyDefaultProviderHandle.Value;

        protected abstract CngExportPolicies ExportPolicy { get; }

        public override X25519DiffieHellmanCng GenerateKey()
        {
            using CngKey key = GenerateCngKey(exportPolicy: ExportPolicy);
            return new X25519DiffieHellmanCng(key);
        }

        public override X25519DiffieHellmanCng ImportPrivateKey(ReadOnlySpan<byte> source)
        {
            using CryptoPoolLease lease = X25519WindowsHelpers.CreateCngBlob(source, true, out _);
            using SafeNCryptKeyHandle keyHandle = ECCng.ImportKeyBlob(
                CngKeyBlobFormat.EccPrivateBlob.Format,
                lease.Span,
                "curve25519",
                DefaultProviderHandle,
                NCRYPT_NO_KEY_VALIDATION);

            using CngKey cngKey = CngKey.Open(keyHandle, CngKeyHandleOpenOptions.EphemeralKey);
            byte[] exportPolicyBytes = BitConverter.GetBytes((int)ExportPolicy);

            cngKey.SetProperty(new CngProperty(
                "Export Policy",
                exportPolicyBytes,
                CngPropertyOptions.None));

            return new X25519DiffieHellmanCng(cngKey);
        }

        public override X25519DiffieHellmanCng ImportPublicKey(ReadOnlySpan<byte> source)
        {
            Span<byte> reducedPublicKey = stackalloc byte[X25519DiffieHellman.PublicKeySizeInBytes];
            X25519WindowsHelpers.ReducePublicKey(source, reducedPublicKey);
            using CryptoPoolLease lease = X25519WindowsHelpers.CreateCngBlob(reducedPublicKey, false, out _);
            using SafeNCryptKeyHandle keyHandle = ECCng.ImportKeyBlob(
                CngKeyBlobFormat.EccPublicBlob.Format,
                lease.Span,
                "curve25519",
                DefaultProviderHandle,
                NCRYPT_NO_KEY_VALIDATION);

            using CngKey cngKey = CngKey.Open(keyHandle, CngKeyHandleOpenOptions.EphemeralKey);
            return new X25519DiffieHellmanCng(cngKey);
        }

        // X25519DiffieHellmanCng CNG can't unfix adjusted keys because it can't keep track of adjustments made
        // since the key came from somewhere else.
        public override bool CanRoundTripKeys => false;

        internal static CngKey GenerateCngKey(
            string curve = "curve25519",
            CngExportPolicies exportPolicy = CngExportPolicies.AllowPlaintextExport)
        {
            CngKeyCreationParameters creationParameters = new() { ExportPolicy = exportPolicy };

            creationParameters.Parameters.Add(
                new CngProperty(
                    "ECCCurveName",
                    Encoding.Unicode.GetBytes(curve + "\0"),
                    CngPropertyOptions.None));

            return CngKey.Create(
                CngAlgorithm.ECDiffieHellman,
                null,
                creationParameters);
        }
    }
}
