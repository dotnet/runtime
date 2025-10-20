// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace System.Text.Json.Serialization.Metadata
{
    public partial class JsonTypeInfo<T>
    {
        // This section provides helper methods guiding root-level deserialization
        // of values corresponding according to the current JsonTypeInfo configuration.

        internal T? Deserialize(ref Utf8JsonReader reader, ref ReadStack state)
        {
            Debug.Assert(IsConfigured);
            bool success = EffectiveConverter.ReadCore(ref reader, out T? result, Options, ref state);
            Debug.Assert(success, "Should only return false for async deserialization");
            return result;
        }

        internal async ValueTask<T?> DeserializeAsync<TReadBufferState, TStream>(TStream utf8Json, TReadBufferState bufferState, CancellationToken cancellationToken)
            where TReadBufferState : struct, IReadBufferState<TReadBufferState, TStream>
        {
            Debug.Assert(IsConfigured);
            JsonSerializerOptions options = Options;
            ReadStack readStack = default;
            readStack.Initialize(this, supportContinuation: true);
            var jsonReaderState = new JsonReaderState(options.GetReaderOptions());

            try
            {
                while (true)
                {
                    bufferState = await bufferState.ReadAsync(utf8Json, cancellationToken).ConfigureAwait(false);
                    bool success = ContinueDeserialize<TReadBufferState, TStream>(
                        ref bufferState,
                        ref jsonReaderState,
                        ref readStack,
                        out T? value);

                    if (success)
                    {
                        return value;
                    }
                }
            }
            finally
            {
                bufferState.Dispose();
            }
        }

        internal ValueTask<T?> DeserializeAsync(Stream utf8Json, CancellationToken cancellationToken)
        {
            // Note: The ReadBufferState ctor rents pooled buffers.
            StreamReadBufferState bufferState = new StreamReadBufferState(Options.DefaultBufferSize);
            return DeserializeAsync(utf8Json, bufferState, cancellationToken);
        }

        internal ValueTask<T?> DeserializeAsync(PipeReader utf8Json, CancellationToken cancellationToken)
        {
            PipeReadBufferState bufferState = new(utf8Json);
            return DeserializeAsync(utf8Json, bufferState, cancellationToken);
        }

        internal T? Deserialize(Stream utf8Json)
        {
            Debug.Assert(IsConfigured);
            JsonSerializerOptions options = Options;
            ReadStack readStack = default;
            readStack.Initialize(this, supportContinuation: true);
            var jsonReaderState = new JsonReaderState(options.GetReaderOptions());
            // Note: The ReadBufferState ctor rents pooled buffers.
            StreamReadBufferState bufferState = new StreamReadBufferState(options.DefaultBufferSize);

            try
            {
                while (true)
                {
                    bufferState.Read(utf8Json);
                    bool success = ContinueDeserialize<StreamReadBufferState, Stream>(
                        ref bufferState,
                        ref jsonReaderState,
                        ref readStack,
                        out T? value);

                    if (success)
                    {
                        return value;
                    }
                }
            }
            finally
            {
                bufferState.Dispose();
            }
        }

        /// <summary>
        /// Caches JsonTypeInfo&lt;List&lt;T&gt;&gt; instances used by the DeserializeAsyncEnumerable method.
        /// Store as a non-generic type to avoid triggering generic recursion in the AOT compiler.
        /// cf. https://github.com/dotnet/runtime/issues/85184
        /// </summary>
        internal JsonTypeInfo? _asyncEnumerableArrayTypeInfo;
        internal JsonTypeInfo? _asyncEnumerableRootLevelValueTypeInfo;

        internal sealed override object? DeserializeAsObject(ref Utf8JsonReader reader, ref ReadStack state)
            => Deserialize(ref reader, ref state);

        internal sealed override async ValueTask<object?> DeserializeAsObjectAsync(Stream utf8Json, CancellationToken cancellationToken)
        {
            // Note: The ReadBufferState ctor rents pooled buffers.
            StreamReadBufferState bufferState = new StreamReadBufferState(Options.DefaultBufferSize);
            T? result = await DeserializeAsync(utf8Json, bufferState, cancellationToken).ConfigureAwait(false);
            return result;
        }

        internal sealed override async ValueTask<object?> DeserializeAsObjectAsync(PipeReader utf8Json, CancellationToken cancellationToken)
        {
            T? result = await DeserializeAsync<PipeReadBufferState, PipeReader>(utf8Json, bufferState: new PipeReadBufferState(utf8Json), cancellationToken).ConfigureAwait(false);
            return result;
        }

        internal sealed override object? DeserializeAsObject(Stream utf8Json)
            => Deserialize(utf8Json);

        internal bool ContinueDeserialize<TReadBufferState, TStream>(
            ref TReadBufferState bufferState,
            ref JsonReaderState jsonReaderState,
            ref ReadStack readStack,
            out T? value)
            where TReadBufferState : struct, IReadBufferState<TReadBufferState, TStream>
        {
            bufferState.GetReader(jsonReaderState, out Utf8JsonReader reader);

            try
            {
                bool success = EffectiveConverter.ReadCore(ref reader, out value, Options, ref readStack);

#if DEBUG
                Debug.Assert(reader.BytesConsumed <= bufferState.Bytes.Length);
                Debug.Assert(!bufferState.IsFinalBlock || reader.AllowMultipleValues || reader.BytesConsumed == bufferState.Bytes.Length,
                    "The reader should have thrown if we have remaining bytes.");
#endif

                jsonReaderState = reader.CurrentState;
                return success;
            }
            finally
            {
                bufferState.Advance(reader.BytesConsumed);
            }
        }
    }
}
