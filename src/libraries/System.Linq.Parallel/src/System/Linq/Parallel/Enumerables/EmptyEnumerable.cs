// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// EmptyEnumerable.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Linq.Parallel
{
    /// <summary>
    /// We occasionally need a no-op enumerator to stand-in when we don't have data left
    /// within a partition's data stream. These are simple enumerable and enumerator
    /// implementations that always and consistently yield no elements.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class EmptyEnumerable<T> : ParallelQuery<T>
    {
        private EmptyEnumerable()
            : base(QuerySettings.Empty)
        {
        }

        // A singleton cached and shared among callers.
        private static volatile EmptyEnumerable<T>? s_instance;
        private static volatile EmptyEnumerator<T>? s_enumeratorInstance;

        internal static EmptyEnumerable<T> Instance =>
            // There is no need for thread safety here.
            s_instance ??= new EmptyEnumerable<T>();

        public override IEnumerator<T> GetEnumerator() =>
            // There is no need for thread safety here.
            s_enumeratorInstance ??= new EmptyEnumerator<T>();
    }

    internal sealed class EmptyEnumerator<T> : QueryOperatorEnumerator<T, int>, IEnumerator<T>
    {
        internal override bool MoveNext([MaybeNullWhen(false), AllowNull] ref T currentElement, ref int currentKey)
        {
            return false;
        }

        // IEnumerator<T> methods.
        public T Current { get { return default!; } }
        object? IEnumerator.Current { get { return null; } }
        public bool MoveNext() { return false; }
        void Collections.IEnumerator.Reset() { }
    }
}
