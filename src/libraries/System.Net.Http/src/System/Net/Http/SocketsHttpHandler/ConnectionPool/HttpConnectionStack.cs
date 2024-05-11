// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Net.Http
{
    /// <summary>
    /// A <see cref="ConcurrentStack{T}"/> with an extra head pointer to opportunistically avoid allocations on pushes.
    /// In situations where Push/Pop operations frequently occur in pairs (common under steady load), this avoids most allocations.
    /// We treat <see langword="null"/> as a sentinel to indicate no value, so we can't store <see langword="null"/> values.
    /// </summary>
    internal struct HttpConnectionStack<T> where T : HttpConnectionBase
    {
        private readonly ConcurrentStack<T> _stack;
        private T? _head;

        public HttpConnectionStack()
        {
            _stack = new ConcurrentStack<T>();
        }

        public readonly int Count => _stack.Count + (_head is null ? 0 : 1);

        public readonly bool DebugContains(T item) =>
            ReferenceEquals(_head, item) ||
            Array.IndexOf([.. _stack], item) >= 0;

        public void Push(T item)
        {
            Debug.Assert(item is not null);

            if (Interlocked.Exchange(ref _head, item) is T previousHead)
            {
                _stack.Push(previousHead);
            }
        }

        public bool TryPop([MaybeNullWhen(false)] out T result)
        {
            if (Interlocked.Exchange(ref _head, null) is T head)
            {
                result = head;
                return true;
            }

            return _stack.TryPop(out result);
        }

        public readonly void PushRange(T[] items, int startIndex, int count)
        {
            _stack.PushRange(items, startIndex, count);
        }
    }
}
