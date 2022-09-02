// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Sockets
{
    internal static partial class SocketExceptionFactory
    {
        public static SocketException CreateSocketException(int socketError, EndPoint endPoint)
        {
            int nativeErr = (int)socketError;

            // If an interop error was not found, then don't invoke Info().RawErrno as that will fail with assert.
            if (SocketErrorPal.TryGetNativeErrorForSocketError((SocketError)socketError, out Interop.Error interopErr))
            {
                nativeErr = interopErr.Info().RawErrno;
            }

            return new SocketException(socketError, CreateMessage(nativeErr, endPoint));
        }
    }
}
