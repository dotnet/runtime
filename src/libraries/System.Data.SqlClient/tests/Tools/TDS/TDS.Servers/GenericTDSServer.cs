// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Security.Cryptography;

using Microsoft.SqlServer.TDS.Done;
using Microsoft.SqlServer.TDS.EndPoint;
using Microsoft.SqlServer.TDS.EndPoint.SSPI;
using Microsoft.SqlServer.TDS.EndPoint.FederatedAuthentication;
using Microsoft.SqlServer.TDS.EnvChange;
using Microsoft.SqlServer.TDS.Error;
using Microsoft.SqlServer.TDS.FeatureExtAck;
using Microsoft.SqlServer.TDS.Info;
using Microsoft.SqlServer.TDS.Login7;
using Microsoft.SqlServer.TDS.LoginAck;
using Microsoft.SqlServer.TDS.PreLogin;
using Microsoft.SqlServer.TDS.SSPI;
using Microsoft.SqlServer.TDS.Authentication;

namespace Microsoft.SqlServer.TDS.Servers
{
    /// <summary>
    /// Generic TDS server without specialization
    /// </summary>
    public class GenericTDSServer : ITDSServer
    {
        /// <summary>
        /// Session counter
        /// </summary>
        private int _sessionCount = 0;

        /// <summary>
        /// Server configuration
        /// </summary>
        protected TDSServerArguments Arguments { get; set; }

