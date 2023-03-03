// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
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

        private JsonTypeInfo<Queue<T>>? _asuncEnumerableQueueTypeInfo;
        internal IAsyncEnumerable<T> DeserializeAsyncEnumerable(Stream utf8Json, CancellationToken cancellationToken)
        {
            Debug.Assert(IsConfigured);

            JsonTypeInfo<Queue<T>>? queueTypeInfo = _asuncEnumerableQueueTypeInfo;
            if (queueTypeInfo is null)
            {
                queueTypeInfo = JsonMetadataServices.CreateQueueInfo<Queue<T>, T>(
                    options: Options,
                    collectionInfo: new()
                    {
                        ObjectCreator = static () => new Queue<T>(),
                        ElementInfo = this,
                        NumberHandling = Options.NumberHandling
                    });

                queueTypeInfo.EnsureConfigured();
                _asuncEnumerableQueueTypeInfo = queueTypeInfo;
            }

            return CreateAsyncEnumerableDeserializer(utf8Json, queueTypeInfo, cancellationToken);

            static async IAsyncEnumerable<T> CreateAsyncEnumerableDeserializer(
                Stream utf8Json,
                JsonTypeInfo<Queue<T>> queueTypeInfo,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                Debug.Assert(queueTypeInfo.IsConfigured);
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

                        if (readStack.Current.ReturnValue is Queue<T> queue)
                        {
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

        internal sealed override IAsyncEnumerable<object?> DeserializeAsyncEnumerableAsObject(Stream utf8Json, CancellationToken cancellationToken)
        {
            IAsyncEnumerable<T> typedSource = DeserializeAsyncEnumerable(utf8Json, cancellationToken);
            return AsObjectEnumerable(typedSource, cancellationToken);

            static async IAsyncEnumerable<object?> AsObjectEnumerable(
                IAsyncEnumerable<T> source,
                [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await foreach (T elem in source.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    yield return elem;
                }
            }
        }

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
