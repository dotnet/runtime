// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Security.Tests
{
    internal sealed class NotifyReadVirtualNetworkStream : VirtualNetworkStream
    {
        public delegate void ReadEventHandler(byte[] buffer, int offset, int count);
        public event ReadEventHandler OnRead;

        public NotifyReadVirtualNetworkStream(VirtualNetwork network, bool isServer)
            : base(network, isServer)
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            OnRead?.Invoke(buffer, offset, count);
            return base.Read(buffer, offset, count);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            OnRead?.Invoke(buffer, offset, count);
            return base.ReadAsync(buffer, offset, count, cancellationToken);
        }
    }
}
