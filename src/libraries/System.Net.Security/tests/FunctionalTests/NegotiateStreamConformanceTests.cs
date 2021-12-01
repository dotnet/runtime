// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.IO.Tests;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Security.Tests
{
    [PlatformSpecific(TestPlatforms.Windows)] // NegotiateStream client needs explicit credentials or SPNs on unix.
    public sealed class NegotiateStreamMemoryConformanceTests : WrappingConnectedStreamConformanceTests
    {
        protected override bool UsableAfterCanceledReads => false;
        protected override bool BlocksOnZeroByteReads => true;
        protected override Type UnsupportedConcurrentExceptionType => typeof(NotSupportedException);

        protected override Task<StreamPair> CreateConnectedStreamsAsync() =>
            CreateWrappedConnectedStreamsAsync(ConnectedStreams.CreateBidirectional(), leaveOpen: false);

        protected override async Task<StreamPair> CreateWrappedConnectedStreamsAsync(StreamPair wrapped, bool leaveOpen)
        {
            var negotiate1 = new NegotiateStream(wrapped.Stream1, leaveOpen);
            var negotiate2 = new NegotiateStream(wrapped.Stream2, leaveOpen);

            await Task.WhenAll(negotiate1.AuthenticateAsClientAsync(), negotiate2.AuthenticateAsServerAsync());

            return new StreamPair(negotiate1, negotiate2);
        }
    }
}
