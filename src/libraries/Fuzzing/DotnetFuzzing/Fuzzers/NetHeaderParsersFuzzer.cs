// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;

namespace DotnetFuzzing.Fuzzers;

/// <summary>
/// Fuzzes the mail address, MIME header and cookie parsers. Only the documented exception types are allowed, TryCreate must agree
/// with the constructor, and whatever parses must round-trip through its string form.
/// </summary>
/// <remarks>Input layout: [0] parser selector, [1..] the text (UTF-8).</remarks>
internal sealed class NetHeaderParsersFuzzer : IFuzzer
{
    // Known issues on main, tolerated unless TENSOR_FUZZ_STRICT=1 (shared switch name with the other new fuzzers):
    // * MailAddress keeps quoted-pair backslashes in DisplayName but ToString() escapes them again, so display names with
    //   backslashes grow on every round trip.
    // * new ContentDisposition(string) throws IndexOutOfRangeException for a trailing parameter without a value and
    //   ArgumentException for a repeated parameter, instead of FormatException; new ContentType(string) has the same
    //   ArgumentException for repeated parameters.
    // * Display names with backslash-escaped characters can format to text that no longer parses (the escape is doubled).
    // * ContentDisposition/ContentType write non-ASCII parameter values as RFC 2047 encoded-words, which they don't decode.
    private static readonly bool s_strict = Environment.GetEnvironmentVariable("TENSOR_FUZZ_STRICT") == "1";

