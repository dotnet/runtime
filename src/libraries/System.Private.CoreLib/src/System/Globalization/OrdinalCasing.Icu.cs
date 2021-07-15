// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Internal.Runtime.CompilerServices;

namespace System.Globalization
{
    internal static partial class OrdinalCasing
    {
        // s_noCasingPage means the Unicode page doesn't support any casing and no case translation is needed.
        private static ushort [] s_noCasingPage = Array.Empty<ushort>();

        // s_basicLatin is covering the casing for the Basic Latin & C0 Controls range.
        // we are not lazy initializing this range because it is the most common used range and we'll cache it anyway very early.
        private static ushort [] s_basicLatin =
        {
            // Upper Casing

            /* 0000-000f */  0x0000, 0x0001, 0x0002, 0x0003, 0x0004, 0x0005, 0x0006, 0x0007, 0x0008, 0x0009, 0x000a, 0x000b, 0x000c, 0x000d, 0x000e, 0x000f,
            /* 0010-001f */  0x0010, 0x0011, 0x0012, 0x0013, 0x0014, 0x0015, 0x0016, 0x0017, 0x0018, 0x0019, 0x001a, 0x001b, 0x001c, 0x001d, 0x001e, 0x001f,
            /* 0020-002f */  0x0020, 0x0021, 0x0022, 0x0023, 0x0024, 0x0025, 0x0026, 0x0027, 0x0028, 0x0029, 0x002a, 0x002b, 0x002c, 0x002d, 0x002e, 0x002f,
            /* 0030-003f */  0x0030, 0x0031, 0x0032, 0x0033, 0x0034, 0x0035, 0x0036, 0x0037, 0x0038, 0x0039, 0x003a, 0x003b, 0x003c, 0x003d, 0x003e, 0x003f,
            /* 0040-004f */  0x0040, 0x0041, 0x0042, 0x0043, 0x0044, 0x0045, 0x0046, 0x0047, 0x0048, 0x0049, 0x004a, 0x004b, 0x004c, 0x004d, 0x004e, 0x004f,
            /* 0050-005f */  0x0050, 0x0051, 0x0052, 0x0053, 0x0054, 0x0055, 0x0056, 0x0057, 0x0058, 0x0059, 0x005a, 0x005b, 0x005c, 0x005d, 0x005e, 0x005f,
            /* 0060-006f */  0x0060, 0x0041, 0x0042, 0x0043, 0x0044, 0x0045, 0x0046, 0x0047, 0x0048, 0x0049, 0x004a, 0x004b, 0x004c, 0x004d, 0x004e, 0x004f,
            /* 0070-007f */  0x0050, 0x0051, 0x0052, 0x0053, 0x0054, 0x0055, 0x0056, 0x0057, 0x0058, 0x0059, 0x005a, 0x007b, 0x007c, 0x007d, 0x007e, 0x007f,
            /* 0080-008f */  0x0080, 0x0081, 0x0082, 0x0083, 0x0084, 0x0085, 0x0086, 0x0087, 0x0088, 0x0089, 0x008a, 0x008b, 0x008c, 0x008d, 0x008e, 0x008f,
            /* 0090-009f */  0x0090, 0x0091, 0x0092, 0x0093, 0x0094, 0x0095, 0x0096, 0x0097, 0x0098, 0x0099, 0x009a, 0x009b, 0x009c, 0x009d, 0x009e, 0x009f,
            /* 00a0-00af */  0x00a0, 0x00a1, 0x00a2, 0x00a3, 0x00a4, 0x00a5, 0x00a6, 0x00a7, 0x00a8, 0x00a9, 0x00aa, 0x00ab, 0x00ac, 0x00ad, 0x00ae, 0x00af,
            /* 00b0-00bf */  0x00b0, 0x00b1, 0x00b2, 0x00b3, 0x00b4, 0x039c, 0x00b6, 0x00b7, 0x00b8, 0x00b9, 0x00ba, 0x00bb, 0x00bc, 0x00bd, 0x00be, 0x00bf,
            /* 00c0-00cf */  0x00c0, 0x00c1, 0x00c2, 0x00c3, 0x00c4, 0x00c5, 0x00c6, 0x00c7, 0x00c8, 0x00c9, 0x00ca, 0x00cb, 0x00cc, 0x00cd, 0x00ce, 0x00cf,
            /* 00d0-00df */  0x00d0, 0x00d1, 0x00d2, 0x00d3, 0x00d4, 0x00d5, 0x00d6, 0x00d7, 0x00d8, 0x00d9, 0x00da, 0x00db, 0x00dc, 0x00dd, 0x00de, 0x00df,
            /* 00e0-00ef */  0x00c0, 0x00c1, 0x00c2, 0x00c3, 0x00c4, 0x00c5, 0x00c6, 0x00c7, 0x00c8, 0x00c9, 0x00ca, 0x00cb, 0x00cc, 0x00cd, 0x00ce, 0x00cf,
            /* 00f0-00ff */  0x00d0, 0x00d1, 0x00d2, 0x00d3, 0x00d4, 0x00d5, 0x00d6, 0x00f7, 0x00d8, 0x00d9, 0x00da, 0x00db, 0x00dc, 0x00dd, 0x00de, 0x0178,
        };

