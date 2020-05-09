// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Unicode;

#if SYSTEM_PRIVATE_CORELIB
using Internal.Runtime.CompilerServices;
#endif

namespace System
{
    /// <summary>
    /// Represents an immutable string of UTF-8 code units.
    /// </summary>
    public sealed partial class Utf8String : IComparable<Utf8String?>, IEquatable<Utf8String?>
    {
        /*
         * STATIC FIELDS
         */

        public static readonly Utf8String Empty = FastAllocate(0);

        /*
         * OPERATORS
         */

        /// <summary>
        /// Compares two <see cref="Utf8String"/> instances for equality using a <see cref="StringComparison.Ordinal"/> comparer.
        /// </summary>
        public static bool operator ==(Utf8String? left, Utf8String? right) => Equals(left, right);

        /// <summary>
        /// Compares two <see cref="Utf8String"/> instances for inequality using a <see cref="StringComparison.Ordinal"/> comparer.
        /// </summary>
        public static bool operator !=(Utf8String? left, Utf8String? right) => !Equals(left, right);

        /// <summary>
        /// Projects a <see cref="Utf8String"/> instance as a <see cref="Utf8Span"/>.
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator Utf8Span(Utf8String? value) => new Utf8Span(value);

        /*
         * INDEXERS
         */

        public Utf8String this[Range range]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                // The two lines immediately below provide no bounds checking.
                // The Substring method we call will both perform a bounds check
                // and check for an improper split across a multi-byte subsequence.

                int startIdx = range.Start.GetOffset(Length);
                int endIdx = range.End.GetOffset(Length);

