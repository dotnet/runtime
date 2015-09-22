// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.Sockets
{
    // Specifies the mode for polling the status of a socket.
    public enum SelectMode
    {
        // Poll the read status of a socket.
        SelectRead = 0,

        // Poll the write status of a socket.
        SelectWrite = 1,

        // Poll the error status of a socket.
        SelectError = 2
    }
}
