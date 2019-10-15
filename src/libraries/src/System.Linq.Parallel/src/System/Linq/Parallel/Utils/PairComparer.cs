// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// PairComparer.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;

namespace System.Linq.Parallel
{
    /// <summary>
    /// PairComparer compares pairs by the first element, and breaks ties by the second
    /// element.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="U"></typeparam>
    internal sealed class PairComparer<T, U> : IComparer<Pair<T, U>>
    {
        private readonly IComparer<T> _comparer1;
        private readonly IComparer<U> _comparer2;

        public PairComparer(IComparer<T> comparer1, IComparer<U> comparer2)
        {
            _comparer1 = comparer1;
            _comparer2 = comparer2;
        }

        public int Compare(Pair<T, U> x, Pair<T, U> y)
        {
            int result1 = _comparer1.Compare(x.First, y.First);
            if (result1 != 0)
            {
                return result1;
            }

            return _comparer2.Compare(x.Second, y.Second);
        }
    }
}
