// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Text.Json.Serialization
{
    // Common interface to help de-dupe code for different types that can do async serialization (Stream and PipeWriter)
    internal interface IAsyncSerializationBufferWriterContext : IDisposable
    {
        int FlushThreshold { get; }

        ValueTask FlushAsync(CancellationToken cancellationToken);

        public IBufferWriter<byte> BufferWriter { get; }
    }

    internal readonly struct AsyncSerializationStreamContext : IAsyncSerializationBufferWriterContext
    {
        private readonly Stream _stream;
        private readonly JsonSerializerOptions _options;
        private readonly PooledByteBufferWriter _bufferWriter;

        public AsyncSerializationStreamContext(Stream stream, JsonSerializerOptions options)
        {
            _stream = stream;
            _options = options;
            _bufferWriter = new PooledByteBufferWriter(_options.DefaultBufferSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async ValueTask FlushAsync(CancellationToken cancellationToken)
        {
            await _bufferWriter.WriteToStreamAsync(_stream, cancellationToken).ConfigureAwait(false);
            _bufferWriter.Clear();
        }

        public int FlushThreshold => (int)(_options.DefaultBufferSize * JsonSerializer.FlushThreshold);

        public IBufferWriter<byte> BufferWriter => _bufferWriter;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            _bufferWriter.Dispose();
        }
    }

    internal readonly struct AsyncSerializationPipeContext : IAsyncSerializationBufferWriterContext
    {
        private readonly PipeWriter _pipe;

        public AsyncSerializationPipeContext(PipeWriter pipe)
        {
            _pipe = pipe;
        }

        public int FlushThreshold => (int)((4 * PipeOptions.Default.MinimumSegmentSize) * JsonSerializer.FlushThreshold);

        public IBufferWriter<byte> BufferWriter => _pipe;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async ValueTask FlushAsync(CancellationToken cancellationToken)
        {
            FlushResult result = await _pipe.FlushAsync(cancellationToken).ConfigureAwait(false);
            if (result.IsCanceled || result.IsCompleted)
            {
                if (result.IsCanceled)
                {
                    ThrowHelper.ThrowOperationCanceledException_PipeWriteCanceled();
                }

                ThrowHelper.ThrowOperationCanceledException_PipeWriteCompleted();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() { }
    }
}
