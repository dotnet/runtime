// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO.Ports;
using System.Net.Sockets;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Serial
    {
        [DllImport(Libraries.IOPortsNative, EntryPoint = "SystemIoPortsNative_SerialPortOpen", SetLastError = true)]
        internal static extern SafeSerialDeviceHandle SerialPortOpen(string name);

        [DllImport(Libraries.IOPortsNative, EntryPoint = "SystemIoPortsNative_SerialPortClose", SetLastError = true)]
        internal static extern int SerialPortClose(IntPtr handle);

        [DllImport(Libraries.IOPortsNative, EntryPoint = "SystemIoPortsNative_Shutdown")]
        internal static extern Error Shutdown(IntPtr socket, SocketShutdown how);

        /// <summary>
        /// Reads a number of bytes from an open file descriptor into a specified buffer.
        /// </summary>
        /// <param name="fd">The open file descriptor to try to read from</param>
        /// <param name="buffer">The buffer to read info into</param>
        /// <param name="count">The size of the buffer</param>
        /// <returns>
        /// Returns the number of bytes read on success; otherwise, -1 is returned
        /// Note - on fail. the position of the stream may change depending on the platform; consult man 2 read for more info
        /// </returns>
        [DllImport(Libraries.IOPortsNative, EntryPoint = "SystemIoPortsNative_Read", SetLastError = true)]
        internal static extern unsafe int Read(SafeHandle fd, byte* buffer, int count);

        /// <summary>
        /// Writes the specified buffer to the provided open file descriptor
        /// </summary>
        /// <param name="fd">The file descriptor to try and write to</param>
        /// <param name="buffer">The data to attempt to write</param>
        /// <param name="bufferSize">The amount of data to write, in bytes</param>
        /// <returns>
        /// Returns the number of bytes written on success; otherwise, returns -1 and sets errno
        /// </returns>
        [DllImport(Libraries.IOPortsNative, EntryPoint = "SystemIoPortsNative_Write", SetLastError = true)]
        internal static extern unsafe int Write(SafeHandle fd, byte* buffer, int bufferSize);

        /// <summary>
        /// Polls a set of file descriptors for signals and returns what signals have been set
        /// </summary>
        /// <param name="pollEvents">A list of PollEvent entries</param>
        /// <param name="eventCount">The number of entries in pollEvents</param>
        /// <param name="timeout">The amount of time to wait; -1 for infinite, 0 for immediate return, and a positive number is the number of milliseconds</param>
        /// <param name="triggered">The number of events triggered (i.e. the number of entries in pollEvents with a non-zero TriggeredEvents). May be zero in the event of a timeout.</param>
        /// <returns>An error or Error.SUCCESS.</returns>
        [DllImport(Libraries.IOPortsNative, EntryPoint = "SystemIoPortsNative_Poll")]
        private static extern unsafe Error Poll(PollEvent* pollEvents, uint eventCount, int timeout, uint* triggered);

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
