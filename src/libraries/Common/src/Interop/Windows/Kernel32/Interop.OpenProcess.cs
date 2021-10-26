// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
#if DLLIMPORTGENERATOR_ENABLED
        [GeneratedDllImport(Libraries.Kernel32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static partial SafeProcessHandle OpenProcess(
#else
        [DllImport(Libraries.Kernel32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern SafeProcessHandle OpenProcess(
#endif
            int access, bool inherit, int processId);
    }
}
