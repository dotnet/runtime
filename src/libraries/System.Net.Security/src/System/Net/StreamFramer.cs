// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Globalization;
using System.Net.Security;
using System.Threading.Tasks;

namespace System.Net
{
    internal sealed class StreamFramer
    {
        private readonly FrameHeader _writeHeader = new FrameHeader();
        private readonly FrameHeader _curReadHeader = new FrameHeader();

        private readonly byte[] _readHeaderBuffer = new byte[FrameHeader.Size];
        private readonly byte[] _writeHeaderBuffer = new byte[FrameHeader.Size];
        private bool _eof;

        public FrameHeader ReadHeader => _curReadHeader;
        public FrameHeader WriteHeader => _writeHeader;

        public async ValueTask<byte[]?> ReadMessageAsync<TAdapter>(TAdapter adapter) where TAdapter : IReadWriteAdapter
        {
            if (_eof)
            {
                return null;
            }

            byte[] buffer = _readHeaderBuffer;

            int bytesRead;
            int offset = 0;
            while (offset < buffer.Length)
            {
                bytesRead = await adapter.ReadAsync(buffer.AsMemory(offset)).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    if (offset == 0)
                    {
                        // m_Eof, return null
                        _eof = true;
                        return null;
                    }

                    throw new IOException(SR.Format(SR.net_io_readfailure, SR.net_io_connectionclosed));
                }

                offset += bytesRead;
            }

            _curReadHeader.CopyFrom(buffer, 0);
            if (_curReadHeader.PayloadSize > FrameHeader.MaxMessageSize)
            {
                throw new InvalidOperationException(SR.Format(SR.net_frame_size,
                                                               FrameHeader.MaxMessageSize,
                                                               _curReadHeader.PayloadSize.ToString(NumberFormatInfo.InvariantInfo)));
            }

            buffer = new byte[_curReadHeader.PayloadSize];

            offset = 0;
            while (offset < buffer.Length)
            {
                bytesRead = await adapter.ReadAsync(buffer.AsMemory(offset)).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    throw new IOException(SR.Format(SR.net_io_readfailure, SR.net_io_connectionclosed));
                }

                offset += bytesRead;
            }
            return buffer;
        }

        public async Task WriteMessageAsync<TAdapter>(TAdapter adapter, byte[] message) where TAdapter : IReadWriteAdapter
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            _writeHeader.PayloadSize = message.Length;
            _writeHeader.CopyTo(_writeHeaderBuffer, 0);

            await adapter.WriteAsync(_writeHeaderBuffer, 0, _writeHeaderBuffer.Length).ConfigureAwait(false);
            if (message.Length != 0)
            {
                await adapter.WriteAsync(message, 0, message.Length).ConfigureAwait(false);
            }
        }
    }

    // Describes the header used in framing of the stream data.
    internal sealed class FrameHeader
    {
        public const int IgnoreValue = -1;
        public const int HandshakeDoneId = 20;
        public const int HandshakeErrId = 21;
        public const int HandshakeId = 22;
        public const int DefaultMajorV = 1;
        public const int DefaultMinorV = 0;
        public const int Size = 5;
        public const int MaxMessageSize = 0xFFFF;

        private int _payloadSize = -1;

        public int MessageId { get; set; } = HandshakeId;
        public int MajorV { get; private set; } = DefaultMajorV;
        public int MinorV { get; private set; } = DefaultMinorV;

        public int PayloadSize
        {
            get => _payloadSize;
            set
            {
                if (value > MaxMessageSize)
                {
                    throw new ArgumentException(SR.Format(SR.net_frame_max_size, MaxMessageSize, value), nameof(PayloadSize));
                }

                _payloadSize = value;
            }
        }

        public void CopyTo(byte[] dest, int start)
        {
            dest[start++] = (byte)MessageId;
            dest[start++] = (byte)MajorV;
            dest[start++] = (byte)MinorV;
            dest[start++] = (byte)((_payloadSize >> 8) & 0xFF);
            dest[start] = (byte)(_payloadSize & 0xFF);
        }

        public void CopyFrom(byte[] bytes, int start)
        {
            MessageId = bytes[start++];
            MajorV = bytes[start++];
            MinorV = bytes[start++];
            _payloadSize = (bytes[start++] << 8) | bytes[start];
        }
    }
}
