// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates
{
    internal sealed partial class StorePal : IDisposable, IStorePal, IExportPal, ILoaderPal
    {
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
                    {
                        unsafe
                        {
                            Interop.Crypt32.DATA_BLOB dataBlob = new Interop.Crypt32.DATA_BLOB(IntPtr.Zero, 0);

                            if (!Interop.Crypt32.PFXExportCertStore(_certStore, ref dataBlob, password, Interop.Crypt32.PFXExportFlags.EXPORT_PRIVATE_KEYS | Interop.Crypt32.PFXExportFlags.REPORT_NOT_ABLE_TO_EXPORT_PRIVATE_KEY))
                                throw Marshal.GetHRForLastWin32Error().ToCryptographicException();

                            byte[] pbEncoded = new byte[dataBlob.cbData];
                            fixed (byte* ppbEncoded = pbEncoded)
                            {
                                dataBlob.pbData = new IntPtr(ppbEncoded);
                                if (!Interop.Crypt32.PFXExportCertStore(_certStore, ref dataBlob, password, Interop.Crypt32.PFXExportFlags.EXPORT_PRIVATE_KEYS | Interop.Crypt32.PFXExportFlags.REPORT_NOT_ABLE_TO_EXPORT_PRIVATE_KEY))
                                    throw Marshal.GetHRForLastWin32Error().ToCryptographicException();
                            }

                            return pbEncoded;
                        }
                    }

                case X509ContentType.SerializedStore:
                    return SaveToMemoryStore(Interop.Crypt32.CertStoreSaveAs.CERT_STORE_SAVE_AS_STORE);

                case X509ContentType.Pkcs7:
                    return SaveToMemoryStore(Interop.Crypt32.CertStoreSaveAs.CERT_STORE_SAVE_AS_PKCS7);

                default:
                    throw new CryptographicException(SR.Cryptography_X509_InvalidContentType);
            }
        }

        private byte[] SaveToMemoryStore(Interop.Crypt32.CertStoreSaveAs dwSaveAs)
        {
            unsafe
            {
                Interop.Crypt32.DATA_BLOB blob = new Interop.Crypt32.DATA_BLOB(IntPtr.Zero, 0);
                if (!Interop.Crypt32.CertSaveStore(_certStore, Interop.Crypt32.CertEncodingType.All, dwSaveAs, Interop.Crypt32.CertStoreSaveTo.CERT_STORE_SAVE_TO_MEMORY, ref blob, 0))
                    throw Marshal.GetLastWin32Error().ToCryptographicException();

                byte[] exportedData = new byte[blob.cbData];
                fixed (byte* pExportedData = exportedData)
                {
                    blob.pbData = new IntPtr(pExportedData);
                    if (!Interop.Crypt32.CertSaveStore(_certStore, Interop.Crypt32.CertEncodingType.All, dwSaveAs, Interop.Crypt32.CertStoreSaveTo.CERT_STORE_SAVE_TO_MEMORY, ref blob, 0))
                        throw Marshal.GetLastWin32Error().ToCryptographicException();
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
