// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// WrapperEqualityComparer.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq.Parallel
{
    /// <summary>
    /// Compares two wrapped structs of the same underlying type for equality.  Simply
    /// wraps the actual comparer for the type being wrapped.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal struct WrapperEqualityComparer<T> : IEqualityComparer<Wrapper<T>>
    {
        private readonly IEqualityComparer<T> _comparer;

        internal WrapperEqualityComparer(IEqualityComparer<T>? comparer) =>
            _comparer = comparer ?? EqualityComparer<T>.Default;

        public bool Equals(Wrapper<T> x, Wrapper<T> y)
        {
            Debug.Assert(_comparer != null);
            return _comparer.Equals(x.Value, y.Value);
        }

        public int GetHashCode(Wrapper<T> x)
        {
            Debug.Assert(_comparer != null);
            T value = x.Value;
            return value == null ? 0 : _comparer.GetHashCode(value);
        }
    }
}
