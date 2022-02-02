// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Winspool
    {
        [GeneratedDllImport(Libraries.Winspool, CharSet = CharSet.Auto, SetLastError = true)]
        internal static partial int DeviceCapabilities(string pDevice, string pPort, short fwCapabilities, IntPtr pOutput, IntPtr /*DEVMODE*/ pDevMode);

        [DllImport(Libraries.Winspool, SetLastError = true, CharSet = CharSet.Auto, BestFitMapping = false)]
        internal static extern int DocumentProperties(HandleRef hwnd, HandleRef hPrinter, string pDeviceName, IntPtr /*DEVMODE*/ pDevModeOutput, HandleRef /*DEVMODE*/ pDevModeInput, int fMode);

        [DllImport(Libraries.Winspool, SetLastError = true, CharSet = CharSet.Auto, BestFitMapping = false)]
        internal static extern int DocumentProperties(HandleRef hwnd, HandleRef hPrinter, string pDeviceName, IntPtr /*DEVMODE*/ pDevModeOutput, IntPtr /*DEVMODE*/ pDevModeInput, int fMode);

        [GeneratedDllImport(Libraries.Winspool, CharSet = CharSet.Auto, SetLastError = true)]
        internal static partial int EnumPrinters(int flags, string? name, int level, IntPtr pPrinterEnum/*buffer*/, int cbBuf, out int pcbNeeded, out int pcReturned);
    }
}