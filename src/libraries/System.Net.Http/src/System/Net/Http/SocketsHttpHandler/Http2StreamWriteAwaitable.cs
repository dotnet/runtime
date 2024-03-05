// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace System.Net.Http
{
    internal sealed partial class Http2Connection
    {
        /// <summary>
        /// Serves as a reusable <see cref="IValueTaskSource"/> for writing stream headers and data to an <see cref="Http2Connection"/>.
        /// Also manages the stream window size, such as chunking of large writes while waiting for window updates.
        /// </summary>
        private sealed class Http2StreamWriteAwaitable : IValueTaskSource
        {
            private static readonly Action<object?, CancellationToken> s_cancelThisAwaitableCallback = static (state, cancellationToken) =>
            {
                Http2StreamWriteAwaitable thisRef = (Http2StreamWriteAwaitable)state!;

                // We've hit a cancellation. Give the stream a chance to throw a more informative reset exception instead.
                Exception exception =
                    thisRef.Stream.ReplaceExceptionOnRequestBodyCancellationIfNeeded() ??
                    ExceptionDispatchInfo.SetCurrentStackTrace(
                        CancellationHelper.CreateOperationCanceledException(innerException: null, cancellationToken));

                thisRef._waitSource.SetException(exception);
            };

            private static readonly Action<object?> s_cancelLinkedCtsCallback = static state =>
            {
                ((CancellationTokenSource)state!).Cancel(throwOnFirstException: false);
            };

            public readonly Http2Stream Stream;

            private int _streamWindow;
            private bool _waitingForMoreStreamWindow;
            private readonly object _windowUpdateLock = new();

            private ManualResetValueTaskSourceCore<bool> _waitSource = new() { RunContinuationsAsynchronously = true };
            private readonly CancellationTokenSource _requestBodyCTS;
            private CancellationToken _cancellationTokenForCurrentWrite;
            private CancellationTokenRegistration _cancelThisAwaitableRegistration;
            private CancellationTokenRegistration _cancelRequestCtsRegistration;

            public Http2StreamWriteAwaitable(Http2Stream stream, CancellationTokenSource requestBodyCTS)
            {
                Stream = stream;
                _requestBodyCTS = requestBodyCTS;
            }

            public bool WritingHeaders { get; private set; }
            public bool ShouldFlushAfterData { get; private set; }
            public ReadOnlyMemory<byte> DataRemaining { get; set; }
            public int FlushCounterAtLastDataWrite { get; set; }

            public void SetInitialStreamWindow(int initialWindowSize)
            {
                Debug.Assert(_streamWindow == 0);
                Debug.Assert(!_waitingForMoreStreamWindow);

                _streamWindow = initialWindowSize;
            }

            public void AdjustStreamWindow(int delta)
            {
                int newStreamWindow = Interlocked.Add(ref _streamWindow, delta);

                if (newStreamWindow > delta || newStreamWindow <= 0)
                {
                    // We already had some window available, or we're still in the negatives.
                    return;
                }

                // We just added enough window to be able to send some data.
                // If there's a waiter waiting for window, wake it up.

                lock (_windowUpdateLock)
                {
                    if (!_waitingForMoreStreamWindow)
                    {
                        return;
                    }

                    _waitingForMoreStreamWindow = false;
                }

                // Wake up the waiter in WaitForStreamWindowAndWriteDataAsync.
                if (TryDisableCancellation())
                {
                    SetResult();
                }
            }

            public void Complete()
            {
                lock (_windowUpdateLock)
                {
                    if (!_waitingForMoreStreamWindow)
                    {
                        return;
                    }

                    _waitingForMoreStreamWindow = false;
                }

                // Wake up the waiter in WaitForStreamWindowAndWriteDataAsync.
                if (TryDisableCancellation())
                {
                    SetException(ExceptionDispatchInfo.SetCurrentStackTrace(new ObjectDisposedException(nameof(Http2StreamWriteAwaitable), SR.net_http_disposed_while_in_use)));
                }
            }

            public ValueTask WriteStreamDataAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
            {
                Debug.Assert(!_waitingForMoreStreamWindow);

                if (data.IsEmpty)
                {
                    return default;
                }

                int newStreamWindow = Interlocked.Add(ref _streamWindow, -data.Length);
                if (newStreamWindow >= 0)
                {
                    // Common case: the entire write can be satisfied from the currently available stream window.
                    // If we just ran out of stream window, make sure to flush.
                    SetupForWrite(data, writingHeaders: false, shouldFlush: newStreamWindow == 0, cancellationToken);
                    ScheduleStreamWrite();
                    return AsValueTask();
                }

                return WaitForStreamWindowAndWriteDataAsync(data, cancellationToken);
            }

            private async ValueTask WaitForStreamWindowAndWriteDataAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
            {
                // Account for the window we just tried to use in WriteStreamDataAsync.
                Interlocked.Add(ref _streamWindow, data.Length);

                while (!data.IsEmpty)
                {
                    Debug.Assert(!_waitingForMoreStreamWindow);
                    int windowAvailable = 0;

                    lock (_windowUpdateLock)
                    {
                        if (_streamWindow > 0)
                        {
                            windowAvailable = Math.Min(_streamWindow, data.Length);
                            Interlocked.Add(ref _streamWindow, -windowAvailable);
                        }
                        else
                        {
                            // We're reusing the ValueTaskSource infrastructure to efficiently wait for the stream window to become available.
                            // These arguments to SetupForWrite will be ignored.
                            SetupForWrite(ReadOnlyMemory<byte>.Empty, writingHeaders: false, shouldFlush: false, cancellationToken);

                            _waitingForMoreStreamWindow = true;
                        }
                    }

                    if (windowAvailable == 0)
                    {
                        // Logically this is part of the else block above, but we can't await while holding the lock.
                        await AsValueTask().ConfigureAwait(false);
                        Debug.Assert(!_waitingForMoreStreamWindow);
                        continue;
                    }

                    // We have some stream window available, so we can write some data.

                    // Keep flushing writes as long as we're running out of the stream window.
                    bool shouldFlush = data.Length >= windowAvailable;

                    ReadOnlyMemory<byte> currentChunk = data.Slice(0, windowAvailable);
                    data = data.Slice(currentChunk.Length);

                    SetupForWrite(currentChunk, writingHeaders: false, shouldFlush, cancellationToken);
                    ScheduleStreamWrite();
                    await AsValueTask().ConfigureAwait(false);
                }
            }

            public void QueueStreamDataFlushIfNeeded() =>
                Stream.Connection._frameWriter.QueueStreamDataFlushIfNeeded(this);

            public ValueTask SendHeadersAsync(ReadOnlyMemory<byte> headers, CancellationToken cancellationToken)
            {
                Debug.Assert(!headers.IsEmpty);
                Debug.Assert(_streamWindow == 0);
                Debug.Assert(!_waitingForMoreStreamWindow);

                SetupForWrite(headers, writingHeaders: true, shouldFlush: false, cancellationToken);
                ScheduleStreamWrite();
                return AsValueTask();
            }

            private void SetupForWrite(ReadOnlyMemory<byte> buffer, bool writingHeaders, bool shouldFlush, CancellationToken cancellationToken)
            {
                Debug.Assert(DataRemaining.IsEmpty);
                Debug.Assert(_cancellationTokenForCurrentWrite == default);
                Debug.Assert(_cancelThisAwaitableRegistration == default);
                Debug.Assert(_cancelRequestCtsRegistration == default);
                Debug.Assert(_waitSource.GetStatus(_waitSource.Version) == ValueTaskSourceStatus.Pending);

                DataRemaining = buffer;
                WritingHeaders = writingHeaders;
                ShouldFlushAfterData = shouldFlush;

                _cancellationTokenForCurrentWrite = cancellationToken;
                _cancelRequestCtsRegistration = cancellationToken.UnsafeRegister(s_cancelLinkedCtsCallback, _requestBodyCTS);
                _cancelThisAwaitableRegistration = _requestBodyCTS.Token.UnsafeRegister(s_cancelThisAwaitableCallback, this);
            }

            private ValueTask AsValueTask() =>
                new ValueTask(this, _waitSource.Version);

            private void ScheduleStreamWrite() =>
                Stream.Connection._frameWriter.ScheduleStreamWrite(this);

            public bool TryDisableCancellation()
            {
                Debug.Assert(_waitSource.GetStatus(_waitSource.Version) != ValueTaskSourceStatus.Succeeded);

                // Only disable _cancelThisAwaitableRegistration.
                // We can keep _cancelRequestCtsRegistration around in case we need to re-register later.
                _cancelThisAwaitableRegistration.Dispose();
                _cancelThisAwaitableRegistration = default;

                // Checking GetStatus here instead of _requestBodyCTS.IsCancellationRequested as
                // the latter may be canceled by other threads even after we've disabled our registration.
                return _waitSource.GetStatus(_waitSource.Version) == ValueTaskSourceStatus.Pending;
            }

            public bool TryReRegisterForCancellation()
            {
                Debug.Assert(_waitSource.GetStatus(_waitSource.Version) == ValueTaskSourceStatus.Pending);
                Debug.Assert(_cancelThisAwaitableRegistration == default);

                _cancelThisAwaitableRegistration = _requestBodyCTS.Token.UnsafeRegister(s_cancelThisAwaitableCallback, this);

                return !_requestBodyCTS.IsCancellationRequested;
            }

            public void SetException(Exception exception)
            {
                Debug.Assert(_waitSource.GetStatus(_waitSource.Version) == ValueTaskSourceStatus.Pending);
                Debug.Assert(_cancelThisAwaitableRegistration == default);

                _waitSource.SetException(exception);
            }

            public void SetResult()
            {
                Debug.Assert(_waitSource.GetStatus(_waitSource.Version) == ValueTaskSourceStatus.Pending);
                Debug.Assert(_cancelThisAwaitableRegistration == default);

                _waitSource.SetResult(false);
            }

            ValueTaskSourceStatus IValueTaskSource.GetStatus(short token) =>
                _waitSource.GetStatus(token);

            void IValueTaskSource.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) =>
                _waitSource.OnCompleted(continuation, state, token, flags);

            void IValueTaskSource.GetResult(short token)
            {
                _cancelRequestCtsRegistration.Dispose();
                _cancelRequestCtsRegistration = default;

                _cancelThisAwaitableRegistration.Dispose();
                _cancelThisAwaitableRegistration = default;

                _cancellationTokenForCurrentWrite = default;

                DataRemaining = default;

                _waitSource.GetResult(token);

                // A Http2StreamWriteAwaitable should never be reused after being canceled or faulting.
                Debug.Assert(_waitSource.GetStatus(_waitSource.Version) == ValueTaskSourceStatus.Succeeded);
                _waitSource.Reset();
            }
        }
    }
}
