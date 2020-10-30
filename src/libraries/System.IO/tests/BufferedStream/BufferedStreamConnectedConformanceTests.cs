// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace System.IO.Tests
{
    public class BufferedStreamConnectedConformanceTests : WrappingConnectedStreamConformanceTests
    {
        protected override Task<StreamPair> CreateConnectedStreamsAsync() =>
            CreateWrappedConnectedStreamsAsync(ConnectedStreams.CreateUnidirectional(4096, 16384), leaveOpen: false);

        protected override Task<StreamPair> CreateWrappedConnectedStreamsAsync(StreamPair wrapped, bool leaveOpen = false)
        {
            var b1 = new BufferedStream(wrapped.Stream1, 1024);
            var b2 = new BufferedStream(wrapped.Stream2, 1024);
            return Task.FromResult<StreamPair>((b1, b2));
        }

        protected override bool SupportsLeaveOpen => false;
        protected override Type UnsupportedConcurrentExceptionType => null;
        protected override int BufferedSize => 1024 + 16384;
    }
}
