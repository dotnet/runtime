// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Authz
    {
        [LibraryImport(Libraries.Authz, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool AuthzInitializeResourceManager(
            int flags,
            IntPtr pfnAccessCheck,
            IntPtr pfnComputeDynamicGroups,
            IntPtr pfnFreeDynamicGroups,
            string name,
            out IntPtr rm);

        [LibraryImport(Libraries.Authz)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool AuthzFreeResourceManager(IntPtr rm);
    }
}
