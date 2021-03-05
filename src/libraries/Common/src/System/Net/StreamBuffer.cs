// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace System.IO
{
    internal sealed class StreamBuffer : IDisposable
    {
        private MultiArrayBuffer _buffer; // mutable struct, do not make this readonly
        private readonly int _maxBufferSize;
        private bool _writeEnded;
        private bool _readAborted;
        private readonly ResettableValueTaskSource _readTaskSource;
        private readonly ResettableValueTaskSource _writeTaskSource;

        public const int DefaultInitialBufferSize = 4 * 1024;
        public const int DefaultMaxBufferSize = 32 * 1024;

        public StreamBuffer(int initialBufferSize = DefaultInitialBufferSize, int maxBufferSize = DefaultMaxBufferSize)
        {
            _buffer = new MultiArrayBuffer(initialBufferSize);
            _maxBufferSize = maxBufferSize;
            _readTaskSource = new ResettableValueTaskSource();
            _writeTaskSource = new ResettableValueTaskSource();
        }

        private object SyncObject => _readTaskSource;

        public bool IsComplete
        {
            get
            {
                Debug.Assert(!Monitor.IsEntered(SyncObject));
                lock (SyncObject)
                {
                    return (_writeEnded && _buffer.IsEmpty);
                }
            }
        }

        public bool IsAborted
        {
            get
            {
                Debug.Assert(!Monitor.IsEntered(SyncObject));
                lock (SyncObject)
                {
                    return _readAborted;
                }
            }
        }

        public int ReadBytesAvailable
        {
            get
            {
                Debug.Assert(!Monitor.IsEntered(SyncObject));
                lock (SyncObject)
                {
                    if (_readAborted)
                    {
                        return 0;
                    }

                    return _buffer.ActiveMemory.Length;
                }
            }
        }

        public int WriteBytesAvailable
        {
            get
            {
                Debug.Assert(!Monitor.IsEntered(SyncObject));
                lock (SyncObject)
                {
                    if (_writeEnded)
                    {
                        throw new InvalidOperationException();
                    }

                    return _maxBufferSize - _buffer.ActiveMemory.Length;
                }
            }
        }

        private (bool wait, int bytesWritten) TryWriteToBuffer(ReadOnlySpan<byte> buffer)
        {
            Debug.Assert(buffer.Length > 0);

            Debug.Assert(!Monitor.IsEntered(SyncObject));
            lock (SyncObject)
            {
                if (_writeEnded)
                {
                    throw new InvalidOperationException();
                }

                if (_readAborted)
                {
                    return (false, buffer.Length);
                }

                _buffer.EnsureAvailableSpaceUpToLimit(buffer.Length, _maxBufferSize);

                int bytesWritten = Math.Min(buffer.Length, _buffer.AvailableMemory.Length);
                if (bytesWritten > 0)
                {
                    _buffer.AvailableMemory.CopyFrom(buffer.Slice(0, bytesWritten));
                    _buffer.Commit(bytesWritten);

                    _readTaskSource.SignalWaiter();
                }

                buffer = buffer.Slice(bytesWritten);
                if (buffer.Length == 0)
                {
                    return (false, bytesWritten);
                }

                _writeTaskSource.Reset();

                return (true, bytesWritten);
            }
        }

        public void Write(ReadOnlySpan<byte> buffer)
        {
            if (buffer.Length == 0)
            {
                return;
            }

            while (true)
            {
                (bool wait, int bytesWritten) = TryWriteToBuffer(buffer);
                if (!wait)
                {
                    Debug.Assert(bytesWritten == buffer.Length);
                    break;
                }

                buffer = buffer.Slice(bytesWritten);
                _writeTaskSource.Wait();
            }
        }

        public async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (buffer.Length == 0)
            {
                return;
            }

            while (true)
            {
                (bool wait, int bytesWritten) = TryWriteToBuffer(buffer.Span);
                if (!wait)
                {
                    Debug.Assert(bytesWritten == buffer.Length);
                    break;
                }

                buffer = buffer.Slice(bytesWritten);
                await _writeTaskSource.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public void EndWrite()
        {
            Debug.Assert(!Monitor.IsEntered(SyncObject));
            lock (SyncObject)
            {
                if (_writeEnded)
                {
                    return;
                }

                _writeEnded = true;

                _readTaskSource.SignalWaiter();
            }
        }

        private (bool wait, int bytesRead) TryReadFromBuffer(Span<byte> buffer)
        {
            Debug.Assert(buffer.Length > 0);

            Debug.Assert(!Monitor.IsEntered(SyncObject));
            lock (SyncObject)
            {
                if (_readAborted)
                {
                    return (false, 0);
                }

                if (!_buffer.IsEmpty)
                {
                    int bytesRead = Math.Min(buffer.Length, _buffer.ActiveMemory.Length);
                    _buffer.ActiveMemory.Slice(0, bytesRead).CopyTo(buffer);
                    _buffer.Discard(bytesRead);

                    _writeTaskSource.SignalWaiter();

                    return (false, bytesRead);
                }
                else if (_writeEnded)
                {
                    return (false, 0);
                }

                _readTaskSource.Reset();

                return (true, 0);
            }
        }

        public int Read(Span<byte> buffer)
        {
            if (buffer.Length == 0)
            {
                return 0;
            }

            (bool wait, int bytesRead) = TryReadFromBuffer(buffer);
            if (wait)
            {
                Debug.Assert(bytesRead == 0);
                _readTaskSource.Wait();
                (wait, bytesRead) = TryReadFromBuffer(buffer);
                Debug.Assert(!wait);
            }

            return bytesRead;
        }

        public async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (buffer.Length == 0)
            {
                return 0;
            }

            (bool wait, int bytesRead) = TryReadFromBuffer(buffer.Span);
            if (wait)
            {
                Debug.Assert(bytesRead == 0);
                await _readTaskSource.WaitAsync(cancellationToken).ConfigureAwait(false);
                (wait, bytesRead) = TryReadFromBuffer(buffer.Span);
                Debug.Assert(!wait);
            }

            return bytesRead;
        }

        // Note, this can be called while a read is in progress, and will cause it to return 0 bytes.
        // Caller can then check IsAborted if appropriate to distinguish between EOF and abort.
        public void AbortRead()
        {
            Debug.Assert(!Monitor.IsEntered(SyncObject));
            lock (SyncObject)
            {
                if (_readAborted)
                {
                    return;
                }

                _readAborted = true;
                _buffer.DiscardAll();

                _readTaskSource.SignalWaiter();
                _writeTaskSource.SignalWaiter();
            }
        }

        public void Dispose()
        {
            AbortRead();
            EndWrite();

            lock (SyncObject)
            {
                _buffer.Dispose();
            }
        }

        private sealed class ResettableValueTaskSource : IValueTaskSource
        {
            // This object is used as the backing source for ValueTask.
            // There should only ever be one awaiter at a time; users of this object must ensure this themselves.
            // We use _hasWaiter to ensure mutual exclusion between successful completion and cancellation,
            // and dispose/clear the cancellation registration in GetResult to guarantee it will not affect subsequent waiters.
            // The rest of the logic is deferred to ManualResetValueTaskSourceCore.

            private ManualResetValueTaskSourceCore<bool> _waitSource; // mutable struct, do not make this readonly
            private CancellationTokenRegistration _waitSourceCancellation;
            private int _hasWaiter;

            ValueTaskSourceStatus IValueTaskSource.GetStatus(short token) => _waitSource.GetStatus(token);

            void IValueTaskSource.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _waitSource.OnCompleted(continuation, state, token, flags);

            void IValueTaskSource.GetResult(short token)
            {
                Debug.Assert(_hasWaiter == 0);

                // Clean up the registration.  This will wait for any in-flight cancellation to complete.
                _waitSourceCancellation.Dispose();
                _waitSourceCancellation = default;

                // Propagate any exceptions if there were any.
                _waitSource.GetResult(token);
            }

            public void SignalWaiter()
            {
                if (Interlocked.Exchange(ref _hasWaiter, 0) == 1)
                {
                    _waitSource.SetResult(true);
                }
            }

            private void CancelWaiter(CancellationToken cancellationToken)
            {
                Debug.Assert(cancellationToken.IsCancellationRequested);

                if (Interlocked.Exchange(ref _hasWaiter, 0) == 1)
                {
                    _waitSource.SetException(ExceptionDispatchInfo.SetCurrentStackTrace(new OperationCanceledException(cancellationToken)));
                }
            }

            public void Reset()
            {
                if (_hasWaiter != 0)
                {
                    throw new InvalidOperationException("Concurrent use is not supported");
                }

                _waitSource.Reset();
                Volatile.Write(ref _hasWaiter, 1);
            }

            public void Wait()
            {
                _waitSource.RunContinuationsAsynchronously = false;
                new ValueTask(this, _waitSource.Version).AsTask().GetAwaiter().GetResult();
            }

            public ValueTask WaitAsync(CancellationToken cancellationToken)
            {
                _waitSource.RunContinuationsAsynchronously = true;

                _waitSourceCancellation = cancellationToken.UnsafeRegister(static (s, token) => ((ResettableValueTaskSource)s!).CancelWaiter(token), this);

                return new ValueTask(this, _waitSource.Version);
            }
        }
    }
}
