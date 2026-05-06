// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Linq
{
    public static partial class AsyncEnumerable
    {
        /// <summary>Generates an infinite sequence that begins with <paramref name="start"/> and yields additional values each incremented by <paramref name="step"/>.</summary>
        /// <typeparam name="T">The type of the value to be yielded in the result sequence.</typeparam>
        /// <param name="start">The starting value.</param>
        /// <param name="step">The amount by which the next yielded value should be incremented from the previous yielded value.</param>
        /// <returns>An <see cref="IAsyncEnumerable{T}"/> that contains the sequence.</returns>
        public static IAsyncEnumerable<T> InfiniteSequence<T>(T start, T step) where T : IAdditionOperators<T, T, T>
        {
            if (start is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(start));
            }

            if (step is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(step));
            }

            return Iterator(start, step);

            static async IAsyncEnumerable<T> Iterator(T start, T step)
            {
                while (true)
                {
                    yield return start;
                    start += step;
                }
            }
        }
    }
}
