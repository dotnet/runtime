// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Comdlg32
    {
        [LibraryImport(Libraries.Comdlg32, EntryPoint="PrintDlgW", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool PrintDlg(ref PRINTDLG lppd);

        [LibraryImport(Libraries.Comdlg32, EntryPoint="PrintDlgW", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool PrintDlg(ref PRINTDLGX86 lppd);

        [StructLayout(LayoutKind.Sequential)]
        internal struct PRINTDLG
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
            internal IntPtr lpPrintTemplateName;
            internal IntPtr lpSetupTemplateName;
            internal IntPtr hPrintTemplate;
            internal IntPtr hSetupTemplate;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct PRINTDLGX86
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
            internal IntPtr lpPrintTemplateName;
            internal IntPtr lpSetupTemplateName;
            internal IntPtr hPrintTemplate;
            internal IntPtr hSetupTemplate;
        }
    }
}
