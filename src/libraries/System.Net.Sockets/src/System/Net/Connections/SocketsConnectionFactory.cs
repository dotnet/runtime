// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.IO.Pipelines;
using System.Net.Connections;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    /// <summary>
    /// TODO
    /// </summary>
    public class SocketsConnectionFactory : ConnectionFactory
    {
        // dual-mode IPv6 socket. See Socket(SocketType socketType, ProtocolType protocolType)
        public SocketsConnectionFactory(SocketType socketType, ProtocolType protocolType)
        {
        }

        // See Socket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
        public SocketsConnectionFactory(
            AddressFamily addressFamily,
            SocketType socketType,
            ProtocolType protocolType)
        {
        }

        // This must be thread-safe!
        public override ValueTask<Connection> ConnectAsync(
            EndPoint? endPoint,
            IConnectionProperties? options = null,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        // These exist to provide an easy way to shim the default behavior.
        // Note: Connect must call this to create its socket.
        protected virtual Socket CreateSocket(
            AddressFamily addressFamily,
            SocketType socketType,
            ProtocolType protocolType,
            EndPoint? endPoint,
            IConnectionProperties? options)
        {
            throw new NotImplementedException();
        }

        protected virtual Stream CreateStream(Socket socket, IConnectionProperties? options)
        {
            throw new NotImplementedException();
        }

        protected virtual IDuplexPipe CreatePipe(Socket socket, IConnectionProperties? options)
        {
            throw new NotImplementedException();
        }
    }
}
