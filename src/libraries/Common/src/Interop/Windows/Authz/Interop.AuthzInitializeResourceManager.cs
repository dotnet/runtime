// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Authz
    {
        [DllImport(Libraries.Authz, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        internal static extern bool AuthzInitializeResourceManager(
            int flags,
            IntPtr pfnAccessCheck,
            IntPtr pfnComputeDynamicGroups,
            IntPtr pfnFreeDynamicGroups,
            string name,
            out IntPtr rm);

        [DllImport(Libraries.Authz)]
        internal static extern bool AuthzFreeResourceManager(IntPtr rm);
    }
}
