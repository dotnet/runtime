// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography.Xml
{
    // abstract class providing symmetric key wrap implementation
    internal static class SymmetricKeyWrap
    {
        private static readonly byte[] s_rgbTripleDES_KW_IV = { 0x4a, 0xdd, 0xa2, 0x2c, 0x79, 0xe8, 0x21, 0x05 };
        private static readonly byte[] s_rgbAES_KW_IV = { 0xa6, 0xa6, 0xa6, 0xa6, 0xa6, 0xa6, 0xa6, 0xa6 };

        //
        // internal static methods
        //

        // CMS TripleDES KeyWrap as described in "http://www.w3.org/2001/04/xmlenc#kw-tripledes"
        [SuppressMessage("Microsoft.Cryptography", "CA5350", Justification = "Explicitly requested by the message contents")]
        internal static byte[] TripleDESKeyWrapEncrypt(byte[] rgbKey, byte[] rgbWrappedKeyData)
        {
            byte[] rgbCKS;

            using (var sha = SHA1.Create())
            {
                // checksum the key
                rgbCKS = sha.ComputeHash(rgbWrappedKeyData);
            }

            // generate a random IV
            byte[] rgbIV = new byte[8];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(rgbIV);
            }

            // rgbWKCS = rgbWrappedKeyData | (first 8 bytes of the hash)
            byte[] rgbWKCKS = new byte[rgbWrappedKeyData.Length + 8];
            TripleDES tripleDES = null;
            ICryptoTransform enc1 = null;
            ICryptoTransform enc2 = null;

            try
            {
                tripleDES = TripleDES.Create();
                // Don't add padding, use CBC mode: for example, a 192 bits key will yield 40 bytes of encrypted data
                tripleDES.Padding = PaddingMode.None;
                enc1 = tripleDES.CreateEncryptor(rgbKey, rgbIV);
                enc2 = tripleDES.CreateEncryptor(rgbKey, s_rgbTripleDES_KW_IV);

                Buffer.BlockCopy(rgbWrappedKeyData, 0, rgbWKCKS, 0, rgbWrappedKeyData.Length);
                Buffer.BlockCopy(rgbCKS, 0, rgbWKCKS, rgbWrappedKeyData.Length, 8);
                byte[] temp1 = enc1.TransformFinalBlock(rgbWKCKS, 0, rgbWKCKS.Length);
                byte[] temp2 = new byte[rgbIV.Length + temp1.Length];
                Buffer.BlockCopy(rgbIV, 0, temp2, 0, rgbIV.Length);
                Buffer.BlockCopy(temp1, 0, temp2, rgbIV.Length, temp1.Length);
                // temp2 = REV (rgbIV | E_k(rgbWrappedKeyData | rgbCKS))
                Array.Reverse(temp2);

                return enc2.TransformFinalBlock(temp2, 0, temp2.Length);
            }
            finally
            {
                enc2?.Dispose();
                enc1?.Dispose();
                tripleDES?.Dispose();
            }
        }

        [SuppressMessage("Microsoft.Cryptography", "CA5350", Justification = "Explicitly requested by the message contents")]
        internal static byte[] TripleDESKeyWrapDecrypt(byte[] rgbKey, byte[] rgbEncryptedWrappedKeyData)
        {
            // Check to see whether the length of the encrypted key is reasonable
            if (rgbEncryptedWrappedKeyData.Length != 32 && rgbEncryptedWrappedKeyData.Length != 40
                && rgbEncryptedWrappedKeyData.Length != 48)
                throw new CryptographicException(SR.Cryptography_Xml_KW_BadKeySize);

            TripleDES tripleDES = null;
            ICryptoTransform dec1 = null;
            ICryptoTransform dec2 = null;

            try
            {
                tripleDES = TripleDES.Create();
                // Assume no padding, use CBC mode
                tripleDES.Padding = PaddingMode.None;
                dec1 = tripleDES.CreateDecryptor(rgbKey, s_rgbTripleDES_KW_IV);

                byte[] temp2 = dec1.TransformFinalBlock(rgbEncryptedWrappedKeyData, 0, rgbEncryptedWrappedKeyData.Length);
                Array.Reverse(temp2);
                // Get the IV and temp1
                byte[] rgbIV = new byte[8];
                Buffer.BlockCopy(temp2, 0, rgbIV, 0, 8);
                byte[] temp1 = new byte[temp2.Length - rgbIV.Length];
                Buffer.BlockCopy(temp2, 8, temp1, 0, temp1.Length);

                dec2 = tripleDES.CreateDecryptor(rgbKey, rgbIV);
                byte[] rgbWKCKS = dec2.TransformFinalBlock(temp1, 0, temp1.Length);

                // checksum the key
                byte[] rgbWrappedKeyData = new byte[rgbWKCKS.Length - 8];
                Buffer.BlockCopy(rgbWKCKS, 0, rgbWrappedKeyData, 0, rgbWrappedKeyData.Length);
                using (var sha = SHA1.Create())
                {
                    byte[] rgbCKS = sha.ComputeHash(rgbWrappedKeyData);
                    for (int index = rgbWrappedKeyData.Length, index1 = 0; index < rgbWKCKS.Length; index++, index1++)
                        if (rgbWKCKS[index] != rgbCKS[index1])
                            throw new CryptographicException(SR.Cryptography_Xml_BadWrappedKeySize);
                    return rgbWrappedKeyData;
                }
            }
            finally
            {
                dec2?.Dispose();
                dec1?.Dispose();
                tripleDES?.Dispose();
            }
        }

        // AES KeyWrap described in "http://www.w3.org/2001/04/xmlenc#kw-aes***", as suggested by NIST
        internal static byte[] AESKeyWrapEncrypt(byte[] rgbKey, byte[] rgbWrappedKeyData)
        {
            int N = rgbWrappedKeyData.Length >> 3;
            // The information wrapped need not actually be a key, but it needs to be a multiple of 64 bits
            if ((rgbWrappedKeyData.Length % 8 != 0) || N <= 0)
                throw new CryptographicException(SR.Cryptography_Xml_KW_BadKeySize);

            Aes aes = null;
            ICryptoTransform enc = null;

            try
            {
                aes = Aes.Create();
                aes.Key = rgbKey;
                // Use ECB mode, no padding
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.None;
                enc = aes.CreateEncryptor();
                // special case: only 1 block -- 8 bytes
                if (N == 1)
                {
                    // temp = 0xa6a6a6a6a6a6a6a6 | P(1)
                    byte[] temp = new byte[s_rgbAES_KW_IV.Length + rgbWrappedKeyData.Length];
                    Buffer.BlockCopy(s_rgbAES_KW_IV, 0, temp, 0, s_rgbAES_KW_IV.Length);
                    Buffer.BlockCopy(rgbWrappedKeyData, 0, temp, s_rgbAES_KW_IV.Length, rgbWrappedKeyData.Length);
                    return enc.TransformFinalBlock(temp, 0, temp.Length);
                }
                // second case: more than 1 block
                long t = 0;
                byte[] rgbOutput = new byte[(N + 1) << 3];
                // initialize the R_i's
                Buffer.BlockCopy(rgbWrappedKeyData, 0, rgbOutput, 8, rgbWrappedKeyData.Length);
                byte[] rgbA = new byte[8];
                byte[] rgbBlock = new byte[16];
                Buffer.BlockCopy(s_rgbAES_KW_IV, 0, rgbA, 0, 8);
                for (int j = 0; j <= 5; j++)
                {
                    for (int i = 1; i <= N; i++)
                    {
                        t = i + j * N;
                        Buffer.BlockCopy(rgbA, 0, rgbBlock, 0, 8);
                        Buffer.BlockCopy(rgbOutput, 8 * i, rgbBlock, 8, 8);
                        byte[] rgbB = enc.TransformFinalBlock(rgbBlock, 0, 16);
                        for (int k = 0; k < 8; k++)
                        {
                            byte tmp = (byte)((t >> (8 * (7 - k))) & 0xFF);
                            rgbA[k] = (byte)(tmp ^ rgbB[k]);
                        }
                        Buffer.BlockCopy(rgbB, 8, rgbOutput, 8 * i, 8);
                    }
                }
                // Set the first block of rgbOutput to rgbA
                Buffer.BlockCopy(rgbA, 0, rgbOutput, 0, 8);
                return rgbOutput;
            }
            finally
            {
                enc?.Dispose();
                aes?.Dispose();
            }
        }

        internal static byte[] AESKeyWrapDecrypt(byte[] rgbKey, byte[] rgbEncryptedWrappedKeyData)
        {
            int N = (rgbEncryptedWrappedKeyData.Length >> 3) - 1;
            // The information wrapped need not actually be a key, but it needs to be a multiple of 64 bits
            if ((rgbEncryptedWrappedKeyData.Length % 8 != 0) || N <= 0)
                throw new CryptographicException(SR.Cryptography_Xml_KW_BadKeySize);

            byte[] rgbOutput = new byte[N << 3];
            Aes aes = null;
            ICryptoTransform dec = null;

            try
            {
                aes = Aes.Create();
                aes.Key = rgbKey;
                // Use ECB mode, no padding
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.None;
                dec = aes.CreateDecryptor();

                // special case: only 1 block -- 8 bytes
                if (N == 1)
                {
                    byte[] temp = dec.TransformFinalBlock(rgbEncryptedWrappedKeyData, 0, rgbEncryptedWrappedKeyData.Length);
                    // checksum the key
                    for (int index = 0; index < 8; index++)
                        if (temp[index] != s_rgbAES_KW_IV[index])
                            throw new CryptographicException(SR.Cryptography_Xml_BadWrappedKeySize);
                    // rgbOutput is LSB(temp)
                    Buffer.BlockCopy(temp, 8, rgbOutput, 0, 8);
                    return rgbOutput;
                }
                // second case: more than 1 block
                long t = 0;
                // initialize the C_i's
                Buffer.BlockCopy(rgbEncryptedWrappedKeyData, 8, rgbOutput, 0, rgbOutput.Length);
                byte[] rgbA = new byte[8];
                byte[] rgbBlock = new byte[16];
                Buffer.BlockCopy(rgbEncryptedWrappedKeyData, 0, rgbA, 0, 8);
                for (int j = 5; j >= 0; j--)
                {
                    for (int i = N; i >= 1; i--)
                    {
                        t = i + j * N;
                        for (int k = 0; k < 8; k++)
                        {
                            byte tmp = (byte)((t >> (8 * (7 - k))) & 0xFF);
                            rgbA[k] ^= tmp;
                        }
                        Buffer.BlockCopy(rgbA, 0, rgbBlock, 0, 8);
                        Buffer.BlockCopy(rgbOutput, 8 * (i - 1), rgbBlock, 8, 8);
                        byte[] rgbB = dec.TransformFinalBlock(rgbBlock, 0, 16);
                        Buffer.BlockCopy(rgbB, 8, rgbOutput, 8 * (i - 1), 8);
                        Buffer.BlockCopy(rgbB, 0, rgbA, 0, 8);
                    }
                }
                // checksum the key
                if (!rgbA.AsSpan().SequenceEqual(s_rgbAES_KW_IV.AsSpan()))
                    throw new CryptographicException(SR.Cryptography_Xml_BadWrappedKeySize);
                return rgbOutput;
            }
            finally
            {
                dec?.Dispose();
                aes?.Dispose();
            }
        }
    }
}