                return Substring(startIdx, endIdx - startIdx);
            }
        }

        /*
         * METHODS
         */

        /// <summary>
        /// Similar to <see cref="Utf8Extensions.AsSpan(Utf8String)"/>, but skips the null check on the input.
        /// Throws a <see cref="NullReferenceException"/> if the input is null.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Utf8Span AsSpanSkipNullCheck()
        {
            return Utf8Span.UnsafeCreateWithoutValidation(this.AsBytesSkipNullCheck());
        }

        public int CompareTo(Utf8String? other)
        {
            // TODO_UTF8STRING: This is ordinal, but String.CompareTo uses CurrentCulture.
            // Is this acceptable? Should we perhaps just remove the interface?

            return Utf8StringComparer.Ordinal.Compare(this, other);
        }

        public int CompareTo(Utf8String? other, StringComparison comparison)
        {
            // TODO_UTF8STRING: We can avoid the virtual dispatch by moving the switch into this method.

            return Utf8StringComparer.FromComparison(comparison).Compare(this, other);
        }

        /// <summary>
        /// Returns a <em>mutable</em> reference to the element at index <paramref name="index"/>
        /// of this <see cref="Utf8String"/> instance. The index is not bounds-checked.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref byte DangerousGetMutableReference(int index)
        {
            Debug.Assert(index >= 0, "Caller should've performed bounds checking.");
            return ref DangerousGetMutableReference((uint)index);
        }

        /// <summary>
        /// Returns a <em>mutable</em> reference to the element at index <paramref name="index"/>
        /// of this <see cref="Utf8String"/> instance. The index is not bounds-checked.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref byte DangerousGetMutableReference(nuint index)
        {
            // Allow retrieving references to the null terminator.

            Debug.Assert(index <= (uint)Length, "Caller should've performed bounds checking.");
#if SYSTEM_PRIVATE_CORELIB
            return ref Unsafe.AddByteOffset(ref DangerousGetMutableReference(), index);
#else
            return ref Unsafe.AddByteOffset(ref DangerousGetMutableReference(), (nint)index);
#endif
        }

        /// <summary>
        /// Performs an equality comparison using a <see cref="StringComparison.Ordinal"/> comparer.
        /// </summary>
        public override bool Equals(object? obj)
        {
            return (obj is Utf8String other) && this.Equals(other);
        }

        /// <summary>
        /// Performs an equality comparison using a <see cref="StringComparison.Ordinal"/> comparer.
        /// </summary>
        public bool Equals(Utf8String? value)
        {
            // First, a very quick check for referential equality.

            if (ReferenceEquals(this, value))
            {
                return true;
            }

            // Otherwise, perform a simple bitwise equality check.

            return !(value is null)
                && this.Length == value.Length
#if SYSTEM_PRIVATE_CORELIB
                && SpanHelpers.SequenceEqual(ref this.DangerousGetMutableReference(), ref value.DangerousGetMutableReference(), (uint)Length);
#else
                && this.GetSpan().SequenceEqual(value.GetSpan());
#endif
        }

        /// <summary>
        /// Performs an equality comparison using the specified <see cref="StringComparison"/>.
        /// </summary>
        public bool Equals(Utf8String? value, StringComparison comparison) => Equals(this, value, comparison);

        /// <summary>
        /// Compares two <see cref="Utf8String"/> instances using a <see cref="StringComparison.Ordinal"/> comparer.
        /// </summary>
        public static bool Equals(Utf8String? left, Utf8String? right)
        {
            // First, a very quick check for referential equality.

            if (ReferenceEquals(left, right))
            {
                return true;
            }

            // Otherwise, perform a simple bitwise equality check.

            return !(left is null)
                && !(right is null)
                && left.Length == right.Length
#if SYSTEM_PRIVATE_CORELIB
                && SpanHelpers.SequenceEqual(ref left.DangerousGetMutableReference(), ref right.DangerousGetMutableReference(), (uint)left.Length);
#else
                && left.GetSpan().SequenceEqual(right.GetSpan());
#endif
        }

        /// <summary>
        /// Performs an equality comparison using the specified <see cref="StringComparison"/>.
        /// </summary>
        public static bool Equals(Utf8String? a, Utf8String? b, StringComparison comparison)
        {
            // TODO_UTF8STRING: This perf can be improved, including removing
            // the virtual dispatch by putting the switch directly in this method.

            return Utf8StringComparer.FromComparison(comparison).Equals(a, b);
        }

        /// <summary>
        /// Returns a hash code using a <see cref="StringComparison.Ordinal"/> comparison.
        /// </summary>
        public override int GetHashCode()
        {
            // TODO_UTF8STRING: Consider whether this should use a different seed than String.GetHashCode.

            ulong seed = Marvin.DefaultSeed;
#if SYSTEM_PRIVATE_CORELIB
            return Marvin.ComputeHash32(ref DangerousGetMutableReference(), (uint)_length /* in bytes */, (uint)seed, (uint)(seed >> 32));
#else
            return Marvin.ComputeHash32(_bytes, seed);
#endif
        }

        /// <summary>
        /// Returns a hash code using the specified <see cref="StringComparison"/>.
        /// </summary>
        public int GetHashCode(StringComparison comparison)
        {
            // TODO_UTF8STRING: This perf can be improved, including removing
            // the virtual dispatch by putting the switch directly in this method.

            return Utf8StringComparer.FromComparison(comparison).GetHashCode(this);
        }

        /// <summary>
        /// Returns <see langword="true"/> if this UTF-8 text consists of all-ASCII data,
        /// <see langword="false"/> if there is any non-ASCII data within this UTF-8 text.
        /// </summary>
        /// <remarks>
        /// ASCII text is defined as text consisting only of scalar values in the range [ U+0000..U+007F ].
        /// Empty strings are considered to be all-ASCII. The runtime of this method is O(n).
        /// </remarks>
        public bool IsAscii()
        {
            return this.AsSpan().IsAscii();
        }

        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="value"/> is <see langword="null"/> or zero length;
        /// <see langword="false"/> otherwise.
        /// </summary>
        public static bool IsNullOrEmpty([NotNullWhen(false)] Utf8String? value)
        {
            // Copied from String.IsNullOrEmpty. See that method for detailed comments on why this pattern is used.
            return (value is null || 0u >= (uint)value.Length) ? true : false;
        }

        public static bool IsNullOrWhiteSpace([NotNullWhen(false)] Utf8String? value)
        {
            return (value is null) || value.AsSpan().IsEmptyOrWhiteSpace();
        }

        /// <summary>
        /// Returns the entire <see cref="Utf8String"/> as an array of UTF-8 bytes.
        /// </summary>
        public byte[] ToByteArray() => this.AsSpanSkipNullCheck().ToByteArray();

        /// <summary>
        /// Converts this <see cref="Utf8String"/> instance to a <see cref="string"/>.
        /// </summary>
        public override string ToString()
        {
            // TODO_UTF8STRING: Optimize the call below, potentially by avoiding the two-pass.

#if !NETSTANDARD2_0
            return Encoding.UTF8.GetString(this.AsBytesSkipNullCheck());
#else
            if (Length == 0)
            {
                return string.Empty;
            }

            unsafe
            {
                fixed (byte* pBytes = this.AsBytesSkipNullCheck())
                {
                    return Encoding.UTF8.GetString(pBytes, Length);
                }
            }
#endif
        }
    }
}
