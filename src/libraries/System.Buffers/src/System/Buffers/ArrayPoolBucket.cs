// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading;

namespace System.Buffers
{
    /// <summary>
    /// Provides a thread-safe bucket containing buffers that can be Rented and Returned as part 
    /// of a buffer pool; it should not be used independent of the pool.
    /// </summary>
    internal sealed class ArrayPoolBucket<T>
    {
        private int _index;
        private readonly T[][] _data;
        private readonly int _bufferLength;
        private SpinLock _lock;

        /// <summary>
        /// Creates the pool with numberOfBuffers arrays where each buffer is of bufferLength length.
        /// </summary>
        internal ArrayPoolBucket(int bufferLength, int numberOfBuffers)
        {
            _lock = new SpinLock();
            _data = new T[numberOfBuffers][];
            _bufferLength = bufferLength;
        }

        /// <summary>
        /// Returns an array from the Bucket sized according to the Bucket size.
        /// If the Bucket is empty, null is returned.
        /// </summary>
        /// <returns>Returns a valid buffer when the bucket has free buffers; otherwise, returns null</returns>
        internal T[] Rent()
        {
            T[] buffer = null;

            // Use a SpinLock since it is super lightweight
            // and our lock is very short lived. Wrap in try-finally
            // to protect against thread-aborts
            bool taken = false;
            try
            {
                _lock.Enter(ref taken);

                // Check if all of our buffers have been rented
                if (_index < _data.Length)
                {
                    buffer = _data[_index] ?? new T[_bufferLength];
                    _data[_index++] = null;
                }
            }
            finally
            {
                if (taken) _lock.Exit(false);
            }

            return buffer;
        }

        /// <summary>
        /// Attempts to return a Buffer to the bucket. This can fail
        /// if the buffer being returned was allocated and we don't have
        /// room for it in the bucket.
        /// </summary>
        internal void Return(T[] buffer)
        {
            // Use a SpinLock since it is super lightweight
            // and our lock is very short lived. Wrap in try-finally
            // to protect against thread-aborts
            bool taken = false;
            try
            {
                _lock.Enter(ref taken);

                // If we have space to put the buffer back, do it. If we don't
                // then there was a buffer alloc'd that was returned instead so
                // we can just drop this buffer
                if (_index != 0)
                {
                    _data[--_index] = buffer;
                }
            }
            finally
            {
                if (taken) _lock.Exit(false);
            }
        }
    }
}
