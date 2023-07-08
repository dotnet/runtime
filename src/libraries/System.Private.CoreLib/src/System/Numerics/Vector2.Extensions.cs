// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Numerics
{
    public static unsafe partial class Vector
    {
        /// <summary>Gets the element at the specified index.</summary>
        /// <param name="vector">The vector to get the element from.</param>
        /// <param name="index">The index of the element to get.</param>
        /// <returns>The value of the element at <paramref name="index" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float GetElement(this Vector2 vector, int index)
        {
            if ((uint)(index) >= (uint)(Vector2.Count))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            return vector.GetElementUnsafe(index);
        }

        /// <summary>Creates a new <see cref="Vector2" /> with the element at the specified index set to the specified value and the remaining elements set to the same value as that in the given vector.</summary>
        /// <param name="vector">The vector to get the remaining elements from.</param>
        /// <param name="index">The index of the element to set.</param>
        /// <param name="value">The value to set the element to.</param>
        /// <returns>A <see cref="Vector2" /> with the value of the element at <paramref name="index" /> set to <paramref name="value" /> and the remaining elements set to the same value as that in <paramref name="vector" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
        [Intrinsic]
        internal static Vector2 WithElement(this Vector2 vector, int index, float value)
        {
            if ((uint)(index) >= (uint)(Vector2.Count))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            Vector2 result = vector;
            result.SetElementUnsafe(index, value);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float GetElementUnsafe(in this Vector2 vector, int index)
        {
            Debug.Assert((index >= 0) && (index < Vector2.Count));
            ref float address = ref Unsafe.AsRef(in vector.X);
            return Unsafe.Add(ref address, index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetElementUnsafe(ref this Vector2 vector, int index, float value)
        {
            Debug.Assert((index >= 0) && (index < Vector2.Count));
            Unsafe.Add(ref vector.X, index) = value;
        }
    }
}
