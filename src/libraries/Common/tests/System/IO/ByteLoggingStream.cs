// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    internal sealed class BytesLoggingStream : Stream
    {
        public delegate void FormattedBytesCallback(Stream stream, ReadOnlySpan<char> hex, ReadOnlySpan<char> ascii);

        [ThreadStatic]
        private static char[]? s_hexBuffer;

        [ThreadStatic]
        private static char[]? s_asciiBuffer;

        private readonly Stream _stream;
        private readonly FormattedBytesCallback _readCallback;
        private readonly FormattedBytesCallback _writeCallback;
        private int _bytesPerLine = 24;

        public BytesLoggingStream(Stream stream, FormattedBytesCallback writeCallback, FormattedBytesCallback readCallback)
        {
            _stream = stream;
            _readCallback = readCallback;
            _writeCallback = writeCallback;
        }

        public override bool CanRead => _stream.CanRead;
        public override bool CanSeek => _stream.CanSeek;
        public override bool CanWrite => _stream.CanWrite;
        public override bool CanTimeout => _stream.CanTimeout;

        public override long Length => _stream.Length;
        public override long Position { get => _stream.Position; set => _stream.Position = value; }

        public override void Flush() => _stream.Flush();
        public override Task FlushAsync(CancellationToken cancellationToken) => _stream.FlushAsync(cancellationToken);

        public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);
        public override void SetLength(long value) => _stream.SetLength(value);

        public override int ReadTimeout { get => _stream.ReadTimeout; set => _stream.ReadTimeout = value; }
        public override int WriteTimeout { get => _stream.WriteTimeout; set => _stream.WriteTimeout = value; }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _stream.Dispose();
            }
        }

        public int BytesPerLine
        {
            get => _bytesPerLine;
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException(nameof(BytesPerLine));
                _bytesPerLine = value;
            }
        }

        public override int ReadByte()
        {
            int read = _stream.ReadByte();
            if (read != -1)
            {
                byte b = (byte)read;
                FormatBytes(read: true, MemoryMarshal.CreateReadOnlySpan(ref b, 1));
            }
            return read;
        }

        public override int Read(Span<byte> buffer)
        {
            int read = _stream.Read(buffer);
            FormatBytes(read: true, buffer);
            return read;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = _stream.Read(buffer, offset, count);
            FormatBytes(read: true, buffer.AsSpan(offset, read));
            return read;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            int read = await _stream.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
            FormatBytes(read: true, buffer.AsSpan(offset, read));
            return read;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            int read = await _stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            FormatBytes(read: true, buffer.Span.Slice(0, read));
            return read;
        }

        public override void WriteByte(byte value)
        {
            FormatBytes(read: false, MemoryMarshal.CreateReadOnlySpan(ref value, 1));
            _stream.WriteByte(value);
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            FormatBytes(read: false, buffer);
            _stream.Write(buffer);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            FormatBytes(read: false, buffer.AsSpan(offset, count));
            _stream.Write(buffer, offset, count);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            FormatBytes(read: false, buffer.AsSpan(offset, count));
            return _stream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            FormatBytes(read: false, buffer.Span);
            return _stream.WriteAsync(buffer, cancellationToken);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            FormatBytes(read: false, buffer.AsSpan(offset, count));
            return _stream.BeginWrite(buffer, offset, count, callback, state);
        }

        public override void EndWrite(IAsyncResult asyncResult) =>
            _stream.EndWrite(asyncResult);

        private void FormatBytes(bool read, ReadOnlySpan<byte> bytes)
        {
            if (bytes.IsEmpty)
            {
                return;
            }

            ReadOnlySpan<byte> hex = "0123456789ABCDEF"u8;

            int bytesPerLine = _bytesPerLine;
            int requiredHexLength = bytesPerLine * 3 - 1;

            char[]? hexBuffer = s_hexBuffer;
            if (hexBuffer is null || hexBuffer.Length < requiredHexLength)
            {
                s_hexBuffer = hexBuffer = new char[requiredHexLength];
            }

            char[]? asciiBuffer = s_asciiBuffer;
            if (asciiBuffer is null || asciiBuffer.Length < bytesPerLine)
            {
                s_asciiBuffer = asciiBuffer = new char[bytesPerLine];
            }

            while (!bytes.IsEmpty)
            {
                ReadOnlySpan<byte> span = bytes.Slice(0, Math.Min(bytes.Length, bytesPerLine));
                int hexPos = 0;
                int asciiPos = 0;

                for (int i = 0; i < span.Length; i++)
                {
                    byte b = span[i];
                    hexBuffer[hexPos++] = (char)hex[b >> 4];
                    hexBuffer[hexPos++] = (char)hex[b & 0XF];
                    if (i != span.Length - 1)
                    {
                        hexBuffer[hexPos++] = ' ';
                    }

                    asciiBuffer[asciiPos++] =
                        b switch
                        {
                            < 32 or >= 0x7F => '.',
                            _ => (char)b,
                        };
                }

                (read ? _readCallback : _writeCallback)(this, new ReadOnlySpan<char>(hexBuffer, 0, hexPos), new ReadOnlySpan<char>(asciiBuffer, 0, asciiPos));
                bytes = bytes.Slice(span.Length);
            }
        }
    }
}
