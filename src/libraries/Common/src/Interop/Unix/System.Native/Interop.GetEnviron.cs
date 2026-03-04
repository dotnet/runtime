// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal unsafe partial class Sys
    {
        [LibraryImport(Interop.Libraries.SystemNative, EntryPoint = "SystemNative_GetEnviron")]
        [RequiresUnsafe]
        internal static unsafe partial byte** GetEnviron();

        [LibraryImport(Interop.Libraries.SystemNative, EntryPoint = "SystemNative_FreeEnviron")]
        [RequiresUnsafe]
        internal static unsafe partial void FreeEnviron(byte** environ);
    }
}
