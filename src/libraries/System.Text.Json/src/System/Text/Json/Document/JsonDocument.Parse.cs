// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Text.Json
{
    public sealed partial class JsonDocument
    {
        private const int UnseekableStreamInitialRentSize = 4096;

        /// <summary>
        ///   Parse memory as UTF-8-encoded text representing a single JSON value into a JsonDocument.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     The <see cref="ReadOnlyMemory{T}"/> value will be used for the entire lifetime of the
        ///     JsonDocument object, and the caller must ensure that the data therein does not change during
        ///     the object lifetime.
        ///   </para>
        ///
        ///   <para>
        ///     Because the input is considered to be text, a UTF-8 Byte-Order-Mark (BOM) must not be present.
        ///   </para>
        /// </remarks>
        /// <param name="utf8Json">JSON text to parse.</param>
        /// <param name="readerOptions">Options to control the reader behavior during parsing.</param>
        /// <returns>
        ///   A JsonDocument representation of the JSON value.
        /// </returns>
        /// <exception cref="JsonReaderException">
        ///   <paramref name="utf8Json"/> does not represent a valid single JSON value.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="readerOptions"/> contains unsupported options.
        /// </exception>
        public static JsonDocument Parse(ReadOnlyMemory<byte> utf8Json, JsonReaderOptions readerOptions = default)
        {
            CheckSupportedOptions(readerOptions);

            return Parse(utf8Json, readerOptions, null);
        }

        /// <summary>
        ///   Parse a sequence as UTF-8-encoded text representing a single JSON value into a JsonDocument.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     The <see cref="ReadOnlySequence{T}"/> may be used for the entire lifetime of the
        ///     JsonDocument object, and the caller must ensure that the data therein does not change during
        ///     the object lifetime.
        ///   </para>
        ///
        ///   <para>
        ///     Because the input is considered to be text, a UTF-8 Byte-Order-Mark (BOM) must not be present.
        ///   </para>
        /// </remarks>
        /// <param name="utf8Json">JSON text to parse.</param>
        /// <param name="readerOptions">Options to control the reader behavior during parsing.</param>
        /// <returns>
        ///   A JsonDocument representation of the JSON value.
        /// </returns>
        /// <exception cref="JsonReaderException">
        ///   <paramref name="utf8Json"/> does not represent a valid single JSON value.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="readerOptions"/> contains unsupported options.
        /// </exception>
        public static JsonDocument Parse(ReadOnlySequence<byte> utf8Json, JsonReaderOptions readerOptions = default)
        {
            CheckSupportedOptions(readerOptions);

            if (utf8Json.IsSingleSegment)
            {
                return Parse(utf8Json.First, readerOptions, null);
            }

            int length = checked((int)utf8Json.Length);
            byte[] utf8Bytes = ArrayPool<byte>.Shared.Rent(length);

            try
            {
                utf8Json.CopyTo(utf8Bytes.AsSpan());
                return Parse(utf8Bytes.AsMemory(0, length), readerOptions, utf8Bytes);
            }
            catch
            {
                // Holds document content, clear it before returning it.
                utf8Bytes.AsSpan(0, length).Clear();
                ArrayPool<byte>.Shared.Return(utf8Bytes);
                throw;
            }
        }

        /// <summary>
        ///   Parse a <see cref="Stream"/> as UTF-8-encoded data representing a single JSON value into a
        ///   JsonDocument.  The Stream will be read to completion.
        /// </summary>
        /// <param name="utf8Json">JSON data to parse.</param>
        /// <param name="readerOptions">Options to control the reader behavior during parsing.</param>
        /// <returns>
        ///   A JsonDocument representation of the JSON value.
        /// </returns>
        /// <exception cref="JsonReaderException">
        ///   <paramref name="utf8Json"/> does not represent a valid single JSON value.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="readerOptions"/> contains unsupported options.
        /// </exception>
        public static JsonDocument Parse(Stream utf8Json, JsonReaderOptions readerOptions = default)
        {
            if (utf8Json == null)
            {
                throw new ArgumentNullException(nameof(utf8Json));
            }

            CheckSupportedOptions(readerOptions);

            ArraySegment<byte> drained = ReadToEnd(utf8Json);

            try
            {
                return Parse(drained.AsMemory(), readerOptions, drained.Array);
            }
            catch
            {
                // Holds document content, clear it before returning it.
                drained.AsSpan().Clear();
                ArrayPool<byte>.Shared.Return(drained.Array);
                throw;
            }
        }

        /// <summary>
        ///   Parse a <see cref="Stream"/> as UTF-8-encoded data representing a single JSON value into a
        ///   JsonDocument.  The Stream will be read to completion.
        /// </summary>
        /// <param name="utf8Json">JSON data to parse.</param>
        /// <param name="readerOptions">Options to control the reader behavior during parsing.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>
        ///   A Task to produce a JsonDocument representation of the JSON value.
        /// </returns>
        /// <exception cref="JsonReaderException">
        ///   <paramref name="utf8Json"/> does not represent a valid single JSON value.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="readerOptions"/> contains unsupported options.
        /// </exception>
        public static Task<JsonDocument> ParseAsync(
            Stream utf8Json,
            JsonReaderOptions readerOptions = default,
            CancellationToken cancellationToken = default)
        {
            if (utf8Json == null)
            {
                throw new ArgumentNullException(nameof(utf8Json));
            }

            CheckSupportedOptions(readerOptions);

            return ParseAsyncCore(utf8Json, readerOptions, cancellationToken);
        }

        private static async Task<JsonDocument> ParseAsyncCore(
            Stream utf8Json,
            JsonReaderOptions readerOptions = default,
            CancellationToken cancellationToken = default)
        {
            ArraySegment<byte> drained = await ReadToEndAsync(utf8Json, cancellationToken).ConfigureAwait(false);

            try
            {
                return Parse(drained.AsMemory(), readerOptions, drained.Array);
            }
            catch
            {
                // Holds document content, clear it before returning it.
                drained.AsSpan().Clear();
                ArrayPool<byte>.Shared.Return(drained.Array);
                throw;
            }
        }

        /// <summary>
        ///   Parse text representing a single JSON value into a JsonDocument.
        /// </summary>
        /// <remarks>
        ///   The <see cref="ReadOnlyMemory{T}"/> value may be used for the entire lifetime of the
        ///   JsonDocument object, and the caller must ensure that the data therein does not change during
        ///   the object lifetime.
        /// </remarks>
        /// <param name="json">JSON text to parse.</param>
        /// <param name="readerOptions">Options to control the reader behavior during parsing.</param>
        /// <returns>
        ///   A JsonDocument representation of the JSON value.
        /// </returns>
        /// <exception cref="JsonReaderException">
        ///   <paramref name="json"/> does not represent a valid single JSON value.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="readerOptions"/> contains unsupported options.
        /// </exception>
        public static JsonDocument Parse(ReadOnlyMemory<char> json, JsonReaderOptions readerOptions = default)
        {
            CheckSupportedOptions(readerOptions);

            ReadOnlySpan<char> jsonChars = json.Span;
            int expectedByteCount = JsonReaderHelper.GetUtf8ByteCount(jsonChars);
            byte[] utf8Bytes = ArrayPool<byte>.Shared.Rent(expectedByteCount);

            try
            {
                int actualByteCount = JsonReaderHelper.GetUtf8FromText(jsonChars, utf8Bytes);
                Debug.Assert(expectedByteCount == actualByteCount);

                return Parse(utf8Bytes.AsMemory(0, actualByteCount), readerOptions, utf8Bytes);
            }
            catch
            {
                // Holds document content, clear it before returning it.
                utf8Bytes.AsSpan(0, expectedByteCount).Clear();
                ArrayPool<byte>.Shared.Return(utf8Bytes);
                throw;
            }
        }

        /// <summary>
        ///   Parse text representing a single JSON value into a JsonDocument.
        /// </summary>
        /// <param name="json">JSON text to parse.</param>
        /// <param name="readerOptions">Options to control the reader behavior during parsing.</param>
        /// <returns>
        ///   A JsonDocument representation of the JSON value.
        /// </returns>
        /// <exception cref="JsonReaderException">
        ///   <paramref name="json"/> does not represent a valid single JSON value.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="readerOptions"/> contains unsupported options.
        /// </exception>
        public static JsonDocument Parse(string json, JsonReaderOptions readerOptions = default)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            CheckSupportedOptions(readerOptions);

            return Parse(json.AsMemory(), readerOptions);
        }

        private static JsonDocument Parse(
            ReadOnlyMemory<byte> utf8Json,
            JsonReaderOptions readerOptions,
            byte[] extraRentedBytes)
        {
            ReadOnlySpan<byte> utf8JsonSpan = utf8Json.Span;
            Utf8JsonReader reader = new Utf8JsonReader(
                utf8JsonSpan,
                isFinalBlock: true,
                new JsonReaderState(options: readerOptions));

            var database = new MetadataDb(utf8Json.Length);
            var stack = new StackRowStack(JsonReaderState.DefaultMaxDepth * StackRow.Size);

            try
            {
                Parse(utf8JsonSpan, reader, ref database, ref stack);
            }
            catch
            {
                database.Dispose();
                throw;
            }
            finally
            {
                stack.Dispose();
            }

            return new JsonDocument(utf8Json, database, extraRentedBytes);
        }

        private static ArraySegment<byte> ReadToEnd(Stream stream)
        {
            int written = 0;
            byte[] rented = null;

            try
            {
                if (stream.CanSeek)
                {
                    // Ask for 1 more than the length to avoid resizing later,
                    // which is unnecessary in the common case where the stream length doesn't change.
                    long expectedLength = Math.Max(0, stream.Length - stream.Position) + 1;
                    rented = ArrayPool<byte>.Shared.Rent(checked((int)expectedLength));
                }
                else
                {
                    rented = ArrayPool<byte>.Shared.Rent(UnseekableStreamInitialRentSize);
                }

                int lastRead;

                do
                {
                    if (rented.Length == written)
                    {
                        byte[] toReturn = rented;
                        rented = ArrayPool<byte>.Shared.Rent(checked(toReturn.Length * 2));
                        Buffer.BlockCopy(toReturn, 0, rented, 0, toReturn.Length);
                        // Holds document content, clear it.
                        ArrayPool<byte>.Shared.Return(toReturn, clearArray: true);
                    }

                    lastRead = stream.Read(rented, written, rented.Length - written);
                    written += lastRead;
                } while (lastRead > 0);

                return new ArraySegment<byte>(rented, 0, written);
            }
            catch
            {
                if (rented != null)
                {
                    // Holds document content, clear it before returning it.
                    rented.AsSpan(0, written).Clear();
                    ArrayPool<byte>.Shared.Return(rented);
                }

                throw;
            }
        }

        private static async
#if BUILDING_INBOX_LIBRARY
            ValueTask<ArraySegment<byte>>
#else
            Task<ArraySegment<byte>>
#endif
            ReadToEndAsync(
            Stream stream,
            CancellationToken cancellationToken)
        {
            int written = 0;
            byte[] rented = null;

            try
            {
                if (stream.CanSeek)
                {
                    // Ask for 1 more than the length to avoid resizing later,
                    // which is unnecessary in the common case where the stream length doesn't change.
                    long expectedLength = Math.Max(0, stream.Length - stream.Position) + 1;
                    rented = ArrayPool<byte>.Shared.Rent(checked((int)expectedLength));
                }
                else
                {
                    rented = ArrayPool<byte>.Shared.Rent(UnseekableStreamInitialRentSize);
                }

                int lastRead;

                do
                {
                    if (rented.Length == written)
                    {
                        byte[] toReturn = rented;
                        rented = ArrayPool<byte>.Shared.Rent(toReturn.Length * 2);
                        Buffer.BlockCopy(toReturn, 0, rented, 0, toReturn.Length);
                        // Holds document content, clear it.
                        ArrayPool<byte>.Shared.Return(toReturn, clearArray: true);
                    }

                    lastRead = await stream.ReadAsync(
                        rented,
                        written,
                        rented.Length - written,
                        cancellationToken).ConfigureAwait(false);

                    written += lastRead;

                } while (lastRead > 0);

                return new ArraySegment<byte>(rented, 0, written);
            }
            catch
            {
                if (rented != null)
                {
                    // Holds document content, clear it before returning it.
                    rented.AsSpan(0, written).Clear();
                    ArrayPool<byte>.Shared.Return(rented);
                }

                throw;
            }
        }
    }
}
