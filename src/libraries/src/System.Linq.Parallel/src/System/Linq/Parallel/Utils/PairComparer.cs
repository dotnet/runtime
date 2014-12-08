// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    internal class PairComparer<T, U> : IComparer<Pair>, IComparer<Pair<T, U>>
    {
        private IComparer<T> _comparer1;
        private IComparer<U> _comparer2;

        public PairComparer(IComparer<T> comparer1, IComparer<U> comparer2)
        {
            _comparer1 = comparer1;
            _comparer2 = comparer2;
        }

        public int Compare(Pair x, Pair y)
        {
            int result1 = _comparer1.Compare((T)x.First, (T)y.First);
            if (result1 != 0)
            {
                return result1;
            }

            return _comparer2.Compare((U)x.Second, (U)y.Second);
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