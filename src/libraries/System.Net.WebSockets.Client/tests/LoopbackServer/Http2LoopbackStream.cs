// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Test.Common
{
    public class Http2LoopbackStream : Stream
    {
        private readonly Http2LoopbackConnection _connection;
        private readonly int _streamId;
        private bool _readEnded;
        private ReadOnlyMemory<byte> _leftoverReadData;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;

        public Http2LoopbackConnection Connection => _connection;
        public int StreamId => _streamId;

        public Http2LoopbackStream(Http2LoopbackConnection connection, int streamId)
        {
            _connection = connection;
            _streamId = streamId;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (!_leftoverReadData.IsEmpty)
            {
                int read = Math.Min(buffer.Length, _leftoverReadData.Length);
                _leftoverReadData.Span.Slice(0, read).CopyTo(buffer.Span);
                _leftoverReadData = _leftoverReadData.Slice(read);
                return read;
            }

            if (_readEnded)
            {
                return 0;
            }

            DataFrame dataFrame = (DataFrame)await _connection.ReadFrameAsync(cancellationToken);
            Assert.Equal(_streamId, dataFrame.StreamId);
            _leftoverReadData = dataFrame.Data;
            _readEnded = dataFrame.EndStreamFlag;

            return await ReadAsync(buffer, cancellationToken);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await _connection.SendResponseDataAsync(_streamId, buffer, endStream: false);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        protected override void Dispose(bool disposing) => DisposeAsync().GetAwaiter().GetResult();

        public override async ValueTask DisposeAsync()
        {
            try
            {
                await _connection.SendResponseDataAsync(_streamId, Memory<byte>.Empty, endStream: true).ConfigureAwait(false);

                if (!_readEnded)
                {
                    var rstFrame = new RstStreamFrame(FrameFlags.None, (int)ProtocolErrors.NO_ERROR, _streamId);
                    await _connection.WriteFrameAsync(rstFrame).ConfigureAwait(false);
                }
            }
            catch (IOException)
            {
                // Ignore connection errors
            }
            catch (SocketException)
            {
                // Ignore connection errors
            }
        }

        public override void Flush() { }
        public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();
        public override void SetLength(long value) => throw new NotImplementedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();
        public override long Length => throw new NotImplementedException();
        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }
}
