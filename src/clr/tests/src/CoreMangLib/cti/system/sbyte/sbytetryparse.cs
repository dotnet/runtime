// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

// CoreCLR Port from Co7524TryParse_all.cs
// Tests SByte.TryParse(String), SByte.TryParse(String, NumberStyles, IFormatProvider, ref SByte)
// 2003/04/01  KatyK
// 2007/06/28  adapted by MarielY

public class SByteTryParse
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

            /////////// Parse(String) - We include Parse for comparison
            passed &= VerifySByteParse("5", 5);
            passed &= VerifySByteParse("-5", -5);
            passed &= VerifySByteParse("5  ", 5);
            passed &= VerifySByteParse("5\0", 5);
            passed &= VerifySByteParse("5", NumberStyles.Integer, CultureInfo.InvariantCulture, 5);
            passed &= VerifySByteParse("-5", NumberStyles.Integer, CultureInfo.InvariantCulture, -5);
            passed &= VerifySByteParse("5  \0", NumberStyles.Integer, CultureInfo.InvariantCulture, 5);
            passed &= VerifySByteParse("5\0\0\0", NumberStyles.Integer, CultureInfo.InvariantCulture, 5);
            passed &= VerifySByteParse("12", NumberStyles.HexNumber, CultureInfo.InvariantCulture, 0x12);
            passed &= VerifySByteParse("fF", NumberStyles.HexNumber, CultureInfo.InvariantCulture, -1);
            passed &= VerifySByteParse("5", NumberStyles.Integer, goodNFI, 5);
            passed &= VerifySByteParse("123", NumberStyles.Integer, germanCulture, 123);
            passed &= VerifySByteParse("123", NumberStyles.Integer, japaneseCulture, 123);
            passed &= VerifySByteParse("123,000", NumberStyles.AllowDecimalPoint, germanCulture, 123);
            passed &= VerifySByteParse("123.000", NumberStyles.AllowDecimalPoint, japaneseCulture, 123);
            passed &= VerifySByteParse("5,00 " + germanCulture.NumberFormat.CurrencySymbol, NumberStyles.Any, germanCulture, 5); // currency
            //
            passed &= VerifySByteParse("5", NumberStyles.Integer, corruptNFI, 5);
            passed &= VerifySByteParse("5", NumberStyles.Number, corruptNFI, 5);
            passed &= VerifySByteParse("5.0", NumberStyles.Number, corruptNFI, 5);
            passed &= VerifySByteParseException("5,0", NumberStyles.Number, corruptNFI, typeof(FormatException));
            passed &= VerifySByteParseException("5.0.0", NumberStyles.Number, corruptNFI, typeof(FormatException));
            passed &= VerifySByteParse("$5.0", NumberStyles.Currency, corruptNFI, 5);
            passed &= VerifySByteParseException("$5,0", NumberStyles.Currency, corruptNFI, typeof(FormatException));
            passed &= VerifySByteParseException("$5.0.0", NumberStyles.Currency, corruptNFI, typeof(FormatException));
            passed &= VerifySByteParse("5.0", NumberStyles.Currency, corruptNFI, 5);
            passed &= VerifySByteParseException("5,0", NumberStyles.Currency, corruptNFI, typeof(FormatException));
            passed &= VerifySByteParseException("5.0.0", NumberStyles.Currency, corruptNFI, typeof(FormatException));
            passed &= VerifySByteParse("5.0", NumberStyles.Any, corruptNFI, 5);
            passed &= VerifySByteParseException("5,0", NumberStyles.Any, corruptNFI, typeof(FormatException));
            passed &= VerifySByteParseException("5.0.0", NumberStyles.Any, corruptNFI, typeof(FormatException));
            //
            passed &= VerifySByteParse("5", NumberStyles.Integer, swappedNFI, 5);
            passed &= VerifySByteParse("5", NumberStyles.Number, swappedNFI, 5);
            passed &= VerifySByteParse("5.0", NumberStyles.Number, swappedNFI, 5);
            passed &= VerifySByteParseException("5.000.000", NumberStyles.Number, swappedNFI, typeof(FormatException));
            passed &= VerifySByteParse("5.0", NumberStyles.Currency, swappedNFI, 5); //???
            passed &= VerifySByteParse("$5.0", NumberStyles.Currency, swappedNFI, 50);
            passed &= VerifySByteParse("5.0", NumberStyles.Any, swappedNFI, 5);  //?
            passed &= VerifySByteParse("$5.0", NumberStyles.Any, swappedNFI, 50);
            passed &= VerifySByteParse("5,0", NumberStyles.Currency, swappedNFI, 5);
            passed &= VerifySByteParse("$5,0", NumberStyles.Currency, swappedNFI, 5);
            passed &= VerifySByteParse("5,000", NumberStyles.Currency, swappedNFI, 5);
            passed &= VerifySByteParse("$5,000", NumberStyles.Currency, swappedNFI, 5);
            passed &= VerifySByteParse("5,000", NumberStyles.Any, swappedNFI, 5);
            passed &= VerifySByteParse("$5,000", NumberStyles.Any, swappedNFI, 5);
            //
            passed &= VerifySByteParse("5.0", NumberStyles.Number, distinctNFI, 5);
            passed &= VerifySByteParse("5.0", NumberStyles.Currency, distinctNFI, 5);
            passed &= VerifySByteParse("5.0", NumberStyles.Any, distinctNFI, 5);
            passed &= VerifySByteParseException("$5.0", NumberStyles.Currency, distinctNFI, typeof(FormatException));
            passed &= VerifySByteParseException("$5.0", NumberStyles.Any, distinctNFI, typeof(FormatException));
            passed &= VerifySByteParseException("5:0", NumberStyles.Number, distinctNFI, typeof(FormatException));
            passed &= VerifySByteParseException("$5:0", NumberStyles.Number, distinctNFI, typeof(FormatException));
            passed &= VerifySByteParse("5:0", NumberStyles.Currency, distinctNFI, 5);
            passed &= VerifySByteParse("$5:0", NumberStyles.Currency, distinctNFI, 5);
            passed &= VerifySByteParse("5:0", NumberStyles.Any, distinctNFI, 5);
            passed &= VerifySByteParse("$5:0", NumberStyles.Any, distinctNFI, 5);
            //
            passed &= VerifySByteParseException("200", typeof(OverflowException));
            passed &= VerifySByteParseException("-200", typeof(OverflowException));
            passed &= VerifySByteParseException("Garbage", typeof(FormatException));
            passed &= VerifySByteParseException("5\0Garbage", typeof(FormatException));
            passed &= VerifySByteParseException(null, typeof(ArgumentNullException));
            passed &= VerifySByteParseException("1FF", NumberStyles.HexNumber, CultureInfo.InvariantCulture, typeof(OverflowException));
            passed &= VerifySByteParseException("FFFFFFFF", NumberStyles.HexNumber, CultureInfo.InvariantCulture, typeof(OverflowException));
            passed &= VerifySByteParseException("5", NumberStyles.AllowHexSpecifier | NumberStyles.AllowParentheses, null, typeof(ArgumentException));
            passed &= VerifySByteParseException("4", (NumberStyles)(-1), typeof(ArgumentException));
            passed &= VerifySByteParseException("4", (NumberStyles)0x10000, typeof(ArgumentException));
            passed &= VerifySByteParseException("4", (NumberStyles)(-1), CultureInfo.InvariantCulture, typeof(ArgumentException));
            passed &= VerifySByteParseException("4", (NumberStyles)0x10000, CultureInfo.InvariantCulture, typeof(ArgumentException));
            passed &= VerifySByteParseException("123.000", NumberStyles.Any, germanCulture, typeof(OverflowException));
            passed &= VerifySByteParseException("123,000", NumberStyles.Any, japaneseCulture, typeof(OverflowException));
            passed &= VerifySByteParseException("123,000", NumberStyles.Integer, germanCulture, typeof(FormatException));
            passed &= VerifySByteParseException("123.000", NumberStyles.Integer, japaneseCulture, typeof(FormatException));
            passed &= VerifySByteParseException("5,00 " + germanCulture.NumberFormat.CurrencySymbol, NumberStyles.Integer, germanCulture, typeof(FormatException)); // currency


            /////////// TryParse(String)
            //// Pass cases
            passed &= VerifySByteTryParse("5", 5, true);
            passed &= VerifySByteTryParse("-5", -5, true);
            passed &= VerifySByteTryParse(" 5 ", 5, true);
            passed &= VerifySByteTryParse("5\0", 5, true);
            passed &= VerifySByteTryParse("5  \0", 5, true);
            passed &= VerifySByteTryParse("5\0\0\0", 5, true);
            passed &= VerifySByteTryParse(SByte.MaxValue.ToString(), SByte.MaxValue, true);
            passed &= VerifySByteTryParse(SByte.MinValue.ToString(), SByte.MinValue, true);

            //// Fail cases
            passed &= VerifySByteTryParse(null, 0, false);
            passed &= VerifySByteTryParse("", 0, false);
            passed &= VerifySByteTryParse("Garbage", 0, false);
            passed &= VerifySByteTryParse("5\0Garbage", 0, false);
            passed &= VerifySByteTryParse("300", 0, false);
            passed &= VerifySByteTryParse("FF", 0, false);
            passed &= VerifySByteTryParse("27.3", 0, false);
            passed &= VerifySByteTryParse("23 5", 0, false);
            passed &= VerifySByteTryParse("200", 0, false);
            passed &= VerifySByteTryParse("-200", 0, false);


            /////////// TryParse(TryParse(String, NumberStyles, IFormatProvider, ref SByte)
            //// Pass cases
            passed &= VerifySByteTryParse("5", NumberStyles.Integer, CultureInfo.InvariantCulture, 5, true);
            passed &= VerifySByteTryParse("-5", NumberStyles.Integer, CultureInfo.InvariantCulture, -5, true);
            // Variations on NumberStyles
            passed &= VerifySByteTryParse("12", NumberStyles.HexNumber, CultureInfo.InvariantCulture, 0x12, true);
            passed &= VerifySByteTryParse("FF", NumberStyles.HexNumber, CultureInfo.InvariantCulture, -1, true);
            passed &= VerifySByteTryParse("fF", NumberStyles.HexNumber, CultureInfo.InvariantCulture, -1, true);
            passed &= VerifySByteTryParse("   5", NumberStyles.AllowLeadingWhite, goodNFI, 5, true);
            passed &= VerifySByteTryParse("5", NumberStyles.Number, goodNFI, 5, true);
            // Variations on IFP
            passed &= VerifySByteTryParse("5", NumberStyles.Integer, goodNFI, 5, true);
            passed &= VerifySByteTryParse("5", NumberStyles.Integer, null, 5, true);
            passed &= VerifySByteTryParse("5", NumberStyles.Integer, new DateTimeFormatInfo(), 5, true);
            passed &= VerifySByteTryParse("^42", NumberStyles.Any, customNFI, -42, true);
            passed &= VerifySByteTryParse("123", NumberStyles.Integer, germanCulture, 123, true);
            passed &= VerifySByteTryParse("123", NumberStyles.Integer, japaneseCulture, 123, true);
            passed &= VerifySByteTryParse("123,000", NumberStyles.AllowDecimalPoint, germanCulture, 123, true);
            passed &= VerifySByteTryParse("123.000", NumberStyles.AllowDecimalPoint, japaneseCulture, 123, true);
            passed &= VerifySByteTryParse("5,00 " + germanCulture.NumberFormat.CurrencySymbol, NumberStyles.Any, germanCulture, 5, true); // currency

            //// Fail cases
            passed &= VerifySByteTryParse("FF", NumberStyles.Integer, CultureInfo.InvariantCulture, 0, false);
            passed &= VerifySByteTryParse("1FF", NumberStyles.HexNumber, CultureInfo.InvariantCulture, 0, false);
            passed &= VerifySByteTryParse("FFFFFFFF", NumberStyles.HexNumber, CultureInfo.InvariantCulture, 0, false);
            passed &= VerifySByteTryParse("-42", NumberStyles.Any, customNFI, 0, false);
            passed &= VerifySByteTryParse("5.3", NumberStyles.AllowDecimalPoint, goodNFI, 0, false);
            passed &= VerifySByteTryParse("5   ", NumberStyles.AllowLeadingWhite, goodNFI, 0, false);
            passed &= VerifySByteTryParse("123.000", NumberStyles.Any, germanCulture, 0, false);
            passed &= VerifySByteTryParse("123,000", NumberStyles.Any, japaneseCulture, 0, false);
            passed &= VerifySByteTryParse("123,000", NumberStyles.Integer, germanCulture, 0, false);
            passed &= VerifySByteTryParse("123.000", NumberStyles.Integer, japaneseCulture, 0, false);
            passed &= VerifySByteTryParse("5,00 " + germanCulture.NumberFormat.CurrencySymbol, NumberStyles.Integer, germanCulture, 0, false); // currency

            //// Exception cases
            passed &= VerifySByteTryParseException("5", NumberStyles.AllowHexSpecifier | NumberStyles.AllowParentheses, null, typeof(ArgumentException));
            passed &= VerifySByteTryParseException("4", (NumberStyles)(-1), CultureInfo.InvariantCulture, typeof(ArgumentException));
            passed &= VerifySByteTryParseException("4", (NumberStyles)0x10000, CultureInfo.InvariantCulture, typeof(ArgumentException));

            // NumberStyles/NFI variations
            //
            passed &= VerifySByteTryParse("5", NumberStyles.Integer, corruptNFI, 5, true);
            passed &= VerifySByteTryParse("5", NumberStyles.Number, corruptNFI, 5, true);
            passed &= VerifySByteTryParse("5.0", NumberStyles.Number, corruptNFI, 5, true);
            passed &= VerifySByteTryParse("5,0", NumberStyles.Number, corruptNFI, 0, false);
            passed &= VerifySByteTryParse("5.0.0", NumberStyles.Number, corruptNFI, 0, false);
            passed &= VerifySByteTryParse("$5.0", NumberStyles.Currency, corruptNFI, 5, true);
            passed &= VerifySByteTryParse("$5,0", NumberStyles.Currency, corruptNFI, 0, false);
            passed &= VerifySByteTryParse("$5.0.0", NumberStyles.Currency, corruptNFI, 0, false);
            passed &= VerifySByteTryParse("5.0", NumberStyles.Currency, corruptNFI, 5, true);
            passed &= VerifySByteTryParse("5,0", NumberStyles.Currency, corruptNFI, 0, false);
            passed &= VerifySByteTryParse("5.0.0", NumberStyles.Currency, corruptNFI, 0, false);
            passed &= VerifySByteTryParse("5.0", NumberStyles.Any, corruptNFI, 5, true);
            passed &= VerifySByteTryParse("5,0", NumberStyles.Any, corruptNFI, 0, false);
            passed &= VerifySByteTryParse("5.0.0", NumberStyles.Any, corruptNFI, 0, false);
            //
            passed &= VerifySByteTryParse("5", NumberStyles.Integer, swappedNFI, 5, true);
            passed &= VerifySByteTryParse("5", NumberStyles.Number, swappedNFI, 5, true);
            passed &= VerifySByteTryParse("5.0", NumberStyles.Number, swappedNFI, 5, true);
            passed &= VerifySByteTryParse("5.000.000", NumberStyles.Number, swappedNFI, 0, false);
            passed &= VerifySByteTryParse("5.0", NumberStyles.Currency, swappedNFI, 5, true); //???
            passed &= VerifySByteTryParse("$5.0", NumberStyles.Currency, swappedNFI, 50, true);
            passed &= VerifySByteTryParse("5.0", NumberStyles.Any, swappedNFI, 5, true);  //?
            passed &= VerifySByteTryParse("$5.0", NumberStyles.Any, swappedNFI, 50, true);
            passed &= VerifySByteTryParse("5,0", NumberStyles.Currency, swappedNFI, 5, true);
            passed &= VerifySByteTryParse("$5,0", NumberStyles.Currency, swappedNFI, 5, true);
            passed &= VerifySByteTryParse("5,000", NumberStyles.Currency, swappedNFI, 5, true);
            passed &= VerifySByteTryParse("$5,000", NumberStyles.Currency, swappedNFI, 5, true);
            passed &= VerifySByteTryParse("5,000", NumberStyles.Any, swappedNFI, 5, true);
            passed &= VerifySByteTryParse("$5,000", NumberStyles.Any, swappedNFI, 5, true);
            //
            passed &= VerifySByteTryParse("5.0", NumberStyles.Number, distinctNFI, 5, true);
            passed &= VerifySByteTryParse("5.0", NumberStyles.Currency, distinctNFI, 5, true);
            passed &= VerifySByteTryParse("5.0", NumberStyles.Any, distinctNFI, 5, true);
            passed &= VerifySByteTryParse("$5.0", NumberStyles.Currency, distinctNFI, 0, false);
            passed &= VerifySByteTryParse("$5.0", NumberStyles.Any, distinctNFI, 0, false);
            passed &= VerifySByteTryParse("5:0", NumberStyles.Number, distinctNFI, 0, false);
            passed &= VerifySByteTryParse("$5:0", NumberStyles.Number, distinctNFI, 0, false);
            passed &= VerifySByteTryParse("5:0", NumberStyles.Currency, distinctNFI, 5, true);
            passed &= VerifySByteTryParse("$5:0", NumberStyles.Currency, distinctNFI, 5, true);
            passed &= VerifySByteTryParse("5:0", NumberStyles.Any, distinctNFI, 5, true);
            passed &= VerifySByteTryParse("$5:0", NumberStyles.Any, distinctNFI, 5, true);


            // Should these pass or fail?  Current parse behavior is to pass, so they might be
            // parse bugs, but they're not tryparse bugs.
            passed &= VerifySByteParse("5", NumberStyles.Float, goodNFI, 5);
            passed &= VerifySByteTryParse("5", NumberStyles.Float, goodNFI, 5, true);
            passed &= VerifySByteParse("5", NumberStyles.AllowDecimalPoint, goodNFI, 5);
            passed &= VerifySByteTryParse("5", NumberStyles.AllowDecimalPoint, goodNFI, 5, true);
            // I expect ArgumentException with an ambiguous NFI
            passed &= VerifySByteParse("^42", NumberStyles.Any, ambigNFI, -42);
            passed &= VerifySByteTryParse("^42", NumberStyles.Any, ambigNFI, -42, true);

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

    public static bool VerifySByteTryParse(string value, SByte expectedResult, bool expectedReturn)
    {
        if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: SByte.TryParse, Value = '{0}', Expected Result = {1}, Expected Return = {2}",
                value, expectedResult, expectedReturn);
        }
        SByte result = 0;
        try
        {
            bool returnValue = SByte.TryParse(value, out result);
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

    public static bool VerifySByteTryParse(string value, NumberStyles style, IFormatProvider provider, SByte expectedResult, bool expectedReturn)
    {
		if (provider == null) return true;

		if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: SByte.TryParse, Value = '{0}', Style = {1}, Provider = {2}, Expected Result = {3}, Expected Return = {4}",
                  value, style, provider, expectedResult, expectedReturn);
        }
        SByte result = 0;
        try
        {
            bool returnValue = SByte.TryParse(value, style, provider, out result);
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

    public static bool VerifySByteTryParseException(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
    {
		if (provider == null) return true;

		if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: SByte.TryParse, Value = '{0}', Style = {1}, Provider = {2}, Expected Exception = {3}",
                value, style, provider, exceptionType);
        }
        try
        {
            SByte result = 0;
            Boolean returnValue = SByte.TryParse(value, style, provider, out result);
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

    public static bool VerifySByteParse(string value, SByte expectedResult)
    {
        if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: SByte.Parse, Value = '{0}', Expected Result, {1}",
                value, expectedResult);
        }
        try
        {
            SByte returnValue = SByte.Parse(value);
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

    public static bool VerifySByteParse(string value, NumberStyles style, IFormatProvider provider, SByte expectedResult)
    {
		if (provider == null) return true;

		if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: SByte.Parse, Value = '{0}', Style = {1}, provider = {2}, Expected Result = {3}",
                value, style, provider, expectedResult);
        }
        try
        {
            SByte returnValue = SByte.Parse(value, style, provider);
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

    public static bool VerifySByteParseException(string value, Type exceptionType)
    {
        if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: SByte.Parse, Value = '{0}', Expected Exception, {1}",
                value, exceptionType);
        }
        try
        {
            SByte returnValue = SByte.Parse(value);
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

    public static bool VerifySByteParseException(string value, NumberStyles style, Type exceptionType)
    {
        if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: SByte.Parse, Value = '{0}', Style = {1}, Expected Exception = {3}",
                value, style, exceptionType);
        }
        try
        {
            SByte returnValue = SByte.Parse(value, style);
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

    public static bool VerifySByteParseException(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
    {
		if (provider == null) return true;

		if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: SByte.Parse, Value = '{0}', Style = {1}, Provider = {2}, Expected Exception = {3}",
                value, style, provider, exceptionType);
        }
        try
        {
            SByte returnValue = SByte.Parse(value, style, provider);
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
