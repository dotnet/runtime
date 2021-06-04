// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
#if DLLIMPORTGENERATOR_ENABLED
        [GeneratedDllImport(Libraries.Advapi32, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "LookupPrivilegeValueW")]
        internal static partial bool LookupPrivilegeValue(
#else
        [DllImport(Libraries.Advapi32, CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false, EntryPoint = "LookupPrivilegeValueW")]
        internal static extern bool LookupPrivilegeValue(
#endif
            [MarshalAs(UnmanagedType.LPTStr)] string? lpSystemName, [MarshalAs(UnmanagedType.LPTStr)] string lpName, out LUID lpLuid);

        internal const string SeDebugPrivilege = "SeDebugPrivilege";
    }
}
