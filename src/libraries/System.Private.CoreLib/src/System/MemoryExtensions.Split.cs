// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System
{
    public static partial class MemoryExtensions
    {
        /// <summary>
        /// Returns a type that allows for enumeration of each element within a split span
        /// using the provided separator character.
        /// </summary>
        /// <param name="source">The source span to be enumerated.</param>
        /// <param name="separator">The separator character to be used to split the provided span.</param>
        /// <returns>Returns a <see cref="System.MemoryExtensions.SpanSplitEnumerator{T}"/>.</returns>
        public static SpanSplitEnumerator<T> Split<T>(this ReadOnlySpan<T> source, T separator)
           where T : IEquatable<T> => new SpanSplitEnumerator<T>(source, separator);

        /// <summary>
        /// Returns a type that allows for enumeration of each element within a split span
        /// using the provided separator span.
        /// </summary>
        /// <param name="source">The source span to be enumerated.</param>
        /// <param name="separator">The separator span to be used to split the provided span.</param>
        /// <returns>Returns a <see cref="System.MemoryExtensions.SpanSplitEnumerator{T}"/>.</returns>
        public static SpanSplitEnumerator<T> Split<T>(this ReadOnlySpan<T> source, ReadOnlySpan<T> separator)
            where T : IEquatable<T> => new SpanSplitEnumerator<T>(source, separator, treatAsSingleSeparator: true);

        /// <summary>
        /// Returns a type that allows for enumeration of each element within a split span
        /// using any of the provided elements.
        /// </summary>
        /// <param name="source">The source span to be enumerated.</param>
        /// <param name="separators">The separators to be used to split the provided span.</param>
        /// <returns>Returns a <see cref="System.MemoryExtensions.SpanSplitEnumerator{T}"/>.</returns>
        public static SpanSplitEnumerator<T> SplitAny<T>(this ReadOnlySpan<T> source, [UnscopedRef] params ReadOnlySpan<T> separators)
            where T : IEquatable<T> => new SpanSplitEnumerator<T>(source, separators, treatAsSingleSeparator: false);

        /// <summary>
        /// Returns a type that allows for enumeration of each element within a split span
        /// using the provided <see cref="System.MemoryExtensions.SpanSplitEnumerator{T}"/>.
        /// </summary>
        /// <param name="source">The source span to be enumerated.</param>
        /// <param name="separators">The <see cref="System.MemoryExtensions.SpanSplitEnumerator{T}"/> to be used to split the provided span.</param>
        /// <returns>Returns a <see cref="System.MemoryExtensions.SpanSplitEnumerator{T}"/>.</returns>
        public static SpanSplitEnumerator<T> SplitAny<T>(this ReadOnlySpan<T> source, SearchValues<T> separators)
            where T : IEquatable<T> => new SpanSplitEnumerator<T>(source, separators);

        private enum SpanSplitEnumeratorMode
        {
            None = 0,
            SingleElement,
            Sequence,
            EmptySequence,
            Any,
            SearchValues
        }

        /// <summary>
        /// <see cref="System.MemoryExtensions.SpanSplitEnumerator{T}"/> allows for enumeration of each element within a <see cref="System.ReadOnlySpan{T}"/>
        /// that has been split using a provided separator.
        /// </summary>
        public ref struct SpanSplitEnumerator<T> where T : IEquatable<T>
        {
            private readonly ReadOnlySpan<T> _span;

            private readonly T _separator = default!;
            private readonly ReadOnlySpan<T> _separatorBuffer;
            private readonly SearchValues<T> _searchValues = default!;

            private readonly SpanSplitEnumeratorMode _splitMode;

            private int _startCurrent = 0;
            private int _endCurrent = 0;
            private int _startNext = 0;

            /// <summary>
            /// Returns an enumerator that allows for iteration over the split span.
            /// </summary>
            /// <returns>Returns a <see cref="System.MemoryExtensions.SpanSplitEnumerator{T}"/> that can be used to iterate over the split span.</returns>
            public SpanSplitEnumerator<T> GetEnumerator() => this;

            /// <summary>
            /// Returns the current element of the enumeration.
            /// </summary>
            /// <returns>Returns a <see cref="System.Range"/> instance that indicates the bounds of the current element withing the source span.</returns>
            public Range Current => new Range(_startCurrent, _endCurrent);

            internal SpanSplitEnumerator(ReadOnlySpan<T> span, SearchValues<T> searchValues)
            {
                _span = span;
                _splitMode = SpanSplitEnumeratorMode.SearchValues;
                _searchValues = searchValues;
            }

            internal SpanSplitEnumerator(ReadOnlySpan<T> span, ReadOnlySpan<T> separator, bool treatAsSingleSeparator)
            {
                _span = span;
                _separatorBuffer = separator;
                _splitMode = (separator.Length, treatAsSingleSeparator) switch
                {
                    (0, true) => SpanSplitEnumeratorMode.EmptySequence,
                    (_, true) => SpanSplitEnumeratorMode.Sequence,
                    _ => SpanSplitEnumeratorMode.Any
                };
            }

            internal SpanSplitEnumerator(ReadOnlySpan<T> span, T separator)
            {
                _span = span;
                _separator = separator;
                _splitMode = SpanSplitEnumeratorMode.SingleElement;
            }

            /// <summary>
            /// Advances the enumerator to the next element of the enumeration.
            /// </summary>
            /// <returns><see langword="true"/> if the enumerator was successfully advanced to the next element; <see langword="false"/> if the enumerator has passed the end of the enumeration.</returns>
            public bool MoveNext()
            {
                if (_splitMode is SpanSplitEnumeratorMode.None || _startNext > _span.Length)
                {
                    return false;
                }

                ReadOnlySpan<T> slice = _span[_startNext..];

                Debug.Assert(_splitMode is not SpanSplitEnumeratorMode.None);
                (int separatorIndex, int separatorLength) = _splitMode switch
                {
                    SpanSplitEnumeratorMode.SingleElement => (slice.IndexOf(_separator), 1),
                    SpanSplitEnumeratorMode.Sequence => (slice.IndexOf(_separatorBuffer), _separatorBuffer.Length),
                    SpanSplitEnumeratorMode.EmptySequence => (-1, 1),
                    SpanSplitEnumeratorMode.Any => (slice.IndexOfAny(_separatorBuffer), 1),
                    _ => (_searchValues.IndexOfAny(slice), 1)
                };

                int elementLength = (separatorIndex != -1 ? separatorIndex : slice.Length);

                _startCurrent = _startNext;
                _endCurrent = _startCurrent + elementLength;
                _startNext = _endCurrent + separatorLength;
                return true;
            }
        }
    }
}
