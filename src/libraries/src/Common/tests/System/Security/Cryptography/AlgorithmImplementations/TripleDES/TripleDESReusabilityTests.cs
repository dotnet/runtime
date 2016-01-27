// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Encryption.TripleDes.Tests
{
    public static class TripleDESReusabilityTests
    {
        [Fact]
        public static void TripleDESReuseEncryptorDecryptor()
        {
            byte[] key = "6b42da08f93e819fbd26fce0785b0eec3d0cb6bfa053c505".HexToByteArray();
            byte[] iv = "8fc67ce5e7f28cde".HexToByteArray();

            using (TripleDES alg = TripleDESFactory.Create())
            {
                alg.Key = key;
                alg.IV = iv;
                alg.Padding = PaddingMode.PKCS7;
                alg.Mode = CipherMode.CBC;

                using (ICryptoTransform encryptor = alg.CreateEncryptor())
                using (ICryptoTransform decryptor = alg.CreateDecryptor())
                {
                    for (int i = 0; i < 2; i++)
                    {
                        byte[] plainText1 = "e867f915e275eab27d6951165d26dec6dd0acafcfc".HexToByteArray();
                        byte[] cipher1 = encryptor.Transform(plainText1);
                        byte[] expectedCipher1 = "446f57875e107702afde16b57eaf250b87b8110bef29af89".HexToByteArray();
                        Assert.Equal<byte>(expectedCipher1, cipher1);

                        byte[] decrypted1 = decryptor.Transform(cipher1);
                        byte[] expectedDecrypted1 = "e867f915e275eab27d6951165d26dec6dd0acafcfc".HexToByteArray();
                        Assert.Equal<byte>(expectedDecrypted1, decrypted1);


                        byte[] plainText2 = "54686973206973206120736563726574206d657373616765".HexToByteArray();
                        byte[] cipher2 = encryptor.Transform(plainText2);
                        byte[] expectedCipher2 = "da6af8adc5d934c24943176db82eef34aa027c93e9dbe52dc5f1fa64fef4061c".HexToByteArray();
                        Assert.Equal<byte>(expectedCipher2, cipher2);

                        byte[] decrypted2 = decryptor.Transform(cipher2);
                        byte[] expectedDecrypted2 = "54686973206973206120736563726574206d657373616765".HexToByteArray();
                        Assert.Equal<byte>(expectedDecrypted2, decrypted2);
                    }
                }
            }
        }
    }
}
