// Finding: new ContentDisposition(string) and new ContentType(string) are documented to throw FormatException for input they
// can't parse, but a ContentDisposition parameter without a value at the end of the header throws IndexOutOfRangeException from
// ContentDisposition.ParseValue, and a repeated parameter name throws ArgumentException ("An item with the same key has already
// been added") from the parameter dictionary in both parsers.
// Code that parses untrusted MIME headers and catches FormatException crashes.
// Run: dotnet run 23-ContentDisposition-ParseExceptions.cs
using System.Net.Mime;

bool reproduced = false;
foreach (string header in new[] { "0000000000;Y", "attachment; size=100; size=100", "attachment; filename=\"a.txt\"" })
{
    string outcome;
    try { outcome = "parsed as " + new ContentDisposition(header); }
    catch (Exception ex)
    {
        outcome = ex.GetType().Name + ": " + ex.Message;
        reproduced |= ex is not FormatException;
    }

    Console.WriteLine($"new ContentDisposition(\"{header}\") -> {outcome}");
}

foreach (string header in new[] { "text/html; charset=utf-8; charset=utf-8" })
{
    string outcome;
    try { outcome = "parsed as " + new ContentType(header); }
    catch (Exception ex)
    {
        outcome = ex.GetType().Name + ": " + ex.Message;
        reproduced |= ex is not FormatException;
    }

    Console.WriteLine($"new ContentType(\"{header}\") -> {outcome}");
}

Console.WriteLine(reproduced ? "REPRODUCED: malformed headers throw undocumented exception types." : "NOT REPRODUCED");
