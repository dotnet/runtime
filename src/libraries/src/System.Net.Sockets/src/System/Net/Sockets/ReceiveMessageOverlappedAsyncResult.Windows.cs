// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;
using System.Collections.Generic;

namespace System.Net.Sockets
{
    unsafe internal sealed partial class ReceiveMessageOverlappedAsyncResult : BaseOverlappedAsyncResult
    {
        private Interop.Winsock.WSAMsg* _message;
        private WSABuffer* _wsaBuffer;
        private byte[] _wsaBufferArray;
        private byte[] _controlBuffer;
        internal byte[] _messageBuffer;

        private static readonly int s_controlDataSize = Marshal.SizeOf<Interop.Winsock.ControlData>();
        private static readonly int s_controlDataIPv6Size = Marshal.SizeOf<Interop.Winsock.ControlDataIPv6>();
        private static readonly int s_wsaBufferSize = Marshal.SizeOf<WSABuffer>();
        private static readonly int s_wsaMsgSize = Marshal.SizeOf<Interop.Winsock.WSAMsg>();

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
        internal void SetUnmanagedStructures(byte[] buffer, int offset, int size, Internals.SocketAddress socketAddress, SocketFlags socketFlags)
        {
            _messageBuffer = new byte[s_wsaMsgSize];
            _wsaBufferArray = new byte[s_wsaBufferSize];

            bool ipv4, ipv6;
            Socket.GetIPProtocolInformation(((Socket)AsyncObject).AddressFamily, socketAddress, out ipv4, out ipv6);

            // Prepare control buffer.
            if (ipv4)
            {
                _controlBuffer = new byte[s_controlDataSize];
            }
            else if (ipv6)
            {
                _controlBuffer = new byte[s_controlDataIPv6Size];
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

        unsafe private void InitIPPacketInformation()
        {
            IPAddress address = null;

            if (_controlBuffer.Length == s_controlDataSize)
            {
                // IPv4
                Interop.Winsock.ControlData controlData = Marshal.PtrToStructure<Interop.Winsock.ControlData>(_message->controlBuffer.Pointer);
                if (controlData.length != UIntPtr.Zero)
                {
                    address = new IPAddress((long)controlData.address);
                }

                _ipPacketInformation = new IPPacketInformation(((address != null) ? address : IPAddress.None), (int)controlData.index);
            }
            else if (_controlBuffer.Length == s_controlDataIPv6Size)
            {
                // IPv6
                Interop.Winsock.ControlDataIPv6 controlData = Marshal.PtrToStructure<Interop.Winsock.ControlDataIPv6>(_message->controlBuffer.Pointer);
                if (controlData.length != UIntPtr.Zero)
                {
                    address = new IPAddress(controlData.address);
                }

                _ipPacketInformation = new IPPacketInformation(((address != null) ? address : IPAddress.IPv6None), (int)controlData.index);
            }
            else
            {
                // Other
                _ipPacketInformation = new IPPacketInformation();
            }
        }

        // This method is called after an asynchronous call is made for the user.
        // It checks and acts accordingly if the IO:
        // 1) completed synchronously.
        // 2) was pended.
        // 3) failed.
        internal void SyncReleaseUnmanagedStructures()
        {
            InitIPPacketInformation();
            ForceReleaseUnmanagedStructures();
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
            if (NetEventSource.IsEnabled) NetEventSource.DumpBuffer(this, _wsaBuffer->Pointer, Math.Min(_wsaBuffer->Length, size));
        }
    }
}
