// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Signals = Interop.Termios.Signals;

namespace System.IO.Ports
{
    internal sealed partial class SerialStream : Stream
    {
        // Use a Queue with locking instead of ConcurrentQueue because ConcurrentQueue preserves segments for
        // observation when using TryPeek(). These segments will not clear out references after a dequeue
        // and as a result they hold on to SerialStreamIORequest instances so that they cannot be GC'ed.
        // This in turn means that any buffers that the client supplied are not eligible for GC either.
        private readonly Queue<SerialStreamIORequest> _readQueue = new();
        private readonly object _readQueueLock = new();
        private readonly Queue<SerialStreamIORequest> _writeQueue = new();
        private readonly object _writeQueueLock = new();

        private long _totalBytesRead;
        private long TotalBytesAvailable => _totalBytesRead + GetBytesToRead(buffered: 0);
        private long _lastTotalBytesAvailable;

        private void DataReceiveEnable()
        {
            EnsureIOLoopRunning();
        }

        private bool HasCancelledTasksToProcess
        {
            get => Volatile.Read(ref field);
            set => Volatile.Write(ref field, value);
        }
        internal void DiscardInBuffer()
        {
            if (_handle == null) InternalResources.FileNotOpen();
            // This may or may not work depending on hardware.
            Interop.Termios.TermiosDiscard(_handle, Interop.Termios.Queue.ReceiveQueue);
        }

        private void FlushWrites()
        {
            SpinWait sw = default;
            while (!IsWriteQueueEmpty())
            {
                sw.SpinOnce();
            }
        }

        internal int Read(byte[] array, int offset, int count, int timeout)
        {
            using (CancellationTokenSource cts = GetCancellationTokenSourceFromTimeout(timeout))
            {
                Task<int> t = ReadAsync(array, offset, count, cts?.Token ?? CancellationToken.None);

                try
                {
                    return t.GetAwaiter().GetResult();
                }
                catch (OperationCanceledException)
                {
                    throw new TimeoutException();
                }
            }
        }

        public override Task<int> ReadAsync(byte[] array, int offset, int count, CancellationToken cancellationToken)
        {
            CheckReadWriteArguments(array, offset, count);

            if (count == 0)
                return Task<int>.FromResult(0); // return immediately if no bytes requested; no need for overhead.

            Memory<byte> buffer = new Memory<byte>(array, offset, count);
            SerialStreamReadRequest result = new SerialStreamReadRequest(this, cancellationToken, buffer);
            lock (_readQueueLock)
            {
                _readQueue.Enqueue(result);
            }

            EnsureIOLoopRunning();

            return result.Task;
        }

#if !NETFRAMEWORK && !NETSTANDARD2_0
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            CheckHandle();

            if (buffer.IsEmpty)
                return new ValueTask<int>(0);

            SerialStreamReadRequest result = new SerialStreamReadRequest(this, cancellationToken, buffer);
            lock (_readQueueLock)
            {
                _readQueue.Enqueue(result);
            }

            EnsureIOLoopRunning();

            return new ValueTask<int>(result.Task);
        }
#endif

        public override Task WriteAsync(byte[] array, int offset, int count, CancellationToken cancellationToken)
        {
            CheckWriteArguments(array, offset, count);

            if (count == 0)
                return Task.CompletedTask; // return immediately if no bytes to write; no need for overhead.

            ReadOnlyMemory<byte> buffer = new ReadOnlyMemory<byte>(array, offset, count);
            SerialStreamWriteRequest result = new SerialStreamWriteRequest(this, cancellationToken, buffer);
            lock (_writeQueueLock)
            {
                _writeQueue.Enqueue(result);
            }

            EnsureIOLoopRunning();

            return result.Task;
        }

#if !NETFRAMEWORK && !NETSTANDARD2_0
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            CheckWriteArguments();

            if (buffer.IsEmpty)
                return ValueTask.CompletedTask; // return immediately if no bytes to write; no need for overhead.

            SerialStreamWriteRequest result = new SerialStreamWriteRequest(this, cancellationToken, buffer);
            lock (_writeQueueLock)
            {
                _writeQueue.Enqueue(result);
            }

            EnsureIOLoopRunning();

            return new ValueTask(result.Task);
        }
