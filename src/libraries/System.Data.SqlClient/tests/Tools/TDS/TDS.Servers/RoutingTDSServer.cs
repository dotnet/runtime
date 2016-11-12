// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;

using Microsoft.SqlServer.TDS.Done;
using Microsoft.SqlServer.TDS.EndPoint;
using Microsoft.SqlServer.TDS.EnvChange;
using Microsoft.SqlServer.TDS.Error;
using Microsoft.SqlServer.TDS.PreLogin;
using Microsoft.SqlServer.TDS.Login7;
using Microsoft.SqlServer.TDS.FeatureExtAck;

namespace Microsoft.SqlServer.TDS.Servers
{
    /// <summary>
    /// TDS Server that routes clients to the configured destination
    /// </summary>
    public class RoutingTDSServer : GenericTDSServer
    {
        /// <summary>
        /// Initialization constructor
        /// </summary>
        public RoutingTDSServer() :
            this(new RoutingTDSServerArguments())
        {
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public RoutingTDSServer(RoutingTDSServerArguments arguments) :
            base(arguments)
        {
        }

        /// <summary>
        /// Handler for pre-login request
        /// </summary>
        public override TDSMessageCollection OnPreLoginRequest(ITDSServerSession session, TDSMessage request)
        {
            // Delegate to the base class
            TDSMessageCollection response = base.OnPreLoginRequest(session, request);

            // Check if arguments are of the routing server
            if (Arguments is RoutingTDSServerArguments)
            {
                // Cast to routing server arguments
                RoutingTDSServerArguments serverArguments = Arguments as RoutingTDSServerArguments;

                // Check if routing is configured during login
                if (serverArguments.RouteOnPacket == TDSMessageType.TDS7Login)
                {
                    // Check if pre-login response is contained inside the first message
                    if (response.Count > 0 && response[0].Any(t => t is TDSPreLoginToken))
                    {
                        // Find the first prelogin token
                        TDSPreLoginToken preLoginResponse = (TDSPreLoginToken)response[0].Where(t => t is TDSPreLoginToken).First();

                        // Inflate pre-login request from the message
                        TDSPreLoginToken preLoginRequest = request[0] as TDSPreLoginToken;

                        // Update MARS with the requested value
                        preLoginResponse.IsMARS = preLoginRequest.IsMARS;
                    }
                }
            }

            return response;
        }

        /// <summary>
        /// Handler for login request
        /// </summary>
        public override TDSMessageCollection OnLogin7Request(ITDSServerSession session, TDSMessage request)
        {
            // Inflate login7 request from the message
            TDSLogin7Token loginRequest = request[0] as TDSLogin7Token;

            // Check if arguments are of the routing server
            if (Arguments is RoutingTDSServerArguments)
            {
                // Cast to routing server arguments
                RoutingTDSServerArguments ServerArguments = Arguments as RoutingTDSServerArguments;

                // Check filter
                if (ServerArguments.RequireReadOnly && (loginRequest.TypeFlags.ReadOnlyIntent != TDSLogin7TypeFlagsReadOnlyIntent.ReadOnly))
                {
                    // Log request
                    TDSUtilities.Log(Arguments.Log, "Request", loginRequest);

                    // Prepare ERROR token with the denial details
                    TDSErrorToken errorToken = new TDSErrorToken(18456, 1, 14, "Received application intent: " + loginRequest.TypeFlags.ReadOnlyIntent.ToString(), Arguments.ServerName);

                    // Log response
                    TDSUtilities.Log(Arguments.Log, "Response", errorToken);

                    // Serialize the error token into the response packet
                    TDSMessage responseMessage = new TDSMessage(TDSMessageType.Response, errorToken);

                    // Prepare ERROR token for the final decision
                    errorToken = new TDSErrorToken(18456, 1, 14, "Read-Only application intent is required for routing", Arguments.ServerName);

                    // Log response
                    TDSUtilities.Log(Arguments.Log, "Response", errorToken);

                    // Serialize the error token into the response packet
                    responseMessage.Add(errorToken);

                    // Create DONE token
                    TDSDoneToken doneToken = new TDSDoneToken(TDSDoneTokenStatusType.Final | TDSDoneTokenStatusType.Error);

                    // Log response
                    TDSUtilities.Log(Arguments.Log, "Response", doneToken);

                    // Serialize DONE token into the response packet
                    responseMessage.Add(doneToken);

                    // Return a single message in the collection
                    return new TDSMessageCollection(responseMessage);
                }
            }

            // Delegate to the base class
            return base.OnLogin7Request(session, request);
        }

        /// <summary>
        /// It is called when SQL batch request arrives
        /// </summary>
        /// <param name="message">TDS message recieved</param>
        /// <returns>TDS message to respond with</returns>
        public override TDSMessageCollection OnSQLBatchRequest(ITDSServerSession session, TDSMessage request)
        {
            // Delegate to the base class to produce the response first
            TDSMessageCollection batchResponse = base.OnSQLBatchRequest(session, request);

            // Check if arguments are of routing server
            if (Arguments is RoutingTDSServerArguments)
            {
                // Cast to routing server arguments
                RoutingTDSServerArguments ServerArguments = Arguments as RoutingTDSServerArguments;

                // Check routing condition
                if (ServerArguments.RouteOnPacket == TDSMessageType.SQLBatch)
                {
                    // Construct routing token
                    TDSPacketToken routingToken = CreateRoutingToken();

                    // Log response
                    TDSUtilities.Log(Arguments.Log, "Response", routingToken);

                    // Insert the routing token at the beginning of the response
                    batchResponse[0].Insert(0, routingToken);
                }
                else
                {
                    // Get the first response message
                    TDSMessage responseMessage = batchResponse[0];

                    // Reset the content of the first message
                    responseMessage.Clear();

                    // Prepare ERROR token with the denial details
                    responseMessage.Add(new TDSErrorToken(11111, 1, 14, "Client should have been routed by now", Arguments.ServerName));

                    // Log response
                    TDSUtilities.Log(Arguments.Log, "Response", responseMessage[0]);

                    // Prepare DONE token
                    responseMessage.Add(new TDSDoneToken(TDSDoneTokenStatusType.Final | TDSDoneTokenStatusType.Error));

                    // Log response
                    TDSUtilities.Log(Arguments.Log, "Response", responseMessage[1]);
                }
            }

            // Register only one message with the collection
            return batchResponse;
        }

        /// <summary>
        /// Complete login sequence
        /// </summary>
        protected override TDSMessageCollection OnAuthenticationCompleted(ITDSServerSession session)
        {
            // Delegate to the base class
            TDSMessageCollection responseMessageCollection = base.OnAuthenticationCompleted(session);

            // Check if arguments are of routing server
            if (Arguments is RoutingTDSServerArguments)
            {
                // Cast to routing server arguments
                RoutingTDSServerArguments serverArguments = Arguments as RoutingTDSServerArguments;

                // Check routing condition
                if (serverArguments.RouteOnPacket == TDSMessageType.TDS7Login)
                {
                    // Construct routing token
                    TDSPacketToken routingToken = CreateRoutingToken();

                    // Log response
                    TDSUtilities.Log(Arguments.Log, "Response", routingToken);

                    // Get the first message
                    TDSMessage targetMessage = responseMessageCollection[0];

                    // Index at which to insert the routing token
                    int insertIndex = targetMessage.Count - 1;

                    // VSTS# 1021027 - Read-Only Routing yields TDS protocol error
                    // Resolution: Send TDS FeatureExtAct token before TDS ENVCHANGE token with routing information
                    TDSPacketToken featureExtAckToken = targetMessage.Find(t => t is TDSFeatureExtAckToken);

                    // Check if found
                    if (featureExtAckToken != null)
                    {
                        // Find token position
                        insertIndex = targetMessage.IndexOf(featureExtAckToken);
                    }

                    // Insert right before the done token
                    targetMessage.Insert(insertIndex, routingToken);
                }
            }

            return responseMessageCollection;
        }

        /// <summary>
        /// Create a new instance of the routing token that instructs client according to the routing destination URL
        /// </summary>
        protected TDSPacketToken CreateRoutingToken()
        {
            // Cast to routing server arguments
            RoutingTDSServerArguments ServerArguments = Arguments as RoutingTDSServerArguments;

            // Construct routing token value
            TDSRoutingEnvChangeTokenValue routingInfo = new TDSRoutingEnvChangeTokenValue();

            // Read the values and populate routing info
            routingInfo.Protocol = (TDSRoutingEnvChangeTokenValueType)ServerArguments.RoutingProtocol;
            routingInfo.ProtocolProperty = ServerArguments.RoutingTCPPort;
            routingInfo.AlternateServer = ServerArguments.RoutingTCPHost;

            // Construct routing token
            return new TDSEnvChangeToken(TDSEnvChangeTokenType.Routing, routingInfo);
        }
    }
}
