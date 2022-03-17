// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private unsafe delegate void MountPointFound(byte* name);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetAllMountPoints", SetLastError = true)]
        private static partial int GetAllMountPoints(MountPointFound mpf);

        internal static string[] GetAllMountPoints()
        {
            int count = 0;
            var found = new string[4];

            unsafe
            {
                GetAllMountPoints((byte* name) =>
                {
                    if (count == found.Length)
                    {
                        Array.Resize(ref found, count * 2);
                    }
                    found[count++] = Marshal.PtrToStringAnsi((IntPtr)name)!;
                });
            }

            Array.Resize(ref found, count);
            return found;
        }
    }
}
