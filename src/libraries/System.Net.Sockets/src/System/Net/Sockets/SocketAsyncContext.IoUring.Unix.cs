// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Threading;

namespace System.Net.Sockets
{
    // EXPERIMENTAL, PROTOTYPE-ONLY: implements "Architecture B" (true completion-based
    // Receive/Send/Accept/Connect, submitted directly via io_uring) for TCP/stream sockets, on top
    // of "Option 3"'s single, shared io_uring ring (see System.Private.CoreLib's
    // PortableThreadPool.IoUring.Unix.cs) via the public System.Threading.IoUring API - this file
    // is a straight port of the equivalent per-thread-ring implementation, used here to measure the
    // maximum throughput achievable with a single shared ring instead. This is purely additive:
    // every method here either completes the operation by invoking the caller's callback
    // asynchronously (returning true), or returns false immediately without side effects, in which
    // case the caller falls back to the existing epoll-based SocketAsyncEngine path unchanged.
    // UDP/datagram sockets, multi-buffer scatter/gather, and cancellation are all out of scope -
    // callers only attempt these methods for the plain single-buffer / no-destination-address
    // cases; anything else always returns false.
    internal sealed partial class SocketAsyncContext
    {
        /// <summary>
        /// Attempts to complete a plain, single-buffer, no-destination-address Receive via io_uring
        /// instead of registering the socket for epoll-based readiness notification. Returns
        /// <see langword="true"/> if the operation was submitted - <paramref name="callback"/> will be
        /// invoked exactly once, later, with the final result (bytes received, or a mapped
        /// <see cref="SocketError"/> on failure). Returns <see langword="false"/> if the fast path does
        /// not apply or submission failed; the caller must fall back to its normal code path and no
        /// callback will ever be invoked for this attempt.
        /// </summary>
        private unsafe bool TryReceiveViaIoUring(Memory<byte> buffer, SocketFlags flags, Action<int, Memory<byte>, SocketFlags, SocketError> callback)
        {
            if (!System.Threading.IoUring.IsSupported || flags != SocketFlags.None || buffer.Length == 0)
            {
                return false;
            }

            MemoryHandle pin = buffer.Pin();
            bool submitted = System.Threading.IoUring.TrySubmitRecv(
                _socket,
                (byte*)pin.Pointer,
                buffer.Length,
                0,
                result => CompleteReceiveOrSend(pin, callback, result));

            if (!submitted)
            {
                pin.Dispose();
            }

            return submitted;
        }

        /// <summary>
        /// Attempts to complete a plain, single-buffer, no-destination-address Send via io_uring
        /// instead of registering the socket for epoll-based readiness notification. See
        /// <see cref="TryReceiveViaIoUring"/> for the submission/callback contract.
        /// </summary>
        private unsafe bool TrySendViaIoUring(Memory<byte> buffer, int offset, int count, SocketFlags flags, Action<int, Memory<byte>, SocketFlags, SocketError> callback)
        {
            if (!System.Threading.IoUring.IsSupported || flags != SocketFlags.None)
            {
                return false;
            }

            MemoryHandle pin = buffer.Pin();
            byte* bufferPtr = (byte*)pin.Pointer + offset;
            bool submitted = System.Threading.IoUring.TrySubmitSend(
                _socket,
                bufferPtr,
                count,
                0,
                result => CompleteReceiveOrSend(pin, callback, result));

            if (!submitted)
            {
                pin.Dispose();
            }

            return submitted;
        }

        private static void CompleteReceiveOrSend(MemoryHandle pin, Action<int, Memory<byte>, SocketFlags, SocketError> callback, int result)
        {
            pin.Dispose();

            int bytesTransferred = result >= 0 ? result : 0;
            SocketError errorCode = result >= 0
                ? SocketError.Success
                : SocketPal.GetSocketErrorForErrorCode(new Interop.ErrorInfo(-result).Error);

            callback(bytesTransferred, Memory<byte>.Empty, SocketFlags.None, errorCode);
        }

