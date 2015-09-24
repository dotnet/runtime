// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace System.Linq.Parallel.Tests
{
    internal static class Enumerables<T>
    {
        /// <summary>
        /// Get an enumerable that throws an error on the first enumeration.
        /// </summary>
        /// <returns>An enumerable that throws on the first enumeration.</returns>
        public static IEnumerable<T> ThrowOnEnumeration()
        {
            return ThrowOnEnumeration(1);
        }

        /// <summary>
        /// Get an enumerable that throws an error after the given number of enumerations.
        /// </summary>
        /// <param name="count">The number of enumerations until the error is thrown.</param>
        /// <returns>An enumerable that throws on the given enumeration.</returns>
        public static IEnumerable<T> ThrowOnEnumeration(int count)
        {
            return Enumerable.Range(0, count).Select(i =>
            {
                if (i == count - 1) throw new DeliberateTestException();
                return default(T);
            });
        }
    }
}
