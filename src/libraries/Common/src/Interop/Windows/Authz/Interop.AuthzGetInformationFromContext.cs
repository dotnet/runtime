// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Authz
    {
        [DllImport(Libraries.Authz, SetLastError = true)]
        internal static extern bool AuthzGetInformationFromContext(
            IntPtr hAuthzClientContext,
            int InfoClass,
            int BufferSize,
            out int pSizeRequired,
            IntPtr Buffer);
    }
}
