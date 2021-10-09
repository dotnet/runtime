// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Winspool
    {
        [DllImport(Libraries.Winspool, SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int DeviceCapabilities(string pDevice, string pPort, short fwCapabilities, IntPtr pOutput, IntPtr /*DEVMODE*/ pDevMode);

        [DllImport(Libraries.Winspool, SetLastError = true, CharSet = CharSet.Auto, BestFitMapping = false)]
        internal static extern int DocumentProperties(HandleRef hwnd, HandleRef hPrinter, string pDeviceName, IntPtr /*DEVMODE*/ pDevModeOutput, HandleRef /*DEVMODE*/ pDevModeInput, int fMode);

        [DllImport(Libraries.Winspool, SetLastError = true, CharSet = CharSet.Auto, BestFitMapping = false)]
        internal static extern int DocumentProperties(HandleRef hwnd, HandleRef hPrinter, string pDeviceName, IntPtr /*DEVMODE*/ pDevModeOutput, IntPtr /*DEVMODE*/ pDevModeInput, int fMode);

        [DllImport(Libraries.Winspool, SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern int EnumPrinters(int flags, string? name, int level, IntPtr pPrinterEnum/*buffer*/, int cbBuf, out int pcbNeeded, out int pcReturned);
    }
}