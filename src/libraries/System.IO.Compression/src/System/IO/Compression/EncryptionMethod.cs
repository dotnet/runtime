// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    /// <summary>
    /// Specifies the encryption method used to encrypt an entry in a zip archive.
    /// </summary>
    public enum EncryptionMethod
    {
        /// <summary>
        /// No Encryption is applied to the entry.
        /// </summary>
        None = 0,

        /// <summary>
        /// Legacy PKware encryption.
        /// </summary>
        ZipCrypto = 1,

        /// <summary>
        /// WinZip AES encryption with 128-bit key.
        /// </summary>
        Aes128 = 2,

        /// <summary>
        /// WinZip AES encryption with 192-bit key.
        /// </summary>
        Aes192 = 3,

        /// <summary>
        /// WinZip AES encryption with 256-bit key.
        /// </summary>
        Aes256 = 4
    }
}
