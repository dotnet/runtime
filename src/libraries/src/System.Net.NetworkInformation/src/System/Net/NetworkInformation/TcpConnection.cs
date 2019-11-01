// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.NetworkInformation
{
    /// <summary>
    /// Provides information about the Transmission Control Protocol (TCP) connections on the local computer.
    /// </summary>
    public abstract class TcpConnectionInformation
    {
        /// <summary>
        /// Gets the local endpoint of a Transmission Control Protocol (TCP) connection.
        /// </summary>
        public abstract IPEndPoint LocalEndPoint { get; }

        /// <summary>
        /// Gets the remote endpoint of a Transmission Control Protocol (TCP) connection.
        /// </summary>
        public abstract IPEndPoint RemoteEndPoint { get; }

        /// <summary>
        /// Gets the state of this Transmission Control Protocol (TCP) connection.
        /// </summary>
        public abstract TcpState State { get; }
    }
}
