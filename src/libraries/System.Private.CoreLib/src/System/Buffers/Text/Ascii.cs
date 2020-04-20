// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Buffers.Text
{
    /// <summary>
    /// Provides APIs for performing text-like operations over <see cref="byte"/> or
    /// <see cref="char"/> buffers which represent ASCII text.
    /// </summary>
    /// <remarks>
    /// For the purposes of this class, "ASCII" means any <see cref="byte"/> or
    /// <see cref="char"/> in the range 0 - 127, inclusive.
    ///
    /// Unless otherwise specified, APIs on this class treat non-ASCII data as opaque.
    /// This means that <see cref="Equals(ReadOnlySpan{char}, ReadOnlySpan{char})"/>
    /// may produce a different result than <see cref="string.Equals(string?, string?, StringComparison)"/>
    /// with <see cref="StringComparison.OrdinalIgnoreCase"/>.
    ///
    /// All APIs on this class are culture-agnostic.
    /// </remarks>
    public static class Ascii
    {
        /*
         * Some of these APIs are wrappers around other existing APIs; e.g.,
         * Equals is a wrapper around MemoryExtensions.SequenceEqual. The reason
         * for this is two-fold. First, it's discoverable to have both case-sensitive
         * and case-insensitive methods side-by-side on this type. Second, it might
         * not be intuitive to the caller as to what the correct behavior should be.
         * For example, Ascii.Equals([ 30 40 50 DD ], [ 30 40 50 EE ]) will return
         * a different answer than "Encoding.ASCII.GetString([ 30 40 50 DD ]) ==
         * Encoding.ASCII.GetString([ 30 40 50 EE ])". The reason for this is that
         * the Encoding classes can perform lossy substitution when they see non-
         * ASCII data, while we generally treat the data as opaque. This generally
         * results in this class having "correct" behavior over alternatives.
         *
         * For APIs which take both a source and a destination buffer, the behavior of
         * the method is undefined if the source and destination buffers overlap,
         * unless the API description specifies otherwise. The behavior of all APIs
         * is undefined if another thread mutates the buffers while these APIs are
         * operating on them.
         */

        /*
         * Equals routines
         *
         * Compares two ASCII buffers for equality, optionally treating [A-Z] and [a-z] as equal.
         * All non-ASCII bytes / chars are compared for pure binary equivalence.
         */

        /// <summary>
        /// Returns a value stating whether the contents of two ASCII text buffers are equal.
        /// </summary>
        /// <param name="left">The first ASCII text buffer to compare.</param>
        /// <param name="right">The second ASCII text buffer to compare.</param>
        /// <returns><see langword="true"/> if the buffers are equivalent; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// The comparison is performed in a case-sensitive fashion. Calling this method is equivalent
        /// to comparing the raw binary contents of the two buffers for equality.
        /// </remarks>
        public static bool Equals(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
            => left.SequenceEqual(right);

        /// <summary>
        /// Returns a value stating whether the contents of two ASCII text buffers are equal.
        /// </summary>
        /// <param name="left">The first ASCII text buffer to compare.</param>
        /// <param name="right">The second ASCII text buffer to compare.</param>
        /// <returns><see langword="true"/> if the buffers are equivalent; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// The comparison is performed in a case-sensitive fashion. Calling this method is equivalent
        /// to comparing the raw binary contents of the two buffers for equality.
        /// </remarks>
        public static bool Equals(ReadOnlySpan<char> left, ReadOnlySpan<char> right)
            => left.SequenceEqual(right);

        /// <summary>
        /// Returns a value stating whether the contents of two ASCII text buffers are equal
        /// using a case-insensitive comparison.
        /// </summary>
        /// <param name="left">The first ASCII text buffer to compare.</param>
        /// <param name="right">The second ASCII text buffer to compare.</param>
        /// <returns><see langword="true"/> if the buffers are equivalent; otherwise, <see langword="false"/>.</returns>
        public static bool EqualsIgnoreCase(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
        {
            if (left.Length != right.Length)
            {
                return false;
            }

            for (int i = 0; i < left.Length; i++)
            {
                byte leftValue = left[i];
                byte rightValue = right[i];

                // The elements must be an exact match, or they must be
                // equivalent when converted to lowercase.

                if ((leftValue != rightValue) && (ToLower(leftValue) != ToLower(rightValue)))
                {
                    return false;
                }
            }

            return true; // no mismatches seen
        }

        /// <summary>
        /// Returns a value stating whether the contents of two ASCII text buffers are equal
        /// using a case-insensitive comparison.
        /// </summary>
        /// <param name="left">The first ASCII text buffer to compare.</param>
        /// <param name="right">The second ASCII text buffer to compare.</param>
        /// <returns><see langword="true"/> if the buffers are equivalent; otherwise, <see langword="false"/>.</returns>
        public static bool EqualsIgnoreCase(ReadOnlySpan<char> left, ReadOnlySpan<char> right)
        {
            if (left.Length != right.Length)
            {
                return false;
            }

            for (int i = 0; i < left.Length; i++)
            {
                char leftValue = left[i];
                char rightValue = right[i];

                // The elements must be an exact match, or they must be
                // equivalent when converted to lowercase.

                if ((leftValue != rightValue) && (ToLower(leftValue) != ToLower(rightValue)))
                {
                    return false;
                }
            }

            return true; // no mismatches seen
        }

        /*
         * Compares an ASCII byte buffer and an ASCII char buffer for equality, optionally treating
         * [A-Z] and [a-z] as equal. Returns false if the ASCII byte buffer contains any non-ASCII
         * data or if the char buffer contains any element in the range [ 0080 .. FFFF ], as we
         * wouldn't know what encoding to use to perform the transcode-then-compare operation.
         */

        /// <summary>
        /// Returns a value stating whether the contents of two ASCII text buffers are equal.
        /// One buffer is provided as 8-bit <see cref="byte"/>s. The other buffer is provided
        /// as 16-bit <see cref="char"/>s.
        /// </summary>
        /// <param name="left">The first ASCII text buffer to compare.</param>
        /// <param name="right">The second ASCII text buffer to compare.</param>
        /// <returns><see langword="true"/> if the buffers are equivalent; otherwise, <see langword="false"/>.</returns>
        public static bool Equals(ReadOnlySpan<byte> left, ReadOnlySpan<char> right)
        {
            if (left.Length != right.Length)
            {
                return false;
            }

            for (int i = 0; i < left.Length; i++)
            {
                byte leftValue = left[i];
                if (!IsAscii(leftValue) || leftValue != right[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns a value stating whether the contents of two ASCII text buffers are equal.
        /// One buffer is provided as 8-bit <see cref="byte"/>s. The other buffer is provided
        /// as 16-bit <see cref="char"/>s.
        /// </summary>
        /// <param name="left">The first ASCII text buffer to compare.</param>
        /// <param name="right">The second ASCII text buffer to compare.</param>
        /// <returns><see langword="true"/> if the buffers are equivalent; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="right"/> is <see langword="null"/>.</exception>
        public static bool Equals(ReadOnlySpan<byte> left, string right)
        {
            if (right is null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            return Equals(left, right.AsSpan());
        }

        /// <summary>
        /// Returns a value stating whether the contents of two ASCII text buffers are equal
        /// using a case-insensitive comparison. One buffer is provided as 8-bit <see cref="byte"/>s.
        /// The other buffer is provided as 16-bit <see cref="char"/>s.
        /// </summary>
        /// <param name="left">The first ASCII text buffer to compare.</param>
        /// <param name="right">The second ASCII text buffer to compare.</param>
        /// <returns><see langword="true"/> if the buffers are equivalent; otherwise, <see langword="false"/>.</returns>
        public static bool EqualsIgnoreCase(ReadOnlySpan<byte> left, ReadOnlySpan<char> right)
        {
            if (left.Length != right.Length)
            {
                return false;
            }

            for (int i = 0; i < left.Length; i++)
            {
                byte leftValue = left[i];
                if (!IsAscii(leftValue) || ToLower(leftValue) != ToLower(right[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns a value stating whether the contents of two ASCII text buffers are equal
        /// using a case-insensitive comparison. One buffer is provided as 8-bit <see cref="byte"/>s.
        /// The other buffer is provided as 16-bit <see cref="char"/>s.
        /// </summary>
        /// <param name="left">The first ASCII text buffer to compare.</param>
        /// <param name="right">The second ASCII text buffer to compare.</param>
        /// <returns><see langword="true"/> if the buffers are equivalent; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="right"/> is <see langword="null"/>.</exception>
        public static bool EqualsIgnoreCase(ReadOnlySpan<byte> left, string right)
        {
            if (right is null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            return EqualsIgnoreCase(left, right.AsSpan());
        }

        /*
         * IndexOf routines
         *
         * Searches for the first (last) occurrence of the target substring within the search space,
         * optionally treating [A-Z] and [a-z] as equal. All non-ASCII bytes are compared for pure
         * binary equivalence. Returns the index of where the first (last) match is found, else returns -1.
         */

        /// <summary>
        /// Given two ASCII text buffers, searches for the first occurrence of where
        /// <paramref name="value"/> appears within <paramref name="text"/>.
        /// </summary>
        /// <param name="text">The ASCII text buffer which will be searched.</param>
        /// <param name="value">The ASCII text which will be sought within <paramref name="text"/>.</param>
        /// <returns>The zero-based index in <paramref name="text"/> where <paramref name="value"/>
        /// first appears; else -1 if <paramref name="value"/> is not found within <paramref name="text"/>.</returns>
        public static int IndexOf(ReadOnlySpan<byte> text, ReadOnlySpan<byte> value)
            => text.IndexOf(value);

        /// <summary>
        /// Given two ASCII text buffers, searches for the first occurrence of where
        /// <paramref name="value"/> appears within <paramref name="text"/> using a
        /// case-insensitive comparison.
        /// </summary>
        /// <param name="text">The ASCII text buffer which will be searched.</param>
        /// <param name="value">The ASCII text which will be sought within <paramref name="text"/>.</param>
        /// <returns>The zero-based index in <paramref name="text"/> where <paramref name="value"/>
        /// first appears; else -1 if <paramref name="value"/> is not found within <paramref name="text"/>.</returns>
        public static int IndexOfIgnoreCase(ReadOnlySpan<byte> text, ReadOnlySpan<byte> value)
        {
            // If 'value' is longer than 'text', 'lastIdx' will go negative.
            // That's acceptable since it'll early exit the loop below.

            int lastIdx = text.Length - value.Length;
            for (int i = 0; i <= lastIdx; i++)
            {
                if (EqualsIgnoreCase(text.Slice(i, value.Length), value))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Given two ASCII text buffers, searches for the last occurrence of where
        /// <paramref name="value"/> appears within <paramref name="text"/>.
        /// </summary>
        /// <param name="text">The ASCII text buffer which will be searched.</param>
        /// <param name="value">The ASCII text which will be sought within <paramref name="text"/>.</param>
        /// <returns>The zero-based index in <paramref name="text"/> where <paramref name="value"/>
        /// last appears; else -1 if <paramref name="value"/> is not found within <paramref name="text"/>.</returns>
        public static int LastIndexOf(ReadOnlySpan<byte> text, ReadOnlySpan<byte> value)
            => text.LastIndexOf(value);

        /// <summary>
        /// Given two ASCII text buffers, searches for the last occurrence of where
        /// <paramref name="value"/> appears within <paramref name="text"/> using a
        /// case-insensitive comparison.
        /// </summary>
        /// <param name="text">The ASCII text buffer which will be searched.</param>
        /// <param name="value">The ASCII text which will be sought within <paramref name="text"/>.</param>
        /// <returns>The zero-based index in <paramref name="text"/> where <paramref name="value"/>
        /// last appears; else -1 if <paramref name="value"/> is not found within <paramref name="text"/>.</returns>
        public static int LastIndexOfIgnoreCase(ReadOnlySpan<byte> text, ReadOnlySpan<byte> value)
        {
            // If 'value' is longer than 'text', 'lastIdx' will go negative.
            // That's acceptable since it'll early exit the loop below.

            int lastIdx = text.Length - value.Length;
            for (int i = lastIdx; i >= 0; i--)
            {
                if (EqualsIgnoreCase(text.Slice(i, value.Length), value))
                {
                    return i;
                }
            }

            return -1;
        }

        /*
         * Searches for the first (last) occurrence of the target substring within the search space,
         * optionally treating [A-Z] and [a-z] as equal. Returns the index of where the first (last) match
         * is found, else returns -1. If the target string contains any non-ASCII chars ([ 0080 .. FFFF ]),
         * the search is assume to have failed, and the method returns -1.
         */

        /// <summary>
        /// Given two ASCII text buffers, searches for the first occurrence of where
        /// <paramref name="value"/> appears within <paramref name="text"/>.
        /// </summary>
        /// <param name="text">The ASCII text buffer which will be searched.</param>
        /// <param name="value">The ASCII text which will be sought within <paramref name="text"/>.</param>
        /// <returns>The zero-based index in <paramref name="text"/> where <paramref name="value"/>
        /// first appears; else -1 if <paramref name="value"/> is not found within <paramref name="text"/>.</returns>
        public static int IndexOf(ReadOnlySpan<byte> text, ReadOnlySpan<char> value)
        {
            // If 'value' is longer than 'text', 'lastIdx' will go negative.
            // That's acceptable since it'll early exit the loop below.

            int lastIdx = text.Length - value.Length;
            for (int i = 0; i <= lastIdx; i++)
            {
                if (Equals(text.Slice(i, value.Length), value))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Given two ASCII text buffers, searches for the first occurrence of where
        /// <paramref name="value"/> appears within <paramref name="text"/>.
        /// </summary>
        /// <param name="text">The ASCII text buffer which will be searched.</param>
        /// <param name="value">The ASCII text which will be sought within <paramref name="text"/>.</param>
        /// <returns>The zero-based index in <paramref name="text"/> where <paramref name="value"/>
        /// first appears; else -1 if <paramref name="value"/> is not found within <paramref name="text"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        public static int IndexOf(ReadOnlySpan<byte> text, string value)
        {
            if (value is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            }

            return IndexOf(text, value.AsSpan());
        }

        /// <summary>
        /// Given two ASCII text buffers, searches for the first occurrence of where
        /// <paramref name="value"/> appears within <paramref name="text"/> using
        /// a case-insensitive comparison.
        /// </summary>
        /// <param name="text">The ASCII text buffer which will be searched.</param>
        /// <param name="value">The ASCII text which will be sought within <paramref name="text"/>.</param>
        /// <returns>The zero-based index in <paramref name="text"/> where <paramref name="value"/>
        /// first appears; else -1 if <paramref name="value"/> is not found within <paramref name="text"/>.</returns>
        public static int IndexOfIgnoreCase(ReadOnlySpan<byte> text, ReadOnlySpan<char> value)
        {
            // If 'value' is longer than 'text', 'lastIdx' will go negative.
            // That's acceptable since it'll early exit the loop below.

            int lastIdx = text.Length - value.Length;
            for (int i = 0; i <= lastIdx; i++)
            {
                if (EqualsIgnoreCase(text.Slice(i, value.Length), value))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Given two ASCII text buffers, searches for the first occurrence of where
        /// <paramref name="value"/> appears within <paramref name="text"/> using
        /// a case-insensitive comparison.
        /// </summary>
        /// <param name="text">The ASCII text buffer which will be searched.</param>
        /// <param name="value">The ASCII text which will be sought within <paramref name="text"/>.</param>
        /// <returns>The zero-based index in <paramref name="text"/> where <paramref name="value"/>
        /// first appears; else -1 if <paramref name="value"/> is not found within <paramref name="text"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        public static int IndexOfIgnoreCase(ReadOnlySpan<byte> text, string value)
        {
            if (value is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            }

            return IndexOfIgnoreCase(text, value.AsSpan());
        }

        /// <summary>
        /// Given two ASCII text buffers, searches for the last occurrence of where
        /// <paramref name="value"/> appears within <paramref name="text"/>.
        /// </summary>
        /// <param name="text">The ASCII text buffer which will be searched.</param>
        /// <param name="value">The ASCII text which will be sought within <paramref name="text"/>.</param>
        /// <returns>The zero-based index in <paramref name="text"/> where <paramref name="value"/>
        /// last appears; else -1 if <paramref name="value"/> is not found within <paramref name="text"/>.</returns>
        public static int LastIndexOf(ReadOnlySpan<byte> text, ReadOnlySpan<char> value)
        {
            // If 'value' is longer than 'text', 'lastIdx' will go negative.
            // That's acceptable since it'll early exit the loop below.

            int lastIdx = text.Length - value.Length;
            for (int i = lastIdx; i >= 0; i--)
            {
                if (Equals(text.Slice(i, value.Length), value))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Given two ASCII text buffers, searches for the last occurrence of where
        /// <paramref name="value"/> appears within <paramref name="text"/>.
        /// </summary>
        /// <param name="text">The ASCII text buffer which will be searched.</param>
        /// <param name="value">The ASCII text which will be sought within <paramref name="text"/>.</param>
        /// <returns>The zero-based index in <paramref name="text"/> where <paramref name="value"/>
        /// last appears; else -1 if <paramref name="value"/> is not found within <paramref name="text"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        public static int LastIndexOf(ReadOnlySpan<byte> text, string value)
        {
            if (value is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            }

            return LastIndexOf(text, value.AsSpan());
        }

        /// <summary>
        /// Given two ASCII text buffers, searches for the last occurrence of where
        /// <paramref name="value"/> appears within <paramref name="text"/>.
        /// </summary>
        /// <param name="text">The ASCII text buffer which will be searched.</param>
        /// <param name="value">The ASCII text which will be sought within <paramref name="text"/>.</param>
        /// <returns>The zero-based index in <paramref name="text"/> where <paramref name="value"/>
        /// last appears; else -1 if <paramref name="value"/> is not found within <paramref name="text"/>.</returns>
        public static int LastIndexOfIgnoreCase(ReadOnlySpan<byte> text, ReadOnlySpan<char> value)
        {
            // If 'value' is longer than 'text', 'lastIdx' will go negative.
            // That's acceptable since it'll early exit the loop below.

            int lastIdx = text.Length - value.Length;
            for (int i = lastIdx; i >= 0; i--)
            {
                if (EqualsIgnoreCase(text.Slice(i, value.Length), value))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Given two ASCII text buffers, searches for the last occurrence of where
        /// <paramref name="value"/> appears within <paramref name="text"/>.
        /// </summary>
        /// <param name="text">The ASCII text buffer which will be searched.</param>
        /// <param name="value">The ASCII text which will be sought within <paramref name="text"/>.</param>
        /// <returns>The zero-based index in <paramref name="text"/> where <paramref name="value"/>
        /// last appears; else -1 if <paramref name="value"/> is not found within <paramref name="text"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        public static int LastIndexOfIgnoreCase(ReadOnlySpan<byte> text, string value)
        {
            if (value is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.value);
            }

            return LastIndexOfIgnoreCase(text, value.AsSpan());
        }

        /*
         * GetIndexOfFirst* and IsAscii routines
         *
         * Given a buffer, returns the index of the first element in the buffer which
         * is a non-ASCII byte, or -1 if the buffer is empty or all-ASCII. The bool-
         * returning method is a convenience shortcut to perform the same check.
         */

        /// <summary>
        /// Finds the first non-ASCII <see cref="byte"/> within the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer whose contents should be examined.</param>
        /// <returns>The zero-based index in <paramref name="buffer"/> where the first non-ASCII
        /// <see cref="byte"/> appears; else -1 if <paramref name="buffer"/> is empty or
        /// contains only ASCII <see cref="byte"/>s.</returns>
        public static unsafe int GetIndexOfFirstNonAsciiByte(ReadOnlySpan<byte> buffer)
        {
            fixed (byte* pBuffer = &MemoryMarshal.GetReference(buffer))
            {
                int result = (int)ASCIIUtility.GetIndexOfFirstNonAsciiByte(pBuffer, (uint)buffer.Length);
                if (result == buffer.Length)
                {
                    result = -1; // no non-ASCII data found
                }
                return result;
            }
        }

        /// <summary>
        /// Finds the first non-ASCII <see cref="char"/> within the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer whose contents should be examined.</param>
        /// <returns>The zero-based index in <paramref name="buffer"/> where the first non-ASCII
        /// <see cref="char"/> appears; else -1 if <paramref name="buffer"/> is empty or
        /// contains only ASCII <see cref="char"/>s.</returns>
        public static unsafe int GetIndexOfFirstNonAsciiChar(ReadOnlySpan<char> buffer)
        {
            fixed (char* pBuffer = &MemoryMarshal.GetReference(buffer))
            {
                int result = (int)ASCIIUtility.GetIndexOfFirstNonAsciiChar(pBuffer, (uint)buffer.Length);
                if (result == buffer.Length)
                {
                    result = -1; // no non-ASCII data found
                }
                return result;
            }
        }

        /// <summary>
        /// Determines whether a buffer contains only ASCII <see cref="byte"/>s.
        /// </summary>
        /// <param name="value">The buffer whose contents should be examined.</param>
        /// <returns><see langword="true"/> if <paramref name="value"/> is empty or
        /// contains only ASCII <see cref="byte"/>s; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// This method is behaviorally equivalent to calling <see cref="GetIndexOfFirstNonAsciiByte"/>
        /// and comparing the return value against -1.
        /// </remarks>
        public static bool IsAscii(ReadOnlySpan<byte> value) => GetIndexOfFirstNonAsciiByte(value) < 0;

        /// <summary>
        /// Determines whether a buffer contains only ASCII <see cref="char"/>s.
        /// </summary>
        /// <param name="value">The buffer whose contents should be examined.</param>
        /// <returns><see langword="true"/> if <paramref name="value"/> is empty or
        /// contains only ASCII <see cref="char"/>s; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// This method is behaviorally equivalent to calling <see cref="GetIndexOfFirstNonAsciiChar"/>
        /// and comparing the return value against -1.
        /// </remarks>
        public static bool IsAscii(ReadOnlySpan<char> value) => GetIndexOfFirstNonAsciiChar(value) < 0;

        /*
         * Returns true iff the provided byte is an ASCII byte; i.e., in the range [ 00 .. 7F ];
         * or if the provided char is in the range [ 0000 .. 007F ].
         */

        /// <summary>
        /// Determines whether the specified <see cref="byte"/> is an ASCII <see cref="byte"/>.
        /// </summary>
        /// <param name="value">The <see cref="byte"/> to examine.</param>
        /// <returns><see langword="true"/> if <paramref name="value"/> is an ASCII <see cref="byte"/>;
        /// otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// An ASCII <see cref="byte"/> is defined as any <see cref="byte"/> in the range
        /// 0x00 through 0x7F, inclusive.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAscii(byte value) => UnicodeUtility.IsAsciiCodePoint(value);

        /// <summary>
        /// Determines whether the specified <see cref="char"/> is an ASCII <see cref="char"/>.
        /// </summary>
        /// <param name="value">The <see cref="char"/> to examine.</param>
        /// <returns><see langword="true"/> if <paramref name="value"/> is an ASCII <see cref="char"/>;
        /// otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// An ASCII <see cref="char"/> is defined as any <see cref="char"/> in the range
        /// 0x0000 through 0x007F, inclusive.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAscii(char value) => UnicodeUtility.IsAsciiCodePoint(value);

        /*
         * ToLower / ToUpper routines
         *
         * Copies source to destination, converting [A-Z] -> [a-z] or vice versa during
         * the copy. All values outside [A-Za-z] - including non-ASCII values - are unchanged
         * during the copy.
         */

        /// <summary>
        /// Copies the contents of <paramref name="source"/> to <paramref name="destination"/>,
        /// converting any uppercase ASCII <see cref="byte"/>s to their lowercase equivalents
        /// during the copy.
        /// </summary>
        /// <param name="source">The source buffer.</param>
        /// <param name="destination">The destination buffer.</param>
        /// <returns>
        /// The number of <see cref="byte"/>s copied to <paramref name="destination"/>.
        /// This will be equivalent to <paramref name="source"/>'s <see cref="Span{Byte}.Length"/>.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="destination"/> is not large enough
        /// to contain the copied data.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> and <paramref name="destination"/> overlap.</exception>
        public static int ToLower(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            if (destination.Length < source.Length)
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }

            if (source.Overlaps(destination))
            {
                throw new InvalidOperationException(SR.InvalidOperation_SpanOverlappedOperation);
            }

            for (int i = 0; i < source.Length; i++)
            {
                destination[i] = ToLower(source[i]);
            }

            return source.Length;
        }

        /// <summary>
        /// Copies the contents of <paramref name="source"/> to <paramref name="destination"/>,
        /// converting any uppercase ASCII <see cref="char"/>s to their lowercase equivalents
        /// during the copy.
        /// </summary>
        /// <param name="source">The source buffer.</param>
        /// <param name="destination">The destination buffer.</param>
        /// <returns>
        /// The number of <see cref="char"/>s copied to <paramref name="destination"/>.
        /// This will be equivalent to <paramref name="source"/>'s <see cref="Span{Char}.Length"/>.
        /// </returns>
        /// <remarks>
        /// If <paramref name="source"/> contains non-ASCII data, use <see cref="MemoryExtensions.ToLowerInvariant"/> instead.
        /// </remarks>
        /// <exception cref="ArgumentException"><paramref name="destination"/> is not large enough
        /// to contain the copied data.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> and <paramref name="destination"/> overlap.</exception>
        public static int ToLower(ReadOnlySpan<char> source, Span<char> destination)
        {
            if (destination.Length < source.Length)
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }

            if (source.Overlaps(destination))
            {
                throw new InvalidOperationException(SR.InvalidOperation_SpanOverlappedOperation);
            }

            for (int i = 0; i < source.Length; i++)
            {
                destination[i] = ToLower(source[i]);
            }

            return source.Length;
        }

        /// <summary>
        /// Copies the contents of <paramref name="source"/> to <paramref name="destination"/>,
        /// converting any lowercase ASCII <see cref="byte"/>s to their uppercase equivalents
        /// during the copy.
        /// </summary>
        /// <param name="source">The source buffer.</param>
        /// <param name="destination">The destination buffer.</param>
        /// <returns>
        /// The number of <see cref="byte"/>s copied to <paramref name="destination"/>.
        /// This will be equivalent to <paramref name="source"/>'s <see cref="Span{Byte}.Length"/>.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="destination"/> is not large enough
        /// to contain the copied data.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> and <paramref name="destination"/> overlap.</exception>
        public static int ToUpper(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            if (destination.Length < source.Length)
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }

            if (source.Overlaps(destination))
            {
                throw new InvalidOperationException(SR.InvalidOperation_SpanOverlappedOperation);
            }

            for (int i = 0; i < source.Length; i++)
            {
                destination[i] = ToUpper(source[i]);
            }

            return source.Length;
        }

        /// <summary>
        /// Copies the contents of <paramref name="source"/> to <paramref name="destination"/>,
        /// converting any lowercase ASCII <see cref="char"/>s to their uppercase equivalents
        /// during the copy.
        /// </summary>
        /// <param name="source">The source buffer.</param>
        /// <param name="destination">The destination buffer.</param>
        /// <returns>
        /// The number of <see cref="char"/>s copied to <paramref name="destination"/>.
        /// This will be equivalent to <paramref name="source"/>'s <see cref="Span{Char}.Length"/>.
        /// </returns>
        /// <remarks>
        /// If <paramref name="source"/> contains non-ASCII data, use <see cref="MemoryExtensions.ToUpperInvariant"/> instead.
        /// </remarks>
        /// <exception cref="ArgumentException"><paramref name="destination"/> is not large enough
        /// to contain the copied data.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="source"/> and <paramref name="destination"/> overlap.</exception>
        public static int ToUpper(ReadOnlySpan<char> source, Span<char> destination)
        {
            if (destination.Length < source.Length)
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }

            if (source.Overlaps(destination))
            {
                throw new InvalidOperationException(SR.InvalidOperation_SpanOverlappedOperation);
            }

            for (int i = 0; i < source.Length; i++)
            {
                destination[i] = ToUpper(source[i]);
            }

            return source.Length;
        }

        /*
         * Performs case conversion ([A-Z] -> [a-z] or vice versa) in-place. All values
         * outside [A-Za-z] - including non-ASCII values - are unchanged.
         */

        /// <summary>
        /// Performs an in-place lowercase ASCII conversion of a buffer's contents.
        /// </summary>
        /// <param name="value">The buffer over which to operate.</param>
        /// <remarks>
        /// The contents of <paramref name="value"/> are modified in-place. Uppercase
        /// ASCII <see cref="byte"/>s are converted to lowercase. Non-ASCII <see cref="byte"/>s
        /// remain unchanged.
        /// </remarks>
        public static void ToLowerInPlace(Span<byte> value)
        {
            foreach (ref byte refElement in value)
            {
                refElement = ToLower(refElement);
            }
        }

        /// <summary>
        /// Performs an in-place lowercase ASCII conversion of a buffer's contents.
        /// </summary>
        /// <param name="value">The buffer over which to operate.</param>
        /// <remarks>
        /// The contents of <paramref name="value"/> are modified in-place. Uppercase
        /// ASCII <see cref="char"/>s are converted to lowercase. Non-ASCII <see cref="char"/>s
        /// remain unchanged.
        /// </remarks>
        public static void ToLowerInPlace(Span<char> value)
        {
            foreach (ref char refElement in value)
            {
                refElement = ToLower(refElement);
            }
        }

        /// <summary>
        /// Performs an in-place uppercase ASCII conversion of a buffer's contents.
        /// </summary>
        /// <param name="value">The buffer over which to operate.</param>
        /// <remarks>
        /// The contents of <paramref name="value"/> are modified in-place. Lowercase
        /// ASCII <see cref="byte"/>s are converted to uppercase. Non-ASCII <see cref="byte"/>s
        /// remain unchanged.
        /// </remarks>
        public static void ToUpperInPlace(Span<byte> value)
        {
            foreach (ref byte refElement in value)
            {
                refElement = ToUpper(refElement);
            }
        }

        /// <summary>
        /// Performs an in-place uppercase ASCII conversion of a buffer's contents.
        /// </summary>
        /// <param name="value">The buffer over which to operate.</param>
        /// <remarks>
        /// The contents of <paramref name="value"/> are modified in-place. Lowercase
        /// ASCII <see cref="char"/>s are converted to uppercase. Non-ASCII <see cref="char"/>s
        /// remain unchanged.
        /// </remarks>
        public static void ToUpperInPlace(Span<char> value)
        {
            foreach (ref char refElement in value)
            {
                refElement = ToUpper(refElement);
            }
        }

        /*
         * Performs case conversion on a single value, converting [A-Z] -> [a-z] or vice versa.
         * All values outside [A-Za-z] - including non-ASCII values - are unchanged.
         */

        /// <summary>
        /// Converts an ASCII <see cref="byte"/> to lowercase.
        /// </summary>
        /// <param name="value">The <see cref="byte"/> to convert.</param>
        /// <returns>The lowercase form of <paramref name="value"/> if <paramref name="value"/>
        /// is an uppercase ASCII <see cref="byte"/>; otherwise, returns <paramref name="value"/> unchanged
        /// if <paramref name="value"/> is already lowercase or is not an ASCII <see cref="byte"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ToLower(byte value)
        {
            if (UnicodeUtility.IsInRangeInclusive(value, 'A', 'Z'))
            {
                value = (byte)(value | 0x20u);
            }
            return value;
        }

        /// <summary>
        /// Converts an ASCII <see cref="char"/> to lowercase.
        /// </summary>
        /// <param name="value">The <see cref="char"/> to convert.</param>
        /// <returns>The lowercase form of <paramref name="value"/> if <paramref name="value"/>
        /// is an uppercase ASCII <see cref="char"/>; otherwise, returns <paramref name="value"/> unchanged
        /// if <paramref name="value"/> is already lowercase or is not an ASCII <see cref="char"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char ToLower(char value)
        {
            if (UnicodeUtility.IsInRangeInclusive(value, 'A', 'Z'))
            {
                value = (char)(byte)(value | 0x20u);
            }
            return value;
        }

        /// <summary>
        /// Converts an ASCII <see cref="byte"/> to uppercase.
        /// </summary>
        /// <param name="value">The <see cref="byte"/> to convert.</param>
        /// <returns>The uppercase form of <paramref name="value"/> if <paramref name="value"/>
        /// is an lowercase ASCII <see cref="byte"/>; otherwise, returns <paramref name="value"/> unchanged
        /// if <paramref name="value"/> is already uppercase or is not an ASCII <see cref="byte"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ToUpper(byte value)
        {
            if (UnicodeUtility.IsInRangeInclusive(value, 'a', 'z'))
            {
                value = (byte)(value & 0x5Fu); // = low 7 bits of ~0x20
            }
            return value;
        }

        /// <summary>
        /// Converts an ASCII <see cref="char"/> to uppercase.
        /// </summary>
        /// <param name="value">The <see cref="char"/> to convert.</param>
        /// <returns>The uppercase form of <paramref name="value"/> if <paramref name="value"/>
        /// is an lowercase ASCII <see cref="char"/>; otherwise, returns <paramref name="value"/> unchanged
        /// if <paramref name="value"/> is already uppercase or is not an ASCII <see cref="char"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char ToUpper(char value)
        {
            if (UnicodeUtility.IsInRangeInclusive(value, 'a', 'z'))
            {
                value = (char)(value & 0x5Fu); // = low 7 bits of ~0x20
            }
            return value;
        }

        /*
         * Hash code calculation routines
         *
         * Returns a hash code for the provided buffer suitable for use in a dictionary or
         * other keyed collection. For the OrdinalIgnoreCase method, the values [A-Z] and [a-z]
         * are treated as equivalent during hash code computation. All non-ASCII values
         * are treated as opaque data. The hash code is randomized but is not guaranteed to
         * implement any particular algorithm, nor is it guaranteed to be a member of the same
         * PRF family as other GetHashCode routines in the framework.
         */

        /// <summary>
        /// Computes a hash code over the contents of an ASCII buffer.
        /// </summary>
        /// <param name="value">The buffer over which to compute the hash code.</param>
        /// <returns>A hash code for the contents of <paramref name="value"/>.</returns>
        /// <remarks>
        /// If two <see cref="byte"/> buffers <em>left</em> and <em>right</em> compare as equal under
        /// <see cref="Equals(ReadOnlySpan{byte}, ReadOnlySpan{byte})"/>, then this method
        /// will return identical hash codes for each buffer. The implementation of this
        /// method is not guaranteed to match the implementation of any other GetHashCode
        /// routine in the framework, including <see cref="GetHashCodeIgnoreCase(ReadOnlySpan{byte})"/>.
        ///
        /// This method is not intended to pair with <see cref="Equals(ReadOnlySpan{byte}, ReadOnlySpan{char})"/>
        /// or other overloads that operate on heterogeneous parameter types.
        ///
        /// The output of this routine is hardened against hash code collision attacks.
        /// </remarks>
        public static int GetHashCode(ReadOnlySpan<byte> value)
            => Marvin.ComputeHash32(value, Marvin.DefaultSeed);

        /// <summary>
        /// Computes a hash code over the contents of an ASCII buffer.
        /// </summary>
        /// <param name="value">The buffer over which to compute the hash code.</param>
        /// <returns>A hash code for the contents of <paramref name="value"/>.</returns>
        /// <remarks>
        /// If two <see cref="char"/> buffers <em>left</em> and <em>right</em> compare as equal under
        /// <see cref="Equals(ReadOnlySpan{char}, ReadOnlySpan{char})"/>, then this method
        /// will return identical hash codes for each buffer. The implementation of this
        /// method is not guaranteed to match the implementation of any other GetHashCode
        /// routine in the framework, including <see cref="GetHashCodeIgnoreCase(ReadOnlySpan{char})"/>
        /// or <see cref="string.GetHashCode"/>.
        ///
        /// This method is not intended to pair with <see cref="Equals(ReadOnlySpan{byte}, ReadOnlySpan{char})"/>
        /// or other overloads that operate on heterogeneous parameter types.
        ///
        /// The output of this routine is hardened against hash code collision attacks.
        /// </remarks>
        public static int GetHashCode(ReadOnlySpan<char> value)
            => string.GetHashCode(value);

        /// <summary>
        /// Computes a hash code over the contents of an ASCII buffer in a case-insensitive fashion.
        /// </summary>
        /// <param name="value">The buffer over which to compute the hash code.</param>
        /// <returns>A hash code for the contents of <paramref name="value"/>.</returns>
        /// <remarks>
        /// If two <see cref="byte"/> buffers <em>left</em> and <em>right</em> compare as equal under
        /// <see cref="EqualsIgnoreCase(ReadOnlySpan{byte}, ReadOnlySpan{byte})"/>, then this method
        /// will return identical hash codes for each buffer. The implementation of this
        /// method is not guaranteed to match the implementation of any other GetHashCode
        /// routine in the framework, including <see cref="GetHashCode(ReadOnlySpan{byte})"/>.
        ///
        /// This method is not intended to pair with <see cref="EqualsIgnoreCase(ReadOnlySpan{byte}, ReadOnlySpan{char})"/>
        /// or other overloads that operate on heterogeneous parameter types.
        ///
        /// The output of this routine is hardened against hash code collision attacks.
        /// </remarks>
        public static int GetHashCodeIgnoreCase(ReadOnlySpan<byte> value)
        {
            byte[] rentedArray = ArrayPool<byte>.Shared.Rent(value.Length);
            int bytesWritten = ToLower(value, rentedArray);
            int hashCode = GetHashCode(rentedArray.AsSpan(0, bytesWritten));
            ArrayPool<byte>.Shared.Return(rentedArray);
            return hashCode;
        }

        /// <summary>
        /// Computes a hash code over the contents of an ASCII buffer in a case-insensitive fashion.
        /// </summary>
        /// <param name="value">The buffer over which to compute the hash code.</param>
        /// <returns>A hash code for the contents of <paramref name="value"/>.</returns>
        /// <remarks>
        /// If two <see cref="char"/> buffers <em>left</em> and <em>right</em> compare as equal under
        /// <see cref="EqualsIgnoreCase(ReadOnlySpan{char}, ReadOnlySpan{char})"/>, then this method
        /// will return identical hash codes for each buffer. The implementation of this
        /// method is not guaranteed to match the implementation of any other GetHashCode
        /// routine in the framework, including <see cref="GetHashCode(ReadOnlySpan{char})"/>
        /// or <see cref="string.GetHashCode(StringComparison)"/> with <see cref="StringComparison.OrdinalIgnoreCase"/>.
        ///
        /// This method is not intended to pair with <see cref="EqualsIgnoreCase(ReadOnlySpan{byte}, ReadOnlySpan{char})"/>
        /// or other overloads that operate on heterogeneous parameter types.
        ///
        /// The output of this routine is hardened against hash code collision attacks.
        /// </remarks>
        public static int GetHashCodeIgnoreCase(ReadOnlySpan<char> value)
        {
            char[] rentedArray = ArrayPool<char>.Shared.Rent(value.Length);
            int bytesWritten = ToLower(value, rentedArray);
            int hashCode = GetHashCode(rentedArray.AsSpan(0, bytesWritten));
            ArrayPool<char>.Shared.Return(rentedArray);
            return hashCode;
        }

        /*
         * Transcoding routines
         *
         * Widens an ASCII buffer to UTF-16 or narrows a UTF-16 buffer to ASCII.
         * Returns OperationStatus.InvalidData if the source buffer contains a non-ASCII byte
         * or a char in the range [ 0080 .. FFFF ].
         */

        /// <summary>
        /// Copies data from an ASCII buffer <paramref name="source"/> to a UTF-16 buffer <paramref name="destination"/>,
        /// widening ASCII <see cref="byte"/>s to UTF-16 <see cref="char"/>s during the copy.
        /// </summary>
        /// <param name="source">The ASCII source buffer.</param>
        /// <param name="destination">The UTF-16 destination buffer.</param>
        /// <param name="bytesConsumed">When this method returns, the number of <see cref="byte"/>s which were processed
        /// from <paramref name="source"/>. This value will always match <paramref name="charsWritten"/>.</param>
        /// <param name="charsWritten">When this method returns, the number of <see cref="char"/>s which were written
        /// to <paramref name="destination"/>. This value will always match <paramref name="bytesConsumed"/>.</param>
        /// <returns>An <see cref="OperationStatus"/> describing the state of the operation.</returns>
        /// <remarks>
        /// This method will not copy non-ASCII data from <paramref name="source"/> to <paramref name="destination"/>.
        /// Instead, if non-ASCII data is encountered in <paramref name="source"/>, this method returns
        /// <see cref="OperationStatus.InvalidData"/> and sets <paramref name="bytesConsumed"/> and <paramref name="charsWritten"/>
        /// to how far the operation progressed before the non-ASCII source data was seen.
        /// </remarks>
        public static unsafe OperationStatus WidenToUtf16(ReadOnlySpan<byte> source, Span<char> destination, out int bytesConsumed, out int charsWritten)
        {
            fixed (byte* pSource = &MemoryMarshal.GetReference(source))
            fixed (char* pDestination = &MemoryMarshal.GetReference(destination))
            {
                OperationStatus result = OperationStatus.Done;

                int numElementsToConvert = source.Length;
                if (numElementsToConvert > destination.Length)
                {
                    numElementsToConvert = destination.Length;
                    result = OperationStatus.DestinationTooSmall;
                }

                int numElementsActuallyConverted = (int)ASCIIUtility.WidenAsciiToUtf16(pSource, pDestination, (uint)numElementsToConvert);
                if (numElementsActuallyConverted < numElementsToConvert)
                {
                    result = OperationStatus.InvalidData;
                }

                bytesConsumed = numElementsActuallyConverted;
                charsWritten = numElementsActuallyConverted;
                return result;
            }
        }

        /// <summary>
        /// Copies data from a UTF-16 buffer <paramref name="source"/> to an ASCII buffer <paramref name="destination"/>,
        /// narrowing UTF-16 <see cref="char"/>s to ASCII <see cref="byte"/>s during the copy.
        /// </summary>
        /// <param name="source">The ASCII source buffer.</param>
        /// <param name="destination">The UTF-16 destination buffer.</param>
        /// <param name="charsConsumed">When this method returns, the number of <see cref="char"/>s which were processed
        /// from <paramref name="source"/>. This value will always match <paramref name="bytesWritten"/>.</param>
        /// <param name="bytesWritten">When this method returns, the number of <see cref="byte"/>s which were written
        /// to <paramref name="destination"/>. This value will always match <paramref name="charsConsumed"/>.</param>
        /// <returns>An <see cref="OperationStatus"/> describing the state of the operation.</returns>
        /// <remarks>
        /// This method will not copy non-ASCII data from <paramref name="source"/> to <paramref name="destination"/>.
        /// Instead, if non-ASCII data is encountered in <paramref name="source"/>, this method returns
        /// <see cref="OperationStatus.InvalidData"/> and sets <paramref name="charsConsumed"/> and <paramref name="bytesWritten"/>
        /// to how far the operation progressed before the non-ASCII source data was seen.
        /// </remarks>
        public static unsafe OperationStatus NarrowFromUtf16(ReadOnlySpan<char> source, Span<byte> destination, out int charsConsumed, out int bytesWritten)
        {
            fixed (char* pSource = &MemoryMarshal.GetReference(source))
            fixed (byte* pDestination = &MemoryMarshal.GetReference(destination))
            {
                OperationStatus result = OperationStatus.Done;

                int numElementsToConvert = source.Length;
                if (numElementsToConvert > destination.Length)
                {
                    numElementsToConvert = destination.Length;
                    result = OperationStatus.DestinationTooSmall;
                }

                int numElementsActuallyConverted = (int)ASCIIUtility.NarrowUtf16ToAscii(pSource, pDestination, (uint)numElementsToConvert);
                if (numElementsActuallyConverted < numElementsToConvert)
                {
                    result = OperationStatus.InvalidData;
                }

                charsConsumed = numElementsActuallyConverted;
                bytesWritten = numElementsActuallyConverted;
                return result;
            }
        }

        /*
         * Trim routines
         *
         * Unlike string.Trim, these APIs only trim ASCII whitespace. So while
         * "\u00A0Hello\u00A0".Trim() would return "Hello", these trim routines
         * won't trim the [ 00A0 ] chars because they're not considered ASCII
         * whitespace.
         */

        // returns true iff the specified char is a whitespace ASCII char
        private static bool IsAsciiWhitespace(uint ch)
        {
            // Per https://www.unicode.org/Public/UCD/latest/ucd/PropList.txt,
            // the only whitespace ASCII chars are [ 0009..000D ] and [ 0020 ].

            return (ch <= 0x20)
                && (ch == 0x20 || UnicodeUtility.IsInRangeInclusive(ch, 0x09, 0x0D));
        }

        /// <summary>
        /// Trims ASCII whitespace characters from both the beginning and the end of the input buffer.
        /// </summary>
        /// <param name="value">The input buffer from which to trim whitespace characters.</param>
        /// <returns>
        /// A <see cref="Range"/> which represents the data within <paramref name="value"/>
        /// which remains after whitespace characters are trimmed.
        /// </returns>
        public static Range Trim(ReadOnlySpan<byte> value) => TrimCore(value, TrimType.Both);

        /// <summary>
        /// Trims ASCII whitespace characters from both the beginning and the end of the input buffer.
        /// </summary>
        /// <param name="value">The input buffer from which to trim whitespace characters.</param>
        /// <returns>
        /// A <see cref="Range"/> which represents the data within <paramref name="value"/>
        /// which remains after whitespace characters are trimmed.
        /// </returns>
        public static Range Trim(ReadOnlySpan<char> value) => TrimCore(value, TrimType.Both);

        /// <summary>
        /// Trims ASCII whitespace characters from the beginning of the input buffer.
        /// </summary>
        /// <param name="value">The input buffer from which to trim whitespace characters.</param>
        /// <returns>
        /// A <see cref="Range"/> which represents the data within <paramref name="value"/>
        /// which remains after whitespace characters are trimmed.
        /// </returns>
        public static Range TrimStart(ReadOnlySpan<byte> value) => TrimCore(value, TrimType.Head);

        /// <summary>
        /// Trims ASCII whitespace characters from the beginning of the input buffer.
        /// </summary>
        /// <param name="value">The input buffer from which to trim whitespace characters.</param>
        /// <returns>
        /// A <see cref="Range"/> which represents the data within <paramref name="value"/>
        /// which remains after whitespace characters are trimmed.
        /// </returns>
        public static Range TrimStart(ReadOnlySpan<char> value) => TrimCore(value, TrimType.Head);

        /// <summary>
        /// Trims ASCII whitespace characters from the end of the input buffer.
        /// </summary>
        /// <param name="value">The input buffer from which to trim whitespace characters.</param>
        /// <returns>
        /// A <see cref="Range"/> which represents the data within <paramref name="value"/>
        /// which remains after whitespace characters are trimmed.
        /// </returns>
        public static Range TrimEnd(ReadOnlySpan<byte> value) => TrimCore(value, TrimType.Tail);

        /// <summary>
        /// Trims ASCII whitespace characters from the end of the input buffer.
        /// </summary>
        /// <param name="value">The input buffer from which to trim whitespace characters.</param>
        /// <returns>
        /// A <see cref="Range"/> which represents the data within <paramref name="value"/>
        /// which remains after whitespace characters are trimmed.
        /// </returns>
        public static Range TrimEnd(ReadOnlySpan<char> value) => TrimCore(value, TrimType.Tail);

        private static Range TrimCore(ReadOnlySpan<byte> value, TrimType trimType)
        {
            int startIdx = 0;
            if ((trimType & TrimType.Head) != 0)
            {
                for (; startIdx < value.Length; startIdx++)
                {
                    if (!IsAsciiWhitespace(value[startIdx]))
                    {
                        break;
                    }
                }
            }

            int endIdx = value.Length - 1;
            if ((trimType & TrimType.Tail) != 0)
            {
                for (; endIdx >= startIdx; endIdx--)
                {
                    if (!IsAsciiWhitespace(value[endIdx]))
                    {
                        break;
                    }
                }
            }

            return (startIdx..(endIdx + 1));
        }

        private static Range TrimCore(ReadOnlySpan<char> value, TrimType trimType)
        {
            int startIdx = 0;
            if ((trimType & TrimType.Head) != 0)
            {
                for (; startIdx < value.Length; startIdx++)
                {
                    if (!IsAsciiWhitespace(value[startIdx]))
                    {
                        break;
                    }
                }
            }

            int endIdx = value.Length - 1;
            if ((trimType & TrimType.Tail) != 0)
            {
                for (; endIdx >= startIdx; endIdx--)
                {
                    if (!IsAsciiWhitespace(value[endIdx]))
                    {
                        break;
                    }
                }
            }

            return (startIdx..(endIdx + 1));
        }
    }
}
