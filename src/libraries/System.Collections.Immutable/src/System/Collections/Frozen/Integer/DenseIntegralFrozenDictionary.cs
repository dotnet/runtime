// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Collections.Frozen
{
    /// <summary>Provides a <see cref="FrozenDictionary{TKey, TValue}"/> for densely-packed integral keys.</summary>
    internal sealed class DenseIntegralFrozenDictionary
    {
        /// <summary>
        /// Maximum allowed ratio of the number of key/value pairs to the range between the minimum and maximum keys.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is dialable. The closer the value gets to 0, the more likely this implementation will be used,
        /// and the more memory will be consumed to store the values. The value of 0.1 means that up to 90% of the
        /// slots in the values array may be unused.
        /// </para>
        /// <para>
        /// As an example, DaysOfWeek's min is 0, its max is 6, and it has 7 values, such that 7 / (6 - 0 + 1) = 1.0; thus
        /// with a threshold of 0.1, DaysOfWeek will use this implementation. But SocketError's min is -1, its max is 11004, and
        /// it has 47 values, such that 47 / (11004 - (-1) + 1) = 0.004; thus, SocketError will not use this implementation.
        /// </para>
        /// </remarks>
        private const double CountToLengthRatio = 0.1;

        public static bool TryCreate<TKey, TValue>(Dictionary<TKey, TValue> source, [NotNullWhen(true)] out FrozenDictionary<TKey, TValue>? result)
            where TKey : notnull
        {
            // Int32 and integer types that fit within Int32. This is to minimize difficulty later validating that
            // inputs are in range of int: we can always cast everything to Int32 without loss of information.

            if (typeof(TKey) == typeof(byte) || (typeof(TKey).IsEnum && typeof(TKey).GetEnumUnderlyingType() == typeof(byte)))
                return TryCreate<TKey, byte, TValue>(source, out result);

            if (typeof(TKey) == typeof(sbyte) || (typeof(TKey).IsEnum && typeof(TKey).GetEnumUnderlyingType() == typeof(sbyte)))
                return TryCreate<TKey, sbyte, TValue>(source, out result);

            if (typeof(TKey) == typeof(ushort) || (typeof(TKey).IsEnum && typeof(TKey).GetEnumUnderlyingType() == typeof(ushort)))
                return TryCreate<TKey, ushort, TValue>(source, out result);

            if (typeof(TKey) == typeof(short) || (typeof(TKey).IsEnum && typeof(TKey).GetEnumUnderlyingType() == typeof(short)))
                return TryCreate<TKey, short, TValue>(source, out result);

            if (typeof(TKey) == typeof(int) || (typeof(TKey).IsEnum && typeof(TKey).GetEnumUnderlyingType() == typeof(int)))
                return TryCreate<TKey, int, TValue>(source, out result);

            result = null;
            return false;
        }

        private static bool TryCreate<TKey, TKeyUnderlying, TValue>(Dictionary<TKey, TValue> source, [NotNullWhen(true)] out FrozenDictionary<TKey, TValue>? result)
            where TKey : notnull
            where TKeyUnderlying : unmanaged, IBinaryInteger<TKeyUnderlying>
        {
            // Start enumerating the dictionary to ensure it has at least one element.
            Dictionary<TKey, TValue>.Enumerator e = source.GetEnumerator();
            if (e.MoveNext())
            {
                // Get that element and treat it as the min and max. Then continue enumerating the remainder
                // of the dictionary to count the number of elements and track the full min and max.
                int count = 1;
                int min = int.CreateTruncating((TKeyUnderlying)(object)e.Current.Key);
                int max = min;
                while (e.MoveNext())
                {
                    count++;
                    int key = int.CreateTruncating((TKeyUnderlying)(object)e.Current.Key);
                    if (key < min)
                    {
                        min = key;
                    }
                    else if (key > max)
                    {
                        max = key;
                    }
                }

                // Based on the min and max, determine the spread. If the range fits within a non-negative Int32
                // and the ratio of the number of elements in the dictionary to the length is within the allowed
                // threshold, create the new dictionary.
                long length = (long)max - min + 1;
                Debug.Assert(length > 0);
                if (length <= int.MaxValue &&
                    (double)count / length >= CountToLengthRatio)
                {
                    // Create arrays of the keys and values, sorted ascending by key.
                    var keys = new TKey[count];
                    var values = new TValue[keys.Length];
                    int i = 0;
                    foreach (KeyValuePair<TKey, TValue> entry in source)
                    {
                        keys[i] = entry.Key;
                        values[i] = entry.Value;
                        i++;
                    }

                    if (i != keys.Length)
                    {
                        throw new InvalidOperationException(SR.CollectionModifiedDuringEnumeration);
                    }

                    // Sort the values so that we can more easily check for contiguity but also so that
                    // the keys/values returned from various properties/enumeration are in a predictable order.
                    Array.Sort(keys, values);

                    // Determine whether all of the keys are contiguous starting at 0.
                    bool isFull = true;
                    for (i = 0; i < keys.Length; i++)
                    {
                        if (int.CreateTruncating((TKeyUnderlying)(object)keys[i]) != i)
                        {
                            isFull = false;
                            break;
                        }
                    }

                    if (isFull)
                    {
                        // All of the keys are contiguous starting at 0, so we can use an implementation that
                        // just stores all the values in an array indexed by key. This both provides faster access
                        // and allows the single values array to be used for lookups and for ValuesCore.
                        result = new WithFullValues<TKey, TKeyUnderlying, TValue>(keys, values);
                    }
                    else
                    {
                        // Some of the keys in the length are missing, so create an array to hold optional values
                        // and populate the entries just for the elements we have. The 0th element of the optional
                        // values array corresponds to the element with the min key.
                        var optionalValues = new Optional<TValue>[length];
                        for (i = 0; i < keys.Length; i++)
                        {
                            optionalValues[int.CreateTruncating((TKeyUnderlying)(object)keys[i]) - min] = new(values[i], hasValue: true);
                        }

                        result = new WithOptionalValues<TKey, TKeyUnderlying, TValue>(keys, values, optionalValues, min);
                    }

                    return true;
                }
            }

            result = null;
            return false;
        }

        /// <summary>Implementation used when all keys are contiguous starting at 0.</summary>
        [DebuggerTypeProxy(typeof(DebuggerProxy<,,>))]
        private sealed class WithFullValues<TKey, TKeyUnderlying, TValue>(TKey[] keys, TValue[] values) :
            FrozenDictionary<TKey, TValue>(EqualityComparer<TKey>.Default)
            where TKey : notnull
            where TKeyUnderlying : IBinaryInteger<TKeyUnderlying>
        {
            private readonly TKey[] _keys = keys;
            private readonly TValue[] _values = values;

            private protected override TKey[] KeysCore => _keys;

            private protected override TValue[] ValuesCore => _values;

            private protected override int CountCore => _keys.Length;

            private protected override Enumerator GetEnumeratorCore() => new Enumerator(_keys, _values);

            private protected override ref readonly TValue GetValueRefOrNullRefCore(TKey key)
            {
                int index = int.CreateTruncating((TKeyUnderlying)(object)key);
                TValue[] values = _values;
                if ((uint)index < (uint)values.Length)
                {
                    return ref values[index];
                }

                return ref Unsafe.NullRef<TValue>();
            }
        }

        /// <summary>Implementation used when keys are not contiguous and/or do not start at 0.</summary>
        [DebuggerTypeProxy(typeof(DebuggerProxy<,,>))]
        private sealed class WithOptionalValues<TKey, TKeyUnderlying, TValue>(TKey[] keys, TValue[] values, Optional<TValue>[] optionalValues, int minInclusive) :
            FrozenDictionary<TKey, TValue>(EqualityComparer<TKey>.Default)
            where TKey : notnull
            where TKeyUnderlying : IBinaryInteger<TKeyUnderlying>
        {
            private readonly TKey[] _keys = keys;
            private readonly TValue[] _values = values;
            private readonly Optional<TValue>[] _optionalValues = optionalValues;
            private readonly int _minInclusive = minInclusive;

            private protected override TKey[] KeysCore => _keys;

            private protected override TValue[] ValuesCore => _values;

            private protected override int CountCore => _keys.Length;

            private protected override Enumerator GetEnumeratorCore() => new Enumerator(_keys, _values);

            private protected override ref readonly TValue GetValueRefOrNullRefCore(TKey key)
            {
                int index = int.CreateTruncating((TKeyUnderlying)(object)key) - _minInclusive;
                Optional<TValue>[] optionalValues = _optionalValues;
                if ((uint)index < (uint)optionalValues.Length)
                {
                    ref Optional<TValue> value = ref optionalValues[index];
                    if (value.HasValue)
                    {
                        return ref value.Value;
                    }
                }

                return ref Unsafe.NullRef<TValue>();
            }
        }

        private readonly struct Optional<TValue>(TValue value, bool hasValue)
        {
            public readonly TValue Value = value;
            public readonly bool HasValue = hasValue;
        }

        private sealed class DebuggerProxy<TKey, TKeyUnderlying, TValue>(IReadOnlyDictionary<TKey, TValue> dictionary) :
            ImmutableDictionaryDebuggerProxy<TKey, TValue>(dictionary)
            where TKey : notnull;
    }
}
