// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class AsyncEnumerable
    {
        /// <summary>Generates a sequence that contains one repeated value.</summary>
        /// <typeparam name="TResult">The type of the value to be repeated in the result sequence.</typeparam>
        /// <param name="element">The value to be repeated.</param>
        /// <param name="count">The number of times to repeat the value in the generated sequence.</param>
        /// <returns>An <see cref="IAsyncEnumerable{T}"/> that contains a repeated value.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is less than 0.</exception>
        public static IAsyncEnumerable<TResult> Repeat<TResult>(TResult element, int count)
        {
            if (count == 0)
            {
                return Empty<TResult>();
            }

            ThrowHelper.ThrowIfNegative(count);

            return Impl(element, count);

            static async IAsyncEnumerable<TResult> Impl(TResult element, int count)
            {
                while (count-- != 0)
                {
                    yield return element;
                }
            }
        }
    }
}
