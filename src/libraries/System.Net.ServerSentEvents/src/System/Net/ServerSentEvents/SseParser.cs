// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
namespace System.Net.ServerSentEvents
{
    /// <summary>Provides a parser for parsing server-sent events.</summary>
    public static class SseParser
    {
        /// <summary>The default <see cref="SseItem{T}.EventType"/> ("message") for an event that did not explicitly specify a type.</summary>
        public const string EventTypeDefault = "message";

        /// <summary>Creates a parser for parsing a <paramref name="sseStream"/> of server-sent events into a sequence of <see cref="SseItem{T}"/> values.</summary>
        /// <typeparam name="T">Specifies the type of data in each event.</typeparam>
        /// <param name="sseStream">The stream containing the data to parse.</param>
        /// <param name="options">The options to use when parsing the stream.</param>
        /// <returns>The enumerable, which can be enumerated synchronously or asynchronously.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="sseStream"/> or <paramref name="options"/> is null.</exception>
        public static SseParser<T> Create<T>(Stream sseStream, SseParserOptions<T> options)
        {
            if (sseStream is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(sseStream));
            }

            if (options is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(options));
            }

            return new SseParser<T>(sseStream, options);
        }
    }
}
