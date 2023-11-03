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

        private static T CreateDelegate<T>(Func<IntPtr, T> functionPointerWrapper, [NotNull] ref T? cache, SafeSocketHandle socketHandle, string guidString) where T: Delegate
        {
            Guid guid = new Guid(guidString);
            IntPtr ptr;
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
                   out _,
                   IntPtr.Zero,
                   IntPtr.Zero);
            }

            if (errorCode != SocketError.Success)
            {
                throw new SocketException();
            }

            Interlocked.CompareExchange(ref cache, functionPointerWrapper(ptr), null);
            return cache;
        }

        internal unsafe AcceptExDelegate GetAcceptExDelegate(SafeSocketHandle socketHandle)
            => _acceptEx ?? CreateDelegate(ptr => new SocketDelegateHelper(ptr).AcceptEx, ref _acceptEx, socketHandle, "b5367df1cbac11cf95ca00805f48a192");

        internal unsafe GetAcceptExSockaddrsDelegate GetGetAcceptExSockaddrsDelegate(SafeSocketHandle socketHandle)
            => _getAcceptExSockaddrs ?? CreateDelegate<GetAcceptExSockaddrsDelegate>(ptr => new SocketDelegateHelper(ptr).GetAcceptExSockaddrs, ref _getAcceptExSockaddrs, socketHandle, "b5367df2cbac11cf95ca00805f48a192");

        internal unsafe ConnectExDelegate GetConnectExDelegate(SafeSocketHandle socketHandle)
            => _connectEx ?? CreateDelegate(ptr => new SocketDelegateHelper(ptr).ConnectEx, ref _connectEx, socketHandle, "25a207b9ddf346608ee976e58c74063e");

        internal unsafe DisconnectExDelegate GetDisconnectExDelegate(SafeSocketHandle socketHandle)
            => _disconnectEx ?? CreateDelegate(ptr => new SocketDelegateHelper(ptr).DisconnectEx, ref _disconnectEx, socketHandle, "7fda2e118630436fa031f536a6eec157");

        internal unsafe WSARecvMsgDelegate GetWSARecvMsgDelegate(SafeSocketHandle socketHandle)
            => _recvMsg ?? CreateDelegate(ptr => new SocketDelegateHelper(ptr).WSARecvMsg, ref _recvMsg, socketHandle, "f689d7c86f1f436b8a53e54fe351c322");

        internal unsafe TransmitPacketsDelegate GetTransmitPacketsDelegate(SafeSocketHandle socketHandle)
            => _transmitPackets ?? CreateDelegate(ptr => new SocketDelegateHelper(ptr).TransmitPackets, ref _transmitPackets, socketHandle, "d9689da01f9011d3997100c04f68c876");

        /// <summary>
        /// The SocketDelegateHelper implements manual marshalling wrappers for the various delegates used for the dynamic Winsock methods.
        /// These wrappers were generated with LibraryImportGenerator and then manually converted to use function pointers as the target instead of a P/Invoke.
        /// </summary>
        private readonly struct SocketDelegateHelper
        {
            private readonly IntPtr _target;

            public SocketDelegateHelper(IntPtr target)
            {
                _target = target;
            }

            internal unsafe bool AcceptEx(SafeSocketHandle listenSocketHandle, SafeSocketHandle acceptSocketHandle, IntPtr buffer, int len, int localAddressLength, int remoteAddressLength, out int bytesReceived, NativeOverlapped* overlapped)
            {
                IntPtr __listenSocketHandle_gen_native = default;
                IntPtr __acceptSocketHandle_gen_native = default;
                bytesReceived = default;
                bool __retVal;
                int __retVal_gen_native = default;
                //
                // Setup
                //
                bool listenSocketHandle__addRefd = false;
                bool acceptSocketHandle__addRefd = false;
                try
                {
                    //
                    // Marshal
                    //
                    listenSocketHandle.DangerousAddRef(ref listenSocketHandle__addRefd);
                    __listenSocketHandle_gen_native = listenSocketHandle.DangerousGetHandle();
                    acceptSocketHandle.DangerousAddRef(ref acceptSocketHandle__addRefd);
                    __acceptSocketHandle_gen_native = acceptSocketHandle.DangerousGetHandle();
                    fixed (int* __bytesReceived_gen_native = &bytesReceived)
                    {
                        __retVal_gen_native = ((delegate* unmanaged<IntPtr, IntPtr, IntPtr, int, int, int, int*, NativeOverlapped*, int>)_target)(__listenSocketHandle_gen_native, __acceptSocketHandle_gen_native, buffer, len, localAddressLength, remoteAddressLength, __bytesReceived_gen_native, overlapped);
                    }
                    Marshal.SetLastPInvokeError(Marshal.GetLastSystemError());
                    //
                    // Unmarshal
                    //
                    __retVal = __retVal_gen_native != 0;
                }
                finally
                {
                    //
                    // Cleanup
                    //
                    if (listenSocketHandle__addRefd)
                        listenSocketHandle.DangerousRelease();
                    if (acceptSocketHandle__addRefd)
                        acceptSocketHandle.DangerousRelease();
                }

                return __retVal;
            }
            internal unsafe void GetAcceptExSockaddrs(IntPtr buffer, int receiveDataLength, int localAddressLength, int remoteAddressLength, out IntPtr localSocketAddress, out int localSocketAddressLength, out IntPtr remoteSocketAddress, out int remoteSocketAddressLength)
            {
                localSocketAddress = default;
                localSocketAddressLength = default;
                remoteSocketAddress = default;
                remoteSocketAddressLength = default;
                fixed (IntPtr* __localSocketAddress_gen_native = &localSocketAddress)
                fixed (int* __localSocketAddressLength_gen_native = &localSocketAddressLength)
                fixed (IntPtr* __remoteSocketAddress_gen_native = &remoteSocketAddress)
                fixed (int* __remoteSocketAddressLength_gen_native = &remoteSocketAddressLength)
                {
                    ((delegate* unmanaged<IntPtr, int, int, int, IntPtr*, int*, IntPtr*, int*, void>)_target)(buffer, receiveDataLength, localAddressLength, remoteAddressLength, __localSocketAddress_gen_native, __localSocketAddressLength_gen_native, __remoteSocketAddress_gen_native, __remoteSocketAddressLength_gen_native);
                }
                Marshal.SetLastPInvokeError(Marshal.GetLastSystemError());

            }
            internal unsafe bool ConnectEx(SafeSocketHandle socketHandle, ReadOnlySpan<byte> socketAddress, IntPtr buffer, int dataLength, out int bytesSent, NativeOverlapped* overlapped)
            {
                IntPtr __socketHandle_gen_native = default;
                bytesSent = default;
                bool __retVal;
                int __retVal_gen_native = default;
                //
                // Setup
                //
                bool socketHandle__addRefd = false;
                try
                {
                    //
                    // Marshal
                    //
                    socketHandle.DangerousAddRef(ref socketHandle__addRefd);
                    __socketHandle_gen_native = socketHandle.DangerousGetHandle();
                    fixed (int* __bytesSent_gen_native = &bytesSent)
                    fixed (void* socketAddressPtr = &MemoryMarshal.GetReference(socketAddress))
                    {
                        __retVal_gen_native = ((delegate* unmanaged<IntPtr, void*, int, IntPtr, int, int*, NativeOverlapped*, int>)_target)(__socketHandle_gen_native, socketAddressPtr, socketAddress.Length, buffer, dataLength, __bytesSent_gen_native, overlapped);
                    }
                    Marshal.SetLastPInvokeError(Marshal.GetLastSystemError());
                    //
                    // Unmarshal
                    //
                    __retVal = __retVal_gen_native != 0;
                }
                finally
                {
                    //
                    // Cleanup
                    //
                    if (socketHandle__addRefd)
                        socketHandle.DangerousRelease();
                }

                return __retVal;
            }
            internal unsafe bool DisconnectEx(SafeSocketHandle socketHandle, NativeOverlapped* overlapped, int flags, int reserved)
            {
                IntPtr __socketHandle_gen_native;
                bool __retVal;
                int __retVal_gen_native;
                //
                // Setup
                //
                bool socketHandle__addRefd = false;
                try
                {
                    //
                    // Marshal
                    //
                    socketHandle.DangerousAddRef(ref socketHandle__addRefd);
                    __socketHandle_gen_native = socketHandle.DangerousGetHandle();
                    __retVal_gen_native = ((delegate* unmanaged<IntPtr, NativeOverlapped*, int, int, int>)_target)(__socketHandle_gen_native, overlapped, flags, reserved);
                    Marshal.SetLastPInvokeError(Marshal.GetLastSystemError());
                    //
                    // Unmarshal
                    //
                    __retVal = __retVal_gen_native != 0;
                }
                finally
                {
                    //
                    // Cleanup
                    //
                    if (socketHandle__addRefd)
                        socketHandle.DangerousRelease();
                }

                return __retVal;
            }
            internal unsafe SocketError WSARecvMsg(SafeSocketHandle socketHandle, IntPtr msg, out int bytesTransferred, NativeOverlapped* overlapped, IntPtr completionRoutine)
            {
                IntPtr __socketHandle_gen_native = default;
                bytesTransferred = default;
                SocketError __retVal;
                //
                // Setup
                //
                bool socketHandle__addRefd = false;
                try
                {
                    //
                    // Marshal
                    //
                    socketHandle.DangerousAddRef(ref socketHandle__addRefd);
                    __socketHandle_gen_native = socketHandle.DangerousGetHandle();
                    fixed (int* __bytesTransferred_gen_native = &bytesTransferred)
                    {
                        __retVal = ((delegate* unmanaged<IntPtr, IntPtr, int *, NativeOverlapped*, IntPtr, SocketError>)_target)(__socketHandle_gen_native, msg, __bytesTransferred_gen_native, overlapped, completionRoutine);
                    }
                    Marshal.SetLastPInvokeError(Marshal.GetLastSystemError());
                }
                finally
                {
                    //
                    // Cleanup
                    //
                    if (socketHandle__addRefd)
                        socketHandle.DangerousRelease();
                }

                return __retVal;
            }
            internal unsafe bool TransmitPackets(SafeSocketHandle socketHandle, IntPtr packetArray, int elementCount, int sendSize, NativeOverlapped* overlapped, TransmitFileOptions flags)
            {
                IntPtr __socketHandle_gen_native;
                bool __retVal;
                int __retVal_gen_native;
                //
                // Setup
                //
                bool socketHandle__addRefd = false;
                try
                {
                    //
                    // Marshal
                    //
                    socketHandle.DangerousAddRef(ref socketHandle__addRefd);
                    __socketHandle_gen_native = socketHandle.DangerousGetHandle();
                    __retVal_gen_native = ((delegate* unmanaged<IntPtr, IntPtr, int, int, NativeOverlapped*, TransmitFileOptions, int>)_target)(__socketHandle_gen_native, packetArray, elementCount, sendSize, overlapped, flags);
                    Marshal.SetLastPInvokeError(Marshal.GetLastSystemError());
                    //
                    // Unmarshal
                    //
                    __retVal = __retVal_gen_native != 0;
                }
                finally
                {
                    //
                    // Cleanup
                    //
                    if (socketHandle__addRefd)
                        socketHandle.DangerousRelease();
                }

                return __retVal;
            }
        }
    }

    internal unsafe delegate bool AcceptExDelegate(
                SafeSocketHandle listenSocketHandle,
                SafeSocketHandle acceptSocketHandle,
                IntPtr buffer,
                int len,
                int localAddressLength,
                int remoteAddressLength,
                out int bytesReceived,
                NativeOverlapped* overlapped);

    internal delegate void GetAcceptExSockaddrsDelegate(
                IntPtr buffer,
                int receiveDataLength,
                int localAddressLength,
                int remoteAddressLength,
                out IntPtr localSocketAddress,
                out int localSocketAddressLength,
                out IntPtr remoteSocketAddress,
                out int remoteSocketAddressLength);

    internal unsafe delegate bool ConnectExDelegate(
                SafeSocketHandle socketHandle,
                ReadOnlySpan<byte> socketAddress,
                IntPtr buffer,
                int dataLength,
                out int bytesSent,
                NativeOverlapped* overlapped);

    internal unsafe delegate bool DisconnectExDelegate(
                SafeSocketHandle socketHandle,
                NativeOverlapped* overlapped,
                int flags,
                int reserved);

    internal unsafe delegate SocketError WSARecvMsgDelegate(
                SafeSocketHandle socketHandle,
                IntPtr msg,
                out int bytesTransferred,
                NativeOverlapped* overlapped,
                IntPtr completionRoutine);

    internal unsafe delegate bool TransmitPacketsDelegate(
                SafeSocketHandle socketHandle,
                IntPtr packetArray,
                int elementCount,
                int sendSize,
                NativeOverlapped* overlapped,
                TransmitFileOptions flags);
}
