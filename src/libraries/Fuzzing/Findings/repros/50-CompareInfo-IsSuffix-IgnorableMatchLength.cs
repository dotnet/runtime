// Finding: on ICU, CompareInfo.IsSuffix(source, suffix, options, out matchLength) reports that the *whole* source matched when
// every collation element of the suffix is ignorable (e.g. "\0", U+200D ZERO WIDTH JOINER, U+00AD SOFT HYPHEN). It correctly
// returns true, since an ignorable suffix matches any string, but sets matchLength = source.Length instead of 0.
//
// Root cause (src/native/libs/System.Globalization.Native/pal_collation.c, SimpleAffix_Iterators): before each step the loop
// saves ucol_getOffset(pSourceIterator) and then calls ucol_previous. On a freshly opened iterator ucol_getOffset returns 0 even
// though the first ucol_previous starts from the end of the text. If the pattern runs out before the source iterator has moved
// once (all pattern elements are ignorable, so moveSource stays false after the first step), the saved offset is that initial
// 0, and SimpleAffix returns textLength - 0. The forward (IsPrefix) direction is unaffected because offset 0 is correct there.
// This path is used for CompareOptions.None and IgnoreCase (the "simple" affix path); en-* and the invariant culture reach it
// once the input isn't plain ASCII.
// Code that trims with the match length (source[..^matchLength]) deletes the whole string.
// Run: dotnet run 50-CompareInfo-IsSuffix-IgnorableMatchLength.cs   (on Linux or macOS; Windows uses NLS)
using System.Globalization;

if (OperatingSystem.IsWindows())
{
    Console.WriteLine("This repro targets the ICU implementation (Linux/macOS). NOT REPRODUCED");
    return;
}

bool reproduced = false;
foreach (string cultureName in new[] { "", "en-US", "de-DE", "tr-TR" })
{
    CompareInfo compare = CultureInfo.GetCultureInfo(cultureName).CompareInfo;
    foreach (CompareOptions options in new[] { CompareOptions.None, CompareOptions.IgnoreCase })
    {
        foreach ((string source, string suffix) in new[] { ("Strasse", "\0"), ("hello world", "\u200D"), ("abcé", "\u00AD\u00AD"), ("x", "\0\0\0") })
        {
            bool isSuffix = compare.IsSuffix(source, suffix, options, out int suffixLength);
            bool isPrefix = compare.IsPrefix(source, suffix, options, out int prefixLength);
            Console.WriteLine($"[{(cultureName.Length == 0 ? "invariant" : cultureName)}, {options}] source=\"{Escape(source)}\" suffix=\"{Escape(suffix)}\": " +
                $"IsSuffix={isSuffix} matchLength={suffixLength} (IsPrefix={isPrefix} matchLength={prefixLength})");
            reproduced |= isSuffix && suffixLength != 0;
        }
    }
}

Console.WriteLine("Expected: an ignorable suffix matches with matchLength 0, the same as IsPrefix reports for the same value.");
Console.WriteLine(reproduced ? "REPRODUCED" : "NOT REPRODUCED");

static string Escape(string s) => string.Concat(s.Select(c => c is >= ' ' and < '\x7F' ? c.ToString() : $"\\u{(int)c:X4}"));
