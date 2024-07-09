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
        /// <param name="rawValue">The span containing the raw JSON data of <paramref name="element"/>.</param>
        /// <returns>
        ///   <see langword="true"/> if <paramref name="element"/> is backed by a UTF-8 encoded <see cref="JsonDocument"/>,
        ///   <see langword="false"/> otherwise.
        /// </returns>
        /// <exception cref="ObjectDisposedException">The underlying <see cref="JsonDocument"/> has been disposed.</exception>
        public static bool TryGetRawValue(JsonElement element, out ReadOnlySpan<byte> rawValue)
        {
            rawValue = element.GetRawValue().Span;
            return true;
        }
    }
}
