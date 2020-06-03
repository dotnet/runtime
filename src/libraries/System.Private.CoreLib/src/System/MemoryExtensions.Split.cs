// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
    public static partial class MemoryExtensions
    {
        /// <summary>
        /// Returns a type that allows for enumeration of each element within a split span
        /// using a single space as a separator character.
        /// </summary>
        /// <param name="span">The source span to be enumerated.</param>
        /// <returns>Returns a <see cref="System.SpanSplitEnumerator{T}"/>.</returns>
        public static SpanSplitEnumerator<char> Split(this ReadOnlySpan<char> span)
            => new SpanSplitEnumerator<char>(span, ' ');

        /// <summary>
        /// Returns a type that allows for enumeration of each element within a split span
        /// using the provided separator character.
        /// </summary>
        /// <param name="span">The source span to be enumerated.</param>
        /// <param name="separator">The separator character to be used to split the provided span.</param>
        /// <returns>Returns a <see cref="System.SpanSplitEnumerator{T}"/>.</returns>
        public static SpanSplitEnumerator<char> Split(this ReadOnlySpan<char> span, char separator)
            => new SpanSplitEnumerator<char>(span, separator);

        /// <summary>
        /// Returns a type that allows for enumeration of each element within a split span
        /// using the provided separator string.
        /// </summary>
        /// <param name="span">The source span to be enumerated.</param>
        /// <param name="separator">The separator string to be used to split the provided span.</param>
        /// <returns>Returns a <see cref="System.SpanSplitEnumerator{T}"/>.</returns>
        public static SpanSplitEnumerator<char> Split(this ReadOnlySpan<char> span, string separator)
            => new SpanSplitEnumerator<char>(span, separator ?? string.Empty);
    }
}
