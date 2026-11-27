// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.Tests
{
    [ConditionalClass(typeof(X25519DiffieHellman), nameof(X25519DiffieHellman.IsSupported))]
    public sealed class X25519DiffieHellmanOpenSslTests : X25519DiffieHellmanBaseTests
    {
        public override X25519DiffieHellmanOpenSsl GenerateKey()
        {
            using SafeEvpPKeyHandle key = Interop.Crypto.X25519GenerateKey();
            return new X25519DiffieHellmanOpenSsl(key);
        }

        public override X25519DiffieHellmanOpenSsl ImportPrivateKey(ReadOnlySpan<byte> source)
        {
            using SafeEvpPKeyHandle key = Interop.Crypto.X25519ImportPrivateKey(source);
            return new X25519DiffieHellmanOpenSsl(key);
        }

        public override X25519DiffieHellmanOpenSsl ImportPublicKey(ReadOnlySpan<byte> source)
        {
            using SafeEvpPKeyHandle key = Interop.Crypto.X25519ImportPublicKey(source);
            return new X25519DiffieHellmanOpenSsl(key);
        }

        [Fact]
        public void X25519DiffieHellmanOpenSsl_Ctor_ArgValidation()
        {
            AssertExtensions.Throws<ArgumentNullException>("pkeyHandle", static () => new X25519DiffieHellmanOpenSsl(null));
        }

        [Fact]
        public void X25519DiffieHellmanOpenSsl_Ctor_InvalidHandle()
        {
            AssertExtensions.Throws<ArgumentException>("pkeyHandle", static () => new X25519DiffieHellmanOpenSsl(new SafeEvpPKeyHandle()));
        }

        [Fact]
        public void X25519DiffieHellmanOpenSsl_WrongAlgorithm()
        {
            using RSAOpenSsl rsa = new();
            using SafeEvpPKeyHandle rsaHandle = rsa.DuplicateKeyHandle();

            Assert.Throws<CryptographicException>(() => new X25519DiffieHellmanOpenSsl(rsaHandle));
        }

        [Fact]
        public void X25519DiffieHellmanOpenSsl_DuplicateKeyHandle()
        {
            using SafeEvpPKeyHandle key = Interop.Crypto.X25519GenerateKey();
            using X25519DiffieHellmanOpenSsl xdh = new(key);
            SafeEvpPKeyHandle secondKey;

            using (secondKey = xdh.DuplicateKeyHandle())
            {
                Assert.False(secondKey.IsInvalid, nameof(secondKey.IsInvalid));
            }

            Assert.True(secondKey.IsInvalid, nameof(secondKey.IsInvalid));
            Assert.False(key.IsInvalid, nameof(key.IsInvalid));
            Assert.NotNull(xdh.ExportPrivateKey());
        }

        [Fact]
        public void DeriveRawSecretAgreement_OpenSslKeyWithCreateKey_Symmetric()
        {
            using X25519DiffieHellmanOpenSsl openSslKey = GenerateKey();
            using X25519DiffieHellman createKey = X25519DiffieHellman.GenerateKey();

            byte[] secret1 = openSslKey.DeriveRawSecretAgreement(createKey);
            byte[] secret2 = createKey.DeriveRawSecretAgreement(openSslKey);

            AssertExtensions.SequenceEqual(secret1, secret2);
        }

        [Fact]
        public void DeriveRawSecretAgreement_OpenSslKeyWithCreateKey_ExactBuffers()
        {
            using X25519DiffieHellmanOpenSsl openSslKey = GenerateKey();
            using X25519DiffieHellman createKey = X25519DiffieHellman.GenerateKey();

            byte[] secret1 = new byte[X25519DiffieHellman.SecretAgreementSizeInBytes];
            byte[] secret2 = new byte[X25519DiffieHellman.SecretAgreementSizeInBytes];
            openSslKey.DeriveRawSecretAgreement(createKey, secret1);
            createKey.DeriveRawSecretAgreement(openSslKey, secret2);

            AssertExtensions.SequenceEqual(secret1, secret2);
        }

        [Fact]
        public void DeriveRawSecretAgreement_OpenSslKeyWithImportedCreateKey()
        {
            using X25519DiffieHellmanOpenSsl openSslKey = GenerateKey();

            byte[] openSslPublicKey = openSslKey.ExportPublicKey();
            using X25519DiffieHellman createKeyFromPublic = X25519DiffieHellman.ImportPublicKey(openSslPublicKey);

            using X25519DiffieHellman createKey = X25519DiffieHellman.GenerateKey();

            byte[] secret1 = createKey.DeriveRawSecretAgreement(openSslKey);
            byte[] secret2 = createKey.DeriveRawSecretAgreement(createKeyFromPublic);

            AssertExtensions.SequenceEqual(secret1, secret2);
        }

        [Fact]
        public void DeriveRawSecretAgreement_CreateKeyWithOpenSslPublicOnly()
        {
            using X25519DiffieHellman createKey = X25519DiffieHellman.GenerateKey();

            byte[] createPublicKey = createKey.ExportPublicKey();
            using X25519DiffieHellmanOpenSsl openSslPublicOnly = ImportPublicKey(createPublicKey);

            using X25519DiffieHellmanOpenSsl openSslPrivate = GenerateKey();

            byte[] secret1 = openSslPrivate.DeriveRawSecretAgreement(createKey);
            byte[] secret2 = openSslPrivate.DeriveRawSecretAgreement(openSslPublicOnly);

            AssertExtensions.SequenceEqual(secret1, secret2);
        }

        [Fact]
        public void DeriveRawSecretAgreement_PrivateKeyRoundtripBetweenOpenSslAndCreate()
        {
            using X25519DiffieHellmanOpenSsl openSslKey = GenerateKey();
            byte[] privateKey = openSslKey.ExportPrivateKey();

            using X25519DiffieHellman createFromPrivate = X25519DiffieHellman.ImportPrivateKey(privateKey);
            using X25519DiffieHellman peer = X25519DiffieHellman.GenerateKey();

            byte[] secret1 = openSslKey.DeriveRawSecretAgreement(peer);
            byte[] secret2 = createFromPrivate.DeriveRawSecretAgreement(peer);

            AssertExtensions.SequenceEqual(secret1, secret2);
        }

        [Fact]
        public void DeriveRawSecretAgreement_Pkcs8RoundtripBetweenOpenSslAndCreate()
        {
            using X25519DiffieHellmanOpenSsl openSslKey = GenerateKey();
            byte[] pkcs8 = openSslKey.ExportPkcs8PrivateKey();

            using X25519DiffieHellman createFromPkcs8 = X25519DiffieHellman.ImportPkcs8PrivateKey(pkcs8);
            using X25519DiffieHellman peer = X25519DiffieHellman.GenerateKey();

            byte[] secret1 = openSslKey.DeriveRawSecretAgreement(peer);
            byte[] secret2 = createFromPkcs8.DeriveRawSecretAgreement(peer);

            AssertExtensions.SequenceEqual(secret1, secret2);
        }

        [Fact]
        public void ExportPublicKey_ConsistentBetweenOpenSslAndCreate()
        {
            using X25519DiffieHellmanOpenSsl openSslKey = GenerateKey();
            byte[] privateKey = openSslKey.ExportPrivateKey();

            using X25519DiffieHellman createKey = X25519DiffieHellman.ImportPrivateKey(privateKey);

            AssertExtensions.SequenceEqual(openSslKey.ExportPublicKey(), createKey.ExportPublicKey());
        }
    }
}
