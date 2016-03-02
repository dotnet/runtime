// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization ;

public class DateTimeParse3
{
    private const  int c_MIN_STRING_LEN = 1;
    private const  int c_MAX_STRING_LEN = 2048;
    private const  int c_NUM_LOOPS      = 100;

    public static int Main()
    {
        DateTimeParse3 test = new DateTimeParse3();

        TestLibrary.TestFramework.BeginTestCase("DateTimeParse3");

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

        // The NoCurrentDateDefault value is the only value that is useful with 
        //   the DateTime.Parse method, because DateTime.Parse always ignores leading, 
        //   trailing, and inner white-space characters.

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool     retVal = true;
        string   dateBefore = "";
        DateTime dateAfter;
        MyFormater formater = new MyFormater();

        TestLibrary.TestFramework.BeginScenario("PosTest1: DateTime.Parse(DateTime.Now, formater, DateTimeStyles.NoCurrentDateDefault)");

        try
        {
            dateBefore = DateTime.Now.ToString();

            dateAfter = DateTime.Parse( dateBefore, formater, DateTimeStyles.NoCurrentDateDefault);

            if (!dateBefore.Equals(dateAfter.ToString()))
            {
                TestLibrary.TestFramework.LogError("001", "DateTime.Parse(" + dateBefore + ") did not equal " + dateAfter.ToString());
                retVal = false;
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: DateTime.Parse(DateTime.Now, null, DateTimeStyles.NoCurrentDateDefault)");

        try
        {
            dateBefore = DateTime.Now.ToString();

            dateAfter = DateTime.Parse( dateBefore, null, DateTimeStyles.NoCurrentDateDefault);

            if (!dateBefore.Equals(dateAfter.ToString()))
            {
                TestLibrary.TestFramework.LogError("009", "DateTime.Parse(" + dateBefore + ") did not equal " + dateAfter.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Failing date: " + dateBefore);
            TestLibrary.TestFramework.LogError("010", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool     retVal = true;
        string   dateBefore = "";
        DateTime dateAfter;
        MyFormater formater = new MyFormater();

        TestLibrary.TestFramework.BeginScenario("PosTest3: DateTime.Parse(DateTime.Now, null, <invalid DateTimeStyles>)");

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

                        dateAfter = DateTime.Parse( dateBefore, null, (DateTimeStyles)i);

                        if (!dateBefore.Equals(dateAfter.ToString()))
                        {
                           TestLibrary.TestFramework.LogError("011", "DateTime.Parse(" + dateBefore + ", " + (DateTimeStyles)i + ") did not equal " + dateAfter.ToString());
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

        TestLibrary.TestFramework.BeginScenario("NegTest1: DateTime.Parse(null, formater, DateTimeStyles.NoCurrentDateDefault)");

        try
        {
            DateTime.Parse(null, formater, DateTimeStyles.NoCurrentDateDefault);

            TestLibrary.TestFramework.LogError("003", "DateTime.Parse(null) should have thrown");
            retVal = false;
        }
        catch (ArgumentNullException)
        {
            // expected
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool      retVal = true;
        MyFormater formater = new MyFormater();

        TestLibrary.TestFramework.BeginScenario("NegTest2: DateTime.Parse(String.Empty, formater, DateTimeStyles.NoCurrentDateDefault)");

        try
        {
            DateTime.Parse(String.Empty, formater, DateTimeStyles.NoCurrentDateDefault);

            TestLibrary.TestFramework.LogError("005", "DateTime.Parse(String.Empty) should have thrown");
            retVal = false;
        }
        catch (FormatException)
        {
            // expected
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
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

        TestLibrary.TestFramework.BeginScenario("NegTest3: DateTime.Parse(<garbage>, formater, DateTimeStyles.NoCurrentDateDefault)");

        try
        {
            for (int i=0; i<c_NUM_LOOPS; i++)
            {
                try
                {
                    strDateTime = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
                    dateAfter = DateTime.Parse(strDateTime, formater, DateTimeStyles.NoCurrentDateDefault);

                    TestLibrary.TestFramework.LogError("007", "DateTime.Parse(" + strDateTime + ") should have thrown (" + dateAfter + ")");
                    retVal = false;
                }
                catch (FormatException)
                {
                    // expected
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Failing date: " + strDateTime);
            TestLibrary.TestFramework.LogError("008", "Unexpected exception: " + e);
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
