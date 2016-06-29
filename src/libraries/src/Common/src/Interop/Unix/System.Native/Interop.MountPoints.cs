// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private unsafe delegate void MountPointFound(byte* name);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetAllMountPoints", SetLastError = true)]
        private static extern int GetAllMountPoints(MountPointFound mpf);

        internal static List<string> GetAllMountPoints()
        {
            List<string> lst = new List<string>();
            unsafe
            {
                int result = GetAllMountPoints((byte* name) =>
                {
                    lst.Add(Marshal.PtrToStringAnsi((IntPtr)name));
                });
            }

            return lst;
        }
    }
}
