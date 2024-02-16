// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Collections.Generic
{
    //
    // Helper class to store reusable empty IEnumerables.
    //
    internal static class Empty<T>
    {
        //
        // Returns a reusable empty IEnumerable<T> (that does not secretly implement more advanced collection interfaces.)
        //
        public static IEnumerable<T> Enumerable
        {
            get
            {
                return _enumerable;
            }
        }

        private sealed class EmptyEnumImpl : IEnumerable<T>, IEnumerator<T>
        {
            public IEnumerator<T> GetEnumerator()
            {
                return this;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this;
            }

            public T Current
            {
                get { throw new InvalidOperationException(); }
            }

            object IEnumerator.Current
            {
                get { throw new InvalidOperationException(); }
            }

            public bool MoveNext()
            {
                return false;
            }

            public void Reset()
            {
            }

            public void Dispose()
            {
            }
        }

        private static IEnumerable<T> _enumerable = new EmptyEnumImpl();
    }
}
