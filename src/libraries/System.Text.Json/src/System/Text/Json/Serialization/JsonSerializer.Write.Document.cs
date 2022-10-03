// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        /// <summary>
        /// Converts the provided value into a <see cref="JsonDocument"/>.
        /// </summary>
        /// <typeparam name="TValue">The type of the value to serialize.</typeparam>
        /// <returns>A <see cref="JsonDocument"/> representation of the JSON value.</returns>
        /// <param name="value">The value to convert.</param>
        /// <param name="options">Options to control the conversion behavior.</param>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <typeparamref name="TValue"/> or its serializable members.
        /// </exception>
        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(SerializationRequiresDynamicCodeMessage)]
        public static JsonDocument SerializeToDocument<TValue>(TValue value, JsonSerializerOptions? options = null)
        {
            JsonTypeInfo<TValue> jsonTypeInfo = GetTypeInfo<TValue>(options);
            return WriteDocument(value, jsonTypeInfo);
        }

        /// <summary>
        /// Converts the provided value into a <see cref="JsonDocument"/>.
        /// </summary>
        /// <returns>A <see cref="JsonDocument"/> representation of the value.</returns>
        /// <param name="value">The value to convert.</param>
        /// <param name="inputType">The type of the <paramref name="value"/> to convert.</param>
        /// <param name="options">Options to control the conversion behavior.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="inputType"/> is not compatible with <paramref name="value"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <exception cref="ArgumentNullException">
        /// <paramref name="inputType"/> is <see langword="null"/>.
        /// </exception>
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <paramref name="inputType"/>  or its serializable members.
        /// </exception>
        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(SerializationRequiresDynamicCodeMessage)]
        public static JsonDocument SerializeToDocument(object? value, Type inputType, JsonSerializerOptions? options = null)
        {
            ValidateInputType(value, inputType);
            JsonTypeInfo jsonTypeInfo = GetTypeInfo(options, inputType);
            return WriteDocumentAsObject(value, jsonTypeInfo);
        }

        /// <summary>
        /// Converts the provided value into a <see cref="JsonDocument"/>.
        /// </summary>
        /// <typeparam name="TValue">The type of the value to serialize.</typeparam>
        /// <returns>A <see cref="JsonDocument"/> representation of the value.</returns>
        /// <param name="value">The value to convert.</param>
        /// <param name="jsonTypeInfo">Metadata about the type to convert.</param>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="Serialization.JsonConverter"/>
        /// for <typeparamref name="TValue"/> or its serializable members.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="jsonTypeInfo"/> is <see langword="null"/>.
        /// </exception>
        public static JsonDocument SerializeToDocument<TValue>(TValue value, JsonTypeInfo<TValue> jsonTypeInfo)
        {
            if (jsonTypeInfo is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(jsonTypeInfo));
            }

            jsonTypeInfo.EnsureConfigured();
            return WriteDocument(value, jsonTypeInfo);
        }

        /// <summary>
        /// Converts the provided value into a <see cref="JsonDocument"/>.
        /// </summary>
        /// <returns>A <see cref="JsonDocument"/> representation of the value.</returns>
        /// <param name="value">The value to convert.</param>
        /// <param name="inputType">The type of the <paramref name="value"/> to convert.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <paramref name="inputType"/> or its serializable members.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The <see cref="JsonSerializerContext.GetTypeInfo(Type)"/> method of the provided
        /// <paramref name="context"/> returns <see langword="null"/> for the type to convert.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="inputType"/> or <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        public static JsonDocument SerializeToDocument(object? value, Type inputType, JsonSerializerContext context)
        {
            if (context is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(context));
            }

            ValidateInputType(value, inputType);
            return WriteDocumentAsObject(value, GetTypeInfo(context, inputType));
        }

        private static JsonDocument WriteDocument<TValue>(in TValue value, JsonTypeInfo<TValue> jsonTypeInfo)
        {
            Debug.Assert(jsonTypeInfo.IsConfigured);
            JsonSerializerOptions options = jsonTypeInfo.Options;

            // For performance, share the same buffer across serialization and deserialization.
            // The PooledByteBufferWriter is cleared and returned when JsonDocument.Dispose() is called.
            PooledByteBufferWriter output = new(options.DefaultBufferSize);
            Utf8JsonWriter writer = Utf8JsonWriterCache.RentWriter(options, output);

            try
            {
                WriteCore(writer, value, jsonTypeInfo);
                return JsonDocument.ParseRented(output, options.GetDocumentOptions());
            }
            finally
            {
                Utf8JsonWriterCache.ReturnWriter(writer);
            }
        }

        private static JsonDocument WriteDocumentAsObject(object? value, JsonTypeInfo jsonTypeInfo)
        {
            Debug.Assert(jsonTypeInfo.IsConfigured);
            JsonSerializerOptions options = jsonTypeInfo.Options;

            // For performance, share the same buffer across serialization and deserialization.
            // The PooledByteBufferWriter is cleared and returned when JsonDocument.Dispose() is called.
            PooledByteBufferWriter output = new(options.DefaultBufferSize);
            Utf8JsonWriter writer = Utf8JsonWriterCache.RentWriter(options, output);

            try
            {
                WriteCoreAsObject(writer, value, jsonTypeInfo);
                return JsonDocument.ParseRented(output, options.GetDocumentOptions());
            }
            finally
            {
                Utf8JsonWriterCache.ReturnWriter(writer);
            }
        }
    }
}
