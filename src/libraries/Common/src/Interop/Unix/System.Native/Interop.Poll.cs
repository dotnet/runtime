// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [Flags]
        internal enum PollEvents : short
        {
            POLLNONE = 0x0000,  // No events occurred.
            POLLIN   = 0x0001,  // non-urgent readable data available
            POLLPRI  = 0x0002,  // urgent readable data available
            POLLOUT  = 0x0004,  // data can be written without blocked
            POLLERR  = 0x0008,  // an error occurred
            POLLHUP  = 0x0010,  // the file descriptor hung up
            POLLNVAL = 0x0020,  // the requested events were invalid
        }

        internal struct PollEvent
        {
            internal int FileDescriptor;         // The file descriptor to poll
            internal PollEvents Events;          // The events to poll for
            internal PollEvents TriggeredEvents; // The events that occurred which triggered the poll
        }

        /// <summary>
        /// Polls a set of file descriptors for signals and returns what signals have been set
        /// </summary>
        /// <param name="pollEvents">A list of PollEvent entries</param>
        /// <param name="numberOfPollFds">The number of entries in pollEvents</param>
        /// <param name="timeout">The amount of time to wait; -1 for infinite, 0 for immediate return, and a positive number is the number of milliseconds</param>
        /// <param name="triggered">The number of events triggered (i.e. the number of entries in pollEvents with a non-zero TriggeredEvents). May be zero in the event of a timeout.</param>
        /// <returns>An error or Error.SUCCESS.</returns>
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_Poll")]
        internal static extern unsafe Error Poll(PollEvent* pollEvents, uint eventCount, int timeout, uint* triggered);

        /// <summary>
        /// Polls a File Descriptor for the passed in flags.
        /// </summary>
        /// <param name="fd">The descriptor to poll</param>
        /// <param name="events">The events to poll for</param>
        /// <param name="timeout">The amount of time to wait; -1 for infinite, 0 for immediate return, and a positive number is the number of milliseconds</param>
        /// <param name="triggered">The events that were returned by the poll call. May be PollEvents.POLLNONE in the case of a timeout.</param>
        /// <returns>An error or Error.SUCCESS.</returns>
        internal static unsafe Error Poll(SafeHandle fd, PollEvents events, int timeout, out PollEvents triggered)
        {
            bool gotRef = false;
            try
            {
                fd.DangerousAddRef(ref gotRef);

                var pollEvent = new PollEvent
                {
                    FileDescriptor = fd.DangerousGetHandle().ToInt32(),
                    Events = events,
                };

                uint unused;
                Error err = Poll(&pollEvent, 1, timeout, &unused);
                triggered = pollEvent.TriggeredEvents;
                return err;
            }
            finally
            {
                if (gotRef)
                {
                    fd.DangerousRelease();
                }
            }
        }
    }
}
