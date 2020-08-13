// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;

namespace Internal.NativeCrypto
{
    internal static partial class CapiHelper
    {
        private const int DSS_Q_LEN = 20;

        internal const int DSS_MAGIC = 0x31535344;           // Encoding of "DSS1"
        internal const int DSS_PRIVATE_MAGIC = 0x32535344;   // Encoding of "DSS2"
        internal const int DSS_PUB_MAGIC_VER3 = 0x33535344;  // Encoding of "DSS3"
        internal const int DSS_PRIV_MAGIC_VER3 = 0x34535344; // Encoding of "DSS4"

        /// <summary>
        /// Helper for DsaCryptoServiceProvider.ImportParameters()
        /// </summary>
        internal static byte[] ToKeyBlob(this DSAParameters dsaParameters)
        {
            // Validate the DSA structure first
            // P and Q are required. Q is a 160 bit divisor of P-1.
            if (dsaParameters.P == null || dsaParameters.P.Length == 0 || dsaParameters.Q == null || dsaParameters.Q.Length != DSS_Q_LEN)
                throw GetBadDataException();

            // G is required. G is an element of Z_p
            if (dsaParameters.G == null || dsaParameters.G.Length != dsaParameters.P.Length)
                throw GetBadDataException();

            // If J is present, it should be less than the size of P: J = (P-1) / Q
            // This is only a sanity check. Not doing it here is not really an issue as CAPI will fail.
            if (dsaParameters.J != null && dsaParameters.J.Length >= dsaParameters.P.Length)
                throw GetBadDataException();

            // Y is present for V3 DSA key blobs, Y = g^j mod P
            if (dsaParameters.Y != null && dsaParameters.Y.Length != dsaParameters.P.Length)
                throw GetBadDataException();

            // The seed is always a 20 byte array
            if (dsaParameters.Seed != null && dsaParameters.Seed.Length != 20)
                throw GetBadDataException();

            bool isPrivate = (dsaParameters.X != null && dsaParameters.X.Length > 0);

            // The private key should be the same length as Q
            if (isPrivate && dsaParameters.X!.Length != DSS_Q_LEN)
                throw GetBadDataException();

            uint bitLenP = (uint)dsaParameters.P.Length * 8;
            uint bitLenJ = dsaParameters.J == null ? 0 : (uint)dsaParameters.J.Length * 8;

            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                // Write out the BLOBHEADER
                bool isV3;
                WriteKeyBlobHeader(dsaParameters, bw, isPrivate, out isV3);

                // Write out the DSA key
                if (isV3)
                {
                    // We need to build a key blob (DSSPUBKEY_VER3 or DSSPRIVKEY_VER3) as follows:
                    //  DWORD           magic
                    //  DWORD           bitlenP
                    //  DWORD           bitlenQ
                    //  DWORD           bitlenJ
                    //  DWORD           bitlenX (if private)
                    //  DWORD           counter (DSSSEED)
                    //  BYTE[20]        seed (DSSSEED)
                    //  BYTE[lenP]      P
                    //  BYTE[lenQ]      Q
                    //  BYTE[lenP]      G
                    //  BYTE[lenJ]      J (optional)
                    //  BYTE[lenP]      Y
                    //  BYTE[lenX]      X (if private)

                    bw.Write((int)(isPrivate ? DSS_PRIV_MAGIC_VER3 : DSS_PUB_MAGIC_VER3));
                    bw.Write((uint)(bitLenP));
                    bw.Write((uint)(dsaParameters.Q.Length * 8));
                    bw.Write((uint)(bitLenJ));

                    if (isPrivate)
                    {
                        bw.Write((uint)dsaParameters.X!.Length * 8);
                    }

                    WriteDSSSeed(dsaParameters, bw);

                    bw.WriteReversed(dsaParameters.P);
                    bw.WriteReversed(dsaParameters.Q);
                    bw.WriteReversed(dsaParameters.G);

                    if (bitLenJ != 0)
                    {
                        bw.WriteReversed(dsaParameters.J!);
                    }

                    bw.WriteReversed(dsaParameters.Y!);

                    if (isPrivate)
                    {
                        bw.WriteReversed(dsaParameters.X!);
                    }
                }
                else
                {
                    // We need to build a key blob as follows:
                    //  DWORD           magic (DSSPUBKEY)
                    //  DWORD           bitlen (DSSPUBKEY)
                    //  BYTE[len]       P
                    //  BYTE[DSS_Q_LEN] Q
                    //  BYTE[len]       G
                    //  BYTE[20]        X (if private)
                    //  BYTE[len]       Y (if public)
                    //  DWORD           counter (DSSSEED)
                    //  BYTE[20]        seed (DSSSEED)

                    bw.Write((int)(isPrivate ? DSS_PRIVATE_MAGIC : DSS_MAGIC));
                    bw.Write((uint)(bitLenP));
                    bw.WriteReversed(dsaParameters.P);
                    bw.WriteReversed(dsaParameters.Q);
                    bw.WriteReversed(dsaParameters.G);

                    if (isPrivate)
                    {
                        bw.WriteReversed(dsaParameters.X!);
                    }
                    else
                    {
                        Debug.Assert(dsaParameters.Y != null);
                        bw.WriteReversed(dsaParameters.Y);
                    }

                    WriteDSSSeed(dsaParameters, bw);
                }

                bw.Flush();
                byte[] key = ms.ToArray();
                return key;
            }
        }

