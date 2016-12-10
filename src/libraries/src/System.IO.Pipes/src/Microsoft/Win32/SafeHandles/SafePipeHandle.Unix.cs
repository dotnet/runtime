// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafePipeHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private const int DefaultInvalidHandle = -1;

        // For anonymous pipes, SafePipeHandle.handle is the file descriptor of the pipe, and the 
        // _named* fields remain null. For named pipes, SafePipeHandle.handle is a copy of the file descriptor 
        // extracted from the Socket's SafeHandle, and the _named* fields are the socket and its safe handle.
        // This allows operations related to file descriptors to be performed directly on the SafePipeHandle,
        // and operations that should go through the Socket to be done via _namedPipeSocket.  We keep the
        // Socket's SafeHandle alive as long as this SafeHandle is alive.

        private Socket _namedPipeSocket;
        private SafeHandle _namedPipeSocketHandle;
        private static PropertyInfo s_safeHandleProperty;

        internal SafePipeHandle(Socket namedPipeSocket) : base(ownsHandle: true)
        {
            Debug.Assert(namedPipeSocket != null);
            _namedPipeSocket = namedPipeSocket;

            // TODO: Issue https://github.com/dotnet/corefx/issues/6807
            // This is unfortunately the only way of getting at the Socket's file descriptor right now, until #6807 is implemented.
            PropertyInfo safeHandleProperty = s_safeHandleProperty ?? (s_safeHandleProperty = typeof(Socket).GetTypeInfo().GetDeclaredProperty("SafeHandle"));
            Debug.Assert(safeHandleProperty != null, "Socket.SafeHandle could not be found.");
            _namedPipeSocketHandle = (SafeHandle)safeHandleProperty?.GetValue(namedPipeSocket, null);

            bool ignored = false;
            _namedPipeSocketHandle.DangerousAddRef(ref ignored);
            SetHandle(_namedPipeSocketHandle.DangerousGetHandle());
        }

        internal Socket NamedPipeSocket => _namedPipeSocket;
        internal SafeHandle NamedPipeSocketHandle => _namedPipeSocketHandle;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing); // must be called before trying to Dispose the socket
            if (disposing && _namedPipeSocket != null)
            {
                _namedPipeSocket.Dispose();
                _namedPipeSocket = null;
            }
        }

        protected override bool ReleaseHandle()
        {
            Debug.Assert(!IsInvalid);

            // Clean up resources for named handles
            if (_namedPipeSocketHandle != null)
            {
                SetHandle(DefaultInvalidHandle);
                _namedPipeSocketHandle.DangerousRelease();
                _namedPipeSocketHandle = null;
                return true;
            }

            // Clean up resources for anonymous handles
            return (long)handle >= 0 ?
                Interop.Sys.Close(handle) == 0 :
                true;
        }

        public override bool IsInvalid
        {
            get { return (long)handle < 0 && _namedPipeSocket == null; }
        }
    }
}
