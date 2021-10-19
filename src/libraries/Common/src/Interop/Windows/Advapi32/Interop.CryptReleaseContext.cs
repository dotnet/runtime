// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
#if DLLIMPORTGENERATOR_ENABLED
        [GeneratedDllImport(Libraries.Advapi32, SetLastError = true)]
        public static partial bool CryptReleaseContext(
#else
        [DllImport(Libraries.Advapi32, SetLastError = true)]
        public static extern bool CryptReleaseContext(
#endif
            IntPtr hProv,
            int dwFlags);
    }
}
