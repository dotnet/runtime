// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace Internal.NativeCrypto
{
    internal static partial class CapiHelper
    {
        /// <summary>
        /// Helper for RC2CryptoServiceProvider and DESCryptoServiceProvider
        /// </summary>
        internal static byte[] ToPlainTextKeyBlob(int algId, byte[] rawKey)
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                // Write out the BLOBHEADER
                WriteKeyBlobHeader(algId, bw);

                // Write out the size + key contents
                bw.Write((int)rawKey.Length);
                bw.Write(rawKey);

                bw.Flush();
                byte[] key = ms.ToArray();
                return key;
            }
        }

        private static void WriteKeyBlobHeader(int algId, BinaryWriter bw)
        {
            // Write out the BLOBHEADER.
            bw.Write((byte)PLAINTEXTKEYBLOB);               // BLOBHEADER.bType
            bw.Write((byte)BLOBHEADER_CURRENT_BVERSION);    // BLOBHEADER.bVersion
            bw.Write((ushort)0);                            // BLOBHEADER.wReserved
            bw.Write(algId);                                // BLOBHEADER.aiKeyAlg
        }
    }
}
