// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace System.Text.Json.Serialization
{
    internal interface IReadBufferState<TReadBufferState, TStream> : IDisposable
        where TReadBufferState : struct, IReadBufferState<TReadBufferState, TStream>
    {
        public abstract bool IsFinalBlock { get; }

#if DEBUG
        // Used for Debug.Assert's
        public abstract ReadOnlySequence<byte> Bytes { get; }
#endif

        public abstract ValueTask<TReadBufferState> ReadAsync(
            TStream utf8Json,
            CancellationToken cancellationToken,
            bool fillBuffer = true);

        public abstract void Read(TStream utf8Json);

        public abstract void Advance(long bytesConsumed);

        // This would normally be implemented as returning a Utf8JsonReader, but this pattern hits a limitation
        // in mono aot that is not trivial to fix. For now use the alternative pattern of returning via an out
        // argument. Tracking issue: https://github.com/dotnet/runtime/issues/118697
        public abstract void GetReader(JsonReaderState jsonReaderState, out Utf8JsonReader reader);
    }
}
