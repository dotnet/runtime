// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Text.Json.Serialization.Converters;
using System.Threading;
using System.Threading.Tasks;

namespace System.Text.Json.Serialization.Metadata
{
    public partial class JsonTypeInfo<T>
    {
        // This section provides helper methods guiding root-level serialization
        // of values corresponding according to the current JsonTypeInfo configuration.

        // Root serialization method for sync, non-streaming serialization
        internal void Serialize(
            Utf8JsonWriter writer,
            in T? rootValue,
            object? rootValueBoxed = null)
        {
            Debug.Assert(IsConfigured);
            Debug.Assert(rootValueBoxed is null || rootValueBoxed is T);

            if (CanUseSerializeHandler)
            {
                // Short-circuit calls into SerializeHandler, if supported.
                // Even though this is already handled by JsonMetadataServicesConverter,
                // this avoids creating a WriteStack and calling into the converter infrastructure.

                Debug.Assert(SerializeHandler != null);
                Debug.Assert(Converter is JsonMetadataServicesConverter<T>);

                SerializeHandler(writer, rootValue!);
                writer.Flush();
            }
            else if (
#if NET
                !typeof(T).IsValueType &&
#endif
                Converter.CanBePolymorphic &&
                rootValue is not null &&
                Options.TryGetPolymorphicTypeInfoForRootType(rootValue, out JsonTypeInfo? derivedTypeInfo))
            {
                Debug.Assert(typeof(T) == typeof(object));
                derivedTypeInfo.SerializeAsObject(writer, rootValue);
                // NB flushing is handled by the derived type's serialization method.
            }
            else
            {
                WriteStack state = default;
                state.Initialize(this, rootValueBoxed);

                bool success = EffectiveConverter.WriteCore(writer, rootValue, Options, ref state);
                Debug.Assert(success);
                writer.Flush();
            }
        }

        internal Task SerializeAsync(Stream utf8Json,
            T? rootValue,
            CancellationToken cancellationToken,
            object? rootValueBoxed = null)
        {
            // Value chosen as 90% of the default buffer used in PooledByteBufferWriter.
            // This is a tradeoff between likelihood of needing to grow the array vs. utilizing most of the buffer
            int flushThreshold = (int)(Options.DefaultBufferSize * JsonSerializer.FlushThreshold);
            return SerializeAsync(new PooledByteBufferWriter(Options.DefaultBufferSize, utf8Json), rootValue, flushThreshold, cancellationToken, rootValueBoxed);
        }

        internal Task SerializeAsync(PipeWriter utf8Json,
            T? rootValue,
            CancellationToken cancellationToken,
            object? rootValueBoxed = null)
        {
            // Value chosen as 90% of 4 buffer segments in Pipes. This is semi-arbitrarily chosen and may be changed in future iterations.
            int flushThreshold = (int)(4 * PipeOptions.Default.MinimumSegmentSize * JsonSerializer.FlushThreshold);
            return SerializeAsync(utf8Json, rootValue, flushThreshold, cancellationToken, rootValueBoxed);
        }

        // Root serialization method for async streaming serialization.
        private async Task SerializeAsync(
            PipeWriter pipeWriter,
            T? rootValue,
            int flushThreshold,
            CancellationToken cancellationToken,
            object? rootValueBoxed = null)
        {
            Debug.Assert(IsConfigured);
            Debug.Assert(rootValueBoxed is null || rootValueBoxed is T);

