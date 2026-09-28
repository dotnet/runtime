// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace DotnetFuzzing.Fuzzers;

/// <summary>
/// Fuzzes <see cref="IPNetwork"/> and <see cref="IPEndPoint"/> parsing. The string, char-span and UTF-8 overloads must agree on
/// every input (the finding-44 theme), inputs are placed against a guard page so a read past the end faults, whatever parses
/// must round-trip through its own ToString, and an IPNetwork parse is cross-checked against splitting on '/' and parsing the
/// address and prefix separately. IPNetwork invariants (BaseAddress has no bits set below the prefix, Contains agrees with the
/// mask) are checked too.
/// </summary>
/// <remarks>Input layout: [0] target and generation mode, [1..] the text (mostly from a palette of address tokens).</remarks>
internal sealed class IPNetworkEndPointFuzzer : IFuzzer
{
    public string[] TargetAssemblies => ["System.Net.Primitives", "System.Private.Uri"];
    public string[] TargetCoreLibPrefixes => [];

    private static readonly string[] s_palette =
    [
        "192.168.1.0", "10.0.0.0", "255.255.255.255", "0.0.0.0", "127.0.0.1", "::1", "::", "fe80::1", "2001:db8::",
        "::ffff:192.168.0.1", "[::1]", "1234:5678:9abc:def0:1234:5678:9abc:def0", "/", "/0", "/24", "/32", "/33", "/128", "/129",
        ":", ":8080", ":0", ":65535", ":65536", ".", "%eth0", "1", "12", "192", "256", "abcd", "0x", " ", "\t", "-1", "999",
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
        if (text.Length == 0 || text.Length > 400 || !text.All(char.IsAscii))
        {
            // The UTF-8/char consistency check needs a common ASCII encoding; non-ASCII text is only checked for not throwing.
            NonAsciiSmoke(bytes[0], text);
            return;
        }

        byte[] utf8 = Encoding.ASCII.GetBytes(text);
        if ((bytes[0] & 1) == 0)
        {
            Networks(text, utf8);
        }
        else
        {
            EndPoints(text, utf8);
        }
    }

    private static void Networks(string text, byte[] utf8)
    {
        using PooledBoundedMemory<char> charMemory = PooledBoundedMemory<char>.Rent(text.AsSpan(), PoisonPagePlacement.After);
        using PooledBoundedMemory<byte> utf8Memory = PooledBoundedMemory<byte>.Rent(utf8, PoisonPagePlacement.After);

        bool strOk = IPNetwork.TryParse(text, out IPNetwork fromString);
        bool spanOk = IPNetwork.TryParse(charMemory.Span, out IPNetwork fromSpan);
        bool utf8Ok = IPNetwork.TryParse(utf8Memory.Span, out IPNetwork fromUtf8);

        Check(strOk == spanOk && (!strOk || fromString.Equals(fromSpan)), () => $"IPNetwork.TryParse(string)={strOk} vs (span)={spanOk} for '{Escape(text)}'");
        Check(strOk == utf8Ok && (!strOk || fromString.Equals(fromUtf8)), () => $"IPNetwork.TryParse(string)={strOk} vs (UTF-8)={utf8Ok} for '{Escape(text)}'");

        // Parse must agree with TryParse.
        IPNetwork parsed = default;
        bool threw = false;
        try
        {
            parsed = IPNetwork.Parse(text);
        }
        catch (FormatException)
        {
            threw = true;
        }
        catch (ArgumentException)
        {
            threw = true; // Prefix out of range surfaces as ArgumentOutOfRangeException.
        }

        Check(strOk != threw && (!strOk || parsed.Equals(fromString)), () => $"IPNetwork.Parse {(threw ? "threw" : "succeeded")} but TryParse returned {strOk} for '{Escape(text)}'");
        if (!strOk)
        {
            // A single '/', a parseable address and an in-range decimal prefix is accepted (host bits are masked, not rejected).
            Check(!IsAcceptableNetwork(text), () => $"IPNetwork rejected '{Escape(text)}', which is a valid address/prefix");
            return;
        }

        // Round-trip and invariants.
        string formatted = fromString.ToString();
        Check(IPNetwork.TryParse(formatted, out IPNetwork reparsed) && reparsed.Equals(fromString),
            () => $"IPNetwork '{Escape(text)}' formats as '{Escape(formatted)}', which doesn't round-trip");
        Check(fromString.PrefixLength >= 0 && fromString.PrefixLength <= (fromString.BaseAddress.AddressFamily == AddressFamily.InterNetwork ? 32 : 128),
            () => $"IPNetwork '{Escape(text)}' has out-of-range PrefixLength {fromString.PrefixLength}");

        byte[] baseBytes = fromString.BaseAddress.GetAddressBytes();
        Check(NoBitsBelowPrefix(baseBytes, fromString.PrefixLength), () => $"IPNetwork '{Escape(text)}' BaseAddress {fromString.BaseAddress} has bits set below prefix {fromString.PrefixLength}");
        Check(fromString.Contains(fromString.BaseAddress), () => $"IPNetwork '{Escape(text)}' doesn't contain its own BaseAddress");

        // Cross-check against a manual split. IPNetwork masks host bits after the prefix (see finding 45), so the BaseAddress
        // must equal the parsed address with its low bits cleared, and the prefix must match.
        int slash = text.LastIndexOf('/');
        if (slash > 0 && IPAddress.TryParse(text.AsSpan(0, slash), out IPAddress? addr) && addr.AddressFamily == fromString.BaseAddress.AddressFamily)
        {
            byte[] expected = addr.GetAddressBytes();
            ClearBitsBelowPrefix(expected, fromString.PrefixLength);
            Check(fromString.BaseAddress.GetAddressBytes().AsSpan().SequenceEqual(expected),
                () => $"IPNetwork '{Escape(text)}' has BaseAddress {fromString.BaseAddress}, but masking {addr} to /{fromString.PrefixLength} gives {new IPAddress(expected)}");
        }
    }

