// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_InterfaceNameToIndex", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
        public static partial uint InterfaceNameToIndex(string name);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_InterfaceNameToIndex", SetLastError = true)]
        public static partial uint InterfaceNameToIndex(ReadOnlySpan<byte> utf8NullTerminatedName);
    }
}
