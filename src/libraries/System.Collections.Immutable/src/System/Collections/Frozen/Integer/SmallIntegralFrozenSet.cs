// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace System.Collections.Frozen
{
    /// <summary>Provides initialization methods for instances of the <see cref="SmallIntegralFrozenSet{T, TUnsigned, TVector}"/> class.</summary>
    internal static class SmallIntegralFrozenSet
    {
        /// <summary>Creates a set for the specified source, if <typeparamref name="T"/> is supported and the values fit in a single accelerated vector.</summary>
        public static FrozenSet<T>? CreateIfValid<T>(HashSet<T> source)
        {
            Debug.Assert(source.Count != 0);
            Debug.Assert(ReferenceEquals(source.Comparer, EqualityComparer<T>.Default));

            if (Vector128.IsHardwareAccelerated && Constants.IsIntegralOrEnum<T>())
            {
                switch (Unsafe.SizeOf<T>())
                {
                    case sizeof(byte): return CreateIfValid<T, byte>(source);
                    case sizeof(ushort): return CreateIfValid<T, ushort>(source);
                    case sizeof(uint): return CreateIfValid<T, uint>(source);
                    case sizeof(ulong): return CreateIfValid<T, ulong>(source);
                }
            }

            return null;
        }

        private static FrozenSet<T>? CreateIfValid<T, TUnsigned>(HashSet<T> source)
            where TUnsigned : unmanaged, IBinaryInteger<TUnsigned>, IUnsignedNumber<TUnsigned>
        {
            Debug.Assert(Unsafe.SizeOf<T>() == Unsafe.SizeOf<TUnsigned>());

            // Use the smallest accelerated vector the values fit in.
            int count = source.Count;
            return
                count <= Vector128<TUnsigned>.Count ? new SmallIntegralFrozenSet<T, TUnsigned, Vector128<TUnsigned>>(source) :
                Vector256.IsHardwareAccelerated && count <= Vector256<TUnsigned>.Count ? new SmallIntegralFrozenSet<T, TUnsigned, Vector256<TUnsigned>>(source) :
                Vector512.IsHardwareAccelerated && count <= Vector512<TUnsigned>.Count ? new SmallIntegralFrozenSet<T, TUnsigned, Vector512<TUnsigned>>(source) :
                null;
        }

        /// <summary>Creates a <see cref="Vector128{T}"/> with all its elements set to the specified item, as <typeparamref name="TUnsigned"/>.</summary>
        /// <remarks>
        /// On x86, a 64-bit argument is passed as two 32-bit halves on the stack, which <see cref="Vector128.Create{T}(T)"/>
        /// reads back as a single 64-bit value: such a load can't be forwarded from the two stores, and the resulting stall made
        /// lookups more than twice as slow. The vector is then created from the two halves instead.
        /// Taking the item as <typeparamref name="T"/> rather than as <typeparamref name="TUnsigned"/> avoids storing the
        /// reinterpreted value in a local, which would zero-extend 8-bit and 16-bit values that the broadcast doesn't need extended.
        /// When used in a comparison, the vector is created before the other operand is evaluated (as the first operand, or
        /// into a local before the other operand is loaded), so that the other operand can be read from memory as part of the
        /// comparison, rather than into a register before this method's (inlined) code.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector128<TUnsigned> CreateVector128<T, TUnsigned>(T item)
            where TUnsigned : unmanaged, IBinaryInteger<TUnsigned>, IUnsignedNumber<TUnsigned> =>
            IntPtr.Size == 4 && X86Base.IsSupported && Unsafe.SizeOf<TUnsigned>() == sizeof(ulong) ?
                CreateVector128FromHalves(ulong.CreateTruncating(Unsafe.BitCast<T, TUnsigned>(item))).As<ulong, TUnsigned>() :
                Vector128.Create(Unsafe.BitCast<T, TUnsigned>(item));

        /// <summary>Creates a <see cref="Vector256{T}"/> with all its elements set to the specified item, as <typeparamref name="TUnsigned"/> (see <see cref="CreateVector128"/>).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector256<TUnsigned> CreateVector256<T, TUnsigned>(T item)
            where TUnsigned : unmanaged, IBinaryInteger<TUnsigned>, IUnsignedNumber<TUnsigned> =>
            IntPtr.Size == 4 && X86Base.IsSupported && Unsafe.SizeOf<TUnsigned>() == sizeof(ulong) ?
                Vector256.Create(CreateVector128FromHalves(ulong.CreateTruncating(Unsafe.BitCast<T, TUnsigned>(item)))).As<ulong, TUnsigned>() :
                Vector256.Create(Unsafe.BitCast<T, TUnsigned>(item));

        /// <summary>Creates a <see cref="Vector512{T}"/> with all its elements set to the specified item, as <typeparamref name="TUnsigned"/> (see <see cref="CreateVector128"/>).</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector512<TUnsigned> CreateVector512<T, TUnsigned>(T item)
            where TUnsigned : unmanaged, IBinaryInteger<TUnsigned>, IUnsignedNumber<TUnsigned> =>
            IntPtr.Size == 4 && X86Base.IsSupported && Unsafe.SizeOf<TUnsigned>() == sizeof(ulong) ?
                Vector512.Create(CreateVector128FromHalves(ulong.CreateTruncating(Unsafe.BitCast<T, TUnsigned>(item)))).As<ulong, TUnsigned>() :
                Vector512.Create(Unsafe.BitCast<T, TUnsigned>(item));

        /// <summary>Creates a <see cref="Vector128{T}"/> with all its elements set to the specified value, from its two 32-bit halves.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<ulong> CreateVector128FromHalves(ulong value)
        {
            uint lower = (uint)value, upper = (uint)(value >> 32);
            return Vector128.Create(lower, upper, lower, upper).AsUInt64();
        }

        internal sealed class DebuggerProxy<T, TUnsigned, TVector>(IEnumerable<T> enumerable) : ImmutableEnumerableDebuggerProxy<T>(enumerable);
    }

    /// <summary>
    /// Provides a <see cref="FrozenSet{T}"/> for integral values (or enums) that all fit in a single vector,
    /// which is searched with a single vector comparison.
    /// </summary>
    /// <typeparam name="T">The type of the values in the set.</typeparam>
    /// <typeparam name="TUnsigned">The unsigned integer type with the same size as <typeparamref name="T"/>, as which the values are compared.</typeparam>
    /// <typeparam name="TVector">The vector type the values are stored in: <see cref="Vector128{T}"/>, <see cref="Vector256{T}"/> or <see cref="Vector512{T}"/> of <typeparamref name="TUnsigned"/>.</typeparam>
    /// <remarks>
    /// For the types supported, equality as defined by <see cref="EqualityComparer{T}.Default"/> is bitwise equality, so
    /// values are stored and compared as the unsigned integer type of the same size, which they're reinterpreted as with
    /// <see cref="Unsafe.BitCast{TFrom, TTo}"/>: both types are integers (or enums) without references, and of the same
    /// size, which <see cref="Unsafe.BitCast{TFrom, TTo}"/> validates too. The vector is stored as <typeparamref name="TVector"/>
    /// rather than in an array, so that a lookup doesn't need to load the array or check its length, and it's only ever
    /// reinterpreted as the vector type it is.
    /// </remarks>
    [DebuggerTypeProxy(typeof(SmallIntegralFrozenSet.DebuggerProxy<,,>))]
    internal sealed class SmallIntegralFrozenSet<T, TUnsigned, TVector> : FrozenSetInternalBase<T, SmallIntegralFrozenSet<T, TUnsigned, TVector>.GSW>
        where TUnsigned : unmanaged, IBinaryInteger<TUnsigned>, IUnsignedNumber<TUnsigned>
        where TVector : struct
    {
        /// <summary>The items in the set.</summary>
        private readonly T[] _items;

        /// <summary>
        /// The items in the set, as <typeparamref name="TUnsigned"/>, padded with copies of the first item. As the padding
        /// repeats the first item, a match in the padding implies a match in the first slot, which is the one reported.
        /// </summary>
        private readonly TVector _vector;

        internal SmallIntegralFrozenSet(HashSet<T> source) : base(EqualityComparer<T>.Default)
        {
            Debug.Assert(
                typeof(TVector) == typeof(Vector128<TUnsigned>) ||
                typeof(TVector) == typeof(Vector256<TUnsigned>) ||
                typeof(TVector) == typeof(Vector512<TUnsigned>));

            _items = new T[source.Count];
            source.CopyTo(_items);

            Span<TUnsigned> values = stackalloc TUnsigned[Unsafe.SizeOf<TVector>() / Unsafe.SizeOf<TUnsigned>()];
            Debug.Assert(_items.Length <= values.Length);
            values.Fill(Unsafe.BitCast<T, TUnsigned>(_items[0]));
            for (int i = 1; i < _items.Length; i++)
            {
                values[i] = Unsafe.BitCast<T, TUnsigned>(_items[i]);
            }

            _vector =
                typeof(TVector) == typeof(Vector128<TUnsigned>) ? Unsafe.BitCast<Vector128<TUnsigned>, TVector>(Vector128.Create((ReadOnlySpan<TUnsigned>)values)) :
                typeof(TVector) == typeof(Vector256<TUnsigned>) ? Unsafe.BitCast<Vector256<TUnsigned>, TVector>(Vector256.Create((ReadOnlySpan<TUnsigned>)values)) :
                Unsafe.BitCast<Vector512<TUnsigned>, TVector>(Vector512.Create((ReadOnlySpan<TUnsigned>)values));
        }

        private protected override T[] ItemsCore => _items;
        private protected override Enumerator GetEnumeratorCore() => new Enumerator(_items);
        private protected override int CountCore => _items.Length;

        // Only one of the paths below remains once compiled for a given TVector, which is then small enough to be
        // worth inlining into Contains when the call gets devirtualized.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected override int FindItemIndex(T item)
        {
            // The result is computed without branching on whether the value was found (as IndexOf would once inlined into
            // a loop), as such a branch is mispredicted half of the time when hits and misses are mixed. The index of the
            // first match is computed with TrailingZeroCount, which is never given 0 when it isn't a single instruction
            // (without BMI1, or for a 64-bit mask on 32-bit platforms), as it would then branch on it.
            if (typeof(TVector) == typeof(Vector128<TUnsigned>))
            {
                // There are at most 16 lanes, so with bit 16 set, TrailingZeroCount returns 16 if there's no match.
                uint matches = Vector128.Equals(SmallIntegralFrozenSet.CreateVector128<T, TUnsigned>(item), Unsafe.BitCast<TVector, Vector128<TUnsigned>>(_vector)).ExtractMostSignificantBits();
                int index = BitOperations.TrailingZeroCount(matches | (1u << 16));
                return index | -(index >> 4);
            }

            if (typeof(TVector) == typeof(Vector256<TUnsigned>))
            {
                // Vector256 is only used when accelerated, i.e. with AVX2, so TrailingZeroCount is a single instruction.
                uint matches = Vector256.Equals(SmallIntegralFrozenSet.CreateVector256<T, TUnsigned>(item), Unsafe.BitCast<TVector, Vector256<TUnsigned>>(_vector)).ExtractMostSignificantBits();
                int index = BitOperations.TrailingZeroCount(matches); // 32 if there's no match
                return index | -(index >> 5);
            }

            Debug.Assert(typeof(TVector) == typeof(Vector512<TUnsigned>));
            ulong matches512 = Vector512.Equals(SmallIntegralFrozenSet.CreateVector512<T, TUnsigned>(item), Unsafe.BitCast<TVector, Vector512<TUnsigned>>(_vector)).ExtractMostSignificantBits();
            int index512;
            if (Vector512<TUnsigned>.Count <= 32)
            {
                index512 = BitOperations.TrailingZeroCount((uint)matches512); // 32 if there's no match
                return index512 | -(index512 >> 5);
            }

            if (IntPtr.Size == 8)
            {
                index512 = BitOperations.TrailingZeroCount(matches512); // 64 if there's no match
            }
            else
            {
                // On 32-bit platforms, the halves are counted separately, the upper one only counting if the lower one is 0.
                int lower = BitOperations.TrailingZeroCount((uint)matches512);
                int upper = BitOperations.TrailingZeroCount((uint)(matches512 >> 32));
                index512 = lower + (upper & -(lower >> 5)); // 64 if there's no match
            }

            return index512 | -(index512 >> 6);
        }

        internal struct GSW : IGenericSpecializedWrapper
        {
            private SmallIntegralFrozenSet<T, TUnsigned, TVector> _set;
            public void Store(FrozenSet<T> set) => _set = (SmallIntegralFrozenSet<T, TUnsigned, TVector>)set;

            public int Count => _set.Count;
            public IEqualityComparer<T> Comparer => _set.Comparer;
            public int FindItemIndex(T item) => _set.FindItemIndex(item);
            public Enumerator GetEnumerator() => _set.GetEnumerator();
        }
    }
}
