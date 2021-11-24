// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Cryptography.Pal.Native;
using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

using NTSTATUS = Interop.BCrypt.NTSTATUS;
using SafeBCryptKeyHandle = Microsoft.Win32.SafeHandles.SafeBCryptKeyHandle;

using static Interop.Crypt32;

namespace Internal.Cryptography.Pal
{
    /// <summary>
    /// A singleton class that encapsulates the native implementation of various X509 services. (Implementing this as a singleton makes it
    /// easier to split the class into abstract and implementation classes if desired.)
    /// </summary>
    internal sealed partial class X509Pal : IX509Pal
    {
        private const string BCRYPT_ECC_CURVE_NAME_PROPERTY = "ECCCurveName";
        private const string BCRYPT_ECC_PARAMETERS_PROPERTY = "ECCParameters";

        public ECDsa DecodeECDsaPublicKey(ICertificatePal? certificatePal)
        {
            if (certificatePal is CertificatePal pal)
            {
                return DecodeECPublicKey(
                    pal,
                    factory: cngKey => new ECDsaCng(cngKey),
                    import: (algorithm, ecParams) => algorithm.ImportParameters(ecParams));
            }

            throw new NotSupportedException(SR.NotSupported_KeyAlgorithm);
        }

        public ECDiffieHellman DecodeECDiffieHellmanPublicKey(ICertificatePal? certificatePal)
        {
            if (certificatePal is CertificatePal pal)
            {
                return DecodeECPublicKey(
                    pal,
                    factory: cngKey => new ECDiffieHellmanCng(cngKey),
                    import: (algorithm, ecParams) => algorithm.ImportParameters(ecParams),
                    importFlags: CryptImportPublicKeyInfoFlags.CRYPT_OID_INFO_PUBKEY_ENCRYPT_KEY_FLAG);
            }

            throw new NotSupportedException(SR.NotSupported_KeyAlgorithm);
        }

