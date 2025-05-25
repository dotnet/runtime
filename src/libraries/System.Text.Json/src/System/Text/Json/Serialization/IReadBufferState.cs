// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace System.Text.Json.Serialization
{
    internal interface IReadBufferState<TReadBufferState> : IDisposable
        where TReadBufferState : struct, IReadBufferState<TReadBufferState>
    {
        public abstract bool IsFinalBlock { get; }

        public abstract ReadOnlySequence<byte> Bytes { get; }

        public abstract ValueTask<TReadBufferState> ReadAsync(CancellationToken cancellationToken,
            bool fillBuffer = true);

        public abstract void Read();

        public abstract void Advance(int bytesConsumed);
    }
}
