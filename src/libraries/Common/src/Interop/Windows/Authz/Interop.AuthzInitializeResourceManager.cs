// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Authz
    {
        [GeneratedDllImport(Libraries.Authz, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        internal static partial bool AuthzInitializeResourceManager(
            int flags,
            IntPtr pfnAccessCheck,
            IntPtr pfnComputeDynamicGroups,
            IntPtr pfnFreeDynamicGroups,
            string name,
            out IntPtr rm);

        [GeneratedDllImport(Libraries.Authz)]
        internal static partial bool AuthzFreeResourceManager(IntPtr rm);
    }
}
