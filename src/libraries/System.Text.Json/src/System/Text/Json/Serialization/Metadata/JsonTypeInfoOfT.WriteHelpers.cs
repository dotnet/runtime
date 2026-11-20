// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Text.Json.Serialization.Converters;

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

                Debug.Assert(SerializeHandler is not null);
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

                Debug.Assert(SerializeHandler is not null);
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

                Utf8JsonWriter writer = Utf8JsonWriterCache.RentWriterAndBuffer(Options, out PooledByteBufferWriter bufferWriter);
                Debug.Assert(bufferWriter.CanGetUnflushedBytes);

                try
                {
                    state.PipeWriter = bufferWriter;
                    state.FlushThreshold = (int)(bufferWriter.Capacity * JsonSerializer.FlushThreshold);

                    do
                    {
                        isFinalBlock = EffectiveConverter.WriteCore(writer, rootValue, Options, ref state);
                        writer.Flush();

                        bufferWriter.WriteToStream(utf8Json);
                        bufferWriter.Clear();

                        Debug.Assert(state.PendingTask is null);
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
                finally
                {
                    Utf8JsonWriterCache.ReturnWriterAndBuffer(writer, bufferWriter);
                }
            }
        }

        internal sealed override void SerializeAsObject(Utf8JsonWriter writer, object? rootValue)
            => Serialize(writer, JsonSerializer.UnboxOnWrite<T>(rootValue), rootValue);

        internal sealed override void SerializeAsObject(Stream utf8Json, object? rootValue)
            => Serialize(utf8Json, JsonSerializer.UnboxOnWrite<T>(rootValue), rootValue);

        internal sealed override void ValidateRootValueAsObject(object? rootValue)
            => JsonSerializer.UnboxOnWrite<T>(rootValue);

        internal sealed override bool WriteCoreAsObject(Utf8JsonWriter writer, object? rootValue, ref WriteStack state)
            => EffectiveConverter.WriteCore(writer, JsonSerializer.UnboxOnWrite<T>(rootValue), Options, ref state);
    }
}
