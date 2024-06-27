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
        [LibraryImport(Libraries.Winspool, EntryPoint = "EnumPrintersW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int EnumPrinters(int flags, string? name, int level, IntPtr pPrinterEnum/*buffer*/, int cbBuf, out int pcbNeeded, out int pcReturned);
    }
}
