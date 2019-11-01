// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.SqlServer.TDS.Servers
{
    /// <summary>
    /// Arguments for Fed Auth Negative TDS Server
    /// </summary>
    public class FederatedAuthenticationNegativeTDSServerArguments : TDSServerArguments
    {
        /// <summary>
        /// Type of the Fed Auth Negative TDS Server
        /// </summary>
        public FederatedAuthenticationNegativeTDSScenarioType Scenario { get; set; }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public FederatedAuthenticationNegativeTDSServerArguments()
        {
        }
    }
}
