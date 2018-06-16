// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Xunit;

namespace System.Net.Sockets.Tests
{
    public class KeepAliveTest
    {
        private const int RetryCount = 60;
        private const int Time = 5;
        private const int Interval = 2;

        private static bool IsUnixOrWindowsAtLeast1703 =>
            !PlatformDetection.IsWindows || PlatformDetection.IsWindows10Version1703OrGreater;

        private static bool IsWindowsBelow1703 =>
            PlatformDetection.IsWindows && !PlatformDetection.IsWindows10Version1703OrGreater;

        private static bool IsUnixOrWindowsAtLeast1709 =>
            !PlatformDetection.IsWindows || PlatformDetection.IsWindows10Version1709OrGreater;

        private static bool IsWindowsBelow1709 =>
            PlatformDetection.IsWindows && !PlatformDetection.IsWindows10Version1709OrGreater;

        [Fact]
        public void Socket_KeepAlive_Disabled_By_Default()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                Assert.Equal<int>(0, (int)socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive));
            }
        }

        [Fact]
        public void Socket_KeepAlive_Enable()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                Assert.NotEqual<int>(0, (int)socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive));
            }
        }

        [ConditionalFact(typeof(KeepAliveTest), nameof(IsUnixOrWindowsAtLeast1703))]
        public void Socket_Set_KeepAlive_RetryCount_Success()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, RetryCount);
                Assert.Equal<int>(RetryCount, (int)socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount));
            }
        }

        [ConditionalFact(typeof(KeepAliveTest), nameof(IsWindowsBelow1703))]
        public void Socket_Set_KeepAlive_RetryCount_Failure()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                Assert.Throws<SocketException>(() => socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, RetryCount));
                Assert.Throws<SocketException>(() => socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount));
            }
        }

        [Fact]
        public void Socket_Set_KeepAlive_Time_Success()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, Time);
                Assert.Equal<int>(Time, (int)socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime));
            }
        }

        [ConditionalFact(typeof(KeepAliveTest), nameof(IsUnixOrWindowsAtLeast1709))]
        public void Socket_Set_KeepAlive_Interval_Success()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, Interval);
                Assert.Equal<int>(Interval, (int)socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval));
            }
        }

        [ConditionalFact(typeof(KeepAliveTest), nameof(IsWindowsBelow1709))]
        public void Socket_Set_KeepAlive_Interval_Failure()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                Assert.Throws<SocketException>(() => socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, Interval));
                Assert.Throws<SocketException>(() => socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval));
            }
        }
    }
}