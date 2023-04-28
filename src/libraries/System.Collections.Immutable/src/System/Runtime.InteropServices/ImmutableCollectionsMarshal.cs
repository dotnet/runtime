// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// An unsafe class that provides a set of methods to access the underlying data representations of immutable collections.
    /// </summary>
    public static unsafe class ImmutableCollectionsMarshal
    {
        /// <summary>
        /// Gets an <see cref="ImmutableArray{T}"/> value wrapping the input <typeparamref name="T"/> array.
        /// </summary>
        /// <typeparam name="T">The type of elements in the input array.</typeparam>
        /// <param name="array">The input array to wrap in the returned <see cref="ImmutableArray{T}"/> value.</param>
        /// <returns>An <see cref="ImmutableArray{T}"/> value wrapping <paramref name="array"/>.</returns>
        /// <remarks>
        /// <para>
        /// When using this method, callers should take extra care to ensure that they're the sole owners of the input
        /// array, and that it won't be modified once the returned <see cref="ImmutableArray{T}"/> value starts being
        /// used. Doing so might cause undefined behavior in code paths which don't expect the contents of a given
        /// <see cref="ImmutableArray{T}"/> values to change after its creation.
        /// </para>
        /// <para>
        /// If <paramref name="array"/> is <see langword="null"/>, the returned <see cref="ImmutableArray{T}"/> value
        /// will be uninitialized (ie. its <see cref="ImmutableArray{T}.IsDefault"/> property will be <see langword="true"/>).
        /// </para>
        /// </remarks>
        public static ImmutableArray<T> AsImmutableArray<T>(T[]? array)
        {
#if NET6_0_OR_GREATER
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type ('T[]' and 'ImmutableArray<T>')
            return *(ImmutableArray<T>*)&array;
#pragma warning restore CS8500
#else
            // When on downlevel targets, use Unsafe.As to reinterpret the array reference. This is slightly
            // more expensive (though not really that much) due to the additional generic instantiation, but
            // it avoids problems on older runtimes that might not play well with native pointers to managed
            // objects. For instance, the .NET Native compiler (UWP) can hit assertions in this case.
            return Unsafe.As<T[]?, ImmutableArray<T>>(ref array);
#endif
        }

        /// <summary>
        /// Gets the underlying <typeparamref name="T"/> array for an input <see cref="ImmutableArray{T}"/> value.
        /// </summary>
        /// <typeparam name="T">The type of elements in the input <see cref="ImmutableArray{T}"/> value.</typeparam>
        /// <param name="array">The input <see cref="ImmutableArray{T}"/> value to get the underlying <typeparamref name="T"/> array from.</param>
        /// <returns>The underlying <typeparamref name="T"/> array for <paramref name="array"/>, if present.</returns>
        /// <remarks>
        /// <para>
        /// When using this method, callers should make sure to not pass the resulting underlying array to methods that
        /// might mutate it. Doing so might cause undefined behavior in code paths using <paramref name="array"/> which
        /// don't expect the contents of the <see cref="ImmutableArray{T}"/> value to change.
        /// </para>
        /// <para>
        /// If <paramref name="array"/> is uninitialized (ie. its <see cref="ImmutableArray{T}.IsDefault"/> property is
        /// <see langword="true"/>), the resulting <typeparamref name="T"/> array will be <see langword="null"/>.
        /// </para>
        /// </remarks>
        public static T[]? AsArray<T>(ImmutableArray<T> array)
        {
#if NET6_0_OR_GREATER
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type ('T[]' and 'ImmutableArray<T>')
            return *(T[]?*)&array;
#pragma warning restore CS8500
#else
            return Unsafe.As<ImmutableArray<T>, T[]?>(ref array);
#endif
        }
    }
}
