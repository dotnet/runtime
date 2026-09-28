// Finding (memory safety): parsing a number from UTF-8 (double.TryParse(ReadOnlySpan<byte>, ...) and every other numeric UTF-8
// TryParse/Parse overload) can read past the end of the input span. When the parser matches the NaN / PositiveInfinity /
// NegativeInfinity symbol, or the negative sign, it goes through Ordinal.EqualsIgnoreCaseUtf8_Scalar /
// StartsWithIgnoreCaseUtf8_Scalar. The tail that handles exactly 3 leftover bytes reads a ushort and then one more byte,
// advancing byteOffset by 2, but it never subtracts that from `range`. When the bytes are non-ASCII it falls into the non-ASCII
// helper with the pointer advanced by 2 while the length still counts the full 3 bytes, so it reads 2 bytes past the end. It
// triggers whenever the symbol's UTF-8 length is 3 (mod 4) and the input ends with it. Two symptoms:
//   * Correctness: a 3-byte sign or symbol at the start isn't matched from UTF-8, though it is from UTF-16. With a format whose
//     NegativeSign is "\u2212" (3 bytes), "\u2212nan" parses to NaN as a string but fails as UTF-8.
//   * Memory safety: when the input ends exactly at an unmapped page, the over-read is an AccessViolationException that crashes
//     the process. en-US PositiveInfinitySymbol is "\u221E" (3 bytes), so parsing it from a span that ends at a page boundary AVs.
// Run: dotnet run 44-NumberParsing-Utf8-OutOfBoundsRead.cs   (prints REPRODUCED for the correctness bug, then crashes)
#:property AllowUnsafeBlocks=true
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

// 1. The safe, catchable correctness symptom.
var signInfo = new NumberFormatInfo { NegativeSign = "\u2212", NaNSymbol = "nan" };
bool utf16 = double.TryParse("\u2212nan", NumberStyles.Float, signInfo, out double a);
bool utf8 = double.TryParse(Encoding.UTF8.GetBytes("\u2212nan"), NumberStyles.Float, signInfo, out double b);
Console.WriteLine($"NegativeSign \"\\u2212\": TryParse(\"\u2212nan\") string -> {utf16}/{a}, UTF-8 -> {utf8}/{b}");
Console.WriteLine(utf16 && !utf8 ? "REPRODUCED (UTF-8 disagrees with UTF-16)" : "NOT REPRODUCED");

// 2. The memory-safety symptom: the same over-read against an unmapped page.
unsafe
{
    var enUS = CultureInfo.GetCultureInfo("en-US").NumberFormat; // PositiveInfinitySymbol == "\u221E", E2 88 9E, 3 bytes
    byte[] symbol = Encoding.UTF8.GetBytes(enUS.PositiveInfinitySymbol);
    nuint pageSize = 4096;
    byte* region = (byte*)NativeMemory.AlignedAlloc(pageSize * 2, pageSize);
    if (mprotect((nint)(region + pageSize), pageSize, 0) != 0)
    {
        Console.WriteLine("mprotect failed; skipping the guard-page part");
        return;
    }

    byte* start = region + pageSize - symbol.Length;
    symbol.CopyTo(new Span<byte>(start, symbol.Length));
    Console.WriteLine($"Now parsing the {symbol.Length}-byte symbol \"{enUS.PositiveInfinitySymbol}\" placed against an unmapped page (AccessViolation on an affected runtime)...");
    Console.Out.Flush();

    bool ok = double.TryParse(new ReadOnlySpan<byte>(start, symbol.Length), NumberStyles.Float, enUS, out double value);
    Console.WriteLine($"TryParse returned {ok}, value {value} (no out-of-bounds read on this runtime)");
}

[DllImport("libc", SetLastError = true)]
static extern int mprotect(nint addr, nuint len, int prot);
