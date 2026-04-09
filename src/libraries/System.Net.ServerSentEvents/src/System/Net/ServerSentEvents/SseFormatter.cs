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
                ThrowHelper.ThrowArgumentNullException(nameof(source));
            }

            if (destination is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(destination));
            }

            return WriteAsyncCore(source, destination, static (item, writer) => writer.WriteUtf8String(item.Data), cancellationToken);
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
        public static Task WriteAsync<T>(IAsyncEnumerable<SseItem<T>> source, Stream destination, Action<SseItem<T>, IBufferWriter<byte>> itemFormatter, CancellationToken cancellationToken = default)
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(source));
            }

            if (destination is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(destination));
            }

            if (itemFormatter is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(itemFormatter));
            }

            return WriteAsyncCore(source, destination, itemFormatter, cancellationToken);
        }

        private static async Task WriteAsyncCore<T>(IAsyncEnumerable<SseItem<T>> source, Stream destination, Action<SseItem<T>, IBufferWriter<byte>> itemFormatter, CancellationToken cancellationToken)
        {
            using PooledByteBufferWriter bufferWriter = new();
            using PooledByteBufferWriter userDataBufferWriter = new();

            await foreach (SseItem<T> item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                itemFormatter(item, userDataBufferWriter);

                FormatSseEvent(
                    bufferWriter,
                    eventType: item._eventType, // Do not use the public property since it normalizes to "message" if null
                    data: userDataBufferWriter.WrittenMemory.Span,
                    eventId: item.EventId,
                    reconnectionInterval: item.ReconnectionInterval);

                await destination.WriteAsync(bufferWriter.WrittenMemory, cancellationToken).ConfigureAwait(false);

                userDataBufferWriter.Reset();
                bufferWriter.Reset();
            }
        }

        private static void FormatSseEvent(
            PooledByteBufferWriter bufferWriter,
            string? eventType,
            ReadOnlySpan<byte> data,
            string? eventId,
            TimeSpan? reconnectionInterval)
        {
            Debug.Assert(bufferWriter.WrittenCount is 0);

            if (eventType is not null)
            {
                Debug.Assert(!eventType.ContainsLineBreaks());

                bufferWriter.WriteUtf8String("event: "u8);
                bufferWriter.WriteUtf8String(eventType);
                bufferWriter.WriteUtf8String(s_newLine);
            }

            WriteLinesWithPrefix(bufferWriter, prefix: "data: "u8, data);
            bufferWriter.Write(s_newLine);

            if (eventId is not null)
            {
                Debug.Assert(!eventId.ContainsLineBreaks());

                bufferWriter.WriteUtf8String("id: "u8);
                bufferWriter.WriteUtf8String(eventId);
                bufferWriter.WriteUtf8String(s_newLine);
            }

            if (reconnectionInterval is { } retry)
            {
                Debug.Assert(retry >= TimeSpan.Zero);

                bufferWriter.WriteUtf8String("retry: "u8);
                bufferWriter.WriteUtf8Number((long)retry.TotalMilliseconds);
                bufferWriter.WriteUtf8String(s_newLine);
            }

            bufferWriter.WriteUtf8String(s_newLine);
        }

        private static void WriteLinesWithPrefix(PooledByteBufferWriter writer, ReadOnlySpan<byte> prefix, ReadOnlySpan<byte> data)
        {
            // Writes a potentially multi-line string, prefixing each line with the given prefix.
            // Both \n and \r\n sequences are normalized to \n.

            while (true)
            {
                writer.WriteUtf8String(prefix);

                int i = data.IndexOfAny((byte)'\r', (byte)'\n');
                if (i < 0)
                {
                    writer.WriteUtf8String(data);
                    return;
                }

                int lineLength = i;
                if (data[i++] == '\r' && i < data.Length && data[i] == '\n')
                {
                    i++;
                }

                ReadOnlySpan<byte> nextLine = data.Slice(0, lineLength);
                data = data.Slice(i);

                writer.WriteUtf8String(nextLine);
                writer.WriteUtf8String(s_newLine);
            }
        }
    }
}
