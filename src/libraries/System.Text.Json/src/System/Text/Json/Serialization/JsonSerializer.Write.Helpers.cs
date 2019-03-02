// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;

namespace System.Text.Json.Serialization
{
    public static partial class JsonSerializer
    {
        private static void VerifyValueAndType(object value, Type type)
        {
            if (type == null)
            {
                if (value != null)
                {
                    throw new ArgumentNullException(nameof(type));
                }
            }
            else if (value != null)
            {
                if (!type.IsAssignableFrom(value.GetType()))
                {
                    ThrowHelper.ThrowArgumentException_DeserializeWrongType(type, value);
                }
            }
        }

        private static void WriteNull(
            ref JsonWriterState writerState,
            IBufferWriter<byte> bufferWriter)
        {
            Utf8JsonWriter writer = new Utf8JsonWriter(bufferWriter, writerState);
            writer.WriteNullValue();
            writer.Flush(true);
        }

        private static byte[] WriteCoreBytes(object value, Type type, JsonSerializerOptions options)
        {
            if (options == null)
                options = s_defaultSettings;

            byte[] result;

            using (var output = new ArrayBufferWriter<byte>(options.EffectiveBufferSize))
            {
                WriteCore(output, value, type, options);
                result = output.WrittenMemory.ToArray();
            }

            return result;
        }

        private static string WriteCoreString(object value, Type type, JsonSerializerOptions options)
        {
            if (options == null)
                options = s_defaultSettings;

            string result;

            using (var output = new ArrayBufferWriter<byte>(options.EffectiveBufferSize))
            {
                WriteCore(output, value, type, options);
                result = JsonReaderHelper.TranscodeHelper(output.WrittenMemory.Span);
            }

            return result;
        }

        private static void WriteCore(ArrayBufferWriter<byte> output, object value, Type type, JsonSerializerOptions options)
        {
            var writerState = new JsonWriterState(options.WriterOptions);
            var writer = new Utf8JsonWriter(output, writerState);

            if (value == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                if (type == null)
                {
                    type = value.GetType();
                }

                WriteStack state = default;
                JsonClassInfo classInfo = options.GetOrAddClass(type);
                state.Current.JsonClassInfo = classInfo;
                state.Current.CurrentValue = value;
                if (classInfo.ClassType != ClassType.Object)
                {
                    state.Current.JsonPropertyInfo = classInfo.GetPolicyProperty();
                }

                Write(ref writer, -1, options, ref state);
            }

            writer.Flush(isFinalBlock: true);
        }
    }
}
