// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static IEnumerable<TSource> Reverse<TSource>(this IEnumerable<TSource> source)
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (IsEmptyArray(source))
            {
                return [];
            }

            return new ReverseIterator<TSource>(source);
        }

        /// <summary>
        /// An iterator that yields the items of an <see cref="IEnumerable{TSource}"/> in reverse.
        /// </summary>
        /// <typeparam name="TSource">The type of the source enumerable.</typeparam>
        private sealed partial class ReverseIterator<TSource> : Iterator<TSource>
        {
            private readonly IEnumerable<TSource> _source;
            private TSource[]? _buffer;

            public ReverseIterator(IEnumerable<TSource> source)
            {
                Debug.Assert(source is not null);
                _source = source;
            }

            public override Iterator<TSource> Clone() => new ReverseIterator<TSource>(_source);

            public override bool MoveNext()
            {
                if (_state - 2 <= -2)
                {
                    // Either someone called a method and cast us to IEnumerable without calling GetEnumerator,
                    // or we were already disposed. In either case, iteration has ended, so return false.
                    // A comparison is made against -2 instead of _state <= 0 because we want to handle cases where
                    // the source is really large and adding the bias causes _state to overflow.
                    Debug.Assert(_state == -1 || _state == 0);
                    Dispose();
                    return false;
                }

                switch (_state)
                {
                    case 1:
                        // Iteration has just started. Capture the source into an array and set _state to 2 + the count.
                        // Having an extra field for the count would be more readable, but we save it into _state with a
                        // bias instead to minimize field size of the iterator.
                        TSource[] buffer = _source.ToArray();
                        _buffer = buffer;
                        _state = buffer.Length + 2;
                        goto default;
                    default:
                        // At this stage, _state starts from 2 + the count. _state - 3 represents the current index into the
                        // buffer. It is continuously decremented until it hits 2, which means that we've run out of items to
                        // yield and should return false.
                        int index = _state - 3;
                        if (index != -1)
                        {
                            Debug.Assert(_buffer is not null);
                            _current = _buffer[index];
                            --_state;
                            return true;
                        }

                        break;
                }

                Dispose();
                return false;
            }

            public override void Dispose()
            {
                _buffer = null; // Just in case this ends up being long-lived, allow the memory to be reclaimed.
                base.Dispose();
            }
        }
    }
}
