// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Security.Cryptography.X509Certificates
{
    public enum X509SelectionFlag
    {
        SingleSelection = 0x00,
        MultiSelection = 0x01
    }

    public sealed class X509Certificate2UI
    {
        internal const int ERROR_SUCCESS = 0;
        internal const int ERROR_CANCELLED = 1223;

        public static void DisplayCertificate(X509Certificate2 certificate)
        {
            if (certificate == null)
                throw new ArgumentNullException(nameof(certificate));
            DisplayX509Certificate(certificate, IntPtr.Zero);
        }

        public static void DisplayCertificate(X509Certificate2 certificate, IntPtr hwndParent)
        {
            if (certificate == null)
                throw new ArgumentNullException(nameof(certificate));
            DisplayX509Certificate(certificate, hwndParent);
        }

        public static X509Certificate2Collection SelectFromCollection(X509Certificate2Collection certificates, string title, string message, X509SelectionFlag selectionFlag)
        {
            return SelectFromCollectionHelper(certificates, title, message, selectionFlag, IntPtr.Zero);
        }

        public static X509Certificate2Collection SelectFromCollection(X509Certificate2Collection certificates, string title, string message, X509SelectionFlag selectionFlag, IntPtr hwndParent)
        {
            return SelectFromCollectionHelper(certificates, title, message, selectionFlag, hwndParent);
        }

        private static void DisplayX509Certificate(X509Certificate2 certificate, IntPtr hwndParent)
        {
            using (SafeCertContextHandle safeCertContext = X509Utils.DuplicateCertificateContext(certificate))
            {
                if (safeCertContext.IsInvalid)
                    throw new CryptographicException(SR.Format(SR.Cryptography_InvalidHandle, nameof(safeCertContext)));

                int dwErrorCode = ERROR_SUCCESS;

                // Initialize view structure.
                Interop.CryptUI.CRYPTUI_VIEWCERTIFICATE_STRUCTW ViewInfo = new Interop.CryptUI.CRYPTUI_VIEWCERTIFICATE_STRUCTW();
                ViewInfo.dwSize = (uint)Marshal.SizeOf(ViewInfo);
                ViewInfo.hwndParent = hwndParent;
                ViewInfo.dwFlags = 0;
                ViewInfo.szTitle = null;
                ViewInfo.pCertContext = safeCertContext.DangerousGetHandle();
                ViewInfo.rgszPurposes = IntPtr.Zero;
                ViewInfo.cPurposes = 0;
                ViewInfo.pCryptProviderData = IntPtr.Zero;
                ViewInfo.fpCryptProviderDataTrustedUsage = false;
                ViewInfo.idxSigner = 0;
                ViewInfo.idxCert = 0;
                ViewInfo.fCounterSigner = false;
                ViewInfo.idxCounterSigner = 0;
                ViewInfo.cStores = 0;
                ViewInfo.rghStores = IntPtr.Zero;
                ViewInfo.cPropSheetPages = 0;
                ViewInfo.rgPropSheetPages = IntPtr.Zero;
                ViewInfo.nStartPage = 0;

                // View the certificate
                if (!Interop.CryptUI.CryptUIDlgViewCertificateW(ViewInfo, IntPtr.Zero))
                    dwErrorCode = Marshal.GetLastWin32Error();

                // CryptUIDlgViewCertificateW returns ERROR_CANCELLED if the user closes
                // the window through the x button or by pressing CANCEL, so ignore this error code
                if (dwErrorCode != ERROR_SUCCESS && dwErrorCode != ERROR_CANCELLED)
                    throw new CryptographicException(dwErrorCode);
            }
        }

        private static X509Certificate2Collection SelectFromCollectionHelper(X509Certificate2Collection certificates, string title, string message, X509SelectionFlag selectionFlag, IntPtr hwndParent)
        {
            if (certificates == null)
                throw new ArgumentNullException(nameof(certificates));
            if (selectionFlag < X509SelectionFlag.SingleSelection || selectionFlag > X509SelectionFlag.MultiSelection)
                throw new ArgumentException(SR.Format(SR.Enum_InvalidValue, nameof(selectionFlag)));

            using (SafeCertStoreHandle safeSourceStoreHandle = X509Utils.ExportToMemoryStore(certificates))
            using (SafeCertStoreHandle safeTargetStoreHandle = SelectFromStore(safeSourceStoreHandle, title, message, selectionFlag, hwndParent))
            {
                return X509Utils.GetCertificates(safeTargetStoreHandle);
            }
        }

        private static unsafe SafeCertStoreHandle SelectFromStore(SafeCertStoreHandle safeSourceStoreHandle, string title, string message, X509SelectionFlag selectionFlags, IntPtr hwndParent)
        {
            int dwErrorCode = ERROR_SUCCESS;

            SafeCertStoreHandle safeCertStoreHandle = Interop.Crypt32.CertOpenStore(
                (IntPtr)Interop.Crypt32.CERT_STORE_PROV_MEMORY,
                Interop.Crypt32.X509_ASN_ENCODING | Interop.Crypt32.PKCS_7_ASN_ENCODING,
                IntPtr.Zero,
                0,
                null);

            if (safeCertStoreHandle == null || safeCertStoreHandle.IsInvalid)
                throw new CryptographicException(Marshal.GetLastWin32Error());

            Interop.CryptUI.CRYPTUI_SELECTCERTIFICATE_STRUCTW csc = new Interop.CryptUI.CRYPTUI_SELECTCERTIFICATE_STRUCTW();
            // Older versions of CRYPTUI do not check the size correctly,
            // so always force it to the oldest version of the structure.
            csc.dwSize = (uint)Marshal.OffsetOf(typeof(Interop.CryptUI.CRYPTUI_SELECTCERTIFICATE_STRUCTW), "hSelectedCertStore");
            csc.hwndParent = hwndParent;
            csc.dwFlags = (uint)selectionFlags;
            csc.szTitle = title;
            csc.dwDontUseColumn = 0;
            csc.szDisplayString = message;
            csc.pFilterCallback = IntPtr.Zero;
            csc.pDisplayCallback = IntPtr.Zero;
            csc.pvCallbackData = IntPtr.Zero;
            csc.cDisplayStores = 1;
            IntPtr hSourceCertStore = safeSourceStoreHandle.DangerousGetHandle();
            csc.rghDisplayStores = new IntPtr(&hSourceCertStore);
            csc.cStores = 0;
            csc.rghStores = IntPtr.Zero;
            csc.cPropSheetPages = 0;
            csc.rgPropSheetPages = IntPtr.Zero;
            csc.hSelectedCertStore = safeCertStoreHandle.DangerousGetHandle();

            SafeCertContextHandle safeCertContextHandle = Interop.CryptUI.CryptUIDlgSelectCertificateW(csc);

            if (safeCertContextHandle != null && !safeCertContextHandle.IsInvalid)
            {
                // Single select, so add it to our hCertStore
                SafeCertContextHandle ppStoreContext = SafeCertContextHandle.InvalidHandle;
                if (!Interop.Crypt32.CertAddCertificateLinkToStore(safeCertStoreHandle,
                                                        safeCertContextHandle,
                                                        Interop.Crypt32.CERT_STORE_ADD_ALWAYS,
                                                        ppStoreContext))
                {
                    dwErrorCode = Marshal.GetLastWin32Error();
                }
            }

            if (dwErrorCode != ERROR_SUCCESS)
                throw new CryptographicException(dwErrorCode);

            return safeCertStoreHandle;
        }
    }
}
