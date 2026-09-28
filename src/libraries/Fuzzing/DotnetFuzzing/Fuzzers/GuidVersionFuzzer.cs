// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Text;

namespace DotnetFuzzing.Fuzzers;

/// <summary>
/// Fuzzes <see cref="Guid"/> and <see cref="Version"/> parsing and formatting. Both have hand-written UTF-8 parsers (the same
/// family as the number-parsing over-read in finding 44), so fuzzed text is placed against a guard page and parsed through the
/// string, char-span and UTF-8 overloads, which must agree. Every Guid format (N, D, B, P, X) round-trips through its own
/// ToString, and TryFormat into a char span, a UTF-8 span and a one-short buffer behaves. Version's four field counts round-trip
/// the same way.
/// </summary>
/// <remarks>Input layout: [0] target and format selector, [1..] the text through a palette of Guid/Version tokens.</remarks>
internal sealed class GuidVersionFuzzer : IFuzzer
{
    public string[] TargetAssemblies => [];
    public string[] TargetCoreLibPrefixes { get; } = ["System.Guid", "System.Version", "System.Number", "System.HexConverter"];

    private static readonly string[] s_palette =
    [
        "0", "1", "9", "a", "f", "A", "F", "-", "{", "}", "(", ")", ",", "0x", "00000000", "0000", "12345678", "deadbeef",
        "9abcdef0", " ", "\t", ".", "g", "x", "+", "12", "65535", "2147483647", "4294967296", "-1", "é", "１", ";",
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
        if (text.Length == 0 || text.Length > 400)
        {
            return;
        }

        if ((bytes[0] & 1) == 0)
        {
            Guids(bytes[0], text);
        }
        else
        {
            Versions(text);
        }
    }

    private static void Guids(byte selector, string text)
    {
        bool ascii = text.All(char.IsAscii);
        byte[] utf8 = ascii ? Encoding.ASCII.GetBytes(text) : Encoding.UTF8.GetBytes(text);
        using PooledBoundedMemory<char> charMemory = PooledBoundedMemory<char>.Rent(text.AsSpan(), PoisonPagePlacement.After);
        using PooledBoundedMemory<byte> utf8Memory = PooledBoundedMemory<byte>.Rent(utf8, PoisonPagePlacement.After);

        bool strOk = Guid.TryParse(text, out Guid fromString);
        bool spanOk = Guid.TryParse(charMemory.Span, out Guid fromSpan);
        bool utf8Ok = Guid.TryParse(utf8Memory.Span, out Guid fromUtf8);

        Check(strOk == spanOk && (!strOk || fromString == fromSpan), () => $"Guid.TryParse(string)={strOk} vs (span)={spanOk} for '{Escape(text)}'");
        if (ascii)
        {
            Check(strOk == utf8Ok && (!strOk || fromString == fromUtf8), () => $"Guid.TryParse(string)={strOk} vs (UTF-8)={utf8Ok} for '{Escape(text)}'");
        }

        // Parse must agree with TryParse.
        bool threw = false;
        try
        {
            _ = Guid.Parse(text);
        }
        catch (FormatException)
        {
            threw = true;
        }
        catch (OverflowException)
        {
            threw = true; // The X format's hex fields can overflow.
        }

        Check(strOk != threw, () => $"Guid.Parse {(threw ? "threw" : "succeeded")} but TryParse returned {strOk} for '{Escape(text)}'");
        if (!strOk)
        {
            return;
        }

        // Every format round-trips through string, char span and UTF-8.
        foreach (char f in "NDBPX")
        {
            string formatted = fromString.ToString(f.ToString());
            Check(Guid.TryParseExact(formatted, f.ToString(), out Guid back) && back == fromString, () => $"Guid '{fromString}' format '{f}' = '{formatted}' didn't round-trip");
            Check(Guid.TryParse(formatted, out Guid backLoose) && backLoose == fromString, () => $"Guid '{fromString}' format '{f}' = '{formatted}' didn't loose-parse");

            char[] chars = new char[formatted.Length + 2];
            Check(fromString.TryFormat(chars, out int written, f.ToString()) && new string(chars, 0, written) == formatted, () => $"Guid TryFormat(chars, '{f}') disagrees with ToString for '{fromString}'");
            byte[] utf8Buffer = new byte[formatted.Length + 2];
            Check(fromString.TryFormat(utf8Buffer, out int utf8Written, f.ToString()) && Encoding.ASCII.GetString(utf8Buffer, 0, utf8Written) == formatted, () => $"Guid TryFormat(UTF-8, '{f}') disagrees with ToString for '{fromString}'");
            Check(!fromString.TryFormat(new char[formatted.Length - 1], out int w, f.ToString()) && w == 0, () => $"Guid TryFormat('{f}') into {formatted.Length - 1} chars succeeded for '{fromString}'");
        }
    }

