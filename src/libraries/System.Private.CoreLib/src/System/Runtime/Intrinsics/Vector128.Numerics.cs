// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics
{
    public static partial class Vector128
    {
        /// <inheritdoc cref="Vector4.All(Vector4, float)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool All(Vector2 vector, float value) => vector.AsVector128() == Vector2.Create(value).AsVector128();

        /// <inheritdoc cref="Vector4.All(Vector4, float)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool All(Vector3 vector, float value) => vector.AsVector128() == Vector3.Create(value).AsVector128();

        /// <inheritdoc cref="Vector4.AllWhereAllBitsSet(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool AllWhereAllBitsSet(Vector2 vector) => vector.AsVector128().AsInt32() == Vector2.AllBitsSet.AsVector128().AsInt32();

        /// <inheritdoc cref="Vector4.AllWhereAllBitsSet(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool AllWhereAllBitsSet(Vector3 vector) => vector.AsVector128().AsInt32() == Vector3.AllBitsSet.AsVector128().AsInt32();

        /// <inheritdoc cref="Vector4.Any(Vector4, float)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool Any(Vector2 vector, float value) => EqualsAny(vector.AsVector128(), Create(value, value, -1, -1));

        /// <inheritdoc cref="Vector4.Any(Vector4, float)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool Any(Vector3 vector, float value) => EqualsAny(vector.AsVector128(), Create(value, value, value, -1));

        /// <inheritdoc cref="Vector4.AnyWhereAllBitsSet(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool AnyWhereAllBitsSet(Vector2 vector) => EqualsAny(vector.AsVector128().AsInt32(), Vector128<int>.AllBitsSet);

        /// <inheritdoc cref="Vector4.AnyWhereAllBitsSet(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool AnyWhereAllBitsSet(Vector3 vector) => EqualsAny(vector.AsVector128().AsInt32(), Vector128<int>.AllBitsSet);

        /// <summary>Reinterprets a <see langword="Vector128&lt;Single&gt;" /> as a new <see cref="Plane" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Plane" />.</returns>
        [Intrinsic]
        internal static Plane AsPlane(this Vector128<float> value)
        {
#if MONO
            return Unsafe.As<Vector128<float>, Plane>(ref value);
#else
            return Unsafe.BitCast<Vector128<float>, Plane>(value);
#endif
        }

        /// <summary>Reinterprets a <see langword="Vector128&lt;Single&gt;" /> as a new <see cref="Quaternion" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Quaternion" />.</returns>
        [Intrinsic]
        internal static Quaternion AsQuaternion(this Vector128<float> value)
        {
#if MONO
            return Unsafe.As<Vector128<float>, Quaternion>(ref value);
#else
            return Unsafe.BitCast<Vector128<float>, Quaternion>(value);
#endif
        }

        /// <summary>Reinterprets a <see cref="Plane" /> as a new <see langword="Vector128&lt;Single&gt;" />.</summary>
        /// <param name="value">The plane to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see langword="Vector128&lt;Single&gt;" />.</returns>
        [Intrinsic]
        internal static Vector128<float> AsVector128(this Plane value)
        {
#if MONO
            return Unsafe.As<Plane, Vector128<float>>(ref value);
#else
            return Unsafe.BitCast<Plane, Vector128<float>>(value);
#endif
        }

        /// <summary>Reinterprets a <see cref="Quaternion" /> as a new <see langword="Vector128&lt;Single&gt;" />.</summary>
        /// <param name="value">The quaternion to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see langword="Vector128&lt;Single&gt;" />.</returns>
        [Intrinsic]
        internal static Vector128<float> AsVector128(this Quaternion value)
        {
#if MONO
            return Unsafe.As<Quaternion, Vector128<float>>(ref value);
#else
            return Unsafe.BitCast<Quaternion, Vector128<float>>(value);
#endif
        }

        /// <summary>Reinterprets a <see langword="Vector2" /> as a new <see cref="Vector128&lt;Single&gt;" /> with the new elements zeroed.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see langword="Vector128&lt;Single&gt;" /> with the new elements zeroed.</returns>
        [Intrinsic]
        public static Vector128<float> AsVector128(this Vector2 value) => Vector4.Create(value, 0, 0).AsVector128();

        /// <summary>Reinterprets a <see langword="Vector3" /> as a new <see cref="Vector128&lt;Single&gt;" /> with the new elements zeroed.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see langword="Vector128&lt;Single&gt;" /> with the new elements zeroed.</returns>
        [Intrinsic]
        public static Vector128<float> AsVector128(this Vector3 value) => Vector4.Create(value, 0).AsVector128();

        /// <summary>Reinterprets a <see langword="Vector4" /> as a new <see cref="Vector128&lt;Single&gt;" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see langword="Vector128&lt;Single&gt;" />.</returns>
        [Intrinsic]
        public static Vector128<float> AsVector128(this Vector4 value)
        {
#if MONO
            return Unsafe.As<Vector4, Vector128<float>>(ref value);
#else
            return Unsafe.BitCast<Vector4, Vector128<float>>(value);
#endif
        }

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector128{T}" />.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector128{T}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<T> AsVector128<T>(this Vector<T> value)
        {
            Debug.Assert(Vector<T>.Count >= Vector128<T>.Count);
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();

            ref byte address = ref Unsafe.As<Vector<T>, byte>(ref value);
            return Unsafe.ReadUnaligned<Vector128<T>>(ref address);
        }

        /// <summary>Reinterprets a <see langword="Vector2" /> as a new <see cref="Vector128&lt;Single&gt;" />, leaving the new elements undefined.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see langword="Vector128&lt;Single&gt;" />.</returns>
        [Intrinsic]
        public static Vector128<float> AsVector128Unsafe(this Vector2 value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            Unsafe.SkipInit(out Vector128<float> result);
            Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<float>, byte>(ref result), value);
            return result;
        }

        /// <summary>Reinterprets a <see langword="Vector3" /> as a new <see cref="Vector128&lt;Single&gt;" />, leaving the new elements undefined.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see langword="Vector128&lt;Single&gt;" />.</returns>
        [Intrinsic]
        public static Vector128<float> AsVector128Unsafe(this Vector3 value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            Unsafe.SkipInit(out Vector128<float> result);
            Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<float>, byte>(ref result), value);
            return result;
        }

        /// <summary>Reinterprets a <see langword="Vector128&lt;Single&gt;" /> as a new <see cref="Vector2" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector2" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 AsVector2(this Vector128<float> value)
        {
            ref byte address = ref Unsafe.As<Vector128<float>, byte>(ref value);
            return Unsafe.ReadUnaligned<Vector2>(ref address);
        }

        /// <summary>Reinterprets a <see langword="Vector128&lt;Single&gt;" /> as a new <see cref="Vector3" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector3" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 AsVector3(this Vector128<float> value)
        {
            ref byte address = ref Unsafe.As<Vector128<float>, byte>(ref value);
            return Unsafe.ReadUnaligned<Vector3>(ref address);
        }

        /// <summary>Reinterprets a <see langword="Vector128&lt;Single&gt;" /> as a new <see cref="Vector4" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector4" />.</returns>
        [Intrinsic]
        public static Vector4 AsVector4(this Vector128<float> value)
        {
#if MONO
            return Unsafe.As<Vector128<float>, Vector4>(ref value);
#else
            return Unsafe.BitCast<Vector128<float>, Vector4>(value);
#endif
        }

        /// <summary>Reinterprets a <see cref="Vector128{T}" /> as a new <see cref="Vector{T}" />.</summary>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector128{T}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> AsVector<T>(this Vector128<T> value)
        {
            Debug.Assert(Vector<T>.Count >= Vector128<T>.Count);
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();

            Vector<T> result = default;
            Unsafe.WriteUnaligned(ref Unsafe.As<Vector<T>, byte>(ref result), value);
            return result;
        }

        /// <inheritdoc cref="Vector4.Count(Vector4, float)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int Count(Vector2 vector, float value) => BitOperations.PopCount(Equals(vector.AsVector128(), Create(value, value, -1, -1)).ExtractMostSignificantBits());

        /// <inheritdoc cref="Vector4.Count(Vector4, float)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int Count(Vector3 vector, float value) => BitOperations.PopCount(Equals(vector.AsVector128(), Create(value, value, value, -1)).ExtractMostSignificantBits());

        /// <inheritdoc cref="Vector4.CountWhereAllBitsSet(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int CountWhereAllBitsSet(Vector2 vector) => BitOperations.PopCount(Equals(vector.AsVector128().AsInt32(), Vector128<int>.AllBitsSet).ExtractMostSignificantBits());

        /// <inheritdoc cref="Vector4.CountWhereAllBitsSet(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int CountWhereAllBitsSet(Vector3 vector) => BitOperations.PopCount(Equals(vector.AsVector128().AsInt32(), Vector128<int>.AllBitsSet).ExtractMostSignificantBits());

        /// <inheritdoc cref="Vector4.IndexOf(Vector4, float)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int IndexOf(Vector2 vector, float value)
        {
            int result = BitOperations.TrailingZeroCount(Equals(vector.AsVector128(), Create(value, value, -1, -1)).ExtractMostSignificantBits());
            return (result != 32) ? result : -1;
        }

        /// <inheritdoc cref="Vector4.IndexOf(Vector4, float)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int IndexOf(Vector3 vector, float value)
        {
            int result = BitOperations.TrailingZeroCount(Equals(vector.AsVector128(), Create(value, value, value, -1)).ExtractMostSignificantBits());
            return (result != 32) ? result : -1;
        }

        /// <inheritdoc cref="Vector4.IndexOfWhereAllBitsSet(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int IndexOfWhereAllBitsSet(Vector2 vector)
        {
            int result = BitOperations.TrailingZeroCount(Equals(vector.AsVector128().AsInt32(), Vector128<int>.AllBitsSet).ExtractMostSignificantBits());
            return (result != 32) ? result : -1;
        }

        /// <inheritdoc cref="Vector4.IndexOfWhereAllBitsSet(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int IndexOfWhereAllBitsSet(Vector3 vector)
        {
            int result = BitOperations.TrailingZeroCount(Equals(vector.AsVector128().AsInt32(), Vector128<int>.AllBitsSet).ExtractMostSignificantBits());
            return (result != 32) ? result : -1;
        }

        /// <inheritdoc cref="Vector4.LastIndexOf(Vector4, float)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int LastIndexOf(Vector2 vector, float value) => 31 - BitOperations.LeadingZeroCount(Equals(vector.AsVector128(), Create(value, value, -1, -1)).ExtractMostSignificantBits());

        /// <inheritdoc cref="Vector4.LastIndexOf(Vector4, float)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int LastIndexOf(Vector3 vector, float value) => 31 - BitOperations.LeadingZeroCount(Equals(vector.AsVector128(), Create(value, value, value, -1)).ExtractMostSignificantBits());

        /// <inheritdoc cref="Vector4.LastIndexOfWhereAllBitsSet(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int LastIndexOfWhereAllBitsSet(Vector2 vector) => 31 - BitOperations.LeadingZeroCount(Equals(vector.AsVector128().AsInt32(), Vector128<int>.AllBitsSet).ExtractMostSignificantBits());

        /// <inheritdoc cref="Vector4.LastIndexOfWhereAllBitsSet(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int LastIndexOfWhereAllBitsSet(Vector3 vector) => 31 - BitOperations.LeadingZeroCount(Equals(vector.AsVector128().AsInt32(), Vector128<int>.AllBitsSet).ExtractMostSignificantBits());

        /// <inheritdoc cref="Vector4.None(Vector4, float)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool None(Vector2 vector, float value) => !EqualsAny(vector.AsVector128(), Create(value, value, -1, -1));

        /// <inheritdoc cref="Vector4.None(Vector4, float)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool None(Vector3 vector, float value) => !EqualsAny(vector.AsVector128(), Create(value, value, value, -1));

        /// <inheritdoc cref="Vector4.NoneWhereAllBitsSet(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool NoneWhereAllBitsSet(Vector2 vector) => !EqualsAny(vector.AsVector128().AsInt32(), Vector128<int>.AllBitsSet);

        /// <inheritdoc cref="Vector4.NoneWhereAllBitsSet(Vector4)" />
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool NoneWhereAllBitsSet(Vector3 vector) => !EqualsAny(vector.AsVector128().AsInt32(), Vector128<int>.AllBitsSet);
    }
}