    private static void EndPoints(string text, byte[] utf8)
    {
        using PooledBoundedMemory<char> charMemory = PooledBoundedMemory<char>.Rent(text.AsSpan(), PoisonPagePlacement.After);
        using PooledBoundedMemory<byte> utf8Memory = PooledBoundedMemory<byte>.Rent(utf8, PoisonPagePlacement.After);

        bool strOk = IPEndPoint.TryParse(text, out IPEndPoint? fromString);
        bool spanOk = IPEndPoint.TryParse(charMemory.Span, out IPEndPoint? fromSpan);
        bool utf8Ok = IPEndPoint.TryParse(utf8Memory.Span, out IPEndPoint? fromUtf8);

        Check(strOk == spanOk && (!strOk || fromString!.Equals(fromSpan)), () => $"IPEndPoint.TryParse(string)={strOk} vs (span)={spanOk} for '{Escape(text)}'");
        Check(strOk == utf8Ok && (!strOk || fromString!.Equals(fromUtf8)), () => $"IPEndPoint.TryParse(string)={strOk} vs (UTF-8)={utf8Ok} for '{Escape(text)}'");

        if (!strOk)
        {
            return;
        }

        Check(fromString!.Port >= 0 && fromString.Port <= 65535, () => $"IPEndPoint '{Escape(text)}' has out-of-range Port {fromString.Port}");
        string formatted = fromString.ToString();
        Check(IPEndPoint.TryParse(formatted, out IPEndPoint? reparsed) && fromString.Equals(reparsed),
            () => $"IPEndPoint '{Escape(text)}' formats as '{Escape(formatted)}', which doesn't round-trip to the same endpoint");
    }

    private static void NonAsciiSmoke(byte selector, string text)
    {
        // Only that the parsers don't throw for arbitrary text.
        _ = IPNetwork.TryParse(text, out _);
        _ = IPEndPoint.TryParse(text, out _);
        try
        {
            _ = IPNetwork.Parse(text);
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
        }

        _ = selector;
    }

    private static bool NoBitsBelowPrefix(byte[] address, int prefix)
    {
        for (int bit = prefix; bit < address.Length * 8; bit++)
        {
            if ((address[bit / 8] & (0x80 >> (bit % 8))) != 0)
            {
                return false;
            }
        }

        return true;
    }

    private static void ClearBitsBelowPrefix(byte[] address, int prefix)
    {
        for (int bit = prefix; bit < address.Length * 8; bit++)
        {
            address[bit / 8] &= (byte)~(0x80 >> (bit % 8));
        }
    }

    // A conservative check that a string is an acceptable "address/prefix": one '/', a parseable address, and an in-range
    // canonical decimal prefix. Host bits are allowed (they get masked), so this doesn't require them to be zero.
    private static bool IsAcceptableNetwork(string text)
    {
        int slash = text.IndexOf('/');
        if (slash <= 0 || text.IndexOf('/', slash + 1) >= 0)
        {
            return false;
        }

        if (!IPAddress.TryParse(text.AsSpan(0, slash), out IPAddress? addr))
        {
            return false;
        }

        // IPAddress.TryParse accepts forms IPNetwork doesn't (leading zeros, IPv4 with a scope); keep this conservative.
        string prefixText = text.Substring(slash + 1);
        if (prefixText.Length == 0 || prefixText.Length > 3 || !prefixText.All(char.IsAsciiDigit) || (prefixText.Length > 1 && prefixText[0] == '0'))
        {
            return false;
        }

        int prefix = int.Parse(prefixText);
        byte[] bytes = addr.GetAddressBytes();
        return prefix <= bytes.Length * 8 && addr.ToString() == text.AsSpan(0, slash).ToString();
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
