// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
#if NET
using System.Runtime.InteropServices.Marshalling;
#endif

internal static partial class Interop
{
    internal static partial class Winspool
    {
        [LibraryImport(Libraries.Winspool, EntryPoint = "DocumentPropertiesW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int DocumentProperties(
#if NET
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hwnd,
#if NET
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hPrinter, string pDeviceName, IntPtr /*DEVMODE*/ pDevModeOutput,
#if NET
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef /*DEVMODE*/ pDevModeInput, int fMode);

        [LibraryImport(Libraries.Winspool, EntryPoint = "DocumentPropertiesW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int DocumentProperties(
#if NET
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hwnd,
#if NET
            [MarshalUsing(typeof(HandleRefMarshaller))]
#endif
            HandleRef hPrinter, string pDeviceName, IntPtr /*DEVMODE*/ pDevModeOutput, IntPtr /*DEVMODE*/ pDevModeInput, int fMode);
    }
}
