// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

using Internal.NativeCrypto;
using Internal.Cryptography;
using Internal.Cryptography.Pal.Native;

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography.Pal
{
    /// <summary>
    /// A singleton class that encapsulates the native implementation of various X509 services. (Implementing this as a singleton makes it
    /// easier to split the class into abstract and implementation classes if desired.)
    /// </summary>
    internal sealed partial class X509Pal : IX509Pal
    {
        public AsymmetricAlgorithm DecodePublicKey(Oid oid, byte[] encodedKeyValue, byte[] encodedParameters)
        {
            int algId = OidInfo.FindOidInfo(CryptOidInfoKeyType.CRYPT_OID_INFO_OID_KEY, oid.Value, OidGroup.PublicKeyAlgorithm, fallBackToAllGroups: true).AlgId;
            switch (algId)
            {
                case AlgId.CALG_RSA_KEYX:
                case AlgId.CALG_RSA_SIGN:
                    {
                        byte[] keyBlob = DecodeKeyBlob(CryptDecodeObjectStructType.RSA_CSP_PUBLICKEYBLOB, encodedKeyValue);
                        RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
                        rsa.ImportCspBlob(keyBlob);
                        return rsa;
                    }

                case AlgId.CALG_DSS_SIGN:
                    {
                        byte[] keyBlob = ConstructDSSPublicKeyCspBlob(encodedKeyValue, encodedParameters);
                        DSACryptoServiceProvider dsa = new DSACryptoServiceProvider();
                        dsa.ImportCspBlob(keyBlob);
                        return dsa;
                    }

                default:
                    throw new NotSupportedException(SR.NotSupported_KeyAlgorithm);
            }
        }

        private static byte[] DecodeKeyBlob(CryptDecodeObjectStructType lpszStructType, byte[] encodedKeyValue)
        {
            int cbDecoded = 0;
            if (!Interop.crypt32.CryptDecodeObject(CertEncodingType.All, lpszStructType, encodedKeyValue, encodedKeyValue.Length, CryptDecodeObjectFlags.None, null, ref cbDecoded))
                throw new CryptographicException(Marshal.GetLastWin32Error());

            byte[] keyBlob = new byte[cbDecoded];
            if (!Interop.crypt32.CryptDecodeObject(CertEncodingType.All, lpszStructType, encodedKeyValue, encodedKeyValue.Length, CryptDecodeObjectFlags.None, keyBlob, ref cbDecoded))
                throw new CryptographicException(Marshal.GetLastWin32Error());

            return keyBlob;
        }

        private static byte[] ConstructDSSPublicKeyCspBlob(byte[] encodedKeyValue, byte[] encodedParameters)
        {
            byte[] decodedKeyValue = DecodeDssKeyValue(encodedKeyValue);

            byte[] p, q, g;
            DecodeDssParameters(encodedParameters, out p, out q, out g);

            const byte PUBLICKEYBLOB = 0x6;
            const byte CUR_BLOB_VERSION = 2;

            int cbKey = p.Length;
            if (cbKey == 0)
                throw new CryptographicException(ErrorCode.NTE_BAD_PUBLIC_KEY);

            const int DSS_Q_LEN = 20;
            int capacity = 8 /* sizeof(CAPI.BLOBHEADER) */ + 8 /* sizeof(CAPI.DSSPUBKEY) */ +
                        cbKey + DSS_Q_LEN + cbKey + cbKey + 24 /* sizeof(CAPI.DSSSEED) */;

            MemoryStream keyBlob = new MemoryStream(capacity);
            BinaryWriter bw = new BinaryWriter(keyBlob);

            // PUBLICKEYSTRUC
            bw.Write((byte)PUBLICKEYBLOB); // pPubKeyStruc->bType = PUBLICKEYBLOB
            bw.Write((byte)CUR_BLOB_VERSION); // pPubKeyStruc->bVersion = CUR_BLOB_VERSION
            bw.Write((short)0); // pPubKeyStruc->reserved = 0;
            bw.Write((uint)AlgId.CALG_DSS_SIGN); // pPubKeyStruc->aiKeyAlg = CALG_DSS_SIGN;

            // DSSPUBKEY
            bw.Write((int)(PubKeyMagic.DSS_MAGIC)); // pCspPubKey->magic = DSS_MAGIC; We are constructing a DSS1 Csp blob.
            bw.Write((int)(cbKey * 8)); // pCspPubKey->bitlen = cbKey * 8;

            // rgbP[cbKey]
            bw.Write(p);

            // rgbQ[20]
            int cb = q.Length;
            if (cb == 0 || cb > DSS_Q_LEN)
                throw new CryptographicException(ErrorCode.NTE_BAD_PUBLIC_KEY);

            bw.Write(q);
            if (DSS_Q_LEN > cb)
                bw.Write(new byte[DSS_Q_LEN - cb]);

            // rgbG[cbKey]
            cb = g.Length;
            if (cb == 0 || cb > cbKey)
                throw new CryptographicException(ErrorCode.NTE_BAD_PUBLIC_KEY);

            bw.Write(g);
            if (cbKey > cb)
                bw.Write(new byte[cbKey - cb]);

            // rgbY[cbKey]
            cb = decodedKeyValue.Length;
            if (cb == 0 || cb > cbKey)
                throw new CryptographicException(ErrorCode.NTE_BAD_PUBLIC_KEY);

            bw.Write(decodedKeyValue);
            if (cbKey > cb)
                bw.Write(new byte[cbKey - cb]);

            // DSSSEED: set counter to 0xFFFFFFFF to indicate not available
            bw.Write((uint)0xFFFFFFFF);
            bw.Write(new byte[20]);

            return keyBlob.ToArray();
        }

        private static byte[] DecodeDssKeyValue(byte[] encodedKeyValue)
        {
            unsafe
            {
                byte[] decodedKeyValue = null;

                encodedKeyValue.DecodeObject(
                    CryptDecodeObjectStructType.X509_DSS_PUBLICKEY,
                    delegate (void* pvDecoded)
                    {
                        CRYPTOAPI_BLOB* pBlob = (CRYPTOAPI_BLOB*)pvDecoded;
                        decodedKeyValue = pBlob->ToByteArray();
                    }
                );

                return decodedKeyValue;
            }
        }

        private static void DecodeDssParameters(byte[] encodedParameters, out byte[] p, out byte[] q, out byte[] g)
        {
            byte[] pLocal = null;
            byte[] qLocal = null;
            byte[] gLocal = null;

            unsafe
            {
                encodedParameters.DecodeObject(
                    CryptDecodeObjectStructType.X509_DSS_PARAMETERS,
                    delegate (void* pvDecoded)
                    {
                        CERT_DSS_PARAMETERS* pCertDssParameters = (CERT_DSS_PARAMETERS*)pvDecoded;
                        pLocal = pCertDssParameters->p.ToByteArray();
                        qLocal = pCertDssParameters->q.ToByteArray();
                        gLocal = pCertDssParameters->g.ToByteArray();
                        return;
                    }
                );
            }

            p = pLocal;
            q = qLocal;
            g = gLocal;
            return;
        }
    }
}

