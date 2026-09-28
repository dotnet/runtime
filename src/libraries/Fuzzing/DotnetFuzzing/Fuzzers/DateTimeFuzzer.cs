// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;

namespace DotnetFuzzing.Fuzzers;

/// <summary>
/// Fuzzes formatting and parsing of DateTime, DateTimeOffset, TimeSpan, DateOnly and TimeOnly. For a value built from fuzzed
/// fields it checks that ToString, TryFormat into a char span and TryFormat into a UTF-8 span all agree (the UTF-8 path goes
/// through the IUtf8SpanFormattable implementation, so this is the format-side analogue of the number-parsing UTF-8 checks),
/// that a too-small destination fails cleanly without writing past the end, and that the round-trippable formats ("o", "r", "s"
/// and the default) parse back to the same value. It also feeds fuzzed text to TryParse/ParseExact and checks TryParse agrees
/// with Parse and that ParseExact of a value's own formatted form round-trips.
/// </summary>
/// <remarks>Input layout: [0] type, [1] format selector, [2] culture/style selector, [3..] fields and parse text.</remarks>
internal sealed class DateTimeFuzzer : IFuzzer
{
    public string[] TargetAssemblies => [];
    public string[] TargetCoreLibPrefixes { get; } =
        ["System.DateTime", "System.TimeSpan", "System.DateOnly", "System.TimeOnly", "System.Globalization.DateTime", "System.Globalization.TimeSpan", "System.Globalization.GregorianCalendar", "System.Number"];

