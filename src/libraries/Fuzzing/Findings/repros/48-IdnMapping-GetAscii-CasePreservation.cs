// Finding: IdnMapping.GetAscii(string) and GetUnicode(string) keep the caller's casing when ICU's answer differs from the input
// only by case, while the span overloads TryGetAscii/TryGetUnicode (and the index/count string overloads) return ICU's lowercased
// output. The ICU path (IdnMapping.Icu.cs) always gets a lowercased result from uidna_nameToASCII, then GetStringForOutput hands
// back the *original* string whenever the output matches it with Ordinal.EqualsIgnoreCase. That shortcut is meant to avoid an
// allocation but it also changes the answer, and it only fires when the whole string is converted. So on Linux/macOS:
//   GetAscii("Example.COM")          = "Example.COM"
//   GetAscii("xExample.COM", 1)      = "example.com"
//   TryGetAscii("Example.COM", ...)  = "example.com"
//   GetAscii("xn--BCHER-KVA.de")     = "xn--BCHER-KVA.de"  (an uppercase punycode label passes through as-is)
// Callers that compare the result ordinally (host allow-lists, cache keys) get different answers depending on the overload.
// Run: dotnet run 48-IdnMapping-GetAscii-CasePreservation.cs   (on Linux or macOS; Windows uses NLS)
using System.Globalization;

if (OperatingSystem.IsWindows())
{
    Console.WriteLine("This repro targets the ICU implementation (Linux/macOS). NOT REPRODUCED");
    return;
}

var idn = new IdnMapping();
bool reproduced = false;
char[] buffer = new char[256];

foreach (string input in new[] { "Example.COM", "M.", "xn--BCHER-KVA.de" })
{
    string whole = idn.GetAscii(input);
    string sliced = idn.GetAscii("x" + input, 1);
    idn.TryGetAscii(input, buffer, out int written);
    string span = new string(buffer, 0, written);
    Console.WriteLine($"GetAscii(\"{input}\") = \"{whole}\", GetAscii(\"x{input}\", 1) = \"{sliced}\", TryGetAscii = \"{span}\"");
    reproduced |= whole != span || whole != sliced;
}

{
    string input = "Example.COM";
    string whole = idn.GetUnicode(input);
    idn.TryGetUnicode(input, buffer, out int written);
    string span = new string(buffer, 0, written);
    Console.WriteLine($"GetUnicode(\"{input}\") = \"{whole}\", TryGetUnicode = \"{span}\"");
    reproduced |= whole != span;
}

Console.WriteLine("Expected: every overload returns the same string for the same input.");
Console.WriteLine(reproduced ? "REPRODUCED" : "NOT REPRODUCED");
