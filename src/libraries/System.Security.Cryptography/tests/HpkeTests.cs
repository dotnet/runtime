// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    public static class HpkeTests
    {
        [Fact]
        public static void GenerateKey_NullSuite()
        {
            AssertExtensions.Throws<ArgumentNullException>("suite", () => Hpke.GenerateKey((HpkeSuite)null));
        }

        [Fact]
        public static void DeriveKey_NullArguments()
        {
            HpkeSuite suite = new(
                HpkeKem.DHKEM_P256_HKDF_SHA256,
                HpkeKdf.HKDF_SHA256,
                HpkeAead.AES_128_GCM);

            AssertExtensions.Throws<ArgumentNullException>(
                "suite",
                () => Hpke.DeriveKey((HpkeSuite)null, Array.Empty<byte>()));
            AssertExtensions.Throws<ArgumentNullException>(
                "ikm",
                () => Hpke.DeriveKey(suite, (byte[])null));
        }

        [Theory]
        [InlineData(
            HpkeKem.DHKEM_P256_HKDF_SHA256,
            "4270e54ffd08d79d5928020af4686d8f6b7d35dbe470265f1f5aa22816ce860e",
            "4995788ef4b9d6132b249ce59a77281493eb39af373d236a1fe415cb0c2d7beb",
            "04a92719c6195d5085104f469a8b9814d5838ff72b60501e2c4466e5e67b325a" +
            "c98536d7b61a1af4b78e5b7f951c0900be863c403ce65c9bfcb9382657222d18c4")]
        [InlineData(
            HpkeKem.DHKEM_P384_HKDF_SHA384,
            "65fca3ea3b6db29a62bff28ec53c08710fab10b3798e59b678d3224296d5883f" +
            "039123471784ce57b0d85a17cd521196",
            "679172205e04663f40fda1018cd46c18ebaa876ede6998ba86b051614ca4d5e4" +
            "bfbea34b720617a4b958cc80f6305244",
            "04a5f53da8564364255bc36850df793672782a5c9e4a7fb5fb2e2146eb12e4d8" +
            "477ab1f326a361dfd1e41212109510e813380547c68c0964c1908f16f67b902a" +
            "061be27b2f8b43f1fab1bf0dbf89f5167ce80aca2c210b8fc0f040699db9ee1229")]
        [InlineData(
            HpkeKem.DHKEM_X25519_HKDF_SHA256,
            "7268600d403fce431561aef583ee1613527cff655c1343f29812e66706df3234",
            "52c4a758a802cd8b936eceea314432798d5baf2d7e9235dc084ab1b9cfa2f736",
            "37fda3567bdbd628e88668c3c8d7e97d1d1253b6d4ea6d44c150f741f1bf4431")]
        public static void DeriveKey_KnownAnswer(HpkeKem kem, string ikmHex, string privateKeyHex, string publicKeyHex)
        {
            HpkeSuite suite = new(kem, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM);

            if (!Hpke.IsSupported(suite))
            {
                Assert.Throws<PlatformNotSupportedException>(
                    () => Hpke.DeriveKey(suite, Convert.FromHexString(ikmHex)));
                return;
            }

            byte[] ikm = Convert.FromHexString(ikmHex);
            byte[] expectedPrivateKey = Convert.FromHexString(privateKeyHex);

            try
            {
                using (Hpke keyFromArray = Hpke.DeriveKey(suite, ikm))
                using (Hpke keyFromSpan = Hpke.DeriveKey(suite, ikm.AsSpan()))
                {
                    byte[] arrayPrivateKey = ExportPrivateKey(keyFromArray, kem);
                    byte[] spanPrivateKey = ExportPrivateKey(keyFromSpan, kem);

                    try
                    {
                        Assert.Equal(expectedPrivateKey, arrayPrivateKey);
                        Assert.Equal(Convert.FromHexString(publicKeyHex), ExportPublicKey(keyFromArray, kem));
                        Assert.Equal(arrayPrivateKey, spanPrivateKey);
                        Assert.Equal(ExportPublicKey(keyFromArray, kem), ExportPublicKey(keyFromSpan, kem));
                    }
                    finally
                    {
                        CryptographicOperations.ZeroMemory(arrayPrivateKey);
                        CryptographicOperations.ZeroMemory(spanPrivateKey);
                    }
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(ikm);
                CryptographicOperations.ZeroMemory(expectedPrivateKey);
            }
        }

        [Theory]
        [InlineData(HpkeKem.DHKEM_P256_HKDF_SHA256)]
        [InlineData(HpkeKem.DHKEM_P384_HKDF_SHA384)]
        [InlineData(HpkeKem.DHKEM_X25519_HKDF_SHA256)]
        public static void GenerateKey(HpkeKem kem)
        {
            HpkeSuite suite = new(kem, HpkeKdf.HKDF_SHA256, HpkeAead.AES_128_GCM);

            if (!Hpke.IsSupported(suite))
            {
                Assert.Throws<PlatformNotSupportedException>(() => Hpke.GenerateKey(suite));
                return;
            }

            Hpke key = Hpke.GenerateKey(suite);

            try
            {
                Assert.Same(suite, key.Suite);

                FieldInfo adapterField = key.GetType().GetField(
                    "_adapter",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(adapterField);

                object adapter = adapterField.GetValue(key);
                Assert.NotNull(adapter);

                string keyFieldName = kem == HpkeKem.DHKEM_X25519_HKDF_SHA256 ? "_x25519" : "_ecdh";
                FieldInfo keyField = adapter.GetType().GetField(
                    keyFieldName,
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(keyField);
                Assert.NotNull(keyField.GetValue(adapter));
            }
            finally
            {
                key.Dispose();
            }

            key.Dispose();
        }

        private static object GetAdapter(Hpke key)
        {
            FieldInfo adapterField = key.GetType().GetField(
                "_adapter",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(adapterField);

            object adapter = adapterField.GetValue(key);
            Assert.NotNull(adapter);
            return adapter;
        }

        private static byte[] ExportPrivateKey(Hpke key, HpkeKem kem)
        {
            object adapter = GetAdapter(key);
            string keyFieldName = kem == HpkeKem.DHKEM_X25519_HKDF_SHA256 ? "_x25519" : "_ecdh";
            object implementation = GetImplementation(adapter, keyFieldName);

            if (kem == HpkeKem.DHKEM_X25519_HKDF_SHA256)
            {
                return (byte[])implementation.GetType().GetMethod(
                    nameof(X25519DiffieHellman.ExportPrivateKey),
                    Type.EmptyTypes).Invoke(
                    implementation,
                    parameters: null);
            }

            ECParameters parameters = (ECParameters)implementation.GetType()
                .GetMethod(nameof(ECDiffieHellman.ExportParameters))
                .Invoke(implementation, new object[] { true });
            return parameters.D;
        }

        private static byte[] ExportPublicKey(Hpke key, HpkeKem kem)
        {
            object adapter = GetAdapter(key);
            string keyFieldName = kem == HpkeKem.DHKEM_X25519_HKDF_SHA256 ? "_x25519" : "_ecdh";
            object implementation = GetImplementation(adapter, keyFieldName);

            if (kem == HpkeKem.DHKEM_X25519_HKDF_SHA256)
            {
                return (byte[])implementation.GetType().GetMethod(
                    nameof(X25519DiffieHellman.ExportPublicKey),
                    Type.EmptyTypes).Invoke(
                    implementation,
                    parameters: null);
            }

            ECParameters parameters = (ECParameters)implementation.GetType()
                .GetMethod(nameof(ECDiffieHellman.ExportParameters))
                .Invoke(implementation, new object[] { false });
            byte[] publicKey = new byte[1 + parameters.Q.X.Length + parameters.Q.Y.Length];
            publicKey[0] = 0x04;
            parameters.Q.X.CopyTo(publicKey, 1);
            parameters.Q.Y.CopyTo(publicKey, 1 + parameters.Q.X.Length);
            return publicKey;
        }

        private static object GetImplementation(object adapter, string keyFieldName)
        {
            FieldInfo keyField = adapter.GetType().GetField(
                keyFieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(keyField);

            object implementation = keyField.GetValue(adapter);
            Assert.NotNull(implementation);
            return implementation;
        }
    }
}
