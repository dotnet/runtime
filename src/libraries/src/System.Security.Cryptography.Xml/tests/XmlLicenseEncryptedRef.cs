// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{
    public class XmlLicenseEncryptedRef : IRelDecryptor
    {
        List<AsymmetricAlgorithm> _asymmetricKeys = new List<AsymmetricAlgorithm>();

        public XmlLicenseEncryptedRef()
        {
        }

        public void AddAsymmetricKey(AsymmetricAlgorithm key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            _asymmetricKeys.Add(key);
        }

        private static bool PublicKeysEqual(RSAParameters a, RSAParameters b)
        {
            return a.Exponent.SequenceEqual(b.Exponent) && a.Modulus.SequenceEqual(b.Modulus);
        }

        public Stream Decrypt(EncryptionMethod encryptionMethod, KeyInfo keyInfo, Stream toDecrypt)
        {
            Assert.NotNull(encryptionMethod);
            Assert.NotNull(keyInfo);
            Assert.NotNull(toDecrypt);
            Assert.True(encryptionMethod.KeyAlgorithm == EncryptedXml.XmlEncAES128Url
                     || encryptionMethod.KeyAlgorithm == EncryptedXml.XmlEncAES192Url
                     || encryptionMethod.KeyAlgorithm == EncryptedXml.XmlEncAES256Url);

            Assert.Equal(keyInfo.Count, 1);

            byte[] decryptedKey = null;

            foreach (KeyInfoClause clause in keyInfo)
            {
                if (clause is KeyInfoEncryptedKey)
                {
                    KeyInfoEncryptedKey encryptedKeyInfo = clause as KeyInfoEncryptedKey;
                    EncryptedKey encryptedKey = encryptedKeyInfo.EncryptedKey;

                    Assert.Equal(encryptedKey.EncryptionMethod.KeyAlgorithm, EncryptedXml.XmlEncRSAOAEPUrl);
                    Assert.Equal(encryptedKey.KeyInfo.Count, 1);
                    Assert.NotEqual(_asymmetricKeys.Count, 0);

                    RSAParameters rsaParams = new RSAParameters();
                    RSAParameters rsaInputParams = new RSAParameters();

                    foreach (KeyInfoClause rsa in encryptedKey.KeyInfo)
                    {
                        if (rsa is RSAKeyValue)
                        {
                            rsaParams = (rsa as RSAKeyValue).Key.ExportParameters(false);
                            break;
                        }
                        else
                        {
                            Assert.True(false, "Invalid License - MalformedKeyInfoClause");
                        }
                    }

                    bool keyMismatch = true;
                    foreach (AsymmetricAlgorithm key in _asymmetricKeys)
                    {
                        RSA rsaKey = key as RSA;
                        Assert.NotNull(rsaKey);

                        rsaInputParams = rsaKey.ExportParameters(false);

                        if (!PublicKeysEqual(rsaParams, rsaInputParams))
                        {
                            continue;
                        }

                        keyMismatch = false;

                        // Decrypt session key
                        byte[] encryptedKeyValue = encryptedKey.CipherData.CipherValue;

                        if (encryptedKeyValue == null)
                            throw new CryptographicException("MissingKeyCipher");

                        decryptedKey = EncryptedXml.DecryptKey(encryptedKeyValue,
                                                                     rsaKey, true);
                        break;
                    }

                    if (keyMismatch)
                    {
                        throw new Exception("Invalid License - AsymmetricKeyMismatch");
                    }
                }
                else if (clause is KeyInfoName)
                {
                    Assert.True(false, "This test should not have KeyInfoName clauses");
                }
                else
                {
                    throw new CryptographicException("MalformedKeyInfoClause");
                }

                break;
            }

            if (decryptedKey == null)
            {
                throw new CryptographicException("KeyDecryptionFailure");
            }

            using (Aes aes = Aes.Create())
            {
                aes.Key = decryptedKey;
                aes.Padding = PaddingMode.PKCS7;
                aes.Mode = CipherMode.CBC;
                return DecryptStream(toDecrypt, aes);
            }
        }

        private static Stream DecryptStream(Stream toDecrypt, SymmetricAlgorithm alg)
        {
            Assert.NotNull(alg);

            byte[] IV = new byte[alg.BlockSize / 8];

            // Get the IV from the encrypted content.
            toDecrypt.Read(IV, 0, IV.Length);
            byte[] encryptedContentValue = new byte[toDecrypt.Length - IV.Length];

            // Get the encrypted content following the IV.
            toDecrypt.Read(encryptedContentValue, 0, encryptedContentValue.Length);

            byte[] decryptedContent;

            using (var msDecrypt = new MemoryStream())
            using (ICryptoTransform decryptor = alg.CreateDecryptor(alg.Key, IV))
            using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Write))
            {
                csDecrypt.Write(encryptedContentValue, 0, encryptedContentValue.Length);
                csDecrypt.FlushFinalBlock();
                decryptedContent = msDecrypt.ToArray();
            }

            return new MemoryStream(decryptedContent);
        }

        public static void Encrypt(Stream toEncrypt, RSA key, out KeyInfo keyInfo, out EncryptionMethod encryptionMethod, out CipherData cipherData)
        {
            using (Aes sessionKey = Aes.Create())
            {
                sessionKey.KeySize = 128;
                encryptionMethod = new EncryptionMethod(EncryptedXml.XmlEncAES128Url);
                keyInfo = new KeyInfo();

                EncryptedKey encKey;
                keyInfo.AddClause(
                    new KeyInfoEncryptedKey(
                        encKey = new EncryptedKey()
                        {
                            CipherData = new CipherData(EncryptedXml.EncryptKey(sessionKey.Key, key, useOAEP: true)),
                            EncryptionMethod = new EncryptionMethod(EncryptedXml.XmlEncRSAOAEPUrl)
                        }));

                encKey.KeyInfo.AddClause(new RSAKeyValue(key));

                byte[] dataToEncrypt = new byte[toEncrypt.Length];
                toEncrypt.Read(dataToEncrypt, 0, (int)toEncrypt.Length);

                var encryptedXml = new EncryptedXml();
                encryptedXml.Padding = PaddingMode.PKCS7;
                encryptedXml.Mode = CipherMode.CBC;
                byte[] encryptedData = encryptedXml.EncryptData(dataToEncrypt, sessionKey);
                cipherData = new CipherData(encryptedData);
            }
        }

        [Fact]
        public static void ItRoundTrips()
        {
            byte[] input = new byte[] { 1, 2, 7, 4 };
            MemoryStream ms = new MemoryStream(input);
            KeyInfo keyInfo;
            EncryptionMethod encMethod;
            CipherData cipherData;
            using (RSA rsa = RSA.Create())
            {
                Encrypt(ms, rsa, out keyInfo, out encMethod, out cipherData);

                XmlLicenseEncryptedRef decr = new XmlLicenseEncryptedRef();
                decr.AddAsymmetricKey(rsa);
                using (var encrypted = new MemoryStream(cipherData.CipherValue))
                using (Stream decrypted = decr.Decrypt(encMethod, keyInfo, encrypted))
                {
                    byte[] decryptedBytes = new byte[decrypted.Length];
                    decrypted.Read(decryptedBytes, 0, (int)decrypted.Length);
                    Assert.Equal(input, decryptedBytes);
                }
            }
        }
    }
}
