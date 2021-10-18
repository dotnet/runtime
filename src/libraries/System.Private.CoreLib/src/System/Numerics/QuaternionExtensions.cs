// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Internal.Runtime.CompilerServices;

namespace System.Numerics
{
    public static class QuaternionExtensions
    {
        /// <summary>Gets the element at the specified index.</summary>
        /// <param name="quaternion">The vector of the element to get.</param>
        /// <param name="index">The index of the element to get.</param>
        /// <returns>The value of the element at <paramref name="index" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
        [Intrinsic]
        public static float GetElement(this Quaternion quaternion, int index)
        {
            if ((uint)index >= Quaternion.Count)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            return quaternion.GetElementUnsafe(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float GetElementUnsafe(this Quaternion quaternion, int index)
        {
            Debug.Assert(index is >= 0 and < Quaternion.Count);
            return Unsafe.Add(ref Unsafe.As<Quaternion, float>(ref Unsafe.AsRef(in quaternion)), index);
        }

        /// <summary>Sets the element at the specified index.</summary>
        /// <param name="quaternion">The vector of the element to get.</param>
        /// <param name="index">The index of the element to set.</param>
        /// <param name="value">The value of the element to set.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
        [Intrinsic]
        public static Quaternion WithElement(this Quaternion quaternion, int index, float value)
        {
            if ((uint)index >= Quaternion.Count)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            var newQuaternion = new Quaternion(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
            newQuaternion.WithElementUnsafe(index, value);
            return newQuaternion;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void WithElementUnsafe(this Quaternion quaternion, int index, float value)
        {
            Debug.Assert(index is >= 0 and < Quaternion.Count);
            Unsafe.Add(ref Unsafe.As<Quaternion, float>(ref Unsafe.AsRef(in quaternion)), index) = value;
        }
    }
}
