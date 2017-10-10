// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

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
                int index = InvariantFindString(pSrc, count, pValue, value.Length, ignoreCase, start : true);
                if (index >= 0)
                {
                    return index + startIndex;
                }
                return -1;
            }
        }

        internal static unsafe int InvariantLastIndexOf(string source, string value, int startIndex, int count, bool ignoreCase)
        {
            Debug.Assert(source != null);
            Debug.Assert(value != null);
            Debug.Assert(startIndex >= 0 && startIndex < source.Length);

            fixed (char* pSource = source) fixed (char* pValue = value)
            {
                char* pSrc = &pSource[startIndex - count + 1];
                int index = InvariantFindString(pSrc, count, pValue, value.Length, ignoreCase, start : false);
                if (index >= 0)
                {
                    return index + startIndex - count + 1;
                }
                return -1;
            }
        }

        private static unsafe int InvariantFindString(char* source, int sourceCount, char* value, int valueCount, bool ignoreCase, bool start)
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
                return start ? 0 : sourceCount - 1;
            }

            if (sourceCount < valueCount)
            {
                return -1;
            }

            if (start)
            {
                lastSourceStart = sourceCount - valueCount;
                if (ignoreCase)
                {
                    char firstValueChar = InvariantToUpper(value[0]);
                    for (ctrSource = 0; ctrSource <= lastSourceStart; ctrSource++)
                    {
                        sourceChar = InvariantToUpper(source[ctrSource]);
                        if (sourceChar != firstValueChar)
                        {
                            continue;
                        }

                        for (ctrValue = 1; ctrValue < valueCount; ctrValue++)
                        {
                            sourceChar = InvariantToUpper(source[ctrSource + ctrValue]);
                            valueChar = InvariantToUpper(value[ctrValue]);

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
                    char firstValueChar = InvariantToUpper(value[0]);
                    for (ctrSource = lastSourceStart; ctrSource >= 0; ctrSource--)
                    {
                        sourceChar = InvariantToUpper(source[ctrSource]);
                        if (sourceChar != firstValueChar)
                        {
                            continue;
                        }
                        for (ctrValue = 1; ctrValue < valueCount; ctrValue++)
                        {
                            sourceChar = InvariantToUpper(source[ctrSource + ctrValue]);
                            valueChar = InvariantToUpper(value[ctrValue]);

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

        private static char InvariantToUpper(char c)
        {
            return (uint)(c - 'a') <= (uint)('z' - 'a') ? (char)(c - 0x20) : c;
        }

        private unsafe SortKey InvariantCreateSortKey(string source, CompareOptions options)
        {
            if (source == null) { throw new ArgumentNullException(nameof(source)); }

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
                // In the invariant mode, all string comparisons are done as ordinal so when generating the sort keys we generate it according to this fact
                keyData = new byte[source.Length * sizeof(char)];

                fixed (char* pChar = source) fixed (byte* pByte = keyData)
                {
                    if ((options & (CompareOptions.IgnoreCase | CompareOptions.OrdinalIgnoreCase)) != 0)
                    {
                        short *pShort = (short *) pByte;
                        for (int i=0; i<source.Length; i++)
                        {
                            pShort[i] = (short) InvariantToUpper(source[i]);
                        }
                    } 
                    else
                    {
                        Buffer.MemoryCopy(pChar, pByte, keyData.Length, keyData.Length);
                    }
                }
            }
            return new SortKey(Name, source, options, keyData);
        }
    }
}
