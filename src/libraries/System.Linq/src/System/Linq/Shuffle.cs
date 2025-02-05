// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    public static partial class Enumerable
    {
        /// <summary>Shuffles the order of the elements of a sequence.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence of values to shuffle.</param>
        /// <returns>A sequence whose elements correspond to those of the input sequence in randomized order.</returns>
        /// <remarks>Randomization is performed using a non-cryptographically-secure random number generator.</remarks>
        public static IEnumerable<TSource> Shuffle<TSource>(this IEnumerable<TSource> source)
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (IsEmptyArray(source))
            {
                return [];
            }

            return new ShuffleIterator<TSource>(source);
        }

        private sealed partial class ShuffleIterator<TSource> : Iterator<TSource>
        {
            private readonly IEnumerable<TSource> _source;
            private TSource[]? _buffer;

            public ShuffleIterator(IEnumerable<TSource> source)
            {
                Debug.Assert(source is not null);
                _source = source;
            }

            private protected override Iterator<TSource> Clone() => new ShuffleIterator<TSource>(_source);

            public override bool MoveNext()
            {
                int state = _state;

            Initialized:
                if (state > 1)
                {
                    TSource[]? buffer = _buffer;
                    Debug.Assert(buffer is not null);

                    int i = state - 2;
                    if ((uint)i < (uint)buffer.Length)
                    {
                        _current = buffer[i];
                        _state++;
                        return true;
                    }
                }
                else if (state == 1)
                {
                    TSource[] buffer = _source.ToArray();
                    if (buffer.Length != 0)
                    {
                        Random.Shared.Shuffle(buffer);
                        _buffer = buffer;
                        _state = state = 2;
                        goto Initialized;
                    }
                }

                Dispose();
                return false;
            }

            public override void Dispose()
            {
                _buffer = null;
                base.Dispose();
            }
        }
    }
}
