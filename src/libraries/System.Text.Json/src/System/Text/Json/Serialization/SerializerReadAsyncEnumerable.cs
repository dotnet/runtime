// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace System.Text.Json.Serialization
{
    internal struct SerializerReadAsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        public SerializerReadAsyncEnumerable(Stream stream, JsonSerializerOptions? options)
        {
            Stream = stream;
            Options = options;
        }

        /// <summary>
        /// todo
        /// </summary>
        public Stream Stream { get; set; }

        /// <summary>
        /// todo
        /// </summary>
        public JsonSerializerOptions? Options { get; set; }

        public SerializerReadAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            return new SerializerReadAsyncEnumerator<T>(Stream, Options);
        }

        IAsyncEnumerator<T> IAsyncEnumerable<T>.GetAsyncEnumerator(CancellationToken cancellationToken)
        {
            return GetAsyncEnumerator(cancellationToken);
        }
    }
}