            if (CanUseSerializeHandlerInStreaming)
            {
                // Short-circuit calls into SerializeHandler, if the `CanUseSerializeHandlerInStreaming` heuristic allows it.

                Debug.Assert(SerializeHandler != null);
                Debug.Assert(CanUseSerializeHandler);
                Debug.Assert(Converter is JsonMetadataServicesConverter<T>);

                Utf8JsonWriter writer = Utf8JsonWriterCache.RentWriter(Options, pipeWriter);

                try
                {
                    try
                    {
                        SerializeHandler(writer, rootValue!);
                        writer.Flush();
                    }
                    finally
                    {
                        // Record the serialization size in both successful and failed operations,
                        // since we want to immediately opt out of the fast path if it exceeds the threshold.
                        OnRootLevelAsyncSerializationCompleted(writer.BytesCommitted + writer.BytesPending);

                        Utf8JsonWriterCache.ReturnWriter(writer);
                    }

                    FlushResult result = await pipeWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
                    if (result.IsCanceled)
                    {
                        ThrowHelper.ThrowOperationCanceledException_PipeWriteCanceled();
                    }
                }
                finally
                {
                    if (pipeWriter is PooledByteBufferWriter disposable)
                    {
                        disposable.Dispose();
                    }
                }
            }
            else if (
#if NET
                !typeof(T).IsValueType &&
#endif
                Converter.CanBePolymorphic &&
                rootValue is not null &&
                Options.TryGetPolymorphicTypeInfoForRootType(rootValue, out JsonTypeInfo? derivedTypeInfo))
            {
                Debug.Assert(typeof(T) == typeof(object));
                await derivedTypeInfo.SerializeAsObjectAsync(pipeWriter, rootValue, flushThreshold, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                bool isFinalBlock;
                WriteStack state = default;
                state.Initialize(this,
                    rootValueBoxed,
                    supportContinuation: true,
                    supportAsync: true);

                if (!pipeWriter.CanGetUnflushedBytes)
                {
                    ThrowHelper.ThrowInvalidOperationException_PipeWriterDoesNotImplementUnflushedBytes(pipeWriter);
                }
                state.PipeWriter = pipeWriter;
                state.CancellationToken = cancellationToken;

                var writer = new Utf8JsonWriter(pipeWriter, Options.GetWriterOptions());

                try
                {
                    state.FlushThreshold = flushThreshold;

                    do
                    {
                        try
                        {
                            isFinalBlock = EffectiveConverter.WriteCore(writer, rootValue, Options, ref state);
                            writer.Flush();

                            if (state.SuppressFlush)
                            {
                                Debug.Assert(!isFinalBlock);
                                Debug.Assert(state.PendingTask is not null);
                                state.SuppressFlush = false;
                            }
                            else
                            {
                                FlushResult result = await pipeWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
                                if (result.IsCanceled || result.IsCompleted)
                                {
                                    if (result.IsCanceled)
                                    {
                                        ThrowHelper.ThrowOperationCanceledException_PipeWriteCanceled();
                                    }

                                    // Pipe is completed, no one is reading so no point in continuing serialization
                                    return;
                                }
                            }
                        }
                        finally
                        {
                            // Await any pending resumable converter tasks (currently these can only be IAsyncEnumerator.MoveNextAsync() tasks).
                            // Note that pending tasks are always awaited, even if an exception has been thrown or the cancellation token has fired.
                            if (state.PendingTask is not null)
                            {
                                // Exceptions should only be propagated by the resuming converter
#if NET8_0_OR_GREATER
                                await state.PendingTask.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
#else
                                try
                                {
                                    await state.PendingTask.ConfigureAwait(false);
                                }
                                catch { }
#endif
                            }

                            // Dispose any pending async disposables (currently these can only be completed IAsyncEnumerators).
                            if (state.CompletedAsyncDisposables?.Count > 0)
                            {
                                await state.DisposeCompletedAsyncDisposables().ConfigureAwait(false);
                            }
                        }

                    } while (!isFinalBlock);

                    if (CanUseSerializeHandler)
                    {
                        // On successful serialization, record the serialization size
                        // to determine potential suitability of the type for
                        // fast-path serialization in streaming methods.
                        Debug.Assert(writer.BytesPending == 0);
                        OnRootLevelAsyncSerializationCompleted(writer.BytesCommitted);
                    }
                }
                catch
                {
                    // On exception, walk the WriteStack for any orphaned disposables and try to dispose them.
                    await state.DisposePendingDisposablesOnExceptionAsync().ConfigureAwait(false);
                    throw;
                }
                finally
                {
                    writer.Dispose();
                    if (pipeWriter is PooledByteBufferWriter disposable)
                    {
                        disposable.Dispose();
                    }
                }
            }
        }

        // Root serialization method for non-async streaming serialization
        internal void Serialize(
            Stream utf8Json,
            in T? rootValue,
            object? rootValueBoxed = null)
        {
            Debug.Assert(IsConfigured);
            Debug.Assert(rootValueBoxed is null || rootValueBoxed is T);