        // s_casingTable is covering the Unicode BMP plane only. Surrogate casing is handled separately.
        // Every cell in the table is covering the casing of 256 characters in the BMP.
        // Every cell is array of 512 character for uppercasing mapping.
        private static ushort []?[] s_casingTable = InitCasingTable();

        /*
         The table is initialized to:
        {
            // 0000-07FF //       s_basicLatin,            null,            null,            null,            null,            null,            null,            null,
            // 0800-0FFF //               null,            null,            null,            null,            null,            null,            null,            null,
            // 1000-17FF //               null,  s_noCasingPage,            null,            null,  s_noCasingPage,  s_noCasingPage,            null,            null,
            // 1800-1FFF //               null,            null,            null,            null,            null,            null,            null,            null,
            // 2000-27FF //               null,            null,  s_noCasingPage,  s_noCasingPage,            null,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,
            // 2800-2FFF //     s_noCasingPage,  s_noCasingPage,  s_noCasingPage,            null,            null,            null,            null,            null,
            // 3000-37FF //               null,            null,            null,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,
            // 3800-3FFF //     s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,
            // 4000-47FF //     s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,
            // 4800-4FFF //     s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,
            // 5000-57FF //     s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,
            // 5800-5FFF //     s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,
            // 6000-67FF //     s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,
            // 6800-6FFF //     s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,
            // 7000-77FF //     s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,
            // 7800-7FFF //     s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,
            // 8000-87FF //     s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,
            // 8800-8FFF //     s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,
            // 9000-97FF //     s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,
            // 9800-9FFF //     s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,            null,
            // A000-A7FF //     s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,            null,  s_noCasingPage,            null,            null,
            // A800-AFFF //               null,            null,            null,            null,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,
            // B000-B7FF //     s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,
            // B800-BFFF //     s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,
            // C000-C7FF //     s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,
            // C800-CFFF //     s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,
            // D000-D7FF //     s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,            null,
            // D800-DFFF //     s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,
            // E000-E7FF //     s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,
            // E800-EFFF //     s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,
            // F000-F7FF //     s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,  s_noCasingPage,
            // F800-FFFF //     s_noCasingPage,  s_noCasingPage,            null,            null,  s_noCasingPage,            null,            null,            null,
        };
*/

        // 0 - null
        // 1 - s_noCasingPage
        // The bits are in reverse order for readability, i.e. the highest order bit refers to
        // the lowest index.
        private static ReadOnlySpan<byte> s_casingTableInit => new byte[32]
        {
            /* 0000-07FF */    0b00000000,
            /* 0800-0FFF */    0b00000000,
            /* 1000-17FF */    0b01001100,
            /* 1800-1FFF */    0b00000000,
            /* 2000-27FF */    0b00110111,
            /* 2800-2FFF */    0b11100000,
            /* 3000-37FF */    0b00011111,
            /* 3800-3FFF */    0b11111111,
            /* 4000-47FF */    0b11111111,
            /* 4800-4FFF */    0b11111111,
            /* 5000-57FF */    0b11111111,
            /* 5800-5FFF */    0b11111111,
            /* 6000-67FF */    0b11111111,
            /* 6800-6FFF */    0b11111111,
            /* 7000-77FF */    0b11111111,
            /* 7800-7FFF */    0b11111111,
            /* 8000-87FF */    0b11111111,
            /* 8800-8FFF */    0b11111111,
            /* 9000-97FF */    0b11111111,
            /* 9800-9FFF */    0b11111110,
            /* A000-A7FF */    0b11110100,
            /* A800-AFFF */    0b00001111,
            /* B000-B7FF */    0b11111111,
            /* B800-BFFF */    0b11111111,
            /* C000-C7FF */    0b11111111,
            /* C800-CFFF */    0b11111111,
            /* D000-D7FF */    0b11111110,
            /* D800-DFFF */    0b11111111,
            /* E000-E7FF */    0b11111111,
            /* E800-EFFF */    0b11111111,
            /* F000-F7FF */    0b11111111,
            /* F800-FFFF */    0b11001000,
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static char ToUpper(char c)
        {
            int pageNumber = ((int)c) >> 8;
            if (pageNumber == 0) // optimize for ASCII range
            {
                return (char) s_basicLatin[(int)c];
            }

            ushort[]? casingTable = s_casingTable[pageNumber];

            if (casingTable == s_noCasingPage)
            {
                return c;
            }

            if (casingTable == null)
            {
                casingTable = InitOrdinalCasingPage(pageNumber);
            }

            return (char) casingTable[((int)c) & 0xFF];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static char ToUpperInvariantMode(char c) => c <= '\u00FF' ? (char) s_basicLatin[(int)c] : c;

        public static void ToUpperInvariantMode(this ReadOnlySpan<char> source, Span<char> destination)
        {
            for (int i = 0; i < source.Length; i++)
            {
                destination[i] = ToUpperInvariantMode(source[i]);
            }
        }

        internal static void ToUpperOrdinal(ReadOnlySpan<char> source, Span<char> destination)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);

            for (int i = 0; i < source.Length; i++)
            {
                char c = source[i];
                if (c <= '\u00FF') // optimize ASCII/Latin
                {
                    destination[i] = (char)s_basicLatin[c];
                    continue;
                }

                if (char.IsHighSurrogate(c) && i < source.Length - 1)
                {
                    char cl = source[i + 1];
                    if (char.IsLowSurrogate(cl))
                    {
                        // well formed surrogates
                        SurrogateCasing.ToUpper(c, cl, out destination[i], out destination[i+1]);
                        i++; // skip the low surrogate
                        continue;
                    }
                }

                destination[i] = ToUpper(c);
            }
        }

