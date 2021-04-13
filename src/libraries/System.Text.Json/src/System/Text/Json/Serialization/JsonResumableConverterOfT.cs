// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

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
            // Bridge from resumable to value converters.
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            ReadStack state = default;
            state.Initialize(typeToConvert, options, supportContinuation: false);
            TryRead(ref reader, typeToConvert, options, ref state, out T? value);
            return value;
        }

        public sealed override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            // Bridge from resumable to value converters.
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            WriteStack state = default;
            state.Initialize(typeof(T), options, supportContinuation: false);
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
