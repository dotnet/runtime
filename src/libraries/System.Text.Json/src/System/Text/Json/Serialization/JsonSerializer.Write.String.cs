// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        /// <summary>
        /// Convert the provided value into a <see cref="string"/>.
        /// </summary>
        /// <returns>A <see cref="string"/> representation of the value.</returns>
        /// <param name="value">The value to convert.</param>
        /// <param name="options">Options to control the conversion behavior.</param>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <typeparamref name="TValue"/> or its serializable members.
        /// </exception>
        /// <remarks>Using a <see cref="string"/> is not as efficient as using UTF-8
        /// encoding since the implementation internally uses UTF-8. See also <see cref="SerializeToUtf8Bytes"/>
        /// and <see cref="SerializeAsync"/>.
        /// </remarks>
        public static string Serialize<[DynamicallyAccessedMembers(MembersAccessedOnWrite)] TValue>(TValue value, JsonSerializerOptions? options = null)
        {
            return Serialize<TValue>(value, typeof(TValue), options);
        }

        /// <summary>
        /// Convert the provided value into a <see cref="string"/>.
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
        /// <remarks>Using a <see cref="string"/> is not as efficient as using UTF-8
        /// encoding since the implementation internally uses UTF-8. See also <see cref="SerializeToUtf8Bytes"/>
        /// and <see cref="SerializeAsync"/>.
        /// </remarks>
        public static string Serialize(
            object? value,
            [DynamicallyAccessedMembers(MembersAccessedOnWrite)] Type inputType,
            JsonSerializerOptions? options = null)
        {
            if (inputType == null)
            {
                throw new ArgumentNullException(nameof(inputType));
            }

            if (value != null && !inputType.IsAssignableFrom(value.GetType()))
            {
                ThrowHelper.ThrowArgumentException_DeserializeWrongType(inputType, value);
            }

            return Serialize<object?>(value, inputType, options);
        }

        private static string Serialize<TValue>(in TValue value, Type inputType, JsonSerializerOptions? options)
        {
            if (options == null)
            {
                options = JsonSerializerOptions.s_defaultOptions;
            }

            using (var output = new PooledByteBufferWriter(options.DefaultBufferSize))
            {
                using (var writer = new Utf8JsonWriter(output, options.GetWriterOptions()))
                {
                    WriteCore(writer, value, inputType, options);
                }

                return JsonReaderHelper.TranscodeHelper(output.WrittenMemory.Span);
            }
        }

        /// <summary>
        /// todo
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="value"></param>
        /// <param name="jsonTypeInfo"></param>
        /// <returns></returns>
        public static string Serialize<TValue>(in TValue value, JsonTypeInfo<TValue> jsonTypeInfo)
        {
            if (jsonTypeInfo == null)
            {
                throw new ArgumentNullException(nameof(jsonTypeInfo));
            }

            WriteStack state = default;
            state.Initialize(jsonTypeInfo);

            JsonSerializerOptions options = jsonTypeInfo.Options;

            using (var output = new PooledByteBufferWriter(options.DefaultBufferSize))
            {
                using (var writer = new Utf8JsonWriter(output, options.GetWriterOptions()))
                {
                    JsonConverter? jsonConverter = jsonTypeInfo.PropertyInfoForClassInfo.ConverterBase as JsonConverter<TValue>;
                    if (jsonConverter == null)
                    {
                        throw new InvalidOperationException("todo: classInfo not compatible");
                    }

                    WriteCore(jsonConverter, writer, value, ref state, options);
                }

                return JsonReaderHelper.TranscodeHelper(output.WrittenMemory.Span);
            }
        }
    }
}
