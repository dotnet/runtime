// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Test.Common;
using System.Threading;

using Xunit;

namespace System.Net.Sockets.Tests
{
    public class ReceiveMessageFromAsync
    {
        public void OnCompleted(object sender, SocketAsyncEventArgs args)
        {
            EventWaitHandle handle = (EventWaitHandle)args.UserToken;
            handle.Set();
        }

        [Fact]
        public void Success()
        {
            ManualResetEvent completed = new ManualResetEvent(false);

            if (Socket.OSSupportsIPv4)
            {
                using (Socket receiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    int port = receiver.BindToAnonymousPort(IPAddress.Loopback);
                    receiver.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);

                    Socket sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    sender.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                    sender.SendTo(new byte[1024], new IPEndPoint(IPAddress.Loopback, port));

                    SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                    args.RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    args.SetBuffer(new byte[1024], 0, 1024);
                    args.Completed += OnCompleted;
                    args.UserToken = completed;

                    Assert.True(receiver.ReceiveMessageFromAsync(args));

                    Assert.True(completed.WaitOne(Configuration.PassingTestTimeout), "Timeout while waiting for connection");

                    Assert.Equal(1024, args.BytesTransferred);
                    Assert.Equal(sender.LocalEndPoint, args.RemoteEndPoint);
                    Assert.Equal(((IPEndPoint)sender.LocalEndPoint).Address, args.ReceiveMessageFromPacketInfo.Address);

                    sender.Dispose();
                }
            }
        }
    }
}
