// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Net.Test.Common;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public abstract class SslStreamDisposeTest
    {
        [Fact]
        public async Task DisposeAsync_NotConnected_ClosesStream()
        {
            bool disposed = false;
            var stream = new SslStream(new DelegateStream(disposeFunc: _ => disposed = true), false, delegate { return true; });

            Assert.False(disposed);
            await stream.DisposeAsync();
            Assert.True(disposed);
        }

        [Fact]
        public async Task DisposeAsync_Connected_ClosesStream()
        {
            (Stream stream1, Stream stream2) = TestHelper.GetConnectedStreams();
            var trackingStream1 = new CallTrackingStream(stream1);
            var trackingStream2 = new CallTrackingStream(stream2);

            var clientStream = new SslStream(trackingStream1, false, delegate { return true; });
            var serverStream = new SslStream(trackingStream2, false, delegate { return true; });

            using (X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate())
            {
                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                    clientStream.AuthenticateAsClientAsync(certificate.GetNameInfo(X509NameType.SimpleName, false)),
                    serverStream.AuthenticateAsServerAsync(certificate));
            }

            Assert.Equal(0, trackingStream1.TimesCalled(nameof(Stream.DisposeAsync)));
            await clientStream.DisposeAsync();
            Assert.NotEqual(0, trackingStream1.TimesCalled(nameof(Stream.DisposeAsync)));

            Assert.Equal(0, trackingStream2.TimesCalled(nameof(Stream.DisposeAsync)));
            await serverStream.DisposeAsync();
            Assert.NotEqual(0, trackingStream2.TimesCalled(nameof(Stream.DisposeAsync)));
        }
    }
}
