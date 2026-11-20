// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

namespace ILLink.Shared.DataFlow
{
    // This is a dictionary along with a default value, where every possible key either maps to
    // the default value, or another value. The default value is never explicitly stored in the dictionary,
    // and the empty dictionary (where all possible keys have the default value) is represented without
    // actually allocating a dictionary.
    public struct DefaultValueDictionary<TKey, TValue> : IEquatable<DefaultValueDictionary<TKey, TValue>>,
        IEnumerable<KeyValuePair<TKey, TValue>>
        where TKey : IEquatable<TKey>
        where TValue : IEquatable<TValue>
    {
        private Dictionary<TKey, TValue>? Dictionary;
        private readonly TValue DefaultValue;

        public DefaultValueDictionary(TValue defaultValue) => (Dictionary, DefaultValue) = (null, defaultValue);

        private DefaultValueDictionary(TValue defaultValue, Dictionary<TKey, TValue> dictionary) => (Dictionary, DefaultValue) = (dictionary, defaultValue);

        public DefaultValueDictionary(DefaultValueDictionary<TKey, TValue> other)
        {
            Dictionary = other.Dictionary == null ? null : new Dictionary<TKey, TValue>(other.Dictionary);
            DefaultValue = other.DefaultValue;
        }

        internal DefaultValueDictionary(DefaultValueDictionary<TKey, TValue> other, int capacity)
        {
            Dictionary = new Dictionary<TKey, TValue>(capacity);
            if (other.Dictionary is not null)
            {
                foreach (KeyValuePair<TKey, TValue> item in other.Dictionary)
                {
                    Dictionary.Add(item.Key, item.Value);
                }
            }
            DefaultValue = other.DefaultValue;
        }

        public TValue Get(TKey key) => Dictionary?.TryGetValue(key, out var value) == true ? value : DefaultValue;

        public void Set(TKey key, TValue value)
        {
            if (value.Equals(DefaultValue))
                Dictionary?.Remove(key);
            else
                (Dictionary ??= new Dictionary<TKey, TValue>())[key] = value;
        }

        public bool Equals(DefaultValueDictionary<TKey, TValue> other)
        {
            if (!DefaultValue.Equals(other.DefaultValue))
                return false;

            if (Dictionary == null)
                return other.Dictionary == null;

            if (other.Dictionary == null)
                return false;

            if (Dictionary.Count != other.Dictionary.Count)
                return false;

            foreach (var kvp in other.Dictionary)
            {
                if (!Get(kvp.Key).Equals(kvp.Value))
                    return false;
            }

            return true;
        }

        public override bool Equals(object? obj) => obj is DefaultValueDictionary<TKey, TValue> other && Equals(other);

        public int Count => Dictionary?.Count ?? 0;

        public Enumerator GetEnumerator() => Dictionary is null ? default : new(Dictionary);

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            private readonly bool _hasDictionary;
            private Dictionary<TKey, TValue>.Enumerator _enumerator;

            internal Enumerator(Dictionary<TKey, TValue> dictionary)
            {
                _hasDictionary = true;
                _enumerator = dictionary.GetEnumerator();
            }

            public KeyValuePair<TKey, TValue> Current => _hasDictionary
                ? _enumerator.Current
                : Enumerable.Empty<KeyValuePair<TKey, TValue>>().GetEnumerator().Current;

            object IEnumerator.Current => _hasDictionary
                ? GetCurrent(ref _enumerator)
                : ((IEnumerator)Enumerable.Empty<KeyValuePair<TKey, TValue>>().GetEnumerator()).Current;

            public bool MoveNext()
            {
                if (!_hasDictionary)
                {
                    return false;
                }

                // Avoid exposing the containing struct's address on the empty path.
                Dictionary<TKey, TValue>.Enumerator enumerator = _enumerator;
                bool hasNext = enumerator.MoveNext();
                _enumerator = enumerator;
                return hasNext;
            }

            public void Dispose() => _enumerator.Dispose();

            public void Reset()
            {
                if (_hasDictionary)
                {
                    ResetEnumerator(ref _enumerator);
                }
            }

            private static object GetCurrent<TEnumerator>(ref TEnumerator enumerator)
                where TEnumerator : struct, IEnumerator => enumerator.Current;

            private static void ResetEnumerator<TEnumerator>(ref TEnumerator enumerator)
                where TEnumerator : struct, IEnumerator => enumerator.Reset();
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.Append('{');
            if (Dictionary != null)
            {
                foreach (var kvp in Dictionary)
                    sb.AppendLine().Append('\t').Append(kvp.Key.ToString()).Append(" -> ").Append(kvp.Value.ToString());
            }
            sb.AppendLine().Append("\t_ -> ").Append(DefaultValue.ToString());
            sb.AppendLine().Append('}');
            return sb.ToString();
        }

        public DefaultValueDictionary<TKey, TValue> Clone()
        {
            var defaultValue = DefaultValue is IDeepCopyValue<TValue> copyDefaultValue ? copyDefaultValue.DeepCopy() : DefaultValue;
            if (Dictionary == null)
                return new DefaultValueDictionary<TKey, TValue>(defaultValue);

            var dict = new Dictionary<TKey, TValue>(Dictionary.Count);
            foreach (var kvp in Dictionary)
            {
                var key = kvp.Key;
                var value = kvp.Value;
                dict.Add(key, value is IDeepCopyValue<TValue> copyValue ? copyValue.DeepCopy() : value);
            }
            return new DefaultValueDictionary<TKey, TValue>(defaultValue, dict);
        }

        // Prevent warning CS0659 https://learn.microsoft.com/dotnet/csharp/misc/cs0659.
        // This type should never be used as a dictionary key.
        public override int GetHashCode() => throw new NotImplementedException();

        public static bool operator ==(DefaultValueDictionary<TKey, TValue> left, DefaultValueDictionary<TKey, TValue> right) => left.Equals(right);
        public static bool operator !=(DefaultValueDictionary<TKey, TValue> left, DefaultValueDictionary<TKey, TValue> right) => !(left == right);
    }
}
