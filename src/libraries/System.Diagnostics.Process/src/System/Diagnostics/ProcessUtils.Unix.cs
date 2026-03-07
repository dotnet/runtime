// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Diagnostics
{
    internal static partial class ProcessUtils
    {
        /// <summary>Converts the environment variables information into an envp array.</summary>
        internal static string[] CreateEnvp(IDictionary<string, string?> environment)
        {
            var envp = new string[environment.Count];
            int index = 0;
            foreach (KeyValuePair<string, string?> pair in environment)
            {
                // Ignore null values for consistency with Environment.SetEnvironmentVariable
                if (pair.Value is not null)
                {
                    envp[index++] = pair.Key + "=" + pair.Value;
                }
            }
            // Resize the array in case we skipped some entries
            Array.Resize(ref envp, index);
            return envp;
        }

        internal static unsafe void AllocNullTerminatedArray(string[] arr, ref byte** arrPtr)
        {
            nuint arrLength = (nuint)arr.Length + 1; // +1 is for null termination

            // Allocate the unmanaged array to hold each string pointer.
            // It needs to have an extra element to null terminate the array.
            // Zero the memory so that if any of the individual string allocations fails,
            // we can loop through the array to free any that succeeded.
            // The last element will remain null.
            arrPtr = (byte**)NativeMemory.AllocZeroed(arrLength, (nuint)sizeof(byte*));

            // Now copy each string to unmanaged memory referenced from the array.
            // We need the data to be an unmanaged, null-terminated array of UTF8-encoded bytes.
            for (int i = 0; i < arr.Length; i++)
            {
                string str = arr[i];

                int byteLength = Encoding.UTF8.GetByteCount(str);
                arrPtr[i] = (byte*)NativeMemory.Alloc((nuint)byteLength + 1); //+1 for null termination

                int bytesWritten = Encoding.UTF8.GetBytes(str, new Span<byte>(arrPtr[i], byteLength));
                Debug.Assert(bytesWritten == byteLength);

                arrPtr[i][bytesWritten] = (byte)'\0'; // null terminate
            }
        }

        internal static unsafe void FreeArray(byte** arr, int length)
        {
            if (arr != null)
            {
                // Free each element of the array
                for (int i = 0; i < length; i++)
                {
                    NativeMemory.Free(arr[i]);
                }

                // And then the array itself
                NativeMemory.Free(arr);
            }
        }

        private static bool IsExecutable(string fullPath)
        {
            Interop.Sys.FileStatus fileinfo;

            if (Interop.Sys.Stat(fullPath, out fileinfo) < 0)
            {
                return false;
            }

            // Check if the path is a directory.
            if ((fileinfo.Mode & Interop.Sys.FileTypes.S_IFMT) == Interop.Sys.FileTypes.S_IFDIR)
            {
                return false;
            }

            const UnixFileMode AllExecute = UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;

            UnixFileMode permissions = ((UnixFileMode)fileinfo.Mode) & AllExecute;

            // Avoid checking user/group when permission.
            if (permissions == AllExecute)
            {
                return true;
            }
            else if (permissions == 0)
            {
                return false;
            }

            uint euid = Interop.Sys.GetEUid();

            if (euid == 0)
            {
                return true; // We're root.
            }

            if (euid == fileinfo.Uid)
            {
                // We own the file.
                return (permissions & UnixFileMode.UserExecute) != 0;
            }

            bool groupCanExecute = (permissions & UnixFileMode.GroupExecute) != 0;
            bool otherCanExecute = (permissions & UnixFileMode.OtherExecute) != 0;

            // Avoid group check when group and other have same permissions.
            if (groupCanExecute == otherCanExecute)
            {
                return groupCanExecute;
            }

            if (Interop.Sys.IsMemberOfGroup(fileinfo.Gid))
            {
                return groupCanExecute;
            }
            else
            {
                return otherCanExecute;
            }
        }

    }
}
