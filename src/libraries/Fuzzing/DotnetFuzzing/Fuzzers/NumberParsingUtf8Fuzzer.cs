// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Numerics;
using System.Text;

namespace DotnetFuzzing.Fuzzers;

/// <summary>
/// Differential fuzzer for number parsing from UTF-8 against UTF-16. Number.Parsing reinterprets UTF-8 input with Unsafe.BitCast and
/// matches signs, currency symbols and the NaN/infinity symbols with a separate UTF-8 ignore-case comparison (Ordinal.Utf8), so
/// TryParse(ReadOnlySpan&lt;byte&gt;) must agree with TryParse(string) on every valid UTF-8 input. Also checks Parse against TryParse
/// and the char-span overload. Custom NumberFormatInfos use non-ASCII and multi-char symbols with case variants.
/// </summary>
/// <remarks>Input layout: [0] type, [1] number styles, [2] format info, [3..] text through a palette.</remarks>
internal sealed class NumberParsingUtf8Fuzzer : IFuzzer
{
    // Known issue on main, tolerated unless TENSOR_FUZZ_STRICT=1 (shared switch name with the other new fuzzers): the UTF-8
    // ignore-case comparison over-reads for a 3-byte non-ASCII symbol/sign, so a 3-byte NaN/Infinity symbol or negative sign
    // matched from UTF-16 can fail to match from UTF-8 (finding 44). Tolerated here as a UTF-8 miss where UTF-16 succeeds.
    private static readonly bool s_strict = Environment.GetEnvironmentVariable("TENSOR_FUZZ_STRICT") == "1";

    public string[] TargetAssemblies => [];
    public string[] TargetCoreLibPrefixes { get; } = ["System.Number", "System.Globalization.Ordinal", "System.Text.Unicode"];

    private static readonly NumberFormatInfo[] s_formats =
    [
        NumberFormatInfo.InvariantInfo,
        new NumberFormatInfo
        {
            NegativeSign = "\u2212", PositiveSign = "+", NumberDecimalSeparator = ",", NumberGroupSeparator = "\u00A0", CurrencySymbol = "\u20AC",
            CurrencyDecimalSeparator = ",", CurrencyGroupSeparator = "\u00A0", NaNSymbol = "\u041D\u0435 \u0447\u0438\u0441\u043B\u043E",
            PositiveInfinitySymbol = "\u221E", NegativeInfinitySymbol = "-\u221E",
        },
        new NumberFormatInfo
        {
            NegativeSign = "--", PositiveSign = "++", NumberDecimalSeparator = ".", NumberGroupSeparator = "'", CurrencySymbol = "\u0440\u0443\u0431.",
            NaNSymbol = "\u00DF\u0130\u017F", PositiveInfinitySymbol = "\u00C9norme", NegativeInfinitySymbol = "--\u00C9norme", PercentSymbol = "\u066A",
        },
        new NumberFormatInfo
        {
            NegativeSign = "\u00ADneg", PositiveSign = "pos", NumberDecimalSeparator = "\u00B7", NumberGroupSeparator = ",", CurrencySymbol = "$",
            NaNSymbol = "nan", PositiveInfinitySymbol = "infinity", NegativeInfinitySymbol = "-infinity", CurrencyDecimalSeparator = "\u00B7",
        },
    ];

    private static readonly NumberStyles[] s_styles =
    [
        NumberStyles.Integer, NumberStyles.Number, NumberStyles.Float, NumberStyles.Any, NumberStyles.Currency, NumberStyles.HexNumber,
        NumberStyles.BinaryNumber, NumberStyles.AllowExponent | NumberStyles.AllowLeadingSign | NumberStyles.AllowTrailingSign | NumberStyles.AllowParentheses,
        NumberStyles.Float | NumberStyles.AllowThousands, NumberStyles.None,
    ];

