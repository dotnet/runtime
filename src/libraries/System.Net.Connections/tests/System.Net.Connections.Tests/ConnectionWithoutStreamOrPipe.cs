// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Connections.Tests
{
    internal class ConnectionWithoutStreamOrPipe : Connection
    {
        public override IConnectionProperties ConnectionProperties => throw new NotImplementedException();

        public override EndPoint LocalEndPoint => throw new NotImplementedException();

        public override EndPoint RemoteEndPoint => throw new NotImplementedException();

        protected override ValueTask CloseAsyncCore(ConnectionCloseMethod method, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
