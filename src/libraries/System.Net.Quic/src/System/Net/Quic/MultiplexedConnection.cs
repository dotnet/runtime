// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    public class MultiplexedConnection : IDisposable
    {
        public virtual ValueTask ConnectAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public virtual ValueTask CloseAsync(long errorCode, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public virtual void Dispose() => throw new NotImplementedException();

        public virtual ValueTask<Stream> AcceptStreamAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public virtual int GetRemoteAvailableUnidirectionalStreamCount() => throw new NotImplementedException();
        public virtual int GetRemoteAvailableBidirectionalStreamCount() => throw new NotImplementedException();
        public virtual ValueTask WaitForAvailableUnidirectionalStreamsAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public virtual ValueTask WaitForAvailableBidirectionalStreamsAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public virtual Stream OpenUnidirectionalStream() => throw new NotImplementedException();
        public virtual Stream OpenBidirectionalStream() => throw new NotImplementedException();

        public virtual ValueTask<Stream> OpenStreamAsync(StreamType streamType, bool waitOnCapacity = false, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    public enum StreamType {
        Unidirectional,
        Bidirectional
    }
}