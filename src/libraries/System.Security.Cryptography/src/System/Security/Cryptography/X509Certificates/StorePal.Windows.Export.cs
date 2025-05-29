// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Pkcs;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates
{
    internal sealed partial class StorePal : IDisposable, IStorePal, IExportPal, ILoaderPal
    {
        // Windows 10 1709 / Windows Server 2019.
        private static readonly bool s_supportsAes256Sha256 = OperatingSystem.IsWindowsVersionAtLeast(10, 0, 16299);

        public void MoveTo(X509Certificate2Collection collection)
        {
            CopyTo(collection);

            // ILoaderPal expects to only be called once.
            Dispose();
        }

        public byte[]? Export(X509ContentType contentType, SafePasswordHandle password)
        {
            Debug.Assert(password != null);
            switch (contentType)
            {
                case X509ContentType.Cert:
                    {
                        SafeCertContextHandle? pCertContext = null;
                        if (!Interop.crypt32.CertEnumCertificatesInStore(_certStore, ref pCertContext))
                            return null;
                        try
                        {
                            unsafe
                            {
                                // We can use the DangerousCertContext because the safehandle never leaves this method
                                // and can't be disposed of by another thread.
                                byte[] rawData = new byte[pCertContext.DangerousCertContext->cbCertEncoded];
                                Marshal.Copy((IntPtr)(pCertContext.DangerousCertContext->pbCertEncoded), rawData, 0, rawData.Length);
                                GC.KeepAlive(pCertContext);
                                return rawData;
                            }
                        }
                        finally
                        {
                            pCertContext.Dispose();
                        }
                    }

                case X509ContentType.SerializedCert:
                    {
                        SafeCertContextHandle? pCertContext = null;
                        if (!Interop.crypt32.CertEnumCertificatesInStore(_certStore, ref pCertContext))
                            return null;

                        try
                        {
                            int cbEncoded = 0;
                            if (!Interop.Crypt32.CertSerializeCertificateStoreElement(pCertContext, 0, null, ref cbEncoded))
                                throw Marshal.GetHRForLastWin32Error().ToCryptographicException();

                            byte[] pbEncoded = new byte[cbEncoded];
                            if (!Interop.Crypt32.CertSerializeCertificateStoreElement(pCertContext, 0, pbEncoded, ref cbEncoded))
                                throw Marshal.GetHRForLastWin32Error().ToCryptographicException();

                            return pbEncoded;
                        }
                        finally
                        {
                            pCertContext.Dispose();
                        }
                    }

                case X509ContentType.Pkcs12:
                    return ExportPkcs12Core(null, password);

                case X509ContentType.SerializedStore:
                    return SaveToMemoryStore(Interop.Crypt32.CertStoreSaveAs.CERT_STORE_SAVE_AS_STORE);

                case X509ContentType.Pkcs7:
                    return SaveToMemoryStore(Interop.Crypt32.CertStoreSaveAs.CERT_STORE_SAVE_AS_PKCS7);

                default:
                    throw new CryptographicException(SR.Cryptography_X509_InvalidContentType);
            }
        }

        public byte[] ExportPkcs12(Pkcs12ExportPbeParameters exportParameters, SafePasswordHandle password)
        {
            return ExportPkcs12Core(exportParameters, password);
        }

        public byte[] ExportPkcs12(PbeParameters exportParameters, SafePasswordHandle password)
        {
            byte[] exported = ExportPkcs12Core(null, password);
            return ReEncryptAndSealPkcs12(exported, password, exportParameters);
        }

        private unsafe byte[] ExportPkcs12Core(Pkcs12ExportPbeParameters? exportParameters, SafePasswordHandle password)
        {
            Interop.Crypt32.DATA_BLOB dataBlob = new Interop.Crypt32.DATA_BLOB(IntPtr.Zero, 0);
            Interop.Crypt32.PFXExportFlags flags =
                Interop.Crypt32.PFXExportFlags.EXPORT_PRIVATE_KEYS |
                Interop.Crypt32.PFXExportFlags.REPORT_NOT_ABLE_TO_EXPORT_PRIVATE_KEY;

            Interop.Crypt32.PKCS12_PBES2_EXPORT_PARAMS* exportParams = null;
            PbeParameters? reEncodeParameters = null;

            if (exportParameters is Pkcs12ExportPbeParameters.Pbes2Aes256Sha256 or Pkcs12ExportPbeParameters.Default)
            {
                if (s_supportsAes256Sha256)
                {
                    flags |= Interop.Crypt32.PFXExportFlags.PKCS12_EXPORT_PBES2_PARAMS;
                    // PKCS12_PBES2_ALG_AES256_SHA256
                    char* algStr = stackalloc char[] { 'A', 'E', 'S', '2', '5', '6', '-', 'S', 'H', 'A', '2', '5', '6', '\0' };
                    Interop.Crypt32.PKCS12_PBES2_EXPORT_PARAMS p = new()
                    {
                        dwSize = (uint)Marshal.SizeOf<Interop.Crypt32.PKCS12_PBES2_EXPORT_PARAMS>(),
                        hNcryptDescriptor = 0,
                        pwszPbes2Alg = algStr,
                    };
                    exportParams = &p;
                }
                else
                {
                    reEncodeParameters = Helpers.WindowsAesPbe;
                }
            }
            else if (exportParameters == Pkcs12ExportPbeParameters.Pkcs12TripleDesSha1)
            {
                // Older Windows is not guaranteed to export in 3DES. If 3DES was asked for explicitly, then re-encode
                // it as 3DES.
                reEncodeParameters = Helpers.Windows3desPbe;
            }
            else
            {
                Debug.Assert(exportParameters is null);
            }

            if (!Interop.Crypt32.PFXExportCertStoreEx(_certStore, ref dataBlob, password, exportParams, flags))
            {
                throw Marshal.GetHRForLastWin32Error().ToCryptographicException();
            }

            byte[] pbEncoded = new byte[dataBlob.cbData];

            fixed (byte* ppbEncoded = pbEncoded)
            {
                dataBlob.pbData = new IntPtr(ppbEncoded);

                if (!Interop.Crypt32.PFXExportCertStoreEx(_certStore, ref dataBlob, password, exportParams, flags))
                {
                    throw Marshal.GetHRForLastWin32Error().ToCryptographicException();
                }
            }

            return reEncodeParameters is not null ?
                ReEncryptAndSealPkcs12(pbEncoded, password, reEncodeParameters) :
                pbEncoded;
        }

        private static byte[] ReEncryptAndSealPkcs12(byte[] pkcs12, SafePasswordHandle password, PbeParameters newPbeParameters)
        {
            bool addedRef = false;

            try
            {
                password.DangerousAddRef(ref addedRef);
                ReadOnlySpan<char> passwordChars = password.DangerousGetSpan();
                Pkcs12Info info = Pkcs12Info.Decode(pkcs12, out _, skipCopy: true);

                if (!info.VerifyMac(passwordChars))
                {
                    Debug.Fail("Mac validation failed.");
                    throw new CryptographicException();
                }

                Pkcs12Builder builder = new();

                foreach (Pkcs12SafeContents safeContents in info.AuthenticatedSafe)
                {
                    bool passwordProtectedSafe;

                    switch (safeContents.ConfidentialityMode)
                    {
                        case Pkcs12ConfidentialityMode.Password:
                            safeContents.Decrypt(passwordChars);
                            passwordProtectedSafe = true;
                            break;
                        case Pkcs12ConfidentialityMode.None:
                            passwordProtectedSafe = false;
                            break;
                        default:
                            // Covers Unknown and PublicKey, neither of which Windows produces.
                            Debug.Fail($"Unknown confidentiality mode {safeContents.ConfidentialityMode}.");
                            throw new CryptographicException();
                    }

                    Pkcs12SafeContents newSafeContents = new();

                    foreach (Pkcs12SafeBag safeBag in safeContents.GetBags())
                    {
                        if (safeBag is Pkcs12ShroudedKeyBag shroudedKeyBag)
                        {
                            // Shrouded keys need to be re-shrouded.
                            Pkcs8PrivateKeyInfo keyInfo = Pkcs8PrivateKeyInfo.DecryptAndDecode(
                                passwordChars,
                                shroudedKeyBag.EncryptedPkcs8PrivateKey,
                                out _);

                            byte[] encrypted = keyInfo.Encrypt(passwordChars, newPbeParameters);
                            Pkcs12ShroudedKeyBag newShroudedKeyBag = new(encrypted, skipCopy: true);

                            // Since the attribute collection is not written to, share the whole collection rather than
                            // dup them.
                            newShroudedKeyBag.Attributes = shroudedKeyBag.Attributes;
                            newSafeContents.AddSafeBag(newShroudedKeyBag);
                        }
                        else
                        {
                            // Other bag types get passed through as-is.
                            newSafeContents.AddSafeBag(safeBag);
                        }
                    }

                    if (passwordProtectedSafe)
                    {
                        builder.AddSafeContentsEncrypted(newSafeContents, passwordChars, newPbeParameters);
                    }
                    else
                    {
                        builder.AddSafeContentsUnencrypted(newSafeContents);
                    }
                }

                builder.SealWithMac(passwordChars, newPbeParameters.HashAlgorithm, newPbeParameters.IterationCount);
                return builder.Encode();
            }
            finally
            {
                if (addedRef)
                {
                    password.DangerousRelease();
                }
            }
        }

        private byte[] SaveToMemoryStore(Interop.Crypt32.CertStoreSaveAs dwSaveAs)
        {
            unsafe
            {
                Interop.Crypt32.DATA_BLOB blob = new Interop.Crypt32.DATA_BLOB(IntPtr.Zero, 0);
                if (!Interop.Crypt32.CertSaveStore(_certStore, Interop.Crypt32.CertEncodingType.All, dwSaveAs, Interop.Crypt32.CertStoreSaveTo.CERT_STORE_SAVE_TO_MEMORY, ref blob, 0))
                    throw Marshal.GetLastPInvokeError().ToCryptographicException();

                byte[] exportedData = new byte[blob.cbData];
                fixed (byte* pExportedData = exportedData)
                {
                    blob.pbData = new IntPtr(pExportedData);
                    if (!Interop.Crypt32.CertSaveStore(_certStore, Interop.Crypt32.CertEncodingType.All, dwSaveAs, Interop.Crypt32.CertStoreSaveTo.CERT_STORE_SAVE_TO_MEMORY, ref blob, 0))
                        throw Marshal.GetLastPInvokeError().ToCryptographicException();
                }

                // When calling CertSaveStore to get the initial length, it returns a cbData that is big enough but
                // not exactly the right size, at least in the case of PKCS7. So we need to right-size it once we
                // know exactly how much was written.
                if (exportedData.Length != blob.cbData)
                {
                    return exportedData[0..(int)blob.cbData];
                }

                // If CertSaveStore calculation got the size right on the first try, then return the buffer as-is.
                return exportedData;
            }
        }
    }
}
