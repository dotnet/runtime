// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Internal.Runtime.CompilerServices;

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

            return string.Create(s.Length, (s, i), static (destination, state) =>
            {
                ReadOnlySpan<char> src = state.s;
                src.Slice(0, state.i).CopyTo(destination);
                InvariantModeCasing.ToLower(src.Slice(state.i), destination.Slice(state.i));
            });

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

            return string.Create(s.Length, (s, i), static (destination, state) =>
            {
                ReadOnlySpan<char> src = state.s;
                src.Slice(0, state.i).CopyTo(destination);
                InvariantModeCasing.ToUpper(src.Slice(state.i), destination.Slice(state.i));
            });
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
                        destination[i]   = h;
                        destination[i+1] = l;
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
                if (char.IsHighSurrogate(c) && i < source.Length - 1 )
                {
                    char cl = source[i + 1];
                    if (char.IsLowSurrogate(cl))
                    {
                        // well formed surrogates
                        SurrogateCasing.ToLower(c, cl, out char h, out char l);
                        destination[i]   = h;
                        destination[i+1] = l;
                        i++; // skip the low surrogate
                        continue;
                    }
                }

                destination[i] = ToLower(c);
            }
        }

        internal static int CompareStringIgnoreCase(ref char strA, int lengthA, ref char strB, int lengthB)
        {
            Debug.Assert(GlobalizationMode.Invariant);

            int length = Math.Min(lengthA, lengthB);

            ref char charA = ref strA;
            ref char charB = ref strB;

            while (length != 0)
            {
                if (!char.IsHighSurrogate(charA) || !char.IsHighSurrogate(charB))
                {
                    if (charA == charB)
                    {
                        length--;
                        charA = ref Unsafe.Add(ref charA, 1);
                        charB = ref Unsafe.Add(ref charB, 1);
                        continue;
                    }

                    char aUpper = ToUpper(charA);
                    char bUpper = ToUpper(charB);

                    if (aUpper == bUpper)
                    {
                        length--;
                        charA = ref Unsafe.Add(ref charA, 1);
                        charB = ref Unsafe.Add(ref charB, 1);
                        continue;
                    }

                    return aUpper - bUpper;
                }

                if (length == 1)
                {
                    return charA - charB;
                }

                // We come here only if we have valid high surrogates and length > 1

                char a = charA;
                char b = charB;

                length--;
                charA = ref Unsafe.Add(ref charA, 1);
                charB = ref Unsafe.Add(ref charB, 1);

                if (!char.IsLowSurrogate(charA) || !char.IsLowSurrogate(charB))
                {
                    // malformed Surrogates - should be rare cases
                    if (a != b)
                    {
                        return a - b;
                    }

                    // Should be pointing to the right characters in the string to resume at.
                    // Just in case we could be pointing at high surrogate now.
                    continue;
                }

                // we come here only if we have valid full surrogates
                SurrogateCasing.ToUpper(a, charA, out char h1, out char l1);
                SurrogateCasing.ToUpper(b, charB, out char h2, out char l2);

                if (h1 != h2)
                {
                    return (int)h1 - (int)h2;
                }

                if (l1 != l2)
                {
                    return (int)l1 - (int)l2;
                }

                length--;
                charA = ref Unsafe.Add(ref charA, 1);
                charB = ref Unsafe.Add(ref charB, 1);
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
                        return (int) (pCurrentSource - pSource);
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
                        return (int) (pCurrentSource - pSource);
                    }

                    pCurrentSource--;
                }

                return -1;
            }
        }
    }
}