#endif

        // Will wait `timeout` miliseconds or until reading or writing is possible
        // If no operation is requested it will throw
        // Returns event which has happened
        private Interop.PollEvents PollEvents(int timeout, bool pollReadEvents, bool pollWriteEvents, out Interop.ErrorInfo? error)
        {
            if (!pollReadEvents && !pollWriteEvents)
            {
                Debug.Fail("This should not happen");
                throw new Exception();
            }

            Interop.PollEvents eventsToPoll = Interop.PollEvents.POLLERR;

            if (pollReadEvents)
            {
                eventsToPoll |= Interop.PollEvents.POLLIN;
            }

            if (pollWriteEvents)
            {
                eventsToPoll |= Interop.PollEvents.POLLOUT;
            }

            Interop.PollEvents events;
            Interop.Error ret = Interop.Serial.Poll(
                _handle,
                eventsToPoll,
                timeout,
                out events);

            error = ret != Interop.Error.SUCCESS ? Interop.Sys.GetLastErrorInfo() : (Interop.ErrorInfo?)null;
            return events;
        }

        internal void Write(byte[] array, int offset, int count, int timeout)
        {
            using (CancellationTokenSource cts = GetCancellationTokenSourceFromTimeout(timeout))
            {
                Task t = WriteAsync(array, offset, count, cts?.Token ?? CancellationToken.None);

                try
                {
                    t.GetAwaiter().GetResult();
                }
                catch (OperationCanceledException)
                {
                    throw new TimeoutException();
                }
            }
        }

        private void OnCtor()
        {
            _processReadDelegate = ProcessRead;
            _processWriteDelegate = ProcessWrite;
            _lastTotalBytesAvailable = TotalBytesAvailable;
        }

#pragma warning disable CA1822
        internal void OnRaiseCharsEventSkipped()
        {
        }
