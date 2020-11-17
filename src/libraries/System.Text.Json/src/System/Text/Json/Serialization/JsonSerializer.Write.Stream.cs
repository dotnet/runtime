// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        /// <summary>
        /// Convert the provided value to UTF-8 encoded JSON text and write it to the <see cref="System.IO.Stream"/>.
        /// </summary>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        /// <param name="utf8Json">The UTF-8 <see cref="System.IO.Stream"/> to write to.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="options">Options to control the conversion behavior.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken"/> which may be used to cancel the write operation.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="utf8Json"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <typeparamref name="TValue"/> or its serializable members.
        /// </exception>
        public static Task SerializeAsync<[DynamicallyAccessedMembers(MembersAccessedOnWrite)] TValue>(
            Stream utf8Json,
            TValue value,
            JsonSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            if (utf8Json == null)
                throw new ArgumentNullException(nameof(utf8Json));

            return WriteAsyncCore(utf8Json, value, typeof(TValue), options, cancellationToken);
        }

        /// <summary>
        /// Convert the provided value to UTF-8 encoded JSON text and write it to the <see cref="System.IO.Stream"/>.
        /// </summary>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        /// <param name="utf8Json">The UTF-8 <see cref="System.IO.Stream"/> to write to.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="inputType">The type of the <paramref name="value"/> to convert.</param>
        /// <param name="options">Options to control the conversion behavior.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken"/> which may be used to cancel the write operation.</param>
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
        public static Task SerializeAsync(
            Stream utf8Json,
            object? value,
            [DynamicallyAccessedMembers(MembersAccessedOnWrite)] Type inputType,
            JsonSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            if (utf8Json == null)
            {
                throw new ArgumentNullException(nameof(utf8Json));
            }

            if (inputType == null)
            {
                throw new ArgumentNullException(nameof(inputType));
            }

            if (value != null && !inputType.IsAssignableFrom(value.GetType()))
            {
                ThrowHelper.ThrowArgumentException_DeserializeWrongType(inputType, value);
            }

            return WriteAsyncCore<object>(utf8Json, value!, inputType, options, cancellationToken);
        }

        private static async Task WriteAsyncCore<TValue>(
            Stream utf8Json,
            TValue value,
            Type inputType,
            JsonSerializerOptions? options,
            CancellationToken cancellationToken)
        {
            // We flush the Stream when the buffer is >=90% of capacity.
            // This threshold is a compromise between buffer utilization and minimizing cases where the buffer
            // needs to be expanded\doubled because it is not large enough to write the current property or element.
            // We check for flush after each object property and array element is written to the buffer.
            // Once the buffer is expanded to contain the largest single element\property, a 90% thresold
            // means the buffer may be expanded a maximum of 4 times: 1-(1\(2^4))==.9375.
            const float FlushThreshold = .9f;

            if (options == null)
            {
                options = JsonSerializerOptions.s_defaultOptions;
            }

            JsonWriterOptions writerOptions = options.GetWriterOptions();

            using (var bufferWriter = new PooledByteBufferWriter(options.DefaultBufferSize))
            using (var writer = new Utf8JsonWriter(bufferWriter, writerOptions))
            {
                //  We treat typeof(object) special and allow polymorphic behavior.
                if (inputType == JsonClassInfo.ObjectType && value != null)
                {
                    inputType = value!.GetType();
                }

                WriteStack state = default;
                JsonConverter converterBase = state.Initialize(inputType, options, supportContinuation: true);

                bool isFinalBlock;

                do
                {
                    state.FlushThreshold = (int)(bufferWriter.Capacity * FlushThreshold);

                    isFinalBlock = WriteCore(converterBase, writer, value, options, ref state);

                    await bufferWriter.WriteToStreamAsync(utf8Json, cancellationToken).ConfigureAwait(false);

                    bufferWriter.Clear();
                } while (!isFinalBlock);
            }
        }
    }
}
