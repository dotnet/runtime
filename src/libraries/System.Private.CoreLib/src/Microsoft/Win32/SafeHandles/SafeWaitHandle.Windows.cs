// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafeWaitHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private WaitCompletionPacket? _reusableWaitCompletionPacket;

        protected override bool ReleaseHandle()
        {
            _reusableWaitCompletionPacket?.Dispose();
            return Interop.Kernel32.CloseHandle(handle);
        }

        internal WaitCompletionPacket GetWaitCompletionPacket() => Interlocked.Exchange(ref _reusableWaitCompletionPacket, null) ?? new(this);

        internal void TryToReuse(WaitCompletionPacket packet)
        {
            if (Interlocked.CompareExchange(ref _reusableWaitCompletionPacket, packet, null) is not null)
            {
                packet.Dispose();
            }
            else if (IsClosed)
            {
                Interlocked.Exchange(ref _reusableWaitCompletionPacket, null)?.Dispose();
            }
        }

        internal sealed unsafe class WaitCompletionPacket
        {
            private static readonly IOCompletionCallback s_completionCallback = OnCompletion;

            private static readonly TimerCallback s_timerCallback = OnTimeout;

            private readonly nint _completionPacketHandle;

            private readonly LowLevelLock _lock = new LowLevelLock();

            private readonly PreAllocatedOverlapped _preAllocatedOverlapped;

            private NativeOverlapped* _overlapped;

            private TimerQueueTimer? _timer;

            private bool _needsTimerReset;

            private State _state;

            private RegisteredWaitHandle? _registeredWaitHandle;

            private uint _timeout;

            private bool _repeating;

            internal SafeWaitHandle Handle { get; }

            internal WaitCompletionPacket(SafeWaitHandle handle)
            {
                Handle = handle;

                _preAllocatedOverlapped = PreAllocatedOverlapped.UnsafeCreatePortableUnbound(s_completionCallback, this, null);

                uint status = Interop.NtDll.NtCreateWaitCompletionPacket(
                    out _completionPacketHandle,
                    Interop.NtDll.DesiredAccess.FILE_GENERIC_ALL,
                    null);
                if (!Interop.StatusOptions.NT_SUCCESS(status))
                {
                    _preAllocatedOverlapped.Dispose();
                    Interop.NtDll.ThrowExceptionForNtStatus(status);
                }
            }

            internal void StartWait(RegisteredWaitHandle registeredWaitHandle)
            {
                _registeredWaitHandle = registeredWaitHandle;
                _timeout = (uint)registeredWaitHandle.TimeoutDurationMs;
                _repeating = registeredWaitHandle.Repeating;
                RestartWait();
            }

            private void RestartTimer()
            {
                _needsTimerReset = _timeout != Timeout.UnsignedInfinite;
                if (_timer != null)
                {
                    _timer.Change(_timeout, Timeout.UnsignedInfinite);
                }
                else if (_timeout != Timeout.UnsignedInfinite)
                {
                    _timer = new TimerQueueTimer(s_timerCallback, this, _timeout, Timeout.UnsignedInfinite, flowExecutionContext: false);
                }
            }

            private void RestartWait()
            {
                RestartTimer();

                // Free and immediately allocate again the overlapped, to reset its _completed field.
                if (_overlapped != null)
                {
                    _preAllocatedOverlapped.FreeNativeOverlappedPortableUnbound(_overlapped);
                }
                _overlapped = _preAllocatedOverlapped.AllocateNativeOverlappedPortableUnbound();
                _state = State.Pending;

                uint status = Interop.NtDll.NtAssociateWaitCompletionPacket(
                    _completionPacketHandle,
                    PortableThreadPool.ThreadPoolInstance.SelectIOCompletionPortForRegister(),
                    Handle,
                    null,
                    _overlapped,
                    0,
                    0,
                    null);
                if (!Interop.StatusOptions.NT_SUCCESS(status))
                {
                    Dispose();
                    Interop.NtDll.ThrowExceptionForNtStatus(status);
                }
            }

            private static void OnCompletion(uint errorCode, uint numberOfBytesTransferred, NativeOverlapped* nativeOverlapped)
            {
                WaitCompletionPacket packet = (WaitCompletionPacket)PreAllocatedOverlapped.GetNativeOverlappedStatePortableUnbound(nativeOverlapped)!;
                packet.ChangeState(StateTransition.Completion);
            }

            private static void OnTimeout(object? state)
            {
                WaitCompletionPacket packet = (WaitCompletionPacket)state!;
                Volatile.Write(ref packet._needsTimerReset, false);
                packet.ChangeState(StateTransition.Timeout);
            }

            internal void UnregisterWait()
            {
                ChangeState(StateTransition.Unregister);
            }

            internal bool Cancel()
            {
                uint status = Interop.NtDll.NtCancelWaitCompletionPacket(
                    _completionPacketHandle,
                    Interop.BOOLEAN.TRUE);
                if (Interop.StatusOptions.NT_SUCCESS(status))
                {
                    // The completion packet was cancelled, and removed from the I/O completion port.
                    return true;
                }
                if (status is not (Interop.StatusOptions.STATUS_CANCELLED or Interop.StatusOptions.STATUS_PENDING))
                {
                    Interop.NtDll.ThrowExceptionForNtStatus(status);
                }
                return false;
            }

            private static void DisposeTimerBlocking(TimerQueueTimer timer)
            {
                using AutoResetEvent ev = new AutoResetEvent(false);
                timer.Dispose(ev);
                ev.WaitOne();
            }

            private void ChangeState(StateTransition transition)
            {
                bool removed = false;
                RegisteredWaitHandle registeredWaitHandle;
                try
                {
                    _lock.Acquire();
                    registeredWaitHandle = _registeredWaitHandle!;
                    switch (_state, transition)
                    {
                        case (State.Unregistered, StateTransition.Timeout):
                            // This is the only event we are waiting for in Unregistered state, which may when the
                            // unregistering thread has left the lock and is waiting for the timer callbacks to finish.
                            return;
                        case (State.Pending, StateTransition.Timeout):
                            registeredWaitHandle.RequestCallback();
                            if (_repeating)
                            {
                                RestartTimer();
                            }
                            else
                            {
                                // Optimization: Try removing the wait from the I/O completion port, and if it succeeds,
                                // we can already handle completion as well.
                                _state = Cancel() ? State.Completed : State.TimedOut;
                            }
                            break;
                        case (State.Pending, StateTransition.Completion):
                            registeredWaitHandle.RequestCallback();
                            if (_repeating)
                            {
                                RestartWait();
                            }
                            else
                            {
                                _timer?.Change(Timeout.UnsignedInfinite, Timeout.UnsignedInfinite);
                                _state = State.Completed;
                            }
                            break;
                        case (State.Pending, StateTransition.Unregister):
                            removed = Cancel();
                            if (removed)
                            {
                                _state = State.Unregistered;
                                registeredWaitHandle.OnRemoveWait();
                            }
                            else
                            {
                                // If we failed to cancel, it means that the wait callback is already queued to execute.
                                // We need to wait for the completion callback to finish before we can unregister.
                                _state = State.UnregisteringWaitingCompletion;
                            }
                            break;
                        case (State.TimedOut, StateTransition.Completion):
                            _state = State.Completed;
                            return;
                        case (State.TimedOut, StateTransition.Unregister):
                            // The real completion for the timed-out wait hasn't arrived yet; wait for it
                            // before finishing unregistration.
                            _state = State.UnregisteringWaitingCompletion;
                            break;
                        case (State.Completed, StateTransition.Unregister):
                            _state = State.Unregistered;
                            registeredWaitHandle.OnRemoveWait();
                            break;
                        case (State.UnregisteringWaitingCompletion, StateTransition.Timeout):
                            // A stray timer fire while waiting for the real completion callback to finish
                            // unregistering; ignore it and let the Completion transition drive things.
                            return;
                        case (State.UnregisteringWaitingCompletion, StateTransition.Completion):
                            _state = State.Unregistered;
                            // Unblock the unregistering thread and return.
                            registeredWaitHandle.OnRemoveWait();
                            return;
                        default:
                            Debug.Fail($"Unexpected state transition from {_state} on {transition}");
                            return;
                    }
                }
                finally
                {
                    _lock.Release();
                }

                if (transition is StateTransition.Unregister)
                {
                    if (Volatile.Read(ref _needsTimerReset) && _timer is not null)
                    {
                        DisposeTimerBlocking(_timer);
                        _timer = null;
                    }
                    if (registeredWaitHandle.IsBlocking)
                    {
                        registeredWaitHandle.WaitForCallbacks();
                    }
                    else if (!removed)
                    {
                        registeredWaitHandle.WaitForRemoval();
                    }
                    ReleaseResources();
                    Handle.TryToReuse(this);
                }
                else
                {
                    PortableThreadPool.CompleteWait(registeredWaitHandle, timedOut: transition is StateTransition.Timeout);
                }
            }

            internal void Dispose()
            {
                ReleaseResources();
                _lock.Dispose();
                _preAllocatedOverlapped.Dispose();
                _timer?.Dispose();
                if (_completionPacketHandle != 0)
                {
                    Interop.Kernel32.CloseHandle(_completionPacketHandle);
                }
            }

            private void ReleaseResources()
            {
                _registeredWaitHandle = null;
                _state = State.Inactive;
                _needsTimerReset = false;

                // Free the overlapped.
                if (_overlapped != null)
                {
                    _preAllocatedOverlapped.FreeNativeOverlappedPortableUnbound(_overlapped);
                    _overlapped = null;
                }
            }

            private enum State
            {
                /// <summary>
                /// The wait packet is not currently associated with a wait handle.
                /// </summary>
                Inactive,
                /// <summary>
                /// Waiting for the wait to complete or timeout.
                /// </summary>
                Pending,
                /// <summary>
                /// Timed out, but the wait callback has not yet been executed.
                /// </summary>
                TimedOut,
                /// <summary>
                /// Completed, and the wait callback has been executed.
                /// </summary>
                Completed,
                /// <summary>
                /// <see cref="UnregisterWait"/> was called but the wait has not yet been removed from the I/O completion port.
                /// Waiting for a <see cref="StateTransition.Completion"/> to finish the unregistering process.
                /// </summary>
                UnregisteringWaitingCompletion,
                /// <summary>
                /// <see cref="UnregisterWait"/> was called and the wait has either been called or removed from the I/O completion
                /// port. The clean-up process can now be executed.
                /// </summary>
                Unregistered,
            }

            private enum StateTransition
            {
                Timeout,
                Completion,
                Unregister,
            }
        }
    }
}
