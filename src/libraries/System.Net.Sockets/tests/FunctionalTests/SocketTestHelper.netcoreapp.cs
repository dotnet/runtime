// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Threading.Tasks;

namespace System.Net.Sockets.Tests
{
    public class SocketHelperSpanSync : SocketHelperArraySync
    {
        public override bool ValidatesArrayArguments => false;
        public override Task<int> ReceiveAsync(Socket s, ArraySegment<byte> buffer) =>
            Task.Run(() => s.Receive((Span<byte>)buffer, SocketFlags.None));
        public override Task<int> SendAsync(Socket s, ArraySegment<byte> buffer) =>
            Task.Run(() => s.Send((ReadOnlySpan<byte>)buffer, SocketFlags.None));
    }

    public sealed class SocketHelperSpanSyncForceNonBlocking : SocketHelperSpanSync
    {
        public override bool ValidatesArrayArguments => false;
        public override Task<Socket> AcceptAsync(Socket s) =>
            Task.Run(() => { s.ForceNonBlocking(true); Socket accepted = s.Accept(); accepted.ForceNonBlocking(true); return accepted; });
        public override Task ConnectAsync(Socket s, EndPoint endPoint) =>
            Task.Run(() => { s.ForceNonBlocking(true); s.Connect(endPoint); });
    }

    public sealed class SocketHelperMemoryArrayTask : SocketHelperTask
    {
        public override bool ValidatesArrayArguments => false;
        public override Task<int> ReceiveAsync(Socket s, ArraySegment<byte> buffer) =>
            s.ReceiveAsync((Memory<byte>)buffer, SocketFlags.None).AsTask();
        public override Task<int> SendAsync(Socket s, ArraySegment<byte> buffer) =>
            s.SendAsync((ReadOnlyMemory<byte>)buffer, SocketFlags.None).AsTask();
    }

    public sealed class SocketHelperMemoryNativeTask : SocketHelperTask
    {
        public override bool ValidatesArrayArguments => false;
        public override async Task<int> ReceiveAsync(Socket s, ArraySegment<byte> buffer)
        {
            using (var m = new NativeOwnedMemory(buffer.Count))
            {
                int bytesReceived = await s.ReceiveAsync(m.Memory, SocketFlags.None).ConfigureAwait(false);
                m.Memory.Span.Slice(0, bytesReceived).CopyTo(buffer.AsSpan());
                return bytesReceived;
            }
        }
        public override async Task<int> SendAsync(Socket s, ArraySegment<byte> buffer)
        {
            using (var m = new NativeOwnedMemory(buffer.Count))
            {
                buffer.AsSpan().CopyTo(m.Memory.Span);
                return await s.SendAsync(m.Memory, SocketFlags.None).ConfigureAwait(false);
            }
        }
    }
}
