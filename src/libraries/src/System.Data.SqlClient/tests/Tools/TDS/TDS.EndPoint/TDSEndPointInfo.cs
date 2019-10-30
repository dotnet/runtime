// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net;

namespace Microsoft.SqlServer.TDS.EndPoint
{
    /// <summary>
    /// Container of the connection end-point information
    /// </summary>
    public class TDSEndPointInfo
    {
        /// <summary>
        /// IP address to/from which connection is established
        /// </summary>
        public IPAddress Address { get; internal set; }

        /// <summary>
        /// Port number
        /// </summary>
        public int Port { get; internal set; }

        /// <summary>
        /// Transport protocol for the end-point
        /// </summary>
        public TDSEndPointTransportType Transport { get; internal set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public TDSEndPointInfo()
        {
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSEndPointInfo(IPAddress address)
        {
            Address = address;
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSEndPointInfo(IPAddress address, int port) :
            this(address)
        {
            Port = port;
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSEndPointInfo(IPAddress address, int port, TDSEndPointTransportType transport) :
            this(address, port)
        {
            Transport = transport;
        }
    }
}
