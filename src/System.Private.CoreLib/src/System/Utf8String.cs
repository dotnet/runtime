// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using Internal.Runtime.CompilerServices;

namespace System
{
    /// <summary>
    /// Represents an immutable string of UTF-8 code units.
    /// </summary>
    public sealed partial class Utf8String :
#nullable disable // see comment on String
        IEquatable<Utf8String>
#nullable restore
    {
        /*
         * STATIC FIELDS
         */

        public static readonly Utf8String Empty = FastAllocate(0);

        /*
         * INSTANCE FIELDS
         * Do not reorder these fields. They must match the layout of Utf8StringObject in object.h.
         */

        private readonly int _length;
        private readonly byte _firstByte;

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
        /// Projects a <see cref="Utf8String"/> instance as a <see cref="ReadOnlySpan{Byte}"/>.
        /// </summary>
        public static explicit operator ReadOnlySpan<byte>(Utf8String? value) => value.AsBytes();

        /// <summary>
        /// Projects a <see cref="Utf8String"/> instance as a <see cref="ReadOnlySpan{Char8}"/>.
        /// </summary>
        public static implicit operator ReadOnlySpan<Char8>(Utf8String? value) => value.AsSpan();

        /*
         * INSTANCE PROPERTIES
         */

        /// <summary>
        /// Returns the length (in UTF-8 code units) of this instance.
        /// </summary>
        public int Length => _length;

        /*
         * INSTANCE INDEXERS
         */

        /// <summary>
        /// Gets the <see cref="Char8"/> at the specified position.
        /// </summary>
        public Char8 this[int index]
        {
            get
            {
                // Just like String, we don't allow indexing into the null terminator itself.

                if ((uint)index >= (uint)Length)
                {
                    ThrowHelper.ThrowArgumentOutOfRange_IndexException();
                }

                return Unsafe.Add(ref DangerousGetMutableReference(), index);
            }
        }

        /*
         * METHODS
         */

        /// <summary>
        /// Returns a <em>mutable</em> reference to the first byte of this <see cref="Utf8String"/>
        /// (or the null terminator if the string is empty).
        /// </summary>
        /// <returns></returns>
        internal ref byte DangerousGetMutableReference() => ref Unsafe.AsRef(in _firstByte);

        /// <summary>
        /// Returns a <em>mutable</em> reference to the element at index <paramref name="index"/>
        /// of this <see cref="Utf8String"/> instance. The index is not bounds-checked.
        /// </summary>
        internal ref byte DangerousGetMutableReference(int index)
        {
            // Allow retrieving references to the null terminator.
            Debug.Assert((uint)index <= (uint)Length, "Caller should've performed bounds checking.");

            return ref Unsafe.Add(ref DangerousGetMutableReference(), index);
        }

        /// <summary>
        /// Performs an equality comparison using a <see cref="StringComparison.Ordinal"/> comparer.
        /// </summary>
        public override bool Equals(object? obj)
        {
            return obj is Utf8String other && this.Equals(other);
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
                && SpanHelpers.SequenceEqual(ref this.DangerousGetMutableReference(), ref value.DangerousGetMutableReference(), (uint)Length);
        }

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
                && SpanHelpers.SequenceEqual(ref left.DangerousGetMutableReference(), ref right.DangerousGetMutableReference(), (uint)left.Length);
        }

        /// <summary>
        /// Returns a hash code using a <see cref="StringComparison.Ordinal"/> comparison.
        /// </summary>
        public override int GetHashCode()
        {
            // TODO_UTF8STRING: Consider whether this should use a different seed than String.GetHashCode.

            ulong seed = Marvin.DefaultSeed;
            return Marvin.ComputeHash32(ref DangerousGetMutableReference(), (uint)_length /* in bytes */, (uint)seed, (uint)(seed >> 32));
        }

        /// <summary>
        /// Gets an immutable reference that can be used in a <see langword="fixed"/> statement. The resulting
        /// reference can be pinned and used as a null-terminated <em>LPCUTF8STR</em>.
        /// </summary>
        /// <remarks>
        /// If this <see cref="Utf8String"/> instance is empty, returns a reference to the null terminator.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)] // for compiler use only
        public ref readonly byte GetPinnableReference() => ref _firstByte;

        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="value"/> is <see langword="null"/> or zero length;
        /// <see langword="false"/> otherwise.
        /// </summary>
        public static bool IsNullOrEmpty([NotNullWhen(false)] Utf8String? value)
        {
            // Copied from String.IsNullOrEmpty. See that method for detailed comments on why this pattern is used.
            return (value is null || 0u >= (uint)value.Length) ? true : false;
        }

        /// <summary>
        /// Returns the entire <see cref="Utf8String"/> as an array of bytes.
        /// </summary>
        public byte[] ToByteArray()
        {
            if (Length == 0)
            {
                return Array.Empty<byte>();
            }

            byte[] bytes = new byte[Length];
            Buffer.Memmove(ref bytes.GetRawSzArrayData(), ref DangerousGetMutableReference(), (uint)Length);
            return bytes;
        }

        /// <summary>
        /// Returns a substring of this <see cref="Utf8String"/> as an array of bytes.
        /// </summary>
        public byte[] ToByteArray(int startIndex, int length)
        {
            ValidateStartIndexAndLength(startIndex, length);

            if (length == 0)
            {
                return Array.Empty<byte>();
            }

            byte[] bytes = new byte[length];
            Buffer.Memmove(ref bytes.GetRawSzArrayData(), ref DangerousGetMutableReference(startIndex), (uint)length);
            return bytes;
        }

        /// <summary>
        /// Converts this <see cref="Utf8String"/> instance to a <see cref="string"/>.
        /// </summary>
        /// <remarks>
        /// Invalid subsequences are replaced with U+FFFD during conversion.
        /// </remarks>
        public override string ToString()
        {
            // TODO_UTF8STRING: Call into optimized transcoding routine when it's available.

            return Encoding.UTF8.GetString(new ReadOnlySpan<byte>(ref DangerousGetMutableReference(), Length));
        }
    }
}
