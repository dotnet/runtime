// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.SqlServer.TDS.Done;
using Microsoft.SqlServer.TDS.EndPoint;
using Microsoft.SqlServer.TDS.Error;
using Microsoft.SqlServer.TDS.Login7;


namespace Microsoft.SqlServer.TDS.Servers
{
    /// <summary>
    /// TDS Server that authenticates clients according to the requested parameters
    /// </summary>
    public class AuthenticatingTDSServer : GenericTDSServer
    {
        /// <summary>
        /// Initialization constructor
        /// </summary>
        public AuthenticatingTDSServer() :
            this(new AuthenticatingTDSServerArguments())
        {
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public AuthenticatingTDSServer(AuthenticatingTDSServerArguments arguments) :
            base(arguments)
        {
        }

        /// <summary>
        /// Handler for login request
        /// </summary>
        public override TDSMessageCollection OnLogin7Request(ITDSServerSession session, TDSMessage request)
        {
            // Inflate login7 request from the message
            TDSLogin7Token loginRequest = request[0] as TDSLogin7Token;

            // Check if arguments are of the authenticating TDS server
            if (Arguments is AuthenticatingTDSServerArguments)
            {
                // Cast to authenticating TDS server arguments
                AuthenticatingTDSServerArguments ServerArguments = Arguments as AuthenticatingTDSServerArguments;

                // Check if we're still processing normal login
                if (ServerArguments.ApplicationIntentFilter != ApplicationIntentFilterType.All)
                {
                    // Check filter
                    if ((ServerArguments.ApplicationIntentFilter == ApplicationIntentFilterType.ReadOnly && loginRequest.TypeFlags.ReadOnlyIntent != TDSLogin7TypeFlagsReadOnlyIntent.ReadOnly)
                        || (ServerArguments.ApplicationIntentFilter == ApplicationIntentFilterType.None))
                    {
                        // Log request to which we're about to send a failure
                        TDSUtilities.Log(Arguments.Log, "Request", loginRequest);

                        // Prepare ERROR token with the denial details
                        TDSErrorToken errorToken = new TDSErrorToken(18456, 1, 14, "Received application intent: " + loginRequest.TypeFlags.ReadOnlyIntent.ToString(), Arguments.ServerName);

                        // Log response
                        TDSUtilities.Log(Arguments.Log, "Response", errorToken);

                        // Serialize the error token into the response packet
                        TDSMessage responseMessage = new TDSMessage(TDSMessageType.Response, errorToken);

                        // Prepare ERROR token for the final decision
                        errorToken = new TDSErrorToken(18456, 1, 14, "Connection is denied by application intent filter", Arguments.ServerName);

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

                        // Put a single message into the collection and return it
                        return new TDSMessageCollection(responseMessage);
                    }
                }

                // Check if we're still processing normal login and there's a filter to check
                if (ServerArguments.ServerNameFilterType != ServerNameFilterType.None)
                {
                    // Check each algorithm
                    if ((ServerArguments.ServerNameFilterType == ServerNameFilterType.Equals && string.Compare(ServerArguments.ServerNameFilter, loginRequest.ServerName, true) != 0)
                        || (ServerArguments.ServerNameFilterType == ServerNameFilterType.StartsWith && !loginRequest.ServerName.StartsWith(ServerArguments.ServerNameFilter))
                        || (ServerArguments.ServerNameFilterType == ServerNameFilterType.EndsWith && !loginRequest.ServerName.EndsWith(ServerArguments.ServerNameFilter))
                        || (ServerArguments.ServerNameFilterType == ServerNameFilterType.Contains && !loginRequest.ServerName.Contains(ServerArguments.ServerNameFilter)))
                    {
                        // Log request to which we're about to send a failure
                        TDSUtilities.Log(Arguments.Log, "Request", loginRequest);

                        // Prepare ERROR token with the reason
                        TDSErrorToken errorToken = new TDSErrorToken(18456, 1, 14, string.Format("Received server name \"{0}\", expected \"{1}\" using \"{2}\" algorithm", loginRequest.ServerName, ServerArguments.ServerNameFilter, ServerArguments.ServerNameFilterType), Arguments.ServerName);

                        // Log response
                        TDSUtilities.Log(Arguments.Log, "Response", errorToken);

                        // Serialize the errorToken token into the response packet
                        TDSMessage responseMessage = new TDSMessage(TDSMessageType.Response, errorToken);

                        // Prepare ERROR token with the final errorToken
                        errorToken = new TDSErrorToken(18456, 1, 14, "Connection is denied by server name filter", Arguments.ServerName);

                        // Log response
                        TDSUtilities.Log(Arguments.Log, "Response", errorToken);

                        // Serialize the errorToken token into the response packet
                        responseMessage.Add(errorToken);

                        // Create DONE token
                        TDSDoneToken doneToken = new TDSDoneToken(TDSDoneTokenStatusType.Final | TDSDoneTokenStatusType.Error);

                        // Log response
                        TDSUtilities.Log(Arguments.Log, "Response", doneToken);

                        // Serialize DONE token into the response packet
                        responseMessage.Add(doneToken);

                        // Return only a single message with the collection
                        return new TDSMessageCollection(responseMessage);
                    }
                }

                // Check if packet size filter is applied
                if (ServerArguments.PacketSizeFilter != null)
                {
                    // Check if requested packet size is the same as the filter specified
                    if (loginRequest.PacketSize != ServerArguments.PacketSizeFilter.Value)
                    {
                        // Log request to which we're about to send a failure
                        TDSUtilities.Log(Arguments.Log, "Request", loginRequest);

                        // Prepare ERROR token with the reason
                        TDSErrorToken errorToken = new TDSErrorToken(1919, 1, 14, string.Format("Received packet size \"{0}\", expected \"{1}\"", loginRequest.PacketSize, ServerArguments.PacketSizeFilter.Value), Arguments.ServerName);

                        // Log response
                        TDSUtilities.Log(Arguments.Log, "Response", errorToken);

                        // Serialize the errorToken token into the response packet
                        TDSMessage responseMessage = new TDSMessage(TDSMessageType.Response, errorToken);

                        // Prepare ERROR token with the final errorToken
                        errorToken = new TDSErrorToken(1919, 1, 14, "Connection is denied by packet size filter", Arguments.ServerName);

                        // Log response
                        TDSUtilities.Log(Arguments.Log, "Response", errorToken);

                        // Serialize the errorToken token into the response packet
                        responseMessage.Add(errorToken);

                        // Create DONE token
                        TDSDoneToken doneToken = new TDSDoneToken(TDSDoneTokenStatusType.Final | TDSDoneTokenStatusType.Error);

                        // Log response
                        TDSUtilities.Log(Arguments.Log, "Response", doneToken);

                        // Serialize DONE token into the response packet
                        responseMessage.Add(doneToken);

                        // Return only a single message with the collection
                        return new TDSMessageCollection(responseMessage);
                    }
                }

                // If we have an application name filter
                if (ServerArguments.ApplicationNameFilter != null)
                {
                    // If we are supposed to block this connection attempt
                    if (loginRequest.ApplicationName.Equals(ServerArguments.ApplicationNameFilter, System.StringComparison.OrdinalIgnoreCase))
                    {
                        // Log request to which we're about to send a failure
                        TDSUtilities.Log(Arguments.Log, "Request", loginRequest);

                        // Prepare ERROR token with the denial details
                        TDSErrorToken errorToken = new TDSErrorToken(18456, 1, 14, "Received application name: " + loginRequest.ApplicationName, Arguments.ServerName);

                        // Log response
                        TDSUtilities.Log(Arguments.Log, "Response", errorToken);

                        // Serialize the error token into the response packet
                        TDSMessage responseMessage = new TDSMessage(TDSMessageType.Response, errorToken);

                        // Prepare ERROR token for the final decision
                        errorToken = new TDSErrorToken(18456, 1, 14, "Connection is denied by application name filter", Arguments.ServerName);

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

                        // Put a single message into the collection and return it
                        return new TDSMessageCollection(responseMessage);
                    }
                }
            }

            // Return login response from the base class
            return base.OnLogin7Request(session, request);
        }
    }
}
