// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.Headers;
using System.Text;

namespace DotnetFuzzing.Fuzzers;

/// <summary>
/// Fuzzes the structured HTTP header value parsers in System.Net.Http.Headers (MediaType, CacheControl, ContentRange,
/// ContentDisposition, EntityTag, NameValue, Product, Range, Via, Warning, Authentication, RetryCondition and the quality
/// variants). TryParse must only ever return false for bad input, never throw; whatever parses must round-trip through its own
/// ToString (parsing the formatted form gives an equal value and the same string again); and the char-span overload must agree
/// with the string overload.
/// </summary>
/// <remarks>Input layout: [0] header type selector, [1..] the text, built from a palette of header tokens.</remarks>
internal sealed class HttpHeaderValuesFuzzer : IFuzzer
{
    public string[] TargetAssemblies { get; } = ["System.Net.Http"];
    public string[] TargetCoreLibPrefixes => [];

    private static readonly string[] s_palette =
    [
        "text/html", "application/json", "charset=utf-8", "; ", "q=0.8", "=", "\"", "gzip", "bytes", "0-499", "/1234", "*",
        "max-age=3600", "no-cache", "no-store", ", ", "W/", "\"etag\"", "Basic ", "realname", " ", "\t", "name", "value",
        "1.1", "host:80", "(comment)", "199", "Miscellaneous warning", "boundary", "filename", "utf-8''a.txt", "\\", "\r\n",
        "identity", "chunked", "close", "max-stale", "must-revalidate", "private", "public", "0", ".5", "-", "+", "%", "é",
    ];

    private static readonly (string Name, Func<string, (bool Ok, object? Value, string? Formatted)> Parse)[] s_parsers =
    [
        ("MediaTypeHeaderValue", t => { bool ok = MediaTypeHeaderValue.TryParse(t, out MediaTypeHeaderValue? v); return (ok, v, v?.ToString()); }),
        ("MediaTypeWithQualityHeaderValue", t => { bool ok = MediaTypeWithQualityHeaderValue.TryParse(t, out MediaTypeWithQualityHeaderValue? v); return (ok, v, v?.ToString()); }),
        ("CacheControlHeaderValue", t => { bool ok = CacheControlHeaderValue.TryParse(t, out CacheControlHeaderValue? v); return (ok, v, v?.ToString()); }),
        ("ContentRangeHeaderValue", t => { bool ok = ContentRangeHeaderValue.TryParse(t, out ContentRangeHeaderValue? v); return (ok, v, v?.ToString()); }),
        ("ContentDispositionHeaderValue", t => { bool ok = ContentDispositionHeaderValue.TryParse(t, out ContentDispositionHeaderValue? v); return (ok, v, v?.ToString()); }),
        ("EntityTagHeaderValue", t => { bool ok = EntityTagHeaderValue.TryParse(t, out EntityTagHeaderValue? v); return (ok, v, v?.ToString()); }),
        ("NameValueHeaderValue", t => { bool ok = NameValueHeaderValue.TryParse(t, out NameValueHeaderValue? v); return (ok, v, v?.ToString()); }),
        ("ProductHeaderValue", t => { bool ok = ProductHeaderValue.TryParse(t, out ProductHeaderValue? v); return (ok, v, v?.ToString()); }),
        ("ProductInfoHeaderValue", t => { bool ok = ProductInfoHeaderValue.TryParse(t, out ProductInfoHeaderValue? v); return (ok, v, v?.ToString()); }),
        ("RangeHeaderValue", t => { bool ok = RangeHeaderValue.TryParse(t, out RangeHeaderValue? v); return (ok, v, v?.ToString()); }),
        ("RangeConditionHeaderValue", t => { bool ok = RangeConditionHeaderValue.TryParse(t, out RangeConditionHeaderValue? v); return (ok, v, v?.ToString()); }),
        ("ViaHeaderValue", t => { bool ok = ViaHeaderValue.TryParse(t, out ViaHeaderValue? v); return (ok, v, v?.ToString()); }),
        ("WarningHeaderValue", t => { bool ok = WarningHeaderValue.TryParse(t, out WarningHeaderValue? v); return (ok, v, v?.ToString()); }),
        ("AuthenticationHeaderValue", t => { bool ok = AuthenticationHeaderValue.TryParse(t, out AuthenticationHeaderValue? v); return (ok, v, v?.ToString()); }),
        ("RetryConditionHeaderValue", t => { bool ok = RetryConditionHeaderValue.TryParse(t, out RetryConditionHeaderValue? v); return (ok, v, v?.ToString()); }),
        ("StringWithQualityHeaderValue", t => { bool ok = StringWithQualityHeaderValue.TryParse(t, out StringWithQualityHeaderValue? v); return (ok, v, v?.ToString()); }),
        ("TransferCodingWithQualityHeaderValue", t => { bool ok = TransferCodingWithQualityHeaderValue.TryParse(t, out TransferCodingWithQualityHeaderValue? v); return (ok, v, v?.ToString()); }),
    ];

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 2)
        {
            return;
        }

        var builder = new StringBuilder();
        foreach (byte b in bytes.Slice(1))
        {
            builder.Append(b < 0x80 ? ((char)b).ToString() : s_palette[b % s_palette.Length]);
        }

        string text = builder.ToString();
        if (text.Length > 2000)
        {
            return;
        }

        (string name, Func<string, (bool, object?, string?)> parse) = s_parsers[bytes[0] % s_parsers.Length];

        bool ok;
        object? value;
        string? formatted;
        try
        {
            (ok, value, formatted) = parse(text);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"{name}.TryParse threw {ex.GetType().Name} for '{Escape(text)}': {ex.Message}", ex);
        }

        if (!ok || formatted is null)
        {
            return;
        }

        // Round-trip: the formatted form parses to an equal value and formats the same way.
        (bool reparsedOk, object? reparsed, string? reformatted) = parse(formatted);
        Check(reparsedOk, () => $"{name} '{Escape(text)}' formats as '{Escape(formatted)}', which doesn't parse");
        Check(value!.Equals(reparsed), () => $"{name} '{Escape(text)}' formats as '{Escape(formatted)}', which parses to a different value");
        Check(reformatted == formatted, () => $"{name} '{Escape(text)}' isn't a formatting fixed point: '{Escape(formatted)}' -> '{Escape(reformatted)}'");
        Check(value.GetHashCode() == reparsed!.GetHashCode(), () => $"{name} '{Escape(text)}' and its round-trip have different hash codes");
    }

    private static string Escape(string? text) =>
        text is null ? "(null)" : string.Concat(text.Take(300).Select(c => c is >= ' ' and < '\x7F' ? c.ToString() : $"\\u{(int)c:X4}"));

    private static void Check(bool condition, Func<string> message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message());
        }
    }
}
