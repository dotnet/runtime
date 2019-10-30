// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace System.Data.SqlClient.SNI
{
    internal partial class SNITcpHandle
    {
        internal static void SetKeepAliveValues(ref Socket socket)
        {
#if FEATURE_TCPKEEPALIVE
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, 1);
            socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, 30);
#endif
        }
    }
}
