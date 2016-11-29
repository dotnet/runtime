// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.SqlServer.TDS.Login7;
using Microsoft.SqlServer.TDS.PreLogin;
using Microsoft.SqlServer.TDS.SQLBatch;
using Microsoft.SqlServer.TDS.SSPI;
using Microsoft.SqlServer.TDS.Authentication;

namespace Microsoft.SqlServer.TDS
{
    /// <summary>
    /// A single message that consists of a collection of TDS packets and represents a semantically complete and indivisible unit of information
    /// </summary>
    public class TDSMessage : List<TDSPacketToken>
    {
        /// <summary>
        /// Internal stream that contains the data
        /// </summary>
        private MemoryStream _dataStream;

        /// <summary>
        /// Type of the content being delivered
        /// </summary>
        public TDSMessageType MessageType { get; private set; }

        /// <summary>
        /// Collection of packet statuses
        /// </summary>
        public List<TDSPacketStatus> PacketStatuses { get; private set; }

        /// <summary>
        /// Check if we have seen Reset Connection request
        /// </summary>
        public bool IsResetConnectionRequestSet
        {
            get
            {
                return PacketStatuses.Count > 0 && (PacketStatuses[0] & TDSPacketStatus.ResetConnection) != 0;
            }
        }

        /// <summary>
        /// Check if we have seen Reset Connection Skip Transaction request
        /// </summary>
        public bool IsResetConnectionSkipTransactionRequestSet
        {
            get
            {
                return PacketStatuses.Count > 0 && (PacketStatuses[0] & TDSPacketStatus.ResetConnectionSkipTransaction) != 0;
            }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public TDSMessage()
        {
            // Initialize PacketStatuses collection
            PacketStatuses = new List<TDSPacketStatus>();
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSMessage(TDSMessageType type) :
            this()
        {
            // Save type
            MessageType = type;
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSMessage(TDSMessageType type, params TDSPacketToken[] tokens) :
            this(type)
        {
            AddRange(tokens);
        }

        /// <summary>
        /// Inflate the message from protocol-aware stream on the server
        /// </summary>
        /// <param name="stream">Source to inflate the message</param>
        public bool InflateClientRequest(TDSStream stream)
        {
            // Chck if data stream is fully inflated
            if (!_InflateDataStream(stream))
            {
                return false;
            }

            // Inflate all tokens in the stream
            _InflateClientTokens();

            // If we reached this point it means that both data and tokens are inflated
            return true;
        }

        /// <summary>
        /// Inflate the message from protocol-aware stream on the client side
        /// </summary>
        /// <param name="clientState">State of the client parser</param>
        /// <param name="stream">Source to inflate the message</param>
        public bool InflateServerResponse(TDSClientState clientState, TDSStream stream)
        {
            // Chck if data stream is fully inflated
            if (!_InflateDataStream(stream))
            {
                return false;
            }

            // Inflate all tokens in the stream
            _InflateServerTokens(clientState);

            // If we reached this point it means that both data and tokens are inflated
            return true;
        }

        /// <summary>
        /// Protocol-aware deflation routine
        /// </summary>
        /// <param name="stream">Destination to deflate the message</param>
        public void Deflate(TDSStream stream)
        {
            // Start a message on the stream
            stream.StartMessage(MessageType);

            // Iterate through each token and deflate it
            foreach (TDSPacketToken token in this)
            {
                // Deflate token into the data stream
                token.Deflate(stream);
            }

            // Complete the message
            stream.EndMessage();
        }

        /// <summary>
        /// Inflates the stream of data in this message
        /// </summary>
        /// <returns></returns>
        private bool _InflateDataStream(TDSStream stream)
        {
            // Indicates that end-of-message marker was reached
            bool isEndOfMessageReached = false;

            // Continue inflating packets as long as there's data in the stream or we've not reached the end of the message
            while (!isEndOfMessageReached)
            {
                // Chunk of data still available in the current packet
                int packetDataLeft = 0;

                // Check if we have a packet header
                if (stream.IncomingPacketHeader != null)
                {
                    // Calculate the chunk of remaining data
                    packetDataLeft = stream.IncomingPacketHeader.Length - stream.IncomingPacketPosition;
                }

                // Check if we have data to work with
                if (packetDataLeft == 0)
                {
                    // Position stream on the next packet header and read it
                    if (!stream.ReadNextHeader())
                    {
                        // We couldn't inflate the next packet header
                        break;
                    }

                    // Record packet status 
                    PacketStatuses.Add(stream.IncomingPacketHeader.Status);

                    // Use the packet header to establish message type
                    MessageType = stream.IncomingPacketHeader.Type;

                    // Recalculate the chunk of remaining data
                    packetDataLeft = stream.IncomingPacketHeader.Length - stream.IncomingPacketPosition;
                }

                // Allocate a buffer to read the data into the buffer
                byte[] packetDataBuffer = new byte[packetDataLeft];

                // Read the data into the buffer
                int packetDataRead = stream.Read(packetDataBuffer, 0, packetDataLeft);

                // Check if we have a data stream
                if (_dataStream == null)
                {
                    // Allocate a new data stream
                    _dataStream = new MemoryStream();
                }

                // Write the data into the message stream
                _dataStream.Write(packetDataBuffer, 0, packetDataRead);

                // Check if the whole packet was read
                if (packetDataRead < packetDataLeft)
                {
                    // We don't have enough data to inflate the whole message
                    break;
                }

                // Check if inflation succeded
                isEndOfMessageReached = (stream.IncomingPacketHeader.Status & TDSPacketStatus.EndOfMessage) != 0;
            }

            // Tell caller whether we reached the end of message
            return isEndOfMessageReached;
        }

        /// <summary>
        /// Traverse the input stream and inflate tokens that server sends to the client
        /// </summary>
        private void _InflateServerTokens(TDSClientState clientState)
        {
            // Check if we have a data stream
            if (_dataStream == null)
            {
                // Nothing to inflate
                return;
            }

            // Position to the beginning on the stream
            _dataStream.Seek(0, SeekOrigin.Begin);

            // Check client state
            switch (clientState)
            {
                case TDSClientState.PreLoginSent:
                    {
                        // Create a pre-login token
                        Add(new TDSPreLoginToken(_dataStream));
                        break;
                    }
                default:
                    {
                        // Check message type
                        switch (MessageType)
                        {
                            case TDSMessageType.Response:
                                {
                                    // Server responses must be inflated using token factory
                                    AddRange(TDSTokenFactory.Create(_dataStream));
                                    break;
                                }
                            default:
                                {
                                    // We don't recognize this message type
                                    throw new NotImplementedException(string.Format("Inflation for {0} TDS message is not implemented", MessageType));
                                }
                        }
                        break;
                    }
            }

            // Inflation is complete so we should release the stream
            _dataStream.Dispose();

            // Make sure we're not going to reuse it
            _dataStream = null;
        }

        /// <summary>
        /// Traverse the input stream and inflate tokens that client sends to the server
        /// </summary>
        private void _InflateClientTokens()
        {
            // Check if we have a data stream
            if (_dataStream == null)
            {
                // Nothing to inflate
                return;
            }

            // Position to the beginning on the stream
            _dataStream.Seek(0, SeekOrigin.Begin);

            // Check message type
            switch (MessageType)
            {
                case TDSMessageType.PreLogin:
                    {
                        // Create a pre-login token
                        Add(new TDSPreLoginToken(_dataStream));
                        break;
                    }
                case TDSMessageType.TDS7Login:
                    {
                        // Create and inflate login token
                        Add(new TDSLogin7Token(_dataStream));
                        break;
                    }
                case TDSMessageType.SSPI:
                    {
                        // Create a client-originated SSPI token
                        Add(new TDSSSPIClientToken(_dataStream, (int)_dataStream.Length));
                        break;
                    }
                case TDSMessageType.SQLBatch:
                    {
                        // Create and inflate SQL batch token
                        Add(new TDSSQLBatchToken(_dataStream));
                        break;
                    }
                case TDSMessageType.Attention:
                    {
                        // Do nothing
                        break;
                    }
                case TDSMessageType.FederatedAuthenticationToken:
                    {
                        Add(new TDSFedAuthToken(_dataStream));
                        break;
                    }
                default:
                    {
                        // We don't recognize this message type
                        throw new NotImplementedException(string.Format("Inflation for {0} TDS message is not implemented", MessageType));
                    }
            }

            // Inflation is complete so we should release the stream
            _dataStream.Dispose();

            // Make sure we're not going to reuse it
            _dataStream = null;
        }
    }
}
