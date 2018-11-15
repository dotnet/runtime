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
        private const int WindowsDefaultTime = 7200;
        private const int WindowsDefaultInterval = 1;

        private static bool IsUnixOrWindowsAtLeast1703 =>
            !PlatformDetection.IsWindows || PlatformDetection.IsWindows10Version1703OrGreater;

        private static bool IsWindowsBelow1703 =>
            PlatformDetection.IsWindows && !PlatformDetection.IsWindows10Version1703OrGreater;

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

        [ConditionalFact(typeof(KeepAliveTest), nameof(IsUnixOrWindowsAtLeast1703))] // RetryCount not supported by earlier versions of Windows
        public void Socket_KeepAlive_RetryCount_Success()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, RetryCount);
                Assert.Equal<int>(RetryCount, (int)socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount));
            }
        }

        [ConditionalFact(typeof(KeepAliveTest), nameof(IsWindowsBelow1703))] // RetryCount not supported by earlier versions of Windows
        public void Socket_KeepAlive_RetryCount_Failure()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                Assert.Throws<SocketException>(() => socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, RetryCount));
                Assert.Throws<SocketException>(() => socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount));
            }
        }

        [Fact]
        public void Socket_KeepAlive_Time()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, Time);
                Assert.Equal<int>(Time, (int)socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime));
            }
        }

        [Fact]
        public void Socket_KeepAlive_Interval()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, Interval);
                Assert.Equal<int>(Interval, (int)socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval));
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)] // relies on Windows defaults
        [Fact]
        public void Socket_KeepAlive_Time_And_Interval()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                Assert.Equal<int>(WindowsDefaultTime, (int)socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime));
                Assert.Equal<int>(WindowsDefaultInterval, (int)socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval));

                socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, Time);
                Assert.Equal<int>(Time, (int)socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime));
                Assert.Equal<int>(WindowsDefaultInterval, (int)socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval));
        
                socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, Interval);
                Assert.Equal<int>(Time, (int)socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime));
                Assert.Equal<int>(Interval, (int)socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval));
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)] // relies on Windows defaults
        [Fact]
        public void Socket_KeepAlive_Interval_And_Time()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                Assert.Equal<int>(WindowsDefaultTime, (int)socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime));
                Assert.Equal<int>(WindowsDefaultInterval, (int)socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval));

                socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, Interval);
                Assert.Equal<int>(WindowsDefaultTime, (int)socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime));
                Assert.Equal<int>(Interval, (int)socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval));

                socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, Time);
                Assert.Equal<int>(Time, (int)socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime));
                Assert.Equal<int>(Interval, (int)socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval));
            }
        }

        [Fact]
        public void Socket_Get_KeepAlive_Time_AsByteArray_OptionLengthZero_Failure()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                if (PlatformDetection.IsWindows)
                {
                    Assert.Throws<SocketException>(() => socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, 0));
                }
                else
                {
                    // Unix's getsockopt is a nop when the buffer is of insufficient length
                    socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, 0);
                }
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData(new byte[0])]
        [InlineData(new byte[3] { 0, 0, 0 })]
        public void Socket_Get_KeepAlive_Time_AsByteArray_BufferNullOrTooSmall_Failure(byte[] buffer)
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                if (PlatformDetection.IsWindows)
                {
                    Assert.Throws<SocketException>(() => socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, buffer));
                }
                else
                {
                    // Unix's getsockopt is a nop when the buffer is of insufficient length
                    socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, buffer);
                }
            }
        }

        [Fact]
        public void Socket_Set_KeepAlive_Time_AsByteArray_BufferNull_Failure()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                byte[] bufferNull = null;
                Assert.Throws<SocketException>(() => socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, bufferNull));
            }
        }

        [Fact]
        public void Socket_Set_KeepAlive_Time_AsByteArray_BufferLengthZero_Failure()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                byte[] bufferLengthZero = new byte[0];
                Assert.Throws<SocketException>(() => socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, bufferLengthZero));
            }
        }

        [Fact]
        public void Socket_Set_KeepAlive_Time_AsByteArray_BufferShort_Failure()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                byte[] bufferShort = new byte[1];
                Assert.Throws<SocketException>(() => socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, bufferShort));
            }
        }

        [Fact]
        public void Socket_KeepAlive_Time_AsByteArray_Success()
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, BitConverter.GetBytes(Time));
                Assert.Equal<int>(Time, BitConverter.ToInt32(socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, sizeof(int)), 0));
            }
        }
    }
}
