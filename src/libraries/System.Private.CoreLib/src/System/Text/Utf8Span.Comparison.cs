// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Unicode;

namespace System.Text
{
    public readonly ref partial struct Utf8Span
    {
        public static bool operator ==(Utf8Span left, Utf8Span right) => Equals(left, right);
        public static bool operator !=(Utf8Span left, Utf8Span right) => !Equals(left, right);

        public int CompareTo(Utf8Span other)
        {
            // TODO_UTF8STRING: This is ordinal, but String.CompareTo uses CurrentCulture.
            // Is this acceptable?

            return Utf8StringComparer.Ordinal.Compare(this, other);
        }

        public int CompareTo(Utf8Span other, StringComparison comparison)
        {
            // TODO_UTF8STRING: We can avoid the virtual dispatch by moving the switch into this method.

            return Utf8StringComparer.FromComparison(comparison).Compare(this, other);
        }

        /// <summary>
        /// Returns a value stating whether the current <see cref="Utf8Span"/> instance contains
        /// <paramref name="value"/>. An ordinal comparison is used.
        /// </summary>
        public bool Contains(char value)
        {
            return Rune.TryCreate(value, out Rune rune) && Contains(rune);
        }

        /// <summary>
        /// Returns a value stating whether the current <see cref="Utf8Span"/> instance contains
        /// <paramref name="value"/>. The specified comparison is used.
        /// </summary>
        public bool Contains(char value, StringComparison comparison)
        {
            return Rune.TryCreate(value, out Rune rune) && Contains(rune, comparison);
        }

        /// <summary>
        /// Returns a value stating whether the current <see cref="Utf8Span"/> instance contains
        /// the specified <see cref="Rune"/>. An ordinal comparison is used.
        /// </summary>
        public bool Contains(Rune value)
        {
            // TODO_UTF8STRING: This should be split into two methods:
            // One which operates on a single-byte (ASCII) search value,
            // the other which operates on a multi-byte (non-ASCII) search value.

            Span<byte> runeBytes = stackalloc byte[Utf8Utility.MaxBytesPerScalar];
            int runeBytesWritten = value.EncodeToUtf8(runeBytes);

            return (this.Bytes.IndexOf(runeBytes.Slice(0, runeBytesWritten)) >= 0);
        }

        /// <summary>
        /// Returns a value stating whether the current <see cref="Utf8Span"/> instance contains
        /// the specified <see cref="Rune"/>. The specified comparison is used.
        /// </summary>
        public bool Contains(Rune value, StringComparison comparison)
        {
            // TODO_UTF8STRING: Optimize me to avoid allocations.

            return this.ToString().Contains(value.ToString(), comparison);
        }

        /// <summary>
        /// Returns a value stating whether the current <see cref="Utf8Span"/> instance contains <paramref name="value"/>.
        /// An ordinal comparison is used.
        /// </summary>
        public bool Contains(Utf8Span value)
        {
            return (this.Bytes.IndexOf(value.Bytes) >= 0);
        }

        /// <summary>
        /// Returns a value stating whether the current <see cref="Utf8Span"/> instance contains <paramref name="value"/>.
        /// The specified comparison is used.
        /// </summary>
        public bool Contains(Utf8Span value, StringComparison comparison)
        {
            // TODO_UTF8STRING: Optimize me to avoid allocations.

            return this.ToString().Contains(value.ToString(), comparison);
        }

        /// <summary>
        /// Returns a value stating whether the current <see cref="Utf8Span"/> instance ends with
        /// <paramref name="value"/>. An ordinal comparison is used.
        /// </summary>
        public bool EndsWith(char value)
        {
            return Rune.TryCreate(value, out Rune rune) && EndsWith(rune);
        }

        /// <summary>
        /// Returns a value stating whether the current <see cref="Utf8Span"/> instance ends with
        /// <paramref name="value"/>. The specified comparison is used.
        /// </summary>
        public bool EndsWith(char value, StringComparison comparison)
        {
            return Rune.TryCreate(value, out Rune rune) && EndsWith(rune, comparison);
        }

        /// <summary>
        /// Returns a value stating whether the current <see cref="Utf8Span"/> instance ends with
        /// the specified <see cref="Rune"/>. An ordinal comparison is used.
        /// </summary>
        public bool EndsWith(Rune value)
        {
            // TODO_UTF8STRING: This should be split into two methods:
            // One which operates on a single-byte (ASCII) search value,
            // the other which operates on a multi-byte (non-ASCII) search value.

            Span<byte> runeBytes = stackalloc byte[Utf8Utility.MaxBytesPerScalar];
            int runeBytesWritten = value.EncodeToUtf8(runeBytes);

            return this.Bytes.EndsWith(runeBytes.Slice(0, runeBytesWritten));
        }

        /// <summary>
        /// Returns a value stating whether the current <see cref="Utf8Span"/> instance ends with
        /// the specified <see cref="Rune"/>. The specified comparison is used.
        /// </summary>
        public bool EndsWith(Rune value, StringComparison comparison)
        {
            // TODO_UTF8STRING: Optimize me to avoid allocations.

            return this.ToString().EndsWith(value.ToString(), comparison);
        }

        /// <summary>
        /// Returns a value stating whether the current <see cref="Utf8Span"/> instance ends with <paramref name="value"/>.
        /// An ordinal comparison is used.
        /// </summary>
        public bool EndsWith(Utf8Span value)
        {
            return this.Bytes.EndsWith(value.Bytes);
        }

        /// <summary>
        /// Returns a value stating whether the current <see cref="Utf8Span"/> instance ends with <paramref name="value"/>.
        /// The specified comparison is used.
        /// </summary>
        public bool EndsWith(Utf8Span value, StringComparison comparison)
        {
            // TODO_UTF8STRING: Optimize me to avoid allocations.

            return this.ToString().EndsWith(value.ToString(), comparison);
        }

        /// <summary>
        /// Returns a value stating whether the current <see cref="Utf8Span"/> instance begins with
        /// <paramref name="value"/>. An ordinal comparison is used.
        /// </summary>
        public bool StartsWith(char value)
        {
            return Rune.TryCreate(value, out Rune rune) && StartsWith(rune);
        }

        /// <summary>
        /// Returns a value stating whether the current <see cref="Utf8Span"/> instance begins with
        /// <paramref name="value"/>. The specified comparison is used.
        /// </summary>
        public bool StartsWith(char value, StringComparison comparison)
        {
            return Rune.TryCreate(value, out Rune rune) && StartsWith(rune, comparison);
        }

        /// <summary>
        /// Returns a value stating whether the current <see cref="Utf8Span"/> instance begins with
        /// the specified <see cref="Rune"/>. An ordinal comparison is used.
        /// </summary>
        public bool StartsWith(Rune value)
        {
            // TODO_UTF8STRING: This should be split into two methods:
            // One which operates on a single-byte (ASCII) search value,
            // the other which operates on a multi-byte (non-ASCII) search value.

            Span<byte> runeBytes = stackalloc byte[Utf8Utility.MaxBytesPerScalar];
            int runeBytesWritten = value.EncodeToUtf8(runeBytes);

            return this.Bytes.StartsWith(runeBytes.Slice(0, runeBytesWritten));
        }

        /// <summary>
        /// Returns a value stating whether the current <see cref="Utf8Span"/> instance begins with
        /// the specified <see cref="Rune"/>. The specified comparison is used.
        /// </summary>
        public bool StartsWith(Rune value, StringComparison comparison)
        {
            // TODO_UTF8STRING: Optimize me to avoid allocations.

            return this.ToString().StartsWith(value.ToString(), comparison);
        }

        /// <summary>
        /// Returns a value stating whether the current <see cref="Utf8Span"/> instance begins with <paramref name="value"/>.
        /// An ordinal comparison is used.
        /// </summary>
        public bool StartsWith(Utf8Span value)
        {
            return this.Bytes.StartsWith(value.Bytes);
        }

        /// <summary>
        /// Returns a value stating whether the current <see cref="Utf8Span"/> instance begins with <paramref name="value"/>.
        /// The specified comparison is used.
        /// </summary>
        public bool StartsWith(Utf8Span value, StringComparison comparison)
        {
            // TODO_UTF8STRING: Optimize me to avoid allocations.

            return this.ToString().StartsWith(value.ToString(), comparison);
        }
    }
}
