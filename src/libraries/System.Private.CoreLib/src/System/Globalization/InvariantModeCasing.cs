// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Globalization
{
    internal static class InvariantModeCasing
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static char ToLower(char c) => CharUnicodeInfo.ToLower(c);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static char ToUpper(char c) => CharUnicodeInfo.ToUpper(c);

        internal static string ToLower(string s)
        {
            if (s.Length == 0)
            {
                return string.Empty;
            }

            ReadOnlySpan<char> source = s;

            int i = 0;
            while (i < s.Length)
            {
                if (char.IsHighSurrogate(source[i]) && i < s.Length - 1 && char.IsLowSurrogate(source[i + 1]))
                {
                    SurrogateCasing.ToLower(source[i], source[i + 1], out char h, out char l);
                    if (source[i] != h || source[i + 1] != l)
                    {
                        break;
                    }

                    i += 2;
                    continue;
                }

                if (ToLower(source[i]) != source[i])
                {
                    break;
                }

                i++;
            }

            if (i >= s.Length)
            {
                return s;
            }

            string result = string.FastAllocateString(s.Length);
            var destination = new Span<char>(ref result.GetRawStringData(), result.Length);
            ReadOnlySpan<char> src = s;
            src.Slice(0, i).CopyTo(destination);
            ToLower(src.Slice(i), destination.Slice(i));

            return result;
        }

        internal static string ToUpper(string s)
        {
            if (s.Length == 0)
            {
                return string.Empty;
            }

            ReadOnlySpan<char> source = s;

            int i = 0;
            while (i < s.Length)
            {
                if (char.IsHighSurrogate(source[i]) && i < s.Length - 1 && char.IsLowSurrogate(source[i + 1]))
                {
                    SurrogateCasing.ToUpper(source[i], source[i + 1], out char h, out char l);
                    if (source[i] != h || source[i + 1] != l)
                    {
                        break;
                    }

                    i += 2;
                    continue;
                }

                if (ToUpper(source[i]) != source[i])
                {
                    break;
                }

                i++;
            }

            if (i >= s.Length)
            {
                return s;
            }

            string result = string.FastAllocateString(s.Length);
            var destination = new Span<char>(ref result.GetRawStringData(), result.Length);
            ReadOnlySpan<char> src = s;
            src.Slice(0, i).CopyTo(destination);
            ToUpper(src.Slice(i), destination.Slice(i));

            return result;
        }

        internal static void ToUpper(ReadOnlySpan<char> source, Span<char> destination)
        {
            Debug.Assert(GlobalizationMode.Invariant);
            Debug.Assert(source.Length <= destination.Length);

            for (int i = 0; i < source.Length; i++)
            {
                char c = source[i];
                if (char.IsHighSurrogate(c) && i < source.Length - 1)
                {
                    char cl = source[i + 1];
                    if (char.IsLowSurrogate(cl))
                    {
                        // well formed surrogates
                        SurrogateCasing.ToUpper(c, cl, out char h, out char l);
                        destination[i] = h;
                        destination[i + 1] = l;
                        i++; // skip the low surrogate
                        continue;
                    }
                }

                destination[i] = ToUpper(c);
            }
        }

        internal static void ToLower(ReadOnlySpan<char> source, Span<char> destination)
        {
            Debug.Assert(GlobalizationMode.Invariant);
            Debug.Assert(source.Length <= destination.Length);

            for (int i = 0; i < source.Length; i++)
            {
                char c = source[i];
                if (char.IsHighSurrogate(c) && i < source.Length - 1)
                {
                    char cl = source[i + 1];
                    if (char.IsLowSurrogate(cl))
                    {
                        // well formed surrogates
                        SurrogateCasing.ToLower(c, cl, out char h, out char l);
                        destination[i] = h;
                        destination[i + 1] = l;
                        i++; // skip the low surrogate
                        continue;
                    }
                }

                destination[i] = ToLower(c);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (uint, int) GetScalar(ref char source, int index, int length)
        {
            char charA = source;
            if (!char.IsHighSurrogate(charA) || index >= length - 1)
            {
                return ((uint)charA, 1);
            }

            char charB = Unsafe.Add(ref source, 1);
            if (!char.IsLowSurrogate(charB))
            {
                return ((uint)charA, 1);
            }

            return (UnicodeUtility.GetScalarFromUtf16SurrogatePair(charA, charB), 2);
        }

        internal static int CompareStringIgnoreCase(ref char strA, int lengthA, ref char strB, int lengthB)
        {
            Debug.Assert(GlobalizationMode.Invariant);

            int length = Math.Min(lengthA, lengthB);

            ref char charA = ref strA;
            ref char charB = ref strB;

            int index = 0;

            while (index < length)
            {
                (uint codePointA, int codePointLengthA) = GetScalar(ref charA, index, lengthA);
                (uint codePointB, int codePointLengthB) = GetScalar(ref charB, index, lengthB);

                if (codePointA == codePointB)
                {
                    Debug.Assert(codePointLengthA == codePointLengthB);
                    index += codePointLengthA;
                    charA = ref Unsafe.Add(ref charA, codePointLengthA);
                    charB = ref Unsafe.Add(ref charB, codePointLengthB);
                    continue;
                }

                uint aUpper = CharUnicodeInfo.ToUpper(codePointA);
                uint bUpper = CharUnicodeInfo.ToUpper(codePointB);

                if (aUpper == bUpper)
                {
                    Debug.Assert(codePointLengthA == codePointLengthB);
                    index += codePointLengthA;
                    charA = ref Unsafe.Add(ref charA, codePointLengthA);
                    charB = ref Unsafe.Add(ref charB, codePointLengthB);
                    continue;
                }

                return (int)codePointA - (int)codePointB;
            }

            return lengthA - lengthB;
        }

        internal static unsafe int IndexOfIgnoreCase(ReadOnlySpan<char> source, ReadOnlySpan<char> value)
        {
            Debug.Assert(value.Length > 0);
            Debug.Assert(value.Length <= source.Length);
            Debug.Assert(GlobalizationMode.Invariant);

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
                        return (int)(pCurrentSource - pSource);
                    }

                    pCurrentSource++;
                }

                return -1;
            }
        }

        internal static unsafe int LastIndexOfIgnoreCase(ReadOnlySpan<char> source, ReadOnlySpan<char> value)
        {
            Debug.Assert(value.Length > 0);
            Debug.Assert(value.Length <= source.Length);
            Debug.Assert(GlobalizationMode.Invariant);

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
                        return (int)(pCurrentSource - pSource);
                    }

                    pCurrentSource--;
                }

                return -1;
            }
        }
    }
}
