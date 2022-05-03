// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
#if NET7_0_OR_GREATER
using System.Runtime.InteropServices.Marshalling;
#endif
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class CryptUI
    {
#if NET7_0_OR_GREATER
        [NativeMarshalling(typeof(Native))]
#else
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
#endif
        internal struct CRYPTUI_VIEWCERTIFICATE_STRUCTW
        {
            internal uint dwSize;
            internal IntPtr hwndParent;
            internal uint dwFlags;
            internal string? szTitle;
            internal IntPtr pCertContext;
            internal IntPtr rgszPurposes;
            internal uint cPurposes;
            internal IntPtr pCryptProviderData;
            internal bool fpCryptProviderDataTrustedUsage;
            internal uint idxSigner;
            internal uint idxCert;
            internal bool fCounterSigner;
            internal uint idxCounterSigner;
            internal uint cStores;
            internal IntPtr rghStores;
            internal uint cPropSheetPages;
            internal IntPtr rgPropSheetPages;
            internal uint nStartPage;

#if NET7_0_OR_GREATER
            [CustomTypeMarshaller(typeof(CRYPTUI_VIEWCERTIFICATE_STRUCTW), Features = CustomTypeMarshallerFeatures.UnmanagedResources)]
            internal unsafe struct Native
            {
                private uint dwSize;
                private IntPtr hwndParent;
                private uint dwFlags;
                private IntPtr szTitle;
                private IntPtr pCertContext;
                private IntPtr rgszPurposes;
                private uint cPurposes;
                private IntPtr pCryptProviderData;
                private bool fpCryptProviderDataTrustedUsage;
                private uint idxSigner;
                private uint idxCert;
                private bool fCounterSigner;
                private uint idxCounterSigner;
                private uint cStores;
                private IntPtr rghStores;
                private uint cPropSheetPages;
                private IntPtr rgPropSheetPages;
                private uint nStartPage;

                public Native(CRYPTUI_VIEWCERTIFICATE_STRUCTW managed)
                {
                    dwSize = managed.dwSize;
                    hwndParent = managed.hwndParent;
                    dwFlags = managed.dwFlags;
                    szTitle = Marshal.StringToCoTaskMemUni(managed.szTitle);
                    pCertContext = managed.pCertContext;
                    rgszPurposes = managed.rgszPurposes;
                    cPurposes = managed.cPurposes;
                    pCryptProviderData = managed.pCryptProviderData;
                    fpCryptProviderDataTrustedUsage = managed.fpCryptProviderDataTrustedUsage;
                    idxSigner = managed.idxSigner;
                    idxCert = managed.idxCert;
                    fCounterSigner = managed.fCounterSigner;
                    idxCounterSigner = managed.idxCounterSigner;
                    cStores = managed.cStores;
                    rghStores = managed.rghStores;
                    cPropSheetPages = managed.cPropSheetPages;
                    rgPropSheetPages = managed.rgPropSheetPages;
                    nStartPage = managed.nStartPage;

                }

                public void FreeNative()
                {
                    Marshal.FreeCoTaskMem(szTitle);
                }

                public CRYPTUI_VIEWCERTIFICATE_STRUCTW ToManaged()
                {
                    return new()
                    {
                        dwSize = dwSize,
                        hwndParent = hwndParent,
                        dwFlags = dwFlags,
                        szTitle = Marshal.PtrToStringUni(szTitle),
                        pCertContext = pCertContext,
                        rgszPurposes = rgszPurposes,
                        cPurposes = cPurposes,
                        pCryptProviderData = pCryptProviderData,
                        fpCryptProviderDataTrustedUsage = fpCryptProviderDataTrustedUsage,
                        idxSigner = idxSigner,
                        idxCert = idxCert,
                        fCounterSigner = fCounterSigner,
                        idxCounterSigner = idxCounterSigner,
                        cStores = cStores,
                        rghStores = rghStores,
                        cPropSheetPages = cPropSheetPages,
                        rgPropSheetPages = rgPropSheetPages,
                        nStartPage = nStartPage
                    };
                }
            }
#endif
        }

#if NET7_0_OR_GREATER
        [NativeMarshalling(typeof(Native))]
#else
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
#endif
        internal struct CRYPTUI_SELECTCERTIFICATE_STRUCTW
        {
            internal uint dwSize;
            internal IntPtr hwndParent;
            internal uint dwFlags;
            internal string? szTitle;
            internal uint dwDontUseColumn;
            internal string? szDisplayString;
            internal IntPtr pFilterCallback;
            internal IntPtr pDisplayCallback;
            internal IntPtr pvCallbackData;
            internal uint cDisplayStores;
            internal IntPtr rghDisplayStores;
            internal uint cStores;
            internal IntPtr rghStores;
            internal uint cPropSheetPages;
            internal IntPtr rgPropSheetPages;
            internal IntPtr hSelectedCertStore;

#if NET7_0_OR_GREATER
            [CustomTypeMarshaller(typeof(CRYPTUI_SELECTCERTIFICATE_STRUCTW), Features = CustomTypeMarshallerFeatures.UnmanagedResources)]
            internal unsafe struct Native
            {
                private uint dwSize;
                private IntPtr hwndParent;
                private uint dwFlags;
                private IntPtr szTitle;
                private uint dwDontUseColumn;
                private IntPtr szDisplayString;
                private IntPtr pFilterCallback;
                private IntPtr pDisplayCallback;
                private IntPtr pvCallbackData;
                private uint cDisplayStores;
                private IntPtr rghDisplayStores;
                private uint cStores;
                private IntPtr rghStores;
                private uint cPropSheetPages;
                private IntPtr rgPropSheetPages;
                internal IntPtr hSelectedCertStore;

                public Native(CRYPTUI_SELECTCERTIFICATE_STRUCTW managed)
                {
                    dwSize = managed.dwSize;
                    hwndParent = managed.hwndParent;
                    dwFlags = managed.dwFlags;
                    szTitle = Marshal.StringToCoTaskMemUni(managed.szTitle);
                    dwDontUseColumn = managed.dwDontUseColumn;
                    szDisplayString = Marshal.StringToCoTaskMemUni(managed.szDisplayString);
                    pFilterCallback = managed.pFilterCallback;
                    pDisplayCallback = managed.pDisplayCallback;
                    pvCallbackData = managed.pvCallbackData;
                    cDisplayStores = managed.cDisplayStores;
                    rghDisplayStores = managed.rghDisplayStores;
                    cStores = managed.cStores;
                    rghStores = managed.rghStores;
                    cPropSheetPages = managed.cPropSheetPages;
                    rgPropSheetPages = managed.rgPropSheetPages;
                    hSelectedCertStore = managed.hSelectedCertStore;
                }

                public void FreeNative()
                {
                    Marshal.FreeCoTaskMem(szTitle);
                    Marshal.FreeCoTaskMem(szDisplayString);
                }

                public CRYPTUI_SELECTCERTIFICATE_STRUCTW ToManaged()
                {
                    return new()
                    {
                        dwSize = dwSize,
                        hwndParent = hwndParent,
                        dwFlags = dwFlags,
                        szTitle = Marshal.PtrToStringUni(szTitle),
                        dwDontUseColumn = dwDontUseColumn,
                        szDisplayString = Marshal.PtrToStringUni(szDisplayString),
                        pFilterCallback = pFilterCallback,
                        pDisplayCallback = pDisplayCallback,
                        pvCallbackData = pvCallbackData,
                        cDisplayStores = cDisplayStores,
                        rghDisplayStores = rghDisplayStores,
                        cStores = cStores,
                        rghStores = rghStores,
                        cPropSheetPages = cPropSheetPages,
                        rgPropSheetPages = rgPropSheetPages,
                        hSelectedCertStore = hSelectedCertStore
                    };
                }
            }
#endif
        }

        [LibraryImport(Interop.Libraries.CryptUI, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool CryptUIDlgViewCertificateW(
            in CRYPTUI_VIEWCERTIFICATE_STRUCTW ViewInfo, IntPtr pfPropertiesChanged);

        [LibraryImport(Interop.Libraries.CryptUI, SetLastError = true)]
        internal static partial SafeCertContextHandle CryptUIDlgSelectCertificateW(ref CRYPTUI_SELECTCERTIFICATE_STRUCTW csc);
    }
}
