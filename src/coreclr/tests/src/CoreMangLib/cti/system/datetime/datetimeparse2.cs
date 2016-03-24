// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

public class DateTimeParse2
{
    private const  int c_MIN_STRING_LEN = 1;
    private const  int c_MAX_STRING_LEN = 2048;
    private const  int c_NUM_LOOPS      = 100;

    public static int Main()
    {
        DateTimeParse2 test = new DateTimeParse2();

        TestLibrary.TestFramework.BeginTestCase("DateTimeParse2");

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

        return retVal;
    }

    public bool PosTest1()
    {
        bool     retVal = true;
        string   dateBefore = "";
        DateTime dateAfter;
        MyFormater formater = new MyFormater();

        TestLibrary.TestFramework.BeginScenario("PosTest1: DateTime.Parse(DateTime.Now, formater)");

        try
        {
            dateBefore = DateTime.Now.ToString();

            dateAfter = DateTime.Parse( dateBefore, formater);

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

        TestLibrary.TestFramework.BeginScenario("PosTest2: DateTime.Parse(DateTime.Now, null)");

        try
        {
            dateBefore = DateTime.Now.ToString();

            dateAfter = DateTime.Parse( dateBefore, null);

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

    public bool NegTest1()
    {
        bool      retVal = true;
        MyFormater formater = new MyFormater();

        TestLibrary.TestFramework.BeginScenario("NegTest1: DateTime.Parse(null, formater)");

        try
        {
            DateTime.Parse(null, formater);

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

        TestLibrary.TestFramework.BeginScenario("NegTest2: DateTime.Parse(String.Empty, formater)");

        try
        {
            DateTime.Parse(String.Empty, formater);

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
        bool     retVal = true;
        MyFormater formater = new MyFormater();
        string   strDateTime = "";
        DateTime dateAfter;

        TestLibrary.TestFramework.BeginScenario("NegTest3: DateTime.Parse(<garbage>, formater)");

        try
        {
            for (int i=0; i<c_NUM_LOOPS; i++)
            {
                try
                {
                    strDateTime = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
                    dateAfter = DateTime.Parse(strDateTime, formater);

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
