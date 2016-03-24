// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// Int16Equals(Int16)
/// </summary>
public class Int16Equals1
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Test two equal random int16");

        try
        {
            Int16 number1, number2;
            number1 = number2 = TestLibrary.Generator.GetInt16(-55);
            if (!number1.Equals(number2))
            {
                TestLibrary.TestFramework.LogError("001", String.Format("equal two equal number {0} did not return true", number2));
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
            Int16 number2 = this.GetInt16(i, Int16.MaxValue);
            if (number1.Equals(number2))
            {
                TestLibrary.TestFramework.LogError("003", String.Format("equal two unqual number did not return false,the two number is {0}and{1}", number1, number2));
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
            Int16 i2 = 0;
            if (!i1.Equals(i2))
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
    #endregion

    #region Nagetive Test Cases
    #endregion
    #endregion

    public static int Main()
    {
        Int16Equals1 test = new Int16Equals1();

        TestLibrary.TestFramework.BeginTestCase("Int16Equals1");

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
