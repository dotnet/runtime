// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;

namespace DotnetFuzzing.Fuzzers;

/// <summary>
/// Fuzzes the composite format string parser behind <see cref="CompositeFormat"/> and <see cref="string.Format(string, object?[])"/>.
/// The two share the same grammar, so <see cref="CompositeFormat.Parse(string)"/> must accept exactly the strings that
/// <see cref="string.Format(IFormatProvider?, string, object?[])"/> accepts (given enough arguments), a parsed CompositeFormat must
/// produce the same output as string.Format on the same arguments, and MinimumArgumentCount must match the highest hole index used.
/// </summary>
/// <remarks>Input layout: raw bytes mapped through a palette of format-string tokens.</remarks>
internal sealed class CompositeFormatFuzzer : IFuzzer
{
    public string[] TargetAssemblies => [];
    public string[] TargetCoreLibPrefixes { get; } = ["System.Text.CompositeFormat", "System.Text.ValueStringBuilder", "System.String", "System.Number"];

    private static readonly string[] s_palette =
    [
        "{0}", "{1}", "{2}", "{0,5}", "{1,-8}", "{0:X}", "{2:N2}", "{0:yyyy}", "{{", "}}", "{", "}", "text", " ", ",", ":",
        "-10", "0", "99", "{10}", "{0,0}", "{3:D4}", "{0:}", "{,5}", "{0:{}}", "１", "{0:C}", "{9}", "abc", ".", "{0 }",
    ];

    private static readonly object?[] s_args =
    [
        42, "hello", 3.14159, new DateTime(2024, 1, 15), -7, 0, ushort.MaxValue, 1.5m, null, long.MinValue, 255,
    ];

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 1)
        {
            return;
        }

        var builder = new StringBuilder();
        foreach (byte b in bytes)
        {
            builder.Append(b < 0x80 ? ((char)b).ToString() : s_palette[b % s_palette.Length]);
        }

        string format = builder.ToString();
        if (format.Length > 1000 || HasOverflowingIndex(format))
        {
            // A digit run of 1,000,000 or more is a huge index/alignment (finding 47) or a huge numeric precision like N902915902,
            // which both APIs honor by producing that many digits. Neither is worth the memory.
            return;
        }

        CultureInfo culture = CultureInfo.InvariantCulture;

        // string.Format is the reference parser.
        string? formatResult = null;
        Exception? formatError = null;
        try
        {
            formatResult = string.Format(culture, format, s_args);
        }
        catch (FormatException ex)
        {
            formatError = ex;
        }
        catch (ArgumentNullException ex)
        {
            formatError = ex; // A null format isn't produced here, but keep parity.
        }

        // CompositeFormat.Parse uses the same grammar.
        CompositeFormat? composite = null;
        Exception? parseError = null;
        try
        {
            composite = CompositeFormat.Parse(format);
        }
        catch (FormatException ex)
        {
            parseError = ex;
        }

        // string.Format only throws FormatException for a bad hole index when that index is actually reached with too few args;
        // CompositeFormat.Parse validates the grammar without args. So a grammar error must agree, but an out-of-range index that
        // string.Format would only catch at format time is allowed to differ. Distinguish the two by whether Parse succeeded.
        if (parseError is not null)
        {
            // A grammar error: string.Format with plenty of args must also fail (it can't format what won't parse).
            Check(formatError is not null, () => $"CompositeFormat.Parse threw but string.Format succeeded for '{Escape(format)}'");
            return;
        }

        // Parse succeeded. MinimumArgumentCount is the number of args needed (a hole index can be up to ~1e9, so it's unbounded).
        int required = composite!.MinimumArgumentCount;
        Check(required >= 0, () => $"CompositeFormat '{Escape(format)}' has negative MinimumArgumentCount {required}");

        if (required > s_args.Length)
        {
            // string.Format with our fixed args would throw for the missing index.
            return;
        }

        // Both should format the same way, including throwing the same way at format time (e.g. an invalid numeric format
        // specifier reached with a real argument throws FormatException from both paths).
        string? composed = null;
        Exception? composeError = null;
        try
        {
            composed = string.Format(culture, composite, s_args);
        }
        catch (FormatException ex)
        {
            composeError = ex;
        }

        Check((formatError is null) == (composeError is null),
            () => $"string.Format {(formatError is null ? "succeeded" : "threw")} but string.Format(CompositeFormat) {(composeError is null ? "succeeded" : "threw")} for '{Escape(format)}'");
        if (composeError is null)
        {
            Check(composed == formatResult, () => $"CompositeFormat and string.Format disagree for '{Escape(format)}': '{Escape(composed)}' vs '{Escape(formatResult)}'");

            var sb = new StringBuilder();
            sb.AppendFormat(culture, format, s_args);
            Check(sb.ToString() == formatResult, () => $"StringBuilder.AppendFormat disagrees with string.Format for '{Escape(format)}'");
        }
    }

    // Finding 47: CompositeFormat.Parse accepts a hole index or alignment that string.Format rejects (string.Format caps both
    // below 1,000,000), and formatting a huge alignment pads to that many chars. Any digit run worth 1,000,000 or more is
    // skipped from the equality check. That also catches some literal text, which only costs a little coverage.
    private static bool HasOverflowingIndex(string format)
    {
        for (int i = 0; i < format.Length; i++)
        {
            if (!char.IsAsciiDigit(format[i]))
            {
                continue;
            }

            int j = i;
            while (j < format.Length && format[j] == '0')
            {
                j++;
            }

            int significant = 0;
            while (j < format.Length && char.IsAsciiDigit(format[j]))
            {
                significant++;
                j++;
            }

            if (significant >= 7)
            {
                return true;
            }

            i = j - 1;
        }

        return false;
    }

    private static string Escape(string? text) =>
        text is null ? "(null)" : string.Concat(text.Take(200).Select(c => c is >= ' ' and < '\x7F' ? c.ToString() : $"\\u{(int)c:X4}"));

    private static void Check(bool condition, Func<string> message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message());
        }
    }
}
