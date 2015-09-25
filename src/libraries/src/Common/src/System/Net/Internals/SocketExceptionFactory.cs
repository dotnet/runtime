// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
