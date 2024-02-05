// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The BigNumber class implements methods for formatting and parsing
// big numeric values. To format and parse numeric values, applications should
// use the Format and Parse methods provided by the numeric
// classes (BigInteger). Those
// Format and Parse methods share a common implementation
// provided by this class, and are thus documented in detail here.
//
// Formatting
//
// The Format methods provided by the numeric classes are all of the
// form
//
//  public static String Format(XXX value, String format);
//  public static String Format(XXX value, String format, NumberFormatInfo info);
//
// where XXX is the name of the particular numeric class. The methods convert
// the numeric value to a string using the format string given by the
// format parameter. If the format parameter is null or
// an empty string, the number is formatted as if the string "G" (general
// format) was specified. The info parameter specifies the
// NumberFormatInfo instance to use when formatting the number. If the
// info parameter is null or omitted, the numeric formatting information
// is obtained from the current culture. The NumberFormatInfo supplies
// such information as the characters to use for decimal and thousand
// separators, and the spelling and placement of currency symbols in monetary
// values.
//
// Format strings fall into two categories: Standard format strings and
// user-defined format strings. A format string consisting of a single
// alphabetic character (A-Z or a-z), optionally followed by a sequence of
// digits (0-9), is a standard format string. All other format strings are
// used-defined format strings.
//
// A standard format string takes the form Axx, where A is an
// alphabetic character called the format specifier and xx is a
// sequence of digits called the precision specifier. The format
// specifier controls the type of formatting applied to the number and the
// precision specifier controls the number of significant digits or decimal
// places of the formatting operation. The following table describes the
// supported standard formats.
//
// C c - Currency format. The number is
// converted to a string that represents a currency amount. The conversion is
// controlled by the currency format information of the NumberFormatInfo
// used to format the number. The precision specifier indicates the desired
// number of decimal places. If the precision specifier is omitted, the default
// currency precision given by the NumberFormatInfo is used.
//
// D d - Decimal format. This format is
// supported for integral types only. The number is converted to a string of
// decimal digits, prefixed by a minus sign if the number is negative. The
// precision specifier indicates the minimum number of digits desired in the
// resulting string. If required, the number will be left-padded with zeros to
// produce the number of digits given by the precision specifier.
//
// E e Engineering (scientific) format.
// The number is converted to a string of the form
// "-d.ddd...E+ddd" or "-d.ddd...e+ddd", where each
// 'd' indicates a digit (0-9). The string starts with a minus sign if the
// number is negative, and one digit always precedes the decimal point. The
// precision specifier indicates the desired number of digits after the decimal
// point. If the precision specifier is omitted, a default of 6 digits after
// the decimal point is used. The format specifier indicates whether to prefix
// the exponent with an 'E' or an 'e'. The exponent is always consists of a
// plus or minus sign and three digits.
//
// F f Fixed point format. The number is
// converted to a string of the form "-ddd.ddd....", where each
// 'd' indicates a digit (0-9). The string starts with a minus sign if the
// number is negative. The precision specifier indicates the desired number of
// decimal places. If the precision specifier is omitted, the default numeric
// precision given by the NumberFormatInfo is used.
//
// G g - General format. The number is
// converted to the shortest possible decimal representation using fixed point
// or scientific format. The precision specifier determines the number of
// significant digits in the resulting string. If the precision specifier is
// omitted, the number of significant digits is determined by the type of the
// number being converted (10 for int, 19 for long, 7 for
// float, 15 for double, 19 for Currency, and 29 for
// Decimal). Trailing zeros after the decimal point are removed, and the
// resulting string contains a decimal point only if required. The resulting
// string uses fixed point format if the exponent of the number is less than
// the number of significant digits and greater than or equal to -4. Otherwise,
// the resulting string uses scientific format, and the case of the format
// specifier controls whether the exponent is prefixed with an 'E' or an
// 'e'.
//
// N n Number format. The number is
// converted to a string of the form "-d,ddd,ddd.ddd....", where
// each 'd' indicates a digit (0-9). The string starts with a minus sign if the
// number is negative. Thousand separators are inserted between each group of
// three digits to the left of the decimal point. The precision specifier
// indicates the desired number of decimal places. If the precision specifier
// is omitted, the default numeric precision given by the
// NumberFormatInfo is used.
//
// X x - Hexadecimal format. This format is
// supported for integral types only. The number is converted to a string of
// hexadecimal digits. The format specifier indicates whether to use upper or
// lower case characters for the hexadecimal digits above 9 ('X' for 'ABCDEF',
// and 'x' for 'abcdef'). The precision specifier indicates the minimum number
// of digits desired in the resulting string. If required, the number will be
// left-padded with zeros to produce the number of digits given by the
// precision specifier.
//
// Some examples of standard format strings and their results are shown in the
// table below. (The examples all assume a default NumberFormatInfo.)
//
// Value        Format  Result
// 12345.6789   C       $12,345.68
// -12345.6789  C       ($12,345.68)
// 12345        D       12345
// 12345        D8      00012345
// 12345.6789   E       1.234568E+004
// 12345.6789   E10     1.2345678900E+004
// 12345.6789   e4      1.2346e+004
// 12345.6789   F       12345.68
// 12345.6789   F0      12346
// 12345.6789   F6      12345.678900
// 12345.6789   G       12345.6789
// 12345.6789   G7      12345.68
// 123456789    G7      1.234568E8
// 12345.6789   N       12,345.68
// 123456789    N4      123,456,789.0000
// 0x2c45e      x       2c45e
// 0x2c45e      X       2C45E
// 0x2c45e      X8      0002C45E
//
// Format strings that do not start with an alphabetic character, or that start
// with an alphabetic character followed by a non-digit, are called
// user-defined format strings. The following table describes the formatting
// characters that are supported in user defined format strings.
//
//
// 0 - Digit placeholder. If the value being
// formatted has a digit in the position where the '0' appears in the format
// string, then that digit is copied to the output string. Otherwise, a '0' is
// stored in that position in the output string. The position of the leftmost
// '0' before the decimal point and the rightmost '0' after the decimal point
// determines the range of digits that are always present in the output
// string.
//
// # - Digit placeholder. If the value being
// formatted has a digit in the position where the '#' appears in the format
// string, then that digit is copied to the output string. Otherwise, nothing
// is stored in that position in the output string.
//
// . - Decimal point. The first '.' character
// in the format string determines the location of the decimal separator in the
// formatted value; any additional '.' characters are ignored. The actual
// character used as a the decimal separator in the output string is given by
// the NumberFormatInfo used to format the number.
//
// , - Thousand separator and number scaling.
// The ',' character serves two purposes. First, if the format string contains
// a ',' character between two digit placeholders (0 or #) and to the left of
// the decimal point if one is present, then the output will have thousand
// separators inserted between each group of three digits to the left of the
// decimal separator. The actual character used as a the decimal separator in
// the output string is given by the NumberFormatInfo used to format the
// number. Second, if the format string contains one or more ',' characters
// immediately to the left of the decimal point, or after the last digit
// placeholder if there is no decimal point, then the number will be divided by
// 1000 times the number of ',' characters before it is formatted. For example,
// the format string '0,,' will represent 100 million as just 100. Use of the
// ',' character to indicate scaling does not also cause the formatted number
// to have thousand separators. Thus, to scale a number by 1 million and insert
// thousand separators you would use the format string '#,##0,,'.
//
// % - Percentage placeholder. The presence of
// a '%' character in the format string causes the number to be multiplied by
// 100 before it is formatted. The '%' character itself is inserted in the
// output string where it appears in the format string.
//
// E+ E- e+ e-   - Scientific notation.
// If any of the strings 'E+', 'E-', 'e+', or 'e-' are present in the format
// string and are immediately followed by at least one '0' character, then the
// number is formatted using scientific notation with an 'E' or 'e' inserted
// between the number and the exponent. The number of '0' characters following
// the scientific notation indicator determines the minimum number of digits to
// output for the exponent. The 'E+' and 'e+' formats indicate that a sign
// character (plus or minus) should always precede the exponent. The 'E-' and
// 'e-' formats indicate that a sign character should only precede negative
// exponents.
//
// \ - Literal character. A backslash character
// causes the next character in the format string to be copied to the output
// string as-is. The backslash itself isn't copied, so to place a backslash
// character in the output string, use two backslashes (\\) in the format
// string.
//
// 'ABC' "ABC" - Literal string. Characters
// enclosed in single or double quotation marks are copied to the output string
// as-is and do not affect formatting.
//
// ; - Section separator. The ';' character is
// used to separate sections for positive, negative, and zero numbers in the
// format string.
//
// Other - All other characters are copied to
// the output string in the position they appear.
//
// For fixed point formats (formats not containing an 'E+', 'E-', 'e+', or
// 'e-'), the number is rounded to as many decimal places as there are digit
// placeholders to the right of the decimal point. If the format string does
// not contain a decimal point, the number is rounded to the nearest
// integer. If the number has more digits than there are digit placeholders to
// the left of the decimal point, the extra digits are copied to the output
// string immediately before the first digit placeholder.
//
// For scientific formats, the number is rounded to as many significant digits
// as there are digit placeholders in the format string.
//
// To allow for different formatting of positive, negative, and zero values, a
// user-defined format string may contain up to three sections separated by
// semicolons. The results of having one, two, or three sections in the format
// string are described in the table below.
//
// Sections:
//
// One - The format string applies to all values.
//
// Two - The first section applies to positive values
// and zeros, and the second section applies to negative values. If the number
// to be formatted is negative, but becomes zero after rounding according to
// the format in the second section, then the resulting zero is formatted
// according to the first section.
//
// Three - The first section applies to positive
// values, the second section applies to negative values, and the third section
// applies to zeros. The second section may be left empty (by having no
// characters between the semicolons), in which case the first section applies
// to all non-zero values. If the number to be formatted is non-zero, but
// becomes zero after rounding according to the format in the first or second
// section, then the resulting zero is formatted according to the third
// section.
//
// For both standard and user-defined formatting operations on values of type
// float and double, if the value being formatted is a NaN (Not
// a Number) or a positive or negative infinity, then regardless of the format
// string, the resulting string is given by the NaNSymbol,
// PositiveInfinitySymbol, or NegativeInfinitySymbol property of
// the NumberFormatInfo used to format the number.
//
// Parsing
//
// The Parse methods provided by the numeric classes are all of the form
//
//  public static XXX Parse(String s);
//  public static XXX Parse(String s, int style);
//  public static XXX Parse(String s, int style, NumberFormatInfo info);
//
// where XXX is the name of the particular numeric class. The methods convert a
// string to a numeric value. The optional style parameter specifies the
// permitted style of the numeric string. It must be a combination of bit flags
// from the NumberStyles enumeration. The optional info parameter
// specifies the NumberFormatInfo instance to use when parsing the
// string. If the info parameter is null or omitted, the numeric
// formatting information is obtained from the current culture.
//
// Numeric strings produced by the Format methods using the Currency,
// Decimal, Engineering, Fixed point, General, or Number standard formats
// (the C, D, E, F, G, and N format specifiers) are guaranteed to be parseable
// by the Parse methods if the NumberStyles.Any style is
// specified. Note, however, that the Parse methods do not accept
// NaNs or Infinities.
//

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System
{
    internal static partial class Number
    {
        private const NumberStyles InvalidNumberStyles = ~(NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite
                                                           | NumberStyles.AllowLeadingSign | NumberStyles.AllowTrailingSign
                                                           | NumberStyles.AllowParentheses | NumberStyles.AllowDecimalPoint
                                                           | NumberStyles.AllowThousands | NumberStyles.AllowExponent
                                                           | NumberStyles.AllowCurrencySymbol | NumberStyles.AllowHexSpecifier
                                                           | NumberStyles.AllowBinarySpecifier);

        private static ReadOnlySpan<uint> UInt32PowersOfTen => [1, 10, 100, 1000, 10000, 100000, 1000000, 10000000, 100000000, 1000000000];

        [DoesNotReturn]
        internal static void ThrowOverflowOrFormatException(ParsingStatus status) => throw GetException(status);

        private static Exception GetException(ParsingStatus status)
        {
            return status == ParsingStatus.Failed
                ? new FormatException(SR.Overflow_ParseBigInteger)
                : new OverflowException(SR.Overflow_ParseBigInteger);
        }

        internal static bool TryValidateParseStyleInteger(NumberStyles style, [NotNullWhen(false)] out ArgumentException? e)
        {
            // Check for undefined flags
            if ((style & InvalidNumberStyles) != 0)
            {
                e = new ArgumentException(SR.Argument_InvalidNumberStyles, nameof(style));
                return false;
            }
            if ((style & NumberStyles.AllowHexSpecifier) != 0)
            { // Check for hex number
                if ((style & ~NumberStyles.HexNumber) != 0)
                {
                    e = new ArgumentException(SR.Argument_InvalidHexStyle, nameof(style));
                    return false;
                }
            }
            e = null;
            return true;
        }

        internal static unsafe ParsingStatus TryParseBigInteger(ReadOnlySpan<char> value, NumberStyles style, NumberFormatInfo info, out BigInteger result)
        {
            if (!TryValidateParseStyleInteger(style, out ArgumentException? e))
            {
                throw e; // TryParse still throws ArgumentException on invalid NumberStyles
            }

            if ((style & NumberStyles.AllowHexSpecifier) != 0)
            {
                return TryParseBigIntegerHexOrBinaryNumberStyle<BigIntegerHexParser<char>, char>(value, style, out result);
            }

            if ((style & NumberStyles.AllowBinarySpecifier) != 0)
            {
                return TryParseBigIntegerHexOrBinaryNumberStyle<BigIntegerBinaryParser<char>, char>(value, style, out result);
            }

            return TryParseBigIntegerNumber(value, style, info, out result);
        }

        internal static unsafe ParsingStatus TryParseBigIntegerNumber(ReadOnlySpan<char> value, NumberStyles style, NumberFormatInfo info, out BigInteger result)
        {
            scoped Span<byte> buffer;
            byte[]? arrayFromPool = null;

            if (value.Length == 0)
            {
                result = default;
                return ParsingStatus.Failed;
            }
            if (value.Length < 255)
            {
                buffer = stackalloc byte[value.Length + 1 + 1];
            }
            else
            {
                buffer = arrayFromPool = ArrayPool<byte>.Shared.Rent(value.Length + 1 + 1);
            }

            ParsingStatus ret;

            fixed (byte* ptr = buffer) // NumberBuffer expects pinned span
            {
                NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, buffer);

                if (!TryStringToNumber(MemoryMarshal.Cast<char, Utf16Char>(value), style, ref number, info))
                {
                    result = default;
                    ret = ParsingStatus.Failed;
                }
                else
                {
                    ret = NumberToBigInteger(ref number, out result);
                }
            }

            if (arrayFromPool != null)
            {
                ArrayPool<byte>.Shared.Return(arrayFromPool);
            }

            return ret;
        }

        internal static BigInteger ParseBigInteger(ReadOnlySpan<char> value, NumberStyles style, NumberFormatInfo info)
        {
            if (!TryValidateParseStyleInteger(style, out ArgumentException? e))
            {
                throw e;
            }

            ParsingStatus status = TryParseBigInteger(value, style, info, out BigInteger result);
            if (status != ParsingStatus.OK)
            {
                ThrowOverflowOrFormatException(status);
            }

            return result;
        }

        internal static ParsingStatus TryParseBigIntegerHexOrBinaryNumberStyle<TParser, TChar>(ReadOnlySpan<TChar> value, NumberStyles style, out BigInteger result)
            where TParser : struct, IBigIntegerHexOrBinaryParser<TParser, TChar>
            where TChar : unmanaged, IBinaryInteger<TChar>
        {
            int whiteIndex;

            // Skip past any whitespace at the beginning.
            if ((style & NumberStyles.AllowLeadingWhite) != 0)
            {
                for (whiteIndex = 0; whiteIndex < value.Length; whiteIndex++)
                {
                    if (!IsWhite(uint.CreateTruncating(value[whiteIndex])))
                        break;
                }

                value = value[whiteIndex..];
            }

            // Skip past any whitespace at the end.
            if ((style & NumberStyles.AllowTrailingWhite) != 0)
            {
                for (whiteIndex = value.Length - 1; whiteIndex >= 0; whiteIndex--)
                {
                    if (!IsWhite(uint.CreateTruncating(value[whiteIndex])))
                        break;
                }

                value = value[..(whiteIndex + 1)];
            }

            if (value.IsEmpty)
            {
                goto FailExit;
            }

            // Remember the sign from original leading input
            // Invalid digits will be caught in parsing below
            uint signBits = TParser.GetSignBitsIfValid(uint.CreateTruncating(value[0]));

            // Start from leading blocks. Leading blocks can be unaligned, or whole of 0/F's that need to be trimmed.
            int leadingBitsCount = value.Length % TParser.DigitsPerBlock;

            uint leading = signBits;
            // First parse unaligned leading block if exists.
            if (leadingBitsCount != 0)
            {
                if (!TParser.TryParseUnalignedBlock(value[0..leadingBitsCount], out leading))
                {
                    goto FailExit;
                }

                // Fill leading sign bits
                leading |= signBits << (leadingBitsCount * TParser.BitsPerDigit);
                value = value[leadingBitsCount..];
            }

            // Skip all the blocks consists of the same bit of sign
            while (!value.IsEmpty && leading == signBits)
            {
                if (!TParser.TryParseSingleBlock(value[0..TParser.DigitsPerBlock], out leading))
                {
                    goto FailExit;
                }
                value = value[TParser.DigitsPerBlock..];
            }

            if (value.IsEmpty)
            {
                // There's nothing beyond significant leading block. Return it as the result.
                if ((int)(leading ^ signBits) >= 0)
                {
                    // Small value that fits in Int32.
                    // Delegate to the constructor for int.MinValue handling.
                    result = new BigInteger((int)leading);
                    return ParsingStatus.OK;
                }
                else if (leading != 0)
                {
                    // The sign of result differs with leading digit.
                    // Require to store in _bits.

                    // Positive: sign=1, bits=[leading]
                    // Negative: sign=-1, bits=[(leading ^ -1) + 1]=[-leading]
                    result = new BigInteger((int)signBits | 1, [(leading ^ signBits) - signBits]);
                    return ParsingStatus.OK;
                }
                else
                {
                    // -1 << 32, which requires an additional uint
                    result = new BigInteger(-1, [0, 1]);
                    return ParsingStatus.OK;
                }
            }

            // Now the size of bits array can be calculated, except edge cases of -2^32N
            int wholeBlockCount = value.Length / TParser.DigitsPerBlock;
            int totalUIntCount = wholeBlockCount + 1;

            // Early out for too large input
            if (totalUIntCount > BigInteger.MaxLength)
            {
                result = default;
                return ParsingStatus.Overflow;
            }

            uint[] bits = new uint[totalUIntCount];
            Span<uint> wholeBlockDestination = bits.AsSpan(0, wholeBlockCount);

            if (!TParser.TryParseWholeBlocks(value, wholeBlockDestination))
            {
                goto FailExit;
            }

            bits[^1] = leading;

            if (signBits != 0)
            {
                // For negative values, negate the whole array
                if (bits.AsSpan().ContainsAnyExcept(0u))
                {
                    NumericsHelpers.DangerousMakeTwosComplement(bits);
                }
                else
                {
                    // For negative values with all-zero trailing digits,
                    // It requires additional leading 1.
                    bits = new uint[bits.Length + 1];
                    bits[^1] = 1;
                }

                result = new BigInteger(-1, bits);
                return ParsingStatus.OK;
            }
            else
            {
                Debug.Assert(leading != 0);

                // For positive values, it's done
                result = new BigInteger(1, bits);
                return ParsingStatus.OK;
            }

        FailExit:
            result = default;
            return ParsingStatus.Failed;
        }

        //
        // This threshold is for choosing the algorithm to use based on the number of digits.
        //
        // Let N be the number of digits. If N is less than or equal to the bound, use a naive
        // algorithm with a running time of O(N^2). And if it is greater than the threshold, use
        // a divide-and-conquer algorithm with a running time of O(NlogN).
        //
        // `1233`, which is approx the upper bound of most RSA key lengths, covers the majority
        // of most common inputs and allows for the less naive algorithm to be used for
        // large/uncommon inputs.
        //
#if DEBUG
        // Mutable for unit testing...
        internal static
#else
        internal const
#endif
        int
            BigIntegerParseNaiveThreshold = 1233,
            BigIntegerParseNaiveThresholdInRecursive = 9 * (1 << 7);
        private static ParsingStatus NumberToBigInteger(ref NumberBuffer number, out BigInteger result)
        {
            const uint TenPowMaxPartial = PowersOf1e9.TenPowMaxPartial;
            const int MaxPartialDigits = PowersOf1e9.MaxPartialDigits;

            if (number.Scale == int.MaxValue)
            {
                result = default;
                return ParsingStatus.Overflow;
            }

            if (number.Scale < 0)
            {
                result = default;
                return ParsingStatus.Failed;
            }

            const double digitRatio = 0.10381025297; // log_{2^32}(10)
            int resultLength = checked((int)(number.Scale * digitRatio) + 1 + 2);
            uint[]? resultBufferFromPool = null;
            Span<uint> resultBuffer = (
                resultLength <= BigIntegerCalculator.StackAllocThreshold
                ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                : resultBufferFromPool = ArrayPool<uint>.Shared.Rent(resultLength)).Slice(0, resultLength);
            resultBuffer.Clear();

            if (number.Scale <= BigIntegerParseNaiveThreshold
                ? !Naive(ref number, resultBuffer)
                : !DivideAndConquer(ref number, resultBuffer))
            {
                result = default;
                return ParsingStatus.Failed;
            }

            result = NumberBufferToBigInteger(resultBuffer.Slice(0, BigIntegerCalculator.ActualLength(resultBuffer)), number.IsNegative);

            if (resultBufferFromPool != null)
                ArrayPool<uint>.Shared.Return(resultBufferFromPool);

            return ParsingStatus.OK;

            static bool DivideAndConquer(ref NumberBuffer number, scoped Span<uint> bits)
            {
                ReadOnlySpan<byte> intDigits = number.Digits.Slice(0, Math.Min(number.Scale, number.DigitsCount));
                int intDigitsEnd = intDigits.IndexOf<byte>(0);
                if (intDigitsEnd < 0)
                {
                    // Check for nonzero digits after the decimal point.
                    ReadOnlySpan<byte> fracDigitsSpan = number.Digits.Slice(intDigits.Length);
                    for (int i = 0; i < fracDigitsSpan.Length; i++)
                    {
                        char digitChar = (char)fracDigitsSpan[i];
                        if (digitChar == '\0')
                        {
                            break;
                        }
                        if (digitChar != '0')
                        {
                            return false;
                        }
                    }
                }
                else
                    intDigits = intDigits.Slice(0, intDigitsEnd);

                int totalDigitCount = Math.Min(number.DigitsCount, number.Scale);
                int trailingZeroCount = number.Scale - totalDigitCount;

                int powersOf1e9BufferLength = PowersOf1e9.GetBufferSize(Math.Max(totalDigitCount, trailingZeroCount));
                uint[]? powersOf1e9BufferFromPool = null;
                Span<uint> powersOf1e9Buffer = (
                    powersOf1e9BufferLength <= BigIntegerCalculator.StackAllocThreshold
                    ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                    : powersOf1e9BufferFromPool = ArrayPool<uint>.Shared.Rent(powersOf1e9BufferLength)).Slice(0, powersOf1e9BufferLength);
                powersOf1e9Buffer.Clear();

                PowersOf1e9 powersOf1e9 = new PowersOf1e9(powersOf1e9Buffer);

                const double digitRatio = 0.103810253; // log_{2^32}(10)

                if (trailingZeroCount > 0)
                {
                    int leadingLength = checked((int)(digitRatio * totalDigitCount) + 2);
                    uint[]? leadingFromPool = null;
                    Span<uint> leading = (
                        leadingLength <= BigIntegerCalculator.StackAllocThreshold
                        ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                        : leadingFromPool = ArrayPool<uint>.Shared.Rent(leadingLength)).Slice(0, leadingLength);
                    leading.Clear();

                    Recursive(powersOf1e9, intDigits, leading);
                    leading = leading.Slice(0, BigIntegerCalculator.ActualLength(leading));

                    int trailingZeroBufferLength = checked((int)(digitRatio * trailingZeroCount) + 2);
                    uint[]? trailingZeroBufferFromPool = null;
                    Span<uint> trailingZeroBuffer = (
                        trailingZeroBufferLength <= BigIntegerCalculator.StackAllocThreshold
                        ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                        : trailingZeroBufferFromPool = ArrayPool<uint>.Shared.Rent(trailingZeroBufferLength)).Slice(0, trailingZeroBufferLength);
                    trailingZeroBuffer.Clear();

                    powersOf1e9.CalculatePowerOfTen(trailingZeroCount, trailingZeroBuffer);
                    trailingZeroBuffer = trailingZeroBuffer.Slice(0, BigIntegerCalculator.ActualLength(trailingZeroBuffer));

                    // Merge leading and trailing
                    Span<uint> bitsResult = bits.Slice(0, trailingZeroBuffer.Length + leading.Length);

                    if (trailingZeroBuffer.Length < leading.Length)
                        BigIntegerCalculator.Multiply(leading, trailingZeroBuffer, bitsResult);
                    else
                        BigIntegerCalculator.Multiply(trailingZeroBuffer, leading, bitsResult);

                    if (leadingFromPool != null)
                        ArrayPool<uint>.Shared.Return(leadingFromPool);
                    if (trailingZeroBufferFromPool != null)
                        ArrayPool<uint>.Shared.Return(trailingZeroBufferFromPool);
                }
                else
                {
                    Recursive(powersOf1e9, intDigits, bits);
                }


                if (powersOf1e9BufferFromPool != null)
                    ArrayPool<uint>.Shared.Return(powersOf1e9BufferFromPool);

                return true;
            }

            static void Recursive(in PowersOf1e9 powersOf1e9, ReadOnlySpan<byte> digits, Span<uint> bits)
            {
                Debug.Assert(BigIntegerParseNaiveThresholdInRecursive >= MaxPartialDigits);
                if (digits.Length < BigIntegerParseNaiveThresholdInRecursive)
                {
                    NaiveDigits(digits, bits);
                    return;
                }

                int lengthe9 = (digits.Length + MaxPartialDigits - 1) / MaxPartialDigits;
                int log2 = BitOperations.Log2((uint)(lengthe9 - 1));
                int powOfTenSize = 1 << log2;

                ReadOnlySpan<byte> digitsUpper = digits.Slice(0, digits.Length - MaxPartialDigits * powOfTenSize);
                ReadOnlySpan<byte> digitsLower = digits.Slice(digitsUpper.Length);
                {
                    int iv = 0;
                    while ((uint)iv < (uint)digitsLower.Length && digitsLower[iv] == 0) ++iv;
                    digitsLower = digitsLower.Slice(iv);
                }

                int upperBufferLength = checked((int)(digitsUpper.Length * digitRatio) + 1 + 2);
                uint[]? upperBufferFromPool = null;
                Span<uint> upperBuffer = (
                    upperBufferLength <= BigIntegerCalculator.StackAllocThreshold
                    ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                    : upperBufferFromPool = ArrayPool<uint>.Shared.Rent(upperBufferLength)).Slice(0, upperBufferLength);
                upperBuffer.Clear();
                {
                    Recursive(powersOf1e9, digitsUpper, upperBuffer);
                    upperBuffer = upperBuffer.Slice(0, BigIntegerCalculator.ActualLength(upperBuffer));
                    ReadOnlySpan<uint> multiplier = powersOf1e9.GetSpan(log2);
                    Span<uint> bitsUpper = bits.Slice(0, upperBuffer.Length + multiplier.Length);

                    if (multiplier.Length < upperBuffer.Length)
                        BigIntegerCalculator.Multiply(upperBuffer, multiplier, bitsUpper);
                    else
                        BigIntegerCalculator.Multiply(multiplier, upperBuffer, bitsUpper);
                }
                if (upperBufferFromPool != null)
                    ArrayPool<uint>.Shared.Return(upperBufferFromPool);


                int lowerBufferLength = checked((int)(digitsLower.Length * digitRatio) + 1 + 2);
                uint[]? lowerBufferFromPool = null;
                Span<uint> lowerBuffer = (
                    lowerBufferLength <= BigIntegerCalculator.StackAllocThreshold
                    ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                    : lowerBufferFromPool = ArrayPool<uint>.Shared.Rent(lowerBufferLength)).Slice(0, lowerBufferLength);
                lowerBuffer.Clear();

                Recursive(powersOf1e9, digitsLower, lowerBuffer);
                lowerBuffer = lowerBuffer.Slice(0, BigIntegerCalculator.ActualLength(lowerBuffer));

                BigIntegerCalculator.AddSelf(bits, lowerBuffer);

                if (lowerBufferFromPool != null)
                    ArrayPool<uint>.Shared.Return(lowerBufferFromPool);
            }

            static int NaiveDigits(ReadOnlySpan<byte> intDigits, Span<uint> bits)
            {
                int leadingLength = intDigits.Length % 9;
                uint partialValue = 0;
                foreach (uint dig in intDigits.Slice(0, leadingLength))
                {
                    partialValue = partialValue * 10 + (dig - '0');
                }
                bits[0] = partialValue;

                int partialDigitCount = 0;
                int resultLength = 1;
                partialValue = 0;
                for (int i = leadingLength; i < intDigits.Length; i++)
                {
                    partialValue = partialValue * 10 + (uint)(intDigits[i] - '0');

                    // Update the buffer when enough partial digits have been accumulated.
                    if (++partialDigitCount == MaxPartialDigits)
                    {
                        uint carry = MultiplyAdd(bits.Slice(0, resultLength), TenPowMaxPartial, partialValue);
                        partialValue = 0;
                        partialDigitCount = 0;
                        Debug.Assert(bits[resultLength] == 0);
                        if (carry != 0)
                            bits[resultLength++] = carry;
                    }
                }

                return resultLength;
            }

            static bool Naive(ref NumberBuffer number, scoped Span<uint> bits)
            {
                ReadOnlySpan<byte> intDigits = number.Digits.Slice(0, Math.Min(number.Scale, number.DigitsCount));
                int intDigitsEnd = intDigits.IndexOf<byte>(0);
                if (intDigitsEnd < 0)
                {
                    // Check for nonzero digits after the decimal point.
                    ReadOnlySpan<byte> fracDigitsSpan = number.Digits.Slice(intDigits.Length);
                    for (int i = 0; i < fracDigitsSpan.Length; i++)
                    {
                        char digitChar = (char)fracDigitsSpan[i];
                        if (digitChar == '\0')
                        {
                            break;
                        }
                        if (digitChar != '0')
                        {
                            return false;
                        }
                    }
                }
                else
                    intDigits = intDigits.Slice(0, intDigitsEnd);


                int totalDigitCount = Math.Min(number.DigitsCount, number.Scale);
                if (totalDigitCount == 0)
                {
                    // number is 0.
                    return true;
                }
                int resultLength = NaiveDigits(intDigits, bits);

                int trailingZeroCount = number.Scale - totalDigitCount;
                int trailingPartialCount = Math.DivRem(trailingZeroCount, MaxPartialDigits, out int remainingTrailingZeroCount);
                for (int i = 0; i < trailingPartialCount; i++)
                {
                    uint carry = MultiplyAdd(bits.Slice(0, resultLength), TenPowMaxPartial, 0);
                    Debug.Assert(bits[resultLength] == 0);
                    if (carry != 0)
                        bits[resultLength++] = carry;
                }

                if (remainingTrailingZeroCount != 0)
                {
                    uint multiplier = UInt32PowersOfTen[remainingTrailingZeroCount];
                    uint carry = MultiplyAdd(bits.Slice(0, resultLength), multiplier, 0);
                    Debug.Assert(bits[resultLength] == 0);
                    if (carry != 0)
                        bits[resultLength++] = carry;
                }

                return true;
            }

            static uint MultiplyAdd(Span<uint> bits, uint multiplier, uint addValue)
            {
                uint carry = addValue;

                for (int i = 0; i < bits.Length; i++)
                {
                    ulong p = (ulong)multiplier * bits[i] + carry;
                    bits[i] = (uint)p;
                    carry = (uint)(p >> 32);
                }
                return carry;
            }

            static BigInteger NumberBufferToBigInteger(ReadOnlySpan<uint> result, bool isNegative)
            {
                Debug.Assert(result.Length == 0 || result[^1] != 0);

                if (result.Length == 0)
                {
                    return BigInteger.Zero;
                }
                else if (result is [uint leading] && (leading <= int.MaxValue || isNegative && leading == unchecked((uint)(int.MaxValue + 1))))
                {
                    return new BigInteger((int)(isNegative ? -leading : leading));
                }
                else
                {
                    return new BigInteger(isNegative ? -1 : 1, result.ToArray());
                }
            }
        }
        internal readonly ref struct PowersOf1e9
        {
            private readonly ReadOnlySpan<uint> pow1E9;
            public const uint TenPowMaxPartial = 1000000000;
            public const int MaxPartialDigits = 9;

            // indexes[i] is pre-calculated length of (10^9)^i
            // This means that pow1E9[indexes[i-1]..indexes[i]] equals 1000000000 * (1<<i)
            //
            // The `indexes` are calculated as follows
            //    const double digitRatio = 0.934292276687070661; // log_{2^32}(10^9)
            //    int[] indexes = new int[32];
            //    indexes[0] = 0;
            //    for (int i = 0; i + 1 < indexes.Length; i++)
            //    {
            //        int length = unchecked((int)(digitRatio * (1 << i)) + 1);
            //        indexes[i+1] = indexes[i] + length;
            //    }
            private static ReadOnlySpan<int> Indexes => new int[] {
                0,
                1,
                3,
                7,
                15,
                30,
                60,
                120,
                240,
                480,
                959,
                1916,
                3830,
                7657,
                15311,
                30619,
                61234,
                122464,
                244924,
                489844,
                979683,
                1959360,
                3918713,
                7837419,
                15674831,
                31349655,
                62699302,
                125398596,
                250797183,
                501594357,
                1003188704,
                2006377398,
            };

            public PowersOf1e9(Span<uint> pow1E9)
            {
                this.pow1E9 = pow1E9;

                Debug.Assert(pow1E9.Length >= 1);
                pow1E9[0] = TenPowMaxPartial;
                ReadOnlySpan<uint> src = pow1E9.Slice(0, 1);
                int toExclusive = 1;
                for (int i = 1; i + 1 < Indexes.Length; i++)
                {
                    Debug.Assert(2 * src.Length - (Indexes[i + 1] - Indexes[i]) is 0 or 1);
                    if (pow1E9.Length - toExclusive < (src.Length << 1))
                        break;
                    Span<uint> dst = pow1E9.Slice(toExclusive, src.Length << 1);
                    BigIntegerCalculator.Square(src, dst);
                    int from = toExclusive;
                    toExclusive = Indexes[i + 1];
                    src = pow1E9.Slice(from, toExclusive - from);
                }
            }
            public ReadOnlySpan<uint> GetSpan(int index)
            {
                // Returns 1E9^(1<<index)
                int from = Indexes[index];
                int toExclusive = Indexes[index + 1];
                return pow1E9[from..toExclusive];
            }

            public void CalculatePowerOfTen(int trailingZeroCount, Span<uint> bits)
            {
                Debug.Assert(bits.Length > unchecked((int)(0.934292276687070661 / 9 * trailingZeroCount)) + 1);
                Debug.Assert(trailingZeroCount >= 0);


                int trailingPartialCount = Math.DivRem(trailingZeroCount, MaxPartialDigits, out int remainingTrailingZeroCount);

                if (trailingPartialCount == 0)
                {
                    bits[0] = UInt32PowersOfTen[remainingTrailingZeroCount];
                    return;
                }

                int popCount = BitOperations.PopCount((uint)trailingPartialCount);

                int bits2Length = (bits.Length + 1) >> 1;
                uint[]? bits2FromPool = null;
                scoped Span<uint> bits2;
                if (popCount > 1)
                {
                    bits2 = (
                        bits2Length <= BigIntegerCalculator.StackAllocThreshold
                        ? stackalloc uint[BigIntegerCalculator.StackAllocThreshold]
                        : bits2FromPool = ArrayPool<uint>.Shared.Rent(bits2Length)).Slice(0, bits2Length);
                    bits2.Clear();
                }
                else
                    bits2 = default;

                int curLength;
                scoped Span<uint> curBits, otherBits;

                if ((popCount & 1) != 0)
                {
                    curBits = bits;
                    otherBits = bits2;
                }
                else
                {
                    curBits = bits2;
                    otherBits = bits;
                }

                // Copy first
                int fi = BitOperations.TrailingZeroCount(trailingPartialCount);
                {
                    ReadOnlySpan<uint> first = GetSpan(fi);
                    first.CopyTo(curBits);
                    curLength = first.Length;
                    trailingPartialCount >>= fi;
                    trailingPartialCount >>= 1;
                }


                for (int i = fi + 1; trailingPartialCount != 0 && i + 1 < Indexes.Length; i++, trailingPartialCount >>= 1)
                {
                    Debug.Assert(GetSpan(i).Length >= curLength);
                    if ((trailingPartialCount & 1) != 0)
                    {
                        ReadOnlySpan<uint> power = GetSpan(i);

                        Span<uint> src = curBits.Slice(0, curLength);
                        Span<uint> dst = otherBits.Slice(0, curLength += power.Length);
                        BigIntegerCalculator.Multiply(power, src, dst);

                        Span<uint> tmp = curBits;
                        curBits = otherBits;
                        otherBits = tmp;
                        otherBits.Clear();

                        // Trim
                        while (--curLength >= 0 && curBits[curLength] == 0) ;
                        ++curLength;
                    }
                }

                Debug.Assert(Unsafe.AreSame(ref bits[0], ref curBits[0]));

                curBits = curBits.Slice(0, curLength);
                uint multiplier = UInt32PowersOfTen[remainingTrailingZeroCount];
                uint carry = 0;
                for (int i = 0; i < curBits.Length; i++)
                {
                    ulong p = (ulong)multiplier * curBits[i] + carry;
                    curBits[i] = (uint)p;
                    carry = (uint)(p >> 32);
                }

                if (carry != 0)
                {
                    bits[curLength] = carry;
                }

                if (bits2FromPool != null)
                    ArrayPool<uint>.Shared.Return(bits2FromPool);
            }

            public static int GetBufferSize(int digits)
            {
                int scale1E9 = digits / MaxPartialDigits;
                int log2 = BitOperations.Log2((uint)scale1E9) + 1;
                return (uint)log2 < (uint)Indexes.Length ? Indexes[log2] + 1 : Indexes[^1];
            }
        }

        internal static char ParseFormatSpecifier(ReadOnlySpan<char> format, out int digits)
        {
            digits = -1;
            if (format.Length == 0)
            {
                return 'R';
            }

            int i = 0;
            char ch = format[i];
            if (char.IsAsciiLetter(ch))
            {
                // The digits value must be >= 0 && <= 999_999_999,
                // but it can begin with any number of 0s, and thus we may need to check more than 9
                // digits.  Further, for compat, we need to stop when we hit a null char.
                i++;
                int n = 0;
                while ((uint)i < (uint)format.Length && char.IsAsciiDigit(format[i]))
                {
                    // Check if we are about to overflow past our limit of 9 digits
                    if (n >= 100_000_000)
                    {
                        throw new FormatException(SR.Argument_BadFormatSpecifier);
                    }
                    n = ((n * 10) + format[i++] - '0');
                }

                // If we're at the end of the digits rather than having stopped because we hit something
                // other than a digit or overflowed, return the standard format info.
                if (i >= format.Length || format[i] == '\0')
                {
                    digits = n;
                    return ch;
                }
            }
            return (char)0; // Custom format
        }

        private static string? FormatBigIntegerToHex(bool targetSpan, BigInteger value, char format, int digits, NumberFormatInfo info, Span<char> destination, out int charsWritten, out bool spanSuccess)
        {
            Debug.Assert(format == 'x' || format == 'X');

            // Get the bytes that make up the BigInteger.
            byte[]? arrayToReturnToPool = null;
            Span<byte> bits = stackalloc byte[64]; // arbitrary threshold
            if (!value.TryWriteOrCountBytes(bits, out int bytesWrittenOrNeeded))
            {
                bits = arrayToReturnToPool = ArrayPool<byte>.Shared.Rent(bytesWrittenOrNeeded);
                bool success = value.TryWriteBytes(bits, out bytesWrittenOrNeeded);
                Debug.Assert(success);
            }
            bits = bits.Slice(0, bytesWrittenOrNeeded);

            var sb = new ValueStringBuilder(stackalloc char[128]); // each byte is typically two chars

            int cur = bits.Length - 1;
            if (cur > -1)
            {
                // [FF..F8] drop the high F as the two's complement negative number remains clear
                // [F7..08] retain the high bits as the two's complement number is wrong without it
                // [07..00] drop the high 0 as the two's complement positive number remains clear
                bool clearHighF = false;
                byte head = bits[cur];

                if (head > 0xF7)
                {
                    head -= 0xF0;
                    clearHighF = true;
                }

                if (head < 0x08 || clearHighF)
                {
                    // {0xF8-0xFF} print as {8-F}
                    // {0x00-0x07} print as {0-7}
                    sb.Append(head < 10 ?
                        (char)(head + '0') :
                        format == 'X' ? (char)((head & 0xF) - 10 + 'A') : (char)((head & 0xF) - 10 + 'a'));
                    cur--;
                }
            }

            if (cur > -1)
            {
                Span<char> chars = sb.AppendSpan((cur + 1) * 2);
                int charsPos = 0;
                string hexValues = format == 'x' ? "0123456789abcdef" : "0123456789ABCDEF";
                while (cur > -1)
                {
                    byte b = bits[cur--];
                    chars[charsPos++] = hexValues[b >> 4];
                    chars[charsPos++] = hexValues[b & 0xF];
                }
            }

            if (digits > sb.Length)
            {
                // Insert leading zeros, e.g. user specified "X5" so we create "0ABCD" instead of "ABCD"
                sb.Insert(
                    0,
                    value._sign >= 0 ? '0' : (format == 'x') ? 'f' : 'F',
                    digits - sb.Length);
            }

            if (arrayToReturnToPool != null)
            {
                ArrayPool<byte>.Shared.Return(arrayToReturnToPool);
            }

            if (targetSpan)
            {
                spanSuccess = sb.TryCopyTo(destination, out charsWritten);
                return null;
            }
            else
            {
                charsWritten = 0;
                spanSuccess = false;
                return sb.ToString();
            }
        }

        private static string? FormatBigIntegerToBinary(bool targetSpan, BigInteger value, int digits, Span<char> destination, out int charsWritten, out bool spanSuccess)
        {
            // Get the bytes that make up the BigInteger.
            byte[]? arrayToReturnToPool = null;
            Span<byte> bytes = stackalloc byte[64]; // arbitrary threshold
            if (!value.TryWriteOrCountBytes(bytes, out int bytesWrittenOrNeeded))
            {
                bytes = arrayToReturnToPool = ArrayPool<byte>.Shared.Rent(bytesWrittenOrNeeded);
                bool success = value.TryWriteBytes(bytes, out _);
                Debug.Assert(success);
            }
            bytes = bytes.Slice(0, bytesWrittenOrNeeded);

            Debug.Assert(!bytes.IsEmpty);

            byte highByte = bytes[^1];

            int charsInHighByte = 9 - byte.LeadingZeroCount(value._sign >= 0 ? highByte : (byte)~highByte);
            long tmpCharCount = charsInHighByte + ((long)(bytes.Length - 1) << 3);

            if (tmpCharCount > Array.MaxLength)
            {
                Debug.Assert(arrayToReturnToPool is not null);
                ArrayPool<byte>.Shared.Return(arrayToReturnToPool);

                throw new FormatException(SR.Format_TooLarge);
            }

            int charsForBits = (int)tmpCharCount;

            Debug.Assert(digits < Array.MaxLength);
            int charsIncludeDigits = Math.Max(digits, charsForBits);

            try
            {
                scoped ValueStringBuilder sb;
                if (targetSpan)
                {
                    if (charsIncludeDigits > destination.Length)
                    {
                        charsWritten = 0;
                        spanSuccess = false;
                        return null;
                    }

                    // Because we have ensured destination can take actual char length, so now just use ValueStringBuilder as wrapper so that subsequent logic can be reused by 2 flows (targetSpan and non-targetSpan);
                    // meanwhile there is no need to copy to destination again after format data for targetSpan flow.
                    sb = new ValueStringBuilder(destination);
                }
                else
                {
                    // each byte is typically eight chars
                    sb = charsIncludeDigits > 512
                        ? new ValueStringBuilder(charsIncludeDigits)
                        : new ValueStringBuilder(stackalloc char[512]);
                }

                if (digits > charsForBits)
                {
                    sb.Append(value._sign >= 0 ? '0' : '1', digits - charsForBits);
                }

                AppendByte(ref sb, highByte, charsInHighByte - 1);

                for (int i = bytes.Length - 2; i >= 0; i--)
                {
                    AppendByte(ref sb, bytes[i]);
                }

                Debug.Assert(sb.Length == charsIncludeDigits);

                if (targetSpan)
                {
                    charsWritten = charsIncludeDigits;
                    spanSuccess = true;
                    return null;
                }

                charsWritten = 0;
                spanSuccess = false;
                return sb.ToString();
            }
            finally
            {
                if (arrayToReturnToPool is not null)
                {
                    ArrayPool<byte>.Shared.Return(arrayToReturnToPool);
                }
            }

            static void AppendByte(ref ValueStringBuilder sb, byte b, int startHighBit = 7)
            {
                for (int i = startHighBit; i >= 0; i--)
                {
                    sb.Append((char)('0' + ((b >> i) & 0x1)));
                }
            }
        }

        internal static string FormatBigInteger(BigInteger value, string? format, NumberFormatInfo info)
        {
            return FormatBigInteger(targetSpan: false, value, format, format, info, default, out _, out _)!;
        }

        internal static bool TryFormatBigInteger(BigInteger value, ReadOnlySpan<char> format, NumberFormatInfo info, Span<char> destination, out int charsWritten)
        {
            FormatBigInteger(targetSpan: true, value, null, format, info, destination, out charsWritten, out bool spanSuccess);
            return spanSuccess;
        }

        private static string? FormatBigInteger(
            bool targetSpan, BigInteger value,
            string? formatString, ReadOnlySpan<char> formatSpan,
            NumberFormatInfo info, Span<char> destination, out int charsWritten, out bool spanSuccess)
        {
            Debug.Assert(formatString == null || formatString.Length == formatSpan.Length);

            int digits = 0;
            char fmt = ParseFormatSpecifier(formatSpan, out digits);
            if (fmt == 'x' || fmt == 'X')
            {
                return FormatBigIntegerToHex(targetSpan, value, fmt, digits, info, destination, out charsWritten, out spanSuccess);
            }
            if (fmt == 'b' || fmt == 'B')
            {
                return FormatBigIntegerToBinary(targetSpan, value, digits, destination, out charsWritten, out spanSuccess);
            }

            if (value._bits == null)
            {
                if (fmt == 'g' || fmt == 'G' || fmt == 'r' || fmt == 'R')
                {
                    formatSpan = formatString = digits > 0 ? $"D{digits}" : "D";
                }

                if (targetSpan)
                {
                    spanSuccess = value._sign.TryFormat(destination, out charsWritten, formatSpan, info);
                    return null;
                }
                else
                {
                    charsWritten = 0;
                    spanSuccess = false;
                    return value._sign.ToString(formatString, info);
                }
            }

            // First convert to base 10^9.
            const uint kuBase = 1000000000; // 10^9
            const int kcchBase = 9;

            int cuSrc = value._bits.Length;
            int cuMax;
            try
            {
                cuMax = checked(cuSrc * 10 / 9 + 2);
            }
            catch (OverflowException e) { throw new FormatException(SR.Format_TooLarge, e); }
            uint[] rguDst = new uint[cuMax];
            int cuDst = 0;

            for (int iuSrc = cuSrc; --iuSrc >= 0;)
            {
                uint uCarry = value._bits[iuSrc];
                for (int iuDst = 0; iuDst < cuDst; iuDst++)
                {
                    Debug.Assert(rguDst[iuDst] < kuBase);
                    ulong uuRes = NumericsHelpers.MakeUInt64(rguDst[iuDst], uCarry);
                    rguDst[iuDst] = (uint)(uuRes % kuBase);
                    uCarry = (uint)(uuRes / kuBase);
                }
                if (uCarry != 0)
                {
                    rguDst[cuDst++] = uCarry % kuBase;
                    uCarry /= kuBase;
                    if (uCarry != 0)
                        rguDst[cuDst++] = uCarry;
                }
            }

            int cchMax;
            try
            {
                // Each uint contributes at most 9 digits to the decimal representation.
                cchMax = checked(cuDst * kcchBase);
            }
            catch (OverflowException e) { throw new FormatException(SR.Format_TooLarge, e); }

            bool decimalFmt = (fmt == 'g' || fmt == 'G' || fmt == 'd' || fmt == 'D' || fmt == 'r' || fmt == 'R');
            if (decimalFmt)
            {
                if (digits > 0 && digits > cchMax)
                    cchMax = digits;
                if (value._sign < 0)
                {
                    try
                    {
                        // Leave an extra slot for a minus sign.
                        cchMax = checked(cchMax + info.NegativeSign.Length);
                    }
                    catch (OverflowException e) { throw new FormatException(SR.Format_TooLarge, e); }
                }
            }

            int rgchBufSize;

            try
            {
                // We'll pass the rgch buffer to native code, which is going to treat it like a string of digits, so it needs
                // to be null terminated.  Let's ensure that we can allocate a buffer of that size.
                rgchBufSize = checked(cchMax + 1);
            }
            catch (OverflowException e) { throw new FormatException(SR.Format_TooLarge, e); }

            char[] rgch = new char[rgchBufSize];

            int ichDst = cchMax;

            for (int iuDst = 0; iuDst < cuDst - 1; iuDst++)
            {
                uint uDig = rguDst[iuDst];
                Debug.Assert(uDig < kuBase);
                for (int cch = kcchBase; --cch >= 0;)
                {
                    rgch[--ichDst] = (char)('0' + uDig % 10);
                    uDig /= 10;
                }
            }
            for (uint uDig = rguDst[cuDst - 1]; uDig != 0;)
            {
                rgch[--ichDst] = (char)('0' + uDig % 10);
                uDig /= 10;
            }

            if (!decimalFmt)
            {
                // sign = true for negative and false for 0 and positive values
                bool sign = (value._sign < 0);
                // The cut-off point to switch (G)eneral from (F)ixed-point to (E)xponential form
                int precision = 29;
                int scale = cchMax - ichDst;

                var sb = new ValueStringBuilder(stackalloc char[128]); // arbitrary stack cut-off
                FormatProvider.FormatBigInteger(ref sb, precision, scale, sign, formatSpan, info, rgch, ichDst);

                if (targetSpan)
                {
                    spanSuccess = sb.TryCopyTo(destination, out charsWritten);
                    return null;
                }
                else
                {
                    charsWritten = 0;
                    spanSuccess = false;
                    return sb.ToString();
                }
            }

            // Format Round-trip decimal
            // This format is supported for integral types only. The number is converted to a string of
            // decimal digits (0-9), prefixed by a minus sign if the number is negative. The precision
            // specifier indicates the minimum number of digits desired in the resulting string. If required,
            // the number is padded with zeros to its left to produce the number of digits given by the
            // precision specifier.
            int numDigitsPrinted = cchMax - ichDst;
            while (digits > 0 && digits > numDigitsPrinted)
            {
                // pad leading zeros
                rgch[--ichDst] = '0';
                digits--;
            }
            if (value._sign < 0)
            {
                string negativeSign = info.NegativeSign;
                for (int i = negativeSign.Length - 1; i > -1; i--)
                    rgch[--ichDst] = negativeSign[i];
            }

            int resultLength = cchMax - ichDst;
            if (!targetSpan)
            {
                charsWritten = 0;
                spanSuccess = false;
                return new string(rgch, ichDst, cchMax - ichDst);
            }
            else if (new ReadOnlySpan<char>(rgch, ichDst, cchMax - ichDst).TryCopyTo(destination))
            {
                charsWritten = resultLength;
                spanSuccess = true;
                return null;
            }
            else
            {
                charsWritten = 0;
                spanSuccess = false;
                return null;
            }
        }
    }

    internal interface IBigIntegerHexOrBinaryParser<TParser, TChar>
        where TParser : struct, IBigIntegerHexOrBinaryParser<TParser, TChar>
        where TChar : unmanaged, IBinaryInteger<TChar>
    {
        static abstract int BitsPerDigit { get; }

        static virtual int DigitsPerBlock => sizeof(uint) * 8 / TParser.BitsPerDigit;

        static abstract NumberStyles BlockNumberStyle { get; }

        static abstract uint GetSignBitsIfValid(uint ch);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static virtual bool TryParseUnalignedBlock(ReadOnlySpan<TChar> input, out uint result)
        {
            if (typeof(TChar) == typeof(char))
            {
                return uint.TryParse(MemoryMarshal.Cast<TChar, char>(input), TParser.BlockNumberStyle, null, out result);
            }

            throw new NotSupportedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static virtual bool TryParseSingleBlock(ReadOnlySpan<TChar> input, out uint result)
            => TParser.TryParseUnalignedBlock(input, out result);

        static virtual bool TryParseWholeBlocks(ReadOnlySpan<TChar> input, Span<uint> destination)
        {
            Debug.Assert(destination.Length * TParser.DigitsPerBlock == input.Length);
            ref TChar lastWholeBlockStart = ref Unsafe.Add(ref MemoryMarshal.GetReference(input), input.Length - TParser.DigitsPerBlock);

            for (int i = 0; i < destination.Length; i++)
            {
                if (!TParser.TryParseSingleBlock(
                    MemoryMarshal.CreateReadOnlySpan(ref Unsafe.Subtract(ref lastWholeBlockStart, i * TParser.DigitsPerBlock), TParser.DigitsPerBlock),
                    out destination[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }

    internal readonly struct BigIntegerHexParser<TChar> : IBigIntegerHexOrBinaryParser<BigIntegerHexParser<TChar>, TChar>
        where TChar : unmanaged, IBinaryInteger<TChar>
    {
        public static int BitsPerDigit => 4;

        public static NumberStyles BlockNumberStyle => NumberStyles.AllowHexSpecifier;

        // A valid ASCII hex digit is positive (0-7) if it starts with 00110
        public static uint GetSignBitsIfValid(uint ch) => (uint)((ch & 0b_1111_1000) == 0b_0011_0000 ? 0 : -1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryParseWholeBlocks(ReadOnlySpan<TChar> input, Span<uint> destination)
        {
            if (typeof(TChar) == typeof(char))
            {
                if (Convert.FromHexString(MemoryMarshal.Cast<TChar, char>(input), MemoryMarshal.AsBytes(destination), out _, out _) != OperationStatus.Done)
                {
                    return false;
                }

                if (BitConverter.IsLittleEndian)
                {
                    MemoryMarshal.AsBytes(destination).Reverse();
                }
                else
                {
                    destination.Reverse();
                }

                return true;
            }

            throw new NotSupportedException();
        }
    }

    internal readonly struct BigIntegerBinaryParser<TChar> : IBigIntegerHexOrBinaryParser<BigIntegerBinaryParser<TChar>, TChar>
        where TChar : unmanaged, IBinaryInteger<TChar>
    {
        public static int BitsPerDigit => 1;

        public static NumberStyles BlockNumberStyle => NumberStyles.AllowBinarySpecifier;

        // Taking the LSB is enough for distinguishing 0/1
        public static uint GetSignBitsIfValid(uint ch) => (uint)(((int)ch << 31) >> 31);
    }
}
