// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using Xunit;

namespace System.Net.Sockets.Tests
{
    public class SocketInformationTest
    {
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
