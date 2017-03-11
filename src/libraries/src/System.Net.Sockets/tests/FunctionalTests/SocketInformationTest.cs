// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using Xunit;

namespace System.Net.Sockets.Tests
{
    public class SocketInformationTest
    {
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        [Fact]
        public void Socket_Ctor_DuplicateAndClose_Throw()
        {
            Assert.Throws<PlatformNotSupportedException>(() => new Socket(new SocketInformation()));
            using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                Assert.Throws<PlatformNotSupportedException>(() => s.DuplicateAndClose(0));
            }
        }

        [Fact]
        public void Properties_Roundtrip()
        {
            SocketInformation si = default(SocketInformation);

            Assert.Equal((SocketInformationOptions)0, si.Options);
            si.Options = SocketInformationOptions.Listening | SocketInformationOptions.NonBlocking;
            Assert.Equal(SocketInformationOptions.Listening | SocketInformationOptions.NonBlocking, si.Options);

            Assert.Null(si.ProtocolInformation);
            byte[] data = new byte[1];
            si.ProtocolInformation = data;
            Assert.Same(data, si.ProtocolInformation);
        }
    }
}
