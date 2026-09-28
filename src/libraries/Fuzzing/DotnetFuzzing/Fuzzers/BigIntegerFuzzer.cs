// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace DotnetFuzzing.Fuzzers;

/// <summary>
/// Differential fuzzer for <see cref="BigInteger"/>. Operands are built from "recipes" that produce the shapes the arithmetic
/// kernels special-case (low zero-limb prefixes, powers of two, repeated and shifted-repeated limbs, factors of 3, 5 and 7,
/// B^k - 1 Mersenne-like values, long random runs past the Karatsuba/Toom thresholds), and every result is compared with a
/// small, independent reference implementation on 32-bit words (schoolbook multiplication, Knuth division).
/// </summary>
/// <remarks>
/// Input layout: [0] selector for the expensive operation (Pow, ModPow, GCD, parsing), then two operand recipes, then an int
/// used for shift amounts and exponents; the tail is also used as text for the parsing checks.
/// </remarks>
internal sealed class BigIntegerFuzzer : IFuzzer
{
    // Known issues on main, tolerated unless TENSOR_FUZZ_STRICT=1 (shared switch name with the other new fuzzers).
    private static readonly bool s_strict = Environment.GetEnvironmentVariable("TENSOR_FUZZ_STRICT") == "1";

    public string[] TargetAssemblies { get; } = ["System.Runtime.Numerics"];
    public string[] TargetCoreLibPrefixes => [];

    public void FuzzTarget(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 3)
        {
            return;
        }

        var reader = new Reader(bytes.ToArray());
        byte selector = reader.Next();
        R ra = reader.ReadOperand();
        R rb = reader.ReadOperand();
        int amount = reader.NextInt();

        BigInteger a = ToBig(ra);
        BigInteger b = ToBig(rb);
        Check(FromBig(a) == ra, () => $"new BigInteger(bytes) / ToByteArray doesn't round-trip {ra}: got {FromBig(a)}");
        Check(FromBig(b) == rb, () => $"new BigInteger(bytes) / ToByteArray doesn't round-trip {rb}: got {FromBig(b)}");

        Unary(a, ra, amount);
        Binary(a, ra, b, rb);

