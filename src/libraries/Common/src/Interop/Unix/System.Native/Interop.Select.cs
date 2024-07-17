// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_Select")]
        internal static unsafe partial Error Select(Span<int> readFDs, int readFDsLength, Span<int> writeFDs, int writeFDsLength,  Span<int> checkError, int checkErrorLength, int timeout, int maxFd, out int triggered);
    }
}
