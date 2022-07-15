// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Runtime.CompilerServices;
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
            ArgumentNullException.ThrowIfNull(certificate);

            DisplayX509Certificate(certificate, IntPtr.Zero);
        }

        public static void DisplayCertificate(X509Certificate2 certificate, IntPtr hwndParent)
        {
            ArgumentNullException.ThrowIfNull(certificate);

            DisplayX509Certificate(certificate, hwndParent);
        }

        public static X509Certificate2Collection SelectFromCollection(X509Certificate2Collection certificates, string? title, string? message, X509SelectionFlag selectionFlag)
        {
            return SelectFromCollectionHelper(certificates, title, message, selectionFlag, IntPtr.Zero);
        }

        public static X509Certificate2Collection SelectFromCollection(X509Certificate2Collection certificates, string? title, string? message, X509SelectionFlag selectionFlag, IntPtr hwndParent)
        {
            return SelectFromCollectionHelper(certificates, title, message, selectionFlag, hwndParent);
        }

        private static unsafe void DisplayX509Certificate(X509Certificate2 certificate, IntPtr hwndParent)
        {
            using (SafeCertContextHandle safeCertContext = X509Utils.DuplicateCertificateContext(certificate))
            {
                if (safeCertContext.IsInvalid)
                    throw new CryptographicException(SR.Format(SR.Cryptography_InvalidHandle, nameof(safeCertContext)));

                int dwErrorCode = ERROR_SUCCESS;

                // Initialize view structure.
                Interop.CryptUI.CRYPTUI_VIEWCERTIFICATE_STRUCTW ViewInfo = default;
#if NET7_0_OR_GREATER
                ViewInfo.dwSize = (uint)sizeof(Interop.CryptUI.CRYPTUI_VIEWCERTIFICATE_STRUCTW.Marshaller.Native);
#else
                ViewInfo.dwSize = (uint)Marshal.SizeOf<Interop.CryptUI.CRYPTUI_VIEWCERTIFICATE_STRUCTW>();
#endif
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

        private static X509Certificate2Collection SelectFromCollectionHelper(X509Certificate2Collection certificates, string? title, string? message, X509SelectionFlag selectionFlag, IntPtr hwndParent)
        {
            ArgumentNullException.ThrowIfNull(certificates);

            if (selectionFlag < X509SelectionFlag.SingleSelection || selectionFlag > X509SelectionFlag.MultiSelection)
                throw new ArgumentException(SR.Format(SR.Enum_InvalidValue, nameof(selectionFlag)));

            using (SafeCertStoreHandle safeSourceStoreHandle = X509Utils.ExportToMemoryStore(certificates))
            using (SafeCertStoreHandle safeTargetStoreHandle = SelectFromStore(safeSourceStoreHandle, title, message, selectionFlag, hwndParent))
            {
                return X509Utils.GetCertificates(safeTargetStoreHandle);
            }
        }

        private static unsafe SafeCertStoreHandle SelectFromStore(SafeCertStoreHandle safeSourceStoreHandle, string? title, string? message, X509SelectionFlag selectionFlags, IntPtr hwndParent)
        {
            int dwErrorCode = ERROR_SUCCESS;

            SafeCertStoreHandle safeCertStoreHandle = Interop.Crypt32.CertOpenStore(
                (IntPtr)Interop.Crypt32.CERT_STORE_PROV_MEMORY,
                Interop.Crypt32.X509_ASN_ENCODING | Interop.Crypt32.PKCS_7_ASN_ENCODING,
                IntPtr.Zero,
                0,
                IntPtr.Zero);

            if (safeCertStoreHandle == null || safeCertStoreHandle.IsInvalid)
            {
                Exception e = new CryptographicException(Marshal.GetLastWin32Error());
                safeCertStoreHandle?.Dispose();
                throw e;
            }

            Interop.CryptUI.CRYPTUI_SELECTCERTIFICATE_STRUCTW csc = default;
            // Older versions of CRYPTUI do not check the size correctly,
            // so always force it to the oldest version of the structure.
#if NET7_0_OR_GREATER
            // Declare a local for Native to enable us to get the managed byte offset
            // without having a null check cause a failure.
            Interop.CryptUI.CRYPTUI_SELECTCERTIFICATE_STRUCTW.Marshaller.Native native;
            Unsafe.SkipInit(out native);
            csc.dwSize = (uint)Unsafe.ByteOffset(ref Unsafe.As<Interop.CryptUI.CRYPTUI_SELECTCERTIFICATE_STRUCTW.Marshaller.Native, byte>(ref native), ref Unsafe.As<IntPtr, byte>(ref native.hSelectedCertStore));
#else
            csc.dwSize = (uint)Marshal.OffsetOf(typeof(Interop.CryptUI.CRYPTUI_SELECTCERTIFICATE_STRUCTW), "hSelectedCertStore");
#endif
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

            SafeCertContextHandle safeCertContextHandle = Interop.CryptUI.CryptUIDlgSelectCertificateW(ref csc);

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
            {
                safeCertContextHandle?.Dispose();
                throw new CryptographicException(dwErrorCode);
            }

            return safeCertStoreHandle;
        }
    }
}
