// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// Int16.Parse(String)
/// </summary>
public class Int16Parse1
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;
        retVal = PosTest5() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
        retVal = NegTest5() && retVal;
        retVal = NegTest6() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Normally test a random string of int16 ");

        try
        {
            string str = TestLibrary.Generator.GetInt16(-55).ToString();
            Int16 i1 = Int16.Parse(str);
            Int16 i2 = Convert.ToInt16(str);
            if (i1 != i2)
            {
                TestLibrary.TestFramework.LogError("001", "the result is not the value as expected, the string is " + str);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Test the int16.MaxValue");

        try
        {
            string str = "32767";
            Int16 i1 = Int16.Parse(str);
            if (i1 != 32767)
            {
                TestLibrary.TestFramework.LogError("003", "the result is not the value as expected ");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Test the int16.MinValue");

        try
        {
            string str = "-32768";
            Int16 i1 = Int16.Parse(str);
            if (i1 != -32768)
            {
                TestLibrary.TestFramework.LogError("005", "the result is not the value as expected ");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: The argument with white space in both beginning and the end");

        try
        {
            string str2;
            string str = str2 = TestLibrary.Generator.GetInt16(-55).ToString();
            str = "  " + str;
            str = str + "  ";
            Int16 i1 = Int16.Parse(str);
            Int16 i2 = Int16.Parse(str2);
            if (i1 != i2)
            {
                TestLibrary.TestFramework.LogError("007", "the result is not the value as expected,the string is :" + str);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest5: Test the parameter \"-0\"");

        try
        {
            Int16 i1 = Int16.Parse("-0");
            if (i1 != 0)
            {
                TestLibrary.TestFramework.LogError("009", "the result is not the value as expected ");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: The argument is null reference");

        try
        {
            string str = null;
            Int16 i1 = Int16.Parse(str);
            TestLibrary.TestFramework.LogError("101", "the Method did not throw an ArgumentNullException");
            retVal = false;
        }
        catch (ArgumentNullException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest2: Test format exception 1");

        try
        {
            string str = "-123-567";
            Int16 i1 = Int16.Parse(str);
            TestLibrary.TestFramework.LogError("103", "the Method did not throw a FormatException");
            retVal = false;
        }
        catch (FormatException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("104", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest3: Test format exception 2");

        try
        {
            string str = "98d5t6w7";
            Int16 i1 = Int16.Parse(str);
            TestLibrary.TestFramework.LogError("105", "the Method did not throw a FormatException");
            retVal = false;
        }
        catch (FormatException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("106", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest4: Test format exception 3, the string is white space");

        try
        {
            string str = "  ";
            Int16 i1 = Int16.Parse(str);
            TestLibrary.TestFramework.LogError("107", "the Method did not throw a FormatException");
            retVal = false;
        }
        catch (FormatException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("108", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest5()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest5: The string represents a number less than int16.minvalue");

        try
        {
            string str = (Int16.MinValue - 1).ToString();
            Int16 i1 = Int16.Parse(str);
            TestLibrary.TestFramework.LogError("109", "the Method did not throw a OverflowException");
            retVal = false;
        }
        catch (OverflowException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("110", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest6()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest6: The string represents a number greater than int16.maxvalue");

        try
        {
            string str = (Int16.MaxValue + 1).ToString();
            Int16 i1 = Int16.Parse(str);
            TestLibrary.TestFramework.LogError("111", "the Method did not throw a OverflowException");
            retVal = false;
        }
        catch (OverflowException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("112", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        Int16Parse1 test = new Int16Parse1();

        TestLibrary.TestFramework.BeginTestCase("Int16Parse1");

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
}
