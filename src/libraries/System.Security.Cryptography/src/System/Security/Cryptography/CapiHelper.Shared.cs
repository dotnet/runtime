// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    internal static partial class CapiHelper
    {
        internal const int S_OK = 0;
        internal const int E_FAIL = unchecked((int)0x80004005);

        //Leaving these constants same as they are defined in Windows
        internal const int ALG_TYPE_DSS = (1 << 9);
        internal const int ALG_TYPE_RSA = (2 << 9);
        internal const int ALG_TYPE_BLOCK = (3 << 9);
        internal const int ALG_CLASS_SIGNATURE = (1 << 13);
        internal const int ALG_CLASS_DATA_ENCRYPT = (3 << 13);
        internal const int ALG_CLASS_KEY_EXCHANGE = (5 << 13);
        internal const int CALG_RSA_SIGN = (ALG_CLASS_SIGNATURE | ALG_TYPE_RSA | 0);
        internal const int CALG_DSS_SIGN = (ALG_CLASS_SIGNATURE | ALG_TYPE_DSS | 0);
        internal const int CALG_RSA_KEYX = (ALG_CLASS_KEY_EXCHANGE | ALG_TYPE_RSA | 0);

        internal const int ALG_CLASS_HASH = (4 << 13);
        internal const int ALG_TYPE_ANY = (0);

        internal const int SIMPLEBLOB = 0x1;
        internal const int PUBLICKEYBLOB = 0x6;
        internal const int PRIVATEKEYBLOB = 0x7;
        internal const int PLAINTEXTKEYBLOB = 0x8;
        internal const byte BLOBHEADER_CURRENT_BVERSION = 0x2;
        internal const int CRYPT_BLOB_VER3 = 0x00000080; // export version 3 of a blob type
        internal const int RSA_PUB_MAGIC = 0x31415352;
        internal const int RSA_PRIV_MAGIC = 0x32415352;

        // Provider type to use by default for RSA operations. We want to use RSA-AES CSP
        // since it enables access to SHA-2 operations. All currently supported OSes support RSA-AES.
        internal const int DefaultRsaProviderType = (int)ProviderType.PROV_RSA_AES;

        internal enum CryptKeyError : uint
        {
            NTE_NO_KEY = 0x8009000D, // Key does not exist.
            NTE_BAD_KEYSET = 0x80090016, // Keyset does not exist.
            NTE_KEYSET_NOT_DEF = 0x80090019, // The keyset is not defined.
            NTE_BAD_KEY_STATE = 0x8009000B,
            NTE_BAD_KEYSET_PARAM = 0x8009001F,
            NTE_KEYSET_ENTRY_BAD = 0x8009001A,
            NTE_FILENOTFOUND = 0x80070002,
            NTE_BAD_KEY = 0x80090003,
            NTE_BAD_FLAGS = 0x80090009,
            NTE_BAD_DATA = 0x80090005,
            NTE_BAD_HASH = 0x80090003,
        }

        /// <summary>
        /// dwProvType is fourth parameter in the CryptAcquireContext and it uses following
        /// </summary>
        internal enum ProviderType : int
        {
            PROV_RSA_FULL = 1,
            PROV_DSS_DH = 13,
            PROV_RSA_AES = 24
        }

        /// <summary>
        /// Helper for RsaCryptoServiceProvider.ImportParameters()
        /// </summary>
        internal static byte[] ToKeyBlob(this RSAParameters rsaParameters)
        {
            // Validate the RSA structure first.
            if (rsaParameters.Modulus == null)
                throw GetBadDataException();

            if (rsaParameters.Exponent == null || rsaParameters.Exponent.Length > 4)
                throw GetBadDataException();

            int modulusLength = rsaParameters.Modulus.Length;
            int halfModulusLength = (modulusLength + 1) / 2;

            // We assume that if P != null, then so are Q, DP, DQ, InverseQ and D
            if (rsaParameters.P != null)
            {
                if (rsaParameters.P.Length != halfModulusLength)
                    throw GetBadDataException();

                if (rsaParameters.Q == null || rsaParameters.Q.Length != halfModulusLength)
                    throw GetBadDataException();

                if (rsaParameters.DP == null || rsaParameters.DP.Length != halfModulusLength)
                    throw GetBadDataException();

                if (rsaParameters.DQ == null || rsaParameters.DQ.Length != halfModulusLength)
                    throw GetBadDataException();

                if (rsaParameters.InverseQ == null || rsaParameters.InverseQ.Length != halfModulusLength)
                    throw GetBadDataException();

                if (rsaParameters.D == null || rsaParameters.D.Length != modulusLength)
                    throw GetBadDataException();
            }

            bool isPrivate = (rsaParameters.P != null && rsaParameters.P.Length != 0);

            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);

            // Write out the BLOBHEADER
            bw.Write((byte)(isPrivate ? PRIVATEKEYBLOB : PUBLICKEYBLOB));  // BLOBHEADER.bType
            bw.Write((byte)(BLOBHEADER_CURRENT_BVERSION));                 // BLOBHEADER.bVersion
            bw.Write((ushort)0);                                           // BLOBHEADER.wReserved
            bw.Write((uint)CapiHelper.CALG_RSA_KEYX);                      // BLOBHEADER.aiKeyAlg

            // Write the RSAPubKey header
            bw.Write((int)(isPrivate ? RSA_PRIV_MAGIC : RSA_PUB_MAGIC));   // RSAPubKey.magic
            bw.Write((uint)(modulusLength * 8));                           // RSAPubKey.bitLen

            uint expAsDword = 0;
            for (int i = 0; i < rsaParameters.Exponent.Length; i++)
            {
                expAsDword <<= 8;
                expAsDword |= rsaParameters.Exponent[i];
            }
            bw.Write((uint)expAsDword);                                    // RSAPubKey.pubExp

            bw.WriteReversed(rsaParameters.Modulus);                       // Copy over the modulus for both public and private

            if (isPrivate)
            {
                bw.WriteReversed(rsaParameters.P!);
                bw.WriteReversed(rsaParameters.Q!);
                bw.WriteReversed(rsaParameters.DP!);
                bw.WriteReversed(rsaParameters.DQ!);
                bw.WriteReversed(rsaParameters.InverseQ!);
                bw.WriteReversed(rsaParameters.D!);
            }

            bw.Flush();
            byte[] key = ms.ToArray();
            return key;
        }

        /// <summary>
        /// Write out a byte array in reverse order.
        /// </summary>
        private static void WriteReversed(this BinaryWriter bw, byte[] bytes)
        {
            byte[] reversedBytes = bytes.CloneByteArray();
            Array.Reverse(reversedBytes);
            bw.Write(reversedBytes);
            return;
        }

        /// <summary>
        /// Helper for RsaCryptoServiceProvider.ExportParameters()
        /// </summary>
        internal static RSAParameters ToRSAParameters(this byte[] cspBlob, bool includePrivateParameters)
        {
            try
            {
                BinaryReader br = new BinaryReader(new MemoryStream(cspBlob));

                br.ReadByte();    // BLOBHEADER.bType: Expected to be 0x6 (PUBLICKEYBLOB) or 0x7 (PRIVATEKEYBLOB), though there's no check for backward compat reasons.
                br.ReadByte(); // BLOBHEADER.bVersion: Expected to be 0x2, though there's no check for backward compat reasons.
                br.ReadUInt16();               // BLOBHEADER.wReserved
                int algId = br.ReadInt32();    // BLOBHEADER.aiKeyAlg
                if (algId != CALG_RSA_KEYX && algId != CALG_RSA_SIGN)
                    throw new PlatformNotSupportedException();  // The FCall this code was ported from supports other algid's but we're only porting what we use.

                br.ReadInt32();    // RSAPubKey.magic: Expected to be 0x31415352 ('RSA1') or 0x32415352 ('RSA2')
                int bitLen = br.ReadInt32();   // RSAPubKey.bitLen

                int modulusLength = bitLen / 8;
                int halfModulusLength = (modulusLength + 1) / 2;

                uint expAsDword = br.ReadUInt32();

                RSAParameters rsaParameters = default;
                rsaParameters.Exponent = ExponentAsBytes(expAsDword);
                rsaParameters.Modulus = br.ReadReversed(modulusLength);
                if (includePrivateParameters)
                {
                    rsaParameters.P = br.ReadReversed(halfModulusLength);
                    rsaParameters.Q = br.ReadReversed(halfModulusLength);
                    rsaParameters.DP = br.ReadReversed(halfModulusLength);
                    rsaParameters.DQ = br.ReadReversed(halfModulusLength);
                    rsaParameters.InverseQ = br.ReadReversed(halfModulusLength);
                    rsaParameters.D = br.ReadReversed(modulusLength);
                }

                return rsaParameters;
            }
            catch (EndOfStreamException)
            {
                // For compat reasons, we throw an E_FAIL CrytoException if CAPI returns a smaller blob than expected.
                // For compat reasons, we ignore the extra bits if the CAPI returns a larger blob than expected.
                throw GetEFailException();
            }
        }

        internal static byte GetKeyBlobHeaderVersion(byte[] cspBlob)
        {
            if (cspBlob.Length < 8)
                throw new EndOfStreamException(); // Simulate same exception as ReadKeyBlobHeader would throw

            // Second byte is BLOBHEADER.bVersion: Expected to be 0x2 or 0x3 for DSA.
            return cspBlob[1];
        }

        /// <summary>
        /// Helper for converting a UInt32 exponent to bytes.
        /// </summary>
        private static byte[] ExponentAsBytes(uint exponent)
        {
            if (exponent <= 0xFF)
            {
                return new[] { (byte)exponent };
            }
            else if (exponent <= 0xFFFF)
            {
                unchecked
                {
                    return new[]
                    {
                        (byte)(exponent >> 8),
                        (byte)(exponent)
                    };
                }
            }
            else if (exponent <= 0xFFFFFF)
            {
                unchecked
                {
                    return new[]
                    {
                        (byte)(exponent >> 16),
                        (byte)(exponent >> 8),
                        (byte)(exponent)
                    };
                }
            }
            else
            {
                return new[]
                {
                    (byte)(exponent >> 24),
                    (byte)(exponent >> 16),
                    (byte)(exponent >> 8),
                    (byte)(exponent)
                };
            }
        }

        /// <summary>
        /// Read in a byte array in reverse order.
        /// </summary>
        private static byte[] ReadReversed(this BinaryReader br, int count)
        {
            byte[] data = br.ReadBytes(count);
            Array.Reverse(data);
            return data;
        }

        internal static byte[]? TrimLargeIV(byte[]? currentIV, int blockSizeInBits)
        {
            int blockSizeBytes = checked((blockSizeInBits + 7) / 8);

            if (currentIV?.Length > blockSizeBytes)
            {
                byte[] tmp = new byte[blockSizeBytes];
                Buffer.BlockCopy(currentIV, 0, tmp, 0, tmp.Length);
                return tmp;
            }

            return currentIV;
        }
    }
}
