// Finding: CompositeFormat.Parse doesn't bound the argument-hole index, so it accepts indices that overflow Int32, unlike
// string.Format, which rejects an index above 1,000,000 with FormatException. The digit loop in TryParseLiterals does
// index = index * 10 + ch - '0' with no overflow check, so "{2147483648}" parses to a hole with a wrapped ArgIndex and the
// resulting CompositeFormat reports MinimumArgumentCount = 0 (the wrapped index is negative and skipped), while "{9999999999}"
// reports a wrapped positive count like 1410065408. Two consequences: string.Format(compositeFormat, args) throws
// IndexOutOfRangeException instead of the documented FormatException, and MinimumArgumentCount lies about how many arguments the
// format needs. string.Format(string, args) rejects all of these with FormatException.
// Run: dotnet run 47-CompositeFormat-IndexOverflow.cs
using System.Globalization;
using System.Text;

bool reproduced = false;
object?[] values = { 1, 2, 3 };

foreach (string format in new[] { "{2147483648}", "{9999999999}", "{111111111117}" })
{
    string stringFormat;
    try { string.Format(CultureInfo.InvariantCulture, format, values); stringFormat = "succeeded"; }
    catch (FormatException) { stringFormat = "FormatException"; }
    catch (Exception ex) { stringFormat = ex.GetType().Name; }

    CompositeFormat? composite = null;
    string parseResult;
    try { composite = CompositeFormat.Parse(format); parseResult = $"parsed, MinimumArgumentCount={composite.MinimumArgumentCount}"; }
    catch (FormatException) { parseResult = "FormatException"; }

    string composeResult = "n/a";
    if (composite is not null)
    {
        try { string.Format(CultureInfo.InvariantCulture, composite, values); composeResult = "succeeded"; }
        catch (Exception ex) { composeResult = ex.GetType().Name; }
        reproduced |= composeResult == "IndexOutOfRangeException" || composite.MinimumArgumentCount <= 0;
    }

    Console.WriteLine($"{format,-16}: string.Format -> {stringFormat}; CompositeFormat.Parse -> {parseResult}; format(cf) -> {composeResult}");
}

Console.WriteLine(reproduced ? "REPRODUCED" : "NOT REPRODUCED");
