// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Collections.Frozen
{
    /// <summary>Provides initialization methods for instances of the <see cref="SmallIntegralFrozenSet{T, TUnsigned}"/> class.</summary>
    internal static class SmallIntegralFrozenSet
    {
        /// <summary>Gets whether <typeparamref name="T"/> is supported and <paramref name="count"/> values fit in a single accelerated vector.</summary>
        public static bool IsSupported<T>(int count) =>
            Vector128.IsHardwareAccelerated &&
            Constants.IsIntegralOrEnum<T>() &&
            count * Unsafe.SizeOf<T>() <= (Vector512.IsHardwareAccelerated ? Vector512<byte>.Count : Vector256.IsHardwareAccelerated ? Vector256<byte>.Count : Vector128<byte>.Count);

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
            int vectorLength =
                count <= Vector128<TUnsigned>.Count ? Vector128<TUnsigned>.Count :
                Vector256.IsHardwareAccelerated && count <= Vector256<TUnsigned>.Count ? Vector256<TUnsigned>.Count :
                Vector512.IsHardwareAccelerated && count <= Vector512<TUnsigned>.Count ? Vector512<TUnsigned>.Count :
                0;

            return vectorLength != 0 ? new SmallIntegralFrozenSet<T, TUnsigned>(source, vectorLength) : null;
        }
    }

    /// <summary>
    /// Provides a <see cref="FrozenSet{T}"/> for integral values (or enums) that all fit in a single vector,
    /// which is searched with a single vector comparison.
    /// </summary>
    /// <typeparam name="T">The type of the values in the set.</typeparam>
    /// <typeparam name="TUnsigned">The unsigned integer type with the same size as <typeparamref name="T"/>, as which the values are compared.</typeparam>
    /// <remarks>
    /// For the types supported, equality as defined by <see cref="EqualityComparer{T}.Default"/> is bitwise equality,
    /// so values are stored and compared as the unsigned integer type of the same size.
    /// </remarks>
    internal sealed class SmallIntegralFrozenSet<T, TUnsigned> : FrozenSetInternalBase<T, SmallIntegralFrozenSet<T, TUnsigned>.GSW>
        where TUnsigned : unmanaged, IBinaryInteger<TUnsigned>, IUnsignedNumber<TUnsigned>
    {
        /// <summary>The items in the set.</summary>
        private readonly T[] _items;

        /// <summary>
        /// The items in the set, as <typeparamref name="TUnsigned"/>, padded with copies of the first item to the length of the vector
        /// used to search them (whose size is thus determined by the length of this array). As the padding repeats the first item,
        /// a match in the padding implies a match in the first slot, which is the one reported.
        /// </summary>
        private readonly TUnsigned[] _vector;

        internal SmallIntegralFrozenSet(HashSet<T> source, int vectorLength) : base(EqualityComparer<T>.Default)
        {
            Debug.Assert(source.Count <= vectorLength);

            _items = new T[source.Count];
            source.CopyTo(_items);

            _vector = new TUnsigned[vectorLength];
            for (int i = 0; i < _items.Length; i++)
            {
                _vector[i] = Unsafe.BitCast<T, TUnsigned>(_items[i]);
            }
            _vector.AsSpan(_items.Length).Fill(_vector[0]);
        }

        private protected override T[] ItemsCore => _items;
        private protected override Enumerator GetEnumeratorCore() => new Enumerator(_items);
        private protected override int CountCore => _items.Length;

        // Only one of the paths below survives JIT compilation, which is then small enough to be
        // worth inlining into Contains when the call gets devirtualized.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected override int FindItemIndex(T item)
        {
            TUnsigned value = Unsafe.BitCast<T, TUnsigned>(item);
            TUnsigned[] vector = _vector;

            // IndexOf doesn't branch on whether the value is found, so the generated code doesn't depend on whether
            // hits or misses were more frequent when the method was profiled by dynamic PGO.
            if (Vector512.IsHardwareAccelerated && vector.Length == Vector512<TUnsigned>.Count)
            {
                return Vector512.IndexOf(Vector512.Create((ReadOnlySpan<TUnsigned>)vector), value);
            }

            if (Vector256.IsHardwareAccelerated && vector.Length == Vector256<TUnsigned>.Count)
            {
                return Vector256.IndexOf(Vector256.Create((ReadOnlySpan<TUnsigned>)vector), value);
            }

            return Vector128.IndexOf(Vector128.Create((ReadOnlySpan<TUnsigned>)vector), value);
        }

        internal struct GSW : IGenericSpecializedWrapper
        {
            private SmallIntegralFrozenSet<T, TUnsigned> _set;
            public void Store(FrozenSet<T> set) => _set = (SmallIntegralFrozenSet<T, TUnsigned>)set;

            public int Count => _set.Count;
            public IEqualityComparer<T> Comparer => _set.Comparer;
            public int FindItemIndex(T item) => _set.FindItemIndex(item);
            public Enumerator GetEnumerator() => _set.GetEnumerator();
        }
    }
}
