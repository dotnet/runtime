// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Pipelines.Tests
{
    public class ThrowAfterNWritesStream : WriteOnlyStream
    {
        private readonly int _maxWrites;
        private int _writes;

        public int Writes => _writes;

        public ThrowAfterNWritesStream(int maxWrites)
        {
            _maxWrites = maxWrites;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException();
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (_writes >= _maxWrites)
            {
                throw new InvalidOperationException();
            }
            _writes++;
            return Task.CompletedTask;
        }

#if netcoreapp
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_writes >= _maxWrites)
            {
                throw new InvalidOperationException();
            }
            _writes++;
            return default;
        }
#endif
    }
}
