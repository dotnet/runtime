// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Diagnostics;

namespace System.Threading
{
    /// <summary>
    /// Provides callbacks to objects whose lifetime is managed by <see cref="DeferredDisposableLifetime{T}"/>.
    /// </summary>
    internal interface IDeferredDisposable
    {
        /// <summary>
        /// Called when the object's refcount reaches zero.
        /// </summary>
        /// <param name="disposed">
        /// Indicates whether the object has been disposed.
        /// </param>
        /// <remarks>
        /// If the refcount reaches zero before the object is disposed, this method will be called with
        /// <paramref name="disposed"/> set to false.  If the object is then disposed, this method will be
        /// called again, with <paramref name="disposed"/> set to true.  If the refcount reaches zero
        /// after the object has already been disposed, this will be called a single time, with 
        /// <paramref name="disposed"/> set to true.
        /// </remarks>
        void OnFinalRelease(bool disposed);
    }

    /// <summary>
    /// Manages the lifetime of an object which implements IDisposable, but which must defer the actual
    /// cleanup of state until all existing uses of the object are complete.
    /// </summary>
    /// <typeparam name="T">The type of object whose lifetime will be managed.</typeparam>
    /// <remarks>
    /// This type maintains a reference count, and tracks whether the object has been disposed.  When
    /// Callbacks are made to <see cref="IDeferredDisposable.OnFinalRelease(bool)"/> when the refcount
    /// reaches zero.  Objects that need to defer cleanup until they have been disposed *and* they have
    /// no more references can do so in <see cref="IDeferredDisposable.OnFinalRelease(bool)"/> when
    /// 'disposed' is true.
    /// </remarks>
    internal struct DeferredDisposableLifetime<T> where T : class, IDeferredDisposable
    {
        /// <summary>_count is positive until Dispose is called, after which it's (-1 - refcount).</summary>
        private int _count;

        public bool AddRef(T obj)
        {
            while (true)
            {
                int oldCount = Volatile.Read(ref _count);

                // Have we been disposed?
                if (oldCount < 0)
                    throw new ObjectDisposedException(typeof(T).ToString());

                int newCount = checked(oldCount + 1);

                if (Interlocked.CompareExchange(ref _count, newCount, oldCount) == oldCount)
                    return true;
            }
        }

        public void Release(T obj)
        {
            while (true)
            {
                int oldCount = Volatile.Read(ref _count);
                if (oldCount > 0)
                {
                    // We haven't been disposed.  Decrement _count.
                    int newCount = oldCount - 1;
                    if (Interlocked.CompareExchange(ref _count, newCount, oldCount) == oldCount)
                    {
                        if (newCount == 0)
                            obj.OnFinalRelease(disposed: false);
                        return;
                    }
                }
                else
                {
                    Debug.Assert(oldCount != 0 && oldCount != -1);

                    // We've been disposed.  Increment _count.
                    int newCount = oldCount + 1;
                    if (Interlocked.CompareExchange(ref _count, newCount, oldCount) == oldCount)
                    {
                        if (newCount == -1)
                            obj.OnFinalRelease(disposed: true);
                        return;
                    }
                }
            }
        }

        public void Dispose(T obj)
        {
            while (true)
            {
                int oldCount = Volatile.Read(ref _count);
                if (oldCount < 0)
                    return; // already disposed

                int newCount = -1 - oldCount;
                if (Interlocked.CompareExchange(ref _count, newCount, oldCount) == oldCount)
                {
                    if (newCount == -1)
                        obj.OnFinalRelease(disposed: true);
                    return;
                }
            }
        }
    }
}
