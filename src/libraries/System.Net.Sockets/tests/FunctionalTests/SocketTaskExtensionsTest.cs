// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Sockets.Tests
{
    public class SocketTaskExtensionsTest
    {
        [Fact]
        public async Task EnsureMethodsAreCallable()
        {
            // The purpose of this test is just to ensure that the now-hidden extension methods in SocketTaskExtensions are
            // properly invoking the underlying instance methods, and not accidentally binding to themselves, causing infinite recursion.
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint badEndPoint = new IPEndPoint(IPAddress.None, 0);
            string badHostName = "nosuchhost.invalid";
            byte[] buffer = new byte[1];

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await SocketTaskExtensions.AcceptAsync(s));
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await SocketTaskExtensions.AcceptAsync(s, null));

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await SocketTaskExtensions.ReceiveFromAsync(s, new ArraySegment<byte>(buffer), SocketFlags.None, badEndPoint));
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await SocketTaskExtensions.ReceiveMessageFromAsync(s, new ArraySegment<byte>(buffer), SocketFlags.None, badEndPoint));

            await Assert.ThrowsAsync<SocketException>(async () => await SocketTaskExtensions.ConnectAsync(s, badEndPoint));
            await Assert.ThrowsAsync<SocketException>(async () => await SocketTaskExtensions.ConnectAsync(s, badEndPoint, CancellationToken.None));
            await Assert.ThrowsAsync<SocketException>(async () => await SocketTaskExtensions.ConnectAsync(s, badEndPoint.Address, badEndPoint.Port));
            await Assert.ThrowsAsync<SocketException>(async () => await SocketTaskExtensions.ConnectAsync(s, badEndPoint.Address, badEndPoint.Port, CancellationToken.None));
            await Assert.ThrowsAsync<SocketException>(async () => await SocketTaskExtensions.ConnectAsync(s, new IPAddress[] { badEndPoint.Address, badEndPoint.Address }, badEndPoint.Port));
            await Assert.ThrowsAsync<SocketException>(async () => await SocketTaskExtensions.ConnectAsync(s, new IPAddress[] { badEndPoint.Address, badEndPoint.Address }, badEndPoint.Port, CancellationToken.None));
            await Assert.ThrowsAsync<SocketException>(async () => await SocketTaskExtensions.ConnectAsync(s, badHostName, badEndPoint.Port));
            await Assert.ThrowsAsync<SocketException>(async () => await SocketTaskExtensions.ConnectAsync(s, badHostName, badEndPoint.Port, CancellationToken.None));

            await Assert.ThrowsAsync<SocketException>(async () => await SocketTaskExtensions.ReceiveAsync(s, new ArraySegment<byte>(buffer), SocketFlags.None));
            await Assert.ThrowsAsync<SocketException>(async () => await SocketTaskExtensions.ReceiveAsync(s, buffer.AsMemory(), SocketFlags.None));
            await Assert.ThrowsAsync<SocketException>(async () => await SocketTaskExtensions.ReceiveAsync(s, new ArraySegment<byte>[] { new ArraySegment<byte>(buffer) }, SocketFlags.None));

            await Assert.ThrowsAsync<SocketException>(async () => await SocketTaskExtensions.SendAsync(s, new ArraySegment<byte>(buffer), SocketFlags.None));
            await Assert.ThrowsAsync<SocketException>(async () => await SocketTaskExtensions.SendAsync(s, buffer.AsMemory(), SocketFlags.None));
            await Assert.ThrowsAsync<SocketException>(async () => await SocketTaskExtensions.SendAsync(s, new ArraySegment<byte>[] { new ArraySegment<byte>(buffer) }, SocketFlags.None));
            await Assert.ThrowsAsync<SocketException>(async () => await SocketTaskExtensions.SendToAsync(s, new ArraySegment<byte>(buffer), SocketFlags.None, badEndPoint));
        }
    }
}
