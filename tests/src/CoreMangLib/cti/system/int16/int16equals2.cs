// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// Int16Equals(Object)
/// </summary>
public class Int16Equals2
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
        retVal = PosTest6() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Test two equal random int16");

        try
        {
            Int16 number1 = TestLibrary.Generator.GetInt16(-55);
            Object ob = number1;
            if (!number1.Equals(ob))
            {
                TestLibrary.TestFramework.LogError("001", String.Format("equal two equal number {0} did not return true", number1));
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Test two different int16");

        try
        {
            Int16 i = TestLibrary.Generator.GetInt16(-55);
            Int16 number1 = this.GetInt16(Int16.MinValue, (Int16)(i - 1));
            object ob = this.GetInt16(i, Int16.MaxValue);
            if (number1.Equals(ob))
            {
                TestLibrary.TestFramework.LogError("003", String.Format("equal two unqual number did not return false,the two number is {0}and{1}", number1, ob.ToString()));
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Test zero equals zero");

        try
        {
            Int16 i1 = 0;
            object ob = (Int16)0;
            if (!i1.Equals(ob))
            {
                TestLibrary.TestFramework.LogError("005", "0!=0");
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

        TestLibrary.TestFramework.BeginScenario("PosTest4: Test int16MinValue");

        try
        {
            Int16 i1 = Int16.MinValue;
            object ob = Int16.MinValue;
            if (!i1.Equals(ob))
            {
                TestLibrary.TestFramework.LogError("007", "equals error int16MinValue");
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

        TestLibrary.TestFramework.BeginScenario("PosTest5: Test int16MaxValue");

        try
        {
            Int16 i1 = Int16.MaxValue;
            object ob = Int16.MaxValue;
            if (!i1.Equals(ob))
            {
                TestLibrary.TestFramework.LogError("009", "equals error int16MinValue");
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

    public bool PosTest6()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest6: The argument is not int16 type");

        try
        {
            Int16 i1 = TestLibrary.Generator.GetInt16(-55);
            object ob = Convert.ToInt32(i1);
            if (i1.Equals(ob))
            {
                TestLibrary.TestFramework.LogError("011", "equals error int16MinValue");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    #endregion
    #endregion

    public static int Main()
    {
        Int16Equals2 test = new Int16Equals2();

        TestLibrary.TestFramework.BeginTestCase("Int16Equals2");

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
    private Int16 GetInt16(Int16 minValue, Int16 maxValue)
    {
        try
        {
            if (minValue == maxValue)
            {
                return minValue;
            }
            if (minValue < maxValue)
            {
                return (Int16)(minValue + TestLibrary.Generator.GetInt16(-55) % (maxValue - minValue));
            }
        }
        catch
        {
            throw;
        }

        return minValue;
    }
}
