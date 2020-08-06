// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;

namespace System.Net
{
    internal static class NetworkErrorHelper
    {
        internal static NetworkException MapSocketException(SocketException socketException)
        {
            NetworkError error = socketException.SocketErrorCode switch
            {
                SocketError.AddressAlreadyInUse => NetworkError.EndPointInUse,
                SocketError.HostNotFound => NetworkError.HostNotFound,
                SocketError.ConnectionRefused => NetworkError.ConnectionRefused,
                SocketError.OperationAborted => NetworkError.OperationAborted,
                SocketError.ConnectionAborted => NetworkError.ConnectionAborted,
                SocketError.ConnectionReset => NetworkError.ConnectionReset,
                _ => NetworkError.Unknown
            };

            return new NetworkException(error, socketException);
        }
    }
}
