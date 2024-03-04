// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipelines;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        // We flush the Stream when the buffer is >=90% of capacity.
        // This threshold is a compromise between buffer utilization and minimizing cases where the buffer
        // needs to be expanded\doubled because it is not large enough to write the current property or element.
        // We check for flush after each JSON property and element is written to the buffer.
        // Once the buffer is expanded to contain the largest single element\property, a 90% threshold
        // means the buffer may be expanded a maximum of 4 times: 1-(1/(2^4))==.9375.
        internal const float FlushThreshold = .90f;

        /// <summary>
        /// Converts the provided value to UTF-8 encoded JSON text and write it to the <see cref="System.IO.Stream"/>.
        /// </summary>
        /// <typeparam name="TValue">The type of the value to serialize.</typeparam>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        /// <param name="utf8Json">The UTF-8 <see cref="System.IO.Stream"/> to write to.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="options">Options to control the conversion behavior.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken"/> that can be used to cancel the write operation.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="utf8Json"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <typeparamref name="TValue"/> or its serializable members.
        /// </exception>
        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(SerializationRequiresDynamicCodeMessage)]
        public static Task SerializeAsync<TValue>(
            Stream utf8Json,
            TValue value,
            JsonSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            if (utf8Json is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(utf8Json));
            }

            JsonTypeInfo<TValue> jsonTypeInfo = GetTypeInfo<TValue>(options);
            return jsonTypeInfo.SerializeAsync(utf8Json, value, cancellationToken);
        }

        /// <summary>
        /// g
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="utf8Json"></param>
        /// <param name="value"></param>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(SerializationRequiresDynamicCodeMessage)]
        public static Task SerializeAsync<TValue>(
            PipeWriter utf8Json,
            TValue value,
            JsonSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            if (utf8Json is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(utf8Json));
            }

            JsonTypeInfo<TValue> jsonTypeInfo = GetTypeInfo<TValue>(options);
            return jsonTypeInfo.SerializeAsync(utf8Json, value, cancellationToken);
        }

        /// <summary>
        /// f
        /// </summary>
        /// <param name="utf8Json"></param>
        /// <param name="value"></param>
        /// <param name="jsonTypeInfo"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task SerializeAsync(
            PipeWriter utf8Json,
            object? value,
            JsonTypeInfo jsonTypeInfo,
            CancellationToken cancellationToken = default)
        {
            if (utf8Json is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(utf8Json));
            }

            if (jsonTypeInfo is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(jsonTypeInfo));
            }

            jsonTypeInfo.EnsureConfigured();
            return jsonTypeInfo.SerializeAsObjectAsync(utf8Json, value, cancellationToken);
        }

        /// <summary>
        /// g
        /// </summary>
        /// <param name="utf8Json"></param>
        /// <param name="value"></param>
        /// <param name="inputType"></param>
        /// <param name="context"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task SerializeAsync(
                PipeWriter utf8Json,
                object? value,
                Type inputType,
                JsonSerializerContext context,
                CancellationToken cancellationToken = default)
        {
            if (utf8Json is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(utf8Json));
            }

            if (context is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(context));
            }

            ValidateInputType(value, inputType);
            JsonTypeInfo jsonTypeInfo = GetTypeInfo(context, inputType);

            return jsonTypeInfo.SerializeAsObjectAsync(utf8Json, value, cancellationToken);
        }

        /// <summary>
        /// g
        /// </summary>
        /// <param name="utf8Json"></param>
        /// <param name="value"></param>
        /// <param name="inputType"></param>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(SerializationRequiresDynamicCodeMessage)]
        public static Task SerializeAsync(
                PipeWriter utf8Json,
                object? value,
                Type inputType,
                JsonSerializerOptions? options = null,
                CancellationToken cancellationToken = default)
        {
            if (utf8Json is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(utf8Json));
            }

            ValidateInputType(value, inputType);
            JsonTypeInfo jsonTypeInfo = GetTypeInfo(options, inputType);

            return jsonTypeInfo.SerializeAsObjectAsync(utf8Json, value, cancellationToken);
        }

        /// <summary>
        /// Converts the provided value to UTF-8 encoded JSON text and write it to the <see cref="System.IO.Stream"/>.
        /// </summary>
        /// <typeparam name="TValue">The type of the value to serialize.</typeparam>
        /// <param name="utf8Json">The UTF-8 <see cref="System.IO.Stream"/> to write to.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="options">Options to control the conversion behavior.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="utf8Json"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <typeparamref name="TValue"/> or its serializable members.
        /// </exception>
        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(SerializationRequiresDynamicCodeMessage)]
        public static void Serialize<TValue>(
            Stream utf8Json,
            TValue value,
            JsonSerializerOptions? options = null)
        {
            if (utf8Json is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(utf8Json));
            }

            JsonTypeInfo<TValue> jsonTypeInfo = GetTypeInfo<TValue>(options);
            jsonTypeInfo.Serialize(utf8Json, value);
        }

        /// <summary>
        /// Converts the provided value to UTF-8 encoded JSON text and write it to the <see cref="System.IO.Stream"/>.
        /// </summary>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        /// <param name="utf8Json">The UTF-8 <see cref="System.IO.Stream"/> to write to.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="inputType">The type of the <paramref name="value"/> to convert.</param>
        /// <param name="options">Options to control the conversion behavior.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken"/> that can be used to cancel the write operation.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="inputType"/> is not compatible with <paramref name="value"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="utf8Json"/> or <paramref name="inputType"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <paramref name="inputType"/>  or its serializable members.
        /// </exception>
        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(SerializationRequiresDynamicCodeMessage)]
        public static Task SerializeAsync(
            Stream utf8Json,
            object? value,
            Type inputType,
            JsonSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            if (utf8Json is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(utf8Json));
            }

            ValidateInputType(value, inputType);
            JsonTypeInfo jsonTypeInfo = GetTypeInfo(options, inputType);
            return jsonTypeInfo.SerializeAsObjectAsync(utf8Json, value, cancellationToken);
        }

        /// <summary>
        /// Converts the provided value to UTF-8 encoded JSON text and write it to the <see cref="System.IO.Stream"/>.
        /// </summary>
        /// <param name="utf8Json">The UTF-8 <see cref="System.IO.Stream"/> to write to.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="inputType">The type of the <paramref name="value"/> to convert.</param>
        /// <param name="options">Options to control the conversion behavior.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="inputType"/> is not compatible with <paramref name="value"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="utf8Json"/> or <paramref name="inputType"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <paramref name="inputType"/>  or its serializable members.
        /// </exception>
        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(SerializationRequiresDynamicCodeMessage)]
        public static void Serialize(
            Stream utf8Json,
            object? value,
            Type inputType,
            JsonSerializerOptions? options = null)
        {
            if (utf8Json is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(utf8Json));
            }

            ValidateInputType(value, inputType);
            JsonTypeInfo jsonTypeInfo = GetTypeInfo(options, inputType);
            jsonTypeInfo.SerializeAsObject(utf8Json, value);
        }

        /// <summary>
        /// Converts the provided value to UTF-8 encoded JSON text and write it to the <see cref="System.IO.Stream"/>.
        /// </summary>
        /// <typeparam name="TValue">The type of the value to serialize.</typeparam>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        /// <param name="utf8Json">The UTF-8 <see cref="System.IO.Stream"/> to write to.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="jsonTypeInfo">Metadata about the type to convert.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken"/> that can be used to cancel the write operation.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="utf8Json"/> is <see langword="null"/>.
        /// </exception>
        public static Task SerializeAsync<TValue>(
            Stream utf8Json,
            TValue value,
            JsonTypeInfo<TValue> jsonTypeInfo,
            CancellationToken cancellationToken = default)
        {
            if (utf8Json is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(utf8Json));
            }
            if (jsonTypeInfo is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(jsonTypeInfo));
            }

            jsonTypeInfo.EnsureConfigured();
            return jsonTypeInfo.SerializeAsync(utf8Json, value, cancellationToken);
        }

        /// <summary>
        /// tmp
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="utf8Json"></param>
        /// <param name="value"></param>
        /// <param name="jsonTypeInfo"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static Task SerializeAsync<TValue>(
            PipeWriter utf8Json,
            TValue value,
            JsonTypeInfo<TValue> jsonTypeInfo,
            CancellationToken cancellationToken = default)
        {
            if (utf8Json is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(utf8Json));
            }
            if (jsonTypeInfo is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(jsonTypeInfo));
            }

            jsonTypeInfo.EnsureConfigured();
            return jsonTypeInfo.SerializeAsync(utf8Json, value, cancellationToken);
        }

        /// <summary>
        /// Converts the provided value to UTF-8 encoded JSON text and write it to the <see cref="System.IO.Stream"/>.
        /// </summary>
        /// <typeparam name="TValue">The type of the value to serialize.</typeparam>
        /// <param name="utf8Json">The UTF-8 <see cref="System.IO.Stream"/> to write to.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="jsonTypeInfo">Metadata about the type to convert.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="utf8Json"/> is <see langword="null"/>.
        /// </exception>
        public static void Serialize<TValue>(
            Stream utf8Json,
            TValue value,
            JsonTypeInfo<TValue> jsonTypeInfo)
        {
            if (utf8Json is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(utf8Json));
            }
            if (jsonTypeInfo is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(jsonTypeInfo));
            }

            jsonTypeInfo.EnsureConfigured();
            jsonTypeInfo.Serialize(utf8Json, value);
        }

        /// <summary>
        /// Converts the provided value to UTF-8 encoded JSON text and write it to the <see cref="System.IO.Stream"/>.
        /// </summary>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        /// <param name="utf8Json">The UTF-8 <see cref="System.IO.Stream"/> to write to.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="jsonTypeInfo">Metadata about the type to convert.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken"/> that can be used to cancel the write operation.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="utf8Json"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="InvalidCastException">
        /// <paramref name="value"/> does not match the type of <paramref name="jsonTypeInfo"/>.
        /// </exception>
        public static Task SerializeAsync(
            Stream utf8Json,
            object? value,
            JsonTypeInfo jsonTypeInfo,
            CancellationToken cancellationToken = default)
        {
            if (utf8Json is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(utf8Json));
            }
            if (jsonTypeInfo is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(jsonTypeInfo));
            }

            jsonTypeInfo.EnsureConfigured();
            return jsonTypeInfo.SerializeAsObjectAsync(utf8Json, value, cancellationToken);
        }

        /// <summary>
        /// Converts the provided value to UTF-8 encoded JSON text and write it to the <see cref="System.IO.Stream"/>.
        /// </summary>
        /// <param name="utf8Json">The UTF-8 <see cref="System.IO.Stream"/> to write to.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="jsonTypeInfo">Metadata about the type to convert.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="utf8Json"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="InvalidCastException">
        /// <paramref name="value"/> does not match the type of <paramref name="jsonTypeInfo"/>.
        /// </exception>
        public static void Serialize(
            Stream utf8Json,
            object? value,
            JsonTypeInfo jsonTypeInfo)
        {
            if (utf8Json is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(utf8Json));
            }
            if (jsonTypeInfo is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(jsonTypeInfo));
            }

            jsonTypeInfo.EnsureConfigured();
            jsonTypeInfo.SerializeAsObject(utf8Json, value);
        }

        /// <summary>
        /// Converts the provided value to UTF-8 encoded JSON text and write it to the <see cref="System.IO.Stream"/>.
        /// </summary>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        /// <param name="utf8Json">The UTF-8 <see cref="System.IO.Stream"/> to write to.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="inputType">The type of the <paramref name="value"/> to convert.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken"/> that can be used to cancel the write operation.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="inputType"/> is not compatible with <paramref name="value"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="utf8Json"/>, <paramref name="inputType"/>, or <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <paramref name="inputType"/>  or its serializable members.
        /// </exception>
        public static Task SerializeAsync(
            Stream utf8Json,
            object? value,
            Type inputType,
            JsonSerializerContext context,
            CancellationToken cancellationToken = default)
        {
            if (utf8Json is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(utf8Json));
            }
            if (context is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(context));
            }

            ValidateInputType(value, inputType);
            JsonTypeInfo jsonTypeInfo = GetTypeInfo(context, inputType);
            return jsonTypeInfo.SerializeAsObjectAsync(utf8Json, value, cancellationToken);
        }

        /// <summary>
        /// Converts the provided value to UTF-8 encoded JSON text and write it to the <see cref="System.IO.Stream"/>.
        /// </summary>
        /// <param name="utf8Json">The UTF-8 <see cref="System.IO.Stream"/> to write to.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="inputType">The type of the <paramref name="value"/> to convert.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="inputType"/> is not compatible with <paramref name="value"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="utf8Json"/>, <paramref name="inputType"/>, or <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <paramref name="inputType"/>  or its serializable members.
        /// </exception>
        public static void Serialize(
            Stream utf8Json,
            object? value,
            Type inputType,
            JsonSerializerContext context)
        {
            if (utf8Json is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(utf8Json));
            }
            if (context is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(context));
            }

            ValidateInputType(value, inputType);
            JsonTypeInfo jsonTypeInfo = GetTypeInfo(context, inputType);
            jsonTypeInfo.SerializeAsObject(utf8Json, value);
        }
    }
}
