// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
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
}
