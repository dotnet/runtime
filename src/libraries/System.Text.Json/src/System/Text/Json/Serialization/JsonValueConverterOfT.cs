// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json.Serialization
{
    // Used for value converters that need to re-enter the serializer since it will support JsonPath
    // and reference handling.
    internal abstract class JsonValueConverter<T> : JsonConverter<T>
    {
        internal sealed override ClassType ClassType => ClassType.NewValue;

        public override sealed T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Bridge from resumable to value converters.
            if (options == null)
            {
                options = JsonSerializerOptions.s_defaultOptions;
            }

            ReadStack state = default;
            state.Initialize(typeToConvert, options, supportContinuation: false);
            TryRead(ref reader, typeToConvert, options, ref state, out T value);
            return value;
        }

        public override sealed void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            // Bridge from resumable to value converters.
            if (options == null)
            {
                options = JsonSerializerOptions.s_defaultOptions;
            }

            WriteStack state = default;
            state.Initialize(typeof(T), options, supportContinuation: false);
            TryWrite(writer, value, options, ref state);
        }
    }
}
