// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.SqlServer.TDS.Servers
{
    /// <summary>
    /// Arguments for authenticating TDS Server
    /// </summary>
    public class AuthenticatingTDSServerArguments : TDSServerArguments
    {
        /// <summary>
        /// Type of the application intent filter
        /// </summary>
        public ApplicationIntentFilterType ApplicationIntentFilter { get; set; }

        /// <summary>
        /// Filter for server name
        /// </summary>
        public string ServerNameFilter { get; set; }

        /// <summary>
        /// Type of the filtering algorithm to use
        /// </summary>
        public ServerNameFilterType ServerNameFilterType { get; set; }

        /// <summary>
        /// TDS packet size filtering
        /// </summary>
        public ushort? PacketSizeFilter { get; set; }

        /// <summary>
        /// Filter for application name
        /// </summary>
        public string ApplicationNameFilter { get; set; }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public AuthenticatingTDSServerArguments()
        {
            // Allow everyone to connect
            ApplicationIntentFilter = ApplicationIntentFilterType.All;

            // By default we don't turn on server name filter
            ServerNameFilterType = Servers.ServerNameFilterType.None;
        }
    }
}
