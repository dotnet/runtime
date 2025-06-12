// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Numerics
{
    public static unsafe partial class Vector
    {
        /// <summary>Reinterprets a <see cref="Vector4" /> as a new <see cref="Plane" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Plane" />.</returns>
        public static Plane AsPlane(this Vector4 value)
        {
#if MONO
            return Unsafe.As<Vector4, Plane>(ref value);
#else
            return Unsafe.BitCast<Vector4, Plane>(value);
#endif
        }

        /// <summary>Reinterprets a <see cref="Vector4" /> as a new <see cref="Quaternion" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Quaternion" />.</returns>
        public static Quaternion AsQuaternion(this Vector4 value)
        {
#if MONO
            return Unsafe.As<Vector4, Quaternion>(ref value);
#else
            return Unsafe.BitCast<Vector4, Quaternion>(value);
#endif
        }

        /// <summary>Reinterprets a <see cref="Vector4" /> as a new <see cref="Vector2" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector2" />.</returns>
        public static Vector2 AsVector2(this Vector4 value) => value.AsVector128().AsVector2();

        /// <summary>Reinterprets a <see cref="Vector4" /> as a new <see cref="Vector3" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector3" />.</returns>
        public static Vector3 AsVector3(this Vector4 value) => value.AsVector128().AsVector3();

        /// <inheritdoc cref="Vector128.ExtractMostSignificantBits{T}(Vector128{T})" />
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ExtractMostSignificantBits(this Vector4 vector) => vector.AsVector128().ExtractMostSignificantBits();

        /// <inheritdoc cref="Vector128.GetElement{T}(Vector128{T}, int)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetElement(this Vector4 vector, int index) => vector.AsVector128().GetElement(index);

        /// <summary>Stores a vector at the given destination.</summary>
        /// <param name="source">The vector that will be stored.</param>
        /// <param name="destination">The destination at which <paramref name="source" /> will be stored.</param>
        [CLSCompliant(false)]
        public static void Store(this Vector4 source, float* destination) => source.AsVector128().Store(destination);

        /// <summary>Stores a vector at the given 16-byte aligned destination.</summary>
        /// <param name="source">The vector that will be stored.</param>
        /// <param name="destination">The aligned destination at which <paramref name="source" /> will be stored.</param>
        /// <exception cref="AccessViolationException"><paramref name="destination" /> is not 16-byte aligned.</exception>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StoreAligned(this Vector4 source, float* destination) => source.AsVector128().StoreAligned(destination);

        /// <summary>Stores a vector at the given 16-byte aligned destination.</summary>
        /// <param name="source">The vector that will be stored.</param>
        /// <param name="destination">The aligned destination at which <paramref name="source" /> will be stored.</param>
        /// <exception cref="AccessViolationException"><paramref name="destination" /> is not 16-byte aligned.</exception>
        /// <remarks>This method may bypass the cache on certain platforms.</remarks>
        [CLSCompliant(false)]
        public static void StoreAlignedNonTemporal(this Vector4 source, float* destination) => source.AsVector128().StoreAlignedNonTemporal(destination);

        /// <summary>Stores a vector at the given destination.</summary>
        /// <param name="source">The vector that will be stored.</param>
        /// <param name="destination">The destination at which <paramref name="source" /> will be stored.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StoreUnsafe(this Vector4 source, ref float destination) => source.AsVector128().StoreUnsafe(ref destination);

        /// <summary>Stores a vector at the given destination.</summary>
        /// <param name="source">The vector that will be stored.</param>
        /// <param name="destination">The destination to which <paramref name="elementOffset" /> will be added before the vector will be stored.</param>
        /// <param name="elementOffset">The element offset from <paramref name="destination" /> from which the vector will be stored.</param>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StoreUnsafe(this Vector4 source, ref float destination, nuint elementOffset) => source.AsVector128().StoreUnsafe(ref destination, elementOffset);

        /// <inheritdoc cref="Vector128.ToScalar{T}(Vector128{T})" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToScalar(this Vector4 vector) => vector.AsVector128().ToScalar();

        /// <inheritdoc cref="Vector128.WithElement{T}(Vector128{T}, int, T)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 WithElement(this Vector4 vector, int index, float value) => vector.AsVector128().WithElement(index, value).AsVector4();
    }
}
