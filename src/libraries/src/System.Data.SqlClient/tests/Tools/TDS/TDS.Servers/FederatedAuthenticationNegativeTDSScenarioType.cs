// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.SqlServer.TDS.Servers
{
    public enum FederatedAuthenticationNegativeTDSScenarioType : int
    {
        /// <summary>
        /// Valid Scenario. Do not perform negative activity.
        /// </summary>
        ValidScenario,

        /// <summary>
        /// Nonce is missing from LOGIN7 response in FedAuth's FEATUREEXTACK
        /// </summary>
        NonceMissingInFedAuthFEATUREXTACK,

        /// <summary>
        /// Nonce is missing from PRELOGIN response in FedAuth
        /// </summary>
        NonceMissingInFedAuthPreLogin,

        /// <summary>
        /// FedAuth is missing from FEATUREEXTACK Login response when it is expected
        /// </summary>
        FedAuthMissingInFEATUREEXTACK,

        /// <summary>
        /// Invalid B_FEDAUTHREQUIRED response
        /// </summary>
        InvalidB_FEDAUTHREQUIREDResponse,

        /// <summary>
        /// Signature is missing from LOGIN7 response in FedAuth's FEATUREEXTACK
        /// </summary>
        SignatureMissingInFedAuthFEATUREXTACK,
    }
}
