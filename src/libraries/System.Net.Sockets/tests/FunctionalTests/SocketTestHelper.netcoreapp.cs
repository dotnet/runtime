// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace System.Net.Sockets.Tests
{
    public class SocketHelperSpanSync : SocketHelperArraySync
    {
        public override Task<int> ReceiveAsync(Socket s, ArraySegment<byte> buffer) =>
            Task.Run(() => s.Receive((Span<byte>)buffer, SocketFlags.None));
        public override Task<int> SendAsync(Socket s, ArraySegment<byte> buffer) =>
            Task.Run(() => s.Send((ReadOnlySpan<byte>)buffer, SocketFlags.None));
    }

    public sealed class SocketHelperSpanSyncForceNonBlocking : SocketHelperSpanSync
    {
        public override Task<Socket> AcceptAsync(Socket s) =>
            Task.Run(() => { s.ForceNonBlocking(true); Socket accepted = s.Accept(); accepted.ForceNonBlocking(true); return accepted; });
        public override Task ConnectAsync(Socket s, EndPoint endPoint) =>
            Task.Run(() => { s.ForceNonBlocking(true); s.Connect(endPoint); });
    }

    public sealed class SocketHelperMemoryArrayTask : SocketHelperTask
    {
        public override Task<int> ReceiveAsync(Socket s, ArraySegment<byte> buffer) =>
            s.ReceiveAsync((Memory<byte>)buffer, SocketFlags.None).AsTask();
        public override Task<int> SendAsync(Socket s, ArraySegment<byte> buffer) =>
            s.SendAsync((ReadOnlyMemory<byte>)buffer, SocketFlags.None).AsTask();
    }
}
