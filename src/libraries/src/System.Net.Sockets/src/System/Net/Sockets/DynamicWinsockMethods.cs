// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Net.Sockets
{
    internal sealed class DynamicWinsockMethods
    {
        // In practice there will never be more than four of these, so its not worth a complicated
        // hash table structure.  Store them in a list and search through it.
        private static List<DynamicWinsockMethods> s_methodTable = new List<DynamicWinsockMethods>();

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

        private AddressFamily _addressFamily;
        private SocketType _socketType;
        private ProtocolType _protocolType;
        private object _lockObject;

        private AcceptExDelegate _acceptEx;
        private GetAcceptExSockaddrsDelegate _getAcceptExSockaddrs;
        private ConnectExDelegate _connectEx;
        private TransmitPacketsDelegate _transmitPackets;

        private DisconnectExDelegate _disconnectEx;
        private DisconnectExDelegateBlocking _disconnectExBlocking;

        private WSARecvMsgDelegate _recvMsg;
        private WSARecvMsgDelegateBlocking _recvMsgBlocking;

        private DynamicWinsockMethods(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
        {
            _addressFamily = addressFamily;
            _socketType = socketType;
            _protocolType = protocolType;
            _lockObject = new object();
        }

        public T GetDelegate<T>(SafeCloseSocket socketHandle)
            where T : class
        {
            if (typeof(T) == typeof(AcceptExDelegate))
            {
                EnsureAcceptEx(socketHandle);
                Debug.Assert(_acceptEx != null);
                return (T)(object)_acceptEx;
            }
            else if (typeof(T) == typeof(GetAcceptExSockaddrsDelegate))
            {
                EnsureGetAcceptExSockaddrs(socketHandle);
                Debug.Assert(_getAcceptExSockaddrs != null);
                return (T)(object)_getAcceptExSockaddrs;
            }
            else if (typeof(T) == typeof(ConnectExDelegate))
            {
                EnsureConnectEx(socketHandle);
                Debug.Assert(_connectEx != null);
                return (T)(object)_connectEx;
            }
            else if (typeof(T) == typeof(DisconnectExDelegate))
            {
                EnsureDisconnectEx(socketHandle);
                return (T)(object)_disconnectEx;
            }
            else if (typeof(T) == typeof(DisconnectExDelegateBlocking))
            {
                EnsureDisconnectEx(socketHandle);
                return (T)(object)_disconnectExBlocking;
            }
            else if (typeof(T) == typeof(WSARecvMsgDelegate))
            {
                EnsureWSARecvMsg(socketHandle);
                Debug.Assert(_recvMsg != null);
                return (T)(object)_recvMsg;
            }
            else if (typeof(T) == typeof(WSARecvMsgDelegateBlocking))
            {
                EnsureWSARecvMsgBlocking(socketHandle);
                Debug.Assert(_recvMsgBlocking != null);
                return (T)(object)_recvMsgBlocking;
            }
            else if (typeof(T) == typeof(TransmitPacketsDelegate))
            {
                EnsureTransmitPackets(socketHandle);
                Debug.Assert(_transmitPackets != null);
                return (T)(object)_transmitPackets;
            }

            Debug.Fail("Invalid type passed to DynamicWinsockMethods.GetDelegate");
            return null;
        }

        // Private methods that actually load the function pointers.
        private IntPtr LoadDynamicFunctionPointer(SafeCloseSocket socketHandle, ref Guid guid)
        {
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

            return ptr;
        }

        // NOTE: the volatile writes in the functions below are necessary to ensure that all writes
        //       to the fields of the delegate instances are visible before the write to the field
        //       that holds the reference to the delegate instance.

        private void EnsureAcceptEx(SafeCloseSocket socketHandle)
        {
            if (_acceptEx == null)
            {
                lock (_lockObject)
                {
                    if (_acceptEx == null)
                    {
                        Guid guid = new Guid("{0xb5367df1,0xcbac,0x11cf,{0x95, 0xca, 0x00, 0x80, 0x5f, 0x48, 0xa1, 0x92}}");
                        IntPtr ptrAcceptEx = LoadDynamicFunctionPointer(socketHandle, ref guid);
                        Volatile.Write(ref _acceptEx, Marshal.GetDelegateForFunctionPointer<AcceptExDelegate>(ptrAcceptEx));
                    }
                }
            }
        }

        private void EnsureGetAcceptExSockaddrs(SafeCloseSocket socketHandle)
        {
            if (_getAcceptExSockaddrs == null)
            {
                lock (_lockObject)
                {
                    if (_getAcceptExSockaddrs == null)
                    {
                        Guid guid = new Guid("{0xb5367df2,0xcbac,0x11cf,{0x95, 0xca, 0x00, 0x80, 0x5f, 0x48, 0xa1, 0x92}}");
                        IntPtr ptrGetAcceptExSockaddrs = LoadDynamicFunctionPointer(socketHandle, ref guid);
                        Volatile.Write(ref _getAcceptExSockaddrs, Marshal.GetDelegateForFunctionPointer<GetAcceptExSockaddrsDelegate>(ptrGetAcceptExSockaddrs));
                    }
                }
            }
        }

        private void EnsureConnectEx(SafeCloseSocket socketHandle)
        {
            if (_connectEx == null)
            {
                lock (_lockObject)
                {
                    if (_connectEx == null)
                    {
                        Guid guid = new Guid("{0x25a207b9,0x0ddf3,0x4660,{0x8e,0xe9,0x76,0xe5,0x8c,0x74,0x06,0x3e}}");
                        IntPtr ptrConnectEx = LoadDynamicFunctionPointer(socketHandle, ref guid);
                        Volatile.Write(ref _connectEx, Marshal.GetDelegateForFunctionPointer<ConnectExDelegate>(ptrConnectEx));
                    }
                }
            }
        }

        private void EnsureDisconnectEx(SafeCloseSocket socketHandle)
        {
            if (_disconnectEx == null)
            {
                lock (_lockObject)
                {
                    if (_disconnectEx == null)
                    {
                        Guid guid = new Guid("{0x7fda2e11,0x8630,0x436f,{0xa0, 0x31, 0xf5, 0x36, 0xa6, 0xee, 0xc1, 0x57}}");
                        IntPtr ptrDisconnectEx = LoadDynamicFunctionPointer(socketHandle, ref guid);
                        _disconnectExBlocking = Marshal.GetDelegateForFunctionPointer<DisconnectExDelegateBlocking>(ptrDisconnectEx);
                        Volatile.Write(ref _disconnectEx, Marshal.GetDelegateForFunctionPointer<DisconnectExDelegate>(ptrDisconnectEx));
                    }
                }
            }
        }
        private void EnsureWSARecvMsg(SafeCloseSocket socketHandle)
        {
            if (_recvMsg == null)
            {
                lock (_lockObject)
                {
                    if (_recvMsg == null)
                    {
                        Guid guid = new Guid("{0xf689d7c8,0x6f1f,0x436b,{0x8a,0x53,0xe5,0x4f,0xe3,0x51,0xc3,0x22}}");
                        IntPtr ptrWSARecvMsg = LoadDynamicFunctionPointer(socketHandle, ref guid);
                        _recvMsgBlocking = Marshal.GetDelegateForFunctionPointer<WSARecvMsgDelegateBlocking>(ptrWSARecvMsg);
                        Volatile.Write(ref _recvMsg, Marshal.GetDelegateForFunctionPointer<WSARecvMsgDelegate>(ptrWSARecvMsg));
                    }
                }
            }
        }

        private void EnsureWSARecvMsgBlocking(SafeCloseSocket socketHandle)
        {
            if (_recvMsgBlocking == null)
            {
                lock (_lockObject)
                {
                    if (_recvMsgBlocking == null)
                    {
                        Guid guid = new Guid("{0xf689d7c8,0x6f1f,0x436b,{0x8a,0x53,0xe5,0x4f,0xe3,0x51,0xc3,0x22}}");
                        IntPtr ptrWSARecvMsg = LoadDynamicFunctionPointer(socketHandle, ref guid);
                        Volatile.Write(ref _recvMsgBlocking, Marshal.GetDelegateForFunctionPointer<WSARecvMsgDelegateBlocking>(ptrWSARecvMsg));
                    }
                }
            }
        }

        private void EnsureTransmitPackets(SafeCloseSocket socketHandle)
        {
            if (_transmitPackets == null)
            {
                lock (_lockObject)
                {
                    if (_transmitPackets == null)
                    {
                        Guid guid = new Guid("{0xd9689da0,0x1f90,0x11d3,{0x99,0x71,0x00,0xc0,0x4f,0x68,0xc8,0x76}}");
                        IntPtr ptrTransmitPackets = LoadDynamicFunctionPointer(socketHandle, ref guid);
                        Volatile.Write(ref _transmitPackets, Marshal.GetDelegateForFunctionPointer<TransmitPacketsDelegate>(ptrTransmitPackets));
                    }
                }
            }
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError=true)]
    internal unsafe delegate bool AcceptExDelegate(
                SafeCloseSocket listenSocketHandle,
                SafeCloseSocket acceptSocketHandle,
                IntPtr buffer,
                int len,
                int localAddressLength,
                int remoteAddressLength,
                out int bytesReceived,
                NativeOverlapped* overlapped);

    [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError=true)]
    internal delegate void GetAcceptExSockaddrsDelegate(
                IntPtr buffer,
                int receiveDataLength,
                int localAddressLength,
                int remoteAddressLength,
                out IntPtr localSocketAddress,
                out int localSocketAddressLength,
                out IntPtr remoteSocketAddress,
                out int remoteSocketAddressLength);


    [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError=true)]
    internal unsafe delegate bool ConnectExDelegate(
                SafeCloseSocket socketHandle,
                IntPtr socketAddress,
                int socketAddressSize,
                IntPtr buffer,
                int dataLength,
                out int bytesSent,
                NativeOverlapped* overlapped);

    [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError=true)]
    internal unsafe delegate bool DisconnectExDelegate(
                SafeCloseSocket socketHandle, 
                NativeOverlapped* overlapped, 
                int flags, 
                int reserved);

    [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError=true)]
    internal delegate bool DisconnectExDelegateBlocking(
                SafeCloseSocket socketHandle, 
                IntPtr overlapped, 
                int flags, 
                int reserved);

    [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError=true)]
    internal unsafe delegate SocketError WSARecvMsgDelegate(
                SafeCloseSocket socketHandle,
                IntPtr msg,
                out int bytesTransferred,
                NativeOverlapped* overlapped,
                IntPtr completionRoutine);

    [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError=true)]
    internal delegate SocketError WSARecvMsgDelegateBlocking(
                IntPtr socketHandle,
                IntPtr msg,
                out int bytesTransferred,
                IntPtr overlapped,
                IntPtr completionRoutine);

    [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError=true)]
    internal unsafe delegate bool TransmitPacketsDelegate(
                SafeCloseSocket socketHandle,
                IntPtr packetArray,
                int elementCount,
                int sendSize,
                NativeOverlapped* overlapped,
                TransmitFileOptions flags);
}
