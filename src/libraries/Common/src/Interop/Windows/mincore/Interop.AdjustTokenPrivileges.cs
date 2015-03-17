// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.SecurityBase, SetLastError = true)]
        internal static extern bool AdjustTokenPrivileges(
            SafeTokenHandle TokenHandle,
            bool DisableAllPrivileges,
            TokenPrivileges NewState,
            int BufferLength,
            IntPtr PreviousState,
            IntPtr ReturnLength
        );

        [StructLayout(LayoutKind.Sequential)]
        internal class TokenPrivileges
        {
            internal int PrivilegeCount = 1;
            internal LUID Luid;
            internal int Attributes = 0;
        }
    }
}
