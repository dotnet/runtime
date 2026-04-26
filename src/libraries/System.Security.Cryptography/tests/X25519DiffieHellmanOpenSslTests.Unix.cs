// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.Tests
{
    [ConditionalClass(typeof(X25519DiffieHellman), nameof(X25519DiffieHellman.IsSupported))]
    public sealed class X25519DiffieHellmanOpenSslTests : X25519DiffieHellmanBaseTests
    {
        public override X25519DiffieHellman GenerateKey()
        {
            using SafeEvpPKeyHandle key = Interop.Crypto.X25519GenerateKey();
            return new X25519DiffieHellmanOpenSsl(key);
        }

        public override X25519DiffieHellman ImportPrivateKey(ReadOnlySpan<byte> source)
        {
            using SafeEvpPKeyHandle key = Interop.Crypto.X25519ImportPrivateKey(source);
            return new X25519DiffieHellmanOpenSsl(key);
        }

        public override X25519DiffieHellman ImportPublicKey(ReadOnlySpan<byte> source)
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
    }
}
