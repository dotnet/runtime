// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.Text.Unicode;

namespace System.Text
{
    public readonly ref partial struct Utf8Span
    {
        /// <summary>
        /// Attempts to locate the target <paramref name="value"/> within this <see cref="Utf8Span"/> instance.
        /// If <paramref name="value"/> is found, returns <see langword="true"/> and sets <paramref name="range"/> to
        /// the location where <paramref name="value"/> occurs within this <see cref="Utf8Span"/> instance.
        /// If <paramref name="value"/> is not found, returns <see langword="false"/> and sets <paramref name="range"/>
        /// to <see langword="default"/>.
        /// </summary>
        /// <remarks>
        /// An ordinal search is performed.
        /// </remarks>
        public bool TryFind(char value, out Range range)
        {
            if (Rune.TryCreate(value, out Rune rune))
            {
                return TryFind(rune, out range);
            }
            else
            {
                // Surrogate chars can't exist in well-formed UTF-8 data - bail immediately.

                range = default;
                return false;
            }
        }

        /// <summary>
        /// Attempts to locate the target <paramref name="value"/> within this <see cref="Utf8Span"/> instance.
        /// If <paramref name="value"/> is found, returns <see langword="true"/> and sets <paramref name="range"/> to
        /// the location where <paramref name="value"/> occurs within this <see cref="Utf8Span"/> instance.
        /// If <paramref name="value"/> is not found, returns <see langword="false"/> and sets <paramref name="range"/>
        /// to <see langword="default"/>.
        /// </summary>
        /// <remarks>
        /// The search is performed using the specified <paramref name="comparisonType"/>.
        /// </remarks>
        public bool TryFind(char value, StringComparison comparisonType, out Range range)
        {
            if (Rune.TryCreate(value, out Rune rune))
            {
                return TryFind(rune, comparisonType, out range);
            }
            else
            {
                string.CheckStringComparison(comparisonType);

                // Surrogate chars can't exist in well-formed UTF-8 data - bail immediately.

                range = default;
                return false;
            }
        }

        /// <summary>
        /// Attempts to locate the target <paramref name="value"/> within this <see cref="Utf8Span"/> instance.
        /// If <paramref name="value"/> is found, returns <see langword="true"/> and sets <paramref name="range"/> to
        /// the location where <paramref name="value"/> occurs within this <see cref="Utf8Span"/> instance.
        /// If <paramref name="value"/> is not found, returns <see langword="false"/> and sets <paramref name="range"/>
        /// to <see langword="default"/>.
        /// </summary>
        /// <remarks>
        /// An ordinal search is performed.
        /// </remarks>
        public bool TryFind(Rune value, out Range range)
        {
            if (value.IsAscii)
            {
                // Special-case ASCII since it's a simple single byte search.

                int idx = Bytes.IndexOf((byte)value.Value);
                if (idx < 0)
                {
                    range = default;
                    return false;
                }
                else
                {
                    range = idx..(idx + 1);
                    return true;
                }
            }
            else
            {
                // Slower path: need to search a multi-byte sequence.
                // TODO_UTF8STRING: As an optimization, we could use unsafe APIs below since we
                // know Rune instances are well-formed and slicing is safe.

                Span<byte> runeBytes = stackalloc byte[Utf8Utility.MaxBytesPerScalar];
                int utf8ByteLengthOfRune = value.EncodeToUtf8(runeBytes);

                return TryFind(UnsafeCreateWithoutValidation(runeBytes.Slice(0, utf8ByteLengthOfRune)), out range);
            }
        }

        /// <summary>
        /// Attempts to locate the target <paramref name="value"/> within this <see cref="Utf8Span"/> instance.
        /// If <paramref name="value"/> is found, returns <see langword="true"/> and sets <paramref name="range"/> to
        /// the location where <paramref name="value"/> occurs within this <see cref="Utf8Span"/> instance.
        /// If <paramref name="value"/> is not found, returns <see langword="false"/> and sets <paramref name="range"/>
        /// to <see langword="default"/>.
        /// </summary>
        /// <remarks>
        /// The search is performed using the specified <paramref name="comparisonType"/>.
        /// </remarks>
        public bool TryFind(Rune value, StringComparison comparisonType, out Range range)
        {
            if (comparisonType == StringComparison.Ordinal)
            {
                return TryFind(value, out range);
            }
            else
            {
                // Slower path: not an ordinal comparison.
                // TODO_UTF8STRING: As an optimization, we could use unsafe APIs below since we
                // know Rune instances are well-formed and slicing is safe.

                Span<byte> runeBytes = stackalloc byte[Utf8Utility.MaxBytesPerScalar];
                int utf8ByteLengthOfRune = value.EncodeToUtf8(runeBytes);

                return TryFind(UnsafeCreateWithoutValidation(runeBytes.Slice(0, utf8ByteLengthOfRune)), comparisonType, out range);
            }
        }

        /// <summary>
        /// Attempts to locate the target <paramref name="value"/> within this <see cref="Utf8Span"/> instance.
        /// If <paramref name="value"/> is found, returns <see langword="true"/> and sets <paramref name="range"/> to
        /// the location where <paramref name="value"/> occurs within this <see cref="Utf8Span"/> instance.
        /// If <paramref name="value"/> is not found, returns <see langword="false"/> and sets <paramref name="range"/>
        /// to <see langword="default"/>.
        /// </summary>
        /// <remarks>
        /// An ordinal search is performed.
        /// </remarks>
        public bool TryFind(Utf8Span value, out Range range)
        {
            int idx;

            if (value.Bytes.Length == 1)
            {
                // Special-case ASCII since it's a simple single byte search.

                idx = this.Bytes.IndexOf(value.Bytes[0]);
            }
            else
            {
                // Slower path: need to search a multi-byte sequence.

                idx = this.Bytes.IndexOf(value.Bytes);
            }

            if (idx < 0)
            {
                range = default;
                return false;
            }
            else
            {
                range = idx..(idx + value.Bytes.Length);
                return true;
            }
        }

        /// <summary>
        /// Attempts to locate the target <paramref name="value"/> within this <see cref="Utf8Span"/> instance.
        /// If <paramref name="value"/> is found, returns <see langword="true"/> and sets <paramref name="range"/> to
        /// the location where <paramref name="value"/> occurs within this <see cref="Utf8Span"/> instance.
        /// If <paramref name="value"/> is not found, returns <see langword="false"/> and sets <paramref name="range"/>
        /// to <see langword="default"/>.
        /// </summary>
        /// <remarks>
        /// The search is performed using the specified <paramref name="comparisonType"/>.
        /// </remarks>
        public bool TryFind(Utf8Span value, StringComparison comparisonType, out Range range) => TryFind(value, comparisonType, out range, fromBeginning: true);

        private unsafe bool TryFind(Utf8Span value, StringComparison comparisonType, out Range range, bool fromBeginning)
        {
            string.CheckStringComparison(comparisonType);

            if (value.IsEmpty)
            {
                // sourceString.IndexOf/LastIndexOf(term, comparer) should return the minimum/maximum value index
                // for which the expression "sourceString.Substring(index).StartsWith(term, comparer)" is true.
                // The range we return to the caller should reflect this so that they can pull out the correct index.

                if (fromBeginning)
                {
                    range = Index.Start..Index.Start;
                }
                else
                {
                    range = Index.End..Index.End;
                }
                return true;
            }

            if (this.IsEmpty)
            {
                range = default;
                return false;
            }

            CompareInfo compareInfo = default!; // will be overwritten if it matters
            CompareOptions compareOptions = string.GetCaseCompareOfComparisonCulture(comparisonType);

            if (GlobalizationMode.Invariant)
            {
                // In the Invariant globalization mode, all comparisons are normalized to Ordinal or OrdinalIgnoreCase,
                // and even in "ignore case" we only map [a-z] <-> [A-Z]. All other code points remain unmapped.

                // TODO_UTF8STRING: We should take advantage of the property described above to avoid the UTF-16
                // transcoding step entirely.

                if (compareOptions != CompareOptions.None)
                {
                    return (fromBeginning)
                        ? TryFind(value, out range)
                        : TryFindLast(value, out range); // call the ordinal search routine
                }
            }
            else
            {
                switch (comparisonType)
                {
                    case StringComparison.Ordinal:
                        return (fromBeginning)
                          ? TryFind(value, out range)
                          : TryFindLast(value, out range);

                    case StringComparison.OrdinalIgnoreCase:
                        // TODO_UTF8STRING: Can probably optimize this case.
                        compareInfo = CompareInfo.Invariant;
                        break;

                    case StringComparison.CurrentCulture:
                    case StringComparison.CurrentCultureIgnoreCase:
                        compareInfo = CultureInfo.CurrentCulture.CompareInfo;
                        break;

                    default:
                        Debug.Assert(comparisonType == StringComparison.InvariantCulture || comparisonType == StringComparison.InvariantCultureIgnoreCase);
                        compareInfo = CompareInfo.Invariant;
                        break;
                }
            }

            // TODO_UTF8STRING: Remove allocations below, and try to avoid the transcoding step if possible.

            string thisTranscodedToUtf16 = this.ToStringNoReplacement();
            string otherTranscodedToUtf16 = value.ToStringNoReplacement();

            int idx, matchLength;

            if (GlobalizationMode.Invariant)
            {
                // If we got here, it meant we're doing an OrdinalIgnoreCase comparison.

                Debug.Assert(compareOptions == CompareOptions.IgnoreCase);

                idx = CompareInfo.InvariantIndexOf(thisTranscodedToUtf16, otherTranscodedToUtf16, ignoreCase: true, fromBeginning);
                matchLength = otherTranscodedToUtf16.Length; // If there was a match, it involved only simple case folding.
            }
            else
            {
                idx = compareInfo.IndexOf(thisTranscodedToUtf16, otherTranscodedToUtf16, 0, thisTranscodedToUtf16.Length, compareOptions, &matchLength, fromBeginning);
            }

            if (idx < 0)
            {
                // No match found. Bail out now.

                range = default;
                return false;
            }

            // If we reached this point, we found a match. The 'idx' local is the index in the source
            // string (indexed by UTF-16 code units) where the match was found, and the 'matchLength'
            // local is the number of chars in the source string which constitute the match. This length
            // can be different than the length of the search string, as non-ordinal IndexOf operations
            // follow Unicode full case folding semantics and might also normalize characters like
            // digraphs.

            fixed (char* pThisTranscodedToUtf16 = &thisTranscodedToUtf16.GetRawStringData())
            {
                // First, we need to convert the UTF-16 'idx' to its UTF-8 equivalent.

                char* pStoppedCounting = Utf16Utility.GetPointerToFirstInvalidChar(pThisTranscodedToUtf16, idx, out long utf8CodeUnitCountAdjustment, out _);
                Debug.Assert(pStoppedCounting == pThisTranscodedToUtf16 + idx, "We shouldn't have generated an ill-formed UTF-16 temp string.");
                Debug.Assert((ulong)(idx + utf8CodeUnitCountAdjustment) <= (uint)this.Bytes.Length, "Start index should be within the source UTF-8 data.");

                // Normally when we produce a UTF-8 code unit count from a UTF-16 source we
                // need to perform 64-bit arithmetic so we don't overflow. But in this case
                // we know the true original source was UTF-8, so its length is known already
                // to fit into a signed 32-bit integer. So we'll perform an unchecked cast.

                int utf8StartIdx = idx + (int)utf8CodeUnitCountAdjustment;

                // Now we need to convert the UTF-16 'matchLength' to its UTF-8 equivalent.

                pStoppedCounting = Utf16Utility.GetPointerToFirstInvalidChar(pThisTranscodedToUtf16 + idx, matchLength, out utf8CodeUnitCountAdjustment, out _);
                Debug.Assert(pStoppedCounting == pThisTranscodedToUtf16 + idx + matchLength, "We shouldn't have generated an ill-formed UTF-16 temp string.");
                Debug.Assert((ulong)(utf8StartIdx + matchLength + utf8CodeUnitCountAdjustment) <= (uint)this.Bytes.Length, "End index should be within the source UTF-8 data.");

                int utf8EndIdx = utf8StartIdx + matchLength + (int)utf8CodeUnitCountAdjustment;

                // Some quick sanity checks on the return value before we return.

                Debug.Assert(0 <= utf8StartIdx);
                Debug.Assert(utf8StartIdx <= utf8EndIdx);
                Debug.Assert(utf8EndIdx <= this.Bytes.Length);

                range = utf8StartIdx..utf8EndIdx;
                return true;
            }
        }

        /// <summary>
        /// Attempts to locate the last occurrence of the target <paramref name="value"/> within this <see cref="Utf8Span"/> instance.
        /// If <paramref name="value"/> is found, returns <see langword="true"/> and sets <paramref name="range"/> to
        /// the location where <paramref name="value"/> occurs within this <see cref="Utf8Span"/> instance.
        /// If <paramref name="value"/> is not found, returns <see langword="false"/> and sets <paramref name="range"/>
        /// to <see langword="default"/>.
        /// </summary>
        /// <remarks>
        /// An ordinal search is performed.
        /// </remarks>
        public bool TryFindLast(char value, out Range range)
        {
            if (Rune.TryCreate(value, out Rune rune))
            {
                return TryFindLast(rune, out range);
            }
            else
            {
                // Surrogate chars can't exist in well-formed UTF-8 data - bail immediately.

                range = default;
                return false;
            }
        }

        /// <summary>
        /// Attempts to locate the last occurrence of the target <paramref name="value"/> within this <see cref="Utf8Span"/> instance.
        /// If <paramref name="value"/> is found, returns <see langword="true"/> and sets <paramref name="range"/> to
        /// the location where <paramref name="value"/> occurs within this <see cref="Utf8Span"/> instance.
        /// If <paramref name="value"/> is not found, returns <see langword="false"/> and sets <paramref name="range"/>
        /// to <see langword="default"/>.
        /// </summary>
        /// <remarks>
        /// The search is performed using the specified <paramref name="comparisonType"/>.
        /// </remarks>
        public bool TryFindLast(char value, StringComparison comparisonType, out Range range)
        {
            if (Rune.TryCreate(value, out Rune rune))
            {
                return TryFindLast(rune, comparisonType, out range);
            }
            else
            {
                string.CheckStringComparison(comparisonType);

                // Surrogate chars can't exist in well-formed UTF-8 data - bail immediately.

                range = default;
                return false;
            }
        }

        /// <summary>
        /// Attempts to locate the last occurrence of the target <paramref name="value"/> within this <see cref="Utf8Span"/> instance.
        /// If <paramref name="value"/> is found, returns <see langword="true"/> and sets <paramref name="range"/> to
        /// the location where <paramref name="value"/> occurs within this <see cref="Utf8Span"/> instance.
        /// If <paramref name="value"/> is not found, returns <see langword="false"/> and sets <paramref name="range"/>
        /// to <see langword="default"/>.
        /// </summary>
        /// <remarks>
        /// An ordinal search is performed.
        /// </remarks>
        public bool TryFindLast(Rune value, out Range range)
        {
            if (value.IsAscii)
            {
                // Special-case ASCII since it's a simple single byte search.

                int idx = Bytes.LastIndexOf((byte)value.Value);
                if (idx < 0)
                {
                    range = default;
                    return false;
                }
                else
                {
                    range = idx..(idx + 1);
                    return true;
                }
            }
            else
            {
                // Slower path: need to search a multi-byte sequence.
                // TODO_UTF8STRING: As an optimization, we could use unsafe APIs below since we
                // know Rune instances are well-formed and slicing is safe.

                Span<byte> runeBytes = stackalloc byte[Utf8Utility.MaxBytesPerScalar];
                int utf8ByteLengthOfRune = value.EncodeToUtf8(runeBytes);

                return TryFindLast(UnsafeCreateWithoutValidation(runeBytes.Slice(0, utf8ByteLengthOfRune)), out range);
            }
        }

        /// <summary>
        /// Attempts to locate the last occurrence of the target <paramref name="value"/> within this <see cref="Utf8Span"/> instance.
        /// If <paramref name="value"/> is found, returns <see langword="true"/> and sets <paramref name="range"/> to
        /// the location where <paramref name="value"/> occurs within this <see cref="Utf8Span"/> instance.
        /// If <paramref name="value"/> is not found, returns <see langword="false"/> and sets <paramref name="range"/>
        /// to <see langword="default"/>.
        /// </summary>
        /// <remarks>
        /// The search is performed using the specified <paramref name="comparisonType"/>.
        /// </remarks>
        public bool TryFindLast(Rune value, StringComparison comparisonType, out Range range)
        {
            if (comparisonType == StringComparison.Ordinal)
            {
                return TryFindLast(value, out range);
            }
            else
            {
                // Slower path: not an ordinal comparison.
                // TODO_UTF8STRING: As an optimization, we could use unsafe APIs below since we
                // know Rune instances are well-formed and slicing is safe.

                Span<byte> runeBytes = stackalloc byte[Utf8Utility.MaxBytesPerScalar];
                int utf8ByteLengthOfRune = value.EncodeToUtf8(runeBytes);

                return TryFindLast(UnsafeCreateWithoutValidation(runeBytes.Slice(0, utf8ByteLengthOfRune)), comparisonType, out range);
            }
        }

        /// <summary>
        /// Attempts to locate the last occurrence of the target <paramref name="value"/> within this <see cref="Utf8Span"/> instance.
        /// If <paramref name="value"/> is found, returns <see langword="true"/> and sets <paramref name="range"/> to
        /// the location where <paramref name="value"/> occurs within this <see cref="Utf8Span"/> instance.
        /// If <paramref name="value"/> is not found, returns <see langword="false"/> and sets <paramref name="range"/>
        /// to <see langword="default"/>.
        /// </summary>
        /// <remarks>
        /// An ordinal search is performed.
        /// </remarks>
        public bool TryFindLast(Utf8Span value, out Range range)
        {
            int idx;

            if (value.Bytes.Length <= 1)
            {
                if (value.Bytes.Length == 1)
                {
                    idx = this.Bytes.LastIndexOf(value.Bytes[0]); // special-case ASCII since it's a single byte search
                }
                else
                {
                    idx = this.Length; // the last empty substring always occurs at the end of the buffer
                }
            }
            else
            {
                // Slower path: need to search a multi-byte sequence.

                idx = this.Bytes.LastIndexOf(value.Bytes);
            }

            if (idx < 0)
            {
                range = default;
                return false;
            }
            else
            {
                range = idx..(idx + value.Bytes.Length);
                return true;
            }
        }

        /// <summary>
        /// Attempts to locate the last occurrence of the target <paramref name="value"/> within this <see cref="Utf8Span"/> instance.
        /// If <paramref name="value"/> is found, returns <see langword="true"/> and sets <paramref name="range"/> to
        /// the location where <paramref name="value"/> occurs within this <see cref="Utf8Span"/> instance.
        /// If <paramref name="value"/> is not found, returns <see langword="false"/> and sets <paramref name="range"/>
        /// to <see langword="default"/>.
        /// </summary>
        /// <remarks>
        /// The search is performed using the specified <paramref name="comparisonType"/>.
        /// </remarks>
        public bool TryFindLast(Utf8Span value, StringComparison comparisonType, out Range range) => TryFind(value, comparisonType, out range, fromBeginning: false);
    }
}
