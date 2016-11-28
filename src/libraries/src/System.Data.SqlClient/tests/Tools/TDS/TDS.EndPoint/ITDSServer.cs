// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.SqlServer.TDS.EndPoint
{
    /// <summary>
    /// Interface that TDS parser calls into to process TDS stream
    /// </summary>
    public interface ITDSServer
    {
        /// <summary>
        /// Create a new TDS server session
        /// </summary>
        /// <returns>A new instance of the TDS server session</returns>
        ITDSServerSession OpenSession();

        /// <summary>
        /// Close TDS server session
        /// </summary>
        /// <param name="session">An instance of the TDS server session to close</param>
        void CloseSession(ITDSServerSession session);

        /// <summary>
        /// It is called when pre-login request arrives
        /// </summary>
        /// <param name="message">TDS message recieved</param>
        /// <returns>TDS messages to be respond with</returns>
        TDSMessageCollection OnPreLoginRequest(ITDSServerSession session, TDSMessage message);

        /// <summary>
        /// It is called when login request arrives
        /// </summary>
        /// <param name="message">TDS message recieved</param>
        /// <returns>TDS message to respond with</returns>
        TDSMessageCollection OnLogin7Request(ITDSServerSession session, TDSMessage message);

        /// <summary>
        /// It is called when federated authentication token message arrives. Called only when
        /// such a message arrives in response to federated authentication info, not when the
        /// token is part of a login request.
        /// </summary>
        /// <param name="message">TDS message received</param>
        /// <returns>TDS message to respond with</returns>
        TDSMessageCollection OnFederatedAuthenticationTokenMessage(ITDSServerSession session, TDSMessage message);

        /// <summary>
        /// It is called when SSPI payload arrives
        /// </summary>
        /// <param name="message">TDS message recieved</param>
        /// <returns>TDS message to respond with</returns>
        TDSMessageCollection OnSSPIRequest(ITDSServerSession session, TDSMessage message);

        /// <summary>
        /// It is called when SQL batch request arrives
        /// </summary>
        /// <param name="message">TDS message recieved</param>
        /// <returns>TDS message to respond with</returns>
        TDSMessageCollection OnSQLBatchRequest(ITDSServerSession session, TDSMessage message);

        /// <summary>
        /// It is called when attention arrives
        /// </summary>
        /// <param name="message">TDS message recieved</param>
        /// <returns>TDS message to respond with</returns>
        TDSMessageCollection OnAttention(ITDSServerSession session, TDSMessage message);
    }
}
