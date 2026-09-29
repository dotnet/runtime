// Finding: on ICU, IdnMapping.GetUnicode throws ArgumentException ("Decoded string is not a valid IDN name") for plain ASCII
// host names with "--" in the third and fourth positions of a label, such as "ab--cd.example" or the real-world YouTube CDN
// shape "r3---sn-abcd.googlevideo.com", even though GetAscii accepts the same name and returns it unchanged. So
// GetUnicode(GetAscii(x)) throws for inputs GetAscii considers valid.
//
// Root cause (src/native/libs/System.Globalization.Native/pal_idna.c): GlobalizationNative_ToAscii masks out
// UIDNA_ERROR_HYPHEN_3_4 "to have a consistent behavior with Windows", but GlobalizationNative_ToUnicode doesn't, so ICU's
// UTS #46 CheckHyphens error only fails the ToUnicode direction. TryGetUnicode behaves the same as GetUnicode.
// Run: dotnet run 52-IdnMapping-GetUnicode-Hyphen34.cs   (on Linux or macOS; Windows uses NLS)
using System.Globalization;

if (OperatingSystem.IsWindows())
{
    Console.WriteLine("This repro targets the ICU implementation (Linux/macOS). NOT REPRODUCED");
    return;
}

bool reproduced = false;
var idn = new IdnMapping();
foreach (string host in new[] { "ab--cd.example", "r3---sn-abcd.googlevideo.com", "le--bcher.example" })
{
    string ascii = idn.GetAscii(host);
    string unicode;
    try
    {
        unicode = "\"" + idn.GetUnicode(ascii) + "\"";
    }
    catch (ArgumentException ex)
    {
        unicode = $"threw ArgumentException: {ex.Message}";
        reproduced = true;
    }

    Console.WriteLine($"GetAscii(\"{host}\") = \"{ascii}\", GetUnicode of that {unicode}");
}

Console.WriteLine("Expected: GetUnicode accepts what GetAscii produced (an all-ASCII name without xn-- labels maps to itself).");
Console.WriteLine(reproduced ? "REPRODUCED" : "NOT REPRODUCED");
