// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Sockets;

namespace System.Net.Internals
{
    internal class SocketExceptionFactory
    {
        public static SocketException CreateSocketException(int socketError, EndPoint endPoint)
        {
            // TODO: expose SocketException(int, EndPoint) to maintain exception Message compatibility.
            return new SocketException(socketError);
        }
    }
}
