// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
using TestLibrary;

// ToString_str.cs
// Tests TimeSpan.ToString(str)
// The desktop changes 2009/04/21 KatyK are ported 2009/06/08 DidemG
public class TimeSpanTest
{
    static bool verbose = false;
    static int iCountTestcases = 0;
    static int iCountErrors = 0;

    public static int Main(String[] args)
    {
        try
        {
            // for SL, String.ToLower(InvariantCulture)
            if ((args.Length > 0) && args[0].ToLower() == "true") 
                verbose = true;

            Logging.WriteLine("CurrentCulture:  " + Utilities.CurrentCulture.Name);
            Logging.WriteLine("CurrentUICulture:  " + Utilities.CurrentCulture.Name);

            RunTests();
        }
        catch (Exception e)
        {
            iCountErrors++;
            Logging.WriteLine("Unexpected exception!!  " + e.ToString());
        }

        if (iCountErrors == 0)
        {
            Logging.WriteLine("Pass.  iCountTestcases=="+iCountTestcases);
            return 100;
        }
        else
        {
            Logging.WriteLine("FAIL!  iCountTestcases=="+iCountTestcases+", iCountErrors=="+iCountErrors);
            return 1;
        }
    }

    public static void RunTests()
    {
        // The current implementation uses the same character as the default NumberDecimalSeparator
        // for a culture, but this is an implementation detail and could change.  No user overrides
        // are respected.
        String GDecimalSeparator = Utilities.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        // Standard formats
        foreach (TimeSpan ts in Support.InterestingTimeSpans)
        {
            String defaultFormat = Support.CFormat(ts);
            VerifyToString(ts, defaultFormat); // no regressions
            VerifyToString(ts, "c", defaultFormat);
            VerifyToString(ts, "t", defaultFormat);
            VerifyToString(ts, "T", defaultFormat);
            VerifyToString(ts, null, defaultFormat);
            VerifyToString(ts, "", defaultFormat);
            VerifyToString(ts, "g", Support.gFormat(ts, GDecimalSeparator));
            VerifyToString(ts, "G", Support.GFormat(ts, GDecimalSeparator));
        }

        // Custom formats
        TimeSpan ts1 = new TimeSpan(1, 2, 3, 4, 56);
        VerifyToString(ts1, "d'-'", "1-");
        VerifyToString(ts1, "%d", "1");
        VerifyToString(ts1, "dd", "01");
        VerifyToString(ts1, "ddd", "001");
        VerifyToString(ts1, "dddd", "0001");
        VerifyToString(ts1, "ddddd", "00001");
        VerifyToString(ts1, "dddddd", "000001");
        VerifyToString(ts1, "ddddddd", "0000001");
        VerifyToString(ts1, "dddddddd", "00000001");
        VerifyToString(ts1, "h'-'", "2-");
        VerifyToString(ts1, "%h", "2");
        VerifyToString(ts1, "hh", "02");
        VerifyToString(ts1, "m'-'", "3-");
        VerifyToString(ts1, "%m", "3");
        VerifyToString(ts1, "mm", "03");
        VerifyToString(ts1, "s'-'", "4-");
        VerifyToString(ts1, "%s", "4");
        VerifyToString(ts1, "ss", "04");
        VerifyToString(ts1, "f'-'", "0-");
        VerifyToString(ts1, "ff", "05");
        VerifyToString(ts1, "fff", "056");
        VerifyToString(ts1, "ffff", "0560");
        VerifyToString(ts1, "fffff", "05600");
        VerifyToString(ts1, "ffffff", "056000");
        VerifyToString(ts1, "fffffff", "0560000");
        VerifyToString(ts1, "F'-'", "-");
        VerifyToString(ts1, "FF", "05");
        VerifyToString(ts1, "FFF", "056");
        VerifyToString(ts1, "FFFF", "056");
        VerifyToString(ts1, "FFFFF", "056");
        VerifyToString(ts1, "FFFFFF", "056");
        VerifyToString(ts1, "FFFFFFF", "056");
        VerifyToString(ts1, "hhmmss", "020304");

        ts1 = new TimeSpan(-1, -2, -3, -4, -56);
        VerifyToString(ts1, "d'-'", "1-");
        VerifyToString(ts1, "%d", "1");
        VerifyToString(ts1, "dd", "01");
        VerifyToString(ts1, "ddd", "001");
        VerifyToString(ts1, "dddd", "0001");
        VerifyToString(ts1, "ddddd", "00001");
        VerifyToString(ts1, "dddddd", "000001");
        VerifyToString(ts1, "ddddddd", "0000001");
        VerifyToString(ts1, "dddddddd", "00000001");
        VerifyToString(ts1, "h'-'", "2-");
        VerifyToString(ts1, "%h", "2");
        VerifyToString(ts1, "hh", "02");
        VerifyToString(ts1, "m'-'", "3-");
        VerifyToString(ts1, "%m", "3");
        VerifyToString(ts1, "mm", "03");
        VerifyToString(ts1, "s'-'", "4-");
        VerifyToString(ts1, "%s", "4");
        VerifyToString(ts1, "ss", "04");
        VerifyToString(ts1, "f'-'", "0-");
        VerifyToString(ts1, "ff", "05");
        VerifyToString(ts1, "fff", "056");
        VerifyToString(ts1, "ffff", "0560");
        VerifyToString(ts1, "fffff", "05600");
        VerifyToString(ts1, "ffffff", "056000");
        VerifyToString(ts1, "fffffff", "0560000");
        VerifyToString(ts1, "F'-'", "-");
        VerifyToString(ts1, "FF", "05");
        VerifyToString(ts1, "FFF", "056");
        VerifyToString(ts1, "FFFF", "056");
        VerifyToString(ts1, "FFFFF", "056");
        VerifyToString(ts1, "FFFFFF", "056");
        VerifyToString(ts1, "FFFFFFF", "056");
        VerifyToString(ts1, "hhmmss", "020304");

        ts1 = new TimeSpan(1, 2, 3, 4, 56).Add(new TimeSpan(78));
        VerifyToString(ts1, "'.'F", ".");
        VerifyToString(ts1, "FF", "05");
        VerifyToString(ts1, "FFF", "056");
        VerifyToString(ts1, "FFFF", "056");
        VerifyToString(ts1, "FFFFF", "056");
        VerifyToString(ts1, "FFFFFF", "056007");
        VerifyToString(ts1, "FFFFFFF", "0560078");

        ts1 = new TimeSpan(1, 2, 3, 4).Add(new TimeSpan(789));
        VerifyToString(ts1, "'.'F", ".");
        VerifyToString(ts1, "FF", "");
        VerifyToString(ts1, "FFF", "");
        VerifyToString(ts1, "FFFF", "");
        VerifyToString(ts1, "FFFFF", "00007");
        VerifyToString(ts1, "FFFFFF", "000078");
        VerifyToString(ts1, "FFFFFFF", "0000789");

        // Literals
        ts1 = new TimeSpan(1, 2, 3, 4, 56).Add(new TimeSpan(78));
        VerifyToString(ts1, "d'd'", "1d");
        VerifyToString(ts1, "d' days'", "1 days");
        VerifyToString(ts1, "d' days, 'h' hours, 'm' minutes, 's'.'FFFF' seconds'", "1 days, 2 hours, 3 minutes, 4.056 seconds");

        // Error formats
        foreach (String errorFormat in Support.ErrorFormats)
        {
            ts1 = new TimeSpan(1, 2, 3, 4, 56).Add(new TimeSpan(78));
            VerifyToStringException<FormatException>(ts1, errorFormat);
        }


        // Vary current culture
        Utilities.CurrentCulture = CultureInfo.InvariantCulture;
        foreach (TimeSpan ts in Support.InterestingTimeSpans)
        {
            String defaultFormat = Support.CFormat(ts);
            VerifyToString(ts, defaultFormat);
            VerifyToString(ts, "c", defaultFormat);
            VerifyToString(ts, "t", defaultFormat);
            VerifyToString(ts, "T", defaultFormat);
            VerifyToString(ts, null, defaultFormat);
            VerifyToString(ts, "", defaultFormat);
            VerifyToString(ts, "g", Support.gFormat(ts, "."));
            VerifyToString(ts, "G", Support.GFormat(ts, "."));
        }

        Utilities.CurrentCulture = new CultureInfo("en-US");
        foreach (TimeSpan ts in Support.InterestingTimeSpans)
        {
            String defaultFormat = Support.CFormat(ts);
            VerifyToString(ts, defaultFormat);
            VerifyToString(ts, "c", defaultFormat);
            VerifyToString(ts, "t", defaultFormat);
            VerifyToString(ts, "T", defaultFormat);
            VerifyToString(ts, null, defaultFormat);
            VerifyToString(ts, "", defaultFormat);
            VerifyToString(ts, "g", Support.gFormat(ts, "."));
            VerifyToString(ts, "G", Support.GFormat(ts, "."));
        }

        Utilities.CurrentCulture = new CultureInfo("de-DE");
        foreach (TimeSpan ts in Support.InterestingTimeSpans)
        {
            String defaultFormat = Support.CFormat(ts);
            VerifyToString(ts, defaultFormat);
            VerifyToString(ts, "c", defaultFormat);
            VerifyToString(ts, "t", defaultFormat);
            VerifyToString(ts, "T", defaultFormat);
            VerifyToString(ts, null, defaultFormat);
            VerifyToString(ts, "", defaultFormat);
            VerifyToString(ts, "g", Support.gFormat(ts, ","));
            VerifyToString(ts, "G", Support.GFormat(ts, ","));
        }

    }

