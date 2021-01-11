// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace System.IO.Tests
{
    public class BufferedStreamStandaloneConformanceTests : StandaloneStreamConformanceTests
    {
        private const int BufferSize = 4;

        protected override Task<Stream> CreateReadOnlyStreamCore(byte[] initialData) =>
            Task.FromResult<Stream>(new BufferedStream(new MemoryStream(initialData ?? Array.Empty<byte>(), writable: false), BufferSize));

        protected override Task<Stream> CreateReadWriteStreamCore(byte[] initialData) =>
            Task.FromResult<Stream>(
                initialData != null ? new BufferedStream(new MemoryStream(initialData), BufferSize) :
                new BufferedStream(new MemoryStream(), BufferSize));

        protected override Task<Stream> CreateWriteOnlyStreamCore(byte[] initialData) =>
            Task.FromResult<Stream>(null);
    }

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

        protected override int BufferedSize => 1024 + 16384;
        protected override bool SupportsLeaveOpen => false;
        protected override Type UnsupportedConcurrentExceptionType => null;
    }
}
