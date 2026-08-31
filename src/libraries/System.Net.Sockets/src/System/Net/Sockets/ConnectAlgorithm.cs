// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Sockets
{
    /// <summary>
    /// Specifies the algorithm used to establish a socket connection.
    /// </summary>
    public enum ConnectAlgorithm
    {
        /// <summary>
        /// The default connection mechanism, typically sequential processing.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Uses a Happy Eyeballs-like algorithm to connect, attempting connections in parallel to improve speed and reliability.
        /// </summary>
        Parallel = 1,
    }
}
