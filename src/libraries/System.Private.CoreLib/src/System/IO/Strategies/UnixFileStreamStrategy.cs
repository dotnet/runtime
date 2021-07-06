// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Strategies
{
    internal sealed partial class UnixFileStreamStrategy : OSFileStreamStrategy
    {
        private ReadAsyncTaskSource? _readAsyncTaskSource;

        internal UnixFileStreamStrategy(SafeFileHandle handle, FileAccess access, FileShare share) :
            base(handle, access, share)
        {
        }

        internal UnixFileStreamStrategy(string path, FileMode mode, FileAccess access, FileShare share, FileOptions options, long preallocationSize) :
            base(path, mode, access, share, options, preallocationSize)
        {
        }

        internal override bool IsAsync => _fileHandle.IsAsync;

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) =>
            TaskToApm.Begin(ReadAsync(buffer, offset, count), callback, state);

        public override int EndRead(IAsyncResult asyncResult) =>
            TaskToApm.End<int>(asyncResult);

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();

        public override ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken)
        {
            if (!CanRead)
            {
                ThrowHelper.ThrowNotSupportedException_UnreadableStream();
            }

            if (CanSeek)
            {
                // This implementation updates the file position after the operation completes, rather than before.
                // Also, unlike the Net5CompatFileStreamStrategy implementation, this implementation doesn't serialize operations.
                ReadAsyncTaskSource rats = Interlocked.Exchange(ref _readAsyncTaskSource, null) ?? new ReadAsyncTaskSource(this);
                return rats.QueueRead(destination, cancellationToken);
            }

            return RandomAccess.ReadAtOffsetAsync(_fileHandle, destination, fileOffset: -1, cancellationToken);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) =>
            TaskToApm.Begin(WriteAsync(buffer, offset, count), callback, state);

        public override void EndWrite(IAsyncResult asyncResult) =>
            TaskToApm.End(asyncResult);

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            WriteAsyncCore(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).AsTask();

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken) =>
#pragma warning disable CA2012 // The analyzer doesn't know the internal AsValueTask is safe.
            WriteAsyncCore(source, cancellationToken).AsValueTask();
#pragma warning restore CA2012

        private ValueTask<int> WriteAsyncCore(ReadOnlyMemory<byte> source, CancellationToken cancellationToken)
        {
            if (!CanWrite)
            {
                ThrowHelper.ThrowNotSupportedException_UnwritableStream();
            }

            long filePositionBefore = -1;
            if (CanSeek)
            {
                filePositionBefore = _filePosition;
                _filePosition += source.Length;
            }

            return RandomAccess.WriteAtOffsetAsync(_fileHandle, source, filePositionBefore, cancellationToken);
        }

        /// <summary>Provides a reusable ValueTask-backing object for implementing ReadAsync.</summary>
        private sealed class ReadAsyncTaskSource : IValueTaskSource<int>, IThreadPoolWorkItem
        {
            private readonly UnixFileStreamStrategy _stream;
            private ManualResetValueTaskSourceCore<int> _source;

            private Memory<byte> _destination;
            private ExecutionContext? _context;
            private CancellationToken _cancellationToken;

            public ReadAsyncTaskSource(UnixFileStreamStrategy stream) => _stream = stream;

            public ValueTask<int> QueueRead(Memory<byte> destination, CancellationToken cancellationToken)
            {
                _destination = destination;
                _cancellationToken = cancellationToken;
                _context = ExecutionContext.Capture();

                ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: true);
                return new ValueTask<int>(this, _source.Version);
            }

            void IThreadPoolWorkItem.Execute()
            {
                if (_context is null || _context.IsDefault)
                {
                    Read();
                }
                else
                {
                    ExecutionContext.RunForThreadPoolUnsafe(_context, static x => x.Read(), this);
                }
            }

            private void Read()
            {
                Exception? error = null;
                int result = 0;

                try
                {
                    if (_cancellationToken.IsCancellationRequested)
                    {
                        error = new OperationCanceledException(_cancellationToken);
                    }
                    else
                    {
                        result = _stream.Read(_destination.Span);
                    }
                }
                catch (Exception e)
                {
                    error = e;
                }
                finally
                {
                    _destination = default;
                    _cancellationToken = default;
                    _context = null;
                }

                if (error is not null)
                {
                    _source.SetException(error);
                }
                else
                {
                    _source.SetResult(result);
                }
            }

            int IValueTaskSource<int>.GetResult(short token)
            {
                try
                {
                    return _source.GetResult(token);
                }
                finally
                {
                    _source.Reset();
#pragma warning disable CS0197
                    Volatile.Write(ref _stream._readAsyncTaskSource, this);
#pragma warning restore CS0197
                }
            }

            ValueTaskSourceStatus IValueTaskSource<int>.GetStatus(short token) =>
                _source.GetStatus(token);

            void IValueTaskSource<int>.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) =>
                _source.OnCompleted(continuation, state, token, flags);
        }
    }
}
