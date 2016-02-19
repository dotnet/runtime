// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;

namespace System.Net.Sockets
{
    public partial class Socket
    {
        internal static unsafe int FillFdSetFromSocketList(uint* fdset, IList socketList)
        {
            if (socketList == null || socketList.Count == 0)
            {
                return 0;
            }

            Interop.Sys.FD_ZERO(fdset);

            int maxFd = -1;
            for (int i = 0; i < socketList.Count; i++)
            {
                var socket = socketList[i] as Socket;
                if (socket == null)
                {
                    throw new ArgumentException(SR.Format(SR.net_sockets_select, socketList[i].GetType().FullName, typeof(System.Net.Sockets.Socket).FullName), nameof(socketList));
                }

                int fd = socket._handle.FileDescriptor;
                Interop.Sys.FD_SET(fd, fdset);

                if (fd > maxFd)
                {
                    maxFd = fd;
                }
            }

            return maxFd + 1;
        }

        // Transform the list socketList such that the only sockets left are those
        // with a file descriptor contained in the array "fileDescriptorArray".
        internal static unsafe void FilterSocketListUsingFdSet(uint* fdset, IList socketList)
        {
            if (socketList == null || socketList.Count == 0)
            {
                return;
            }

            lock (socketList)
            {
                for (int i = socketList.Count - 1; i >= 0; i--)
                {
                    var socket = (Socket)socketList[i];
                    if (!Interop.Sys.FD_ISSET(socket._handle.FileDescriptor, fdset))
                    {
                        socketList.RemoveAt(i);
                    }
                }
            }
        }

        private Socket GetOrCreateAcceptSocket(Socket acceptSocket, bool unused, string propertyName, out SafeCloseSocket handle)
        {
            // AcceptSocket is not supported on Unix.
            if (acceptSocket != null)
            {
                throw new PlatformNotSupportedException();
            }

            handle = null;
            return null;
        }
    }
}
