// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Net.Sockets
{
    internal unsafe sealed partial class ReceiveMessageOverlappedAsyncResult : BaseOverlappedAsyncResult
    {
        private Interop.Winsock.WSAMsg* _message;
        private WSABuffer* _wsaBuffer;
        private byte[] _wsaBufferArray;
        private byte[] _controlBuffer;
        internal byte[] _messageBuffer;

        private IntPtr GetSocketAddressSizePtr()
        {
            return Marshal.UnsafeAddrOfPinnedArrayElement(_socketAddress.Buffer, _socketAddress.GetAddressSizeOffset());
        }

        internal unsafe int GetSocketAddressSize()
        {
            return *(int*)GetSocketAddressSizePtr();
        }

        // SetUnmanagedStructures
        //
        // Fills in overlapped Structures used in an async overlapped Winsock call.
        // These calls are outside the runtime and are unmanaged code, so we need
        // to prepare specific structures and ints that lie in unmanaged memory
        // since the overlapped calls may complete asynchronously.
        internal unsafe void SetUnmanagedStructures(byte[] buffer, int offset, int size, Internals.SocketAddress socketAddress, SocketFlags socketFlags)
        {
            _messageBuffer = new byte[sizeof(Interop.Winsock.WSAMsg)];
            _wsaBufferArray = new byte[sizeof(WSABuffer)];

            bool ipv4, ipv6;
            Socket.GetIPProtocolInformation(((Socket)AsyncObject).AddressFamily, socketAddress, out ipv4, out ipv6);

            // Prepare control buffer.
            if (ipv4)
            {
                _controlBuffer = new byte[sizeof(Interop.Winsock.ControlData)];
            }
            else if (ipv6)
            {
                _controlBuffer = new byte[sizeof(Interop.Winsock.ControlDataIPv6)];
            }

            // Pin buffers.
            object[] objectsToPin = new object[(_controlBuffer != null) ? 5 : 4];
            objectsToPin[0] = buffer;
            objectsToPin[1] = _messageBuffer;
            objectsToPin[2] = _wsaBufferArray;

            // Prepare socketaddress buffer.
            _socketAddress = socketAddress;
            _socketAddress.CopyAddressSizeIntoBuffer();
            objectsToPin[3] = _socketAddress.Buffer;

            if (_controlBuffer != null)
            {
                objectsToPin[4] = _controlBuffer;
            }

            base.SetUnmanagedStructures(objectsToPin);

            // Prepare data buffer.
            _wsaBuffer = (WSABuffer*)Marshal.UnsafeAddrOfPinnedArrayElement(_wsaBufferArray, 0);
            _wsaBuffer->Length = size;
            _wsaBuffer->Pointer = Marshal.UnsafeAddrOfPinnedArrayElement(buffer, offset);


            // Setup structure.
            _message = (Interop.Winsock.WSAMsg*)Marshal.UnsafeAddrOfPinnedArrayElement(_messageBuffer, 0);
            _message->socketAddress = Marshal.UnsafeAddrOfPinnedArrayElement(_socketAddress.Buffer, 0);
            _message->addressLength = (uint)_socketAddress.Size;
            _message->buffers = Marshal.UnsafeAddrOfPinnedArrayElement(_wsaBufferArray, 0);
            _message->count = 1;

            if (_controlBuffer != null)
            {
                _message->controlBuffer.Pointer = Marshal.UnsafeAddrOfPinnedArrayElement(_controlBuffer, 0);
                _message->controlBuffer.Length = _controlBuffer.Length;
            }

            _message->flags = socketFlags;
        }

        private unsafe void InitIPPacketInformation()
        {
            if (_controlBuffer.Length == sizeof(Interop.Winsock.ControlData))
            {
                // IPv4
                _ipPacketInformation = SocketPal.GetIPPacketInformation((Interop.Winsock.ControlData*)_message->controlBuffer.Pointer);
            }
            else if (_controlBuffer.Length == sizeof(Interop.Winsock.ControlDataIPv6))
            {
                // IPv6
                _ipPacketInformation = SocketPal.GetIPPacketInformation((Interop.Winsock.ControlDataIPv6*)_message->controlBuffer.Pointer);
            }
            else
            {
                // Other
                _ipPacketInformation = new IPPacketInformation();
            }
        }

        protected override void ForceReleaseUnmanagedStructures()
        {
            _socketFlags = _message->flags;
            base.ForceReleaseUnmanagedStructures();
        }

        internal override object PostCompletion(int numBytes)
        {
            InitIPPacketInformation();
            if (ErrorCode == 0 && NetEventSource.IsEnabled)
            {
                LogBuffer(numBytes);
            }

            return base.PostCompletion(numBytes);
        }

        private void LogBuffer(int size)
        {
            // This should only be called if tracing is enabled. However, there is the potential for a race
            // condition where tracing is disabled between a calling check and here, in which case the assert
            // may fire erroneously.
            Debug.Assert(NetEventSource.IsEnabled);

            NetEventSource.DumpBuffer(this, _wsaBuffer->Pointer, Math.Min(_wsaBuffer->Length, size));
        }
    }
}