        /// <summary>
        /// Attempts to complete an Accept via io_uring instead of registering the listening socket for
        /// epoll-based readiness notification. <paramref name="socketAddress"/> must remain valid until
        /// <paramref name="callback"/> is invoked (it receives the peer's address, sliced to its actual
        /// length, on success). See <see cref="TryReceiveViaIoUring"/> for the general
        /// submission/callback contract.
        ///
        /// Internally this is backed by a single, persistent IORING_ACCEPT_MULTISHOT submission per
        /// listening socket (see <see cref="System.Threading.IoUring.TrySubmitAcceptMultishot"/>),
        /// started lazily the first time this method is called for a given <see cref="SocketAsyncContext"/>
        /// and kept alive for as long as connections keep being accepted, instead of one one-shot Accept
        /// submission per call. Because a multishot accept's completions never carry a peer address
        /// (the kernel would otherwise reuse/overwrite one shared address buffer across every
        /// connection), <see cref="OnMultishotConnectionAccepted"/> calls <c>getpeername(2)</c> itself
        /// to fill <paramref name="socketAddress"/> once a connection is actually handed to a caller of
        /// this method.
        ///
        /// Every accepted-but-not-yet-claimed connection is buffered in <see cref="_multishotAcceptResults"/>,
        /// and every call to this method that arrives before a connection is available is buffered in
        /// <see cref="_multishotAcceptWaiters"/> instead; <see cref="_multishotAcceptLock"/> guards both
        /// queues so a connection accepted concurrently with a new call here is matched up exactly once,
        /// on whichever side arrives second.
        /// </summary>
        private bool TryAcceptViaIoUring(Memory<byte> socketAddress, Action<IntPtr, Memory<byte>, SocketError> callback)
        {
            if (!System.Threading.IoUring.IsSupported)
            {
                return false;
            }

            if (Volatile.Read(ref _multishotAcceptStarted) == 0 &&
                Interlocked.CompareExchange(ref _multishotAcceptStarted, 1, 0) == 0)
            {
                if (!System.Threading.IoUring.TrySubmitAcceptMultishot(_socket, OnMultishotConnectionAccepted))
                {
                    // Could not start at all (e.g. the socket is concurrently being disposed) - leave
                    // _multishotAcceptStarted permanently at 1 (this instance never retries) and fall
                    // back to the caller's normal (epoll-based) path for good; a disposing socket will
                    // not usefully accept again regardless.
                    _multishotAcceptUnavailable = true;
                }
            }

            if (_multishotAcceptUnavailable)
            {
                return false;
            }

            lock (_multishotAcceptLock)
            {
                if (_multishotAcceptResults.Count > 0)
                {
                    int result = _multishotAcceptResults.Dequeue();

                    // The calling convention for AcceptAsync requires the callback to run later, never
                    // inline on the calling thread - even though we already have a buffered result here.
                    ThreadPool.UnsafeQueueUserWorkItem(
                        static state => CompleteMultishotAccept(state.Result, state.SocketAddress, state.Callback),
                        (Result: result, SocketAddress: socketAddress, Callback: callback),
                        preferLocal: false);
                }
                else
                {
                    _multishotAcceptWaiters.Enqueue((socketAddress, callback));
                }
            }

            return true;
        }

        // Guards _multishotAcceptResults/_multishotAcceptWaiters below.
        private readonly object _multishotAcceptLock = new object();

        // Connections the kernel has already reported via OnMultishotConnectionAccepted, but that no
        // pending TryAcceptViaIoUring call has claimed yet. Only ever populated for a
        // SocketAsyncContext backing a listening socket, but allocated eagerly here (rather than
        // lazily, on first use) to keep TryAcceptViaIoUring/OnMultishotConnectionAccepted simple -
        // guarded by _multishotAcceptLock.
        private readonly Queue<int> _multishotAcceptResults = new Queue<int>();

        // TryAcceptViaIoUring calls waiting for a connection to be accepted, in FIFO order. Guarded by
        // _multishotAcceptLock.
        private readonly Queue<(Memory<byte> SocketAddress, Action<IntPtr, Memory<byte>, SocketError> Callback)> _multishotAcceptWaiters = new Queue<(Memory<byte>, Action<IntPtr, Memory<byte>, SocketError>)>();

        // 0: the multishot accept submission has not been started yet for this socket; 1: it has been
        // started (successfully or not - see _multishotAcceptUnavailable).
        private int _multishotAcceptStarted;

        // Set (once, permanently) if starting the multishot accept submission failed; every subsequent
        // TryAcceptViaIoUring call then falls back to the caller's normal path immediately, without
        // ever enqueueing a waiter that would otherwise never be fulfilled.
        private bool _multishotAcceptUnavailable;

