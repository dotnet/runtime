// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression.Tests
{
    public partial class ManualSyncMemoryStream : MemoryStream
    {
        public override async ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken)
        {
            ReadHit = true;

            if (isSync)
            {
                manualResetEvent.Wait(cancellationToken);
            }
            else
            {
                await Task.Run(() => manualResetEvent.Wait(cancellationToken));
            }

            return await base.ReadAsync(destination, cancellationToken);
        }

        public override async Task WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken)
        {
            WriteHit = true;

            if (isSync)
            {
                manualResetEvent.Wait(cancellationToken);
            }
            else
            {
                await Task.Run(() => manualResetEvent.Wait(cancellationToken));
            }

            await base.WriteAsync(source, cancellationToken);
        }
    }
}
