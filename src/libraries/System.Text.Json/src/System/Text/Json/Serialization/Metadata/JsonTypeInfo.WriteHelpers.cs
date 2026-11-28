// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Text.Json.Serialization.Metadata
{
    public abstract partial class JsonTypeInfo
    {
        // Root-level async streaming serialization is implemented here, on the non-generic base type, so that no
        // async state machine is instantiated for every JsonTypeInfo<T>. Only the synchronous steps are specialized
        // per type, through WriteCoreAsObject (untyped roots) or the IWriteRoot implementation (typed roots).
        // Typed callers must be static generic methods, since an instance method of JsonTypeInfo<T> that calls
        // a generic method over T would fill a dictionary slot for every constructed JsonTypeInfo<T>.
        // cf. https://github.com/dotnet/runtime/issues/85184

        // Writes (a slice of) the root value using the effective converter for the current type.
        // Must be equivalent to JsonConverter<T>.WriteCore, but takes a boxed root value.
        internal abstract bool WriteCoreAsObject(Utf8JsonWriter writer, object? rootValue, ref WriteStack state);

        // Throws if the boxed root value is not compatible with the current type.
        // Called before starting the async operation, so that the exception is thrown synchronously.
        internal abstract void ValidateRootValueAsObject(object? rootValue);

        internal Task SerializeAsObjectAsync(Stream utf8Json, object? rootValue, CancellationToken cancellationToken)
        {
            ValidateRootValueAsObject(rootValue);
            return SerializeToStreamAsync(utf8Json, new BoxedWriteRoot(rootValue), cancellationToken);
        }

        internal Task SerializeAsObjectAsync(PipeWriter utf8Json, object? rootValue, CancellationToken cancellationToken)
        {
            ValidateRootValueAsObject(rootValue);
            return SerializeToPipeAsync(utf8Json, new BoxedWriteRoot(rootValue), cancellationToken);
        }

        internal Task SerializeToStreamAsync<TRoot>(Stream utf8Json, TRoot root, CancellationToken cancellationToken)
            where TRoot : struct, IWriteRoot
        {
            PooledByteBufferWriter writer = new(Options.DefaultBufferSize, utf8Json);
            // Value chosen as 90% of the default buffer used in PooledByteBufferWriter.
            // This is a tradeoff between likelihood of needing to grow the array vs. utilizing most of the buffer
            int flushThreshold = (int)(writer.Capacity * JsonSerializer.FlushThreshold);
            return SerializeToPipeAsync(writer, root, flushThreshold, cancellationToken);
        }

        internal Task SerializeToPipeAsync<TRoot>(PipeWriter utf8Json, TRoot root, CancellationToken cancellationToken)
            where TRoot : struct, IWriteRoot
        {
            // Value chosen as 90% of 4 buffer segments in Pipes. This is semi-arbitrarily chosen and may be changed in future iterations.
            int flushThreshold = (int)(4 * PipeOptions.Default.MinimumSegmentSize * JsonSerializer.FlushThreshold);
            return SerializeToPipeAsync(utf8Json, root, flushThreshold, cancellationToken);
        }

        // Root serialization method for async streaming serialization.
        internal async Task<FlushResult> SerializeToPipeAsync<TRoot>(
            PipeWriter pipeWriter,
            TRoot root,
            int flushThreshold,
            CancellationToken cancellationToken,
            Utf8JsonWriter? jsonLineWriter = null)
            where TRoot : struct, IWriteRoot
        {
            Debug.Assert(IsConfigured);

            if (CanUseSerializeHandlerInStreaming)
            {
                // Short-circuit calls into SerializeHandler, if the `CanUseSerializeHandlerInStreaming` heuristic allows it.

                Debug.Assert(CanUseSerializeHandler);

                Utf8JsonWriter writer = jsonLineWriter ?? Utf8JsonWriterCache.RentWriter(Options, pipeWriter);

                try
                {
                    try
                    {
                        // Invokes the SerializeHandler and flushes the writer.
                        root.SerializeWithHandler(this, writer);
                        if (jsonLineWriter is not null)
                        {
                            WriteJsonLineTerminator(pipeWriter);
                        }
                    }
                    finally
                    {
                        // Record the serialization size in both successful and failed operations,
                        // since we want to immediately opt out of the fast path if it exceeds the threshold.
                        OnRootLevelAsyncSerializationCompleted(writer.BytesCommitted + writer.BytesPending);

                        if (jsonLineWriter is null)
                        {
                            Utf8JsonWriterCache.ReturnWriter(writer);
                        }
                    }

                    FlushResult result = await pipeWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
                    if (result.IsCanceled)
                    {
                        ThrowHelper.ThrowOperationCanceledException_PipeWriteCanceled();
                    }

                    return result;
                }
                finally
                {
                    if (jsonLineWriter is null && pipeWriter is PooledByteBufferWriter disposable)
                    {
                        disposable.Dispose();
                    }
                }
            }
            else if (
#if NET
                !Type.IsValueType &&
#endif
                Converter.CanBePolymorphic &&
                root.Value is { } rootValue &&
                Options.TryGetPolymorphicTypeInfoForRootType(rootValue, out JsonTypeInfo? derivedTypeInfo))
            {
                Debug.Assert(Type == typeof(object));
                return await derivedTypeInfo.SerializeToPipeAsync(pipeWriter, new BoxedWriteRoot(rootValue), flushThreshold, cancellationToken, jsonLineWriter).ConfigureAwait(false);
            }
            else
            {
                bool isFinalBlock;
                FlushResult result = default;
                WriteStack state = default;
                state.Initialize(this,
                    root.CallerBoxedValue,
                    supportContinuation: true,
                    supportAsync: true);

                if (!pipeWriter.CanGetUnflushedBytes)
                {
                    ThrowHelper.ThrowInvalidOperationException_PipeWriterDoesNotImplementUnflushedBytes(pipeWriter);
                }
                state.PipeWriter = pipeWriter;
                state.CancellationToken = cancellationToken;

                Utf8JsonWriter writer = jsonLineWriter ?? new Utf8JsonWriter(pipeWriter, Options.GetWriterOptions());

                try
                {
                    state.FlushThreshold = flushThreshold;

                    do
                    {
                        try
                        {
                            isFinalBlock = root.WriteCore(this, writer, ref state);

                            if (state.SuppressFlush)
                            {
                                Debug.Assert(!isFinalBlock);
                                Debug.Assert(state.PendingTask is not null);
                                state.SuppressFlush = false;
                            }
                            else
                            {
                                writer.Flush();
                                if (isFinalBlock && jsonLineWriter is not null)
                                {
                                    WriteJsonLineTerminator(pipeWriter);
                                }

                                result = await pipeWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
                                if (result.IsCanceled || result.IsCompleted)
                                {
                                    if (result.IsCanceled)
                                    {
                                        ThrowHelper.ThrowOperationCanceledException_PipeWriteCanceled();
                                    }

                                    // Pipe is completed, no one is reading so no point in continuing serialization
                                    return result;
                                }
                            }
                        }
                        finally
                        {
                            // Await any pending resumable converter tasks (currently these can only be IAsyncEnumerator.MoveNextAsync() or DisposeAsync() tasks).
                            // Note that pending tasks are always awaited, even if an exception has been thrown or the cancellation token has fired.
                            if (state.PendingTask is not null)
                            {
                                // Exceptions should only be propagated by the resuming converter
#if NET
                                await state.PendingTask.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
#else
                                try
                                {
                                    await state.PendingTask.ConfigureAwait(false);
                                }
                                catch { }
#endif
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

                    return result;
                }
                catch
                {
                    if (jsonLineWriter is null)
                    {
                        // Reset the writer in exception cases as we don't want the writer.Dispose() call to flush any pending bytes.
                        writer.Reset();
                        writer.Dispose();
                    }

                    // On exception, walk the WriteStack for any orphaned disposables and try to dispose them.
                    await state.DisposePendingDisposablesOnExceptionAsync().ConfigureAwait(false);
                    throw;
                }
                finally
                {
                    if (jsonLineWriter is null)
                    {
                        writer.Dispose();
                        if (pipeWriter is PooledByteBufferWriter disposable)
                        {
                            disposable.Dispose();
                        }
                    }
                }
            }

            static void WriteJsonLineTerminator(PipeWriter pipeWriter)
            {
                // The JSON Lines spec mandates a single line-feed character as the line separator,
                // independently of any platform-specific or user-configured newline preference.
                Span<byte> destination = pipeWriter.GetSpan(1);
                destination[0] = (byte)'\n';
                pipeWriter.Advance(1);
            }
        }

        // Fast-path serialization in source gen has not been designed with streaming in mind.
        // Even though it's not used in streaming by default, we can sometimes try to turn it on
        // assuming that the current type is known to produce small enough JSON payloads.
        // The `CanUseSerializeHandlerInStreaming` flag returns true iff:
        //  * The type has been used in at least `MinSerializationsSampleSize` streaming serializations AND
        //  * No serialization size exceeding JsonSerializerOptions.DefaultBufferSize / 2 has been recorded so far.
        private protected bool CanUseSerializeHandlerInStreaming => _canUseSerializeHandlerInStreamingState == 1;
        private volatile int _canUseSerializeHandlerInStreamingState; // 0: unspecified, 1: allowed, 2: forbidden

        private const int MinSerializationsSampleSize = 10;
        private volatile int _serializationCount;

        // Samples the latest serialization size for the current type to determine
        // if the fast-path SerializeHandler is appropriate for streaming serialization.
        private protected void OnRootLevelAsyncSerializationCompleted(long serializationSize)
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

    // Abstracts over the root value of an async streaming serialization, so that the serialization loop in
    // JsonTypeInfo can be shared by all JsonTypeInfo<T> instances without boxing typed root values.
    internal interface IWriteRoot
    {
        // The root value as an object. Only used for resolving polymorphic root types, so it is not called for value types.
        object? Value { get; }

        // The box provided by a non-generic caller, or null if the root was not boxed by the caller.
        // Used to track the root value for ReferenceHandler.IgnoreCycles.
        object? CallerBoxedValue { get; }

        // Writes the root value using JsonTypeInfo<T>.SerializeHandler and flushes the writer.
        void SerializeWithHandler(JsonTypeInfo typeInfo, Utf8JsonWriter writer);

        // Writes (a slice of) the root value using the effective converter of the type info.
        bool WriteCore(JsonTypeInfo typeInfo, Utf8JsonWriter writer, ref WriteStack state);
    }

    internal readonly struct TypedWriteRoot<T> : IWriteRoot
    {
        private readonly T? _value;

        public TypedWriteRoot(T? value) => _value = value;

        public object? Value => _value;

        public object? CallerBoxedValue => null;

        public void SerializeWithHandler(JsonTypeInfo typeInfo, Utf8JsonWriter writer)
        {
            Debug.Assert(typeInfo is JsonTypeInfo<T>);
            Debug.Assert(Unsafe.As<JsonTypeInfo<T>>(typeInfo).SerializeHandler is not null);
            Unsafe.As<JsonTypeInfo<T>>(typeInfo).SerializeHandler!(writer, _value!);
            writer.Flush();
        }

        public bool WriteCore(JsonTypeInfo typeInfo, Utf8JsonWriter writer, ref WriteStack state)
        {
            Debug.Assert(typeInfo is JsonTypeInfo<T>);
            return Unsafe.As<JsonTypeInfo<T>>(typeInfo).EffectiveConverter.WriteCore(writer, _value, typeInfo.Options, ref state);
        }
    }

    internal readonly struct BoxedWriteRoot : IWriteRoot
    {
        private readonly object? _value;

        public BoxedWriteRoot(object? value) => _value = value;

        public object? Value => _value;

        public object? CallerBoxedValue => _value;

        public void SerializeWithHandler(JsonTypeInfo typeInfo, Utf8JsonWriter writer)
            => typeInfo.SerializeAsObject(writer, _value);

        public bool WriteCore(JsonTypeInfo typeInfo, Utf8JsonWriter writer, ref WriteStack state)
            => typeInfo.WriteCoreAsObject(writer, _value, ref state);
    }
}
