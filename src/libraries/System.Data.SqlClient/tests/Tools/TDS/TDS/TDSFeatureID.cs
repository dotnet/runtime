// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.SqlServer.TDS
{
    /// <summary>
    /// TDS feature identifier
    /// </summary>
    public enum TDSFeatureID : byte
    {
        /// <summary>
        /// Session recovery (connection resiliency)
        /// </summary>
        SessionRecovery = 0x01,

        /// <summary>
        /// Federated authentication
        /// </summary>
        FederatedAuthentication = 0x02,

        /// <summary>
        /// End of the list
        /// </summary>
        Terminator = 0xFF
    }
}