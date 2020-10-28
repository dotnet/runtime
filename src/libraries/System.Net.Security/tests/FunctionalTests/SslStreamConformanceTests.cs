// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.IO.Tests;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace System.Net.Security.Tests
{
    public abstract class SslStreamConformanceTests : WrappingConnectedStreamConformanceTests
    {
        protected override bool UsableAfterCanceledReads => false;
        protected override bool BlocksOnZeroByteReads => true;
        protected override Type UnsupportedConcurrentExceptionType => typeof(NotSupportedException);

        protected override async Task<StreamPair> CreateWrappedConnectedStreamsAsync(StreamPair wrapped, bool leaveOpen = false)
        {
            X509Certificate2? cert = Test.Common.Configuration.Certificates.GetServerCertificate();
            var ssl1 = new SslStream(wrapped.Stream1, leaveOpen, delegate { return true; });
            var ssl2 = new SslStream(wrapped.Stream2, leaveOpen, delegate { return true; });

            await new[]
            {
                ssl1.AuthenticateAsClientAsync(cert.GetNameInfo(X509NameType.SimpleName, false)),
                ssl2.AuthenticateAsServerAsync(cert, false, false)
            }.WhenAllOrAnyFailed().ConfigureAwait(false);

            return new StreamPair(ssl1, ssl2);
        }
    }

    public sealed class SslStreamMemoryConformanceTests : SslStreamConformanceTests
    {
        protected override Task<StreamPair> CreateConnectedStreamsAsync() =>
            CreateWrappedConnectedStreamsAsync(ConnectedStreams.CreateBidirectional());
    }

    public sealed class SslStreamNetworkConformanceTests : SslStreamConformanceTests
    {
        protected override bool CanTimeout => true;

        protected override Task<StreamPair> CreateConnectedStreamsAsync() =>
            CreateWrappedConnectedStreamsAsync(TestHelper.GetConnectedTcpStreams());
    }
}
