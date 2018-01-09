// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.WebSockets.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public sealed class WebSocketProtocolTests
    {
        [Fact]
        public void CreateFromStream_InvalidArguments_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("stream",
                () => WebSocketProtocol.CreateFromStream(null, true, "subProtocol", TimeSpan.FromSeconds(30), default(Memory<byte>)));
            AssertExtensions.Throws<ArgumentException>("stream",
                () => WebSocketProtocol.CreateFromStream(new MemoryStream(new byte[100], writable: false), true, "subProtocol", TimeSpan.FromSeconds(30), default(Memory<byte>)));
            AssertExtensions.Throws<ArgumentException>("stream",
                () => WebSocketProtocol.CreateFromStream(new UnreadableStream(), true, "subProtocol", TimeSpan.FromSeconds(30), default(Memory<byte>)));

            AssertExtensions.Throws<ArgumentException>("subProtocol",
                () => WebSocketProtocol.CreateFromStream(new MemoryStream(), true, "    ", TimeSpan.FromSeconds(30), default(Memory<byte>)));
            AssertExtensions.Throws<ArgumentException>("subProtocol",
                () => WebSocketProtocol.CreateFromStream(new MemoryStream(), true, "\xFF", TimeSpan.FromSeconds(30), default(Memory<byte>)));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("keepAliveInterval", () =>
                WebSocketProtocol.CreateFromStream(new MemoryStream(), true, "subProtocol", TimeSpan.FromSeconds(-2), default(Memory<byte>)));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(14)]
        [InlineData(4096)]
        public void CreateFromStream_ValidBufferSizes_Succeed(int bufferSize)
        {
            Assert.NotNull(WebSocketProtocol.CreateFromStream(new MemoryStream(), false, null, Timeout.InfiniteTimeSpan, new Memory<byte>(new byte[bufferSize])));
            Assert.NotNull(WebSocketProtocol.CreateFromStream(new MemoryStream(), true, null, Timeout.InfiniteTimeSpan, new Memory<byte>(new byte[bufferSize])));
        }

        [OuterLoop] // Connects to external server.
        [Theory]
        [MemberData(nameof(EchoServers))]
        public async Task WebSocketProtocol_CreateFromConnectedStream_Succeeds(Uri echoUri)
        {
            Uri uri = new UriBuilder(echoUri) { Scheme = (echoUri.Scheme == "ws") ? "http" : "https" }.Uri;
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
            KeyValuePair<string, string> secKeyAndSecWebSocketAccept = CreateSecKeyAndSecWebSocketAccept();
            AddWebSocketHeaders(request, secKeyAndSecWebSocketAccept.Key);
            DirectManagedHttpClientHandler handler = DirectManagedHttpClientHandler.CreateHandler();
            using (HttpResponseMessage response = await handler.SendAsync(request, CancellationToken.None).ConfigureAwait(false))
            {
                Assert.Equal(HttpStatusCode.SwitchingProtocols, response.StatusCode);
                using (Stream connectedStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                {
                    Assert.True(connectedStream.CanRead);
                    Assert.True(connectedStream.CanWrite);
                    using (WebSocket socket = WebSocketProtocol.CreateFromStream(connectedStream, false, null, TimeSpan.FromSeconds(10)))
                    {
                        Assert.NotNull(socket);
                        Assert.Equal(WebSocketState.Open, socket.State);

                        string expected = "Hello World!";
                        ArraySegment<byte> buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(expected));
                        await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);

                        buffer = new ArraySegment<byte>(new byte[buffer.Count]);
                        await socket.ReceiveAsync(buffer, CancellationToken.None);

                        Assert.Equal(expected, Encoding.UTF8.GetString(buffer.Array));
                    }
                }
            }
        }

        public static readonly object[][] EchoServers = System.Net.Test.Common.Configuration.WebSockets.EchoServers;

        /// <summary>GUID appended by the server as part of the security key response.  Defined in the RFC.</summary>
        private const string WSServerGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

        private static KeyValuePair<string, string> CreateSecKeyAndSecWebSocketAccept()
        {
            string secKey = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            using (SHA1 sha = SHA1.Create())
            {
                return new KeyValuePair<string, string>(
                    secKey,
                    Convert.ToBase64String(sha.ComputeHash(Encoding.ASCII.GetBytes(secKey + WSServerGuid))));
            }
        }

        private static void AddWebSocketHeaders(HttpRequestMessage request, string secKey)
        {
            request.Headers.TryAddWithoutValidation(HttpKnownHeaderNames.Connection, HttpKnownHeaderNames.Upgrade);
            request.Headers.TryAddWithoutValidation(HttpKnownHeaderNames.Upgrade, "websocket");
            request.Headers.TryAddWithoutValidation(HttpKnownHeaderNames.SecWebSocketVersion, "13");
            request.Headers.TryAddWithoutValidation(HttpKnownHeaderNames.SecWebSocketKey, secKey);
        }

        private sealed class UnreadableStream : Stream
        {
            public override bool CanRead => false;
            public override bool CanSeek => true;
            public override bool CanWrite => true;
            public override long Length => throw new NotImplementedException();
            public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public override void Flush() => throw new NotImplementedException();
            public override int Read(byte[] buffer, int offset, int count) => throw new NotImplementedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();
            public override void SetLength(long value) => throw new NotImplementedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();
        }

        private sealed class DirectManagedHttpClientHandler : HttpClientHandler
        {
            private const string ManagedHandlerEnvVar = "COMPlus_UseManagedHttpClientHandler";
            private static readonly LocalDataStoreSlot s_managedHandlerSlot = GetSlot();
            private static readonly object s_true = true;

            private static LocalDataStoreSlot GetSlot()
            {
                LocalDataStoreSlot slot = Thread.GetNamedDataSlot(ManagedHandlerEnvVar);
                if (slot != null)
                {
                    return slot;
                }

                try
                {
                    return Thread.AllocateNamedDataSlot(ManagedHandlerEnvVar);
                }
                catch (ArgumentException) // in case of a race condition where multiple threads all try to allocate the slot concurrently
                {
                    return Thread.GetNamedDataSlot(ManagedHandlerEnvVar);
                }
            }

            public static DirectManagedHttpClientHandler CreateHandler()
            {
                Thread.SetData(s_managedHandlerSlot, s_true);
                try
                {
                    return new DirectManagedHttpClientHandler();
                }
                finally { Thread.SetData(s_managedHandlerSlot, null); }
            }

            public new Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request, CancellationToken cancellationToken) =>
                base.SendAsync(request, cancellationToken);
        }
    }
}
