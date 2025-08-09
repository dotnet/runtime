// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal static int PageSize { get => field == 0 ? (field = GetPageSize()) : field; }

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetPageSize")]
        private static partial int GetPageSize();
    }
}
