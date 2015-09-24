// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        internal static int FillFdSetFromSocketList(ref Interop.libc.fd_set fdset, IList socketList)
        {
            if (socketList == null || socketList.Count == 0)
            {
                return 0;
            }

            int maxFd = -1;
            for (int i = 0; i < socketList.Count; i++)
            {
                var socket = socketList[i] as Socket;
                if (socket == null)
                {
                    throw new ArgumentException(SR.Format(SR.net_sockets_select, socketList[i].GetType().FullName, typeof(System.Net.Sockets.Socket).FullName), "socketList");
                }

                int fd = socket._handle.FileDescriptor;
                fdset.Set(fd);

                if (fd > maxFd)
                {
                    maxFd = fd;
                }
            }

            return maxFd + 1;
        }

        // Transform the list socketList such that the only sockets left are those
        // with a file descriptor contained in the array "fileDescriptorArray".
        internal static void FilterSocketListUsingFdSet(ref Interop.libc.fd_set fdset, IList socketList)
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
                    if (!fdset.IsSet(socket._handle.FileDescriptor))
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
