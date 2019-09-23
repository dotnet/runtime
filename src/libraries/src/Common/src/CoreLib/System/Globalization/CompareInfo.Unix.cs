// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;

using Internal.Runtime.CompilerServices;

namespace System.Globalization
{
    public partial class CompareInfo
    {
        [NonSerialized]
        private IntPtr _sortHandle;

        [NonSerialized]
        private bool _isAsciiEqualityOrdinal;

        private void InitSort(CultureInfo culture)
        {
            _sortName = culture.SortName;

            if (GlobalizationMode.Invariant)
            {
                _isAsciiEqualityOrdinal = true;
            }
            else
            {
                _isAsciiEqualityOrdinal = (_sortName == "en-US" || _sortName == "");

                _sortHandle = SortHandleCache.GetCachedSortHandle(_sortName);
            }
        }

        internal static unsafe int IndexOfOrdinalCore(string source, string value, int startIndex, int count, bool ignoreCase)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            Debug.Assert(source != null);
            Debug.Assert(value != null);

            if (value.Length == 0)
            {
                return startIndex;
            }

            if (count < value.Length)
            {
                return -1;
            }

            if (ignoreCase)
            {
                fixed (char* pSource = source)
                {
                    int index = Interop.Globalization.IndexOfOrdinalIgnoreCase(value, value.Length, pSource + startIndex, count, findLast: false);
                    return index != -1 ?
                        startIndex + index :
                        -1;
                }
            }

            int endIndex = startIndex + (count - value.Length);
            for (int i = startIndex; i <= endIndex; i++)
            {
                int valueIndex, sourceIndex;

                for (valueIndex = 0, sourceIndex = i;
                     valueIndex < value.Length && source[sourceIndex] == value[valueIndex];
                     valueIndex++, sourceIndex++) ;

                if (valueIndex == value.Length)
                {
                    return i;
                }
            }

            return -1;
        }

        internal static unsafe int IndexOfOrdinalCore(ReadOnlySpan<char> source, ReadOnlySpan<char> value, bool ignoreCase, bool fromBeginning)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            Debug.Assert(source.Length != 0);
            Debug.Assert(value.Length != 0);

            if (source.Length < value.Length)
            {
                return -1;
            }

            if (ignoreCase)
            {
                fixed (char* pSource = &MemoryMarshal.GetReference(source))
                fixed (char* pValue = &MemoryMarshal.GetReference(value))
                {
                    return Interop.Globalization.IndexOfOrdinalIgnoreCase(pValue, value.Length, pSource, source.Length, findLast: !fromBeginning);
                }
            }

            int startIndex, endIndex, jump;
            if (fromBeginning)
            {
                // Left to right, from zero to last possible index in the source string.
                // Incrementing by one after each iteration. Stop condition is last possible index plus 1.
                startIndex = 0;
                endIndex = source.Length - value.Length + 1;
                jump = 1;
            }
            else
            {
                // Right to left, from first possible index in the source string to zero.
                // Decrementing by one after each iteration. Stop condition is last possible index minus 1.
                startIndex = source.Length - value.Length;
                endIndex = -1;
                jump = -1;
            }

            for (int i = startIndex; i != endIndex; i += jump)
            {
                int valueIndex, sourceIndex;

                for (valueIndex = 0, sourceIndex = i;
                     valueIndex < value.Length && source[sourceIndex] == value[valueIndex];
                     valueIndex++, sourceIndex++)
                    ;

                if (valueIndex == value.Length)
                {
                    return i;
                }
            }

