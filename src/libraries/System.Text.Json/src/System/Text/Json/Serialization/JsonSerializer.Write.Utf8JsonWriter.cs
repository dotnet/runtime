// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        /// <summary>
        /// Write one JSON value (including objects or arrays) to the provided writer.
        /// </summary>
        /// <param name="writer">The writer to write.</param>
        /// <param name="value">The value to convert and write.</param>
        /// <param name="options">Options to control the behavior.</param>
        public static void WriteValue<TValue>(Utf8JsonWriter writer, TValue value, JsonSerializerOptions options = null)
        {
            WriteValueCore(writer, value, typeof(TValue), options);
        }

        /// <summary>
        /// Write one JSON value (including objects or arrays) to the provided writer.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value">The value to convert and write.</param>
        /// <param name="type">The type of the <paramref name="value"/> to convert.</param>
        /// <param name="options">Options to control the behavior.</param>
        public static void WriteValue(Utf8JsonWriter writer, object value, Type type, JsonSerializerOptions options = null)
        {
            VerifyValueAndType(value, type);
            WriteValueCore(writer, value, type, options);
        }
    }
}
