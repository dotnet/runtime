// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using System.Diagnostics;

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        /// <summary>
        /// Internal version that allows re-entry with preserving WriteStack so that JsonPath works correctly.
        /// </summary>
        // If this is made public, we will also want to have a non-generic version.
        internal static void Serialize<T>(Utf8JsonWriter writer, T value, JsonSerializerOptions options, ref WriteStack state, string? propertyName = null)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            JsonConverter jsonConverter = state.Current.InitializeReEntry(typeof(T), options, propertyName);
            bool success = jsonConverter.TryWriteAsObject(writer, value, options, ref state);
            Debug.Assert(success);
        }

        /// <summary>
        /// Write one JSON value (including objects or arrays) to the provided writer.
        /// </summary>
        /// <param name="writer">The writer to write.</param>
        /// <param name="value">The value to convert and write.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="writer"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <typeparamref name="TValue"/> or its serializable members.
        /// </exception>
        public static void Serialize<TValue>(Utf8JsonWriter writer, TValue value, JsonSerializerOptions? options = null)
        {
            Serialize<TValue>(writer, value, typeof(TValue), options);
        }

        /// <summary>
        /// Write one JSON value (including objects or arrays) to the provided writer.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value">The value to convert and write.</param>
        /// <param name="inputType">The type of the <paramref name="value"/> to convert.</param>
        /// <param name="options">Options to control the behavior.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="inputType"/> is not compatible with <paramref name="value"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="writer"/> or <paramref name="inputType"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <paramref name="inputType"/> or its serializable members.
        /// </exception>
        public static void Serialize(Utf8JsonWriter writer, object? value, Type inputType, JsonSerializerOptions? options = null)
        {
            if (inputType == null)
            {
                throw new ArgumentNullException(nameof(inputType));
            }

            if (value != null && !inputType.IsAssignableFrom(value.GetType()))
            {
                ThrowHelper.ThrowArgumentException_DeserializeWrongType(inputType, value);
            }

            Serialize<object?>(writer, value, inputType, options);
        }

        private static void Serialize<TValue>(Utf8JsonWriter writer, TValue value, Type type, JsonSerializerOptions? options)
        {
            if (options == null)
            {
                options = JsonSerializerOptions.s_defaultOptions;
            }

            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            WriteCore<TValue>(writer, value, type, options);
        }
    }
}
