// Observation: ContentDisposition.ToString() writes a non-ASCII parameter value as an RFC 2047 encoded-word
// ("=?utf-8?B?...?=", folded with CRLF when long), but the ContentDisposition parser doesn't decode encoded-words, so parsing
// the formatted header gives a different parameter value than the original.
// Run: dotnet run 24-ContentDisposition-EncodedWordRoundTrip.cs
using System.Net.Mime;

var original = new ContentDisposition("attachment") { FileName = "na\u00EFve.txt" };
string formatted = original.ToString();
var reparsed = new ContentDisposition(formatted);
Console.WriteLine($"FileName            = {original.FileName}");
Console.WriteLine($"ToString()          = {formatted}");
Console.WriteLine($"reparsed FileName   = {reparsed.FileName}");
Console.WriteLine(original.FileName != reparsed.FileName ? "REPRODUCED: non-ASCII parameters don't round-trip." : "NOT REPRODUCED");