        internal static int CompareStringIgnoreCase(ref char strA, int lengthA, ref char strB, int lengthB)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);

            int length = Math.Min(lengthA, lengthB);

            ref char charA = ref strA;
            ref char charB = ref strB;

            int index = 0;

            while (index < length)
            {
                char a = charA;
                char b = charB;
                char lowSurrogateA = '\0';

                if (!char.IsHighSurrogate(a) || index >= lengthA - 1 || !char.IsLowSurrogate(lowSurrogateA = Unsafe.Add(ref charA, 1)))
                {
                    if (!char.IsHighSurrogate(b) || index >= lengthB - 1 || !char.IsLowSurrogate(Unsafe.Add(ref charB, 1)))
                    {
                        //
                        // Neither A or B are surrogates
                        //

                        if (b == a)
                        {
                            index++;
                            charA = ref Unsafe.Add(ref charA, 1);
                            charB = ref Unsafe.Add(ref charB, 1);
                            continue;
                        }

                        char aUpper = OrdinalCasing.ToUpper(a);
                        char bUpper = OrdinalCasing.ToUpper(b);

                        if (aUpper == bUpper)
                        {
                            index++;
                            charA = ref Unsafe.Add(ref charA, 1);
                            charB = ref Unsafe.Add(ref charB, 1);
                            continue;
                        }

                        return a - b;
                    }

                    //
                    // charA is not surrogate and charB is valid surrogate
                    //

                    return -1;
                }

                //
                // A is Surrogate
                //

                char lowSurrogateB = '\0';

                if (!char.IsHighSurrogate(b) || index >= lengthB - 1 || !char.IsLowSurrogate(lowSurrogateB = Unsafe.Add(ref charB, 1)))
                {
                    //
                    // charB is not surrogate and charA is surrogate
                    //

                    return 1;
                }

                //
                // charA and charB are surrogates
                //

                Debug.Assert(lowSurrogateA != '\0');
                Debug.Assert(lowSurrogateB != '\0');

                if (a == b && lowSurrogateA == lowSurrogateB)
                {
                    index += 2;
                    charA = ref Unsafe.Add(ref charA, 2);
                    charB = ref Unsafe.Add(ref charB, 2);
                    continue;
                }

                uint upperSurrogateA = CharUnicodeInfo.ToUpper(UnicodeUtility.GetScalarFromUtf16SurrogatePair(a, lowSurrogateA));
                uint upperSurrogateB = CharUnicodeInfo.ToUpper(UnicodeUtility.GetScalarFromUtf16SurrogatePair(b, lowSurrogateB));

                if (upperSurrogateA == upperSurrogateB)
                {
                    index += 2;
                    charA = ref Unsafe.Add(ref charA, 2);
                    charB = ref Unsafe.Add(ref charB, 2);
                    continue;
                }

                return (int)upperSurrogateA - (int)upperSurrogateB;
            }

