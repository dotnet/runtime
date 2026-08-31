// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace System.IO.Tests
{
    public class UnidirectionalConnectedStreamsTests : ConnectedStreamConformanceTests
    {
        protected override int BufferedSize => StreamBuffer.DefaultMaxBufferSize;
        protected override bool FlushRequiredToWriteData => false;
        protected override bool BlocksOnZeroByteReads => true;

        protected override Task<StreamPair> CreateConnectedStreamsAsync() =>
            Task.FromResult<StreamPair>(ConnectedStreams.CreateUnidirectional());
    }

    public class BidirectionalConnectedStreamsTests : ConnectedStreamConformanceTests
    {
        protected override int BufferedSize => StreamBuffer.DefaultMaxBufferSize;
        protected override bool FlushRequiredToWriteData => false;
        protected override bool BlocksOnZeroByteReads => true;

        protected override Task<StreamPair> CreateConnectedStreamsAsync() =>
            Task.FromResult<StreamPair>(ConnectedStreams.CreateBidirectional());
    }
}
