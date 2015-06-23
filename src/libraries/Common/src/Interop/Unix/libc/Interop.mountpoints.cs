// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

using uid_t = System.UInt32;

internal static partial class Interop
{
    internal static partial class libc
    {
        [DllImport("libc")]
        static extern int printf(string data);
        /// <summary>
        /// Gets all the current mount points on the system
        /// </summary>
        /// <param name="ppBuffer">A pointer to an array of mount points</param>
        /// <param name="flags">Flags that are passed to the getfsstat call for each statfs struct</param>
        /// <returns>Returns the number of retrieved statfs structs</returns>
        /// <remarks
        /// Do NOT free this memory...this memory is allocated by the OS, which is responsible for it.
        /// This call could also block for a bit to wait for slow network drives.
        /// </remarks>
        [DllImport(Interop.Libraries.libc, EntryPoint = "getmntinfo" + Interop.Libraries.INODE64SUFFIX, SetLastError = true)]
        internal static unsafe extern int getmntinfo(statfs** ppBuffer, int flags);

        /// <summary>
        /// Gets a statfs struct for the given path that describes that mount point
        /// </summary>
        /// <param name="path">The path to retrieve the statfs for</param>
        /// <param name="buffer">The output statfs struct describing the mount point</param>
        /// <returns>Returns 0 on success, -1 on failure</returns>
        [DllImport(Interop.Libraries.libc, EntryPoint = "statfs" + Interop.Libraries.INODE64SUFFIX, SetLastError = true)]
        private static unsafe extern int get_statfs(string path, statfs* buffer);

        /// <summary>
        /// Gets a statfs struct for a given mount point
        /// </summary>
        /// <param name="name">The drive name to retrieve the statfs data for</param>
        /// <returns>Returns </returns>
        internal static unsafe statfs GetStatFsForDriveName(string name)
        {
            statfs data = default(statfs);
            int result = get_statfs(name, &data);
            if (result < 0)
            {
                int errno = Marshal.GetLastWin32Error();
                if (errno == Interop.Errors.ENOENT)
                    throw new System.IO.DriveNotFoundException(SR.Format(SR.IO_DriveNotFound_Drive, name)); // match Win32 exception
                else
                    throw Interop.GetExceptionForIoErrno(errno, isDirectory: true);
            }
            else
            {
                printf(System.Runtime.InteropServices.Marshal.SizeOf<statfs>().ToString() + "\r\n");
                printf("Should be here\r\n");
                return data;
            }
        }
    }
}