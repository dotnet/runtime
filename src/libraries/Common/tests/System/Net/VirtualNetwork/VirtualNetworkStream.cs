// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Test.Common
{
    internal class VirtualNetworkStream : Stream
    {
        private readonly VirtualNetwork _network;
        private MemoryStream _readStream;
        private readonly bool _isServer;
        private readonly bool _gracefulShutdown;
        private SemaphoreSlim _readStreamLock = new SemaphoreSlim(1, 1);
        private TaskCompletionSource _flushTcs;

        public VirtualNetworkStream(VirtualNetwork network, bool isServer, bool gracefulShutdown = false)
        {
            _network = network;
            _isServer = isServer;
            _gracefulShutdown = gracefulShutdown;
        }

        public int DelayMilliseconds { get; set; }

        public bool Disposed { get; private set; }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotImplementedException();

        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool DelayFlush { get; set; }

        public override void Flush() => HasBeenSyncFlushed = true;

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            if (!DelayFlush)
            {
                return Task.CompletedTask;
            }

            if (_flushTcs != null)
            {
                throw new InvalidOperationException();
            }
            _flushTcs = new TaskCompletionSource();

            return _flushTcs.Task;
        }

        public bool HasBeenSyncFlushed { get; private set; }

        public void CompleteAsyncFlush()
        {
            if (_flushTcs == null)
            {
                throw new InvalidOperationException();
            }

            _flushTcs.SetResult();
            _flushTcs = null;
        }

        public override void SetLength(long value) => throw new NotImplementedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();

        public override int Read(byte[] buffer, int offset, int count)
        {
            _readStreamLock.Wait();
            try
            {
                if (_readStream == null || (_readStream.Position >= _readStream.Length))
                {
                    byte[] frame = _network.ReadFrame(_isServer);
                    if (frame.Length == 0) return 0;

                    _readStream = new MemoryStream(frame);
                }

                return _readStream.Read(buffer, offset, count);
            }
            finally
            {
                _readStreamLock.Release();
            }
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await _readStreamLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (DelayMilliseconds > 0)
                {
                    await Task.Delay(DelayMilliseconds, cancellationToken);
                }

                if (_readStream == null || (_readStream.Position >= _readStream.Length))
                {
                    byte[] frame = await _network.ReadFrameAsync(_isServer, cancellationToken).ConfigureAwait(false);
                    if (frame.Length == 0) return 0;

                    _readStream = new MemoryStream(frame);
                }

                return await _readStream.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _readStreamLock.Release();
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _network.WriteFrame(_isServer, buffer.AsSpan(offset, count).ToArray());
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (DelayMilliseconds > 0)
            {
                await Task.Delay(DelayMilliseconds, cancellationToken);
            }

            Write(buffer, offset, count);
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state) =>
            TaskToApm.Begin(ReadAsync(buffer, offset, count), callback, state);

        public override int EndRead(IAsyncResult asyncResult) =>
            TaskToApm.End<int>(asyncResult);

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state) =>
            TaskToApm.Begin(WriteAsync(buffer, offset, count), callback, state);

        public override void EndWrite(IAsyncResult asyncResult) =>
            TaskToApm.End(asyncResult);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Disposed = true;
                if (_gracefulShutdown)
                {
                    GracefulShutdown();
                }
                else
                {
                    _network.BreakConnection();
                }
            }

            base.Dispose(disposing);
        }

        public void GracefulShutdown()
        {
            _network.GracefulShutdown(_isServer);
        }
    }
}
