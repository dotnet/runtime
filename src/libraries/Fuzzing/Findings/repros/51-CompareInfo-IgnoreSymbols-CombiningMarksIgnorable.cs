// Finding: on ICU, with CompareOptions.IgnoreSymbols, a search value made only of combining marks (optionally mixed with symbols
// or punctuation) is treated as *ignorable* by IndexOf, LastIndexOf and IsPrefix, so it "matches" everywhere:
//   CompareInfo.IndexOf("abc", "\u0308", IgnoreSymbols, out len) = 0, len = 0
//   CompareInfo.LastIndexOf("abc", "\u0308", IgnoreSymbols)      = 3
//   CompareInfo.IsPrefix("abc", "\u0308", IgnoreSymbols)         = true
// while IsSuffix returns false and Compare("", "\u0308", IgnoreSymbols) says the value is *not* ignorable (it sorts after the
// empty string at the secondary level). With CompareOptions.None the same searches correctly return -1/false.
//
// Root cause: IgnoreSymbols turns on alternate=shifted in the native collator. The search APIs use ICU usearch, whose getCE()
// drops any collation element numerically below variableTop when shifting is on. A combining mark's CE has primary weight 0,
// so its 32-bit value is below variableTop and it gets discarded as if it were a symbol, leaving an empty pattern. ucol_strcoll
// (Compare) handles shifted mode correctly, so the two disagree.
// Thai is worse: CLDR's Thai collation turns on alternate=shifted by default, so th-TH hits this with CompareOptions.None and
// through the ordinary string APIs. With CurrentCulture = th-TH, "abc".Contains("\u0E48", StringComparison.CurrentCulture)
// (U+0E48 THAI CHARACTER MAI EK, a tone mark) is true and IndexOf returns 0, so searching Thai text for a tone mark always
// "finds" it. The marks also make forward and backward search disagree (IndexOf finds a match that LastIndexOf doesn't).
// Run: dotnet run 51-CompareInfo-IgnoreSymbols-CombiningMarksIgnorable.cs   (on Linux or macOS; Windows uses NLS)
using System.Globalization;

if (OperatingSystem.IsWindows())
{
    Console.WriteLine("This repro targets the ICU implementation (Linux/macOS). NOT REPRODUCED");
    return;
}

bool reproduced = false;
foreach (string cultureName in new[] { "", "en-US", "de-DE" })
{
    CompareInfo compare = CultureInfo.GetCultureInfo(cultureName).CompareInfo;
    foreach ((string source, string value) in new[] { ("abc", "\u0308"), ("xyz", "\u0301\u0301"), ("hello", "\u031B-$") })
    {
        int index = compare.IndexOf(source, value, CompareOptions.IgnoreSymbols, out int matchLength);
        int lastIndex = compare.LastIndexOf(source, value, CompareOptions.IgnoreSymbols);
        bool isPrefix = compare.IsPrefix(source, value, CompareOptions.IgnoreSymbols);
        bool isSuffix = compare.IsSuffix(source, value, CompareOptions.IgnoreSymbols);
        int vsEmpty = compare.Compare("", value, CompareOptions.IgnoreSymbols);
        int noneIndex = compare.IndexOf(source, value, CompareOptions.None);
        Console.WriteLine($"[{(cultureName.Length == 0 ? "invariant" : cultureName)}] source=\"{source}\" value=\"{Escape(value)}\" IgnoreSymbols: IndexOf={index} (len {matchLength}) " +
            $"LastIndexOf={lastIndex} IsPrefix={isPrefix} IsSuffix={isSuffix} Compare(\"\", value)={vsEmpty}; None: IndexOf={noneIndex}");
        reproduced |= index == 0 && matchLength == 0 && vsEmpty != 0;
    }
}

// Thai culture, default options, plain string APIs.
CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("th-TH");
foreach (string value in new[] { "\u0E48", "\u0308" })
{
    bool contains = "abc".Contains(value, StringComparison.CurrentCulture);
    int index = "abc".IndexOf(value, StringComparison.CurrentCulture);
    int vsEmpty = string.Compare("", value, StringComparison.CurrentCulture);
    Console.WriteLine($"[th-TH, CurrentCulture] \"abc\".Contains(\"{Escape(value)}\")={contains} IndexOf={index} string.Compare(\"\", value)={vsEmpty}");
    reproduced |= contains && vsEmpty != 0;
}

Console.WriteLine("Expected: a value that doesn't compare equal to \"\" isn't ignorable, so IndexOf/IsPrefix don't match it in a string that doesn't contain it.");
Console.WriteLine(reproduced ? "REPRODUCED" : "NOT REPRODUCED");

static string Escape(string s) => string.Concat(s.Select(c => c is >= ' ' and < '\x7F' ? c.ToString() : $"\\u{(int)c:X4}"));
