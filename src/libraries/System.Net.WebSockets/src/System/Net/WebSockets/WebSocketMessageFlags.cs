// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.WebSockets
{
    /// <summary>
    /// Flags for controlling how the <see cref="WebSocket"/> should send a message.
    /// </summary>
    [Flags]
    public enum WebSocketMessageFlags
    {
        /// <summary>
        /// None
        /// </summary>
        None = 0,

        /// <summary>
        /// Indicates that the data in "buffer" is the last part of a message.
        /// </summary>
        EndOfMessage = 1,

        /// <summary>
        /// Disables compression for the message if compression has been enabled for the <see cref="WebSocket"/> instance.
        /// </summary>
        DisableCompression = 2
    }
}
