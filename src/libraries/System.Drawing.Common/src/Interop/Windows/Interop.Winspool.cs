// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
#if NET7_0_OR_GREATER
using System.Runtime.InteropServices.GeneratedMarshalling;
#endif

internal static partial class Interop
{
    internal static partial class Winspool
    {
        [GeneratedDllImport(Libraries.Winspool, EntryPoint = "DeviceCapabilitiesW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static partial int DeviceCapabilities(string pDevice, string pPort, short fwCapabilities, IntPtr pOutput, IntPtr /*DEVMODE*/ pDevMode);

        [GeneratedDllImport(Libraries.Winspool, EntryPoint = "DocumentPropertiesW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static partial int DocumentProperties(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hwnd,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hPrinter, string pDeviceName, IntPtr /*DEVMODE*/ pDevModeOutput,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef /*DEVMODE*/ pDevModeInput, int fMode);

        [GeneratedDllImport(Libraries.Winspool, EntryPoint = "DocumentPropertiesW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static partial int DocumentProperties(
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hwnd,
#if NET7_0_OR_GREATER
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hPrinter, string pDeviceName, IntPtr /*DEVMODE*/ pDevModeOutput, IntPtr /*DEVMODE*/ pDevModeInput, int fMode);

        [GeneratedDllImport(Libraries.Winspool, CharSet = CharSet.Auto, SetLastError = true)]
        internal static partial int EnumPrinters(int flags, string? name, int level, IntPtr pPrinterEnum/*buffer*/, int cbBuf, out int pcbNeeded, out int pcReturned);
    }
}