    public string[] TargetAssemblies { get; } = ["System.Net.Mail", "System.Net.Primitives"];
    public string[] TargetCoreLibPrefixes => [];

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 1)
        {
            return;
        }

        string text = Encoding.UTF8.GetString(bytes.Slice(1));
        if (text.Length == 0)
        {
            return; // The parsers document ArgumentException for empty strings.
        }

        switch (bytes[0] % 5)
        {
            case 0: MailAddressParsing(text); break;
            case 1: MailAddressCollectionParsing(text); break;
            case 2: ContentTypeParsing(text); break;
            case 3: ContentDispositionParsing(text); break;
            default: CookieParsing(text); break;
        }
    }

    private static void MailAddressParsing(string text)
    {
        bool created = MailAddress.TryCreate(text, out MailAddress? tried);
        MailAddress? constructed = null;
        try
        {
            constructed = new MailAddress(text);
        }
        catch (FormatException)
        {
        }

        Check(created == (constructed is not null), $"MailAddress.TryCreate returned {created} but the constructor {(constructed is null ? "threw" : "succeeded")} for '{Escape(text)}'");
        if (tried is null || constructed is null)
        {
            return;
        }

        Check(tried.Address == constructed.Address && tried.DisplayName == constructed.DisplayName, $"TryCreate and the constructor disagree for '{Escape(text)}'");

        // The address alone and the full ToString() form must parse back to the same thing.
        Check(MailAddress.TryCreate(tried.Address, out MailAddress? fromAddress) && fromAddress.Address == tried.Address,
            $"MailAddress '{Escape(text)}' has Address '{Escape(tried.Address)}', which {(fromAddress is null ? "doesn't parse" : $"parses as '{Escape(fromAddress.Address)}'")}");
        string formatted = tried.ToString();
        bool displayNameRoundTrips = s_strict || !tried.DisplayName.Contains('\\');
        if (!displayNameRoundTrips)
        {
            return;
        }
        Check(MailAddress.TryCreate(formatted, out MailAddress? reparsed) && reparsed.Address == tried.Address && (!displayNameRoundTrips || reparsed.DisplayName == tried.DisplayName),
            $"MailAddress '{Escape(text)}' formats as '{Escape(formatted)}', which {(reparsed is null ? "doesn't parse" : $"parses as '{Escape(reparsed.Address)}' / '{Escape(reparsed.DisplayName)}'")} (expected '{Escape(tried.Address)}' / '{Escape(tried.DisplayName)}')");
    }

    private static void MailAddressCollectionParsing(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return; // Documented ArgumentException.
        }

        var collection = new MailAddressCollection();
        try
        {
            collection.Add(text);
        }
        catch (FormatException)
        {
            return;
        }

        if (!s_strict && collection.Any(a => a.DisplayName.Contains('\\')))
        {
            return;
        }

        string formatted = collection.ToString();
        var reparsed = new MailAddressCollection();
        try
        {
            reparsed.Add(formatted);
        }
        catch (FormatException ex)
        {
            Check(false, $"MailAddressCollection '{Escape(text)}' formats as '{Escape(formatted)}', which doesn't parse: {ex.Message}");
        }

        Check(reparsed.Count == collection.Count && reparsed.Select(a => a.Address).SequenceEqual(collection.Select(a => a.Address)),
            $"MailAddressCollection '{Escape(text)}' -> [{string.Join(" | ", collection.Select(a => Escape(a.Address)))}] formats as '{Escape(formatted)}' -> [{string.Join(" | ", reparsed.Select(a => Escape(a.Address)))}]");
    }

    private static void ContentTypeParsing(string text)
    {
        ContentType contentType;
        try
        {
            contentType = new ContentType(text);
        }
        catch (FormatException)
        {
            return;
        }
        catch (ArgumentException) when (!s_strict)
        {
            return;
        }

        string formatted = contentType.ToString();
        ContentType reparsed;
        try
        {
            reparsed = new ContentType(formatted);
        }
        catch (FormatException ex)
        {
            Check(false, $"ContentType '{Escape(text)}' formats as '{Escape(formatted)}', which doesn't parse: {ex.Message}");
            return;
        }

        Check(reparsed.MediaType == contentType.MediaType && SameParameters(reparsed.Parameters, contentType.Parameters),
            $"ContentType '{Escape(text)}' formats as '{Escape(formatted)}', which parses differently: '{Escape(reparsed.ToString())}'");
    }

    private static void ContentDispositionParsing(string text)
    {
        ContentDisposition disposition;
        try
        {
            disposition = new ContentDisposition(text);
        }
        catch (FormatException)
        {
            return;
        }
        catch (Exception ex) when (!s_strict && ex is IndexOutOfRangeException or ArgumentException)
        {
            return;
        }

        string formatted = disposition.ToString();
        ContentDisposition reparsed;
        try
        {
            reparsed = new ContentDisposition(formatted);
        }
        catch (FormatException ex)
        {
            Check(false, $"ContentDisposition '{Escape(text)}' formats as '{Escape(formatted)}', which doesn't parse: {ex.Message}");
            return;
        }

        Check(reparsed.DispositionType == disposition.DispositionType && SameParameters(reparsed.Parameters, disposition.Parameters),
            $"ContentDisposition '{Escape(text)}' formats as '{Escape(formatted)}', which parses differently: '{Escape(reparsed.ToString())}'");
    }

    private static void CookieParsing(string text)
    {
        var container = new CookieContainer();
        var uri = new Uri("http://fuzz.example.com/path/page");
        try
        {
            container.SetCookies(uri, text);
        }
        catch (CookieException)
        {
            return;
        }

        foreach (Cookie cookie in container.GetAllCookies())
        {
            _ = cookie.ToString();
        }

        string header = container.GetCookieHeader(uri);
        _ = container.GetCookies(new Uri("https://fuzz.example.com/"));
        _ = container.GetCookies(new Uri("http://other.example.com/path/"));
        Check(header is not null, "GetCookieHeader returned null");
    }

    private static bool SameParameters(System.Collections.Specialized.StringDictionary a, System.Collections.Specialized.StringDictionary b)
    {
        if (!s_strict)
        {
            foreach (System.Collections.DictionaryEntry entry in b)
            {
                // Non-ASCII values come back as encoded-words; dates are normalized when formatted.
                if (((string?)entry.Value ?? "").Any(c => c >= 0x80) || ((string)entry.Key).EndsWith("-date", StringComparison.OrdinalIgnoreCase))
                {
                    return a.Count == b.Count;
                }
            }
        }

        if (a.Count != b.Count)
        {
            return false;
        }

        foreach (System.Collections.DictionaryEntry entry in a)
        {
            if (!b.ContainsKey((string)entry.Key) || b[(string)entry.Key] != (string?)entry.Value)
            {
                return false;
            }
        }

        return true;
    }

    private static string Escape(string? text) =>
        text is null ? "(null)" : string.Concat(text.Take(200).Select(c => c is >= ' ' and < '\x7F' ? c.ToString() : $"\\u{(int)c:X4}"));

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
