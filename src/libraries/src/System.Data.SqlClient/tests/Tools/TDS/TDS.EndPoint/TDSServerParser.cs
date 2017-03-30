// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

namespace Microsoft.SqlServer.TDS.EndPoint
{
    /// <summary>
    /// Parser of the TDS packets on the client
    /// </summary>
    public class TDSServerParser : TDSParser
    {
        /// <summary>
        /// Implementation that provides data to be delivered over the protocol
        /// </summary>
        private ITDSServer Server { get; set; }

        /// <summary>
        /// TDS server session
        /// </summary>
        private ITDSServerSession Session { get; set; }

        /// <summary>
        /// Communication from the client being received incrementally
        /// </summary>
        private TDSMessage MessageBeingReceived { get; set; }

        /// <summary>
        /// Server TDS parser initialization constructor
        /// </summary>
        public TDSServerParser(ITDSServer server, ITDSServerSession session, Stream stream) :
            base(stream)
        {
            // Save TDS server
            Server = server;

            // Save session
            Session = session;

            // Configure TDS server
            Transport.PacketSize = session.PacketSize;
            Transport.OutgoingSessionID = (ushort)session.SessionID;
        }

        /// <summary>
        /// Run one cycle of the parser to process incoming stream of data or dispatch outgoing data
        /// </summary>
        public void Run()
        {
            // Check if there's a message being inflated
            if (MessageBeingReceived == null)
            {
                // Create a new message
                MessageBeingReceived = new TDSMessage();
            }

            // Inflate the message using incoming stream
            // This call will use only as much data is it needs and leave everything else on the stream
            if (MessageBeingReceived.InflateClientRequest(Transport))
            {
                // Call into the server logics to process the message and generate the response
                TDSMessageCollection responseMessages = null;

                // Check the type of the packet
                switch (MessageBeingReceived.MessageType)
                {
                    case TDSMessageType.PreLogin:
                        {
                            // Call into the subscriber to process the packet
                            responseMessages = Server.OnPreLoginRequest(Session, MessageBeingReceived);
                            break;
                        }
                    case TDSMessageType.TDS7Login:
                        {
                            // Call into the subscriber to process the packet
                            responseMessages = Server.OnLogin7Request(Session, MessageBeingReceived);

                            // Check if encryption needs to be turned off
                            if (Session.Encryption == TDSEncryptionType.LoginOnly)
                            {
                                // Disable transport encryption
                                DisableTransportEncryption();
                            }

                            break;
                        }
                    case TDSMessageType.SSPI:
                        {
                            // Call into the subscriber to process the packet
                            responseMessages = Server.OnSSPIRequest(Session, MessageBeingReceived);
                            break;
                        }
                    case TDSMessageType.SQLBatch:
                        {
                            // Call into the subscriber to process the packet
                            responseMessages = Server.OnSQLBatchRequest(Session, MessageBeingReceived);
                            break;
                        }
                    case TDSMessageType.Attention:
                        {
                            // Call into the subscriber to process the packet
                            responseMessages = Server.OnAttention(Session, MessageBeingReceived);
                            break;
                        }
                    case TDSMessageType.FederatedAuthenticationToken:
                        {
                            // Call into the subscriber to process the packet
                            responseMessages = Server.OnFederatedAuthenticationTokenMessage(Session, MessageBeingReceived);
                            break;
                        }
                    default:
                        {
                            // New code is needed to process this message
                            throw new NotImplementedException(string.Format("Handler of TDS message \"{0}\" is not implemented", MessageBeingReceived.MessageType));
                        }
                }

                // Check if TDS packet size changed
                if (Session.PacketSize != Transport.PacketSize)
                {
                    // Update packet size
                    Transport.PacketSize = Session.PacketSize;
                }

                // Send all messages to the client
                responseMessages.Deflate(Transport);

                // Check the type of message just sent
                if (MessageBeingReceived.MessageType == TDSMessageType.PreLogin)
                {
                    // Check encryption settings on the session
                    if (Session.Encryption == TDSEncryptionType.LoginOnly || Session.Encryption == TDSEncryptionType.Full)
                    {
                        // Enable server side encryption
                        EnableServerTransportEncryption(Session.EncryptionCertificate);
                    }
                }

                // Reset the current message because it's complete
                // It would also trigger creation of the next message during the next cycle
                MessageBeingReceived = null;
            }
        }
    }
}
