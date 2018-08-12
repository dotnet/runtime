// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading;

namespace Microsoft.SqlServer.TDS
{
    /// <summary>
    /// Stream that wraps the data with TDS protocol
    /// </summary>
    public class TDSStream : Stream
    {
        /// <summary>
        /// Indicates whether inner stream should be closed when TDS stream is closed
        /// </summary>
        private bool _leaveInnerStreamOpen = false;

        /// <summary>
        /// Size of the packet
        /// </summary>
        private uint _packetSize;

        /// <summary>
        /// Header of the packet being processed
        /// </summary>
        private TDSPacketHeader OutgoingPacketHeader { get; set; }

        /// <summary>
        /// Cache of packet header and data
        /// </summary>
        private byte[] _outgoingPacket;

        /// <summary>
        /// Header of the packet being read
        /// </summary>
        public TDSPacketHeader IncomingPacketHeader { get; private set; }

        /// <summary>
        /// Indicates the position inside the request packet data section
        /// </summary>
        public ushort IncomingPacketPosition { get; private set; }

        /// <summary>
        /// Transport stream used to deliver TDS protocol
        /// </summary>
        public Stream InnerStream { get; set; }

        /// <summary>
        /// Size of the TDS packet
        /// </summary>
        public uint PacketSize
        {
            get
            {
                return _packetSize;
            }
            set
            {
                // Update packet size
                _packetSize = value;

                // Reallocate outgoing packet buffers
                _AllocateOutgoingPacket();
            }
        }

        /// <summary>
        /// Identifier of the session
        /// </summary>
        public ushort OutgoingSessionID { get; set; }

        /// <summary>
        /// Indicates whether stream can be read
        /// </summary>
        public override bool CanRead
        {
            // Delegate to the inner stream
            get { return InnerStream.CanRead; }
        }

        /// <summary>
        /// Indicates whether the stream can be positioned
        /// </summary>
        public override bool CanSeek
        {
            // Delegate to the inner stream
            get { return InnerStream.CanSeek; }
        }

        /// <summary>
        /// Indicates whether the stream can be written
        /// </summary>
        public override bool CanWrite
        {
            // Delegate to the inner stream
            get { return InnerStream.CanWrite; }
        }

        /// <summary>
        /// Return the length of the stream
        /// </summary>
        public override long Length
        {
            // Delegate to the inner stream
            get { return InnerStream.Length; }
        }

        /// <summary>
        /// Return position in the stream
        /// </summary>
        public override long Position
        {
            // Delegate to the inner stream
            get { return InnerStream.Position; }
            set { InnerStream.Position = value; }
        }

