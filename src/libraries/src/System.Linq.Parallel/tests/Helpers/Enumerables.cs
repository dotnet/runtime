// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            return ThrowOnEnumeration(new DeliberateTestException(), count);
        }

        /// <summary>
        /// Get an enumerable that throws the given exception after the given number of enumerations.
        /// </summary>
        /// <param name="count">The number of enumerations until the error is thrown.</param>The
        /// <param name="e">The exception to throw.</param>
        /// <returns>An enumerable that throws on the given enumeration.</returns>
        public static IEnumerable<T> ThrowOnEnumeration(Exception e, int count)
        {
            return Enumerable.Range(0, count).Select(i =>
            {
                if (i == count - 1) throw e;
                return default(T);
            });
        }
    }
}
