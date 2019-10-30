// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
