// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
using System.Security;

// This test was ported over to CoreCLR from Co7523TryParse_all.cs
// Tests Byte.TryParse(String), Byte.TryParse(String, NumberStyles, IFormatProvider, ref Byte)
// 2003/02/25  KatyK
// 2007/06/28  adapted by MarielY

public class ByteTryParse
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
            passed &= VerifyByteParse("5", 5);
            passed &= VerifyByteParse("5   ", 5);
            passed &= VerifyByteParse("5  \0", 5);
            passed &= VerifyByteParse("5\0\0\0", 5);
            passed &= VerifyByteParse("5", NumberStyles.Integer, CultureInfo.InvariantCulture, 5);
            passed &= VerifyByteParse("12", NumberStyles.HexNumber, CultureInfo.InvariantCulture, 0x12);
            passed &= VerifyByteParse("fF", NumberStyles.HexNumber, CultureInfo.InvariantCulture, 0xFF);
            passed &= VerifyByteParse("5", NumberStyles.Integer, goodNFI, 5);
            passed &= VerifyByteParse("5\0", NumberStyles.Integer, CultureInfo.InvariantCulture, 5);
            passed &= VerifyByteParse("123", NumberStyles.Integer, germanCulture, 123);
            passed &= VerifyByteParse("123", NumberStyles.Integer, japaneseCulture, 123);
            passed &= VerifyByteParse("123,000", NumberStyles.AllowDecimalPoint, germanCulture, 123);
            passed &= VerifyByteParse("123.000", NumberStyles.AllowDecimalPoint, japaneseCulture, 123);
            passed &= VerifyByteParse("5,00 " + germanCulture.NumberFormat.CurrencySymbol, NumberStyles.Any, germanCulture, 5); // currency
            //
            passed &= VerifyByteParse("5", NumberStyles.Integer, corruptNFI, 5);
            passed &= VerifyByteParse("5", NumberStyles.Number, corruptNFI, 5);
            passed &= VerifyByteParse("5.0", NumberStyles.Number, corruptNFI, 5);
            passed &= VerifyByteParseException("5,0", NumberStyles.Number, corruptNFI, typeof(FormatException));
            passed &= VerifyByteParseException("5.0.0", NumberStyles.Number, corruptNFI, typeof(FormatException));
            passed &= VerifyByteParse("$5.0", NumberStyles.Currency, corruptNFI, 5);
            passed &= VerifyByteParseException("$5,0", NumberStyles.Currency, corruptNFI, typeof(FormatException));
            passed &= VerifyByteParseException("$5.0.0", NumberStyles.Currency, corruptNFI, typeof(FormatException));
            passed &= VerifyByteParse("5.0", NumberStyles.Currency, corruptNFI, 5);
            passed &= VerifyByteParseException("5,0", NumberStyles.Currency, corruptNFI, typeof(FormatException));
            passed &= VerifyByteParseException("5.0.0", NumberStyles.Currency, corruptNFI, typeof(FormatException));
            passed &= VerifyByteParse("5.0", NumberStyles.Any, corruptNFI, 5);
            passed &= VerifyByteParseException("5,0", NumberStyles.Any, corruptNFI, typeof(FormatException));
            passed &= VerifyByteParseException("5.0.0", NumberStyles.Any, corruptNFI, typeof(FormatException));
            //
            passed &= VerifyByteParse("5", NumberStyles.Integer, swappedNFI, 5);
            passed &= VerifyByteParse("5", NumberStyles.Number, swappedNFI, 5);
            passed &= VerifyByteParse("5.0", NumberStyles.Number, swappedNFI, 5);
            passed &= VerifyByteParseException("5.000.000", NumberStyles.Number, swappedNFI, typeof(FormatException));
            passed &= VerifyByteParse("5.0", NumberStyles.Currency, swappedNFI, 5); //???
            passed &= VerifyByteParse("$5.0", NumberStyles.Currency, swappedNFI, 50);
            passed &= VerifyByteParse("5.0", NumberStyles.Any, swappedNFI, 5);  //?
            passed &= VerifyByteParse("$5.0", NumberStyles.Any, swappedNFI, 50);
            passed &= VerifyByteParse("5,0", NumberStyles.Currency, swappedNFI, 5);
            passed &= VerifyByteParse("$5,0", NumberStyles.Currency, swappedNFI, 5);
            passed &= VerifyByteParse("5,000", NumberStyles.Currency, swappedNFI, 5);
            passed &= VerifyByteParse("$5,000", NumberStyles.Currency, swappedNFI, 5);
            passed &= VerifyByteParse("5,000", NumberStyles.Any, swappedNFI, 5);
            passed &= VerifyByteParse("$5,000", NumberStyles.Any, swappedNFI, 5);
            //
            passed &= VerifyByteParse("5.0", NumberStyles.Number, distinctNFI, 5);
            passed &= VerifyByteParse("5.0", NumberStyles.Currency, distinctNFI, 5);
            passed &= VerifyByteParse("5.0", NumberStyles.Any, distinctNFI, 5);
            passed &= VerifyByteParseException("$5.0", NumberStyles.Currency, distinctNFI, typeof(FormatException));
            passed &= VerifyByteParseException("$5.0", NumberStyles.Any, distinctNFI, typeof(FormatException));
            passed &= VerifyByteParseException("5:0", NumberStyles.Number, distinctNFI, typeof(FormatException));
            passed &= VerifyByteParseException("$5:0", NumberStyles.Number, distinctNFI, typeof(FormatException));
            passed &= VerifyByteParse("5:0", NumberStyles.Currency, distinctNFI, 5);
            passed &= VerifyByteParse("$5:0", NumberStyles.Currency, distinctNFI, 5);
            passed &= VerifyByteParse("5:0", NumberStyles.Any, distinctNFI, 5);
            passed &= VerifyByteParse("$5:0", NumberStyles.Any, distinctNFI, 5);

            passed &= VerifyByteParseException("300", typeof(OverflowException));
            passed &= VerifyByteParseException("-5", typeof(OverflowException));
            passed &= VerifyByteParseException("Garbage", typeof(FormatException));
            passed &= VerifyByteParseException("5\0Garbage", typeof(FormatException));
            passed &= VerifyByteParseException(null, typeof(ArgumentNullException));
            passed &= VerifyByteParseException("1FF", NumberStyles.HexNumber, CultureInfo.InvariantCulture, typeof(OverflowException));
            passed &= VerifyByteParseException("FFFFFFFF", NumberStyles.HexNumber, CultureInfo.InvariantCulture, typeof(OverflowException));
            passed &= VerifyByteParseException("5", NumberStyles.AllowHexSpecifier | NumberStyles.AllowParentheses, null, typeof(ArgumentException));
			passed &= VerifyByteParseException("4", (NumberStyles)(-1), typeof(ArgumentException));
			passed &= VerifyByteParseException("4", (NumberStyles)0x10000, typeof(ArgumentException));
			passed &= VerifyByteParseException("4", (NumberStyles)(-1), CultureInfo.InvariantCulture, typeof(ArgumentException));
			passed &= VerifyByteParseException("4", (NumberStyles)0x10000, CultureInfo.InvariantCulture, typeof(ArgumentException));
            passed &= VerifyByteParseException("123.000", NumberStyles.Any, germanCulture, typeof(OverflowException));
            passed &= VerifyByteParseException("123,000", NumberStyles.Any, japaneseCulture, typeof(OverflowException));
            passed &= VerifyByteParseException("123,000", NumberStyles.Integer, germanCulture, typeof(FormatException));
            passed &= VerifyByteParseException("123.000", NumberStyles.Integer, japaneseCulture, typeof(FormatException));
            passed &= VerifyByteParseException("5,00 " + germanCulture.NumberFormat.CurrencySymbol, NumberStyles.Integer, germanCulture, typeof(FormatException)); // currency

            /////////// TryParse(String)
            //// Pass cases
            passed &= VerifyByteTryParse("5", 5, true);
            passed &= VerifyByteTryParse(" 5 ", 5, true);
            passed &= VerifyByteTryParse("5\0", 5, true);
            passed &= VerifyByteTryParse("5   \0", 5, true);
            passed &= VerifyByteTryParse("5\0\0\0", 5, true);
            passed &= VerifyByteTryParse(Byte.MaxValue.ToString(), Byte.MaxValue, true);
            passed &= VerifyByteTryParse(Byte.MinValue.ToString(), Byte.MinValue, true);

            //// Fail cases
            passed &= VerifyByteTryParse(null, 0, false);
            passed &= VerifyByteTryParse("", 0, false);
            passed &= VerifyByteTryParse("Garbage", 0, false);
            passed &= VerifyByteTryParse("5\0Garbage", 0, false);
            passed &= VerifyByteTryParse("300", 0, false);
            passed &= VerifyByteTryParse("-5", 0, false);
            passed &= VerifyByteTryParse("FF", 0, false);
            passed &= VerifyByteTryParse("27.3", 0, false);
            passed &= VerifyByteTryParse("23 5", 0, false);


            /////////// TryParse(TryParse(String, NumberStyles, IFormatProvider, ref Byte)
            //// Pass cases
            passed &= VerifyByteTryParse("5", NumberStyles.Integer, CultureInfo.InvariantCulture, 5, true);
            // Variations on NumberStyles
            passed &= VerifyByteTryParse("12", NumberStyles.HexNumber, CultureInfo.InvariantCulture, 0x12, true);
            passed &= VerifyByteTryParse("FF", NumberStyles.HexNumber, CultureInfo.InvariantCulture, 0xFF, true);
            passed &= VerifyByteTryParse("fF", NumberStyles.HexNumber, CultureInfo.InvariantCulture, 0xFF, true);
            passed &= VerifyByteTryParse("   5", NumberStyles.AllowLeadingWhite, goodNFI, 5, true);
            passed &= VerifyByteTryParse("5", NumberStyles.Number, goodNFI, 5, true);
            // Variations on IFP
            passed &= VerifyByteTryParse("5", NumberStyles.Integer, goodNFI, 5, true);
            passed &= VerifyByteTryParse("5", NumberStyles.Integer, null, 5, true);
            passed &= VerifyByteTryParse("5", NumberStyles.Integer, new DateTimeFormatInfo(), 5, true);
            passed &= VerifyByteTryParse("123", NumberStyles.Integer, germanCulture, 123, true);
            passed &= VerifyByteTryParse("123", NumberStyles.Integer, japaneseCulture, 123, true);
            passed &= VerifyByteTryParse("123,000", NumberStyles.AllowDecimalPoint, germanCulture, 123, true);
            passed &= VerifyByteTryParse("123.000", NumberStyles.AllowDecimalPoint, japaneseCulture, 123, true);
            passed &= VerifyByteTryParse("5,00 " + germanCulture.NumberFormat.CurrencySymbol, NumberStyles.Any, germanCulture, 5, true); // currency

            //// Fail cases
            passed &= VerifyByteTryParse("FF", NumberStyles.Integer, CultureInfo.InvariantCulture, 0, false);
            passed &= VerifyByteTryParse("1FF", NumberStyles.HexNumber, CultureInfo.InvariantCulture, 0, false);
            passed &= VerifyByteTryParse("FFFFFFFF", NumberStyles.HexNumber, CultureInfo.InvariantCulture, 0, false);
            passed &= VerifyByteTryParse("^42", NumberStyles.Any, customNFI, 0, false);
            passed &= VerifyByteTryParse("-42", NumberStyles.Any, customNFI, 0, false);
            passed &= VerifyByteTryParse("5.3", NumberStyles.AllowDecimalPoint, goodNFI, 0, false);
            passed &= VerifyByteTryParse("5   ", NumberStyles.AllowLeadingWhite, goodNFI, 0, false);
            passed &= VerifyByteTryParse("123.000", NumberStyles.Any, germanCulture, 0, false);
            passed &= VerifyByteTryParse("123,000", NumberStyles.Any, japaneseCulture, 0, false);
            passed &= VerifyByteTryParse("123,000", NumberStyles.Integer, germanCulture, 0, false);
            passed &= VerifyByteTryParse("123.000", NumberStyles.Integer, japaneseCulture, 0, false);
            passed &= VerifyByteTryParse("5,00 " + germanCulture.NumberFormat.CurrencySymbol, NumberStyles.Integer, germanCulture, 0, false); // currency

            //// Exception cases
			passed &= VerifyByteTryParseException("5", NumberStyles.AllowHexSpecifier | NumberStyles.AllowParentheses, null, typeof(ArgumentException));
			passed &= VerifyByteTryParseException("4", (NumberStyles)(-1), CultureInfo.InvariantCulture, typeof(ArgumentException));
			passed &= VerifyByteTryParseException("4", (NumberStyles)0x10000, CultureInfo.InvariantCulture, typeof(ArgumentException));

            // NumberStyles/NFI variations
            //
            passed &= VerifyByteTryParse("5", NumberStyles.Integer, corruptNFI, 5, true);
            passed &= VerifyByteTryParse("5", NumberStyles.Number, corruptNFI, 5, true);
            passed &= VerifyByteTryParse("5.0", NumberStyles.Number, corruptNFI, 5, true);
            passed &= VerifyByteTryParse("5,0", NumberStyles.Number, corruptNFI, 0, false);
            passed &= VerifyByteTryParse("5.0.0", NumberStyles.Number, corruptNFI, 0, false);
            passed &= VerifyByteTryParse("$5.0", NumberStyles.Currency, corruptNFI, 5, true);
            passed &= VerifyByteTryParse("$5,0", NumberStyles.Currency, corruptNFI, 0, false);
            passed &= VerifyByteTryParse("$5.0.0", NumberStyles.Currency, corruptNFI, 0, false);
            passed &= VerifyByteTryParse("5.0", NumberStyles.Currency, corruptNFI, 5, true);
            passed &= VerifyByteTryParse("5,0", NumberStyles.Currency, corruptNFI, 0, false);
            passed &= VerifyByteTryParse("5.0.0", NumberStyles.Currency, corruptNFI, 0, false);
            passed &= VerifyByteTryParse("5.0", NumberStyles.Any, corruptNFI, 5, true);
            passed &= VerifyByteTryParse("5,0", NumberStyles.Any, corruptNFI, 0, false);
            passed &= VerifyByteTryParse("5.0.0", NumberStyles.Any, corruptNFI, 0, false);
            //
            passed &= VerifyByteTryParse("5", NumberStyles.Integer, swappedNFI, 5, true);
            passed &= VerifyByteTryParse("5", NumberStyles.Number, swappedNFI, 5, true);
            passed &= VerifyByteTryParse("5.0", NumberStyles.Number, swappedNFI, 5, true);
            passed &= VerifyByteTryParse("5.000.000", NumberStyles.Number, swappedNFI, 0, false);
            passed &= VerifyByteTryParse("5.0", NumberStyles.Currency, swappedNFI, 5, true); //???
            passed &= VerifyByteTryParse("$5.0", NumberStyles.Currency, swappedNFI, 50, true);
            passed &= VerifyByteTryParse("5.0", NumberStyles.Any, swappedNFI, 5, true);  //?
            passed &= VerifyByteTryParse("$5.0", NumberStyles.Any, swappedNFI, 50, true);
            passed &= VerifyByteTryParse("5,0", NumberStyles.Currency, swappedNFI, 5, true);
            passed &= VerifyByteTryParse("$5,0", NumberStyles.Currency, swappedNFI, 5, true);
            passed &= VerifyByteTryParse("5,000", NumberStyles.Currency, swappedNFI, 5, true);
            passed &= VerifyByteTryParse("$5,000", NumberStyles.Currency, swappedNFI, 5, true);
            passed &= VerifyByteTryParse("5,000", NumberStyles.Any, swappedNFI, 5, true);
            passed &= VerifyByteTryParse("$5,000", NumberStyles.Any, swappedNFI, 5, true);
            //
            passed &= VerifyByteTryParse("5.0", NumberStyles.Number, distinctNFI, 5, true);
            passed &= VerifyByteTryParse("5.0", NumberStyles.Currency, distinctNFI, 5, true);
            passed &= VerifyByteTryParse("5.0", NumberStyles.Any, distinctNFI, 5, true);
            passed &= VerifyByteTryParse("$5.0", NumberStyles.Currency, distinctNFI, 0, false);
            passed &= VerifyByteTryParse("$5.0", NumberStyles.Any, distinctNFI, 0, false);
            passed &= VerifyByteTryParse("5:0", NumberStyles.Number, distinctNFI, 0, false);
            passed &= VerifyByteTryParse("$5:0", NumberStyles.Number, distinctNFI, 0, false);
            passed &= VerifyByteTryParse("5:0", NumberStyles.Currency, distinctNFI, 5, true);
            passed &= VerifyByteTryParse("$5:0", NumberStyles.Currency, distinctNFI, 5, true);
            passed &= VerifyByteTryParse("5:0", NumberStyles.Any, distinctNFI, 5, true);
            passed &= VerifyByteTryParse("$5:0", NumberStyles.Any, distinctNFI, 5, true);


            // Should these pass or fail?  Current parse behavior is to pass, so they might be
            // parse bugs, but they're not tryparse bugs.
            passed &= VerifyByteParse("5", NumberStyles.Float, goodNFI, 5);
            passed &= VerifyByteTryParse("5", NumberStyles.Float, goodNFI, 5, true);
            passed &= VerifyByteParse("5", NumberStyles.AllowDecimalPoint, goodNFI, 5);
            passed &= VerifyByteTryParse("5", NumberStyles.AllowDecimalPoint, goodNFI, 5, true);
			// I expect ArgumentException with an ambiguous NFI
            passed &= VerifyByteParseException("^42", NumberStyles.Any, ambigNFI, typeof(OverflowException));
            passed &= VerifyByteTryParse("^42", NumberStyles.Any, ambigNFI, 0, false);

            ///  END TEST CASES
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001", "Unexpected exception: " + e);
            passed = false;
        }

        if (passed)
        {
            TestLibrary.TestFramework.LogInformation("PASS");
            return 100;
        }
        else
        {
            TestLibrary.TestFramework.LogInformation("FAIL");
            return 1;
        }
    }

    public static bool VerifyByteTryParse(string value, Byte expectedResult, bool expectedReturn)
    {
        if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: Byte.TryParse, Value = '{0}', Expected Result = {1}, Expected Return = {2}",
                value, expectedResult, expectedReturn);
        }
        Byte result = 0;
        try
        {
            bool returnValue = Byte.TryParse(value, out result);
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

    public static bool VerifyByteTryParse(string value, NumberStyles style, IFormatProvider provider, Byte expectedResult, bool expectedReturn)
    {
		if (provider == null) return true;

        if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: Byte.TryParse, Value = '{0}', Style = {1}, Provider = {2}, Expected Result = {3}, Expected Return = {4}",
                  value, style, provider, expectedResult, expectedReturn);
        }
        Byte result = 0;
        try
        {
            bool returnValue = Byte.TryParse(value, style, provider, out result);
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

    public static bool VerifyByteTryParseException(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
    {
		if (provider == null) return true;
		
		if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: Byte.TryParse, Value = '{0}', Style = {1}, Provider = {2}, Expected Exception = {3}",
                value, style, provider, exceptionType);
        }
        try
        {
            Byte result = 0;
            Boolean returnValue = Byte.TryParse(value, style, provider, out result);
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

    public static bool VerifyByteParse(string value, Byte expectedResult)
    {
        if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: Byte.Parse, Value = '{0}', Expected Result, {1}",
                value, expectedResult);
        }
        try
        {
            Byte returnValue = Byte.Parse(value);
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

    public static bool VerifyByteParse(string value, NumberStyles style, IFormatProvider provider, Byte expectedResult)
    {
		if (provider == null) return true;

		if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: Byte.Parse, Value = '{0}', Style = {1}, provider = {2}, Expected Result = {3}",
                value, style, provider, expectedResult);
        }
        try
        {
            Byte returnValue = Byte.Parse(value, style, provider);
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

    public static bool VerifyByteParseException(string value, Type exceptionType)
    {
        if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: Byte.Parse, Value = '{0}', Expected Exception, {1}",
                value, exceptionType);
        }
        try
        {
            Byte returnValue = Byte.Parse(value);
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

    public static bool VerifyByteParseException(string value, NumberStyles style, Type exceptionType)
    {
        if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: Byte.Parse, Value = '{0}', Style = {1}, Expected Exception = {3}",
                value, style, exceptionType);
        }
        try
        {
            Byte returnValue = Byte.Parse(value, style);
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

    public static bool VerifyByteParseException(string value, NumberStyles style, IFormatProvider provider, Type exceptionType)
    {
		if (provider == null) return true;

		if (verbose)
        {
            TestLibrary.Logging.WriteLine("Test: Byte.Parse, Value = '{0}', Style = {1}, Provider = {2}, Expected Exception = {3}",
                value, style, provider, exceptionType);
        }
        try
        {
            Byte returnValue = Byte.Parse(value, style, provider);
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
