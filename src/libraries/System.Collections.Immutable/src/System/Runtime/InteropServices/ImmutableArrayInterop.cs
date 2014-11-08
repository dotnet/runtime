// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Provides tools for using <see cref="ImmutableArray{T}"/> in interop scenarios.
    /// </summary>
    public static class ImmutableArrayInterop
    {
        /// <summary>
        /// Creates a new instance of <see cref="ImmutableArray{T}"/> using a given mutable array as the backing
        /// field, without creating a defensive copy.  Clears the given reference to the mutable array.  It is the
        /// responsibility of the caller to ensure no other mutable references exist to the array.  Do not mutate
        /// the array after calling this method.
        /// </summary>
        /// <typeparam name="T">The type of element stored in the array.</typeparam>
        /// <param name="array">The mutable array to use as the backing field.</param>
        /// <remarks>
        /// Users of this method should take extra care to ensure that the mutable array given as a parameter
        /// is never modified. The returned <see cref="ImmutableArray{T}"/> will use the given array as its backing
        /// field without creating a defensive copy, so changes made to the given mutable array will be observable
        /// on the returned <see cref="ImmutableArray{T}"/>.  Instance and static methods of <see cref="ImmutableArray{T}"/>
        /// and <see cref="ImmutableArray"/> may malfunction if they operate on an <see cref="ImmutableArray{T}"/> instance
        /// whose underlying backing field is modified. 
        /// </remarks>
        /// <returns>An immutable array.</returns>
        public static ImmutableArray<T> DangerousCreateFromUnderlyingArray<T>(ref T[] array)
        {
            T[] givenArray = array;
            array = null;
            return new ImmutableArray<T>(givenArray);
        }

        /// <summary>
        /// Access the backing mutable array instance for the given <see cref="ImmutableArray{T}"/>, without
        /// creating a defensive copy.  It is the responsibility of the caller to ensure the array is not modified
        /// through the returned mutable reference.  Do not mutate the returned array.
        /// </summary>
        /// <typeparam name="T">The type of element stored in the array.</typeparam>
        /// <param name="array">The <see cref="ImmutableArray{T}"/> from which to retrieve the backing field.</param>
        /// <remarks>
        /// Users of this method should take extra care to ensure that the returned mutable array is never modified.
        /// The returned mutable array continues to be used as the backing field of the given <see cref="ImmutableArray{T}"/>
        /// without creating a defensive copy, so changes made to the returned mutable array will be observable
        /// on the given <see cref="ImmutableArray{T}"/>.  Instance and static methods of <see cref="ImmutableArray{T}"/>
        /// and <see cref="ImmutableArray"/> may malfunction if they operate on an <see cref="ImmutableArray{T}"/> instance
        /// whose underlying backing field is modified. 
        /// </remarks>
        /// <returns>The underlying array, or null if <see cref="ImmutableArray{T}.IsDefault"/> is true.</returns>
        public static T[] DangerousGetUnderlyingArray<T>(ImmutableArray<T> array)
        {
            return array.array;
        }
    }
}
