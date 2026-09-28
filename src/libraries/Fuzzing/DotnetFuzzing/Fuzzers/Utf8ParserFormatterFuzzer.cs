// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Text;
using System.Globalization;
using System.Text;

namespace DotnetFuzzing.Fuzzers;

/// <summary>
/// Fuzzes <see cref="Utf8Parser"/> and <see cref="Utf8Formatter"/> (System.Buffers.Text), which read and write UTF-8 through
/// span primitives. For each supported type it round-trips (format then parse), compares the formatter output with the managed
/// string/UTF-8 formatter, compares the parser with the managed parse of the same bytes, and feeds fuzzed bytes placed against a
/// guard page so any read past the end of the source faults. bytesConsumed must never exceed the input, and re-parsing the
/// consumed prefix must give the same value.
/// </summary>
/// <remarks>Input layout: [0] type, [1] standard format selector, [2..] value bytes / parse text.</remarks>
internal sealed class Utf8ParserFormatterFuzzer : IFuzzer
{
    public string[] TargetAssemblies => [];
    public string[] TargetCoreLibPrefixes { get; } = ["System.Buffers.Text", "System.Number", "System.Globalization.FormatProvider"];

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 3)
        {
            return;
        }

        byte formatSel = bytes[1];
        ReadOnlySpan<byte> data = bytes.Slice(2);
        switch (bytes[0] % 12)
        {
            case 0: Integer<int>(formatSel, data, "DNXG", i => (int)i, (ReadOnlySpan<byte> s, out int v, out int c, char f) => Utf8Parser.TryParse(s, out v, out c, f), (Span<byte> d, int v, out int w, char f) => Utf8Formatter.TryFormat(v, d, out w, Std(f)), (v, f) => v.ToString(NumFmt(f), CultureInfo.InvariantCulture)); break;
            case 1: Integer<long>(formatSel, data, "DNXG", i => i, (ReadOnlySpan<byte> s, out long v, out int c, char f) => Utf8Parser.TryParse(s, out v, out c, f), (Span<byte> d, long v, out int w, char f) => Utf8Formatter.TryFormat(v, d, out w, Std(f)), (v, f) => v.ToString(NumFmt(f), CultureInfo.InvariantCulture)); break;
            case 2: Integer<uint>(formatSel, data, "DNXG", i => (uint)i, (ReadOnlySpan<byte> s, out uint v, out int c, char f) => Utf8Parser.TryParse(s, out v, out c, f), (Span<byte> d, uint v, out int w, char f) => Utf8Formatter.TryFormat(v, d, out w, Std(f)), (v, f) => v.ToString(NumFmt(f), CultureInfo.InvariantCulture)); break;
            case 3: Integer<ulong>(formatSel, data, "DNXG", i => (ulong)i, (ReadOnlySpan<byte> s, out ulong v, out int c, char f) => Utf8Parser.TryParse(s, out v, out c, f), (Span<byte> d, ulong v, out int w, char f) => Utf8Formatter.TryFormat(v, d, out w, Std(f)), (v, f) => v.ToString(NumFmt(f), CultureInfo.InvariantCulture)); break;
            case 4: Integer<short>(formatSel, data, "DNXG", i => (short)i, (ReadOnlySpan<byte> s, out short v, out int c, char f) => Utf8Parser.TryParse(s, out v, out c, f), (Span<byte> d, short v, out int w, char f) => Utf8Formatter.TryFormat(v, d, out w, Std(f)), (v, f) => v.ToString(NumFmt(f), CultureInfo.InvariantCulture)); break;
            case 5: Integer<byte>(formatSel, data, "DNXG", i => (byte)i, (ReadOnlySpan<byte> s, out byte v, out int c, char f) => Utf8Parser.TryParse(s, out v, out c, f), (Span<byte> d, byte v, out int w, char f) => Utf8Formatter.TryFormat(v, d, out w, Std(f)), (v, f) => v.ToString(NumFmt(f), CultureInfo.InvariantCulture)); break;
            case 6: Integer<sbyte>(formatSel, data, "DNXG", i => (sbyte)i, (ReadOnlySpan<byte> s, out sbyte v, out int c, char f) => Utf8Parser.TryParse(s, out v, out c, f), (Span<byte> d, sbyte v, out int w, char f) => Utf8Formatter.TryFormat(v, d, out w, Std(f)), (v, f) => v.ToString(NumFmt(f), CultureInfo.InvariantCulture)); break;
            case 7: Floats(formatSel, data); break;
            case 8: Bools(formatSel, data); break;
            case 9: Guids(formatSel, data); break;
            case 10: DateTimes(formatSel, data); break;
            default: TimeSpans(formatSel, data); break;
        }
    }

    private delegate bool ParseFn<T>(ReadOnlySpan<byte> source, out T value, out int consumed, char format);
    private delegate bool FormatFn<T>(Span<byte> destination, T value, out int written, char format);

    private static void Integer<T>(byte formatSel, ReadOnlySpan<byte> data, string formats, Func<long, T> make, ParseFn<T> parse, FormatFn<T> format, Func<T, char, string> managedFormat)
        where T : struct, IEquatable<T>
    {
        char std = formats[formatSel % formats.Length];
        long raw = 0;
        for (int i = 0; i < 8 && i < data.Length; i++)
        {
            raw |= (long)data[i] << (8 * i);
        }

        T value = make(raw);

        // Format then parse back.
        byte[] buffer = new byte[64];
        bool formatted = format(buffer, value, out int written, std);
        Check(formatted, () => $"Utf8Formatter.TryFormat('{std}') failed for {value} into 64 bytes");
        string managed = managedFormat(value, std);
        Check(Encoding.ASCII.GetString(buffer, 0, written) == managed, () => $"Utf8Formatter('{std}') of {value} = '{Encoding.ASCII.GetString(buffer, 0, written)}', managed = '{managed}'");

        Check(parse(buffer.AsSpan(0, written), out T round, out int consumed, std == 'G' ? default : std) && round.Equals(value) && consumed == written,
            () => $"Utf8Parser('{std}') didn't round-trip {value}: got parsed value with consumed {consumed} of {written}");

        // Too-small destination fails cleanly.
        if (written > 0)
        {
            Check(!format(buffer.AsSpan(0, written - 1), value, out int w, std) && w == 0, () => $"Utf8Formatter('{std}') into {written - 1} bytes succeeded for {value}");
        }

        FuzzParse(data, std, parse);
    }

    private static void Floats(byte formatSel, ReadOnlySpan<byte> data)
    {
        char std = "GEF"[formatSel % 3];
        double raw = data.Length >= 8 ? BitConverter.ToDouble(data.Slice(0, 8)) : data.Length > 0 ? data[0] : 0;
        if (double.IsNaN(raw) || double.IsInfinity(raw))
        {
            raw = data.Length > 0 ? data[0] : 0; // Utf8Formatter doesn't format non-finite values.
        }

        foreach (double value in new[] { raw, (double)(float)raw })
        {
            byte[] buffer = new byte[128];
            if (Utf8Formatter.TryFormat(value, buffer, out int written, new StandardFormat(std, 6)))
            {
                Check(Utf8Parser.TryParse(buffer.AsSpan(0, written), out double round, out int consumed, std) && consumed == written,
                    () => $"Utf8Parser('{std}') didn't parse back '{Encoding.ASCII.GetString(buffer, 0, written)}' from {value:R}");
            }
        }

        FuzzParse<double>(data, std, (ReadOnlySpan<byte> s, out double v, out int c, char f) => Utf8Parser.TryParse(s, out v, out c, f));
        FuzzParse<float>(data, std, (ReadOnlySpan<byte> s, out float v, out int c, char f) => Utf8Parser.TryParse(s, out v, out c, f));
        FuzzParse<decimal>(data, std, (ReadOnlySpan<byte> s, out decimal v, out int c, char f) => Utf8Parser.TryParse(s, out v, out c, f));
    }

    private static void Bools(byte formatSel, ReadOnlySpan<byte> data)
    {
        char std = "Gl"[formatSel % 2];
        foreach (bool value in new[] { true, false })
        {
            byte[] buffer = new byte[8];
            Check(Utf8Formatter.TryFormat(value, buffer, out int written, std == 'G' ? default : new StandardFormat(std)), () => $"Utf8Formatter bool('{std}') failed for {value}");
            Check(Utf8Parser.TryParse(buffer.AsSpan(0, written), out bool round, out int consumed) && round == value && consumed == written,
                () => $"Utf8Parser bool didn't round-trip {value} from '{Encoding.ASCII.GetString(buffer, 0, written)}'");
        }

        FuzzParse<bool>(data, default, (ReadOnlySpan<byte> s, out bool v, out int c, char f) => Utf8Parser.TryParse(s, out v, out c));
    }

    private static void Guids(byte formatSel, ReadOnlySpan<byte> data)
    {
        char std = "DBPN"[formatSel % 4];
        byte[] guidBytes = new byte[16];
        for (int i = 0; i < 16 && i < data.Length; i++)
        {
            guidBytes[i] = data[i];
        }

        var value = new Guid(guidBytes);
        byte[] buffer = new byte[68];
        Check(Utf8Formatter.TryFormat(value, buffer, out int written, new StandardFormat(std)), () => $"Utf8Formatter Guid('{std}') failed for {value}");
        string managed = value.ToString(std.ToString(), CultureInfo.InvariantCulture);
        Check(Encoding.ASCII.GetString(buffer, 0, written) == managed, () => $"Utf8Formatter Guid('{std}') = '{Encoding.ASCII.GetString(buffer, 0, written)}', managed = '{managed}'");
        Check(Utf8Parser.TryParse(buffer.AsSpan(0, written), out Guid round, out int consumed, std) && round == value && consumed == written,
            () => $"Utf8Parser Guid('{std}') didn't round-trip {value}");

        FuzzParse<Guid>(data, std, (ReadOnlySpan<byte> s, out Guid v, out int c, char f) => Utf8Parser.TryParse(s, out v, out c, f));
    }

    private static void DateTimes(byte formatSel, ReadOnlySpan<byte> data)
    {
        char std = "GRlO"[formatSel % 4];
        long ticks = 0;
        for (int i = 0; i < 8 && i < data.Length; i++)
        {
            ticks |= (long)data[i] << (8 * i);
        }

        ticks = Math.Abs(ticks % (DateTime.MaxValue.Ticks + 1));
        var value = new DateTimeOffset(new DateTime(ticks, DateTimeKind.Unspecified), TimeSpan.Zero);
        byte[] buffer = new byte[64];
        if (Utf8Formatter.TryFormat(value, buffer, out int written, new StandardFormat(std)))
        {
            Check(Utf8Parser.TryParse(buffer.AsSpan(0, written), out DateTimeOffset round, out int consumed, std) && consumed == written,
                () => $"Utf8Parser DateTimeOffset('{std}') didn't parse back '{Encoding.ASCII.GetString(buffer, 0, written)}'");
        }

        var dt = new DateTime(ticks, DateTimeKind.Utc);
        if (Utf8Formatter.TryFormat(dt, buffer, out int w2, new StandardFormat(std)))
        {
            Check(Utf8Parser.TryParse(buffer.AsSpan(0, w2), out DateTime round, out int consumed, std) && consumed == w2,
                () => $"Utf8Parser DateTime('{std}') didn't parse back '{Encoding.ASCII.GetString(buffer, 0, w2)}'");
            _ = round;
        }

        FuzzParse<DateTime>(data, std, (ReadOnlySpan<byte> s, out DateTime v, out int c, char f) => Utf8Parser.TryParse(s, out v, out c, f));
        FuzzParse<DateTimeOffset>(data, std, (ReadOnlySpan<byte> s, out DateTimeOffset v, out int c, char f) => Utf8Parser.TryParse(s, out v, out c, f));
    }

    private static void TimeSpans(byte formatSel, ReadOnlySpan<byte> data)
    {
        char std = "cgGt"[formatSel % 4];
        long ticks = 0;
        for (int i = 0; i < 8 && i < data.Length; i++)
        {
            ticks |= (long)data[i] << (8 * i);
        }

        var value = new TimeSpan(ticks);
        byte[] buffer = new byte[48];
        if (Utf8Formatter.TryFormat(value, buffer, out int written, new StandardFormat(std == 'G' ? 'c' : std)))
        {
            Check(Utf8Parser.TryParse(buffer.AsSpan(0, written), out TimeSpan round, out int consumed, std == 'G' ? 'c' : std) && round == value && consumed == written,
                () => $"Utf8Parser TimeSpan('{std}') didn't round-trip {value.Ticks} from '{Encoding.ASCII.GetString(buffer, 0, written)}'");
        }

        FuzzParse<TimeSpan>(data, std == 'G' ? 'c' : std, (ReadOnlySpan<byte> s, out TimeSpan v, out int c, char f) => Utf8Parser.TryParse(s, out v, out c, f));
    }

    // Feed fuzzed bytes placed against a guard page, so any read past the end faults. bytesConsumed must be in range, and
    // re-parsing the consumed prefix must give the same result.
    private static void FuzzParse<T>(ReadOnlySpan<byte> data, char std, ParseFn<T> parse) where T : struct, IEquatable<T>
    {
        int length = Math.Min(data.Length, 300);
        using PooledBoundedMemory<byte> memory = PooledBoundedMemory<byte>.Rent(data.Slice(0, length), PoisonPagePlacement.After);
        ReadOnlySpan<byte> source = memory.Span;

        bool ok = parse(source, out T value, out int consumed, std);
        if (!ok)
        {
            return;
        }

        Check(consumed >= 0 && consumed <= length, () => $"Utf8Parser<{typeof(T).Name}>('{std}') consumed {consumed} of {length}");

        using PooledBoundedMemory<byte> prefix = PooledBoundedMemory<byte>.Rent(data.Slice(0, consumed), PoisonPagePlacement.After);
        Check(parse(prefix.Span, out T again, out int consumed2, std) && again.Equals(value) && consumed2 == consumed,
            () => $"Utf8Parser<{typeof(T).Name}>('{std}') consumed {consumed} bytes returning {value}, but re-parsing that prefix gave {(consumed2 == consumed ? "a different value" : $"consumed {consumed2}")}");
    }

    private static char Std(char format) => format;
    private static string NumFmt(char std) => std switch { 'D' or 'G' => "D", 'N' => "N", 'X' => "X", _ => std.ToString() };

    private static void Check(bool condition, Func<string> message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message());
        }
    }
}
