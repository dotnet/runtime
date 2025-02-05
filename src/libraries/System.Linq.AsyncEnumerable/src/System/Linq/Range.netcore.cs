// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Numerics;

namespace System.Linq
{
    public static partial class AsyncEnumerable
    {
        /// <summary>Generates a sequence of integral numbers within a specified range.</summary>
        /// <typeparam name="T">The <see cref="IBinaryInteger{TSelf}"/> type of the elements in the sequence.</typeparam>
        /// <param name="start">The value of the first <see cref="IBinaryInteger{TSelf}"/> in the sequence.</param>
        /// <param name="count">The number of sequential <see cref="IBinaryInteger{TSelf}"/> to generate.</param>
        /// <returns>An <see cref="IAsyncEnumerable{T}"/> that contains a range of sequential integral numbers.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is less than 0</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="start"/> + <paramref name="count"/> -1 is larger than <see cref="IMinMaxValue{TSelf}.MaxValue"/>.</exception>
        public static IAsyncEnumerable<T> Range<T>(T start, int count) where T : IBinaryInteger<T>
        {
            if (count == 0)
            {
                return Empty<T>();
            }

            if (count < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(count));
            }

            T max = start + T.CreateTruncating(count - 1);
            if (start > max || StartMaxCount(start, max) + 1 != count)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(count));
            }

            return Impl(start, count);

            static async IAsyncEnumerable<T> Impl(T start, int count)
            {
                for (int i = 0; i < count; i++, start++)
                {
                    yield return start;
                }
            }

            static int StartMaxCount(T start, T max)
            {
                int count = int.CreateTruncating(max - start);
                if (count < 0)
                    count = int.CreateTruncating(max) - int.CreateTruncating(start);
                return count;
            }
        }
    }
}