        /// <summary>
        /// Call back function before calling InnerStream.Write 
        /// the func should return actual packet length to send
        /// </summary>
        public Func<byte[], int, int, ushort> PreWriteCallBack { get; set; }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSStream(Stream innerStream) :
            this(innerStream, true)
        {
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSStream(Stream innerStream, bool leaveInnerStreamOpen)
        {
            // Check if inner stream is valid
            if (innerStream == null)
            {
                // We can't proceed without underlying stream
                throw new ArgumentNullException(nameof(innerStream), "Underlying stream is required");
            }

            // Save transport stream
            InnerStream = innerStream;

            // Save whether inner stream is to be closed as well
            _leaveInnerStreamOpen = leaveInnerStreamOpen;
        }

        /// <summary>
        /// Close the stream
        /// </summary>
        public override void Close()
        {
            // Check if inner stream needs to be closed
            if (!_leaveInnerStreamOpen)
            {
                // Close inner stream
                InnerStream.Close();
            }

            // Delegate to the base class
            base.Close();
        }

        /// <summary>
        /// Flush the data into the underlying stream
        /// </summary>
        public override void Flush()
        {
            // Complete current message before flushing the data
            EndMessage();

            // Delegate to the inner stream
            InnerStream.Flush();
        }

        /// <summary>
        /// Start a new message
        /// </summary>
        /// <param name="type">Type of the message to start</param>
        public virtual void StartMessage(TDSMessageType type)
        {
            // Flush current packet if available
            _SendCurrentPacket();

            // Create a new packet of the specified type
            _CreateOutgoingPacket(type, 1);
        }

        /// <summary>
        /// Send the last packet of the message and complete the request/response
        /// </summary>
        public virtual void EndMessage()
        {
            // Check if we have a current packet
            if (OutgoingPacketHeader != null)
            {
                // Indicate that this is the end of message
                OutgoingPacketHeader.Status |= TDSPacketStatus.EndOfMessage;

                // Send the packet out
                _SendCurrentPacket();
            }
        }

        /// <summary>
        /// Read packet header
        /// </summary>
        public virtual bool ReadNextHeader()
        {
            // Check if we can move to the next incoming packet header
            if (!_MoveToNextIncomingPacketHeader())
            {
                // We can't reach next header at this time
                return false;
            }

            // Allocate a new incoming packet
            _AllocateIncomingPacket();

            // Inflate the header
            if (!IncomingPacketHeader.Inflate(InnerStream))
            {
                // Header inflation failed
                return false;
            }

            // Advance post header position
            IncomingPacketPosition += TDSPacketHeader.Size;

            // Header successfully inflated
            return true;
        }

        /// <summary>
        /// Read the data from the stream
        /// </summary>
        public override int Read(byte[] buffer, int offset, int count)
        {
            // Indication of current position in the buffer that was read from the underlying stream
            int bufferReadPosition = 0;

            // Get the starting time
            DateTime startTime = DateTime.Now;

            // Read operation timeout
            TimeSpan timeout = new TimeSpan(0, 0, 30); // 30 Sec

            // Iterate while there's buffer data left
            while (bufferReadPosition < count && DateTime.Now - startTime < timeout)
            {
                // We need to make sure that there's a packet to read before we start reading it
                if (!_EnsureIncomingPacketHasData())
                {
                    // We don't have enough data
                    return bufferReadPosition;
                }

                // Calculate how much data can be read until the end of the packet is reached
                long packetDataAvailable = IncomingPacketHeader.Length - IncomingPacketPosition;

                // Check how much data we should give back in the current iteration
                int packetDataToRead = Math.Min((int)packetDataAvailable, count - bufferReadPosition);

                // Check if there's data chunk still to be returned
                if (packetDataToRead > 0)
                {
                    // Do read operation while the number of read bytes is 0
                    // Read the data from the underlying stream
                    int packetDataRead = InnerStream.Read(buffer, bufferReadPosition + offset, packetDataToRead);

                    if (packetDataRead == 0)
                    {
                        for (int i = 0; i < 3 && packetDataRead == 0; i++)
                        {
                            Thread.Sleep(50);
                            packetDataRead = InnerStream.Read(buffer, bufferReadPosition + offset, packetDataToRead);
                        }

                        if (packetDataRead == 0)
                        {
                            // Server side socket is FIN_WAIT_2 state, throw
                            throw new EndOfStreamException("Unexpected end of stream");
                        }
                    }

                    // Update current read position
                    bufferReadPosition += packetDataRead;

                    // Update current packet position
                    IncomingPacketPosition += (ushort)packetDataRead;
                }
            }

            // Check if timeout expired
            if (DateTime.Now - startTime > timeout)
            {
                throw new EndOfStreamException("Unexpected end of stream");
            }

            return bufferReadPosition;
        }

        /// <summary>
        /// Seek position in the stream
        /// </summary>
        public override long Seek(long offset, SeekOrigin origin)
        {
            // Delegate to the inner stream
            return InnerStream.Seek(offset, origin);
        }

        /// <summary>
        /// Set stream length
        /// </summary>
        public override void SetLength(long value)
        {
            // Delegate to the inner stream
            InnerStream.SetLength(value);
        }

        /// <summary>
        /// Write data into the stream
        /// </summary>
        public override void Write(byte[] buffer, int offset, int count)
        {
            // Indication of current position in the buffer that was sent to the underlying stream
            int bufferWrittenPosition = 0;

            // Iterate while there's buffer data left
            while (bufferWrittenPosition < count)
            {
                // We need to make sure that current packet has enough space for at least a single byte
                _EnsureOutgoingPacketHasSpace();

                // Check the last packet available and see how much of data we can write
                long packetDataAvailable = PacketSize - OutgoingPacketHeader.Length;

                // Check how much data we still have to write
                // We shouldn't be writing more than packet data available or left to write
                int packetDataToWrite = Math.Min((int)packetDataAvailable, count - bufferWrittenPosition);

                // Check if there's space in the last packet
                if (packetDataToWrite > 0)
                {
                    // Append new data to the end of the packet data
                    Array.Copy(buffer, bufferWrittenPosition + offset, _outgoingPacket, OutgoingPacketHeader.Length, packetDataToWrite);

                    // Register that we've written new data
                    bufferWrittenPosition += packetDataToWrite;

                    // Update packet length
                    OutgoingPacketHeader.Length += (ushort)packetDataToWrite;
                }
            }
        }

        /// <summary>
        /// Skip current packet if it is pending and move to the next packet, reading only the header
        /// </summary>
        private bool _MoveToNextIncomingPacketHeader()
        {
            // Check if we have a packet
            if (IncomingPacketHeader == null)
            {
                // We don't have a packet which means we are at the right spot
                return true;
            }

            // Calculate the span between current position in the packet and it's end
            int distanceToEnd = IncomingPacketHeader.Length - IncomingPacketPosition;

            // Check if we are right at the end of the packet
            if (distanceToEnd <= 0)
            {
                // We consumed the whole packet so we are ready for the next one
                return true;
            }

            // Allocate a buffer for the rest of the packet
            byte[] packetData = new byte[distanceToEnd];

            // Read the data
            int packetDataRead = InnerStream.Read(packetData, 0, packetData.Length);

            // Update packet position
            IncomingPacketPosition += (ushort)packetDataRead;

            // Check if all of the data was read
            return packetDataRead >= packetData.Length;
        }

        /// <summary>
        /// This routine checks whether current packet still has data to read and moves to the next if it doesn't
        /// </summary>
        private bool _EnsureIncomingPacketHasData()
        {
            // Indicates whether the current packet is the one we need
            bool IsRightPacket = true;  // Assume

            do
            {
                // Check if we have a packet
                if (IncomingPacketHeader == null || !IsRightPacket)
                {
                    // Move to the next packet
                    if (!ReadNextHeader())
                    {
                        return false;
                    }
                }

                // Check if current packet is right
                IsRightPacket = (IncomingPacketPosition < IncomingPacketHeader.Length);
            }
            while (!IsRightPacket);

            // We found a packet that satisfies the requirements
            return true;
        }

        /// <summary>
        /// Ensures that the current packet has at least a single spare byte
        /// </summary>
        /// <param name="type">Type of the packet to look for</param>
        private void _EnsureOutgoingPacketHasSpace()
        {
            // Check if we have a packet
            if (OutgoingPacketHeader == null)
            {
                // Message must be started before we can ensure packet availability
                throw new InvalidOperationException("Message has not been started");
            }

            // Check if last packet has no space in it
            if (OutgoingPacketHeader.Length >= PacketSize)
            {
                // Save outgoing packet type before sending it, which will reset the packet header
                TDSMessageType outgoingPacketType = OutgoingPacketHeader.Type;

                // Save outgoing packet number
                byte packetID = OutgoingPacketHeader.PacketID;

                // Before allocating a new packet we need to serialize the current packet
                _SendCurrentPacket();

                // Allocate a new packet since the last packet is full
                _CreateOutgoingPacket(outgoingPacketType, (byte)(((int)packetID + 1) % 256));
            }
        }

        /// <summary>
        /// Create a new TDS packet in the message
        /// </summary>
        private void _CreateOutgoingPacket(TDSMessageType type, byte packetID)
        {
            // Allocate an outgoing packet in case it isn't available
            _AllocateOutgoingPacket();

            // Allocate a new packet with the specified type and normal status
            OutgoingPacketHeader = new TDSPacketHeader(_outgoingPacket, type, TDSPacketStatus.Normal);

            // Assign session identifier to the packet
            OutgoingPacketHeader.SPID = OutgoingSessionID;

            // Increment packet identifier
            OutgoingPacketHeader.PacketID = packetID;
        }

        /// <summary>
        /// Serialize current packet into the underlying stream
        /// </summary>
        private void _SendCurrentPacket()
        {
            // Check if we have a current packet
            if (OutgoingPacketHeader != null)
            {
                // store the OutgoingPacketHeader.Length, it could be updated in PreWriteCallBack (for Fuzz test)
                ushort outgoingPacketHeader_Length = OutgoingPacketHeader.Length;

                // PreWrite call before packet writing 
                if (PreWriteCallBack != null)
                {
                    // By calling PreWriteCallBack, 
                    // The length value in OutgoingPacketHeader (i.e. OutgoingPacketHeader.Length) could be fuzzed
                    // The actual written length of the packet (i.e. outgoingPacketHeader_Length) could be fuzzed
                    outgoingPacketHeader_Length = PreWriteCallBack(_outgoingPacket, 0, OutgoingPacketHeader.Length);
                }

                // Send the packet header along with packet body into the underlying stream
                InnerStream.Write(_outgoingPacket, 0, outgoingPacketHeader_Length);

                // Reset packet header
                OutgoingPacketHeader = null;

                // Reset packet
                _outgoingPacket = null;
            }
        }

        /// <summary>
        /// Allocate or reallocate a packet
        /// </summary>
        private void _AllocateOutgoingPacket()
        {
            // Check if we have incoming packet already
            if (_outgoingPacket != null)
            {
                // Check if incoming packet complies
                if (_outgoingPacket.Length != PacketSize)
                {
                    // Re-allocate
                    Array.Resize(ref _outgoingPacket, (int)PacketSize);
                }
            }
            else
            {
                // Allocate a new packet
                _outgoingPacket = new byte[PacketSize];
            }
        }

        /// <summary>
        /// Prepare to read and inflate incoming packet
        /// </summary>
        private void _AllocateIncomingPacket()
        {
            // Create a new incoming packet header
            IncomingPacketHeader = new TDSPacketHeader(new byte[TDSPacketHeader.Size]);

            // Reset header data position
            IncomingPacketPosition = 0;
        }
    }
}
