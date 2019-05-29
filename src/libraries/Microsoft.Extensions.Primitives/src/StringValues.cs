// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Internal;

namespace Microsoft.Extensions.Primitives
{
    /// <summary>
    /// Represents zero/null, one, or many strings in an efficient way.
    /// </summary>
    public readonly struct StringValues : IList<string>, IReadOnlyList<string>, IEquatable<StringValues>, IEquatable<string>, IEquatable<string[]>
    {
        public static readonly StringValues Empty = new StringValues(Array.Empty<string>());

        private readonly object _values;

        public StringValues(string value)
        {
            _values = value;
        }

        public StringValues(string[] values)
        {
            _values = values;
        }

        public static implicit operator StringValues(string value)
        {
            return new StringValues(value);
        }

        public static implicit operator StringValues(string[] values)
        {
            return new StringValues(values);
        }

        public static implicit operator string (StringValues values)
        {
            return values.GetStringValue();
        }

        public static implicit operator string[] (StringValues value)
        {
            return value.GetArrayValue();
        }

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                // Take local copy of _values so type checks remain valid even if the StringValues is overwritten in memory
                var value = _values;
                if (value is string)
                {
                    return 1;
                }
                if (value is null)
                {
                    return 0;
                }
                else
                {
                    // Not string, not null, can only be string[]
                    return Unsafe.As<string[]>(value).Length;
                }
            }
        }

        bool ICollection<string>.IsReadOnly
        {
            get { return true; }
        }

        string IList<string>.this[int index]
        {
            get { return this[index]; }
            set { throw new NotSupportedException(); }
        }

        public string this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                // Take local copy of _values so type checks remain valid even if the StringValues is overwritten in memory
                var value = _values;
                if (index == 0 && value is string str)
                {
                    return str;
                }
                else if (value != null)
                {
                    // Not string, not null, can only be string[]
                    return Unsafe.As<string[]>(value)[index]; // may throw
                }
                else
                {
                    return OutOfBounds(); // throws
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string OutOfBounds()
        {
            return Array.Empty<string>()[0]; // throws
        }

        public override string ToString()
        {
            return GetStringValue() ?? string.Empty;
        }

        private string GetStringValue()
        {
            // Take local copy of _values so type checks remain valid even if the StringValues is overwritten in memory
            var value = _values;
            if (value is string s)
            {
                return s;
            }
            else
            {
                return GetStringValueFromArray(value);
            }

            static string GetStringValueFromArray(object value)
            {
                if (value is null)
                {
                    return null;
                }

                Debug.Assert(value is string[]);
                // value is not null or string, array, can only be string[]
                var values = Unsafe.As<string[]>(value);
                switch (values.Length)
                {
                    case 0: return null;
                    case 1: return values[0];
                    default: return GetJoinedStringValueFromArray(values);
                }
            }

            static string GetJoinedStringValueFromArray(string[] values)
            {
                // Calculate final length
                var length = 0;
                for (var i = 0; i < values.Length; i++)
                {
                    var value = values[i];
                    // Skip null and empty values
                    if (value != null && value.Length > 0)
                    {
                        if (length > 0)
                        {
                            // Add seperator
                            length++;
                        }

                        length += value.Length;
                    }
                }
#if NETCOREAPP
                // Create the new string
                return string.Create(length, values, (span, strings) => {
                    var offset = 0;
                    // Skip null and empty values
                    for (var i = 0; i < strings.Length; i++)
                    {
                        var value = strings[i];
                        if (value != null && value.Length > 0)
                        {
                            if (offset > 0)
                            {
                                // Add seperator
                                span[offset] = ',';
                                offset++;
                            }

                            value.AsSpan().CopyTo(span.Slice(offset));
                            offset += value.Length;
                        }
                    }
                });
#else
#pragma warning disable CS0618
                var sb = new InplaceStringBuilder(length);
#pragma warning enable CS0618
                var hasAdded = false;
                // Skip null and empty values
                for (var i = 0; i < values.Length; i++)
                {
                    var value = values[i];
                    if (value != null && value.Length > 0)
                    {
                        if (hasAdded)
                        {
                            // Add seperator
                            sb.Append(',');
                        }

                        sb.Append(value);
                        hasAdded = true;
                    }
                }

                return sb.ToString();
#endif
            }
        }

        public string[] ToArray()
        {
            return GetArrayValue() ?? Array.Empty<string>();
        }

        private string[] GetArrayValue()
        {
            // Take local copy of _values so type checks remain valid even if the StringValues is overwritten in memory
            var value = _values;
            if (value is string[] values)
            {
                return values;
            }
            else if (value != null)
            {
                // value not array, can only be string
                return new[] { Unsafe.As<string>(value) };
            }
            else
            {
                return null;
            }
        }

        int IList<string>.IndexOf(string item)
        {
            return IndexOf(item);
        }

        private int IndexOf(string item)
        {
            // Take local copy of _values so type checks remain valid even if the StringValues is overwritten in memory
            var value = _values;
            if (value is string[] values)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    if (string.Equals(values[i], item, StringComparison.Ordinal))
                    {
                        return i;
                    }
                }
                return -1;
            }

            if (value != null)
            {
                // value not array, can only be string
                return string.Equals(Unsafe.As<string>(value), item, StringComparison.Ordinal) ? 0 : -1;
            }

            return -1;
        }

        bool ICollection<string>.Contains(string item)
        {
            return IndexOf(item) >= 0;
        }

        void ICollection<string>.CopyTo(string[] array, int arrayIndex)
        {
            CopyTo(array, arrayIndex);
        }

        private void CopyTo(string[] array, int arrayIndex)
        {
            // Take local copy of _values so type checks remain valid even if the StringValues is overwritten in memory
            var value = _values;
            if (value is string[] values)
            {
                Array.Copy(values, 0, array, arrayIndex, values.Length);
                return;
            }

            if (value != null)
            {
                if (array == null)
                {
                    throw new ArgumentNullException(nameof(array));
                }
                if (arrayIndex < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(arrayIndex));
                }
                if (array.Length - arrayIndex < 1)
                {
                    throw new ArgumentException(
                        $"'{nameof(array)}' is not long enough to copy all the items in the collection. Check '{nameof(arrayIndex)}' and '{nameof(array)}' length.");
                }

                // value not array, can only be string
                array[arrayIndex] = Unsafe.As<string>(value);
            }
        }

        void ICollection<string>.Add(string item)
        {
            throw new NotSupportedException();
        }

        void IList<string>.Insert(int index, string item)
        {
            throw new NotSupportedException();
        }

        bool ICollection<string>.Remove(string item)
        {
            throw new NotSupportedException();
        }

        void IList<string>.RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        void ICollection<string>.Clear()
        {
            throw new NotSupportedException();
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_values);
        }

        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static bool IsNullOrEmpty(StringValues value)
        {
            var data = value._values;
            if (data is null)
            {
                return true;
            }
            if (data is string[] values)
            {
                switch (values.Length)
                {
                    case 0: return true;
                    case 1: return string.IsNullOrEmpty(values[0]);
                    default: return false;
                }
            }
            else
            {
                // Not array, can only be string
                return string.IsNullOrEmpty(Unsafe.As<string>(data));
            }
        }

        public static StringValues Concat(StringValues values1, StringValues values2)
        {
            var count1 = values1.Count;
            var count2 = values2.Count;

            if (count1 == 0)
            {
                return values2;
            }

            if (count2 == 0)
            {
                return values1;
            }

            var combined = new string[count1 + count2];
            values1.CopyTo(combined, 0);
            values2.CopyTo(combined, count1);
            return new StringValues(combined);
        }

        public static StringValues Concat(in StringValues values, string value)
        {
            if (value == null)
            {
                return values;
            }

            var count = values.Count;
            if (count == 0)
            {
                return new StringValues(value);
            }

            var combined = new string[count + 1];
            values.CopyTo(combined, 0);
            combined[count] = value;
            return new StringValues(combined);
        }

        public static StringValues Concat(string value, in StringValues values)
        {
            if (value == null)
            {
                return values;
            }

            var count = values.Count;
            if (count == 0)
            {
                return new StringValues(value);
            }

            var combined = new string[count + 1];
            combined[0] = value;
            values.CopyTo(combined, 1);
            return new StringValues(combined);
        }

        public static bool Equals(StringValues left, StringValues right)
        {
            var count = left.Count;

            if (count != right.Count)
            {
                return false;
            }

            for (var i = 0; i < count; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }

            return true;
        }

        public static bool operator ==(StringValues left, StringValues right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(StringValues left, StringValues right)
        {
            return !Equals(left, right);
        }

        public bool Equals(StringValues other)
        {
            return Equals(this, other);
        }

        public static bool Equals(string left, StringValues right)
        {
            return Equals(new StringValues(left), right);
        }

        public static bool Equals(StringValues left, string right)
        {
            return Equals(left, new StringValues(right));
        }

        public bool Equals(string other)
        {
            return Equals(this, new StringValues(other));
        }

        public static bool Equals(string[] left, StringValues right)
        {
            return Equals(new StringValues(left), right);
        }

        public static bool Equals(StringValues left, string[] right)
        {
            return Equals(left, new StringValues(right));
        }

        public bool Equals(string[] other)
        {
            return Equals(this, new StringValues(other));
        }

        public static bool operator ==(StringValues left, string right)
        {
            return Equals(left, new StringValues(right));
        }

        public static bool operator !=(StringValues left, string right)
        {
            return !Equals(left, new StringValues(right));
        }

        public static bool operator ==(string left, StringValues right)
        {
            return Equals(new StringValues(left), right);
        }

        public static bool operator !=(string left, StringValues right)
        {
            return !Equals(new StringValues(left), right);
        }

        public static bool operator ==(StringValues left, string[] right)
        {
            return Equals(left, new StringValues(right));
        }

        public static bool operator !=(StringValues left, string[] right)
        {
            return !Equals(left, new StringValues(right));
        }

        public static bool operator ==(string[] left, StringValues right)
        {
            return Equals(new StringValues(left), right);
        }

        public static bool operator !=(string[] left, StringValues right)
        {
            return !Equals(new StringValues(left), right);
        }

        public static bool operator ==(StringValues left, object right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StringValues left, object right)
        {
            return !left.Equals(right);
        }
        public static bool operator ==(object left, StringValues right)
        {
            return right.Equals(left);
        }

        public static bool operator !=(object left, StringValues right)
        {
            return !right.Equals(left);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return Equals(this, StringValues.Empty);
            }

            if (obj is string)
            {
                return Equals(this, (string)obj);
            }
            
            if (obj is string[])
            {
                return Equals(this, (string[])obj);
            }

            if (obj is StringValues)
            {
                return Equals(this, (StringValues)obj);
            }

            return false;
        }

        public override int GetHashCode()
        {
            var value = _values;
            if (value is string[] values)
            {
                var hcc = new HashCodeCombiner();
                for (var i = 0; i < values.Length; i++)
                {
                    hcc.Add(values[i]);
                }
                return hcc.CombinedHash;
            }
            else
            {
                return Unsafe.As<string>(value)?.GetHashCode() ?? 0;
            }
        }

        public struct Enumerator : IEnumerator<string>
        {
            private readonly string[] _values;
            private string _current;
            private int _index;

            internal Enumerator(object value)
            {
                if (value is string str)
                {
                    _values = null;
                    _current = str;
                }
                else
                {
                    _current = null;
                    _values = Unsafe.As<string[]>(value);
                }
               _index = 0;
            }

            public Enumerator(ref StringValues values) : this(values._values)
            { }

            public bool MoveNext()
            {
                var index = _index;
                if (index < 0)
                {
                    return false;
                }

                var values = _values;
                if (values != null)
                {
                    if ((uint)index < (uint)values.Length)
                    {
                        _index = index + 1;
                        _current = values[index];
                        return true;
                    }

                    _index = -1;
                    return false;
                }

                _index = -1; // sentinel value
                return _current != null;
            }

            public string Current => _current;

            object IEnumerator.Current => _current;

            void IEnumerator.Reset()
            {
                throw new NotSupportedException();
            }

            public void Dispose()
            {
            }
        }
    }
}
