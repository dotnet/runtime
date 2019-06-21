// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using System.Diagnostics;

namespace System.Net.Sockets
{
    partial class SafeSocketHandle
    {
        private int _receiveTimeout = -1;
        private int _sendTimeout = -1;
        private bool _nonBlocking;
        private bool _underlyingHandleNonBlocking;
        private SocketAsyncContext _asyncContext;

        private TrackedSocketOptions _trackedOptions;
        internal bool LastConnectFailed { get; set; }
        internal bool DualMode { get; set; }
        internal bool ExposedHandleOrUntrackedConfiguration { get; private set; }

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

                return _asyncContext;
            }
        }

        // This will set the underlying OS handle to be nonblocking, for whatever reason --
        // performing an async operation or using a timeout will cause this to happen.
        // Once the OS handle is nonblocking, it never transitions back to blocking.
        private void SetHandleNonBlocking()
        {
            // We don't care about synchronization because this is idempotent
            if (!_underlyingHandleNonBlocking)
            {
                AsyncContext.SetNonBlocking();
                _underlyingHandleNonBlocking = true;
            }
        }

        internal bool IsNonBlocking
        {
            get
            {
                return _nonBlocking;
            }
            set
            {
                _nonBlocking = value;

                //
                // If transitioning to non-blocking, we need to set the native socket to non-blocking mode.
                // If we ever transition back to blocking, we keep the native socket in non-blocking mode, and emulate
                // blocking.  This avoids problems with switching to native blocking while there are pending async
                // operations.
                //
                if (value)
                {
                    SetHandleNonBlocking();
                }
            }
        }

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

        internal bool IsDisconnected { get; private set; } = false;

        internal void SetToDisconnected()
        {
            IsDisconnected = true;
        }

        internal static unsafe SafeSocketHandle CreateSocket(IntPtr fileDescriptor)
        {
            return CreateSocket(InnerSafeCloseSocket.CreateSocket(fileDescriptor));
        }

        internal static unsafe SocketError CreateSocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType, out SafeSocketHandle socket)
        {
            SocketError errorCode;
            socket = CreateSocket(InnerSafeCloseSocket.CreateSocket(addressFamily, socketType, protocolType, out errorCode));
            return errorCode;
        }

        internal static unsafe SocketError Accept(SafeSocketHandle socketHandle, byte[] socketAddress, ref int socketAddressSize, out SafeSocketHandle socket)
        {
            SocketError errorCode;
            socket = CreateSocket(InnerSafeCloseSocket.Accept(socketHandle, socketAddress, ref socketAddressSize, out errorCode));
            return errorCode;
        }

        private void InnerReleaseHandle()
        {
            if (_asyncContext != null)
            {
                _asyncContext.Close();
            }
        }

        internal sealed partial class InnerSafeCloseSocket : SafeHandleMinusOneIsInvalid
        {
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

                if (NetEventSource.IsEnabled)
                {
                    NetEventSource.Info(this, remappedError ?
                        $"handle:{handle}, close():ECONNRESET, but treating it as SUCCESS" :
                        $"handle:{handle}, close():{errorCode}");
                }

#if DEBUG
                _closeSocketHandle = handle;
                _closeSocketResult = SocketPal.GetSocketErrorForErrorCode(errorCode);
#endif

                return errorCode;
            }

            private unsafe SocketError InnerReleaseHandle()
            {
                Interop.Error errorCode = Interop.Error.SUCCESS;

                // If _blockable was set in BlockingRelease, it's safe to block here, which means
                // we can honor the linger options set on the socket.  It also means closesocket() might return WSAEWOULDBLOCK, in which
                // case we need to do some recovery.
                if (_blockable)
                {
                    if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"handle:{handle} Following 'blockable' branch.");

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

                // By default or if CloseAsIs() path failed, set linger timeout to zero to get an abortive close (RST).
                var linger = new Interop.Sys.LingerOption
                {
                    OnOff = 1,
                    Seconds = 0
                };

                errorCode = Interop.Sys.SetLingerOption(handle, &linger);
#if DEBUG
                _closeSocketLinger = SocketPal.GetSocketErrorForErrorCode(errorCode);
#endif
                if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"handle:{handle}, setsockopt():{errorCode}");

                switch (errorCode)
                {
                    case Interop.Error.SUCCESS:
                    case Interop.Error.EINVAL:
                    case Interop.Error.ENOPROTOOPT:
                        errorCode = CloseHandle(handle);
                        break;

                    // For other errors, it's too dangerous to try closesocket() - it might block!
                }

                return SocketPal.GetSocketErrorForErrorCode(errorCode);
            }

            internal static InnerSafeCloseSocket CreateSocket(IntPtr fileDescriptor)
            {
                var res = new InnerSafeCloseSocket();
                res.SetHandle(fileDescriptor);
                return res;
            }

            internal static unsafe InnerSafeCloseSocket CreateSocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType, out SocketError errorCode)
            {
                IntPtr fd;
                Interop.Error error = Interop.Sys.Socket(addressFamily, socketType, protocolType, &fd);
                if (error == Interop.Error.SUCCESS)
                {
                    Debug.Assert(fd != (IntPtr)(-1), "fd should not be -1");

                    errorCode = SocketError.Success;

                    // The socket was created successfully; enable IPV6_V6ONLY by default for normal AF_INET6 sockets.
                    // This fails on raw sockets so we just let them be in default state.
                    if (addressFamily == AddressFamily.InterNetworkV6 && socketType != SocketType.Raw)
                    {
                        int on = 1;
                        error = Interop.Sys.SetSockOpt(fd, SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, (byte*)&on, sizeof(int));
                        if (error != Interop.Error.SUCCESS)
                        {
                            Interop.Sys.Close(fd);
                            fd = (IntPtr)(-1);
                            errorCode = SocketPal.GetSocketErrorForErrorCode(error);
                        }
                    }
                }
                else
                {
                    Debug.Assert(fd == (IntPtr)(-1), $"Unexpected fd: {fd}");

                    errorCode = SocketPal.GetSocketErrorForErrorCode(error);
                }

                var res = new InnerSafeCloseSocket();
                res.SetHandle(fd);
                return res;
            }

            internal static unsafe InnerSafeCloseSocket Accept(SafeSocketHandle socketHandle, byte[] socketAddress, ref int socketAddressLen, out SocketError errorCode)
            {
                IntPtr acceptedFd;
                if (!socketHandle.IsNonBlocking)
                {
                    errorCode = socketHandle.AsyncContext.Accept(socketAddress, ref socketAddressLen, out acceptedFd);
                }
                else
                {
                    bool completed = SocketPal.TryCompleteAccept(socketHandle, socketAddress, ref socketAddressLen, out acceptedFd, out errorCode);
                    if (!completed)
                    {
                        errorCode = SocketError.WouldBlock;
                    }
                }

                var res = new InnerSafeCloseSocket();
                res.SetHandle(acceptedFd);
                return res;
            }
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
