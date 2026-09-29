// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Globalization;
using System.Text;

namespace DotnetFuzzing.Fuzzers;

/// <summary>
/// Fuzzes the globalization APIs that call into ICU (System.Globalization.Native) and write the native result into managed
/// buffers: normalization (Normalize, TryNormalize, GetNormalizedLength, IsNormalized), IDN (GetAscii/GetUnicode and the
/// span-writing TryGetAscii/TryGetUnicode), sort keys (GetSortKey, GetSortKeyLength and the span-writing GetSortKey), culture-aware
/// search (IndexOf/LastIndexOf/IsPrefix/IsSuffix with matchLength, including the managed unsafe ordinal fast paths in
/// CompareInfo.Icu.cs) and culture-aware casing into spans. Every input and every destination sits right before (or after) a
/// guard page, so any read or write past a buffer faults, and destinations are sized exactly and one short. Results are
/// cross-checked: span vs string overloads, sort-key order vs Compare, matchLength vs Compare, normalization idempotence and
/// composition identities, IDN round trips.
/// </summary>
/// <remarks>Input layout: [0] operation, [1] culture, [2] options, [3..] text through a palette of tricky code points.</remarks>
internal sealed class GlobalizationIcuFuzzer : IFuzzer
{
    public string[] TargetAssemblies => [];
    public string[] TargetCoreLibPrefixes { get; } =
        ["System.Globalization.CompareInfo", "System.Globalization.TextInfo", "System.Globalization.Normalization", "System.Globalization.IdnMapping",
         "System.Globalization.Ordinal", "System.StringNormalizationExtensions", "System.Globalization.SortKey"];

    private static readonly bool s_strict = Environment.GetEnvironmentVariable("TENSOR_FUZZ_STRICT") == "1";

    private static readonly string[] s_cultureNames = ["", "en-US", "tr-TR", "de-DE", "lt-LT", "el-GR", "ja-JP", "th-TH", "sv-SE", "zh-Hans-CN", "ar-SA", "az-Latn-AZ", "hu-HU", "da-DK"];
    private static readonly CultureInfo[] s_cultures = s_cultureNames.Select(n => CultureInfo.GetCultureInfo(n)).ToArray();

    private static readonly CompareOptions[] s_options =
    [
        CompareOptions.None, CompareOptions.IgnoreCase, CompareOptions.IgnoreNonSpace, CompareOptions.IgnoreSymbols, CompareOptions.IgnoreKanaType,
        CompareOptions.IgnoreWidth, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace, CompareOptions.Ordinal, CompareOptions.OrdinalIgnoreCase,
        CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth, CompareOptions.IgnoreSymbols | CompareOptions.IgnoreCase,
        CompareOptions.StringSort,
    ];

