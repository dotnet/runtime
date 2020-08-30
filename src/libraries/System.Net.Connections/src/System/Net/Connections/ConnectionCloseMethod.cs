// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Connections
{
    /// <summary>
    /// Methods for closing a connection.
    /// </summary>
    public enum ConnectionCloseMethod
    {
        /// <summary>
        /// The connection should be flushed and closed.
        /// </summary>
        GracefulShutdown,

        /// <summary>
        /// The connection should be aborted gracefully, performing any I/O needed to notify the other side of the connection that it has been aborted.
        /// </summary>
        Abort,

        /// <summary>
        /// The connection should be aborted immediately, avoiding any I/O needed to notify the other side of the connection that it has been aborted.
        /// </summary>
        Immediate
    }
}
