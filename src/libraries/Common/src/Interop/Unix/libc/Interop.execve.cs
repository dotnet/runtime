// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libc
    {
        internal static unsafe int execve(string filename, string[] argv, string[] envp)
        {
            byte** argvPtr = null, envpPtr = null;
            try
            {
                AllocArray(argv, ref argvPtr);
                AllocArray(envp, ref envpPtr);
                return execve(filename, argvPtr, envpPtr);
            }
            finally
            {
                FreeArray(envpPtr, envp.Length);
                FreeArray(argvPtr, argv.Length);
            }
        }

        [DllImport(Libraries.Libc, SetLastError = true)]
        private static extern unsafe int execve(string filename, byte** argv, byte** envp);

        private static unsafe void AllocArray(string[] arr, ref byte** arrPtr)
        {
            // Allocate the unmanaged array to hold each string pointer
            arrPtr = (byte**)Marshal.AllocHGlobal(sizeof(IntPtr) * arr.Length);

            // Zero the memory so that if any of the individual string allocations fails,
            // we can loop through the array to free any that succeeded
            for (int i = 0; i < arr.Length; i++)
                arrPtr[i] = null;
            
            // Now copy each string to unmanaged memory referenced from the array
            for (int i = 0; i < arr.Length; i++)
                arrPtr[i] = (byte*)Marshal.StringToHGlobalUni(arr[i]);
        }

        private static unsafe void FreeArray(byte** arr, int length)
        {
            if (arr != null)
            {
                // Free each element of the array
                for (int i = 0; i < length; i++)
                {
                    if (arr[i] != null)
                    {
                        Marshal.FreeHGlobal((IntPtr)arr[i]);
                    }
                }

                // And then the array itself
                Marshal.FreeHGlobal((IntPtr)arr);
            }
        }
    }
}
