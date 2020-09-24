// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    internal sealed class BytesLoggingStream : Stream
    {
        private readonly Stream _stream;
        private readonly Action<bool, string> _log;

        public BytesLoggingStream(Stream stream, Action<bool, string> log)
        {
            _stream = stream;
            _log = log;
        }

        public override bool CanRead => _stream.CanRead;
        public override bool CanSeek => _stream.CanSeek;
        public override bool CanWrite => _stream.CanWrite;
        public override long Length => _stream.Length;
        public override long Position { get => _stream.Position; set => _stream.Position = value; }
        public override void Flush() => _stream.Flush();
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

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = _stream.Read(buffer, offset, count);
            FormatBytes(read: true, buffer.AsSpan(offset, read));
            return read;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            FormatBytes(read: false, buffer.AsSpan(offset, count));
            _stream.Write(buffer, offset, count);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            int read = await _stream.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
            FormatBytes(read: true, buffer.AsSpan(offset, read));
            return read;
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            FormatBytes(read: false, buffer.AsSpan(offset, count));
            return _stream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        private void FormatBytes(bool read, ReadOnlySpan<byte> bytes)
        {
            if (bytes.IsEmpty)
            {
                return;
            }

            ReadOnlySpan<byte> hex = new byte[]
            {
                (byte)'0', (byte)'1', (byte)'2', (byte)'3', (byte)'4', (byte)'5', (byte)'6', (byte)'7',
                (byte)'8', (byte)'9', (byte)'A', (byte)'B', (byte)'C', (byte)'D', (byte)'E', (byte)'F',
            };

            var sb = new StringBuilder();
            string header = (read ? "RECV (" : "SEND (") + GetHashCode().ToString("X8") + "): ";
            sb.Append(header);

            const int BytesPerLine = 24;

            while (!bytes.IsEmpty)
            {
                var span = bytes.Slice(0, Math.Min(bytes.Length, BytesPerLine));

                // Format hex
                int width = BytesPerLine * 3 + 4;
                foreach (byte b in span)
                {
                    sb.Append((char)hex[b >> 4]).Append((char)hex[b & 0XF]).Append(' ');
                    width -= 3;
                }
                sb.Append(' ', width);

                // Format ASCII
                foreach (byte b in span)
                {
                    char c = (char)b;
                    sb.Append(c >= 32 && c < 0x7F ? c : '.');
                }

                bytes = bytes.Slice(span.Length);

                if (!bytes.IsEmpty)
                {
                    sb.AppendLine();
                    sb.Append(' ', header.Length);
                }
            }

            _log(read, sb.ToString());
        }
    }
}
