// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [DllImport(Libraries.Kernel32, CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
        internal static extern unsafe bool GetNamedPipeHandleStateW(
            SafePipeHandle hNamedPipe,
            uint* lpState,
            uint* lpCurInstances,
            uint* lpMaxCollectionCount,
            uint* lpCollectDataTimeout,
            char* lpUserName,
            uint nMaxUserNameSize);
    }
}
