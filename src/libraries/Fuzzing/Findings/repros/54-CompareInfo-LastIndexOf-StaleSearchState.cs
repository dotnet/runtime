// Finding: on ICU, culture-aware backward searches give different answers for the *same* input depending on what the previous
// search did, because the native layer reuses cached ICU string-search objects. In a fresh process, calling
// CompareInfo.LastIndexOf(source, value, options, out int matchLength) twice with identical arguments:
//   source "\t\u0301q", value "\u0301q":   1st call index 1, matchLength 2;   2nd call index 1, matchLength -1
// A negative matchLength breaks the documented contract, and source.AsSpan(index, matchLength) / Substring throws on it. The
// stale state also changes the *index*: for source "unu\0\u0345\0\0\0\0\0\u0010\0\0\u0345" and value "\u0345\0\0" with
// IgnoreCase, the first LastIndexOf returns 13 and the second returns 4 (not the last occurrence). And IsSuffix, which for
// IgnoreKanaType/IgnoreWidth computes idx + usearch_getMatchedLength, flips from true to false on the second call.
// The trigger is a value that starts with a combining mark preceded in the source by a control or whitespace character (\t, \n,
// \r, \v, \f, U+0085, U+2028), where the mark starts a new grapheme. Forward searches (IndexOf) are unaffected, and a forward
// search in between "resets" things, so results depend on call history.
//
// The cache is GetSearchIteratorUsingCollator in src/native/libs/System.Globalization.Native/pal_collation.c: the first search
// per options value opens a fresh UStringSearch; later ones borrow it and only call usearch_setText/usearch_setPattern.
// usearch_last on the reused object then reports a stale match. Resetting the borrowed iterator (usearch_reset) or rejecting a
// negative usearch_getMatchedLength would be the place to fix it.
// Run: dotnet run 54-CompareInfo-LastIndexOf-StaleSearchState.cs   (on Linux or macOS; Windows uses NLS)
using System.Globalization;

if (OperatingSystem.IsWindows())
{
    Console.WriteLine("This repro targets the ICU implementation (Linux/macOS). NOT REPRODUCED");
    return;
}

bool reproduced = false;
CompareInfo compare = CultureInfo.GetCultureInfo("en-US").CompareInfo;

foreach ((string source, string value, CompareOptions options) in new[]
{
    ("\t\u0301q", "\u0301q", CompareOptions.None),
    ("line\n\u0308x", "\u0308x", CompareOptions.None),
    ("unu\0\u0345\0\0\0\0\0\u0010\0\0\u0345", "\u0345\0\0", CompareOptions.IgnoreCase),
})
{
    var results = new List<string>();
    int? firstIndex = null;
    for (int call = 0; call < 3; call++)
    {
        int index = compare.LastIndexOf(source, value, options, out int matchLength);
        firstIndex ??= index;
        string slice;
        try { slice = $"\"{Escape(source.Substring(index, matchLength))}\""; }
        catch (ArgumentOutOfRangeException) { slice = "Substring(index, matchLength) throws"; }
        results.Add($"({index}, {matchLength}, {slice})");
        reproduced |= matchLength < 0 || index != firstIndex;
    }

    Console.WriteLine($"{options} LastIndexOf(\"{Escape(source)}\", \"{Escape(value)}\") x3: {string.Join(", ", results)}");
}

{
    string source = "\u000B\u0301q", value = "\u0301q";
    bool first = compare.IsSuffix(source, value, CompareOptions.IgnoreKanaType);
    bool second = compare.IsSuffix(source, value, CompareOptions.IgnoreKanaType);
    Console.WriteLine($"IgnoreKanaType IsSuffix(\"{Escape(source)}\", \"{Escape(value)}\") twice: {first}, {second}");
    reproduced |= first != second;
}

Console.WriteLine("Expected: identical calls return identical results, and matchLength is never negative.");
Console.WriteLine(reproduced ? "REPRODUCED" : "NOT REPRODUCED");

static string Escape(string s) => string.Concat(s.Select(c => c is >= ' ' and < '\x7F' ? c.ToString() : $"\\u{(int)c:X4}"));
