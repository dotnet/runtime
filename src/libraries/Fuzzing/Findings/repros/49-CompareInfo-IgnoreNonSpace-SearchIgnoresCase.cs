// Finding: on ICU, CompareOptions.IgnoreNonSpace (without IgnoreCase) is case-sensitive in Compare but case-insensitive in
// IndexOf/LastIndexOf/IsPrefix/IsSuffix. For IgnoreNonSpace the native collator runs at primary strength with UCOL_CASE_LEVEL
// turned on (pal_collation.c), which ucol_strcoll honours. The search APIs go through usearch, which matches collation elements
// masked to the primary weight and never looks at the case level, so "a" and "A" (and "ä" and "A") match there even though
// Compare says they're different. With the invariant or an en-* culture, all-ASCII input takes the managed ordinal fast path in
// CompareInfo.Icu.cs, which *is* case-sensitive, so the answer also depends on whether the text happens to be ASCII:
//   en-US: IndexOf("a",  "A", IgnoreNonSpace) = -1   (managed fast path)
//   en-US: IndexOf("ä",  "A", IgnoreNonSpace) =  0   (ICU usearch)      but Compare("ä", "A", IgnoreNonSpace) != 0
//   ja-JP: IndexOf("a",  "A", IgnoreNonSpace) =  0   (ICU usearch)      but Compare("a", "A", IgnoreNonSpace) != 0
// So source.IndexOf(value) can return a match whose matched text doesn't compare equal to value under the same options.
// Run: dotnet run 49-CompareInfo-IgnoreNonSpace-SearchIgnoresCase.cs   (on Linux or macOS; Windows uses NLS)
using System.Globalization;

if (OperatingSystem.IsWindows())
{
    Console.WriteLine("This repro targets the ICU implementation (Linux/macOS). NOT REPRODUCED");
    return;
}

const CompareOptions options = CompareOptions.IgnoreNonSpace;
bool reproduced = false;

foreach (string cultureName in new[] { "en-US", "ja-JP", "de-DE" })
{
    CompareInfo compare = CultureInfo.GetCultureInfo(cultureName).CompareInfo;
    foreach ((string source, string value) in new[] { ("a", "A"), ("ä", "A"), ("ä", "Å"), ("xǄy", "ǆ") })
    {
        int index = compare.IndexOf(source, value, options, out int matchLength);
        bool isPrefix = compare.IsPrefix(source, value, options);
        int lastIndex = compare.LastIndexOf(source, value, options);
        string matched = index >= 0 ? source.Substring(index, matchLength) : "";
        int compareMatched = index >= 0 ? compare.Compare(matched, value, options) : 0;
        Console.WriteLine($"{cultureName}: source={Escape(source)} value={Escape(value)} IndexOf={index} (len {matchLength}) LastIndexOf={lastIndex} IsPrefix={isPrefix} " +
            $"Compare(matched, value)={compareMatched} Compare(source, value)={compare.Compare(source, value, options)}");
        reproduced |= index >= 0 && compareMatched != 0;
    }
}

Console.WriteLine("Expected: any match IndexOf reports compares equal to the value under the same options (so \"a\" vs \"A\" never matches without IgnoreCase).");
Console.WriteLine(reproduced ? "REPRODUCED" : "NOT REPRODUCED");

static string Escape(string s) => "\"" + string.Concat(s.Select(c => c is >= ' ' and < '\x7F' ? c.ToString() : $"\\u{(int)c:X4}")) + "\"";
