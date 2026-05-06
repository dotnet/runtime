// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Tests;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipelines.Tests
{
    public class PipeReaderWriterStreamTests : ConnectedStreamConformanceTests
    {
        protected override Task<StreamPair> CreateConnectedStreamsAsync()
        {
            var p = new Pipe();
            return Task.FromResult<StreamPair>((p.Writer.AsStream(), p.Reader.AsStream()));
        }

        protected override bool BlocksOnZeroByteReads => true;
        protected override bool CansReturnFalseAfterDispose => false;
        protected override string ReadWriteOffsetName => null;
        protected override string ReadWriteCountName => null;
        protected override Type UnsupportedConcurrentExceptionType => null;

        [ActiveIssue("Implementation doesn't currently special-case Dispose, treating it instead as completion.")]
        public override Task Disposed_ThrowsObjectDisposedException() => base.Disposed_ThrowsObjectDisposedException();
    }
}