            if (CanUseSerializeHandlerInStreaming)
            {
                // Short-circuit calls into SerializeHandler, if the `CanUseSerializeHandlerInStreaming` heuristic allows it.

                Debug.Assert(SerializeHandler != null);
                Debug.Assert(CanUseSerializeHandler);
                Debug.Assert(Converter is JsonMetadataServicesConverter<T>);

                Utf8JsonWriter writer = Utf8JsonWriterCache.RentWriterAndBuffer(Options, out PooledByteBufferWriter bufferWriter);
                try
                {
                    SerializeHandler(writer, rootValue!);
                    writer.Flush();
                    bufferWriter.WriteToStream(utf8Json);
                }
                finally
                {
                    // Record the serialization size in both successful and failed operations,
                    // since we want to immediately opt out of the fast path if it exceeds the threshold.
                    OnRootLevelAsyncSerializationCompleted(writer.BytesCommitted + writer.BytesPending);

                    Utf8JsonWriterCache.ReturnWriterAndBuffer(writer, bufferWriter);
                }
            }
            else if (
#if NET
                !typeof(T).IsValueType &&
#endif
                Converter.CanBePolymorphic &&
                rootValue is not null &&
                Options.TryGetPolymorphicTypeInfoForRootType(rootValue, out JsonTypeInfo? polymorphicTypeInfo))
            {
                Debug.Assert(typeof(T) == typeof(object));
                polymorphicTypeInfo.SerializeAsObject(utf8Json, rootValue);
            }
            else
            {
                bool isFinalBlock;
                WriteStack state = default;
                state.Initialize(this,
                    rootValueBoxed,
                    supportContinuation: true,
                    supportAsync: false);

                using var bufferWriter = new PooledByteBufferWriter(Options.DefaultBufferSize);
                using var writer = new Utf8JsonWriter(bufferWriter, Options.GetWriterOptions());

                if (!bufferWriter.CanGetUnflushedBytes)
                {
                    ThrowHelper.ThrowInvalidOperationException_PipeWriterDoesNotImplementUnflushedBytes(bufferWriter);
                }
                state.PipeWriter = bufferWriter;
                state.FlushThreshold = (int)(bufferWriter.Capacity * JsonSerializer.FlushThreshold);

                do
                {
                    isFinalBlock = EffectiveConverter.WriteCore(writer, rootValue, Options, ref state);
                    writer.Flush();

                    bufferWriter.WriteToStream(utf8Json);
                    bufferWriter.Clear();

                    Debug.Assert(state.PendingTask == null);
                } while (!isFinalBlock);

                if (CanUseSerializeHandler)
                {
                    // On successful serialization, record the serialization size
                    // to determine potential suitability of the type for
                    // fast-path serialization in streaming methods.
                    Debug.Assert(writer.BytesPending == 0);
                    OnRootLevelAsyncSerializationCompleted(writer.BytesCommitted);
                }
            }
        }

        internal sealed override void SerializeAsObject(Utf8JsonWriter writer, object? rootValue)
            => Serialize(writer, JsonSerializer.UnboxOnWrite<T>(rootValue), rootValue);

        internal sealed override Task SerializeAsObjectAsync(PipeWriter pipeWriter, object? rootValue, int flushThreshold, CancellationToken cancellationToken)
            => SerializeAsync(pipeWriter, JsonSerializer.UnboxOnWrite<T>(rootValue), flushThreshold, cancellationToken, rootValue);

        internal sealed override Task SerializeAsObjectAsync(Stream utf8Json, object? rootValue, CancellationToken cancellationToken)
            => SerializeAsync(utf8Json, JsonSerializer.UnboxOnWrite<T>(rootValue), cancellationToken, rootValue);

        internal sealed override Task SerializeAsObjectAsync(PipeWriter utf8Json, object? rootValue, CancellationToken cancellationToken)
            => SerializeAsync(utf8Json, JsonSerializer.UnboxOnWrite<T>(rootValue), cancellationToken, rootValue);

        internal sealed override void SerializeAsObject(Stream utf8Json, object? rootValue)
            => Serialize(utf8Json, JsonSerializer.UnboxOnWrite<T>(rootValue), rootValue);

        // Fast-path serialization in source gen has not been designed with streaming in mind.
        // Even though it's not used in streaming by default, we can sometimes try to turn it on
        // assuming that the current type is known to produce small enough JSON payloads.
        // The `CanUseSerializeHandlerInStreaming` flag returns true iff:
        //  * The type has been used in at least `MinSerializationsSampleSize` streaming serializations AND
        //  * No serialization size exceeding JsonSerializerOptions.DefaultBufferSize / 2 has been recorded so far.
        private bool CanUseSerializeHandlerInStreaming => _canUseSerializeHandlerInStreamingState == 1;
        private volatile int _canUseSerializeHandlerInStreamingState; // 0: unspecified, 1: allowed, 2: forbidden

        private const int MinSerializationsSampleSize = 10;
        private volatile int _serializationCount;

        // Samples the latest serialization size for the current type to determine
        // if the fast-path SerializeHandler is appropriate for streaming serialization.
        private void OnRootLevelAsyncSerializationCompleted(long serializationSize)
        {
            Debug.Assert(CanUseSerializeHandler);

            if (_canUseSerializeHandlerInStreamingState != 2)
            {
                if ((ulong)serializationSize > (ulong)(Options.DefaultBufferSize / 2))
                {
                    // We have a serialization that exceeds the buffer size --
                    // forbid any use future use of the fast-path handler.
                    _canUseSerializeHandlerInStreamingState = 2;
                }
                else if ((uint)_serializationCount < MinSerializationsSampleSize)
                {
                    if (Interlocked.Increment(ref _serializationCount) == MinSerializationsSampleSize)
                    {
                        // We have the minimum number of serializations needed to flag the type as safe for fast-path.
                        // Use CMPXCHG to avoid racing with threads reporting a large serialization.
                        Interlocked.CompareExchange(ref _canUseSerializeHandlerInStreamingState, 1, 0);
                    }
                }
            }
        }
    }
}
