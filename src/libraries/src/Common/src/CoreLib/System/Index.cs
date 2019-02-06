// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System
{
    public readonly struct Index : IEquatable<Index>
    {
        private readonly int _value;

        public Index(int value, bool fromEnd)
        {
            if (value < 0)
            {
                ThrowHelper.ThrowValueArgumentOutOfRange_NeedNonNegNumException();
            }

            _value = fromEnd ? ~value : value;
        }

        public int Value => _value < 0 ? ~_value : _value;
        public bool FromEnd => _value < 0;
        public override bool Equals(object value) => value is Index && _value == ((Index)value)._value;
        public bool Equals (Index other) => _value == other._value;

        public override int GetHashCode()
        {
            return _value;
        }

        public override string ToString() => FromEnd ? ToStringFromEnd() : ((uint)Value).ToString();

        private string ToStringFromEnd()
        {
            Span<char> span = stackalloc char[11]; // 1 for ^ and 10 for longest possible uint value
            bool formatted = ((uint)Value).TryFormat(span.Slice(1), out int charsWritten);
            Debug.Assert(formatted);
            span[0] = '^';
            return new string(span.Slice(0, charsWritten + 1));
        }

        public static implicit operator Index(int value)
            => new Index(value, fromEnd: false);
    }
}
