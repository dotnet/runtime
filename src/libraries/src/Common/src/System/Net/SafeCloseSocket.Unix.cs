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
    internal partial class SafeCloseSocket :
#if DEBUG
        DebugSafeHandleMinusOneIsInvalid
#else
        SafeHandleMinusOneIsInvalid
#endif
    {
        private int _receiveTimeout = -1;
        private int _sendTimeout = -1;
        private bool _nonBlocking;
        private SocketAsyncContext _asyncContext;

        public SocketAsyncContext AsyncContext
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


        public bool IsNonBlocking
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
                    AsyncContext.SetNonBlocking();
                }
            }
        }

        public int ReceiveTimeout
        {
            get
            {
                return _receiveTimeout;
            }
            set
            {
                Debug.Assert(value == -1 || value > 0, $"Unexpected value: {value}");
                _receiveTimeout = value;;
            }
        }

        public int SendTimeout
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

        public static unsafe SafeCloseSocket CreateSocket(IntPtr fileDescriptor)
        {
            return CreateSocket(InnerSafeCloseSocket.CreateSocket(fileDescriptor));
        }

        public static unsafe SocketError CreateSocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType, out SafeCloseSocket socket)
        {
            SocketError errorCode;
            socket = CreateSocket(InnerSafeCloseSocket.CreateSocket(addressFamily, socketType, protocolType, out errorCode));
            return errorCode;
        }

        public static unsafe SocketError Accept(SafeCloseSocket socketHandle, byte[] socketAddress, ref int socketAddressSize, out SafeCloseSocket socket)
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
            private unsafe SocketError InnerReleaseHandle()
            {
                int errorCode;

                // If _blockable was set in BlockingRelease, it's safe to block here, which means
                // we can honor the linger options set on the socket.  It also means closesocket() might return WSAEWOULDBLOCK, in which
                // case we need to do some recovery.
                if (_blockable)
                {
                    if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"handle:{handle} Following 'blockable' branch.");

                    errorCode = Interop.Sys.Close(handle);
                    if (errorCode == -1)
                    {
                        errorCode = (int)Interop.Sys.GetLastError();
                    }

                    if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"handle:{handle}, close()#1:{errorCode}");
#if DEBUG
                    _closeSocketHandle = handle;
                    _closeSocketResult = SocketPal.GetSocketErrorForErrorCode((Interop.Error)errorCode);
#endif

                    // If it's not EWOULDBLOCK, there's no more recourse - we either succeeded or failed.
                    if (errorCode != (int)Interop.Error.EWOULDBLOCK)
                    {
                        return SocketPal.GetSocketErrorForErrorCode((Interop.Error)errorCode);
                    }

                    // The socket must be non-blocking with a linger timeout set.
                    // We have to set the socket to blocking.
                    errorCode = Interop.Sys.Fcntl.DangerousSetIsNonBlocking(handle, 0);
                    if (errorCode == 0)
                    {
                        // The socket successfully made blocking; retry the close().
                        errorCode = Interop.Sys.Close(handle);

                        if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"handle:{handle}, close()#2:{errorCode}");
#if DEBUG
                        _closeSocketHandle = handle;
                        _closeSocketResult = SocketPal.GetSocketErrorForErrorCode((Interop.Error)errorCode);
#endif
                        return SocketPal.GetSocketErrorForErrorCode((Interop.Error)errorCode);
                    }

                    // The socket could not be made blocking; fall through to the regular abortive close.
                }

                // By default or if CloseAsIs() path failed, set linger timeout to zero to get an abortive close (RST).
                var linger = new Interop.Sys.LingerOption {
                    OnOff = 1,
                    Seconds = 0
                };

                errorCode = (int)Interop.Sys.SetLingerOption(handle, &linger);
#if DEBUG
                _closeSocketLinger = SocketPal.GetSocketErrorForErrorCode((Interop.Error)errorCode);
#endif
                if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"handle:{handle}, setsockopt():{errorCode}");

                if (errorCode != 0 && errorCode != (int)Interop.Error.EINVAL && errorCode != (int)Interop.Error.ENOPROTOOPT)
                {
                    // Too dangerous to try closesocket() - it might block!
                    return SocketPal.GetSocketErrorForErrorCode((Interop.Error)errorCode);
                }

                errorCode = Interop.Sys.Close(handle);
#if DEBUG
                _closeSocketHandle = handle;
                _closeSocketResult = SocketPal.GetSocketErrorForErrorCode((Interop.Error)errorCode);
#endif
                if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"handle:{handle}, close#3():{(errorCode == -1 ? (int)Interop.Sys.GetLastError() : errorCode)}");

                return SocketPal.GetSocketErrorForErrorCode((Interop.Error)errorCode);
            }

            public static InnerSafeCloseSocket CreateSocket(IntPtr fileDescriptor)
            {
                var res = new InnerSafeCloseSocket();
                res.SetHandle(fileDescriptor);
                return res;
            }

            public static unsafe InnerSafeCloseSocket CreateSocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType, out SocketError errorCode)
            {
                IntPtr fd;
                Interop.Error error = Interop.Sys.Socket(addressFamily, socketType, protocolType, &fd);
                if (error == Interop.Error.SUCCESS)
                {
                    Debug.Assert(fd != (IntPtr)(-1), "fd should not be -1");

                    errorCode = SocketError.Success;

                    // The socket was created successfully; enable IPV6_V6ONLY by default for AF_INET6 sockets.
                    if (addressFamily == AddressFamily.InterNetworkV6)
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

            public static unsafe InnerSafeCloseSocket Accept(SafeCloseSocket socketHandle, byte[] socketAddress, ref int socketAddressLen, out SocketError errorCode)
            {
                IntPtr acceptedFd;
                if (!socketHandle.IsNonBlocking)
                {
                    errorCode = socketHandle.AsyncContext.Accept(socketAddress, ref socketAddressLen, -1, out acceptedFd);
                }
                else
                {
                    SocketPal.TryCompleteAccept(socketHandle, socketAddress, ref socketAddressLen, out acceptedFd, out errorCode);
                }

                var res = new InnerSafeCloseSocket();
                res.SetHandle(acceptedFd);
                return res;
            }
        }
    }
}
