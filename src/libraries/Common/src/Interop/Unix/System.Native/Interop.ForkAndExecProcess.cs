// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal static unsafe void ForkAndExecProcess(
            string filename, string[] argv, string[] envp, string cwd,
            bool redirectStdin, bool redirectStdout, bool redirectStderr,
            out int lpChildPid, out int stdinFd, out int stdoutFd, out int stderrFd, bool shouldThrow = true)
        {
            byte** argvPtr = null, envpPtr = null;
            int result = -1;
            try
            {
                AllocNullTerminatedArray(argv, ref argvPtr);
                AllocNullTerminatedArray(envp, ref envpPtr);
                result = ForkAndExecProcess(
                    filename, argvPtr, envpPtr, cwd,
                    redirectStdin ? 1 : 0, redirectStdout ? 1 : 0, redirectStderr ? 1 :0,
                    out lpChildPid, out stdinFd, out stdoutFd, out stderrFd);
                if (result != 0)
                {
                    // Normally we'd simply make this method return the result of the native
                    // call and allow the caller to use GetLastWin32Error.  However, we need
                    // to free the native arrays after calling the function, and doing so
                    // stomps on the runtime's captured last error.  So we need to access the
                    // error here, and without SetLastWin32Error available, we can't propagate
                    // the error to the caller via the normal GetLastWin32Error mechanism.  We could
                    // return 0 on success or the GetLastWin32Error value on failure, but that's
                    // technically ambiguous, in the case of a failure with a 0 errno.  Simplest
                    // solution then is just to throw here the same exception the Process caller
                    // would have.  This can be revisited if we ever have another call site.
                    throw new Win32Exception();
                }
            }
            finally
            {
                FreeArray(envpPtr, envp.Length);
                FreeArray(argvPtr, argv.Length);
            }
        }

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_ForkAndExecProcess", SetLastError = true)]
        private static extern unsafe int ForkAndExecProcess(
            string filename, byte** argv, byte** envp, string cwd,
            int redirectStdin, int redirectStdout, int redirectStderr,
            out int lpChildPid, out int stdinFd, out int stdoutFd, out int stderrFd);

        private static unsafe void AllocNullTerminatedArray(string[] arr, ref byte** arrPtr)
        {
            int arrLength = arr.Length + 1; // +1 is for null termination

            // Allocate the unmanaged array to hold each string pointer.
            // It needs to have an extra element to null terminate the array.
            arrPtr = (byte**)Marshal.AllocHGlobal(sizeof(IntPtr) * arrLength);
            Debug.Assert(arrPtr != null);

            // Zero the memory so that if any of the individual string allocations fails,
            // we can loop through the array to free any that succeeded.
            // The last element will remain null.
            for (int i = 0; i < arrLength; i++)
            {
                arrPtr[i] = null;
            }

            // Now copy each string to unmanaged memory referenced from the array.
            // We need the data to be an unmanaged, null-terminated array of UTF8-encoded bytes.
            for (int i = 0; i < arr.Length; i++)
            {
                byte[] byteArr = Encoding.UTF8.GetBytes(arr[i]);

                arrPtr[i] = (byte*)Marshal.AllocHGlobal(byteArr.Length + 1); //+1 for null termination
                Debug.Assert(arrPtr[i] != null);

                Marshal.Copy(byteArr, 0, (IntPtr)arrPtr[i], byteArr.Length); // copy over the data from the managed byte array
                arrPtr[i][byteArr.Length] = (byte)'\0'; // null terminate
            }
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
                        arr[i] = null;
                    }
                }

                // And then the array itself
                Marshal.FreeHGlobal((IntPtr)arr);
            }
        }
    }
}
