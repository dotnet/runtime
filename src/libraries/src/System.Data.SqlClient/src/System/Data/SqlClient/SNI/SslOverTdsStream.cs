// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace System.Data.SqlClient.SNI
{
    /// <summary>
    /// SSL encapsulated over TDS transport. During SSL handshake, SSL packets are
    /// transported in TDS packet type 0x12. Once SSL handshake has completed, SSL
    /// packets are sent transparently.
    /// </summary>
    internal sealed class SslOverTdsStream : Stream
    {
        private Stream _stream;
        private bool _encapsulate;
        private int _packetBytes = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="stream">Underlying stream</param>
        public SslOverTdsStream(Stream stream)
        {
            _stream = stream;
            _encapsulate = true;
        }

        /// <summary>
        /// Finish SSL handshake. Stop encapsulating in TDS.
        /// </summary>
        public void FinishHandshake()
        {
            _encapsulate = false;
        }

        /// <summary>
        /// Read buffer
        /// </summary>
        /// <param name="buffer">Buffer</param>
        /// <param name="offset">Offset</param>
        /// <param name="count">Byte count</param>
        /// <returns>Bytes read</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            int readBytes;
            byte[] scratch = new byte[count < 8 ? 8 : count];

            if (_encapsulate)
            {
                if (_packetBytes == 0)
                {
                    readBytes = _stream.Read(scratch, 0, 8);
                    _packetBytes = scratch[2] * 256;
                    _packetBytes += scratch[3];
                    _packetBytes -= 8;
                }

                if (count > _packetBytes)
                {
                    count = _packetBytes;
                }
            }

            readBytes = _stream.Read(scratch, 0, count);

            if (_encapsulate)
            {
                _packetBytes -= readBytes;
            }

            Buffer.BlockCopy(scratch, 0, buffer, offset, readBytes);
            return readBytes;
        }

        /// <summary>
        /// Write buffer
        /// </summary>
        /// <param name="buffer">Buffer</param>
        /// <param name="offset">Offset</param>
        /// <param name="count">Byte count</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            int currentCount = 0;
            int currentOffset = offset;

            while (count > 0)
            {
                // During the SSL negotiation phase, SSL is tunnelled over TDS packet type 0x12. After
                // negotiation, the underlying socket only sees SSL frames.
                //
                if (_encapsulate)
                {
                    if (count > 4088)
                    {
                        currentCount = 4088;
                    }
                    else
                    {
                        currentCount = count;
                    }

                    count -= currentCount;

                    byte[] header = new byte[8];

                    // We can only send 4088 bytes in one packet. Header[1] is set to 1 if this is a 
                    // partial packet (whether or not count != 0).
                    // 
                    header[0] = 0x12;
                    header[1] = (byte)(count > 0 ? 0 : 1);
                    header[2] = (byte)((currentCount + 8) / 256);
                    header[3] = (byte)((currentCount + 8) % 256);
                    header[4] = 0;
                    header[5] = 0;
                    header[6] = 0;
                    header[7] = 0;

                    _stream.Write(header, 0, 8);
                    _stream.Flush();
                }
                else
                {
                    currentCount = count;
                    count = 0;
                }

                _stream.Write(buffer, currentOffset, currentCount);
                _stream.Flush();
                currentOffset += currentCount;
            }
        }

        /// <summary>
        /// Set stream length. 
        /// </summary>
        /// <param name="value">Length</param>
        public override void SetLength(long value)
        {
            throw NotImplemented.ByDesignWithMessage("Not required");
        }

        /// <summary>
        /// Flush stream
        /// </summary>
        public override void Flush()
        {
            _stream.Flush();
        }

        /// <summary>
        /// Get/set stream position
        /// </summary>
        public override long Position
        {
            get
            {
                throw NotImplemented.ByDesignWithMessage("Not required");
            }
            set
            {
                throw NotImplemented.ByDesignWithMessage("Not required");
            }
        }

        /// <summary>
        /// Seek in stream
        /// </summary>
        /// <param name="offset">Offset</param>
        /// <param name="origin">Origin</param>
        /// <returns>Position</returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw NotImplemented.ByDesignWithMessage("Not required");
        }

        /// <summary>
        /// Check if stream can be read from
        /// </summary>
        public override bool CanRead
        {
            get { return _stream.CanRead; }
        }

        /// <summary>
        /// Check if stream can be written to
        /// </summary>
        public override bool CanWrite
        {
            get { return _stream.CanWrite; }
        }

        /// <summary>
        /// Check if stream can be seeked
        /// </summary>
        public override bool CanSeek
        {
            get { return _stream.CanSeek; }
        }

        /// <summary>
        /// Get stream length
        /// </summary>
        public override long Length
        {
            get
            {
                throw NotImplemented.ByDesignWithMessage("Not required");
            }
        }
    }
}