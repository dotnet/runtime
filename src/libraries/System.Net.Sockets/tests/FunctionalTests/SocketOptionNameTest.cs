// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Test.Common;

using Xunit;

namespace System.Net.Sockets.Tests
{
    public class SocketOptionNameTest
    {
        // TODO: Issue #4887
        // The socket option 'ReuseUnicastPost' only works on Windows 10 systems. In addition, setting the option
        // is a no-op unless specialized network settings using PowerShell configuration are first applied to the
        // machine. This is currently difficult to test in the CI environment. So, some of these tests will be
        // disabled for now.
        private static bool GetReuseUnicastPortSettingSupported
        {
            get
            {
                Version v = WindowsOSVersionHelper.GetVersion();
                return (v.Major == 10);
            }
        }

        private static bool SetReuseUnicastPortSettingSupported
        {
            get
            {
                return false; // TODO: Issue #4887
            }
        }

        [ConditionalFact("GetReuseUnicastPortSettingSupported")]
        public void ReuseUnicastPort_CreateSocketGetOption_OptionIsZero()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var optionValue = (int)socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseUnicastPort);

            Assert.Equal(0, optionValue);
        }

        [ConditionalFact("SetReuseUnicastPortSettingSupported")]
        public void ReuseUnicastPort_CreateSocketSetOptionToOneAndGetOption_OptionIsOne()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseUnicastPort, 1);
            int optionValue = (int)socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseUnicastPort);

            Assert.Equal(1, optionValue);
        }
    }
}
