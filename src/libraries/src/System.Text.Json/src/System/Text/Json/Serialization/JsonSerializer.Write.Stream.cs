// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
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
        public static Task WriteAsync<TValue>(Stream utf8Json, TValue value, JsonSerializerOptions options = null, CancellationToken cancellationToken = default)
        {
            return WriteAsyncCore(utf8Json, value, typeof(TValue), options, cancellationToken);
        }

        /// <summary>
        /// Convert the provided value to UTF-8 encoded JSON text and write it to the <see cref="System.IO.Stream"/>.
        /// </summary>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        /// <param name="utf8Json">The UTF-8 <see cref="System.IO.Stream"/> to write to.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="type">The type of the <paramref name="value"/> to convert.</param>
        /// <param name="options">Options to control the conversion behavior.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken"/> which may be used to cancel the write operation.</param>
        public static Task WriteAsync(Stream utf8Json, object value, Type type, JsonSerializerOptions options = null, CancellationToken cancellationToken = default)
        {
            if (utf8Json == null)
                throw new ArgumentNullException(nameof(utf8Json));

            VerifyValueAndType(value, type);

            return WriteAsyncCore(utf8Json, value, type, options, cancellationToken);
        }

        private static async Task WriteAsyncCore(Stream utf8Json, object value, Type type, JsonSerializerOptions options, CancellationToken cancellationToken)
        {
            if (options == null)
            {
                options = JsonSerializerOptions.s_defaultOptions;
            }

            JsonWriterOptions writerOptions = options.GetWriterOptions();

            using (var bufferWriter = new PooledByteBufferWriter(options.DefaultBufferSize))
            using (var writer = new Utf8JsonWriter(bufferWriter, writerOptions))
            {
                if (value == null)
                {
                    writer.WriteNullValue();
                    writer.Flush();

                    await bufferWriter.WriteToStreamAsync(utf8Json, cancellationToken).ConfigureAwait(false);

                    return;
                }

                if (type == null)
                {
                    type = value.GetType();
                }

                WriteStack state = default;
                state.Current.Initialize(type, options);
                state.Current.CurrentValue = value;

                bool isFinalBlock;

                int flushThreshold;
                do
                {
                    flushThreshold = (int)(bufferWriter.Capacity * .9); //todo: determine best value here

                    isFinalBlock = Write(writer, flushThreshold, options, ref state);
                    writer.Flush();

                    await bufferWriter.WriteToStreamAsync(utf8Json, cancellationToken).ConfigureAwait(false);

                    bufferWriter.Clear();
                } while (!isFinalBlock);
            }
            
            // todo: verify that we do want to call FlushAsync here (or above). It seems like leaving it to the caller would be best.
        }
    }
}