        /// <summary>
        /// Helper for DSACryptoServiceProvider.ExportParameters()
        /// </summary>
        internal static DSAParameters ToDSAParameters(this byte[] cspBlob, bool includePrivateParameters, byte[]? cspPublicBlob)
        {
            try
            {
                using (var ms = new MemoryStream(cspBlob))
                using (var br = new BinaryReader(ms))
                {
                    byte bVersion;
                    ReadKeyBlobHeader(br, out bVersion);

                    DSAParameters dsaParameters = default;

                    if (bVersion > 2)
                    {
                        Debug.Assert(cspPublicBlob == null);

                        // We need to read a key blob (DSSPUBKEY_VER3 or DSSPRIVKEY_VER3) as follows:
                        //  DWORD           magic
                        //  DWORD           bitlenP
                        //  DWORD           bitlenQ
                        //  DWORD           bitlenJ
                        //  DWORD           bitlenX (if private)
                        //  DWORD           counter (DSSSEED)
                        //  BYTE[20]        seed (DSSSEED)
                        //  BYTE[lenP]      P
                        //  BYTE[lenQ]      Q
                        //  BYTE[lenP]      G
                        //  BYTE[lenJ]      J (optional)
                        //  BYTE[lenP]      Y
                        //  BYTE[lenX]      X (if private)

                        br.ReadInt32(); // Expected to be DSS_PUB_MAGIC_VER3 or DSS_PRIV_MAGIC_VER3
                        int lenP = (br.ReadInt32() + 7) / 8;
                        int lenQ = (br.ReadInt32() + 7) / 8;
                        int lenJ = (br.ReadInt32() + 7) / 8;

                        int lenX = 0;
                        if (includePrivateParameters)
                        {
                            lenX = (br.ReadInt32() + 7) / 8;
                        }

                        ReadDSSSeed(dsaParameters, br, true);

                        dsaParameters.P = br.ReadReversed(lenP);
                        dsaParameters.Q = br.ReadReversed(lenQ);
                        dsaParameters.G = br.ReadReversed(lenP);

                        if (lenJ > 0)
                        {
                            dsaParameters.J = br.ReadReversed(lenJ);
                        }

                        dsaParameters.Y = br.ReadReversed(lenP);

                        if (includePrivateParameters)
                        {
                            dsaParameters.X = br.ReadReversed(lenX);
                        }
                    }
                    else
                    {
                        // We need to read a key blob as follows:
                        //  DWORD           magic (DSSPUBKEY)
                        //  DWORD           bitlen (DSSPUBKEY)
                        //  BYTE[len]       P
                        //  BYTE[DSS_Q_LEN] Q
                        //  BYTE[len]       G
                        //  BYTE[20]        X (if private)
                        //  BYTE[len]       Y (if public)
                        //  DWORD           counter (DSSSEED)
                        //  BYTE[20]        seed (DSSSEED)

                        br.ReadInt32();    // Expected to be DSS_MAGIC or DSS_PRIVATE_MAGIC
                        int len = (br.ReadInt32() + 7) / 8;
                        dsaParameters.P = br.ReadReversed(len);
                        dsaParameters.Q = br.ReadReversed(DSS_Q_LEN);
                        dsaParameters.G = br.ReadReversed(len);

                        long keyLocation = 0;
                        if (includePrivateParameters)
                        {
                            // Save the position of the stream for later access to Y.
                            keyLocation = br.BaseStream.Position;
                            dsaParameters.X = br.ReadReversed(20);
                        }
                        else
                            dsaParameters.Y = br.ReadReversed(len);

                        ReadDSSSeed(dsaParameters, br, false);

                        if (includePrivateParameters)
                        {
                            // If a previous call to CAPI returned a v2 private blob, which was then passed
                            // to ImportCspBlob(byte[] keyBlob) under Unix then that is not supported.
                            // Only Unix calls ToDSAParameters from ImportCspBlob; Windows imports directly via CAPI.
                            // This can only happen if a v2 private blob was obtained directly through
                            // CAPI and saved away for later use, because exporting a private blob with ExportCspBlob
                            // will always export a v3 blob which contains both public and private keys.
                            if (cspPublicBlob == null)
                                throw new CryptographicUnexpectedOperationException();

                            // Since DSSPUBKEY is used for either public or private key, we got X
                            // but not Y. To get Y, use the public key blob.
                            using (var msPublicBlob = new MemoryStream(cspPublicBlob))
                            using (var brPublicBlob = new BinaryReader(msPublicBlob))
                            {
                                brPublicBlob.BaseStream.Position = keyLocation;
                                dsaParameters.Y = brPublicBlob.ReadReversed(len);
                            }
                        }
                    }

                    return dsaParameters;
                }
            }
            catch (EndOfStreamException)
            {
                // For compat reasons, we throw an E_FAIL CrytoException if CAPI returns a smaller blob than expected.
                // For compat reasons, we ignore the extra bits if the CAPI returns a larger blob than expected.
                throw GetEFailException();
            }
        }

