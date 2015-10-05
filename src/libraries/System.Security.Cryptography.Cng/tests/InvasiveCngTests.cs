// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;

using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encryption.Aes.Tests
{
    using Aes = System.Security.Cryptography.Aes;

    public static class InvasiveCngTests
    {
        //[Fact] - Keeping this test for reference but we don't want to run it as an inner-loop test because 
        //         it creates a key on disk.
        public static void StoredKeyAES()
        {
            CngAlgorithm algname = new CngAlgorithm("AES");
            string keyName = "CoreFxTest-" + Guid.NewGuid();
            CngKey _cngKey = CngKey.Create(algname, keyName);
            try
            {
                using (Aes alg = new AesCng(keyName))
                {
                    int keySize = alg.KeySize;
                    Assert.Equal(256, keySize);

                    // Since this is a stored key, it's not going to surrender the actual key bytes. 
                    Assert.ThrowsAny<CryptographicException>(() => alg.Key);
                }
            }
            finally
            {
                _cngKey.Delete();
            }
        }

        //[Fact] - Keeping this test for reference but we don't want to run it as an inner-loop test because 
        //         it creates a key on disk.
        public static void StoredKeyTripleDES()
        {
            CngAlgorithm algname = new CngAlgorithm("3DES");
            string keyName = "CoreFxTest-" + Guid.NewGuid();
            CngKey _cngKey = CngKey.Create(algname, keyName);
            try
            {
                using (TripleDES alg = new TripleDESCng(keyName))
                {
                    int keySize = alg.KeySize;
                    Assert.Equal(192, keySize);

                    // Since this is a stored key, it's not going to surrender the actual key bytes. 
                    Assert.ThrowsAny<CryptographicException>(() => alg.Key);
                }
            }
            finally
            {
                _cngKey.Delete();
            }
        }

        //[Fact] - Keeping this test for reference but we don't want to run it as an inner-loop test because 
        //         it creates a key on disk.
        public static void AesRoundTrip256BitsNoneECBUsingStoredKey()
        {
            CngAlgorithm algname = new CngAlgorithm("AES");
            string keyName = "CoreFxTest-" + Guid.NewGuid();
            CngKey _cngKey = CngKey.Create(algname, keyName);
            try
            {
                using (Aes alg = new AesCng(keyName))
                {
                    try
                    {
                        alg.Padding = PaddingMode.None;
                        alg.Mode = CipherMode.ECB;

                        int keySize = alg.KeySize;

                        byte[] plainText = "15a818701f0f7c99fe4b1b4b860f131b".HexToByteArray();
                        byte[] cipher = alg.Encrypt(plainText);
                        byte[] decrypted = alg.Decrypt(cipher);
                        byte[] expectedDecrypted = "15a818701f0f7c99fe4b1b4b860f131b".HexToByteArray();
                        Assert.Equal<byte>(expectedDecrypted, decrypted);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }
            finally
            {
                _cngKey.Delete();
            }
        }
    }
}