    // Bytes 0x00-0x7F map to themselves; the rest pick code points that expand, compose, case-map across lengths, are ignorable,
    // or are otherwise special to ICU.
    private static readonly string[] s_palette =
    [
        "́", "̇", "̈", "̣", "̧", "̛", "ͅ", "각", "가", "한", "ﬁ", "ﬃ",
        "ﷺ", "ΐ", "ẞ", "ß", "İ", "ı", "Å", "K", "Å", "Σ", "ς", "­", "‍",
        "​", "﻿", "ཷ", "ཱི", "̈́", "Ａ", "ａ", "ガ", "ガ", "が", "ｶﾞ", "　",
        "𝅗𝅥", "𝅘𝅥𝅮", "𐐀", "𐐨", "\uD800", "\uDC00", "\u0000", "�", "￿", "เก",
        "ال", "ﻻ", "Ǆ", "ǅ", "ǆ", "ᾀ", "ᾳ", "ŉ", "xn--", "--", ".", "。", "．", "｡",
        "ä", "ä", "Å", "Ặ", "ේ", "ෝ", "Ω", "Ω", "µ", "μ", "-",
    ];

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 4)
        {
            return;
        }

        CultureInfo culture = s_cultures[bytes[1] % s_cultures.Length];
        CompareOptions options = s_options[bytes[2] % s_options.Length];
        PoisonPagePlacement placement = (bytes[2] & 0x80) != 0 ? PoisonPagePlacement.Before : PoisonPagePlacement.After;

        var builder = new StringBuilder();
        int split = -1;
        foreach (byte b in bytes.Slice(3))
        {
            if (b == 0x7C && split < 0)
            {
                split = builder.Length; // '|' separates the source from a second string (value / other).
                continue;
            }

            builder.Append(b < 0x80 ? ((char)b).ToString() : s_palette[b % s_palette.Length]);
        }

        string all = builder.ToString();
        if (all.Length > 3000)
        {
            return;
        }

        string source = split < 0 ? all : all.Substring(0, split);
        string value = split < 0 ? (all.Length > 2 ? all.Substring(all.Length / 3, Math.Min(3, all.Length - all.Length / 3)) : all) : all.Substring(split);

        switch (bytes[0] % 5)
        {
            case 0: Collation(culture.CompareInfo, options, source, value, placement); break;
            case 1: Search(culture.CompareInfo, options, source, value, placement); break;
            case 2: Normalization(source, placement); break;
            case 3: Idn(source, (bytes[2] & 1) != 0, (bytes[2] & 2) != 0, placement); break;
            default: Casing(culture, source, placement); break;
        }
    }

    // ---------------------------------------------------------------- sort keys and Compare

    private static void Collation(CompareInfo compareInfo, CompareOptions options, string a, string b, PoisonPagePlacement placement)
    {
        if (options is CompareOptions.Ordinal or CompareOptions.OrdinalIgnoreCase)
        {
            options = CompareOptions.None; // Sort keys don't accept the ordinal options.
        }

        using PooledBoundedMemory<char> aMemory = PooledBoundedMemory<char>.Rent(a.AsSpan(), placement);
        using PooledBoundedMemory<char> bMemory = PooledBoundedMemory<char>.Rent(b.AsSpan(), placement);

        int compare = compareInfo.Compare(a, b, options);
        int compareSpan = compareInfo.Compare(aMemory.Span, bMemory.Span, options);
        Check(Math.Sign(compare) == Math.Sign(compareSpan), () => $"{Name(compareInfo)} Compare string={compare} span={compareSpan} ({options}) for {Describe(a)} / {Describe(b)}");
        Check(Math.Sign(compareInfo.Compare(b, a, options)) == -Math.Sign(compare), () => $"{Name(compareInfo)} Compare isn't antisymmetric ({options}) for {Describe(a)} / {Describe(b)}");

        SortKey keyA = compareInfo.GetSortKey(a, options);
        SortKey keyB = compareInfo.GetSortKey(b, options);
        Check(Math.Sign(SortKey.Compare(keyA, keyB)) == Math.Sign(compare),
            () => $"{Name(compareInfo)} sort keys order {Describe(a)} / {Describe(b)} as {SortKey.Compare(keyA, keyB)} but Compare says {compare} ({options})");
        if (compare == 0)
        {
            Check(compareInfo.GetHashCode(a, options) == compareInfo.GetHashCode(b, options) && compareInfo.GetHashCode(aMemory.Span, options) == compareInfo.GetHashCode(a, options),
                () => $"{Name(compareInfo)} equal strings {Describe(a)} / {Describe(b)} hash differently ({options})");
        }

        // The span-writing overload: exact destination, one short, and GetSortKeyLength.
        int length = compareInfo.GetSortKeyLength(aMemory.Span, options);
        Check(length == keyA.KeyData.Length, () => $"{Name(compareInfo)} GetSortKeyLength={length} but GetSortKey gives {keyA.KeyData.Length} bytes for {Describe(a)} ({options})");
        using (PooledBoundedMemory<byte> destination = PooledBoundedMemory<byte>.Rent(length, placement))
        {
            destination.Span.Fill(0xCC);
            int written = compareInfo.GetSortKey(aMemory.Span, destination.Span, options);
            Check(written == length && destination.Span.SequenceEqual(keyA.KeyData), () => $"{Name(compareInfo)} GetSortKey(span) wrote {written} bytes that differ from GetSortKey(string) for {Describe(a)} ({options})");
        }

        if (length > 0)
        {
            using PooledBoundedMemory<byte> small = PooledBoundedMemory<byte>.Rent(length - 1, placement);
            bool threw = false;
            try
            {
                compareInfo.GetSortKey(aMemory.Span, small.Span, options);
            }
            catch (ArgumentException)
            {
                threw = true;
            }

            Check(threw, () => $"{Name(compareInfo)} GetSortKey into {length - 1} bytes (needs {length}) didn't throw for {Describe(a)} ({options})");
        }
    }

    // ---------------------------------------------------------------- IndexOf / LastIndexOf / IsPrefix / IsSuffix

    private static void Search(CompareInfo compareInfo, CompareOptions options, string source, string value, PoisonPagePlacement placement)
    {
        if (options == CompareOptions.StringSort)
        {
            options = CompareOptions.None; // Not valid for searching.
        }

        using PooledBoundedMemory<char> sourceMemory = PooledBoundedMemory<char>.Rent(source.AsSpan(), placement);
        using PooledBoundedMemory<char> valueMemory = PooledBoundedMemory<char>.Rent(value.AsSpan(), placement);
        ReadOnlySpan<char> s = sourceMemory.Span, v = valueMemory.Span;
        string context = $"{Name(compareInfo)} ({options}) source {Describe(source)} value {Describe(value)}";

        int index = compareInfo.IndexOf(s, v, options, out int matchLength);
        int indexString = compareInfo.IndexOf(source, value, options);
        Check(index == indexString, () => $"IndexOf span={index} string={indexString}: {context}");
        CheckMatch(compareInfo, options, source, value, index, matchLength, "IndexOf", context);

        int last = compareInfo.LastIndexOf(s, v, options, out int lastLength);
        int lastString = compareInfo.LastIndexOf(source, value, options);
        // Finding 54: repeated backward searches on the cached ICU search object return stale matches (another index, a negative
        // matchLength), so LastIndexOf is only checked for self-consistency in strict mode.
        if (s_strict)
        {
            Check(last == lastString, () => $"LastIndexOf span={last} string={lastString}: {context}");
            CheckMatch(compareInfo, options, source, value, last, lastLength, "LastIndexOf", context);
        }
        Check((index < 0) == (last < 0) && (index < 0 || last >= index) || !s_strict, () => $"IndexOf={index} but LastIndexOf={last}: {context}");

        bool prefix = compareInfo.IsPrefix(s, v, options, out int prefixLength);
        Check(prefix == compareInfo.IsPrefix(source, value, options), () => $"IsPrefix span/string disagree: {context}");
        if (prefix)
        {
            CheckMatch(compareInfo, options, source, value, 0, prefixLength, "IsPrefix", context);
            Check(index == 0 || value.Length == 0 || (options & CompareOptions.IgnoreSymbols) != 0 || IsIgnorable(compareInfo, source, index, options) ||
                (!s_strict && (HasHangul(source) || HasMark(source) || Decompose(source) != source)),
                () => $"IsPrefix is true but IndexOf={index}: {context}");
        }

        bool suffix = compareInfo.IsSuffix(s, v, options, out int suffixLength);
        Check(suffix == compareInfo.IsSuffix(source, value, options) || (!s_strict && (options & ~CompareOptions.IgnoreCase) != 0),
            () => $"IsSuffix span/string disagree: {context}"); // Finding 54: the usearch-based ComplexEndsWith path sees stale state.
        if (suffix)
        {
            CheckMatch(compareInfo, options, source, value, source.Length - suffixLength, suffixLength, "IsSuffix", context);
        }

        // Single-char overloads go through their own fast paths.
        if (value.Length > 0)
        {
            char c = value[0];
            int charIndex = compareInfo.IndexOf(source, c, options);
            int charLast = compareInfo.LastIndexOf(source, c, options);
            Check(charIndex < 0 || (charIndex <= source.Length && (charLast >= charIndex || !s_strict)), () => $"IndexOf(char)={charIndex} LastIndexOf(char)={charLast}: {context}");
        }
    }

    // Finding 49: ICU's usearch ignores the case level that IgnoreNonSpace (without IgnoreCase) turns on for Compare, so a
    // match found by IndexOf/IsPrefix/IsSuffix may differ from the value only by case.
    private static bool IsCaseOnlySearchMismatch(CompareInfo compareInfo, CompareOptions options, string matched, string value) =>
        (options & (CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreCase)) == CompareOptions.IgnoreNonSpace &&
        compareInfo.Compare(matched, value, options | CompareOptions.IgnoreCase) == 0;

    // Finding 50: IsSuffix reports matchLength = source.Length for an ignorable suffix. Finding 51: with IgnoreSymbols, ICU usearch
    // drops combining-mark collation elements, so a value of only marks "matches" with length 0 though Compare says it isn't ignorable.
    // In shifted mode (IgnoreSymbols, or Thai, whose CLDR collation is shifted by default) usearch drops combining marks anywhere,
    // so compare with marks removed. Finding 53: SimpleAffix accepts an affix that splits a grapheme (Indic vowel signs, Hangul jamo,
    // and backwards even a nonspacing mark). An IsSuffix
    // match that ends inside an expansion (a suffix U+0307 against U+0130) reports matchLength 0, which can't be checked.
    private static bool IsKnownMatchLengthIssue(CompareInfo compareInfo, CompareOptions options, string value, string matched, int matchLength, string what) =>
        (what == "IsSuffix" && (matchLength == 0 || compareInfo.Compare(value, string.Empty, options) == 0)) ||
        (IsShifted(compareInfo, options) && (matchLength == 0 || HasMark(Decompose(matched + value)))) ||
        (what is "IsPrefix" or "IsSuffix" && (HasHangul(matched + value) || HasMark(matched + value) || Decompose(matched + value) != matched + value));

    private static string Decompose(string text)
    {
        try
        {
            return text.Normalize(NormalizationForm.FormKD);
        }
        catch (ArgumentException)
        {
            return text; // lone surrogates
        }
    }

    private static bool IsShifted(CompareInfo compareInfo, CompareOptions options) =>
        (options & CompareOptions.IgnoreSymbols) != 0 || compareInfo.Name.StartsWith("th", StringComparison.Ordinal);

    private static bool IsMark(char c) =>
        char.GetUnicodeCategory(c) is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or UnicodeCategory.EnclosingMark ||
        c is (>= '\u3099' and <= '\u309C') or '\uFF9E' or '\uFF9F'; // kana voiced/semi-voiced sound marks, including halfwidth (category Lm)

    private static bool HasMark(string text) => text.Any(IsMark);

    private static string StripMarks(string text)
    {
        try
        {
            text = text.Normalize(NormalizationForm.FormKD); // so a precomposed ガ loses its voiced mark like ｶﾞ does
        }
        catch (ArgumentException)
        {
            // Lone surrogates can't be normalized; strip what's there.
        }

        return string.Concat(text.Where(c => !IsMark(c)));
    }

    // Hangul syllables/jamo, and Thai/Lao prevowels, which the collation reorders with the following consonant.
    private static bool HasHangul(string text) => text.Any(c => c is (>= '\u1100' and <= '\u11FF') or (>= '\uAC00' and <= '\uD7A3') or (>= '\u3130' and <= '\u318F')
        or (>= '\u0E40' and <= '\u0E44') or (>= '\u0EC0' and <= '\u0EC4'));

    private static bool IsIgnorable(CompareInfo compareInfo, string source, int index, CompareOptions options) =>
        index > 0 && compareInfo.Compare(source.Substring(0, index), string.Empty, options) == 0;

    private static void CheckMatch(CompareInfo compareInfo, CompareOptions options, string source, string value, int index, int matchLength, string what, string context)
    {
        if (index < 0)
        {
            Check(matchLength == 0, () => $"{what} found nothing but matchLength={matchLength}: {context}");
            return;
        }

        Check(index <= source.Length && matchLength >= 0 && index + matchLength <= source.Length, () => $"{what}={index} matchLength={matchLength} is outside the {source.Length}-char source: {context}");
        string matched = source.Substring(index, matchLength);
        if (!s_strict && IsKnownMatchLengthIssue(compareInfo, options, value, matched, matchLength, what))
        {
            return;
        }

        Check(compareInfo.Compare(matched, value, options) == 0 || (!s_strict && IsCaseOnlySearchMismatch(compareInfo, options, matched, value)), () => $"{what}={index} matchLength={matchLength} selects {Describe(matched)}, which doesn't compare equal to the value: {context}");
    }

    // ---------------------------------------------------------------- normalization

    private static readonly NormalizationForm[] s_forms = [NormalizationForm.FormC, NormalizationForm.FormD, NormalizationForm.FormKC, NormalizationForm.FormKD];

    private static void Normalization(string source, PoisonPagePlacement placement)
    {
        using PooledBoundedMemory<char> sourceMemory = PooledBoundedMemory<char>.Rent(source.AsSpan(), placement);
        bool valid = !HasLoneSurrogate(source);

        foreach (NormalizationForm form in s_forms)
        {
            string? normalized = null;
            try
            {
                normalized = source.Normalize(form);
            }
            catch (ArgumentException)
            {
                Check(!valid, () => $"Normalize({form}) threw ArgumentException for valid text {Describe(source)}");
            }

            if (normalized is null)
            {
                // Every API must reject the invalid text the same way.
                Check(Throws(() => source.IsNormalized(form)) && Throws(() => sourceMemory.Span.GetNormalizedLength(form)),
                    () => $"Normalize({form}) rejected {Describe(source)} but IsNormalized/GetNormalizedLength didn't");
                continue;
            }

            Check(valid, () => $"Normalize({form}) accepted text with a lone surrogate {Describe(source)}");
            Check(normalized.IsNormalized(form), () => $"Normalize({form}) of {Describe(source)} isn't IsNormalized({form})");
            Check(normalized.Normalize(form) == normalized, () => $"Normalize({form}) isn't idempotent for {Describe(source)}");
            Check(source.IsNormalized(form) == (normalized == source), () => $"IsNormalized({form}) of {Describe(source)} is {source.IsNormalized(form)} but Normalize changed it: {normalized != source}");

            int length = sourceMemory.Span.GetNormalizedLength(form);
            Check(length == normalized.Length, () => $"GetNormalizedLength({form})={length} but Normalize gives {normalized.Length} chars for {Describe(source)}");

            if (source.Length > 0)
            {
                using PooledBoundedMemory<char> exact = PooledBoundedMemory<char>.Rent(Math.Max(length, 1), placement);
                bool ok = sourceMemory.Span.TryNormalize(exact.Span, out int written, form);
                Check((length == 0 || ok) && (!ok || (written == length && exact.Span.Slice(0, written).SequenceEqual(normalized))),
                    () => $"TryNormalize({form}) into {Math.Max(length, 1)} chars returned {ok}/{written} for {Describe(source)} (expected {length})");

                if (length > 1)
                {
                    using PooledBoundedMemory<char> small = PooledBoundedMemory<char>.Rent(length - 1, placement);
                    Check(!sourceMemory.Span.TryNormalize(small.Span, out int w, form) && w == 0, () => $"TryNormalize({form}) into {length - 1} chars succeeded for {Describe(source)} (needs {length})");
                }
            }
        }

        if (valid)
        {
            string nfd = source.Normalize(NormalizationForm.FormD), nfc = source.Normalize(NormalizationForm.FormC);
            Check(nfd.Normalize(NormalizationForm.FormC) == nfc, () => $"NFC(NFD(x)) != NFC(x) for {Describe(source)}");
            Check(nfc.Normalize(NormalizationForm.FormD) == nfd, () => $"NFD(NFC(x)) != NFD(x) for {Describe(source)}");
            string nfkc = source.Normalize(NormalizationForm.FormKC);
            Check(source.Normalize(NormalizationForm.FormKD).Normalize(NormalizationForm.FormKC) == nfkc, () => $"NFKC(NFKD(x)) != NFKC(x) for {Describe(source)}");
            Check(nfc.Normalize(NormalizationForm.FormKC) == nfkc, () => $"NFKC(NFC(x)) != NFKC(x) for {Describe(source)}");
        }
    }

    // ---------------------------------------------------------------- IDN

    // Finding 48: GetAscii(string)/GetUnicode(string) hand back the caller's string when ICU's (lowercased) answer differs from it
    // only by case, while the span overloads return ICU's output. Lowercasing the string results lines them up.
    // Only ASCII: UTS #46 maps some scripts to uppercase (Cherokee), so a full lowercase would differ from ICU's output.
    private static string IcuCase(string text) => s_strict ? text : string.Concat(text.Select(c => char.IsAsciiLetterUpper(c) ? (char)(c | 0x20) : c));

    private static bool HasHyphen34Label(string ascii) =>
        ascii.Split('.').Any(label => label.Length >= 4 && label[2] == '-' && label[3] == '-');

    private static void Idn(string source, bool useStd3, bool allowUnassigned, PoisonPagePlacement placement)
    {
        var idn = new IdnMapping { UseStd3AsciiRules = useStd3, AllowUnassigned = allowUnassigned };
        if (source.Length == 0 || source.Length > 400)
        {
            return;
        }

        using PooledBoundedMemory<char> sourceMemory = PooledBoundedMemory<char>.Rent(source.AsSpan(), placement);
        string? ascii = null;
        try
        {
            ascii = IcuCase(idn.GetAscii(source));
        }
        catch (ArgumentException)
        {
        }

        // The span-writing overload must agree, with an exact destination and a generous one.
        using (PooledBoundedMemory<char> big = PooledBoundedMemory<char>.Rent(1024, placement))
        {
            bool ok;
            int written = 0;
            try
            {
                ok = idn.TryGetAscii(sourceMemory.Span, big.Span, out written);
            }
            catch (ArgumentException)
            {
                ok = false;
                Check(ascii is null, () => $"TryGetAscii threw but GetAscii returned {Describe(ascii)} for {Describe(source)}");
            }

            if (ascii is not null)
            {
                Check(ok && big.Span.Slice(0, written).SequenceEqual(ascii), () => $"TryGetAscii returned {ok}/{Describe(new string(big.Span.Slice(0, written)))} but GetAscii {Describe(ascii)} for {Describe(source)}");
            }
        }

        if (ascii is null)
        {
            return;
        }

        Check(ascii.All(char.IsAscii), () => $"GetAscii({Describe(source)}) returned non-ASCII {Describe(ascii)}");
        using (PooledBoundedMemory<char> exact = PooledBoundedMemory<char>.Rent(ascii.Length, placement))
        {
            Check(idn.TryGetAscii(sourceMemory.Span, exact.Span, out int w) && w == ascii.Length && exact.Span.SequenceEqual(ascii),
                () => $"TryGetAscii into exactly {ascii.Length} chars failed for {Describe(source)}");
        }

        if (ascii.Length > 1)
        {
            using PooledBoundedMemory<char> small = PooledBoundedMemory<char>.Rent(ascii.Length - 1, placement);
            Check(!idn.TryGetAscii(sourceMemory.Span, small.Span, out int w) && w == 0, () => $"TryGetAscii into {ascii.Length - 1} chars succeeded for {Describe(source)} (needs {ascii.Length})");
        }

        // Round trip: GetUnicode(GetAscii(x)) and back is stable.
        string unicode;
        try
        {
            unicode = IcuCase(idn.GetUnicode(ascii));
        }
        catch (ArgumentException) when (!s_strict && HasHyphen34Label(ascii))
        {
            return; // Finding 52: ToAscii masks UIDNA_ERROR_HYPHEN_3_4 but ToUnicode doesn't.
        }

        Check(IcuCase(idn.GetAscii(unicode)) == ascii, () => $"GetAscii(GetUnicode({Describe(ascii)})) = {Describe(idn.GetAscii(unicode))}, not the same ASCII form");
        using PooledBoundedMemory<char> asciiMemory = PooledBoundedMemory<char>.Rent(ascii.AsSpan(), placement);
        using (PooledBoundedMemory<char> exactUnicode = PooledBoundedMemory<char>.Rent(Math.Max(unicode.Length, 1), placement))
        {
            Check(idn.TryGetUnicode(asciiMemory.Span, exactUnicode.Span, out int w) && w == unicode.Length && exactUnicode.Span.Slice(0, w).SequenceEqual(unicode),
                () => $"TryGetUnicode into exactly {unicode.Length} chars disagrees with GetUnicode for {Describe(ascii)}");
        }

        if (unicode.Length > 1)
        {
            using PooledBoundedMemory<char> small = PooledBoundedMemory<char>.Rent(unicode.Length - 1, placement);
            Check(!idn.TryGetUnicode(asciiMemory.Span, small.Span, out int w) && w == 0, () => $"TryGetUnicode into {unicode.Length - 1} chars succeeded for {Describe(ascii)} (needs {unicode.Length})");
        }
    }

    // ---------------------------------------------------------------- casing

    private static void Casing(CultureInfo culture, string source, PoisonPagePlacement placement)
    {
        using PooledBoundedMemory<char> sourceMemory = PooledBoundedMemory<char>.Rent(source.AsSpan(), placement);
        TextInfo textInfo = culture.TextInfo;

        foreach (bool upper in new[] { true, false })
        {
            string expected = upper ? textInfo.ToUpper(source) : textInfo.ToLower(source);
            Check(expected.Length == source.Length, () => $"{culture.Name} {(upper ? "ToUpper" : "ToLower")} changed the length of {Describe(source)} to {expected.Length}");

            using (PooledBoundedMemory<char> exact = PooledBoundedMemory<char>.Rent(source.Length, placement))
            {
                int written = upper ? sourceMemory.Span.ToUpper(exact.Span, culture) : sourceMemory.Span.ToLower(exact.Span, culture);
                Check(written == source.Length && exact.Span.SequenceEqual(expected), () => $"{culture.Name} span {(upper ? "ToUpper" : "ToLower")} wrote {written} chars that differ from the string overload for {Describe(source)}");
            }

            if (source.Length > 0)
            {
                using PooledBoundedMemory<char> small = PooledBoundedMemory<char>.Rent(source.Length - 1, placement);
                int w = upper ? sourceMemory.Span.ToUpper(small.Span, culture) : sourceMemory.Span.ToLower(small.Span, culture);
                Check(w == -1, () => $"{culture.Name} span {(upper ? "ToUpper" : "ToLower")} into {source.Length - 1} chars returned {w} for {Describe(source)}");
            }

            // Per-char casing agrees with string casing for BMP non-surrogates.
            for (int i = 0; i < source.Length; i++)
            {
                if (!char.IsSurrogate(source[i]))
                {
                    char single = upper ? textInfo.ToUpper(source[i]) : textInfo.ToLower(source[i]);
                    int index = i;
                    Check(single == expected[i], () => $"{culture.Name} {(upper ? "ToUpper" : "ToLower")}(char U+{(int)source[index]:X4}) = U+{(int)single:X4} but the string overload gives U+{(int)expected[index]:X4}");
                }
            }
        }

        _ = textInfo.ToTitleCase(source);
    }

    // ---------------------------------------------------------------- helpers

    private static bool HasLoneSurrogate(string text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                i++;
            }
            else if (char.IsSurrogate(text[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool Throws(Action action)
    {
        try
        {
            action();
            return false;
        }
        catch (ArgumentException)
        {
            return true;
        }
    }

    private delegate void SpanAction(ReadOnlySpan<char> span);

    private static string Name(CompareInfo compareInfo) => compareInfo.Name.Length == 0 ? "invariant" : compareInfo.Name;

    private static string Describe(string? text) =>
        text is null ? "(null)" : "\"" + string.Concat(text.Take(80).Select(c => c is >= ' ' and < '\x7F' ? c.ToString() : $"\\u{(int)c:X4}")) + (text.Length > 80 ? $"...\" ({text.Length})" : "\"");

    private static void Check(bool condition, Func<string> message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message());
        }
    }
}
