// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        /// <summary>
        /// Internal version that allows re-entry with preserving ReadStack so that JsonPath works correctly.
        /// </summary>
        internal static void Serialize<T>(Utf8JsonWriter writer, T value, JsonSerializerOptions options, ref WriteStack state, string? propertyName = null)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            JsonConverter jsonConverter = state.Current.InitializeReEntry(typeof(T), options, propertyName);
            Write(writer, value, options, ref state, jsonConverter);
        }

        /// <summary>
        /// Internal version that allows re-entry with preserving ReadStack so that JsonPath works correctly.
        /// </summary>
        internal static void Serialize(Utf8JsonWriter writer, object? value, Type inputType, JsonSerializerOptions options, ref WriteStack state, string? propertyName = null)
        {
            if (inputType == null)
            {
                throw new ArgumentNullException(nameof(inputType));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            JsonConverter jsonConverter = state.Current.InitializeReEntry(inputType, options, propertyName);
            Write(writer, value, options, ref state, jsonConverter);
        }

        /// <summary>
        /// Write one JSON value (including objects or arrays) to the provided writer.
        /// </summary>
        /// <param name="writer">The writer to write.</param>
        /// <param name="value">The value to convert and write.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="writer"/> is null.
        /// </exception>
        public static void Serialize<TValue>(Utf8JsonWriter writer, TValue value, JsonSerializerOptions? options = null)
        {
            WriteValueCore(writer, value, typeof(TValue), options);
        }

        /// <summary>
        /// Write one JSON value (including objects or arrays) to the provided writer.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value">The value to convert and write.</param>
        /// <param name="inputType">The type of the <paramref name="value"/> to convert.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="writer"/> is null.
        /// </exception>
        public static void Serialize(Utf8JsonWriter writer, object? value, Type inputType, JsonSerializerOptions? options = null)
        {
            VerifyValueAndType(value, inputType);
            WriteValueCore(writer, value, inputType, options);
        }
    }
}