        /// <summary>
        /// Query engine instance
        /// </summary>
        protected QueryEngine Engine { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public GenericTDSServer() :
            this(new TDSServerArguments())
        {
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public GenericTDSServer(TDSServerArguments arguments) :
            this(arguments, new QueryEngine(arguments))
        {
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public GenericTDSServer(TDSServerArguments arguments, QueryEngine queryEngine)
        {
            // Save arguments
            Arguments = arguments;

            // Save "relational" query engine
            Engine = queryEngine;

            // Configure log for the query engine
            Engine.Log = Arguments.Log;
        }

        /// <summary>
        /// Create a new session on the server
        /// </summary>
        /// <returns>A new instance of the session</returns>
        public virtual ITDSServerSession OpenSession()
        {
            // Atomically increment the session counter
            Interlocked.Increment(ref _sessionCount);

            // Create a new session
            GenericTDSServerSession session = new GenericTDSServerSession(this, (uint)_sessionCount);

            // Use configured encryption certificate
            session.EncryptionCertificate = Arguments.EncryptionCertificate;

            return session;
        }

        /// <summary>
        /// Notify server of the session termination
        /// </summary>
        public virtual void CloseSession(ITDSServerSession session)
        {
            // Do nothing
        }

        /// <summary>
        /// Handler for pre-login request
        /// </summary>
        public virtual TDSMessageCollection OnPreLoginRequest(ITDSServerSession session, TDSMessage request)
        {
            // Inflate pre-login request from the message
            TDSPreLoginToken preLoginRequest = request[0] as TDSPreLoginToken;

            // Log request
            TDSUtilities.Log(Arguments.Log, "Request", preLoginRequest);

            // Generate server response for encryption
            TDSPreLoginTokenEncryptionType serverResponse = TDSUtilities.GetEncryptionResponse(preLoginRequest.Encryption, Arguments.Encryption);

            // Update client state with encryption resolution
            session.Encryption = TDSUtilities.ResolveEncryption(preLoginRequest.Encryption, serverResponse);

            // Create TDS prelogin packet
            TDSPreLoginToken preLoginToken = new TDSPreLoginToken(Arguments.ServerVersion, serverResponse, false); // TDS server doesn't support MARS

            // Cache the recieved Nonce into the session
            (session as GenericTDSServerSession).ClientNonce = preLoginRequest.Nonce;

            // Check if the server has been started up as requiring FedAuth when choosing between SSPI and FedAuth
            if (Arguments.FedAuthRequiredPreLoginOption == TdsPreLoginFedAuthRequiredOption.FedAuthRequired)
            {
                if (preLoginRequest.FedAuthRequired == TdsPreLoginFedAuthRequiredOption.FedAuthRequired)
                {
                    // Set the FedAuthRequired option
                    preLoginToken.FedAuthRequired = TdsPreLoginFedAuthRequiredOption.FedAuthRequired;
                }

                // Keep the federated authentication required flag in the server session
                (session as GenericTDSServerSession).FedAuthRequiredPreLoginServerResponse = preLoginToken.FedAuthRequired;

                if (preLoginRequest.Nonce != null)
                {
                    // Generate Server Nonce
                    preLoginToken.Nonce = _GenerateRandomBytes(32);
                }
            }

            // Cache the server Nonce in a session
            (session as GenericTDSServerSession).ServerNonce = preLoginToken.Nonce;

            // Log response
            TDSUtilities.Log(Arguments.Log, "Response", preLoginToken);

            // Reset authentication information
            session.SQLUserID = null;
            session.NTUserAuthenticationContext = null;

            // Respond with a single message that contains only one token
            return new TDSMessageCollection(new TDSMessage(TDSMessageType.Response, preLoginToken));
        }

        /// <summary>
        /// Handler for login request
        /// </summary>
        public virtual TDSMessageCollection OnLogin7Request(ITDSServerSession session, TDSMessage request)
        {
            // Inflate login7 request from the message
            TDSLogin7Token loginRequest = request[0] as TDSLogin7Token;

            // Log request
            TDSUtilities.Log(Arguments.Log, "Request", loginRequest);

            // Update server context
            session.Database = string.IsNullOrEmpty(loginRequest.Database) ? "master" : loginRequest.Database;

            // Resolve TDS version
            session.TDSVersion = TDSVersion.Resolve(TDSVersion.GetTDSVersion(Arguments.ServerVersion), loginRequest.TDSVersion);

            // Check for the TDS version
            TDSMessageCollection collection = CheckTDSVersion(session);

            // Check if any errors are posted
            if (collection != null)
            {
                // Version check needs to send own message hence we can't proceed
                return collection;
            }

            // Indicates federated authentication
            bool bIsFedAuthConnection = false;

            // Federated authentication option to be used later
            TDSLogin7FedAuthOptionToken federatedAuthenticationOption = null;

            // Check if feature extension block is available
            if (loginRequest.FeatureExt != null)
            {
                // Go over the feature extension data
                foreach (TDSLogin7FeatureOptionToken option in loginRequest.FeatureExt)
                {
                    // Check option type
                    switch (option.FeatureID)
                    {
                        case TDSFeatureID.SessionRecovery:
                            {
                                // Enable session recovery
                                session.IsSessionRecoveryEnabled = true;

                                // Cast to session state options
                                TDSLogin7SessionRecoveryOptionToken sessionStateOption = option as TDSLogin7SessionRecoveryOptionToken;

                                // Inflate session state
                                (session as GenericTDSServerSession).Inflate(sessionStateOption.Initial, sessionStateOption.Current);

                                break;
                            }
                        case TDSFeatureID.FederatedAuthentication:
                            {
                                // Cast to federated authentication option
                                federatedAuthenticationOption = option as TDSLogin7FedAuthOptionToken;

                                // Mark authentication as federated
                                bIsFedAuthConnection = true;

                                // Validate federated authentication option
                                collection = CheckFederatedAuthenticationOption(session, option as TDSLogin7FedAuthOptionToken);

                                if (collection != null)
                                {
                                    // Version error happened.
                                    return collection;
                                }

                                // Save the fed auth library to be used
                                (session as GenericTDSServerSession).FederatedAuthenticationLibrary = federatedAuthenticationOption.Library;

                                break;
                            }
                        default:
                            {
                                // Do nothing
                                break;
                            }
                    }
                }
            }

            // Check if SSPI authentication is requested
            if (loginRequest.OptionalFlags2.IntegratedSecurity == TDSLogin7OptionalFlags2IntSecurity.On)
            {
                // Delegate to SSPI authentication
                return ContinueSSPIAuthentication(session, loginRequest.SSPI);
            }

            // If it is not a FedAuth connection or the server has been started up as not supporting FedAuth, just ignore the FeatureExtension
            // Yes unfortunately for the fake server, supporting FedAuth = Requiring FedAuth
            if (!bIsFedAuthConnection
                || Arguments.FedAuthRequiredPreLoginOption == TdsPreLoginFedAuthRequiredOption.FedAuthNotRequired)
            {
                // We use SQL authentication
                session.SQLUserID = loginRequest.UserID;

                // Process with the SQL login.
                return OnSqlAuthenticationCompleted(session);
            }
            else
            {
                // Fedauth feature extension is present and server has been started up as Requiring (or Supporting) FedAuth
                if (federatedAuthenticationOption.IsRequestingAuthenticationInfo)
                {
                    // Must provide client with more info before completing authentication
                    return OnFederatedAuthenticationInfoRequest(session);
                }
                else
                {
                    return OnFederatedAuthenticationCompleted(session, federatedAuthenticationOption.Token);
                }
            }
        }

        public virtual TDSMessageCollection OnFederatedAuthenticationTokenMessage(ITDSServerSession session, TDSMessage message)
        {
            // Get the FedAuthToken
            TDSFedAuthToken fedauthToken = message[0] as TDSFedAuthToken;

            // Log
            TDSUtilities.Log(Arguments.Log, "Request", fedauthToken);

            return OnFederatedAuthenticationCompleted(session, fedauthToken.Token);
        }

        /// <summary>
        /// Handler for SSPI request
        /// </summary>
        public virtual TDSMessageCollection OnSSPIRequest(ITDSServerSession session, TDSMessage request)
        {
            // Get the SSPI token
            TDSSSPIClientToken sspiRequest = request[0] as TDSSSPIClientToken;

            // Log request
            TDSUtilities.Log(Arguments.Log, "Request", sspiRequest.Payload);

            // Delegate to SSPI routine
            return ContinueSSPIAuthentication(session, sspiRequest.Payload);
        }

        /// <summary>
        /// It is called when SQL batch request arrives
        /// </summary>
        public virtual TDSMessageCollection OnSQLBatchRequest(ITDSServerSession session, TDSMessage message)
        {
            // Delegate to the query engine
            TDSMessageCollection responseMessage = Engine.ExecuteBatch(session, message);

            // Check if session packet size is different than the engine packet size
            if (session.PacketSize != Arguments.PacketSize)
            {
                // Get the first message
                TDSMessage firstMessage = responseMessage[0];

                // Find DONE token in it
                int indexOfDone = firstMessage.IndexOf(firstMessage.Where(t => t is TDSDoneToken).First());

                // Create new packet size environment change token
                TDSEnvChangeToken envChange = new TDSEnvChangeToken(TDSEnvChangeTokenType.PacketSize, Arguments.PacketSize.ToString(), session.PacketSize.ToString());

                // Log response
                TDSUtilities.Log(Arguments.Log, "Response", envChange);

                // Insert env change before done token
                firstMessage.Insert(indexOfDone, envChange);

                // Update session with the new packet size
                session.PacketSize = (uint)Arguments.PacketSize;
            }

            return responseMessage;
        }

        /// <summary>
        /// It is called when attention arrives
        /// </summary>
        public virtual TDSMessageCollection OnAttention(ITDSServerSession session, TDSMessage message)
        {
            // Delegate into the query engine
            return Engine.ExecuteAttention(session, message);
        }

        /// <summary>
        /// Advances one step in SSPI authentication sequence
        /// </summary>
        protected virtual TDSMessageCollection ContinueSSPIAuthentication(ITDSServerSession session, byte[] payload)
        {
            // Response to send to the client
            SSPIResponse response;

            try
            {
                // Check if we have an SSPI context
                if (session.NTUserAuthenticationContext == null)
                {
                    // This is the first step so we need to create a server context and initialize it
                    session.NTUserAuthenticationContext = SSPIContext.CreateServer();

                    // Run the first step of authentication
                    response = session.NTUserAuthenticationContext.StartServerAuthentication(payload);
                }
                else
                {
                    // Process SSPI request from the client
                    response = session.NTUserAuthenticationContext.ContinueServerAuthentication(payload);
                }
            }
            catch (Exception e)
            {
                // Prepare ERROR token with the reason
                TDSErrorToken errorToken = new TDSErrorToken(12345, 1, 15, "Failed to accept security SSPI context", Arguments.ServerName);

                // Log response
                TDSUtilities.Log(Arguments.Log, "Response", errorToken);

                // Serialize the error token into the response packet
                TDSMessage responseErrorMessage = new TDSMessage(TDSMessageType.Response, errorToken);

                // Prepare ERROR token with the final errorToken
                errorToken = new TDSErrorToken(12345, 1, 15, e.Message, Arguments.ServerName);

                // Log response
                TDSUtilities.Log(Arguments.Log, "Response", errorToken);

                // Serialize the error token into the response packet
                responseErrorMessage.Add(errorToken);

                // Create DONE token
                TDSDoneToken doneToken = new TDSDoneToken(TDSDoneTokenStatusType.Final | TDSDoneTokenStatusType.Error);

                // Log response
                TDSUtilities.Log(Arguments.Log, "Response", doneToken);

                // Serialize DONE token into the response packet
                responseErrorMessage.Add(doneToken);

                // Respond with a single message
                return new TDSMessageCollection(responseErrorMessage);
            }

            // Message collection to respond with
            TDSMessageCollection responseMessages = new TDSMessageCollection();

            // Check if there's a response
            if (response != null)
            {
                // Check if there's a payload
                if (response.Payload != null)
                {
                    // Create SSPI token
                    TDSSSPIToken sspiToken = new TDSSSPIToken(response.Payload);

                    // Log response
                    TDSUtilities.Log(Arguments.Log, "Response", sspiToken);

                    // Prepare response message with a single response token
                    responseMessages.Add(new TDSMessage(TDSMessageType.Response, sspiToken));

                    // Check if we can complete login
                    if (!response.IsFinal)
                    {
                        // Send the message to the client
                        return responseMessages;
                    }
                }
            }

            // Reset SQL user identifier since we're using NT authentication
            session.SQLUserID = null;

            // Append successfully authentication response
            responseMessages.AddRange(OnAuthenticationCompleted(session));

            // Return the message with SSPI token
            return responseMessages;
        }

        protected virtual TDSMessageCollection OnFederatedAuthenticationInfoRequest(ITDSServerSession session)
        {
            TDSFedAuthInfoToken infoToken = new TDSFedAuthInfoToken();

            // Make fake info options
            TDSFedAuthInfoOptionSPN spn = new TDSFedAuthInfoOptionSPN(Arguments.ServerPrincipalName);
            TDSFedAuthInfoOptionSTSURL stsurl = new TDSFedAuthInfoOptionSTSURL(Arguments.StsUrl);
            infoToken.Options.Add(0, spn);
            infoToken.Options.Add(1, stsurl);

            // Log response
            TDSUtilities.Log(Arguments.Log, "Response", spn);
            TDSUtilities.Log(Arguments.Log, "Response", stsurl);

            TDSMessage infoMessage = new TDSMessage(TDSMessageType.FederatedAuthenticationInfo, infoToken);
            return new TDSMessageCollection(infoMessage);
        }

        /// <summary>
        /// Called by OnLogin7Request when client is using SQL auth. Overridden by subclasses to easily detect when SQL auth is used.
        /// </summary>
        protected virtual TDSMessageCollection OnSqlAuthenticationCompleted(ITDSServerSession session)
        {
            return OnAuthenticationCompleted(session);
        }

        protected virtual TDSMessageCollection OnAuthenticationCompleted(ITDSServerSession session)
        {
            // Create new database environment change token
            TDSEnvChangeToken envChange = new TDSEnvChangeToken(TDSEnvChangeTokenType.Database, session.Database, "master");

            // Log response
            TDSUtilities.Log(Arguments.Log, "Response", envChange);

            // Serialize the login token into the response packet
            TDSMessage responseMessage = new TDSMessage(TDSMessageType.Response, envChange);

            // Create information token on the change
            TDSInfoToken infoToken = new TDSInfoToken(5701, 2, 0, string.Format("Changed database context to '{0}'", envChange.NewValue), Arguments.ServerName);

            // Log response
            TDSUtilities.Log(Arguments.Log, "Response", infoToken);

            // Serialize the login token into the response packet
            responseMessage.Add(infoToken);

            // Create new collation change token
            envChange = new TDSEnvChangeToken(TDSEnvChangeTokenType.SQLCollation, (session as GenericTDSServerSession).Collation);

            // Log response
            TDSUtilities.Log(Arguments.Log, "Response", envChange);

            // Serialize the login token into the response packet
            responseMessage.Add(envChange);

            // Create new language change token
            envChange = new TDSEnvChangeToken(TDSEnvChangeTokenType.Language, LanguageString.ToString((session as GenericTDSServerSession).Language));

            // Log response
            TDSUtilities.Log(Arguments.Log, "Response", envChange);

            // Serialize the login token into the response packet
            responseMessage.Add(envChange);

            // Create information token on the change
            infoToken = new TDSInfoToken(5703, 1, 0, string.Format("Changed language setting to {0}", envChange.NewValue), Arguments.ServerName);

            // Log response
            TDSUtilities.Log(Arguments.Log, "Response", infoToken);

            // Serialize the login token into the response packet
            responseMessage.Add(infoToken);

            // Create new packet size environment change token
            envChange = new TDSEnvChangeToken(TDSEnvChangeTokenType.PacketSize, Arguments.PacketSize.ToString(), Arguments.PacketSize.ToString());

            // Log response
            TDSUtilities.Log(Arguments.Log, "Response", envChange);

            // Serialize the login token into the response packet
            responseMessage.Add(envChange);

            // Update session packet size
            session.PacketSize = (uint)Arguments.PacketSize;

            // Create login acknowledgement packet
            TDSLoginAckToken loginResponseToken = new TDSLoginAckToken(Arguments.ServerVersion, session.TDSVersion, TDSLogin7TypeFlagsSQL.SQL, "Microsoft SQL Server");  // Otherwise SNAC yields E_FAIL

            // Log response
            TDSUtilities.Log(Arguments.Log, "Response", loginResponseToken);

            // Serialize the login token into the response packet
            responseMessage.Add(loginResponseToken);

            // Check if session recovery is enabled
            if (session.IsSessionRecoveryEnabled)
            {
                // Create Feature extension Ack token
                TDSFeatureExtAckToken featureExtActToken = new TDSFeatureExtAckToken(new TDSFeatureExtAckSessionStateOption((session as GenericTDSServerSession).Deflate()));

                // Log response
                TDSUtilities.Log(Arguments.Log, "Response", featureExtActToken);

                // Serialize feature extnesion token into the response
                responseMessage.Add(featureExtActToken);
            }

            // Create DONE token
            TDSDoneToken doneToken = new TDSDoneToken(TDSDoneTokenStatusType.Final);

            // Log response
            TDSUtilities.Log(Arguments.Log, "Response", doneToken);

            // Serialize DONE token into the response packet
            responseMessage.Add(doneToken);

            // Wrap a single message in a collection
            return new TDSMessageCollection(responseMessage);
        }

        /// <summary>
        /// Complete the Federated Login
        /// </summary>
        /// <param name="session">Server session</param>
        /// <returns>Federated Login message collection</returns>
        protected virtual TDSMessageCollection OnFederatedAuthenticationCompleted(ITDSServerSession session, byte[] ticket)
        {
            // Delegate to successful authentication routine
            TDSMessageCollection responseMessageCollection = OnAuthenticationCompleted(session);

            // Get the last message
            TDSMessage targetMessage = responseMessageCollection.Last();

            IFederatedAuthenticationTicket decryptedTicket = null;

            try
            {
                // Get the Federated Authentication ticket using RPS
                decryptedTicket = FederatedAuthenticationTicketService.DecryptTicket((session as GenericTDSServerSession).FederatedAuthenticationLibrary, ticket);

                if (decryptedTicket is RpsTicket)
                {
                    TDSUtilities.Log(Arguments.Log, "RPS ticket session key: ", (decryptedTicket as RpsTicket).sessionKey);
                }
                else if (decryptedTicket is JwtTicket)
                {
                    TDSUtilities.Log(Arguments.Log, "JWT Ticket Received", null);
                }
            }
            catch (Exception ex)
            {
                // Prepare ERROR token
                TDSErrorToken errorToken = new TDSErrorToken(54879, 1, 20, "Authentication error in Federated Authentication Ticket Service: " + ex.Message, Arguments.ServerName);

                // Log response
                TDSUtilities.Log(Arguments.Log, "Response", errorToken);

                // Create DONE token
                TDSDoneToken doneToken = new TDSDoneToken(TDSDoneTokenStatusType.Final | TDSDoneTokenStatusType.Error);

                // Log response
                TDSUtilities.Log(Arguments.Log, "Response", doneToken);

                // Return the message and stop processing request
                return new TDSMessageCollection(new TDSMessage(TDSMessageType.Response, errorToken, doneToken));
            }

            // Create federated authentication extension option
            TDSFeatureExtAckFederatedAuthenticationOption federatedAuthenticationOption;
            if ((session as GenericTDSServerSession).FederatedAuthenticationLibrary == TDSFedAuthLibraryType.ADAL)
            {
                // For the time being, fake fedauth tokens are used for ADAL, so decryptedTicket is null.
                federatedAuthenticationOption =
                    new TDSFeatureExtAckFederatedAuthenticationOption((session as GenericTDSServerSession).ClientNonce, null);
            }
            else
            {
                federatedAuthenticationOption =
                    new TDSFeatureExtAckFederatedAuthenticationOption((session as GenericTDSServerSession).ClientNonce,
                                                                       decryptedTicket.GetSignature((session as GenericTDSServerSession).ClientNonce));
            }

            // Look for feature extension token
            TDSFeatureExtAckToken featureExtActToken = (TDSFeatureExtAckToken)targetMessage.Where(t => t is TDSFeatureExtAckToken).FirstOrDefault();

            // Check if response already contains federated authentication
            if (featureExtActToken == null)
            {
                // Create Feature extension Ack token
                featureExtActToken = new TDSFeatureExtAckToken(federatedAuthenticationOption);

                // Serialize feature extension token into the response
                // The last token is Done token, so we should put feautureextack token before done token
                targetMessage.Insert(targetMessage.Count - 1, featureExtActToken);
            }
            else
            {
                // Update
                featureExtActToken.Options.Add(federatedAuthenticationOption);
            }

            // Log response
            TDSUtilities.Log(Arguments.Log, "Response", federatedAuthenticationOption);

            // Wrap a message with a collection
            return responseMessageCollection;
        }

        /// <summary>
        /// Ensure that federated authentication option is valid
        /// </summary>
        protected virtual TDSMessageCollection CheckFederatedAuthenticationOption(ITDSServerSession session, TDSLogin7FedAuthOptionToken federatedAuthenticationOption)
        {
            // Check if server's prelogin response for FedAuthRequired prelogin option is echoed back correctly in FedAuth Feature Extenion Echo
            if (federatedAuthenticationOption.Echo != (session as GenericTDSServerSession).FedAuthRequiredPreLoginServerResponse)
            {
                // Create Error message
                string message =
                    string.Format("FEDAUTHREQUIRED option in the prelogin response is not echoed back correctly: in prelogin response, it is {0} and in login, it is {1}: ",
                    (session as GenericTDSServerSession).FedAuthRequiredPreLoginServerResponse,
                    federatedAuthenticationOption.Echo);

                // Create errorToken token
                TDSErrorToken errorToken = new TDSErrorToken(3456, 34, 23, message);

                // Log response
                TDSUtilities.Log(Arguments.Log, "Response", errorToken);

                // Create DONE token
                TDSDoneToken doneToken = new TDSDoneToken(TDSDoneTokenStatusType.Final | TDSDoneTokenStatusType.Error);

                // Log response
                TDSUtilities.Log(Arguments.Log, "Response", doneToken);

                // Build a collection with a single message of two tokens
                return new TDSMessageCollection(new TDSMessage(TDSMessageType.Response, errorToken, doneToken));
            }

            // Check if the nonce exists
            if ((federatedAuthenticationOption.Nonce == null && federatedAuthenticationOption.Library == TDSFedAuthLibraryType.IDCRL)
                || !AreEqual((session as GenericTDSServerSession).ServerNonce, federatedAuthenticationOption.Nonce))
            {
                // Error message
                string message = string.Format("Unexpected NONCEOPT specified in the Federated authentication feature extension");

                // Create errorToken token
                TDSErrorToken errorToken = new TDSErrorToken(5672, 32, 87, message);

                // Log response
                TDSUtilities.Log(Arguments.Log, "Response", errorToken);

                // Create DONE token
                TDSDoneToken doneToken = new TDSDoneToken(TDSDoneTokenStatusType.Final | TDSDoneTokenStatusType.Error);

                // Log response
                TDSUtilities.Log(Arguments.Log, "Response", doneToken);

                // Build a collection with a single message of two tokens
                return new TDSMessageCollection(new TDSMessage(TDSMessageType.Response, errorToken, doneToken));
            }

            // We're good
            return null;
        }

        /// <summary>
        /// Checks the TDS version
        /// </summary>
        /// <param name="session">Server session</param>
        /// <returns>Null if the TDS version is supported, errorToken message otherwise</returns>
        protected virtual TDSMessageCollection CheckTDSVersion(ITDSServerSession session)
        {
            // Check if version is supported
            if (TDSVersion.IsSupported(session.TDSVersion))
            {
                return null;
            }

            // Prepare ERROR token
            TDSErrorToken errorToken = new TDSErrorToken(12345, 1, 16, "Unsupported TDS client version", Arguments.ServerName);

            // Log response
            TDSUtilities.Log(Arguments.Log, "Response", errorToken);

            // Create DONE token
            TDSDoneToken doneToken = new TDSDoneToken(TDSDoneTokenStatusType.Final | TDSDoneTokenStatusType.Error);

            // Log response
            TDSUtilities.Log(Arguments.Log, "Response", doneToken);

            // Wrap with message collection
            return new TDSMessageCollection(new TDSMessage(TDSMessageType.Response, errorToken, doneToken));
        }

        /// <summary>
        /// Generates random bytes
        /// </summary>
        /// <param name="count">The number of bytes to be generated.</param>
        /// <returns>Generated random bytes.</returns>
        private byte[] _GenerateRandomBytes(int count)
        {
            byte[] randomBytes = new byte[count];

            RNGCryptoServiceProvider gen = new RNGCryptoServiceProvider();
            // Generate bytes
            gen.GetBytes(randomBytes);

            return randomBytes;
        }

        /// <summary>
        /// Check if two byte arrays are equal
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        private bool AreEqual(byte[] left, byte[] right)
        {
            if (object.ReferenceEquals(left, right))
            {
                return true;
            }

            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            return left.SequenceEqual<byte>(right);
        }
    }
}
