// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Collections.Immutable.Test
{
    /// <summary>
    /// Produces the same hash for every value.
    /// </summary>
    /// <typeparam name="T">The type to hash</typeparam>
    internal class BadHasher<T> : IEqualityComparer<T>
    {
        private readonly IEqualityComparer<T> equalityComparer;

        internal BadHasher(IEqualityComparer<T> equalityComparer = null)
        {
            this.equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;
        }

        public bool Equals(T x, T y)
        {
            return this.equalityComparer.Equals(x, y);
        }

        public int GetHashCode(T obj)
        {
            return 1;
        }
    }
}