    private static void Versions(string text)
    {
        bool ascii = text.All(char.IsAscii);
        byte[] utf8 = ascii ? Encoding.ASCII.GetBytes(text) : Encoding.UTF8.GetBytes(text);
        using PooledBoundedMemory<char> charMemory = PooledBoundedMemory<char>.Rent(text.AsSpan(), PoisonPagePlacement.After);
        using PooledBoundedMemory<byte> utf8Memory = PooledBoundedMemory<byte>.Rent(utf8, PoisonPagePlacement.After);

        bool strOk = Version.TryParse(text, out Version? fromString);
        bool spanOk = Version.TryParse(charMemory.Span, out Version? fromSpan);
        bool utf8Ok = Version.TryParse(utf8Memory.Span, out Version? fromUtf8);

        Check(strOk == spanOk && (!strOk || fromString!.Equals(fromSpan)), () => $"Version.TryParse(string)={strOk} vs (span)={spanOk} for '{Escape(text)}'");
        if (ascii)
        {
            Check(strOk == utf8Ok && (!strOk || fromString!.Equals(fromUtf8)), () => $"Version.TryParse(string)={strOk} vs (UTF-8)={utf8Ok} for '{Escape(text)}'");
        }

        bool threw = false;
        try
        {
            _ = Version.Parse(text);
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException or OverflowException)
        {
            threw = true;
        }

        Check(strOk != threw, () => $"Version.Parse {(threw ? "threw" : "succeeded")} but TryParse returned {strOk} for '{Escape(text)}'");
        if (!strOk)
        {
            return;
        }

        // Round-trip through ToString and each valid field count.
        string formatted = fromString!.ToString();
        Check(Version.TryParse(formatted, out Version? back) && fromString.Equals(back), () => $"Version '{formatted}' didn't round-trip");

        int maxFieldCount = 2 + (fromString.Build >= 0 ? 1 : 0) + (fromString.Revision >= 0 ? 1 : 0);
        for (int fieldCount = 0; fieldCount <= 4; fieldCount++)
        {
            bool valid = fieldCount >= 0 && fieldCount <= maxFieldCount;
            string? partial = null;
            bool formatThrew = false;
            try
            {
                partial = fromString.ToString(fieldCount);
            }
            catch (ArgumentException)
            {
                formatThrew = true;
            }

            Check(formatThrew != valid, () => $"Version '{formatted}'.ToString({fieldCount}) {(formatThrew ? "threw" : "succeeded")}, expected valid={valid}");
            if (valid)
            {
                char[] chars = new char[partial!.Length + 2];
                Check(fromString.TryFormat(chars, fieldCount, out int written) && new string(chars, 0, written) == partial, () => $"Version.TryFormat({fieldCount}) disagrees with ToString({fieldCount}) for '{formatted}'");
                byte[] utf8Buffer = new byte[partial.Length + 2];
                Check(fromString.TryFormat(utf8Buffer, fieldCount, out int utf8Written) && Encoding.ASCII.GetString(utf8Buffer, 0, utf8Written) == partial, () => $"Version UTF-8 TryFormat({fieldCount}) disagrees for '{formatted}'");
            }
        }

        // Fields are non-negative and comparison is consistent with equality.
        Check(fromString.Major >= 0 && fromString.Minor >= 0, () => $"Version '{formatted}' has a negative Major/Minor");
        Check(fromString.CompareTo(back) == 0 && fromString.GetHashCode() == back!.GetHashCode(), () => $"Version '{formatted}' compares unequal to its round-trip");
    }

    private static string Escape(string text) =>
        string.Concat(text.Take(120).Select(c => c is >= ' ' and < '\x7F' ? c.ToString() : $"\\u{(int)c:X4}"));

    private static void Check(bool condition, Func<string> message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message());
        }
    }
}
