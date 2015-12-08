// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Test.Common;
using System.Runtime.InteropServices;

using Xunit;

namespace System.Net.Sockets.Tests
{
    public class SocketOptionNameTest
    {
        private static bool IsWindows10Platform
        {
            get
            {
                return Capability.IsWindows10Platform();
            }
        }

        private static bool IsNotWindows10Platform
        {
            get
            {
                return !Capability.IsWindows10Platform();
            }
        }

        [ConditionalFact("IsNotWindows10Platform")]
        public void ReuseUnicastPort_CreateSocketGetOption_NotWindows10Platform_Throws()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.Throws<SocketException>(() =>
                    socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseUnicastPort));
            }
            else
            {
                Assert.Throws<PlatformNotSupportedException>(() =>
                    socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseUnicastPort));
            }
        }

        [ConditionalFact("IsWindows10Platform")]
        public void ReuseUnicastPort_CreateSocketGetOption_Windows10Platform_OptionIsZero()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            
            var optionValue = (int)socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseUnicastPort);
            Assert.Equal(0, optionValue);
        }

        [ConditionalFact("IsNotWindows10Platform")]
        public void ReuseUnicastPort_CreateSocketSetOption_NotWindows10Platform_Throws()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.Throws<SocketException>(() =>
                    socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseUnicastPort, 1));
            }
            else
            {
                Assert.Throws<PlatformNotSupportedException>(() =>
                    socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseUnicastPort, 1));
            }
        }

        // TODO: Issue #4887
        // The socket option 'ReuseUnicastPost' only works on Windows 10 systems. In addition, setting the option
        // is a no-op unless specialized network settings using PowerShell configuration are first applied to the
        // machine. This is currently difficult to test in the CI environment. So, this ests will be disabled for now
        [ActiveIssue(4887)]
        public void ReuseUnicastPort_CreateSocketSetOptionToOneAndGetOption_Windows10Platform_OptionIsOne()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseUnicastPort, 1);
            int optionValue = (int)socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseUnicastPort);
            Assert.Equal(1, optionValue);
        }
    }
}
