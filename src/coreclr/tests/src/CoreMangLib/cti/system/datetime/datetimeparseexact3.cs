// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

public class DateTimeParseExact2
{
    private const  int c_MIN_STRING_LEN = 1;
    private const  int c_MAX_STRING_LEN = 2048;
    private const  int c_NUM_LOOPS      = 100;
    private static DateTimeStyles[] c_STYLES = new DateTimeStyles[7] {DateTimeStyles.AdjustToUniversal, DateTimeStyles.AllowInnerWhite, DateTimeStyles.AllowLeadingWhite, DateTimeStyles.AllowTrailingWhite , DateTimeStyles.AllowWhiteSpaces, DateTimeStyles.NoCurrentDateDefault, DateTimeStyles.None };


    public static int Main()
    {
        DateTimeParseExact2 test = new DateTimeParseExact2();

        TestLibrary.TestFramework.BeginTestCase("DateTimeParseExact2");

        if (test.RunTests())
        {
            TestLibrary.TestFramework.EndTestCase();
            TestLibrary.TestFramework.LogInformation("PASS");
            return 100;
        }
        else
        {
            TestLibrary.TestFramework.EndTestCase();
            TestLibrary.TestFramework.LogInformation("FAIL");
            return 0;
        }
    }

    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool     retVal = true;
        MyFormater formater = new MyFormater();
        string   dateBefore = "";
        DateTime dateAfter;

        TestLibrary.TestFramework.BeginScenario("PosTest1: DateTime.ParseExact(DateTime.Now)");

