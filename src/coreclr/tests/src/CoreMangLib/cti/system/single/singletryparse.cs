// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

// CoreCLR Port from Co7533TryParse_all.cs
// Tests Single.TryParse(String), Single.TryParse(String, NumberStyles, IFormatProvider, ref Single)
// 2003/04/02  KatyK
// 2007/06/28  adapted by MarielY

public class SingleTryParse
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
				TestLibrary.Logging.WriteLine("East Asian Languages are not installed. Skiping Japanese culture tests.");
				japaneseCulture = null;
			}

            // Parse tests included for comparison/regression
            passed &= VerifySingleParse("0", 0);
            passed &= VerifySingleParse("5", 5);
            passed &= VerifySingleParse("-5", -5);
            passed &= VerifySingleParse("5  ", 5);
            passed &= VerifySingleParse("5\0", 5);
            passed &= VerifySingleParse("893382737", 893382737);
            passed &= VerifySingleParse("-893382737", -893382737);
            passed &= VerifySingleParse("1234567891", 1234567891);
            passed &= VerifySingleParse("-1234567891", -1234567891);
            passed &= VerifySingleParse("123456789123456789", 123456789123456789);
            passed &= VerifySingleParse("-123456789123456789", -123456789123456789);
            passed &= VerifySingleParse("5", NumberStyles.Integer, CultureInfo.InvariantCulture, 5);
            passed &= VerifySingleParse("-5", NumberStyles.Integer, CultureInfo.InvariantCulture, -5);
            passed &= VerifySingleParse("5  \0", NumberStyles.Integer, CultureInfo.InvariantCulture, 5);
            passed &= VerifySingleParse("5\0\0\0", NumberStyles.Integer, CultureInfo.InvariantCulture, 5);
            passed &= VerifySingleParse("5", NumberStyles.Integer, goodNFI, 5);
            passed &= VerifySingleParse("5.0", NumberStyles.AllowDecimalPoint, goodNFI, 5);
            passed &= VerifySingleParse("5.3", NumberStyles.AllowDecimalPoint, goodNFI, 5.3f);
            passed &= VerifySingleParse("123456789123456789123", 123456789123456789123.0f);
            passed &= VerifySingleParse("-123456789123456789123", -123456789123456789123.0f);
            passed &= VerifySingleParse("18446744073709551615", 18446744073709551615.0f);
            passed &= VerifySingleParse("79228162514264337593543950335", 79228162514264337593543950335.0f);
            passed &= VerifySingleParse("-79228162514264337593543950335", -79228162514264337593543950335.0f);
            passed &= VerifySingleParse("5.555555555", 5.555555555f);
            passed &= VerifySingleParse("1.000000", 1.0f);
            passed &= VerifySingleParse("79228162514264337593543950336", 79228162514264337593543950336.0f);
            passed &= VerifySingleParse("-79228162514264337593543950336", -79228162514264337593543950336.0f);
            passed &= VerifySingleParse("3.40282347E+38", NumberStyles.Float | NumberStyles.AllowExponent, null, (float)3.40282347E+38);
            passed &= VerifySingleParse("-3.40282347E+38 ", NumberStyles.Float | NumberStyles.AllowExponent, null, (float)-3.40282347E+38);
            passed &= VerifySingleParse("3.402822E+38", NumberStyles.Float | NumberStyles.AllowExponent, null, 3.402822E+38f);
            passed &= VerifySingleParse("-3.402822E+38", NumberStyles.Float | NumberStyles.AllowExponent, null, -3.402822E+38f);
            passed &= VerifySingleParse("NaN", NumberStyles.Float, NumberFormatInfo.InvariantInfo, Single.NaN);
            passed &= VerifySingleParse("Infinity", NumberStyles.Float, NumberFormatInfo.InvariantInfo, Single.PositiveInfinity);
            passed &= VerifySingleParse("-Infinity", NumberStyles.Float, NumberFormatInfo.InvariantInfo, Single.NegativeInfinity);
            passed &= VerifySingleParse("123", NumberStyles.Integer, germanCulture, 123);
            passed &= VerifySingleParse("123", NumberStyles.Integer, japaneseCulture, 123);
            passed &= VerifySingleParse("123.456", NumberStyles.Any, germanCulture, 123456);
            passed &= VerifySingleParse("123,456", NumberStyles.Any, japaneseCulture, 123456);
            passed &= VerifySingleParse("123,456", NumberStyles.AllowDecimalPoint, germanCulture, 123.456f);
            passed &= VerifySingleParse("123.456", NumberStyles.AllowDecimalPoint, japaneseCulture, 123.456f);
            passed &= VerifySingleParse("5,23 " + germanCulture.NumberFormat.CurrencySymbol, NumberStyles.Any, germanCulture, 5.23f); // currency
            passed &= VerifySingleParse("5.23 " + germanCulture.NumberFormat.CurrencySymbol, NumberStyles.Any, germanCulture, 523); // currency
            //
            passed &= VerifySingleParse("5", NumberStyles.Integer, corruptNFI, 5f);
            passed &= VerifySingleParse("5", NumberStyles.Number, corruptNFI, 5f);
            passed &= VerifySingleParse("5.3", NumberStyles.Number, corruptNFI, 5.3f);
            passed &= VerifySingleParseException("5,3", NumberStyles.Number, corruptNFI, typeof(FormatException));
            passed &= VerifySingleParseException("5.2.3", NumberStyles.Number, corruptNFI, typeof(FormatException));
            passed &= VerifySingleParse("$5.3", NumberStyles.Currency, corruptNFI, 5.3f);
            passed &= VerifySingleParseException("$5,3", NumberStyles.Currency, corruptNFI, typeof(FormatException));
            passed &= VerifySingleParseException("$5.2.3", NumberStyles.Currency, corruptNFI, typeof(FormatException));
            passed &= VerifySingleParse("5.3", NumberStyles.Currency, corruptNFI, 5.3f);
            passed &= VerifySingleParseException("5,3", NumberStyles.Currency, corruptNFI, typeof(FormatException));
            passed &= VerifySingleParseException("5.2.3", NumberStyles.Currency, corruptNFI, typeof(FormatException));
            passed &= VerifySingleParse("5.3", NumberStyles.Any, corruptNFI, 5.3f);
            passed &= VerifySingleParseException("5,3", NumberStyles.Any, corruptNFI, typeof(FormatException));
            passed &= VerifySingleParseException("5.2.3", NumberStyles.Any, corruptNFI, typeof(FormatException));
            //
            passed &= VerifySingleParse("5", NumberStyles.Integer, swappedNFI, 5);
            passed &= VerifySingleParseException("1,234", NumberStyles.Integer, swappedNFI, typeof(FormatException));
            passed &= VerifySingleParse("5", NumberStyles.Number, swappedNFI, 5);
            passed &= VerifySingleParse("5.0", NumberStyles.Number, swappedNFI, 5);
            passed &= VerifySingleParse("1,234", NumberStyles.Number, swappedNFI, 1234);
            passed &= VerifySingleParse("1,234.0", NumberStyles.Number, swappedNFI, 1234);
            passed &= VerifySingleParseException("5.000.000", NumberStyles.Number, swappedNFI, typeof(FormatException));
            passed &= VerifySingleParseException("5.000,00", NumberStyles.Number, swappedNFI, typeof(FormatException));
            passed &= VerifySingleParse("5.000", NumberStyles.Currency, swappedNFI, 5); //???
            passed &= VerifySingleParseException("5.000,00", NumberStyles.Currency, swappedNFI, typeof(FormatException)); //???
            passed &= VerifySingleParse("$5.000", NumberStyles.Currency, swappedNFI, 5000);
            passed &= VerifySingleParse("$5.000,00", NumberStyles.Currency, swappedNFI, 5000);
            passed &= VerifySingleParse("5.000", NumberStyles.Any, swappedNFI, 5);  //?
            passed &= VerifySingleParseException("5.000,00", NumberStyles.Any, swappedNFI, typeof(FormatException));  //?
            passed &= VerifySingleParse("$5.000", NumberStyles.Any, swappedNFI, 5000);
            passed &= VerifySingleParse("$5.000,00", NumberStyles.Any, swappedNFI, 5000);
            passed &= VerifySingleParse("5,0", NumberStyles.Currency, swappedNFI, 5);
            passed &= VerifySingleParse("$5,0", NumberStyles.Currency, swappedNFI, 5);
            passed &= VerifySingleParse("5,000", NumberStyles.Currency, swappedNFI, 5);
            passed &= VerifySingleParse("$5,000", NumberStyles.Currency, swappedNFI, 5);
            passed &= VerifySingleParseException("5,000.0", NumberStyles.Currency, swappedNFI, typeof(FormatException));
            passed &= VerifySingleParseException("$5,000.0", NumberStyles.Currency, swappedNFI, typeof(FormatException));
            passed &= VerifySingleParse("5,000", NumberStyles.Any, swappedNFI, 5);
            passed &= VerifySingleParse("$5,000", NumberStyles.Any, swappedNFI, 5);
            passed &= VerifySingleParseException("5,000.0", NumberStyles.Any, swappedNFI, typeof(FormatException));
            passed &= VerifySingleParseException("$5,000.0", NumberStyles.Any, swappedNFI, typeof(FormatException));
            //
            passed &= VerifySingleParse("5.0", NumberStyles.Number, distinctNFI, 5);
            passed &= VerifySingleParse("1,234.0", NumberStyles.Number, distinctNFI, 1234);
            passed &= VerifySingleParse("5.0", NumberStyles.Currency, distinctNFI, 5);
            passed &= VerifySingleParse("1,234.0", NumberStyles.Currency, distinctNFI, 1234);
            passed &= VerifySingleParse("5.0", NumberStyles.Any, distinctNFI, 5);
            passed &= VerifySingleParse("1,234.0", NumberStyles.Any, distinctNFI, 1234);
            passed &= VerifySingleParseException("$5.0", NumberStyles.Currency, distinctNFI, typeof(FormatException));
            passed &= VerifySingleParseException("$5.0", NumberStyles.Any, distinctNFI, typeof(FormatException));
            passed &= VerifySingleParseException("5:0", NumberStyles.Number, distinctNFI, typeof(FormatException));
            passed &= VerifySingleParseException("5;0", NumberStyles.Number, distinctNFI, typeof(FormatException));
            passed &= VerifySingleParseException("$5:0", NumberStyles.Number, distinctNFI, typeof(FormatException));
            passed &= VerifySingleParse("5:0", NumberStyles.Currency, distinctNFI, 5);
            passed &= VerifySingleParse("5:000", NumberStyles.Currency, distinctNFI, 5);
            passed &= VerifySingleParse("5;000", NumberStyles.Currency, distinctNFI, 5000);
            passed &= VerifySingleParse("$5:0", NumberStyles.Currency, distinctNFI, 5);
            passed &= VerifySingleParse("$5;0", NumberStyles.Currency, distinctNFI, 50);
            passed &= VerifySingleParse("5:0", NumberStyles.Any, distinctNFI, 5);
            passed &= VerifySingleParse("5;0", NumberStyles.Any, distinctNFI, 50);
            passed &= VerifySingleParse("$5:0", NumberStyles.Any, distinctNFI, 5);
            passed &= VerifySingleParse("$5;0", NumberStyles.Any, distinctNFI, 50);
            passed &= VerifySingleParseException("123,456;789.0", NumberStyles.Number, distinctNFI, typeof(FormatException));
            passed &= VerifySingleParse("123,456;789.0", NumberStyles.Currency, distinctNFI, 123456789);
            passed &= VerifySingleParse("123,456;789.0", NumberStyles.Any, distinctNFI, 123456789);
            passed &= VerifySingleParseException("$123,456;789.0", NumberStyles.Any, distinctNFI, typeof(FormatException));
            //
            passed &= VerifySingleParseException("3.402822E+39", NumberStyles.Float | NumberStyles.AllowExponent, null, typeof(OverflowException));
            passed &= VerifySingleParseException("-3.402822E+39", NumberStyles.Float | NumberStyles.AllowExponent, null, typeof(OverflowException));
            passed &= VerifySingleParseException("1.89769313486231E+308", NumberStyles.Float | NumberStyles.AllowExponent, null, typeof(OverflowException));
            passed &= VerifySingleParseException("-1.89769313486231E+308", NumberStyles.Float | NumberStyles.AllowExponent, null, typeof(OverflowException));
            passed &= VerifySingleParseException("Garbage", typeof(FormatException));
            passed &= VerifySingleParseException("5\0Garbage", typeof(FormatException));
            passed &= VerifySingleParseException(null, typeof(ArgumentNullException));
            passed &= VerifySingleParseException("FF", NumberStyles.HexNumber, goodNFI, typeof(ArgumentException));
            passed &= VerifySingleParseException("4", (NumberStyles)(-1), typeof(ArgumentException));
            passed &= VerifySingleParseException("4", (NumberStyles)0x10000, typeof(ArgumentException));
            passed &= VerifySingleParseException("4", (NumberStyles)(-1), CultureInfo.InvariantCulture, typeof(ArgumentException));
            passed &= VerifySingleParseException("4", (NumberStyles)0x10000, CultureInfo.InvariantCulture, typeof(ArgumentException));
            passed &= VerifySingleParseException("123,000,000,000,000,000,000", NumberStyles.Any, germanCulture, typeof(FormatException));
            passed &= VerifySingleParseException("123.000.000.000.000.000.000", NumberStyles.Any, japaneseCulture, typeof(FormatException));
            passed &= VerifySingleParseException("5,00 " + germanCulture.NumberFormat.CurrencySymbol, NumberStyles.Integer, germanCulture, typeof(FormatException)); // currency


            /////////// TryParse(String)
            //// Pass cases
            passed &= VerifySingleTryParse("0", 0, true);
            passed &= VerifySingleTryParse("-0", 0, true);
            passed &= VerifySingleTryParse("5", 5, true);
            passed &= VerifySingleTryParse(" 5 ", 5, true);
            passed &= VerifySingleTryParse("-5", -5, true);
            passed &= VerifySingleTryParse("5\0", 5, true);
            passed &= VerifySingleTryParse("5  \0", 5, true);
            passed &= VerifySingleTryParse("5\0\0\0", 5, true);
            passed &= VerifySingleTryParse("893382737", 893382737, true);
            passed &= VerifySingleTryParse("-893382737", -893382737, true);
            passed &= VerifySingleTryParse("1234567891", 1234567891, true);
            passed &= VerifySingleTryParse("-1234567891", -1234567891, true);
            passed &= VerifySingleTryParse("123456789123456789", 123456789123456789, true);
            passed &= VerifySingleTryParse("-123456789123456789", -123456789123456789, true);
            passed &= VerifySingleTryParse("123456789123456789123", 123456789123456789123.0f, true);
            passed &= VerifySingleTryParse("-123456789123456789123", -123456789123456789123.0f, true);
            passed &= VerifySingleTryParse("18446744073709551615", 18446744073709551615, true);
            passed &= VerifySingleTryParse("79228162514264337593543950335", 79228162514264337593543950335.0f, true);
            passed &= VerifySingleTryParse("-79228162514264337593543950335", -79228162514264337593543950335.0f, true);
            passed &= VerifySingleTryParse("79228162514264337593543950336", 79228162514264337593543950336.0f, true);
            passed &= VerifySingleTryParse("-79228162514264337593543950336", -79228162514264337593543950336.0f, true);
            passed &= VerifySingleTryParse("7.3", 7.3f, true);
            passed &= VerifySingleTryParse(".297", 0.297f, true);
            passed &= VerifySingleTryParse("5.555555555", 5.555555555f, true);
            passed &= VerifySingleTryParse("1.000000", 1.0f, true);
            passed &= VerifySingleTryParse("1.234E+05", 123400, true);
            passed &= VerifySingleTryParse("NaN", Single.NaN, true);
            passed &= VerifySingleTryParse("Infinity", Single.PositiveInfinity, true);
            passed &= VerifySingleTryParse("-Infinity", Single.NegativeInfinity, true);

            //// Fail cases
            passed &= VerifySingleTryParse(null, 0, false);
            passed &= VerifySingleTryParse("", 0, false);
            passed &= VerifySingleTryParse("Garbage", 0, false);
            passed &= VerifySingleTryParse("5\0Garbage", 0, false);
            passed &= VerifySingleTryParse("FF", 0, false);
            passed &= VerifySingleTryParse("23 5", 0, false);
            passed &= VerifySingleTryParse("1.234+E05", 0, false);


            /////////// TryParse(TryParse(String, NumberStyles, IFormatProvider, ref Single)
            //// Pass cases
            passed &= VerifySingleTryParse("5", NumberStyles.Integer, CultureInfo.InvariantCulture, 5, true);
            passed &= VerifySingleTryParse("-5", NumberStyles.Integer, CultureInfo.InvariantCulture, -5, true);
            passed &= VerifySingleTryParse("-79228162514264337593543950336", NumberStyles.Integer, CultureInfo.InvariantCulture, -79228162514264337593543950336.0f, true);
            passed &= VerifySingleTryParse("NaN", NumberStyles.Float, NumberFormatInfo.InvariantInfo, Single.NaN, true);
            passed &= VerifySingleTryParse("Infinity", NumberStyles.Float, NumberFormatInfo.InvariantInfo, Single.PositiveInfinity, true);
            passed &= VerifySingleTryParse("-Infinity", NumberStyles.Float, NumberFormatInfo.InvariantInfo, Single.NegativeInfinity, true);
            // Variations on NumberStyles
            passed &= VerifySingleTryParse("   5", NumberStyles.AllowLeadingWhite, goodNFI, 5, true);
            passed &= VerifySingleTryParse("5", NumberStyles.Number, goodNFI, 5, true);
            passed &= VerifySingleTryParse("5.0", NumberStyles.AllowDecimalPoint, goodNFI, 5.0f, true);
            passed &= VerifySingleTryParse("5.3", NumberStyles.AllowDecimalPoint, goodNFI, 5.3f, true);
            passed &= VerifySingleTryParse("1.234E+05", NumberStyles.Float | NumberStyles.AllowExponent, goodNFI, 123400, true);
            passed &= VerifySingleTryParse("3.40282347E+38", NumberStyles.Float | NumberStyles.AllowExponent, null, (float)3.40282347E+38, true);
            passed &= VerifySingleTryParse("-3.40282347E+38 ", NumberStyles.Float | NumberStyles.AllowExponent, null, (float)-3.40282347E+38, true);
            // Variations on IFP
            passed &= VerifySingleTryParse("5", NumberStyles.Integer, goodNFI, 5, true);
            passed &= VerifySingleTryParse("5", NumberStyles.Integer, null, 5, true);
            passed &= VerifySingleTryParse("5", NumberStyles.Integer, new DateTimeFormatInfo(), 5, true);
            passed &= VerifySingleTryParse("^42", NumberStyles.Any, customNFI, -42, true);
            passed &= VerifySingleTryParse("123", NumberStyles.Integer, germanCulture, 123, true);
            passed &= VerifySingleTryParse("123", NumberStyles.Integer, japaneseCulture, 123, true);
            passed &= VerifySingleTryParse("123.456", NumberStyles.Any, germanCulture, 123456, true);
            passed &= VerifySingleTryParse("123,456", NumberStyles.Any, japaneseCulture, 123456, true);
            passed &= VerifySingleTryParse("123,456", NumberStyles.AllowDecimalPoint, germanCulture, 123.456f, true);
            passed &= VerifySingleTryParse("123.456", NumberStyles.AllowDecimalPoint, japaneseCulture, 123.456f, true);
            passed &= VerifySingleTryParse("5,23 " + germanCulture.NumberFormat.CurrencySymbol, NumberStyles.Any, germanCulture, 5.23f, true); // currency
            passed &= VerifySingleTryParse("5.23 " + germanCulture.NumberFormat.CurrencySymbol, NumberStyles.Any, germanCulture, 523, true); // currency
            //
            //// Fail cases
            passed &= VerifySingleTryParse("-42", NumberStyles.Any, customNFI, 0, false);
            passed &= VerifySingleTryParse("5   ", NumberStyles.AllowLeadingWhite, goodNFI, 0, false);
            passed &= VerifySingleTryParse("5.3", NumberStyles.None, goodNFI, 0, false);
            passed &= VerifySingleTryParse("1.234E+05", NumberStyles.AllowExponent, goodNFI, 0, false);
            passed &= VerifySingleTryParse("3.40282347E+39", NumberStyles.Float | NumberStyles.AllowExponent, null, 0, false);
            passed &= VerifySingleTryParse("-3.40282347E+39", NumberStyles.Float | NumberStyles.AllowExponent, null, 0, false);
            passed &= VerifySingleTryParse("1.79769313486231E+308", NumberStyles.Float | NumberStyles.AllowExponent, null, 0, false);
            passed &= VerifySingleTryParse("-1.79769313486231E+308", NumberStyles.Float | NumberStyles.AllowExponent, null, 0, false);
            passed &= VerifySingleTryParse("-1.79769313486231E+309", NumberStyles.Float | NumberStyles.AllowExponent, null, 0, false);
            passed &= VerifySingleTryParse("1.89769313486231E+308", NumberStyles.Float | NumberStyles.AllowExponent, null, 0, false);
            passed &= VerifySingleTryParse("-1.89769313486231E+308", NumberStyles.Float | NumberStyles.AllowExponent, null, 0, false);
            passed &= VerifySingleTryParse("123,000,000,000,000,000,000", NumberStyles.Any, germanCulture, 0, false);
            passed &= VerifySingleTryParse("123.000.000.000.000.000.000", NumberStyles.Any, japaneseCulture, 0, false);

            //// Exception cases
            passed &= VerifySingleTryParseException("12", NumberStyles.HexNumber, CultureInfo.InvariantCulture, typeof(ArgumentException));
            passed &= VerifySingleTryParseException("5", NumberStyles.AllowHexSpecifier | NumberStyles.AllowParentheses, null, typeof(ArgumentException));
            passed &= VerifySingleTryParseException("4", (NumberStyles)(-1), CultureInfo.InvariantCulture, typeof(ArgumentException));
            passed &= VerifySingleTryParseException("4", (NumberStyles)0x10000, CultureInfo.InvariantCulture, typeof(ArgumentException));

            // NumberStyles/NFI variations
            //
            passed &= VerifySingleTryParse("5", NumberStyles.Integer, corruptNFI, 5, true);
            passed &= VerifySingleTryParse("5", NumberStyles.Number, corruptNFI, 5, true);
            passed &= VerifySingleTryParse("5.3", NumberStyles.Number, corruptNFI, 5.3f, true);
            passed &= VerifySingleTryParse("5,3", NumberStyles.Number, corruptNFI, 0, false);
            passed &= VerifySingleTryParse("5.2.3", NumberStyles.Number, corruptNFI, 0, false);
            passed &= VerifySingleTryParse("$5.3", NumberStyles.Currency, corruptNFI, 5.3f, true);
            passed &= VerifySingleTryParse("$5,3", NumberStyles.Currency, corruptNFI, 0, false);
            passed &= VerifySingleTryParse("$5.2.3", NumberStyles.Currency, corruptNFI, 0, false);
            passed &= VerifySingleTryParse("5.3", NumberStyles.Currency, corruptNFI, 5.3f, true);
            passed &= VerifySingleTryParse("5,3", NumberStyles.Currency, corruptNFI, 0, false);
            passed &= VerifySingleTryParse("5.2.3", NumberStyles.Currency, corruptNFI, 0, false);
            passed &= VerifySingleTryParse("5.3", NumberStyles.Any, corruptNFI, 5.3f, true);
            passed &= VerifySingleTryParse("5,3", NumberStyles.Any, corruptNFI, 0, false);
            passed &= VerifySingleTryParse("5.2.3", NumberStyles.Any, corruptNFI, 0, false);
            //
            passed &= VerifySingleTryParse("5", NumberStyles.Integer, swappedNFI, 5, true);
            passed &= VerifySingleTryParse("1,234", NumberStyles.Integer, swappedNFI, 0, false);
            passed &= VerifySingleTryParse("5", NumberStyles.Number, swappedNFI, 5, true);
            passed &= VerifySingleTryParse("5.0", NumberStyles.Number, swappedNFI, 5, true);
            passed &= VerifySingleTryParse("1,234", NumberStyles.Number, swappedNFI, 1234, true);
            passed &= VerifySingleTryParse("1,234.0", NumberStyles.Number, swappedNFI, 1234, true);
            passed &= VerifySingleTryParse("5.000.000", NumberStyles.Number, swappedNFI, 0, false);
            passed &= VerifySingleTryParse("5.000,00", NumberStyles.Number, swappedNFI, 0, false);
            passed &= VerifySingleTryParse("5.000", NumberStyles.Currency, swappedNFI, 5, true); //???
            passed &= VerifySingleTryParse("5.000,00", NumberStyles.Currency, swappedNFI, 0, false); //???
            passed &= VerifySingleTryParse("$5.000", NumberStyles.Currency, swappedNFI, 5000, true);
            passed &= VerifySingleTryParse("$5.000,00", NumberStyles.Currency, swappedNFI, 5000, true);
            passed &= VerifySingleTryParse("5.000", NumberStyles.Any, swappedNFI, 5, true);  //?
            passed &= VerifySingleTryParse("5.000,00", NumberStyles.Any, swappedNFI, 0, false);  //?
            passed &= VerifySingleTryParse("$5.000", NumberStyles.Any, swappedNFI, 5000, true);
            passed &= VerifySingleTryParse("$5.000,00", NumberStyles.Any, swappedNFI, 5000, true);
            passed &= VerifySingleTryParse("5,0", NumberStyles.Currency, swappedNFI, 5, true);
            passed &= VerifySingleTryParse("$5,0", NumberStyles.Currency, swappedNFI, 5, true);
            passed &= VerifySingleTryParse("5,000", NumberStyles.Currency, swappedNFI, 5, true);
            passed &= VerifySingleTryParse("$5,000", NumberStyles.Currency, swappedNFI, 5, true);
            passed &= VerifySingleTryParse("5,000.0", NumberStyles.Currency, swappedNFI, 0, false);
            passed &= VerifySingleTryParse("$5,000.0", NumberStyles.Currency, swappedNFI, 0, false);
            passed &= VerifySingleTryParse("5,000", NumberStyles.Any, swappedNFI, 5, true);
            passed &= VerifySingleTryParse("$5,000", NumberStyles.Any, swappedNFI, 5, true);
            passed &= VerifySingleTryParse("5,000.0", NumberStyles.Any, swappedNFI, 0, false);
            passed &= VerifySingleTryParse("$5,000.0", NumberStyles.Any, swappedNFI, 0, false);
            //
            passed &= VerifySingleTryParse("5.0", NumberStyles.Number, distinctNFI, 5, true);
            passed &= VerifySingleTryParse("1,234.0", NumberStyles.Number, distinctNFI, 1234, true);
            passed &= VerifySingleTryParse("5.0", NumberStyles.Currency, distinctNFI, 5, true);
            passed &= VerifySingleTryParse("1,234.0", NumberStyles.Currency, distinctNFI, 1234, true);
            passed &= VerifySingleTryParse("5.0", NumberStyles.Any, distinctNFI, 5, true);
            passed &= VerifySingleTryParse("1,234.0", NumberStyles.Any, distinctNFI, 1234, true);
            passed &= VerifySingleTryParse("$5.0", NumberStyles.Currency, distinctNFI, 0, false);
            passed &= VerifySingleTryParse("$5.0", NumberStyles.Any, distinctNFI, 0, false);
            passed &= VerifySingleTryParse("5:0", NumberStyles.Number, distinctNFI, 0, false);
            passed &= VerifySingleTryParse("5;0", NumberStyles.Number, distinctNFI, 0, false);
            passed &= VerifySingleTryParse("$5:0", NumberStyles.Number, distinctNFI, 0, false);
            passed &= VerifySingleTryParse("5:0", NumberStyles.Currency, distinctNFI, 5, true);
            passed &= VerifySingleTryParse("5:000", NumberStyles.Currency, distinctNFI, 5, true);
            passed &= VerifySingleTryParse("5;000", NumberStyles.Currency, distinctNFI, 5000, true);
            passed &= VerifySingleTryParse("$5:0", NumberStyles.Currency, distinctNFI, 5, true);
            passed &= VerifySingleTryParse("$5;0", NumberStyles.Currency, distinctNFI, 50, true);
            passed &= VerifySingleTryParse("5:0", NumberStyles.Any, distinctNFI, 5, true);
            passed &= VerifySingleTryParse("5;0", NumberStyles.Any, distinctNFI, 50, true);
            passed &= VerifySingleTryParse("$5:0", NumberStyles.Any, distinctNFI, 5, true);
            passed &= VerifySingleTryParse("$5;0", NumberStyles.Any, distinctNFI, 50, true);
            passed &= VerifySingleTryParse("123,456;789.0", NumberStyles.Number, distinctNFI, 0, false);
            passed &= VerifySingleTryParse("123,456;789.0", NumberStyles.Currency, distinctNFI, 123456789, true);
            passed &= VerifySingleTryParse("123,456;789.0", NumberStyles.Any, distinctNFI, 123456789, true);
            passed &= VerifySingleTryParse("$123,456;789.0", NumberStyles.Any, distinctNFI, 0, false);


            // Should these pass or fail?  Current parse behavior is to pass, so they might be
            // parse bugs, but they're not tryparse bugs.
            // I expect ArgumentException with an ambiguous NFI
            passed &= VerifySingleParse("^42", NumberStyles.Any, ambigNFI, -42);
            passed &= VerifySingleTryParse("^42", NumberStyles.Any, ambigNFI, -42, true);

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

    public static bool VerifySingleTryParse(string value, Single expectedResult, bool expectedReturn)
    {
        if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: Single.TryParse, Value = '{0}', Expected Result = {1}, Expected Return = {2}",
                value, expectedResult, expectedReturn);
        }
        Single result = 0;
        try
        {
            bool returnValue = Single.TryParse(value, out result);
            if (returnValue != expectedReturn)
            {
                TestLibrary.Logging.WriteLine("FAILURE: Value = '{0}', Expected Return: {1}, Actual Return: {2}", value, expectedReturn, returnValue);
                return false;
            }
            if (Single.IsNaN(expectedResult) && Single.IsNaN(result))
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

    public static bool VerifySingleTryParse(string value, NumberStyles style, IFormatProvider provider, Single expectedResult, bool expectedReturn)
    {
		if (provider == null) return true;

		if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: Single.TryParse, Value = '{0}', Style = {1}, Provider = {2}, Expected Result = {3}, Expected Return = {4}",
                  value, style, provider, expectedResult, expectedReturn);
        }
        Single result = 0;
        try
        {
            bool returnValue = Single.TryParse(value, style, provider, out result);
            if (returnValue != expectedReturn)
            {
                TestLibrary.Logging.WriteLine("FAILURE: Value = '{0}', Style = {1}, Provider = {2}, Expected Return = {3}, Actual Return = {4}",
                  value, style, provider, expectedReturn, returnValue);
                return false;
            }
            if (Single.IsNaN(expectedResult) && Single.IsNaN(result))
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

    public static bool VerifySingleTryParseException(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
    {
		if (provider == null) return true;

		if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: Single.TryParse, Value = '{0}', Style = {1}, Provider = {2}, Expected Exception = {3}",
                value, style, provider, exceptionType);
        }
        try
        {
            Single result = 0;
            Boolean returnValue = Single.TryParse(value, style, provider, out result);
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

    public static bool VerifySingleParse(string value, Single expectedResult)
    {
        if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: Single.Parse, Value = '{0}', Expected Result, {1}",
                value, expectedResult);
        }
        try
        {
            Single returnValue = Single.Parse(value);
            if (Single.IsNaN(expectedResult) && Single.IsNaN(returnValue))
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

    public static bool VerifySingleParse(string value, NumberStyles style, IFormatProvider provider, Single expectedResult)
    {
		if (provider == null) return true;

		if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: Single.Parse, Value = '{0}', Style = {1}, provider = {2}, Expected Result = {3}",
                value, style, provider, expectedResult);
        }
        try
        {
            Single returnValue = Single.Parse(value, style, provider);
            if (Single.IsNaN(expectedResult) && Single.IsNaN(returnValue))
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

    public static bool VerifySingleParseException(string value, Type exceptionType)
    {
        if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: Single.Parse, Value = '{0}', Expected Exception, {1}",
                value, exceptionType);
        }
        try
        {
            Single returnValue = Single.Parse(value);
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

    public static bool VerifySingleParseException(string value, NumberStyles style, Type exceptionType)
    {
        if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: Single.Parse, Value = '{0}', Style = {1}, Expected Exception = {3}",
                value, style, exceptionType);
        }
        try
        {
            Single returnValue = Single.Parse(value, style);
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

    public static bool VerifySingleParseException(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
    {
		if (provider == null) return true;

		if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: Single.Parse, Value = '{0}', Style = {1}, Provider = {2}, Expected Exception = {3}",
                value, style, provider, exceptionType);
        }
        try
        {
            Single returnValue = Single.Parse(value, style, provider);
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
