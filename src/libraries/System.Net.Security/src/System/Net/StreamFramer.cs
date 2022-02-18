// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Globalization;
using System.Net.Security;
using System.Threading.Tasks;
using System.Threading;

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

        public async ValueTask<byte[]?> ReadMessageAsync<TAdapter>(Stream stream, CancellationToken cancellationToken)
            where TAdapter : IReadWriteAdapter
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
                bytesRead = await TAdapter.ReadAsync(stream, buffer.AsMemory(offset), cancellationToken).ConfigureAwait(false);
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
                bytesRead = await TAdapter.ReadAsync(stream, buffer.AsMemory(offset), cancellationToken).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    throw new IOException(SR.Format(SR.net_io_readfailure, SR.net_io_connectionclosed));
                }

                offset += bytesRead;
            }
            return buffer;
        }

        public async Task WriteMessageAsync<TAdapter>(Stream stream, byte[] message, CancellationToken cancellationToken)
            where TAdapter : IReadWriteAdapter
        {
            _writeHeader.PayloadSize = message.Length;
            _writeHeader.CopyTo(_writeHeaderBuffer, 0);

            await TAdapter.WriteAsync(stream, _writeHeaderBuffer, 0, _writeHeaderBuffer.Length, cancellationToken).ConfigureAwait(false);
            if (message.Length != 0)
            {
                await TAdapter.WriteAsync(stream, message, 0, message.Length, cancellationToken).ConfigureAwait(false);
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