        try
        {
            for(int i=0; i<c_STYLES.Length; i++)
            {
                dateBefore = DateTime.Now.ToString();

                dateAfter = DateTime.ParseExact( dateBefore, new string[] {"G"}, formater, c_STYLES[i] );

                if (!TestLibrary.Utilities.IsWindows && 
                    (c_STYLES[i]==DateTimeStyles.AdjustToUniversal)) // Mac prints offset
                {
                    dateAfter = dateAfter.ToLocalTime();
                }

                if (!dateBefore.Equals(dateAfter.ToString()))
                {
                   TestLibrary.TestFramework.LogError("001", "DateTime.ParseExact(" + dateBefore + ", G, " + c_STYLES[i] + ") did not equal " + dateAfter.ToString());
                   retVal = false;
               }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Failing date: " + dateBefore);
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool     retVal = true;
        string   dateBefore = "";
        DateTime dateAfter;
        MyFormater formater = new MyFormater();

        TestLibrary.TestFramework.BeginScenario("PosTest2: DateTime.ParseExact(DateTime.Now, g, formater, <invalid DateTimeStyles>)");

        try
        {
            for(int i=-1024; i<1024; i++)
            {
                try
                {
                    // skip the valid values
                    if (0 == (i & (int)DateTimeStyles.AdjustToUniversal)
                        && 0 == (i & (int)DateTimeStyles.AssumeUniversal)
                        && 0 == (i & (int)DateTimeStyles.AllowInnerWhite)
                        && 0 == (i & (int)DateTimeStyles.AllowLeadingWhite)
                        && 0 == (i & (int)DateTimeStyles.AllowTrailingWhite)
                        && 0 == (i & (int)DateTimeStyles.AllowWhiteSpaces)
                        && 0 == (i & (int)DateTimeStyles.NoCurrentDateDefault)
                        && i != (int)DateTimeStyles.None
                       )
                    {
                        dateBefore = DateTime.Now.ToString();

                        dateAfter = DateTime.ParseExact( dateBefore, new string[] {"G"}, formater, (DateTimeStyles)i);

                        if (!dateBefore.Equals(dateAfter.ToString()))
                        {
                           TestLibrary.TestFramework.LogError("011", "DateTime.ParseExact(" + dateBefore + ", " + (DateTimeStyles)i + ") did not equal " + dateAfter.ToString());
                            retVal = false;
                        }
                    }
                }
                catch (System.ArgumentException)
                {
                    //
                }
           }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Failing date: " + dateBefore);
            TestLibrary.TestFramework.LogError("012", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest1()
    {
        bool      retVal = true;
        MyFormater formater = new MyFormater();

        TestLibrary.TestFramework.BeginScenario("NegTest1: DateTime.ParseExact(null)");

        try
        {
            try
            {
                for(int i=0; i<c_STYLES.Length; i++)
                {
                     DateTime.ParseExact(null, new string[] {"d"}, formater, c_STYLES[i]);

                     TestLibrary.TestFramework.LogError("029", "DateTime.ParseExact(null, d, " + c_STYLES[i] + ") should have thrown");
                     retVal = false;
                }
            }
            catch (ArgumentNullException)
            {
                // expected
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("030", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool      retVal = true;
        MyFormater formater = new MyFormater();

        TestLibrary.TestFramework.BeginScenario("NegTest2: DateTime.ParseExact(String.Empty)");

        try
        {
            try
            {
                for(int i=0; i<c_STYLES.Length; i++)
                {
                     DateTime.ParseExact(String.Empty, new string[] {"d"}, formater, c_STYLES[i]);

                     TestLibrary.TestFramework.LogError("029", "DateTime.ParseExact(String.Empty, d, " + c_STYLES[i] + ") should have thrown");
                     retVal = false;
                }
            }
            catch (ArgumentNullException)
            {
                // expected
            }
        }
        catch (FormatException)
        {
            // expected
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("032", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool   retVal = true;
        MyFormater formater = new MyFormater();
        string strDateTime = "";
        DateTime dateAfter;
        string[] formats = new string[17] {"d", "D", "f", "F", "g", "G", "m", "M", "r", "R", "s", "t", "T", "u", "U", "y", "Y"};
        string   format;
        int      formatIndex;
       

        TestLibrary.TestFramework.BeginScenario("NegTest3: DateTime.ParseExact(<garbage>)");

        try
        {
            for (int i=0; i<c_NUM_LOOPS; i++)
            {
                for(int j=0; j<c_STYLES.Length; j++)
                {
                    try
                    {
                        formatIndex = TestLibrary.Generator.GetInt32(-55) % 34;

                        if (0 <= formatIndex && formatIndex < 17)
                        {
                            format = formats[formatIndex];
                        }
                        else
                        {
                            format = TestLibrary.Generator.GetChar(-55) + "";
                        }

                        strDateTime = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
                        dateAfter = DateTime.ParseExact(strDateTime, new string[] {format}, formater, c_STYLES[j]);

                        TestLibrary.TestFramework.LogError("033", "DateTime.ParseExact(" + strDateTime + ", "+ format + ", " + c_STYLES[j] + ") should have thrown (" + dateAfter + ")");
                        retVal = false;
                    }
                    catch (FormatException)
                    {
                        // expected
                    }
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("034", "Failing date: " + strDateTime);
            TestLibrary.TestFramework.LogError("034", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest4()
    {
        bool     retVal = true;
        MyFormater formater = new MyFormater();
        string   dateBefore = "";
        DateTime dateAfter;
        string[] formats = null;

        TestLibrary.TestFramework.BeginScenario("NegTest4: DateTime.ParseExact(DateTime.Now, null)");

        try
        {
            dateBefore = DateTime.Now.ToString();

            dateAfter = DateTime.ParseExact( dateBefore, formats, formater, DateTimeStyles.NoCurrentDateDefault );

            TestLibrary.TestFramework.LogError("035", "DateTime.ParseExact(" + dateBefore + ", null, " + DateTimeStyles.NoCurrentDateDefault  + ") should have thrown " + dateAfter.ToString());
            retVal = false;
        }
        catch (System.ArgumentNullException)
        {
            // expected
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("036", "Failing date: " + dateBefore);
            TestLibrary.TestFramework.LogError("036", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
}

public class MyFormater : IFormatProvider
{
    public object GetFormat(Type formatType)
    {
        if (typeof(IFormatProvider) == formatType)
        {
            return this;
        }
        else
        {
            return null;
        }
    }
}

