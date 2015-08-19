// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libc
    {
        [Flags]
        internal enum PollFlags : short
        {
            POLLIN      = 0x0001,       /* any readable data available */
            POLLOUT     = 0x0004,       /* data can be written without blocking */
            POLLERR     = 0x0008,       /* some poll error occurred */
            POLLHUP     = 0x0010,       /* file descriptor was "hung up" */
            POLLNVAL    = 0x0020,       /* requested events "invalid" */
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct pollfd
        {
            internal int fd;            /* file descriptor to poll*/
            internal PollFlags events;  /* events to poll for */
            internal PollFlags revents; /* events received from polling */
        }

        /// <summary>
        /// Polls a set of file descriptors for signals and returns what signals have been set
        /// </summary>
        /// <param name="fds">A pointer to pollfd structs to look for</param>
        /// <param name="count">The number of entries in fds</param>
        /// <param name="timeout">The amount of time to wait; -1 for infinite, 0 for immediate return, and a positive number is the number of milliseconds</param>
        /// <returns>
        /// Returns a positive number (which is the number of structures with nonzero revent files), 0 for a timeout or no 
        /// descriptors were ready, or -1 on error.
        /// </returns>
        [DllImport(Libraries.Libc, SetLastError = true)]
        internal static unsafe extern int poll(pollfd* fds, uint count, int timeout);

        /// <summary>
        /// Polls a File Descriptor for the passed in flags.
        /// </summary>
        /// <param name="fd">The descriptor to poll</param>
        /// <param name="flags">The flags to poll for</param>
        /// <param name="timeout">The amount of time to wait; -1 for infinite, 0 for immediate return, and a positive number is the number of milliseconds</param>
        /// <param name="resultFlags">The flags that were returned by the poll call</param>
        /// <returns>
        /// Returns a positive number (which is the number of structures with nonzero revent files), 0 for a timeout or no 
        /// descriptors were ready, or -1 on error.
        /// </returns>
        internal unsafe static int poll(int fd, PollFlags flags, int timeout, out PollFlags resultFlags)
        {
            pollfd pfd = default(pollfd);
            pfd.fd = fd;
            pfd.events = flags;
            int result = poll(&pfd, 1, timeout);
            resultFlags = pfd.revents;
            return result;
        }
    }
}
