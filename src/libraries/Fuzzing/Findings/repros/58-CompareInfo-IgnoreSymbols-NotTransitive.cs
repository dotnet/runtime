// Finding: on ICU, CompareInfo.Compare with CompareOptions.IgnoreSymbols is not transitive, and it disagrees with the sort keys
// that CompareInfo.GetSortKey produces for the same options. With a = "-\u0001́", b = "-́", c = "-":
//   Compare(a, b) = 0,  Compare(b, c) = 0,  but  Compare(a, c) = 1
// while the three sort keys are all equal. The pattern is a symbol (ignored with IgnoreSymbols), then a completely ignorable
// control character (U+0000, U+0001, U+0004...), then a combining mark. In shifted collation a mark that follows an ignored
// symbol is ignored too. The sort key keeps applying that across the control character; Compare (ucol_strcoll) doesn't, so it
// counts the mark. Some pairs even come out in opposite order: Compare("^^̈á", "^\u0000́a-") is -1 while the
// sort keys say +1. SortKey is documented to order strings the same way Compare does.
// Consequence: a non-transitive comparer breaks sorting. Array.Sort with CompareInfo.GetStringComparer(IgnoreSymbols) returns
// arrays that aren't sorted according to that same comparer, depending on the input order. Hashing is not affected: no pair was
// found where Compare returns 0 but the sort keys (and so StringComparer.GetHashCode) differ.
// Run: dotnet run 58-CompareInfo-IgnoreSymbols-NotTransitive.cs   (on Linux or macOS; Windows uses NLS)
using System.Globalization;

if (OperatingSystem.IsWindows())
{
    Console.WriteLine("This repro targets the ICU implementation (Linux/macOS). NOT REPRODUCED");
    return;
}

const CompareOptions options = CompareOptions.IgnoreSymbols;
CompareInfo compare = CultureInfo.GetCultureInfo("en-US").CompareInfo;
string a = "-\u0001́", b = "-́", c = "-";

int ab = compare.Compare(a, b, options), bc = compare.Compare(b, c, options), ac = compare.Compare(a, c, options);
Console.WriteLine($"Compare(a, b) = {ab}, Compare(b, c) = {bc}, Compare(a, c) = {ac}   (a = \"-\\u0001\\u0301\", b = \"-\\u0301\", c = \"-\")");
Console.WriteLine($"Sort keys: a vs b = {Key(a, b)}, b vs c = {Key(b, c)}, a vs c = {Key(a, c)}");
bool reproduced = ab == 0 && bc == 0 && ac != 0;

string x = "^^̈á", y = "^\u0000́a-";
Console.WriteLine($"Compare(\"^^\\u0308a\\u0301\", \"^\\u0000\\u0301a-\") = {Math.Sign(compare.Compare(x, y, options))}, sort keys say {Key(x, y)}");

// Sort the same six strings in every input order and count results that aren't sorted by the comparer itself.
StringComparer comparer = compare.GetStringComparer(options);
string[] items = ["-\u0001́", "-", "-́", "-\u0001́x", "-x", "-́x"];
int unsorted = 0, total = 0;
foreach (string[] permutation in Permutations(items))
{
    total++;
    Array.Sort(permutation, comparer);
    bool ok = true;
    for (int i = 0; i < permutation.Length; i++)
        for (int j = i + 1; j < permutation.Length; j++)
            ok &= comparer.Compare(permutation[i], permutation[j]) <= 0;
    unsorted += ok ? 0 : 1;
}

Console.WriteLine($"Array.Sort: {unsorted} of {total} input orders produce an array that isn't sorted by the comparer");
Console.WriteLine("Expected: Compare is transitive and orders strings the same way their sort keys do.");
Console.WriteLine(reproduced ? "REPRODUCED" : "NOT REPRODUCED");

int Key(string s, string t) => Math.Sign(SortKey.Compare(compare.GetSortKey(s, options), compare.GetSortKey(t, options)));

static IEnumerable<string[]> Permutations(string[] items)
{
    if (items.Length <= 1)
    {
        yield return items;
        yield break;
    }

    for (int i = 0; i < items.Length; i++)
    {
        string[] rest = [.. items[..i], .. items[(i + 1)..]];
        foreach (string[] tail in Permutations(rest))
        {
            yield return [items[i], .. tail];
        }
    }
}
