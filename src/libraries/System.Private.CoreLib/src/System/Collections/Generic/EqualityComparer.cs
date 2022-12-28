// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Collections.Generic
{
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public abstract partial class EqualityComparer<T> : IEqualityComparer, IEqualityComparer<T>
    {
        // public static EqualityComparer<T> Default is runtime-specific

        /// <summary>
        /// Creates an <see cref="EqualityComparer{T}"/> by using the specified delegates as the implementation of the comparer's
        /// <see cref="EqualityComparer{T}.Equals"/> and <see cref="EqualityComparer{T}.GetHashCode"/> methods.
        /// </summary>
        /// <param name="equals">The delegate to use to implement the <see cref="EqualityComparer{T}.Equals"/> method.</param>
        /// <param name="getHashCode">
        /// The delegate to use to implement the <see cref="EqualityComparer{T}.GetHashCode"/> method.
        /// If no delegate is supplied, calls to the resulting comparer's <see cref="EqualityComparer{T}.GetHashCode"/>
        /// will throw <see cref="NotSupportedException"/>.
        /// </param>
        /// <returns>The new comparer.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="equals"/> delegate was null.</exception>
        public static EqualityComparer<T> Create(Func<T?, T?, bool> equals, Func<T, int>? getHashCode = null)
        {
            ArgumentNullException.ThrowIfNull(equals);

            getHashCode ??= _ => throw new NotSupportedException();

            return new DelegateEqualityComparer<T>(equals, getHashCode);
        }

        public abstract bool Equals(T? x, T? y);
        public abstract int GetHashCode([DisallowNull] T obj);

        int IEqualityComparer.GetHashCode(object? obj)
        {
            if (obj == null) return 0;
            if (obj is T) return GetHashCode((T)obj);
            ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArgumentForComparison);
            return 0;
        }

        bool IEqualityComparer.Equals(object? x, object? y)
        {
            if (x == y) return true;
            if (x == null || y == null) return false;
            if ((x is T) && (y is T)) return Equals((T)x, (T)y);
            ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArgumentForComparison);
            return false;
        }

#if !NATIVEAOT
        internal virtual int IndexOf(T[] array, T value, int startIndex, int count)
        {
            int endIndex = startIndex + count;
            for (int i = startIndex; i < endIndex; i++)
            {
                if (Equals(array[i], value))
                {
                    return i;
                }
            }
            return -1;
        }

        internal virtual int LastIndexOf(T[] array, T value, int startIndex, int count)
        {
            int endIndex = startIndex - count + 1;
            for (int i = startIndex; i >= endIndex; i--)
            {
                if (Equals(array[i], value))
                {
                    return i;
                }
            }
            return -1;
        }
