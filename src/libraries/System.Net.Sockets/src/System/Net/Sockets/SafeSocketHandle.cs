// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

using System.Diagnostics;
using System.Threading;

namespace System.Net.Sockets
{
    public sealed partial class SafeSocketHandle : SafeHandleMinusOneIsInvalid
    {
#if DEBUG
        private SocketError _closeSocketResult = unchecked((SocketError)0xdeadbeef);
        private SocketError _closeSocketLinger = unchecked((SocketError)0xdeadbeef);
        private int _closeSocketThread;
        private int _closeSocketTick;
#endif
        private int _ownClose;

        public SafeSocketHandle(IntPtr preexistingHandle, bool ownsHandle)
            : base(ownsHandle)
        {
            OwnsHandle = ownsHandle;
            SetHandleAndValid(preexistingHandle);
        }

        private SafeSocketHandle() : base(ownsHandle: true) => OwnsHandle = true;

        internal bool OwnsHandle { get; }

        private bool TryOwnClose()
        {
            return OwnsHandle && Interlocked.CompareExchange(ref _ownClose, 1, 0) == 0;
        }

        private volatile bool _released;
        private bool _hasShutdownSend;

        internal void TrackShutdown(SocketShutdown how)
        {
            if (how == SocketShutdown.Send ||
                how == SocketShutdown.Both)
            {
                _hasShutdownSend = true;
            }
        }

        public override bool IsInvalid
        {
            get
            {
                return IsClosed || base.IsInvalid;
            }
        }

        protected override bool ReleaseHandle()
        {
            _released = true;
            bool shouldClose = TryOwnClose();

            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"shouldClose={shouldClose}");

            // When shouldClose is true, the user called Dispose on the SafeHandle.
            // When it is false, the handle was closed from the Socket via CloseAsIs.
            if (shouldClose)
            {
                CloseHandle(abortive: true, canceledOperations: false);
            }

            return true;
        }

        internal void CloseAsIs(bool abortive, bool finalizing)
        {
#if DEBUG
            // If this throws it could be very bad.
            try
            {
#endif
                // When the handle was not released due it being used, we try to make those on-going calls return.
                // TryUnblockSocket will unblock current operations but it doesn't prevent
                // a new one from starting. So we must call TryUnblockSocket multiple times.
                //
                // When the Socket is disposed from the finalizer thread
                // it is no longer used for operations and we can skip TryUnblockSocket fall back to ReleaseHandle.
                // This avoids blocking the finalizer thread when TryUnblockSocket is unable to get the reference count to zero.
                if (finalizing)
                {
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"finalizing");

                    Dispose();
                    return;
                }

                bool shouldClose = TryOwnClose();

                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"shouldClose={shouldClose}");

                Dispose();

                if (shouldClose)
                {
                    bool canceledOperations = false;

                    // Wait until it's safe.
                    SpinWait sw = default;
                    while (!_released)
                    {
                        canceledOperations |= TryUnblockSocket(abortive);
                        sw.SpinOnce();
                    }

                    CloseHandle(abortive, canceledOperations);
                }
#if DEBUG
            }
            catch (Exception exception) when (!ExceptionCheck.IsFatal(exception))
            {
                NetEventSource.Fail(this, $"handle:{handle}, error:{exception}");
                throw;
            }
#endif
        }

        private bool CloseHandle(bool abortive, bool canceledOperations)
        {
            bool ret = false;

#if DEBUG
            try
            {
#endif
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"handle:{handle}");

                canceledOperations |= OnHandleClose();

                // In case we cancel operations, switch to an abortive close.
                // Unless the user requested a normal close using Socket.Shutdown.
                if (canceledOperations && !_hasShutdownSend)
                {
                    abortive = true;
                }

                SocketError errorCode = DoCloseHandle(abortive);
                return ret = errorCode == SocketError.Success;
#if DEBUG
            }
            catch (Exception exception)
            {
                if (!ExceptionCheck.IsFatal(exception))
                {
                    NetEventSource.Fail(this, $"handle:{handle}, error:{exception}");
                }

                ret = true;  // Avoid a second assert.
                throw;
            }
            finally
            {
                _closeSocketThread = Environment.CurrentManagedThreadId;
                _closeSocketTick = Environment.TickCount;
                if (!ret)
                {
                    NetEventSource.Fail(this, $"ReleaseHandle failed. handle:{handle}");
                }
            }
#endif
        }

        private void SetHandleAndValid(IntPtr handle)
        {
            Debug.Assert(!IsClosed);

            base.SetHandle(handle);

            if (IsInvalid)
            {
                // CloseAsIs musn't wait for a release.
                TryOwnClose();

                // Mark handle as invalid, so it won't be released.
                SetHandleAsInvalid();
            }
        }
    }
}
