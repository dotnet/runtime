// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Connections.Tests
{
    internal class MockConnection : Connection
    {
        public Func<IConnectionProperties> OnConnectionProperties { get; set; }
        public Func<EndPoint> OnLocalEndPoint { get; set; }
        public Func<EndPoint> OnRemoteEndPoint { get; set; }
        public Func<ConnectionCloseMethod, CancellationToken, ValueTask> OnCloseAsyncCore { get; set; }
        public Func<IDuplexPipe> OnCreatePipe { get; set; }
        public Func<Stream> OnCreateStream { get; set; }

        public override IConnectionProperties ConnectionProperties => OnConnectionProperties();

        public override EndPoint LocalEndPoint => OnLocalEndPoint();

        public override EndPoint RemoteEndPoint => OnRemoteEndPoint();

        protected override ValueTask CloseAsyncCore(ConnectionCloseMethod method, CancellationToken cancellationToken) =>
            OnCloseAsyncCore(method, cancellationToken);

        protected override IDuplexPipe CreatePipe() => OnCreatePipe != null ? OnCreatePipe() : base.CreatePipe();

        protected override Stream CreateStream() => OnCreateStream != null ? OnCreateStream() : base.CreateStream();
    }
}
