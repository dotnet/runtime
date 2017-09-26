// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression.Tests
{
    public partial class ManualSyncMemoryStream : MemoryStream
    {
        public override ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken)
        {
            ReadHit = true;
            return base.ReadAsync(destination, cancellationToken);
        }

        public override Task WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken)
        {
            WriteHit = true;
            return base.WriteAsync(source, cancellationToken);
        }
    }
}
