// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace Microsoft.SqlServer.TDS
{
    /// <summary>
    /// TDS packet header
    /// </summary>
    public class TDSPacketHeader : IInflatable
    {
        /// <summary>
        /// Size of the packet header in bytes
        /// </summary>
        public const int Size = 8;

        /// <summary>
        /// Packet header data
        /// </summary>
        private byte[] _data;

        /// <summary>
        /// The last offset where inflation operation was interrupted
        /// </summary>
        private ushort _inflationOffset;

        /// <summary>
        /// Type of the packet
        /// 1 byte unsigned
        /// </summary>
        public TDSMessageType Type
        {
            get
            {
                // Read a byte from the buffer
                return (TDSMessageType)_data[0];
            }
            set
            {
                // Write into the buffer
                _data[0] = (byte)value;
            }
        }

        /// <summary>
        /// Status of the packet
        /// 1 byte unsigned
        /// </summary>
        public TDSPacketStatus Status
        {
            get
            {
                // Read a byte from the buffer
                return (TDSPacketStatus)_data[1];
            }
            set
            {
                // Write into the buffer
                _data[1] = (byte)value;
            }
        }

        /// <summary>
        /// Size of the packet including header
        /// 2 byte unsigned short
        /// </summary>
        public ushort Length
        {
            get
            {
                // Read big endian length from the buffer
                return (ushort)((ushort)(_data[2] << 8) + (ushort)_data[3]);
            }
            set
            {
                // Write big endian value into the buffer
                _data[2] = (byte)(value >> 8);
                _data[3] = (byte)value;
            }
        }

        /// <summary>
        /// Process ID that sent this packet
        /// 2 byte big-endian
        /// </summary>
        public ushort SPID
        {
            get
            {
                // Read big endian length from the buffer
                return (ushort)((ushort)(_data[4] << 8) + (ushort)_data[5]);
            }
            set
            {
                // Write big endian value into the buffer
                _data[4] = (byte)(value >> 8);
                _data[5] = (byte)value;
            }
        }

        /// <summary>
        /// Identifier of the packet
        /// 1 byte unsigned char
        /// Incremented by 1 modulo 256
        /// </summary>
        public byte PacketID
        {
            get
            {
                // Read a byte from the buffer
                return _data[6];
            }
            set
            {
                // Write into the buffer
                _data[6] = value;
            }
        }

        /// <summary>
        /// Window (not used)
        /// 1 byte
        /// Should always be set to 0x00
        /// </summary>
        public byte Window
        {
            get
            {
                // Read a byte from the buffer
                return _data[7];
            }
            set
            {
                // Write into the buffer
                _data[7] = value;
            }
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        /// <param name="buffer">Buffer which contains or will contain a packet header</param>
        public TDSPacketHeader(byte[] buffer)
        {
            // Allocate packet header data
            _data = buffer;

            // We have not started the inflation
            _inflationOffset = 0;

            // Default length of the packet
            Length = Size;
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSPacketHeader(byte[] buffer, TDSMessageType type) :
            this(buffer)
        {
            // Save type
            Type = type;
        }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        public TDSPacketHeader(byte[] buffer, TDSMessageType type, TDSPacketStatus status) :
            this(buffer, type)
        {
            // Save status
            Status = status;
        }

        /// <summary>
        /// Inflate TDS packet from the stream
        /// </summary>
        /// <param name="source">Source of data</param>
        public bool Inflate(Stream source)
        {
            // Check if inflation is complete
            if (_inflationOffset < Size)
            {
                // Calculate remaining bytes
                int packetHeaderLeft = Size - _inflationOffset;

                // Read the rest of the packet header
                int packetHeaderRead = source.Read(_data, _inflationOffset, packetHeaderLeft);

                // Update offset
                _inflationOffset += (ushort)packetHeaderRead;
            }

            // Tell caller if inflation is complete
            return _inflationOffset >= Size;
        }
    }
}
