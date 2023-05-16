// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization.Converters;
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
            return EffectiveConverter.ReadCore(ref reader, Options, ref state);
        }

        internal async ValueTask<T?> DeserializeAsync(Stream utf8Json, CancellationToken cancellationToken)
        {
            Debug.Assert(IsConfigured);
            JsonSerializerOptions options = Options;
            var bufferState = new ReadBufferState(options.DefaultBufferSize);
            ReadStack readStack = default;
            readStack.Initialize(this, supportContinuation: true);
            var jsonReaderState = new JsonReaderState(options.GetReaderOptions());

            try
            {
                while (true)
                {
                    bufferState = await bufferState.ReadFromStreamAsync(utf8Json, cancellationToken).ConfigureAwait(false);
                    T? value = ContinueDeserialize(ref bufferState, ref jsonReaderState, ref readStack);

                    if (bufferState.IsFinalBlock)
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

        internal T? Deserialize(Stream utf8Json)
        {
            Debug.Assert(IsConfigured);
            JsonSerializerOptions options = Options;
            var bufferState = new ReadBufferState(options.DefaultBufferSize);
            ReadStack readStack = default;
            readStack.Initialize(this, supportContinuation: true);
            var jsonReaderState = new JsonReaderState(options.GetReaderOptions());

            try
            {
                while (true)
                {
                    bufferState.ReadFromStream(utf8Json);
                    T? value = ContinueDeserialize(ref bufferState, ref jsonReaderState, ref readStack);

                    if (bufferState.IsFinalBlock)
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
        /// Creating a queue JsonTypeInfo from within the DeserializeAsyncEnumerable method
        /// triggers generic recursion warnings from the AOT compiler so we instead
        /// have the caller do it for us externally (cf. https://github.com/dotnet/runtime/issues/85184)
        /// </summary>
        internal JsonTypeInfo<Queue<T>>? _asyncEnumerableQueueTypeInfo;

        internal async IAsyncEnumerable<T> DeserializeAsyncEnumerable(Stream utf8Json, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            Debug.Assert(_asyncEnumerableQueueTypeInfo?.IsConfigured == true, "must be populated before calling the method.");
            JsonTypeInfo<Queue<T>> queueTypeInfo = _asyncEnumerableQueueTypeInfo;
            JsonSerializerOptions options = queueTypeInfo.Options;
            var bufferState = new ReadBufferState(options.DefaultBufferSize);
            ReadStack readStack = default;
            readStack.Initialize(queueTypeInfo, supportContinuation: true);

            var jsonReaderState = new JsonReaderState(options.GetReaderOptions());

            try
            {
                do
                {
                    bufferState = await bufferState.ReadFromStreamAsync(utf8Json, cancellationToken, fillBuffer: false).ConfigureAwait(false);
                    queueTypeInfo.ContinueDeserialize(
                        ref bufferState,
                        ref jsonReaderState,
                        ref readStack);

                    if (readStack.Current.ReturnValue is { } returnValue)
                    {
                        var queue = (Queue<T>)returnValue!;
                        while (queue.Count > 0)
                        {
                            yield return queue.Dequeue();
                        }
                    }
                }
                while (!bufferState.IsFinalBlock);
            }
            finally
            {
                bufferState.Dispose();
            }
        }

        internal sealed override object? DeserializeAsObject(ref Utf8JsonReader reader, ref ReadStack state)
            => Deserialize(ref reader, ref state);

        internal sealed override async ValueTask<object?> DeserializeAsObjectAsync(Stream utf8Json, CancellationToken cancellationToken)
        {
            T? result = await DeserializeAsync(utf8Json, cancellationToken).ConfigureAwait(false);
            return result;
        }

        internal sealed override object? DeserializeAsObject(Stream utf8Json)
            => Deserialize(utf8Json);

        private T? ContinueDeserialize(
            ref ReadBufferState bufferState,
            ref JsonReaderState jsonReaderState,
            ref ReadStack readStack)
        {
            var reader = new Utf8JsonReader(bufferState.Bytes, bufferState.IsFinalBlock, jsonReaderState);

            // If we haven't read in the entire stream's payload we'll need to signify that we want
            // to enable read ahead behaviors to ensure we have complete json objects and arrays
            // ({}, []) when needed. (Notably to successfully parse JsonElement via JsonDocument
            // to assign to object and JsonElement properties in the constructed .NET object.)
            readStack.ReadAhead = !bufferState.IsFinalBlock;
            readStack.BytesConsumed = 0;

            T? value = EffectiveConverter.ReadCore(ref reader, Options, ref readStack);
            Debug.Assert(readStack.BytesConsumed <= bufferState.Bytes.Length);
            bufferState.AdvanceBuffer((int)readStack.BytesConsumed);
            jsonReaderState = reader.CurrentState;
            return value;
        }
    }
}
