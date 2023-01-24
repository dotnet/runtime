// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using System.Diagnostics;

namespace System.Net.Sockets
{
    public partial class SafeSocketHandle
    {
        private int _receiveTimeout = -1;
        private int _sendTimeout = -1;
        private bool _nonBlocking;
        private SocketAsyncContext? _asyncContext;

        private TrackedSocketOptions _trackedOptions;
        internal bool LastConnectFailed { get; set; }
        internal bool DualMode { get; set; }
        internal bool ExposedHandleOrUntrackedConfiguration { get; private set; }
        internal bool PreferInlineCompletions { get; set; } = SocketAsyncEngine.InlineSocketCompletionsEnabled;
        internal bool IsSocket { get; set; } = true; // (ab)use Socket class for performing async I/O on non-socket fds.

        internal void RegisterConnectResult(SocketError error)
        {
            switch (error)
            {
                case SocketError.Success:
                case SocketError.WouldBlock:
                    break;
                default:
                    LastConnectFailed = true;
                    break;
            }
        }

        internal void TransferTrackedState(SafeSocketHandle target)
        {
            target._trackedOptions = _trackedOptions;
            target.LastConnectFailed = LastConnectFailed;
            target.DualMode = DualMode;
            target.ExposedHandleOrUntrackedConfiguration = ExposedHandleOrUntrackedConfiguration;
            target.IsSocket = IsSocket;
        }

        internal void SetExposed() => ExposedHandleOrUntrackedConfiguration = true;

        internal bool IsTrackedOption(TrackedSocketOptions option) => (_trackedOptions & option) != 0;

        internal void TrackOption(SocketOptionLevel level, SocketOptionName name)
        {
            // As long as only these options are set, we can support Connect{Async}(IPAddress[], ...).
            switch (level)
            {
                case SocketOptionLevel.Tcp:
                    switch (name)
                    {
                        case SocketOptionName.NoDelay: _trackedOptions |= TrackedSocketOptions.NoDelay; return;
                    }
                    break;

                case SocketOptionLevel.IP:
                    switch (name)
                    {
                        case SocketOptionName.DontFragment: _trackedOptions |= TrackedSocketOptions.DontFragment; return;
                        case SocketOptionName.IpTimeToLive: _trackedOptions |= TrackedSocketOptions.Ttl; return;
                    }
                    break;

                case SocketOptionLevel.IPv6:
                    switch (name)
                    {
                        case SocketOptionName.IPv6Only: _trackedOptions |= TrackedSocketOptions.DualMode; return;
                        case SocketOptionName.IpTimeToLive: _trackedOptions |= TrackedSocketOptions.Ttl; return;
                    }
                    break;

                case SocketOptionLevel.Socket:
                    switch (name)
                    {
                        case SocketOptionName.Broadcast: _trackedOptions |= TrackedSocketOptions.EnableBroadcast; return;
                        case SocketOptionName.Linger: _trackedOptions |= TrackedSocketOptions.LingerState; return;
                        case SocketOptionName.ReceiveBuffer: _trackedOptions |= TrackedSocketOptions.ReceiveBufferSize; return;
                        case SocketOptionName.ReceiveTimeout: _trackedOptions |= TrackedSocketOptions.ReceiveTimeout; return;
                        case SocketOptionName.SendBuffer: _trackedOptions |= TrackedSocketOptions.SendBufferSize; return;
                        case SocketOptionName.SendTimeout: _trackedOptions |= TrackedSocketOptions.SendTimeout; return;
                    }
                    break;
            }

            // For any other settings, we need to track that they were used so that we can error out
            // if a Connect{Async}(IPAddress[],...) attempt is made.
            ExposedHandleOrUntrackedConfiguration = true;
        }

        internal SocketAsyncContext AsyncContext
        {
            get
            {
                if (Volatile.Read(ref _asyncContext) == null)
                {
                    Interlocked.CompareExchange(ref _asyncContext, new SocketAsyncContext(this), null);
                }

                return _asyncContext!;
            }
        }

        /// <summary>
        /// This represents whether the Socket instance is blocking or non-blocking *from the user's point of view*,
        /// i.e. it corresponds to the Socket.Blocking property (except in reverse).
        /// Even if this is false, the underlying native socket may still be non-blocking if anything ever caused it to become non-blocking,
        /// either by issuing an async operation or explicitly setting this property to true.
        /// </summary>
        internal bool IsNonBlocking
        {
            get
            {
                return _nonBlocking;
            }
            set
            {
                _nonBlocking = value;

                // If transitioning from blocking to non-blocking, we need to set the native socket to non-blocking mode.
                // If transitioning from non-blocking to blocking, we keep the native socket in non-blocking mode, and emulate
                // blocking operations within SocketAsyncContext on top of epoll/kqueue.
                // This avoids problems with switching to native blocking while there are pending operations.
                if (value)
                {
                    AsyncContext.SetHandleNonBlocking();
                }
            }
        }

        internal bool IsUnderlyingHandleBlocking => !AsyncContext.IsHandleNonBlocking;

        internal int ReceiveTimeout
        {
            get
            {
                return _receiveTimeout;
            }
            set
            {
                Debug.Assert(value == -1 || value > 0, $"Unexpected value: {value}");
                _receiveTimeout = value;
            }
        }

        internal int SendTimeout
        {
            get
            {
                return _sendTimeout;
            }
            set
            {
                Debug.Assert(value == -1 || value > 0, $"Unexpected value: {value}");
                _sendTimeout = value;
            }
        }

        internal bool IsDisconnected { get; private set; }

        internal void SetToDisconnected()
        {
            IsDisconnected = true;
        }

