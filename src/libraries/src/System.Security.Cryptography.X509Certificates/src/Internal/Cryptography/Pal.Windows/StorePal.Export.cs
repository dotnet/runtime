// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Internal.NativeCrypto;
using Internal.Cryptography;
using Internal.Cryptography.Pal.Native;


using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Internal.Cryptography.Pal
{
    internal sealed partial class StorePal : IDisposable, IStorePal
    {
        public byte[] Export(X509ContentType contentType, String password)
        {
            switch (contentType)
            {
                case X509ContentType.Cert:
                    {
                        SafeCertContextHandle pCertContext = null;
                        if (!Interop.crypt32.CertEnumCertificatesInStore(_certStore, ref pCertContext))
                            return null;
                        try
                        {
                            unsafe
                            {
                                byte[] rawData = new byte[pCertContext.CertContext->cbCertEncoded];
                                Marshal.Copy((IntPtr)(pCertContext.CertContext->pbCertEncoded), rawData, 0, rawData.Length);
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
                        SafeCertContextHandle pCertContext = null;
                        if (!Interop.crypt32.CertEnumCertificatesInStore(_certStore, ref pCertContext))
                            return null;

                        try
                        {
                            int cbEncoded = 0;
                            if (!Interop.crypt32.CertSerializeCertificateStoreElement(pCertContext, 0, null, ref cbEncoded))
                                throw Marshal.GetHRForLastWin32Error().ToCryptographicException();;

                            byte[] pbEncoded = new byte[cbEncoded];
                            if (!Interop.crypt32.CertSerializeCertificateStoreElement(pCertContext, 0, pbEncoded, ref cbEncoded))
                                throw Marshal.GetHRForLastWin32Error().ToCryptographicException();;

                            return pbEncoded;
                        }
                        finally
                        {
                            pCertContext.Dispose();
                        }
                    }

                case X509ContentType.Pkcs12:
                    {
                        unsafe
                        {
                            CRYPTOAPI_BLOB dataBlob = new CRYPTOAPI_BLOB(0, (byte*)null);

                            if (!Interop.crypt32.PFXExportCertStore(_certStore, ref dataBlob, password, PFXExportFlags.EXPORT_PRIVATE_KEYS | PFXExportFlags.REPORT_NOT_ABLE_TO_EXPORT_PRIVATE_KEY))
                                throw Marshal.GetHRForLastWin32Error().ToCryptographicException();;

                            byte[] pbEncoded = new byte[dataBlob.cbData];
                            fixed (byte* ppbEncoded = pbEncoded)
                            {
                                dataBlob.pbData = ppbEncoded;
                                if (!Interop.crypt32.PFXExportCertStore(_certStore, ref dataBlob, password, PFXExportFlags.EXPORT_PRIVATE_KEYS | PFXExportFlags.REPORT_NOT_ABLE_TO_EXPORT_PRIVATE_KEY))
                                    throw Marshal.GetHRForLastWin32Error().ToCryptographicException();;
                            }

                            return pbEncoded;
                        }
                    }

                case X509ContentType.SerializedStore:
                    return SaveToMemoryStore(CertStoreSaveAs.CERT_STORE_SAVE_AS_STORE);

                case X509ContentType.Pkcs7:
                    return SaveToMemoryStore(CertStoreSaveAs.CERT_STORE_SAVE_AS_PKCS7);

                default:
                    throw new CryptographicException(SR.Cryptography_X509_InvalidContentType);
            }
        }

        private byte[] SaveToMemoryStore(CertStoreSaveAs dwSaveAs)
        {
            unsafe
            {
                CRYPTOAPI_BLOB blob = new CRYPTOAPI_BLOB(0, null);
                if (!Interop.crypt32.CertSaveStore(_certStore, CertEncodingType.All, dwSaveAs, CertStoreSaveTo.CERT_STORE_SAVE_TO_MEMORY, ref blob, 0))
                    throw Marshal.GetLastWin32Error().ToCryptographicException();

                byte[] exportedData = new byte[blob.cbData];
                fixed (byte* pExportedData = exportedData)
                {
                    blob.pbData = pExportedData;
                    if (!Interop.crypt32.CertSaveStore(_certStore, CertEncodingType.All, dwSaveAs, CertStoreSaveTo.CERT_STORE_SAVE_TO_MEMORY, ref blob, 0))
                        throw Marshal.GetLastWin32Error().ToCryptographicException();
                }
                return exportedData;
            }
        }
    }
}