        switch (selector % 5)
        {
            case 0: PowCheck(a, ra, amount); break;
            case 1: ModPowCheck(a, ra, b, rb, reader.ReadOperand()); break;
            case 2: GcdCheck(a, ra, b, rb); break;
            case 3: ParseCheck(bytes.Slice(1)); break;
            default: FormatCheck(a, ra, amount); break;
        }
    }

    private static void Unary(BigInteger a, R ra, int amount)
    {
        // Comparisons, sign and hashing.
        Check(a.Sign == ra.Sign && a.IsZero == (ra.Sign == 0) && a.IsOne == (ra == R.One) && a.IsEven == ra.IsEven,
            () => $"Sign/IsZero/IsOne/IsEven wrong for {ra}");
        Check(-a == ToBig(-ra) && BigInteger.Abs(a) == ToBig(R.Abs(ra)), () => $"negation/Abs wrong for {ra}");
        Check(a + BigInteger.One == ToBig(ra + R.One) && a - BigInteger.One == ToBig(ra - R.One), () => $"increment/decrement wrong for {ra}");

        // Decimal text through the reference parser, and hex/binary round trips.
        string text = a.ToString(CultureInfo.InvariantCulture);
        Check(R.ParseDecimal(text) == ra, () => $"ToString() of {ra} is {Truncate(text)}, which is {R.ParseDecimal(text)}");
        Check(BigInteger.Parse(text, CultureInfo.InvariantCulture) == a, () => $"Parse(ToString()) doesn't round-trip {ra}");
        string hex = a.ToString("X", CultureInfo.InvariantCulture);
        Check(BigInteger.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture) == a, () => $"hex {Truncate(hex)} doesn't round-trip {ra}");
        string binary = a.ToString("B", CultureInfo.InvariantCulture);
        Check(BigInteger.Parse(binary, NumberStyles.BinaryNumber, CultureInfo.InvariantCulture) == a, () => $"binary text doesn't round-trip {ra}");
        if (ra.Sign >= 0)
        {
            Check(hex.TrimStart('0') == ra.ToHex(), () => $"ToString(\"X\") of {ra} is {Truncate(hex)}");
        }

        // Byte round trips in all four layouts.
        foreach (bool bigEndian in new[] { false, true })
        {
            byte[] signed = a.ToByteArray(isUnsigned: false, isBigEndian: bigEndian);
            Check(new BigInteger(signed, isUnsigned: false, isBigEndian: bigEndian) == a && signed.Length == a.GetByteCount(),
                () => $"signed{(bigEndian ? " big-endian" : "")} bytes don't round-trip {ra}");
            if (ra.Sign >= 0)
            {
                byte[] unsigned = a.ToByteArray(isUnsigned: true, isBigEndian: bigEndian);
                Check(new BigInteger(unsigned, isUnsigned: true, isBigEndian: bigEndian) == a && unsigned.Length == a.GetByteCount(isUnsigned: true),
                    () => $"unsigned bytes don't round-trip {ra}");
            }

            byte[] small = new byte[Math.Max(0, signed.Length - 1)];
            Check(!a.TryWriteBytes(small, out int written, isUnsigned: false, isBigEndian: bigEndian) && written == 0,
                () => $"TryWriteBytes into {small.Length} bytes succeeded for {ra}, which needs {signed.Length}");
        }

        // Bit queries.
        if (ra.Sign > 0)
        {
            Check(BigInteger.Log2(a) == ra.BitLength - 1, () => $"Log2({ra}) = {BigInteger.Log2(a)}, expected {ra.BitLength - 1}");
            Check(BigInteger.PopCount(a) == ra.PopCount(), () => $"PopCount({ra}) = {BigInteger.PopCount(a)}, expected {ra.PopCount()}");
            Check(BigInteger.IsPow2(a) == (ra.PopCount() == 1), () => $"IsPow2({ra}) = {BigInteger.IsPow2(a)}");
        }

        if (ra.Sign != 0)
        {
            Check(BigInteger.TrailingZeroCount(a) == ra.TrailingZeroCount(), () => $"TrailingZeroCount({ra}) = {BigInteger.TrailingZeroCount(a)}, expected {ra.TrailingZeroCount()}");
        }

        // GetBitLength: the shortest two's complement form without the sign bit.
        long bitLength = ra.Sign >= 0 ? ra.BitLength : (R.Abs(ra) - R.One).BitLength;
        Check(a.GetBitLength() == bitLength, () => $"GetBitLength({ra}) = {a.GetBitLength()}, expected {bitLength}");

        // Shifts: << multiplies by 2^k, >> is floor division by 2^k.
        int shift = (int)((uint)amount % 3000);
        Check(a << shift == ToBig(ra.ShiftLeft(shift)), () => $"{ra} << {shift} wrong");
        Check(a >> shift == ToBig(ra.FloorShiftRight(shift)), () => $"{ra} >> {shift} = {FromBig(a >> shift)}, expected {ra.FloorShiftRight(shift)}");
        Check(a << -shift == a >> shift, () => $"{ra} << -{shift} != {ra} >> {shift}");
        Check(~a == ToBig(-ra - R.One), () => $"~{ra} wrong");

        // Conversions to floating point are correctly rounded (round half to even).
        double d = (double)a;
        double expectedDouble = ra.ToFloating(53, 1024);
        Check(d.Equals(expectedDouble), () => $"(double){ra} = {d:R}, expected {expectedDouble:R}");
        float f = (float)a;
        double expectedFloat = ra.ToFloating(24, 128);
        Check(((double)f).Equals(expectedFloat), () => $"(float){ra} = {f:R}, expected {(float)expectedFloat:R}");
        Half h = (Half)a;
        double expectedHalf = ra.ToFloating(11, 16);
        Check(((double)h).Equals(expectedHalf), () => $"(Half){ra} = {h}, expected {(Half)expectedHalf}");

        // Checked integer and decimal conversions throw exactly when the value doesn't fit.
        CheckedConversion(ra, "long", 63, true, () => (BigInteger)(long)a == a);
        CheckedConversion(ra, "ulong", 64, false, () => (BigInteger)(ulong)a == a);
        CheckedConversion(ra, "int", 31, true, () => (BigInteger)(int)a == a);
        CheckedConversion(ra, "Int128", 127, true, () => (BigInteger)(Int128)a == a);
        CheckedConversion(ra, "UInt128", 128, false, () => (BigInteger)(UInt128)a == a);
        bool fitsDecimal = ra.BitLength <= 96;
        decimal m = 0;
        bool threw = false;
        try
        {
            m = (decimal)a;
        }
        catch (OverflowException)
        {
            threw = true;
        }

        Check(threw != fitsDecimal, () => $"(decimal){ra} {(threw ? "threw" : "didn't throw")}");
        if (!threw)
        {
            Check((BigInteger)m == a, () => $"(decimal){ra} = {m}");
        }

        // double -> BigInteger truncates.
        double fromBits = BitConverter.Int64BitsToDouble((long)(ra.LowBits64() ^ ((ulong)amount << 20)));
        if (double.IsFinite(fromBits))
        {
            Check((BigInteger)fromBits == ToBig(R.FromDouble(fromBits)), () => $"(BigInteger){fromBits:R} = {(BigInteger)fromBits}, expected {R.FromDouble(fromBits)}");
        }
        else
        {
            Check(Throws<OverflowException>(() => _ = (BigInteger)fromBits), () => $"(BigInteger){fromBits} didn't throw OverflowException");
        }
    }

    private static void CheckedConversion(R value, string type, int bits, bool signed, Func<bool> convertAndCompare)
    {
        bool fits = signed
            ? value.Sign >= 0 ? value.BitLength <= bits : (R.Abs(value) - R.One).BitLength <= bits
            : value.Sign >= 0 && value.BitLength <= bits;
        bool threw = false, same = false;
        try
        {
            same = convertAndCompare();
        }
        catch (OverflowException)
        {
            threw = true;
        }

        Check(threw != fits, () => $"({type}){value} {(threw ? "threw" : "didn't throw")}");
        Check(threw || same, () => $"({type}){value} has the wrong value");
    }

    private static void Binary(BigInteger a, R ra, BigInteger b, R rb)
    {
        Check(a.CompareTo(b) == R.Compare(ra, rb) && (a == b) == (ra == rb) && (a < b) == (R.Compare(ra, rb) < 0),
            () => $"compare({ra}, {rb}) = {a.CompareTo(b)}, expected {R.Compare(ra, rb)}");
        if (a == b)
        {
            Check(a.GetHashCode() == b.GetHashCode(), () => $"equal values {ra} have different hash codes");
        }

        Check(a + b == ToBig(ra + rb), () => $"{ra} + {rb} = {FromBig(a + b)}, expected {ra + rb}");
        Check(a - b == ToBig(ra - rb), () => $"{ra} - {rb} = {FromBig(a - b)}, expected {ra - rb}");
        R product = ra * rb;
        Check(a * b == ToBig(product), () => $"{ra} * {rb} = {FromBig(a * b)}, expected {product}");
        Check(a * a == ToBig(ra * ra), () => $"{ra} squared = {FromBig(a * a)}, expected {ra * ra}");

        R[] pair = [ra, rb];
        BigInteger[] big = [a, b];
        for (int i = 0; i < 2; i++)
        {
            BigInteger dividend = big[i], divisor = big[1 - i];
            R rDividend = pair[i], rDivisor = pair[1 - i];
            if (rDivisor.Sign == 0)
            {
                Check(Throws<DivideByZeroException>(() => _ = dividend / divisor) && Throws<DivideByZeroException>(() => _ = dividend % divisor),
                    () => $"{rDividend} / 0 didn't throw DivideByZeroException");
                continue;
            }

            (R q, R r) = R.DivRem(rDividend, rDivisor);
            Check(dividend / divisor == ToBig(q), () => $"{rDividend} / {rDivisor} = {FromBig(dividend / divisor)}, expected {q}");
            Check(dividend % divisor == ToBig(r), () => $"{rDividend} % {rDivisor} = {FromBig(dividend % divisor)}, expected {r}");
            (BigInteger tq, BigInteger tr) = BigInteger.DivRem(dividend, divisor);
            BigInteger oq = BigInteger.DivRem(dividend, divisor, out BigInteger or);
            Check(tq == ToBig(q) && tr == ToBig(r) && oq == ToBig(q) && or == ToBig(r), () => $"DivRem({rDividend}, {rDivisor}) disagrees with / and %");
            Check((dividend * divisor) / divisor == dividend && (dividend * divisor) % divisor == 0, () => $"({rDividend} * {rDivisor}) / {rDivisor} != {rDividend}");
        }

        // Bitwise operators work on infinite two's complement.
        Check((a & b) == ToBig(R.Bitwise(ra, rb, (x, y) => x & y)), () => $"{ra} & {rb} = {FromBig(a & b)}, expected {R.Bitwise(ra, rb, (x, y) => x & y)}");
        Check((a | b) == ToBig(R.Bitwise(ra, rb, (x, y) => x | y)), () => $"{ra} | {rb} = {FromBig(a | b)}, expected {R.Bitwise(ra, rb, (x, y) => x | y)}");
        Check((a ^ b) == ToBig(R.Bitwise(ra, rb, (x, y) => x ^ y)), () => $"{ra} ^ {rb} = {FromBig(a ^ b)}, expected {R.Bitwise(ra, rb, (x, y) => x ^ y)}");

        Check(BigInteger.Max(a, b) == (R.Compare(ra, rb) >= 0 ? a : b) && BigInteger.Min(a, b) == (R.Compare(ra, rb) <= 0 ? a : b),
            () => $"Min/Max({ra}, {rb}) wrong");
    }

    private static void PowCheck(BigInteger a, R ra, int amount)
    {
        // Keep the result below ~200k bits.
        int exponent = (int)((uint)amount % 64);
        if ((long)ra.BitLength * exponent > 200_000)
        {
            exponent = (int)(200_000 / Math.Max(1, ra.BitLength));
        }

        R expected = R.One;
        for (int i = 0; i < exponent; i++)
        {
            expected *= ra;
        }

        Check(BigInteger.Pow(a, exponent) == ToBig(expected), () => $"Pow({ra}, {exponent}) = {FromBig(BigInteger.Pow(a, exponent))}, expected {expected}");
        Check(Throws<ArgumentOutOfRangeException>(() => BigInteger.Pow(a, -1 - exponent)), () => "Pow with a negative exponent didn't throw");
    }

    private static void ModPowCheck(BigInteger a, R ra, BigInteger b, R rb, R rm)
    {
        R exponent = R.Abs(rb);
        if (exponent.BitLength > 256)
        {
            exponent = exponent.FloorShiftRight(exponent.BitLength - 256);
        }

        if (rm.BitLength > 4096)
        {
            rm = rm.FloorShiftRight(rm.BitLength - 4096);
        }

        BigInteger e = ToBig(exponent), m = ToBig(rm);
        if (rm.Sign == 0)
        {
            Check(Throws<DivideByZeroException>(() => BigInteger.ModPow(a, e, m)), () => $"ModPow({ra}, {exponent}, 0) didn't throw");
            return;
        }

        if (exponent.Sign > 0)
        {
            Check(Throws<ArgumentOutOfRangeException>(() => BigInteger.ModPow(a, -e, m)), () => "ModPow with a negative exponent didn't throw");
        }

        // Square and multiply on the magnitude, then the sign of the remainder follows the sign of value^exponent.
        R modulus = R.Abs(rm);
        R @base = R.DivRem(R.Abs(ra), modulus).Remainder;
        R result = R.DivRem(R.One, modulus).Remainder;
        for (long bit = exponent.BitLength - 1; bit >= 0; bit--)
        {
            result = R.DivRem(result * result, modulus).Remainder;
            if (exponent.TestBit(bit))
            {
                result = R.DivRem(result * @base, modulus).Remainder;
            }
        }

        if (ra.Sign < 0 && !exponent.IsEven)
        {
            result = -result;
        }

        BigInteger actual = BigInteger.ModPow(a, e, m);
        Check(actual == ToBig(result), () => $"ModPow({ra}, {exponent}, {rm}) = {FromBig(actual)}, expected {result}");
    }

    private static void GcdCheck(BigInteger a, R ra, BigInteger b, R rb)
    {
        R x = R.Abs(ra), y = R.Abs(rb);
        while (y.Sign != 0)
        {
            (x, y) = (y, R.DivRem(x, y).Remainder);
        }

        BigInteger actual = BigInteger.GreatestCommonDivisor(a, b);
        Check(actual == ToBig(x), () => $"GreatestCommonDivisor({ra}, {rb}) = {FromBig(actual)}, expected {x}");
    }

    private static void FormatCheck(BigInteger a, R ra, int amount)
    {
        // Every format must agree between ToString and TryFormat (chars and UTF-8), and a too-small buffer must fail cleanly.
        string[] formats = ["D", "D40", "X", "X50", "x", "B", "B70", "N0", "G", "R", "E3", "e20", "C2", "P1", "F2"];
        string format = formats[(uint)amount % (uint)formats.Length];
        NumberFormatInfo culture = s_formats[((uint)amount >> 16) % (uint)s_formats.Length];
        string text = a.ToString(format, culture);
        char[] chars = new char[text.Length + 8];
        Check(a.TryFormat(chars, out int written, format, culture) && chars.AsSpan(0, written).SequenceEqual(text),
            () => $"TryFormat(\"{format}\") of {ra} disagrees with ToString: {Truncate(new string(chars, 0, written))} vs {Truncate(text)}");
        Array.Fill(chars, '\uFFFF');
        Check(!a.TryFormat(chars.AsSpan(0, text.Length - 1), out written, format, culture) && chars[text.Length - 1] == '\uFFFF',
            () => $"TryFormat(\"{format}\") of {ra} succeeded or wrote past a {text.Length - 1}-char buffer");
        byte[] utf8 = new byte[Encoding.UTF8.GetByteCount(text) + 8];
        Check(a.TryFormat(utf8, out written, format, culture) && Encoding.UTF8.GetString(utf8, 0, written) == text,
            () => $"UTF-8 TryFormat(\"{format}\") of {ra} disagrees with ToString");

        if (format is "D" or "D40" or "N0" or "R" or "G")
        {
            NumberStyles style = format == "N0" ? NumberStyles.Number : NumberStyles.Integer;
            bool parsed = BigInteger.TryParse(text, style, culture, out BigInteger back);
            Check(parsed && back == a,
                () => $"ToString(\"{format}\") of {ra} with {Describe(culture)} is '{Escape(Truncate(text))}', which parses as {(parsed ? FromBig(back) : "nothing")} ({style})");
        }
    }

    private static void ParseCheck(ReadOnlySpan<byte> input)
    {
        // Fuzzed text: string, char span and UTF-8 overloads must agree with each other, TryParse must agree with Parse, and
        // TryParsePartial must agree with TryParse on the consumed prefix. Plain decimal text is also checked against the
        // reference parser and against long for short inputs.
        if (input.Length < 1)
        {
            return;
        }

        NumberStyles[] styles = [NumberStyles.Integer, NumberStyles.Number, NumberStyles.HexNumber, NumberStyles.BinaryNumber, NumberStyles.Any, NumberStyles.AllowLeadingSign, NumberStyles.AllowExponent | NumberStyles.AllowLeadingSign];
        NumberStyles style = styles[input[0] % styles.Length];
        NumberFormatInfo culture = s_formats[(input[0] >> 5) % s_formats.Length];
        string text = Encoding.Latin1.GetString(input.Slice(1));

        bool ok = BigInteger.TryParse(text, style, culture, out BigInteger value);
        bool spanOk = BigInteger.TryParse(text.AsSpan(), style, culture, out BigInteger spanValue);
        Check(ok == spanOk && value == spanValue, () => $"TryParse(string) and TryParse(span) disagree for '{Escape(text)}' ({style})");
        BigInteger parsed = default;
        Exception? parseException = null;
        try
        {
            parsed = BigInteger.Parse(text, style, culture);
        }
        catch (FormatException ex)
        {
            parseException = ex;
        }
        catch (OverflowException ex)
        {
            parseException = ex; // Exponents can make the value too large.
        }

        Check(ok == (parseException is null) && (!ok || parsed == value), () => $"Parse and TryParse disagree for '{Escape(text)}' ({style}): {parseException?.GetType().Name}");

        if (text.All(char.IsAscii))
        {
            bool utf8Ok = BigInteger.TryParse(Encoding.ASCII.GetBytes(text), style, culture, out BigInteger utf8Value);
            Check(utf8Ok == ok && utf8Value == value, () => $"TryParse(UTF-8) and TryParse(string) disagree for '{Escape(text)}' ({style})");
        }

        bool partialOk = BigInteger.TryParsePartial(text.AsSpan(), style, culture, out BigInteger partialValue, out int consumed);
        Check(consumed >= 0 && consumed <= text.Length, () => $"TryParsePartial consumed {consumed} of {text.Length} chars");
        if (ok)
        {
            Check(partialOk && consumed == text.Length && partialValue == value,
                () => $"TryParse succeeded for '{Escape(text)}' ({style}) but TryParsePartial returned {partialOk}, consumed {consumed}, value {partialValue}");
        }

        if (partialOk)
        {
            bool prefixOk = BigInteger.TryParse(text.AsSpan(0, consumed), style, culture, out BigInteger prefixValue);
            Check(prefixOk && prefixValue == partialValue,
                () => $"TryParsePartial('{Escape(text)}', {style}) consumed {consumed} chars and returned {partialValue}, but TryParse of that prefix returned {prefixOk}, {prefixValue}");
        }
        else
        {
            Check(consumed == 0, () => $"TryParsePartial('{Escape(text)}', {style}) failed but reported {consumed} chars consumed");
        }

        // Pure decimal digits, optionally signed.
        string trimmed = text.TrimStart('-');
        if (trimmed.Length > 0 && trimmed.All(char.IsAsciiDigit) && text.Length - trimmed.Length <= 1 && (style & NumberStyles.AllowHexSpecifier) == 0 && (style & NumberStyles.AllowBinarySpecifier) == 0)
        {
            R expected = R.ParseDecimal(text);
            Check(!ReferenceEquals(culture, NumberFormatInfo.InvariantInfo) || (ok && FromBig(value) == expected), () => $"TryParse('{Escape(text)}', {style}) = {ok}, {value}; expected {expected}");
        }

        if (text.Length <= 40 && (style & (NumberStyles.AllowHexSpecifier | NumberStyles.AllowBinarySpecifier)) == 0 && long.TryParse(text, style, culture, out long l))
        {
            Check(ok && value == l, () => $"long.TryParse('{Escape(text)}', {style}) = {l} but BigInteger.TryParse returned {ok}, {value}");
        }
    }

    // BigInteger <-> reference conversions. ToBig goes through the unsigned little-endian byte constructor and FromBig through
    // ToByteArray; both are checked against each other on every operand.
    private static readonly NumberFormatInfo[] s_formats =
    [
        NumberFormatInfo.InvariantInfo,
        new NumberFormatInfo { NumberGroupSeparator = ".", NumberDecimalSeparator = ",", CurrencyGroupSeparator = ".", CurrencyDecimalSeparator = ",", CurrencySymbol = "\u20AC" },
        new NumberFormatInfo { NegativeSign = "\u2212", PositiveSign = "+", NumberGroupSeparator = "\u00A0", NumberGroupSizes = [3, 2], PercentSymbol = "%%" },
        new NumberFormatInfo { NegativeSign = "--", PositiveSign = "+!", NumberGroupSeparator = "'", NumberDecimalSeparator = "\u00B7", CurrencySymbol = "$$", NumberGroupSizes = [1] },
    ];

    private static BigInteger ToBig(R value)
    {
        byte[] bytes = new byte[value.Words.Length * 4];
        for (int i = 0; i < value.Words.Length; i++)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(i * 4), value.Words[i]);
        }

        var magnitude = new BigInteger(bytes, isUnsigned: true, isBigEndian: false);
        return value.Sign < 0 ? -magnitude : magnitude;
    }

    private static R FromBig(BigInteger value)
    {
        byte[] bytes = BigInteger.Abs(value).ToByteArray(isUnsigned: true, isBigEndian: false);
        uint[] words = new uint[(bytes.Length + 3) / 4];
        for (int i = 0; i < bytes.Length; i++)
        {
            words[i / 4] |= (uint)bytes[i] << (8 * (i % 4));
        }

        return R.Create(value.Sign, words);
    }

    private static bool Throws<TException>(Action action) where TException : Exception
    {
        try
        {
            action();
            return false;
        }
        catch (TException)
        {
            return true;
        }
    }

    private static string Describe(NumberFormatInfo info) =>
        $"NegativeSign '{Escape(info.NegativeSign)}', PositiveSign '{Escape(info.PositiveSign)}', group '{Escape(info.NumberGroupSeparator)}' [{string.Join(",", info.NumberGroupSizes)}], decimal '{Escape(info.NumberDecimalSeparator)}'";

    private static string Truncate(string text) => text.Length <= 80 ? text : $"{text[..40]}...{text[^40..]} ({text.Length} chars)";

    private static string Escape(string text) =>
        string.Concat(text.Take(120).Select(c => c is >= ' ' and < '\x7F' ? c.ToString() : $"\\u{(int)c:X4}"));

    private static void Check(bool condition, Func<string> message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message());
        }
    }

    /// <summary>Reads operand recipes from the input.</summary>
    private sealed class Reader(byte[] data)
    {
        private int _position;

        private static readonly ulong[] s_palette =
        [
            0, 1, 2, 3, 5, 7, 9, 15, 21, 35, 0xFFFFFFFF, 0x100000000, 0xFFFFFFFFFFFFFFFF, 0x8000000000000000, 0x7FFFFFFFFFFFFFFF,
            0x5555555555555555, 0xAAAAAAAAAAAAAAAA, 0x0000000100000001, 0x3333333333333333, 0x8000000000000001, 0xFFFFFFFF00000000,
        ];

        public byte Next() => _position < data.Length ? data[_position++] : (byte)0;

        public int NextInt() => Next() | (Next() << 8) | (Next() << 16) | (Next() << 24);

        private ulong NextLimb()
        {
            byte b = Next();
            if (b < 0xC0)
            {
                return s_palette[b % s_palette.Length];
            }

            ulong value = 0;
            for (int i = 0; i < 8; i++)
            {
                value |= (ulong)Next() << (8 * i);
            }

            return value;
        }

        public R ReadOperand()
        {
            byte header = Next();
            int sign = (header & 0x80) != 0 ? -1 : 1;
            var limbs = new List<ulong>();
            switch ((header >> 3) & 7)
            {
                case 0: // A few limbs straight from the input.
                    for (int n = Next() % 6; n >= 0; n--)
                    {
                        limbs.Add(NextLimb());
                    }

                    break;

                case 1: // One limb repeated.
                {
                    ulong limb = NextLimb();
                    for (int n = Next() % 96; n >= 0; n--)
                    {
                        limbs.Add(limb);
                    }

                    break;
                }

                case 2: // Zero limbs, then a repeated limb, then maybe a different top limb.
                {
                    int zeros = Next() % 24;
                    ulong limb = NextLimb();
                    int count = Next() % 64;
                    limbs.AddRange(Enumerable.Repeat(0UL, zeros));
                    limbs.AddRange(Enumerable.Repeat(limb, count + 1));
                    if ((header & 1) != 0)
                    {
                        limbs.Add(NextLimb());
                    }

                    break;
                }

                case 3: // Low zero-limb prefix, then random limbs.
                {
                    limbs.AddRange(Enumerable.Repeat(0UL, Next() % 48));
                    for (int n = Next() % 5; n >= 0; n--)
                    {
                        limbs.Add(NextLimb());
                    }

                    break;
                }

                case 4: // 2^k, optionally +-1.
                {
                    int k = (Next() | (Next() << 8)) % 8192;
                    R value = R.One.ShiftLeft(k);
                    value = (header & 3) switch { 1 => value + R.One, 2 => value - R.One, _ => value };
                    return sign < 0 ? -value : value;
                }

                case 5: // B^k - 1 in 32- or 64-bit limbs, maybe shifted or times a small factor.
                {
                    int k = 1 + Next() % 80;
                    R value = R.One.ShiftLeft(k * ((header & 1) != 0 ? 64 : 32)) - R.One;
                    value = (header & 6) switch
                    {
                        2 => value.ShiftLeft(Next() % 200),
                        4 => value * R.FromUInt64(s_palette[Next() % 10]),
                        6 => value + R.FromUInt64(NextLimb()),
                        _ => value,
                    };
                    return sign < 0 ? -value : value;
                }

                case 6: // 3^a * 5^b * 7^c * 2^d.
                {
                    R value = R.One;
                    for (int i = Next() % 120; i > 0; i--) value *= R.FromUInt64(3);
                    for (int i = Next() % 60; i > 0; i--) value *= R.FromUInt64(5);
                    for (int i = Next() % 50; i > 0; i--) value *= R.FromUInt64(7);
                    value = value.ShiftLeft(Next() % 130);
                    return sign < 0 ? -value : value;
                }

                default: // Long pseudo-random run (past the Karatsuba/Toom thresholds), seeded from the input.
                {
                    int count = 1 + (Next() | (Next() << 8)) % 700;
                    ulong state = NextLimb() | 1;
                    for (int n = 0; n < count; n++)
                    {
                        state ^= state << 13;
                        state ^= state >> 7;
                        state ^= state << 17;
                        limbs.Add((header & 1) != 0 && n % 3 == 0 ? ulong.MaxValue : state);
                    }

                    break;
                }
            }

            uint[] words = new uint[limbs.Count * 2];
            for (int i = 0; i < limbs.Count; i++)
            {
                words[2 * i] = (uint)limbs[i];
                words[2 * i + 1] = (uint)(limbs[i] >> 32);
            }

            return R.Create(sign, words);
        }
    }

    /// <summary>Reference integer: sign and little-endian 32-bit magnitude words without leading zeros.</summary>
    private sealed class R
    {
        public static readonly R Zero = new(0, []);
        public static readonly R One = new(1, [1]);

        public int Sign { get; }
        public uint[] Words { get; }

        private R(int sign, uint[] words)
        {
            Sign = sign;
            Words = words;
        }

        public static R Create(int sign, ReadOnlySpan<uint> words)
        {
            int length = words.Length;
            while (length > 0 && words[length - 1] == 0)
            {
                length--;
            }

            return length == 0 ? Zero : new R(sign < 0 ? -1 : 1, words.Slice(0, length).ToArray());
        }

        public static R FromUInt64(ulong value) => Create(1, [(uint)value, (uint)(value >> 32)]);

        public long BitLength => Words.Length == 0 ? 0 : (Words.Length - 1) * 32L + (32 - BitOperations.LeadingZeroCount(Words[^1]));
        public bool IsEven => Words.Length == 0 || (Words[0] & 1) == 0;
        public bool TestBit(long bit) => bit / 32 < Words.Length && ((Words[bit / 32] >> (int)(bit % 32)) & 1) != 0;
        public ulong LowBits64() => (Words.Length > 0 ? Words[0] : 0) | ((ulong)(Words.Length > 1 ? Words[1] : 0) << 32);
        public long PopCount() => Words.Sum(w => (long)BitOperations.PopCount(w));

        public long TrailingZeroCount()
        {
            int i = 0;
            while (Words[i] == 0)
            {
                i++;
            }

            return i * 32L + BitOperations.TrailingZeroCount(Words[i]);
        }

        public static R Abs(R value) => value.Sign < 0 ? new R(1, value.Words) : value;
        public static R operator -(R value) => value.Sign == 0 ? value : new R(-value.Sign, value.Words);

        public static bool operator ==(R? left, R? right) =>
            left is null ? right is null : right is not null && left.Sign == right.Sign && left.Words.AsSpan().SequenceEqual(right.Words);
        public static bool operator !=(R? left, R? right) => !(left == right);
        public override bool Equals(object? obj) => obj is R other && this == other;
        public override int GetHashCode() => Sign;

        public static int Compare(R left, R right) =>
            left.Sign != right.Sign ? left.Sign.CompareTo(right.Sign) : left.Sign * CompareMagnitude(left.Words, right.Words);

        private static int CompareMagnitude(uint[] left, uint[] right)
        {
            if (left.Length != right.Length)
            {
                return left.Length.CompareTo(right.Length);
            }

            for (int i = left.Length - 1; i >= 0; i--)
            {
                if (left[i] != right[i])
                {
                    return left[i].CompareTo(right[i]);
                }
            }

            return 0;
        }

        private static uint[] AddMagnitude(uint[] left, uint[] right)
        {
            if (left.Length < right.Length)
            {
                (left, right) = (right, left);
            }

            uint[] result = new uint[left.Length + 1];
            ulong carry = 0;
            for (int i = 0; i < left.Length; i++)
            {
                ulong sum = (ulong)left[i] + (i < right.Length ? right[i] : 0) + carry;
                result[i] = (uint)sum;
                carry = sum >> 32;
            }

            result[left.Length] = (uint)carry;
            return result;
        }

        // left >= right.
        private static uint[] SubtractMagnitude(uint[] left, uint[] right)
        {
            uint[] result = new uint[left.Length];
            long borrow = 0;
            for (int i = 0; i < left.Length; i++)
            {
                long diff = (long)left[i] - (i < right.Length ? right[i] : 0) - borrow;
                result[i] = (uint)diff;
                borrow = diff < 0 ? 1 : 0;
            }

            return result;
        }

        public static R operator +(R left, R right)
        {
            if (left.Sign == 0) return right;
            if (right.Sign == 0) return left;
            if (left.Sign == right.Sign)
            {
                return Create(left.Sign, AddMagnitude(left.Words, right.Words));
            }

            int c = CompareMagnitude(left.Words, right.Words);
            return c == 0 ? Zero
                : c > 0 ? Create(left.Sign, SubtractMagnitude(left.Words, right.Words))
                : Create(right.Sign, SubtractMagnitude(right.Words, left.Words));
        }

        public static R operator -(R left, R right) => left + -right;

        public static R operator *(R left, R right)
        {
            if (left.Sign == 0 || right.Sign == 0)
            {
                return Zero;
            }

            uint[] result = new uint[left.Words.Length + right.Words.Length];
            for (int i = 0; i < left.Words.Length; i++)
            {
                ulong carry = 0;
                ulong x = left.Words[i];
                if (x == 0)
                {
                    continue;
                }

                for (int j = 0; j < right.Words.Length; j++)
                {
                    ulong t = x * right.Words[j] + result[i + j] + carry;
                    result[i + j] = (uint)t;
                    carry = t >> 32;
                }

                result[i + right.Words.Length] = (uint)carry;
            }

            return Create(left.Sign * right.Sign, result);
        }

        public R ShiftLeft(long shift)
        {
            if (Sign == 0 || shift == 0)
            {
                return this;
            }

            int wordShift = (int)(shift / 32), bitShift = (int)(shift % 32);
            uint[] result = new uint[Words.Length + wordShift + 1];
            for (int i = 0; i < Words.Length; i++)
            {
                ulong v = (ulong)Words[i] << bitShift;
                result[i + wordShift] |= (uint)v;
                result[i + wordShift + 1] |= (uint)(v >> 32);
            }

            return Create(Sign, result);
        }

        private static uint[] ShiftRightMagnitude(uint[] words, long shift, out bool lostBits)
        {
            int wordShift = (int)Math.Min(shift / 32, words.Length), bitShift = (int)(shift % 32);
            lostBits = words.AsSpan(0, wordShift).IndexOfAnyExcept(0u) >= 0;
            if (wordShift < words.Length && bitShift != 0 && (words[wordShift] & ((1u << bitShift) - 1)) != 0)
            {
                lostBits = true;
            }

            uint[] result = new uint[Math.Max(0, words.Length - wordShift)];
            for (int i = 0; i < result.Length; i++)
            {
                ulong v = words[i + wordShift] | ((ulong)(i + wordShift + 1 < words.Length ? words[i + wordShift + 1] : 0) << 32);
                result[i] = (uint)(v >> bitShift);
            }

            return result;
        }

        public R FloorShiftRight(long shift)
        {
            uint[] result = ShiftRightMagnitude(Words, shift, out bool lostBits);
            R value = Create(Sign, result);
            return Sign < 0 && lostBits ? value - One : value;
        }

        public static (R Quotient, R Remainder) DivRem(R left, R right)
        {
            uint[] q = DivRemMagnitude(left.Words, right.Words, out uint[] r);
            return (Create(left.Sign * right.Sign, q), Create(left.Sign, r));
        }

        // Knuth's algorithm D (Hacker's Delight divmnu).
        private static uint[] DivRemMagnitude(uint[] u, uint[] v, out uint[] remainder)
        {
            if (CompareMagnitude(u, v) < 0)
            {
                remainder = u;
                return [];
            }

            int n = v.Length, m = u.Length - n;
            uint[] q = new uint[m + 1];
            if (n == 1)
            {
                ulong rem = 0;
                for (int i = u.Length - 1; i >= 0; i--)
                {
                    ulong cur = (rem << 32) | u[i];
                    q[i] = (uint)(cur / v[0]);
                    rem = cur % v[0];
                }

                remainder = [(uint)rem];
                return q;
            }

            int s = BitOperations.LeadingZeroCount(v[n - 1]);
            uint[] vn = new uint[n];
            for (int i = n - 1; i > 0; i--)
            {
                vn[i] = (v[i] << s) | (uint)((ulong)v[i - 1] >> (32 - s));
            }

            vn[0] = v[0] << s;
            uint[] un = new uint[m + n + 1];
            un[m + n] = (uint)((ulong)u[m + n - 1] >> (32 - s));
            for (int i = m + n - 1; i > 0; i--)
            {
                un[i] = (u[i] << s) | (uint)((ulong)u[i - 1] >> (32 - s));
            }

            un[0] = u[0] << s;
            const ulong B = 1UL << 32;
            for (int j = m; j >= 0; j--)
            {
                ulong num = ((ulong)un[j + n] << 32) | un[j + n - 1];
                ulong qhat = num / vn[n - 1];
                ulong rhat = num % vn[n - 1];
                while (qhat >= B || qhat * vn[n - 2] > ((rhat << 32) | un[j + n - 2]))
                {
                    qhat--;
                    rhat += vn[n - 1];
                    if (rhat >= B)
                    {
                        break;
                    }
                }

                long k = 0, t;
                for (int i = 0; i < n; i++)
                {
                    ulong p = qhat * vn[i];
                    t = (long)un[i + j] - k - (long)(p & 0xFFFFFFFF);
                    un[i + j] = (uint)t;
                    k = (long)(p >> 32) - (t >> 32);
                }

                t = (long)un[j + n] - k;
                un[j + n] = (uint)t;
                q[j] = (uint)qhat;
                if (t < 0)
                {
                    q[j]--;
                    ulong carry = 0;
                    for (int i = 0; i < n; i++)
                    {
                        ulong sum = (ulong)un[i + j] + vn[i] + carry;
                        un[i + j] = (uint)sum;
                        carry = sum >> 32;
                    }

                    un[j + n] = (uint)(un[j + n] + carry);
                }
            }

            remainder = new uint[n];
            for (int i = 0; i < n; i++)
            {
                remainder[i] = (uint)((((ulong)un[i + 1] << 32) | un[i]) >> s);
            }

            return q;
        }

        // Infinite two's complement.
        public static R Bitwise(R left, R right, Func<uint, uint, uint> op)
        {
            int length = Math.Max(left.Words.Length, right.Words.Length) + 1;
            uint[] x = TwosComplement(left, length), y = TwosComplement(right, length);
            uint[] z = new uint[length];
            for (int i = 0; i < length; i++)
            {
                z[i] = op(x[i], y[i]);
            }

            if ((z[^1] & 0x80000000) == 0)
            {
                return Create(1, z);
            }

            return -Create(1, TwosComplement(Create(-1, z), length));
        }

        // For negative values: ~|v| + 1 in 'length' words (also maps a negative two's complement pattern back to |v|).
        private static uint[] TwosComplement(R value, int length)
        {
            uint[] result = new uint[length];
            value.Words.CopyTo(result, 0);
            if (value.Sign < 0)
            {
                ulong carry = 1;
                for (int i = 0; i < length; i++)
                {
                    ulong v = (ulong)(uint)~result[i] + carry;
                    result[i] = (uint)v;
                    carry = v >> 32;
                }
            }

            return result;
        }

        // Round half to even at 'precision' significand bits; +-Infinity at or above 2^maxExponent.
        public double ToFloating(int precision, int maxExponent)
        {
            if (Sign == 0)
            {
                return 0;
            }

            long bits = BitLength;
            ulong significand;
            long exponent;
            if (bits <= precision)
            {
                significand = LowBits64();
                exponent = 0;
            }
            else
            {
                long drop = bits - precision;
                uint[] top = ShiftRightMagnitude(Words, drop, out _);
                significand = Create(1, top).LowBits64();
                bool half = TestBit(drop - 1);
                bool sticky = LowerBitsNonZero(drop - 1);
                if (half && (sticky || (significand & 1) != 0))
                {
                    significand++;
                }

                exponent = drop;
            }

            double magnitude = BitOperations.Log2(significand) + exponent >= maxExponent ? double.PositiveInfinity : Math.ScaleB(significand, (int)exponent);
            return Sign * magnitude;
        }

        private bool LowerBitsNonZero(long bitCount)
        {
            for (long i = 0; i < bitCount / 32; i++)
            {
                if (Words[i] != 0)
                {
                    return true;
                }
            }

            int rest = (int)(bitCount % 32);
            return rest != 0 && (Words[bitCount / 32] & ((1u << rest) - 1)) != 0;
        }

        public static R FromDouble(double value)
        {
            double truncated = Math.Truncate(value);
            if (truncated == 0)
            {
                return Zero;
            }

            long raw = BitConverter.DoubleToInt64Bits(Math.Abs(truncated));
            int biasedExponent = (int)(raw >> 52);
            ulong mantissa = ((ulong)raw & 0xFFFFFFFFFFFFF) | (1UL << 52);
            int shift = biasedExponent - 1075;
            R magnitude = shift >= 0 ? FromUInt64(mantissa).ShiftLeft(shift) : FromUInt64(mantissa >> -shift);
            return value < 0 ? -magnitude : magnitude;
        }

        public static R ParseDecimal(string text)
        {
            int sign = 1, start = 0;
            if (text.StartsWith('-'))
            {
                sign = -1;
                start = 1;
            }

            R value = Zero;
            int first = (text.Length - start) % 9;
            if (first == 0)
            {
                first = 9;
            }

            for (int i = start; i < text.Length;)
            {
                int take = i == start ? first : 9;
                ulong chunk = ulong.Parse(text.AsSpan(i, take), CultureInfo.InvariantCulture);
                value = value * FromUInt64(Pow10(take)) + FromUInt64(chunk);
                i += take;
            }

            return sign < 0 ? -value : value;

            static ulong Pow10(int n)
            {
                ulong p = 1;
                for (int i = 0; i < n; i++)
                {
                    p *= 10;
                }

                return p;
            }
        }

        public string ToHex()
        {
            if (Sign == 0)
            {
                return "";
            }

            var builder = new StringBuilder();
            builder.Append(Words[^1].ToString("X", CultureInfo.InvariantCulture));
            for (int i = Words.Length - 2; i >= 0; i--)
            {
                builder.Append(Words[i].ToString("X8", CultureInfo.InvariantCulture));
            }

            return builder.ToString();
        }

        public override string ToString()
        {
            if (Sign == 0)
            {
                return "0";
            }

            string hex = ToHex();
            string body = hex.Length <= 64 ? hex : $"{hex[..24]}...{hex[^24..]} ({BitLength} bits)";
            return (Sign < 0 ? "-0x" : "0x") + body;
        }
    }
}
