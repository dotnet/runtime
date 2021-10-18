// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Internal.Runtime.CompilerServices;

namespace System.Numerics
{
    public static class Vector3Extensions
    {
        /// <summary>Gets the element at the specified index.</summary>
        /// <param name="vector">The vector of the element to get.</param>
        /// <param name="index">The index of the element to get.</param>
        /// <returns>The value of the element at <paramref name="index" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
        [Intrinsic]
        public static float GetElement(this Vector3 vector, int index)
        {
            if ((uint)index >= Vector3.Count)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            return vector.GetElementUnsafe(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float GetElementUnsafe(this Vector3 vector, int index)
        {
            Debug.Assert(index is >= 0 and < Vector3.Count);
            return Unsafe.Add(ref Unsafe.As<Vector3, float>(ref Unsafe.AsRef(in vector)), index);
        }

        /// <summary>Sets the element at the specified index.</summary>
        /// <param name="vector">The vector of the element to get.</param>
        /// <param name="index">The index of the element to set.</param>
        /// <param name="value">The value of the element to set.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
        [Intrinsic]
        public static Vector3 WithElement(this Vector3 vector, int index, float value)
        {
            if ((uint)index >= Vector3.Count)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            var newVector = new Vector3(vector.X, vector.Y, vector.Z);
            newVector.WithElementUnsafe(index, value);
            return newVector;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void WithElementUnsafe(this Vector3 vector, int index, float value)
        {
            Debug.Assert(index is >= 0 and < Vector3.Count);
            Unsafe.Add(ref Unsafe.As<Vector3, float>(ref Unsafe.AsRef(in vector)), index) = value;
        }
    }
}
