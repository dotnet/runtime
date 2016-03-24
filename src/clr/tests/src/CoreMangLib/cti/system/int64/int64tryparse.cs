// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

// Ported over to CoreCLR from Co7529TryParse_all.cs
// Tests Int64.TryParse(String), Int64.TryParse(String, NumberStyles, IFormatProvider, ref Int64)
// 2003/04/01  KatyK
// 2007/06/28  adapted by MarielY

public class Int64TryParse
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
            passed &= VerifyInt64Parse("5", 5);
            passed &= VerifyInt64Parse("-5", -5);
            passed &= VerifyInt64Parse("5  ", 5);
            passed &= VerifyInt64Parse("5\0", 5);
            passed &= VerifyInt64Parse("893382737", 893382737);
            passed &= VerifyInt64Parse("-893382737", -893382737);
            passed &= VerifyInt64Parse("1234567891", 1234567891);
            passed &= VerifyInt64Parse("-1234567891", -1234567891);
            passed &= VerifyInt64Parse("123456789123456789", 123456789123456789);
            passed &= VerifyInt64Parse("-123456789123456789", -123456789123456789);
            passed &= VerifyInt64Parse("5", NumberStyles.Integer, CultureInfo.InvariantCulture, 5);
            passed &= VerifyInt64Parse("-5", NumberStyles.Integer, CultureInfo.InvariantCulture, -5);
            passed &= VerifyInt64Parse("5  \0", NumberStyles.Integer, CultureInfo.InvariantCulture, 5);
            passed &= VerifyInt64Parse("5\0\0\0", NumberStyles.Integer, CultureInfo.InvariantCulture, 5);
            passed &= VerifyInt64Parse("12", NumberStyles.HexNumber, CultureInfo.InvariantCulture, 0x12);
            passed &= VerifyInt64Parse("fF", NumberStyles.HexNumber, CultureInfo.InvariantCulture, 0xFF);
            passed &= VerifyInt64Parse("FFFFFFFF", NumberStyles.HexNumber, CultureInfo.InvariantCulture, 0xFFFFFFFF);
            passed &= VerifyInt64Parse("FFFFFFFFFFFFFFFF", NumberStyles.HexNumber, CultureInfo.InvariantCulture, -1);
            passed &= VerifyInt64Parse("123", NumberStyles.Integer, germanCulture, 123);
            passed &= VerifyInt64Parse("123", NumberStyles.Integer, japaneseCulture, 123);
            passed &= VerifyInt64Parse("123.000", NumberStyles.Any, germanCulture, 123000);
            passed &= VerifyInt64Parse("123,000", NumberStyles.Any, japaneseCulture, 123000);
            passed &= VerifyInt64Parse("123,000", NumberStyles.AllowDecimalPoint, germanCulture, 123);
            passed &= VerifyInt64Parse("123.000", NumberStyles.AllowDecimalPoint, japaneseCulture, 123);
            passed &= VerifyInt64Parse("5,00 " + germanCulture.NumberFormat.CurrencySymbol, NumberStyles.Any, germanCulture, 5); // currency
            passed &= VerifyInt64Parse("5", NumberStyles.Integer, goodNFI, 5);
            passed &= VerifyInt64Parse("5.0", NumberStyles.AllowDecimalPoint, goodNFI, 5);
            //
            passed &= VerifyInt64Parse("5", NumberStyles.Integer, corruptNFI, 5);
            passed &= VerifyInt64Parse("5", NumberStyles.Number, corruptNFI, 5);
            passed &= VerifyInt64Parse("5.0", NumberStyles.Number, corruptNFI, 5);
            passed &= VerifyInt64ParseException("5,0", NumberStyles.Number, corruptNFI, typeof(FormatException));
            passed &= VerifyInt64ParseException("5.0.0", NumberStyles.Number, corruptNFI, typeof(FormatException));
            passed &= VerifyInt64Parse("$5.0", NumberStyles.Currency, corruptNFI, 5);
            passed &= VerifyInt64ParseException("$5,0", NumberStyles.Currency, corruptNFI, typeof(FormatException));
            passed &= VerifyInt64ParseException("$5.0.0", NumberStyles.Currency, corruptNFI, typeof(FormatException));
            passed &= VerifyInt64Parse("5.0", NumberStyles.Currency, corruptNFI, 5);
            passed &= VerifyInt64ParseException("5,0", NumberStyles.Currency, corruptNFI, typeof(FormatException));
            passed &= VerifyInt64ParseException("5.0.0", NumberStyles.Currency, corruptNFI, typeof(FormatException));
            passed &= VerifyInt64Parse("5.0", NumberStyles.Any, corruptNFI, 5);
            passed &= VerifyInt64ParseException("5,0", NumberStyles.Any, corruptNFI, typeof(FormatException));
            passed &= VerifyInt64ParseException("5.0.0", NumberStyles.Any, corruptNFI, typeof(FormatException));
            //
            passed &= VerifyInt64Parse("5", NumberStyles.Integer, swappedNFI, 5);
            passed &= VerifyInt64ParseException("1,234", NumberStyles.Integer, swappedNFI, typeof(FormatException));
            passed &= VerifyInt64Parse("5", NumberStyles.Number, swappedNFI, 5);
            passed &= VerifyInt64Parse("5.0", NumberStyles.Number, swappedNFI, 5);
            passed &= VerifyInt64Parse("1,234", NumberStyles.Number, swappedNFI, 1234);
            passed &= VerifyInt64Parse("1,234.0", NumberStyles.Number, swappedNFI, 1234);
            passed &= VerifyInt64ParseException("5.000.000", NumberStyles.Number, swappedNFI, typeof(FormatException));
            passed &= VerifyInt64ParseException("5.000,00", NumberStyles.Number, swappedNFI, typeof(FormatException));
            passed &= VerifyInt64Parse("5.000", NumberStyles.Currency, swappedNFI, 5); //???
            passed &= VerifyInt64ParseException("5.000,00", NumberStyles.Currency, swappedNFI, typeof(FormatException)); //???
            passed &= VerifyInt64Parse("$5.000", NumberStyles.Currency, swappedNFI, 5000);
            passed &= VerifyInt64Parse("$5.000,00", NumberStyles.Currency, swappedNFI, 5000);
            passed &= VerifyInt64Parse("5.000", NumberStyles.Any, swappedNFI, 5);  //?
            passed &= VerifyInt64ParseException("5.000,00", NumberStyles.Any, swappedNFI, typeof(FormatException));  //?
            passed &= VerifyInt64Parse("$5.000", NumberStyles.Any, swappedNFI, 5000);
            passed &= VerifyInt64Parse("$5.000,00", NumberStyles.Any, swappedNFI, 5000);
            passed &= VerifyInt64Parse("5,0", NumberStyles.Currency, swappedNFI, 5);
            passed &= VerifyInt64Parse("$5,0", NumberStyles.Currency, swappedNFI, 5);
            passed &= VerifyInt64Parse("5,000", NumberStyles.Currency, swappedNFI, 5);
            passed &= VerifyInt64Parse("$5,000", NumberStyles.Currency, swappedNFI, 5);
            passed &= VerifyInt64ParseException("5,000.0", NumberStyles.Currency, swappedNFI, typeof(FormatException));
            passed &= VerifyInt64ParseException("$5,000.0", NumberStyles.Currency, swappedNFI, typeof(FormatException));
            passed &= VerifyInt64Parse("5,000", NumberStyles.Any, swappedNFI, 5);
            passed &= VerifyInt64Parse("$5,000", NumberStyles.Any, swappedNFI, 5);
            passed &= VerifyInt64ParseException("5,000.0", NumberStyles.Any, swappedNFI, typeof(FormatException));
            passed &= VerifyInt64ParseException("$5,000.0", NumberStyles.Any, swappedNFI, typeof(FormatException));
            //
            passed &= VerifyInt64Parse("5.0", NumberStyles.Number, distinctNFI, 5);
            passed &= VerifyInt64Parse("1,234.0", NumberStyles.Number, distinctNFI, 1234);
            passed &= VerifyInt64Parse("5.0", NumberStyles.Currency, distinctNFI, 5);
            passed &= VerifyInt64Parse("1,234.0", NumberStyles.Currency, distinctNFI, 1234);
            passed &= VerifyInt64Parse("5.0", NumberStyles.Any, distinctNFI, 5);
            passed &= VerifyInt64Parse("1,234.0", NumberStyles.Any, distinctNFI, 1234);
            passed &= VerifyInt64ParseException("$5.0", NumberStyles.Currency, distinctNFI, typeof(FormatException));
            passed &= VerifyInt64ParseException("$5.0", NumberStyles.Any, distinctNFI, typeof(FormatException));
            passed &= VerifyInt64ParseException("5:0", NumberStyles.Number, distinctNFI, typeof(FormatException));
            passed &= VerifyInt64ParseException("5;0", NumberStyles.Number, distinctNFI, typeof(FormatException));
            passed &= VerifyInt64ParseException("$5:0", NumberStyles.Number, distinctNFI, typeof(FormatException));
            passed &= VerifyInt64Parse("5:0", NumberStyles.Currency, distinctNFI, 5);
            passed &= VerifyInt64Parse("5:000", NumberStyles.Currency, distinctNFI, 5);
            passed &= VerifyInt64Parse("5;000", NumberStyles.Currency, distinctNFI, 5000);
            passed &= VerifyInt64Parse("$5:0", NumberStyles.Currency, distinctNFI, 5);
            passed &= VerifyInt64Parse("$5;0", NumberStyles.Currency, distinctNFI, 50);
            passed &= VerifyInt64Parse("5:0", NumberStyles.Any, distinctNFI, 5);
            passed &= VerifyInt64Parse("5;0", NumberStyles.Any, distinctNFI, 50);
            passed &= VerifyInt64Parse("$5:0", NumberStyles.Any, distinctNFI, 5);
            passed &= VerifyInt64Parse("$5;0", NumberStyles.Any, distinctNFI, 50);
            passed &= VerifyInt64ParseException("123,456;789.0", NumberStyles.Number, distinctNFI, typeof(FormatException));
            passed &= VerifyInt64Parse("123,456;789.0", NumberStyles.Currency, distinctNFI, 123456789);
            passed &= VerifyInt64Parse("123,456;789.0", NumberStyles.Any, distinctNFI, 123456789);
            passed &= VerifyInt64ParseException("$123,456;789.0", NumberStyles.Any, distinctNFI, typeof(FormatException));
            //
            passed &= VerifyInt64ParseException("123456789123456789123", typeof(OverflowException));
            passed &= VerifyInt64ParseException("-123456789123456789123", typeof(OverflowException));
            passed &= VerifyInt64ParseException("18446744073709551615", typeof(OverflowException));
            passed &= VerifyInt64ParseException("Garbage", typeof(FormatException));
            passed &= VerifyInt64ParseException("5\0Garbage", typeof(FormatException));
            passed &= VerifyInt64ParseException(null, typeof(ArgumentNullException));
            passed &= VerifyInt64ParseException("123456789123456789", NumberStyles.HexNumber, CultureInfo.InvariantCulture, typeof(OverflowException));
            passed &= VerifyInt64ParseException("5.3", NumberStyles.AllowDecimalPoint, goodNFI, typeof(OverflowException));  //weird that it's Overflow, but consistent with v1
            passed &= VerifyInt64ParseException("5", NumberStyles.AllowHexSpecifier | NumberStyles.AllowParentheses, null, typeof(ArgumentException));
            passed &= VerifyInt64ParseException("4", (NumberStyles)(-1), typeof(ArgumentException));
            passed &= VerifyInt64ParseException("4", (NumberStyles)0x10000, typeof(ArgumentException));
            passed &= VerifyInt64ParseException("4", (NumberStyles)(-1), CultureInfo.InvariantCulture, typeof(ArgumentException));
            passed &= VerifyInt64ParseException("4", (NumberStyles)0x10000, CultureInfo.InvariantCulture, typeof(ArgumentException));
            passed &= VerifyInt64ParseException("123.000.000.000.000.000.000", NumberStyles.Any, germanCulture, typeof(OverflowException));
            passed &= VerifyInt64ParseException("123,000,000,000,000,000,000", NumberStyles.Any, japaneseCulture, typeof(OverflowException));
            passed &= VerifyInt64ParseException("123,000,000,000,000,000,000", NumberStyles.Integer, germanCulture, typeof(FormatException));
            passed &= VerifyInt64ParseException("123.000.000.000.000.000.000", NumberStyles.Integer, japaneseCulture, typeof(FormatException));
            passed &= VerifyInt64ParseException("5,00 " + germanCulture.NumberFormat.CurrencySymbol, NumberStyles.Integer, germanCulture, typeof(FormatException)); // currency


            /////////// TryParse(String)
            //// Pass cases
            passed &= VerifyInt64TryParse("5", 5, true);
            passed &= VerifyInt64TryParse("-5", -5, true);
            passed &= VerifyInt64TryParse(" 5 ", 5, true);
            passed &= VerifyInt64TryParse("5\0", 5, true);
            passed &= VerifyInt64TryParse("5  \0", 5, true);
            passed &= VerifyInt64TryParse("5\0\0\0", 5, true);
            passed &= VerifyInt64TryParse("10000", 10000, true);
            passed &= VerifyInt64TryParse("893382737", 893382737, true);
            passed &= VerifyInt64TryParse("-893382737", -893382737, true);
            passed &= VerifyInt64TryParse("1234567891", 1234567891, true);
            passed &= VerifyInt64TryParse("-1234567891", -1234567891, true);
            passed &= VerifyInt64TryParse("123456789123456789", 123456789123456789, true);
            passed &= VerifyInt64TryParse("-123456789123456789", -123456789123456789, true);
            passed &= VerifyInt64TryParse(Int64.MaxValue.ToString(), Int64.MaxValue, true);
            passed &= VerifyInt64TryParse(Int64.MinValue.ToString(), Int64.MinValue, true);

            //// Fail cases
            passed &= VerifyInt64TryParse(null, 0, false);
            passed &= VerifyInt64TryParse("", 0, false);
            passed &= VerifyInt64TryParse("Garbage", 0, false);
            passed &= VerifyInt64TryParse("5\0Garbage", 0, false);
            passed &= VerifyInt64TryParse("18446744073709551615", 0, false);
            passed &= VerifyInt64TryParse("123456789123456789123", 0, false);
            passed &= VerifyInt64TryParse("-123456789123456789123", 0, false);
            passed &= VerifyInt64TryParse("FF", 0, false);
            passed &= VerifyInt64TryParse("27.3", 0, false);
            passed &= VerifyInt64TryParse("23 5", 0, false);


            /////////// TryParse(TryParse(String, NumberStyles, IFormatProvider, ref Int64)
            //// Pass cases
            passed &= VerifyInt64TryParse("5", NumberStyles.Integer, CultureInfo.InvariantCulture, 5, true);
            passed &= VerifyInt64TryParse("-5", NumberStyles.Integer, CultureInfo.InvariantCulture, -5, true);
            // Variations on NumberStyles
            passed &= VerifyInt64TryParse("12", NumberStyles.HexNumber, CultureInfo.InvariantCulture, 0x12, true);
            passed &= VerifyInt64TryParse("FF", NumberStyles.HexNumber, CultureInfo.InvariantCulture, 0xFF, true);
            passed &= VerifyInt64TryParse("fF", NumberStyles.HexNumber, CultureInfo.InvariantCulture, 0xFF, true);
            passed &= VerifyInt64TryParse("FFFFFFFF", NumberStyles.HexNumber, CultureInfo.InvariantCulture, 0xFFFFFFFF, true);
            passed &= VerifyInt64TryParse("FFFFFFFFFFFFFFFF", NumberStyles.HexNumber, CultureInfo.InvariantCulture, -1, true);
            passed &= VerifyInt64TryParse("   5", NumberStyles.AllowLeadingWhite, goodNFI, 5, true);
            passed &= VerifyInt64TryParse("5", NumberStyles.Number, goodNFI, 5, true);
            passed &= VerifyInt64TryParse("5.0", NumberStyles.AllowDecimalPoint, goodNFI, 5, true);
            // Variations on IFP
            passed &= VerifyInt64TryParse("5", NumberStyles.Integer, goodNFI, 5, true);
            passed &= VerifyInt64TryParse("5", NumberStyles.Integer, null, 5, true);
            passed &= VerifyInt64TryParse("5", NumberStyles.Integer, new DateTimeFormatInfo(), 5, true);
            passed &= VerifyInt64TryParse("^42", NumberStyles.Any, customNFI, -42, true);
            passed &= VerifyInt64TryParse("123", NumberStyles.Integer, germanCulture, 123, true);
            passed &= VerifyInt64TryParse("123", NumberStyles.Integer, japaneseCulture, 123, true);
            passed &= VerifyInt64TryParse("123.000", NumberStyles.Any, germanCulture, 123000, true);
            passed &= VerifyInt64TryParse("123,000", NumberStyles.Any, japaneseCulture, 123000, true);
            passed &= VerifyInt64TryParse("123,000", NumberStyles.AllowDecimalPoint, germanCulture, 123, true);
            passed &= VerifyInt64TryParse("123.000", NumberStyles.AllowDecimalPoint, japaneseCulture, 123, true);
            passed &= VerifyInt64TryParse("5,00 " + germanCulture.NumberFormat.CurrencySymbol, NumberStyles.Any, germanCulture, 5, true); // currency

            //// Fail cases
            passed &= VerifyInt64TryParse("FF", NumberStyles.Integer, CultureInfo.InvariantCulture, 0, false);
            passed &= VerifyInt64TryParse("123456789123456789", NumberStyles.HexNumber, CultureInfo.InvariantCulture, 0, false);
            passed &= VerifyInt64TryParse("-42", NumberStyles.Any, customNFI, 0, false);
            passed &= VerifyInt64TryParse("5.3", NumberStyles.AllowDecimalPoint, goodNFI, 0, false);
            passed &= VerifyInt64TryParse("5   ", NumberStyles.AllowLeadingWhite, goodNFI, 0, false);
            passed &= VerifyInt64TryParse("123.000.000.000.000.000.000", NumberStyles.Any, germanCulture, 0, false);
            passed &= VerifyInt64TryParse("123,000,000,000,000,000,000", NumberStyles.Any, japaneseCulture, 0, false);
            passed &= VerifyInt64TryParse("123,000,000,000,000,000,000", NumberStyles.Integer, germanCulture, 0, false);
            passed &= VerifyInt64TryParse("123.000.000.000.000.000.000", NumberStyles.Integer, japaneseCulture, 0, false);

            //// Exception cases
            passed &= VerifyInt64TryParseException("5", NumberStyles.AllowHexSpecifier | NumberStyles.AllowParentheses, null, typeof(ArgumentException));
            passed &= VerifyInt64TryParseException("4", (NumberStyles)(-1), CultureInfo.InvariantCulture, typeof(ArgumentException));
            passed &= VerifyInt64TryParseException("4", (NumberStyles)0x10000, CultureInfo.InvariantCulture, typeof(ArgumentException));

            // NumberStyles/NFI variations
            //
            passed &= VerifyInt64TryParse("5", NumberStyles.Integer, corruptNFI, 5, true);
            passed &= VerifyInt64TryParse("5", NumberStyles.Number, corruptNFI, 5, true);
            passed &= VerifyInt64TryParse("5.0", NumberStyles.Number, corruptNFI, 5, true);
            passed &= VerifyInt64TryParse("5,0", NumberStyles.Number, corruptNFI, 0, false);
            passed &= VerifyInt64TryParse("5.0.0", NumberStyles.Number, corruptNFI, 0, false);
            passed &= VerifyInt64TryParse("$5.0", NumberStyles.Currency, corruptNFI, 5, true);
            passed &= VerifyInt64TryParse("$5,0", NumberStyles.Currency, corruptNFI, 0, false);
            passed &= VerifyInt64TryParse("$5.0.0", NumberStyles.Currency, corruptNFI, 0, false);
            passed &= VerifyInt64TryParse("5.0", NumberStyles.Currency, corruptNFI, 5, true);
            passed &= VerifyInt64TryParse("5,0", NumberStyles.Currency, corruptNFI, 0, false);
            passed &= VerifyInt64TryParse("5.0.0", NumberStyles.Currency, corruptNFI, 0, false);
            passed &= VerifyInt64TryParse("5.0", NumberStyles.Any, corruptNFI, 5, true);
            passed &= VerifyInt64TryParse("5,0", NumberStyles.Any, corruptNFI, 0, false);
            passed &= VerifyInt64TryParse("5.0.0", NumberStyles.Any, corruptNFI, 0, false);
            //
            passed &= VerifyInt64TryParse("5", NumberStyles.Integer, swappedNFI, 5, true);
            passed &= VerifyInt64TryParse("1,234", NumberStyles.Integer, swappedNFI, 0, false);
            passed &= VerifyInt64TryParse("5", NumberStyles.Number, swappedNFI, 5, true);
            passed &= VerifyInt64TryParse("5.0", NumberStyles.Number, swappedNFI, 5, true);
            passed &= VerifyInt64TryParse("1,234", NumberStyles.Number, swappedNFI, 1234, true);
            passed &= VerifyInt64TryParse("1,234.0", NumberStyles.Number, swappedNFI, 1234, true);
            passed &= VerifyInt64TryParse("5.000.000", NumberStyles.Number, swappedNFI, 0, false);
            passed &= VerifyInt64TryParse("5.000,00", NumberStyles.Number, swappedNFI, 0, false);
            passed &= VerifyInt64TryParse("5.000", NumberStyles.Currency, swappedNFI, 5, true); //???
            passed &= VerifyInt64TryParse("5.000,00", NumberStyles.Currency, swappedNFI, 0, false); //???
            passed &= VerifyInt64TryParse("$5.000", NumberStyles.Currency, swappedNFI, 5000, true);
            passed &= VerifyInt64TryParse("$5.000,00", NumberStyles.Currency, swappedNFI, 5000, true);
            passed &= VerifyInt64TryParse("5.000", NumberStyles.Any, swappedNFI, 5, true);  //?
            passed &= VerifyInt64TryParse("5.000,00", NumberStyles.Any, swappedNFI, 0, false);  //?
            passed &= VerifyInt64TryParse("$5.000", NumberStyles.Any, swappedNFI, 5000, true);
            passed &= VerifyInt64TryParse("$5.000,00", NumberStyles.Any, swappedNFI, 5000, true);
            passed &= VerifyInt64TryParse("5,0", NumberStyles.Currency, swappedNFI, 5, true);
            passed &= VerifyInt64TryParse("$5,0", NumberStyles.Currency, swappedNFI, 5, true);
            passed &= VerifyInt64TryParse("5,000", NumberStyles.Currency, swappedNFI, 5, true);
            passed &= VerifyInt64TryParse("$5,000", NumberStyles.Currency, swappedNFI, 5, true);
            passed &= VerifyInt64TryParse("5,000.0", NumberStyles.Currency, swappedNFI, 0, false);
            passed &= VerifyInt64TryParse("$5,000.0", NumberStyles.Currency, swappedNFI, 0, false);
            passed &= VerifyInt64TryParse("5,000", NumberStyles.Any, swappedNFI, 5, true);
            passed &= VerifyInt64TryParse("$5,000", NumberStyles.Any, swappedNFI, 5, true);
            passed &= VerifyInt64TryParse("5,000.0", NumberStyles.Any, swappedNFI, 0, false);
            passed &= VerifyInt64TryParse("$5,000.0", NumberStyles.Any, swappedNFI, 0, false);
            //
            passed &= VerifyInt64TryParse("5.0", NumberStyles.Number, distinctNFI, 5, true);
            passed &= VerifyInt64TryParse("1,234.0", NumberStyles.Number, distinctNFI, 1234, true);
            passed &= VerifyInt64TryParse("5.0", NumberStyles.Currency, distinctNFI, 5, true);
            passed &= VerifyInt64TryParse("1,234.0", NumberStyles.Currency, distinctNFI, 1234, true);
            passed &= VerifyInt64TryParse("5.0", NumberStyles.Any, distinctNFI, 5, true);
            passed &= VerifyInt64TryParse("1,234.0", NumberStyles.Any, distinctNFI, 1234, true);
            passed &= VerifyInt64TryParse("$5.0", NumberStyles.Currency, distinctNFI, 0, false);
            passed &= VerifyInt64TryParse("$5.0", NumberStyles.Any, distinctNFI, 0, false);
            passed &= VerifyInt64TryParse("5:0", NumberStyles.Number, distinctNFI, 0, false);
            passed &= VerifyInt64TryParse("5;0", NumberStyles.Number, distinctNFI, 0, false);
            passed &= VerifyInt64TryParse("$5:0", NumberStyles.Number, distinctNFI, 0, false);
            passed &= VerifyInt64TryParse("5:0", NumberStyles.Currency, distinctNFI, 5, true);
            passed &= VerifyInt64TryParse("5:000", NumberStyles.Currency, distinctNFI, 5, true);
            passed &= VerifyInt64TryParse("5;000", NumberStyles.Currency, distinctNFI, 5000, true);
            passed &= VerifyInt64TryParse("$5:0", NumberStyles.Currency, distinctNFI, 5, true);
            passed &= VerifyInt64TryParse("$5;0", NumberStyles.Currency, distinctNFI, 50, true);
            passed &= VerifyInt64TryParse("5:0", NumberStyles.Any, distinctNFI, 5, true);
            passed &= VerifyInt64TryParse("5;0", NumberStyles.Any, distinctNFI, 50, true);
            passed &= VerifyInt64TryParse("$5:0", NumberStyles.Any, distinctNFI, 5, true);
            passed &= VerifyInt64TryParse("$5;0", NumberStyles.Any, distinctNFI, 50, true);
            passed &= VerifyInt64TryParse("123,456;789.0", NumberStyles.Number, distinctNFI, 0, false);
            passed &= VerifyInt64TryParse("123,456;789.0", NumberStyles.Currency, distinctNFI, 123456789, true);
            passed &= VerifyInt64TryParse("123,456;789.0", NumberStyles.Any, distinctNFI, 123456789, true);
            passed &= VerifyInt64TryParse("$123,456;789.0", NumberStyles.Any, distinctNFI, 0, false);


            // Should these pass or fail?  Current parse behavior is to pass, so they might be
            // parse bugs, but they're not tryparse bugs.
            passed &= VerifyInt64Parse("5", NumberStyles.Float, goodNFI, 5);
            passed &= VerifyInt64TryParse("5", NumberStyles.Float, goodNFI, 5, true);
            passed &= VerifyInt64Parse("5", NumberStyles.AllowDecimalPoint, goodNFI, 5);
            passed &= VerifyInt64TryParse("5", NumberStyles.AllowDecimalPoint, goodNFI, 5, true);
            // I expect ArgumentException with an ambiguous NFI
            passed &= VerifyInt64Parse("^42", NumberStyles.Any, ambigNFI, -42);
            passed &= VerifyInt64TryParse("^42", NumberStyles.Any, ambigNFI, -42, true);

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

    public static bool VerifyInt64TryParse(string value, Int64 expectedResult, bool expectedReturn)
    {
        if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: Int64.TryParse, Value = '{0}', Expected Result = {1}, Expected Return = {2}",
                value, expectedResult, expectedReturn);
        }
        Int64 result = 0;
        try
        {
            bool returnValue = Int64.TryParse(value, out result);
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

    public static bool VerifyInt64TryParse(string value, NumberStyles style, IFormatProvider provider, Int64 expectedResult, bool expectedReturn)
    {
		if (provider == null) return true;

		if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: Int64.TryParse, Value = '{0}', Style = {1}, Provider = {2}, Expected Result = {3}, Expected Return = {4}",
                  value, style, provider, expectedResult, expectedReturn);
        }
        Int64 result = 0;
        try
        {
            bool returnValue = Int64.TryParse(value, style, provider, out result);
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

    public static bool VerifyInt64TryParseException(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
    {
		if (provider == null) return true;

		if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: Int64.TryParse, Value = '{0}', Style = {1}, Provider = {2}, Expected Exception = {3}",
                value, style, provider, exceptionType);
        }
        try
        {
            Int64 result = 0;
            Boolean returnValue = Int64.TryParse(value, style, provider, out result);
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

    public static bool VerifyInt64Parse(string value, Int64 expectedResult)
    {
        if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: Int64.Parse, Value = '{0}', Expected Result, {1}",
                value, expectedResult);
        }
        try
        {
            Int64 returnValue = Int64.Parse(value);
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

    public static bool VerifyInt64Parse(string value, NumberStyles style, IFormatProvider provider, Int64 expectedResult)
    {
		if (provider == null) return true;

		if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: Int64.Parse, Value = '{0}', Style = {1}, provider = {2}, Expected Result = {3}",
                value, style, provider, expectedResult);
        }
        try
        {
            Int64 returnValue = Int64.Parse(value, style, provider);
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

    public static bool VerifyInt64ParseException(string value, Type exceptionType)
    {
        if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: Int64.Parse, Value = '{0}', Expected Exception, {1}",
                value, exceptionType);
        }
        try
        {
            Int64 returnValue = Int64.Parse(value);
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

    public static bool VerifyInt64ParseException(string value, NumberStyles style, Type exceptionType)
    {
        if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: Int64.Parse, Value = '{0}', Style = {1}, Expected Exception = {3}",
                value, style, exceptionType);
        }
        try
        {
            Int64 returnValue = Int64.Parse(value, style);
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

    public static bool VerifyInt64ParseException(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
    {
		if (provider == null) return true;

		if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: Int64.Parse, Value = '{0}', Style = {1}, Provider = {2}, Expected Exception = {3}",
                value, style, provider, exceptionType);
        }
        try
        {
            Int64 returnValue = Int64.Parse(value, style, provider);
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