        /// <summary>
        /// Invoked (via <see cref="System.Threading.IoUring.TrySubmitAcceptMultishot"/>) once per
        /// connection accepted by this socket's persistent multishot accept submission, and once more,
        /// with a negative result, if that submission ever terminates with a genuine error. Matches this
        /// completion up against whichever <see cref="TryAcceptViaIoUring"/> call is waiting longest (if
        /// any), or buffers it in <see cref="_multishotAcceptResults"/> otherwise.
        /// </summary>
        private void OnMultishotConnectionAccepted(int result)
        {
            (Memory<byte> SocketAddress, Action<IntPtr, Memory<byte>, SocketError> Callback) waiter;
            bool haveWaiter;

            lock (_multishotAcceptLock)
            {
                haveWaiter = _multishotAcceptWaiters.Count > 0;
                if (haveWaiter)
                {
                    waiter = _multishotAcceptWaiters.Dequeue();
                }
                else
                {
                    waiter = default;
                    _multishotAcceptResults.Enqueue(result);
                }

                if (result < 0 && haveWaiter)
                {
                    // A genuine terminal failure of the whole multishot submission (not just a single
                    // connection): every other still-pending waiter would otherwise wait forever, since
                    // no further completions will ever arrive for this submission - fail them all with
                    // the same error instead of just the one dequeued above.
                    while (_multishotAcceptWaiters.Count > 0)
                    {
                        (Memory<byte> SocketAddress, Action<IntPtr, Memory<byte>, SocketError> Callback) other = _multishotAcceptWaiters.Dequeue();
                        SocketError otherError = SocketPal.GetSocketErrorForErrorCode(new Interop.ErrorInfo(-result).Error);
                        ThreadPool.UnsafeQueueUserWorkItem(
                            static state => state.Callback((IntPtr)(-1), state.SocketAddress, state.Error),
                            (SocketAddress: other.SocketAddress, other.Callback, Error: otherError),
                            preferLocal: false);
                    }
                }
            }

            if (haveWaiter)
            {
                // Already running on a Thread Pool work item (see IIoUringOperation.CompleteFromIoUring)
                // - safe to do the getpeername(2) call and invoke the callback directly from here.
                CompleteMultishotAccept(result, waiter.SocketAddress, waiter.Callback!);
            }
        }

        private static void CompleteMultishotAccept(int result, Memory<byte> socketAddress, Action<IntPtr, Memory<byte>, SocketError> callback)
        {
            if (result < 0)
            {
                SocketError errorCode = SocketPal.GetSocketErrorForErrorCode(new Interop.ErrorInfo(-result).Error);
                callback((IntPtr)(-1), socketAddress, errorCode);
                return;
            }

            // Unlike the one-shot TryAcceptViaIoUring path, the kernel never filled socketAddress here
            // (see this file's remarks on TryAcceptViaIoUring) - fetch it ourselves. ownsHandle: false
            // because this fd's real ownership is established afterward by whichever caller wraps it in
            // an actual Socket - this temporary wrapper must not close it.
            using SafeSocketHandle tempHandle = new SafeSocketHandle((IntPtr)result, ownsHandle: false);
            int addressLength = socketAddress.Length;
            SocketError getPeerNameError = SocketPal.GetPeerName(tempHandle, socketAddress.Span, ref addressLength);

            Memory<byte> actualAddress = getPeerNameError == SocketError.Success
                ? socketAddress.Slice(0, Math.Min(addressLength, socketAddress.Length))
                : socketAddress;

            callback((IntPtr)result, actualAddress, SocketError.Success);
        }


        /// <summary>
        /// Attempts to complete a Connect (with no data to send alongside it - TCP Fast Open-style
        /// connect-with-data always falls back to the existing path) via io_uring instead of the
        /// existing non-blocking-connect-then-epoll-wait sequence. See
        /// <see cref="TryReceiveViaIoUring"/> for the general submission/callback contract.
        /// </summary>
        private unsafe bool TryConnectViaIoUring(Memory<byte> socketAddress, Action<int, Memory<byte>, SocketFlags, SocketError> callback)
        {
            if (!System.Threading.IoUring.IsSupported)
            {
                return false;
            }

            MemoryHandle addressPin = socketAddress.Pin();
            int[] addressLengthBox = GC.AllocateArray<int>(1, pinned: true);
            addressLengthBox[0] = socketAddress.Length;

            bool submitted;
            fixed (int* addressLengthPtr = addressLengthBox)
            {
                submitted = System.Threading.IoUring.TrySubmitConnect(
                    _socket,
                    (byte*)addressPin.Pointer,
                    addressLengthPtr,
                    result =>
                    {
                        addressPin.Dispose();

                        SocketError errorCode = result == 0
                            ? SocketError.Success
                            : SocketPal.GetSocketErrorForErrorCode(new Interop.ErrorInfo(-result).Error);

                        _socket.RegisterConnectResult(errorCode);
                        _socket.SetBlocking();

                        callback(0, socketAddress, SocketFlags.None, errorCode);
                    });
            }

            if (!submitted)
            {
                addressPin.Dispose();
            }

            return submitted;
        }
    }
}
