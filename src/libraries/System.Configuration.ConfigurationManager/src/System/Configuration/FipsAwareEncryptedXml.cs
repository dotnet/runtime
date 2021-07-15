// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Xml;
using System.Linq;

namespace System.Configuration
{
    //
    // Extends EncryptedXml to use FIPS-certified symmetric algorithm
    //
    internal sealed class FipsAwareEncryptedXml : EncryptedXml
    {
        public FipsAwareEncryptedXml(XmlDocument doc)
            : base(doc)
        {
        }

        public override SymmetricAlgorithm GetDecryptionKey(EncryptedData encryptedData, string symmetricAlgorithmUri)
        {
            if (IsAES256(encryptedData, symmetricAlgorithmUri))
            {
                // Obtain the EncryptedKey
                EncryptedKey ek = encryptedData.KeyInfo.OfType<KeyInfoEncryptedKey>().FirstOrDefault()?.EncryptedKey;

                // Got an EncryptedKey, decrypt it to get the AES key
                if (ek != null)
                {
                    byte[] key = DecryptEncryptedKey(ek);

                    // Construct FIPS-certified AES provider
                    if (key != null)
                    {
                        var aes = Aes.Create(typeof(AesManaged).FullName), typeof(AesManaged));
                        aes.Key = key;

                        return aes;
                    }
                }
            }

            // Fallback to the base implementation
            return base.GetDecryptionKey(encryptedData, symmetricAlgorithmUri);
        }

        private static bool IsAES256(EncryptedData encryptedData, string algorithmUri)
        {
            if (encryptedData != null && encryptedData.KeyInfo != null && (algorithmUri != null || encryptedData.EncryptionMethod != null))
            {
                if (algorithmUri == null)
                {
                    algorithmUri = encryptedData.EncryptionMethod.KeyAlgorithm;
                }

                // Check if the Uri matches AES256
                return string.Equals(algorithmUri, XmlEncAES256Url, StringComparison.InvariantCultureIgnoreCase);
            }

            return false;
        }
    }
}
