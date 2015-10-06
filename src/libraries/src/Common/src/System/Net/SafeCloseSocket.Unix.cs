// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        public SocketAsyncContext AsyncContext
        {
            get
            {
                return _innerSocket.AsyncContext;
            }
        }

        public int FileDescriptor
        {
            get
            {
                return (int)handle;
            }
        }

        public bool IsNonBlocking { get; set; }

        public int ReceiveTimeout
        {
            get
            {
                return _receiveTimeout;
            }
            set
            {
                Debug.Assert(value == -1 || value > 0);
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
                Debug.Assert(value == -1 || value > 0);
                _sendTimeout = value;
            }
        }

        public unsafe static SafeCloseSocket CreateSocket(int fileDescriptor)
        {
            return CreateSocket(InnerSafeCloseSocket.CreateSocket(fileDescriptor));
        }

        public unsafe static SafeCloseSocket CreateSocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
        {
            return CreateSocket(InnerSafeCloseSocket.CreateSocket(addressFamily, socketType, protocolType));
        }

        public unsafe static SafeCloseSocket Accept(SafeCloseSocket socketHandle, byte[] socketAddress, ref int socketAddressSize)
        {
            return CreateSocket(InnerSafeCloseSocket.Accept(socketHandle, socketAddress, ref socketAddressSize));
        }

        private void InnerReleaseHandle()
        {
            // No-op for Unix.
        }

        internal sealed partial class InnerSafeCloseSocket : SafeHandleMinusOneIsInvalid
        {
            private SocketAsyncContext _asyncContext;

            public SocketAsyncContext AsyncContext
            {
                get
                {
                    if (Volatile.Read(ref _asyncContext) == null)
                    {
                        Interlocked.CompareExchange(ref _asyncContext, new SocketAsyncContext((int)handle, SocketAsyncEngine.Instance), null);
                    }
                    return _asyncContext;
                }
            }

            private unsafe SocketError InnerReleaseHandle()
            {
                int errorCode;

                if (_asyncContext != null)
                {
                    _asyncContext.Close();
                }

                // If _blockable was set in BlockingRelease, it's safe to block here, which means
                // we can honor the linger options set on the socket.  It also means closesocket() might return WSAEWOULDBLOCK, in which
                // case we need to do some recovery.
                if (_blockable)
                {
                    GlobalLog.Print("SafeCloseSocket::ReleaseHandle(handle:" + handle.ToString("x") + ") Following 'blockable' branch.");

                    errorCode = Interop.Sys.Close((int)handle);
                    if (errorCode == -1)
                    {
                        errorCode = (int)Interop.Sys.GetLastError();
                    }
                    GlobalLog.Print("SafeCloseSocket::ReleaseHandle(handle:" + handle.ToString("x") + ") close()#1:" + errorCode.ToString());

#if DEBUG
                    _closeSocketHandle = handle;
                    _closeSocketResult = SocketPal.GetSocketErrorForErrorCode((Interop.Error)errorCode);
#endif

                    // If it's not EWOULDBLOCK, there's no more recourse - we either succeeded or failed.
                    if (errorCode != (int)Interop.Error.EWOULDBLOCK)
                    {
                        if (errorCode == 0 && _asyncContext != null)
                        {
                            _asyncContext.Close();
                        }
                        return SocketPal.GetSocketErrorForErrorCode((Interop.Error)errorCode);
                    }

                    // The socket must be non-blocking with a linger timeout set.
                    // We have to set the socket to blocking.
                    errorCode = Interop.Sys.Fcntl.SetIsNonBlocking((int)handle, 0);
                    if (errorCode == 0)
                    {
                        // The socket successfully made blocking; retry the close().
                        errorCode = Interop.Sys.Close((int)handle);

                        GlobalLog.Print("SafeCloseSocket::ReleaseHandle(handle:" + handle.ToString("x") + ") close()#2:" + errorCode.ToString());
#if DEBUG
                        _closeSocketHandle = handle;
                        _closeSocketResult = SocketPal.GetSocketErrorForErrorCode((Interop.Error)errorCode);
#endif
                        if (errorCode == 0 && _asyncContext != null)
                        {
                            _asyncContext.Close();
                        }
                        return SocketPal.GetSocketErrorForErrorCode((Interop.Error)errorCode);
                    }

                    // The socket could not be made blocking; fall through to the regular abortive close.
                }

                // By default or if CloseAsIs() path failed, set linger timeout to zero to get an abortive close (RST).
                var linger = new Interop.libc.linger {
                    l_onoff = 1,
                    l_linger = 0
                };

                errorCode = Interop.libc.setsockopt((int)handle, Interop.libc.SOL_SOCKET, Interop.libc.SO_LINGER, &linger, (uint)sizeof(Interop.libc.linger));
#if DEBUG
                _closeSocketLinger = SocketPal.GetSocketErrorForErrorCode((Interop.Error)errorCode);
#endif
                if (errorCode == -1)
                {
                    errorCode = (int)Interop.Sys.GetLastError();
                }
                GlobalLog.Print("SafeCloseSocket::ReleaseHandle(handle:" + handle.ToString("x") + ") setsockopt():" + errorCode.ToString());

                if (errorCode != 0 && errorCode != (int)Interop.Error.EINVAL && errorCode != (int)Interop.Error.ENOPROTOOPT)
                {
                    // Too dangerous to try closesocket() - it might block!
                    return SocketPal.GetSocketErrorForErrorCode((Interop.Error)errorCode);
                }

                errorCode = Interop.Sys.Close((int)handle);
#if DEBUG
                _closeSocketHandle = handle;
                _closeSocketResult = SocketPal.GetSocketErrorForErrorCode((Interop.Error)errorCode);
#endif
                GlobalLog.Print("SafeCloseSocket::ReleaseHandle(handle:" + handle.ToString("x") + ") close#3():" + (errorCode == -1 ? (int)Interop.Sys.GetLastError() : errorCode).ToString());

                return SocketPal.GetSocketErrorForErrorCode((Interop.Error)errorCode);
            }

            public static InnerSafeCloseSocket CreateSocket(int fileDescriptor)
            {
                var res = new InnerSafeCloseSocket();
                res.SetHandle((IntPtr)fileDescriptor);
                return res;
            }

            public static unsafe InnerSafeCloseSocket CreateSocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
            {
                int af = SocketPal.GetPlatformAddressFamily(addressFamily);
                int sock = SocketPal.GetPlatformSocketType(socketType);
                int pt = (int)protocolType;

                int fd = Interop.libc.socket(af, sock, pt);
                if (fd != -1)
                {
                    // The socket was created successfully; make it non-blocking and enable
                    // IPV6_V6ONLY by default for AF_INET6 sockets.
                    int err = Interop.Sys.Fcntl.SetIsNonBlocking(fd, 1);
                    if (err != 0)
                    {
                        Interop.Sys.Close(fd);
                        fd = -1;
                    }
                    else if (addressFamily == AddressFamily.InterNetworkV6)
                    {
                        int on = 1;
                        err = Interop.libc.setsockopt(fd, Interop.libc.IPPROTO_IPV6, Interop.libc.IPV6_V6ONLY, &on, (uint)sizeof(int));
                        if (err != 0)
                        {
                            Interop.Sys.Close(fd);
                            fd = -1;
                        }
                    }
                }

                var res = new InnerSafeCloseSocket();
                res.SetHandle((IntPtr)fd);
                return res;
            }

            public static unsafe InnerSafeCloseSocket Accept(SafeCloseSocket socketHandle, byte[] socketAddress, ref int socketAddressLen)
            {
                int acceptedFd;
                if (!socketHandle.IsNonBlocking)
                {
                    socketHandle.AsyncContext.Accept(socketAddress, ref socketAddressLen, -1, out acceptedFd);
                }
                else
                {
                    SocketError unused;
                    SocketPal.TryCompleteAccept(socketHandle.FileDescriptor, socketAddress, ref socketAddressLen, out acceptedFd, out unused);
                }

                var res = new InnerSafeCloseSocket();
                res.SetHandle((IntPtr)acceptedFd);
                return res;
            }
        }
    }
}