            return -1;
        }

        internal static unsafe int LastIndexOfOrdinalCore(string source, string value, int startIndex, int count, bool ignoreCase)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            Debug.Assert(source != null);
            Debug.Assert(value != null);

            if (value.Length == 0)
            {
                return startIndex;
            }

            if (count < value.Length)
            {
                return -1;
            }

            // startIndex is the index into source where we start search backwards from.
            // leftStartIndex is the index into source of the start of the string that is
            // count characters away from startIndex.
            int leftStartIndex = startIndex - count + 1;

            if (ignoreCase)
            {
                fixed (char* pSource = source)
                {
                    int lastIndex = Interop.Globalization.IndexOfOrdinalIgnoreCase(value, value.Length, pSource + leftStartIndex, count, findLast: true);
                    return lastIndex != -1 ?
                        leftStartIndex + lastIndex :
                        -1;
                }
            }

            for (int i = startIndex - value.Length + 1; i >= leftStartIndex; i--)
            {
                int valueIndex, sourceIndex;

                for (valueIndex = 0, sourceIndex = i;
                     valueIndex < value.Length && source[sourceIndex] == value[valueIndex];
                     valueIndex++, sourceIndex++) ;

                if (valueIndex == value.Length) {
                    return i;
                }
            }

            return -1;
        }

        private static unsafe int CompareStringOrdinalIgnoreCase(ref char string1, int count1, ref char string2, int count2)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            fixed (char* char1 = &string1)
            fixed (char* char2 = &string2)
            {
                return Interop.Globalization.CompareStringOrdinalIgnoreCase(char1, count1, char2, count2);
            }
        }

        // TODO https://github.com/dotnet/coreclr/issues/13827:
        // This method shouldn't be necessary, as we should be able to just use the overload
        // that takes two spans.  But due to this issue, that's adding significant overhead.
        private unsafe int CompareString(ReadOnlySpan<char> string1, string string2, CompareOptions options)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(string2 != null);
            Debug.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            fixed (char* pString1 = &MemoryMarshal.GetReference(string1))
            fixed (char* pString2 = &string2.GetRawStringData())
            {
                return Interop.Globalization.CompareString(_sortHandle, pString1, string1.Length, pString2, string2.Length, options);
            }
        }

        private unsafe int CompareString(ReadOnlySpan<char> string1, ReadOnlySpan<char> string2, CompareOptions options)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            fixed (char* pString1 = &MemoryMarshal.GetReference(string1))
            fixed (char* pString2 = &MemoryMarshal.GetReference(string2))
            {
                return Interop.Globalization.CompareString(_sortHandle, pString1, string1.Length, pString2, string2.Length, options);
            }
        }

        internal unsafe int IndexOfCore(string source, string target, int startIndex, int count, CompareOptions options, int* matchLengthPtr)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            Debug.Assert(!string.IsNullOrEmpty(source));
            Debug.Assert(target != null);
            Debug.Assert((options & CompareOptions.OrdinalIgnoreCase) == 0);
            Debug.Assert((options & CompareOptions.Ordinal) == 0);

            int index;

            if (_isAsciiEqualityOrdinal && CanUseAsciiOrdinalForOptions(options))
            {
                if ((options & CompareOptions.IgnoreCase) != 0)
                    index = IndexOfOrdinalIgnoreCaseHelper(source.AsSpan(startIndex, count), target.AsSpan(), options, matchLengthPtr, fromBeginning: true);
                else
                    index = IndexOfOrdinalHelper(source.AsSpan(startIndex, count), target.AsSpan(), options, matchLengthPtr, fromBeginning: true);
            }
            else
            {
                fixed (char* pSource = source)
                fixed (char* pTarget = target)
                {
                    index = Interop.Globalization.IndexOf(_sortHandle, pTarget, target.Length, pSource + startIndex, count, options, matchLengthPtr);
                }
            }

            return index != -1 ? index + startIndex : -1;
        }

        // For now, this method is only called from Span APIs with either options == CompareOptions.None or CompareOptions.IgnoreCase
        internal unsafe int IndexOfCore(ReadOnlySpan<char> source, ReadOnlySpan<char> target, CompareOptions options, int* matchLengthPtr, bool fromBeginning)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(source.Length != 0);
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
                fixed (char* pSource = &MemoryMarshal.GetReference(source))
                fixed (char* pTarget = &MemoryMarshal.GetReference(target))
                {
                    if (fromBeginning)
                        return Interop.Globalization.IndexOf(_sortHandle, pTarget, target.Length, pSource, source.Length, options, matchLengthPtr);
                    else
                        return Interop.Globalization.LastIndexOf(_sortHandle, pTarget, target.Length, pSource, source.Length, options);
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
            Debug.Assert(_isAsciiEqualityOrdinal);

            fixed (char* ap = &MemoryMarshal.GetReference(source))
            fixed (char* bp = &MemoryMarshal.GetReference(target))
            {
                char* a = ap;
                char* b = bp;

                if (target.Length > source.Length)
                    goto InteropCall;

                for (int j = 0; j < target.Length; j++)
                {
                    char targetChar = *(b + j);
                    if (targetChar >= 0x80 || HighCharTable[targetChar])
                        goto InteropCall;
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

                        if (valueChar == targetChar && valueChar < 0x80 && !HighCharTable[valueChar])
                        {
                            continue;
                        }

                        // uppercase both chars - notice that we need just one compare per char
                        if ((uint)(valueChar - 'a') <= ('z' - 'a'))
                            valueChar = (char)(valueChar - 0x20);
                        if ((uint)(targetChar - 'a') <= ('z' - 'a'))
                            targetChar = (char)(targetChar - 0x20);

                        if (valueChar >= 0x80 || HighCharTable[valueChar])
                            goto InteropCall;
                        else if (valueChar != targetChar)
                            break;
                    }

                    if (targetIndex == target.Length)
                    {
                        if (matchLengthPtr != null)
                            *matchLengthPtr = target.Length;
                        return i;
                    }
                }

                return -1;

            InteropCall:
                if (fromBeginning)
                    return Interop.Globalization.IndexOf(_sortHandle, b, target.Length, a, source.Length, options, matchLengthPtr);
                else
                    return Interop.Globalization.LastIndexOf(_sortHandle, b, target.Length, a, source.Length, options);
            }
        }

        private unsafe int IndexOfOrdinalHelper(ReadOnlySpan<char> source, ReadOnlySpan<char> target, CompareOptions options, int* matchLengthPtr, bool fromBeginning)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            Debug.Assert(!target.IsEmpty);
            Debug.Assert(_isAsciiEqualityOrdinal);

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
                        else if (valueChar != targetChar)
                            break;
                    }

                    if (targetIndex == target.Length)
                    {
                        if (matchLengthPtr != null)
                            *matchLengthPtr = target.Length;
                        return i;
                    }
                }

                return -1;

            InteropCall:
                if (fromBeginning)
                    return Interop.Globalization.IndexOf(_sortHandle, b, target.Length, a, source.Length, options, matchLengthPtr);
                else
                    return Interop.Globalization.LastIndexOf(_sortHandle, b, target.Length, a, source.Length, options);
            }
        }

        private unsafe int LastIndexOfCore(string source, string target, int startIndex, int count, CompareOptions options)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            Debug.Assert(!string.IsNullOrEmpty(source));
            Debug.Assert(target != null);
            Debug.Assert((options & CompareOptions.OrdinalIgnoreCase) == 0);

            if (target.Length == 0)
            {
                return startIndex;
            }

            if (options == CompareOptions.Ordinal)
            {
                return LastIndexOfOrdinalCore(source, target, startIndex, count, ignoreCase: false);
            }

            // startIndex is the index into source where we start search backwards from. leftStartIndex is the index into source
            // of the start of the string that is count characters away from startIndex.
            int leftStartIndex = (startIndex - count + 1);

            int lastIndex;

            if (_isAsciiEqualityOrdinal && CanUseAsciiOrdinalForOptions(options))
            {
                if ((options & CompareOptions.IgnoreCase) != 0)
                    lastIndex = IndexOfOrdinalIgnoreCaseHelper(source.AsSpan(leftStartIndex, count), target.AsSpan(), options, matchLengthPtr: null, fromBeginning: false);
                else
                    lastIndex = IndexOfOrdinalHelper(source.AsSpan(leftStartIndex, count), target.AsSpan(), options, matchLengthPtr: null, fromBeginning: false);
            }
            else
            {
                fixed (char* pSource = source)
                fixed (char* pTarget = target)
                {
                    lastIndex = Interop.Globalization.LastIndexOf(_sortHandle, pTarget, target.Length, pSource + (startIndex - count + 1), count, options);
                }
            }

            return lastIndex != -1 ? lastIndex + leftStartIndex : -1;
        }

        private unsafe bool StartsWith(ReadOnlySpan<char> source, ReadOnlySpan<char> prefix, CompareOptions options)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            Debug.Assert(!source.IsEmpty);
            Debug.Assert(!prefix.IsEmpty);
            Debug.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            if (_isAsciiEqualityOrdinal && CanUseAsciiOrdinalForOptions(options))
            {
                if ((options & CompareOptions.IgnoreCase) != 0)
                    return StartsWithOrdinalIgnoreCaseHelper(source, prefix, options);
                else
                    return StartsWithOrdinalHelper(source, prefix, options);
            }
            else
            {
                fixed (char* pSource = &MemoryMarshal.GetReference(source))
                fixed (char* pPrefix = &MemoryMarshal.GetReference(prefix))
                {
                    return Interop.Globalization.StartsWith(_sortHandle, pPrefix, prefix.Length, pSource, source.Length, options);
                }
            }
        }

        private unsafe bool StartsWithOrdinalIgnoreCaseHelper(ReadOnlySpan<char> source, ReadOnlySpan<char> prefix, CompareOptions options)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            Debug.Assert(!source.IsEmpty);
            Debug.Assert(!prefix.IsEmpty);
            Debug.Assert(_isAsciiEqualityOrdinal);

            int length = Math.Min(source.Length, prefix.Length);

            fixed (char* ap = &MemoryMarshal.GetReference(source))
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
                    if (*b >= 0x80)
                        goto InteropCall;
                    return false;
                }

                if (source.Length > prefix.Length)
                {
                    if (*a >= 0x80)
                        goto InteropCall;
                }
                return true;

            InteropCall:
                return Interop.Globalization.StartsWith(_sortHandle, bp, prefix.Length, ap, source.Length, options);
            }
        }

        private unsafe bool StartsWithOrdinalHelper(ReadOnlySpan<char> source, ReadOnlySpan<char> prefix, CompareOptions options)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            Debug.Assert(!source.IsEmpty);
            Debug.Assert(!prefix.IsEmpty);
            Debug.Assert(_isAsciiEqualityOrdinal);

            int length = Math.Min(source.Length, prefix.Length);

            fixed (char* ap = &MemoryMarshal.GetReference(source))
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
                    if (*b >= 0x80)
                        goto InteropCall;
                    return false;
                }

                if (source.Length > prefix.Length)
                {
                    if (*a >= 0x80)
                        goto InteropCall;
                }
                return true;

            InteropCall:
                return Interop.Globalization.StartsWith(_sortHandle, bp, prefix.Length, ap, source.Length, options);
            }
        }

        private unsafe bool EndsWith(ReadOnlySpan<char> source, ReadOnlySpan<char> suffix, CompareOptions options)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            Debug.Assert(!source.IsEmpty);
            Debug.Assert(!suffix.IsEmpty);
            Debug.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            if (_isAsciiEqualityOrdinal && CanUseAsciiOrdinalForOptions(options))
            {
                if ((options & CompareOptions.IgnoreCase) != 0)
                    return EndsWithOrdinalIgnoreCaseHelper(source, suffix, options);
                else
                    return EndsWithOrdinalHelper(source, suffix, options);
            }
            else
            {
                fixed (char* pSource = &MemoryMarshal.GetReference(source))
                fixed (char* pSuffix = &MemoryMarshal.GetReference(suffix))
                {
                    return Interop.Globalization.EndsWith(_sortHandle, pSuffix, suffix.Length, pSource, source.Length, options);
                }
            }
        }

        private unsafe bool EndsWithOrdinalIgnoreCaseHelper(ReadOnlySpan<char> source, ReadOnlySpan<char> suffix, CompareOptions options)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            Debug.Assert(!source.IsEmpty);
            Debug.Assert(!suffix.IsEmpty);
            Debug.Assert(_isAsciiEqualityOrdinal);

            int length = Math.Min(source.Length, suffix.Length);

            fixed (char* ap = &MemoryMarshal.GetReference(source))
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

                    return false;
                }

                return (source.Length >= suffix.Length);

            InteropCall:
                return Interop.Globalization.EndsWith(_sortHandle, bp, suffix.Length, ap, source.Length, options);
            }
        }

        private unsafe bool EndsWithOrdinalHelper(ReadOnlySpan<char> source, ReadOnlySpan<char> suffix, CompareOptions options)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            Debug.Assert(!source.IsEmpty);
            Debug.Assert(!suffix.IsEmpty);
            Debug.Assert(_isAsciiEqualityOrdinal);

            int length = Math.Min(source.Length, suffix.Length);

            fixed (char* ap = &MemoryMarshal.GetReference(source))
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

                    return false;
                }

                return (source.Length >= suffix.Length);

            InteropCall:
                return Interop.Globalization.EndsWith(_sortHandle, bp, suffix.Length, ap, source.Length, options);
            }
        }

        private unsafe SortKey CreateSortKey(string source, CompareOptions options)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            if (source==null) { throw new ArgumentNullException(nameof(source)); }

            if ((options & ValidSortkeyCtorMaskOffFlags) != 0)
            {
                throw new ArgumentException(SR.Argument_InvalidFlag, nameof(options));
            }

            byte [] keyData;
            if (source.Length == 0)
            {
                keyData = Array.Empty<byte>();
            }
            else
            {
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
            }

            return new SortKey(Name, source, options, keyData);
        }

        private static unsafe bool IsSortable(char *text, int length)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            int index = 0;
            UnicodeCategory uc;

            while (index < length)
            {
                if (char.IsHighSurrogate(text[index]))
                {
                    if (index == length - 1 || !char.IsLowSurrogate(text[index+1]))
                        return false; // unpaired surrogate

                    uc = CharUnicodeInfo.GetUnicodeCategory(char.ConvertToUtf32(text[index], text[index+1]));
                    if (uc == UnicodeCategory.PrivateUse || uc == UnicodeCategory.OtherNotAssigned)
                        return false;

                    index += 2;
                    continue;
                }

                if (char.IsLowSurrogate(text[index]))
                {
                    return false; // unpaired surrogate
                }

                uc = CharUnicodeInfo.GetUnicodeCategory(text[index]);
                if (uc == UnicodeCategory.PrivateUse || uc == UnicodeCategory.OtherNotAssigned)
                {
                    return false;
                }

                index++;
            }

            return true;
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        internal unsafe int GetHashCodeOfStringCore(ReadOnlySpan<char> source, CompareOptions options)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert((options & (CompareOptions.Ordinal | CompareOptions.OrdinalIgnoreCase)) == 0);

            if (source.Length == 0)
            {
                return 0;
            }

            // according to ICU User Guide the performance of ucol_getSortKey is worse when it is called with null output buffer
            // the solution is to try to fill the sort key in a temporary buffer of size equal 4 x string length
            // 1MB is the biggest array that can be rented from ArrayPool.Shared without memory allocation
            int sortKeyLength = (source.Length > 1024 * 1024 / 4) ? 0 : 4 * source.Length;

            byte[]? borrowedArray = null;
            Span<byte> sortKey = sortKeyLength <= 1024
                ? stackalloc byte[1024]
                : (borrowedArray = ArrayPool<byte>.Shared.Rent(sortKeyLength));

            fixed (char* pSource = &MemoryMarshal.GetReference(source))
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

        private SortVersion GetSortVersion()
        {
            Debug.Assert(!GlobalizationMode.Invariant);

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
            true,  /*0x27, '*/
            false, /*0x28, (*/
            false, /*0x29, )*/
            false, /*0x2A **/
            false, /*0x2B, +*/
            false, /*0x2C, ,*/
            true,  /*0x2D, -*/
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
