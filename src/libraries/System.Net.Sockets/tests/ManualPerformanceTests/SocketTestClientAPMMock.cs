// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

using Xunit.Abstractions;

namespace System.Net.Sockets.Performance.Tests
{
    public class SocketTestClientAPM : SocketTestClient
    {
        public SocketTestClientAPM(
            ITestOutputHelper log,
            EndPoint endpoint,
            int iterations,
            string message,
            Stopwatch timeProgramStart) : base(log, endpoint, iterations, message, timeProgramStart)
        {
        }

        public override void Close(Action onCloseCallback)
        {
            throw new NotSupportedException();
        }

        public override void Connect(Action<SocketError> onConnectCallback)
        {
            throw new NotSupportedException();
        }

        public override bool Receive(out int bytesReceived, out SocketError socketError, Action<int, SocketError> onReceiveCallback)
        {
            throw new NotSupportedException();
        }

        public override bool Send(out int bytesSent, out SocketError socketError, Action<int, SocketError> onSendCallback)
        {
            throw new NotSupportedException();
        }

        protected override string ImplementationName()
        {
            throw new NotSupportedException();
        }
    }
}