    public static void VerifyToString(TimeSpan timeSpan, String expectedResult)
    {
        iCountTestcases++;
        try
        {
            String result = timeSpan.ToString();
            if (verbose)
                Logging.WriteLine("{0} ({1}) ==> {2}", Support.PrintTimeSpan(timeSpan), "default", result);
            if (result != expectedResult)
            {
                iCountErrors++;
                Logging.WriteLine("FAILURE: Input = {0}, Format: '{1}', Expected Return: '{2}', Actual Return: '{3}'", Support.PrintTimeSpan(timeSpan), "default", expectedResult, result);
            }
        }
        catch (Exception ex)
        {
            iCountErrors++;
            Logging.WriteLine("FAILURE: Unexpected Exception, Input = {0}, Format = '{1}', Exception: {2}", Support.PrintTimeSpan(timeSpan), "default", ex);
        }
    }

    public static void VerifyToString(TimeSpan timeSpan, String format, String expectedResult)
    {
        iCountTestcases++;
        try
        {
            String result = timeSpan.ToString(format);
            if (verbose)
                Logging.WriteLine("{0} ('{1}') ==> {2}", Support.PrintTimeSpan(timeSpan), format, result);
            if (result != expectedResult)
            {
                iCountErrors++;
                Logging.WriteLine("FAILURE: Input = {0}, Format: '{1}', Expected Return: '{2}', Actual Return: '{3}'", Support.PrintTimeSpan(timeSpan), format, expectedResult, result);
            }
        }
        catch (Exception ex)
        {
            iCountErrors++;
            Logging.WriteLine("FAILURE: Unexpected Exception, Input = {0}, Format = '{1}', Exception: {2}", Support.PrintTimeSpan(timeSpan), format, ex);
        }
    }

    public static void VerifyToStringException<TException>(TimeSpan timeSpan, String format) where TException : Exception
    {
        iCountTestcases++;
        if (verbose)
            Logging.WriteLine("Expecting {2}: {0} ('{1}')", Support.PrintTimeSpan(timeSpan), format, typeof(TException));
        try
        {
            String result = timeSpan.ToString(format);
            iCountErrors++;
            Logging.WriteLine("FAILURE: Input = {0}, Format: '{1}', Expected Exception: {2}, Actual Return: '{3}'", Support.PrintTimeSpan(timeSpan), format, typeof(TException), result);
        }
        catch (TException)
        {
            //Logging.WriteLine("INFO: {0}: {1}", ex.GetType(), ex.Message);
        }
        catch (Exception ex)
        {
            iCountErrors++;
            Logging.WriteLine("FAILURE: Unexpected Exception, Input = {0}, Format = '{1}', Exception: {2}", Support.PrintTimeSpan(timeSpan), format, ex);
        }
    }
}
