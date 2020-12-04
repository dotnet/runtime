// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Net.Sockets
{
    internal sealed class DynamicWinsockMethods
    {
        // In practice there will never be more than four of these, so its not worth a complicated
        // hash table structure.  Store them in a list and search through it.
        private static readonly List<DynamicWinsockMethods> s_methodTable = new List<DynamicWinsockMethods>();

        public static DynamicWinsockMethods GetMethods(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
        {
            lock (s_methodTable)
            {
                DynamicWinsockMethods methods;

                for (int i = 0; i < s_methodTable.Count; i++)
                {
                    methods = s_methodTable[i];
                    if (methods._addressFamily == addressFamily && methods._socketType == socketType && methods._protocolType == protocolType)
                    {
                        return methods;
                    }
                }

                methods = new DynamicWinsockMethods(addressFamily, socketType, protocolType);
                s_methodTable.Add(methods);
                return methods;
            }
        }

        private readonly AddressFamily _addressFamily;
        private readonly SocketType _socketType;
        private readonly ProtocolType _protocolType;

        private AcceptExDelegate? _acceptEx;
        private GetAcceptExSockaddrsDelegate? _getAcceptExSockaddrs;
        private ConnectExDelegate? _connectEx;
        private TransmitPacketsDelegate? _transmitPackets;
        private DisconnectExDelegate? _disconnectEx;
        private WSARecvMsgDelegate? _recvMsg;

        private DynamicWinsockMethods(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
        {
            _addressFamily = addressFamily;
            _socketType = socketType;
            _protocolType = protocolType;
        }

        private static T CreateDelegate<T>([NotNull] ref T? cache, SafeSocketHandle socketHandle, string guidString) where T: Delegate
        {
            Guid guid = new Guid(guidString);
            IntPtr ptr = IntPtr.Zero;
            int length;
            SocketError errorCode;

            unsafe
            {
                errorCode = Interop.Winsock.WSAIoctl(
                   socketHandle,
                   Interop.Winsock.IoctlSocketConstants.SIOGETEXTENSIONFUNCTIONPOINTER,
                   ref guid,
                   sizeof(Guid),
                   out ptr,
                   sizeof(IntPtr),
                   out length,
                   IntPtr.Zero,
                   IntPtr.Zero);
            }

            if (errorCode != SocketError.Success)
            {
                throw new SocketException();
            }

            Interlocked.CompareExchange(ref cache, Marshal.GetDelegateForFunctionPointer<T>(ptr), null);
            return cache;
        }

        internal AcceptExDelegate GetAcceptExDelegate(SafeSocketHandle socketHandle)
            => _acceptEx ?? CreateDelegate(ref _acceptEx, socketHandle, "b5367df1cbac11cf95ca00805f48a192");

        internal GetAcceptExSockaddrsDelegate GetGetAcceptExSockaddrsDelegate(SafeSocketHandle socketHandle)
            => _getAcceptExSockaddrs ?? CreateDelegate(ref _getAcceptExSockaddrs, socketHandle, "b5367df2cbac11cf95ca00805f48a192");

        internal ConnectExDelegate GetConnectExDelegate(SafeSocketHandle socketHandle)
            => _connectEx ?? CreateDelegate(ref _connectEx, socketHandle, "25a207b9ddf346608ee976e58c74063e");

        internal DisconnectExDelegate GetDisconnectExDelegate(SafeSocketHandle socketHandle)
            => _disconnectEx ?? CreateDelegate(ref _disconnectEx, socketHandle, "7fda2e118630436fa031f536a6eec157");

        internal WSARecvMsgDelegate GetWSARecvMsgDelegate(SafeSocketHandle socketHandle)
            => _recvMsg ?? CreateDelegate(ref _recvMsg, socketHandle, "f689d7c86f1f436b8a53e54fe351c322");

        internal TransmitPacketsDelegate GetTransmitPacketsDelegate(SafeSocketHandle socketHandle)
            => _transmitPackets ?? CreateDelegate(ref _transmitPackets, socketHandle, "d9689da01f9011d3997100c04f68c876");
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
    internal unsafe delegate bool AcceptExDelegate(
                SafeSocketHandle listenSocketHandle,
                SafeSocketHandle acceptSocketHandle,
                IntPtr buffer,
                int len,
                int localAddressLength,
                int remoteAddressLength,
                out int bytesReceived,
                NativeOverlapped* overlapped);

    [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
    internal delegate void GetAcceptExSockaddrsDelegate(
                IntPtr buffer,
                int receiveDataLength,
                int localAddressLength,
                int remoteAddressLength,
                out IntPtr localSocketAddress,
                out int localSocketAddressLength,
                out IntPtr remoteSocketAddress,
                out int remoteSocketAddressLength);


    [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
    internal unsafe delegate bool ConnectExDelegate(
                SafeSocketHandle socketHandle,
                IntPtr socketAddress,
                int socketAddressSize,
                IntPtr buffer,
                int dataLength,
                out int bytesSent,
                NativeOverlapped* overlapped);

    [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
    internal unsafe delegate bool DisconnectExDelegate(
                SafeSocketHandle socketHandle,
                NativeOverlapped* overlapped,
                int flags,
                int reserved);

    [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
    internal unsafe delegate SocketError WSARecvMsgDelegate(
                SafeSocketHandle socketHandle,
                IntPtr msg,
                out int bytesTransferred,
                NativeOverlapped* overlapped,
                IntPtr completionRoutine);

    [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
    internal unsafe delegate bool TransmitPacketsDelegate(
                SafeSocketHandle socketHandle,
                IntPtr packetArray,
                int elementCount,
                int sendSize,
                NativeOverlapped* overlapped,
                TransmitFileOptions flags);
}
