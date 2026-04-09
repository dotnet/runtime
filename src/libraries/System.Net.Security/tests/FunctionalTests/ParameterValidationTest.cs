// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;


namespace System.Net.Security.Tests
{
    public class ParameterValidationTest
    {
        [Fact]
        public async Task SslStreamConstructor_BadEncryptionPolicy_ThrowException()
        {
            (NetworkStream clientStream, NetworkStream serverStream) = await TestHelper.GetConnectedTcpStreamsAsync();
            using (clientStream)
            using (serverStream)
            {
                AssertExtensions.Throws<ArgumentException>("encryptionPolicy", () =>
                {
                    SslStream sslStream = new SslStream(clientStream, false, TestHelper.AllowAnyServerCertificate, null, (EncryptionPolicy)100);
                });
            }
        }
    }
}
