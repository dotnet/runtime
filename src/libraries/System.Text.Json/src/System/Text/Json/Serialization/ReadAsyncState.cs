// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Threading;

namespace System.Text.Json.Serialization
{
    internal sealed class ReadAsyncState : IDisposable
    {
        public readonly CancellationToken CancellationToken;
        public byte[] Buffer;
        public int BytesInBuffer;
        public int ClearMax;
        public JsonConverter Converter;
        public bool IsFirstIteration;
        public JsonReaderState ReaderState;
        public ReadStack ReadStack;
        public JsonSerializerOptions Options;
        public long TotalBytesRead;

        public ReadAsyncState(Type returnType, JsonSerializerOptions? options = null, CancellationToken cancellationToken = default)
        {
            Options = options ??= JsonSerializerOptions.s_defaultOptions;
            Buffer = ArrayPool<byte>.Shared.Rent(Math.Max(Options.DefaultBufferSize, JsonConstants.Utf8Bom.Length));
            ReadStack.Initialize(returnType, Options, supportContinuation: true);
            Converter = ReadStack.Current.JsonPropertyInfo!.ConverterBase;
            ReaderState = new JsonReaderState(Options.GetReaderOptions());
            CancellationToken = cancellationToken;
            IsFirstIteration = true;
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Clear only what we used and return the buffer to the pool
                new Span<byte>(Buffer, 0, ClearMax).Clear();
                ArrayPool<byte>.Shared.Return(Buffer);
                Buffer = null!;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
