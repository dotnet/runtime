// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Apple;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class AppleCrypto
    {
        private const string OidPbes2 = "1.2.840.113549.1.5.13";
        private const string OidPbkdf2 = "1.2.840.113549.1.5.12";
        private const string OidSha1 = "1.3.14.3.2.26";
        private const string OidTripleDesCbc = "1.2.840.113549.3.7";

        private static readonly SafeCreateHandle s_nullExportString = new SafeCreateHandle();

        private static readonly SafeCreateHandle s_emptyExportString =
            CoreFoundation.CFStringCreateWithCString("");

        [DllImport(Libraries.AppleCryptoNative)]
        private static extern int AppleCryptoNative_SecKeyExport(
            SafeSecKeyRefHandle key,
            int exportPrivate,
            SafeCreateHandle cfExportPassphrase,
            out SafeCFDataHandle cfDataOut,
            out int pOSStatus);

        internal static DerSequenceReader SecKeyExport(
            SafeSecKeyRefHandle key,
            bool exportPrivate)
        {
            // Apple requires all private keys to be exported encrypted, but since we're trying to export
            // as parsed structures we will need to decrypt it for the user.
            const string ExportPassword = "DotnetExportPassphrase";

            SafeCreateHandle exportPassword = exportPrivate
                ? CoreFoundation.CFStringCreateWithCString(ExportPassword)
                : s_nullExportString;

            int ret;
            SafeCFDataHandle cfData;
            int osStatus;

            try
            {
                ret = AppleCryptoNative_SecKeyExport(
                    key,
                    exportPrivate ? 1 : 0,
                    exportPassword,
                    out cfData,
                    out osStatus);
            }
            finally
            {
                if (exportPassword != s_nullExportString)
                {
                    exportPassword.Dispose();
                }
            }

            byte[] exportedData;

            using (cfData)
            {
                if (ret == 0)
                {
                    throw CreateExceptionForOSStatus(osStatus);
                }

                if (ret != 1)
                {
                    Debug.Fail($"AppleCryptoNative_SecKeyExport returned {ret}");
                    throw new CryptographicException();
                }

                exportedData = CoreFoundation.CFGetData(cfData);
            }

            DerSequenceReader reader = new DerSequenceReader(exportedData);

            if (!exportPrivate)
            {
                return reader;
            }

            byte tag = reader.PeekTag();

            // PKCS#8 defines two structures, PrivateKeyInfo, which starts with an integer,
            // and EncryptedPrivateKey, which starts with an encryption algorithm (DER sequence).
            if (tag == (byte)DerSequenceReader.DerTag.Integer)
            {
                return reader;
            }

            const byte ConstructedSequence =
                DerSequenceReader.ConstructedFlag | (byte)DerSequenceReader.DerTag.Sequence;

            if (tag == ConstructedSequence)
            {
                return ReadEncryptedPkcs8Blob(ExportPassword, reader);
            }

            Debug.Fail($"Data was neither PrivateKey or EncryptedPrivateKey: {tag:X2}");
            throw new CryptographicException();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA5350", Justification = "3DES identified from payload by OID")]
        private static DerSequenceReader ReadEncryptedPkcs8Blob(string passphrase, DerSequenceReader reader)
        {
            // EncryptedPrivateKeyInfo::= SEQUENCE {
            //    encryptionAlgorithm EncryptionAlgorithmIdentifier,
            //    encryptedData        EncryptedData }
            //
            // EncryptionAlgorithmIdentifier ::= AlgorithmIdentifier
            //
            // EncryptedData ::= OCTET STRING
            DerSequenceReader algorithmIdentifier = reader.ReadSequence();
            string algorithmOid = algorithmIdentifier.ReadOidAsString();

            // PBES2 (Password-Based Encryption Scheme 2)
            if (algorithmOid != OidPbes2)
            {
                Debug.Fail($"Expected PBES2 ({OidPbes2}), got {algorithmOid}");
                throw new CryptographicException();
            }

            // PBES2-params ::= SEQUENCE {
            //    keyDerivationFunc AlgorithmIdentifier { { PBES2 - KDFs} },
            //    encryptionScheme AlgorithmIdentifier { { PBES2 - Encs} }
            // }

            DerSequenceReader pbes2Params = algorithmIdentifier.ReadSequence();
            algorithmIdentifier = pbes2Params.ReadSequence();

            string kdfOid = algorithmIdentifier.ReadOidAsString();

            // PBKDF2 (Password-Based Key Derivation Function 2)
            if (kdfOid != OidPbkdf2)
            {
                Debug.Fail($"Expected PBKDF2 ({OidPbkdf2}), got {kdfOid}");
                throw new CryptographicException();
            }

            // PBKDF2-params ::= SEQUENCE {
            //   salt CHOICE {
            //     specified OCTET STRING,
            //     otherSource AlgorithmIdentifier { { PBKDF2 - SaltSources} }
            //   },
            //   iterationCount INTEGER (1..MAX),
            //   keyLength INTEGER(1..MAX) OPTIONAL,
            //   prf AlgorithmIdentifier { { PBKDF2 - PRFs} }  DEFAULT algid - hmacWithSHA1
            // }
            DerSequenceReader pbkdf2Params = algorithmIdentifier.ReadSequence();

            byte[] salt = pbkdf2Params.ReadOctetString();
            int iterCount = pbkdf2Params.ReadInteger();
            int keySize = -1;

            if (pbkdf2Params.HasData && pbkdf2Params.PeekTag() == (byte)DerSequenceReader.DerTag.Integer)
            {
                keySize = pbkdf2Params.ReadInteger();
            }

            if (pbkdf2Params.HasData)
            {
                string prfOid = pbkdf2Params.ReadOidAsString();

                // SHA-1 is the only hash algorithm our PBKDF2 supports.
                if (prfOid != OidSha1)
                {
                    Debug.Fail($"Expected SHA1 ({OidSha1}), got {prfOid}");
                    throw new CryptographicException();
                }
            }

            DerSequenceReader encryptionScheme = pbes2Params.ReadSequence();
            string cipherOid = encryptionScheme.ReadOidAsString();

            // DES-EDE3-CBC (TripleDES in CBC mode)
            if (cipherOid != OidTripleDesCbc)
            {
                Debug.Fail($"Expected DES-EDE3-CBC ({OidTripleDesCbc}), got {cipherOid}");
                throw new CryptographicException();
            }

            byte[] decrypted;

            using (TripleDES des3 = TripleDES.Create())
            {
                if (keySize == -1)
                {
                    foreach (KeySizes keySizes in des3.LegalKeySizes)
                    {
                        keySize = Math.Max(keySize, keySizes.MaxSize);
                    }
                }

                byte[] iv = encryptionScheme.ReadOctetString();

                using (Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(passphrase, salt, iterCount))
                using (ICryptoTransform decryptor = des3.CreateDecryptor(pbkdf2.GetBytes(keySize / 8), iv))
                {
                    byte[] encrypted = reader.ReadOctetString();
                    decrypted = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);
                }
            }

            DerSequenceReader pkcs8Reader = new DerSequenceReader(decrypted);
            return pkcs8Reader;
        }
    }
}
