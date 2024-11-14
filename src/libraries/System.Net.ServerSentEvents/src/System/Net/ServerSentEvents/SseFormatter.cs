// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.ServerSentEvents
{
    /// <summary>
    /// Provides methods for formatting server-sent events.
    /// </summary>
    public static class SseFormatter
    {
        private static readonly byte[] s_newLine = "\n"u8.ToArray();

        /// <summary>
        /// Writes the <paramref name="source"/> of server-sent events to the <paramref name="destination"/> stream.
        /// </summary>
        /// <param name="source">The events to write to the stream.</param>
        /// <param name="destination">The destination stream to write the events.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the write operation.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        public static Task WriteAsync(IAsyncEnumerable<SseItem<string>> source, Stream destination, CancellationToken cancellationToken = default)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (destination is null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            return WriteAsyncCore(source, destination, static (writer, item) => writer.WriteAsUtf8String(item.Data), cancellationToken);
        }

        /// <summary>
        /// Writes the <paramref name="source"/> of server-sent events to the <paramref name="destination"/> stream.
        /// </summary>
        /// <typeparam name="T">The data type of the event.</typeparam>
        /// <param name="source">The events to write to the stream.</param>
        /// <param name="destination">The destination stream to write the events.</param>
        /// <param name="itemFormatter">The formatter for the data field of given event.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that can be used to cancel the write operation.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        public static Task WriteAsync<T>(IAsyncEnumerable<SseItem<T>> source, Stream destination, Action<IBufferWriter<byte>, SseItem<T>> itemFormatter, CancellationToken cancellationToken = default)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (destination is null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (itemFormatter is null)
            {
                throw new ArgumentNullException(nameof(itemFormatter));
            }

            return WriteAsyncCore(source, destination, itemFormatter, cancellationToken);
        }

        private static async Task WriteAsyncCore<T>(IAsyncEnumerable<SseItem<T>> source, Stream destination, Action<IBufferWriter<byte>, SseItem<T>> itemFormatter, CancellationToken cancellationToken)
        {
            using PooledByteBufferWriter bufferWriter = new();
            using PooledByteBufferWriter userDataBufferWriter = new();

            await foreach (SseItem<T> item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                FormatSseEvent(bufferWriter, userDataBufferWriter, itemFormatter, item);

                await destination.WriteAsync(bufferWriter.WrittenMemory, cancellationToken).ConfigureAwait(false);

                userDataBufferWriter.Reset();
                bufferWriter.Reset();
            }
        }

        private static void FormatSseEvent<T>(
            PooledByteBufferWriter bufferWriter,
            PooledByteBufferWriter userDataBufferWriter,
            Action<IBufferWriter<byte>, SseItem<T>> itemFormatter,
            SseItem<T> sseItem)
        {
            Debug.Assert(bufferWriter.WrittenCount is 0, "Must not contain any data");
            Debug.Assert(userDataBufferWriter.WrittenCount is 0, "Must not contain any data");

            if (sseItem._eventType is { } eventType)
            {
                Debug.Assert(!eventType.Contains('\n'), "Event type must not contain line breaks");

                bufferWriter.Write("event: "u8);
                bufferWriter.WriteAsUtf8String(eventType);
                bufferWriter.Write(s_newLine);
            }

            itemFormatter(userDataBufferWriter, sseItem);
            WriteDataWithLineBreakHandling(bufferWriter, userDataBufferWriter.WrittenMemory.Span);

            if (sseItem.EventId is { } eventId)
            {
                Debug.Assert(!eventId.Contains('\n'), "Event id must not contain line breaks");

                bufferWriter.Write("id: "u8);
                bufferWriter.WriteAsUtf8String(eventId);
                bufferWriter.Write(s_newLine);
            }

            bufferWriter.Write(s_newLine);
        }

        private static void WriteDataWithLineBreakHandling(PooledByteBufferWriter bufferWriter, ReadOnlySpan<byte> data)
        {
            // The data field can contain multiple lines, each line must be prefixed with "data: " and suffixed with a line break.

            ReadOnlySpan<byte> nextLine;
            int lineBreak;

            do
            {
                lineBreak = data.IndexOf((byte)'\n');
                if (lineBreak < 0)
                {
                    nextLine = data;
                    data = default;
                }
                else
                {
                    int lineLength = lineBreak > 0 && data[lineBreak - 1] == '\r'
                        ? lineBreak - 1
                        : lineBreak;

                    nextLine = data.Slice(0, lineLength);
                    data = data.Slice(lineBreak + 1);
                }

                bufferWriter.Write("data: "u8);
                bufferWriter.Write(nextLine);
                bufferWriter.Write(s_newLine);

            } while (lineBreak >= 0);
        }
    }
}
