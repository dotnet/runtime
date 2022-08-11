// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafePipeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private const int DefaultInvalidHandle = -1;

        // For anonymous pipes, SafePipeHandle.handle is the file descriptor of the pipe.
        // For named pipes, SafePipeHandle.handle is a copy of the file descriptor
        // extracted from the Socket's SafeHandle.
        // This allows operations related to file descriptors to be performed directly on the SafePipeHandle,
        // and operations that should go through the Socket to be done via PipeSocket. We keep the
        // Socket's SafeHandle alive as long as this SafeHandle is alive.

        private Socket? _pipeSocket;
        private SafeHandle? _pipeSocketHandle;
        private volatile int _disposed;

        internal SafePipeHandle(Socket namedPipeSocket) : base(ownsHandle: true)
        {
            SetPipeSocketInterlocked(namedPipeSocket, ownsHandle: true);
            base.SetHandle(_pipeSocketHandle!.DangerousGetHandle());
        }

        internal Socket PipeSocket => _pipeSocket ?? CreatePipeSocket();

        internal SafeHandle? PipeSocketHandle => _pipeSocketHandle;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing); // must be called before trying to Dispose the socket
            _disposed = 1;
            if (disposing && Volatile.Read(ref _pipeSocket) is Socket socket)
            {
                socket.Dispose();
                _pipeSocket = null;
            }
        }

        protected override bool ReleaseHandle()
        {
            Debug.Assert(!IsInvalid);

            if (_pipeSocketHandle != null)
            {
                base.SetHandle((IntPtr)DefaultInvalidHandle);
                _pipeSocketHandle.DangerousRelease();
                _pipeSocketHandle = null;
                return true;
            }
            else
            {
                return (long)handle >= 0 ?
                    Interop.Sys.Close(handle) == 0 :
                    true;
            }
        }

        public override bool IsInvalid
        {
            get { return (long)handle < 0 && _pipeSocket == null; }
        }

        private Socket CreatePipeSocket(bool ownsHandle = true)
        {
            Socket? socket = null;
            if (_disposed == 0)
            {
                bool refAdded = false;
                try
                {
                    DangerousAddRef(ref refAdded);

                    socket = SetPipeSocketInterlocked(new Socket(new SafeSocketHandle(handle, ownsHandle)), ownsHandle);

                    // Double check if we haven't Disposed in the meanwhile, and ensure
                    // the Socket is disposed, in case Dispose() missed the _pipeSocket assignment.
                    if (_disposed == 1)
                    {
                        Volatile.Write(ref _pipeSocket, null);
                        socket.Dispose();
                        socket = null;
                    }
                }
                finally
                {
                    if (refAdded)
                    {
                        DangerousRelease();
                    }
                }
            }

            ObjectDisposedException.ThrowIf(socket is null, this);
            return socket;
        }

        private Socket SetPipeSocketInterlocked(Socket socket, bool ownsHandle)
        {
            Debug.Assert(socket != null);

            // Multiple threads may try to create the PipeSocket.
            Socket? current = Interlocked.CompareExchange(ref _pipeSocket, socket, null);
            if (current != null)
            {
                socket.Dispose();
                return current;
            }

            // If we own the handle, defer ownership to the SocketHandle.
            SafeSocketHandle socketHandle = _pipeSocket.SafeHandle;
            if (ownsHandle)
            {
                _pipeSocketHandle = socketHandle;

                bool ignored = false;
                socketHandle.DangerousAddRef(ref ignored);
            }

            return socket;
        }

        internal void SetHandle(IntPtr descriptor, bool ownsHandle = true)
        {
            base.SetHandle(descriptor);

            // Avoid throwing when we own the handle by defering pipe creation.
            if (!ownsHandle)
            {
                _pipeSocket = CreatePipeSocket(ownsHandle);
            }
        }
    }
}
