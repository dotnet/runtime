// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Authz
    {
        [LibraryImport(Libraries.Authz, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool AuthzGetInformationFromContext(
            IntPtr hAuthzClientContext,
            int InfoClass,
            int BufferSize,
            out int pSizeRequired,
            IntPtr Buffer);
    }
}
