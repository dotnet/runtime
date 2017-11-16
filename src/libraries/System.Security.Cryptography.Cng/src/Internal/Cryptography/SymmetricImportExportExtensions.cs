// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    internal static class SymmetricImportExportExtensions
    {

        /// <summary>
        /// Note! This can and likely will throw if the algorithm was given a hardware-based key.
        /// </summary>
        public static byte[] GetSymmetricKeyDataIfExportable(this CngKey cngKey, string algorithm)
        {
            byte[] keyBlob = cngKey.Export(s_cipherKeyBlobFormat);
            using (MemoryStream ms = new MemoryStream(keyBlob))
            {
                using (BinaryReader br = new BinaryReader(ms, Encoding.Unicode))
                {
                    // Read NCRYPT_KEY_BLOB_HEADER
                    int cbSize = br.ReadInt32();                      // NCRYPT_KEY_BLOB_HEADER.cbSize
                    if (cbSize != SizeOf_NCRYPT_KEY_BLOB_HEADER_SIZE)
                        throw new CryptographicException(SR.Cryptography_KeyBlobParsingError);

                    int ncryptMagic = br.ReadInt32();                 // NCRYPT_KEY_BLOB_HEADER.dwMagic
                    if (ncryptMagic != Interop.NCrypt.NCRYPT_CIPHER_KEY_BLOB_MAGIC)
                        throw new CryptographicException(SR.Cryptography_KeyBlobParsingError);

                    int cbAlgName = br.ReadInt32();                   // NCRYPT_KEY_BLOB_HEADER.cbAlgName

                    br.ReadInt32();                                   // NCRYPT_KEY_BLOB_HEADER.cbKey

                    string algorithmName = new string(br.ReadChars((cbAlgName / 2) - 1));
                    if (algorithmName != algorithm)
                        throw new CryptographicException(SR.Format(SR.Cryptography_CngKeyWrongAlgorithm, algorithmName, algorithm));

                    char nullTerminator = br.ReadChar();
                    if (nullTerminator != 0)
                        throw new CryptographicException(SR.Cryptography_KeyBlobParsingError);

                    // Read BCRYPT_KEY_DATA_BLOB_HEADER
                    int bcryptMagic = br.ReadInt32();                 // BCRYPT_KEY_DATA_BLOB_HEADER.dwMagic
                    if (bcryptMagic != Interop.BCrypt.BCRYPT_KEY_DATA_BLOB_MAGIC)
                        throw new CryptographicException(SR.Cryptography_KeyBlobParsingError);

                    int dwVersion = br.ReadInt32();                   // BCRYPT_KEY_DATA_BLOB_HEADER.dwVersion
                    if (dwVersion != Interop.BCrypt.BCRYPT_KEY_DATA_BLOB_VERSION1)
                        throw new CryptographicException(SR.Cryptography_KeyBlobParsingError);

                    int keyLength = br.ReadInt32();                   // BCRYPT_KEY_DATA_BLOB_HEADER.cbKeyData
                    byte[] key = br.ReadBytes(keyLength);
                    return key;
                }
            }
        }

        private const int SizeOf_NCRYPT_KEY_BLOB_HEADER_SIZE = sizeof(int) + sizeof(int) + sizeof(int) + sizeof(int);
        private const int SizeOf_BCRYPT_KEY_DATA_BLOB_HEADER = sizeof(int) + sizeof(int) + sizeof(int);

        private static readonly CngKeyBlobFormat s_cipherKeyBlobFormat = new CngKeyBlobFormat(Interop.NCrypt.NCRYPT_CIPHER_KEY_BLOB);
    }
}
