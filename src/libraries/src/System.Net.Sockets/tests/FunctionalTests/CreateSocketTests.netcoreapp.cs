// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Net.Sockets.Tests
{
    public partial class CreateSocket
    {
        [Theory]
        [InlineData(AddressFamily.Netlink)]
        [InlineData(AddressFamily.Packet)]
        [InlineData(AddressFamily.ControllerAreaNetwork)]
        [PlatformSpecific(~TestPlatforms.Linux)]
        public void Ctor_Netcoreapp_Throws(AddressFamily addressFamily)
        {
            // All protocols are Linux specific and throw on other platforms
            Assert.Throws<SocketException>(() => new Socket(addressFamily, SocketType.Raw, 0));
        }

        [Theory]
        [InlineData(AddressFamily.Netlink)]
        [InlineData(AddressFamily.Packet)]
        [InlineData(AddressFamily.ControllerAreaNetwork)]
        [PlatformSpecific(TestPlatforms.Linux)]
        public void Ctor_Netcoreapp_Success(AddressFamily addressFamily)
        {
            Socket s = null;
            try
            {
                s = new Socket(addressFamily, SocketType.Raw, ProtocolType.Raw);
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.AccessDenied ||
                                            e.SocketErrorCode == SocketError.ProtocolNotSupported ||
                                            e.SocketErrorCode == SocketError.AddressFamilyNotSupported)
            {
                // Ignore. We may not have privilege or protocol modules are not loaded.
                return;
            }
            s.Close();
        }
    }
}
