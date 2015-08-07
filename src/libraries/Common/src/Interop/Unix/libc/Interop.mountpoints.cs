// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libc
    {
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
        [DllImport(Interop.Libraries.Libc, EntryPoint = "getmntinfo" + Interop.Libraries.INODE64SUFFIX, SetLastError = true)]
        internal static unsafe extern int getmntinfo(statfs** ppBuffer, int flags);

        /// <summary>
        /// Gets a statfs struct for the given path that describes that mount point
        /// </summary>
        /// <param name="path">The path to retrieve the statfs for</param>
        /// <param name="buffer">The output statfs struct describing the mount point</param>
        /// <returns>Returns 0 on success, -1 on failure</returns>
        [DllImport(Interop.Libraries.Libc, EntryPoint = "statfs" + Interop.Libraries.INODE64SUFFIX, SetLastError = true)]
        private static unsafe extern int get_statfs(string path, out statfs buffer);

        /// <summary>
        /// Attempts to get a statfs struct for a given mount point.
        /// </summary>
        /// <param name="name">The drive name to retrieve the statfs data for.</param>
        /// <param name="data">The data retrieved from the mount point.</param>
        /// <returns>Returns true if data was filled with the results; otherwise, false.</returns>
        internal static bool TryGetStatFsForDriveName(string name, out statfs data, out ErrorInfo errorInfo)
        {
            data = default(statfs);
            if (get_statfs(name, out data) < 0)
            {
                errorInfo = Interop.Sys.GetLastErrorInfo();
                return false;
            }

            errorInfo = default(ErrorInfo);
            return true;
        }

        /// <summary>
        /// Gets a statfs struct for a given mount point
        /// </summary>
        /// <param name="name">The drive name to retrieve the statfs data for.</param>
        /// <returns>Returns the statfs.</returns>
        internal static statfs GetStatFsForDriveName(string name)
        {
            statfs data;
            ErrorInfo errorInfo;
            if (!TryGetStatFsForDriveName(name, out data, out errorInfo))
            {
                throw errorInfo.Error == Error.ENOENT ?
                    new System.IO.DriveNotFoundException(SR.Format(SR.IO_DriveNotFound_Drive, name)) : // match Win32 exception
                    GetExceptionForIoErrno(errorInfo, isDirectory: true);
            }
            return data;
        }

    }
}
