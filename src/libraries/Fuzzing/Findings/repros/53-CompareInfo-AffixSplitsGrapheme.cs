// Finding: on ICU, culture-aware StartsWith/EndsWith accept a prefix or suffix that splits a grapheme cluster, while
// IndexOf/LastIndexOf (correctly) don't, so a string can "start with" something it doesn't "contain". In every culture, with
// CompareOptions.None or IgnoreCase:
//   "क\u093F".StartsWith("क", CurrentCulture) = true     but  "क\u093F".IndexOf("क", CurrentCulture)      = -1  (Hindi KA + vowel sign I)
//   "க\u0BBF".StartsWith("க", CurrentCulture) = true     but  IndexOf = -1                               (Tamil)
//   "가나".StartsWith("ᄀ")         = true     but  IndexOf = -1  (the syllable's leading conjoining jamo)
//   "e\u0301".EndsWith("\u0301")        = true     but  "e\u0301".LastIndexOf("\u0301") = -1  (even plain Latin, backwards)
//   "เก".StartsWith("ก")                  = true     but  IndexOf = -1  (Thai: the collation reorders the prevowel with the consonant)
//   "ﬁx".StartsWith("f", CurrentCultureIgnoreCase) = true  but  IndexOf(..., CurrentCultureIgnoreCase) = -1  (ligature; also Ⅸ/I, ǆ/d)
//   "ŉ".EndsWith("n", CurrentCultureIgnoreCase) = true  but  LastIndexOf = -1, and IsSuffix("ŉ\0", "n", IgnoreCase, out len)
//   reports len = 1, a slice that holds only the "\0" and not the matched text
//   "Straße".EndsWith("se", CurrentCultureIgnoreCase) = true  but  Contains("se") = false; and "Fuß".EndsWith("s") is true
//   while "groß".EndsWith("ss") is false (ß collates as "ss", and the backward check stops halfway through it)
//
// StartsWith/EndsWith go through SimpleAffix in pal_collation.c, which walks raw collation elements. Going forward it refuses a
// match followed by a *nonspacing* mark (it checks for an element with primary weight 0 and a secondary weight, which is why
// "e\u0301".StartsWith("e") is false), but Indic vowel signs, viramas and Hangul vowel/final jamo have primary weights, so the
// check doesn't fire. The same happens when the prefix ends inside one character's expansion (a Thai prevowel+consonant
// contraction, or a ligature once IgnoreCase drops the tertiary difference). Going backward there's no such check at all. IndexOf/LastIndexOf use ICU usearch, which only accepts
// matches on grapheme boundaries. With IgnoreWidth or IgnoreKanaType (the usearch-based ComplexStartsWith/ComplexEndsWith path)
// StartsWith/EndsWith agree with IndexOf.
// Run: dotnet run 53-CompareInfo-AffixSplitsGrapheme.cs   (on Linux or macOS; Windows uses NLS)
using System.Globalization;

if (OperatingSystem.IsWindows())
{
    Console.WriteLine("This repro targets the ICU implementation (Linux/macOS). NOT REPRODUCED");
    return;
}

bool reproduced = false;
(string Text, string Prefix, string Suffix, string What)[] cases =
[
    ("क\u093F", "क", "\u093F", "Hindi KA + vowel sign I"),
    ("க\u0BBF", "க", "\u0BBF", "Tamil KA + vowel sign I"),
    ("क\u094Dष", "क", "ष", "Hindi conjunct KSSA"),
    ("가나", "ᄀ", "ᅡ", "Hangul syllables vs conjoining jamo"),
    ("e\u0301", "e", "\u0301", "Latin e + combining acute"),
    ("\u0E40\u0E01", "\u0E01", "\u0E40", "Thai SARA E + KO KAI"),
];

foreach (string cultureName in new[] { "", "en-US", "hi-IN", "ko-KR" })
{
    CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
    foreach ((string text, string prefix, string suffix, string what) in cases)
    {
        bool startsWith = text.StartsWith(prefix, StringComparison.CurrentCulture);
        int indexOf = text.IndexOf(prefix, StringComparison.CurrentCulture);
        bool endsWith = text.EndsWith(suffix, StringComparison.CurrentCulture);
        int lastIndexOf = text.LastIndexOf(suffix, StringComparison.CurrentCulture);
        Console.WriteLine($"[{(cultureName.Length == 0 ? "invariant" : cultureName)}] {what}: StartsWith={startsWith} IndexOf={indexOf}; EndsWith={endsWith} LastIndexOf={lastIndexOf}");
        reproduced |= (startsWith && indexOf != 0) || (endsWith && lastIndexOf < 0);
    }
}

// Ligatures and compatibility characters with IgnoreCase.
CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
foreach ((string text, string prefix) in new[] { ("\uFB01x", "f"), ("\u2168", "I"), ("\u01C6x", "d") })
{
    bool startsWith = text.StartsWith(prefix, StringComparison.CurrentCultureIgnoreCase);
    int indexOf = text.IndexOf(prefix, StringComparison.CurrentCultureIgnoreCase);
    Console.WriteLine($"[en-US, IgnoreCase] \"{Escape(text)}\": StartsWith(\"{prefix}\")={startsWith} IndexOf={indexOf}");
    reproduced |= startsWith && indexOf != 0;
}

{
    bool endsWith = "\u0149".EndsWith("n", StringComparison.CurrentCultureIgnoreCase);
    int lastIndexOf = "\u0149".LastIndexOf("n", StringComparison.CurrentCultureIgnoreCase);
    bool isSuffix = CultureInfo.CurrentCulture.CompareInfo.IsSuffix("\u0149\0", "n", CompareOptions.IgnoreCase, out int suffixLength);
    Console.WriteLine($"[en-US, IgnoreCase] \"\\u0149\": EndsWith(\"n\")={endsWith} LastIndexOf={lastIndexOf}; IsSuffix(\"\\u0149\\0\", \"n\") = {isSuffix}, matchLength {suffixLength}");
    reproduced |= endsWith && lastIndexOf < 0;
}

// German sharp s, which collates as "ss".
foreach ((string text, string suffix) in new[] { ("Straße", "se"), ("Fuß", "s"), ("groß", "ss") })
{
    bool endsWith = text.EndsWith(suffix, StringComparison.CurrentCultureIgnoreCase);
    int lastIndexOf = text.LastIndexOf(suffix, StringComparison.CurrentCultureIgnoreCase);
    Console.WriteLine($"[en-US, IgnoreCase] \"{text}\".EndsWith(\"{suffix}\")={endsWith} LastIndexOf={lastIndexOf}");
    reproduced |= endsWith && lastIndexOf < 0;
}

Console.WriteLine("Expected: StartsWith(x) implies IndexOf(x) == 0 and EndsWith(x) implies LastIndexOf(x) >= 0.");
Console.WriteLine(reproduced ? "REPRODUCED" : "NOT REPRODUCED");

static string Escape(string s) => string.Concat(s.Select(c => c is >= ' ' and < '\x7F' ? c.ToString() : $"\\u{(int)c:X4}"));
