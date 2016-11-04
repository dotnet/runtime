// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Net.Sockets
{
    // This class implements a safe socket handle.
    // It uses an inner and outer SafeHandle to do so.  The inner
    // SafeHandle holds the actual socket, but only ever has one
    // reference to it.  The outer SafeHandle guards the inner
    // SafeHandle with real ref counting.  When the outer SafeHandle
    // is cleaned up, it releases the inner SafeHandle - since
    // its ref is the only ref to the inner SafeHandle, it deterministically
    // gets closed at that point - no races with concurrent IO calls.
    // This allows Close() on the outer SafeHandle to deterministically
    // close the inner SafeHandle, in turn allowing the inner SafeHandle
    // to block the user thread in case a graceful close has been
    // requested.  (It's not legal to block any other thread - such closes
    // are always abortive.)
    internal partial class SafeCloseSocket :
#if DEBUG
        DebugSafeHandleMinusOneIsInvalid
#else
        SafeHandleMinusOneIsInvalid
#endif
    {
        protected SafeCloseSocket() : base(true) { }

        private InnerSafeCloseSocket _innerSocket;
        private volatile bool _released;
#if DEBUG
        private InnerSafeCloseSocket _innerSocketCopy;
#endif

        public override bool IsInvalid
        {
            get
            {
                return IsClosed || base.IsInvalid;
            }
        }

#if DEBUG
        public void AddRef()
        {
            try
            {
                // The inner socket can be closed by CloseAsIs and when SafeHandle runs ReleaseHandle.
                InnerSafeCloseSocket innerSocket = Volatile.Read(ref _innerSocket);
                if (innerSocket != null)
                {
                    innerSocket.AddRef();
                }
            }
            catch (Exception e)
            {
                Debug.Assert(false, "SafeCloseSocket.AddRef after inner socket disposed." + e);
            }
        }

        public void Release()
        {
            try
            {
                // The inner socket can be closed by CloseAsIs and when SafeHandle runs ReleaseHandle.
                InnerSafeCloseSocket innerSocket = Volatile.Read(ref _innerSocket);
                if (innerSocket != null)
                {
                    innerSocket.Release();
                }
            }
            catch (Exception e)
            {
                Debug.Assert(false, "SafeCloseSocket.Release after inner socket disposed." + e);
            }
        }
#endif

        private void SetInnerSocket(InnerSafeCloseSocket socket)
        {
            _innerSocket = socket;
            SetHandle(socket.DangerousGetHandle());
#if DEBUG
            _innerSocketCopy = socket;
#endif
        }

        private static SafeCloseSocket CreateSocket(InnerSafeCloseSocket socket)
        {
            SafeCloseSocket ret = new SafeCloseSocket();
            CreateSocket(socket, ret);

            if (NetEventSource.IsEnabled) NetEventSource.Info(null, ret);

            return ret;
        }

        protected static void CreateSocket(InnerSafeCloseSocket socket, SafeCloseSocket target)
        {
            if (socket != null && socket.IsInvalid)
            {
                target.SetHandleAsInvalid();
                return;
            }

            bool b = false;
            try
            {
                socket.DangerousAddRef(ref b);
            }
            catch
            {
                if (b)
                {
                    socket.DangerousRelease();
                    b = false;
                }
            }
            finally
            {
                if (b)
                {
                    target.SetInnerSocket(socket);
                    socket.Dispose();
                }
                else
                {
                    target.SetHandleAsInvalid();
                }
            }
        }

        protected override bool ReleaseHandle()
        {
            if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"_innerSocket={_innerSocket}");

            _released = true;
            InnerSafeCloseSocket innerSocket = _innerSocket == null ? null : Interlocked.Exchange<InnerSafeCloseSocket>(ref _innerSocket, null);
            if (innerSocket != null)
            {
#if DEBUG
                // On AppDomain unload we may still have pending Overlapped operations.
                // ThreadPoolBoundHandle should handle this scenario by canceling them.
                innerSocket.LogRemainingOperations();
#endif

                innerSocket.DangerousRelease();
            }

            InnerReleaseHandle();

            return true;
        }

        internal void CloseAsIs()
        {
            if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"_innerSocket={_innerSocket}");

#if DEBUG
                // If this throws it could be very bad.
            try
            {
#endif
                InnerSafeCloseSocket innerSocket = _innerSocket == null ? null : Interlocked.Exchange<InnerSafeCloseSocket>(ref _innerSocket, null);

                Dispose();
                if (innerSocket != null)
                {
                    // Wait until it's safe.
                    SpinWait sw = new SpinWait();
                    while (!_released)
                    {
                        sw.SpinOnce();
                    }

                    // Now free it with blocking.
                    innerSocket.BlockingRelease();
                }

                InnerReleaseHandle();
#if DEBUG
            }
            catch (Exception exception) when (!ExceptionCheck.IsFatal(exception))
            {
                NetEventSource.Fail(this, $"handle:{handle}, error:{exception}");
                throw;
            }
#endif
        }

        internal sealed partial class InnerSafeCloseSocket : SafeHandleMinusOneIsInvalid
        {
            private InnerSafeCloseSocket() : base(true) { }

            private bool _blockable;

            public override bool IsInvalid
            {
                get
                {
                    return IsClosed || base.IsInvalid;
                }
            }

            // This method is implicitly reliable and called from a CER.
            protected override bool ReleaseHandle()
            {
                bool ret = false;

#if DEBUG
                try
                {
#endif
                    if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"handle:{handle}");

                    SocketError errorCode = InnerReleaseHandle();
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

#if DEBUG
            private IntPtr _closeSocketHandle;
            private SocketError _closeSocketResult = unchecked((SocketError)0xdeadbeef);
            private SocketError _closeSocketLinger = unchecked((SocketError)0xdeadbeef);
            private int _closeSocketThread;
            private int _closeSocketTick;

            private int _refCount = 0;

            public void AddRef()
            {
                Interlocked.Increment(ref _refCount);
            }

            public void Release()
            {
                Interlocked.MemoryBarrier();
                Debug.Assert(_refCount > 0, "InnerSafeCloseSocket: Release() called more times than AddRef");
                Interlocked.Decrement(ref _refCount);
            }

            public void LogRemainingOperations()
            {
                Interlocked.MemoryBarrier();
                if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"Releasing with pending operations: {_refCount}");
            }
#endif

            // Use this method to close the socket handle using the linger options specified on the socket.
            // Guaranteed to only be called once, under a CER, and not if regular DangerousRelease is called.
            internal void BlockingRelease()
            {
#if DEBUG
                // Expected to have outstanding operations such as Accept.
                LogRemainingOperations();
#endif

                _blockable = true;
                DangerousRelease();
            }
        }
    }
}
