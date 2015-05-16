// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO.Pipes;
using System.Threading.Tasks;
using Xunit;

using Microsoft.Win32.SafeHandles;
using System.Threading;

namespace System.IO.Pipes.Tests
{
    public class NamedPipesThrowsTests
    {
        // Server Parameter Checking throws
        [Fact]
        public static void ServerNullPipeNameThrows()
        {
            Assert.Throws<ArgumentNullException>(() => new NamedPipeServerStream(null));
            Assert.Throws<ArgumentNullException>(() => new NamedPipeServerStream(null, PipeDirection.In));
            Assert.Throws<ArgumentNullException>(() => new NamedPipeServerStream(null, PipeDirection.In, 2));
            Assert.Throws<ArgumentNullException>(() => new NamedPipeServerStream(null, PipeDirection.In, 3, PipeTransmissionMode.Byte));
            Assert.Throws<ArgumentNullException>(() => new NamedPipeServerStream(null, PipeDirection.In, 3, PipeTransmissionMode.Byte, PipeOptions.None));
            Assert.Throws<ArgumentNullException>(() => new NamedPipeServerStream(null, PipeDirection.In, 3, PipeTransmissionMode.Byte, PipeOptions.None, 0, 0));
        }

        [Fact]
        public static void ServerZeroLengthPipeNameThrows()
        {
            Assert.Throws<ArgumentException>(() => new NamedPipeServerStream(""));
            Assert.Throws<ArgumentException>(() => new NamedPipeServerStream("", PipeDirection.In));
            Assert.Throws<ArgumentException>(() => new NamedPipeServerStream("", PipeDirection.In, 2));
            Assert.Throws<ArgumentException>(() => new NamedPipeServerStream("", PipeDirection.In, 3, PipeTransmissionMode.Byte));
            Assert.Throws<ArgumentException>(() => new NamedPipeServerStream("", PipeDirection.In, 3, PipeTransmissionMode.Byte, PipeOptions.None));
            Assert.Throws<ArgumentException>(() => new NamedPipeServerStream("", PipeDirection.In, 3, PipeTransmissionMode.Byte, PipeOptions.None, 0, 0));
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)]
        public static void ServerReservedPipeNameThrows()
        {
            const string reservedName = "anonymous";
            Assert.Throws<ArgumentOutOfRangeException>(() => new NamedPipeServerStream(reservedName));
            Assert.Throws<ArgumentOutOfRangeException>(() => new NamedPipeServerStream(reservedName, PipeDirection.In));
            Assert.Throws<ArgumentOutOfRangeException>(() => new NamedPipeServerStream(reservedName, PipeDirection.In, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new NamedPipeServerStream(reservedName, PipeDirection.In, 1, PipeTransmissionMode.Byte));
            Assert.Throws<ArgumentOutOfRangeException>(() => new NamedPipeServerStream(reservedName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.None));
            Assert.Throws<ArgumentOutOfRangeException>(() => new NamedPipeServerStream(reservedName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.None, 0, 0));
        }

        [Fact]
        public static void ServerPipeDirectionThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new NamedPipeServerStream("temp1", (PipeDirection)123));
            Assert.Throws<ArgumentOutOfRangeException>(() => new NamedPipeServerStream("temp1", (PipeDirection)123, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new NamedPipeServerStream("temp1", (PipeDirection)123, 1, PipeTransmissionMode.Byte));
            Assert.Throws<ArgumentOutOfRangeException>(() => new NamedPipeServerStream("temp1", (PipeDirection)123, 1, PipeTransmissionMode.Byte, PipeOptions.None));
            Assert.Throws<ArgumentOutOfRangeException>(() => new NamedPipeServerStream("tempx", (PipeDirection)123, 1, PipeTransmissionMode.Byte, PipeOptions.None, 0, 0));
        }

