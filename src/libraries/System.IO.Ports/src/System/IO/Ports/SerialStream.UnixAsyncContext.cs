// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using Signals = Interop.Termios.Signals;

namespace System.IO.Ports
{
    internal sealed partial class SerialStream : Stream
    {
        // Track how many bytes were available when we raised DataReceived.
        // We use this to track how many bytes were read and start watching for new data when the user consumed enough.
        private volatile int _cachedBytesAvailable;
        private int _waitingForReceiveThreshold;

        internal int Read(byte[] array, int offset, int count, int timeout)
        {
            CheckReadWriteArguments(array, offset, count);

            if (count == 0)
                return 0;

            return _handle.Read(new Span<byte>(array, offset, count), MapTimeout(timeout), this);
        }

        public override Task<int> ReadAsync(byte[] array, int offset, int count, CancellationToken cancellationToken)
        {
            CheckReadWriteArguments(array, offset, count);

            if (count == 0)
                return Task<int>.FromResult(0);

            return _handle.ReadAsync(new Memory<byte>(array, offset, count), cancellationToken, this).AsTask();
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            CheckHandle();

            if (buffer.IsEmpty)
                return new ValueTask<int>(0);

            return _handle.ReadAsync(buffer, cancellationToken, this);
        }

        public override Task WriteAsync(byte[] array, int offset, int count, CancellationToken cancellationToken)
        {
            CheckWriteArguments(array, offset, count);

            if (count == 0)
                return Task.CompletedTask;

            return _handle.WriteAsync(new ReadOnlyMemory<byte>(array, offset, count), cancellationToken).AsTask();
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            CheckWriteArguments();

            if (buffer.IsEmpty)
                return ValueTask.CompletedTask;

            return _handle.WriteAsync(buffer, cancellationToken);
        }

        internal void Write(byte[] array, int offset, int count, int timeout)
        {
            CheckWriteArguments(array, offset, count);

            if (count == 0)
                return;

            _handle.Write(new ReadOnlySpan<byte>(array, offset, count), MapTimeout(timeout));
        }

        private void DataReceiveEnable()
            => EnsureWaitForReceiveThreshold();

        private void FlushWrites()
        {
            // A zero-byte write doesn't actually write, but it won't complete until the preceding writes are handled.
            _handle.Write(ReadOnlySpan<byte>.Empty, Timeout.Infinite);
        }

#pragma warning disable CA1822
        private void OnCtor() { }
        private void FinishPendingIORequests() { }
#pragma warning restore CA1822

        internal void DiscardInBuffer()
        {
            if (_handle == null) InternalResources.FileNotOpen();

            _handle.DiscardInBuffer();

            // Arming readable by reading (via OnBytesRead) might not happen due to clearing the readable data.
            // Ensure we're armed.
            if (_dataReceived != null)
            {
                EnsureWaitForReceiveThreshold();
            }
        }

        internal void OnBytesRead(int bytesRead)
        {
            // Start watching for new data received event when the user consumed the previous data.
            Debug.Assert(bytesRead > 0);

            int remaining = Interlocked.Add(ref _cachedBytesAvailable, -bytesRead);
            // Clamp to zero: direct reads (without a prior OnReceiveThreshold) can make this negative.
            if (remaining < 0)
            {
                _cachedBytesAvailable = 0;
            }

            if (remaining < ReceivedBytesThreshold && _dataReceived != null)
            {
                EnsureWaitForReceiveThreshold();
            }
        }

        private void EnsureWaitForReceiveThreshold()
        {
            SafeSerialDeviceHandle? handle = _handle;
            if (handle != null && Interlocked.CompareExchange(ref _waitingForReceiveThreshold, 1, 0) == 0)
            {
                handle.WaitForReceiveThreshold(this);
            }
        }

        internal void OnReceiveThreshold(int bytesAvailable, SafeSerialDeviceHandle.ReceiveThresholdResult result)
        {
            Volatile.Write(ref _waitingForReceiveThreshold, 0);

            if (_dataReceived == null)
            {
                return;
            }

            _cachedBytesAvailable = bytesAvailable;

            if (result == SafeSerialDeviceHandle.ReceiveThresholdResult.Success)
            {
                RaiseDataReceivedChars();
            }
            // Emit Eof for errors too so that the user gets "an" event to trigger a read.
            else if (result is SafeSerialDeviceHandle.ReceiveThresholdResult.Eof or SafeSerialDeviceHandle.ReceiveThresholdResult.Error)
            {
                RaiseDataReceivedEof();
            }
            else if (result == SafeSerialDeviceHandle.ReceiveThresholdResult.Disposed)
            { }
            else
            {
                OnRaiseCharsEventSkipped();
            }
        }

        internal void OnRaiseCharsEventSkipped()
        {
            // Unlikely: we didn't meet the threshold because it changed.
            EnsureWaitForReceiveThreshold();
        }

        // This is the same loop as UnixPollLoop implementation, but limited to the pin polling.
        private void IOLoop()
        {
            Signals lastSignals = Interop.Termios.TermiosGetAllSignals(_handle);
            bool lastIsIdle = false;
            int ticksWhenIdleStarted = 0;

            while (IsOpen && !_disposed)
            {
                bool isIdle = _pinChanged == null;

                if (isIdle)
                {
                    if (!lastIsIdle)
                    {
                        ticksWhenIdleStarted = Environment.TickCount;
                    }
                    else if (Environment.TickCount - ticksWhenIdleStarted > IOLoopIdleTimeout)
                    {
                        lock (_ioLoopLock)
                        {
                            if (_pinChanged == null)
                            {
                                _ioLoop = null;
                                break;
                            }
                            else
                            {
                                lastIsIdle = false;
                                continue;
                            }
                        }
                    }
                }

                Thread.Sleep(1);

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

        private static int MapTimeout(int timeoutMs)
        {
            // SerialPort.InfiniteTimeout is -1, which maps to infinite for ReadSync/WriteSync.
            // Positive values pass through.
            return timeoutMs == SerialPort.InfiniteTimeout ? -1 : Math.Max(timeoutMs, TimeoutResolution);
        }
    }
}
