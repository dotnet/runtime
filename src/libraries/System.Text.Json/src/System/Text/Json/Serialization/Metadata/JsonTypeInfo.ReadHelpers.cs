// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace System.Text.Json.Serialization.Metadata
{
    public abstract partial class JsonTypeInfo
    {
        // Root-level async streaming deserialization is implemented here, on the non-generic base type.
        // Only the (small, synchronous) ContinueDeserializeAsObject step is specialized per type,
        // so no async state machine or ValueTask<T> is instantiated for every JsonTypeInfo<T> instantiation.

        // Reads (a slice of) the root value using the effective converter for the current type.
        // Must be equivalent to JsonTypeInfo<T>.ContinueDeserialize, but returns the value as a boxed object.
        internal abstract bool ContinueDeserializeAsObject(ref Utf8JsonReader reader, ref ReadStack state, out object? value);

        internal ValueTask<object?> DeserializeAsObjectAsync(Stream utf8Json, CancellationToken cancellationToken)
        {
            // Note: The ReadBufferState ctor rents pooled buffers.
            StreamReadBufferState bufferState = new(Options.DefaultBufferSize);
            return DeserializeAsObjectAsyncCore<StreamReadBufferState, Stream>(utf8Json, bufferState, cancellationToken);
        }

        internal ValueTask<object?> DeserializeAsObjectAsync(PipeReader utf8Json, CancellationToken cancellationToken)
        {
            PipeReadBufferState bufferState = new(utf8Json);
            return DeserializeAsObjectAsyncCore<PipeReadBufferState, PipeReader>(utf8Json, bufferState, cancellationToken);
        }

        private async ValueTask<object?> DeserializeAsObjectAsyncCore<TReadBufferState, TStream>(TStream utf8Json, TReadBufferState bufferState, CancellationToken cancellationToken)
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
                    bool success = ContinueDeserializeAsObjectCore<TReadBufferState, TStream>(
                        ref bufferState,
                        ref jsonReaderState,
                        ref readStack,
                        out object? value);

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

        private bool ContinueDeserializeAsObjectCore<TReadBufferState, TStream>(
            ref TReadBufferState bufferState,
            ref JsonReaderState jsonReaderState,
            ref ReadStack readStack,
            out object? value)
            where TReadBufferState : struct, IReadBufferState<TReadBufferState, TStream>
        {
            bufferState.GetReader(jsonReaderState, out Utf8JsonReader reader);

            try
            {
                bool success = ContinueDeserializeAsObject(ref reader, ref readStack, out value);

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
