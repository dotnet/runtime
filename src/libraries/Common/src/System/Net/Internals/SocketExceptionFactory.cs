// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;

namespace System.Net.Internals
{
    internal static partial class SocketExceptionFactory
    {
        private sealed class ExtendedSocketException : SocketException
        {
            private readonly EndPoint? _endPoint;

            public ExtendedSocketException(int errorCode, EndPoint? endPoint)
                : base(errorCode)
            {
                _endPoint = endPoint;
            }

            public ExtendedSocketException(SocketError socketError, int platformError)
                : base((int)socketError)
            {
                HResult = platformError;
            }

            public override string Message =>
                (_endPoint == null) ? base.Message : base.Message + " " + _endPoint.ToString();
        }

        public static SocketException CreateSocketException(int socketError, EndPoint? endPoint)
        {
            return new ExtendedSocketException(socketError, endPoint);
        }
    }
}
