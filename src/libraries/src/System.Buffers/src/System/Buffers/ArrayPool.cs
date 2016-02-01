// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.Tracing;
using System.Threading;

namespace System.Buffers
{
    /// <summary>
    /// Provides a resource pool that enables reusing instances of type <see cref="T:T[]"/>. 
    /// </summary>
    /// <remarks>
    /// <para>
    /// Renting and returning buffers with an <see cref="ArrayPool{T}"/> can increase performance
    /// in situations where arrays are created and destroyed frequently, resulting in significant
    /// memory pressure on the garbage collector.
    /// </para>
    /// <para>
    /// This class is thread-safe.  All members may be used by multiple threads concurrently.
    /// </para>
    /// </remarks>
    public abstract class ArrayPool<T>
    {
        /// <summary>The default number of arrays that are available for rent.</summary>
        private const int DefaultNumberOfArraysPerBucket = 50;
        /// <summary>The default length of each Rent'able array; equal to 1MB in bytes</summary>
        private const int DefaultArrayLength = 1024 * 1024;

        /// <summary>The lazily-initialized shared pool instance.</summary>
        private static ArrayPool<T> s_sharedInstance = null;

        /// <summary>
        /// Retrieves a shared <see cref="ArrayPool{T}"/> instance.
        /// </summary>
        /// <remarks>
        /// With the <see cref="Shared"/> pool, renting a buffer with <see cref="Rent"/> 
        /// will result in an existing buffer being taken from the pool if an appropriate buffer 
        /// is available or in a new buffer being allocated if one is not available.
        /// </remarks>
        public static ArrayPool<T> Shared
        {
            get
            {
                ArrayPool<T> instance = Volatile.Read(ref s_sharedInstance);
                if (instance == null)
                {
                    Interlocked.CompareExchange(ref s_sharedInstance, new DefaultArrayPool<T>(DefaultArrayLength, DefaultNumberOfArraysPerBucket), null);
                    instance = s_sharedInstance;
                }

                return instance;
            }
        }

        /// <summary>
        /// Creates a new <see cref="ArrayPool{T}"/> instance using default configuration options.
        /// </summary>
        /// <returns>A new <see cref="ArrayPool{T}"/> instance.</returns>
        public static ArrayPool<T> Create()
        {
            return Create(DefaultArrayLength, DefaultNumberOfArraysPerBucket);
        }

        /// <summary>
        /// Creates a new <see cref="ArrayPool{T}"/> instance using custom configuration options.
        /// </summary>
        /// <param name="maxArrayLength">The maximum length of array instances that may be stored in the pool.</param>
        /// <param name="numberOfArrays">The maximum number of array instances that may be stored in the pool.</param>
        /// <returns>A new <see cref="ArrayPool{T}"/> instance with the specified configuration options.</returns>
        public static ArrayPool<T> Create(int maxArrayLength, int numberOfArrays)
        {
            return new DefaultArrayPool<T>(maxArrayLength, numberOfArrays);
        }

        /// <summary>
        /// Retrieves a buffer that is at least the requested length.
        /// </summary>
        /// <param name="minimumLength">The minimum length of the array needed.</param>
        /// <returns>
        /// An <see cref="T:T[]"/> that is at least <paramref name="minimumLength"/> in length.
        /// </returns>
        /// <remarks>
        /// This buffer is loaned to the caller and should be returned to the same pool via 
        /// <see cref="Return"/> so that it may be reused in subsequent usage of <see cref="Rent"/>.  
        /// It is not a fatal error to not return a rented buffer, but failure to do so may lead to 
        /// decreased application performance, as the pool may need to create a new buffer to replace
        /// the one lost.
        /// </remarks>
        public abstract T[] Rent(int minimumLength);

        /// <summary>
        /// Returns to the pool an array that was previously obtained via <see cref="Rent"/> on the same 
        /// <see cref="ArrayPool{T}"/> instance.
        /// </summary>
        /// <param name="buffer">
        /// The buffer previously obtained from <see cref="Rent"/> to return to the pool.
        /// </param>
        /// <param name="clearArray">
        /// If <c>true</c> and if the pool will store the buffer to enable subsequent reuse, <see cref="Return"/>
        /// will clear <paramref name="buffer"/> of its contents so that a subsequent consumer via <see cref="Rent"/> 
        /// will not see the previous consumer's content.  If <c>false</c> or if the pool will release the buffer,
        /// the array's contents are left unchanged.
        /// </param>
        /// <remarks>
        /// Once a buffer has been returned to the pool, the caller gives up all ownership of the buffer 
        /// and must not use it. The reference returned from a given call to <see cref="Rent"/> must only be
        /// returned via <see cref="Return"/> once.  The default <see cref="ArrayPool{T}"/>
        /// may hold onto the returned buffer in order to rent it again, or it may release the returned buffer
        /// if it's determined that the pool already has enough buffers stored.
        /// </remarks>
        public abstract void Return(T[] buffer, bool clearArray = false);
    }
}
