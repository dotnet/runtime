// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Comdlg32
    {
#pragma warning disable DLLIMPORTGENANALYZER015 // Use 'GeneratedDllImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
        // TODO: [DllImportGenerator] Switch to use GeneratedDllImport once we support non-blittable structs.
        [DllImport(Libraries.Comdlg32, CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool PrintDlg([In, Out] PRINTDLG lppd);

        [DllImport(Libraries.Comdlg32, CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool PrintDlg([In, Out] PRINTDLGX86 lppd);
#pragma warning restore DLLIMPORTGENANALYZER015

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal sealed class PRINTDLG
        {
            internal int lStructSize;
            internal IntPtr hwndOwner;
            internal IntPtr hDevMode;
            internal IntPtr hDevNames;
            internal IntPtr hDC;
            internal int Flags;
            internal short nFromPage;
            internal short nToPage;
            internal short nMinPage;
            internal short nMaxPage;
            internal short nCopies;
            internal IntPtr hInstance;
            internal IntPtr lCustData;
            internal IntPtr lpfnPrintHook;
            internal IntPtr lpfnSetupHook;
            internal string? lpPrintTemplateName;
            internal string? lpSetupTemplateName;
            internal IntPtr hPrintTemplate;
            internal IntPtr hSetupTemplate;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 1)]
        internal sealed class PRINTDLGX86
        {
            internal int lStructSize;
            internal IntPtr hwndOwner;
            internal IntPtr hDevMode;
            internal IntPtr hDevNames;
            internal IntPtr hDC;
            internal int Flags;
            internal short nFromPage;
            internal short nToPage;
            internal short nMinPage;
            internal short nMaxPage;
            internal short nCopies;
            internal IntPtr hInstance;
            internal IntPtr lCustData;
            internal IntPtr lpfnPrintHook;
            internal IntPtr lpfnSetupHook;
            internal string? lpPrintTemplateName;
            internal string? lpSetupTemplateName;
            internal IntPtr hPrintTemplate;
            internal IntPtr hSetupTemplate;
        }
    }
}
