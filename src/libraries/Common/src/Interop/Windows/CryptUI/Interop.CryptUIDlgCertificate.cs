// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class CryptUI
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal sealed class CRYPTUI_VIEWCERTIFICATE_STRUCTW
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
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal sealed class CRYPTUI_SELECTCERTIFICATE_STRUCTW
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
        }

#pragma warning disable DLLIMPORTGENANALYZER015 // Use 'GeneratedDllImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
        // TODO: [DllImportGenerator] Switch to use GeneratedDllImport once we support non-blittable types.
        [DllImport(Interop.Libraries.CryptUI, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        internal static extern bool CryptUIDlgViewCertificateW([MarshalAs(UnmanagedType.LPStruct)] CRYPTUI_VIEWCERTIFICATE_STRUCTW ViewInfo, IntPtr pfPropertiesChanged);

        [DllImport(Interop.Libraries.CryptUI, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        internal static extern SafeCertContextHandle CryptUIDlgSelectCertificateW([In, Out, MarshalAs(UnmanagedType.LPStruct)] CRYPTUI_SELECTCERTIFICATE_STRUCTW csc);
#pragma warning restore DLLIMPORTGENANALYZER015
    }
}
