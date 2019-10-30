// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.SqlServer.TDS.Servers
{
    /// <summary>
    /// Arguments for routing TDS Server
    /// </summary>
    public class RoutingTDSServerArguments : TDSServerArguments
    {
        /// <summary>
        /// Routing destination protocol
        /// </summary>
        public int RoutingProtocol { get; set; }

        /// <summary>
        /// Routing TCP port
        /// </summary>
        public ushort RoutingTCPPort { get; set; }

        /// <summary>
        /// Routing TCP host name
        /// </summary>
        public string RoutingTCPHost { get; set; }

        /// <summary>
        /// Packet on which routing should occur
        /// </summary>
        public TDSMessageType RouteOnPacket { get; set; }

        /// <summary>
        /// Indicates that routing should only occur on read-only connections
        /// </summary>
        public bool RequireReadOnly { get; set; }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public RoutingTDSServerArguments()
        {
            // By default we route on login
            RouteOnPacket = TDSMessageType.TDS7Login;

            // By default we reject non-read-only connections
            RequireReadOnly = true;
        }
    }
}
