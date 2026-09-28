// Finding: StringWithQualityHeaderValue keeps a quality value with more than three decimal places, but its ToString formats the
// quality with "0.0##" (at most three decimals), so the value doesn't round-trip through its own string form. Both the parser and
// the public constructor accept "q=0.1234" / new StringWithQualityHeaderValue("gzip", 0.1234) and report Quality = 0.1234, while
// ToString() gives "gzip; q=0.123", which parses back to 0.123. RFC 9110 defines the quality value as at most three digits after
// the decimal point, so the parser and constructor should reject or round to three places to match ToString; as it stands, an
// Accept-Encoding built from these values is re-serialized with a different weight than the object reports.
// Run: dotnet run 46-StringWithQualityHeaderValue-QualityRoundTrip.cs
using System.Net.Http.Headers;

bool reproduced = false;

Check("Parse(\"gzip; q=0.1234\")", () => StringWithQualityHeaderValue.TryParse("gzip; q=0.1234", out StringWithQualityHeaderValue? v) ? v : null);
Check("new StringWithQualityHeaderValue(\"gzip\", 0.1234)", () => new StringWithQualityHeaderValue("gzip", 0.1234));
Check("new StringWithQualityHeaderValue(\"br\", 0.99999)", () => new StringWithQualityHeaderValue("br", 0.99999));

Console.WriteLine(reproduced ? "REPRODUCED" : "NOT REPRODUCED");

void Check(string label, Func<StringWithQualityHeaderValue?> make)
{
    StringWithQualityHeaderValue? value = make();
    if (value is null)
    {
        Console.WriteLine($"{label}: (did not produce a value)");
        return;
    }

    string formatted = value.ToString();
    bool roundTrips = StringWithQualityHeaderValue.TryParse(formatted, out StringWithQualityHeaderValue? reparsed) && value.Equals(reparsed);
    reproduced |= !roundTrips;
    Console.WriteLine($"{label}: Quality={value.Quality}, ToString=\"{formatted}\" (reparsed Quality={reparsed?.Quality}), round-trips={roundTrips}");
}
