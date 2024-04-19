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
        /// Caches a JsonTypeInfo&lt;Queue&lt;T&gt;&gt; instance used by the DeserializeAsyncEnumerable method.
        /// Store as a non-generic type to avoid triggering generic recursion in the AOT compiler.
        /// cf. https://github.com/dotnet/runtime/issues/85184
        /// </summary>
        internal JsonTypeInfo? _asyncEnumerableQueueTypeInfo;

        internal sealed override object? DeserializeAsObject(ref Utf8JsonReader reader, ref ReadStack state)
            => Deserialize(ref reader, ref state);

        internal sealed override async ValueTask<object?> DeserializeAsObjectAsync(Stream utf8Json, CancellationToken cancellationToken)
        {
            T? result = await DeserializeAsync(utf8Json, cancellationToken).ConfigureAwait(false);
            return result;
        }

        internal sealed override object? DeserializeAsObject(Stream utf8Json)
            => Deserialize(utf8Json);

        internal T? ContinueDeserialize(
            ref ReadBufferState bufferState,
            ref JsonReaderState jsonReaderState,
            ref ReadStack readStack)
        {
            var reader = new Utf8JsonReader(bufferState.Bytes, bufferState.IsFinalBlock, jsonReaderState);

            T? value = EffectiveConverter.ReadCore(ref reader, Options, ref readStack);
            Debug.Assert(reader.BytesConsumed <= bufferState.Bytes.Length);
            bufferState.AdvanceBuffer((int)reader.BytesConsumed);
            jsonReaderState = reader.CurrentState;
            return value;
        }
    }
}
