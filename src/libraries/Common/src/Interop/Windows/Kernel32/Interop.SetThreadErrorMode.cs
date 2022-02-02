// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [SuppressGCTransition]
        [GeneratedDllImport(Libraries.Kernel32, ExactSpelling = true, SetLastError = true)]
        internal static partial bool SetThreadErrorMode(
            uint dwNewMode,
            out uint lpOldMode);

        internal const uint SEM_FAILCRITICALERRORS = 1;
    }
}
