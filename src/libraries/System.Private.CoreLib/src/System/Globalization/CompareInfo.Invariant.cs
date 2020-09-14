// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Globalization
{
    public partial class CompareInfo
    {
        internal static unsafe int InvariantIndexOf(string source, string value, int startIndex, int count, bool ignoreCase)
        {
            Debug.Assert(source != null);
            Debug.Assert(value != null);
            Debug.Assert(startIndex >= 0 && startIndex < source.Length);

            fixed (char* pSource = source) fixed (char* pValue = value)
            {
                char* pSrc = &pSource[startIndex];
                int index = InvariantFindString(pSrc, count, pValue, value.Length, ignoreCase, fromBeginning: true);
                if (index >= 0)
                {
                    return index + startIndex;
                }
                return -1;
            }
        }

        internal static unsafe int InvariantIndexOf(ReadOnlySpan<char> source, ReadOnlySpan<char> value, bool ignoreCase, bool fromBeginning = true)
        {
            Debug.Assert(source.Length != 0);
            Debug.Assert(value.Length != 0);

            fixed (char* pSource = &MemoryMarshal.GetReference(source))
            fixed (char* pValue = &MemoryMarshal.GetReference(value))
            {
                return InvariantFindString(pSource, source.Length, pValue, value.Length, ignoreCase, fromBeginning);
            }
        }

        internal static unsafe int InvariantLastIndexOf(string source, string value, int startIndex, int count, bool ignoreCase)
        {
            Debug.Assert(!string.IsNullOrEmpty(source));
            Debug.Assert(value != null);
            Debug.Assert(startIndex >= 0 && startIndex < source.Length);

            fixed (char* pSource = source) fixed (char* pValue = value)
            {
                char* pSrc = &pSource[startIndex - count + 1];
                int index = InvariantFindString(pSrc, count, pValue, value.Length, ignoreCase, fromBeginning: false);
                if (index >= 0)
                {
                    return index + startIndex - count + 1;
                }
                return -1;
            }
        }

        private static unsafe int InvariantFindString(char* source, int sourceCount, char* value, int valueCount, bool ignoreCase, bool fromBeginning)
        {
            int ctrSource = 0;  // index value into source
            int ctrValue = 0;   // index value into value
            char sourceChar;    // Character for case lookup in source
            char valueChar;     // Character for case lookup in value
            int lastSourceStart;

            Debug.Assert(source != null);
            Debug.Assert(value != null);
            Debug.Assert(sourceCount >= 0);
            Debug.Assert(valueCount >= 0);

            if (valueCount == 0)
            {
                return fromBeginning ? 0 : sourceCount;
            }

            if (sourceCount < valueCount)
            {
                return -1;
            }

            if (fromBeginning)
            {
                lastSourceStart = sourceCount - valueCount;
                if (ignoreCase)
                {
                    char firstValueChar = InvariantCaseFold(value[0]);
                    for (ctrSource = 0; ctrSource <= lastSourceStart; ctrSource++)
                    {
                        sourceChar = InvariantCaseFold(source[ctrSource]);
                        if (sourceChar != firstValueChar)
                        {
                            continue;
                        }

                        for (ctrValue = 1; ctrValue < valueCount; ctrValue++)
                        {
                            sourceChar = InvariantCaseFold(source[ctrSource + ctrValue]);
                            valueChar = InvariantCaseFold(value[ctrValue]);

                            if (sourceChar != valueChar)
                            {
                                break;
                            }
                        }

                        if (ctrValue == valueCount)
                        {
                            return ctrSource;
                        }
                    }
                }
                else
                {
                    char firstValueChar = value[0];
                    for (ctrSource = 0; ctrSource <= lastSourceStart; ctrSource++)
                    {
                        sourceChar = source[ctrSource];
                        if (sourceChar != firstValueChar)
                        {
                            continue;
                        }

                        for (ctrValue = 1; ctrValue < valueCount; ctrValue++)
                        {
                            sourceChar = source[ctrSource + ctrValue];
                            valueChar = value[ctrValue];

                            if (sourceChar != valueChar)
                            {
                                break;
                            }
                        }

                        if (ctrValue == valueCount)
                        {
                            return ctrSource;
                        }
                    }
                }
            }
            else
            {
                lastSourceStart = sourceCount - valueCount;
                if (ignoreCase)
                {
                    char firstValueChar = InvariantCaseFold(value[0]);
                    for (ctrSource = lastSourceStart; ctrSource >= 0; ctrSource--)
                    {
                        sourceChar = InvariantCaseFold(source[ctrSource]);
                        if (sourceChar != firstValueChar)
                        {
                            continue;
                        }
                        for (ctrValue = 1; ctrValue < valueCount; ctrValue++)
                        {
                            sourceChar = InvariantCaseFold(source[ctrSource + ctrValue]);
                            valueChar = InvariantCaseFold(value[ctrValue]);

                            if (sourceChar != valueChar)
                            {
                                break;
                            }
                        }

                        if (ctrValue == valueCount)
                        {
                            return ctrSource;
                        }
                    }
                }
                else
                {
                    char firstValueChar = value[0];
                    for (ctrSource = lastSourceStart; ctrSource >= 0; ctrSource--)
                    {
                        sourceChar = source[ctrSource];
                        if (sourceChar != firstValueChar)
                        {
                            continue;
                        }

                        for (ctrValue = 1; ctrValue < valueCount; ctrValue++)
                        {
                            sourceChar = source[ctrSource + ctrValue];
                            valueChar = value[ctrValue];

                            if (sourceChar != valueChar)
                            {
                                break;
                            }
                        }

                        if (ctrValue == valueCount)
                        {
                            return ctrSource;
                        }
                    }
                }
            }

            return -1;
        }

        private static char InvariantCaseFold(char c)
        {
            // If we ever make Invariant mode support more than just simple ASCII-range case folding,
            // then we should update this method to perform proper case folding instead of an
            // uppercase conversion. For now it only understands the ASCII range and reflects all
            // non-ASCII values unchanged.

            return (uint)(c - 'a') <= (uint)('z' - 'a') ? (char)(c - 0x20) : c;
        }

        private SortKey InvariantCreateSortKey(string source, CompareOptions options)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }

            if ((options & ValidCompareMaskOffFlags) != 0)
            {
                throw new ArgumentException(SR.Argument_InvalidFlag, nameof(options));
            }

            byte[] keyData;
            if (source.Length == 0)
            {
                keyData = Array.Empty<byte>();
            }
            else
            {
                // In the invariant mode, all string comparisons are done as ordinal so when generating the sort keys we generate it according to this fact
                keyData = new byte[source.Length * sizeof(char)];

                if ((options & (CompareOptions.IgnoreCase | CompareOptions.OrdinalIgnoreCase)) != 0)
                {
                    InvariantCreateSortKeyOrdinalIgnoreCase(source, keyData);
                }
                else
                {
                    InvariantCreateSortKeyOrdinal(source, keyData);
                }
            }

            return new SortKey(this, source, options, keyData);
        }

        private static void InvariantCreateSortKeyOrdinal(ReadOnlySpan<char> source, Span<byte> sortKey)
        {
            Debug.Assert(sortKey.Length >= source.Length * sizeof(char));

            for (int i = 0; i < source.Length; i++)
            {
                // convert machine-endian to big-endian
                BinaryPrimitives.WriteUInt16BigEndian(sortKey, (ushort)source[i]);
                sortKey = sortKey.Slice(sizeof(ushort));
            }
        }

        private static void InvariantCreateSortKeyOrdinalIgnoreCase(ReadOnlySpan<char> source, Span<byte> sortKey)
        {
            Debug.Assert(sortKey.Length >= source.Length * sizeof(char));

            for (int i = 0; i < source.Length; i++)
            {
                // convert machine-endian to big-endian
                BinaryPrimitives.WriteUInt16BigEndian(sortKey, (ushort)InvariantCaseFold(source[i]));
                sortKey = sortKey.Slice(sizeof(ushort));
            }
        }

        private int InvariantGetSortKey(ReadOnlySpan<char> source, Span<byte> destination, CompareOptions options)
        {
            Debug.Assert(GlobalizationMode.Invariant);
            Debug.Assert((options & ValidCompareMaskOffFlags) == 0);

            // Make sure the destination buffer is large enough to hold the source projection.
            // Using unsigned arithmetic below also checks for buffer overflow since the incoming
            // length is always a non-negative signed integer.

            if ((uint)destination.Length < (uint)source.Length * sizeof(char))
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }

            if ((options & CompareOptions.IgnoreCase) == 0)
            {
                InvariantCreateSortKeyOrdinal(source, destination);
            }
            else
            {
                InvariantCreateSortKeyOrdinalIgnoreCase(source, destination);
            }

            return source.Length * sizeof(char);
        }

        private int InvariantGetSortKeyLength(ReadOnlySpan<char> source, CompareOptions options)
        {
            Debug.Assert(GlobalizationMode.Invariant);
            Debug.Assert((options & ValidCompareMaskOffFlags) == 0);

            // In invariant mode, sort keys are simply a byte projection of the source input,
            // optionally with casing modifications. We need to make sure we don't overflow
            // while computing the length.

            int byteLength = source.Length * sizeof(char);

            if (byteLength < 0)
            {
                throw new ArgumentException(
                    paramName: nameof(source),
                    message: SR.ArgumentOutOfRange_GetByteCountOverflow);
            }

            return byteLength;
        }
    }
}