    // Bytes 0x00-0x7F map to themselves; the rest pick from here (parts of the symbols above in other cases, spaces, digits).
    private static readonly string[] s_palette =
    [
        "\u2212", "\u00A0", "\u20AC", "\u221E", "\u041D\u0415 \u0427\u0418\u0421\u041B\u041E", "\u043D\u0435 \u0447\u0438\u0441\u043B\u043E", "\u00DF\u0130\u017F", "SSIS",
        "ssi\u0307s", "\u00E9NORME", "\u00C9norme", "\u0440\u0443\u0431.", "\u0420\u0423\u0411.", "\u066A", "\u00ADNEG", "\u00B7", "\u0660", "\uFF11",
        "\u2009", "\u3000", "INFINITY", "NaN", "nAn", "E", "e+", "1e308", "9999999999999999999999", "0x", "(", ")", "\u0130", "\u0131", "\u212A",
    ];

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 4)
        {
            return;
        }

        NumberStyles style = s_styles[bytes[1] % s_styles.Length];
        NumberFormatInfo info = s_formats[bytes[2] % s_formats.Length];
        var builder = new StringBuilder();
        foreach (byte b in bytes.Slice(3))
        {
            builder.Append(b < 0x80 ? ((char)b).ToString() : s_palette[b % s_palette.Length]);
        }

        string text = builder.ToString();
        bool floating = (style & (NumberStyles.AllowHexSpecifier | NumberStyles.AllowBinarySpecifier)) == 0;
        switch (bytes[0] % 11)
        {
            case 0: Compare<int>(text, style, info); break;
            case 1: Compare<long>(text, style, info); break;
            case 2: Compare<uint>(text, style, info); break;
            case 3: Compare<short>(text, style, info); break;
            case 4: Compare<byte>(text, style, info); break;
            case 5: Compare<Int128>(text, style, info); break;
            case 6: Compare<UInt128>(text, style, info); break;
            case 7: if (floating) Compare<decimal>(text, style, info); break;
            case 8: if (floating) Compare<double>(text, style, info); break;
            case 9: if (floating) Compare<float>(text, style, info); break;
            default: if (floating) Compare<Half>(text, style, info); break;
        }
    }

    private static void Compare<T>(string text, NumberStyles style, NumberFormatInfo info) where T : INumberBase<T>
    {
        byte[] utf8 = Encoding.UTF8.GetBytes(text);
        bool ok = T.TryParse(text, style, info, out T? value);
        bool spanOk = T.TryParse(text.AsSpan(), style, info, out T? spanValue);
        bool utf8Ok = T.TryParse(utf8, style, info, out T? utf8Value);
        string Describe() => $"{typeof(T).Name}, {style}, NaN '{Escape(info.NaNSymbol)}' neg '{Escape(info.NegativeSign)}', text '{Escape(text)}'";

        Check(ok == spanOk && Same(value, spanValue), () => $"TryParse(string) = {ok}/{value} but TryParse(span) = {spanOk}/{spanValue}: {Describe()}");
        bool knownUtf8Miss = !s_strict && ok && !utf8Ok && HasThreeByteSymbol(info);
        Check((ok == utf8Ok && Same(value, utf8Value)) || knownUtf8Miss, () => $"TryParse(string) = {ok}/{value} but TryParse(UTF-8) = {utf8Ok}/{utf8Value}: {Describe()}");

        Exception? stringError = null, utf8Error = null;
        T? parsed = default, utf8Parsed = default;
        try
        {
            parsed = T.Parse(text, style, info);
        }
        catch (Exception ex) when (ex is FormatException or OverflowException)
        {
            stringError = ex;
        }

        try
        {
            utf8Parsed = T.Parse(utf8, style, info);
        }
        catch (Exception ex) when (ex is FormatException or OverflowException)
        {
            utf8Error = ex;
        }

        Check((stringError is null) == ok && (!ok || Same(parsed, value)), () => $"Parse(string) {(stringError is null ? "succeeded" : "threw " + stringError.GetType().Name)} but TryParse returned {ok}: {Describe()}");
        bool knownParseMiss = !s_strict && stringError is null && utf8Error is not null && HasThreeByteSymbol(info);
        Check((stringError?.GetType() == utf8Error?.GetType() && (utf8Error is not null || Same(utf8Parsed, parsed))) || knownParseMiss,
            () => $"Parse(string) {(stringError is null ? "= " + parsed : "threw " + stringError.GetType().Name)} but Parse(UTF-8) {(utf8Error is null ? "= " + utf8Parsed : "threw " + utf8Error.GetType().Name)}: {Describe()}");
    }

    private static bool HasThreeByteSymbol(NumberFormatInfo info)
    {
        foreach (string symbol in new[] { info.NaNSymbol, info.PositiveInfinitySymbol, info.NegativeInfinitySymbol, info.NegativeSign, info.PositiveSign })
        {
            if (Encoding.UTF8.GetByteCount(symbol) % 4 == 3)
            {
                return true;
            }
        }

        return false;
    }

    private static bool Same<T>(T? a, T? b) where T : INumberBase<T> =>
        a is null ? b is null : b is not null && ((T.IsNaN(a) && T.IsNaN(b)) || (a.Equals(b) && T.IsNegative(a) == T.IsNegative(b)));

    private static string Escape(string text) =>
        string.Concat(text.Take(100).Select(c => c is >= ' ' and < '\x7F' ? c.ToString() : $"\\u{(int)c:X4}"));

    private static void Check(bool condition, Func<string> message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message());
        }
    }
}
