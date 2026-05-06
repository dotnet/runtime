// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Sockets
{
    internal static partial class SocketExceptionFactory
    {
        public static SocketException CreateSocketException(int socketError, EndPoint endPoint)
        {
            // Windows directly maps socketError to native error code.
            return new SocketException(socketError, CreateMessage(socketError, endPoint));
        }
    }
}
