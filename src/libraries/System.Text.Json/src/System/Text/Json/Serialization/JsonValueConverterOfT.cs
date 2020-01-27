// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json.Serialization
{
    // Used for value converters that need to re-enter the serializer since it will
    // support JsonPath and other features that require per-serialization state.
    internal abstract class JsonValueConverter<T> : JsonConverter<T>
    {
        internal override sealed ClassType ClassType => ClassType.NewValue;

        public override sealed T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Bridge from resumable to value converters.
            if (options == null)
            {
                options = JsonSerializerOptions.s_defaultOptions;
            }

            ReadStack state = default;
            state.InitializeRoot(typeToConvert, options);
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
            state.InitializeRoot(typeof(T), options);
            TryWrite(writer, value, options, ref state);
        }
    }
}
