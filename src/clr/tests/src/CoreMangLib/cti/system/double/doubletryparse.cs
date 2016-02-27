// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

// Ported to CoreCLR from Co7532TryParse_all.cs
// Tests Double.TryParse(String), Double.TryParse(String, NumberStyles, IFormatProvider, ref Double)
// 2003/04/01  KatyK
// 2007/06/28  adapted by MarielY
public class DoubleTryParse
{
    static bool verbose = false;

    public static int Main()
    {
        bool passed = true;

        try
        {
            // Make the test culture independent
            TestLibrary.Utilities.CurrentCulture = CultureInfo.InvariantCulture;

            // Set up NFIs to use
            NumberFormatInfo goodNFI = new NumberFormatInfo();

            NumberFormatInfo corruptNFI = new NumberFormatInfo(); // DecimalSeparator == GroupSeparator 
            corruptNFI.NumberDecimalSeparator = ".";
            corruptNFI.NumberGroupSeparator = ".";
            corruptNFI.CurrencyDecimalSeparator = ".";
            corruptNFI.CurrencyGroupSeparator = ".";
            corruptNFI.CurrencySymbol = "$";

            NumberFormatInfo swappedNFI = new NumberFormatInfo(); // DecimalSeparator & GroupSeparator swapped
            swappedNFI.NumberDecimalSeparator = ".";
            swappedNFI.NumberGroupSeparator = ",";
            swappedNFI.CurrencyDecimalSeparator = ",";
            swappedNFI.CurrencyGroupSeparator = ".";
            swappedNFI.CurrencySymbol = "$";

            NumberFormatInfo distinctNFI = new NumberFormatInfo(); // DecimalSeparator & GroupSeparator distinct
            distinctNFI.NumberDecimalSeparator = ".";
            distinctNFI.NumberGroupSeparator = ",";
            distinctNFI.CurrencyDecimalSeparator = ":";
            distinctNFI.CurrencyGroupSeparator = ";";
            distinctNFI.CurrencySymbol = "$";

            NumberFormatInfo customNFI = new NumberFormatInfo();
            customNFI.NegativeSign = "^";

            NumberFormatInfo ambigNFI = new NumberFormatInfo();
            ambigNFI.NegativeSign = "^";
            ambigNFI.CurrencySymbol = "^";

            CultureInfo germanCulture = new CultureInfo("de-DE");
			CultureInfo japaneseCulture;
			try
			{
				japaneseCulture = new CultureInfo("ja-JP");
			}
			catch (Exception)
			{
				TestLibrary.TestFramework.LogInformation("East Asian Languages are not installed. Skipping Japanese culture test(s).");
				japaneseCulture = null;
			} 

            // Parse tests included for comparison/regression
            passed &= VerifyDoubleParse("0", 0);
            passed &= VerifyDoubleParse("5", 5);
            passed &= VerifyDoubleParse("5  ", 5);
            passed &= VerifyDoubleParse("5\0", 5);
            passed &= VerifyDoubleParse("-5", -5);
            passed &= VerifyDoubleParse("893382737", 893382737);
            passed &= VerifyDoubleParse("-893382737", -893382737);
            passed &= VerifyDoubleParse("1234567891", 1234567891);
            passed &= VerifyDoubleParse("-1234567891", -1234567891);
            passed &= VerifyDoubleParse("123456789123456789", 123456789123456789);
            passed &= VerifyDoubleParse("-123456789123456789", -123456789123456789);
            passed &= VerifyDoubleParse("5", NumberStyles.Integer, CultureInfo.InvariantCulture, 5);
            passed &= VerifyDoubleParse("5  \0", NumberStyles.Integer, CultureInfo.InvariantCulture, 5);
            passed &= VerifyDoubleParse("5\0\0\0", NumberStyles.Integer, CultureInfo.InvariantCulture, 5);
            passed &= VerifyDoubleParse("-5", NumberStyles.Integer, CultureInfo.InvariantCulture, -5);
            passed &= VerifyDoubleParse("5", NumberStyles.Integer, goodNFI, 5);
            passed &= VerifyDoubleParse("5.0", NumberStyles.AllowDecimalPoint, goodNFI, 5);
            passed &= VerifyDoubleParse("5.3", NumberStyles.AllowDecimalPoint, goodNFI, 5.3);
            passed &= VerifyDoubleParse("123456789123456789123", 123456789123456789123.0);
            passed &= VerifyDoubleParse("-123456789123456789123", -123456789123456789123.0);
            passed &= VerifyDoubleParse("18446744073709551615", 18446744073709551615);
            passed &= VerifyDoubleParse("79228162514264337593543950335", 79228162514264337593543950335.0);
            passed &= VerifyDoubleParse("-79228162514264337593543950335", -79228162514264337593543950335.0);
            passed &= VerifyDoubleParse("5.555555555", 5.555555555);
            passed &= VerifyDoubleParse("1.000000", 1.0);
            passed &= VerifyDoubleParse("79228162514264337593543950336", 79228162514264337593543950336.0);
            passed &= VerifyDoubleParse("-79228162514264337593543950336", -79228162514264337593543950336.0);
            passed &= VerifyDoubleParse("1.79769313486231E+308", NumberStyles.Float | NumberStyles.AllowExponent, null, 1.79769313486231E+308);
            passed &= VerifyDoubleParse("-1.79769313486231E+308", NumberStyles.Float | NumberStyles.AllowExponent, null, -1.79769313486231E+308);
            passed &= VerifyDoubleParse("NaN", NumberStyles.Float, NumberFormatInfo.InvariantInfo, Double.NaN);
            passed &= VerifyDoubleParse("Infinity", NumberStyles.Float, NumberFormatInfo.InvariantInfo, Double.PositiveInfinity);
            passed &= VerifyDoubleParse("-Infinity", NumberStyles.Float, NumberFormatInfo.InvariantInfo, Double.NegativeInfinity);
            passed &= VerifyDoubleParse("123", NumberStyles.Integer, germanCulture, 123);
            passed &= VerifyDoubleParse("123", NumberStyles.Integer, japaneseCulture, 123);
            passed &= VerifyDoubleParse("123.456", NumberStyles.Any, germanCulture, 123456);
            passed &= VerifyDoubleParse("123,456", NumberStyles.Any, japaneseCulture, 123456);
            passed &= VerifyDoubleParse("123,456", NumberStyles.AllowDecimalPoint, germanCulture, 123.456);
            passed &= VerifyDoubleParse("123.456", NumberStyles.AllowDecimalPoint, japaneseCulture, 123.456);
            passed &= VerifyDoubleParse("5,23 " + germanCulture.NumberFormat.CurrencySymbol, NumberStyles.Any, germanCulture, 5.23); // currency
            passed &= VerifyDoubleParse("5.23 " + germanCulture.NumberFormat.CurrencySymbol, NumberStyles.Any, germanCulture, 523); // currency
            //
            passed &= VerifyDoubleParse("5", NumberStyles.Integer, corruptNFI, 5);
            passed &= VerifyDoubleParse("5", NumberStyles.Number, corruptNFI, 5);
            passed &= VerifyDoubleParse("5.3", NumberStyles.Number, corruptNFI, 5.3);
            passed &= VerifyDoubleParseException("5,3", NumberStyles.Number, corruptNFI, typeof(FormatException));
            passed &= VerifyDoubleParseException("5.2.3", NumberStyles.Number, corruptNFI, typeof(FormatException));
            passed &= VerifyDoubleParse("$5.3", NumberStyles.Currency, corruptNFI, 5.3);
            passed &= VerifyDoubleParseException("$5,3", NumberStyles.Currency, corruptNFI, typeof(FormatException));
            passed &= VerifyDoubleParseException("$5.2.3", NumberStyles.Currency, corruptNFI, typeof(FormatException));
            passed &= VerifyDoubleParse("5.3", NumberStyles.Currency, corruptNFI, 5.3);
            passed &= VerifyDoubleParseException("5,3", NumberStyles.Currency, corruptNFI, typeof(FormatException));
            passed &= VerifyDoubleParseException("5.2.3", NumberStyles.Currency, corruptNFI, typeof(FormatException));
            passed &= VerifyDoubleParse("5.3", NumberStyles.Any, corruptNFI, 5.3);
            passed &= VerifyDoubleParseException("5,3", NumberStyles.Any, corruptNFI, typeof(FormatException));
            passed &= VerifyDoubleParseException("5.2.3", NumberStyles.Any, corruptNFI, typeof(FormatException));
            //
            passed &= VerifyDoubleParse("5", NumberStyles.Integer, swappedNFI, 5);
            passed &= VerifyDoubleParseException("1,234", NumberStyles.Integer, swappedNFI, typeof(FormatException));
            passed &= VerifyDoubleParse("5", NumberStyles.Number, swappedNFI, 5);
            passed &= VerifyDoubleParse("5.0", NumberStyles.Number, swappedNFI, 5);
            passed &= VerifyDoubleParse("1,234", NumberStyles.Number, swappedNFI, 1234);
            passed &= VerifyDoubleParse("1,234.0", NumberStyles.Number, swappedNFI, 1234);
            passed &= VerifyDoubleParseException("5.000.000", NumberStyles.Number, swappedNFI, typeof(FormatException));
            passed &= VerifyDoubleParseException("5.000,00", NumberStyles.Number, swappedNFI, typeof(FormatException));
            passed &= VerifyDoubleParse("5.000", NumberStyles.Currency, swappedNFI, 5); //???
            passed &= VerifyDoubleParseException("5.000,00", NumberStyles.Currency, swappedNFI, typeof(FormatException)); //???
            passed &= VerifyDoubleParse("$5.000", NumberStyles.Currency, swappedNFI, 5000);
            passed &= VerifyDoubleParse("$5.000,00", NumberStyles.Currency, swappedNFI, 5000);
            passed &= VerifyDoubleParse("5.000", NumberStyles.Any, swappedNFI, 5);  //?
            passed &= VerifyDoubleParseException("5.000,00", NumberStyles.Any, swappedNFI, typeof(FormatException));  //?
            passed &= VerifyDoubleParse("$5.000", NumberStyles.Any, swappedNFI, 5000);
            passed &= VerifyDoubleParse("$5.000,00", NumberStyles.Any, swappedNFI, 5000);
            passed &= VerifyDoubleParse("5,0", NumberStyles.Currency, swappedNFI, 5);
            passed &= VerifyDoubleParse("$5,0", NumberStyles.Currency, swappedNFI, 5);
            passed &= VerifyDoubleParse("5,000", NumberStyles.Currency, swappedNFI, 5);
            passed &= VerifyDoubleParse("$5,000", NumberStyles.Currency, swappedNFI, 5);
            passed &= VerifyDoubleParseException("5,000.0", NumberStyles.Currency, swappedNFI, typeof(FormatException));
            passed &= VerifyDoubleParseException("$5,000.0", NumberStyles.Currency, swappedNFI, typeof(FormatException));
            passed &= VerifyDoubleParse("5,000", NumberStyles.Any, swappedNFI, 5);
            passed &= VerifyDoubleParse("$5,000", NumberStyles.Any, swappedNFI, 5);
            passed &= VerifyDoubleParseException("5,000.0", NumberStyles.Any, swappedNFI, typeof(FormatException));
            passed &= VerifyDoubleParseException("$5,000.0", NumberStyles.Any, swappedNFI, typeof(FormatException));
            //
            passed &= VerifyDoubleParse("5.0", NumberStyles.Number, distinctNFI, 5);
            passed &= VerifyDoubleParse("1,234.0", NumberStyles.Number, distinctNFI, 1234);
            passed &= VerifyDoubleParse("5.0", NumberStyles.Currency, distinctNFI, 5);
            passed &= VerifyDoubleParse("1,234.0", NumberStyles.Currency, distinctNFI, 1234);
            passed &= VerifyDoubleParse("5.0", NumberStyles.Any, distinctNFI, 5);
            passed &= VerifyDoubleParse("1,234.0", NumberStyles.Any, distinctNFI, 1234);
            passed &= VerifyDoubleParseException("$5.0", NumberStyles.Currency, distinctNFI, typeof(FormatException));
            passed &= VerifyDoubleParseException("$5.0", NumberStyles.Any, distinctNFI, typeof(FormatException));
            passed &= VerifyDoubleParseException("5:0", NumberStyles.Number, distinctNFI, typeof(FormatException));
            passed &= VerifyDoubleParseException("5;0", NumberStyles.Number, distinctNFI, typeof(FormatException));
            passed &= VerifyDoubleParseException("$5:0", NumberStyles.Number, distinctNFI, typeof(FormatException));
            passed &= VerifyDoubleParse("5:0", NumberStyles.Currency, distinctNFI, 5);
            passed &= VerifyDoubleParse("5:000", NumberStyles.Currency, distinctNFI, 5);
            passed &= VerifyDoubleParse("5;000", NumberStyles.Currency, distinctNFI, 5000);
            passed &= VerifyDoubleParse("$5:0", NumberStyles.Currency, distinctNFI, 5);
            passed &= VerifyDoubleParse("$5;0", NumberStyles.Currency, distinctNFI, 50);
            passed &= VerifyDoubleParse("5:0", NumberStyles.Any, distinctNFI, 5);
            passed &= VerifyDoubleParse("5;0", NumberStyles.Any, distinctNFI, 50);
            passed &= VerifyDoubleParse("$5:0", NumberStyles.Any, distinctNFI, 5);
            passed &= VerifyDoubleParse("$5;0", NumberStyles.Any, distinctNFI, 50);
            passed &= VerifyDoubleParseException("123,456;789.0", NumberStyles.Number, distinctNFI, typeof(FormatException));
            passed &= VerifyDoubleParse("123,456;789.0", NumberStyles.Currency, distinctNFI, 123456789);
            passed &= VerifyDoubleParse("123,456;789.0", NumberStyles.Any, distinctNFI, 123456789);
            passed &= VerifyDoubleParseException("$123,456;789.0", NumberStyles.Any, distinctNFI, typeof(FormatException));
            //
            passed &= VerifyDoubleParseException("1.79769313486231E+309", NumberStyles.Float | NumberStyles.AllowExponent, null, typeof(OverflowException));
            passed &= VerifyDoubleParseException("-1.79769313486231E+309", NumberStyles.Float | NumberStyles.AllowExponent, null, typeof(OverflowException));
            passed &= VerifyDoubleParseException("1.89769313486231E+308", NumberStyles.Float | NumberStyles.AllowExponent, null, typeof(OverflowException));
            passed &= VerifyDoubleParseException("-1.89769313486231E+308", NumberStyles.Float | NumberStyles.AllowExponent, null, typeof(OverflowException));
            passed &= VerifyDoubleParseException("Garbage", typeof(FormatException));
            passed &= VerifyDoubleParseException("5\0Garbage", typeof(FormatException));
            passed &= VerifyDoubleParseException(null, typeof(ArgumentNullException));
            passed &= VerifyDoubleParseException("FF", NumberStyles.HexNumber, goodNFI, typeof(ArgumentException));
            passed &= VerifyDoubleParseException("4", (NumberStyles)(-1), typeof(ArgumentException));
            passed &= VerifyDoubleParseException("4", (NumberStyles)0x10000, typeof(ArgumentException));
            passed &= VerifyDoubleParseException("4", (NumberStyles)(-1), CultureInfo.InvariantCulture, typeof(ArgumentException));
            passed &= VerifyDoubleParseException("4", (NumberStyles)0x10000, CultureInfo.InvariantCulture, typeof(ArgumentException));
            passed &= VerifyDoubleParseException("123,000,000,000,000,000,000", NumberStyles.Any, germanCulture, typeof(FormatException));
            passed &= VerifyDoubleParseException("123.000.000.000.000.000.000", NumberStyles.Any, japaneseCulture, typeof(FormatException));
            passed &= VerifyDoubleParseException("5,00 " + germanCulture.NumberFormat.CurrencySymbol, NumberStyles.Integer, germanCulture, typeof(FormatException)); // currency


            /////////// TryParse(String)
            //// Pass cases
            passed &= VerifyDoubleTryParse("0", 0, true);
            passed &= VerifyDoubleTryParse("-0", 0, true);
            passed &= VerifyDoubleTryParse("5", 5, true);
            passed &= VerifyDoubleTryParse(" 5 ", 5, true);
            passed &= VerifyDoubleTryParse("-5", -5, true);
            passed &= VerifyDoubleTryParse("5\0", 5, true);
            passed &= VerifyDoubleTryParse("5  \0", 5, true);
            passed &= VerifyDoubleTryParse("5\0\0\0", 5, true);
            passed &= VerifyDoubleTryParse("893382737", 893382737, true);
            passed &= VerifyDoubleTryParse("-893382737", -893382737, true);
            passed &= VerifyDoubleTryParse("1234567891", 1234567891, true);
            passed &= VerifyDoubleTryParse("-1234567891", -1234567891, true);
            passed &= VerifyDoubleTryParse("123456789123456789", 123456789123456789, true);
            passed &= VerifyDoubleTryParse("-123456789123456789", -123456789123456789, true);
            passed &= VerifyDoubleTryParse("123456789123456789123", 123456789123456789123.0, true);
            passed &= VerifyDoubleTryParse("-123456789123456789123", -123456789123456789123.0, true);
            passed &= VerifyDoubleTryParse("18446744073709551615", 18446744073709551615, true);
            passed &= VerifyDoubleTryParse("79228162514264337593543950335", 79228162514264337593543950335.0, true);
            passed &= VerifyDoubleTryParse("-79228162514264337593543950335", -79228162514264337593543950335.0, true);
            passed &= VerifyDoubleTryParse("79228162514264337593543950336", 79228162514264337593543950336.0, true);
            passed &= VerifyDoubleTryParse("-79228162514264337593543950336", -79228162514264337593543950336.0, true);
            passed &= VerifyDoubleTryParse("7.3", 7.3, true);
            passed &= VerifyDoubleTryParse(".297", 0.297, true);
            passed &= VerifyDoubleTryParse("5.555555555", 5.555555555, true);
            passed &= VerifyDoubleTryParse("1.000000", 1.0, true);
            passed &= VerifyDoubleTryParse("1.234E+05", 123400, true);
            passed &= VerifyDoubleTryParse("NaN", Double.NaN, true);
            passed &= VerifyDoubleTryParse("Infinity", Double.PositiveInfinity, true);
            passed &= VerifyDoubleTryParse("-Infinity", Double.NegativeInfinity, true);

            //// Fail cases
            passed &= VerifyDoubleTryParse(null, 0, false);
            passed &= VerifyDoubleTryParse("", 0, false);
            passed &= VerifyDoubleTryParse("Garbage", 0, false);
            passed &= VerifyDoubleTryParse("5\0Garbage", 0, false);
            passed &= VerifyDoubleTryParse("FF", 0, false);
            passed &= VerifyDoubleTryParse("23 5", 0, false);
            passed &= VerifyDoubleTryParse("1.234+E05", 0, false);


            /////////// TryParse(TryParse(String, NumberStyles, IFormatProvider, ref Double)
            //// Pass cases
            passed &= VerifyDoubleTryParse("5", NumberStyles.Integer, CultureInfo.InvariantCulture, 5, true);
            passed &= VerifyDoubleTryParse("-5", NumberStyles.Integer, CultureInfo.InvariantCulture, -5, true);
            passed &= VerifyDoubleTryParse("-79228162514264337593543950336", NumberStyles.Integer, CultureInfo.InvariantCulture, -79228162514264337593543950336.0, true);
            // Variations on NumberStyles
            passed &= VerifyDoubleTryParse("   5", NumberStyles.AllowLeadingWhite, goodNFI, 5, true);
            passed &= VerifyDoubleTryParse("5", NumberStyles.Number, goodNFI, 5, true);
            passed &= VerifyDoubleTryParse("5.0", NumberStyles.AllowDecimalPoint, goodNFI, 5.0, true);
            passed &= VerifyDoubleTryParse("5.3", NumberStyles.AllowDecimalPoint, goodNFI, 5.3, true);
            passed &= VerifyDoubleTryParse("1.234E+05", NumberStyles.Float | NumberStyles.AllowExponent, goodNFI, 123400, true);
            passed &= VerifyDoubleTryParse("1.79769313486231E+308", NumberStyles.Float | NumberStyles.AllowExponent, null, 1.79769313486231E+308, true);
            passed &= VerifyDoubleTryParse("-1.79769313486231E+308", NumberStyles.Float | NumberStyles.AllowExponent, null, -1.79769313486231E+308, true);
            // Variations on IFP
            passed &= VerifyDoubleTryParse("5", NumberStyles.Integer, goodNFI, 5, true);
            passed &= VerifyDoubleTryParse("5", NumberStyles.Integer, null, 5, true);
            passed &= VerifyDoubleTryParse("5", NumberStyles.Integer, new DateTimeFormatInfo(), 5, true);
            passed &= VerifyDoubleTryParse("^42", NumberStyles.Any, customNFI, -42, true);
            passed &= VerifyDoubleTryParse("123", NumberStyles.Integer, germanCulture, 123, true);
            passed &= VerifyDoubleTryParse("123", NumberStyles.Integer, japaneseCulture, 123, true);
            passed &= VerifyDoubleTryParse("123.456", NumberStyles.Any, germanCulture, 123456, true);
            passed &= VerifyDoubleTryParse("123,456", NumberStyles.Any, japaneseCulture, 123456, true);
            passed &= VerifyDoubleTryParse("123,456", NumberStyles.AllowDecimalPoint, germanCulture, 123.456, true);
            passed &= VerifyDoubleTryParse("123.456", NumberStyles.AllowDecimalPoint, japaneseCulture, 123.456, true);
            passed &= VerifyDoubleTryParse("5,23 " + germanCulture.NumberFormat.CurrencySymbol, NumberStyles.Any, germanCulture, 5.23, true); // currency
            passed &= VerifyDoubleTryParse("5.23 " + germanCulture.NumberFormat.CurrencySymbol, NumberStyles.Any, germanCulture, 523, true); // currency
            //
            //// Fail cases
            passed &= VerifyDoubleTryParse("-42", NumberStyles.Any, customNFI, 0, false);
            passed &= VerifyDoubleTryParse("5   ", NumberStyles.AllowLeadingWhite, goodNFI, 0, false);
            passed &= VerifyDoubleTryParse("5.3", NumberStyles.None, goodNFI, 0, false);
            passed &= VerifyDoubleTryParse("1.234E+05", NumberStyles.AllowExponent, goodNFI, 0, false);
            passed &= VerifyDoubleTryParse("-1.79769313486231E+309", NumberStyles.Float | NumberStyles.AllowExponent, null, 0, false);
            passed &= VerifyDoubleTryParse("1.89769313486231E+308", NumberStyles.Float | NumberStyles.AllowExponent, null, 0, false);
            passed &= VerifyDoubleTryParse("-1.89769313486231E+308", NumberStyles.Float | NumberStyles.AllowExponent, null, 0, false);
            passed &= VerifyDoubleTryParse("123,000,000,000,000,000,000", NumberStyles.Any, germanCulture, 0, false);
            passed &= VerifyDoubleTryParse("123.000.000.000.000.000.000", NumberStyles.Any, japaneseCulture, 0, false);

            //// Exception cases
            passed &= VerifyDoubleTryParseException("12", NumberStyles.HexNumber, CultureInfo.InvariantCulture, typeof(ArgumentException));
            passed &= VerifyDoubleTryParseException("5", NumberStyles.AllowHexSpecifier | NumberStyles.AllowParentheses, null, typeof(ArgumentException));
            passed &= VerifyDoubleTryParseException("4", (NumberStyles)(-1), CultureInfo.InvariantCulture, typeof(ArgumentException));
            passed &= VerifyDoubleTryParseException("4", (NumberStyles)0x10000, CultureInfo.InvariantCulture, typeof(ArgumentException));

            // NumberStyles/NFI variations
            //
            passed &= VerifyDoubleTryParse("5", NumberStyles.Integer, corruptNFI, 5, true);
            passed &= VerifyDoubleTryParse("5", NumberStyles.Number, corruptNFI, 5, true);
            passed &= VerifyDoubleTryParse("5.3", NumberStyles.Number, corruptNFI, 5.3, true);
            passed &= VerifyDoubleTryParse("5,3", NumberStyles.Number, corruptNFI, 0, false);
            passed &= VerifyDoubleTryParse("5.2.3", NumberStyles.Number, corruptNFI, 0, false);
            passed &= VerifyDoubleTryParse("$5.3", NumberStyles.Currency, corruptNFI, 5.3, true);
            passed &= VerifyDoubleTryParse("$5,3", NumberStyles.Currency, corruptNFI, 0, false);
            passed &= VerifyDoubleTryParse("$5.2.3", NumberStyles.Currency, corruptNFI, 0, false);
            passed &= VerifyDoubleTryParse("5.3", NumberStyles.Currency, corruptNFI, 5.3, true);
            passed &= VerifyDoubleTryParse("5,3", NumberStyles.Currency, corruptNFI, 0, false);
            passed &= VerifyDoubleTryParse("5.2.3", NumberStyles.Currency, corruptNFI, 0, false);
            passed &= VerifyDoubleTryParse("5.3", NumberStyles.Any, corruptNFI, 5.3, true);
            passed &= VerifyDoubleTryParse("5,3", NumberStyles.Any, corruptNFI, 0, false);
            passed &= VerifyDoubleTryParse("5.2.3", NumberStyles.Any, corruptNFI, 0, false);
            //
            passed &= VerifyDoubleTryParse("5", NumberStyles.Integer, swappedNFI, 5, true);
            passed &= VerifyDoubleTryParse("1,234", NumberStyles.Integer, swappedNFI, 0, false);
            passed &= VerifyDoubleTryParse("5", NumberStyles.Number, swappedNFI, 5, true);
            passed &= VerifyDoubleTryParse("5.0", NumberStyles.Number, swappedNFI, 5, true);
            passed &= VerifyDoubleTryParse("1,234", NumberStyles.Number, swappedNFI, 1234, true);
            passed &= VerifyDoubleTryParse("1,234.0", NumberStyles.Number, swappedNFI, 1234, true);
            passed &= VerifyDoubleTryParse("5.000.000", NumberStyles.Number, swappedNFI, 0, false);
            passed &= VerifyDoubleTryParse("5.000,00", NumberStyles.Number, swappedNFI, 0, false);
            passed &= VerifyDoubleTryParse("5.000", NumberStyles.Currency, swappedNFI, 5, true); //???
            passed &= VerifyDoubleTryParse("5.000,00", NumberStyles.Currency, swappedNFI, 0, false); //???
            passed &= VerifyDoubleTryParse("$5.000", NumberStyles.Currency, swappedNFI, 5000, true);
            passed &= VerifyDoubleTryParse("$5.000,00", NumberStyles.Currency, swappedNFI, 5000, true);
            passed &= VerifyDoubleTryParse("5.000", NumberStyles.Any, swappedNFI, 5, true);  //?
            passed &= VerifyDoubleTryParse("5.000,00", NumberStyles.Any, swappedNFI, 0, false);  //?
            passed &= VerifyDoubleTryParse("$5.000", NumberStyles.Any, swappedNFI, 5000, true);
            passed &= VerifyDoubleTryParse("$5.000,00", NumberStyles.Any, swappedNFI, 5000, true);
            passed &= VerifyDoubleTryParse("5,0", NumberStyles.Currency, swappedNFI, 5, true);
            passed &= VerifyDoubleTryParse("$5,0", NumberStyles.Currency, swappedNFI, 5, true);
            passed &= VerifyDoubleTryParse("5,000", NumberStyles.Currency, swappedNFI, 5, true);
            passed &= VerifyDoubleTryParse("$5,000", NumberStyles.Currency, swappedNFI, 5, true);
            passed &= VerifyDoubleTryParse("5,000.0", NumberStyles.Currency, swappedNFI, 0, false);
            passed &= VerifyDoubleTryParse("$5,000.0", NumberStyles.Currency, swappedNFI, 0, false);
            passed &= VerifyDoubleTryParse("5,000", NumberStyles.Any, swappedNFI, 5, true);
            passed &= VerifyDoubleTryParse("$5,000", NumberStyles.Any, swappedNFI, 5, true);
            passed &= VerifyDoubleTryParse("5,000.0", NumberStyles.Any, swappedNFI, 0, false);
            passed &= VerifyDoubleTryParse("$5,000.0", NumberStyles.Any, swappedNFI, 0, false);
            //
            passed &= VerifyDoubleTryParse("5.0", NumberStyles.Number, distinctNFI, 5, true);
            passed &= VerifyDoubleTryParse("1,234.0", NumberStyles.Number, distinctNFI, 1234, true);
            passed &= VerifyDoubleTryParse("5.0", NumberStyles.Currency, distinctNFI, 5, true);
            passed &= VerifyDoubleTryParse("1,234.0", NumberStyles.Currency, distinctNFI, 1234, true);
            passed &= VerifyDoubleTryParse("5.0", NumberStyles.Any, distinctNFI, 5, true);
            passed &= VerifyDoubleTryParse("1,234.0", NumberStyles.Any, distinctNFI, 1234, true);
            passed &= VerifyDoubleTryParse("$5.0", NumberStyles.Currency, distinctNFI, 0, false);
            passed &= VerifyDoubleTryParse("$5.0", NumberStyles.Any, distinctNFI, 0, false);
            passed &= VerifyDoubleTryParse("5:0", NumberStyles.Number, distinctNFI, 0, false);
            passed &= VerifyDoubleTryParse("5;0", NumberStyles.Number, distinctNFI, 0, false);
            passed &= VerifyDoubleTryParse("$5:0", NumberStyles.Number, distinctNFI, 0, false);
            passed &= VerifyDoubleTryParse("5:0", NumberStyles.Currency, distinctNFI, 5, true);
            passed &= VerifyDoubleTryParse("5:000", NumberStyles.Currency, distinctNFI, 5, true);
            passed &= VerifyDoubleTryParse("5;000", NumberStyles.Currency, distinctNFI, 5000, true);
            passed &= VerifyDoubleTryParse("$5:0", NumberStyles.Currency, distinctNFI, 5, true);
            passed &= VerifyDoubleTryParse("$5;0", NumberStyles.Currency, distinctNFI, 50, true);
            passed &= VerifyDoubleTryParse("5:0", NumberStyles.Any, distinctNFI, 5, true);
            passed &= VerifyDoubleTryParse("5;0", NumberStyles.Any, distinctNFI, 50, true);
            passed &= VerifyDoubleTryParse("$5:0", NumberStyles.Any, distinctNFI, 5, true);
            passed &= VerifyDoubleTryParse("$5;0", NumberStyles.Any, distinctNFI, 50, true);
            passed &= VerifyDoubleTryParse("123,456;789.0", NumberStyles.Number, distinctNFI, 0, false);
            passed &= VerifyDoubleTryParse("123,456;789.0", NumberStyles.Currency, distinctNFI, 123456789, true);
            passed &= VerifyDoubleTryParse("123,456;789.0", NumberStyles.Any, distinctNFI, 123456789, true);
            passed &= VerifyDoubleTryParse("$123,456;789.0", NumberStyles.Any, distinctNFI, 0, false);


            // Should these pass or fail?  Current parse behavior is to pass, so they might be
            // parse bugs, but they're not tryparse bugs.
            passed &= VerifyDoubleParse("5", NumberStyles.AllowExponent, goodNFI, 5);
            passed &= VerifyDoubleTryParse("5", NumberStyles.AllowExponent, goodNFI, 5, true);
            // I expect ArgumentException with an ambiguous NFI
            passed &= VerifyDoubleParse("^42", NumberStyles.Any, ambigNFI, -42);
            passed &= VerifyDoubleTryParse("^42", NumberStyles.Any, ambigNFI, -42, true);

            ///  END TEST CASES
        }
        catch (Exception e)
        {
            TestLibrary.Logging.WriteLine("Unexpected exception!!  " + e.ToString());
            passed = false;
        }

        if (passed)
        {
            TestLibrary.Logging.WriteLine("paSs");
            return 100;
        }
        else
        {
            TestLibrary.Logging.WriteLine("FAiL");
            return 1;
        }
    }

