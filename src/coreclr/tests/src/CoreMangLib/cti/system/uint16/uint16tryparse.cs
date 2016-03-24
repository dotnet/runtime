// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

// CoreCLR Port from Co7526TryParse_all.cs
// Tests UInt16.TryParse(String), UInt16.TryParse(String, NumberStyles, IFormatProvider, ref UInt16)
// 2003/04/01  KatyK
// 2007/06/28  adapted by MarielY

public class Co7526TryParse
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
            passed &= VerifyUInt16Parse("5", 5);
            passed &= VerifyUInt16Parse("5  ", 5);
            passed &= VerifyUInt16Parse("5\0", 5);
            passed &= VerifyUInt16Parse("10000", 10000);
            passed &= VerifyUInt16Parse("50000", 50000);
            passed &= VerifyUInt16Parse("5", NumberStyles.Integer, CultureInfo.InvariantCulture, 5);
            passed &= VerifyUInt16Parse("5  \0", NumberStyles.Integer, CultureInfo.InvariantCulture, 5);
            passed &= VerifyUInt16Parse("5\0\0\0", NumberStyles.Integer, CultureInfo.InvariantCulture, 5);
            passed &= VerifyUInt16Parse("12", NumberStyles.HexNumber, CultureInfo.InvariantCulture, 0x12);
            passed &= VerifyUInt16Parse("fF", NumberStyles.HexNumber, CultureInfo.InvariantCulture, 0xFF);
            passed &= VerifyUInt16Parse("fFFF", NumberStyles.HexNumber, CultureInfo.InvariantCulture, 0xFFFF);
            passed &= VerifyUInt16Parse("5", NumberStyles.Integer, goodNFI, 5);
            passed &= VerifyUInt16Parse("5.0", NumberStyles.AllowDecimalPoint, goodNFI, 5);
            passed &= VerifyUInt16Parse("123", NumberStyles.Integer, germanCulture, 123);
            passed &= VerifyUInt16Parse("123", NumberStyles.Integer, japaneseCulture, 123);
            passed &= VerifyUInt16Parse("123,000", NumberStyles.AllowDecimalPoint, germanCulture, 123);
            passed &= VerifyUInt16Parse("123.000", NumberStyles.AllowDecimalPoint, japaneseCulture, 123);
            passed &= VerifyUInt16Parse("5,00 " + germanCulture.NumberFormat.CurrencySymbol, NumberStyles.Any, germanCulture, 5); // currency
            //
            passed &= VerifyUInt16Parse("5", NumberStyles.Integer, corruptNFI, 5);
            passed &= VerifyUInt16Parse("5", NumberStyles.Number, corruptNFI, 5);
            passed &= VerifyUInt16Parse("5.0", NumberStyles.Number, corruptNFI, 5);
            passed &= VerifyUInt16ParseException("5,0", NumberStyles.Number, corruptNFI, typeof(FormatException));
            passed &= VerifyUInt16ParseException("5.0.0", NumberStyles.Number, corruptNFI, typeof(FormatException));
            passed &= VerifyUInt16Parse("$5.0", NumberStyles.Currency, corruptNFI, 5);
            passed &= VerifyUInt16ParseException("$5,0", NumberStyles.Currency, corruptNFI, typeof(FormatException));
            passed &= VerifyUInt16ParseException("$5.0.0", NumberStyles.Currency, corruptNFI, typeof(FormatException));
            passed &= VerifyUInt16Parse("5.0", NumberStyles.Currency, corruptNFI, 5);
            passed &= VerifyUInt16ParseException("5,0", NumberStyles.Currency, corruptNFI, typeof(FormatException));
            passed &= VerifyUInt16ParseException("5.0.0", NumberStyles.Currency, corruptNFI, typeof(FormatException));
            passed &= VerifyUInt16Parse("5.0", NumberStyles.Any, corruptNFI, 5);
            passed &= VerifyUInt16ParseException("5,0", NumberStyles.Any, corruptNFI, typeof(FormatException));
            passed &= VerifyUInt16ParseException("5.0.0", NumberStyles.Any, corruptNFI, typeof(FormatException));
            //
            passed &= VerifyUInt16Parse("5", NumberStyles.Integer, swappedNFI, 5);
            passed &= VerifyUInt16ParseException("1,234", NumberStyles.Integer, swappedNFI, typeof(FormatException));
            passed &= VerifyUInt16Parse("5", NumberStyles.Number, swappedNFI, 5);
            passed &= VerifyUInt16Parse("5.0", NumberStyles.Number, swappedNFI, 5);
            passed &= VerifyUInt16Parse("1,234", NumberStyles.Number, swappedNFI, 1234);
            passed &= VerifyUInt16Parse("1,234.0", NumberStyles.Number, swappedNFI, 1234);
            passed &= VerifyUInt16ParseException("5.000.000", NumberStyles.Number, swappedNFI, typeof(FormatException));
            passed &= VerifyUInt16ParseException("5.000,00", NumberStyles.Number, swappedNFI, typeof(FormatException));
            passed &= VerifyUInt16Parse("5.000", NumberStyles.Currency, swappedNFI, 5); //???
            passed &= VerifyUInt16ParseException("5.000,00", NumberStyles.Currency, swappedNFI, typeof(FormatException)); //???
            passed &= VerifyUInt16Parse("$5.000", NumberStyles.Currency, swappedNFI, 5000);
            passed &= VerifyUInt16Parse("$5.000,00", NumberStyles.Currency, swappedNFI, 5000);
            passed &= VerifyUInt16Parse("5.000", NumberStyles.Any, swappedNFI, 5);  //?
            passed &= VerifyUInt16ParseException("5.000,00", NumberStyles.Any, swappedNFI, typeof(FormatException));  //?
            passed &= VerifyUInt16Parse("$5.000", NumberStyles.Any, swappedNFI, 5000);
            passed &= VerifyUInt16Parse("$5.000,00", NumberStyles.Any, swappedNFI, 5000);
            passed &= VerifyUInt16Parse("5,0", NumberStyles.Currency, swappedNFI, 5);
            passed &= VerifyUInt16Parse("$5,0", NumberStyles.Currency, swappedNFI, 5);
            passed &= VerifyUInt16Parse("5,000", NumberStyles.Currency, swappedNFI, 5);
            passed &= VerifyUInt16Parse("$5,000", NumberStyles.Currency, swappedNFI, 5);
            passed &= VerifyUInt16ParseException("5,000.0", NumberStyles.Currency, swappedNFI, typeof(FormatException));
            passed &= VerifyUInt16ParseException("$5,000.0", NumberStyles.Currency, swappedNFI, typeof(FormatException));
            passed &= VerifyUInt16Parse("5,000", NumberStyles.Any, swappedNFI, 5);
            passed &= VerifyUInt16Parse("$5,000", NumberStyles.Any, swappedNFI, 5);
            passed &= VerifyUInt16ParseException("5,000.0", NumberStyles.Any, swappedNFI, typeof(FormatException));
            passed &= VerifyUInt16ParseException("$5,000.0", NumberStyles.Any, swappedNFI, typeof(FormatException));
            //
            passed &= VerifyUInt16Parse("5.0", NumberStyles.Number, distinctNFI, 5);
            passed &= VerifyUInt16Parse("1,234.0", NumberStyles.Number, distinctNFI, 1234);
            passed &= VerifyUInt16Parse("5.0", NumberStyles.Currency, distinctNFI, 5);
            passed &= VerifyUInt16Parse("1,234.0", NumberStyles.Currency, distinctNFI, 1234);
            passed &= VerifyUInt16Parse("5.0", NumberStyles.Any, distinctNFI, 5);
            passed &= VerifyUInt16Parse("1,234.0", NumberStyles.Any, distinctNFI, 1234);
            passed &= VerifyUInt16ParseException("$5.0", NumberStyles.Currency, distinctNFI, typeof(FormatException));
            passed &= VerifyUInt16ParseException("$5.0", NumberStyles.Any, distinctNFI, typeof(FormatException));
            passed &= VerifyUInt16ParseException("5:0", NumberStyles.Number, distinctNFI, typeof(FormatException));
            passed &= VerifyUInt16ParseException("5;0", NumberStyles.Number, distinctNFI, typeof(FormatException));
            passed &= VerifyUInt16ParseException("$5:0", NumberStyles.Number, distinctNFI, typeof(FormatException));
            passed &= VerifyUInt16Parse("5:0", NumberStyles.Currency, distinctNFI, 5);
            passed &= VerifyUInt16Parse("5:000", NumberStyles.Currency, distinctNFI, 5);
            passed &= VerifyUInt16Parse("5;000", NumberStyles.Currency, distinctNFI, 5000);
            passed &= VerifyUInt16Parse("$5:0", NumberStyles.Currency, distinctNFI, 5);
            passed &= VerifyUInt16Parse("$5;0", NumberStyles.Currency, distinctNFI, 50);
            passed &= VerifyUInt16Parse("5:0", NumberStyles.Any, distinctNFI, 5);
            passed &= VerifyUInt16Parse("5;0", NumberStyles.Any, distinctNFI, 50);
            passed &= VerifyUInt16Parse("$5:0", NumberStyles.Any, distinctNFI, 5);
            passed &= VerifyUInt16Parse("$5;0", NumberStyles.Any, distinctNFI, 50);
            passed &= VerifyUInt16ParseException("1,23;45.0", NumberStyles.Number, distinctNFI, typeof(FormatException));
            passed &= VerifyUInt16Parse("1,23;45.0", NumberStyles.Currency, distinctNFI, 12345);
            passed &= VerifyUInt16Parse("1,23;45.0", NumberStyles.Any, distinctNFI, 12345);
            passed &= VerifyUInt16ParseException("$1,23;45.0", NumberStyles.Any, distinctNFI, typeof(FormatException));
            //
            passed &= VerifyUInt16ParseException("-5", typeof(OverflowException));
            passed &= VerifyUInt16ParseException("100000", typeof(OverflowException));
            passed &= VerifyUInt16ParseException("Garbage", typeof(FormatException));
            passed &= VerifyUInt16ParseException("5\0Garbage", typeof(FormatException));
            passed &= VerifyUInt16ParseException(null, typeof(ArgumentNullException));
            passed &= VerifyUInt16ParseException("1FFFF", NumberStyles.HexNumber, CultureInfo.InvariantCulture, typeof(OverflowException));
            passed &= VerifyUInt16ParseException("FFFFFFFF", NumberStyles.HexNumber, CultureInfo.InvariantCulture, typeof(OverflowException));
            passed &= VerifyUInt16ParseException("5", NumberStyles.AllowHexSpecifier | NumberStyles.AllowParentheses, null, typeof(ArgumentException));
            passed &= VerifyUInt16ParseException("4", (NumberStyles)(-1), typeof(ArgumentException));
            passed &= VerifyUInt16ParseException("4", (NumberStyles)0x10000, typeof(ArgumentException));
            passed &= VerifyUInt16ParseException("4", (NumberStyles)(-1), CultureInfo.InvariantCulture, typeof(ArgumentException));
            passed &= VerifyUInt16ParseException("4", (NumberStyles)0x10000, CultureInfo.InvariantCulture, typeof(ArgumentException));
            passed &= VerifyUInt16ParseException("123.000", NumberStyles.Any, germanCulture, typeof(OverflowException));
            passed &= VerifyUInt16ParseException("123,000", NumberStyles.Any, japaneseCulture, typeof(OverflowException));
            passed &= VerifyUInt16ParseException("123,000", NumberStyles.Integer, germanCulture, typeof(FormatException));
            passed &= VerifyUInt16ParseException("123.000", NumberStyles.Integer, japaneseCulture, typeof(FormatException));
            passed &= VerifyUInt16ParseException("5,00 " + germanCulture.NumberFormat.CurrencySymbol, NumberStyles.Integer, germanCulture, typeof(FormatException)); // currency


            /////////// TryParse(String)
            //// Pass cases
            passed &= VerifyUInt16TryParse("5", 5, true);
            passed &= VerifyUInt16TryParse(" 5 ", 5, true);
            passed &= VerifyUInt16TryParse("5\0", 5, true);
            passed &= VerifyUInt16TryParse("5  \0", 5, true);
            passed &= VerifyUInt16TryParse("5\0\0\0", 5, true);
            passed &= VerifyUInt16TryParse("10000", 10000, true);
            passed &= VerifyUInt16TryParse("50000", 50000, true);
            passed &= VerifyUInt16TryParse(UInt16.MaxValue.ToString(), UInt16.MaxValue, true);
            passed &= VerifyUInt16TryParse(UInt16.MinValue.ToString(), UInt16.MinValue, true);

            //// Fail cases
            passed &= VerifyUInt16TryParse(null, 0, false);
            passed &= VerifyUInt16TryParse("", 0, false);
            passed &= VerifyUInt16TryParse("Garbage", 0, false);
            passed &= VerifyUInt16TryParse("5\0Garbage", 0, false);
            passed &= VerifyUInt16TryParse("-5", 0, false);
            passed &= VerifyUInt16TryParse("100000", 0, false);
            passed &= VerifyUInt16TryParse("-100000", 0, false);
            passed &= VerifyUInt16TryParse("FF", 0, false);
            passed &= VerifyUInt16TryParse("27.3", 0, false);
            passed &= VerifyUInt16TryParse("23 5", 0, false);


            /////////// TryParse(TryParse(String, NumberStyles, IFormatProvider, ref UInt16)
            //// Pass cases
            passed &= VerifyUInt16TryParse("5", NumberStyles.Integer, CultureInfo.InvariantCulture, 5, true);
            // Variations on NumberStyles
            passed &= VerifyUInt16TryParse("12", NumberStyles.HexNumber, CultureInfo.InvariantCulture, 0x12, true);
            passed &= VerifyUInt16TryParse("FF", NumberStyles.HexNumber, CultureInfo.InvariantCulture, 0xFF, true);
            passed &= VerifyUInt16TryParse("fF", NumberStyles.HexNumber, CultureInfo.InvariantCulture, 0xFF, true);
            passed &= VerifyUInt16TryParse("fFFF", NumberStyles.HexNumber, CultureInfo.InvariantCulture, 0xFFFF, true);
            passed &= VerifyUInt16TryParse("   5", NumberStyles.AllowLeadingWhite, goodNFI, 5, true);
            passed &= VerifyUInt16TryParse("5", NumberStyles.Number, goodNFI, 5, true);
            passed &= VerifyUInt16TryParse("5.0", NumberStyles.AllowDecimalPoint, goodNFI, 5, true);
            // Variations on IFP
            passed &= VerifyUInt16TryParse("5", NumberStyles.Integer, goodNFI, 5, true);
            passed &= VerifyUInt16TryParse("5", NumberStyles.Integer, null, 5, true);
            passed &= VerifyUInt16TryParse("5", NumberStyles.Integer, new DateTimeFormatInfo(), 5, true);
            passed &= VerifyUInt16TryParse("123", NumberStyles.Integer, germanCulture, 123, true);
            passed &= VerifyUInt16TryParse("123", NumberStyles.Integer, japaneseCulture, 123, true);
            passed &= VerifyUInt16TryParse("123,000", NumberStyles.AllowDecimalPoint, germanCulture, 123, true);
            passed &= VerifyUInt16TryParse("123.000", NumberStyles.AllowDecimalPoint, japaneseCulture, 123, true);
            passed &= VerifyUInt16TryParse("5,00 " + germanCulture.NumberFormat.CurrencySymbol, NumberStyles.Any, germanCulture, 5, true); // currency

            //// Fail cases
            passed &= VerifyUInt16TryParse("-5", NumberStyles.Integer, CultureInfo.InvariantCulture, 0, false);
            passed &= VerifyUInt16TryParse("FF", NumberStyles.Integer, CultureInfo.InvariantCulture, 0, false);
            passed &= VerifyUInt16TryParse("1FFFF", NumberStyles.HexNumber, CultureInfo.InvariantCulture, 0, false);
            passed &= VerifyUInt16TryParse("FFFFFFFF", NumberStyles.HexNumber, CultureInfo.InvariantCulture, 0, false);
            passed &= VerifyUInt16TryParse("^42", NumberStyles.Any, customNFI, 0, false);
            passed &= VerifyUInt16TryParse("-42", NumberStyles.Any, customNFI, 0, false);
            passed &= VerifyUInt16TryParse("5.3", NumberStyles.AllowDecimalPoint, goodNFI, 0, false);
            passed &= VerifyUInt16TryParse("5   ", NumberStyles.AllowLeadingWhite, goodNFI, 0, false);
            passed &= VerifyUInt16TryParse("123.000", NumberStyles.Any, germanCulture, 0, false);
            passed &= VerifyUInt16TryParse("123,000", NumberStyles.Any, japaneseCulture, 0, false);
            passed &= VerifyUInt16TryParse("123,000", NumberStyles.Integer, germanCulture, 0, false);
            passed &= VerifyUInt16TryParse("123.000", NumberStyles.Integer, japaneseCulture, 0, false);
            passed &= VerifyUInt16TryParse("5,00 " + germanCulture.NumberFormat.CurrencySymbol, NumberStyles.Integer, germanCulture, 0, false); // currency

            //// Exception cases
            passed &= VerifyUInt16TryParseException("5", NumberStyles.AllowHexSpecifier | NumberStyles.AllowParentheses, null, typeof(ArgumentException));
            passed &= VerifyUInt16TryParseException("4", (NumberStyles)(-1), CultureInfo.InvariantCulture, typeof(ArgumentException));
            passed &= VerifyUInt16TryParseException("4", (NumberStyles)0x10000, CultureInfo.InvariantCulture, typeof(ArgumentException));

            // NumberStyles/NFI variations
            //
            passed &= VerifyUInt16TryParse("5", NumberStyles.Integer, corruptNFI, 5, true);
            passed &= VerifyUInt16TryParse("5", NumberStyles.Number, corruptNFI, 5, true);
            passed &= VerifyUInt16TryParse("5.0", NumberStyles.Number, corruptNFI, 5, true);
            passed &= VerifyUInt16TryParse("5,0", NumberStyles.Number, corruptNFI, 0, false);
            passed &= VerifyUInt16TryParse("5.0.0", NumberStyles.Number, corruptNFI, 0, false);
            passed &= VerifyUInt16TryParse("$5.0", NumberStyles.Currency, corruptNFI, 5, true);
            passed &= VerifyUInt16TryParse("$5,0", NumberStyles.Currency, corruptNFI, 0, false);
            passed &= VerifyUInt16TryParse("$5.0.0", NumberStyles.Currency, corruptNFI, 0, false);
            passed &= VerifyUInt16TryParse("5.0", NumberStyles.Currency, corruptNFI, 5, true);
            passed &= VerifyUInt16TryParse("5,0", NumberStyles.Currency, corruptNFI, 0, false);
            passed &= VerifyUInt16TryParse("5.0.0", NumberStyles.Currency, corruptNFI, 0, false);
            passed &= VerifyUInt16TryParse("5.0", NumberStyles.Any, corruptNFI, 5, true);
            passed &= VerifyUInt16TryParse("5,0", NumberStyles.Any, corruptNFI, 0, false);
            passed &= VerifyUInt16TryParse("5.0.0", NumberStyles.Any, corruptNFI, 0, false);
            //
            passed &= VerifyUInt16TryParse("5", NumberStyles.Integer, swappedNFI, 5, true);
            passed &= VerifyUInt16TryParse("1,234", NumberStyles.Integer, swappedNFI, 0, false);
            passed &= VerifyUInt16TryParse("5", NumberStyles.Number, swappedNFI, 5, true);
            passed &= VerifyUInt16TryParse("5.0", NumberStyles.Number, swappedNFI, 5, true);
            passed &= VerifyUInt16TryParse("1,234", NumberStyles.Number, swappedNFI, 1234, true);
            passed &= VerifyUInt16TryParse("1,234.0", NumberStyles.Number, swappedNFI, 1234, true);
            passed &= VerifyUInt16TryParse("5.000.000", NumberStyles.Number, swappedNFI, 0, false);
            passed &= VerifyUInt16TryParse("5.000,00", NumberStyles.Number, swappedNFI, 0, false);
            passed &= VerifyUInt16TryParse("5.000", NumberStyles.Currency, swappedNFI, 5, true); //???
            passed &= VerifyUInt16TryParse("5.000,00", NumberStyles.Currency, swappedNFI, 0, false); //???
            passed &= VerifyUInt16TryParse("$5.000", NumberStyles.Currency, swappedNFI, 5000, true);
            passed &= VerifyUInt16TryParse("$5.000,00", NumberStyles.Currency, swappedNFI, 5000, true);
            passed &= VerifyUInt16TryParse("5.000", NumberStyles.Any, swappedNFI, 5, true);  //?
            passed &= VerifyUInt16TryParse("5.000,00", NumberStyles.Any, swappedNFI, 0, false);  //?
            passed &= VerifyUInt16TryParse("$5.000", NumberStyles.Any, swappedNFI, 5000, true);
            passed &= VerifyUInt16TryParse("$5.000,00", NumberStyles.Any, swappedNFI, 5000, true);
            passed &= VerifyUInt16TryParse("5,0", NumberStyles.Currency, swappedNFI, 5, true);
            passed &= VerifyUInt16TryParse("$5,0", NumberStyles.Currency, swappedNFI, 5, true);
            passed &= VerifyUInt16TryParse("5,000", NumberStyles.Currency, swappedNFI, 5, true);
            passed &= VerifyUInt16TryParse("$5,000", NumberStyles.Currency, swappedNFI, 5, true);
            passed &= VerifyUInt16TryParse("5,000.0", NumberStyles.Currency, swappedNFI, 0, false);
            passed &= VerifyUInt16TryParse("$5,000.0", NumberStyles.Currency, swappedNFI, 0, false);
            passed &= VerifyUInt16TryParse("5,000", NumberStyles.Any, swappedNFI, 5, true);
            passed &= VerifyUInt16TryParse("$5,000", NumberStyles.Any, swappedNFI, 5, true);
            passed &= VerifyUInt16TryParse("5,000.0", NumberStyles.Any, swappedNFI, 0, false);
            passed &= VerifyUInt16TryParse("$5,000.0", NumberStyles.Any, swappedNFI, 0, false);
            //
            passed &= VerifyUInt16TryParse("5.0", NumberStyles.Number, distinctNFI, 5, true);
            passed &= VerifyUInt16TryParse("1,234.0", NumberStyles.Number, distinctNFI, 1234, true);
            passed &= VerifyUInt16TryParse("5.0", NumberStyles.Currency, distinctNFI, 5, true);
            passed &= VerifyUInt16TryParse("1,234.0", NumberStyles.Currency, distinctNFI, 1234, true);
            passed &= VerifyUInt16TryParse("5.0", NumberStyles.Any, distinctNFI, 5, true);
            passed &= VerifyUInt16TryParse("1,234.0", NumberStyles.Any, distinctNFI, 1234, true);
            passed &= VerifyUInt16TryParse("$5.0", NumberStyles.Currency, distinctNFI, 0, false);
            passed &= VerifyUInt16TryParse("$5.0", NumberStyles.Any, distinctNFI, 0, false);
            passed &= VerifyUInt16TryParse("5:0", NumberStyles.Number, distinctNFI, 0, false);
            passed &= VerifyUInt16TryParse("5;0", NumberStyles.Number, distinctNFI, 0, false);
            passed &= VerifyUInt16TryParse("$5:0", NumberStyles.Number, distinctNFI, 0, false);
            passed &= VerifyUInt16TryParse("5:0", NumberStyles.Currency, distinctNFI, 5, true);
            passed &= VerifyUInt16TryParse("5:000", NumberStyles.Currency, distinctNFI, 5, true);
            passed &= VerifyUInt16TryParse("5;000", NumberStyles.Currency, distinctNFI, 5000, true);
            passed &= VerifyUInt16TryParse("$5:0", NumberStyles.Currency, distinctNFI, 5, true);
            passed &= VerifyUInt16TryParse("$5;0", NumberStyles.Currency, distinctNFI, 50, true);
            passed &= VerifyUInt16TryParse("5:0", NumberStyles.Any, distinctNFI, 5, true);
            passed &= VerifyUInt16TryParse("5;0", NumberStyles.Any, distinctNFI, 50, true);
            passed &= VerifyUInt16TryParse("$5:0", NumberStyles.Any, distinctNFI, 5, true);
            passed &= VerifyUInt16TryParse("$5;0", NumberStyles.Any, distinctNFI, 50, true);
            passed &= VerifyUInt16TryParse("1,23;45.0", NumberStyles.Number, distinctNFI, 0, false);
            passed &= VerifyUInt16TryParse("1,23;45.0", NumberStyles.Currency, distinctNFI, 12345, true);
            passed &= VerifyUInt16TryParse("1,23;45.0", NumberStyles.Any, distinctNFI, 12345, true);
            passed &= VerifyUInt16TryParse("$1,23;45.0", NumberStyles.Any, distinctNFI, 0, false);


            // Should these pass or fail?  Current parse behavior is to pass, so they might be
            // parse bugs, but they're not tryparse bugs.
            passed &= VerifyUInt16Parse("5", NumberStyles.Float, goodNFI, 5);
            passed &= VerifyUInt16TryParse("5", NumberStyles.Float, goodNFI, 5, true);
            passed &= VerifyUInt16Parse("5", NumberStyles.AllowDecimalPoint, goodNFI, 5);
            passed &= VerifyUInt16TryParse("5", NumberStyles.AllowDecimalPoint, goodNFI, 5, true);
            // I expect ArgumentException with an ambiguous NFI
            passed &= VerifyUInt16ParseException("^42", NumberStyles.Any, ambigNFI, typeof(OverflowException));
            passed &= VerifyUInt16TryParse("^42", NumberStyles.Any, ambigNFI, 0, false);

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

    public static bool VerifyUInt16TryParse(string value, UInt16 expectedResult, bool expectedReturn)
    {
        if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: UInt16.TryParse, Value = '{0}', Expected Result = {1}, Expected Return = {2}",
                value, expectedResult, expectedReturn);
        }
        UInt16 result = 0;
        try
        {
            bool returnValue = UInt16.TryParse(value, out result);
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

    public static bool VerifyUInt16TryParse(string value, NumberStyles style, IFormatProvider provider, UInt16 expectedResult, bool expectedReturn)
    {
		if (provider == null) return true;

		if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: UInt16.TryParse, Value = '{0}', Style = {1}, Provider = {2}, Expected Result = {3}, Expected Return = {4}",
                  value, style, provider, expectedResult, expectedReturn);
        }
        UInt16 result = 0;
        try
        {
            bool returnValue = UInt16.TryParse(value, style, provider, out result);
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

    public static bool VerifyUInt16TryParseException(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
    {
		if (provider == null) return true;

		if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: UInt16.TryParse, Value = '{0}', Style = {1}, Provider = {2}, Expected Exception = {3}",
                value, style, provider, exceptionType);
        }
        try
        {
            UInt16 result = 0;
            Boolean returnValue = UInt16.TryParse(value, style, provider, out result);
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

    public static bool VerifyUInt16Parse(string value, UInt16 expectedResult)
    {
        if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: UInt16.Parse, Value = '{0}', Expected Result, {1}",
                value, expectedResult);
        }
        try
        {
            UInt16 returnValue = UInt16.Parse(value);
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

    public static bool VerifyUInt16Parse(string value, NumberStyles style, IFormatProvider provider, UInt16 expectedResult)
    {
		if (provider == null) return true;

		if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: UInt16.Parse, Value = '{0}', Style = {1}, provider = {2}, Expected Result = {3}",
                value, style, provider, expectedResult);
        }
        try
        {
            UInt16 returnValue = UInt16.Parse(value, style, provider);
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

    public static bool VerifyUInt16ParseException(string value, Type exceptionType)
    {
        if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: UInt16.Parse, Value = '{0}', Expected Exception, {1}",
                value, exceptionType);
        }
        try
        {
            UInt16 returnValue = UInt16.Parse(value);
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

    public static bool VerifyUInt16ParseException(string value, NumberStyles style, Type exceptionType)
    {
        if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: UInt16.Parse, Value = '{0}', Style = {1}, Expected Exception = {3}",
                value, style, exceptionType);
        }
        try
        {
            UInt16 returnValue = UInt16.Parse(value, style);
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

    public static bool VerifyUInt16ParseException(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
    {
		if (provider == null) return true;

		if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: UInt16.Parse, Value = '{0}', Style = {1}, Provider = {2}, Expected Exception = {3}",
                value, style, provider, exceptionType);
        }
        try
        {
            UInt16 returnValue = UInt16.Parse(value, style, provider);
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
