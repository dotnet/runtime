// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Text
{
    /// <summary>
    /// Discriminated union of a string and a char array/offset/count.  Enables looking up
    /// a portion of a char array as a key in a dictionary of string keys without having to
    /// allocate/copy the chars into a new string.  This comes at the expense of an extra
    /// reference field + two Int32s per key in the size of the dictionary's Entry array.
    /// </summary>
    internal readonly struct StringOrCharArray : IEquatable<StringOrCharArray>
    {
        public readonly ReadOnlyMemory<char> Value;

        public StringOrCharArray(ReadOnlyMemory<char> value) =>
            Value = value;

        public StringOrCharArray(char[] value, int start, int length) =>
            Value = value.AsMemory(start, length);

        public static implicit operator StringOrCharArray(string value) =>
            new StringOrCharArray(value.AsMemory());

        public int Length => Value.Length;

        public override bool Equals(object? obj) =>
            obj is StringOrCharArray other && Equals(other);

        public bool Equals(StringOrCharArray other) =>
            Value.Span.SequenceEqual(other.Value.Span);

        public override int GetHashCode()
        {
            // This hash code is a simplified version of some of the code in String,
            // when not using randomized hash codes.  We don't use string's GetHashCode
            // because we need to be able to use the exact same algorithms on a char[].
            // As such, this should not be used anywhere there are concerns around
            // hash-based attacks that would require a better code.

            int hash1 = (5381 << 16) + 5381;
            int hash2 = hash1;
            var span = Value.Span;

            for (int i = 0; i < span.Length; ++i)
            {
                hash1 = unchecked((hash1 << 5) + hash1) ^ span[i];

                if (++i >= span.Length)
                    break;

                hash2 = unchecked((hash2 << 5) + hash2) ^ span[i];
            }

            return unchecked(hash1 + (hash2 * 1566083941));
        }
    }
}
