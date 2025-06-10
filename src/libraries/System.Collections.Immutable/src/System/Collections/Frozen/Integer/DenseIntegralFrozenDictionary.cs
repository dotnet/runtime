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
        /// Maximum allowed factor by which the spread between the min and max of keys in the dictionary may exceed the count.
        /// </summary>
        /// <remarks>
        /// This is dialable. The larger this value, the more likely this implementation will be used,
        /// and the more memory will be consumed to store the values. The value of 10 means that up to 90% of the
        /// slots in the values array may be unused.
        /// </remarks>
        private const int LengthToCountFactor = 10;

        public static FrozenDictionary<TKey, TValue>? CreateIfValid<TKey, TValue>(Dictionary<TKey, TValue> source)
            where TKey : notnull
        {
            // Int32 and integer types that fit within Int32. This is to minimize difficulty later validating that
            // inputs are in range of int: we can always cast everything here to Int32 without loss of information.
            return
                typeof(TKey) == typeof(byte) || (typeof(TKey).IsEnum && typeof(TKey).GetEnumUnderlyingType() == typeof(byte)) ? CreateIfValid<TKey, byte, TValue>(source) :
                typeof(TKey) == typeof(sbyte) || (typeof(TKey).IsEnum && typeof(TKey).GetEnumUnderlyingType() == typeof(sbyte)) ? CreateIfValid<TKey, sbyte, TValue>(source) :
                typeof(TKey) == typeof(ushort) || (typeof(TKey).IsEnum && typeof(TKey).GetEnumUnderlyingType() == typeof(ushort)) ? CreateIfValid<TKey, ushort, TValue>(source) :
                typeof(TKey) == typeof(short) || (typeof(TKey).IsEnum && typeof(TKey).GetEnumUnderlyingType() == typeof(short)) ? CreateIfValid<TKey, short, TValue>(source) :
                typeof(TKey) == typeof(char) ? CreateIfValid<TKey, char, TValue>(source) :
                typeof(TKey) == typeof(int) || (typeof(TKey).IsEnum && typeof(TKey).GetEnumUnderlyingType() == typeof(int)) ? CreateIfValid<TKey, int, TValue>(source) :
                null;
        }

        private static FrozenDictionary<TKey, TValue>? CreateIfValid<TKey, TKeyUnderlying, TValue>(Dictionary<TKey, TValue> source)
            where TKey : notnull
            where TKeyUnderlying : unmanaged, IBinaryInteger<TKeyUnderlying>
        {
            int count = source.Count;

            Dictionary<TKey, TValue>.Enumerator e = source.GetEnumerator();
            if (e.MoveNext())
            {
                // Get the first element and treat it as the min and max. Then continue enumerating the remainder
                // of the dictionary to track the full min and max. Along the way, bail if at any point the length
                // exceeds the allowed limit based on the known count.
                int min = int.CreateTruncating((TKeyUnderlying)(object)e.Current.Key);
                int max = min;
                while (e.MoveNext())
                {
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

                long maxAllowedLength = Math.Min((long)count * LengthToCountFactor, Array.MaxLength);
                long length = (long)max - min + 1;
                if (length <= maxAllowedLength)
                {
                    var keys = new TKey[count];
                    var values = new TValue[keys.Length];

                    if (min == 0 && length == count)
                    {
                        // All of the keys are contiguous starting at 0, so we can use an implementation that
                        // just stores all the values in an array indexed by key. This both provides faster access
                        // and allows the single values array to be used for lookups and for ValuesCore.
                        foreach (KeyValuePair<TKey, TValue> entry in source)
                        {
                            int index = int.CreateTruncating((TKeyUnderlying)(object)entry.Key);
                            keys[index] = entry.Key;
                            values[index] = entry.Value;
                        }

                        return new WithFullValues<TKey, TKeyUnderlying, TValue>(keys, values);
                    }
                    else
                    {
                        // Some of the keys in the length are missing, so create an array to hold optional values
                        // and populate the entries just for the elements we have. The 0th element of the optional
                        // values array corresponds to the element with the min key.
                        var optionalValues = new Optional<TValue>[length];
                        int i = 0;
                        foreach (KeyValuePair<TKey, TValue> entry in source)
                        {
                            keys[i] = entry.Key;
                            values[i] = entry.Value;
                            i++;

                            optionalValues[int.CreateTruncating((TKeyUnderlying)(object)entry.Key) - min] = new(entry.Value, hasValue: true);
                        }

                        return new WithOptionalValues<TKey, TKeyUnderlying, TValue>(keys, values, optionalValues, min);
                    }
                }
            }

            return null;
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