    private static readonly CultureInfo[] s_cultures =
    [
        CultureInfo.InvariantCulture,
        CultureInfo.GetCultureInfo("en-US"),
        CultureInfo.GetCultureInfo("de-DE"),
        CultureInfo.GetCultureInfo("fr-FR"),
        CultureInfo.GetCultureInfo("ja-JP"),
        CultureInfo.GetCultureInfo("ar-SA"), // Umm al-Qura calendar
        CultureInfo.GetCultureInfo("th-TH"), // Buddhist calendar
        CultureInfo.GetCultureInfo("fa-IR"), // Persian calendar
    ];

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 4)
        {
            return;
        }

        CultureInfo culture = s_cultures[bytes[2] % s_cultures.Length];
        ReadOnlySpan<byte> data = bytes.Slice(3);
        switch (bytes[0] % 5)
        {
            case 0: DateTimes(bytes[1], culture, data); break;
            case 1: DateTimeOffsets(bytes[1], culture, data); break;
            case 2: TimeSpans(bytes[1], culture, data); break;
            case 3: DateOnlys(bytes[1], culture, data); break;
            default: TimeOnlys(bytes[1], culture, data); break;
        }
    }

    private static readonly string[] s_dateTimeFormats = ["o", "O", "r", "R", "s", "u", "G", "g", "F", "f", "d", "D", "t", "T", "M", "Y", "yyyy-MM-ddTHH:mm:ss.fffffffK", "ddd, dd MMM yyyy", "", "yyyyMMdd", "HH:mm:ss.FFFFFFF"];

    private static void DateTimes(byte selector, CultureInfo culture, ReadOnlySpan<byte> data)
    {
        long ticks = ReadInt64(data) % (DateTime.MaxValue.Ticks + 1);
        if (ticks < 0)
        {
            ticks = -ticks;
        }

        DateTimeKind kind = (DateTimeKind)(data.Length > 0 ? data[0] % 3 : 0);
        var value = new DateTime(ticks, kind);
        string format = s_dateTimeFormats[selector % s_dateTimeFormats.Length];
        if (SafeToString(value, format, culture) is not string formatted)
        {
            return;
        }

        string text = FormatChecks(value, format, culture, formatted);

        // Round trips: "o", "s" and "u" are culture-independent and exact; "r" is invariant.
        if (format is "o" or "O")
        {
            Check(DateTime.TryParseExact(text, format, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime back) && back == value && back.Kind == value.Kind,
                () => $"DateTime \"{format}\" '{Escape(text)}' didn't round-trip {value.Ticks}/{value.Kind}");
            Check(DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime back2) && back2 == value,
                () => $"DateTime.TryParse of \"{format}\" '{Escape(text)}' didn't round-trip {value.Ticks}");
        }
        else if (format == "s")
        {
            Check(DateTime.TryParseExact(text, "s", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime back) && back == value.AddTicks(-(value.Ticks % TimeSpan.TicksPerSecond)),
                () => $"DateTime \"s\" '{Escape(text)}' didn't round-trip to the second for {value.Ticks}");
        }
        else if (format == "r" || format == "R")
        {
            DateTime utc = value.ToUniversalTime();
            Check(DateTime.TryParseExact(text, "r", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime back) && back == value.AddTicks(-(value.Ticks % TimeSpan.TicksPerSecond)),
                () => $"DateTime \"r\" '{Escape(text)}' didn't round-trip to the second for {value.Ticks}");
            _ = utc;
        }

        ParseFuzz<DateTime>(data, culture, (string s, IFormatProvider p, out DateTime r) => DateTime.TryParse(s, p, DateTimeStyles.None, out r), s => DateTime.Parse(s, culture));
    }

    private static void DateTimeOffsets(byte selector, CultureInfo culture, ReadOnlySpan<byte> data)
    {
        long ticks = ReadInt64(data) % (DateTime.MaxValue.Ticks + 1);
        if (ticks < 0)
        {
            ticks = -ticks;
        }

        int offsetMinutes = (data.Length > 8 ? (sbyte)data[8] : 0) % 840; // +-14:00
        TimeSpan offset = TimeSpan.FromMinutes(offsetMinutes);
        DateTimeOffset value;
        try
        {
            value = new DateTimeOffset(ticks, offset);
        }
        catch (ArgumentOutOfRangeException)
        {
            return; // The UTC instant fell outside the range.
        }

        string format = s_dateTimeFormats[selector % s_dateTimeFormats.Length];
        if (SafeToString(value, format, culture) is not string formatted)
        {
            return;
        }

        string text = FormatChecks(value, format, culture, formatted);

        if (format is "o" or "O")
        {
            Check(DateTimeOffset.TryParseExact(text, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset back) && back == value && back.Offset == value.Offset,
                () => $"DateTimeOffset \"{format}\" '{Escape(text)}' didn't round-trip {value.Ticks}/{value.Offset}");
        }
        else if (format == "u")
        {
            Check(DateTimeOffset.TryParseExact(text, "u", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset back) && back.UtcDateTime == value.UtcDateTime.AddTicks(-(value.UtcTicks % TimeSpan.TicksPerSecond)),
                () => $"DateTimeOffset \"u\" '{Escape(text)}' didn't round-trip {value.UtcTicks}");
        }

        ParseFuzz<DateTimeOffset>(data, culture, (string s, IFormatProvider p, out DateTimeOffset r) => DateTimeOffset.TryParse(s, p, DateTimeStyles.None, out r), s => DateTimeOffset.Parse(s, culture));
    }

    private static readonly string[] s_timeSpanFormats = ["c", "g", "G", "", @"dd\.hh\:mm\:ss", @"hh\:mm", "%d", @"dd\:hh\:mm\:ss\.fffffff"];

    private static void TimeSpans(byte selector, CultureInfo culture, ReadOnlySpan<byte> data)
    {
        var value = new TimeSpan(ReadInt64(data));
        string format = s_timeSpanFormats[selector % s_timeSpanFormats.Length];
        string text;
        try
        {
            text = value.ToString(format, culture);
        }
        catch (FormatException)
        {
            return; // Custom formats with too many digits etc.
        }

        text = FormatChecks(value, format, culture, text);

        if (format is "c" or "")
        {
            Check(TimeSpan.TryParseExact(text, "c", CultureInfo.InvariantCulture, out TimeSpan back) && back == value,
                () => $"TimeSpan \"c\" '{Escape(text)}' didn't round-trip {value.Ticks}");
            Check(TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out TimeSpan back2) && back2 == value,
                () => $"TimeSpan.TryParse of '{Escape(text)}' didn't round-trip {value.Ticks}");
        }

        ParseFuzz<TimeSpan>(data, culture, (string s, IFormatProvider p, out TimeSpan r) => TimeSpan.TryParse(s, p, out r), s => TimeSpan.Parse(s, culture));
    }

    private static readonly string[] s_dateOnlyFormats = ["o", "O", "r", "R", "d", "D", "m", "M", "y", "Y", "", "yyyy-MM-dd", "ddd dd MMM yyyy"];

    private static void DateOnlys(byte selector, CultureInfo culture, ReadOnlySpan<byte> data)
    {
        int dayNumber = (int)(ReadInt64(data) & 0x7FFFFFFF) % (DateOnly.MaxValue.DayNumber + 1);
        DateOnly value = DateOnly.FromDayNumber(dayNumber);
        string format = s_dateOnlyFormats[selector % s_dateOnlyFormats.Length];
        if (SafeToString(value, format, culture) is not string formatted)
        {
            return;
        }

        string text = FormatChecks(value, format, culture, formatted);

        if (format is "o" or "O")
        {
            Check(DateOnly.TryParseExact(text, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly back) && back == value,
                () => $"DateOnly \"{format}\" '{Escape(text)}' didn't round-trip {value.DayNumber}");
        }

        ParseFuzz<DateOnly>(data, culture, (string s, IFormatProvider p, out DateOnly r) => DateOnly.TryParse(s, p, DateTimeStyles.None, out r), s => DateOnly.Parse(s, culture));
    }

    private static readonly string[] s_timeOnlyFormats = ["o", "O", "r", "R", "t", "T", "", "HH:mm:ss.fffffff", "hh:mm tt"];

    private static void TimeOnlys(byte selector, CultureInfo culture, ReadOnlySpan<byte> data)
    {
        long ticks = ReadInt64(data) % TimeSpan.TicksPerDay;
        if (ticks < 0)
        {
            ticks += TimeSpan.TicksPerDay;
        }

        var value = new TimeOnly(ticks);
        string format = s_timeOnlyFormats[selector % s_timeOnlyFormats.Length];
        if (SafeToString(value, format, culture) is not string formatted)
        {
            return;
        }

        string text = FormatChecks(value, format, culture, formatted);

        if (format is "o" or "O")
        {
            Check(TimeOnly.TryParseExact(text, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly back) && back == value,
                () => $"TimeOnly \"{format}\" '{Escape(text)}' didn't round-trip {value.Ticks}");
        }

        ParseFuzz<TimeOnly>(data, culture, (string s, IFormatProvider p, out TimeOnly r) => TimeOnly.TryParse(s, p, DateTimeStyles.None, out r), s => TimeOnly.Parse(s, culture));
    }

    // Non-Gregorian cultures (ar-SA, th-TH, fa-IR) can't represent every DateTime; ToString throws ArgumentOutOfRangeException.
    private static string? SafeToString<T>(T value, string format, CultureInfo culture) where T : IFormattable
    {
        try
        {
            return value.ToString(format, culture);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    // ToString, TryFormat(char span) and TryFormat(UTF-8 span) must all agree, and a too-small buffer must fail cleanly.
    private static string FormatChecks<T>(T value, string format, CultureInfo culture, string text) where T : ISpanFormattable, IUtf8SpanFormattable
    {
        char[] chars = new char[text.Length + 4];
        Check(value.TryFormat(chars, out int written, format, culture) && new string(chars, 0, written) == text,
            () => $"{typeof(T).Name} \"{format}\": TryFormat(chars) = '{Escape(new string(chars, 0, written))}', ToString = '{Escape(text)}'");

        if (text.Length > 0)
        {
            // A failed TryFormat may scribble in the destination; only the return value and charsWritten are contractual.
            char[] small = new char[text.Length - 1];
            Check(!value.TryFormat(small, out int w, format, culture) && w == 0,
                () => $"{typeof(T).Name} \"{format}\": TryFormat into {text.Length - 1} chars returned true or wrote {w} for '{Escape(text)}'");
        }

        byte[] utf8 = new byte[Encoding.UTF8.GetByteCount(text) + 4];
        Check(value.TryFormat(utf8, out int utf8Written, format, culture) && Encoding.UTF8.GetString(utf8, 0, utf8Written) == text,
            () => $"{typeof(T).Name} \"{format}\": UTF-8 TryFormat = '{Escape(Encoding.UTF8.GetString(utf8, 0, utf8Written))}', ToString = '{Escape(text)}'");

        int exactUtf8 = Encoding.UTF8.GetByteCount(text);
        if (exactUtf8 > 0)
        {
            byte[] smallUtf8 = new byte[exactUtf8 - 1];
            Check(!value.TryFormat(smallUtf8, out int wb, format, culture) && wb == 0,
                () => $"{typeof(T).Name} \"{format}\": UTF-8 TryFormat into {exactUtf8 - 1} bytes succeeded for '{Escape(text)}'");
        }

        return text;
    }

    private delegate bool TryParse<T>(string text, IFormatProvider provider, out T result);

    private static void ParseFuzz<T>(ReadOnlySpan<byte> data, CultureInfo culture, TryParse<T> tryParse, Func<string, T> parse)
    {
        // Build fuzzed text from a palette that favours date/time punctuation and tokens.
        var builder = new StringBuilder();
        foreach (byte b in data)
        {
            builder.Append(b < 0x80 ? ((char)b).ToString() : s_palette[b % s_palette.Length]);
        }

        string text = builder.ToString();
        if (text.Length == 0 || text.Length > 500)
        {
            return;
        }

        bool ok = tryParse(text, culture, out T value);
        bool spanOk = TryParseSpan(text, culture, out T spanValue, tryParse);
        Check(ok == spanOk && (!ok || EqualityComparer<T>.Default.Equals(value, spanValue)), () => $"{typeof(T).Name}: TryParse(string) and TryParse(span) disagree for '{Escape(text)}' ({culture.Name})");

        T parsed = default!;
        bool threw = false;
        try
        {
            parsed = parse(text);
        }
        catch (FormatException)
        {
            threw = true;
        }
        catch (ArgumentOutOfRangeException)
        {
            threw = true; // Some calendars throw this for out-of-range dates.
        }
        catch (OverflowException)
        {
            threw = true; // TimeSpan overflow.
        }

        Check(ok != threw && (!ok || EqualityComparer<T>.Default.Equals(parsed, value)), () => $"{typeof(T).Name}: TryParse = {ok} but Parse {(threw ? "threw" : "succeeded")} for '{Escape(text)}' ({culture.Name})");
    }

    private static bool TryParseSpan<T>(string text, CultureInfo culture, out T result, TryParse<T> _)
    {
        // Route through the ReadOnlySpan<char> overloads by type.
        result = default!;
        switch (result)
        {
            case DateTime: { bool ok = DateTime.TryParse(text.AsSpan(), culture, DateTimeStyles.None, out DateTime r); result = (T)(object)r; return ok; }
            case DateTimeOffset: { bool ok = DateTimeOffset.TryParse(text.AsSpan(), culture, DateTimeStyles.None, out DateTimeOffset r); result = (T)(object)r; return ok; }
            case TimeSpan: { bool ok = TimeSpan.TryParse(text.AsSpan(), culture, out TimeSpan r); result = (T)(object)r; return ok; }
            case DateOnly: { bool ok = DateOnly.TryParse(text.AsSpan(), culture, DateTimeStyles.None, out DateOnly r); result = (T)(object)r; return ok; }
            case TimeOnly: { bool ok = TimeOnly.TryParse(text.AsSpan(), culture, DateTimeStyles.None, out TimeOnly r); result = (T)(object)r; return ok; }
            default: return false;
        }
    }

    private static readonly string[] s_palette =
    [
        "-", "/", ":", ".", ",", " ", "T", "Z", "+", "am", "PM", "1", "12", "30", "59", "2024", "0000", "9999", "Jan", "Dec",
        "Mon", " ", "\t", "GMT", "UTC", "٠١", "−", "13", "24", "60", "99", "000000000", "１", "W",
    ];

    private static long ReadInt64(ReadOnlySpan<byte> data)
    {
        long value = 0;
        for (int i = 0; i < 8 && i < data.Length; i++)
        {
            value |= (long)data[i] << (8 * i);
        }

        return value;
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
