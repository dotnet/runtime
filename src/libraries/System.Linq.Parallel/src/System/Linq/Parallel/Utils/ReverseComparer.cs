// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// ReverseComparer.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Linq.Parallel
{
    /// <summary>
    /// Comparer that wraps another comparer, and flips the result of each comparison to the
    /// opposite answer.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class ReverseComparer<T> : IComparer<T>
    {
        private readonly IComparer<T> _comparer;

        internal ReverseComparer(IComparer<T> comparer)
        {
            _comparer = comparer;
        }

        public int Compare(T? x, T? y)
        {
            return _comparer.Compare(y, x);
        }
    }
}
