// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// WrapperEqualityComparer.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace System.Linq.Parallel
{
    /// <summary>
    /// Compares two wrapped structs of the same underlying type for equality.  Simply
    /// wraps the actual comparer for the type being wrapped.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal struct WrapperEqualityComparer<T> : IEqualityComparer<Wrapper<T>>
    {
        private IEqualityComparer<T> _comparer;

        internal WrapperEqualityComparer(IEqualityComparer<T> comparer)
        {
            if (comparer == null)
            {
                _comparer = EqualityComparer<T>.Default;
            }
            else
            {
                _comparer = comparer;
            }
        }

        public bool Equals(Wrapper<T> x, Wrapper<T> y)
        {
            Contract.Assert(_comparer != null);
            return _comparer.Equals(x.Value, y.Value);
        }

        public int GetHashCode(Wrapper<T> x)
        {
            Contract.Assert(_comparer != null);
            return _comparer.GetHashCode(x.Value);
        }
    }
}