            return lengthA - lengthB;
        }

        internal static unsafe int IndexOf(ReadOnlySpan<char> source, ReadOnlySpan<char> value)
        {
            Debug.Assert(value.Length > 0);
            Debug.Assert(value.Length <= source.Length);

            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);

            fixed (char* pSource = &MemoryMarshal.GetReference(source))
            fixed (char* pValue  = &MemoryMarshal.GetReference(value))
            {
                char* pSourceLimit = pSource + (source.Length - value.Length);
                char* pValueLimit = pValue + value.Length - 1;
                char* pCurrentSource = pSource;

                while (pCurrentSource <= pSourceLimit)
                {
                    char *pVal = pValue;
                    char *pSrc = pCurrentSource;

                    while (pVal <= pValueLimit)
                    {
                        if (!char.IsHighSurrogate(*pVal) || pVal == pValueLimit)
                        {
                            if (*pVal != *pSrc && ToUpper(*pVal) != ToUpper(*pSrc))
                                break; // no match

                            pVal++;
                            pSrc++;
                            continue;
                        }

                        if (char.IsHighSurrogate(*pSrc) && char.IsLowSurrogate(*(pSrc + 1)) && char.IsLowSurrogate(*(pVal + 1)))
                        {
                            // Well formed surrogates
                            // both the source and the Value have well-formed surrogates.
                            if (!SurrogateCasing.Equal(*pSrc, *(pSrc + 1), *pVal, *(pVal + 1)))
                                break; // no match

                            pSrc += 2;
                            pVal += 2;
                            continue;
                        }

                        if (*pVal != *pSrc)
                            break; // no match

                        pSrc++;
                        pVal++;
                    }

                    if (pVal > pValueLimit)
                    {
                        // Found match.
                        return (int) (pCurrentSource - pSource);
                    }

                    pCurrentSource++;
                }

                return -1;
            }
        }

        internal static unsafe int LastIndexOf(ReadOnlySpan<char> source, ReadOnlySpan<char> value)
        {
            Debug.Assert(value.Length > 0);
            Debug.Assert(value.Length <= source.Length);

            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);

            fixed (char* pSource = &MemoryMarshal.GetReference(source))
            fixed (char* pValue  = &MemoryMarshal.GetReference(value))
            {
                char* pValueLimit = pValue + value.Length - 1;
                char* pCurrentSource = pSource + (source.Length - value.Length);

                while (pCurrentSource >= pSource)
                {
                    char *pVal = pValue;
                    char *pSrc = pCurrentSource;

                    while (pVal <= pValueLimit)
                    {
                        if (!char.IsHighSurrogate(*pVal) || pVal == pValueLimit)
                        {
                            if (*pVal != *pSrc && ToUpper(*pVal) != ToUpper(*pSrc))
                                break; // no match

                            pVal++;
                            pSrc++;
                            continue;
                        }

                        if (char.IsHighSurrogate(*pSrc) && char.IsLowSurrogate(*(pSrc + 1)) && char.IsLowSurrogate(*(pVal + 1)))
                        {
                            // Well formed surrogates
                            // both the source and the Value have well-formed surrogates.
                            if (!SurrogateCasing.Equal(*pSrc, *(pSrc + 1), *pVal, *(pVal + 1)))
                                break; // no match

                            pSrc += 2;
                            pVal += 2;
                            continue;
                        }

                        if (*pVal != *pSrc)
                            break; // no match

                        pSrc++;
                        pVal++;
                    }

                    if (pVal > pValueLimit)
                    {
                        // Found match.
                        return (int) (pCurrentSource - pSource);
                    }

                    pCurrentSource--;
                }

                return -1;
            }
        }

        private static ushort[]?[] InitCasingTable()
        {
            ushort[]?[] table = new ushort[]?[s_casingTableInit.Length * 8];
            for (int i = 0; i < s_casingTableInit.Length * 8; ++i)
            {
                // The bits are in reverse order
                byte val = (byte)(s_casingTableInit[i / 8] >> (7 - (i % 8)));
                if ((val & 1) == 1)
                    table[i] = s_noCasingPage;
            }
            table[0] = s_basicLatin;
            return table;
        }

        private static unsafe ushort [] InitOrdinalCasingPage(int pageNumber)
        {
            Debug.Assert(pageNumber >= 0 && pageNumber < 256);

            ushort[] casingTable = new ushort[256];
            fixed (ushort* table = casingTable)
            {
                char* pTable = (char*)table;
                Interop.Globalization.InitOrdinalCasingPage(pageNumber, pTable);
            }
            Volatile.Write(ref s_casingTable[pageNumber], casingTable);
            return casingTable;
        }
    }
}
