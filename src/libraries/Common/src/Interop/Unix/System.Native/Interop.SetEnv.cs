// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [LibraryImport(Interop.Libraries.SystemNative, StringMarshalling = StringMarshalling.Utf8, EntryPoint = "SystemNative_SetEnv")]
        internal static partial int SetEnv(string name, string value);

        [LibraryImport(Interop.Libraries.SystemNative, StringMarshalling = StringMarshalling.Utf8, EntryPoint = "SystemNative_UnsetEnv")]
        internal static partial int UnsetEnv(string name);
    }
}
