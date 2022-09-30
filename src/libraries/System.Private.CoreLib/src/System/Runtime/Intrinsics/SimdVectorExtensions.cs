// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.Intrinsics
{
    internal static class SimdVectorExtensions
    {
        /// <summary>Copies a vector to a given array.</summary>
        /// <typeparam name="TVector">The type of the vector.</typeparam>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector to be copied.</param>
        /// <param name="destination">The array to which <paramref name="vector" /> is copied.</param>
        /// <exception cref="ArgumentException">The length of <paramref name="destination" /> is less than <see cref="ISimdVector{TVector, T}.Count" />.</exception>
        /// <exception cref="NullReferenceException"><paramref name="destination" /> is <c>null</c>.</exception>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        public static void CopyTo<TVector, T>(this TVector vector, T[] destination)
            where TVector : unmanaged, ISimdVector<TVector, T>
            where T : struct
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
            where TVector : unmanaged, ISimdVector<TVector, T>
            where T : struct
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
            where TVector : unmanaged, ISimdVector<TVector, T>
            where T : struct
        {
            TVector.CopyTo(vector, destination);
        }

        /// <summary>Stores a vector at the given destination.</summary>
        /// <typeparam name="TVector">The type of the vector.</typeparam>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="vector">The vector that will be stored.</param>
        /// <param name="destination">The destination at which the vector will be stored.</param>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        public static void StoreUnsafe<TVector, T>(this TVector vector, ref T destination)
            where TVector : unmanaged, ISimdVector<TVector, T>
            where T : struct
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
            where TVector : unmanaged, ISimdVector<TVector, T>
            where T : struct
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
            where TVector : unmanaged, ISimdVector<TVector, T>
            where T : struct
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
            where TVector : unmanaged, ISimdVector<TVector, T>
            where T : struct
        {
            return TVector.TryCopyTo(vector, destination);
        }

        /// <summary>Creates a new <see cref="Vector128{T}" /> with the element at the specified index set to the specified value and the remaining elements set to the same value as that in the given vector.</summary>
        /// <param name="vector">The vector to get the remaining elements from.</param>
        /// <param name="index">The index of the element to set.</param>
        /// <param name="value">The value to set the element to.</param>
        /// <returns>A <see cref="Vector128{T}" /> with the value of the element at <paramref name="index" /> set to <paramref name="value" /> and the remaining elements set to the same value as that in <paramref name="vector" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        public static TVector WithElement<TVector, T>(this TVector vector, int index, T value)
            where TVector : unmanaged, ISimdVector<TVector, T>
            where T : struct
        {
            return TVector.WithElement(vector, index, value);
        }
    }
}
