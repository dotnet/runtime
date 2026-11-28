// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Collections.Frozen
{
    /// <summary>Provides initialization methods for instances of the <see cref="SmallIntegralFrozenSet{T, TUnsigned, TVector}"/> class.</summary>
    internal static class SmallIntegralFrozenSet
    {
        /// <summary>Gets whether <typeparamref name="T"/> is supported and <paramref name="count"/> values fit in a single accelerated vector.</summary>
        public static bool IsSupported<T>(int count) =>
            Vector128.IsHardwareAccelerated &&
            Constants.IsIntegralOrEnum<T>() &&
            count <= (Vector512.IsHardwareAccelerated ? Vector512<byte>.Count : Vector256.IsHardwareAccelerated ? Vector256<byte>.Count : Vector128<byte>.Count) / Unsafe.SizeOf<T>();

        /// <summary>Creates a set for the specified source, if <typeparamref name="T"/> is supported and the values fit in a single accelerated vector.</summary>
        public static FrozenSet<T>? CreateIfValid<T>(HashSet<T> source)
        {
            Debug.Assert(source.Count != 0);
            Debug.Assert(ReferenceEquals(source.Comparer, EqualityComparer<T>.Default));

            if (IsSupported<T>(source.Count))
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
            TUnsigned value = Unsafe.BitCast<T, TUnsigned>(item);

            // The result is computed without branching on whether the value was found (as IndexOf would once inlined into
            // a loop), as such a branch is mispredicted half of the time when hits and misses are mixed. When there's no
            // match, TrailingZeroCount returns the number of bits of the mask (32 or 64), which the shift turns into -1.
            if (typeof(TVector) == typeof(Vector128<TUnsigned>))
            {
                uint matches = Vector128.Equals(Unsafe.BitCast<TVector, Vector128<TUnsigned>>(_vector), Vector128.Create(value)).ExtractMostSignificantBits();
                int index = BitOperations.TrailingZeroCount(matches);
                return index | -(index >> 5);
            }

            if (typeof(TVector) == typeof(Vector256<TUnsigned>))
            {
                uint matches = Vector256.Equals(Unsafe.BitCast<TVector, Vector256<TUnsigned>>(_vector), Vector256.Create(value)).ExtractMostSignificantBits();
                int index = BitOperations.TrailingZeroCount(matches);
                return index | -(index >> 5);
            }

            Debug.Assert(typeof(TVector) == typeof(Vector512<TUnsigned>));
            ulong matches512 = Vector512.Equals(Unsafe.BitCast<TVector, Vector512<TUnsigned>>(_vector), Vector512.Create(value)).ExtractMostSignificantBits();
            int index512 = BitOperations.TrailingZeroCount(matches512);
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