        private static void ReadKeyBlobHeader(BinaryReader br, out byte bVersion)
        {
            // The format of BLOBHEADER (or PUBLICKEYSTRUC):
            //  BYTE   bType
            //  BYTE   bVersion
            //  WORD   reserved
            //  ALG_ID aiKeyAlg

            br.ReadByte();    // BLOBHEADER.bType: Expected to be 0x6 (PUBLICKEYBLOB) or 0x7 (PRIVATEKEYBLOB), though there's no check for backward compat reasons.
            bVersion = br.ReadByte();      // BLOBHEADER.bVersion: Expected to be 0x2 or 0x3, though there's no check for backward compat reasons.
            br.BaseStream.Position += sizeof(ushort); // BLOBHEADER.wReserved
            int algId = br.ReadInt32();    // BLOBHEADER.aiKeyAlg
            if (algId != CALG_DSS_SIGN)
                throw new PlatformNotSupportedException();
        }

        private static void WriteKeyBlobHeader(DSAParameters dsaParameters, BinaryWriter bw, bool isPrivate, out bool isV3)
        {
            // Write out the BLOBHEADER (or PUBLICKEYSTRUC).
            isV3 = false;

            // If Y is present and this is a private key,
            // or Y and J are present and this is a public key, this should be a v3 blob.
            byte version = BLOBHEADER_CURRENT_BVERSION;
            if (((dsaParameters.Y != null) && isPrivate) ||
                ((dsaParameters.Y != null) && (dsaParameters.J != null)))
            {
                Debug.Assert(dsaParameters.Y.Length > 0);
                isV3 = true;
                version = 0x3;
            }
            bw.Write((byte)(isPrivate ? PRIVATEKEYBLOB : PUBLICKEYBLOB));  // BLOBHEADER.bType
            bw.Write((byte)version);                                       // BLOBHEADER.bVersion
            bw.Write((ushort)0);                                           // BLOBHEADER.wReserved
            bw.Write((int)CapiHelper.CALG_DSS_SIGN);                       // BLOBHEADER.aiKeyAlg
        }

        private static void ReadDSSSeed(DSAParameters dsaParameters, BinaryReader br, bool isV3)
        {
            bool hasSeed = false;
            int counter = br.ReadInt32();

            if (isV3)
            {
                hasSeed = (unchecked((uint)counter != 0xFFFFFFFF));
            }
            else
            {
                // This compares > 0 for backwards compatibility, and not just 0xFFFFFFFF
                hasSeed = (counter > 0); // Does not include 0xFFFFFFFF which is signed value of -1
            }

            if (hasSeed)
            {
                dsaParameters.Counter = counter;
                dsaParameters.Seed = br.ReadReversed(20);
            }
            else
            {
                dsaParameters.Counter = 0;
                dsaParameters.Seed = null;
                br.BaseStream.Position += 20; // Advance past seed[20]
            }
        }

        private static void WriteDSSSeed(DSAParameters dsaParameters, BinaryWriter bw)
        {
            if (dsaParameters.Seed == null || dsaParameters.Seed.Length == 0)
            {
                bw.Write(0xFFFFFFFF); // counter

                // seed[20] needs to be all 0xFF
                for (int i = 0; i < 20; i += sizeof(uint))
                {
                    bw.Write(0xFFFFFFFF);
                }
            }
            else
            {
                bw.Write((int)dsaParameters.Counter);
                bw.WriteReversed(dsaParameters.Seed);
            }
        }
    }
}