#pragma warning restore CA1822

        private void FinishPendingIORequests(Interop.ErrorInfo? error = null)
        {
            lock (_readQueueLock)
            {
                while (_readQueue.TryDequeue(out SerialStreamIORequest r))
                {
                    r.Complete(error.HasValue ?
                               Interop.GetIOException(error.Value) :
                               InternalResources.FileNotOpenException());
                }
            }

            lock (_writeQueueLock)
            {
                while (_writeQueue.TryDequeue(out SerialStreamIORequest r))
                {
                    r.Complete(error.HasValue ?
                               Interop.GetIOException(error.Value) :
                               InternalResources.FileNotOpenException());
                }
            }
        }

        // should return non-negative integer meaning numbers of bytes read/written (0 for errors)
        private delegate int RequestProcessor(SerialStreamIORequest r);
        private RequestProcessor _processReadDelegate;
        private RequestProcessor _processWriteDelegate;

        private unsafe int ProcessRead(SerialStreamIORequest r)
        {
            SerialStreamReadRequest readRequest = (SerialStreamReadRequest)r;
            Span<byte> buff = readRequest.Buffer.Span;
            fixed (byte* bufPtr = buff)
            {
                // assumes dequeue-ing happens on a single thread
                int numBytes = Interop.Serial.Read(_handle, bufPtr, buff.Length);

                if (numBytes < 0)
                {
                    Interop.ErrorInfo lastError = Interop.Sys.GetLastErrorInfo();

                    // ignore EWOULDBLOCK since we handle timeout elsewhere
                    if (lastError.Error != Interop.Error.EWOULDBLOCK)
                    {
                        readRequest.Complete(Interop.GetIOException(lastError));
                    }
                }
                else if (numBytes > 0)
                {
                    readRequest.Complete(numBytes);
                    return numBytes;
                }
                else // numBytes == 0
                {
                    RaiseDataReceivedEof();
                }
            }

            return 0;
        }

        private unsafe int ProcessWrite(SerialStreamIORequest r)
        {
            SerialStreamWriteRequest writeRequest = (SerialStreamWriteRequest)r;
            ReadOnlySpan<byte> buff = writeRequest.Buffer.Span;
            fixed (byte* bufPtr = buff)
            {
                // assumes dequeue-ing happens on a single thread
                int numBytes = Interop.Serial.Write(_handle, bufPtr, buff.Length);

                if (numBytes <= 0)
                {
                    Interop.ErrorInfo lastError = Interop.Sys.GetLastErrorInfo();

                    // ignore EWOULDBLOCK since we handle timeout elsewhere
                    // numBytes == 0 means that there might be an error
                    if (lastError.Error != Interop.Error.SUCCESS && lastError.Error != Interop.Error.EWOULDBLOCK)
                    {
                        r.Complete(Interop.GetIOException(lastError));
                    }
                }
                else
                {
                    writeRequest.ProcessBytes(numBytes);

                    if (writeRequest.Buffer.Length == 0)
                    {
                        writeRequest.Complete();
                    }

                    return numBytes;
                }
            }

            return 0;
        }

        // returns number of bytes read/written
        private static int DoIORequest(Queue<SerialStreamIORequest> q, object queueLock, RequestProcessor op)
        {
            // assumes dequeue-ing happens on a single thread
            while (TryPeekNextRequest(out SerialStreamIORequest r))
            {
                int ret = op(r);
                Debug.Assert(ret >= 0);

                if (r.IsCompleted)
                {
                    lock (queueLock)
                    {
                        q.TryDequeue(out _);
                    }
                }

                return ret;
            }

            return 0;

            bool TryPeekNextRequest(out SerialStreamIORequest r)
            {
                lock (queueLock)
                {
                    while (q.TryPeek(out r))
                    {
                        if (!r.IsCompleted)
                        {
                            return true;
                        }
                        q.TryDequeue(out _);
                    }
                }
                r = default;
                return false;
            }
        }

        private void IOLoop()
        {
            bool eofReceived = false;
            // we do not care about bytes we got before - only about changes
            // loop just got started which means we just got request
            bool lastIsIdle = false;
            int ticksWhenIdleStarted = 0;

            Signals lastSignals = _pinChanged != null ? Interop.Termios.TermiosGetAllSignals(_handle) : Signals.Error;

            bool IsNoEventRegistered() => _dataReceived == null && _pinChanged == null;

            while (IsOpen && !eofReceived && !_disposed)
            {
                if (HasCancelledTasksToProcess)
                {
                    HasCancelledTasksToProcess = false;
                    RemoveCompletedTasks(_readQueue, _readQueueLock);
                    RemoveCompletedTasks(_writeQueue, _writeQueueLock);
                }

                bool hasPendingReads = !IsReadQueueEmpty();
                bool hasPendingWrites = !IsWriteQueueEmpty();

                bool hasPendingIO = hasPendingReads || hasPendingWrites;
                bool isIdle = IsNoEventRegistered() && !hasPendingIO;

                if (!hasPendingIO)
                {
                    if (isIdle)
                    {
                        if (!lastIsIdle)
                        {
                            // we've just started idling
                            ticksWhenIdleStarted = Environment.TickCount;
                        }
                        else if (Environment.TickCount - ticksWhenIdleStarted > IOLoopIdleTimeout)
                        {
                            // we are already idling for a while
                            // let's stop the loop until there is some work to do

                            lock (_ioLoopLock)
                            {
                                // double check we are done under lock
                                if (IsNoEventRegistered() && IsReadQueueEmpty() && IsWriteQueueEmpty())
                                {
                                    _ioLoop = null;
                                    break;
                                }
                                else
                                {
                                    // to make sure timer restarts
                                    lastIsIdle = false;
                                    continue;
                                }
                            }
                        }
                    }

                    Thread.Sleep(1);
                }
                else
                {
                    Interop.PollEvents events = PollEvents(1,
                                                               pollReadEvents: hasPendingReads,
                                                               pollWriteEvents: hasPendingWrites,
                                                               out Interop.ErrorInfo? error);

                    if (error.HasValue)
                    {
                        FinishPendingIORequests(error);
                        break;
                    }

                    if (events.HasFlag(Interop.PollEvents.POLLNVAL) ||
                        events.HasFlag(Interop.PollEvents.POLLERR))
                    {
                        // bad descriptor or some other error we can't handle
                        FinishPendingIORequests();
                        break;
                    }

                    if (events.HasFlag(Interop.PollEvents.POLLIN))
                    {
                        int bytesRead = DoIORequest(_readQueue, _readQueueLock, _processReadDelegate);
                        _totalBytesRead += bytesRead;
                    }

                    if (events.HasFlag(Interop.PollEvents.POLLOUT))
                    {
                        DoIORequest(_writeQueue, _writeQueueLock, _processWriteDelegate);
                    }
                }

                // check if there is any new data (either already read or in the driver input)
                // this event is private and handled inside of SerialPort
                // which then throttles it with the threshold
                long totalBytesAvailable = TotalBytesAvailable;
                if (totalBytesAvailable > _lastTotalBytesAvailable)
                {
                    _lastTotalBytesAvailable = totalBytesAvailable;
                    RaiseDataReceivedChars();
                }

                if (_pinChanged != null)
                {
                    // Checking for changes could technically speaking be done by waiting with ioctl+TIOCMIWAIT
                    // This would require spinning new thread and also would potentially trigger events when
                    // user didn't have time to respond.
                    // Diffing seems like a better solution.
                    Signals current = Interop.Termios.TermiosGetAllSignals(_handle);

                    // There is no really good action we can take when this errors so just ignore
                    // a sinle event.
                    if (current != Signals.Error && lastSignals != Signals.Error)
                    {
                        Signals changed = current ^ lastSignals;
                        if (changed != Signals.None)
                        {
                            NotifyPinChanges(changed);
                        }
                    }

                    lastSignals = current;
                }

                lastIsIdle = isIdle;
            }
        }

        private static void RemoveCompletedTasks(Queue<SerialStreamIORequest> queue, object queueLock)
        {
            // assumes dequeue-ing happens on a single thread
            lock (queueLock)
            {
                while (queue.TryPeek(out var r) && r.IsCompleted)
                    queue.TryDequeue(out _);
            }
        }

        private bool IsReadQueueEmpty()
        {
            lock (_readQueueLock)
            {
                return _readQueue.Count == 0;
            }
        }

        private bool IsWriteQueueEmpty()
        {
            lock (_writeQueueLock)
            {
                return _writeQueue.Count == 0;
            }
        }

        private static CancellationTokenSource GetCancellationTokenSourceFromTimeout(int timeoutMs)
        {
            return timeoutMs == SerialPort.InfiniteTimeout ?
                null :
                new CancellationTokenSource(Math.Max(timeoutMs, TimeoutResolution));
        }

        private abstract class SerialStreamIORequest : TaskCompletionSource<int>
        {
            public bool IsCompleted => Task.IsCompleted;
            private readonly SerialStream _parent;
            private readonly CancellationTokenRegistration _cancellationTokenRegistration;

            protected SerialStreamIORequest(SerialStream parent, CancellationToken ct)
                : base(TaskCreationOptions.RunContinuationsAsynchronously)
            {
                _parent = parent;
                _cancellationTokenRegistration = ct.Register(s =>
                {
                    var request = (SerialStreamIORequest)s;
                    request.TrySetCanceled();
                    request._parent.HasCancelledTasksToProcess = true;
                }, this);
            }

            internal void Complete(int numBytes)
            {
                TrySetResult(numBytes);
                _cancellationTokenRegistration.Dispose();
            }

            internal void Complete(Exception exception)
            {
                TrySetException(exception);
                _cancellationTokenRegistration.Dispose();
            }
        }

        private sealed class SerialStreamReadRequest : SerialStreamIORequest
        {
            public Memory<byte> Buffer { get; }

            public SerialStreamReadRequest(SerialStream parent, CancellationToken ct, Memory<byte> buffer)
                : base(parent, ct)
            {
                Buffer = buffer;
            }
        }

        private sealed class SerialStreamWriteRequest : SerialStreamIORequest
        {
            public ReadOnlyMemory<byte> Buffer { get; private set; }

            public SerialStreamWriteRequest(SerialStream parent, CancellationToken ct, ReadOnlyMemory<byte> buffer)
                : base(parent, ct)
            {
                Buffer = buffer;
            }

            internal void Complete()
            {
                Debug.Assert(Buffer.Length == 0);
                Complete(Buffer.Length);
            }

            internal void ProcessBytes(int numBytes)
            {
                Buffer = Buffer.Slice(numBytes);
            }
        }
    }
}