        [Fact]
        public static void ServerServerInstancesThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new NamedPipeServerStream("temp3", PipeDirection.Out, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new NamedPipeServerStream("temp3", PipeDirection.Out, 0, PipeTransmissionMode.Byte));
            Assert.Throws<ArgumentOutOfRangeException>(() => new NamedPipeServerStream("temp3", PipeDirection.Out, 0, PipeTransmissionMode.Byte, PipeOptions.None));
            Assert.Throws<ArgumentOutOfRangeException>(() => new NamedPipeServerStream("temp3", PipeDirection.Out, 0, PipeTransmissionMode.Byte, PipeOptions.None, 0, 0));
        }

        [Fact]
        public static void ServerTransmissionModeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new NamedPipeServerStream("temp1", PipeDirection.Out, 1, (PipeTransmissionMode)123));
            Assert.Throws<ArgumentOutOfRangeException>(() => new NamedPipeServerStream("temp1", PipeDirection.Out, 1, (PipeTransmissionMode)123, PipeOptions.None));
            Assert.Throws<ArgumentOutOfRangeException>(() => new NamedPipeServerStream("tempx", PipeDirection.Out, 1, (PipeTransmissionMode)123, PipeOptions.None, 0, 0));
        }

        [Fact]
        public static void ServerPipeOptionsThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new NamedPipeServerStream("temp1", PipeDirection.Out, 1, PipeTransmissionMode.Byte, (PipeOptions)255));
            Assert.Throws<ArgumentOutOfRangeException>(() => new NamedPipeServerStream("tempx", PipeDirection.Out, 1, PipeTransmissionMode.Byte, (PipeOptions)255, 0, 0));
        }

        [Fact]
        public static void ServerBufferSizeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new NamedPipeServerStream("temp2", PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.None, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new NamedPipeServerStream("temp2", PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.None, 0, -123));
        }

        [Fact]
        public static void ServerNullPipeHandleThrows()
        {
            Assert.Throws<ArgumentNullException>(() => new NamedPipeServerStream(PipeDirection.InOut, false, true, null));
        }

        [Fact]
        [ActiveIssue(1764, PlatformID.AnyUnix)]
        public static void ServerNotConnectedThrows()
        {
            using (NamedPipeServerStream server = new NamedPipeServerStream("tempnotconnected", PipeDirection.InOut, 1))
            {
                // doesn't throw execeptions
                PipeTransmissionMode transmitMode = server.TransmissionMode;
                Assert.Throws<ArgumentOutOfRangeException>(() => server.ReadMode = (PipeTransmissionMode)999);

                if (Interop.IsWindows)
                {
                    int inbuffersize = server.InBufferSize;
                    int outbuffersize = server.OutBufferSize;
                }
                else
                {
                    Assert.Throws<PlatformNotSupportedException>(() => server.InBufferSize);
                    Assert.Throws<PlatformNotSupportedException>(() => server.OutBufferSize);
                }
                PipeTransmissionMode readMode = server.ReadMode;
                server.ReadMode = PipeTransmissionMode.Byte;

                Assert.Throws<InvalidOperationException>(() => server.WriteByte(5));
                Assert.Throws<InvalidOperationException>(() => server.ReadByte());
                Assert.Throws<InvalidOperationException>(() => server.Disconnect());    // disconnect when not connected 
                Assert.Throws<InvalidOperationException>(() => server.IsMessageComplete);
            }
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)]
        public static void ServerVerifyMultipleServerOverflow()
        {
            const string uniqueServerName = "UniqueName00000";
            using (NamedPipeServerStream server = new NamedPipeServerStream(uniqueServerName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            {
                Assert.Throws<IOException>(() => new NamedPipeServerStream(uniqueServerName));
            }
        }

        [Fact]
        [ActiveIssue(1763, PlatformID.AnyUnix)]
        public static void ServerVerifyMultipleServerInconsistantType()
        {
            const string uniqueServerName = "UniqueName11111";
            using (NamedPipeServerStream server = new NamedPipeServerStream(uniqueServerName, PipeDirection.In, 2, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            {
                //Assert.Throws<IOException>(() => new NamedPipeServerStream(uniqueServerName, PipeDirection.In, 2, PipeTransmissionMode.Byte, PipeOptions.Asynchronous));
                Assert.Throws<UnauthorizedAccessException>(() => new NamedPipeServerStream(uniqueServerName, PipeDirection.Out));
                //Assert.Throws<UnauthorizedAccessException>(() => new NamedPipeServerStream(uniqueServerName, PipeDirection.In, 4, PipeTransmissionMode.Message));
            }
        }

        [Fact]
        [ActiveIssue(1772, PlatformID.AnyUnix)]
        public static void ServerWhenDisplosedThrows()
        {
            using (NamedPipeServerStream server = new NamedPipeServerStream("unique2", PipeDirection.InOut))
            {
                server.Dispose();

                Assert.Throws<ObjectDisposedException>(() => server.Disconnect());  // disconnect when disposed
                Assert.Throws<ObjectDisposedException>(() => server.WaitForConnection());  // disconnect when disposed
                Assert.Throws<ObjectDisposedException>(() => server.WriteByte(5));
                Assert.Throws<ObjectDisposedException>(() => server.ReadByte());
                Assert.Throws<ObjectDisposedException>(() => server.IsMessageComplete);
            }
        }

        [Fact]
        [ActiveIssue(1762, PlatformID.AnyUnix)]
        public static void ServerAfterDisconnectThrows()
        {
            using (NamedPipeServerStream server = new NamedPipeServerStream("unique3", PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            {
                using (NamedPipeClientStream client = new NamedPipeClientStream("unique3"))
                {
                    Task clientConnect = client.ConnectAsync(); 
                    server.WaitForConnection();
                    Assert.Throws<InvalidOperationException>(() => server.IsMessageComplete);
                    Assert.Throws<InvalidOperationException>(() => server.WaitForConnection());
                    Assert.ThrowsAsync<InvalidOperationException>(() => server.WaitForConnectionAsync());

                    server.Disconnect();
                    Assert.Throws<InvalidOperationException>(() => server.Disconnect());    // double disconnect
                    Assert.Throws<InvalidOperationException>(() => server.WriteByte(5));
                    Assert.Throws<InvalidOperationException>(() => server.ReadByte());
                    Assert.Throws<InvalidOperationException>(() => server.IsMessageComplete);
                }
            }
        }

        [Fact]
        public static void ServerWaitForConnectThrows()
        {
            using (NamedPipeServerStream server = new NamedPipeServerStream("unique3", PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 0, 0))
            {
                var ctx = new CancellationTokenSource();

                Task serverWaitTimeout = server.WaitForConnectionAsync(ctx.Token);
                ctx.Cancel();
                Assert.ThrowsAsync<TimeoutException>(() => serverWaitTimeout);

                Assert.ThrowsAsync<TimeoutException>(() => server.WaitForConnectionAsync(ctx.Token));
            }
        }

        [Fact]
        public static void ServerInvalidPipeHandleThrows()
        {
            SafePipeHandle pipeHandle = new SafePipeHandle(new IntPtr(-1), true);
            Assert.Throws<ArgumentException>(() => new NamedPipeServerStream(PipeDirection.InOut, false, true, pipeHandle));
        }

        [Fact]
        public static void ServerBadHandleKindThrows()
        {
            using (FileStream fs = new FileStream("tempTestFile", FileMode.Create, FileAccess.Write, FileShare.None, 8, FileOptions.DeleteOnClose))
            {
                SafeFileHandle safeHandle = fs.SafeFileHandle;

                bool gotRef = false;
                try
                {

                    safeHandle.DangerousAddRef(ref gotRef);
                    IntPtr handle = safeHandle.DangerousGetHandle();

                    SafePipeHandle fakePipeHandle = new SafePipeHandle(handle, true);
                    Assert.Throws<IOException>(() => new NamedPipeServerStream(PipeDirection.InOut, false, true, fakePipeHandle));
                }
                finally
                {
                    if (gotRef)
                        safeHandle.DangerousRelease();
                }
            }
        }

        // Client Parameter Checking throws
        [Fact]
        public static void ClientNullPipeNameParameterThrows()
        {
            Assert.Throws<ArgumentNullException>(() => new NamedPipeClientStream(null));
            Assert.Throws<ArgumentNullException>(() => new NamedPipeClientStream(".", null));
        }

        [Fact]
        public static void ClientZeroLengthPipeNameThrows()
        {
            Assert.Throws<ArgumentException>(() => new NamedPipeClientStream(""));
            Assert.Throws<ArgumentException>(() => new NamedPipeClientStream(".", ""));
        }

        [Fact]
        public static void ClientNullServerNameParameterThrows()
        {
            Assert.Throws<ArgumentNullException>(() => new NamedPipeClientStream(null, "client1"));
            Assert.Throws<ArgumentNullException>(() => new NamedPipeClientStream(null, "client1", PipeDirection.In));
            Assert.Throws<ArgumentNullException>(() => new NamedPipeClientStream(null, "client1", PipeDirection.In, PipeOptions.None));
            Assert.Throws<ArgumentNullException>(() => new NamedPipeClientStream(null, "client1", PipeDirection.In, PipeOptions.None, Security.Principal.TokenImpersonationLevel.None));
        }

        [Fact]
        public static void ClientZeroLengthServerNameThrows()
        {
            Assert.Throws<ArgumentException>(() => new NamedPipeClientStream("", "client1"));
            Assert.Throws<ArgumentException>(() => new NamedPipeClientStream("", "client1", PipeDirection.In));
            Assert.Throws<ArgumentException>(() => new NamedPipeClientStream("", "client1", PipeDirection.In, PipeOptions.None));
            Assert.Throws<ArgumentException>(() => new NamedPipeClientStream("", "client1", PipeDirection.In, PipeOptions.None, Security.Principal.TokenImpersonationLevel.None));
        }

        [Fact]
        public static void ClientPipeDiretionThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new NamedPipeClientStream(".", "client1", (PipeDirection)123));
            Assert.Throws<ArgumentOutOfRangeException>(() => new NamedPipeClientStream(".", "client1", (PipeDirection)123, PipeOptions.None));
            Assert.Throws<ArgumentOutOfRangeException>(() => new NamedPipeClientStream(".", "client1", (PipeDirection)123, PipeOptions.None, Security.Principal.TokenImpersonationLevel.None));
        }
        [Fact]
        public static void ClientPipeOptionsThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new NamedPipeClientStream(".", "client1", PipeDirection.In, (PipeOptions)255));
            Assert.Throws<ArgumentOutOfRangeException>(() => new NamedPipeClientStream(".", "client1", PipeDirection.In, (PipeOptions)255, Security.Principal.TokenImpersonationLevel.None));
        }

        [Fact]
        public static void ClientImpersonationLevelThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new NamedPipeClientStream(".", "client1", PipeDirection.In, PipeOptions.None, (System.Security.Principal.TokenImpersonationLevel)999));
        }

        [Fact]
        public static void ClientConnectTimeoutThrows()
        {
            using (NamedPipeClientStream client = new NamedPipeClientStream("client1"))
            {
                Assert.Throws<System.ArgumentOutOfRangeException>(() => client.Connect(-111));
                Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.ConnectAsync(-111));
            }
        }

        [Fact]
        public static void ClientNullHandleThrows()
        {
            Assert.Throws<ArgumentNullException>(() =>new NamedPipeClientStream(PipeDirection.InOut, false, true, null));
        }

        [Fact]
        public static void ClientInvalidHandleThrows()
        {
            SafePipeHandle pipeHandle = new SafePipeHandle(new IntPtr(-1), true);
            Assert.Throws<ArgumentException>(() => new NamedPipeClientStream(PipeDirection.InOut, false, true, pipeHandle));
        }

        [Fact]
        public static void ClientBadHandleKindThrows()
        {
            using (FileStream fs = new FileStream("tempTestFile", FileMode.Create, FileAccess.Write, FileShare.None, 8, FileOptions.DeleteOnClose))
            {
                SafeFileHandle safeHandle = fs.SafeFileHandle;

                bool gotRef = false;
                try
                {
                    
                    safeHandle.DangerousAddRef(ref gotRef);
                    IntPtr handle = safeHandle.DangerousGetHandle();

                    SafePipeHandle fakePipeHandle = new SafePipeHandle(handle, true);
                    Assert.Throws<IOException>(() => new NamedPipeClientStream(PipeDirection.InOut, false, true, fakePipeHandle));
                }
                finally
                {
                    if (gotRef)
                        safeHandle.DangerousRelease();
                }
            }
        }

        [Fact]
        public static void ClientNotConnectedThrows()
        {
            NamedPipeClientStream client = new NamedPipeClientStream(".", "notthere");

            Assert.Throws<InvalidOperationException>(() => client.WriteByte(5));
            Assert.Throws<InvalidOperationException>(() => client.ReadByte());
            Assert.Throws<InvalidOperationException>(() => client.NumberOfServerInstances);
            Assert.Throws<InvalidOperationException>(() => client.TransmissionMode);
            Assert.Throws<InvalidOperationException>(() => client.InBufferSize);
            Assert.Throws<InvalidOperationException>(() => client.OutBufferSize);
            Assert.Throws<InvalidOperationException>(() => client.ReadMode);
            Assert.Throws<InvalidOperationException>(() => client.ReadMode = PipeTransmissionMode.Byte);
            Assert.Throws<InvalidOperationException>(() => client.SafePipeHandle);
        }

        [Fact]
        public static void ClientTryConnectedThrows()
        {
            using (NamedPipeClientStream client = new NamedPipeClientStream(".", "notthere"))
            {
                var ctx = new CancellationTokenSource();

                Assert.Throws<TimeoutException>(() => client.Connect(60));  // 60 to be over internal 50 interval

                Assert.ThrowsAsync<TimeoutException>(() => client.ConnectAsync(50));

                Assert.ThrowsAsync<TimeoutException>(() => client.ConnectAsync(60, ctx.Token));

                Task clientConnectToken = client.ConnectAsync(ctx.Token);
                ctx.Cancel();
                Assert.ThrowsAsync<TimeoutException>(() => clientConnectToken);

                Assert.ThrowsAsync<TimeoutException>(() => client.ConnectAsync(ctx.Token));
            }
        }

        [Fact]
        public static void ClientAllReadyConnectedThrows()
        {
            using (NamedPipeServerStream server = new NamedPipeServerStream("testServer1"))
            {
                using (NamedPipeClientStream client = new NamedPipeClientStream("testServer1"))
                {
                    byte[] buffer = new byte[] { 0, 0, 0, 0 };

                    Task clientConnect1 = client.ConnectAsync();
                    server.WaitForConnection();
                    clientConnect1.Wait();

                    Assert.True(client.IsConnected);
                    Assert.True(server.IsConnected);

                    Assert.Throws<InvalidOperationException>(() => client.Connect());

                    var ctx = new CancellationTokenSource();
                    Task clientReadToken = client.ReadAsync(buffer, 0, buffer.Length, ctx.Token);
                    ctx.Cancel();
                    Assert.ThrowsAsync<TimeoutException>(() => clientReadToken);
                    Assert.ThrowsAsync<TimeoutException>(() => client.ReadAsync(buffer, 0, buffer.Length, ctx.Token));

                    var ctx1 = new CancellationTokenSource();
                    Task ServerReadToken = server.ReadAsync(buffer, 0, buffer.Length, ctx1.Token);
                    ctx1.Cancel();
                    Assert.ThrowsAsync<TimeoutException>(() => ServerReadToken);
                    Assert.ThrowsAsync<TimeoutException>(() => server.ReadAsync(buffer, 0, buffer.Length, ctx1.Token));
                }
            }
        }

        [Fact]
        [ActiveIssue(1765, PlatformID.AnyUnix)]
        public static void ClientDisconnectedPipeThrows()
        {
            using (NamedPipeServerStream server = new NamedPipeServerStream("testServer2", PipeDirection.InOut))
            {
                using (NamedPipeClientStream client = new NamedPipeClientStream("testServer2"))
                {
                    byte[] buffer = new byte[] { 0, 0, 0, 0 };

                    Task clientConnect1 = client.ConnectAsync();
                    server.WaitForConnection();
                    clientConnect1.Wait();

                    Assert.True(client.IsConnected);
                    Assert.True(server.IsConnected);

                    client.Dispose();
                    
                    Assert.Throws<IOException>(() => server.Write(buffer, 0, buffer.Length));
                    Assert.Throws<IOException>(() => server.WriteByte(123));
                    Assert.Throws<IOException>(() => server.Flush());
                    int length = server.Read(buffer, 0, buffer.Length);
                    Assert.Equal(0, length);
                    int byt = server.ReadByte();
                }
            }
        }

        [Fact]
        [ActiveIssue(1765, PlatformID.AnyUnix)]
        public static void ServerDisconnectedPipeThrows()
        {
            using (NamedPipeServerStream server = new NamedPipeServerStream("testServer3", PipeDirection.InOut))
            {
                using (NamedPipeClientStream client = new NamedPipeClientStream("testServer3"))
                {
                    byte[] buffer = new byte[] { 0, 0, 0, 0 };

                    Task clientConnect1 = client.ConnectAsync();
                    server.WaitForConnection();
                    clientConnect1.Wait();

                    Assert.True(client.IsConnected);
                    Assert.True(server.IsConnected);

                    server.Dispose();

                    Assert.Throws<IOException>(() => client.Write(buffer, 0, buffer.Length));
                    Assert.Throws<IOException>(() => client.WriteByte(123));
                    Assert.Throws<IOException>(() => client.Flush());
                    int length = client.Read(buffer, 0, buffer.Length);
                    Assert.Equal(0, length);
                    int byt = client.ReadByte();
                }
            }
        }
    }
}
