// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Threading;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Net.Sockets.Tests
{
    public class OSSupportTest
    {
        [Fact]
        public void SupportsIPv4_MatchesOSSupportsIPv4()
        {
#pragma warning disable 0618 // Supports* are obsoleted
            Assert.Equal(Socket.SupportsIPv4, Socket.OSSupportsIPv4);
#pragma warning restore
        }

        [Fact]
        public void SupportsIPv6_MatchesOSSupportsIPv6()
        {
#pragma warning disable 0618 // Supports* are obsoleted
            Assert.Equal(Socket.SupportsIPv6, Socket.OSSupportsIPv6);
#pragma warning restore
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void DisableIPv6_OSSupportsIPv6_False()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.StartInfo.EnvironmentVariables["DOTNET_SYSTEM_NET_DISABLEIPV6"] = "1";
            RemoteExecutor.Invoke(RunTest, options).Dispose();

            static void RunTest()
            {
                Assert.False(Socket.OSSupportsIPv6);
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void DisableIPv6_SocketConstructor_CreatesIPv4Socket()
        {
            RemoteExecutor.Invoke(RunTest).Dispose();

            static void RunTest()
            {
                AppContext.SetSwitch("System.Net.DisableIPv6", true);
                using Socket socket1 = new Socket(SocketType.Stream, ProtocolType.Tcp);
                using Socket socket2 = new Socket(SocketType.Dgram, ProtocolType.Udp);

                Assert.Equal(AddressFamily.InterNetwork, socket1.AddressFamily);
                Assert.Equal(AddressFamily.InterNetwork, socket2.AddressFamily);
                Assert.False(socket1.DualMode);
                Assert.False(socket2.DualMode);
            }
        }

        [Fact]
        public void IOControl_FIONREAD_Success()
        {
            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                Assert.Throws<SocketException>(() => client.IOControl(IOControlCode.DataToRead, null, null));
                Assert.Throws<SocketException>(() => client.IOControl(IOControlCode.DataToRead, null, new byte[0]));
                Assert.Throws<SocketException>(() => client.IOControl(IOControlCode.DataToRead, null, new byte[sizeof(int) - 1]));

                byte[] fionreadResult = new byte[sizeof(int)];

                Assert.Equal(4, client.IOControl(IOControlCode.DataToRead, null, fionreadResult));
                Assert.Equal(client.Available, BitConverter.ToInt32(fionreadResult, 0));
                Assert.Equal(0, BitConverter.ToInt32(fionreadResult, 0));

                Assert.Equal(4, client.IOControl((int)IOControlCode.DataToRead, null, fionreadResult));
                Assert.Equal(client.Available, BitConverter.ToInt32(fionreadResult, 0));
                Assert.Equal(0, BitConverter.ToInt32(fionreadResult, 0));

                using (var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                    listener.Listen(1);

                    client.Connect(listener.LocalEndPoint);
                    using (Socket server = listener.Accept())
                    {
                        server.Send(new byte[] { 42 });
                        Assert.True(SpinWait.SpinUntil(() => client.Available != 0, 10_000));

                        Assert.Equal(4, client.IOControl(IOControlCode.DataToRead, null, fionreadResult));
                        Assert.Equal(client.Available, BitConverter.ToInt32(fionreadResult, 0));
                        Assert.Equal(1, BitConverter.ToInt32(fionreadResult, 0));
                    }
                }
            }
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50568", TestPlatforms.Android)]
        public void IOControl_SIOCATMARK_Unix_Success()
        {
            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                Assert.Throws<SocketException>(() => client.IOControl(IOControlCode.OobDataRead, null, null));
                Assert.Throws<SocketException>(() => client.IOControl(IOControlCode.OobDataRead, null, new byte[0]));
                Assert.Throws<SocketException>(() => client.IOControl(IOControlCode.OobDataRead, null, new byte[sizeof(int) - 1]));

                using (var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                    listener.Listen(1);

                    client.Connect(listener.LocalEndPoint);
                    using (Socket server = listener.Accept())
                    {
                        byte[] siocatmarkResult = new byte[sizeof(int)];

                        // Socket connected but no data sent.
                        Assert.Equal(4, client.IOControl(IOControlCode.OobDataRead, null, siocatmarkResult));
                        Assert.Equal(0, BitConverter.ToInt32(siocatmarkResult, 0));

                        server.Send(new byte[] { 42 }, SocketFlags.None);
                        server.Send(new byte[] { 43 }, SocketFlags.OutOfBand);

                        // OOB data recieved, but read pointer not at mark.
                        Assert.True(SpinWait.SpinUntil(() =>
                        {
                            Assert.Equal(4, client.IOControl(IOControlCode.OobDataRead, null, siocatmarkResult));
                            return BitConverter.ToInt32(siocatmarkResult, 0) == 0;
                        }, 10_000));

                        var received = new byte[1];

                        Assert.Equal(1, client.Receive(received));
                        Assert.Equal(42, received[0]);

                        // OOB data recieved, read pointer at mark.
                        Assert.Equal(4, client.IOControl(IOControlCode.OobDataRead, null, siocatmarkResult));
                        Assert.Equal(1, BitConverter.ToInt32(siocatmarkResult, 0));

                        Assert.Equal(1, client.Receive(received, SocketFlags.OutOfBand));
                        Assert.Equal(43, received[0]);

                        // OOB data read, read pointer at mark.
                        Assert.Equal(4, client.IOControl(IOControlCode.OobDataRead, null, siocatmarkResult));
                        Assert.Equal(PlatformDetection.IsOSXLike ? 0 : 1, BitConverter.ToInt32(siocatmarkResult, 0));
                    }
                }
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)]
        [Fact]
        public void IOControl_SIOCATMARK_Windows_Success()
        {
            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                Assert.Throws<SocketException>(() => client.IOControl(IOControlCode.OobDataRead, null, null));
                Assert.Throws<SocketException>(() => client.IOControl(IOControlCode.OobDataRead, null, new byte[0]));
                Assert.Throws<SocketException>(() => client.IOControl(IOControlCode.OobDataRead, null, new byte[sizeof(int) - 1]));

                using (var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                    listener.Listen(1);

                    client.Connect(listener.LocalEndPoint);
                    using (Socket server = listener.Accept())
                    {
                        byte[] siocatmarkResult = new byte[sizeof(int)];

                        // Socket connected but no data sent.
                        Assert.Equal(4, client.IOControl(IOControlCode.OobDataRead, null, siocatmarkResult));
                        Assert.Equal(1, BitConverter.ToInt32(siocatmarkResult, 0));

                        server.Send(new byte[] { 42 }, SocketFlags.None);
                        server.Send(new byte[] { 43 }, SocketFlags.OutOfBand);

                        // OOB data recieved, but read pointer not at mark
                        Assert.True(SpinWait.SpinUntil(() =>
                        {
                            Assert.Equal(4, client.IOControl(IOControlCode.OobDataRead, null, siocatmarkResult));
                            return BitConverter.ToInt32(siocatmarkResult, 0) == 0;
                        }, 10_000));

                        var received = new byte[1];

                        Assert.Equal(1, client.Receive(received));
                        Assert.Equal(42, received[0]);

                        // OOB data recieved, read pointer at mark.
                        Assert.Equal(4, client.IOControl(IOControlCode.OobDataRead, null, siocatmarkResult));
                        Assert.Equal(0, BitConverter.ToInt32(siocatmarkResult, 0));

                        Assert.Equal(1, client.Receive(received, SocketFlags.OutOfBand));
                        Assert.Equal(43, received[0]);

                        // OOB data read, read pointer at mark.
                        Assert.Equal(4, client.IOControl(IOControlCode.OobDataRead, null, siocatmarkResult));
                        Assert.Equal(1, BitConverter.ToInt32(siocatmarkResult, 0));
                    }
                }
            }
        }

        [Fact]
        public void IOControl_FIONBIO_Throws()
        {
            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                Assert.Throws<InvalidOperationException>(() => client.IOControl(unchecked((int)IOControlCode.NonBlockingIO), null, null));
                Assert.Throws<InvalidOperationException>(() => client.IOControl(IOControlCode.NonBlockingIO, null, null));
            }
        }

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [Fact]
        public void IOControl_UnknownValues_Unix_Throws()
        {
            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                foreach (IOControlCode code in Enum.GetValues(typeof(IOControlCode)))
                {
                    switch (code)
                    {
                        case IOControlCode.NonBlockingIO:
                        case IOControlCode.DataToRead:
                        case IOControlCode.OobDataRead:
                            // These three codes are currently enabled on Unix.
                            break;

                        default:
                            // The rest should throw PNSE.
                            Assert.Throws<PlatformNotSupportedException>(() => client.IOControl((int)code, null, null));
                            Assert.Throws<PlatformNotSupportedException>(() => client.IOControl(code, null, null));
                            break;
                    }
                }
            }
        }
    }
}
