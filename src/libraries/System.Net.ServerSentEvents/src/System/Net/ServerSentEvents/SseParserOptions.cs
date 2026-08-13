// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.ServerSentEvents
{
    /// <summary>Provides options for parsing server-sent events.</summary>
    /// <typeparam name="T">Specifies the type of data parsed from an event.</typeparam>
    public sealed class SseParserOptions<T>
    {
        /// <summary>Initializes a new instance of the <see cref="SseParserOptions{T}"/> class.</summary>
        /// <param name="itemParser">The parser to use to transform each payload of bytes into a data element.</param>
        /// <exception cref="ArgumentNullException"><paramref name="itemParser"/> is null.</exception>
        public SseParserOptions(SseItemParser<T> itemParser)
        {
            if (itemParser is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(itemParser));
            }

            ItemParser = itemParser;
        }

        /// <summary>Gets the parser to use to transform each payload of bytes into a data element.</summary>
        public SseItemParser<T> ItemParser { get; }

        /// <summary>Gets or sets the maximum buffer size, or -1 to use the default limit.</summary>
        public int MaxBufferSize { get; set; } = -1;
    }
}
