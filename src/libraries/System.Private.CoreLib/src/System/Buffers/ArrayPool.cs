// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Buffers
{
    /// <summary>Provides a resource pool that enables reusing array instances of <typeparamref name="T" />.</summary>
    /// <typeparam name="T">The type of the objects that are in the resource pool.</typeparam>
    /// <remarks>Using the <see cref="System.Buffers.ArrayPool{T}" /> class to rent and return buffers (using the <see cref="System.Buffers.ArrayPool{T}.Rent" /> and <see cref="System.Buffers.ArrayPool{T}.Return" /> methods) can improve performance in situations where arrays are created and destroyed frequently, resulting in significant memory pressure on the garbage collector.</remarks>
    public abstract class ArrayPool<T>
    {
        // Store the shared ArrayPool in a field of its derived sealed type so the Jit can "see" the exact type
        // when the Shared property is inlined which will allow it to devirtualize calls made on it.
        private static readonly TlsOverPerCoreLockedStacksArrayPool<T> s_shared = new TlsOverPerCoreLockedStacksArrayPool<T>();

        /// <summary>Gets a shared <see cref="System.Buffers.ArrayPool{T}" /> instance.</summary>
        /// <value>A shared <see cref="System.Buffers.ArrayPool{T}" /> instance.</value>
        /// <remarks>The shared pool provides a default implementation of the <see cref="System.Buffers.ArrayPool{T}" /> class that's intended for general applicability. A shared class maintains arrays of multiple sizes, and may hand back a larger array than was actually requested, but it will never hand back a smaller array than was requested. Renting a buffer from a shared class using the <see cref="System.Buffers.ArrayPool{T}.Rent" /> method will result in an existing buffer being taken from the pool if an appropriate buffer is available or in a new buffer being allocated if one is not available.</remarks>
        public static ArrayPool<T> Shared => s_shared;

        /// <summary>Creates a new instance of the <see cref="System.Buffers.ArrayPool{T}" /> class.</summary>
        /// <returns>A new instance of the <see cref="System.Buffers.ArrayPool{T}" /> class.</returns>
        public static ArrayPool<T> Create() => new ConfigurableArrayPool<T>();

        /// <summary>Creates a new instance of the <see cref="System.Buffers.ArrayPool{T}" /> class using the specified configuration.</summary>
        /// <param name="maxArrayLength">The maximum length of an array instance that may be stored in the pool.</param>
        /// <param name="maxArraysPerBucket">The maximum number of array instances that may be stored in each bucket in the pool. The pool groups arrays of similar lengths into buckets for faster access.</param>
        /// <returns>A new instance of the <see cref="System.Buffers.ArrayPool{T}" /> class with the specified configuration.</returns>
        /// <remarks>The instance of the <see cref="System.Buffers.ArrayPool{T}" /> class created by this method will group arrays into buckets, with no more than <paramref name="maxArraysPerBucket" /> in each bucket, and with those arrays not exceeding <paramref name="maxArrayLength" /> in length.</remarks>
        public static ArrayPool<T> Create(int maxArrayLength, int maxArraysPerBucket) =>
            new ConfigurableArrayPool<T>(maxArrayLength, maxArraysPerBucket);

        /// <summary>Retrieves a buffer that is at least the requested length.</summary>
        /// <param name="minimumLength">The minimum length of the array.</param>
        /// <returns>An array of type <typeparamref name="T" /> that is at least <paramref name="minimumLength" /> in length.</returns>
        /// <remarks>This buffer is loaned to the caller and should be returned to the same pool using the <see cref="System.Buffers.ArrayPool{T}.Return" /> method, so that it can be reused in subsequent calls to the <see cref="System.Buffers.ArrayPool{T}.Rent" /> method. Failure to return a rented buffer is not a fatal error. However, it may lead to decreased application performance, as the pool may need to create a new buffer to replace the lost one.
        /// The array returned by this method may not be zero-initialized.</remarks>
        public abstract T[] Rent(int minimumLength);

        /// <summary>Returns an array to the pool that was previously obtained using the <see cref="System.Buffers.ArrayPool{T}.Rent(int)" /> method on the same <see cref="System.Buffers.ArrayPool{T}" /> instance.</summary>
        /// <param name="array">A buffer to return to the pool that was previously obtained using the <see cref="System.Buffers.ArrayPool{T}.Rent(int)" /> method.</param>
        /// <param name="clearArray">Indicates whether the contents of the buffer should be cleared before reuse. If <paramref name="clearArray" /> is set to <see langword="true" />, and if the pool will store the buffer to enable subsequent reuse, the <see cref="System.Buffers.ArrayPool{T}.Return(T[],bool)" /> method will clear the <paramref name="array" /> of its contents so that a subsequent caller using the <see cref="System.Buffers.ArrayPool{T}.Rent(int)" /> method will not see the content of the previous caller. If <paramref name="clearArray" /> is set to <see langword="false" /> or if the pool will release the buffer, the array's contents are left unchanged.</param>
        /// <remarks>Once a buffer has been returned to the pool, the caller gives up all ownership of the buffer and must not use it. The reference returned from a given call to the <see cref="System.Buffers.ArrayPool{T}.Rent" /> method must only be returned using the <see cref="System.Buffers.ArrayPool{T}.Return" /> method once. The default <see cref="System.Buffers.ArrayPool{T}" /> may hold onto the returned buffer in order to rent it again, or it may release the returned buffer if it's determined that the pool already has enough buffers stored.</remarks>
        public abstract void Return(T[] array, bool clearArray = false);
    }
}
