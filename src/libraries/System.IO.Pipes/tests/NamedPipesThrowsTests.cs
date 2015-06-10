// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipes.Tests
{
    public class NamedPipesThrowsTests : BaseCommonTests
    {
        // Server Parameter Checking throws
        [Fact]
        public static void ServerNullPipeNameThrows()
        {
            Assert.Throws<ArgumentNullException>("pipeName", () => new NamedPipeServerStream(null));
            Assert.Throws<ArgumentNullException>("pipeName", () => new NamedPipeServerStream(null, PipeDirection.In));
            Assert.Throws<ArgumentNullException>("pipeName", () => new NamedPipeServerStream(null, PipeDirection.In, 2));
            Assert.Throws<ArgumentNullException>("pipeName", () => new NamedPipeServerStream(null, PipeDirection.In, 3, PipeTransmissionMode.Byte));
            Assert.Throws<ArgumentNullException>("pipeName", () => new NamedPipeServerStream(null, PipeDirection.In, 3, PipeTransmissionMode.Byte, PipeOptions.None));
            Assert.Throws<ArgumentNullException>("pipeName", () => new NamedPipeServerStream(null, PipeDirection.In, 3, PipeTransmissionMode.Byte, PipeOptions.None, 0, 0));
            Assert.Throws<ArgumentNullException>("pipeName", () => new NamedPipeServerStream(null, PipeDirection.Out));
            Assert.Throws<ArgumentNullException>("pipeName", () => new NamedPipeServerStream(null, PipeDirection.Out, 2));
            Assert.Throws<ArgumentNullException>("pipeName", () => new NamedPipeServerStream(null, PipeDirection.Out, 3, PipeTransmissionMode.Byte));
            Assert.Throws<ArgumentNullException>("pipeName", () => new NamedPipeServerStream(null, PipeDirection.Out, 3, PipeTransmissionMode.Byte, PipeOptions.None));
            Assert.Throws<ArgumentNullException>("pipeName", () => new NamedPipeServerStream(null, PipeDirection.Out, 3, PipeTransmissionMode.Byte, PipeOptions.None, 0, 0));
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
            Assert.Throws<ArgumentException>(() => new NamedPipeServerStream("", PipeDirection.Out));
            Assert.Throws<ArgumentException>(() => new NamedPipeServerStream("", PipeDirection.Out, 2));
            Assert.Throws<ArgumentException>(() => new NamedPipeServerStream("", PipeDirection.Out, 3, PipeTransmissionMode.Byte));
            Assert.Throws<ArgumentException>(() => new NamedPipeServerStream("", PipeDirection.Out, 3, PipeTransmissionMode.Byte, PipeOptions.None));
            Assert.Throws<ArgumentException>(() => new NamedPipeServerStream("", PipeDirection.Out, 3, PipeTransmissionMode.Byte, PipeOptions.None, 0, 0));
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)]
        public static void ServerReservedPipeNameThrows()
        {
            const string reservedName = "anonymous";
            Assert.Throws<ArgumentOutOfRangeException>("pipeName", () => new NamedPipeServerStream(reservedName));
            Assert.Throws<ArgumentOutOfRangeException>("pipeName", () => new NamedPipeServerStream(reservedName, PipeDirection.In));
            Assert.Throws<ArgumentOutOfRangeException>("pipeName", () => new NamedPipeServerStream(reservedName, PipeDirection.In, 1));
            Assert.Throws<ArgumentOutOfRangeException>("pipeName", () => new NamedPipeServerStream(reservedName, PipeDirection.In, 1, PipeTransmissionMode.Byte));
            Assert.Throws<ArgumentOutOfRangeException>("pipeName", () => new NamedPipeServerStream(reservedName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.None));
            Assert.Throws<ArgumentOutOfRangeException>("pipeName", () => new NamedPipeServerStream(reservedName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.None, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>("pipeName", () => new NamedPipeServerStream(reservedName, PipeDirection.Out));
            Assert.Throws<ArgumentOutOfRangeException>("pipeName", () => new NamedPipeServerStream(reservedName, PipeDirection.Out, 1));
            Assert.Throws<ArgumentOutOfRangeException>("pipeName", () => new NamedPipeServerStream(reservedName, PipeDirection.Out, 1, PipeTransmissionMode.Byte));
            Assert.Throws<ArgumentOutOfRangeException>("pipeName", () => new NamedPipeServerStream(reservedName, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.None));
            Assert.Throws<ArgumentOutOfRangeException>("pipeName", () => new NamedPipeServerStream(reservedName, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.None, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>("pipeName", () => new NamedPipeServerStream(reservedName, PipeDirection.InOut));
            Assert.Throws<ArgumentOutOfRangeException>("pipeName", () => new NamedPipeServerStream(reservedName, PipeDirection.InOut, 1));
            Assert.Throws<ArgumentOutOfRangeException>("pipeName", () => new NamedPipeServerStream(reservedName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte));
            Assert.Throws<ArgumentOutOfRangeException>("pipeName", () => new NamedPipeServerStream(reservedName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.None));
            Assert.Throws<ArgumentOutOfRangeException>("pipeName", () => new NamedPipeServerStream(reservedName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.None, 0, 0));
        }

        [Fact]
        public static void ServerPipeDirectionThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>("direction", () => new NamedPipeServerStream("temp1", (PipeDirection)123));
            Assert.Throws<ArgumentOutOfRangeException>("direction", () => new NamedPipeServerStream("temp1", (PipeDirection)123, 1));
            Assert.Throws<ArgumentOutOfRangeException>("direction", () => new NamedPipeServerStream("temp1", (PipeDirection)123, 1, PipeTransmissionMode.Byte));
            Assert.Throws<ArgumentOutOfRangeException>("direction", () => new NamedPipeServerStream("temp1", (PipeDirection)123, 1, PipeTransmissionMode.Byte, PipeOptions.None));
            Assert.Throws<ArgumentOutOfRangeException>("direction", () => new NamedPipeServerStream("tempx", (PipeDirection)123, 1, PipeTransmissionMode.Byte, PipeOptions.None, 0, 0));
        }

        [Fact]
        public static void ServerServerInstancesThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>("maxNumberOfServerInstances", () => new NamedPipeServerStream("temp3", PipeDirection.Out, 0));
            Assert.Throws<ArgumentOutOfRangeException>("maxNumberOfServerInstances", () => new NamedPipeServerStream("temp3", PipeDirection.Out, 0, PipeTransmissionMode.Byte));
            Assert.Throws<ArgumentOutOfRangeException>("maxNumberOfServerInstances", () => new NamedPipeServerStream("temp3", PipeDirection.Out, 0, PipeTransmissionMode.Byte, PipeOptions.None));
            Assert.Throws<ArgumentOutOfRangeException>("maxNumberOfServerInstances", () => new NamedPipeServerStream("temp3", PipeDirection.Out, 0, PipeTransmissionMode.Byte, PipeOptions.None, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>("maxNumberOfServerInstances", () => new NamedPipeServerStream("temp3", PipeDirection.In, 0));
            Assert.Throws<ArgumentOutOfRangeException>("maxNumberOfServerInstances", () => new NamedPipeServerStream("temp3", PipeDirection.In, 0, PipeTransmissionMode.Byte));
            Assert.Throws<ArgumentOutOfRangeException>("maxNumberOfServerInstances", () => new NamedPipeServerStream("temp3", PipeDirection.In, 0, PipeTransmissionMode.Byte, PipeOptions.None));
            Assert.Throws<ArgumentOutOfRangeException>("maxNumberOfServerInstances", () => new NamedPipeServerStream("temp3", PipeDirection.In, 0, PipeTransmissionMode.Byte, PipeOptions.None, 0, 0));
        }

        [Fact]
        public static void ServerTransmissionModeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>("transmissionMode", () => new NamedPipeServerStream("temp1", PipeDirection.Out, 1, (PipeTransmissionMode)123));
            Assert.Throws<ArgumentOutOfRangeException>("transmissionMode", () => new NamedPipeServerStream("temp1", PipeDirection.Out, 1, (PipeTransmissionMode)123, PipeOptions.None));
            Assert.Throws<ArgumentOutOfRangeException>("transmissionMode", () => new NamedPipeServerStream("tempx", PipeDirection.Out, 1, (PipeTransmissionMode)123, PipeOptions.None, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>("transmissionMode", () => new NamedPipeServerStream("temp1", PipeDirection.In, 1, (PipeTransmissionMode)123));
            Assert.Throws<ArgumentOutOfRangeException>("transmissionMode", () => new NamedPipeServerStream("temp1", PipeDirection.In, 1, (PipeTransmissionMode)123, PipeOptions.None));
            Assert.Throws<ArgumentOutOfRangeException>("transmissionMode", () => new NamedPipeServerStream("tempx", PipeDirection.In, 1, (PipeTransmissionMode)123, PipeOptions.None, 0, 0));
        }

        [Fact]
        public static void ServerPipeOptionsThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>("options", () => new NamedPipeServerStream("temp1", PipeDirection.Out, 1, PipeTransmissionMode.Byte, (PipeOptions)255));
            Assert.Throws<ArgumentOutOfRangeException>("options", () => new NamedPipeServerStream("tempx", PipeDirection.Out, 1, PipeTransmissionMode.Byte, (PipeOptions)255, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>("options", () => new NamedPipeServerStream("temp1", PipeDirection.In, 1, PipeTransmissionMode.Byte, (PipeOptions)255));
            Assert.Throws<ArgumentOutOfRangeException>("options", () => new NamedPipeServerStream("tempx", PipeDirection.In, 1, PipeTransmissionMode.Byte, (PipeOptions)255, 0, 0));
        }

        [Fact]
        public static void ServerBufferSizeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>("inBufferSize", () => new NamedPipeServerStream("temp2", PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.None, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>("outBufferSize", () => new NamedPipeServerStream("temp2", PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.None, 0, -123));
            Assert.Throws<ArgumentOutOfRangeException>("inBufferSize", () => new NamedPipeServerStream("temp2", PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.None, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>("outBufferSize", () => new NamedPipeServerStream("temp2", PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.None, 0, -123));
            Assert.Throws<ArgumentOutOfRangeException>("inBufferSize", () => new NamedPipeServerStream("temp2", PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.None, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>("outBufferSize", () => new NamedPipeServerStream("temp2", PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.None, 0, -123));


        }

        [Fact]
        public static void ServerNullPipeHandleThrows()
        {
            Assert.Throws<ArgumentNullException>("safePipeHandle", () => new NamedPipeServerStream(PipeDirection.InOut, false, true, null));
            Assert.Throws<ArgumentNullException>("safePipeHandle", () => new NamedPipeServerStream(PipeDirection.Out, false, true, null));
            Assert.Throws<ArgumentNullException>("safePipeHandle", () => new NamedPipeServerStream(PipeDirection.In, false, true, null));
        }

        [Fact]
        public static void ServerWriteBufferNullThrows()
        {
            using (NamedPipeServerStream server = new NamedPipeServerStream("unique3", PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            using (NamedPipeClientStream client = new NamedPipeClientStream(".", "unique3", PipeDirection.In))
            {
                Task clientConnect = client.ConnectAsync();
                server.WaitForConnection();
                clientConnect.Wait();

                ConnectedPipeWriteBufferNullThrows(server);
            }
        }

        [Fact]
        public static void ServerWriteNegativeOffsetThrows()
        {
            using (NamedPipeServerStream server = new NamedPipeServerStream("unique3", PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            using (NamedPipeClientStream client = new NamedPipeClientStream(".", "unique3", PipeDirection.In))
            {
                Task clientConnect = client.ConnectAsync();
                server.WaitForConnection();
                clientConnect.Wait();

                ConnectedPipeWriteNegativeOffsetThrows(server);
            }
        }

        [Fact]
        public static void ServerWriteNegativeCountThrows()
        {
            using (NamedPipeServerStream server = new NamedPipeServerStream("unique3", PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            using (NamedPipeClientStream client = new NamedPipeClientStream(".", "unique3", PipeDirection.In))
            {
                Task clientConnect = client.ConnectAsync();
                server.WaitForConnection();
                clientConnect.Wait();

                ConnectedPipeWriteNegativeCountThrows(server);
            }
        }

        [Fact]
        public static void ServerWriteArrayOutOfBoundsThrows()
        {
            using (NamedPipeServerStream server = new NamedPipeServerStream("unique3", PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            using (NamedPipeClientStream client = new NamedPipeClientStream(".", "unique3", PipeDirection.In))
            {
                Task clientConnect = client.ConnectAsync();
                server.WaitForConnection();
                clientConnect.Wait();

                ConnectedPipeWriteArrayOutOfBoundsThrows(server);
            }
        }

        [Fact]
        public static void ServerWriteOnlyThrows()
        {
            using (NamedPipeServerStream server = new NamedPipeServerStream("unique3", PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            using (NamedPipeClientStream client = new NamedPipeClientStream(".", "unique3", PipeDirection.In))
            {
                Task clientConnect = client.ConnectAsync();
                server.WaitForConnection();
                clientConnect.Wait();

                ConnectedPipeWriteOnlyThrows(server);
            }
        }

        [Fact]
        public static void ServerUnsupportedOperationThrows()
        {
            using (NamedPipeServerStream server = new NamedPipeServerStream("unique3", PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            using (NamedPipeClientStream client = new NamedPipeClientStream(".", "unique3", PipeDirection.In))
            {
                Task clientConnect = client.ConnectAsync();
                server.WaitForConnection();
                clientConnect.Wait();

                ConnectedPipeUnsupportedOperationThrows(server);
            }
        }

        [Fact]
        public static void ServerReadBufferNullThrows()
        {
            using (NamedPipeServerStream server = new NamedPipeServerStream("unique3", PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            using (NamedPipeClientStream client = new NamedPipeClientStream(".", "unique3", PipeDirection.Out))
            {
                Task clientConnect = client.ConnectAsync();
                server.WaitForConnection();
                clientConnect.Wait();

                ConnectedPipeReadBufferNullThrows(server);
            }
        }

        [Fact]
        public static void ServerReadNegativeOffsetThrows()
        {
            using (NamedPipeServerStream server = new NamedPipeServerStream("unique3", PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            using (NamedPipeClientStream client = new NamedPipeClientStream(".", "unique3", PipeDirection.Out))
            {
                Task clientConnect = client.ConnectAsync();
                server.WaitForConnection();
                clientConnect.Wait();

                ConnectedPipeReadNegativeOffsetThrows(server);
            }
        }

        [Fact]
        public static void ServerReadNegativeCountThrows()
        {
            using (NamedPipeServerStream server = new NamedPipeServerStream("unique3", PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            using (NamedPipeClientStream client = new NamedPipeClientStream(".", "unique3", PipeDirection.Out))
            {
                Task clientConnect = client.ConnectAsync();
                server.WaitForConnection();
                clientConnect.Wait();

                ConnectedPipeReadNegativeCountThrows(server);
            }
        }

        [Fact]
        public static void ServerReadArrayOutOfBoundsThrows()
        {
            using (NamedPipeServerStream server = new NamedPipeServerStream("unique3", PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            using (NamedPipeClientStream client = new NamedPipeClientStream(".", "unique3", PipeDirection.Out))
            {
                Task clientConnect = client.ConnectAsync();
                server.WaitForConnection();
                clientConnect.Wait();

                ConnectedPipeReadArrayOutOfBoundsThrows(server);
            }
        }

        [Fact]
        public static void ServerReadOnlyThrows()
        {
            using (NamedPipeServerStream server = new NamedPipeServerStream("unique3", PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            using (NamedPipeClientStream client = new NamedPipeClientStream(".", "unique3", PipeDirection.Out))
            {
                Task clientConnect = client.ConnectAsync();
                server.WaitForConnection();
                clientConnect.Wait();

                ConnectedPipeReadOnlyThrows(server);
            }
        }

        [Fact]
        public static void ServerNotConnectedThrows()
        {
            using (NamedPipeServerStream server = new NamedPipeServerStream("tempnotconnected", PipeDirection.InOut, 1))
            {
                // doesn't throw exceptions
                PipeTransmissionMode transmitMode = server.TransmissionMode;
                Assert.Throws<ArgumentOutOfRangeException>(() => server.ReadMode = (PipeTransmissionMode)999);

                if (Interop.IsWindows || Interop.IsLinux)
                {
                    Assert.Equal(0, server.InBufferSize);
                    Assert.Equal(0, server.OutBufferSize);
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
        [PlatformSpecific(PlatformID.Windows)] // NumberOfServerInstances > 1 isn't supported and has undefined behavior on Unix
        public static void ServerVerifyMultipleServerInconsistantType()
        {
            const string uniqueServerName = "UniqueName11111";
            using (NamedPipeServerStream server = new NamedPipeServerStream(uniqueServerName, PipeDirection.In, 2, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            {
                Assert.Throws<UnauthorizedAccessException>(() => new NamedPipeServerStream(uniqueServerName, PipeDirection.Out));
            }
        }

        [Fact]
        public static void ServerWhenDisplosedThrows()
        {
            using (NamedPipeServerStream server = new NamedPipeServerStream("unique2", PipeDirection.InOut))
            {
                server.Dispose();

                Assert.Throws<ObjectDisposedException>(() => server.Disconnect());  // disconnect when disposed
                Assert.Throws<ObjectDisposedException>(() => server.WaitForConnection());  // disconnect when disposed
                WhenDisposedPipeThrows(server);
            }
        }

        [Fact]
        public static async Task ServerAfterDisconnectThrows()
        {
            using (NamedPipeServerStream server = new NamedPipeServerStream("unique3", PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            using (NamedPipeClientStream client = new NamedPipeClientStream(".", "unique3", PipeDirection.In))
            {
                Task clientConnect = client.ConnectAsync();
                server.WaitForConnection();
                await clientConnect;

                Assert.Throws<InvalidOperationException>(() => server.IsMessageComplete);
                Assert.Throws<InvalidOperationException>(() => server.WaitForConnection());
                await Assert.ThrowsAsync<InvalidOperationException>(() => server.WaitForConnectionAsync());

                server.Disconnect();

                Assert.Throws<InvalidOperationException>(() => server.Disconnect());    // double disconnect

                AfterDisconnectWriteOnlyPipeThrows(server);
            }

            if (Interop.IsWindows) // on Unix, InOut doesn't result in the same Disconnect-based errors due to allowing for other connections
            {
                using (NamedPipeServerStream server = new NamedPipeServerStream("unique3", PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
                using (NamedPipeClientStream client = new NamedPipeClientStream("unique3"))
                {
                    Task clientConnect = client.ConnectAsync();
                    server.WaitForConnection();
                    await clientConnect;

                    Assert.Throws<InvalidOperationException>(() => server.IsMessageComplete);
                    Assert.Throws<InvalidOperationException>(() => server.WaitForConnection());
                    await Assert.ThrowsAsync<InvalidOperationException>(() => server.WaitForConnectionAsync());

                    server.Disconnect();

                    Assert.Throws<InvalidOperationException>(() => server.Disconnect());    // double disconnect

                    AfterDisconnectReadWritePipeThrows(server);
                }
            }
        }

        [Fact]
        public static async Task ServerWaitForConnectThrows()
        {
            using (NamedPipeServerStream server = new NamedPipeServerStream("unique3", PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 0, 0))
            {
                var ctx = new CancellationTokenSource();

                if (Interop.IsWindows) // [ActiveIssue(812, PlatformID.AnyUnix)] - cancellation token after the operation has been initiated
                {
                    Task serverWaitTimeout = server.WaitForConnectionAsync(ctx.Token);
                    ctx.Cancel();
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => serverWaitTimeout);
                }

                ctx.Cancel();
                Assert.True(server.WaitForConnectionAsync(ctx.Token).IsCanceled);
            }
        }

        [Fact]
        public static void ServerInvalidPipeHandleThrows()
        {
            SafePipeHandle pipeHandle = new SafePipeHandle(new IntPtr(-1), true);
            Assert.Throws<ArgumentException>("safePipeHandle", () => new NamedPipeServerStream(PipeDirection.InOut, false, true, pipeHandle));
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

                    SafePipeHandle fakePipeHandle = new SafePipeHandle(handle, ownsHandle: false);
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
            Assert.Throws<ArgumentNullException>("pipeName", () => new NamedPipeClientStream(null));
            Assert.Throws<ArgumentNullException>("pipeName", () => new NamedPipeClientStream(".", null));
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
            Assert.Throws<ArgumentNullException>("serverName", () => new NamedPipeClientStream(null, "client1"));
            Assert.Throws<ArgumentNullException>("serverName", () => new NamedPipeClientStream(null, "client1", PipeDirection.In));
            Assert.Throws<ArgumentNullException>("serverName", () => new NamedPipeClientStream(null, "client1", PipeDirection.In, PipeOptions.None));
            Assert.Throws<ArgumentNullException>("serverName", () => new NamedPipeClientStream(null, "client1", PipeDirection.In, PipeOptions.None, Security.Principal.TokenImpersonationLevel.None));
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
            Assert.Throws<ArgumentOutOfRangeException>("direction", () => new NamedPipeClientStream(".", "client1", (PipeDirection)123));
            Assert.Throws<ArgumentOutOfRangeException>("direction", () => new NamedPipeClientStream(".", "client1", (PipeDirection)123, PipeOptions.None));
            Assert.Throws<ArgumentOutOfRangeException>("direction", () => new NamedPipeClientStream(".", "client1", (PipeDirection)123, PipeOptions.None, Security.Principal.TokenImpersonationLevel.None));
        }
        [Fact]
        public static void ClientPipeOptionsThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>("options", () => new NamedPipeClientStream(".", "client1", PipeDirection.In, (PipeOptions)255));
            Assert.Throws<ArgumentOutOfRangeException>("options", () => new NamedPipeClientStream(".", "client1", PipeDirection.In, (PipeOptions)255, Security.Principal.TokenImpersonationLevel.None));
        }

        [Fact]
        public static void ClientImpersonationLevelThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>("impersonationLevel", () => new NamedPipeClientStream(".", "client1", PipeDirection.In, PipeOptions.None, (System.Security.Principal.TokenImpersonationLevel)999));
        }

        [Fact]
        public static void ClientConnectTimeoutThrows()
        {
            using (NamedPipeClientStream client = new NamedPipeClientStream("client1"))
            {
                Assert.Throws<ArgumentOutOfRangeException>("timeout", () => client.Connect(-111));
                Assert.Throws<ArgumentOutOfRangeException>("timeout", () => { client.ConnectAsync(-111); } );
            }
        }

        [Fact]
        public static void ClientNullHandleThrows()
        {
            Assert.Throws<ArgumentNullException>("safePipeHandle", () => new NamedPipeClientStream(PipeDirection.InOut, false, true, null));
        }

        [Fact]
        public static void ClientInvalidHandleThrows()
        {
            SafePipeHandle pipeHandle = new SafePipeHandle(new IntPtr(-1), true);
            Assert.Throws<ArgumentException>("safePipeHandle", () => new NamedPipeClientStream(PipeDirection.InOut, false, true, pipeHandle));
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

                    SafePipeHandle fakePipeHandle = new SafePipeHandle(handle, ownsHandle: false);
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
        public static async Task ClientTryConnectedThrows()
        {
            using (NamedPipeClientStream client = new NamedPipeClientStream(".", "notthere"))
            {
                var ctx = new CancellationTokenSource();

                if (Interop.IsWindows) // [ActiveIssue(812, PlatformID.AnyUnix)] - Unix implementation currently ignores timeout and cancellation token once the operation has been initiated
                {
                    Assert.Throws<TimeoutException>(() => client.Connect(60));  // 60 to be over internal 50 interval
                    await Assert.ThrowsAsync<TimeoutException>(() => client.ConnectAsync(50));
                    await Assert.ThrowsAsync<TimeoutException>(() => client.ConnectAsync(60, ctx.Token));

                    Task clientConnectToken = client.ConnectAsync(ctx.Token);
                    ctx.Cancel();
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => clientConnectToken);
                }

                ctx.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.ConnectAsync(ctx.Token));
            }
        }

        [Fact]
        public static void ClientReadBufferNullThrows()
        {
            using (NamedPipeServerStream server = new NamedPipeServerStream("unique4", PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            using (NamedPipeClientStream client = new NamedPipeClientStream(".", "unique4", PipeDirection.In))
            {
                Task clientConnect = client.ConnectAsync();
                server.WaitForConnection();
                clientConnect.Wait();

                ConnectedPipeReadBufferNullThrows(client);
            }
        }

        [Fact]
        public static void ClientReadNegativeOffsetThrows()
        {
            using (NamedPipeServerStream server = new NamedPipeServerStream("unique4", PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            using (NamedPipeClientStream client = new NamedPipeClientStream(".", "unique4", PipeDirection.In))
            {
                Task clientConnect = client.ConnectAsync();
                server.WaitForConnection();
                clientConnect.Wait();

                ConnectedPipeReadNegativeOffsetThrows(client);
            }
        }

        [Fact]
        public static void ClientReadNegativeCountThrows()
        {
            using (NamedPipeServerStream server = new NamedPipeServerStream("unique4", PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            using (NamedPipeClientStream client = new NamedPipeClientStream(".", "unique4", PipeDirection.In))
            {
                Task clientConnect = client.ConnectAsync();
                server.WaitForConnection();
                clientConnect.Wait();

                ConnectedPipeReadNegativeCountThrows(client);
            }
        }

        [Fact]
        public static void ClientReadArrayOutOfBoundsThrows()
        {
            using (NamedPipeServerStream server = new NamedPipeServerStream("unique4", PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            using (NamedPipeClientStream client = new NamedPipeClientStream(".", "unique4", PipeDirection.In))
            {
                Task clientConnect = client.ConnectAsync();
                server.WaitForConnection();
                clientConnect.Wait();

                ConnectedPipeReadArrayOutOfBoundsThrows(client);
            }
        }

        [Fact]
        public static void ClientConnectedPipeReadOnlyThrows()
        {
            using (NamedPipeServerStream server = new NamedPipeServerStream("unique4", PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            using (NamedPipeClientStream client = new NamedPipeClientStream(".", "unique4", PipeDirection.In))
            {
                Task clientConnect = client.ConnectAsync();
                server.WaitForConnection();
                clientConnect.Wait();

                ConnectedPipeReadOnlyThrows(client);
            }
        }

        [Fact]
        public static void ClientWriteBufferNullThrows()
        {
            using (NamedPipeServerStream server = new NamedPipeServerStream("unique4", PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            using (NamedPipeClientStream client = new NamedPipeClientStream(".", "unique4", PipeDirection.Out))
            {
                Task clientConnect = client.ConnectAsync();
                server.WaitForConnection();
                clientConnect.Wait();

                ConnectedPipeWriteBufferNullThrows(client);
            }
        }

        [Fact]
        public static void ClientWriteNegativeOffsetThrows()
        {
            using (NamedPipeServerStream server = new NamedPipeServerStream("unique4", PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            using (NamedPipeClientStream client = new NamedPipeClientStream(".", "unique4", PipeDirection.Out))
            {
                Task clientConnect = client.ConnectAsync();
                server.WaitForConnection();
                clientConnect.Wait();

                ConnectedPipeWriteNegativeOffsetThrows(client);
            }
        }

        [Fact]
        public static void ClientWriteNegativeCountThrows()
        {
            using (NamedPipeServerStream server = new NamedPipeServerStream("unique4", PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            using (NamedPipeClientStream client = new NamedPipeClientStream(".", "unique4", PipeDirection.Out))
            {
                Task clientConnect = client.ConnectAsync();
                server.WaitForConnection();
                clientConnect.Wait();

                ConnectedPipeWriteNegativeCountThrows(client);
            }
        }

        [Fact]
        public static void ClientWriteArrayOutOfBoundsThrows()
        {
            using (NamedPipeServerStream server = new NamedPipeServerStream("unique4", PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            using (NamedPipeClientStream client = new NamedPipeClientStream(".", "unique4", PipeDirection.Out))
            {
                Task clientConnect = client.ConnectAsync();
                server.WaitForConnection();
                clientConnect.Wait();

                ConnectedPipeWriteArrayOutOfBoundsThrows(client);
            }
        }

        [Fact]
        public static void ClientWriteOnlyThrows()
        {
            using (NamedPipeServerStream server = new NamedPipeServerStream("unique4", PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            using (NamedPipeClientStream client = new NamedPipeClientStream(".", "unique4", PipeDirection.Out))
            {
                Task clientConnect = client.ConnectAsync();
                server.WaitForConnection();
                clientConnect.Wait();

                ConnectedPipeWriteOnlyThrows(client);
            }
        }

        [Fact]
        public static void ClientUnsupportedOperationThrows()
        {
            using (NamedPipeServerStream server = new NamedPipeServerStream("unique4", PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            using (NamedPipeClientStream client = new NamedPipeClientStream(".", "unique4", PipeDirection.Out))
            {
                Task clientConnect = client.ConnectAsync();
                server.WaitForConnection();
                clientConnect.Wait();

                ConnectedPipeUnsupportedOperationThrows(client);
            }
        }

        [Fact]
        public static async Task ClientAllReadyConnectedThrows()
        {
            using (NamedPipeServerStream server = new NamedPipeServerStream("testServer1", PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
            using (NamedPipeClientStream client = new NamedPipeClientStream(".", "testServer1", PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                byte[] buffer = new byte[] { 0, 0, 0, 0 };

                Task clientConnect1 = client.ConnectAsync();
                server.WaitForConnection();
                await clientConnect1;

                Assert.True(client.IsConnected);
                Assert.True(server.IsConnected);

                Assert.Throws<InvalidOperationException>(() => client.Connect());

                var ctx = new CancellationTokenSource();
                if (Interop.IsWindows) // [ActiveIssue(812, PlatformID.AnyUnix)] - the cancellation token is ignored after the operation is initiated, due to base Stream's implementation
                {
                    Task clientReadToken = client.ReadAsync(buffer, 0, buffer.Length, ctx.Token);
                    ctx.Cancel();
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => clientReadToken);
                }
                ctx.Cancel();
                Assert.True(client.ReadAsync(buffer, 0, buffer.Length, ctx.Token).IsCanceled);

                var ctx1 = new CancellationTokenSource();
                if (Interop.IsWindows) // [ActiveIssue(812, PlatformID.AnyUnix)] - the cancellation token is ignored after the operation is initiated, due to base Stream's implementation
                {
                    Task serverReadToken = server.ReadAsync(buffer, 0, buffer.Length, ctx1.Token);
                    ctx1.Cancel();
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => serverReadToken);
                }
                ctx1.Cancel();
                Assert.True(server.ReadAsync(buffer, 0, buffer.Length, ctx1.Token).IsCanceled);
            }
        }

        [Fact]
        public static async Task ClientDisconnectedPipeThrows()
        {
            using (NamedPipeServerStream server = new NamedPipeServerStream("testServer2", PipeDirection.Out))
            using (NamedPipeClientStream client = new NamedPipeClientStream(".", "testServer2", PipeDirection.In))
            {
                byte[] buffer = new byte[] { 0, 0, 0, 0 };

                Task clientConnect1 = client.ConnectAsync();
                server.WaitForConnection();
                await clientConnect1;

                Assert.True(client.IsConnected);
                Assert.True(server.IsConnected);

                client.Dispose();

                OtherSidePipeDisconnectWriteThrows(server);
            }

            using (NamedPipeServerStream server = new NamedPipeServerStream("testServer2", PipeDirection.In))
            using (NamedPipeClientStream client = new NamedPipeClientStream(".", "testServer2", PipeDirection.Out))
            {
                byte[] buffer = new byte[] { 0, 0, 0, 0 };

                Task clientConnect1 = client.ConnectAsync();
                server.WaitForConnection();
                await clientConnect1;

                Assert.True(client.IsConnected);
                Assert.True(server.IsConnected);

                client.Dispose();

                OtherSidePipeDisconnectVerifyRead(server);
            }

            if (Interop.IsWindows) // Unix implementation of InOut doesn't fail on server.Write/Read when client disconnects due to allowing for additional connections
            {
                using (NamedPipeServerStream server = new NamedPipeServerStream("testServer2", PipeDirection.InOut))
                using (NamedPipeClientStream client = new NamedPipeClientStream("testServer2"))
                {
                    byte[] buffer = new byte[] { 0, 0, 0, 0 };

                    Task clientConnect1 = client.ConnectAsync();
                    server.WaitForConnection();
                    await clientConnect1;

                    Assert.True(client.IsConnected);
                    Assert.True(server.IsConnected);

                    client.Dispose();

                    OtherSidePipeDisconnectWriteThrows(server);
                    OtherSidePipeDisconnectVerifyRead(server);
                }
            }
        }

        [Fact]
        public static async Task ServerDisconnectedPipeThrows()
        {
            using (NamedPipeServerStream server = new NamedPipeServerStream("testServer3", PipeDirection.In))
            using (NamedPipeClientStream client = new NamedPipeClientStream(".", "testServer3", PipeDirection.Out))
            {
                byte[] buffer = new byte[] { 0, 0, 0, 0 };

                Task clientConnect1 = client.ConnectAsync();
                server.WaitForConnection();
                await clientConnect1;

                Assert.True(client.IsConnected);
                Assert.True(server.IsConnected);

                server.Dispose();

                OtherSidePipeDisconnectWriteThrows(client);
            }

            if (Interop.IsWindows) // Unix implementation of InOut doesn't fail on server.Write/Read when client disconnects due to allowing for additional connections
            {
                using (NamedPipeServerStream server = new NamedPipeServerStream("testServer3", PipeDirection.InOut))
                using (NamedPipeClientStream client = new NamedPipeClientStream("testServer3"))
                {
                    byte[] buffer = new byte[] { 0, 0, 0, 0 };

                    Task clientConnect1 = client.ConnectAsync();
                    server.WaitForConnection();
                    await clientConnect1;

                    Assert.True(client.IsConnected);
                    Assert.True(server.IsConnected);

                    server.Dispose();

                    OtherSidePipeDisconnectWriteThrows(client);
                    OtherSidePipeDisconnectVerifyRead(client);
                }
            }
        }

        // Unix port currently doesn't recognize conflicts in pipe direction
        [Fact]
        [PlatformSpecific(PlatformID.Windows)]
        public static void ClientConnectWrongConflictingDirection()
        {
            const string serverName1 = "testServer4";

            using (NamedPipeServerStream server = new NamedPipeServerStream(serverName1, PipeDirection.Out))
            using (NamedPipeClientStream client = new NamedPipeClientStream(".", serverName1, PipeDirection.Out))
            {
                Assert.Throws<UnauthorizedAccessException>(() => client.Connect());

                Assert.False(client.IsConnected);
            }

            const string serverName2 = "testServer5";

            using (NamedPipeServerStream server = new NamedPipeServerStream(serverName2, PipeDirection.In))
            using (NamedPipeClientStream client = new NamedPipeClientStream(".", serverName2, PipeDirection.In))
            {
                Assert.Throws<UnauthorizedAccessException>(() => client.Connect());

                Assert.False(client.IsConnected);
            }
        }
    }
}
