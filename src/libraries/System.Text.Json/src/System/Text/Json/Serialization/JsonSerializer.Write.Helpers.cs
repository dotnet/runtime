// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Text.Json.Serialization;

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        private static void VerifyValueAndType(object? value, Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            else if (value != null)
            {
                if (!type.IsAssignableFrom(value.GetType()))
                {
                    ThrowHelper.ThrowArgumentException_DeserializeWrongType(type, value);
                }
            }
        }

        private static byte[] WriteCoreBytes(object? value, Type type, JsonSerializerOptions? options)
        {
            if (options == null)
            {
                options = JsonSerializerOptions.s_defaultOptions;
            }

            byte[] result;

            using (var output = new PooledByteBufferWriter(options.DefaultBufferSize))
            {
                WriteCore(output, value, type, options);
                result = output.WrittenMemory.ToArray();
            }

            return result;
        }

        private static string WriteCoreString(object? value, Type type, JsonSerializerOptions? options)
        {
            if (options == null)
            {
                options = JsonSerializerOptions.s_defaultOptions;
            }

            string result;

            using (var output = new PooledByteBufferWriter(options.DefaultBufferSize))
            {
                WriteCore(output, value, type, options);
                result = JsonReaderHelper.TranscodeHelper(output.WrittenMemory.Span);
            }

            return result;
        }

        private static void WriteValueCore(Utf8JsonWriter writer, object? value, Type type, JsonSerializerOptions? options)
        {
            if (options == null)
            {
                options = JsonSerializerOptions.s_defaultOptions;
            }

            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            WriteCore(writer, value, type, options);
        }

        private static void WriteCore(PooledByteBufferWriter output, object? value, Type type, JsonSerializerOptions options)
        {
            using (var writer = new Utf8JsonWriter(output, options.GetWriterOptions()))
            {
                WriteCore(writer, value, type, options);
            }
        }

        private static void WriteCore(Utf8JsonWriter writer, object? value, Type type, JsonSerializerOptions options)
        {
            Debug.Assert(type != null || value == null);
            Debug.Assert(writer != null);

            if (value == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                //  We treat typeof(object) special and allow polymorphic behavior.
                if (type == typeof(object))
                {
                    type = value.GetType();
                }

                WriteStack state = default;
                state.InitializeRoot(type!, options, supportContinuation: false);
                WriteCore(writer, value, options, ref state, state.Current.JsonClassInfo!.PolicyProperty!.ConverterBase);
            }

            writer.Flush();
        }
    }
}
