// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Ported from the Brotli reference implementation (https://github.com/google/brotli), c/enc/static_dict.h and static_dict.c.

using System.Runtime.CompilerServices;

namespace System.IO.Compression;

/// <summary>Matching of the input against the static dictionary words and transforms.</summary>
internal static class StaticDict
{
    public const int MaxStaticDictionaryMatchLen = 37;
    public const uint InvalidMatch = 0xFFFFFFF;

    private const byte TransformUppercaseFirst = 10;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AddMatch(nuint distance, nuint len, nuint lenCode, Span<uint> matches)
    {
        uint match = (uint)((distance << 5) + lenCode);
        matches[(int)len] = Math.Min(matches[(int)len], match);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint DictMatchLength(ReadOnlySpan<byte> data, nuint id, nuint len, nuint maxlen)
    {
        nuint offset = BrotliDictionary.OffsetsByLength[(int)len] + len * id;
        return FindMatchLength.FindMatchLengthWithLimit(BrotliDictionary.Data.Slice((int)offset), data, Math.Min(len, maxlen));
    }

    /// <summary>
    /// Matches <paramref name="data"/> against the dictionary word of length <paramref name="len"/> and index
    /// <paramref name="idx"/> with the given <paramref name="transform"/> (identity, uppercase first or uppercase all).
    /// </summary>
    private static bool IsMatch(nuint len, byte transform, nuint idx, ReadOnlySpan<byte> data, nuint maxLength)
    {
        if (len > maxLength)
        {
            return false;
        }
        else
        {
            nuint offset = BrotliDictionary.OffsetsByLength[(int)len] + len * idx;
            ReadOnlySpan<byte> dict = BrotliDictionary.Data.Slice((int)offset);
            if (transform == 0)
            {
                // Match against base dictionary word.
                return FindMatchLength.FindMatchLengthWithLimit(dict, data, len) == len;
            }
            else if (transform == 10)
            {
                // Match against uppercase first transform.
                // Note that there are only ASCII uppercase words in the lookup table.
                return dict[0] >= 'a' && dict[0] <= 'z' &&
                    (dict[0] ^ 32) == data[0] &&
                    FindMatchLength.FindMatchLengthWithLimit(dict.Slice(1), data.Slice(1), len - 1u) == len - 1u;
            }
            else
            {
                // Match against uppercase all transform.
                // Note that there are only ASCII uppercase words in the lookup table.
                for (int i = 0; (nuint)i < len; ++i)
                {
                    if (dict[i] >= 'a' && dict[i] <= 'z')
                    {
                        if ((dict[i] ^ 32) != data[i])
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (dict[i] != data[i])
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
        }
    }

    /// <summary>
    /// Matches data against static dictionary words, and for each length l, for which a match is found, updates
    /// matches[l] to be the minimum possible (distance &lt;&lt; 5) + len_code.
    /// Returns true if matches have been found, otherwise false.
    /// Prerequisites: matches array is at least MaxStaticDictionaryMatchLen + 1 long, all elements are initialized
    /// to <see cref="InvalidMatch"/>. <paramref name="data"/> is the input data starting at the current position.
    /// </summary>
    public static bool FindAllStaticDictionaryMatches(ReadOnlySpan<byte> data, nuint minLength, nuint maxLength, Span<uint> matches)
    {
        // Only the built-in dictionary is supported, so there is never a second (contextual) dictionary to search.
        return FindAllStaticDictionaryMatchesFor(data, minLength, maxLength, matches);
    }

    /// <summary>Finds matches for a single static dictionary.</summary>
    private static bool FindAllStaticDictionaryMatchesFor(ReadOnlySpan<byte> data, nuint minLength, nuint maxLength, Span<uint> matches)
    {
        bool hasFoundMatch = false;
        ReadOnlySpan<ushort> buckets = EncoderDictionary.Buckets;
        ReadOnlySpan<uint> dictWords = EncoderDictionary.DictWords;
        ReadOnlySpan<byte> sizeBitsByLength = BrotliDictionary.SizeBitsByLength;
        {
            int offset = buckets[(int)HashBase.Hash15(data)];
            bool end = offset == 0;
            while (!end)
            {
                uint w = dictWords[offset++];
                nuint l = w & 0x1F;
                nuint n = (nuint)1 << sizeBitsByLength[(int)l];
                nuint id = w >> 16;
                byte transform = (byte)(w >> 8);
                end = (w & 0x80) != 0;
                if (transform == 0)
                {
                    nuint matchlen = DictMatchLength(data, id, l, maxLength);
                    // Transform "" + BROTLI_TRANSFORM_IDENTITY + ""
                    if (matchlen == l)
                    {
                        AddMatch(id, l, l, matches);
                        hasFoundMatch = true;
                    }
                    // Transforms "" + BROTLI_TRANSFORM_OMIT_LAST_1 + "" and
                    //            "" + BROTLI_TRANSFORM_OMIT_LAST_1 + "ing "
                    if (matchlen >= l - 1)
                    {
                        AddMatch(id + 12 * n, l - 1, l, matches);
                        if (l + 2 < maxLength &&
                            data[(int)l - 1] == 'i' && data[(int)l] == 'n' && data[(int)l + 1] == 'g' &&
                            data[(int)l + 2] == ' ')
                        {
                            AddMatch(id + 49 * n, l + 3, l, matches);
                        }
                        hasFoundMatch = true;
                    }
                    // Transform "" + BROTLI_TRANSFORM_OMIT_LAST_# + "" (# = 2 .. 9)
                    nuint minlen = minLength;
                    if (l > 9)
                    {
                        minlen = Math.Max(minlen, l - 9);
                    }
                    nuint maxlen = Math.Min(matchlen, l - 2);
                    for (nuint len = minlen; len <= maxlen; ++len)
                    {
                        nuint cut = l - len;
                        nuint transformId = (cut << 2) + (nuint)((EncoderDictionary.CutoffTransforms >> (int)(cut * 6)) & 0x3F);
                        AddMatch(id + transformId * n, len, l, matches);
                        hasFoundMatch = true;
                    }
                    if (matchlen < l || l + 6 >= maxLength)
                    {
                        continue;
                    }
                    ReadOnlySpan<byte> s = data.Slice((int)l);
                    // Transforms "" + BROTLI_TRANSFORM_IDENTITY + <suffix>
                    if (s[0] == ' ')
                    {
                        AddMatch(id + n, l + 1, l, matches);
                        if (s[1] == 'a')
                        {
                            if (s[2] == ' ')
                            {
                                AddMatch(id + 28 * n, l + 3, l, matches);
                            }
                            else if (s[2] == 's')
                            {
                                if (s[3] == ' ')
                                {
                                    AddMatch(id + 46 * n, l + 4, l, matches);
                                }
                            }
                            else if (s[2] == 't')
                            {
                                if (s[3] == ' ')
                                {
                                    AddMatch(id + 60 * n, l + 4, l, matches);
                                }
                            }
                            else if (s[2] == 'n')
                            {
                                if (s[3] == 'd' && s[4] == ' ')
                                {
                                    AddMatch(id + 10 * n, l + 5, l, matches);
                                }
                            }
                        }
                        else if (s[1] == 'b')
                        {
                            if (s[2] == 'y' && s[3] == ' ')
                            {
                                AddMatch(id + 38 * n, l + 4, l, matches);
                            }
                        }
                        else if (s[1] == 'i')
                        {
                            if (s[2] == 'n')
                            {
                                if (s[3] == ' ')
                                {
                                    AddMatch(id + 16 * n, l + 4, l, matches);
                                }
                            }
                            else if (s[2] == 's')
                            {
                                if (s[3] == ' ')
                                {
                                    AddMatch(id + 47 * n, l + 4, l, matches);
                                }
                            }
                        }
                        else if (s[1] == 'f')
                        {
                            if (s[2] == 'o')
                            {
                                if (s[3] == 'r' && s[4] == ' ')
                                {
                                    AddMatch(id + 25 * n, l + 5, l, matches);
                                }
                            }
                            else if (s[2] == 'r')
                            {
                                if (s[3] == 'o' && s[4] == 'm' && s[5] == ' ')
                                {
                                    AddMatch(id + 37 * n, l + 6, l, matches);
                                }
                            }
                        }
                        else if (s[1] == 'o')
                        {
                            if (s[2] == 'f')
                            {
                                if (s[3] == ' ')
                                {
                                    AddMatch(id + 8 * n, l + 4, l, matches);
                                }
                            }
                            else if (s[2] == 'n')
                            {
                                if (s[3] == ' ')
                                {
                                    AddMatch(id + 45 * n, l + 4, l, matches);
                                }
                            }
                        }
                        else if (s[1] == 'n')
                        {
                            if (s[2] == 'o' && s[3] == 't' && s[4] == ' ')
                            {
                                AddMatch(id + 80 * n, l + 5, l, matches);
                            }
                        }
                        else if (s[1] == 't')
                        {
                            if (s[2] == 'h')
                            {
                                if (s[3] == 'e')
                                {
                                    if (s[4] == ' ')
                                    {
                                        AddMatch(id + 5 * n, l + 5, l, matches);
                                    }
                                }
                                else if (s[3] == 'a')
                                {
                                    if (s[4] == 't' && s[5] == ' ')
                                    {
                                        AddMatch(id + 29 * n, l + 6, l, matches);
                                    }
                                }
                            }
                            else if (s[2] == 'o')
                            {
                                if (s[3] == ' ')
                                {
                                    AddMatch(id + 17 * n, l + 4, l, matches);
                                }
                            }
                        }
                        else if (s[1] == 'w')
                        {
                            if (s[2] == 'i' && s[3] == 't' && s[4] == 'h' && s[5] == ' ')
                            {
                                AddMatch(id + 35 * n, l + 6, l, matches);
                            }
                        }
                    }
                    else if (s[0] == '"')
                    {
                        AddMatch(id + 19 * n, l + 1, l, matches);
                        if (s[1] == '>')
                        {
                            AddMatch(id + 21 * n, l + 2, l, matches);
                        }
                    }
                    else if (s[0] == '.')
                    {
                        AddMatch(id + 20 * n, l + 1, l, matches);
                        if (s[1] == ' ')
                        {
                            AddMatch(id + 31 * n, l + 2, l, matches);
                            if (s[2] == 'T' && s[3] == 'h')
                            {
                                if (s[4] == 'e')
                                {
                                    if (s[5] == ' ')
                                    {
                                        AddMatch(id + 43 * n, l + 6, l, matches);
                                    }
                                }
                                else if (s[4] == 'i')
                                {
                                    if (s[5] == 's' && s[6] == ' ')
                                    {
                                        AddMatch(id + 75 * n, l + 7, l, matches);
                                    }
                                }
                            }
                        }
                    }
                    else if (s[0] == ',')
                    {
                        AddMatch(id + 76 * n, l + 1, l, matches);
                        if (s[1] == ' ')
                        {
                            AddMatch(id + 14 * n, l + 2, l, matches);
                        }
                    }
                    else if (s[0] == '\n')
                    {
                        AddMatch(id + 22 * n, l + 1, l, matches);
                        if (s[1] == '\t')
                        {
                            AddMatch(id + 50 * n, l + 2, l, matches);
                        }
                    }
                    else if (s[0] == ']')
                    {
                        AddMatch(id + 24 * n, l + 1, l, matches);
                    }
                    else if (s[0] == '\'')
                    {
                        AddMatch(id + 36 * n, l + 1, l, matches);
                    }
                    else if (s[0] == ':')
                    {
                        AddMatch(id + 51 * n, l + 1, l, matches);
                    }
                    else if (s[0] == '(')
                    {
                        AddMatch(id + 57 * n, l + 1, l, matches);
                    }
                    else if (s[0] == '=')
                    {
                        if (s[1] == '"')
                        {
                            AddMatch(id + 70 * n, l + 2, l, matches);
                        }
                        else if (s[1] == '\'')
                        {
                            AddMatch(id + 86 * n, l + 2, l, matches);
                        }
                    }
                    else if (s[0] == 'a')
                    {
                        if (s[1] == 'l' && s[2] == ' ')
                        {
                            AddMatch(id + 84 * n, l + 3, l, matches);
                        }
                    }
                    else if (s[0] == 'e')
                    {
                        if (s[1] == 'd')
                        {
                            if (s[2] == ' ')
                            {
                                AddMatch(id + 53 * n, l + 3, l, matches);
                            }
                        }
                        else if (s[1] == 'r')
                        {
                            if (s[2] == ' ')
                            {
                                AddMatch(id + 82 * n, l + 3, l, matches);
                            }
                        }
                        else if (s[1] == 's')
                        {
                            if (s[2] == 't' && s[3] == ' ')
                            {
                                AddMatch(id + 95 * n, l + 4, l, matches);
                            }
                        }
                    }
                    else if (s[0] == 'f')
                    {
                        if (s[1] == 'u' && s[2] == 'l' && s[3] == ' ')
                        {
                            AddMatch(id + 90 * n, l + 4, l, matches);
                        }
                    }
                    else if (s[0] == 'i')
                    {
                        if (s[1] == 'v')
                        {
                            if (s[2] == 'e' && s[3] == ' ')
                            {
                                AddMatch(id + 92 * n, l + 4, l, matches);
                            }
                        }
                        else if (s[1] == 'z')
                        {
                            if (s[2] == 'e' && s[3] == ' ')
                            {
                                AddMatch(id + 100 * n, l + 4, l, matches);
                            }
                        }
                    }
                    else if (s[0] == 'l')
                    {
                        if (s[1] == 'e')
                        {
                            if (s[2] == 's' && s[3] == 's' && s[4] == ' ')
                            {
                                AddMatch(id + 93 * n, l + 5, l, matches);
                            }
                        }
                        else if (s[1] == 'y')
                        {
                            if (s[2] == ' ')
                            {
                                AddMatch(id + 61 * n, l + 3, l, matches);
                            }
                        }
                    }
                    else if (s[0] == 'o')
                    {
                        if (s[1] == 'u' && s[2] == 's' && s[3] == ' ')
                        {
                            AddMatch(id + 106 * n, l + 4, l, matches);
                        }
                    }
                }
                else
                {
                    // Set is_all_caps=0 for BROTLI_TRANSFORM_UPPERCASE_FIRST and
                    //     is_all_caps=1 otherwise (BROTLI_TRANSFORM_UPPERCASE_ALL) transform.
                    bool isAllCaps = transform != TransformUppercaseFirst;
                    if (!IsMatch(l, transform, id, data, maxLength))
                    {
                        continue;
                    }
                    // Transform "" + kUppercase{First,All} + ""
                    AddMatch(id + (isAllCaps ? 44u : 9u) * n, l, l, matches);
                    hasFoundMatch = true;
                    if (l + 1 >= maxLength)
                    {
                        continue;
                    }
                    // Transforms "" + kUppercase{First,All} + <suffix>
                    ReadOnlySpan<byte> s = data.Slice((int)l);
                    if (s[0] == ' ')
                    {
                        AddMatch(id + (isAllCaps ? 68u : 4u) * n, l + 1, l, matches);
                    }
                    else if (s[0] == '"')
                    {
                        AddMatch(id + (isAllCaps ? 87u : 66u) * n, l + 1, l, matches);
                        if (s[1] == '>')
                        {
                            AddMatch(id + (isAllCaps ? 97u : 69u) * n, l + 2, l, matches);
                        }
                    }
                    else if (s[0] == '.')
                    {
                        AddMatch(id + (isAllCaps ? 101u : 79u) * n, l + 1, l, matches);
                        if (s[1] == ' ')
                        {
                            AddMatch(id + (isAllCaps ? 114u : 88u) * n, l + 2, l, matches);
                        }
                    }
                    else if (s[0] == ',')
                    {
                        AddMatch(id + (isAllCaps ? 112u : 99u) * n, l + 1, l, matches);
                        if (s[1] == ' ')
                        {
                            AddMatch(id + (isAllCaps ? 107u : 58u) * n, l + 2, l, matches);
                        }
                    }
                    else if (s[0] == '\'')
                    {
                        AddMatch(id + (isAllCaps ? 94u : 74u) * n, l + 1, l, matches);
                    }
                    else if (s[0] == '(')
                    {
                        AddMatch(id + (isAllCaps ? 113u : 78u) * n, l + 1, l, matches);
                    }
                    else if (s[0] == '=')
                    {
                        if (s[1] == '"')
                        {
                            AddMatch(id + (isAllCaps ? 105u : 104u) * n, l + 2, l, matches);
                        }
                        else if (s[1] == '\'')
                        {
                            AddMatch(id + (isAllCaps ? 116u : 108u) * n, l + 2, l, matches);
                        }
                    }
                }
            }
        }
        // Transforms with prefixes " " and "."
        if (maxLength >= 5 && (data[0] == ' ' || data[0] == '.'))
        {
            bool isSpace = data[0] == ' ';
            int offset = buckets[(int)HashBase.Hash15(data.Slice(1))];
            bool end = offset == 0;
            while (!end)
            {
                uint w = dictWords[offset++];
                nuint l = w & 0x1F;
                nuint n = (nuint)1 << sizeBitsByLength[(int)l];
                nuint id = w >> 16;
                byte transform = (byte)(w >> 8);
                end = (w & 0x80) != 0;
                if (transform == 0)
                {
                    if (!IsMatch(l, transform, id, data.Slice(1), maxLength - 1))
                    {
                        continue;
                    }
                    // Transforms " " + BROTLI_TRANSFORM_IDENTITY + "" and
                    //            "." + BROTLI_TRANSFORM_IDENTITY + ""
                    AddMatch(id + (isSpace ? 6u : 32u) * n, l + 1, l, matches);
                    hasFoundMatch = true;
                    if (l + 2 >= maxLength)
                    {
                        continue;
                    }
                    // Transforms " " + BROTLI_TRANSFORM_IDENTITY + <suffix> and
                    //            "." + BROTLI_TRANSFORM_IDENTITY + <suffix>
                    ReadOnlySpan<byte> s = data.Slice((int)l + 1);
                    if (s[0] == ' ')
                    {
                        AddMatch(id + (isSpace ? 2u : 77u) * n, l + 2, l, matches);
                    }
                    else if (s[0] == '(')
                    {
                        AddMatch(id + (isSpace ? 89u : 67u) * n, l + 2, l, matches);
                    }
                    else if (isSpace)
                    {
                        if (s[0] == ',')
                        {
                            AddMatch(id + 103 * n, l + 2, l, matches);
                            if (s[1] == ' ')
                            {
                                AddMatch(id + 33 * n, l + 3, l, matches);
                            }
                        }
                        else if (s[0] == '.')
                        {
                            AddMatch(id + 71 * n, l + 2, l, matches);
                            if (s[1] == ' ')
                            {
                                AddMatch(id + 52 * n, l + 3, l, matches);
                            }
                        }
                        else if (s[0] == '=')
                        {
                            if (s[1] == '"')
                            {
                                AddMatch(id + 81 * n, l + 3, l, matches);
                            }
                            else if (s[1] == '\'')
                            {
                                AddMatch(id + 98 * n, l + 3, l, matches);
                            }
                        }
                    }
                }
                else if (isSpace)
                {
                    // Set is_all_caps=0 for BROTLI_TRANSFORM_UPPERCASE_FIRST and
                    //     is_all_caps=1 otherwise (BROTLI_TRANSFORM_UPPERCASE_ALL) transform.
                    bool isAllCaps = transform != TransformUppercaseFirst;
                    if (!IsMatch(l, transform, id, data.Slice(1), maxLength - 1))
                    {
                        continue;
                    }
                    // Transforms " " + kUppercase{First,All} + ""
                    AddMatch(id + (isAllCaps ? 85u : 30u) * n, l + 1, l, matches);
                    hasFoundMatch = true;
                    if (l + 2 >= maxLength)
                    {
                        continue;
                    }
                    // Transforms " " + kUppercase{First,All} + <suffix>
                    ReadOnlySpan<byte> s = data.Slice((int)l + 1);
                    if (s[0] == ' ')
                    {
                        AddMatch(id + (isAllCaps ? 83u : 15u) * n, l + 2, l, matches);
                    }
                    else if (s[0] == ',')
                    {
                        if (!isAllCaps)
                        {
                            AddMatch(id + 109 * n, l + 2, l, matches);
                        }
                        if (s[1] == ' ')
                        {
                            AddMatch(id + (isAllCaps ? 111u : 65u) * n, l + 3, l, matches);
                        }
                    }
                    else if (s[0] == '.')
                    {
                        AddMatch(id + (isAllCaps ? 115u : 96u) * n, l + 2, l, matches);
                        if (s[1] == ' ')
                        {
                            AddMatch(id + (isAllCaps ? 117u : 91u) * n, l + 3, l, matches);
                        }
                    }
                    else if (s[0] == '=')
                    {
                        if (s[1] == '"')
                        {
                            AddMatch(id + (isAllCaps ? 110u : 118u) * n, l + 3, l, matches);
                        }
                        else if (s[1] == '\'')
                        {
                            AddMatch(id + (isAllCaps ? 119u : 120u) * n, l + 3, l, matches);
                        }
                    }
                }
            }
        }
        if (maxLength >= 6)
        {
            // Transforms with prefixes "e ", "s ", ", " and "\xC2\xA0"
            if ((data[1] == ' ' &&
                 (data[0] == 'e' || data[0] == 's' || data[0] == ',')) ||
                (data[0] == 0xC2 && data[1] == 0xA0))
            {
                int offset = buckets[(int)HashBase.Hash15(data.Slice(2))];
                bool end = offset == 0;
                while (!end)
                {
                    uint w = dictWords[offset++];
                    nuint l = w & 0x1F;
                    nuint n = (nuint)1 << sizeBitsByLength[(int)l];
                    nuint id = w >> 16;
                    byte transform = (byte)(w >> 8);
                    end = (w & 0x80) != 0;
                    if (transform == 0 &&
                        IsMatch(l, transform, id, data.Slice(2), maxLength - 2))
                    {
                        if (data[0] == 0xC2)
                        {
                            AddMatch(id + 102 * n, l + 2, l, matches);
                            hasFoundMatch = true;
                        }
                        else if (l + 2 < maxLength && data[(int)l + 2] == ' ')
                        {
                            nuint t = data[0] == 'e' ? 18u : (data[0] == 's' ? 7u : 13u);
                            AddMatch(id + t * n, l + 3, l, matches);
                            hasFoundMatch = true;
                        }
                    }
                }
            }
        }
        if (maxLength >= 9)
        {
            // Transforms with prefixes " the " and ".com/"
            if ((data[0] == ' ' && data[1] == 't' && data[2] == 'h' &&
                 data[3] == 'e' && data[4] == ' ') ||
                (data[0] == '.' && data[1] == 'c' && data[2] == 'o' &&
                 data[3] == 'm' && data[4] == '/'))
            {
                int offset = buckets[(int)HashBase.Hash15(data.Slice(5))];
                bool end = offset == 0;
                while (!end)
                {
                    uint w = dictWords[offset++];
                    nuint l = w & 0x1F;
                    nuint n = (nuint)1 << sizeBitsByLength[(int)l];
                    nuint id = w >> 16;
                    byte transform = (byte)(w >> 8);
                    end = (w & 0x80) != 0;
                    if (transform == 0 &&
                        IsMatch(l, transform, id, data.Slice(5), maxLength - 5))
                    {
                        AddMatch(id + (data[0] == ' ' ? 41u : 72u) * n, l + 5, l, matches);
                        hasFoundMatch = true;
                        if (l + 5 < maxLength)
                        {
                            ReadOnlySpan<byte> s = data.Slice((int)l + 5);
                            if (data[0] == ' ')
                            {
                                if (l + 8 < maxLength &&
                                    s[0] == ' ' && s[1] == 'o' && s[2] == 'f' && s[3] == ' ')
                                {
                                    AddMatch(id + 62 * n, l + 9, l, matches);
                                    if (l + 12 < maxLength &&
                                        s[4] == 't' && s[5] == 'h' && s[6] == 'e' && s[7] == ' ')
                                    {
                                        AddMatch(id + 73 * n, l + 13, l, matches);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        return hasFoundMatch;
    }
}
