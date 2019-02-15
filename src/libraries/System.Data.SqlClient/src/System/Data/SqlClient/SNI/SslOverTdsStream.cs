// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace System.Data.SqlClient.SNI
{
    /// <summary>
    /// SSL encapsulated over TDS transport. During SSL handshake, SSL packets are
    /// transported in TDS packet type 0x12. Once SSL handshake has completed, SSL
    /// packets are sent transparently.
    /// </summary>
    internal sealed class SslOverTdsStream : Stream
    {
        private readonly Stream _stream;

        private int _packetBytes = 0;
        private bool _encapsulate;

        private const int PACKET_SIZE_WITHOUT_HEADER = TdsEnums.DEFAULT_LOGIN_PACKET_SIZE - TdsEnums.HEADER_LEN;
        private const int PRELOGIN_PACKET_TYPE = 0x12;

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
        public override int Read(byte[] buffer, int offset, int count) =>
            ReadInternal(buffer, offset, count, CancellationToken.None, async: false).GetAwaiter().GetResult();

        /// <summary>
        /// Write Buffer
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public override void Write(byte[] buffer, int offset, int count)
            => WriteInternal(buffer, offset, count, CancellationToken.None, async: false).Wait();

        /// <summary>
        /// Write Buffer Asynchronosly
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken token)
            => WriteInternal(buffer, offset, count, token, async: true);

        /// <summary>
        /// Read Buffer Asynchronosly
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token)
            => ReadInternal(buffer, offset, count, token, async: true);

        /// <summary>
        /// Read Internal is called synchronosly when async is false
        /// </summary>
        private async Task<int> ReadInternal(byte[] buffer, int offset, int count, CancellationToken token, bool async)
        {
            if (_encapsulate)
            {
                return await ReadInternalEncapsulate(buffer, offset, count, token, async);
            }
            else if (async)
            {
                return await ReadInternalAsync(buffer, offset, count, token);
            }
            else
            {
                return ReadInternalSync(buffer, offset, count);
            }
        }

        private async Task<int> ReadInternalEncapsulate(byte[] buffer, int offset, int count, CancellationToken token, bool async)
        {
            int readBytes = 0;
            byte[] packetData = ArrayPool<byte>.Shared.Rent(count < TdsEnums.HEADER_LEN ? TdsEnums.HEADER_LEN : count);

            if (_packetBytes == 0)
            {
                // Account for split packets
                while (readBytes < TdsEnums.HEADER_LEN)
                {
                    readBytes += (async ?
                        await ReadInternalAsync(packetData, readBytes, TdsEnums.HEADER_LEN - readBytes, token) :
                        ReadInternalSync(packetData, readBytes, TdsEnums.HEADER_LEN - readBytes)
                   );
                }

                _packetBytes = (packetData[TdsEnums.HEADER_LEN_FIELD_OFFSET] << 8) | packetData[TdsEnums.HEADER_LEN_FIELD_OFFSET + 1];
                _packetBytes -= TdsEnums.HEADER_LEN;
            }

            if (count > _packetBytes)
            {
                count = _packetBytes;
            }
            
            readBytes = (async ?
                await ReadInternalAsync(packetData, 0, count, token) :
                ReadInternalSync(packetData, 0, count)
            );


            _packetBytes -= readBytes;
            
            Buffer.BlockCopy(packetData, 0, buffer, offset, readBytes);

            Array.Clear(packetData, 0, readBytes);
            ArrayPool<byte>.Shared.Return(packetData, clearArray: false);

            return readBytes;
        }

        private async Task<int> ReadInternalAsync(byte[] buffer, int offset, int count, CancellationToken token)
        {
            return await _stream.ReadAsync(buffer, 0, count, token).ConfigureAwait(false);
        }

        private int ReadInternalSync(byte[] buffer, int offset, int count)
        {
            return _stream.Read(buffer, 0, count);
        }

        /// <summary>
        /// The internal write method calls Sync APIs when Async flag is false
        /// </summary>
        private async Task WriteInternal(byte[] buffer, int offset, int count, CancellationToken token, bool async)
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
                    if (count > PACKET_SIZE_WITHOUT_HEADER)
                    {
                        currentCount = PACKET_SIZE_WITHOUT_HEADER;
                    }
                    else
                    {
                        currentCount = count;
                    }

                    count -= currentCount;

                    // Prepend buffer data with TDS prelogin header
                    int combinedLength = TdsEnums.HEADER_LEN + currentCount;
                    byte[] combinedBuffer = ArrayPool<byte>.Shared.Rent(combinedLength);

                    // We can only send 4088 bytes in one packet. Header[1] is set to 1 if this is a 
                    // partial packet (whether or not count != 0).
                    // 
                    combinedBuffer[7] = 0; // touch this first for the jit bounds check
                    combinedBuffer[0] = PRELOGIN_PACKET_TYPE;
                    combinedBuffer[1] = (byte)(count > 0 ? 0 : 1);
                    combinedBuffer[2] = (byte)((currentCount + TdsEnums.HEADER_LEN) / 0x100);
                    combinedBuffer[3] = (byte)((currentCount + TdsEnums.HEADER_LEN) % 0x100);
                    combinedBuffer[4] = 0;
                    combinedBuffer[5] = 0;
                    combinedBuffer[6] = 0;

                    Array.Copy(buffer, currentOffset, combinedBuffer, TdsEnums.HEADER_LEN, (combinedLength - TdsEnums.HEADER_LEN));

                    if (async)
                    {
                        await _stream.WriteAsync(combinedBuffer, 0, combinedLength, token).ConfigureAwait(false);
                    }
                    else
                    {
                        _stream.Write(combinedBuffer, 0, combinedLength);
                    }

                    Array.Clear(combinedBuffer, 0, combinedLength);
                    ArrayPool<byte>.Shared.Return(combinedBuffer);

                }
                else
                {
                    currentCount = count;
                    count = 0;

                    if (async)
                    {
                        await _stream.WriteAsync(buffer, currentOffset, currentCount, token).ConfigureAwait(false);
                    }
                    else
                    {
                        _stream.Write(buffer, currentOffset, currentCount);
                    }
                }

                if (async)
                {
                    await _stream.FlushAsync().ConfigureAwait(false);
                }
                else
                {
                    _stream.Flush();
                }

                currentOffset += currentCount;
            }
        }

        /// <summary>
        /// Set stream length. 
        /// </summary>
        /// <param name="value">Length</param>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Flush stream
        /// </summary>
        public override void Flush()
        {
            // Can sometimes get Pipe broken errors from flushing a PipeStream.
            // PipeStream.Flush() also doesn't do anything, anyway.
            if (!(_stream is PipeStream))
            {
                _stream.Flush();
            }
        }

        /// <summary>
        /// Get/set stream position
        /// </summary>
        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
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
            throw new NotSupportedException();
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
            get { return false; } // Seek not supported
        }

        /// <summary>
        /// Get stream length
        /// </summary>
        public override long Length
        {
            get
            {
                throw new NotSupportedException();
            }
        }
    }
}
