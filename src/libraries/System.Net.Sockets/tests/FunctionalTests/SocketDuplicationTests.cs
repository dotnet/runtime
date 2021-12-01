// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Pipes;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Sockets.Tests
{
    // Test cases for DuplicateAndClose, Socket(socketInformation), Socket.UseOnlyOverlappedIO,
    // and asynchronous IO behavior for duplicate sockets.
    // Since the constructor Socket(socketInformation) is strongly coupled
    // with the rest of the duplication logic, it's being tested here instead of CreateSocketTests.
    public class SocketDuplicationTests
    {
        private readonly ArraySegment<byte> _receiveBuffer = new ArraySegment<byte>(new byte[32]);
        private const string TestMessage = "test123!";
        private static ArraySegment<byte> TestBytes => Encoding.ASCII.GetBytes(TestMessage);
        private static string GetMessageString(ArraySegment<byte> data, int count) =>
            Encoding.ASCII.GetString(data.AsSpan(0, count));

        [Fact]
        public void UseOnlyOverlappedIO_AlwaysFalse()
        {
            using Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

#pragma warning disable 0618
            Assert.False(s.UseOnlyOverlappedIO);
            s.UseOnlyOverlappedIO = true;
            Assert.False(s.UseOnlyOverlappedIO);
#pragma warning restore 0618
        }

        [PlatformSpecific(TestPlatforms.Windows)]
        [Fact]
        public void DuplicateAndClose_TargetProcessDoesNotExist_Throws_SocketException()
        {
            using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            SocketException ex = Assert.Throws<SocketException>(() => socket.DuplicateAndClose(-1));
            Assert.Equal(SocketError.InvalidArgument, ex.SocketErrorCode);
        }

        [PlatformSpecific(TestPlatforms.Windows)]
        [Fact]
        public void DuplicateAndClose_WhenDisposed_Throws()
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Dispose();

            Assert.Throws<ObjectDisposedException>(() => socket.DuplicateAndClose(Environment.ProcessId));
        }

        [PlatformSpecific(TestPlatforms.Windows)]
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void BlockingState_IsTransferred(bool blocking)
        {
            using Socket original = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                Blocking = blocking
            };
            Assert.Equal(blocking, original.Blocking);

            SocketInformation info = original.DuplicateAndClose(Environment.ProcessId);

            using Socket clone = new Socket(info);
            Assert.Equal(blocking, clone.Blocking);
        }

        [Theory]
        [InlineData(null)] // ProtocolInformation == null
        [InlineData(1)] // ProtocolInformation too short
        [InlineData(1000)] // corrupt ProtocolInformation
        [PlatformSpecific(TestPlatforms.Windows)]
        public void SocketCtr_InvalidProtocolInformation_ThrowsArgumentException(int? protocolInfoLength)
        {
            SocketInformation invalidInfo = new SocketInformation();
            if (protocolInfoLength != null)
            {
                invalidInfo.ProtocolInformation = new byte[protocolInfoLength.Value];
            }

            Assert.Throws<ArgumentException>(() => new Socket(invalidInfo));
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [Fact]
        public void SocketCtr_SocketInformation_Unix_ThrowsPlatformNotSupportedException()
        {
            SocketInformation socketInformation = default;
            Assert.Throws<PlatformNotSupportedException>(() => new Socket(socketInformation));
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [Fact]
        public void DuplicateAndClose_Unix_ThrowsPlatformNotSupportedException()
        {
            using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            int processId = Environment.ProcessId;

            Assert.Throws<PlatformNotSupportedException>(() => socket.DuplicateAndClose(processId));
        }

        [PlatformSpecific(TestPlatforms.Windows)]
        [Fact]
        public async Task DuplicateAndClose_TcpClient()
        {
            using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(1);

            using Socket client0 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            using Socket client1 = new Socket(client0.DuplicateAndClose(Environment.ProcessId));
            Assert.False(client1.Connected);
            client1.Connect(listener.LocalEndPoint);

            using Socket client2 = new Socket(client1.DuplicateAndClose(Environment.ProcessId));
            Assert.True(client2.Connected);

            using Socket handler = await listener.AcceptAsync();
            await client2.SendAsync(TestBytes, SocketFlags.None);

            int rcvCount = await handler.ReceiveAsync(_receiveBuffer, SocketFlags.None);

            string receivedMessage = GetMessageString(_receiveBuffer, rcvCount);
            Assert.Equal(TestMessage, receivedMessage);
        }

        [PlatformSpecific(TestPlatforms.Windows)]
        [Fact]
        public async Task DuplicateAndClose_TcpListener()
        {
            using Socket listener0 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener0.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener0.Listen(1);

            using Socket listener1 = new Socket(listener0.DuplicateAndClose(Environment.ProcessId));

            using Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await client.ConnectAsync(listener1.LocalEndPoint);

            using Socket handler = await listener1.AcceptAsync();
            await client.SendAsync(TestBytes, SocketFlags.None);

            byte[] receivedBuffer = new byte[32];
            int rcvCount = await handler.ReceiveAsync(new ArraySegment<byte>(receivedBuffer), SocketFlags.None);

            string receivedMessage = GetMessageString(receivedBuffer, rcvCount);
            Assert.Equal(TestMessage, receivedMessage);
        }

        [PlatformSpecific(TestPlatforms.Windows)]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void DuplicateSocket_IsNotInheritable()
        {
            // 300 ms should be long enough to connect if the socket is actually present & listening.
            const int ConnectionTimeoutMs = 300;

            // The test is based on CreateSocket.CtorAndAccept_SocketNotKeptAliveViaInheritance,
            // but contains simpler validation logic, sufficient to test the behavior on Windows
            static void RunTest()
            {
                using Socket listenerProto = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listenerProto.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listenerProto.Listen(1);
                EndPoint ep = listenerProto.LocalEndPoint;

                using Socket listenerDuplicate = new Socket(listenerProto.DuplicateAndClose(Environment.ProcessId));

                using var serverPipe = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);

                static void ChildProcessBody(string clientPipeHandle)
                {
                    using var clientPipe = new AnonymousPipeClientStream(PipeDirection.In, clientPipeHandle);
                    Assert.Equal(42, clientPipe.ReadByte());
                }

                // Create a child process that blocks waiting to receive a signal on the anonymous pipe.
                // The whole purpose of the child is to test whether handles are inherited, so we
                // keep the child process alive until we're done validating that handles close as expected.
                using (RemoteExecutor.Invoke(ChildProcessBody, serverPipe.GetClientHandleAsString()))
                {
                    using Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                    // Close the listening socket:
                    listenerDuplicate.Dispose();

                    // Validate that we after closing the listening socket, we're not able to connect:
                    bool connected = client.TryConnect(ep, ConnectionTimeoutMs);
                    serverPipe.WriteByte(42);
                    Assert.False(connected);
                }
            }

            // Run the test in another process so as to not have trouble with other tests
            // launching child processes that might impact inheritance.
            RemoteExecutor.Invoke(RunTest).Dispose();
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public async Task DoAsyncOperation_OnBothOriginalAndClone_ThrowsInvalidOperationException()
        {
            // Not applicable for synchronous operations:
            (Socket client, Socket originalServer) = SocketTestExtensions.CreateConnectedSocketPair();

            using (client)
            using (originalServer)
            {
                client.Send(TestBytes);

                await originalServer.ReceiveAsync(_receiveBuffer, SocketFlags.None);

                SocketInformation info = originalServer.DuplicateAndClose(Environment.ProcessId);

                using Socket cloneServer = new Socket(info);
                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    cloneServer.ReceiveAsync(_receiveBuffer, SocketFlags.None));
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)]
        [Fact]
        public void SocketCtr_SocketInformation_NonIpSocket_ThrowsNotSupportedException()
        {
            if (!Socket.OSSupportsUnixDomainSockets) return;

            using Socket original = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            SocketInformation info = original.DuplicateAndClose(Environment.ProcessId);
            Assert.ThrowsAny<NotSupportedException>(() => _ = new Socket(info));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void SocketCtr_SocketInformation_WhenProtocolInformationIsNull_Throws()
        {
            SocketInformation socketInformation = default;

            ArgumentException ex = Assert.Throws<ArgumentException>(() => new Socket(socketInformation));
            Assert.Equal("socketInformation", ex.ParamName);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void SocketCtr_SocketInformation_WhenProtocolInformationTooShort_Throws()
        {
            SocketInformation socketInformation = new SocketInformation() {ProtocolInformation = new byte[4]};

            ArgumentException ex = Assert.Throws<ArgumentException>(() => new Socket(socketInformation));
            Assert.Equal("socketInformation", ex.ParamName);
        }

        // A smaller subset of the tests is being executed against the different Send/Receive implementations of Socket
        // to make sure async IO works as expected in all of those cases.
        public abstract class PolymorphicTests<T> where T : SocketHelperBase, new()
        {
            private static readonly T Helper = new T();
            private readonly string _ipcPipeName = Path.GetRandomFileName();

            private static void WriteSocketInfo(Stream stream, SocketInformation socketInfo)
            {
                BinaryWriter bw = new BinaryWriter(stream);
                bw.Write((int)socketInfo.Options);
                bw.Write(socketInfo.ProtocolInformation.Length);
                bw.Write(socketInfo.ProtocolInformation);
            }

            private static SocketInformation ReadSocketInfo(Stream stream)
            {
                BinaryReader br = new BinaryReader(stream);
                SocketInformationOptions options = (SocketInformationOptions)br.ReadInt32();
                int protocolInfoLength = br.ReadInt32();
                SocketInformation result = new SocketInformation()
                {
                    Options = options, ProtocolInformation = new byte[protocolInfoLength]
                };
                br.Read(result.ProtocolInformation);
                return result;
            }

            public static readonly TheoryData<AddressFamily, bool> TcpServerHandlerData =
                new TheoryData<AddressFamily, bool>()
                {
                    { AddressFamily.InterNetwork, false },
                    { AddressFamily.InterNetwork, true },
                    { AddressFamily.InterNetworkV6, false },
                    { AddressFamily.InterNetworkV6, true },
                };

            [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
            [PlatformSpecific(TestPlatforms.Windows)]
            [MemberData(nameof(TcpServerHandlerData))]
            public async Task DuplicateAndClose_TcpServerHandler(AddressFamily addressFamily, bool sameProcess)
            {
                IPAddress address = addressFamily == AddressFamily.InterNetwork
                    ? IPAddress.Loopback
                    : IPAddress.IPv6Loopback;

                using Socket listener = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp);
                using Socket client = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp);

                listener.BindToAnonymousPort(address);
                listener.Listen(1);

                client.Connect(listener.LocalEndPoint);

                // Async is allowed on the listener:
                using Socket handlerOriginal = await listener.AcceptAsync();

                // pipe used to exchange socket info
                await using NamedPipeServerStream pipeServerStream =
                    new NamedPipeServerStream(_ipcPipeName, PipeDirection.Out);

                if (sameProcess)
                {
                    Task handlerCode = Task.Run(() => HandlerServerCode(_ipcPipeName));
                    RunCommonHostLogic(Environment.ProcessId);
                    await handlerCode;
                }
                else
                {
                    using RemoteInvokeHandle hServerProc = RemoteExecutor.Invoke(HandlerServerCode, _ipcPipeName);
                    RunCommonHostLogic(hServerProc.Process.Id);
                }

                void RunCommonHostLogic(int processId)
                {
                    pipeServerStream.WaitForConnection();

                    // Duplicate the socket:
                    SocketInformation socketInfo = handlerOriginal.DuplicateAndClose(processId);
                    WriteSocketInfo(pipeServerStream, socketInfo);

                    // Send client data:
                    client.Send(TestBytes);
                }

                static async Task<int> HandlerServerCode(string ipcPipeName)
                {
                    await using NamedPipeClientStream pipeClientStream =
                        new NamedPipeClientStream(".", ipcPipeName, PipeDirection.In);
                    pipeClientStream.Connect();

                    SocketInformation socketInfo = ReadSocketInfo(pipeClientStream);
                    using Socket handler = new Socket(socketInfo);

                    Assert.True(handler.IsBound);
                    Assert.NotNull(handler.RemoteEndPoint);
                    Assert.NotNull(handler.LocalEndPoint);

                    byte[] data = new byte[32];

                    int rcvCount = await Helper.ReceiveAsync(handler, new ArraySegment<byte>(data));
                    string actual = GetMessageString(data, rcvCount);

                    Assert.Equal(TestMessage, actual);

                    return RemoteExecutor.SuccessExitCode;
                }
            }
        }

        public class Synchronous : PolymorphicTests<SocketHelperArraySync>
        {
        }

        public class Apm : PolymorphicTests<SocketHelperApm>
        {
        }

        public class TaskBased : PolymorphicTests<SocketHelperTask>
        {
        }

        public class Eap : PolymorphicTests<SocketHelperEap>
        {
        }
    }
}
