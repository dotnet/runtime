// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Sockets;

namespace System.Net.Internals
{
    internal static partial class SocketExceptionFactory
    {
        public static SocketException CreateSocketException(SocketError errorCode, int platformError)
        {
            return new ExtendedSocketException(errorCode, platformError);
        }
    }
}
