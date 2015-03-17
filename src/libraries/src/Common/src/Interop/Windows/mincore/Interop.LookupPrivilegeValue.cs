// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.SecurityLsa, CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true, BestFitMapping = false)]
        internal static extern bool LookupPrivilegeValue([MarshalAs(UnmanagedType.LPTStr)] string lpSystemName, [MarshalAs(UnmanagedType.LPTStr)] string lpName, out LUID lpLuid);

        internal const string SeDebugPrivilege = "SeDebugPrivilege";
    }
}
