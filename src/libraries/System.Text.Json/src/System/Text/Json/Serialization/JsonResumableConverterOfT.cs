// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Base class for converters that are able to resume after reading or writing to a buffer.
    /// This is used when the Stream-based serialization APIs are used.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal abstract class JsonResumableConverter<T> : JsonConverter<T>
    {
        public sealed override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (options is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(options));
            }

            // Bridge from resumable to value converters.

            ReadStack state = default;
            JsonTypeInfo jsonTypeInfo = options.GetTypeInfoInternal(typeToConvert);
            state.Initialize(jsonTypeInfo);

            TryRead(ref reader, typeToConvert, options, ref state, out T? value, out _);
            return value;
        }

        public sealed override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            if (options is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(options));
            }

            // Bridge from resumable to value converters.
            WriteStack state = default;
            JsonTypeInfo typeInfo = options.GetTypeInfoInternal(typeof(T));
            state.Initialize(typeInfo);

            try
            {
                TryWrite(writer, value, options, ref state);
            }
            catch
            {
                state.DisposePendingDisposablesOnException();
                throw;
            }
        }

        public sealed override bool HandleNull => false;
    }
}
