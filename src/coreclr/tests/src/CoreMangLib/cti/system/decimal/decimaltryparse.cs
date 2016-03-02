// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

// Ported to CoreCLR from Co7531TryParse_all.cs
// Tests Decimal.TryParse(String), Decimal.TryParse(String, NumberStyles, IFormatProvider, ref Decimal)
// 2003/04/01  KatyK
// 2007/07/12  adapted by MarielY
public class Co7531TryParse
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

            CultureInfo invariantCulture = CultureInfo.InvariantCulture;
            CultureInfo germanCulture = new CultureInfo("de-DE");
            CultureInfo japaneseCulture;
			try {
				japaneseCulture = new CultureInfo("ja-JP");
			}
			catch (CultureNotFoundException)
			{ 
				TestLibrary.TestFramework.LogInformation("East Asian Languages are not installed. Skipping Japanese culture test(s)."); 
				japaneseCulture = null;
			} 

            // Parse tests included for comparison/regression
            passed &= VerifyDecimalParse("5", 5);
            passed &= VerifyDecimalParse("5  ", 5);
            passed &= VerifyDecimalParse("5\0", 5);
            passed &= VerifyDecimalParse("-5", -5);
            passed &= VerifyDecimalParse("893382737", 893382737);
            passed &= VerifyDecimalParse("-893382737", -893382737);
            passed &= VerifyDecimalParse("1234567891", 1234567891);
            passed &= VerifyDecimalParse("-1234567891", -1234567891);
            passed &= VerifyDecimalParse("123456789123456789", 123456789123456789);
            passed &= VerifyDecimalParse("-123456789123456789", -123456789123456789);
            passed &= VerifyDecimalParse("5", NumberStyles.Integer, CultureInfo.InvariantCulture, 5);
            passed &= VerifyDecimalParse("5  \0", NumberStyles.Integer, CultureInfo.InvariantCulture, 5);
            passed &= VerifyDecimalParse("5\0\0\0", NumberStyles.Integer, CultureInfo.InvariantCulture, 5);
            passed &= VerifyDecimalParse("-5", NumberStyles.Integer, CultureInfo.InvariantCulture, -5);
            passed &= VerifyDecimalParse("5", NumberStyles.Integer, goodNFI, 5);
            passed &= VerifyDecimalParse("5.0", NumberStyles.AllowDecimalPoint, goodNFI, 5.0m);
            passed &= VerifyDecimalParse("5.3", NumberStyles.AllowDecimalPoint, goodNFI, 5.3m);
            passed &= VerifyDecimalParse("123456789123456789123", 123456789123456789123m);
            passed &= VerifyDecimalParse("-123456789123456789123", -123456789123456789123m);
            passed &= VerifyDecimalParse("18446744073709551615", 18446744073709551615);
            passed &= VerifyDecimalParse("79228162514264337593543950335", 79228162514264337593543950335m);
            passed &= VerifyDecimalParse("-79228162514264337593543950335", -79228162514264337593543950335m);
            passed &= VerifyDecimalParse("5.555555555", 5.555555555m);
            passed &= VerifyDecimalParse("1.000000", 1.000000m);
            passed &= VerifyDecimalParse("123", NumberStyles.Integer, germanCulture, 123);
            passed &= VerifyDecimalParse("123", NumberStyles.Integer, japaneseCulture, 123);
            passed &= VerifyDecimalParse("123.456", NumberStyles.Any, germanCulture, 123456);
            passed &= VerifyDecimalParse("123,456", NumberStyles.Any, japaneseCulture, 123456);
            passed &= VerifyDecimalParse("123,456", NumberStyles.AllowDecimalPoint, germanCulture, 123.456m);
            passed &= VerifyDecimalParse("123.456", NumberStyles.AllowDecimalPoint, japaneseCulture, 123.456m);
            passed &= VerifyDecimalParse("5,23 " + germanCulture.NumberFormat.CurrencySymbol, NumberStyles.Any, germanCulture, 5.23m); // currency
            passed &= VerifyDecimalParse("5.23 " + germanCulture.NumberFormat.CurrencySymbol, NumberStyles.Any, germanCulture, 523); // currency
            //
            passed &= VerifyDecimalParse("5", NumberStyles.Integer, corruptNFI, 5);
            passed &= VerifyDecimalParse("5", NumberStyles.Number, corruptNFI, 5);
            passed &= VerifyDecimalParse("5.3", NumberStyles.Number, corruptNFI, 5.3m);
            passed &= VerifyDecimalParseException("5,3", NumberStyles.Number, corruptNFI, typeof(FormatException));
            passed &= VerifyDecimalParseException("5.2.3", NumberStyles.Number, corruptNFI, typeof(FormatException));
            passed &= VerifyDecimalParse("$5.3", NumberStyles.Currency, corruptNFI, 5.3m);
            passed &= VerifyDecimalParseException("$5,3", NumberStyles.Currency, corruptNFI, typeof(FormatException));
            passed &= VerifyDecimalParseException("$5.2.3", NumberStyles.Currency, corruptNFI, typeof(FormatException));
            passed &= VerifyDecimalParse("5.3", NumberStyles.Currency, corruptNFI, 5.3m);
            passed &= VerifyDecimalParseException("5,3", NumberStyles.Currency, corruptNFI, typeof(FormatException));
            passed &= VerifyDecimalParseException("5.2.3", NumberStyles.Currency, corruptNFI, typeof(FormatException));
            passed &= VerifyDecimalParse("5.3", NumberStyles.Any, corruptNFI, 5.3m);
            passed &= VerifyDecimalParseException("5,3", NumberStyles.Any, corruptNFI, typeof(FormatException));
            passed &= VerifyDecimalParseException("5.2.3", NumberStyles.Any, corruptNFI, typeof(FormatException));
            //
            passed &= VerifyDecimalParse("5", NumberStyles.Integer, swappedNFI, 5);
            passed &= VerifyDecimalParseException("1,234", NumberStyles.Integer, swappedNFI, typeof(FormatException));
            passed &= VerifyDecimalParse("5", NumberStyles.Number, swappedNFI, 5);
            passed &= VerifyDecimalParse("5.0", NumberStyles.Number, swappedNFI, 5.0m);
            passed &= VerifyDecimalParse("1,234", NumberStyles.Number, swappedNFI, 1234);
            passed &= VerifyDecimalParse("1,234.0", NumberStyles.Number, swappedNFI, 1234.0m);
            passed &= VerifyDecimalParseException("5.000.000", NumberStyles.Number, swappedNFI, typeof(FormatException));
            passed &= VerifyDecimalParseException("5.000,00", NumberStyles.Number, swappedNFI, typeof(FormatException));
            passed &= VerifyDecimalParse("5.000", NumberStyles.Currency, swappedNFI, 5); //???
            passed &= VerifyDecimalParseException("5.000,00", NumberStyles.Currency, swappedNFI, typeof(FormatException)); //???
            passed &= VerifyDecimalParse("$5.000", NumberStyles.Currency, swappedNFI, 5000);
            passed &= VerifyDecimalParse("$5.000,00", NumberStyles.Currency, swappedNFI, 5000);
            passed &= VerifyDecimalParse("5.000", NumberStyles.Any, swappedNFI, 5);  //?
            passed &= VerifyDecimalParseException("5.000,00", NumberStyles.Any, swappedNFI, typeof(FormatException));  //?
            passed &= VerifyDecimalParse("$5.000", NumberStyles.Any, swappedNFI, 5000);
            passed &= VerifyDecimalParse("$5.000,00", NumberStyles.Any, swappedNFI, 5000);
            passed &= VerifyDecimalParse("5,0", NumberStyles.Currency, swappedNFI, 5);
            passed &= VerifyDecimalParse("$5,0", NumberStyles.Currency, swappedNFI, 5);
            passed &= VerifyDecimalParse("5,000", NumberStyles.Currency, swappedNFI, 5);
            passed &= VerifyDecimalParse("$5,000", NumberStyles.Currency, swappedNFI, 5);
            passed &= VerifyDecimalParseException("5,000.0", NumberStyles.Currency, swappedNFI, typeof(FormatException));
            passed &= VerifyDecimalParseException("$5,000.0", NumberStyles.Currency, swappedNFI, typeof(FormatException));
            passed &= VerifyDecimalParse("5,000", NumberStyles.Any, swappedNFI, 5);
            passed &= VerifyDecimalParse("$5,000", NumberStyles.Any, swappedNFI, 5);
            passed &= VerifyDecimalParseException("5,000.0", NumberStyles.Any, swappedNFI, typeof(FormatException));
            passed &= VerifyDecimalParseException("$5,000.0", NumberStyles.Any, swappedNFI, typeof(FormatException));
            //
            passed &= VerifyDecimalParse("5.0", NumberStyles.Number, distinctNFI, 5);
            passed &= VerifyDecimalParse("1,234.0", NumberStyles.Number, distinctNFI, 1234);
            passed &= VerifyDecimalParse("5.0", NumberStyles.Currency, distinctNFI, 5);
            passed &= VerifyDecimalParse("1,234.0", NumberStyles.Currency, distinctNFI, 1234);
            passed &= VerifyDecimalParse("5.0", NumberStyles.Any, distinctNFI, 5);
            passed &= VerifyDecimalParse("1,234.0", NumberStyles.Any, distinctNFI, 1234);
            passed &= VerifyDecimalParseException("$5.0", NumberStyles.Currency, distinctNFI, typeof(FormatException));
            passed &= VerifyDecimalParseException("$5.0", NumberStyles.Any, distinctNFI, typeof(FormatException));
            passed &= VerifyDecimalParseException("5:0", NumberStyles.Number, distinctNFI, typeof(FormatException));
            passed &= VerifyDecimalParseException("5;0", NumberStyles.Number, distinctNFI, typeof(FormatException));
            passed &= VerifyDecimalParseException("$5:0", NumberStyles.Number, distinctNFI, typeof(FormatException));
            passed &= VerifyDecimalParse("5:0", NumberStyles.Currency, distinctNFI, 5);
            passed &= VerifyDecimalParse("5:000", NumberStyles.Currency, distinctNFI, 5);
            passed &= VerifyDecimalParse("5;000", NumberStyles.Currency, distinctNFI, 5000);
            passed &= VerifyDecimalParse("$5:0", NumberStyles.Currency, distinctNFI, 5);
            passed &= VerifyDecimalParse("$5;0", NumberStyles.Currency, distinctNFI, 50);
            passed &= VerifyDecimalParse("5:0", NumberStyles.Any, distinctNFI, 5);
            passed &= VerifyDecimalParse("5;0", NumberStyles.Any, distinctNFI, 50);
            passed &= VerifyDecimalParse("$5:0", NumberStyles.Any, distinctNFI, 5);
            passed &= VerifyDecimalParse("$5;0", NumberStyles.Any, distinctNFI, 50);
            passed &= VerifyDecimalParseException("123,456;789.0", NumberStyles.Number, distinctNFI, typeof(FormatException));
            passed &= VerifyDecimalParse("123,456;789.0", NumberStyles.Currency, distinctNFI, 123456789);
            passed &= VerifyDecimalParse("123,456;789.0", NumberStyles.Any, distinctNFI, 123456789);
            passed &= VerifyDecimalParseException("$123,456;789.0", NumberStyles.Any, distinctNFI, typeof(FormatException));
            //
            passed &= VerifyDecimalParseException("79228162514264337593543950336", typeof(OverflowException));
            passed &= VerifyDecimalParseException("-79228162514264337593543950336", typeof(OverflowException));
            passed &= VerifyDecimalParseException("Garbage", typeof(FormatException));
            passed &= VerifyDecimalParseException("5\0Garbage", typeof(FormatException));
            passed &= VerifyDecimalParseException(null, typeof(ArgumentNullException));
            passed &= VerifyDecimalParseException("FF", NumberStyles.HexNumber, goodNFI, typeof(ArgumentException));
            passed &= VerifyDecimalParseException("4", (NumberStyles)(-1), typeof(ArgumentException));
            passed &= VerifyDecimalParseException("4", (NumberStyles)0x10000, typeof(ArgumentException));
            passed &= VerifyDecimalParseException("4", (NumberStyles)(-1), CultureInfo.InvariantCulture, typeof(ArgumentException));
            passed &= VerifyDecimalParseException("4", (NumberStyles)0x10000, CultureInfo.InvariantCulture, typeof(ArgumentException));
            passed &= VerifyDecimalParseException("123,000,000,000,000,000,000", NumberStyles.Any, germanCulture, typeof(FormatException));
            passed &= VerifyDecimalParseException("123.000.000.000.000.000.000", NumberStyles.Any, japaneseCulture, typeof(FormatException));
            passed &= VerifyDecimalParseException("5,00 \u20AC", NumberStyles.Integer, germanCulture, typeof(FormatException)); // currency

            // Underflow cases - see VSWhidbey #576556
            Decimal zeroScale28 = new Decimal(0, 0, 0, false, 28);
            Decimal zeroScale27 = new Decimal(0, 0, 0, false, 27);
            passed &= VerifyExactDecimalParse("0E-27", NumberStyles.AllowExponent, zeroScale27);
            passed &= VerifyExactDecimalParse("0E-28", NumberStyles.AllowExponent, zeroScale28);
            passed &= VerifyExactDecimalParse("0E-29", NumberStyles.AllowExponent, zeroScale28);
            passed &= VerifyExactDecimalParse("0E-30", NumberStyles.AllowExponent, zeroScale28);
            passed &= VerifyExactDecimalParse("0E-31", NumberStyles.AllowExponent, zeroScale28);
            passed &= VerifyExactDecimalParse("0E-50", NumberStyles.AllowExponent, zeroScale28);
            passed &= VerifyExactDecimalParse("0E-100", NumberStyles.AllowExponent, zeroScale28);
            passed &= VerifyExactDecimalParse("0.000000000000000000000000000", zeroScale27);
            passed &= VerifyExactDecimalParse("0.0000000000000000000000000000", zeroScale28); //28
            passed &= VerifyExactDecimalParse("0.00000000000000000000000000000", zeroScale28); //29
            passed &= VerifyExactDecimalParse("0.000000000000000000000000000000", zeroScale28); //30
            passed &= VerifyExactDecimalParse("0.0000000000000000000000000000000", zeroScale28); //31
            passed &= VerifyExactDecimalParse("0.0", new Decimal(0, 0, 0, false, 1));
            passed &= VerifyExactDecimalParse("0E-15", NumberStyles.AllowExponent, new Decimal(0, 0, 0, false, 15));
            passed &= VerifyExactDecimalParse("0", Decimal.Zero);

            Decimal oneScale27 = new Decimal(1, 0, 0, false, 27);
            Decimal oneScale28 = new Decimal(1, 0, 0, false, 28);

            passed &= VerifyExactDecimalParse("1E-27", NumberStyles.AllowExponent, oneScale27);
            passed &= VerifyExactDecimalParse("1E-28", NumberStyles.AllowExponent, oneScale28);
            passed &= VerifyExactDecimalParse("1E-29", NumberStyles.AllowExponent, zeroScale28);
            passed &= VerifyExactDecimalParse("1E-30", NumberStyles.AllowExponent, zeroScale28);
            passed &= VerifyExactDecimalParse("1E-31", NumberStyles.AllowExponent, zeroScale28);
            passed &= VerifyExactDecimalParse("1E-50", NumberStyles.AllowExponent, zeroScale28);
            passed &= VerifyExactDecimalParse("1E-100", NumberStyles.AllowExponent, zeroScale28);

            passed &= VerifyExactDecimalParse("1E-27", NumberStyles.AllowExponent, invariantCulture, oneScale27);
            passed &= VerifyExactDecimalParse("1E-28", NumberStyles.AllowExponent, invariantCulture, oneScale28);
            passed &= VerifyExactDecimalParse("1E-29", NumberStyles.AllowExponent, invariantCulture, zeroScale28);
            passed &= VerifyExactDecimalParse("1E-30", NumberStyles.AllowExponent, invariantCulture, zeroScale28);
            passed &= VerifyExactDecimalParse("1E-31", NumberStyles.AllowExponent, invariantCulture, zeroScale28);
            passed &= VerifyExactDecimalParse("1E-50", NumberStyles.AllowExponent, invariantCulture, zeroScale28);
            passed &= VerifyExactDecimalParse("1E-100", NumberStyles.AllowExponent, invariantCulture, zeroScale28);
            passed &= VerifyExactDecimalParse("0E-27", NumberStyles.AllowExponent, invariantCulture, zeroScale27);
            passed &= VerifyExactDecimalParse("0E-28", NumberStyles.AllowExponent, invariantCulture, zeroScale28);
            passed &= VerifyExactDecimalParse("0E-29", NumberStyles.AllowExponent, invariantCulture, zeroScale28);
            passed &= VerifyExactDecimalParse("0E-30", NumberStyles.AllowExponent, invariantCulture, zeroScale28);
            passed &= VerifyExactDecimalParse("0E-31", NumberStyles.AllowExponent, invariantCulture, zeroScale28);
            passed &= VerifyExactDecimalParse("0E-50", NumberStyles.AllowExponent, invariantCulture, zeroScale28);
            passed &= VerifyExactDecimalParse("0E-100", NumberStyles.AllowExponent, invariantCulture, zeroScale28);

            // make sure parse lines up with compiler
            //passed &= VerifyExactDecimalParse("0E-50", NumberStyles.AllowExponent, 0E-50m); // V2->V4 compiler change
            passed &= VerifyExactDecimalParse("1E-50", NumberStyles.AllowExponent, 1E-50m);
            passed &= VerifyExactDecimalParse("2E-100", NumberStyles.AllowExponent, 2E-100m);
            passed &= VerifyExactDecimalParse("100E-29", NumberStyles.AllowExponent, 100E-29m);
            passed &= VerifyExactDecimalParse("200E-29", NumberStyles.AllowExponent, 200E-29m);
            passed &= VerifyExactDecimalParse("500E-29", NumberStyles.AllowExponent, 500E-29m);
            passed &= VerifyExactDecimalParse("900E-29", NumberStyles.AllowExponent, 900E-29m);
            passed &= VerifyExactDecimalParse("1900E-29", NumberStyles.AllowExponent, 1900E-29m);
            passed &= VerifyExactDecimalParse("10900E-29", NumberStyles.AllowExponent, 10900E-29m);
            passed &= VerifyExactDecimalParse("10900E-30", NumberStyles.AllowExponent, 10900E-30m);
            passed &= VerifyExactDecimalParse("10900E-31", NumberStyles.AllowExponent, 10900E-31m);
            passed &= VerifyExactDecimalParse("10900E-32", NumberStyles.AllowExponent, 10900E-32m);
            passed &= VerifyExactDecimalParse("10900E-33", NumberStyles.AllowExponent, 10900E-33m);
            passed &= VerifyExactDecimalParse("10900E-34", NumberStyles.AllowExponent, 10900E-34m);
            passed &= VerifyExactDecimalParse("10900E-340", NumberStyles.AllowExponent, 10900E-340m);
            passed &= VerifyExactDecimalParse("10900E-512", NumberStyles.AllowExponent, 10900E-512m);
            passed &= VerifyExactDecimalParse("10900E-678", NumberStyles.AllowExponent, 10900E-678m);
            passed &= VerifyExactDecimalParse("10900E-999", NumberStyles.AllowExponent, 10900E-999m);


            /////////// TryParse(String)
            //// Pass cases
            passed &= VerifyDecimalTryParse("5", 5, true);
            passed &= VerifyDecimalTryParse(" 5 ", 5, true);
            passed &= VerifyDecimalTryParse("-5", -5, true);
            passed &= VerifyDecimalTryParse("5\0", 5, true);
            passed &= VerifyDecimalTryParse("5  \0", 5, true);
            passed &= VerifyDecimalTryParse("5\0\0\0", 5, true);
            passed &= VerifyDecimalTryParse("893382737", 893382737, true);
            passed &= VerifyDecimalTryParse("-893382737", -893382737, true);
            passed &= VerifyDecimalTryParse("1234567891", 1234567891, true);
            passed &= VerifyDecimalTryParse("-1234567891", -1234567891, true);
            passed &= VerifyDecimalTryParse("123456789123456789", 123456789123456789, true);
            passed &= VerifyDecimalTryParse("-123456789123456789", -123456789123456789, true);
            passed &= VerifyDecimalTryParse("123456789123456789123", 123456789123456789123m, true);
            passed &= VerifyDecimalTryParse("-123456789123456789123", -123456789123456789123m, true);
            passed &= VerifyDecimalTryParse("18446744073709551615", 18446744073709551615, true);
            passed &= VerifyDecimalTryParse("79228162514264337593543950335", 79228162514264337593543950335m, true);
            passed &= VerifyDecimalTryParse("-79228162514264337593543950335", -79228162514264337593543950335m, true);
            passed &= VerifyDecimalTryParse("7.3", 7.3m, true);
            passed &= VerifyDecimalTryParse(".297", 0.297m, true);
            passed &= VerifyDecimalTryParse("5.555555555", 5.555555555m, true);
            passed &= VerifyDecimalTryParse("1.000000", 1.000000m, true);

            //// Fail cases
            passed &= VerifyDecimalTryParse(null, 0, false);
            passed &= VerifyDecimalTryParse("", 0, false);
            passed &= VerifyDecimalTryParse("Garbage", 0, false);
            passed &= VerifyDecimalTryParse("5\0Garbage", 0, false);
            passed &= VerifyDecimalTryParse("FF", 0, false);
            passed &= VerifyDecimalTryParse("23 5", 0, false);
            passed &= VerifyDecimalTryParse("NaN", 0, false);
            passed &= VerifyDecimalTryParse("Infinity", 0, false);
            passed &= VerifyDecimalTryParse("-Infinity", 0, false);
            passed &= VerifyDecimalTryParse("79228162514264337593543950336", 0, false);
            passed &= VerifyDecimalTryParse("-79228162514264337593543950336", 0, false);
            passed &= VerifyDecimalTryParse("1.234+E05", 0, false);
            passed &= VerifyDecimalTryParse("1.234E+05", 0, false);


            /////////// TryParse(TryParse(String, NumberStyles, IFormatProvider, ref Decimal)
            //// Pass cases
            passed &= VerifyDecimalTryParse("5", NumberStyles.Integer, CultureInfo.InvariantCulture, 5, true);
            passed &= VerifyDecimalTryParse("-5", NumberStyles.Integer, CultureInfo.InvariantCulture, -5, true);
            // Variations on NumberStyles
            passed &= VerifyDecimalTryParse("   5", NumberStyles.AllowLeadingWhite, goodNFI, 5, true);
            passed &= VerifyDecimalTryParse("5", NumberStyles.Number, goodNFI, 5, true);
            passed &= VerifyDecimalTryParse("5.0", NumberStyles.AllowDecimalPoint, goodNFI, 5.0m, true);
            passed &= VerifyDecimalTryParse("5.3", NumberStyles.AllowDecimalPoint, goodNFI, 5.3m, true);
            // Variations on IFP
            passed &= VerifyDecimalTryParse("5", NumberStyles.Integer, goodNFI, 5, true);
            passed &= VerifyDecimalTryParse("5", NumberStyles.Integer, null, 5, true);
            passed &= VerifyDecimalTryParse("5", NumberStyles.Integer, new DateTimeFormatInfo(), 5, true);
            passed &= VerifyDecimalTryParse("^42", NumberStyles.Any, customNFI, -42, true);
            passed &= VerifyDecimalTryParse("123", NumberStyles.Integer, germanCulture, 123, true);
            passed &= VerifyDecimalTryParse("123", NumberStyles.Integer, japaneseCulture, 123, true);
            passed &= VerifyDecimalTryParse("123.456", NumberStyles.Any, germanCulture, 123456, true);
            passed &= VerifyDecimalTryParse("123,456", NumberStyles.Any, japaneseCulture, 123456, true);
            passed &= VerifyDecimalTryParse("123,456", NumberStyles.AllowDecimalPoint, germanCulture, 123.456m, true);
            passed &= VerifyDecimalTryParse("123.456", NumberStyles.AllowDecimalPoint, japaneseCulture, 123.456m, true);
            passed &= VerifyDecimalTryParse("5,23 " + germanCulture.NumberFormat.CurrencySymbol, NumberStyles.Any, germanCulture, 5.23m, true); // currency
            passed &= VerifyDecimalTryParse("5.23 " + germanCulture.NumberFormat.CurrencySymbol, NumberStyles.Any, germanCulture, 523, true); // currency
            //
            //// Fail cases
            passed &= VerifyDecimalTryParse("-79228162514264337593543950336", NumberStyles.Integer, CultureInfo.InvariantCulture, 0, false);
            passed &= VerifyDecimalTryParse("-42", NumberStyles.Any, customNFI, 0, false);
            passed &= VerifyDecimalTryParse("5   ", NumberStyles.AllowLeadingWhite, goodNFI, 0, false);
            passed &= VerifyDecimalTryParse("1.234+E05", NumberStyles.AllowExponent, goodNFI, 0, false);
            passed &= VerifyDecimalTryParse("5.3", NumberStyles.None, goodNFI, 0, false);

            //// Exception cases
            passed &= VerifyDecimalTryParseException("12", NumberStyles.HexNumber, CultureInfo.InvariantCulture, typeof(ArgumentException));
            passed &= VerifyDecimalTryParseException("5", NumberStyles.AllowHexSpecifier | NumberStyles.AllowParentheses, null, typeof(ArgumentException));
            passed &= VerifyDecimalTryParseException("4", (NumberStyles)(-1), CultureInfo.InvariantCulture, typeof(ArgumentException));
            passed &= VerifyDecimalTryParseException("4", (NumberStyles)0x10000, CultureInfo.InvariantCulture, typeof(ArgumentException));

            // NumberStyles/NFI variations
            //
            passed &= VerifyDecimalTryParse("5", NumberStyles.Integer, corruptNFI, 5, true);
            passed &= VerifyDecimalTryParse("5", NumberStyles.Number, corruptNFI, 5, true);
            passed &= VerifyDecimalTryParse("5.3", NumberStyles.Number, corruptNFI, 5.3m, true);
            passed &= VerifyDecimalTryParse("5,3", NumberStyles.Number, corruptNFI, 0, false);
            passed &= VerifyDecimalTryParse("5.2.3", NumberStyles.Number, corruptNFI, 0, false);
            passed &= VerifyDecimalTryParse("$5.3", NumberStyles.Currency, corruptNFI, 5.3m, true);
            passed &= VerifyDecimalTryParse("$5,3", NumberStyles.Currency, corruptNFI, 0, false);
            passed &= VerifyDecimalTryParse("$5.2.3", NumberStyles.Currency, corruptNFI, 0, false);
            passed &= VerifyDecimalTryParse("5.3", NumberStyles.Currency, corruptNFI, 5.3m, true);
            passed &= VerifyDecimalTryParse("5,3", NumberStyles.Currency, corruptNFI, 0, false);
            passed &= VerifyDecimalTryParse("5.2.3", NumberStyles.Currency, corruptNFI, 0, false);
            passed &= VerifyDecimalTryParse("5.3", NumberStyles.Any, corruptNFI, 5.3m, true);
            passed &= VerifyDecimalTryParse("5,3", NumberStyles.Any, corruptNFI, 0, false);
            passed &= VerifyDecimalTryParse("5.2.3", NumberStyles.Any, corruptNFI, 0, false);
            //
            passed &= VerifyDecimalTryParse("5", NumberStyles.Integer, swappedNFI, 5, true);
            passed &= VerifyDecimalTryParse("1,234", NumberStyles.Integer, swappedNFI, 0, false);
            passed &= VerifyDecimalTryParse("5", NumberStyles.Number, swappedNFI, 5, true);
            passed &= VerifyDecimalTryParse("5.0", NumberStyles.Number, swappedNFI, 5, true);
            passed &= VerifyDecimalTryParse("1,234", NumberStyles.Number, swappedNFI, 1234, true);
            passed &= VerifyDecimalTryParse("1,234.0", NumberStyles.Number, swappedNFI, 1234, true);
            passed &= VerifyDecimalTryParse("5.000.000", NumberStyles.Number, swappedNFI, 0, false);
            passed &= VerifyDecimalTryParse("5.000,00", NumberStyles.Number, swappedNFI, 0, false);
            passed &= VerifyDecimalTryParse("5.000", NumberStyles.Currency, swappedNFI, 5, true); //???
            passed &= VerifyDecimalTryParse("5.000,00", NumberStyles.Currency, swappedNFI, 0, false); //???
            passed &= VerifyDecimalTryParse("$5.000", NumberStyles.Currency, swappedNFI, 5000, true);
            passed &= VerifyDecimalTryParse("$5.000,00", NumberStyles.Currency, swappedNFI, 5000, true);
            passed &= VerifyDecimalTryParse("5.000", NumberStyles.Any, swappedNFI, 5, true);  //?
            passed &= VerifyDecimalTryParse("5.000,00", NumberStyles.Any, swappedNFI, 0, false);  //?
            passed &= VerifyDecimalTryParse("$5.000", NumberStyles.Any, swappedNFI, 5000, true);
            passed &= VerifyDecimalTryParse("$5.000,00", NumberStyles.Any, swappedNFI, 5000, true);
            passed &= VerifyDecimalTryParse("5,0", NumberStyles.Currency, swappedNFI, 5, true);
            passed &= VerifyDecimalTryParse("$5,0", NumberStyles.Currency, swappedNFI, 5, true);
            passed &= VerifyDecimalTryParse("5,000", NumberStyles.Currency, swappedNFI, 5, true);
            passed &= VerifyDecimalTryParse("$5,000", NumberStyles.Currency, swappedNFI, 5, true);
            passed &= VerifyDecimalTryParse("5,000.0", NumberStyles.Currency, swappedNFI, 0, false);
            passed &= VerifyDecimalTryParse("$5,000.0", NumberStyles.Currency, swappedNFI, 0, false);
            passed &= VerifyDecimalTryParse("5,000", NumberStyles.Any, swappedNFI, 5, true);
            passed &= VerifyDecimalTryParse("$5,000", NumberStyles.Any, swappedNFI, 5, true);
            passed &= VerifyDecimalTryParse("5,000.0", NumberStyles.Any, swappedNFI, 0, false);
            passed &= VerifyDecimalTryParse("$5,000.0", NumberStyles.Any, swappedNFI, 0, false);
            //
            passed &= VerifyDecimalTryParse("5.0", NumberStyles.Number, distinctNFI, 5, true);
            passed &= VerifyDecimalTryParse("1,234.0", NumberStyles.Number, distinctNFI, 1234, true);
            passed &= VerifyDecimalTryParse("5.0", NumberStyles.Currency, distinctNFI, 5, true);
            passed &= VerifyDecimalTryParse("1,234.0", NumberStyles.Currency, distinctNFI, 1234, true);
            passed &= VerifyDecimalTryParse("5.0", NumberStyles.Any, distinctNFI, 5, true);
            passed &= VerifyDecimalTryParse("1,234.0", NumberStyles.Any, distinctNFI, 1234, true);
            passed &= VerifyDecimalTryParse("$5.0", NumberStyles.Currency, distinctNFI, 0, false);
            passed &= VerifyDecimalTryParse("$5.0", NumberStyles.Any, distinctNFI, 0, false);
            passed &= VerifyDecimalTryParse("5:0", NumberStyles.Number, distinctNFI, 0, false);
            passed &= VerifyDecimalTryParse("5;0", NumberStyles.Number, distinctNFI, 0, false);
            passed &= VerifyDecimalTryParse("$5:0", NumberStyles.Number, distinctNFI, 0, false);
            passed &= VerifyDecimalTryParse("5:0", NumberStyles.Currency, distinctNFI, 5, true);
            passed &= VerifyDecimalTryParse("5:000", NumberStyles.Currency, distinctNFI, 5, true);
            passed &= VerifyDecimalTryParse("5;000", NumberStyles.Currency, distinctNFI, 5000, true);
            passed &= VerifyDecimalTryParse("$5:0", NumberStyles.Currency, distinctNFI, 5, true);
            passed &= VerifyDecimalTryParse("$5;0", NumberStyles.Currency, distinctNFI, 50, true);
            passed &= VerifyDecimalTryParse("5:0", NumberStyles.Any, distinctNFI, 5, true);
            passed &= VerifyDecimalTryParse("5;0", NumberStyles.Any, distinctNFI, 50, true);
            passed &= VerifyDecimalTryParse("$5:0", NumberStyles.Any, distinctNFI, 5, true);
            passed &= VerifyDecimalTryParse("$5;0", NumberStyles.Any, distinctNFI, 50, true);
            passed &= VerifyDecimalTryParse("123,456;789.0", NumberStyles.Number, distinctNFI, 0, false);
            passed &= VerifyDecimalTryParse("123,456;789.0", NumberStyles.Currency, distinctNFI, 123456789, true);
            passed &= VerifyDecimalTryParse("123,456;789.0", NumberStyles.Any, distinctNFI, 123456789, true);
            passed &= VerifyDecimalTryParse("$123,456;789.0", NumberStyles.Any, distinctNFI, 0, false);


            // Underflow cases - see VSWhidbey #576556
            passed &= VerifyExactDecimalTryParse("0E-27", NumberStyles.AllowExponent, invariantCulture, zeroScale27, true);
            passed &= VerifyExactDecimalTryParse("0E-28", NumberStyles.AllowExponent, invariantCulture, zeroScale28, true);
            passed &= VerifyExactDecimalTryParse("0E-29", NumberStyles.AllowExponent, invariantCulture, zeroScale28, true);
            passed &= VerifyExactDecimalTryParse("0E-30", NumberStyles.AllowExponent, invariantCulture, zeroScale28, true);
            passed &= VerifyExactDecimalTryParse("0E-31", NumberStyles.AllowExponent, invariantCulture, zeroScale28, true);
            passed &= VerifyExactDecimalTryParse("0E-50", NumberStyles.AllowExponent, invariantCulture, zeroScale28, true);
            passed &= VerifyExactDecimalTryParse("0E-100", NumberStyles.AllowExponent, invariantCulture, zeroScale28, true);
            passed &= VerifyExactDecimalTryParse("0.000000000000000000000000000", zeroScale27, true);
            passed &= VerifyExactDecimalTryParse("0.0000000000000000000000000000", zeroScale28, true); //28
            passed &= VerifyExactDecimalTryParse("0.00000000000000000000000000000", zeroScale28, true); //29
            passed &= VerifyExactDecimalTryParse("0.000000000000000000000000000000", zeroScale28, true); //30
            passed &= VerifyExactDecimalTryParse("0.0000000000000000000000000000000", zeroScale28, true); //31
            passed &= VerifyExactDecimalTryParse("0.0", new Decimal(0, 0, 0, false, 1), true);
            passed &= VerifyExactDecimalTryParse("0E-15", NumberStyles.AllowExponent, invariantCulture, new Decimal(0, 0, 0, false, 15), true);
            passed &= VerifyExactDecimalTryParse("0", Decimal.Zero, true);

            passed &= VerifyExactDecimalTryParse("1E-27", NumberStyles.AllowExponent, invariantCulture, oneScale27, true);
            passed &= VerifyExactDecimalTryParse("1E-28", NumberStyles.AllowExponent, invariantCulture, oneScale28, true);
            passed &= VerifyExactDecimalTryParse("1E-29", NumberStyles.AllowExponent, invariantCulture, zeroScale28, true);
            passed &= VerifyExactDecimalTryParse("1E-30", NumberStyles.AllowExponent, invariantCulture, zeroScale28, true);
            passed &= VerifyExactDecimalTryParse("1E-31", NumberStyles.AllowExponent, invariantCulture, zeroScale28, true);
            passed &= VerifyExactDecimalTryParse("1E-50", NumberStyles.AllowExponent, invariantCulture, zeroScale28, true);
            passed &= VerifyExactDecimalTryParse("1E-100", NumberStyles.AllowExponent, invariantCulture, zeroScale28, true);

            // make sure parse lines up with compiler
            //passed &= VerifyExactDecimalTryParse("0E-50", NumberStyles.AllowExponent, invariantCulture, 0E-50m, true); // V2->V4 compiler change
            passed &= VerifyExactDecimalTryParse("1E-50", NumberStyles.AllowExponent, invariantCulture, 1E-50m, true);
            passed &= VerifyExactDecimalTryParse("2E-100", NumberStyles.AllowExponent, invariantCulture, 2E-100m, true);
            passed &= VerifyExactDecimalTryParse("100E-29", NumberStyles.AllowExponent, invariantCulture, 100E-29m, true);
            passed &= VerifyExactDecimalTryParse("200E-29", NumberStyles.AllowExponent, invariantCulture, 200E-29m, true);
            passed &= VerifyExactDecimalTryParse("500E-29", NumberStyles.AllowExponent, invariantCulture, 500E-29m, true);
            passed &= VerifyExactDecimalTryParse("900E-29", NumberStyles.AllowExponent, invariantCulture, 900E-29m, true);
            passed &= VerifyExactDecimalTryParse("1900E-29", NumberStyles.AllowExponent, invariantCulture, 1900E-29m, true);
            passed &= VerifyExactDecimalTryParse("10900E-29", NumberStyles.AllowExponent, invariantCulture, 10900E-29m, true);
            passed &= VerifyExactDecimalTryParse("10900E-30", NumberStyles.AllowExponent, invariantCulture, 10900E-30m, true);
            passed &= VerifyExactDecimalTryParse("10900E-31", NumberStyles.AllowExponent, invariantCulture, 10900E-31m, true);
            passed &= VerifyExactDecimalTryParse("10900E-32", NumberStyles.AllowExponent, invariantCulture, 10900E-32m, true);
            passed &= VerifyExactDecimalTryParse("10900E-33", NumberStyles.AllowExponent, invariantCulture, 10900E-33m, true);
            passed &= VerifyExactDecimalTryParse("10900E-34", NumberStyles.AllowExponent, invariantCulture, 10900E-34m, true);
            passed &= VerifyExactDecimalTryParse("10900E-340", NumberStyles.AllowExponent, invariantCulture, 10900E-340m, true);
            passed &= VerifyExactDecimalTryParse("10900E-512", NumberStyles.AllowExponent, invariantCulture, 10900E-512m, true);
            passed &= VerifyExactDecimalTryParse("10900E-678", NumberStyles.AllowExponent, invariantCulture, 10900E-678m, true);
            passed &= VerifyExactDecimalTryParse("10900E-999", NumberStyles.AllowExponent, invariantCulture, 10900E-999m, true);


            // Should these pass or fail?  Current parse behavior is to pass, so they might be
            // parse bugs, but they're not tryparse bugs.
            passed &= VerifyDecimalParse("5", NumberStyles.AllowExponent, goodNFI, 5);
            passed &= VerifyDecimalTryParse("5", NumberStyles.AllowExponent, goodNFI, 5, true);
            // I expect ArgumentException with an ambiguous NFI
            passed &= VerifyDecimalParse("^42", NumberStyles.Any, ambigNFI, -42);
            passed &= VerifyDecimalTryParse("^42", NumberStyles.Any, ambigNFI, -42, true);

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

    public static bool VerifyDecimalTryParse(string value, Decimal expectedResult, bool expectedReturn)
    {
        if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: Decimal.TryParse, Value = '{0}', Expected Result = {1}, Expected Return = {2}",
                value, expectedResult, expectedReturn);
        }
        Decimal result = 0;
        try
        {
            bool returnValue = Decimal.TryParse(value, out result);
            if (returnValue != expectedReturn)
            {
                TestLibrary.Logging.WriteLine("FAILURE: Value = '{0}', Expected Return: {1}, Actual Return: {2}", value, expectedReturn, returnValue);
                return false;
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

    public static bool VerifyDecimalTryParse(string value, NumberStyles style, IFormatProvider provider, Decimal expectedResult, bool expectedReturn)
    {
		if (provider == null) return true;
		
		if (verbose)
		{
			TestLibrary.Logging.WriteLine("Test: Decimal.TryParse, Value = '{0}', Style = {1}, Provider = {2}, Expected Result = {3}, Expected Return = {4}",
				  value, style, provider, expectedResult, expectedReturn);
		}
        Decimal result = 0;
        try
        {
            bool returnValue = Decimal.TryParse(value, style, provider, out result);
            if (returnValue != expectedReturn)
            {
                TestLibrary.Logging.WriteLine("FAILURE: Value = '{0}', Style = {1}, Provider = {2}, Expected Return = {3}, Actual Return = {4}",
                  value, style, provider, expectedReturn, returnValue);
                return false;
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

    public static bool VerifyDecimalTryParseException(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
    {
		if (provider == null) return true;

		if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: Decimal.TryParse, Value = '{0}', Style = {1}, Provider = {2}, Expected Exception = {3}",
                value, style, provider, exceptionType);
        }
        try
        {
            Decimal result = 0;
            Boolean returnValue = Decimal.TryParse(value, style, provider, out result);
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

    public static bool VerifyDecimalParse(string value, Decimal expectedResult)
    {
        if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: Decimal.Parse, Value = '{0}', Expected Result, {1}",
                value, expectedResult);
        }
        try
        {
            Decimal returnValue = Decimal.Parse(value);
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

    public static bool VerifyDecimalParse(string value, NumberStyles style, IFormatProvider provider, Decimal expectedResult)
    {
		if (provider == null) return true;

		if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: Decimal.Parse, Value = '{0}', Style = {1}, provider = {2}, Expected Result = {3}",
                value, style, provider, expectedResult);
        }
        try
        {
            Decimal returnValue = Decimal.Parse(value, style, provider);
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

    public static String HexValue(Decimal value)
    {
        Int32[] bits = Decimal.GetBits(value);
        return String.Format("{{0x{0:X8} {1:X8} {2:X8} {3:X8} }}", bits[0], bits[1], bits[2], bits[3]);
    }

    // Verify that decimals have the same bits, not just the same values.
    public static Boolean CompareExact(Decimal x, Decimal y)
    {
        Int32[] arrayX = Decimal.GetBits(x);
        Int32[] arrayY = Decimal.GetBits(y);
        for (int i = 0; i < 4; i++)
        {
            if (arrayX[i] != arrayY[i])
            {
                return false;
            }
        }
        return true;
    }

    public static bool VerifyExactDecimalParse(string value, Decimal expectedResult)
    {
        if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: Decimal.Parse, Value = '{0}', Expected Result = {1}",
                value, expectedResult);
        }
        try
        {
            Decimal returnValue = Decimal.Parse(value);
            if (!CompareExact(returnValue, expectedResult))
            {
                TestLibrary.Logging.WriteLine("FAILURE: Value = '{0}', Expected Return: {1}, Actual Return: {2}", value, HexValue(expectedResult), HexValue(returnValue));
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

    public static bool VerifyExactDecimalParse(string value, NumberStyles style, Decimal expectedResult)
    {
        if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: Decimal.Parse, Value = '{0}', Style = {1}, Expected Result = {2}",
                value, style, expectedResult);
        }
        try
        {
            Decimal returnValue = Decimal.Parse(value, style);
            if (!CompareExact(returnValue, expectedResult))
            {
                TestLibrary.Logging.WriteLine("FAILURE: Value = '{0}', Expected Return: {1}, Actual Return: {2}", value, HexValue(expectedResult), HexValue(returnValue));
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

    public static bool VerifyExactDecimalParse(string value, NumberStyles style, IFormatProvider provider, Decimal expectedResult)
    {
		if (provider == null) return true;

		if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: Decimal.Parse, Value = '{0}', Style = {1}, provider = {2}, Expected Result = {3}",
                value, style, provider, expectedResult);
        }
        try
        {
            Decimal returnValue = Decimal.Parse(value, style, provider);
            if (!CompareExact(returnValue, expectedResult))
            {
                TestLibrary.Logging.WriteLine("FAILURE: Value = '{0}', Expected Return: {1}, Actual Return: {2}", value, HexValue(expectedResult), HexValue(returnValue));
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

    public static bool VerifyExactDecimalTryParse(string value, Decimal expectedResult, bool expectedReturn)
    {
        if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: Decimal.TryParse, Value = '{0}', Expected Result = {1}, Expected Return = {2}",
                value, expectedResult, expectedReturn);
        }
        Decimal result = 0;
        try
        {
            bool returnValue = Decimal.TryParse(value, out result);
            if (returnValue != expectedReturn)
            {
                TestLibrary.Logging.WriteLine("FAILURE: Value = '{0}', Expected Return: {1}, Actual Return: {2}", value, expectedReturn, returnValue);
                return false;
            }
            if (!CompareExact(result, expectedResult))
            {
                TestLibrary.Logging.WriteLine("FAILURE: Value = '{0}', Expected Return: {1}, Actual Return: {2}", value, HexValue(expectedResult), HexValue(result));
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

    public static bool VerifyExactDecimalTryParse(string value, NumberStyles style, IFormatProvider provider, Decimal expectedResult, bool expectedReturn)
    {
		if (provider == null) return true;

		if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: Decimal.TryParse, Value = '{0}', Style = {1}, Provider = {2}, Expected Result = {3}, Expected Return = {4}",
                  value, style, provider, expectedResult, expectedReturn);
        }
        Decimal result = 0;
        try
        {
            bool returnValue = Decimal.TryParse(value, style, provider, out result);
            if (returnValue != expectedReturn)
            {
                TestLibrary.Logging.WriteLine("FAILURE: Value = '{0}', Style = {1}, Provider = {2}, Expected Return = {3}, Actual Return = {4}",
                  value, style, provider, expectedReturn, returnValue);
                return false;
            }
            if (!CompareExact(result, expectedResult))
            {
                TestLibrary.Logging.WriteLine("FAILURE: Value = '{0}', Expected Return: {1}, Actual Return: {2}", value, HexValue(expectedResult), HexValue(result));
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

    public static bool VerifyDecimalParseException(string value, Type exceptionType)
    {
        if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: Decimal.Parse, Value = '{0}', Expected Exception, {1}",
                value, exceptionType);
        }
        try
        {
            Decimal returnValue = Decimal.Parse(value);
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

    public static bool VerifyDecimalParseException(string value, NumberStyles style, Type exceptionType)
    {
        if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: Decimal.Parse, Value = '{0}', Style = {1}, Expected Exception = {3}",
                value, style, exceptionType);
        }
        try
        {
            Decimal returnValue = Decimal.Parse(value, style);
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

    public static bool VerifyDecimalParseException(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
    {
		if (provider == null) return true;

		if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: Decimal.Parse, Value = '{0}', Style = {1}, Provider = {2}, Expected Exception = {3}",
                value, style, provider, exceptionType);
        }
        try
        {
            Decimal returnValue = Decimal.Parse(value, style, provider);
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
