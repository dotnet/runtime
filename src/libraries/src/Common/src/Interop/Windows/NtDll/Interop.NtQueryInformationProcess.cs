// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class NtDll
    {
        [DllImport(Libraries.NtDll)]
        internal static extern int NtQueryInformationProcess(SafeProcessHandle processHandle, int query, NtProcessBasicInfo info, int size, int[] returnedSize);

        [StructLayout(LayoutKind.Sequential)]
        internal class NtProcessBasicInfo
        {
            internal int ExitStatus = 0;
            internal IntPtr PebBaseAddress = (IntPtr)0;
            internal IntPtr AffinityMask = (IntPtr)0;
            internal int BasePriority = 0;
            internal IntPtr UniqueProcessId = (IntPtr)0;
            internal IntPtr InheritedFromUniqueProcessId = (IntPtr)0;
        }

        internal const int NtQueryProcessBasicInfo = 0;
    }
}
