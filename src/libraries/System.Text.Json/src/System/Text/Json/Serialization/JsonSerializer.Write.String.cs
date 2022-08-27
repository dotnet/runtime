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
        /// Converts the provided value into a <see cref="string"/>.
        /// </summary>
        /// <typeparam name="TValue">The type of the value to serialize.</typeparam>
        /// <returns>A <see cref="string"/> representation of the value.</returns>
        /// <param name="value">The value to convert.</param>
        /// <param name="options">Options to control the conversion behavior.</param>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <typeparamref name="TValue"/> or its serializable members.
        /// </exception>
        /// <remarks>Using a <see cref="string"/> is not as efficient as using UTF-8
        /// encoding since the implementation internally uses UTF-8. See also <see cref="SerializeToUtf8Bytes{TValue}(TValue, JsonSerializerOptions?)"/>
        /// and <see cref="SerializeAsync{TValue}(IO.Stream, TValue, JsonSerializerOptions?, Threading.CancellationToken)"/>.
        /// </remarks>
        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(SerializationRequiresDynamicCodeMessage)]
        public static string Serialize<TValue>(TValue value, JsonSerializerOptions? options = null)
        {
            JsonTypeInfo<TValue> jsonTypeInfo = GetTypeInfo<TValue>(options);
            return WriteString(value, jsonTypeInfo);
        }

        /// <summary>
        /// Converts the provided value into a <see cref="string"/>.
        /// </summary>
        /// <returns>A <see cref="string"/> representation of the value.</returns>
        /// <param name="value">The value to convert.</param>
        /// <param name="inputType">The type of the <paramref name="value"/> to convert.</param>
        /// <param name="options">Options to control the conversion behavior.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="inputType"/> is not compatible with <paramref name="value"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <paramref name="inputType"/>  or its serializable members.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="inputType"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>Using a <see cref="string"/> is not as efficient as using UTF-8
        /// encoding since the implementation internally uses UTF-8. See also <see cref="SerializeToUtf8Bytes(object?, Type, JsonSerializerOptions?)"/>
        /// and <see cref="SerializeAsync(IO.Stream, object?, Type, JsonSerializerOptions?, Threading.CancellationToken)"/>.
        /// </remarks>
        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(SerializationRequiresDynamicCodeMessage)]
        public static string Serialize(
            object? value,
            Type inputType,
            JsonSerializerOptions? options = null)
        {
            ValidateInputType(value, inputType);
            JsonTypeInfo jsonTypeInfo = GetTypeInfo(options, inputType);
            return WriteStringAsObject(value, jsonTypeInfo);
        }

        /// <summary>
        /// Converts the provided value into a <see cref="string"/>.
        /// </summary>
        /// <typeparam name="TValue">The type of the value to serialize.</typeparam>
        /// <returns>A <see cref="string"/> representation of the value.</returns>
        /// <param name="value">The value to convert.</param>
        /// <param name="jsonTypeInfo">Metadata about the type to convert.</param>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="Serialization.JsonConverter"/>
        /// for <typeparamref name="TValue"/> or its serializable members.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="jsonTypeInfo"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>Using a <see cref="string"/> is not as efficient as using UTF-8
        /// encoding since the implementation internally uses UTF-8. See also <see cref="SerializeToUtf8Bytes{TValue}(TValue, JsonTypeInfo{TValue})"/>
        /// and <see cref="SerializeAsync{TValue}(IO.Stream, TValue, JsonTypeInfo{TValue}, Threading.CancellationToken)"/>.
        /// </remarks>
        public static string Serialize<TValue>(TValue value, JsonTypeInfo<TValue> jsonTypeInfo)
        {
            if (jsonTypeInfo is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(jsonTypeInfo));
            }

            jsonTypeInfo.EnsureConfigured();
            return WriteString(value, jsonTypeInfo);
        }

        /// <summary>
        /// Converts the provided value into a <see cref="string"/>.
        /// </summary>
        /// <returns>A <see cref="string"/> representation of the value.</returns>
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
        /// <remarks>Using a <see cref="string"/> is not as efficient as using UTF-8
        /// encoding since the implementation internally uses UTF-8. See also <see cref="SerializeToUtf8Bytes(object?, Type, JsonSerializerContext)"/>
        /// and <see cref="SerializeAsync(IO.Stream, object?, Type, JsonSerializerContext, Threading.CancellationToken)"/>.
        /// </remarks>
        public static string Serialize(object? value, Type inputType, JsonSerializerContext context)
        {
            if (context is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(context));
            }

            ValidateInputType(value, inputType);
            JsonTypeInfo jsonTypeInfo = GetTypeInfo(context, inputType);
            return WriteStringAsObject(value, jsonTypeInfo);
        }

        private static string WriteString<TValue>(in TValue value, JsonTypeInfo<TValue> jsonTypeInfo)
        {
            Debug.Assert(jsonTypeInfo.IsConfigured);

            Utf8JsonWriter writer = Utf8JsonWriterCache.RentWriterAndBuffer(jsonTypeInfo.Options, out PooledByteBufferWriter output);

            try
            {
                WriteCore(writer, value, jsonTypeInfo);
                return JsonReaderHelper.TranscodeHelper(output.WrittenMemory.Span);
            }
            finally
            {
                Utf8JsonWriterCache.ReturnWriterAndBuffer(writer, output);
            }
        }

        private static string WriteStringAsObject(object? value, JsonTypeInfo jsonTypeInfo)
        {
            Debug.Assert(jsonTypeInfo.IsConfigured);

            Utf8JsonWriter writer = Utf8JsonWriterCache.RentWriterAndBuffer(jsonTypeInfo.Options, out PooledByteBufferWriter output);

            try
            {
                WriteCoreAsObject(writer, value, jsonTypeInfo);
                return JsonReaderHelper.TranscodeHelper(output.WrittenMemory.Span);
            }
            finally
            {
                Utf8JsonWriterCache.ReturnWriterAndBuffer(writer, output);
            }
        }
    }
}
