// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class OleAut32
    {
        // only using this to clear existing error info with null
        [LibraryImport(Interop.Libraries.OleAut32)]
        // TLS values are preserved between threads, need to check that we use this API to clear the error state only.
        internal static partial void SetErrorInfo(int dwReserved, IntPtr pIErrorInfo);
    }
}
