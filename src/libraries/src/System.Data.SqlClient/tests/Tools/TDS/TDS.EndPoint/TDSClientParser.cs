// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace Microsoft.SqlServer.TDS.EndPoint
{
    /// <summary>
    /// Parser of the TDS packets on the client
    /// </summary>
    public class TDSClientParser : TDSParser
    {
        /// <summary>
        /// TDS client that generates data
        /// </summary>
        private ITDSClient Client { get; set; }

        /// <summary>
        /// Client TDS parser initialization constructor
        /// </summary>
        public TDSClientParser(ITDSClient client, Stream transport) :
            base(transport)
        {
            // Save the client
            Client = client;

            // Initialize protocol stream
            Transport.PacketSize = client.Context.PacketSize;
        }

        /// <summary>
        /// Authenticate against TDS Server
        /// </summary>
        public void Login()
        {
            // Loop until parser enters logged-in state
            while (Client.State != TDSClientState.LoggedIn && Client.State != TDSClientState.ReConnect)
            {
                // Prepare request response container
                TDSMessage requestMessage = null;

                // Check the state of the client
                switch (Client.State)
                {
                    case TDSClientState.PreLoginSent:
                        {
                            // Read pre-login response
                            TDSMessage responseMessage = _ReadResponse();

                            // Call into the subscriber to process the server response and generate subsequent request
                            requestMessage = Client.OnPreLoginResponse(responseMessage);

                            // Check the client context for encryption state
                            if (Client.Context.Encryption != TDSEncryptionType.Off)
                            {
                                // Turn on encryption before sending the response								
                                EnableClientTransportEncryption(Client.Context.ServerHost);
                            }

                            break;
                        }

                    case TDSClientState.Login7FederatedAuthenticationInformationRequestSent:
                        {
                            // Read FedAuthInfoToken response
                            TDSMessage responseMessage = _ReadResponse();

                            // Generate the FedAuthTokenMessage
                            requestMessage = Client.OnFedAuthInfoTokenResponse(responseMessage);
                            break;
                        }

                    case TDSClientState.CompleteLogin7Sent:
                        {
                            // Read login response
                            TDSMessage responseMessage = _ReadResponse();

                            // Call into the subscriber to process the server response and generate subsequent request
                            Client.OnLoginResponse(responseMessage);
                            break;
                        }
                    case TDSClientState.Login7SPNEGOSent:
                        {
                            // Read SPNEGO response
                            TDSMessage responseMessage = _ReadResponse();

                            // Call into the subscriber to process the server response and generate subsequent request
                            requestMessage = Client.OnSSPIResponse(responseMessage);
                            break;
                        }

                    case TDSClientState.LoggedIn:
                    case TDSClientState.ReConnect:
                        {
                            // We reached the target state
                            requestMessage = null;
                            break;
                        }
                    default:
                        {
                            // Initiate conversation with TDS Server
                            requestMessage = Client.OnPreLogin();
                            break;
                        }
                }

                // Send a request to the server
                _WriteRequest(requestMessage);

                // Check if we need to turn off encryption at this point
                if (Client.Context.Encryption == TDSEncryptionType.LoginOnly)
                {
                    // Verify client state
                    if (Client.State == TDSClientState.CompleteLogin7Sent || Client.State == TDSClientState.Login7SPNEGOSent)
                    {
                        // Turn off encryption before reading the response
                        DisableTransportEncryption();
                    }
                }
            }
        }

        /// <summary>
        /// Dispatch a request to the server and process the response
        /// </summary>
        public void SendRequest()
        {
            // Obtain the request message from the client parser
            TDSMessage requestMessage = Client.OnRequest();

            // Send a request to the server
            _WriteRequest(requestMessage);

            // Check if request message is available
            if (requestMessage != null)
            {
                // Read response
                TDSMessage responseMessage = _ReadResponse();

                // Process the response with the client parser
                Client.OnResponse(responseMessage);

                // Log event
                Log("Client entered a state \"{0}\"", Client.State);
            }
        }

        /// <summary>
        /// Complete 
        /// </summary>
        public void Logout()
        {
            // Loop until parser enters final state
            while (Client.State != TDSClientState.Final)
            {
                // Prepare request response container
                TDSMessage requestMessage = null;

                // Check the state of the client
                switch (Client.State)
                {
                    case TDSClientState.LogoutSent:
                        {
                            // Read logout response
                            TDSMessage responseMessage = _ReadResponse();

                            // Process logout response
                            Client.OnLogoutResponse(responseMessage);
                            break;
                        }
                    default:
                        {
                            // Request logout notification
                            requestMessage = Client.OnLogout();
                            break;
                        }
                }

                // Send a request to the server
                _WriteRequest(requestMessage);
            }

            // Turn off transport encryption in case it was turned on so that subsequent login can be handled properly
            DisableTransportEncryption();
        }

        /// <summary>
        /// Read data from TDS server
        /// </summary>
        /// <returns></returns>
        private TDSMessage _ReadResponse()
        {
            // Prepare response container
            TDSMessage responseMessage = new TDSMessage();

            // Loop as long as it takes to receive the entire message
            while (!responseMessage.InflateServerResponse(Client.State, Transport))
            {
                // Switch context to avoid CPU overloading
                System.Threading.Thread.Sleep(1);
            }

            // Log event
            Log("Recieved a response to \"{0}\"", Client.State);

            // Return what we inflated
            return responseMessage;
        }

        /// <summary>
        /// Send a request to the TDS server
        /// </summary>
        private void _WriteRequest(TDSMessage requestMessage)
        {
            // Check if TDS packet size changed
            if (Client.Context.PacketSize != Transport.PacketSize)
            {
                // Update packet size
                Transport.PacketSize = Client.Context.PacketSize;
            }

            // Check if there is  request to send
            if (requestMessage != null)
            {
                // Dispatch the request to the server
                requestMessage.Deflate(Transport);

                // Log event
                Log("Sent a request and entered \"{0}\"", Client.State);
            }
            else
            {
                // Log event
                Log("Client entered \"{0}\"", Client.State);
            }
        }
    }
}
