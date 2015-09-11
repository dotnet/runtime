// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal enum PollFlags : short
        {
            POLLIN   = 0x0001,  /* any readable data available */
            POLLOUT  = 0x0004,  /* data can be written without blocked */
            POLLERR  = 0x0008,  /* an error occurred */
            POLLHUP  = 0x0010,  /* the file descriptor hung up */
            POLLNVAL = 0x0020,  /* the requested events were invalid */
        }

        internal struct PollFD
        {
            internal int        FD;     // The file descriptor to poll
            internal PollFlags  Events;    // The events to poll for
            internal PollFlags  REvents;     // The events that occurred which triggered the poll
        }

        /// <summary>
        /// Polls a set of file descriptors for signals and returns what signals have been set
        /// </summary>
        /// <param name="pollData">A pointer to pollfd structs to look for</param>
        /// <param name="numberOfPollFds">The number of entries in pollData</param>
        /// <param name="timeout">The amount of time to wait; -1 for infinite, 0 for immediate return, and a positive number is the number of milliseconds</param>
        /// <returns>
        /// Returns a positive number (which is the number of structures with nonzero revent files), 0 for a timeout or no 
        /// descriptors were ready, or -1 on error.
        /// </returns>
        [DllImport(Libraries.SystemNative, SetLastError = true)]
        internal static unsafe extern int Poll(PollFD* pollData, uint numberOfPollFds, int timeout);

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
        internal static unsafe int Poll(int fd, PollFlags flags, int timeout, out PollFlags resultFlags)
        {
            PollFD pfd = default(PollFD);
            pfd.FD = fd;
            pfd.Events = flags;
            int result = Poll(&pfd, 1, timeout);
            resultFlags = pfd.REvents;
            return result;
        }
    }
}
