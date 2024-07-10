// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace System.Net.ServerSentEvents
{
    /// <summary>Provides a parser for parsing server-sent events.</summary>
    public static class SseParser
    {
        /// <summary>The default <see cref="SseItem{T}.EventType"/> ("message") for an event that did not explicitly specify a type.</summary>
        public const string EventTypeDefault = "message";

        /// <summary>Creates a parser for parsing a <paramref name="sseStream"/> of server-sent events into a sequence of <see cref="SseItem{String}"/> values.</summary>
        /// <param name="sseStream">The stream containing the data to parse.</param>
        /// <returns>
        /// The enumerable of strings, which may be enumerated synchronously or asynchronously. The strings
        /// are decoded from the UTF8-encoded bytes of the payload of each event.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="sseStream"/> is null.</exception>
        /// <remarks>
        /// This overload has behavior equivalent to calling <see cref="Create{T}(Stream, SseItemParser{T})"/> with a delegate
        /// that decodes the data of each event using <see cref="Encoding.UTF8"/>'s GetString method.
        /// </remarks>
        public static SseParser<string> Create(Stream sseStream) =>
            Create(sseStream, static (_, bytes) => Utf8GetString(bytes));

        /// <summary>Creates a parser for parsing a <paramref name="sseStream"/> of server-sent events into a sequence of <see cref="SseItem{T}"/> values.</summary>
        /// <typeparam name="T">Specifies the type of data in each event.</typeparam>
        /// <param name="sseStream">The stream containing the data to parse.</param>
        /// <param name="itemParser">The parser to use to transform each payload of bytes into a data element.</param>
        /// <returns>The enumerable, which may be enumerated synchronously or asynchronously.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="sseStream"/> is null.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="itemParser"/> is null.</exception>
        public static SseParser<T> Create<T>(Stream sseStream, SseItemParser<T> itemParser) =>
            new SseParser<T>(
                sseStream ?? throw new ArgumentNullException(nameof(sseStream)),
                itemParser ?? throw new ArgumentNullException(nameof(itemParser)));

        /// <summary>Encoding.UTF8.GetString(bytes)</summary>
        internal static unsafe string Utf8GetString(ReadOnlySpan<byte> bytes)
        {
#if NET
            return Encoding.UTF8.GetString(bytes);
#else
            fixed (byte* ptr = bytes)
            {
                return ptr is null ?
                    string.Empty :
                    Encoding.UTF8.GetString(ptr, bytes.Length);
            }
#endif
        }
    }
}
