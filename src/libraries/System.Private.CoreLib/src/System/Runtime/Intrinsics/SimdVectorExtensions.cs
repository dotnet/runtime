// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.Intrinsics
{
    internal static unsafe class SimdVectorExtensions
    {
        // TODO: As<TFrom, TTo>

        /// <summary>Copies a vector to a given array.</summary>
        /// <typeparam name="TVector">The type of the vector.</typeparam>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to be copied.</param>
        /// <param name="destination">The array to which <paramref name="vector" /> is copied.</param>
        /// <exception cref="ArgumentException">The length of <paramref name="destination" /> is less than <see cref="ISimdVector{TVector, T}.Count" />.</exception>
        /// <exception cref="NullReferenceException"><paramref name="destination" /> is <c>null</c>.</exception>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        public static void CopyTo<TVector, T>(this TVector vector, T[] destination)
            where TVector : ISimdVector<TVector, T>
        {
            TVector.CopyTo(vector, destination);
        }

        /// <summary>Copies a vector to a given array starting at the specified index.</summary>
        /// <typeparam name="TVector">The type of the vector.</typeparam>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to be copied.</param>
        /// <param name="destination">The array to which <paramref name="vector" /> is copied.</param>
        /// <param name="startIndex">The starting index of <paramref name="destination" /> which <paramref name="vector" /> will be copied to.</param>
        /// <exception cref="ArgumentException">The length of <paramref name="destination" /> is less than <see cref="ISimdVector{TVector, T}.Count" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startIndex" /> is negative or greater than the length of <paramref name="destination" />.</exception>
        /// <exception cref="NullReferenceException"><paramref name="destination" /> is <c>null</c>.</exception>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        public static void CopyTo<TVector, T>(this TVector vector, T[] destination, int startIndex)
            where TVector : ISimdVector<TVector, T>
        {
            TVector.CopyTo(vector, destination, startIndex);
        }

        /// <summary>Copies a vector to a given span.</summary>
        /// <typeparam name="TVector">The type of the vector.</typeparam>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to be copied.</param>
        /// <param name="destination">The span to which the <paramref name="vector" /> is copied.</param>
        /// <exception cref="ArgumentException">The length of <paramref name="destination" /> is less than <see cref="ISimdVector{TVector, T}.Count" />.</exception>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        public static void CopyTo<TVector, T>(this TVector vector, Span<T> destination)
            where TVector : ISimdVector<TVector, T>
        {
            TVector.CopyTo(vector, destination);
        }

        /// <summary>Gets the element at the specified index.</summary>
        /// <typeparam name="TVector">The type of the vector.</typeparam>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to get the element from.</param>
        /// <param name="index">The index of the element to get.</param>
        /// <returns>The value of the element at <paramref name="index" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        public static T GetElement<TVector, T>(this TVector vector, int index)
            where TVector : ISimdVector<TVector, T>
        {
            return TVector.GetElement(vector, index);
        }

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type ('T')
        /// <summary>Stores a vector at the given destination.</summary>
        /// <typeparam name="TVector">The type of the vector.</typeparam>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="source">The vector that will be stored.</param>
        /// <param name="destination">The destination at which <paramref name="source" /> will be stored.</param>
        /// <exception cref="NotSupportedException">The type of <paramref name="source" /> (<typeparamref name="T" />) is not supported.</exception>
        public static void Store<TVector, T>(this TVector source, T* destination)
            where TVector : ISimdVector<TVector, T>
        {
            TVector.Store(source, destination);
        }

        /// <summary>Stores a vector at the given aligned destination.</summary>
        /// <typeparam name="TVector">The type of the vector.</typeparam>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="source">The vector that will be stored.</param>
        /// <param name="destination">The aligned destination at which <paramref name="source" /> will be stored.</param>
        /// <exception cref="NotSupportedException">The type of <paramref name="source" /> (<typeparamref name="T" />) is not supported.</exception>
        public static void StoreAligned<TVector, T>(this TVector source, T* destination)
            where TVector : ISimdVector<TVector, T>
        {
            TVector.StoreAligned(source, destination);
        }

        /// <summary>Stores a vector at the given aligned destination.</summary>
        /// <typeparam name="TVector">The type of the vector.</typeparam>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="source">The vector that will be stored.</param>
        /// <param name="destination">The aligned destination at which <paramref name="source" /> will be stored.</param>
        /// <remarks>This method may bypass the cache on certain platforms.</remarks>
        /// <exception cref="NotSupportedException">The type of <paramref name="source" /> (<typeparamref name="T" />) is not supported.</exception>
        public static void StoreAlignedNonTemporal<TVector, T>(this TVector source, T* destination)
            where TVector : ISimdVector<TVector, T>
        {
            TVector.StoreAlignedNonTemporal(source, destination);
        }
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type ('T')

        /// <summary>Stores a vector at the given destination.</summary>
        /// <typeparam name="TVector">The type of the vector.</typeparam>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector that will be stored.</param>
        /// <param name="destination">The destination at which the vector will be stored.</param>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        public static void StoreUnsafe<TVector, T>(this TVector vector, ref T destination)
            where TVector : ISimdVector<TVector, T>
        {
            TVector.StoreUnsafe(vector, ref destination);
        }

        /// <summary>Stores a vector at the given destination.</summary>
        /// <typeparam name="TVector">The type of the vector.</typeparam>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector that will be stored.</param>
        /// <param name="destination">The destination to which <paramref name="elementOffset" /> will be added before the vector will be stored.</param>
        /// <param name="elementOffset">The element offset from <paramref name="destination" /> from which the vector will be stored.</param>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        public static void StoreUnsafe<TVector, T>(this TVector vector, ref T destination, nuint elementOffset)
            where TVector : ISimdVector<TVector, T>
        {
            TVector.StoreUnsafe(vector, ref destination, elementOffset);
        }

        /// <summary>Converts the given vector to a scalar containing the value of the first element.</summary>
        /// <typeparam name="TVector">The type of the vector.</typeparam>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to get the first element from.</param>
        /// <returns>A scalar <typeparamref name="T" /> containing the value of the first element.</returns>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        public static T ToScalar<TVector, T>(this TVector vector)
            where TVector : ISimdVector<TVector, T>
        {
            return TVector.ToScalar(vector);
        }

        /// <summary>Tries to copy a vector to a given span.</summary>
        /// <typeparam name="TVector">The type of the vector.</typeparam>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to copy.</param>
        /// <param name="destination">The span to which <paramref name="destination" /> is copied.</param>
        /// <returns><c>true</c> if <paramref name="vector" /> was successfully copied to <paramref name="destination" />; otherwise, <c>false</c> if the length of <paramref name="destination" /> is less than <see cref="ISimdVector{TVector, T}.Count" />.</returns>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        public static bool TryCopyTo<TVector, T>(this TVector vector, Span<T> destination)
            where TVector : ISimdVector<TVector, T>
        {
            return TVector.TryCopyTo(vector, destination);
        }

        /// <summary>Creates a new vector with the element at the specified index set to the specified value and the remaining elements set to the same value as that in the given vector.</summary>
        /// <typeparam name="TVector">The type of the vector.</typeparam>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to get the remaining elements from.</param>
        /// <param name="index">The index of the element to set.</param>
        /// <param name="value">The value to set the element to.</param>
        /// <returns>A vector with the value of the element at <paramref name="index" /> set to <paramref name="value" /> and the remaining elements set to the same value as that in <paramref name="vector" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        public static TVector WithElement<TVector, T>(this TVector vector, int index, T value)
            where TVector : ISimdVector<TVector, T>
        {
            return TVector.WithElement(vector, index, value);
        }
    }
}