        /// <returns>Returns whether operations were canceled.</returns>
        private bool OnHandleClose()
        {
            // If we've aborted async operations, return true to cause an abortive close.
            return _asyncContext?.StopAndAbort() ?? false;
        }

        /// <returns>Returns whether operations were canceled.</returns>
        private unsafe bool TryUnblockSocket(bool abortive)
        {
            // Calling 'close' on a socket that has pending blocking calls (e.g. recv, send, accept, ...)
            // may block indefinitely. This is a best-effort attempt to not get blocked and make those operations return.
            // We need to ensure we keep the expected TCP behavior that is observed by the socket peer (FIN vs RST close).
            // What we do here isn't specified by POSIX and doesn't work on all OSes.
            // On Linux this works well.
            // On OSX, TCP connections will be closed with a FIN close instead of an abortive RST close.
            // And, pending TCP connect operations and UDP receive are not abortable.

            // Don't disconnect sockets we don't own.
            if (!OwnsHandle)
            {
                return false;
            }

            // Unless we're doing an abortive close, don't touch sockets which don't have the CLOEXEC flag set.
            // These may be shared with other processes and we want to avoid disconnecting them.
            if (!abortive)
            {
                int fdFlags = Interop.Sys.Fcntl.GetFD(handle);
                if (fdFlags == 0)
                {
                    return false;
                }
            }

            int type = 0;
            int optLen = sizeof(int);
            Interop.Error err = Interop.Sys.GetSockOpt(handle, SocketOptionLevel.Socket, SocketOptionName.Type, (byte*)&type, &optLen);
            if (err == Interop.Error.SUCCESS)
            {
                // For TCP (SocketType.Stream), perform an abortive close.
                // Unless the user requested a normal close using Socket.Shutdown.
                if (type == (int)SocketType.Stream && !_hasShutdownSend)
                {
                    Interop.Sys.Disconnect(handle);
                }
                else
                {
                    Interop.Sys.Shutdown(handle, SocketShutdown.Both);
                }
            }

            return true;
        }

        private unsafe SocketError DoCloseHandle(bool abortive)
        {
            Interop.Error errorCode = Interop.Error.SUCCESS;

            if (!IsSocket)
            {
                return SocketPal.GetSocketErrorForErrorCode(CloseHandle(handle));
            }

            // If abortive is not set, we're not running on the finalizer thread, so it's safe to block here.
            // We can honor the linger options set on the socket.  It also means closesocket() might return
            // EWOULDBLOCK, in which case we need to do some recovery.
            if (!abortive)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"handle:{handle} Following 'non-abortive' branch.");

                // Close, and if its errno is other than EWOULDBLOCK, there's nothing more to do - we either succeeded or failed.
                errorCode = CloseHandle(handle);
                if (errorCode != Interop.Error.EWOULDBLOCK)
                {
                    return SocketPal.GetSocketErrorForErrorCode(errorCode);
                }

                // The socket must be non-blocking with a linger timeout set.
                // We have to set the socket to blocking.
                if (Interop.Sys.Fcntl.DangerousSetIsNonBlocking(handle, 0) == 0)
                {
                    // The socket successfully made blocking; retry the close().
                    return SocketPal.GetSocketErrorForErrorCode(CloseHandle(handle));
                }

                // The socket could not be made blocking; fall through to the regular abortive close.
            }

            // By default or if the non-abortive path failed, set linger timeout to zero to get an abortive close (RST).
            var linger = new Interop.Sys.LingerOption
            {
                OnOff = 1,
                Seconds = 0
            };

            errorCode = Interop.Sys.SetLingerOption(handle, &linger);
#if DEBUG
            _closeSocketLinger = SocketPal.GetSocketErrorForErrorCode(errorCode);
#endif
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"handle:{handle}, setsockopt():{errorCode}");

            switch (errorCode)
            {
                case Interop.Error.SUCCESS:
                case Interop.Error.EINVAL:
                case Interop.Error.ENOPROTOOPT:
                case Interop.Error.ENOTSOCK:
                    errorCode = CloseHandle(handle);
                    break;

                // For other errors, it's too dangerous to try closesocket() - it might block!
            }

            return SocketPal.GetSocketErrorForErrorCode(errorCode);
        }

        private Interop.Error CloseHandle(IntPtr handle)
        {
            Interop.Error errorCode = Interop.Error.SUCCESS;
            bool remappedError = false;

            if (Interop.Sys.Close(handle) != 0)
            {
                errorCode = Interop.Sys.GetLastError();
                if (errorCode == Interop.Error.ECONNRESET)
                {
                    // Some Unix platforms (e.g. FreeBSD) non-compliantly return ECONNRESET from close().
                    // For our purposes, we want to ignore such a "failure" and treat it as success.
                    // In such a case, the file descriptor was still closed and there's no corrective
                    // action to take.
                    errorCode = Interop.Error.SUCCESS;
                    remappedError = true;
                }
            }

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(this, remappedError ?
                    $"handle:{handle}, close():ECONNRESET, but treating it as SUCCESS" :
                    $"handle:{handle}, close():{errorCode}");
            }

#if DEBUG
            _closeSocketResult = SocketPal.GetSocketErrorForErrorCode(errorCode);
#endif

            return errorCode;
        }
    }

    /// <summary>Flags that correspond to exposed options on Socket.</summary>
    [Flags]
    internal enum TrackedSocketOptions : short
    {
        DontFragment = 0x1,
        DualMode = 0x2,
        EnableBroadcast = 0x4,
        LingerState = 0x8,
        NoDelay = 0x10,
        ReceiveBufferSize = 0x20,
        ReceiveTimeout = 0x40,
        SendBufferSize = 0x80,
        SendTimeout = 0x100,
        Ttl = 0x200,
    }
}
