// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// An unsafe class that provides a set of methods to access the underlying data representations of JSON types.
    /// </summary>
    public static class JsonMarshal
    {
        /// <summary>
        /// Gets a <see cref="ReadOnlySpan{T}"/> view over the raw JSON data of the given <see cref="JsonElement"/>.
        /// </summary>
        /// <param name="element">The JSON element from which to extract the span.</param>
        /// <returns>The span containing the raw JSON data of<paramref name="element"/>.</returns>
        /// <exception cref="ObjectDisposedException">The underlying <see cref="JsonDocument"/> has been disposed.</exception>
        /// <remarks>
        /// While the method itself does check for disposal of the underlying <see cref="JsonDocument"/>,
        /// it is possible that it could be disposed after the method returns, which would result in
        /// the span pointing to a buffer that has been returned to the shared pool. Callers should take
        /// extra care to make sure that such a scenario isn't possible to avoid potential data corruption.
        /// </remarks>
        public static ReadOnlySpan<byte> GetRawUtf8Value(JsonElement element)
        {
            return element.GetRawValue().Span;
        }
    }
}