#endif
    }

    internal sealed class DelegateEqualityComparer<T> : EqualityComparer<T>
    {
        private readonly Func<T?, T?, bool> _equals;
        private readonly Func<T, int> _getHashCode;

        public DelegateEqualityComparer(Func<T?, T?, bool> equals, Func<T, int> getHashCode)
        {
            _equals = equals;
            _getHashCode = getHashCode;
        }

        public override bool Equals(T? x, T? y) =>
            _equals(x, y);

        public override int GetHashCode([DisallowNull] T obj) =>
            _getHashCode(obj);

        public override bool Equals(object? obj) =>
            obj is DelegateEqualityComparer<T> other &&
            _equals == other._equals &&
            _getHashCode == other._getHashCode;

        public override int GetHashCode() =>
            HashCode.Combine(_equals.GetHashCode(), _getHashCode.GetHashCode());
    }

    // The methods in this class look identical to the inherited methods, but the calls
    // to Equal bind to IEquatable<T>.Equals(T) instead of Object.Equals(Object)
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    // Needs to be public to support binary serialization compatibility
    public sealed partial class GenericEqualityComparer<T> : EqualityComparer<T> where T : IEquatable<T>?
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(T? x, T? y)
        {
            if (x is null)
                return y is null;
            if (y is null)
                return false;

            return x.Equals(y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode([DisallowNull] T obj) =>
            obj?.GetHashCode() ?? 0;

        // Equals method for the comparer itself.
        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj != null && GetType() == obj.GetType();

        public override int GetHashCode() =>
            GetType().GetHashCode();
    }

    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    // Needs to be public to support binary serialization compatibility
    public sealed partial class NullableEqualityComparer<T> : EqualityComparer<T?>, ISerializable where T : struct
    {
        public NullableEqualityComparer() { }
        private NullableEqualityComparer(SerializationInfo info, StreamingContext context) { }
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (!typeof(T).IsAssignableTo(typeof(IEquatable<T>)))
            {
                // We used to use NullableComparer only for types implementing IEquatable<T>
                info.SetType(typeof(ObjectEqualityComparer<T?>));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(T? x, T? y)
        {
            if (x.HasValue)
            {
                if (y.HasValue) return EqualityComparer<T>.Default.Equals(x.value, y.value);
                return false;
            }
            if (y.HasValue) return false;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode(T? obj) =>
            obj.GetHashCode();

        // Equals method for the comparer itself.
        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj != null && GetType() == obj.GetType();

        public override int GetHashCode() =>
            GetType().GetHashCode();
    }

    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    // Needs to be public to support binary serialization compatibility
    public sealed partial class ObjectEqualityComparer<T> : EqualityComparer<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(T? x, T? y)
        {
            if (x is null)
                return y is null;
            if (y is null)
                return false;

            return x.Equals(y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode([DisallowNull] T obj) =>
            obj?.GetHashCode() ?? 0;

        // Equals method for the comparer itself.
        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj != null && GetType() == obj.GetType();

        public override int GetHashCode() =>
            GetType().GetHashCode();
    }

    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    // Needs to be public to support binary serialization compatibility
    public sealed partial class ByteEqualityComparer : EqualityComparer<byte>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(byte x, byte y) =>
            x == y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode(byte b) =>
            b.GetHashCode();

        // Equals method for the comparer itself.
        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj != null && GetType() == obj.GetType();

        public override int GetHashCode() =>
            GetType().GetHashCode();
    }

    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    // Needs to be public to support binary serialization compatibility
    public sealed partial class EnumEqualityComparer<T> : EqualityComparer<T>, ISerializable where T : struct, Enum
    {
        public EnumEqualityComparer() { }

        // This is used by the serialization engine.
        private EnumEqualityComparer(SerializationInfo information, StreamingContext context) { }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // For back-compat we need to serialize the comparers for enums with underlying types other than int as ObjectEqualityComparer
            if (Type.GetTypeCode(typeof(T)) != TypeCode.Int32)
            {
                info.SetType(typeof(ObjectEqualityComparer<T>));
            }
        }

        // public override bool Equals(T x, T y) is runtime-specific

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode(T obj) =>
            obj.GetHashCode();

        // Equals method for the comparer itself.
        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj != null && GetType() == obj.GetType();

        public override int GetHashCode() =>
            GetType().GetHashCode();
    }

    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public static class EqualityComparer
    {
        /// <summary>
        /// Creates an <see cref="IEqualityComparer{TEnumerable}"/> which computes the equality comparison by
        /// using sequentially the provided <paramref name="elementComparer"/> on each element of the enumerables to compare.
        /// </summary>
        /// <typeparam name="TEnumerable">Type of the enumerables to compare</typeparam>
        /// <typeparam name="T">Type of the elements in the enumerables to compare</typeparam>
        /// <param name="elementComparer">The comparer used to compute the sequential equality.
        /// If no comparer is provided, the default comparer for <typeparamref name="T"/> is used (see <see cref="EqualityComparer{T}.Default"/>).</param>
        /// <returns>The new equality comparer</returns>
        public static IEqualityComparer<TEnumerable> CreateEnumerableComparer<TEnumerable, T>(IEqualityComparer<T>? elementComparer = null)
            where TEnumerable : IEnumerable<T> =>
            new EnumerableEqualityComparer<TEnumerable, T>(elementComparer);

        /// <summary>
        /// Creates an <see cref="IEqualityComparer{TSet}"/> which computes the equality comparison by
        /// using the provided <paramref name="elementComparer"/> between the elements of the sets to compare.
        /// </summary>
        /// <typeparam name="TSet">Type of the sets to compare</typeparam>
        /// <typeparam name="T">Type of the elements in the sets to compare</typeparam>
        /// <param name="elementComparer">The comparer used to compute the sets equality.
        /// If no comparer is provided, the default comparer for <typeparamref name="T"/> is used (see <see cref="EqualityComparer{T}.Default"/>).</param>
        /// <returns>The new equality comparer</returns>
        public static IEqualityComparer<TSet> CreateSetComparer<TSet, T>(IEqualityComparer<T>? elementComparer = null)
            where TSet : IReadOnlySet<T> =>
            new SetEqualityComparer<TSet, T>(elementComparer);

        /// <summary>
        /// Creates an <see cref="IEqualityComparer{TDictionary}"/> which computes the equality comparison by
        /// using the provided <paramref name="keyComparer"/> (resp. <paramref name="valueComparer"/>) between the keys (resp. values) of the dictionaries to compare.
        /// </summary>
        /// <typeparam name="TDictionary">Type of the dictionaries to compare</typeparam>
        /// <typeparam name="TKey">Type of the keys in the dictionaries to compare</typeparam>
        /// <typeparam name="TValue">Type fo the values in the dictionaries to compare</typeparam>
        /// <param name="keyComparer">>The comparer used to compute the equality of dictionary keys.
        /// If no comparer is provided, the default comparer for <typeparamref name="TKey"/> is used (see <see cref="EqualityComparer{T}.Default"/>).</param>
        /// <param name="valueComparer">>The comparer used to compute the equality of dictionary values.
        /// If no comparer is provided, the default comparer for <typeparamref name="TValue"/> is used (see <see cref="EqualityComparer{T}.Default"/>).</param>
        /// <returns>The new equality comparer</returns>
        public static IEqualityComparer<TDictionary> CreateDictionaryComparer<TDictionary, TKey, TValue>(IEqualityComparer<TKey>? keyComparer = null, IEqualityComparer<TValue>? valueComparer = null)
            where TDictionary : IReadOnlyDictionary<TKey, TValue> =>
            new DictionaryEqualityComparer<TDictionary, TKey, TValue>(keyComparer, valueComparer);
    }

    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    // Needs to be public to support binary serialization compatibility
    public sealed partial class EnumerableEqualityComparer<TEnumerable, T> : EqualityComparer<TEnumerable>
        where TEnumerable : IEnumerable<T>
    {
        private readonly IEqualityComparer<T> _elementComparer;

        public EnumerableEqualityComparer(IEqualityComparer<T>? elementComparer = null)
        {
            _elementComparer = elementComparer ?? EqualityComparer<T>.Default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(TEnumerable? x, TEnumerable? y)
        {
            if (x is null)
                return y is null;
            if (y is null)
                return false;

            return SequenceEquals(x, y);
        }

        private bool SequenceEquals(TEnumerable first, TEnumerable second)
        {
            if (first is ICollection<T> firstCol && second is ICollection<T> secondCol)
            {
                if (first is T[] firstArray && second is T[] secondArray)
                {
                    return ((ReadOnlySpan<T>)firstArray).SequenceEqual(secondArray, _elementComparer);
                }

                if (firstCol.Count != secondCol.Count)
                {
                    return false;
                }

                if (firstCol is IList<T> firstList && secondCol is IList<T> secondList)
                {

                    int count = firstCol.Count;
                    for (int i = 0; i < count; i++)
                    {
                        if (!_elementComparer.Equals(firstList[i], secondList[i]))
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }

            using IEnumerator<T> xEnumerator = first.GetEnumerator();
            using IEnumerator<T> yEnumerator = second.GetEnumerator();

            while (xEnumerator.MoveNext())
            {
                if (!(yEnumerator.MoveNext() && _elementComparer.Equals(xEnumerator.Current, yEnumerator.Current)))
                {
                    return false;
                }
            }

            return !yEnumerator.MoveNext();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode([DisallowNull] TEnumerable obj) => obj switch
        {
            IReadOnlySet<T> set => set.Count == 0 ? 1 : 2,
            IReadOnlyCollection<T> collection => 1 + collection.Count,
            null => 0,
            _ => -1
        };

        // Equals method for the comparer itself.
        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj is not null &&
            obj is EnumerableEqualityComparer<TEnumerable, T> other &&
            _elementComparer == other._elementComparer;

        public override int GetHashCode() => HashCode.Combine(
            GetType().GetHashCode(),
            _elementComparer.GetHashCode());
    }

    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    // Needs to be public to support binary serialization compatibility
    public sealed partial class SetEqualityComparer<TSet, T> : EqualityComparer<TSet>
        where TSet : IReadOnlySet<T>
    {
        private readonly IEqualityComparer<T> _elementComparer;

        public SetEqualityComparer(IEqualityComparer<T>? elementComparer = null)
        {
            _elementComparer = elementComparer ?? EqualityComparer<T>.Default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(TSet? x, TSet? y)
        {
            if (x is null)
                return y is null;
            if (y is null)
                return false;

            if (x.Count != y.Count)
                return false;

            if (x is HashSet<T> hashSetx && hashSetx.Comparer.Equals(_elementComparer) &&
                y is HashSet<T> hashSety && hashSety.Comparer.Equals(_elementComparer))
                return hashSetx.SetEquals(hashSety);

            // Otherwise, do an O(N^2) match.
            foreach (T yi in y)
            {
                bool found = false;
                foreach (T xi in x)
                {
                    if (_elementComparer.Equals(yi, xi))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return false;
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode([DisallowNull] TSet obj) => obj switch
        {
            IReadOnlySet<T> set => set.Count == 0 ? 1 : 2,
            null => 0
        };

        // Equals method for the comparer itself.
        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj is not null &&
            obj is SetEqualityComparer<TSet, T> other &&
            _elementComparer == other._elementComparer;

        public override int GetHashCode() => HashCode.Combine(
            GetType().GetHashCode(),
            _elementComparer.GetHashCode());
    }

    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    // Needs to be public to support binary serialization compatibility
    public sealed partial class DictionaryEqualityComparer<TDictionary, TKey, TValue> : EqualityComparer<TDictionary>
        where TDictionary : IReadOnlyDictionary<TKey, TValue>
    {
        private readonly IEqualityComparer<TKey> _keyComparer;
        private readonly IEqualityComparer<TValue> _valueComparer;

        public DictionaryEqualityComparer(
            IEqualityComparer<TKey>? keyComparer = null,
            IEqualityComparer<TValue>? valueComparer = null)
        {
            _keyComparer = keyComparer ?? EqualityComparer<TKey>.Default;
            _valueComparer = valueComparer ?? EqualityComparer<TValue>.Default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(TDictionary? x, TDictionary? y)
        {
            if (x is null)
                return y is null;
            if (y is null)
                return false;

            if (x.Count != y.Count)
                return false;

            // if (x is Dictionary<TKey, TValue> dictx && dictx.Comparer.Equals(_keyComparer) &&
            //     y is Dictionary<TKey, TValue> dicty && dicty.Comparer.Equals(_keyComparer))
            // {
            //     foreach ((TKey yKey, TValue yValue) in y)
            //     {
            //         if (!x.TryGetValue(yKey, out TValue xValue) || !_valueComparer.Equals(yValue, xValue))
            //             return false;
            //     }
            //
            //     return true;
            // }

            foreach ((TKey yKey, TValue yValue) in y)
            {
                bool found = false;
                foreach ((TKey xKey, TValue xValue) in x)
                {
                    if (_keyComparer.Equals(yKey, xKey) &&
                        _valueComparer.Equals(yValue, xValue))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return false;
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode([DisallowNull] TDictionary obj) => obj switch
        {
            IReadOnlyDictionary<TKey, TValue> dictionary => dictionary.Count == 0 ? 1 : 2,
            null => 0
        };

        // Equals method for the comparer itself.
        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj is not null &&
            obj is DictionaryEqualityComparer<TDictionary, TKey, TValue> other &&
            _keyComparer == other._keyComparer &&
            _valueComparer == other._valueComparer;

        public override int GetHashCode() => HashCode.Combine(
            GetType().GetHashCode(),
            _keyComparer.GetHashCode(),
            _valueComparer.GetHashCode());
    }
}