    public static bool VerifyDoubleTryParse(string value, Double expectedResult, bool expectedReturn)
    {
        if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: Double.TryParse, Value = '{0}', Expected Result = {1}, Expected Return = {2}",
                value, expectedResult, expectedReturn);
        }
        Double result = 0;
        try
        {
            bool returnValue = Double.TryParse(value, out result);
            if (returnValue != expectedReturn)
            {
                TestLibrary.Logging.WriteLine("FAILURE: Value = '{0}', Expected Return: {1}, Actual Return: {2}", value, expectedReturn, returnValue);
                return false;
            }
            if (Double.IsNaN(expectedResult) && Double.IsNaN(result))
            {
                return true;
            }
            if (result != expectedResult)
            {
                TestLibrary.Logging.WriteLine("FAILURE: Value = '{0}', Expected Return: {1}, Actual Return: {2}", value, expectedResult, result);
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            TestLibrary.Logging.WriteLine("FAILURE: Unexpected Exception, Value = '{0}', Exception: {1}", value, ex);
            return false;
        }
    }

    public static bool VerifyDoubleTryParse(string value, NumberStyles style, IFormatProvider provider, Double expectedResult, bool expectedReturn)
    {
		if (provider == null) return true;

		if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: Double.TryParse, Value = '{0}', Style = {1}, Provider = {2}, Expected Result = {3}, Expected Return = {4}",
                  value, style, provider, expectedResult, expectedReturn);
        }
        Double result = 0;
        try
        {
            bool returnValue = Double.TryParse(value, style, provider, out result);
            if (returnValue != expectedReturn)
            {
                TestLibrary.Logging.WriteLine("FAILURE: Value = '{0}', Style = {1}, Provider = {2}, Expected Return = {3}, Actual Return = {4}",
                  value, style, provider, expectedReturn, returnValue);
                return false;
            }
            if (Double.IsNaN(expectedResult) && Double.IsNaN(result))
            {
                return true;
            }
            if (result != expectedResult)
            {
                TestLibrary.Logging.WriteLine("FAILURE: Value = '{0}', Expected Return: {1}, Actual Return: {2}", value, expectedResult, result);
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            TestLibrary.Logging.WriteLine("FAILURE: Unexpected Exception, Value = '{0}', Exception: {1}", value, ex);
            return false;
        }
    }

    public static bool VerifyDoubleTryParseException(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
    {
		if (provider == null) return true;

		if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: Double.TryParse, Value = '{0}', Style = {1}, Provider = {2}, Expected Exception = {3}",
                value, style, provider, exceptionType);
        }
        try
        {
            Double result = 0;
            Boolean returnValue = Double.TryParse(value, style, provider, out result);
            TestLibrary.Logging.WriteLine("FAILURE: Value = '{0}', Expected Exception: {1}", value, exceptionType);
            return false;
        }
        catch (Exception ex)
        {
            if (!ex.GetType().IsAssignableFrom(exceptionType))
            {
                TestLibrary.Logging.WriteLine("FAILURE: Wrong Exception Type, Value = '{0}', Exception Type: {1} Expected Type: {2}", value, ex.GetType(), exceptionType);
                return false;
            }
            return true;
        }
    }

    public static bool VerifyDoubleParse(string value, Double expectedResult)
    {
        if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: Double.Parse, Value = '{0}', Expected Result, {1}",
                value, expectedResult);
        }
        try
        {
            Double returnValue = Double.Parse(value);
            if (Double.IsNaN(expectedResult) && Double.IsNaN(returnValue))
            {
                return true;
            }
            if (returnValue != expectedResult)
            {
                TestLibrary.Logging.WriteLine("FAILURE: Value = '{0}', Expected Return: {1}, Actual Return: {2}", value, expectedResult, returnValue);
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            TestLibrary.Logging.WriteLine("FAILURE: Unexpected Exception, Value = '{0}', Exception: {1}", value, ex);
            return false;
        }
    }

    public static bool VerifyDoubleParse(string value, NumberStyles style, IFormatProvider provider, Double expectedResult)
    {
		if (provider == null) return true;

		if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: Double.Parse, Value = '{0}', Style = {1}, provider = {2}, Expected Result = {3}",
                value, style, provider, expectedResult);
        }
        try
        {
            Double returnValue = Double.Parse(value, style, provider);
            if (Double.IsNaN(expectedResult) && Double.IsNaN(returnValue))
            {
                return true;
            }
            if (returnValue != expectedResult)
            {
                TestLibrary.Logging.WriteLine("FAILURE: Value = '{0}', Expected Return: {1}, Actual Return: {2}", value, expectedResult, returnValue);
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            TestLibrary.Logging.WriteLine("FAILURE: Unexpected Exception, Value = '{0}', Exception: {1}", value, ex);
            return false;
        }
    }

    public static bool VerifyDoubleParseException(string value, Type exceptionType)
    {
        if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: Double.Parse, Value = '{0}', Expected Exception, {1}",
                value, exceptionType);
        }
        try
        {
            Double returnValue = Double.Parse(value);
            TestLibrary.Logging.WriteLine("FAILURE: Value = '{0}', Expected Exception: {1}", value, exceptionType);
            return false;
        }
        catch (Exception ex)
        {
            if (!ex.GetType().IsAssignableFrom(exceptionType))
            {
                TestLibrary.Logging.WriteLine("FAILURE: Wrong Exception Type, Value = '{0}', Exception Type: {1} Expected Type: {2}", value, ex.GetType(), exceptionType);
                return false;
            }
            return true;
        }
    }

    public static bool VerifyDoubleParseException(string value, NumberStyles style, Type exceptionType)
    {
        if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: Double.Parse, Value = '{0}', Style = {1}, Expected Exception = {3}",
                value, style, exceptionType);
        }
        try
        {
            Double returnValue = Double.Parse(value, style);
            TestLibrary.Logging.WriteLine("FAILURE: Value = '{0}', Expected Exception: {1}", value, exceptionType);
            return false;
        }
        catch (Exception ex)
        {
            if (!ex.GetType().IsAssignableFrom(exceptionType))
            {
                TestLibrary.Logging.WriteLine("FAILURE: Wrong Exception Type, Value = '{0}', Exception Type: {1} Expected Type: {2}", value, ex.GetType(), exceptionType);
                return false;
            }
            return true;
        }
    }

    public static bool VerifyDoubleParseException(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
    {
		if (provider == null) return true;

		if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: Double.Parse, Value = '{0}', Style = {1}, Provider = {2}, Expected Exception = {3}",
                value, style, provider, exceptionType);
        }
        try
        {
            Double returnValue = Double.Parse(value, style, provider);
            TestLibrary.Logging.WriteLine("FAILURE: Value = '{0}', Expected Exception: {1}", value, exceptionType);
            return false;
        }
        catch (Exception ex)
        {
            if (!ex.GetType().IsAssignableFrom(exceptionType))
            {
                TestLibrary.Logging.WriteLine("FAILURE: Wrong Exception Type, Value = '{0}', Exception Type: {1} Expected Type: {2}", value, ex.GetType(), exceptionType);
                return false;
            }
            return true;
        }
    }
}
