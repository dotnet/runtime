// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.SqlServer.TDS.EndPoint
{
    /// <summary>
    /// Interface that TDS parser calls into to process TDS stream
    /// </summary>
    public interface ITDSClient
    {
        /// <summary>
        /// Indicates the state of the TDS client
        /// </summary>
        TDSClientState State { get; }

        /// <summary>
        /// Run time context of the client
        /// </summary>
        ITDSClientContext Context { get; }

        /// <summary>
        /// Notification that is fired before the parser establishes a connection to the server. This method should be used to initialize context.
        /// </summary>
        void OnPreConnect();

        /// <summary>
        /// It is called when transport was established with TDS server and client parser is ready to start conversation
        /// </summary>
        /// <returns>TDS message to initiate conversation with TDS Server</returns>
        TDSMessage OnPreLogin();

        /// <summary>
        /// It is called when pre-login response arrives
        /// </summary>
        /// <param name="message">TDS message received</param>
        /// <returns>TDS message to send to the server</returns>
        TDSMessage OnPreLoginResponse(TDSMessage message);

        /// <summary>
        /// It is called when SPNEGO response arrives
        /// </summary>
        /// <param name="packet">TDS message received</param>		
        TDSMessage OnSSPIResponse(TDSMessage message);

        /// <summary>
        /// It is called when FedAuthInfoToken response arrives
        /// </summary>
        /// <param name="packet">TDS message received</param>		
        TDSMessage OnFedAuthInfoTokenResponse(TDSMessage message);

        /// <summary>
        /// It is called when login acknowledgement arrives.
        /// </summary>
        /// <param name="packet">TDS message received</param>		
        void OnLoginResponse(TDSMessage message);

        /// <summary>
        /// Create a request to SQL Server after authentication
        /// </summary>
        /// <returns>TDS message to send to the server</returns>
        TDSMessage OnRequest();

        /// <summary>
        /// Process response to the request
        /// </summary>
        /// <param name="message">TDS message received</param>
        void OnResponse(TDSMessage message);

        /// <summary>
        /// Create a request to SQL Server to logout
        /// </summary>
        /// <returns>TDS message to send to the server</returns>
        TDSMessage OnLogout();

        /// <summary>
        /// Process response to the logout request
        /// </summary>
        /// <param name="message">TDS message received</param>
        void OnLogoutResponse(TDSMessage message);
    }
}
