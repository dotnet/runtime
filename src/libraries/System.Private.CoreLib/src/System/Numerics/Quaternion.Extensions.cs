// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Numerics
{
    public static unsafe partial class Vector
    {
        /// <summary>Gets the element at the specified index.</summary>
        /// <param name="quaternion">The quaternion to get the element from.</param>
        /// <param name="index">The index of the element to get.</param>
        /// <returns>The value of the element at <paramref name="index" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float GetElement(this Quaternion quaternion, int index)
        {
            if ((uint)(index) >= (uint)(Quaternion.Count))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            return quaternion.GetElementUnsafe(index);
        }

        /// <summary>Creates a new <see cref="Quaternion" /> with the element at the specified index set to the specified value and the remaining elements set to the same value as that in the given quaternion.</summary>
        /// <param name="quaternion">The quaternion to get the remaining elements from.</param>
        /// <param name="index">The index of the element to set.</param>
        /// <param name="value">The value to set the element to.</param>
        /// <returns>A <see cref="Quaternion" /> with the value of the element at <paramref name="index" /> set to <paramref name="value" /> and the remaining elements set to the same value as that in <paramref name="quaternion" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
        [Intrinsic]
        internal static Quaternion WithElement(this Quaternion quaternion, int index, float value)
        {
            if ((uint)(index) >= (uint)(Quaternion.Count))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            Quaternion result = quaternion;
            result.SetElementUnsafe(index, value);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float GetElementUnsafe(in this Quaternion quaternion, int index)
        {
            Debug.Assert((index >= 0) && (index < Quaternion.Count));
            ref float address = ref Unsafe.AsRef(in quaternion.X);
            return Unsafe.Add(ref address, index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetElementUnsafe(ref this Quaternion quaternion, int index, float value)
        {
            Debug.Assert((index >= 0) && (index < Quaternion.Count));
            Unsafe.Add(ref quaternion.X, index) = value;
        }
    }
}
