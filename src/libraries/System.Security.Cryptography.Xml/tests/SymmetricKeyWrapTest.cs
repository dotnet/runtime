using System;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{
    public class SymmetricKeyWrapTest
    {
        [Fact]
        public void WrapKey_AES128()
        {
            using (Aes aes = Aes.Create())
            {
                aes.KeySize = 128;
                byte[] keyToWrap = new byte[16];
                byte[] wrappedKey = SymmetricKeyWrap.WrapKey(aes, keyToWrap);
                byte[] unwrappedKey = SymmetricKeyWrap.UnwrapKey(aes, wrappedKey);
                Assert.Equal(keyToWrap, unwrappedKey);
            }
        }

        [Fact]
        public void WrapKey_AES192()
        {
            using (Aes aes = Aes.Create())
            {
                aes.KeySize = 192;
                byte[] keyToWrap = new byte[24];
                byte[] wrappedKey = SymmetricKeyWrap.WrapKey(aes, keyToWrap);
                byte[] unwrappedKey = SymmetricKeyWrap.UnwrapKey(aes, wrappedKey);
                Assert.Equal(keyToWrap, unwrappedKey);
            }
        }

        [Fact]
        public void WrapKey_AES256()
        {
            using (Aes aes = Aes.Create())
            {
                aes.KeySize = 256;
                byte[] keyToWrap = new byte[32];
                byte[] wrappedKey = SymmetricKeyWrap.WrapKey(aes, keyToWrap);
                byte[] unwrappedKey = SymmetricKeyWrap.UnwrapKey(aes, wrappedKey);
                Assert.Equal(keyToWrap, unwrappedKey);
            }
        }

        [Fact]
        public void WrapKey_TripleDES()
        {
            using (TripleDES tripleDES = TripleDES.Create())
            {
                byte[] keyToWrap = new byte[24];
                byte[] wrappedKey = SymmetricKeyWrap.WrapKey(tripleDES, keyToWrap);
                byte[] unwrappedKey = SymmetricKeyWrap.UnwrapKey(tripleDES, wrappedKey);
                Assert.Equal(keyToWrap, unwrappedKey);
            }
        }

        [Fact]
        public void WrapKey_InvalidAlgorithm()
        {
            using (SymmetricAlgorithm algorithm = new InvalidSymmetricAlgorithm())
            {
                byte[] keyToWrap = new byte[16];
                Assert.Throws<CryptographicException>(() => SymmetricKeyWrap.WrapKey(algorithm, keyToWrap));
            }
        }

        private class InvalidSymmetricAlgorithm : SymmetricAlgorithm
        {
            public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV)
            {
                throw new NotImplementedException();
            }

            public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV)
            {
                throw new NotImplementedException();
            }

            public override void GenerateIV()
            {
                throw new NotImplementedException();
            }

            public override void GenerateKey()
            {
                throw new NotImplementedException();
            }
        }
    }
}
