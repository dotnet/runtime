// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression
{
    internal sealed partial class PositionPreservingWriteOnlyStreamWrapper : Stream
    {
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            _position += buffer.Length;
            _stream.Write(buffer);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
        {
            _position += buffer.Length;
            return _stream.WriteAsync(buffer, cancellationToken);
        }
    }
}
