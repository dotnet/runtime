// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Net.Sockets
{
    // AcceptOverlappedAsyncResult - used to take care of storage for async Socket BeginAccept call.
    internal sealed partial class AcceptOverlappedAsyncResult : BaseOverlappedAsyncResult
    {
        private Socket _acceptSocket;
        private int _addressBufferLength;

        // This method will be called by us when the IO completes synchronously and
        // by the ThreadPool when the IO completes asynchronously. (only called on WinNT)
        internal override object PostCompletion(int numBytes)
        {
            SocketError errorCode = (SocketError)ErrorCode;

            Internals.SocketAddress remoteSocketAddress = null;
            if (errorCode == SocketError.Success)
            {
                _numBytes = numBytes;
                if (NetEventSource.IsEnabled) LogBuffer(numBytes);

                // get the endpoint
                remoteSocketAddress = IPEndPointExtensions.Serialize(_listenSocket._rightEndPoint);

                IntPtr localAddr;
                int localAddrLength;
                IntPtr remoteAddr;

                // set the socket context
                try
                {
                    _listenSocket.GetAcceptExSockaddrs(
                        Marshal.UnsafeAddrOfPinnedArrayElement(_buffer, 0),
                        _buffer.Length - (_addressBufferLength * 2),
                        _addressBufferLength,
                        _addressBufferLength,
                        out localAddr,
                        out localAddrLength,
                        out remoteAddr,
                        out remoteSocketAddress.InternalSize);

                    Marshal.Copy(remoteAddr, remoteSocketAddress.Buffer, 0, remoteSocketAddress.Size);

                    IntPtr handle = _listenSocket.SafeHandle.DangerousGetHandle();

                    errorCode = Interop.Winsock.setsockopt(
                        _acceptSocket.SafeHandle,
                        SocketOptionLevel.Socket,
                        SocketOptionName.UpdateAcceptContext,
                        ref handle,
                        IntPtr.Size);

                    if (errorCode == SocketError.SocketError)
                    {
                        errorCode = SocketPal.GetLastSocketError();
                    }

                    if (NetEventSource.IsEnabled) NetEventSource.Info(this, $"setsockopt handle:{handle}, AcceptSocket:{_acceptSocket}, returns:{errorCode}");
                }
                catch (ObjectDisposedException)
                {
                    errorCode = SocketError.OperationAborted;
                }

                ErrorCode = (int)errorCode;
            }

            if (errorCode != SocketError.Success)
            {
                return null;
            }

            return _listenSocket.UpdateAcceptSocket(_acceptSocket, _listenSocket._rightEndPoint.Create(remoteSocketAddress));
        }

        // SetUnmanagedStructures
        //
        // This method fills in overlapped structures used in an asynchronous 
        // overlapped Winsock call. These calls are outside the runtime and are
        // unmanaged code, so we need to prepare specific structures and ints that
        // lie in unmanaged memory since the overlapped calls may complete asynchronously.
        internal void SetUnmanagedStructures(byte[] buffer, int addressBufferLength)
        {
            // has to be called first to pin memory
            base.SetUnmanagedStructures(buffer);

            // Fill in Buffer Array structure that will be used for our send/recv Buffer
            _addressBufferLength = addressBufferLength;
            _buffer = buffer;
        }

        private void LogBuffer(long size)
        {
            // This should only be called if tracing is enabled. However, there is the potential for a race
            // condition where tracing is disabled between a calling check and here, in which case the assert
            // may fire erroneously.
            Debug.Assert(NetEventSource.IsEnabled);

            if (size > -1)
            {
                NetEventSource.DumpBuffer(this, _buffer, 0, Math.Min((int)size, _buffer.Length));
            }
            else
            {
                NetEventSource.DumpBuffer(this, _buffer);
            }
        }

        internal Socket AcceptSocket
        {
            set
            {
                _acceptSocket = value;
            }
        }
    }
}
