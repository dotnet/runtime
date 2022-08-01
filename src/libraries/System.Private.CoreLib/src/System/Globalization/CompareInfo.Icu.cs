// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Globalization
{
    public partial class CompareInfo
    {
        [NonSerialized]
        private bool _isAsciiEqualityOrdinal;

        private void IcuInitSortHandle(string interopCultureName)
        {
            if (GlobalizationMode.Invariant)
            {
                _isAsciiEqualityOrdinal = true;
            }
            else
            {
                Debug.Assert(!GlobalizationMode.UseNls);
                Debug.Assert(interopCultureName != null);

                // Inline the following condition to avoid potential implementation cycles within globalization
                //
                // _isAsciiEqualityOrdinal = _sortName == "" || _sortName == "en" || _sortName.StartsWith("en-", StringComparison.Ordinal);
                //
                _isAsciiEqualityOrdinal = _sortName.Length == 0 ||
                    (_sortName.Length >= 2 && _sortName[0] == 'e' && _sortName[1] == 'n' && (_sortName.Length == 2 || _sortName[2] == '-'));

                _sortHandle = SortHandleCache.GetCachedSortHandle(interopCultureName);
            }
        }

        private unsafe int IcuCompareString(ReadOnlySpan<char> string1, ReadOnlySpan<char> string2, CompareOptions options)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);
            Debug.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            // GetReference may return nullptr if the input span is defaulted. The native layer handles
            // this appropriately; no workaround is needed on the managed side.

            fixed (char* pString1 = &MemoryMarshal.GetReference(string1))
            fixed (char* pString2 = &MemoryMarshal.GetReference(string2))
            {
                return Interop.Globalization.CompareString(_sortHandle, pString1, string1.Length, pString2, string2.Length, options);
            }
        }

        private unsafe int IcuIndexOfCore(ReadOnlySpan<char> source, ReadOnlySpan<char> target, CompareOptions options, int* matchLengthPtr, bool fromBeginning)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);
            Debug.Assert(target.Length != 0);

            if (_isAsciiEqualityOrdinal && CanUseAsciiOrdinalForOptions(options))
            {
                if ((options & CompareOptions.IgnoreCase) != 0)
                    return IndexOfOrdinalIgnoreCaseHelper(source, target, options, matchLengthPtr, fromBeginning);
                else
                    return IndexOfOrdinalHelper(source, target, options, matchLengthPtr, fromBeginning);
            }
            else
            {
                // GetReference may return nullptr if the input span is defaulted. The native layer handles
                // this appropriately; no workaround is needed on the managed side.

                fixed (char* pSource = &MemoryMarshal.GetReference(source))
                fixed (char* pTarget = &MemoryMarshal.GetReference(target))
                {
                    if (fromBeginning)
                        return Interop.Globalization.IndexOf(_sortHandle, pTarget, target.Length, pSource, source.Length, options, matchLengthPtr);
                    else
                        return Interop.Globalization.LastIndexOf(_sortHandle, pTarget, target.Length, pSource, source.Length, options, matchLengthPtr);
                }
            }
        }

        /// <summary>
        /// Duplicate of IndexOfOrdinalHelper that also handles ignore case. Can't converge both methods
        /// as the JIT wouldn't be able to optimize the ignoreCase path away.
        /// </summary>
        /// <returns></returns>
        private unsafe int IndexOfOrdinalIgnoreCaseHelper(ReadOnlySpan<char> source, ReadOnlySpan<char> target, CompareOptions options, int* matchLengthPtr, bool fromBeginning)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            Debug.Assert(!target.IsEmpty);
            Debug.Assert(_isAsciiEqualityOrdinal && CanUseAsciiOrdinalForOptions(options));

            fixed (char* ap = &MemoryMarshal.GetReference(source))
            fixed (char* bp = &MemoryMarshal.GetReference(target))
            {
                char* a = ap;
                char* b = bp;

                for (int j = 0; j < target.Length; j++)
                {
                    char targetChar = *(b + j);
                    if (targetChar >= 0x80 || HighCharTable[targetChar])
                        goto InteropCall;
                }

                if (target.Length > source.Length)
                {
                    for (int k = 0; k < source.Length; k++)
                    {
                        char targetChar = *(a + k);
                        if (targetChar >= 0x80 || HighCharTable[targetChar])
                            goto InteropCall;
                    }
                    return -1;
                }

                int startIndex, endIndex, jump;
                if (fromBeginning)
                {
                    // Left to right, from zero to last possible index in the source string.
                    // Incrementing by one after each iteration. Stop condition is last possible index plus 1.
                    startIndex = 0;
                    endIndex = source.Length - target.Length + 1;
                    jump = 1;
                }
                else
                {
                    // Right to left, from first possible index in the source string to zero.
                    // Decrementing by one after each iteration. Stop condition is last possible index minus 1.
                    startIndex = source.Length - target.Length;
                    endIndex = -1;
                    jump = -1;
                }

                for (int i = startIndex; i != endIndex; i += jump)
                {
                    int targetIndex = 0;
                    int sourceIndex = i;

                    for (; targetIndex < target.Length; targetIndex++, sourceIndex++)
                    {
                        char valueChar = *(a + sourceIndex);
                        char targetChar = *(b + targetIndex);

                        if (valueChar >= 0x80 || HighCharTable[valueChar])
                            goto InteropCall;

                        if (valueChar == targetChar)
                        {
                            continue;
                        }

                        // uppercase both chars - notice that we need just one compare per char
                        if (char.IsAsciiLetterLower(valueChar))
                            valueChar = (char)(valueChar - 0x20);
                        if (char.IsAsciiLetterLower(targetChar))
                            targetChar = (char)(targetChar - 0x20);

                        if (valueChar == targetChar)
                        {
                            continue;
                        }

                        // The match may be affected by special character. Verify that the following character is regular ASCII.
                        if (sourceIndex < source.Length - 1 && *(a + sourceIndex + 1) >= 0x80)
                            goto InteropCall;
                        goto Next;
                    }

                    // The match may be affected by special character. Verify that the following character is regular ASCII.
                    if (sourceIndex < source.Length && *(a + sourceIndex) >= 0x80)
                        goto InteropCall;
                    if (matchLengthPtr != null)
                        *matchLengthPtr = target.Length;
                    return i;

                Next: ;
                }

                return -1;

            InteropCall:
                if (fromBeginning)
                    return Interop.Globalization.IndexOf(_sortHandle, b, target.Length, a, source.Length, options, matchLengthPtr);
                else
                    return Interop.Globalization.LastIndexOf(_sortHandle, b, target.Length, a, source.Length, options, matchLengthPtr);
            }
        }

        private unsafe int IndexOfOrdinalHelper(ReadOnlySpan<char> source, ReadOnlySpan<char> target, CompareOptions options, int* matchLengthPtr, bool fromBeginning)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            Debug.Assert(!target.IsEmpty);
            Debug.Assert(_isAsciiEqualityOrdinal && CanUseAsciiOrdinalForOptions(options));

            fixed (char* ap = &MemoryMarshal.GetReference(source))
            fixed (char* bp = &MemoryMarshal.GetReference(target))
            {
                char* a = ap;
                char* b = bp;

                for (int j = 0; j < target.Length; j++)
                {
                    char targetChar = *(b + j);
                    if (targetChar >= 0x80 || HighCharTable[targetChar])
                        goto InteropCall;
                }

                if (target.Length > source.Length)
                {
                    for (int k = 0; k < source.Length; k++)
                    {
                        char targetChar = *(a + k);
                        if (targetChar >= 0x80 || HighCharTable[targetChar])
                            goto InteropCall;
                    }
                    return -1;
                }

                int startIndex, endIndex, jump;
                if (fromBeginning)
                {
                    // Left to right, from zero to last possible index in the source string.
                    // Incrementing by one after each iteration. Stop condition is last possible index plus 1.
                    startIndex = 0;
                    endIndex = source.Length - target.Length + 1;
                    jump = 1;
                }
                else
                {
                    // Right to left, from first possible index in the source string to zero.
                    // Decrementing by one after each iteration. Stop condition is last possible index minus 1.
                    startIndex = source.Length - target.Length;
                    endIndex = -1;
                    jump = -1;
                }

                for (int i = startIndex; i != endIndex; i += jump)
                {
                    int targetIndex = 0;
                    int sourceIndex = i;

                    for (; targetIndex < target.Length; targetIndex++, sourceIndex++)
                    {
                        char valueChar = *(a + sourceIndex);
                        char targetChar = *(b + targetIndex);

                        if (valueChar >= 0x80 || HighCharTable[valueChar])
                            goto InteropCall;

                        if (valueChar == targetChar)
                        {
                            continue;
                        }

                        // The match may be affected by special character. Verify that the following character is regular ASCII.
                        if (sourceIndex < source.Length - 1 && *(a + sourceIndex + 1) >= 0x80)
                            goto InteropCall;
                        goto Next;
                    }

                    // The match may be affected by special character. Verify that the following character is regular ASCII.
                    if (sourceIndex < source.Length && *(a + sourceIndex) >= 0x80)
                        goto InteropCall;
                    if (matchLengthPtr != null)
                        *matchLengthPtr = target.Length;
                    return i;

                Next: ;
                }

                return -1;

            InteropCall:
                if (fromBeginning)
                    return Interop.Globalization.IndexOf(_sortHandle, b, target.Length, a, source.Length, options, matchLengthPtr);
                else
                    return Interop.Globalization.LastIndexOf(_sortHandle, b, target.Length, a, source.Length, options, matchLengthPtr);
            }
        }

        // this method sets '*matchLengthPtr' (if not nullptr) only on success
        private unsafe bool IcuStartsWith(ReadOnlySpan<char> source, ReadOnlySpan<char> prefix, CompareOptions options, int* matchLengthPtr)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);

            Debug.Assert(!prefix.IsEmpty);
            Debug.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            if (_isAsciiEqualityOrdinal && CanUseAsciiOrdinalForOptions(options))
            {
                if ((options & CompareOptions.IgnoreCase) != 0)
                    return StartsWithOrdinalIgnoreCaseHelper(source, prefix, options, matchLengthPtr);
                else
                    return StartsWithOrdinalHelper(source, prefix, options, matchLengthPtr);
            }
            else
            {
                fixed (char* pSource = &MemoryMarshal.GetReference(source)) // could be null (or otherwise unable to be dereferenced)
                fixed (char* pPrefix = &MemoryMarshal.GetReference(prefix))
                {
                    return Interop.Globalization.StartsWith(_sortHandle, pPrefix, prefix.Length, pSource, source.Length, options, matchLengthPtr);
                }
            }
        }

        private unsafe bool StartsWithOrdinalIgnoreCaseHelper(ReadOnlySpan<char> source, ReadOnlySpan<char> prefix, CompareOptions options, int* matchLengthPtr)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            Debug.Assert(!prefix.IsEmpty);
            Debug.Assert(_isAsciiEqualityOrdinal && CanUseAsciiOrdinalForOptions(options));

            int length = Math.Min(source.Length, prefix.Length);

            fixed (char* ap = &MemoryMarshal.GetReference(source)) // could be null (or otherwise unable to be dereferenced)
            fixed (char* bp = &MemoryMarshal.GetReference(prefix))
            {
                char* a = ap;
                char* b = bp;

                while (length != 0)
                {
                    int charA = *a;
                    int charB = *b;

                    if (charA >= 0x80 || charB >= 0x80 || HighCharTable[charA] || HighCharTable[charB])
                        goto InteropCall;

                    if (charA == charB)
                    {
                        a++; b++;
                        length--;
                        continue;
                    }

                    // uppercase both chars - notice that we need just one compare per char
                    if ((uint)(charA - 'a') <= (uint)('z' - 'a')) charA -= 0x20;
                    if ((uint)(charB - 'a') <= (uint)('z' - 'a')) charB -= 0x20;

                    if (charA == charB)
                    {
                        a++; b++;
                        length--;
                        continue;
                    }

                    // The match may be affected by special character. Verify that the following character is regular ASCII.
                    if (a < ap + source.Length - 1 && *(a + 1) >= 0x80)
                        goto InteropCall;
                    if (b < bp + prefix.Length - 1 && *(b + 1) >= 0x80)
                        goto InteropCall;
                    return false;
                }

                // The match may be affected by special character. Verify that the following character is regular ASCII.

                if (source.Length < prefix.Length)
                {
                    int charB = *b;

                    if (charB >= 0x80 || HighCharTable[charB])
                        goto InteropCall;
                    return false;
                }

                if (source.Length > prefix.Length)
                {
                    int charA = *a;
                    if (charA >= 0x80  || HighCharTable[charA])
                        goto InteropCall;
                }

                if (matchLengthPtr != null)
                {
                    *matchLengthPtr = prefix.Length; // non-linguistic match doesn't change UTF-16 length
                }
                return true;

            InteropCall:
                return Interop.Globalization.StartsWith(_sortHandle, bp, prefix.Length, ap, source.Length, options, matchLengthPtr);
            }
        }

        private unsafe bool StartsWithOrdinalHelper(ReadOnlySpan<char> source, ReadOnlySpan<char> prefix, CompareOptions options, int* matchLengthPtr)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            Debug.Assert(!prefix.IsEmpty);
            Debug.Assert(_isAsciiEqualityOrdinal && CanUseAsciiOrdinalForOptions(options));

            int length = Math.Min(source.Length, prefix.Length);

            fixed (char* ap = &MemoryMarshal.GetReference(source)) // could be null (or otherwise unable to be dereferenced)
            fixed (char* bp = &MemoryMarshal.GetReference(prefix))
            {
                char* a = ap;
                char* b = bp;

                while (length != 0)
                {
                    int charA = *a;
                    int charB = *b;

                    if (charA >= 0x80 || charB >= 0x80 || HighCharTable[charA] || HighCharTable[charB])
                        goto InteropCall;

                    if (charA == charB)
                    {
                        a++; b++;
                        length--;
                        continue;
                    }

                    // The match may be affected by special character. Verify that the following character is regular ASCII.
                    if (a < ap + source.Length - 1 && *(a + 1) >= 0x80)
                        goto InteropCall;
                    if (b < bp + prefix.Length - 1 && *(b + 1) >= 0x80)
                        goto InteropCall;
                    return false;
                }

                // The match may be affected by special character. Verify that the following character is regular ASCII.

                if (source.Length < prefix.Length)
                {
                    int charB = *b;

                    if (charB >= 0x80 || HighCharTable[charB])
                        goto InteropCall;
                    return false;
                }

                if (source.Length > prefix.Length)
                {
                    int charA = *a;

                    if (charA >= 0x80 || HighCharTable[charA])
                        goto InteropCall;
                }

                if (matchLengthPtr != null)
                {
                    *matchLengthPtr = prefix.Length; // non-linguistic match doesn't change UTF-16 length
                }
                return true;

            InteropCall:
                return Interop.Globalization.StartsWith(_sortHandle, bp, prefix.Length, ap, source.Length, options, matchLengthPtr);
            }
        }

        // this method sets '*matchLengthPtr' (if not nullptr) only on success
        private unsafe bool IcuEndsWith(ReadOnlySpan<char> source, ReadOnlySpan<char> suffix, CompareOptions options, int* matchLengthPtr)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);

            Debug.Assert(!suffix.IsEmpty);
            Debug.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            if (_isAsciiEqualityOrdinal && CanUseAsciiOrdinalForOptions(options))
            {
                if ((options & CompareOptions.IgnoreCase) != 0)
                    return EndsWithOrdinalIgnoreCaseHelper(source, suffix, options, matchLengthPtr);
                else
                    return EndsWithOrdinalHelper(source, suffix, options, matchLengthPtr);
            }
            else
            {
                fixed (char* pSource = &MemoryMarshal.GetReference(source)) // could be null (or otherwise unable to be dereferenced)
                fixed (char* pSuffix = &MemoryMarshal.GetReference(suffix))
                {
                    return Interop.Globalization.EndsWith(_sortHandle, pSuffix, suffix.Length, pSource, source.Length, options, matchLengthPtr);
                }
            }
        }

        private unsafe bool EndsWithOrdinalIgnoreCaseHelper(ReadOnlySpan<char> source, ReadOnlySpan<char> suffix, CompareOptions options, int* matchLengthPtr)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            Debug.Assert(!suffix.IsEmpty);
            Debug.Assert(_isAsciiEqualityOrdinal && CanUseAsciiOrdinalForOptions(options));

            int length = Math.Min(source.Length, suffix.Length);

            fixed (char* ap = &MemoryMarshal.GetReference(source)) // could be null (or otherwise unable to be dereferenced)
            fixed (char* bp = &MemoryMarshal.GetReference(suffix))
            {
                char* a = ap + source.Length - 1;
                char* b = bp + suffix.Length - 1;

                while (length != 0)
                {
                    int charA = *a;
                    int charB = *b;

                    if (charA >= 0x80 || charB >= 0x80 || HighCharTable[charA] || HighCharTable[charB])
                        goto InteropCall;

                    if (charA == charB)
                    {
                        a--; b--;
                        length--;
                        continue;
                    }

                    // uppercase both chars - notice that we need just one compare per char
                    if ((uint)(charA - 'a') <= (uint)('z' - 'a')) charA -= 0x20;
                    if ((uint)(charB - 'a') <= (uint)('z' - 'a')) charB -= 0x20;

                    if (charA == charB)
                    {
                        a--; b--;
                        length--;
                        continue;
                    }

                    // The match may be affected by special character. Verify that the preceding character is regular ASCII.
                    if (a > ap && *(a - 1) >= 0x80)
                        goto InteropCall;
                    if (b > bp && *(b - 1) >= 0x80)
                        goto InteropCall;
                    return false;
                }

                // The match may be affected by special character. Verify that the preceding character is regular ASCII.

                if (source.Length < suffix.Length)
                {
                    int charB = *b;

                    if (charB >= 0x80 || HighCharTable[charB])
                        goto InteropCall;
                    return false;
                }

                if (source.Length > suffix.Length)
                {
                    int charA = *a;

                    if (charA >= 0x80 || HighCharTable[charA])
                        goto InteropCall;
                }

                if (matchLengthPtr != null)
                {
                    *matchLengthPtr = suffix.Length; // non-linguistic match doesn't change UTF-16 length
                }
                return true;

            InteropCall:
                return Interop.Globalization.EndsWith(_sortHandle, bp, suffix.Length, ap, source.Length, options, matchLengthPtr);
            }
        }

        private unsafe bool EndsWithOrdinalHelper(ReadOnlySpan<char> source, ReadOnlySpan<char> suffix, CompareOptions options, int* matchLengthPtr)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            Debug.Assert(!suffix.IsEmpty);
            Debug.Assert(_isAsciiEqualityOrdinal && CanUseAsciiOrdinalForOptions(options));

            int length = Math.Min(source.Length, suffix.Length);

            fixed (char* ap = &MemoryMarshal.GetReference(source)) // could be null (or otherwise unable to be dereferenced)
            fixed (char* bp = &MemoryMarshal.GetReference(suffix))
            {
                char* a = ap + source.Length - 1;
                char* b = bp + suffix.Length - 1;

                while (length != 0)
                {
                    int charA = *a;
                    int charB = *b;

                    if (charA >= 0x80 || charB >= 0x80 || HighCharTable[charA] || HighCharTable[charB])
                        goto InteropCall;

                    if (charA == charB)
                    {
                        a--; b--;
                        length--;
                        continue;
                    }

                    // The match may be affected by special character. Verify that the preceding character is regular ASCII.
                    if (a > ap && *(a - 1) >= 0x80)
                        goto InteropCall;
                    if (b > bp && *(b - 1) >= 0x80)
                        goto InteropCall;
                    return false;
                }

                // The match may be affected by special character. Verify that the preceding character is regular ASCII.

                if (source.Length < suffix.Length)
                {
                    int charB = *b;

                    if (charB >= 0x80 || HighCharTable[charB])
                        goto InteropCall;
                    return false;
                }

                if (source.Length > suffix.Length)
                {
                    int charA = *a;

                    if (charA >= 0x80 || HighCharTable[charA])
                        goto InteropCall;
                }

                if (matchLengthPtr != null)
                {
                    *matchLengthPtr = suffix.Length; // non-linguistic match doesn't change UTF-16 length
                }
                return true;

            InteropCall:
                return Interop.Globalization.EndsWith(_sortHandle, bp, suffix.Length, ap, source.Length, options, matchLengthPtr);
            }
        }

        private unsafe SortKey IcuCreateSortKey(string source, CompareOptions options)
        {
            ArgumentNullException.ThrowIfNull(source);

            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);

            if ((options & ValidCompareMaskOffFlags) != 0)
            {
                throw new ArgumentException(SR.Argument_InvalidFlag, nameof(options));
            }

            byte[] keyData;
            fixed (char* pSource = source)
            {
                int sortKeyLength = Interop.Globalization.GetSortKey(_sortHandle, pSource, source.Length, null, 0, options);
                keyData = new byte[sortKeyLength];

                fixed (byte* pSortKey = keyData)
                {
                    if (Interop.Globalization.GetSortKey(_sortHandle, pSource, source.Length, pSortKey, sortKeyLength, options) != sortKeyLength)
                    {
                        throw new ArgumentException(SR.Arg_ExternalException);
                    }
                }
            }

            return new SortKey(this, source, options, keyData);
        }

        private unsafe int IcuGetSortKey(ReadOnlySpan<char> source, Span<byte> destination, CompareOptions options)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);
            Debug.Assert((options & ValidCompareMaskOffFlags) == 0);

            // It's ok to pass nullptr (for empty buffers) to ICU's sort key routines.

            int actualSortKeyLength;

            fixed (char* pSource = &MemoryMarshal.GetReference(source))
            fixed (byte* pDest = &MemoryMarshal.GetReference(destination))
            {
                actualSortKeyLength = Interop.Globalization.GetSortKey(_sortHandle, pSource, source.Length, pDest, destination.Length, options);
            }

            // The check below also handles errors due to negative values / overflow being returned.

            if ((uint)actualSortKeyLength > (uint)destination.Length)
            {
                if (actualSortKeyLength > destination.Length)
                {
                    ThrowHelper.ThrowArgumentException_DestinationTooShort();
                }
                else
                {
                    throw new ArgumentException(SR.Arg_ExternalException);
                }
            }

            return actualSortKeyLength;
        }

        private unsafe int IcuGetSortKeyLength(ReadOnlySpan<char> source, CompareOptions options)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);
            Debug.Assert((options & ValidCompareMaskOffFlags) == 0);

            // It's ok to pass nullptr (for empty buffers) to ICU's sort key routines.

            fixed (char* pSource = &MemoryMarshal.GetReference(source))
            {
                return Interop.Globalization.GetSortKey(_sortHandle, pSource, source.Length, null, 0, options);
            }
        }

        private static bool IcuIsSortable(ReadOnlySpan<char> text)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);
            Debug.Assert(!text.IsEmpty);

            do
            {
                if (Rune.DecodeFromUtf16(text, out Rune result, out int charsConsumed) != OperationStatus.Done)
                {
                    return false; // found an unpaired surrogate somewhere in the text
                }

                UnicodeCategory category = Rune.GetUnicodeCategory(result);
                if (category == UnicodeCategory.PrivateUse || category == UnicodeCategory.OtherNotAssigned)
                {
                    return false; // can't sort private use or unassigned code points
                }

                text = text.Slice(charsConsumed);
            } while (!text.IsEmpty);

            return true; // saw no unsortable data in the buffer
        }

        private unsafe int IcuGetHashCodeOfString(ReadOnlySpan<char> source, CompareOptions options)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);
            Debug.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            // according to ICU User Guide the performance of ucol_getSortKey is worse when it is called with null output buffer
            // the solution is to try to fill the sort key in a temporary buffer of size equal 4 x string length
            // (The ArrayPool used to have a limit on the length of buffers it would cache; this code was avoiding
            // exceeding that limit to avoid a per-operation allocation, and the performance implications here
            // were not re-evaluated when the limit was lifted.)
            int sortKeyLength = (source.Length > 1024 * 1024 / 4) ? 0 : 4 * source.Length;

            byte[]? borrowedArray = null;
            Span<byte> sortKey = sortKeyLength <= 1024
                ? stackalloc byte[1024]
                : (borrowedArray = ArrayPool<byte>.Shared.Rent(sortKeyLength));

            fixed (char* pSource = &MemoryMarshal.GetNonNullPinnableReference(source))
            {
                fixed (byte* pSortKey = &MemoryMarshal.GetReference(sortKey))
                {
                    sortKeyLength = Interop.Globalization.GetSortKey(_sortHandle, pSource, source.Length, pSortKey, sortKey.Length, options);
                }

                if (sortKeyLength > sortKey.Length) // slow path for big strings
                {
                    if (borrowedArray != null)
                    {
                        ArrayPool<byte>.Shared.Return(borrowedArray);
                    }

                    sortKey = (borrowedArray = ArrayPool<byte>.Shared.Rent(sortKeyLength));

                    fixed (byte* pSortKey = &MemoryMarshal.GetReference(sortKey))
                    {
                        sortKeyLength = Interop.Globalization.GetSortKey(_sortHandle, pSource, source.Length, pSortKey, sortKey.Length, options);
                    }
                }
            }

            if (sortKeyLength == 0 || sortKeyLength > sortKey.Length) // internal error (0) or a bug (2nd call failed) in ucol_getSortKey
            {
                throw new ArgumentException(SR.Arg_ExternalException);
            }

            int hash = Marvin.ComputeHash32(sortKey.Slice(0, sortKeyLength), Marvin.DefaultSeed);

            if (borrowedArray != null)
            {
                ArrayPool<byte>.Shared.Return(borrowedArray);
            }

            return hash;
        }

        private static CompareOptions GetOrdinalCompareOptions(CompareOptions options)
        {
            if ((options & CompareOptions.IgnoreCase) != 0)
            {
                return CompareOptions.OrdinalIgnoreCase;
            }
            else
            {
                return CompareOptions.Ordinal;
            }
        }

        private static bool CanUseAsciiOrdinalForOptions(CompareOptions options)
        {
            // Unlike the other Ignore options, IgnoreSymbols impacts ASCII characters (e.g. ').
            return (options & CompareOptions.IgnoreSymbols) == 0;
        }

        private SortVersion IcuGetSortVersion()
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);

            int sortVersion = Interop.Globalization.GetSortVersion(_sortHandle);
            return new SortVersion(sortVersion, LCID, new Guid(sortVersion, 0, 0, 0, 0, 0, 0,
                                                             (byte) (LCID >> 24),
                                                             (byte) ((LCID  & 0x00FF0000) >> 16),
                                                             (byte) ((LCID  & 0x0000FF00) >> 8),
                                                             (byte) (LCID  & 0xFF)));
        }

        private static class SortHandleCache
        {
            // in most scenarios there is a limited number of cultures with limited number of sort options
            // so caching the sort handles and not freeing them is OK, see https://github.com/dotnet/coreclr/pull/25117 for more
            private static readonly Dictionary<string, IntPtr> s_sortNameToSortHandleCache = new Dictionary<string, IntPtr>();

            internal static IntPtr GetCachedSortHandle(string sortName)
            {
                lock (s_sortNameToSortHandleCache)
                {
                    if (!s_sortNameToSortHandleCache.TryGetValue(sortName, out IntPtr result))
                    {
                        Interop.Globalization.ResultCode resultCode = Interop.Globalization.GetSortHandle(sortName, out result);

                        if (resultCode == Interop.Globalization.ResultCode.OutOfMemory)
                            throw new OutOfMemoryException();
                        else if (resultCode != Interop.Globalization.ResultCode.Success)
                            throw new ExternalException(SR.Arg_ExternalException);

                        try
                        {
                            s_sortNameToSortHandleCache.Add(sortName, result);
                        }
                        catch
                        {
                            Interop.Globalization.CloseSortHandle(result);

                            throw;
                        }
                    }

                    return result;
                }
            }
        }

        private static ReadOnlySpan<bool> HighCharTable => new bool[0x80]
        {
            true, /* 0x0, 0x0 */
            true, /* 0x1, .*/
            true, /* 0x2, .*/
            true, /* 0x3, .*/
            true, /* 0x4, .*/
            true, /* 0x5, .*/
            true, /* 0x6, .*/
            true, /* 0x7, .*/
            true, /* 0x8, .*/
            false, /* 0x9,   */
            true, /* 0xA,  */
            false, /* 0xB, .*/
            false, /* 0xC, .*/
            true, /* 0xD,  */
            true, /* 0xE, .*/
            true, /* 0xF, .*/
            true, /* 0x10, .*/
            true, /* 0x11, .*/
            true, /* 0x12, .*/
            true, /* 0x13, .*/
            true, /* 0x14, .*/
            true, /* 0x15, .*/
            true, /* 0x16, .*/
            true, /* 0x17, .*/
            true, /* 0x18, .*/
            true, /* 0x19, .*/
            true, /* 0x1A, */
            true, /* 0x1B, .*/
            true, /* 0x1C, .*/
            true, /* 0x1D, .*/
            true, /* 0x1E, .*/
            true, /* 0x1F, .*/
            false, /*0x20,  */
            false, /*0x21, !*/
            false, /*0x22, "*/
            false, /*0x23,  #*/
            false, /*0x24,  $*/
            false, /*0x25,  %*/
            false, /*0x26,  &*/
            false,  /*0x27, '*/
            false, /*0x28, (*/
            false, /*0x29, )*/
            false, /*0x2A **/
            false, /*0x2B, +*/
            false, /*0x2C, ,*/
            false,  /*0x2D, -*/
            false, /*0x2E, .*/
            false, /*0x2F, /*/
            false, /*0x30, 0*/
            false, /*0x31, 1*/
            false, /*0x32, 2*/
            false, /*0x33, 3*/
            false, /*0x34, 4*/
            false, /*0x35, 5*/
            false, /*0x36, 6*/
            false, /*0x37, 7*/
            false, /*0x38, 8*/
            false, /*0x39, 9*/
            false, /*0x3A, :*/
            false, /*0x3B, ;*/
            false, /*0x3C, <*/
            false, /*0x3D, =*/
            false, /*0x3E, >*/
            false, /*0x3F, ?*/
            false, /*0x40, @*/
            false, /*0x41, A*/
            false, /*0x42, B*/
            false, /*0x43, C*/
            false, /*0x44, D*/
            false, /*0x45, E*/
            false, /*0x46, F*/
            false, /*0x47, G*/
            false, /*0x48, H*/
            false, /*0x49, I*/
            false, /*0x4A, J*/
            false, /*0x4B, K*/
            false, /*0x4C, L*/
            false, /*0x4D, M*/
            false, /*0x4E, N*/
            false, /*0x4F, O*/
            false, /*0x50, P*/
            false, /*0x51, Q*/
            false, /*0x52, R*/
            false, /*0x53, S*/
            false, /*0x54, T*/
            false, /*0x55, U*/
            false, /*0x56, V*/
            false, /*0x57, W*/
            false, /*0x58, X*/
            false, /*0x59, Y*/
            false, /*0x5A, Z*/
            false, /*0x5B, [*/
            false, /*0x5C, \*/
            false, /*0x5D, ]*/
            false, /*0x5E, ^*/
            false, /*0x5F, _*/
            false, /*0x60, `*/
            false, /*0x61, a*/
            false, /*0x62, b*/
            false, /*0x63, c*/
            false, /*0x64, d*/
            false, /*0x65, e*/
            false, /*0x66, f*/
            false, /*0x67, g*/
            false, /*0x68, h*/
            false, /*0x69, i*/
            false, /*0x6A, j*/
            false, /*0x6B, k*/
            false, /*0x6C, l*/
            false, /*0x6D, m*/
            false, /*0x6E, n*/
            false, /*0x6F, o*/
            false, /*0x70, p*/
            false, /*0x71, q*/
            false, /*0x72, r*/
            false, /*0x73, s*/
            false, /*0x74, t*/
            false, /*0x75, u*/
            false, /*0x76, v*/
            false, /*0x77, w*/
            false, /*0x78, x*/
            false, /*0x79, y*/
            false, /*0x7A, z*/
            false, /*0x7B, {*/
            false, /*0x7C, |*/
            false, /*0x7D, }*/
            false, /*0x7E, ~*/
            true, /*0x7F, */
        };
    }
}
