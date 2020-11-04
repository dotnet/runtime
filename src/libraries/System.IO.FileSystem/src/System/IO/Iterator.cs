// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.IO
{
    // Abstract Iterator, borrowed from Linq. Used in anticipation of need for similar enumerables
    // in the future
    internal abstract class Iterator<TSource> : IEnumerable<TSource>, IEnumerator<TSource>
    {
        private readonly int _threadId;
        internal int state;
        internal TSource? current;

        public Iterator()
        {
            _threadId = Environment.CurrentManagedThreadId;
        }

        public TSource Current => current!;

        protected abstract Iterator<TSource> Clone();

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            current = default;
            state = -1;
        }

        public IEnumerator<TSource> GetEnumerator()
        {
            if (state == 0 && _threadId == Environment.CurrentManagedThreadId)
            {
                state = 1;
                return this;
            }

            Iterator<TSource> duplicate = Clone();
            duplicate.state = 1;
            return duplicate;
        }

        public abstract bool MoveNext();

        object? IEnumerator.Current
        {
            get { return Current; }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void IEnumerator.Reset()
        {
            throw new NotSupportedException();
        }
    }
}
