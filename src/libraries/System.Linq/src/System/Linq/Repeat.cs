// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static IEnumerable<TResult> Repeat<TResult>(TResult element, int count)
        {
            if (count < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count);
            }

            if (count == 0)
            {
                return [];
            }

            return new RepeatIterator<TResult>(element, count);
        }

        /// <summary>
        /// An iterator that yields the same item multiple times.
        /// </summary>
        /// <typeparam name="TResult">The type of the item.</typeparam>
        [DebuggerDisplay("Count = {_count}")]
        private sealed partial class RepeatIterator<TResult> : Iterator<TResult>
        {
            private readonly int _count;

            public RepeatIterator(TResult element, int count)
            {
                Debug.Assert(count > 0);
                _current = element;
                _count = count;
            }

            public override Iterator<TResult> Clone()
            {
                return new RepeatIterator<TResult>(_current, _count);
            }

            public override void Dispose()
            {
                // Don't let base.Dispose wipe Current.
                _state = -1;
            }

            public override bool MoveNext()
            {
                // Having a separate field for the number of sent items would be more readable.
                // However, we save it into _state with a bias to minimize field size of the iterator.
                int sent = _state - 1;

                // We can't have sent a negative number of items, obviously. However, if this iterator
                // was illegally casted to IEnumerator without GetEnumerator being called, or if we've
                // already been disposed, then `sent` will be negative.
                if (sent >= 0 && sent != _count)
                {
                    ++_state;
                    return true;
                }

                Dispose();
                return false;
            }
        }
    }
}
