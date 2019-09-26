// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.Text;

namespace System
{
    public sealed partial class Utf8String
    {
        /*
         * COMPARISON OF UTF-8 AGAINST UTF-16
         */

        /// <summary>
        /// Returns a value stating whether <paramref name="utf8Text"/> and <paramref name="utf16Text"/>
        /// represent the same data. An ordinal comparison is performed scalar-by-scalar.
        /// </summary>
        /// <remarks>
        /// This method returns <see langword="true"/> if both <paramref name="utf8Text"/> and
        /// <paramref name="utf16Text"/> are null, or if both are empty. This method returns <see langword="false"/>
        /// if either input contains an ill-formed subsequence. Otherwise, this method returns <see langword="true"/>
        /// if and only if both arguments decode to the same Unicode scalar value sequence.
        /// </remarks>
        public static bool AreEquivalent(Utf8String? utf8Text, string? utf16Text)
        {
            if (ReferenceEquals(utf8Text, utf16Text))
            {
                return true; // both are null
            }

            if (utf8Text is null || utf16Text is null)
            {
                return false; // null is never equivalent to non-null
            }

            if (utf8Text.Length == 0 && utf16Text.Length == 0)
            {
                return true; // empty is equivalent to empty
            }

            // Short-circuit: are the texts of sufficiently different lengths that
            // they could never be equivalent? This check allows us to skip the
            // normal decoding walk, which is O(n).
            //
            // The maximum length of a 'System.String' is around 1 billion elements,
            // so we can perform the multiplication within an unsigned 32-bit domain.

            Debug.Assert((ulong)utf16Text.Length * MAX_UTF8_BYTES_PER_UTF16_CHAR <= uint.MaxValue, "Did somebody change the max. allowed string length?");

            if (utf8Text.Length < utf16Text.Length
                || ((uint)utf16Text.Length * MAX_UTF8_BYTES_PER_UTF16_CHAR < (uint)utf8Text.Length))
            {
                return false;
            }

            return AreEquivalentOrdinalSkipShortCircuitingChecks(utf8Text.AsBytes(), utf16Text);
        }

        /// <summary>
        /// Returns a value stating whether <paramref name="utf8Text"/> and <paramref name="utf16Text"/>
        /// represent the same data. An ordinal comparison is performed scalar-by-scalar.
        /// </summary>
        /// <remarks>
        /// This method returns <see langword="true"/> if both <paramref name="utf8Text"/> and
        /// <paramref name="utf16Text"/> are empty. This method returns <see langword="false"/>
        /// if either input contains an ill-formed subsequence. Otherwise, this method returns <see langword="true"/>
        /// if and only if both arguments decode to the same Unicode scalar value sequence.
        /// </remarks>
        public static bool AreEquivalent(Utf8Span utf8Text, ReadOnlySpan<char> utf16Text) => AreEquivalent(utf8Text.Bytes, utf16Text);

        /// <summary>
        /// Returns a value stating whether <paramref name="utf8Text"/> and <paramref name="utf16Text"/>
        /// represent the same data. An ordinal comparison is performed scalar-by-scalar.
        /// </summary>
        /// <remarks>
        /// This method returns <see langword="true"/> if both <paramref name="utf8Text"/> and
        /// <paramref name="utf16Text"/> are empty. This method returns <see langword="false"/>
        /// if either input contains an ill-formed subsequence. Otherwise, this method returns <see langword="true"/>
        /// if and only if both arguments decode to the same Unicode scalar value sequence.
        /// </remarks>
        public static bool AreEquivalent(ReadOnlySpan<byte> utf8Text, ReadOnlySpan<char> utf16Text)
        {
            if (utf8Text.Length == 0 && utf16Text.Length == 0)
            {
                // Don't use IsEmpty for this check; JIT can optimize "Length == 0" better
                // for this particular scenario.

                return true;
            }

            // Same check as the (Utf8String, string) overload. The primary difference is that
            // since spans can be up to 2 billion elements in length, we need to perform
            // the multiplication step in the unsigned 64-bit domain to avoid integer overflow.

            if (utf8Text.Length < utf16Text.Length
                || ((ulong)(uint)utf16Text.Length * MAX_UTF8_BYTES_PER_UTF16_CHAR < (uint)utf8Text.Length))
            {
                return false;
            }

            return AreEquivalentOrdinalSkipShortCircuitingChecks(utf8Text, utf16Text);
        }

        private static bool AreEquivalentOrdinalSkipShortCircuitingChecks(ReadOnlySpan<byte> utf8Text, ReadOnlySpan<char> utf16Text)
        {
            while (!utf16Text.IsEmpty)
            {
                // If the next UTF-16 subsequence is malformed or incomplete, or if the next
                // UTF-8 subsequence is malformed or incomplete, or if they don't decode to
                // the exact same Unicode scalar value, fail.
                //
                // The Rune.DecodeFrom* APIs handle empty inputs just fine and return "Incomplete".

                // TODO_UTF8STRING: If we assume Utf8String contains well-formed UTF-8, we could
                // create a version of this method that calls a faster implementation of DecodeFromUtf8.
                // We'd need to be careful not to call that optimized routine if the user passed
                // us a normal ROS<byte> that didn't originate from a Utf8String or similar.

                if (Rune.DecodeFromUtf16(utf16Text, out Rune scalarFromUtf16, out int charsConsumedJustNow) != OperationStatus.Done
                    || Rune.DecodeFromUtf8(utf8Text, out Rune scalarFromUtf8, out int bytesConsumedJustNow) != OperationStatus.Done
                    || scalarFromUtf16 != scalarFromUtf8)
                {
                    return false;
                }

                // TODO_UTF8STRING: As an optimization, we could perform unsafe slices below.

                utf16Text = utf16Text.Slice(charsConsumedJustNow);
                utf8Text = utf8Text.Slice(bytesConsumedJustNow);
            }

            // We decoded the entire UTF-16 input, and so far it has matched the decoded form
            // of the UTF-8 input. Now just make sure we've also decoded the entirety of the
            // UTF-8 data, otherwise the input strings aren't equivalent.

            return utf8Text.IsEmpty;
        }
    }
}