        public AsymmetricAlgorithm DecodePublicKey(Oid oid, byte[] encodedKeyValue, byte[] encodedParameters, ICertificatePal? certificatePal)
        {
            int algId = Interop.Crypt32.FindOidInfo(CryptOidInfoKeyType.CRYPT_OID_INFO_OID_KEY, oid.Value!, OidGroup.PublicKeyAlgorithm, fallBackToAllGroups: true).AlgId;
            switch (algId)
            {
                case AlgId.CALG_RSA_KEYX:
                case AlgId.CALG_RSA_SIGN:
                    {
                        byte[] keyBlob = DecodeKeyBlob(CryptDecodeObjectStructType.CNG_RSA_PUBLIC_KEY_BLOB, encodedKeyValue);
                        CngKey cngKey = CngKey.Import(keyBlob, CngKeyBlobFormat.GenericPublicBlob);
                        return new RSACng(cngKey);
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

        private static TAlgorithm DecodeECPublicKey<TAlgorithm>(
            CertificatePal certificatePal,
            Func<CngKey, TAlgorithm> factory,
            Action<TAlgorithm, ECParameters> import,
            CryptImportPublicKeyInfoFlags importFlags = CryptImportPublicKeyInfoFlags.NONE)
                where TAlgorithm : AsymmetricAlgorithm, new()
        {
            TAlgorithm key;

            using (SafeBCryptKeyHandle bCryptKeyHandle = ImportPublicKeyInfo(certificatePal.CertContext, importFlags))
            {
                CngKeyBlobFormat blobFormat;
                byte[] keyBlob;
                string? curveName = GetCurveName(bCryptKeyHandle);

                if (curveName == null)
                {
                    if (HasExplicitParameters(bCryptKeyHandle))
                    {
                        blobFormat = CngKeyBlobFormat.EccFullPublicBlob;
                    }
                    else
                    {
                        blobFormat = CngKeyBlobFormat.EccPublicBlob;
                    }

                    keyBlob = ExportKeyBlob(bCryptKeyHandle, blobFormat);
                    using (CngKey cngKey = CngKey.Import(keyBlob, blobFormat))
                    {
                        key = factory(cngKey);
                    }
                }
                else
                {
                    blobFormat = CngKeyBlobFormat.EccPublicBlob;
                    keyBlob = ExportKeyBlob(bCryptKeyHandle, blobFormat);
                    ECParameters ecparams = default;
                    ExportNamedCurveParameters(ref ecparams, keyBlob, false);
                    ecparams.Curve = ECCurve.CreateFromFriendlyName(curveName);
                    key = new TAlgorithm();
                    import(key, ecparams);
                }
            }

            return key;
        }

        private static SafeBCryptKeyHandle ImportPublicKeyInfo(SafeCertContextHandle certContext, CryptImportPublicKeyInfoFlags importFlags)
        {
            unsafe
            {
                SafeBCryptKeyHandle bCryptKeyHandle;
                bool mustRelease = false;
                certContext.DangerousAddRef(ref mustRelease);
                try
                {
                    unsafe
                    {
                        bool success = Interop.Crypt32.CryptImportPublicKeyInfoEx2(Interop.Crypt32.CertEncodingType.X509_ASN_ENCODING, &(certContext.CertContext->pCertInfo->SubjectPublicKeyInfo), importFlags, null, out bCryptKeyHandle);
                        if (!success)
                            throw Marshal.GetHRForLastWin32Error().ToCryptographicException();
                        return bCryptKeyHandle;
                    }
                }
                finally
                {
                    if (mustRelease)
                        certContext.DangerousRelease();
                }
            }
        }

        private static byte[] ExportKeyBlob(SafeBCryptKeyHandle bCryptKeyHandle, CngKeyBlobFormat blobFormat)
        {
            string blobFormatString = blobFormat.Format;

            int numBytesNeeded = 0;
            NTSTATUS ntStatus = Interop.BCrypt.BCryptExportKey(bCryptKeyHandle, IntPtr.Zero, blobFormatString, null, 0, out numBytesNeeded, 0);
            if (ntStatus != NTSTATUS.STATUS_SUCCESS)
                throw new CryptographicException(Interop.Kernel32.GetMessage((int)ntStatus));

            byte[] keyBlob = new byte[numBytesNeeded];
            ntStatus = Interop.BCrypt.BCryptExportKey(bCryptKeyHandle, IntPtr.Zero, blobFormatString, keyBlob, keyBlob.Length, out numBytesNeeded, 0);
            if (ntStatus != NTSTATUS.STATUS_SUCCESS)
                throw new CryptographicException(Interop.Kernel32.GetMessage((int)ntStatus));

            Array.Resize(ref keyBlob, numBytesNeeded);
            return keyBlob;
        }

        private static void ExportNamedCurveParameters(ref ECParameters ecParams, byte[] ecBlob, bool includePrivateParameters)
        {
            // We now have a buffer laid out as follows:
            //     BCRYPT_ECCKEY_BLOB   header
            //     byte[cbKey]          Q.X
            //     byte[cbKey]          Q.Y
            //     -- Private only --
            //     byte[cbKey]          D

            unsafe
            {
                Debug.Assert(ecBlob.Length >= sizeof(Interop.BCrypt.BCRYPT_ECCKEY_BLOB));

                fixed (byte* pEcBlob = &ecBlob[0])
                {
                    Interop.BCrypt.BCRYPT_ECCKEY_BLOB* pBcryptBlob = (Interop.BCrypt.BCRYPT_ECCKEY_BLOB*)pEcBlob;

                    int offset = sizeof(Interop.BCrypt.BCRYPT_ECCKEY_BLOB);

                    ecParams.Q = new ECPoint
                    {
                        X = Interop.BCrypt.Consume(ecBlob, ref offset, pBcryptBlob->cbKey),
                        Y = Interop.BCrypt.Consume(ecBlob, ref offset, pBcryptBlob->cbKey)
                    };

                    if (includePrivateParameters)
                    {
                        ecParams.D = Interop.BCrypt.Consume(ecBlob, ref offset, pBcryptBlob->cbKey);
                    }
                }
            }
        }

        private static byte[] DecodeKeyBlob(CryptDecodeObjectStructType lpszStructType, byte[] encodedKeyValue)
        {
            int cbDecoded = 0;
            if (!Interop.crypt32.CryptDecodeObject(CertEncodingType.All, lpszStructType, encodedKeyValue, encodedKeyValue.Length, CryptDecodeObjectFlags.None, null, ref cbDecoded))
                throw Marshal.GetLastWin32Error().ToCryptographicException();

            byte[] keyBlob = new byte[cbDecoded];
            if (!Interop.crypt32.CryptDecodeObject(CertEncodingType.All, lpszStructType, encodedKeyValue, encodedKeyValue.Length, CryptDecodeObjectFlags.None, keyBlob, ref cbDecoded))
                throw Marshal.GetLastWin32Error().ToCryptographicException();

            return keyBlob;
        }

        private static byte[] ConstructDSSPublicKeyCspBlob(byte[] encodedKeyValue, byte[] encodedParameters)
        {
            byte[] decodedKeyValue = DecodeDssKeyValue(encodedKeyValue)!;

            byte[] p, q, g;
            DecodeDssParameters(encodedParameters, out p, out q, out g);

            const byte PUBLICKEYBLOB = 0x6;
            const byte CUR_BLOB_VERSION = 2;

            int cbKey = p.Length;
            if (cbKey == 0)
                throw ErrorCode.NTE_BAD_PUBLIC_KEY.ToCryptographicException();

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
                throw ErrorCode.NTE_BAD_PUBLIC_KEY.ToCryptographicException();

            bw.Write(q);
            if (DSS_Q_LEN > cb)
                bw.Write(new byte[DSS_Q_LEN - cb]);

            // rgbG[cbKey]
            cb = g.Length;
            if (cb == 0 || cb > cbKey)
                throw ErrorCode.NTE_BAD_PUBLIC_KEY.ToCryptographicException();

            bw.Write(g);
            if (cbKey > cb)
                bw.Write(new byte[cbKey - cb]);

            // rgbY[cbKey]
            cb = decodedKeyValue.Length;
            if (cb == 0 || cb > cbKey)
                throw ErrorCode.NTE_BAD_PUBLIC_KEY.ToCryptographicException();

            bw.Write(decodedKeyValue);
            if (cbKey > cb)
                bw.Write(new byte[cbKey - cb]);

            // DSSSEED: set counter to 0xFFFFFFFF to indicate not available
            bw.Write((uint)0xFFFFFFFF);
            bw.Write(new byte[20]);

            return keyBlob.ToArray();
        }

        private static byte[]? DecodeDssKeyValue(byte[] encodedKeyValue)
        {
            unsafe
            {
                return encodedKeyValue.DecodeObject(
                    CryptDecodeObjectStructType.X509_DSS_PUBLICKEY,
                    static delegate (void* pvDecoded, int cbDecoded)
                    {
                        Debug.Assert(cbDecoded >= sizeof(DATA_BLOB));
                        DATA_BLOB* pBlob = (DATA_BLOB*)pvDecoded;
                        return pBlob->ToByteArray();
                    });
            }
        }

        private static void DecodeDssParameters(byte[] encodedParameters, out byte[] p, out byte[] q, out byte[] g)
        {
            unsafe
            {
                (p, q, g) = encodedParameters.DecodeObject(
                    CryptDecodeObjectStructType.X509_DSS_PARAMETERS,
                    delegate (void* pvDecoded, int cbDecoded)
                    {
                        Debug.Assert(cbDecoded >= sizeof(CERT_DSS_PARAMETERS));
                        CERT_DSS_PARAMETERS* pCertDssParameters = (CERT_DSS_PARAMETERS*)pvDecoded;
                        return (pCertDssParameters->p.ToByteArray(),
                                pCertDssParameters->q.ToByteArray(),
                                pCertDssParameters->g.ToByteArray());
                    });
            }
        }

        private static bool HasExplicitParameters(SafeBCryptKeyHandle bcryptHandle)
        {
            byte[]? explicitParams = GetProperty(bcryptHandle, BCRYPT_ECC_PARAMETERS_PROPERTY);
            return (explicitParams != null && explicitParams.Length > 0);
        }

        private static string? GetCurveName(SafeBCryptKeyHandle bcryptHandle)
        {
            return GetPropertyAsString(bcryptHandle, BCRYPT_ECC_CURVE_NAME_PROPERTY);
        }

        private static string? GetPropertyAsString(SafeBCryptKeyHandle cryptHandle, string propertyName)
        {
            Debug.Assert(!cryptHandle.IsInvalid);
            byte[]? value = GetProperty(cryptHandle, propertyName);
            if (value == null || value.Length == 0)
                return null;

            unsafe
            {
                fixed (byte* pValue = &value[0])
                {
                    string? valueAsString = Marshal.PtrToStringUni((IntPtr)pValue);
                    return valueAsString;
                }
            }
        }

        private static byte[]? GetProperty(SafeBCryptKeyHandle cryptHandle, string propertyName)
        {
            Debug.Assert(!cryptHandle.IsInvalid);
            unsafe
            {
                int numBytesNeeded;
                NTSTATUS errorCode = Interop.BCrypt.BCryptGetProperty(cryptHandle, propertyName, null, 0, out numBytesNeeded, 0);
                if (errorCode != NTSTATUS.STATUS_SUCCESS)
                    return null;

                byte[] propertyValue = new byte[numBytesNeeded];
                fixed (byte* pPropertyValue = propertyValue)
                {
                    errorCode = Interop.BCrypt.BCryptGetProperty(cryptHandle, propertyName, pPropertyValue, propertyValue.Length, out numBytesNeeded, 0);
                }
                if (errorCode != NTSTATUS.STATUS_SUCCESS)
                    return null;

                Array.Resize(ref propertyValue, numBytesNeeded);
                return propertyValue;
            }
        }
    }
}
